using cddo_users.models;

namespace cddo_users.DTOs
{
    public class OrganisationFilter
    {
        public string? SearchTerm { get; set; }
        public bool AllowListFalse { get; set; } = false;
        public bool AllowListTrue { get; set; } = false;
        public int PageSize { get; set; } = 10;
        public int Page { get; set; } = 1;
        public List<OrganisationType>? OrganisationType { get; set; }
        public string? SortBy { get; set; }
        public string? SortDirection { get; set; }
        public bool? Allowed { get; set; }
    }
}
