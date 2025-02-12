using cddo_users.models;
using System.ComponentModel;

namespace cddo_users.DTOs
{
    public class DomainDetail
    {
        public int? DomainId { get; set; }
        public string DomainName { get; set; }
        public OrganisationType? OrganisationType { get; set; }
        public string? OrganisationFormat { get; set; }
        public bool? AllowList { get; set; }
        public string? DataShareRequestMailboxAddress { get; set; }
    }

    
}
