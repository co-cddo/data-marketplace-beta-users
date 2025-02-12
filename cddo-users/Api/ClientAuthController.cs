using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using Dapper;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using cddo_users.DTOs;
using System.Security.Cryptography;
using cddo_users.models;
using cddo_users.Interface;
using cddo_users.Logic;
using DocumentFormat.OpenXml.Bibliography;

namespace cddo_users.Api
{
    [ApiController]
    [Route("[controller]")]
    public class ClientAuthController : ControllerBase
    {
        private const string Connection = "DefaultConnection";
        private readonly IConfiguration _configuration;
        private readonly IClientAuthService _clientAuthService;

        public ClientAuthController(IConfiguration configuration, IClientAuthService clientAuthService)
        {
            _configuration = configuration;
            _clientAuthService = clientAuthService;
        }

        [Authorize(AuthenticationSchemes = "InteractiveScheme")]
        [HttpGet("credentials")]
        public async Task<IActionResult> GetAllCredentials()
        {
            // Extract the user's email and organisation details
            var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
            {
                return BadRequest("User email not found in token.");
            }

            var result = await _clientAuthService.GetCredentialAsync(userEmail);
            if (result == null)
            {
                return BadRequest("Error getting client credentials");
            }

            if (result.ClientCredentialsList == null)
            {
                return NotFound(result.Message);
            }
            

            return Ok(result.ClientCredentialsList.ToList());
        }

        [Authorize(AuthenticationSchemes = "InteractiveScheme")]
        [HttpGet("credential/{id}")]
        public async Task<IActionResult> GetCredentialDetails(int id)
        {
            var credential = await _clientAuthService.GetClientCredentialsByIdAsync(id);
            if (credential == null)
            {
                return NotFound("Credential not found.");
            }

            return Ok(credential);
        }

        [Authorize(AuthenticationSchemes = "InteractiveScheme")]
        [HttpPut("credential/{id}")]
        public async Task<IActionResult> UpdateCredentialDetails(int id, [FromBody] ClientCredential updateRequest)
        {
            var rowsAffected = await _clientAuthService.UpdateCredentialDetailsAsync(id, updateRequest);

            if (rowsAffected == 0)
            {
                return NotFound("Credential not found.");
            }

            return NoContent();
        }

        [Authorize(AuthenticationSchemes = "InteractiveScheme")]
        [HttpDelete("credential/{id}")]
        public async Task<IActionResult> RevokeCredential(int id)
        {
            var rowsAffected = await _clientAuthService.RevokeCredentialAsync(id);
            if (rowsAffected == 0)
            {
                return NotFound("Credential not found.");
            }

            return NoContent();
        }

        [Authorize(AuthenticationSchemes = "InteractiveScheme")]
        [HttpPost("generate-client")]
        public async Task<IActionResult> GenerateClientCredentials([FromBody] ClientRequest clientRequest)
        {
            // Step 1: Extract the requesting user's email from the claims
            var requestingUserEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(requestingUserEmail))
            {
                return BadRequest("User email not found in token.");
            }

            var requestedScopes = _configuration.GetSection("Authentication:AllowedScopes");
            var result = await _clientAuthService.GenerateClientAsync(clientRequest, requestingUserEmail, requestedScopes);

            if(result.Message != null)
            {
                return BadRequest(result.Message);
            }

            // Step 13: Return the generated credentials to the requesting user
            return Ok(new { result.ClientId, result.ClientSecret, result.Scopes, environment = clientRequest.Environment });
        }

        [HttpPost("get-token")]
        public async Task<IActionResult> GetToken([FromBody] TokenRequest request)
        {
            var issuer = _configuration["Authentication:ApiIssuer"];  // The IDP's issuer (to match validation)
            var audience = _configuration["Authentication:ClientId"];
            var appKey = _configuration["Authentication:ApiKey"];
            var (tokenString, expirationTime) = await _clientAuthService.GetTokenAsync(request, issuer, audience, appKey);

            if (tokenString == null || expirationTime == null) 
            {
                return Unauthorized("Invalid ClientId or ClientSecret");
            }

            // Return the token and expiration time
            return Ok(new { token = tokenString, expires_at = expirationTime });
        }
    }
}