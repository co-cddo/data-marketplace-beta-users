using cddo_users.DTOs;

namespace cddo_users.Interface
{
    public interface IEmailManager
    {
        void SendApprovalRequestEmails(int roleId, UserProfile user, List<EmailUserName> orgAdmins);
        void SendRoleRemovedEmails(string roleId, UserProfile user);
        void SendApprovalRequestEmailsMultipleRoles(UserProfile user, List<EmailUserName> emailUserNames);
        void SendRoleRequestDecisionEmails(UserProfile user, UserRoleApproval approval);
        void OrganisationRequestSubmitted(string userName, string emailAddress, string organisationName);
        void OrganisationRequestApproved(string userName, string emailAddress, string organisationName);
        void OrganisationRequestRejected(string userName, string emailAddress, string organisationName, string reason);
        Task OrganisationRequestSubmittedToSystemAdmin(string organisationName, string organisationUserName);
        Task SendDomainDsrMailboxAddressChangedEmailAsync(string emailAddress, Dictionary<string, dynamic> personalisation);
    }
}