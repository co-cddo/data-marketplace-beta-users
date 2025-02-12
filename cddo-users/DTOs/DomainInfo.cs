namespace cddo_users.DTOs
{
    public class DomainInfoDto
    {
        public int DomainId { get; set; }
        public int OrganisationId { get; set; }
        public string OrganisationType { get; set; }
        public string OrganisationFormat { get; set; }
        public bool AllowList { get; set; }
        public bool? Visible { get; set; }
        public string OrganisationName { get; set; }
    }

}
