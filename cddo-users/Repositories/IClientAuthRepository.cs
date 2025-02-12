using cddo_users.models;

namespace cddo_users.Repositories
{
    public interface IClientAuthRepository
    {
        Task<IEnumerable<ClientCredential>?> GetCredentials(int organisationId);
        Task<ClientCredential?> GetClientCredentialsById(int id);

        Task<int> UpdateCredentialDetails(int id, ClientCredential updateRequest);
        Task<int> RevokeCredential(int id);
        Task CreateClientCredentials(DTOs.UserProfile? existingApiUser, string clientId, string clientSecret, IEnumerable<string> requestedScopes, ClientRequest clientRequest, string domain);
        Task<ClientCredential?> GetToken(TokenRequest request);
    }
}