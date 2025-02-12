using cddo_users.DTOs;
using cddo_users.models;

namespace cddo_users.Interface
{
    public interface IUserService
    {
        Task<bool> AddUserToRoleAsync(int userId, int roleId, int approverUserId);
        Task<(bool, int)> ApprovalDecision(UserRoleApproval approval);
        Task<UserRoleApprovalDetail> Approve(int id);
        Task<ApprovalStatus> CheckPublisherApproval(int userId);
        Task CreateUserApproval(UserRoleApproval approval);
        Task<int> CreateUserAsync(UserProfile userProfile);
        Task DeleteUserAsync(User user);
        Task<List<DTOs.Role>> GetAllRolesAsync();
        Task<IEnumerable<EmailUserName>>? GetOrgAdminsByOrgId(int organisationId);
        Task<DomainInfoDto?> GetOrganisationIdByDomainAsync(string? domainName);
        Task<UserRoleApprovalDetail?> GetUserApprovalAsync(UserRoleApproval approval);
        Task<(IEnumerable<UserRoleApprovalDetail>? Approvals, int TotalCount)> GetUserApprovalsAsync(UserRoleApprovalRequest request);
        Task<IEnumerable<UserRoleApprovalDetail>> GetUserApprovalsAsync(int userId);
        Task<IEnumerable<UserRoleApprovalDetail>?> GetUserPendingApprovalsAsync(
     int? domainId = null,
     int? organisationId = null);
        Task<UserProfile?> GetUserByEmailAsync(string email);
        Task<UserProfile> GetUserByIdAsync(string id);
        Task<bool> RemoveUserFromRoleAsync(int userId, int roleId);
        Task UpdateLastLogin(string email);
        Task<bool> UpdatePreferences(int? user, bool? EmailNotification);
        Task<IEnumerable<UserAdminDto>?> GetAllUsersByRoleType(string roleType);
        Task<UserProfile?> GetUserInfo(string? userEmail);
        Task<UserProfile?> GetUserInfo(string? userName, string? userEmail);
        Task<UserProfile?> SignInOrUpdateUser(string userEmail, string? userName, DomainInfoDto domainInfo);
        Task<UserResponseDto> GetFilteredUsersAsync(UserQueryParameters queryParams);
    }
}