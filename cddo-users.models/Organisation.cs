namespace cddo_users.models
{
    public class Organisation
    {
        public int OrganisationId { get; set; } // Primary key
        public string OrganisationName { get; set; }
        public bool? Visible { get; set; }
        public bool? Allowed { get; set; }
        public OrganisationType? OrganisationType { get; set; }
    }

    public class DepartmentToOrganisationDetail
    {
        public int OrganisationID { get; set; }
        public string? OrganisationName { get; set; }
        public int DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
    }
}
