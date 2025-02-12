using cddo_users.DTOs;
using cddo_users.Interface;

namespace cddo_users.Logic
{
    public class EmailManager : IEmailManager
    {
        private const string UserName = "user-name";
        private const string DomainUrl = "Authentication:Domain";
        private const string OrgAdminName = "new-organisation-user-name";
        private const string OrganisationAdminName = "organisation-admin-name";
        private const string OrganisationName = "organisation-name";
        private readonly IEmailService _emailService;
        private readonly IUserService _userService;
        private readonly IConfiguration _configuration;

        public EmailManager(IEmailService emailService, IUserService userService, IConfiguration configuration)
        {
            _emailService = emailService;
            _userService = userService;
            _configuration = configuration;
        }

        public void SendApprovalRequestEmails(int roleId, UserProfile? user, List<EmailUserName>? orgAdmins)
        {
            if (user?.User == null) return;

            var additionalProps = new Dictionary<string, dynamic>
    {
        { UserName, user.User.UserName }
    };

            // Handle sending emails based on the roleId
            switch (roleId)
            {
                case 4:
                    SendRoleRequestEmails(user, orgAdmins, additionalProps, _emailService.SendPublisherRoleRequestmail, _emailService.SendUserPublisherRoleRequestmail);
                    break;
                case 7:
                    SendRoleRequestEmails(user, orgAdmins, additionalProps, _emailService.SendDataRequestApproverRoleRequestmail, _emailService.SendUserDataRequestApproverRoleRequestmail);
                    break;
                default:
                    // No email sending logic for other roles (1, 2, 3, 5, 6)
                    break;
            }
        }

        private void SendRoleRequestEmails(UserProfile user, List<EmailUserName> orgAdmins, Dictionary<string, dynamic> additionalProps, Action<string, Dictionary<string, dynamic>> sendOrgAdminEmail, Action<string, Dictionary<string, dynamic>> sendUserEmail)
        {
            // Send emails to Org admins
            if (orgAdmins != null && orgAdmins.Any())
            {
                foreach (var item in orgAdmins)
                {
                    var props = new Dictionary<string, dynamic>
            {
                { UserName, user.User.UserName },
                { OrganisationAdminName, item.UserName },
                { "Go to your Dashboard", $"https://{_configuration[DomainUrl]}/manage/approvals" }
            };
                    if (item.EmailNotification)
                    {
                        sendOrgAdminEmail(item.Email, props);
                    }
                }
            }

            // Send email to the user
            if (user.EmailNotification)
            {
                sendUserEmail(user.User.UserEmail, additionalProps);
            }
        }

        public void SendRoleRequestDecisionEmails(UserProfile user, UserRoleApproval approval)
        {
            var additionalProps = new Dictionary<string, dynamic>
                {
                    { UserName, user.User.UserName }
                };

            
            string? templateId;
            if (approval.RoleID == 4)
            {
                switch (approval.ApprovalStatus)
                {
                    case ApprovalStatus.Approved:
                        additionalProps.Add("Go to your profile", $"https://{_configuration[DomainUrl]}/userprofile");
                        _emailService.SendUserApprovedRoleDecisionmail(user.User.UserEmail, additionalProps);
                        break;
                    case ApprovalStatus.Rejected:
                        additionalProps.Add("comment from organisation admin", approval.RejectionComment ?? "No comment from the administrator");
                        templateId = _configuration["DecisionRejectedTemplate"];
                        _emailService.SendUserRejectRoleDecisionmail(user.User.UserEmail, additionalProps, templateId);
                        break;
                    case ApprovalStatus.Revoked:
                        templateId = _configuration["RevokeMetadataPublisher"];
                        _emailService.SendUserRejectRoleDecisionmail(user.User.UserEmail, additionalProps, templateId);
                        break;
                    default:
                        break;
                }
            }

            if (approval.RoleID == 7)
            {
                switch (approval.ApprovalStatus)
                {
                    case ApprovalStatus.Approved:
                        additionalProps.Add("Go to your profile", $"https://{_configuration[DomainUrl]}/userprofile");                        
                        _emailService.SendUserApprovedDataRequestApprover(user.User.UserEmail, additionalProps);
                        break;
                    case ApprovalStatus.Rejected:

                        additionalProps.Add("comment from organisation admin", approval.RejectionComment ?? "No comment from the administrator");
                        _emailService.SendUserRejectDataRequestApprover(user.User.UserEmail, additionalProps);
                        break;
                    case ApprovalStatus.Revoked:
                        templateId = _configuration["RevokeDataRequestApprover"];
                        _emailService.SendUserRejectRoleDecisionmail(user.User.UserEmail, additionalProps, templateId);
                        break;
                    default:
                        break;
                }
            }
        }

