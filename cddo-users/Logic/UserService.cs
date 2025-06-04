using cddo_users.DTOs;
using cddo_users.Interface;
using cddo_users.Repositories;
using Microsoft.Extensions.Configuration;
using System.Net.Mail;
using System.Security.Claims;

namespace cddo_users.Logic
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService;
        private const string UserName = "username";
        private const string EmailAddress = "email";

        public UserService(IUserRepository userRepository, IEmailService emailService)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        }

        public async Task CreateUserApproval(UserRoleApproval approval)
        {
            var existing = await _userRepository.GetUserApprovalAsync(approval);
            if(existing != null)
            {
                await _userRepository.DeleteUserApprovalAsync(existing);
            }

            await _userRepository.CreateUserApproval(approval);
        }

        public async Task<(IEnumerable<UserRoleApprovalDetail>? Approvals, int TotalCount)> GetUserApprovalsAsync(
     UserRoleApprovalRequest request)
        {
            return await _userRepository.GetUserApprovalsAsync(request);
        }

        public async Task<IEnumerable<UserRoleApprovalDetail>> GetUserApprovalsAsync(int userId)
        {
            return await _userRepository.GetUserApprovalsAsync(userId);
        }

        public async Task<IEnumerable<UserRoleApprovalDetail>?> GetUserPendingApprovalsAsync(
     int? domainId = null,
     int? organisationId = null)
        {
            return await _userRepository.GetUserPendingApprovalsAsync(domainId, organisationId);
        }

        public async Task<UserRoleApprovalDetail> Approve(int id)
        {
            return await _userRepository.Approve(id);
        }

        public async Task<(bool, int)> ApprovalDecision(UserRoleApproval approval)
        {
            return await _userRepository.ApprovalDecision(approval);
        }
        public async Task<DomainInfoDto?> GetOrganisationIdByDomainAsync(string? domainName)
        {
            return await _userRepository.GetOrganisationIdByDomainAsync(domainName);
        }
        public async Task<bool> AddUserToRoleAsync(int userId, int roleId, int approverUserId)
        {
            try
            {
                await _userRepository.UpdateUserRoleRequestAsync(userId, roleId, approverUserId);
            }
            catch (Exception)
            {

                return false;
            }
            //We need to update any User role request to approved
            return await _userRepository.AddUserToRoleAsync(userId, roleId);
        }
        public async Task<bool> UpdatePreferences(int? user, bool? EmailNotification)
        {
            return await _userRepository.UpdatePreferences(user, EmailNotification);
        }
        public async Task<bool> RemoveUserFromRoleAsync(int userId, int roleId)
        {
            return await _userRepository.RemoveUserFromRoleAsync(userId, roleId);
        }
        public async Task<UserProfile?> GetUserByEmailAsync(string email)
        {
            return await _userRepository.GetUserByEmailAsync(email);
        }
        public async Task<UserProfile> GetUserByIdAsync(string id)
        {
            return await _userRepository.GetUserByIdAsync(id);
        }
        public async Task<int> CreateUserAsync(UserProfile userProfile)
        {
            return await _userRepository.CreateUserAsync(userProfile);
        }
        public async Task UpdateLastLogin(string email)
        {
            await _userRepository.UpdateLastLogin(email);
        }
        public async Task DeleteUserAsync(models.User user)
        {
            await _userRepository.DeleteUserAsync(user);
        }
        public async Task<ApprovalStatus> CheckPublisherApproval(int userId)
        {
            return await _userRepository.CheckPublisherApproval(userId);
        }
        public async Task<List<DTOs.Role>> GetAllRolesAsync()
        {
            return await _userRepository.GetAllRolesAsync();
        }
        public async Task<IEnumerable<EmailUserName>>? GetOrgAdminsByOrgId(int organisationId)
        {
            return await _userRepository.GetOrgAdminsByOrgId(organisationId);
        }
        
        public async Task<UserRoleApprovalDetail?> GetUserApprovalAsync(UserRoleApproval approval)
        {
            return await _userRepository.GetUserApprovalAsync(approval);
        }

        public async Task<IEnumerable<UserAdminDto>?> GetAllUsersByRoleType(string roleType)
        {
            return await _userRepository.GetAllUsersByRoleTypeAsync(roleType);
        }
        public async Task<UserProfile?> GetUserInfo(string? userEmail)
        {
            if (string.IsNullOrEmpty(userEmail))
            {
                return null;
            }

            var user = await GetUserByEmailAsync(userEmail);
            if (user == null)
            {
                return null;
            }
            return user;
        }

        public async Task<UserProfile?> GetUserInfo(string? userName, string? userEmail)
        {
            if (string.IsNullOrEmpty(userEmail))
            {
                return null;
            }

            var user = await GetUserByEmailAsync(userEmail);
            if (user == null)
            {
                var domain = userEmail.Split('@').LastOrDefault();
                var domainInfo = await GetOrganisationIdByDomainAsync(domain);
                if (domainInfo == null || domainInfo.OrganisationId == 0)
                {
                    return null;
                }

                UserProfile profile = new UserProfile();
                profile.User = new UserInfo
                {
                    UserEmail = userEmail,
                    UserName = userName,
                };
                profile.Organisation = new UserOrganisation
                {
                    OrganisationId = domainInfo.OrganisationId,
                };
                profile.Domain = new UserDomain
                {
                    DomainId = domainInfo.DomainId
                };
                profile.EmailNotification = true;
                profile.WelcomeNotification = true;

                var notificationDetails = new Dictionary<string, dynamic>
            {
                { UserName, userName }
            };

                try
                {
                    _emailService.SendWelcomeEmail(userEmail, notificationDetails);
                }
                catch (Exception)
                {
                    Console.WriteLine("User may not be in Allowed List");
                }

                // User does not exist, create new user
                int userid = await CreateUserAsync(profile);
                await AddUserToRoleAsync(userid, 5, userid);
                //check admins
                if (userEmail == "soydaner.ulker@digital.cabinet-office.gov.uk")
                {
                    await AddUserToRoleAsync(userid, 2, userid);
                    await AddUserToRoleAsync(userid, 1, userid);
                }
                var newuser = await GetUserByEmailAsync(userEmail);

                return newuser;
            }

            //Sort out the roles
            if (user.Roles.Any())
            {
                var visibleRoles = user.Roles.Where(x => x.Visible == true).ToList();
                user.Roles = [.. visibleRoles];
            }

            return user;
        }

        public async Task<UserProfile?> SignInOrUpdateUser(string userEmail, string? userName, DomainInfoDto domainInfo)
        {
            var user = await GetUserByEmailAsync(userEmail);

            if (user == null)
            {
                UserProfile profile = new UserProfile();
                profile.User = new UserInfo
                {
                    UserEmail = userEmail,
                    UserName = userName??"",
                };
                profile.Organisation = new UserOrganisation
                {
                    OrganisationId = domainInfo.OrganisationId,
                };
                profile.Domain = new UserDomain
                {
                    DomainId = domainInfo.DomainId
                };
                profile.EmailNotification = true;
                profile.WelcomeNotification = true;

                var notificationDetails = new Dictionary<string, dynamic>
                {
                    { UserName, userName??"" }
                };
                try
                {
                    _emailService.SendWelcomeEmail(userEmail, notificationDetails);
                }
                catch (Exception e)
                {
                    Console.WriteLine("User not in approved list for emails.");
                }

                // User does not exist, create new user
                int userid = await CreateUserAsync(profile);
                await AddUserToRoleAsync(userid, 5, userid);

                //check admins
                if (userEmail == "soydaner.ulker@digital.cabinet-office.gov.uk")
                {
                    await AddUserToRoleAsync(userid, 6, userid);
                    await AddUserToRoleAsync(userid, 2, userid);
                    await AddUserToRoleAsync(userid, 1, userid);
                }
                var newuser = await GetUserByEmailAsync(userEmail);
                return newuser;
            }

            return user;
        }

        public async Task<UserResponseDto> GetFilteredUsersAsync(UserQueryParameters queryParams)
        {
            // Valid sort columns mapped to their actual SQL representation
            Dictionary<string, string> validSortColumns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                    { "email", "u.Email" },
                    { "username", "u.UserName" },
                    { "lastlogin", "u.LastLogin" },
                    { "organisationname", "o.OrganisationName" }
                };

            // Default sorting
            string sortBy = "u.UserID"; // Default sort column if none specified or if invalid
            string sortOrder = "ASC";   // Default sort order

            // Validate and set the sort column
            if (!string.IsNullOrEmpty(queryParams.SortBy) && validSortColumns.ContainsKey(queryParams.SortBy))
            {
                sortBy = validSortColumns[queryParams.SortBy];
            }

            // Check if a valid sort order is specified ('asc' or 'desc')
            if (!string.IsNullOrEmpty(queryParams.SortOrder) &&
                (queryParams.SortOrder.Equals("asc", StringComparison.OrdinalIgnoreCase) ||
                 queryParams.SortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase)))
            {
                sortOrder = queryParams.SortOrder.ToUpper();
            }
            queryParams.SortOrder = sortOrder;
            queryParams.SortBy = sortBy;

            UserResponseDto response = await _userRepository.GetFilteredUsers(queryParams);

            return response;

            
        }
    }
}
