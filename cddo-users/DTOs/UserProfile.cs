using cddo_users.models;

namespace cddo_users.DTOs
{
    public class UserProfile
    {
        public UserInfo User { get; set; }
        public UserDomain Domain { get; set; }
        public UserOrganisation Organisation { get; set; }
        public List<Role> Roles { get; set; }
        public bool EmailNotification { get; set; }
        public bool WelcomeNotification { get; set; }
        public DateTime LastLogin { get; set; }
    }
}
