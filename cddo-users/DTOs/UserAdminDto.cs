namespace cddo_users.DTOs
{
    public class UserAdminDto
    {
        public int UserId { get; set; }
        public string Email { get; set; }
        public DateTime? LastLogin { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public bool? EmailNotification { get; set; }
        public bool? WelcomeNotification { get; set; }
        public int? OrganisationID { get; set; }
        public string OrganisationName { get; set; }
        public int? DomainID { get; set; }
        public string UserName { get; set; }
        public bool? Visible { get; set; }
        public List<Role> Roles { get; set; } // This is used for deserialization in the UI
        public string? RolesList { get; set; } // This is the raw roles string
    }
}
