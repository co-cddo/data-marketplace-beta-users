using cddo_users.models;

namespace cddo_users.Interface
{
    public interface IClientAuthService
    {
        Task<ClientCredential> GenerateClientAsync(ClientRequest clientRequest, string requestingUserEmail, IConfigurationSection requestedScopes);
        Task<ClientCredentials?> GetCredentialAsync(string userEmail);
        Task<ClientCredential?> GetClientCredentialsByIdAsync(int id);
        Task<int> UpdateCredentialDetailsAsync(int id, ClientCredential updateRequest);
        Task<int> RevokeCredentialAsync(int id);
        Task<(string? token, DateTime? expires_at)> GetTokenAsync(TokenRequest request, string? issuer, string? audience, string? appKey);
    }
}