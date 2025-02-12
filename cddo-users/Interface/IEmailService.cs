namespace cddo_users.Interface
{
    public interface IEmailService
    {
        void SendUserApprovedRoleDecisionmail(string emailAddress, Dictionary<string, dynamic> personalisation);

        void SendUserRejectRoleDecisionmail(string emailAddress, Dictionary<string, dynamic> personalisation, string templateId);

        void SendWelcomeEmail(string emailAddress, Dictionary<string, dynamic> personalisation);

        Task SendDomainDsrMailboxAddressChangedEmailAsync(string emailAddress, Dictionary<string, dynamic> personalisation);
        void SendUserPublisherRoleRequestmail(string emailAddress, Dictionary<string, dynamic> personalisation);
        void SendPublisherRoleRequestmail(string emailAddress, Dictionary<string, dynamic> additionalProps);
        void SendUserApprovedDataRequestApprover(string userEmail, Dictionary<string, dynamic> additionalProps);
        void SendUserRejectDataRequestApprover(string userEmail, Dictionary<string, dynamic> additionalProps);
        void SendUserMetadataPublisherRemoved(string userEmail, Dictionary<string, dynamic> additionalProps);
        void SendDataRequestApproverRemoved(string userEmail, Dictionary<string, dynamic> additionalProps);
        void SendUserDataRequestApproverRoleRequestmail(string emailAddress, Dictionary<string, dynamic> personalisation);
        void SendDataRequestApproverRoleRequestmail(string? email, Dictionary<string, dynamic> props);
        void SendMultipleRoleRequestmail(string? email, Dictionary<string, dynamic> props);
        void OrganisationRequestSubmittedEmail(string? email, Dictionary<string, dynamic> props);
        void OrganisationRequestApprovedEmail(string? email, Dictionary<string, dynamic> props);
        void OrganisationRequestRejectedEmail(string? email, Dictionary<string, dynamic> props);
        void OrganisationRequestSubmittedToSystemAdminEmail(string? email, Dictionary<string, dynamic> props);
    }
}