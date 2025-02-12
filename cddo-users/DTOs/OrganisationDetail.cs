using cddo_users.models;

namespace cddo_users.DTOs
{
    public class OrganisationDetail
    {
        public int? OrganisationId { get; set; }
        public string? OrganisationName { get; set; }
        public OrganisationType? OrganisationType { get; set; }
        public List<DomainDetail>? Domains { get; set; }
        public int DomainCount { get; set; } = 0;
        public Department? OrgDepartment { get; set; }
        public DateTime? Modified { get; set; }
        public int? ModifiedBy { get; set; }
        public OrganisationRequest? OrganisationRequest { get; set; }
        public bool? Allowed { get; set; }

    }

    public class Department
    {
        public int? Id { get; set; }
        public string? DepartmentName { get; set; }
        public bool? Active { get; set; }
        public DateTime? Created { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? Updated { get; set; }
        public int? UpdatedBy { get; set; }

    }
}
