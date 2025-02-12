namespace cddo_users.DTOs
{
    public class Role
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public string Description { get; set; }
        public bool? Visible { get; set; }
        // Other role-related properties
    }
}
