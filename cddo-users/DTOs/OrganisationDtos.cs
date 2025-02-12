namespace cddo_users.DTOs
{
    public class OrganisationControllerResponseDto
    {
        public string? Message { get; set; }
        public int? Id { get; set; }
    }
    public class OrganisationsResponse
    {
        public List<OrganisationDetail> Orgs { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }
}