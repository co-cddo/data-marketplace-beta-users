using cddo_users.DTOs;
using cddo_users.models;
using Dapper;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Data.SqlClient;

namespace cddo_users.Services.Database
{
    public class DepartmentRepository : IDepartmentRepository
    {
        private readonly IConfiguration _configuration;

        private readonly string DefaultConnection;

        public DepartmentRepository(IConfiguration configuration)
        {
            if(configuration == null) throw new ArgumentNullException(nameof(configuration));

            _configuration = configuration;
            DefaultConnection = _configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        }

        public async Task<bool> AssignOrganisationToDepartmentAsync(int departmentId, int organisationId)
        {
            var query = @"
            INSERT INTO [dbo].[DepertmentToOrganisations]
                        ([DepartmentId]
                        ,[OrganisationId])
                VALUES
                        (@DepartmentId
                        ,@OrganisationId);";

            try
            {
                using (var connection = new SqlConnection(DefaultConnection))
                {
                    await connection.OpenAsync();
                    var affectedRows = await connection.ExecuteAsync(query,
                        new
                        {
                            DepartmentId = departmentId,
                            OrganisationId = organisationId,
                        });
                    return affectedRows > 0;
                }
            }
            catch (Exception ex)
            {

                return false;
            }
            
        }

        public async Task<bool> CheckAssigned(int departmentId, int organisationId)
        {
            var query = "SELECT * FROM DepertmentToOrganisations WHERE [OrganisationId] = @OrganisationId";

            using (var connection = new SqlConnection(DefaultConnection))
            {
                var assigned = await connection.ExecuteAsync(query, new { OrganisationId = organisationId });

                return assigned > 0;
            }
        }

        public async Task<bool> ReAssignOrganisationToDepartmentAsync(int departmentId, int organisationId)
        {
            var query = @"
            UPDATE [dbo].[DepertmentToOrganisations]
               SET [DepartmentId] = @DepartmentId
            WHERE [OrganisationId] = @OrganisationId;";

            using (var connection = new SqlConnection(DefaultConnection))
            {
                await connection.OpenAsync();
                var affectedRows = await connection.ExecuteAsync(query,
                    new
                    {
                        DepartmentId = departmentId,
                        OrganisationId = organisationId,
                    });
                return affectedRows > 0;
            }
        }

        public async Task<bool> UnAssignOrganisationToDepartmentAsync(int departmentId, int organisationId)
        {
            var query = @"
            DELETE FROM [dbo].[DepertmentToOrganisations]
                  WHERE [DepartmentId] = @DepartmentId
                  AND [OrganisationId] = @OrganisationId;";

            using (var connection = new SqlConnection(DefaultConnection))
            {
                await connection.OpenAsync();
                var affectedRows = await connection.ExecuteAsync(query,
                    new
                    {
                        DepartmentId = departmentId,
                        OrganisationId = organisationId,
                    });
                return affectedRows > 0;
            }
        }

        public async Task<IEnumerable<Organisation>> GetUnAssignedOrganisationsAsync()
        {
            var query = @"
                        SELECT a.*
                        FROM Organisations a
                        LEFT JOIN DepertmentToOrganisations b ON a.OrganisationID = b.OrganisationId
                        WHERE b.OrganisationId IS NULL;";

            using (var connection = new SqlConnection(DefaultConnection))
            {
                var summaries = await connection.QueryAsync<Organisation>(query);
                return summaries;
            }
        }

        public async Task<IEnumerable<Organisation>> GetAssignedOrganisationsAsync(int id)
        {
            var query = $@"
                        SELECT a.*
                        FROM Organisations a
                        INNER JOIN DepertmentToOrganisations b ON a.OrganisationID = b.OrganisationId
                        WHERE b.DepartmentId = @id;";

            using (var connection = new SqlConnection(DefaultConnection))
            {
                var summaries = await connection.QueryAsync<Organisation>(query, new {id = id});
                return summaries;
            }
        }

        public async Task<IEnumerable<DepartmentToOrganisationDetail>> GetAllAssignedOrganisationsAsync()
        {
            var query = @"SELECT a.OrganisationID,
	                             a.OrganisationName,
	                             b.DepartmentId,
	                             d.DepartmentName
                        FROM Organisations a
                        inner JOIN DepertmentToOrganisations b ON a.OrganisationID = b.OrganisationId
                        INNER JOIN Departments d on d.Id = b.DepartmentId;";

            using (var connection = new SqlConnection(DefaultConnection))
            {
                var summaries = await connection.QueryAsync<DepartmentToOrganisationDetail>(query);
                return summaries;
            }
        }

        //DepartmentToOrganisationDetail

        public async Task<models.Department?> GetDepartmentByIdAsync(int id)
        {
            var query = "SELECT * FROM Departments WHERE [Id] = @Id";

            using (var connection = new SqlConnection(DefaultConnection))
            {
                return await connection.QuerySingleOrDefaultAsync<models.Department>(query, new { Id = id });
            }
        }

        public async Task<(int, IEnumerable<models.Department>)> GetAllPagedDepartmentsAsync(int page, int pageSize, string? searchTerm)
        {
            int offset = (page - 1) * pageSize;
            string countQuery = @"
            SELECT 
                COUNT(*) AS TotalCount
            FROM 
                Departments
            WHERE (@SearchTerm IS NULL OR DepartmentName LIKE '%' + @SearchTerm + '%');";
            

            var query = @"SELECT d.*, u.UserName as CreatedByName FROM Departments d
                          INNER JOIN Users u ON u.UserID = d.CreatedBy
                          WHERE [Active] = 1 
                          AND (@SearchTerm IS NULL OR DepartmentName LIKE '%' + @SearchTerm + '%')
                          ORDER BY 
                              Id
                          OFFSET 
                              @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

            using (var connection = new SqlConnection(DefaultConnection))
            {
                var parameters = new
                {
                    Offset = offset,
                    PageSize = pageSize,
                    SearchTerm = searchTerm
                };

                // Execute the count query to get the total count of organisations
                int totalCount = await connection.ExecuteScalarAsync<int>(countQuery, parameters);

                var departments = await connection.QueryAsync<models.Department>(query, parameters);
                return (totalCount, departments);
            }
        }

        public async Task<models.Department?> CreateDepartment(string departmentName, int userId)
        {
            var query = "INSERT INTO Departments (DepartmentName, Active, Created, CreatedBy) OUTPUT INSERTED.* VALUES (@DepartmentName, 1, GETDATE(), @CreatedBy)";

            using (var connection = new SqlConnection(DefaultConnection))
            {
                var result = await connection.QuerySingleOrDefaultAsync<models.Department>(query, new { DepartmentName = departmentName, CreatedBy = userId});
                return result;
            }
        }

        public async Task<IEnumerable<models.Department>> GetAllDepartmentsAsync()
        {
            var query = @"SELECT *
                        FROM Departments 
                        WHERE Active = 1";

            using (var connection = new SqlConnection(DefaultConnection))
            {
                var summaries = await connection.QueryAsync<models.Department>(query);
                return summaries;
            }
        }
    }
}
