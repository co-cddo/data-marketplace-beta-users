using cddo_users.models;
using System.Data.SqlClient;
using Dapper;
using cddo_users.DTOs;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Transactions;
using Ocelot.RequestId;
using System.Data;

namespace cddo_users.Repositories
{
    public class OrganisationRepository : IOrganisationRepository
    {
        private const string Connection = "DefaultConnection";
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public OrganisationRepository(
            IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString(Connection);
        }

        public async Task<int> CreateOrganisationAsync(OrganisationDetail newOrganisationDetail)
        {
            var insertOrganisationQuery = @"
            INSERT INTO Organisations (OrganisationName)
            VALUES (@OrganisationName);
            SELECT CAST(SCOPE_IDENTITY() as int);";

            var insertDomainQuery = @"
            INSERT INTO Domains (DomainName, OrganisationID, OrganisationType, OrganisationFormat, AllowList)
            VALUES (@DomainName, @OrganisationID, @OrganisationType, @OrganisationFormat, @AllowList);";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        var organisationId = await connection.QuerySingleAsync<int>(insertOrganisationQuery, new { newOrganisationDetail.OrganisationName }, transaction);

                        foreach (var domain in newOrganisationDetail.Domains)
                        {

                            var OrganisationType = domain.OrganisationType.ToString();
                            await connection.ExecuteAsync(insertDomainQuery, new
                            {
                                domain.DomainName,
                                OrganisationID = organisationId,
                                OrganisationType,
                                domain.OrganisationFormat,
                                domain.AllowList
                            }, transaction);
                        }

                        await transaction.CommitAsync();
                        return organisationId;
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        return 0;
                    }
                }
            }
        }

        public async Task<int> CreateDepartmentOrganisationAsync(OrganisationDetail newOrganisationDetail)
        {
            var insertOrganisationQuery = @"
            INSERT INTO Organisations (OrganisationName, OrganisationType, Modified)
            VALUES (@OrganisationName, @OrganisationType, @Modified);
            SELECT CAST(SCOPE_IDENTITY() as int);";

            var insertDomainQuery = @"
            INSERT INTO Domains (DomainName, OrganisationID, OrganisationType, OrganisationFormat, AllowList)
            VALUES (@DomainName, @OrganisationID, @OrganisationType, @OrganisationFormat, @AllowList);";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        var organisationType = OrganisationType.MinisterialDepartments.ToString();
                        if (newOrganisationDetail.OrganisationType != null && !string.IsNullOrEmpty(newOrganisationDetail.OrganisationType.ToString()))
                        {
                            organisationType = newOrganisationDetail.OrganisationType.ToString();
                        }

                        var organisationId = await connection.QuerySingleAsync<int>(insertOrganisationQuery, new { newOrganisationDetail.OrganisationName, OrganisationType = organisationType, Modified = DateTime.UtcNow }, transaction);

                        foreach (var domain in newOrganisationDetail.Domains)
                        {
                            await connection.ExecuteAsync(insertDomainQuery, new
                            {
                                domain.DomainName,
                                OrganisationID = organisationId,
                                OrganisationType = organisationType,
                                domain.OrganisationFormat,
                                domain.AllowList
                            }, transaction);
                        }

                        await transaction.CommitAsync();
                        return organisationId;
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        return 0;
                    }
                }
            }
        }

        public async Task UpdateOrganisationAsync(OrganisationDetail organisationDetail)
        {
            var updateQuery = @"
        UPDATE Organisations
        SET 
            OrganisationName = @OrganisationName,
            OrganisationType = @OrganisationType,
            Modified = @Modified,
            ModifiedBy = @ModifiedBy,
            Allowed = @Allowed
        WHERE 
            OrganisationId = @OrganisationId";

            var updateDomainQuery = @"
            UPDATE Domains 
            SET DomainName = @DomainName, 
                OrganisationType = @OrganisationType, 
                OrganisationFormat = @OrganisationFormat,
                AllowList = @AllowList
            WHERE 
            OrganisationId = @OrganisationId;";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        // Map the OrganisationType enum to its string value for database storage
                        var parameters = new
                        {
                            OrganisationId = organisationDetail.OrganisationId,
                            OrganisationName = organisationDetail.OrganisationName,
                            OrganisationType = organisationDetail.OrganisationType?.ToString(),
                            Modified = organisationDetail.Modified ?? DateTime.UtcNow,
                            ModifiedBy = organisationDetail.ModifiedBy,
                            Allowed = organisationDetail.Allowed,
                        };

                        // Execute the query
                        await connection.ExecuteAsync(updateQuery, parameters, transaction);

                        if(!organisationDetail.Domains.Any())
                        {
                            var existingDomains = await GetDomainsByOrganisationId((int)organisationDetail.OrganisationId);
                            organisationDetail.Domains = existingDomains.ToList();
                        }

                        foreach (var domain in organisationDetail.Domains)
                        {

                            var OrganisationType = organisationDetail.OrganisationType.ToString();
                            if(string.IsNullOrEmpty(OrganisationType))
                            {
                                OrganisationType = "MinisterialDepartments";
                            }
                            var allowList = (bool)!organisationDetail.Allowed ? false : domain.AllowList;
                            await connection.ExecuteAsync(updateDomainQuery, new
                            {
                                domain.DomainName,
                                OrganisationID = organisationDetail.OrganisationId,
                                OrganisationType,
                                domain.OrganisationFormat,
                                AllowList = allowList
                            }, transaction);
                        }

                        await transaction.CommitAsync();
                    }
                    catch (Exception)
                    {

                        await transaction.RollbackAsync();
                    }
                }
                    
            }
        }

        public async Task UpdateOrganisationRequestIdAsync(OrganisationRequest organisationRequest, int requestId)
        {
            var updateQuery = @"
        UPDATE Organisations
        SET 
            RequestId = @RequestId,
            Modified = @Modified
        WHERE 
            OrganisationName = @OrganisationName";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var parameters = new
                {
                    OrganisationName = organisationRequest.OrganisationName,
                    Modified = organisationRequest.UpdatedDate ?? DateTime.UtcNow,
                    RequestId = requestId
                };

                // Execute the query
                await connection.ExecuteAsync(updateQuery, parameters);
            }


        }
        public async Task<(IEnumerable<OrganisationDetail> Orgs, int TotalCount)> GetAllOrganisationDetailsAsync(OrganisationFilter filter)
        {

            int offset = (filter.Page - 1) * filter.PageSize;

            string countQuery = @"
            SELECT 
                COUNT(DISTINCT o.OrganisationId) AS TotalCount
            FROM 
                Organisations o
            INNER JOIN 
                Domains d ON o.OrganisationId = d.OrganisationId
            WHERE 
                (
                    (@AllowListTrue = 0 AND @AllowListFalse = 0) 
                    OR (@AllowListTrue = 1 AND d.AllowList = 1) 
                    OR (@AllowListFalse = 1 AND d.AllowList = 0)
                )
                AND (o.Visible IS NULL OR o.Visible <> 0)
                AND (@OrganisationType IS NULL OR o.OrganisationType IN (SELECT value FROM STRING_SPLIT(@OrganisationType, ',')))
                AND (@SearchTerm IS NULL OR o.OrganisationName LIKE '%' + @SearchTerm + '%' OR d.DomainName LIKE '%' + @SearchTerm + '%')
                AND (@Allowed IS NULL OR o.Allowed = @Allowed);
        ";

            string query = @"
            WITH SORTEDrESULTS AS (
                                    SELECT 
                                    o.OrganisationID,
                                    o.OrganisationName, 
                                    o.OrganisationType AS orgType,
                                    o.Visible As OrgVisible,
                                    o.Modified,
                                    o.ModifiedBy,
                                    o.Allowed,
                                    d.DomainID,
                                    d.DomainName, 
                                    d.OrganisationType AS domainType,
                                    d.OrganisationFormat,
                                    d.AllowList,
                                    d.DataShareRequestMailboxAddress,
                                    d.Visible AS DomainVisible,
                                    dpt.Id,
                                    dpt.DepartmentName,
                                    dpt.Active,
                                    dpt.Created,
                                    dpt.CreatedBy,
                        			orq.OrganisationRequestID,
                        			orq.UserName,
                        			orq.[Status],
                        			orq.Reason,
                              	COUNT(d.DomainId) OVER (PARTITION BY o.OrganisationId) as DomainCount,
                              	ROW_NUMBER() OVER (PARTITION BY o.OrganisationId ORDER BY d.DomainId) as RowNum
                        FROM 
                            Organisations o
                        INNER JOIN 
                            Domains d ON o.OrganisationId = d.OrganisationId
                           	LEFT JOIN [DepertmentToOrganisations] dto on dto.OrganisationId = o.OrganisationID
                         LEFT JOIN [Departments] dpt on dpt.Id = dto.DepartmentId
                         LEFT JOIN [OrganisationRequests] orq on orq.OrganisationName = o.OrganisationName
                         GROUP BY o.OrganisationID,
                              o.OrganisationName, 
                              d.DomainID,
                              d.DomainName,
                              d.OrganisationType,
                              o.Modified,
                              o.ModifiedBy,
                              o.OrganisationType,
                              o.Allowed,
                              d.OrganisationFormat,
                              d.AllowList,
                              d.DataShareRequestMailboxAddress,
                              d.Visible,
                              o.Visible,
                              dpt.Id,
                              dpt.DepartmentName,
                              dpt.Active,
                              dpt.Created,
                              dpt.CreatedBy,
                        	  orq.OrganisationRequestID,
                        	  orq.UserName,
                        	  orq.[Status],
                        	  orq.Reason
            )
            
            SELECT OrganisationId, 
                OrganisationName,
                orgType AS OrganisationType,
                Modified,
                ModifiedBy,
                Allowed,
                DomainCount,
                DomainId, 
                DomainName, 
                AllowList,
                domainType AS OrganisationType,
                DataShareRequestMailboxAddress,
            	Id,
            	DepartmentName,
            	Active,
            	Created,
            	CreatedBy,
                OrganisationRequestID,
                UserName,
                [Status],
                Reason
            FROM SORTEDrESULTS
            WHERE RowNum = 1
            AND 
                (
                    (@AllowListTrue = 0 AND @AllowListFalse = 0) 
                    OR (@AllowListTrue = 1 AND AllowList = 1) 
                    OR (@AllowListFalse = 1 AND AllowList = 0)
                )
                AND (OrgVisible IS NULL OR OrgVisible <> 0)
                AND (@OrganisationType IS NULL OR orgType IN (SELECT value FROM STRING_SPLIT(@OrganisationType, ',')))
                AND (@SearchTerm IS NULL OR OrganisationName LIKE '%' + @SearchTerm + '%' OR DomainName LIKE '%' + @SearchTerm + '%')
                AND (@Allowed IS NULL OR Allowed = @Allowed)
           ";

            if(!string.IsNullOrEmpty(filter.SortBy))
            {
                var sortDirection = filter.SortDirection switch 
                { 
                    "Ascending" => "ASC",
                    "Descending" => "DESC",
                    _ => "ASC"
                };
                query += $" ORDER BY {filter.SortBy} {sortDirection}";
            }
            else
            {
                query += $" ORDER BY OrganisationId";
            }

            query += " OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

            using (var connection = new SqlConnection(_configuration.GetConnectionString(Connection)))
            {
                var parameters = new
                {
                    Offset = offset,
                    PageSize = filter.PageSize,
                    AllowListTrue = filter.AllowListTrue,
                    AllowListFalse = filter.AllowListFalse,
                    SearchTerm = filter.SearchTerm,
                    OrganisationType = filter.OrganisationType != null ? string.Join(",", filter.OrganisationType.Select(x => x.ToString())) : null,
                    Allowed = filter.Allowed,
                };

                // Execute the count query to get the total count of organisations
                int totalCount = await connection.ExecuteScalarAsync<int>(countQuery, parameters);

                // Execute the main query to get the paged results
                var organisationDomainDictionary = new Dictionary<int, OrganisationDetail>();

                await connection.QueryAsync<OrganisationDetail, DomainDetail, cddo_users.DTOs.Department, OrganisationRequest, OrganisationDetail>(
                    query,
                    (organisation, domain, department, organisationRequest) =>
                    {
                        if (!organisationDomainDictionary.TryGetValue((int)organisation.OrganisationId, out var organisationEntry))
                        {
                            organisationEntry = organisation;
                            organisationEntry.Domains = new List<DomainDetail>();
                            organisationDomainDictionary.Add((int)organisation.OrganisationId, organisationEntry);
                        }

                        // Add domain to the organisation entry if its DomainId is not 0
                        if (domain.DomainId != 0)
                        {
                            organisationEntry.Domains.Add(domain);
                        }

                        // Assign department and organisation request if they are not null
                        organisationEntry.OrgDepartment = department ?? organisationEntry.OrgDepartment;
                        organisationEntry.OrganisationRequest = organisationRequest ?? organisationEntry.OrganisationRequest;

                        return organisationEntry;
                    },
                    param: parameters,
                    splitOn: "DomainId, Id, OrganisationRequestID");

                return (organisationDomainDictionary.Values, totalCount);
            }
        }

        public async Task<IEnumerable<OrganisationTypeSummaryDto>> GetOrganisationTypeSummariesAsync()
        {
            var query = @"
                            SELECT 
                                OrganisationType, 
                                SUM(CASE WHEN AllowList = 1 THEN 1 ELSE 0 END) AS AllowedCount,
                                SUM(CASE WHEN AllowList = 0 THEN 1 ELSE 0 END) AS NotAllowedCount
                            FROM Domains
                            GROUP BY OrganisationType";

            using (var connection = new SqlConnection(_connectionString))
            {
                var summaries = await connection.QueryAsync<OrganisationTypeSummaryDto>(query);
                return summaries;
            }
        }

        public async Task<IEnumerable<GroupedByFormatDto>> GetDomainsGroupedByFormatAsync()
        {
            var query = @"
                          SELECT 
                        d.OrganisationFormat, 
                        o.OrganisationType, 
                        o.OrganisationName, 
                        d.DomainName,
                        d.DataShareRequestMailboxAddress
                    FROM Domains d
                    INNER JOIN Organisations o ON d.OrganisationID = o.OrganisationID
                    ORDER BY d.OrganisationFormat, o.OrganisationType, o.OrganisationName, d.DomainName";


            var groupedByFormatDict = new Dictionary<string, GroupedByFormatDto>();

            using (var connection = new SqlConnection(_connectionString))
            {
                var queryResults = await connection.QueryAsync(query);

                foreach (var row in queryResults)
                {
                    // Assuming 'row' is dynamic; adjust if using strong types
                    var formatKey = (string)row.OrganisationFormat;

                    if (!groupedByFormatDict.TryGetValue(formatKey, out var formatGroup))
                    {
                        formatGroup = new GroupedByFormatDto { OrganisationFormat = formatKey, Types = new List<TypeGroup>() };
                        groupedByFormatDict.Add(formatKey, formatGroup);
                    }

                    var typeGroup = formatGroup.Types.FirstOrDefault(t => t.OrganisationType == (string)row.OrganisationType);
                    if (typeGroup == null)
                    {
                        typeGroup = new TypeGroup { OrganisationType = (string)row.OrganisationType, Organisations = new List<OrganisationGroup>() };
                        formatGroup.Types.Add(typeGroup);
                    }

                    var organisationGroup = typeGroup.Organisations.FirstOrDefault(o => o.OrganisationName == (string)row.OrganisationName);
                    if (organisationGroup == null)
                    {
                        organisationGroup = new OrganisationGroup { OrganisationName = (string)row.OrganisationName, Domains = new List<DomainDetail>() };
                        typeGroup.Organisations.Add(organisationGroup);
                    }

                    organisationGroup.Domains.Add(new DomainDetail
                    {
                        DomainName = (string)row.DomainName,
                        OrganisationType = row.OrganisationType != null ? (OrganisationType)Enum.Parse(typeof(OrganisationType), row.OrganisationType.ToString(), true) : null,
                        OrganisationFormat = (string)row.OrganisationFormat,
                        DataShareRequestMailboxAddress = (string?)row.DataShareRequestMailboxAddress
                    });
                }
            }

            return groupedByFormatDict.Values;
        }

        public async Task<IEnumerable<OrganisationDomainsGrouped>> GetDomainsGroupedByTypeAsync()
        {
            // Assuming your database query is ready to fetch the necessary information
            // This SQL should join Organisations with Domains and select the required fields
            var sql = @"
                SELECT
                    o.OrganisationName,
                    d.DomainName,
                    o.OrganisationType,
                    d.OrganisationFormat,
                    d.AllowList,
                    d.DataShareRequestMailboxAddress
                FROM Organisations o
                JOIN Domains d ON o.OrganisationID = d.OrganisationID
                ORDER BY o.OrganisationName, o.OrganisationType, d.DomainName";

            var OrganisationGroups = new Dictionary<string, List<DomainDetail>>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var queryResults = await connection.QueryAsync(sql);

                foreach (var row in queryResults)
                {
                    // Assuming row has the properties directly mapped from your SQL query
                    var key = $"{row.OrganisationName}-{row.OrganisationType}";

                    if (!OrganisationGroups.TryGetValue(key, out var list))
                    {
                        list = new List<DomainDetail>();
                        OrganisationGroups[key] = list;
                    }

                    list.Add(new DomainDetail
                    {
                        DomainName = row.DomainName,
                        OrganisationType = row.OrganisationType != null ? (OrganisationType)Enum.Parse(typeof(OrganisationType), row.OrganisationType.ToString(), true) : null,
                        OrganisationFormat = row.OrganisationFormat,
                        AllowList = row.AllowList,
                        DataShareRequestMailboxAddress = row.DataShareRequestMailboxAddress
                    });
                }
            }

            // Transform the dictionary into the desired output format
            var result = OrganisationGroups.Select(kvp => new OrganisationDomainsGrouped
            {
                OrganisationName = kvp.Key.Split('-')[0],
                OrganisationType = kvp.Key.Split('-')[1],
                Domains = kvp.Value
            });

            return result;
        }

        public async Task<Organisation?> GetOrganisationByNameAsync(string organisationName)
        {
            var query = "SELECT TOP 1 * FROM Organisations WHERE [OrganisationName] = @OrganisationName";

            using (var connection = new SqlConnection(_configuration.GetConnectionString(Connection)))
            {
                return await connection.QuerySingleOrDefaultAsync<Organisation>(query, new { OrganisationName = organisationName });
            }
        }


        public async Task<OrganisationDetail?> GetOrganisationDetailByIdAsync(int OrganisationId)
        {
            var query = @" SELECT
                        o.OrganisationID,
                        o.OrganisationName, 
                        o.OrganisationType,
                        o.Modified,
                        o.ModifiedBy,
                        o.Allowed,
                        d.DomainID,
                        d.DomainName, 
                        d.OrganisationType,
                        d.OrganisationFormat,
                        d.AllowList,
                        d.DataShareRequestMailboxAddress,
                        d.Visible,
                        dpt.Id,
                        dpt.DepartmentName,
                        dpt.Active,
                        dpt.Created,
                        dpt.CreatedBy,
                        orq.OrganisationRequestID,
                        orq.UserName,
                        orq.[Status],
                        orq.Reason
                        FROM Organisations o
                        LEFT JOIN Domains d ON o.OrganisationID = d.OrganisationID 
                                             AND (d.Visible = 1 OR d.Visible IS NULL)
                        LEFT JOIN [DepertmentToOrganisations] dto on dto.OrganisationId = o.OrganisationID
                        LEFT JOIN [Departments] dpt on dpt.Id = dto.DepartmentId
                        LEFT JOIN [OrganisationRequests] orq on orq.OrganisationName = o.OrganisationName
                        WHERE o.OrganisationID = @OrganisationID
                        GROUP BY o.OrganisationID,
                              o.OrganisationName,
                              d.DomainID,
                              d.DomainName,
                              o.OrganisationType,
                              o.Allowed,
                              o.Modified,
                              o.ModifiedBy,
                              d.OrganisationType,
                              d.OrganisationFormat,
                              d.AllowList,
                              d.DataShareRequestMailboxAddress,
                              d.Visible,
                              dpt.Id,
                              dpt.DepartmentName,
                              dpt.Active,
                              dpt.Created,
                              dpt.CreatedBy,
                              orq.OrganisationRequestID,
                              orq.UserName,
                              orq.[Status],
                              orq.Reason
                        ORDER BY d.DomainName
                        ";

            using (var connection = new SqlConnection(_connectionString))
            {
                var OrganisationDict = new Dictionary<int, OrganisationDetail>();

                var Organisation = await connection.QueryAsync<OrganisationDetail, DomainDetail, cddo_users.DTOs.Department, OrganisationRequest, OrganisationDetail>(
                    query,
                    (org, domain, department, organisationRequest) =>
                    {
                        if (!OrganisationDict.TryGetValue((int)org.OrganisationId, out var OrganisationDetail))
                        {
                            OrganisationDetail = org;
                            OrganisationDetail.Domains = new List<DomainDetail>();
                            OrganisationDict.Add((int)OrganisationDetail.OrganisationId, OrganisationDetail);
                        }

                        if (domain != null)
                        {
                            OrganisationDetail.Domains.Add(domain);
                        }
                        if (department != null)
                        {
                            OrganisationDetail.OrgDepartment = department;
                        }
                        if(organisationRequest != null)
                        {
                            OrganisationDetail.OrganisationRequest = organisationRequest;
                        }

                        return OrganisationDetail;
                    },
                    new { OrganisationID = OrganisationId },
                    splitOn: "DomainID, Id, OrganisationRequestID");

                return Organisation.Distinct().FirstOrDefault();
            }
        }

        public async Task<IEnumerable<OrganisationDetail>> SearchOrganisationsAndDomainsAsync(string searchTerm)
        {
            var query = @"
                SELECT
                    o.OrganisationID,
                    o.OrganisationName,
                    o.Modified,
                    o.ModifiedBy,
                    o.Allowed,
                    d.DomainID,
                    d.DomainName,
                    d.OrganisationType,
                    d.OrganisationFormat,
                    d.AllowList,
                    d.DataShareRequestMailboxAddress
                FROM Organisations o
                LEFT JOIN Domains d ON o.OrganisationID = d.OrganisationID
                WHERE o.OrganisationName LIKE '%' + @SearchTerm + '%'
                   OR d.DomainName LIKE '%' + @SearchTerm + '%'
                ORDER BY o.OrganisationName, d.DomainName";

            using (var connection = new SqlConnection(_connectionString))
            {
                var OrganisationDict = new Dictionary<int, OrganisationDetail>();

                await connection.QueryAsync<OrganisationDetail, DomainDetail, OrganisationDetail>(
                    query,
                    (org, domain) =>
                    {
                        if (!OrganisationDict.TryGetValue((int)org.OrganisationId, out var OrganisationDetail))
                        {
                            OrganisationDetail = org;
                            OrganisationDetail.Domains = new List<DomainDetail>();
                            OrganisationDict.Add((int)OrganisationDetail.OrganisationId, OrganisationDetail);
                        }

                        if (domain != null)
                        {
                            OrganisationDetail.Domains.Add(domain);
                        }

                        return OrganisationDetail;
                    },
                    new { SearchTerm = searchTerm },
                    splitOn: "DomainID");

                return OrganisationDict.Values;
            }
        }

        public async Task AddDomainToOrganisationAsync(int OrganisationId, DomainDetail domain)
        {
            var query = @"
        INSERT INTO Domains (DomainName, OrganisationID, OrganisationType, OrganisationFormat, AllowList, DataShareRequestMailboxAddress)
        VALUES (@DomainName, @OrganisationID, @OrganisationType, @OrganisationFormat, @AllowList, @DataShareRequestMailboxAddress)";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.ExecuteAsync(query, new
                {
                    domain.DomainName,
                    OrganisationID = OrganisationId,
                    domain.OrganisationType,
                    domain.OrganisationFormat,
                    domain.AllowList,
                    domain.DataShareRequestMailboxAddress
                });
            }
        }

        public async Task UpdateOrganisationModifiedDate(int organisationId, int userId)
        {
            var updateOrganisationModified = @"
                UPDATE Organisations
                SET 
                    Modified = @Modified,
                    ModifiedBy = @ModifiedBy
                WHERE OrganisationID = @OrganisationID;";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                       await connection.ExecuteAsync(
                            updateOrganisationModified,
                            new
                            {
                                OrganisationID = organisationId,
                                Modified = DateTime.UtcNow,
                                ModifiedBy = userId
                            },
                            transaction
                        );

                        await transaction.CommitAsync();
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                    }
                }
            }
        }

        public async Task SetOrganisationAllowListAsync(int OrganisationId, bool allowList)
        {
            var query = "UPDATE Domains SET AllowList = @AllowList WHERE OrganisationID = @OrganisationID";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.ExecuteAsync(query, new { AllowList = allowList, OrganisationID = OrganisationId });
            }
        }

        public async Task SetDomainAllowListAsync(int domainId, bool allowList)
        {
            var query = "UPDATE Domains SET AllowList = @AllowList WHERE DomainID = @DomainID";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.ExecuteAsync(query, new { AllowList = allowList, DomainID = domainId });
            }
        }

        public async Task RemoveDomainFromOrganisationAsync(int domainId)
        {
            var query = "UPDATE Domains SET Visible = 0 WHERE DomainID = @DomainID";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.ExecuteAsync(query, new { DomainID = domainId });
            }
        }

        public async Task DeleteOrganisationAsync(int OrganisationId)
        {
            var query = "UPDATE Organisations SET Visible = 0 WHERE OrganisationID = @OrganisationID";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.ExecuteAsync(query, new { OrganisationID = OrganisationId });
            }
        }

        public async Task<Organisation?> GetOrganisationByIdAsync(int id)
        {
            var query = "SELECT * FROM Organisations WHERE [OrganisationID] = @Id";

            using (var connection = new SqlConnection(_configuration.GetConnectionString(Connection)))
            {
                return await connection.QuerySingleOrDefaultAsync<Organisation>(query, new { Id = id });
            }
        }

        public async Task<IEnumerable<DomainDetail>> GetDomainsByOrganisationId(int organisationId)
        {
            await using var connection = new SqlConnection(_configuration.GetConnectionString(Connection));

            const string query = @"
                    SELECT
	                    [d].[DomainID] AS DomainId,
	                    [d].[DomainName] AS DomainName,
	                    [o].[OrganisationType] AS OrganisationType,
	                    [d].[OrganisationFormat] AS OrganisationFormat,
	                    [d].[AllowList] AS AllowList,
	                    [d].[DataShareRequestMailboxAddress] AS DataShareRequestMailboxAddress
                    FROM [dbo].[Domains] [d]
                    INNER JOIN [dbo].Organisations o
                    ON o.[OrganisationID] = [d].[OrganisationID]
                    WHERE [d].[OrganisationID] = @OrganisationId
                    AND AllowList = 1
                    AND [o].Visible IS NULL OR [o].Visible <> 0";

            return await connection.QueryAsync<DomainDetail>(query, new
            {
                OrganisationId = organisationId
            });
        }

        public async Task<DomainDetail> SetDataShareRequestMailboxAddressAsync(int domainId, string? dataShareRequestMailboxAddress)
        {
            const string query = @"
                UPDATE [dbo].[Domains]
                SET [DataShareRequestMailboxAddress] = @DataShareRequestMailboxAddress
                WHERE [DomainId] = @DomainId";

            await using (var connection = new SqlConnection(_configuration.GetConnectionString(Connection)))
            {
                await connection.ExecuteScalarAsync(query, new
                {
                    DomainId = domainId,
                    DataShareRequestMailboxAddress = dataShareRequestMailboxAddress
                });
            }

            return await GetDomainDetailAsync(domainId);
        }

        private async Task<DomainDetail> GetDomainDetailAsync(int domainId)
        {
            await using var connection = new SqlConnection(_configuration.GetConnectionString(Connection));

            const string query = @"
                    SELECT
	                    [d].[DomainID] AS DomainId,
	                    [d].[DomainName] AS DomainName,
	                    [o].[OrganisationType] AS OrganisationType,
	                    [d].[OrganisationFormat] AS OrganisationFormat,
	                    [d].[AllowList] AS AllowList,
	                    [d].[DataShareRequestMailboxAddress] AS DataShareRequestMailboxAddress
                    FROM [dbo].[Domains] [d]
                    INNER JOIN [dbo].Organisations o
                    ON o.[OrganisationID] = [d].[OrganisationID]
                    WHERE [d].[DomainID] = @DomainId";

            return await connection.QuerySingleAsync<DomainDetail>(query, new
            {
                DomainId = domainId
            });
        }

        public async Task<int> CreateOrganisationRequestAsync(OrganisationRequest organisationRequest)
        {
            var insertOrganisationQuery = @"
                INSERT INTO OrganisationRequests (OrganisationName, OrganisationType, OrganisationFormat, DomainName, UserName, CreatedBy, CreatedDate, Status, 
                                      ApprovedBy, ApprovedDate, RejectedBy, RejectedDate)
                VALUES (@OrganisationName, @OrganisationType, @OrganisationFormat, @DomainName, @UserName, @CreatedBy, @CreatedDate, @Status, 
                                     @ApprovedBy, @ApprovedDate, @RejectedBy, @RejectedDate);
                SELECT CAST(SCOPE_IDENTITY() as int);";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        // Insert into OrganisationRequests
                        var OrganisationType = organisationRequest.OrganisationType.ToString();
                        var organisationId = await connection.QuerySingleAsync<int>(
                            insertOrganisationQuery,
                            new
                            {
                                organisationRequest.OrganisationName,
                                OrganisationType,
                                organisationRequest.OrganisationFormat,
                                organisationRequest.DomainName,
                                organisationRequest.UserName,
                                organisationRequest.CreatedBy,
                                CreatedDate = DateTime.UtcNow,
                                Status = "Pending",
                                organisationRequest.ApprovedBy,
                                organisationRequest.ApprovedDate,
                                organisationRequest.RejectedBy,
                                organisationRequest.RejectedDate,
                            },
                            transaction
                        );

                        await transaction.CommitAsync();
                        return organisationId;
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        return 0;
                    }
                }
            }
        }

        public async Task<bool> UpdateOrganisationRequestAsync(int organisationRequestId, OrganisationRequest updatedRequest)
        {
            var updateOrganisationQuery = @"
                UPDATE OrganisationRequests
                SET 
                    OrganisationName = @OrganisationName,
                    OrganisationType = @OrganisationType,
                    OrganisationFormat = @OrganisationFormat,
                    DomainName = @DomainName,
                    UpdatedBy = @UpdatedBy,
                    UpdatedDate = @UpdatedDate,
                    Status = @Status,
                    Reason = @Reason,
                    ApprovedBy = @ApprovedBy,
                    ApprovedDate = @ApprovedDate,
                    RejectedBy = @RejectedBy,
                    RejectedDate = @RejectedDate
                WHERE OrganisationRequestID = @OrganisationRequestID;";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        if (updatedRequest.Status == "Rejected")
                        {
                            updatedRequest.RejectedBy = updatedRequest.UpdatedBy;
                            updatedRequest.RejectedDate = DateTime.UtcNow;
                        }

                        if (updatedRequest.Status == "Approved")
                        {
                            updatedRequest.ApprovedBy = updatedRequest.UpdatedBy;
                            updatedRequest.ApprovedDate = DateTime.UtcNow;
                        }

                        var OrganisationType = updatedRequest.OrganisationType.ToString();

                        var rowsAffected = await connection.ExecuteAsync(
                            updateOrganisationQuery,
                            new
                            {
                                OrganisationRequestID = organisationRequestId,
                                updatedRequest.OrganisationName,
                                OrganisationType,
                                updatedRequest.OrganisationFormat,
                                updatedRequest.DomainName,
                                updatedRequest.UpdatedBy,
                                UpdatedDate = DateTime.UtcNow,
                                updatedRequest.Status,
                                updatedRequest.Reason,
                                updatedRequest.ApprovedBy,
                                updatedRequest.ApprovedDate,
                                updatedRequest.RejectedBy,
                                updatedRequest.RejectedDate,
                            },
                            transaction
                        );

                        await transaction.CommitAsync();
                        return rowsAffected > 0;
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        return false;
                    }
                }
            }
        }

        public async Task<IEnumerable<OrganisationRequest>> GetAllOrganisationRequestsAsync()
        {
            var selectAllQuery = "SELECT * FROM OrganisationRequests;";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var rows = await connection.QueryAsync(selectAllQuery);

                var requests = rows.Select(row =>
                {
                    var orgRequest = new OrganisationRequest
                    {
                        OrganisationRequestID = row.OrganisationRequestID,
                        ApprovedBy = row.ApprovedBy,
                        ApprovedDate = row.ApprovedDate,
                        CreatedBy = row.CreatedBy,
                        CreatedDate = row.CreatedDate,
                        DomainName = row.DomainName,
                        OrganisationFormat = row.OrganisationFormat,
                        OrganisationName = row.OrganisationName,
                        Reason = row.Reason,
                        RejectedBy = row.RejectedBy,
                        RejectedDate = row.RejectedDate,
                        Status = row.Status,
                        UpdatedBy = row.UpdatedBy ?? 0,
                        UpdatedDate = row.UpdatedDate,
                        UserName = row.UserName
                    };

                    if (!Enum.TryParse<OrganisationType>(row.OrganisationType.ToString(), true, out OrganisationType orgType))
                    {
                        orgRequest.OrganisationType = null; 
                    }
                    else
                    {
                        orgRequest.OrganisationType = orgType;
                    }

                    return orgRequest;
                });

                return requests;
            }
        }

        public async Task<OrganisationRequest> GetOrganisationRequestByIdAsync(int id)
        {
            var selectByIdQuery = "SELECT * FROM OrganisationRequests WHERE OrganisationRequestID = @Id;";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var row = await connection.QuerySingleOrDefaultAsync(selectByIdQuery, new { Id = id });
                if(row != null)
                {
                    var orgRequest = new OrganisationRequest
                    {
                        OrganisationRequestID = row.OrganisationRequestID,
                        ApprovedBy = row.ApprovedBy,
                        ApprovedDate = row.ApprovedDate,
                        CreatedBy = row.CreatedBy,
                        CreatedDate = row.CreatedDate,
                        DomainName = row.DomainName,
                        OrganisationFormat = row.OrganisationFormat,
                        OrganisationName = row.OrganisationName,
                        Reason = row.Reason,
                        RejectedBy = row.RejectedBy,
                        RejectedDate = row.RejectedDate,
                        Status = row.Status,
                        UpdatedBy = row.UpdatedBy ?? 0,
                        UpdatedDate = row.UpdatedDate,
                        UserName = row.UserName
                    };

                    if (!Enum.TryParse<OrganisationType>(row.OrganisationType.ToString(), true, out OrganisationType orgType))
                    {
                        orgRequest.OrganisationType = null;
                    }
                    else
                    {
                        orgRequest.OrganisationType = orgType;
                    }

                    return orgRequest;
                }
               
                return new OrganisationRequest();
            }
        }

        public async Task<DomainDetail?> GetOrganisationDomainByNameAsync(string domainName)
        {
            await using var connection = new SqlConnection(_configuration.GetConnectionString(Connection));

            const string query = @"
                    SELECT
	                    [d].[DomainID] AS DomainId,
	                    [d].[DomainName] AS DomainName,
	                    [d].[OrganisationFormat] AS OrganisationFormat,
	                    [d].[AllowList] AS AllowList,
	                    [d].[DataShareRequestMailboxAddress] AS DataShareRequestMailboxAddress
                    FROM [dbo].[Domains] [d]
                    WHERE [d].[DomainName] = @DomainName";

            var result = await connection.QuerySingleOrDefaultAsync<DomainDetail?>(query, new
            {
                DomainName = domainName
            });

            return result;
        }

        public async Task<OrganisationRequest?> GetOrganisationRequestByOrganisationNameAsync(string organisationName, string domainName)
        {
            await using var connection = new SqlConnection(_configuration.GetConnectionString(Connection));

            const string query = @"
                    SELECT *
                    FROM [dbo].[OrganisationRequests]
                    WHERE [OrganisationName] = @DomainName
                    OR [DomainName] = @DomainName";

            var result = await connection.QuerySingleOrDefaultAsync<OrganisationRequest?>(query, new
            {
                OrganisationName = organisationName,
                DomainName = domainName

            });

            return result;
        }
    }
}
