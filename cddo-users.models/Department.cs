namespace cddo_users.models
{
    public class Department
    {
        public int Id { get; set; } // Primary key
        public string? DepartmentName { get; set; }
        public bool? Active { get; set; }
        public DateTime? Created { get; set; }
        public int CreatedBy { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime? Updated { get; set; }
        public int UpdatedBy { get; set; }
    }
}
