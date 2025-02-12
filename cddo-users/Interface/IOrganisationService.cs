using cddo_users.DTOs;
using Microsoft.AspNetCore.Mvc;
using static cddo_users.Api.OrganisationsController;

namespace cddo_users.Interface
{
    public interface IOrganisationService
    {
        Task<OrganisationControllerResponseDto> CreateOrganisation(OrganisationDetail organisationDetail);
        Task<OrganisationsResponse> getOrganisationsByPage(OrganisationFilter filter);
        Task SendEmailAsync(DomainDetail updatedDomainDetail, int domainId, string dataShareRequestMailboxAddress);
        Task<OrganisationControllerResponseDto> createOrganisationRequest(OrganisationRequest organisationRequest);
        Task<OrganisationControllerResponseDto> UpdateOrganisationRequest(int id, OrganisationRequest updatedRequest);
        Task UpdateOrganisationAsync(OrganisationDetail organisationDetail);
        Task<IEnumerable<OrganisationRequest>> GetAllOrganisationRequestsAsync();
        Task<OrganisationRequest?> GetOrganisationRequestByIdAsync(int id);
        Task<IEnumerable<OrganisationTypeSummaryDto>> GetOrganisationTypeSummariesAsync();
        Task<IEnumerable<OrganisationDomainsGrouped>> GetDomainsGroupedByTypeAsync();
        Task<IEnumerable<GroupedByFormatDto>> GetDomainsGroupedByFormatAsync();
        Task<OrganisationDetail?> GetOrganisationDetailByIdAsync(int id);
        Task<IEnumerable<OrganisationDetail>> SearchOrganisationsAndDomainsAsync(string query);
        Task DeleteOrganisationAsync(int id);
        Task UpdateOrganisationModifiedDate(int id, int userId);
        Task<DomainDetail?> GetOrganisationDomainByNameAsync(string domainName);
        Task AddDomainToOrganisationAsync(int organisationId, DomainDetail domainDetail);
        Task RemoveDomainFromOrganisationAsync(int domainId);
        Task SetOrganisationAllowListAsync(int organisationId, bool allowList);
        Task SetDomainAllowListAsync(int domainId, bool allowList);
        Task<DomainDetail> SetDataShareRequestMailboxAddressAsync(int domainId, string dataShareRequestMailboxAddress);
    }
}
