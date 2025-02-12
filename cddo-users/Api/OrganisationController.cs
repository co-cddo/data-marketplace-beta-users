using cddo_users.DTOs;
using Microsoft.AspNetCore.Mvc;
using cddo_users.Interface;
using Microsoft.AspNetCore.Authorization;
using cddo_users.Repositories;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;
using System.Security.Claims;
using cddo_users.models;

namespace cddo_users.Api
{
    [Authorize(AuthenticationSchemes = "InteractiveScheme")]
    [ApiController]
    [Route("Organisations")]
    public class OrganisationsController(
    IUserService userService, IOrganisationService organisationService)
        : ControllerBase
    {
        private const string ErrorOccured500 = "An error occurred while processing your request.";

        [HttpPost]
        public async Task<IActionResult> CreateOrganisation([FromBody] OrganisationDetail organisationDetail)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                OrganisationControllerResponseDto result = await organisationService.CreateOrganisation(organisationDetail);

                if (result.Id == 0)
                {
                    return BadRequest(new { message = result.Message });
                }
                return Ok(result.Id);
                
            }
            catch (System.Exception ex)
            {
                // Log the exception
                return StatusCode(500, new { message = ErrorOccured500, details = ex.Message });
            }
        }

        [HttpPatch]
        public async Task<IActionResult> UpdateOrganisation([FromBody] OrganisationDetail organisationDetail)
        {
            try
            {
                await organisationService.UpdateOrganisationAsync(organisationDetail);

                return Ok();
            }
            catch (System.Exception ex)
            {
                // Log the exception
                return StatusCode(500, new { message = ErrorOccured500, details = ex.Message });
            }
        }

        [HttpGet("organisationsByPage")]
        public async Task<IActionResult> GetOrganisationsByPage([FromQuery] OrganisationFilter filter)
        {
            
            OrganisationsResponse organisationsResponse = await organisationService.getOrganisationsByPage(filter);
            // Return the constructed response object as JSON
            return Ok(organisationsResponse);
        }

        [HttpGet("summaries/by-type")]
        public async Task<ActionResult<IEnumerable<OrganisationTypeSummaryDto>>> GetOrganisationTypeSummaries()
        {
            var summaries = await organisationService.GetOrganisationTypeSummariesAsync();
            return Ok(summaries);
        }

        [HttpGet("grouped/by-type")]
        public async Task<IActionResult> GetDomainsGroupedByType()
        {
            var groupedData = await organisationService.GetDomainsGroupedByTypeAsync();
            return Ok(groupedData);
        }

        [HttpGet("grouped/by-format")]
        public async Task<ActionResult<IEnumerable<GroupedByFormatDto>>> GetDomainsGroupedByFormat()
        {
            var groupedData = await organisationService.GetDomainsGroupedByFormatAsync();
            return Ok(groupedData);
        }

        // Get a single Organisation and its domains by ID
        [HttpGet("{id}")]
        public async Task<ActionResult<OrganisationDetail>> GetOrganisationWithDomains(int id)
        {
            var Organisation = await organisationService.GetOrganisationDetailByIdAsync(id);
            if (Organisation == null)
                return NotFound($"Organisation with ID {id} not found.");

            return Ok(Organisation);
        }

        // Search for Organisations and domains
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<OrganisationDetail>>> SearchOrganisationsAndDomains(string query)
        {
            var results = await organisationService.SearchOrganisationsAndDomainsAsync(query);
            return Ok(results);
        }

        // Delete an Organisation and cascade to its domains
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrganisation(int id)
        {
            var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email || c.Type == "email")?.Value;

            var user = await userService.GetUserInfo(userEmail);

            if (user != null)
            {
                await organisationService.DeleteOrganisationAsync(id);
                await organisationService.UpdateOrganisationModifiedDate(id, user.User.UserId);
            }
            
            return NoContent();
        }

        // Add a domain to an Organisation
        [HttpPost("{OrganisationId}/domains")]
        public async Task<ActionResult<DomainDetail>> AddDomainToOrganisation(int OrganisationId, [FromBody] DomainDetail domainDetail)
        {
            try
            {
                var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email || c.Type == "email")?.Value;

                var user = await userService.GetUserInfo(userEmail);

                if (user != null)
                {
                    var existingDomain = await organisationService.GetOrganisationDomainByNameAsync(domainDetail.DomainName);
                    if (existingDomain != null)
                    {
                        return BadRequest(new { message = "A domain with the same name already exists." });
                    }
                    await organisationService.AddDomainToOrganisationAsync(OrganisationId, domainDetail);
                    await organisationService.UpdateOrganisationModifiedDate(OrganisationId, user.User.UserId);
                    return CreatedAtAction(nameof(GetOrganisationWithDomains), new { id = OrganisationId }, domainDetail);
                }

                return NoContent();
               
            }
            catch (Exception)
            {
                return BadRequest();
            }
           
        }

        // Remove a domain from an Organisation
        [HttpDelete("domains/{domainId}")]
        public async Task<IActionResult> RemoveDomain(int domainId)
        {
            //Pass the org Id so we can update the modified date
            await organisationService.RemoveDomainFromOrganisationAsync(domainId);
            return NoContent();
        }

        // Set allowed status for an Organisation and cascade to all its domains
        [HttpPatch("{OrganisationId}/allowList")]
        public async Task<IActionResult> SetOrganisationAllowList(int OrganisationId, [FromBody] bool allowList)
        {
            try
            {
                var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email || c.Type == "email")?.Value;

                var user = await userService.GetUserInfo(userEmail);

                if (user != null)
                {
                    await organisationService.SetOrganisationAllowListAsync(OrganisationId, allowList);
                    await organisationService.UpdateOrganisationModifiedDate(OrganisationId, user.User.UserId);
                }
                return NoContent();
            }
            catch (Exception)
            {

                return BadRequest();
            }
           
        }

        // Set allowed status for a single domain
        [HttpPatch("domains/{domainId}/allowList")]
        public async Task<IActionResult> SetDomainAllowList(int domainId, [FromBody] bool allowList)
        {
            await organisationService.SetDomainAllowListAsync(domainId, allowList);
            return NoContent();
        }

        // Update data share request mailbox address for a single domain
        [HttpPatch("domains/{domainId}/dataShareRequestMailboxAddress")]
        public async Task<IActionResult> SetDataShareRequestMailboxAddress(int domainId, [FromBody] string? dataShareRequestMailboxAddress)
        {
            var updatedDomainDetail = await organisationService.SetDataShareRequestMailboxAddressAsync(domainId, dataShareRequestMailboxAddress);

            if (!string.IsNullOrWhiteSpace(dataShareRequestMailboxAddress))
            {
                await organisationService.SendEmailAsync(updatedDomainDetail, domainId, dataShareRequestMailboxAddress);
            }

            return NoContent();            
        }

        [HttpPost("request")]
        public async Task<IActionResult> CreateOrganisationRequest([FromBody] OrganisationRequest organisationRequest)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                OrganisationControllerResponseDto result = await organisationService.createOrganisationRequest(organisationRequest);

                if (result.Id == 0)
                {
                    return BadRequest(result.Message);
                }
                return Ok(result.Id);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ErrorOccured500, details = ex.Message });
            }
            
        }

        [HttpPatch("request/{id}")]
        public async Task<IActionResult> UpdateOrganisationRequest(int id, [FromBody] OrganisationRequest updatedRequest)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                OrganisationControllerResponseDto result = await organisationService.UpdateOrganisationRequest(id, updatedRequest);
                if (result.Id == 0)
                {
                    return NotFound(new{ message = result.Message });
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ErrorOccured500, details = ex.Message });
            }
        }

        [HttpGet("request/all")]
        public async Task<IActionResult> GetAllOrganisationRequests()
        {
            try
            {
                var requests = await organisationService.GetAllOrganisationRequestsAsync();
                return Ok(requests);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ErrorOccured500, details = ex.Message });
            }
        }

        [HttpGet("request/{id}")]
        public async Task<IActionResult> GetOrganisationRequest(int id)
        {
            try
            {
                var request = await organisationService.GetOrganisationRequestByIdAsync(id);

                if (request == null)
                {
                    return NotFound(new { message = "Organisation request not found." });
                }

                return Ok(request);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ErrorOccured500, details = ex.Message });
            }
        }


    }
}
