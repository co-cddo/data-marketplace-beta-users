using cddo_users.models;

namespace cddo_users.Repositories
{
    public interface IDomainRepository
    {
        Task<int> InsertDomainAsync(Domain domain);
        Task<bool> UpdateDomainAsync(Domain domain);
        Task<Domain> GetDomainByIdAsync(int domainId);
        Task<Domain> GetDomainByNameAsync(string domainName);
        Task<IEnumerable<Domain>> GetAllDomainsAsync();
    }
}
