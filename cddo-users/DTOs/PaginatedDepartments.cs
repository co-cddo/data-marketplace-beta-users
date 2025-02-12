using cddo_users.models;

namespace cddo_users.DTOs
{
    public class PaginatedDepartments
    {
        public List<models.Department>? Departments { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }
}
