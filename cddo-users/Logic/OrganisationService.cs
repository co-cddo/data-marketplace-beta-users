using cddo_users.Api;
using cddo_users.DTOs;
using cddo_users.Interface;
using cddo_users.models;
using cddo_users.Repositories;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using static cddo_users.Api.OrganisationsController;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace cddo_users.Logic
{
    public class OrganisationService : IOrganisationService
    {
        private readonly IOrganisationRepository _organisationRepository;
        private readonly IUserInformationPresenter _userInformationPresenter;
        private readonly IEmailManager _emailManager;
        private readonly ILogger<OrganisationService> _logger;
        public OrganisationService(IOrganisationRepository organisationRepository, IUserInformationPresenter userInformationPresenter, IEmailManager emailManager,
                                    ILogger<OrganisationService> logger)
        {
            _organisationRepository = organisationRepository;
            _userInformationPresenter = userInformationPresenter;
            _emailManager = emailManager;
            _logger = logger;
        }
        public async Task<OrganisationControllerResponseDto> CreateOrganisation(OrganisationDetail organisationDetail)
        {
            var existingOrganisation = await _organisationRepository.GetOrganisationByNameAsync(organisationDetail.OrganisationName!);
            if (existingOrganisation != null)
            {
                return new OrganisationControllerResponseDto() { Id = 0, Message = "An organisation with the same name already exists." };
            }
            if (organisationDetail.Domains != null) 
            {
                //Check for existing org
                foreach (var domain in organisationDetail.Domains)
                {
                    var existingDomain = await _organisationRepository.GetOrganisationDomainByNameAsync(domain.DomainName);
                    if (existingDomain != null)
                    {
                        return new OrganisationControllerResponseDto() { Id = 0, Message = "A domain with the same name already exists." };
                    }
                }
            }

            var result = await _organisationRepository.CreateDepartmentOrganisationAsync(organisationDetail);
            if (result == 0)
            {
                return new OrganisationControllerResponseDto { Id = 0, Message = "An error occurred while creating the organisation." };
            }

            return new OrganisationControllerResponseDto() { Id = result, Message = "" };
        }

        public async Task<OrganisationsResponse> getOrganisationsByPage(OrganisationFilter filter)
        {
            // Call the repository method to get the organisation details and total count
            var (organisationDetails, totalCount) = await _organisationRepository.GetAllOrganisationDetailsAsync(filter);

            if (organisationDetails != null)
            {
                foreach (var org in organisationDetails)
                {
                    if (org.DomainCount > 1)
                    {
                        org.Domains!.Clear();
                        org.Domains = (List<DomainDetail>)await _organisationRepository.GetDomainsByOrganisationId((int)org.OrganisationId);
                    }
                }
            }

            // Calculate the total pages based on the total count and page size
            var totalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize);

            // Construct the response object that includes the organisations, current page, page size, total count, and total pages
            var organisationsResponse = new OrganisationsResponse
            {
                Orgs = organisationDetails != null ? organisationDetails.ToList() : new List<OrganisationDetail>() , // Convert IEnumerable to List if needed
                CurrentPage = filter.Page,
                PageSize = filter.PageSize,
                TotalCount = totalCount,
                TotalPages = totalPages // Include TotalPages if your front end requires it
            };

            return organisationsResponse;
        }

        public async Task SendEmailAsync(DomainDetail updatedDomainDetail, int domainId, string dataShareRequestMailboxAddress)
        {
            try
            {
                var userName = _userInformationPresenter.GetUserNameOfInitiatingUser() ?? string.Empty;

                // If the domain name is sent with an email then some clients will render it as a hyperlink.  This is avoided
                // by inserting a zero width space after each period within the domain
                var domainName = updatedDomainDetail.DomainName?.Replace(".", "\u200B.") ?? string.Empty;

                var emailProperties = new Dictionary<string, dynamic>
                    {
                        { "user-name", userName },
                        { "domain-name", domainName }
                    };

                await _emailManager.SendDomainDsrMailboxAddressChangedEmailAsync(
                    dataShareRequestMailboxAddress,
                    emailProperties);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send notification of update to Data Share Request notifications email address");
            }
        }

        public async Task<OrganisationControllerResponseDto> createOrganisationRequest(OrganisationRequest organisationRequest)
        {

            //Check for existing org
            var existingOrganisation = await _organisationRepository.GetOrganisationByNameAsync(organisationRequest.OrganisationName!);
            if (existingOrganisation != null)
            {
                return new OrganisationControllerResponseDto { Id = 0, Message = "An organisation with the same name already exists." };
            }
            var existingDomain = await _organisationRepository.GetOrganisationDomainByNameAsync(organisationRequest.DomainName!);
            if (existingDomain != null)
            {
                return new OrganisationControllerResponseDto { Id = 0, Message = "A domain with the same name already exists." };
            }

            var existingRequest = await _organisationRepository.GetOrganisationRequestByOrganisationNameAsync(organisationRequest.OrganisationName!, organisationRequest.DomainName!);
            if (existingRequest != null)
            {
                return new OrganisationControllerResponseDto { Id = 0, Message = "A request for this organisation or domain already exists." };
            }


            var result = await _organisationRepository.CreateOrganisationRequestAsync(organisationRequest);

            if (result == 0)
            {
                return new OrganisationControllerResponseDto { Id = 0, Message = "An error occurred while creating the organisation request." };
            }
            else
            {
                _emailManager.OrganisationRequestSubmitted(organisationRequest.UserName, organisationRequest.CreatedBy, organisationRequest.OrganisationName);
                await _emailManager.OrganisationRequestSubmittedToSystemAdmin(organisationRequest.OrganisationName, organisationRequest.CreatedBy);

                //After all has gone well, we need to patch the Organisation table with Request Id
                await _organisationRepository.UpdateOrganisationRequestIdAsync(organisationRequest, result);

                return new OrganisationControllerResponseDto { Id = result, Message = "" };
            }

        }
        public async Task<OrganisationControllerResponseDto> UpdateOrganisationRequest(int id, OrganisationRequest updatedRequest)
        {
            updatedRequest.OrganisationRequestID = id;

            bool success = await _organisationRepository.UpdateOrganisationRequestAsync(id, updatedRequest);

            if (!success)
            {
                return new OrganisationControllerResponseDto() { Id = 0, Message = "Organisation request not found or no changes made." };
            }

            if (updatedRequest.Status == "Approved")
            {
                _emailManager.OrganisationRequestApproved(updatedRequest.UserName, updatedRequest.CreatedBy, updatedRequest.OrganisationName);
            }

            if (updatedRequest.Status == "Rejected")
            {
                _emailManager.OrganisationRequestRejected(updatedRequest.UserName, updatedRequest.CreatedBy, updatedRequest.OrganisationName, updatedRequest.Reason);
            }
            return new OrganisationControllerResponseDto() { Id = 1 };
        }

        public async Task UpdateOrganisationAsync(OrganisationDetail organisationDetail)
        {

            await _organisationRepository.UpdateOrganisationAsync(organisationDetail);

        }

        public async Task<IEnumerable<OrganisationRequest>> GetAllOrganisationRequestsAsync()
        {
            return await _organisationRepository.GetAllOrganisationRequestsAsync();
        }
        public async Task<OrganisationRequest?> GetOrganisationRequestByIdAsync(int id)
        {
            return await _organisationRepository.GetOrganisationRequestByIdAsync(id);
        }
        public async Task<IEnumerable<OrganisationTypeSummaryDto>> GetOrganisationTypeSummariesAsync()
        {
            return await _organisationRepository.GetOrganisationTypeSummariesAsync(); 
        }
        public async Task<IEnumerable<OrganisationDomainsGrouped>> GetDomainsGroupedByTypeAsync()
        {
            return await _organisationRepository.GetDomainsGroupedByTypeAsync();
        }
        public async Task<IEnumerable<GroupedByFormatDto>> GetDomainsGroupedByFormatAsync()
        {
            return await _organisationRepository.GetDomainsGroupedByFormatAsync();
        }
        public async Task<OrganisationDetail?> GetOrganisationDetailByIdAsync(int id)
        {
            return await _organisationRepository.GetOrganisationDetailByIdAsync(id); 
        }
        public async Task<IEnumerable<OrganisationDetail>> SearchOrganisationsAndDomainsAsync(string query)
        {
            return await _organisationRepository.SearchOrganisationsAndDomainsAsync(query);
        }
        public async Task DeleteOrganisationAsync(int id)
        {
            await _organisationRepository.DeleteOrganisationAsync(id);
        }
        public async Task UpdateOrganisationModifiedDate(int id, int userId)
        {
            await _organisationRepository.UpdateOrganisationModifiedDate(id, userId);
        }
        public async Task<DomainDetail?> GetOrganisationDomainByNameAsync(string domainName)
        {
            return await _organisationRepository.GetOrganisationDomainByNameAsync(domainName);
        }
        public async Task AddDomainToOrganisationAsync(int organisationId, DomainDetail domainDetail)
        {
            await _organisationRepository.AddDomainToOrganisationAsync(organisationId, domainDetail);
        }
        public async Task RemoveDomainFromOrganisationAsync(int domainId)
        {
            await _organisationRepository.RemoveDomainFromOrganisationAsync(domainId);
        }
        public async Task SetOrganisationAllowListAsync(int organisationId, bool allowList)
        {
            await _organisationRepository.SetOrganisationAllowListAsync(organisationId, allowList);
        }
        public async Task SetDomainAllowListAsync(int domainId, bool allowList)
        {
            await _organisationRepository.SetDomainAllowListAsync(domainId, allowList);
        }
        public async Task<DomainDetail> SetDataShareRequestMailboxAddressAsync(int domainId, string dataShareRequestMailboxAddress)
        {
            return await _organisationRepository.SetDataShareRequestMailboxAddressAsync(domainId, dataShareRequestMailboxAddress);
        }
    }

}
