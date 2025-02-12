using cddo_users.DTOs;
using cddo_users.models;

namespace cddo_users.Repositories
{
    public interface IOrganisationRepository
    {
        Task<IEnumerable<OrganisationTypeSummaryDto>> GetOrganisationTypeSummariesAsync();
        Task<IEnumerable<GroupedByFormatDto>> GetDomainsGroupedByFormatAsync();
        Task<IEnumerable<OrganisationDomainsGrouped>> GetDomainsGroupedByTypeAsync();
        Task<(IEnumerable<OrganisationDetail> Orgs, int TotalCount)> GetAllOrganisationDetailsAsync(OrganisationFilter filter);
        Task<OrganisationDetail?> GetOrganisationDetailByIdAsync(int OrganisationId);
        Task<IEnumerable<OrganisationDetail>> SearchOrganisationsAndDomainsAsync(string searchTerm);
        Task DeleteOrganisationAsync(int OrganisationId);
        Task AddDomainToOrganisationAsync(int OrganisationId, DomainDetail domain);
        Task RemoveDomainFromOrganisationAsync(int domainId);
        Task SetOrganisationAllowListAsync(int OrganisationId, bool allowList);
        Task SetDomainAllowListAsync(int domainId, bool allowList);
        Task<int> CreateOrganisationAsync(OrganisationDetail newOrganisationDetail);
        Task<int> CreateDepartmentOrganisationAsync(OrganisationDetail newOrganisationDetail);
        Task UpdateOrganisationModifiedDate(int organisationId, int userId);
        Task<DomainDetail> SetDataShareRequestMailboxAddressAsync(int domainId, string? dataShareRequestMailboxAddress);
        Task<Organisation?> GetOrganisationByIdAsync(int id);
        Task<IEnumerable<DomainDetail>> GetDomainsByOrganisationId(int organisationId);
        Task<int> CreateOrganisationRequestAsync(OrganisationRequest organisationRequest);
        Task<bool> UpdateOrganisationRequestAsync(int organisationRequestId, OrganisationRequest updatedRequest);
        Task<IEnumerable<OrganisationRequest>> GetAllOrganisationRequestsAsync();
        Task<OrganisationRequest> GetOrganisationRequestByIdAsync(int id);
        Task UpdateOrganisationAsync(OrganisationDetail organisationDetail);
        Task UpdateOrganisationRequestIdAsync(OrganisationRequest organisationRequest, int requestId);
        Task<Organisation?> GetOrganisationByNameAsync(string organisationName);
        Task<DomainDetail?> GetOrganisationDomainByNameAsync(string domainName);
        Task<OrganisationRequest?> GetOrganisationRequestByOrganisationNameAsync(string organisationName, string domainName);
    }
}
