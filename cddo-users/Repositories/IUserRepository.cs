using cddo_users.Api;
using cddo_users.DTOs;
using cddo_users.models;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;

namespace cddo_users.Repositories
{
    public interface IUserRepository
    {
        Task<(bool, int)> ApprovalDecision(UserRoleApproval approval);
        Task<ApprovalStatus> CheckPublisherApproval(int userId);
        Task<(IEnumerable<UserRoleApprovalDetail> Approvals, int TotalCount)> GetUserApprovalsAsync(
        UserRoleApprovalRequest request);
        Task<IEnumerable<UserRoleApprovalDetail>> GetUserApprovalsAsync(int userId);

        Task<IEnumerable<UserRoleApprovalDetail>> GetUserPendingApprovalsAsync(
     int? domainId = null,
     int? organisationId = null);
        Task<UserRoleApprovalDetail> Approve(int id);
        Task CreateUserApproval(UserRoleApproval approval);
        Task<UserProfile> GetUserByIdAsync(string id);
        Task<UserProfile?> GetUserByEmailAsync(string email);
        Task<int> CreateUserAsync(UserProfile userProfile);
        Task UpdateLastLogin(string email);
        Task DeleteUserAsync(User user);
        Task<bool> AddUserToRoleAsync(int userId, int roleId);
        Task<bool> RemoveUserFromRoleAsync(int userId, int roleId);
        Task<DomainInfoDto?> GetOrganisationIdByDomainAsync(string? domainName);
        Task<bool> UpdatePreferences(int? user, bool? EmailNotification);
        Task<List<DTOs.Role>> GetAllRolesAsync();
        Task<IEnumerable<EmailUserName>>? GetOrgAdminsByOrgId(int organisationId);
        Task<UserRoleApprovalDetail?> GetUserApprovalAsync(UserRoleApproval approval);
        Task DeleteUserApprovalAsync(UserRoleApprovalDetail userApproval);
        Task<bool> UpdateUserRoleRequestAsync(int userId, int roleId, int approverUserId);
        Task<IEnumerable<UserAdminDto>> GetAllUsersByRoleTypeAsync(string roleType);
        Task<UserResponseDto> GetFilteredUsers(UserQueryParameters queryParams);
    }
}
