using cddo_users.Core;
using cddo_users.Interface;
using cddo_users.models;
using cddo_users.Repositories;
using cddo_users.Services.Database;
using DocumentFormat.OpenXml.Wordprocessing;

namespace cddo_users.Services
{
    public class DepartmentService : IDepartmentService
    {
        private readonly IDepartmentRepository _departmentRepository;
        private readonly IOrganisationRepository _organisationRepository;

        public DepartmentService(IDepartmentRepository departmentRepository, IOrganisationRepository organisationRepository)
        {
            _departmentRepository = departmentRepository;
            _organisationRepository = organisationRepository;
        }

        public async Task<bool> AssignOrganisationToDepartmentAsync(int departmentId, int organisationId)
        {
            //Validate orgs and departments actually exist
            var depart = await _departmentRepository.GetDepartmentByIdAsync(departmentId);

            if (depart == null)
            {
                return false;
            }

            var organisation = await _organisationRepository.GetOrganisationByIdAsync(organisationId);
            if (organisation == null)
            {
                return false;
            }

            //Check if not already assigned
            bool assignedOrg = await _departmentRepository.CheckAssigned(departmentId, organisationId);
            if (assignedOrg) 
            {
                return await _departmentRepository.ReAssignOrganisationToDepartmentAsync(departmentId, organisationId);
            }

            return await _departmentRepository.AssignOrganisationToDepartmentAsync(departmentId, organisationId);
        }

        public async Task<bool> ReAssignOrganisationToDepartmentAsync(int departmentId, int organisationId)
        {
            //Validate orgs and departments actually exist
            var depart = await _departmentRepository.GetDepartmentByIdAsync(departmentId);

            if (depart == null)
            {
                return false;
            }

            var organisation = await _organisationRepository.GetOrganisationByIdAsync(organisationId);
            if (organisation == null)
            {
                return false;
            }

            return await _departmentRepository.ReAssignOrganisationToDepartmentAsync(departmentId, organisationId);
        }
        public async Task<bool> UnAssignOrganisationToDepartmentAsync(int departmentId, int organisationId)
        {
            //Validate orgs and departments actually exist
            var depart = await _departmentRepository.GetDepartmentByIdAsync(departmentId);

            if (depart == null)
            {
                return false;
            }

            var organisation = await _organisationRepository.GetOrganisationByIdAsync(organisationId);
            if (organisation == null)
            {
                return false;
            }

            return await _departmentRepository.UnAssignOrganisationToDepartmentAsync(departmentId, organisationId);
        }

        public async Task<IEnumerable<Organisation>> GetUnAssignedOrganisation()
        {
            //Validate orgs and departments actually exist
            return await _departmentRepository.GetUnAssignedOrganisationsAsync();
        }
        public async Task<IEnumerable<Organisation>> GetAssignedOrganisations(int id)
        {
            //Validate orgs and departments actually exist
            return await _departmentRepository.GetAssignedOrganisationsAsync(id);
        }
        public async Task<IEnumerable<DepartmentToOrganisationDetail>> GetAllAssignedOrganisation()
        {
            //Validate orgs and departments actually exist
            return await _departmentRepository.GetAllAssignedOrganisationsAsync();
        }

        public async Task<Department> GetDepartmentByIdAsync(int departmentId)
        {
            //Validate orgs and departments actually exist
            return await _departmentRepository.GetDepartmentByIdAsync(departmentId);
        }

        public async Task<(int, IEnumerable<models.Department>)> GetAllPagedDepartmentsAsync(int page, int pageSize, string? searchTerm)
        {
            return await _departmentRepository.GetAllPagedDepartmentsAsync(page, pageSize, searchTerm);
        }
        public async Task<IEnumerable<Department>> GetAllDepartmentsAsync()
        {
            return await _departmentRepository.GetAllDepartmentsAsync();
        }

        public async Task<Department?> CreateDepartmentAsync(string departmentName, int userId)
        {
            return await _departmentRepository.CreateDepartment(departmentName, userId);
        }
    }
}
