namespace cddo_users.models
{
    public class User
    {
        public int UserId { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public int OrganisationId { get; set; }
        public DateTime? LastLogin { get; set; } 
        public bool TwoFactorEnabled { get; set; }
        public string TwoFactorSecretKey { get; set; } 
        public string BackupCodes { get; set; } // JSON array of encrypted backup codes
        public string UserPreferences { get; set; } // JSON string for user preferences
        public ICollection<UserRole> UserRoles { get; set; }
        public bool? Visible { get; set; }
    }
}
