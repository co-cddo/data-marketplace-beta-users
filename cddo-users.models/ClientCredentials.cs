
namespace cddo_users.models
{
    public class ClientRequest
    {
        public int? UserId { get; set; }
        public int? OrganisationID { get; set; }
        public string Scope { get; set; }
        public string Environment { get; set; }
        public string? Domain { get; set; }
        public string AppName { get; set; }
        public DateTime? Expiration { get; set; } // New expiration field
    }


    public class TokenRequest
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
    }

    public class ClientCredential : ErrorMessage
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int OrganisationID { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string Scopes { get; set; }
        public string Environment { get; set; }
        public string Domain { get; set; }
        public string AppName { get; set; }
        public DateTime? Expiration { get; set; } // New expiration field
        public string Status { get; set; } // New status field
    }

    public class ClientCredentials : ErrorMessage
    {
        public IEnumerable<ClientCredential>? ClientCredentialsList { get; set; }
    }
}
