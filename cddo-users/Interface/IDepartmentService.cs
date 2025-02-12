using cddo_users.models;

namespace cddo_users.Interface
{
    public interface IDepartmentService
    {
        Task<bool> AssignOrganisationToDepartmentAsync(int departmentId, int organisationId);
        Task<Department> GetDepartmentByIdAsync(int departmentId);
        Task<(int, IEnumerable<Department>)> GetAllPagedDepartmentsAsync(int page, int pageSize, string? searchTerm);
        Task<bool> ReAssignOrganisationToDepartmentAsync(int departmentId, int organisationId);
        Task<bool> UnAssignOrganisationToDepartmentAsync(int departmentId, int organisationId);

        Task<IEnumerable<Organisation>> GetUnAssignedOrganisation();
        Task<IEnumerable<DepartmentToOrganisationDetail>> GetAllAssignedOrganisation();
        Task<IEnumerable<Organisation>> GetAssignedOrganisations(int id);
        Task<Department?> CreateDepartmentAsync(string departmentName, int userId);
        Task<IEnumerable<Department>> GetAllDepartmentsAsync();
    }
}