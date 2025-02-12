using cddo_users.models;

namespace cddo_users.Services.Database
{
    public interface IDepartmentRepository
    {
        Task<bool> AssignOrganisationToDepartmentAsync(int departmentId, int organisationId);
        Task<(int, IEnumerable<models.Department>)> GetAllPagedDepartmentsAsync(int page, int pageSize, string? searchTerm);
        Task<Department?> GetDepartmentByIdAsync(int id);
        Task<IEnumerable<Organisation>> GetAssignedOrganisationsAsync(int id);
        Task<IEnumerable<Organisation>> GetUnAssignedOrganisationsAsync();
        Task<bool> ReAssignOrganisationToDepartmentAsync(int departmentId, int organisationId);
        Task<bool> UnAssignOrganisationToDepartmentAsync(int departmentId, int organisationId);
        Task<bool> CheckAssigned(int departmentId, int organisationId);

        Task<IEnumerable<DepartmentToOrganisationDetail>> GetAllAssignedOrganisationsAsync();
        Task<models.Department?> CreateDepartment(string departmentName, int userId);
        Task<IEnumerable<models.Department>> GetAllDepartmentsAsync();
    }
}