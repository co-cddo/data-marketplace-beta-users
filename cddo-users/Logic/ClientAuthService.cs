using cddo_users.DTOs;
using cddo_users.Interface;
using cddo_users.models;
using cddo_users.Repositories;
using IdentityServer4.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace cddo_users.Logic
{
    public class ClientAuthService : IClientAuthService
    {
        private readonly IUserService _userService;
        private readonly IClientAuthRepository _clientAuthRepository;
        public ClientAuthService(IUserService userService, IClientAuthRepository clientAuthRepository)
        {
            _userService = userService;
            _clientAuthRepository = clientAuthRepository;
        }

        public async Task<ClientCredential> GenerateClientAsync(ClientRequest clientRequest, string requestingUserEmail, IConfigurationSection requestedScopes)
        {

            var clientCredentials = new ClientCredential();
            // Step 2: Extract the domain from the requesting user's email (e.g., "user@domain.com" -> "domain.com")
            var domain = clientRequest.Domain ?? requestingUserEmail.Split('@').LastOrDefault();
            if (string.IsNullOrEmpty(domain))
            {
                clientCredentials.Message = "Domain is required to generate API credentials.";
                return clientCredentials;
            }

            // Step 3: Ensure the app name is provided
            if (string.IsNullOrEmpty(clientRequest.AppName))
            {
                clientCredentials.Message = "AppName is required to create an API user.";
                return clientCredentials;
            }

            // Step 4: Clean the AppName to remove extra spaces, convert to lowercase, and replace internal spaces with dashes
            var cleanAppName = clientRequest.AppName.Trim().ToLower().Replace(" ", "-");

            // Step 5: Validate scopes
            var allowedScopes = requestedScopes
                                .Get<string>()?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => s.Trim())
                                .ToList() ?? new List<string>();
            var availableScopes = clientRequest.Scope.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim().ToLower());

            // Ensure all requested scopes are valid
            if (availableScopes.Any(s => !allowedScopes.Contains(s)))
            {
                clientCredentials.Message = "Invalid scopes. Allowed scopes are: " + string.Join(", ", allowedScopes);
                return clientCredentials;
            }

            // Step 6: Validate environment
            var validEnvironments = new List<string> { "test", "production" };
            if (string.IsNullOrEmpty(clientRequest.Environment) || !validEnvironments.Contains(clientRequest.Environment.ToLower()))
            {
                clientCredentials.Message = "Invalid environment. It must be either 'Test' or 'Production'.";
                return clientCredentials;
            }

            // Step 7: Generate the new "API user" email, e.g., "appname-apiuser@domain.com"
            var apiUserEmail = $"{cleanAppName}-apiuser@{domain}";

            // Step 8: Get the current user's domain information based on the domain
            var domainInfo = await _userService.GetOrganisationIdByDomainAsync(domain);
            if (domainInfo == null)
            {
                clientCredentials.Message = "Invalid domain.";
                return clientCredentials;
            }

            // Step 9: Check if the API user already exists
            var existingApiUser = await _userService.GetUserByEmailAsync(apiUserEmail);
            if (existingApiUser == null)
            {
                // Step 10: If the API user does not exist, create a new API user profile
                UserProfile newApiUserProfile = new UserProfile
                {
                    User = new UserInfo
                    {
                        UserEmail = apiUserEmail,
                        UserName = $"{clientRequest.AppName} API User"
                    },
                    Organisation = new UserOrganisation
                    {
                        OrganisationId = domainInfo.OrganisationId // Use OrganisationID provided in the request
                    },
                    Domain = new UserDomain
                    {
                        DomainId = domainInfo.DomainId  // Use the DomainId from the user's domain information
                    }
                };

                // Insert the new API user into the Users table
                int apiUserId = await _userService.CreateUserAsync(newApiUserProfile);

                // Assign a role to the new API user (e.g., API role)
                await _userService.AddUserToRoleAsync(apiUserId, 4, apiUserId);  // Example: Role "4" for API user
                existingApiUser = await _userService.GetUserByEmailAsync(apiUserEmail);  // Fetch the newly created user
            }

            var clientId = Guid.NewGuid().ToString();
            // Secure generation of ClientSecret with a length of 100 characters, using a safe set of characters
            var validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*";
            var randomBytes = new byte[100];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }

            var interimsecret = new StringBuilder(100);
            foreach (var b in randomBytes)
            {
                interimsecret.Append(validChars[b % validChars.Length]);
            }

            // Convert the StringBuilder to a string
            var clientSecret = interimsecret.ToString();

            // Step 12: Store the new Client Credentials with the API user's details
            await _clientAuthRepository.CreateClientCredentials(existingApiUser, clientId, clientSecret, availableScopes, clientRequest, domain);

            clientCredentials.Environment = clientRequest.Environment;
            clientCredentials.ClientSecret = clientSecret;
            clientCredentials.ClientId = clientId;
            clientCredentials.Scopes = string.Join(",", availableScopes);
            return clientCredentials;
        }

        public async Task<ClientCredentials?> GetCredentialAsync(string userEmail)
        {
            var clientCreds = new ClientCredentials();
            var userProfile = await _userService.GetUserByEmailAsync(userEmail);
            if (userProfile == null)
            {
                clientCreds.Message = "User not found.";
                return clientCreds;
            }

            int organisationId = userProfile.Organisation.OrganisationId;

            var result = await _clientAuthRepository.GetCredentials(organisationId);
            if (result == null)
            {
                clientCreds.Message = "Client credentials not found.";
                return clientCreds;
            }

            clientCreds.ClientCredentialsList = result;

            return clientCreds;
        }

        public async Task<ClientCredential?> GetClientCredentialsByIdAsync(int id)
        {
            return await _clientAuthRepository.GetClientCredentialsById(id);
        }

        public async Task<int> UpdateCredentialDetailsAsync(int id, ClientCredential updateRequest)
        {
            return await  _clientAuthRepository.UpdateCredentialDetails(id, updateRequest);
        }

        public async Task<int> RevokeCredentialAsync(int id)
        {
            return await _clientAuthRepository.RevokeCredential(id);
        }

        public async Task<(string? token, DateTime? expires_at)> GetTokenAsync(TokenRequest request, string? issuer, string? audience, string? appKey)
        {
            // Fetch the client credentials from the database using Dapper
            var clientCredential = await _clientAuthRepository.GetToken(request);

            if (clientCredential == null)
            {
                return (null, null);
            }

            // Determine the expiration time: 24 hours or midnight, whichever is sooner
            var now = DateTime.UtcNow;
            var midnight = now.Date.AddDays(1);  // Midnight of the current day (UTC)
            var expirationTime = now.AddHours(24);  // 24 hours from now

            if (midnight < expirationTime)
            {
                expirationTime = midnight;  // Set expiration time to midnight if sooner than 24 hours
            }

            // Clean the AppName for use in the email
            var cleanAppName = clientCredential.AppName.Trim().ToLower().Replace(" ", "-");  // Clean the AppName
            var apiUserEmail = $"{cleanAppName}-apiuser@{clientCredential.Domain.ToLower()}";  // Generate the API user's email

            // Split scopes if stored as a comma-separated string
            var scopes = clientCredential.Scopes
                         .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                         .Select(s => s.Trim());

            // Generate a JWT token for the client
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(appKey ?? string.Empty);  // Use your API's signing key
            string secureRandomNumber;
            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] randomBytes = new byte[4];
                rng.GetBytes(randomBytes);
                int randomInt = Math.Abs(BitConverter.ToInt32(randomBytes, 0)) % 900000 + 100000; // Ensure 6-digit number
                secureRandomNumber = randomInt.ToString();
            }

            // Build the claims for the JWT token
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, clientCredential.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.Iss, issuer ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(), ClaimValueTypes.Integer64), // Using milliseconds for more precision
                new Claim("auth_time", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(), ClaimValueTypes.Integer64),
                new Claim("token_use", "id"),
                new Claim(JwtRegisteredClaimNames.Email, apiUserEmail),
                new Claim("preferred_username", apiUserEmail),
                new Claim("environment", clientCredential.Environment),
                new Claim(JwtRegisteredClaimNames.Exp, new DateTimeOffset(expirationTime).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),  // Unique identifier for each token
                new Claim("nonce", Guid.NewGuid().ToString()),  // Additional randomness using nonce
                new Claim("random", secureRandomNumber)  // Add more randomness with a random number
            };

            // Add the scopes as individual claims
            claims.AddRange(scopes.Select(scope => new Claim("scope", scope)));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expirationTime,  // Token expiry set to either 24 hours or midnight
                Issuer = issuer,  // The IDP's issuer (to match validation)
                Audience = audience,  // Set audience only here (no manual addition in claims)
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)  // Sign with your API's key
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return (tokenString, expirationTime);
        }
    }
}
