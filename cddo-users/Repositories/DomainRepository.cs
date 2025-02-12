using cddo_users.models;
using Dapper;
using System.Data.SqlClient;

namespace cddo_users.Repositories
{
    public class DomainRepository : IDomainRepository
    {
        private readonly IConfiguration _configuration;
        private const string Connection = "DefaultConnection";

        public DomainRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<int> InsertDomainAsync(Domain domain)
        {
            var query = @"
            INSERT INTO Domains (DomainName, OrganisationID, OrganisationType, OrganisationFormat, AllowList)
            OUTPUT INSERTED.DomainID
            VALUES (@DomainName, @OrganisationID, @OrganisationType, @OrganisationFormat, @AllowList);";

            using (var connection = new SqlConnection(_configuration.GetConnectionString(Connection)))
            {
                await connection.OpenAsync();
                return await connection.ExecuteScalarAsync<int>(query, domain);
            }
        }

        public async Task<bool> UpdateDomainAsync(Domain domain)
        {
            var query = @"
            UPDATE Domains
            SET OrganisationID = @OrganisationID, 
                OrganisationType = @OrganisationType, 
                OrganisationFormat = @OrganisationFormat, 
                AllowList = @AllowList
            WHERE DomainID = @DomainID;";

            using (var connection = new SqlConnection(_configuration.GetConnectionString(Connection)))
            {
                await connection.OpenAsync();
                var affectedRows = await connection.ExecuteAsync(query, domain);
                return affectedRows > 0;
            }
        }

        public async Task<Domain> GetDomainByIdAsync(int domainId)
        {
            var query = "SELECT * FROM Domains WHERE DomainID = @DomainID;";

            using (var connection = new SqlConnection(_configuration.GetConnectionString(Connection)))
            {
                await connection.OpenAsync();
                return await connection.QuerySingleOrDefaultAsync<Domain>(query, new { DomainID = domainId });
            }
        }

        public async Task<Domain> GetDomainByNameAsync(string domainName)
        {
            var query = "SELECT * FROM Domains WHERE DomainName = @DomainName;";

            using (var connection = new SqlConnection(_configuration.GetConnectionString(Connection)))
            {
                await connection.OpenAsync();
                return await connection.QuerySingleOrDefaultAsync<Domain>(query, new { DomainName = domainName });
            }
        }

        public async Task<IEnumerable<Domain>> GetAllDomainsAsync()
        {
            var query = "SELECT * FROM Domains;";

            using (var connection = new SqlConnection(_configuration.GetConnectionString(Connection)))
            {
                await connection.OpenAsync();
                return await connection.QueryAsync<Domain>(query);
            }
        }

        public async Task<int> InsertOrUpdateDomainAsync(Domain domain)
        {
            // This query checks if a domain exists and updates it, or inserts a new one if it doesn't
            var query = @"
        DECLARE @ExistingDomainId INT;
        SELECT @ExistingDomainId = DomainID FROM Domains WHERE DomainName = @DomainName;
        
        IF (@ExistingDomainId IS NULL)
        BEGIN
            INSERT INTO Domains (DomainName, OrganisationID, OrganisationType, OrganisationFormat, AllowList)
            VALUES (@DomainName, @OrganisationID, @OrganisationType, @OrganisationFormat, @AllowList);
            SET @ExistingDomainId = SCOPE_IDENTITY();
        END
        ELSE
        BEGIN
            UPDATE Domains
            SET OrganisationID = @OrganisationID, OrganisationType = @OrganisationType, OrganisationFormat = @OrganisationFormat, AllowList = @AllowList
            WHERE DomainID = @ExistingDomainId;
        END
        
        SELECT @ExistingDomainId;";

            using (var connection = new SqlConnection(_configuration.GetConnectionString(Connection)))
            {
                await connection.OpenAsync();
                return await connection.ExecuteScalarAsync<int>(query, new
                {
                    domain.DomainName,
                    domain.OrganisationId,
                    domain.OrganisationType,
                    domain.OrganisationFormat,
                    domain.AllowList
                });
            }
        }
    }

}
