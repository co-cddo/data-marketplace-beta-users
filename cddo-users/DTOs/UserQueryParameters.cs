namespace cddo_users.DTOs
{
    public class UserQueryParameters
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public int? OrganisationID { get; set; }
        public int? DomainID { get; set; }
        public string? SortBy { get; set; }
        public string? SortOrder { get; set; } = "ASC";
        public bool? Visible { get; set; }
    }
}
