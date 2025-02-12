namespace cddo_users.models
{
    public class Domain
    {
        public int DomainId { get; set; } // Primary key
        public string DomainName { get; set; } // Must be unique
        public int OrganisationId { get; set; } // Foreign key to Organisation
        public string OrganisationType { get; set; } // Specific to the domain
        public string OrganisationFormat { get; set; } // Specific to the domain
        public bool AllowList { get; set; } // Indicates if the domain is allowed-listed
        public bool? Visible { get; set; }
        // Navigation property to the Organisation
        public Organisation Organisation { get; set; }
    }
}
