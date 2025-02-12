using cddo_users.DTOs;
using cddo_users.Interface;
using cddo_users.Logic;

// Ensure models namespace includes Role
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Security.Claims;

namespace cddo_users.Api
{
    [Authorize(AuthenticationSchemes = "InteractiveScheme")]
    [Route("/User")]
    [ApiController]
    [Authorize] // Ensure that the user is authenticated
    public class UserController : ControllerBase
    {
        private const string UserName = "username";
        private const string ErrorOccured500 = "An error occurred while processing your request.";
        private const string EmailAddress = "email";
        private readonly IUserService _userService;
        private readonly IApplicationInsightsService _applicationInsightsService;
        private readonly IEmailManager _emailManager;
        private readonly IFormatProvider formatProvider = CultureInfo.InvariantCulture;

        public UserController(IUserService usersService, IApplicationInsightsService applicationInsightsService, IEmailManager emailManager)
        {
            _userService = usersService;
            _applicationInsightsService = applicationInsightsService;
            _emailManager = emailManager;
        }

        [HttpPost("RoleRequestDecision")]
        public async Task<IActionResult> PostUserRoleDecision(UserRoleApproval approval)
        {
            try
            {
                var decision = await _userService.ApprovalDecision(approval);

                if (decision.Item1)
                {
                    // We can send an email
                    var user = await _userService.GetUserByIdAsync(decision.Item2.ToString());
                    if (user != null && user.EmailNotification)
                    {
                        _emailManager.SendRoleRequestDecisionEmails(user, approval);
                    }
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpPost("ApprovalRequest")]
        public async Task<IActionResult> PostUserRoleApprovalRequest(UserRoleApproval approval)
        {
            try
            {
                await _userService.CreateUserApproval(approval);

                // We can send an email
                var user = await _userService.GetUserByIdAsync(approval.UserID.ToString());

                if ((approval.ApprovalStatus == ApprovalStatus.Pending || approval.ApprovalStatus == ApprovalStatus.NotRequested) && user != null && user.User != null)
                {
                    var orgAdmins = await _userService.GetOrgAdminsByOrgId(user.Organisation.OrganisationId);
                    // Send relevant emails to users
                    if (approval.RoleID != null)
                    {
                        _emailManager.SendApprovalRequestEmails((int)approval.RoleID, user, orgAdmins.ToList());
                    }
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpPost("ApprovalRequest-multiple")]
        public async Task<IActionResult> PostUserRoleApprovalRequestMultiple(List<UserRoleApproval> approvals)
        {
            try
            {
                foreach (var approval in approvals)
                {
                    await _userService.CreateUserApproval(approval);
                }

                // We can send an email
                var user = await _userService.GetUserByIdAsync(approvals[0].UserID.ToString());

                if (user != null && user.User != null)
                {
                    var orgAdmins = await _userService.GetOrgAdminsByOrgId(user.Organisation.OrganisationId);
                    if (approvals.Count > 1)
                    {
                        _emailManager.SendApprovalRequestEmailsMultipleRoles(user, orgAdmins.ToList());
                    }
                    else
                    {
                        // Send relevant emails to users
                        _emailManager.SendApprovalRequestEmails((int)approvals[0].RoleID, user, orgAdmins.ToList());
                    }
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }


        [Authorize(AuthenticationSchemes = "InteractiveScheme,ApiAuthScheme")]
        [HttpPost("updateLastLogin")]
        public async Task<IActionResult> UpdateLastLogin()
        {
            var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email || c.Type == EmailAddress)?.Value;
            await _userService.UpdateLastLogin(userEmail);
            return Ok("Last Login Updated");
        }

        [HttpGet("UserRoleApproval/{id}")]
        public async Task<IActionResult> GetUserRoleApproval(int id)
        {
            UserRoleApprovalDetail details = await _userService.Approve(id);
            return Ok(details);
        }

        [HttpGet("GetUserApprovals")]
        public async Task<IActionResult> GetUserApprovals([FromQuery] UserRoleApprovalRequest request)
        {
            try
            {
                var (approvals, totalCount) = await _userService.GetUserApprovalsAsync(request);

                if (approvals == null || !approvals.Any())
                {
                    return NotFound("No approvals found matching the specified criteria.");
                }

                var result = new PaginatedUserRoleApprovalDetails
                {
                    Approvals = approvals.ToList(),
                    TotalCount = totalCount
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Log the exception details here
                return StatusCode(500, ErrorOccured500);
            }
        }

        [HttpGet("GetUserApprovals-pending")]
        [ProducesResponseType(typeof(IEnumerable<UserRoleApprovalDetail>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserPendingApprovals(
        int? domainId = null,
        int? organisationId = null)
        {
            try
            {
                var approvals = await _userService.GetUserPendingApprovalsAsync(domainId, organisationId);

                if (approvals == null || !approvals.Any())
                {
                    return NotFound("No approvals found matching the specified criteria.");
                }

                return Ok(approvals);
            }
            catch (Exception ex)
            {
                // Log the exception details here
                return StatusCode(500, ErrorOccured500);
            }
        }

        [HttpGet("CheckIfPublisherRequestExists")]
        public async Task<IActionResult> GetPublisherRequestStatus(int userid)
        {
            var result = await _userService.CheckPublisherApproval(userid);
            return Ok(result.ToString());
        }

        [AllowAnonymous]
        [HttpGet("eventlogs")]
        public async Task<IActionResult> GetEventLogs(int pageNumber = 1, int pageSize = 10, string searchQuery = null)
        {
            try
            {
                var logs = await _applicationInsightsService.GetEventLogsAsync(pageSize, searchQuery);
                if (logs == null || logs.Logs.Count == 0)
                {
                    return NotFound("No logs found for the specified user.");
                }
                return Ok(logs);
            }
            catch (System.Exception ex)
            {
                // Log the exception details here as needed
                return StatusCode(500, ErrorOccured500);
            }
        }

        [HttpGet("GetEventLogs")]
        public async Task<IActionResult> GetEventLogs(
            [FromQuery] string searchQuery,
            [FromQuery] string timeRange)
        {
            var parsedTimeRange = ParseTimeRange();
            if (parsedTimeRange == null) return BadRequest("Time Range is of illegal format");

            var user = await SetUserInfo();
            if (user != null)
            {
                var result = await _applicationInsightsService.GetEventLogsFromRawQueryAsync(
                               searchQuery, parsedTimeRange.Value, user);

                return Ok(result);
            }

            return BadRequest("No user found for this request");

            TimeSpan? ParseTimeRange()
            {
                if (string.IsNullOrWhiteSpace(timeRange)) return TimeSpan.MaxValue;

                return TimeSpan.TryParse(timeRange, formatProvider, out var parsedTimeSpan)
                    ? parsedTimeSpan
                    : null;
            }
        }

        [HttpGet("GetEventLogsEx")]
        public async Task<IActionResult> GetEventLogsEx(
            [FromQuery] string? tableName,
            [FromQuery] IEnumerable<string> searchClauses,
            [FromQuery] string timeRange)
        {
            var parsedTimeRange = ParseTimeRange();
            if (parsedTimeRange == null) return BadRequest("Time Range is of illegal format");

            var initiatingUserInfo = await SetUserInfo();
            if (initiatingUserInfo != null)
            {
                var result = await _applicationInsightsService.GetEventLogsAsync(
                               tableName, searchClauses, parsedTimeRange.Value, initiatingUserInfo);

                return Ok(result);
            }

            return BadRequest("No user found for this request");

            TimeSpan? ParseTimeRange()
            {
                if (string.IsNullOrWhiteSpace(timeRange)) return TimeSpan.MaxValue;

                return TimeSpan.TryParse(timeRange, formatProvider, out var parsedTimeSpan)
                    ? parsedTimeSpan
                    : null;
            }
        }

        [HttpPost("signInOrUpdateUser")]
        public async Task<IActionResult> SignInOrUpdateUser()
        {
            var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email || c.Type == EmailAddress)?.Value;
            var userName = User.Claims.FirstOrDefault(c => c.Type == "name")?.Value;
            if (string.IsNullOrEmpty(userEmail))
            {
                return BadRequest("User email not found in token.");
            }
            // Extract domain from email to determine the organisationId and domainId, if necessary
            var domain = userEmail.Split('@').LastOrDefault();
            var domainInfo = await _userService.GetOrganisationIdByDomainAsync(domain);
            if (domainInfo == null)
            {
                return NotFound("Domain not found.");
            }
            if (domainInfo.OrganisationId == 0)
            {
                return NotFound("Organization not found for the provided domain.");
            }

            UserProfile? signInUser = await _userService.SignInOrUpdateUser(userEmail, userName, domainInfo);

            return Ok(signInUser);

        }

        [HttpGet("AllRoles")]
        public async Task<IActionResult> AllRoles()
        {
            var roles = await _userService.GetAllRolesAsync();
            return Ok(roles);
        }

        [HttpGet("UserById")]
        public async Task<IActionResult> GetUser(string userid)
        {
            UserProfile user = await _userService.GetUserByIdAsync(userid);
            return Ok(user);
        }

        [HttpGet("UserByEmail")]
        public async Task<IActionResult> GetUserByEmail(string email)
        {
            var user = await _userService.GetUserByEmailAsync(email);
            if (user != null)
            {
                return Ok(user);
            }
            return NotFound();
        }

        [Authorize(AuthenticationSchemes = "InteractiveScheme,ApiAuthScheme")]
        [HttpPost("userinfo")]
        public async Task<IActionResult> GetUserInfo()
        {
            var userInfo = await SetUserInfo();
            if (userInfo != null)
            {
                return Ok(userInfo);
            }

            return BadRequest("User details not found");
        }

        private async Task<UserProfile?> SetUserInfo()
        {
            var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email || c.Type == EmailAddress)?.Value;
            var userName = User.Claims.FirstOrDefault(c => c.Type == "display_name")?.Value;

            var user = await _userService.GetUserInfo(userName, userEmail);

            return user;
        }

        [HttpPost("notifications")]
        public async Task<IActionResult> UpdatePreferences([FromBody] NotificationPreferences prefs)
        {
            bool user = await _userService.UpdatePreferences(prefs.Id, prefs.Set);
            if (user)
            {
                return Ok();
            }
            else
            {
                return BadRequest();
            }
        }


        [HttpPost("AddUserToRole")]
        public async Task<IActionResult> AddUserToRole(string roleId, string userid)
        {
            if (string.IsNullOrEmpty(userid))
            {
                return BadRequest("User ID is missing.");
            }

            var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email || c.Type == EmailAddress)?.Value;
            if (string.IsNullOrEmpty(userEmail))
            {
                return BadRequest("Administrator is missing.");
            }

            var user = await _userService.GetUserByEmailAsync(userEmail);
            if (user == null)
            {
                return BadRequest("Administrator cannot be found.");
            }

            var added = await _userService.AddUserToRoleAsync(int.Parse(userid), int.Parse(roleId), user.User.UserId);
            if (!added)
            {
                return NotFound("User not found or error adding to role.");
            }

            return Ok("User added to role successfully.");
        }

        [HttpPost("RemoveUserFromRole")]
        public async Task<IActionResult> RemoveUserFromRole(string roleId, string userid)
        {
            if (string.IsNullOrEmpty(userid))
            {
                return BadRequest("User ID is missing.");
            }

            var removed = await _userService.RemoveUserFromRoleAsync(int.Parse(userid), int.Parse(roleId));
            if (!removed)
            {
                return NotFound("User not found or error removing from role.");
            }

            //Send emails to user
            var user = await _userService.GetUserByIdAsync(userid);
            if (user != null && user.EmailNotification)
            {
                _emailManager.SendRoleRemovedEmails(roleId, user);
            }

            return Ok("User removed from role successfully.");
        }

        [HttpGet("UsersById")]
        public async Task<IActionResult> GetUsers(
            [FromQuery] IEnumerable<string> userIds)
        {
            var userProfiles = new List<UserProfile>();

            foreach (var userId in userIds)
            {
                try
                {
                    var userProfile = await _userService.GetUserByIdAsync(userId);

                    userProfiles.Add(userProfile);
                }
                catch
                {

                    return Ok(userProfiles);
                }
            }

            return Ok(userProfiles);
        }
        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery] UserQueryParameters queryParams)
        {
            var result = await _userService.GetFilteredUsersAsync(queryParams);

            return Ok(result);
        }

        [HttpGet("myapprovals/{userId}")]
        public async Task<IActionResult> GetUserApprovalsByUserId(int userId)
        {
            if (userId == 0)
            {
                return Ok(null);
            }

            try
            {
                var userApprovals = await _userService.GetUserApprovalsAsync(userId);

                return Ok(userApprovals);
            }
            catch (Exception)
            {

                return BadRequest();
            }
        }
    }


}
