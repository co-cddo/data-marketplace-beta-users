namespace cddo_users.DTOs
{
    public class GroupedByFormatDto
    {
        public string OrganisationFormat { get; set; }
        public List<TypeGroup> Types { get; set; }
        public int TotalCount { get; set; } // If you need a total count
    }

    public class TypeGroup
    {
        public string OrganisationType { get; set; }
        public List<OrganisationGroup> Organisations { get; set; }
        public int TotalCount { get; set; } // Aggregate count
    }

    public class OrganisationGroup
    {
        public string OrganisationName { get; set; }
        public List<DomainDetail> Domains { get; set; }
    }

    public class OrganisationDomainsGrouped
    {
        public string OrganisationName { get; set; }
        public string OrganisationType { get; set; }
        public IEnumerable<DomainDetail> Domains { get; set; }
    }
}