        public void SendRoleRemovedEmails(string roleId, UserProfile user)
        {
            var additionalProps = new Dictionary<string, dynamic>
                {
                    { UserName, user.User.UserName }
                };
            switch (int.Parse(roleId))
            {
                case 4:
                    _emailService.SendUserMetadataPublisherRemoved(user.User.UserEmail, additionalProps);
                    break;
                case 7:
                    _emailService.SendDataRequestApproverRemoved(user.User.UserEmail, additionalProps);
                    break;
                default:
                    break;
            }
        }

        public void SendApprovalRequestEmailsMultipleRoles(UserProfile user, List<EmailUserName> emailUserNames)
        {
            //First we need to tell the Org admins
            if (emailUserNames != null && emailUserNames.Any())
            {
                foreach (var item in emailUserNames)
                {
                    var props = new Dictionary<string, dynamic>
                                    {
                                    { UserName, user.User.UserName },
                                    { OrganisationAdminName, item.UserName },
                                    { "Go to your Dashboard", $"https://{_configuration[DomainUrl]}/manage/approvals" }
                                    };
                    if (item.EmailNotification)
                    {
                        _emailService.SendMultipleRoleRequestmail(item.Email, props);
                    }
                }
            }
        }

        public void OrganisationRequestSubmitted(string userName, string emailAddress, string organisationName)
        {

            var props = new Dictionary<string, dynamic>
                                    {
                                    { OrgAdminName, userName },
                                    { OrganisationName, organisationName },
                                    };

            _emailService.OrganisationRequestSubmittedEmail(emailAddress, props);
        }

        public async Task OrganisationRequestSubmittedToSystemAdmin(string organisationName, string organisationUserName)
        {

            var systemAdmins = await _userService.GetAllUsersByRoleType("System Administrator");
            if (systemAdmins == null) return;

            foreach (var systemAdmin in systemAdmins)
            {
                var props = new Dictionary<string, dynamic>
                {
                    { "system-admin-name", systemAdmin.UserName },
                    { OrgAdminName, organisationUserName },
                    { OrganisationName, organisationName },
                };
                if(systemAdmin.EmailNotification.HasValue && systemAdmin.EmailNotification.Value)
                {
                    _emailService.OrganisationRequestSubmittedToSystemAdminEmail(systemAdmin.Email, props);
                }
            }
        }

        public void OrganisationRequestApproved(string userName, string emailAddress, string organisationName)
        {

            var props = new Dictionary<string, dynamic>
                                    {
                                    { OrgAdminName, userName },
                                    { OrganisationName, organisationName },
                                    };

            _emailService.OrganisationRequestApprovedEmail(emailAddress, props);
        }

        public void OrganisationRequestRejected(string userName, string emailAddress, string organisationName, string reason)
        {

            var props = new Dictionary<string, dynamic>
                                    {
                                    { OrgAdminName, userName },
                                    { OrganisationName, organisationName },
                                    { "reason-for-rejection", reason },
                                    };

            _emailService.OrganisationRequestRejectedEmail(emailAddress, props);
        }

        public async Task SendDomainDsrMailboxAddressChangedEmailAsync(string emailAddress, Dictionary<string, dynamic> personalisation)
        {
            await _emailService.SendDomainDsrMailboxAddressChangedEmailAsync(emailAddress, personalisation);
        }
    }
}
