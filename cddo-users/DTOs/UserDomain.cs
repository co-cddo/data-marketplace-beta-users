namespace cddo_users.DTOs
{
    public class UserDomain
    {
        public int DomainId { get; set; }
        public string DomainName { get; set; }
        public bool IsEnabled { get; set; }
        public string? DataShareRequestMailboxAddress { get; set; }
        // Other domain-related properties
    }
}
