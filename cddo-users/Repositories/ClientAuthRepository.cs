using cddo_users.models;
using Dapper;
using DocumentFormat.OpenXml.Office2010.Excel;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;

namespace cddo_users.Repositories
{
    public class ClientAuthRepository : IClientAuthRepository
    {

        private readonly string DefaultConnection;

        public ClientAuthRepository(IConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            var _configuration = configuration;
            DefaultConnection = _configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        }

        public async Task CreateClientCredentials(DTOs.UserProfile? existingApiUser, string clientId, string clientSecret, IEnumerable<string> requestedScopes, ClientRequest clientRequest, string domain)
        {
            using (var connection = new SqlConnection(DefaultConnection))
            {
                var query = @"INSERT INTO ClientCredentials (UserId, OrganisationID, ClientId, ClientSecret, Scopes, Environment, Domain, AppName, Expiration)
                      VALUES (@UserId, @OrganisationID, @ClientId, @ClientSecret, @Scopes, @Environment, @Domain, @AppName, @Expiration)";

                var parameters = new
                {
                    UserId = existingApiUser.User.UserId,  // Use the new API user ID
                    OrganisationID = existingApiUser.Organisation.OrganisationId,  // Provided in the request
                    ClientId = clientId,
                    ClientSecret = clientSecret,
                    Scopes = string.Join(",", requestedScopes),  // Store validated scopes as a comma-separated string
                    Environment = clientRequest.Environment,  // Use the validated environment
                    Domain = domain,  // Store the literal domain, e.g., "domain.com"
                    AppName = clientRequest.AppName,
                    Expiration = clientRequest.Expiration
                };

                await connection.ExecuteAsync(query, parameters);
            }
        }
        public async Task<IEnumerable<ClientCredential>?> GetCredentials(int organisationId)
        {

            // Fetch credentials from the database
            using (var connection = new SqlConnection(DefaultConnection))
            {
                var query = @"SELECT * 
                      FROM ClientCredentials
                      WHERE OrganisationID = @OrganisationID
                      ORDER BY Id DESC";

                var credentials = await connection.QueryAsync<ClientCredential>(query, new { OrganisationID = organisationId });

                if (!credentials.Any())
                {
                    return null;
                }

                return credentials;
            }
        }

        public async Task<ClientCredential?> GetClientCredentialsById(int id)
        {
            var credential = new ClientCredential();
            using (var connection = new SqlConnection(DefaultConnection))
            {
                var query = @"SELECT * 
                      FROM ClientCredentials
                      WHERE Id = @Id"
                ;

                credential = await connection.QueryFirstOrDefaultAsync<ClientCredential>(query, new { Id = id });
            }

            return credential;
        }

        public async Task<int> UpdateCredentialDetails(int id, ClientCredential updateRequest)
        {
            using (var connection = new SqlConnection(DefaultConnection))
            {
                var query = @"UPDATE ClientCredentials
                              SET AppName = @AppName,
                                  Scopes = @Scopes, 
                                  Expiration = @Expiration, 
                                  Status = @Status
                              WHERE Id = @Id";

                var rowsAffected = await connection.ExecuteAsync(query, new
                {
                    AppName = updateRequest.AppName,
                    Scopes = updateRequest.Scopes,
                    Expiration = updateRequest.Expiration,
                    Status = updateRequest.Status,
                    Id = id
                });


                return rowsAffected;
            }
        }

        public async Task<int> RevokeCredential(int id)
        {
            using (var connection = new SqlConnection(DefaultConnection))
            {
                var query = @"UPDATE ClientCredentials
                      SET Status = 'revoked'
                      WHERE Id = @Id";

                var rowsAffected = await connection.ExecuteAsync(query, new { Id = id });

                return rowsAffected;
            }
        }

        public async Task<ClientCredential?> GetToken(TokenRequest request)
        {
            using (var connection = new SqlConnection(DefaultConnection))
            {
                var query = @"SELECT * FROM ClientCredentials WHERE ClientId = @ClientId AND ClientSecret = @ClientSecret";
                var clientCredential = await connection.QueryFirstOrDefaultAsync<ClientCredential>(query, new
                {
                    request.ClientId,
                    request.ClientSecret
                });

                return clientCredential;
            }
        }
    }
}
