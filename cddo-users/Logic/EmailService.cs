using cddo_users.Interface;
using Notify.Client;
using Notify.Exceptions;
using Notify.Interfaces;
using Notify.Models.Responses;
using System.Net.Mail;

namespace cddo_users.Logic
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration configuration;
        private readonly INotificationClient _notificationClient;
        public EmailService(IConfiguration configuration, INotificationClient notificationClient)
        {
            this.configuration = configuration;
            this._notificationClient = notificationClient;
        }

        public void SendEmail(string emailAddress, Dictionary<string, dynamic> personalisation, string templateId)
        {
            try
            {
                var response = _notificationClient.SendEmail(emailAddress, templateId, personalisation);

                Console.WriteLine($"Email sent. Notification ID: {response.id}");
            }
            catch (NotifyClientException e)
            {
                Console.WriteLine($"An error occurred: {e.Message}");
            }
        }

        public void SendWelcomeEmail(string emailAddress, Dictionary<string, dynamic> personalisation)
        {
            var templateId = configuration["WelcomeTemplate"];

            SendEmail(emailAddress, personalisation, templateId);
        }

        public void SendUserApprovedRoleDecisionmail(string emailAddress, Dictionary<string, dynamic> personalisation)
        {
            var templateId = configuration["DecisionApprovedTemplate"];

            SendEmail(emailAddress, personalisation, templateId);
        }

        public void SendUserRejectRoleDecisionmail(string emailAddress, Dictionary<string, dynamic> personalisation, string templateId)
        {

            SendEmail(emailAddress, personalisation, templateId);
        }

        public async Task SendDomainDsrMailboxAddressChangedEmailAsync(string emailAddress, Dictionary<string, dynamic> personalisation)
        {
            var templateId = configuration["DomainDsrMailboxAddressChangedTemplate"];

            SendEmail(emailAddress, personalisation, templateId);
        }

        public void SendUserPublisherRoleRequestmail(string emailAddress, Dictionary<string, dynamic> personalisation)
        {
            var templateId = configuration["PublisherRoleRequestUser"];

            SendEmail(emailAddress, personalisation, templateId);
        }

        public void SendUserDataRequestApproverRoleRequestmail(string emailAddress, Dictionary<string, dynamic> personalisation)
        {
            var templateId = configuration["DataRequestApproverRequestUser"];

            SendEmail(emailAddress, personalisation, templateId);
        }

        public void SendPublisherRoleRequestmail(string emailAddress, Dictionary<string, dynamic> additionalProps)
        {
            var templateId = configuration["PublisherRoleRequestPublisher"];

            SendEmail(emailAddress, additionalProps, templateId);
        }

        public void SendUserApprovedDataRequestApprover(string userEmail, Dictionary<string, dynamic> additionalProps)
        {
            var templateId = configuration["DataRequestApproverRequestUserApproved"];

            SendEmail(userEmail, additionalProps, templateId);
        }

        public void SendUserRejectDataRequestApprover(string userEmail, Dictionary<string, dynamic> additionalProps)
        {
            var templateId = configuration["DataRequestApproverRequestUserRejected"];

            SendEmail(userEmail, additionalProps, templateId);
        }

        public void SendUserMetadataPublisherRemoved(string userEmail, Dictionary<string, dynamic> additionalProps)
        {
            var templateId = configuration["MetadataPublisherRoleRemoved"];

            SendEmail(userEmail, additionalProps, templateId);
        }

        public void SendDataRequestApproverRemoved(string userEmail, Dictionary<string, dynamic> additionalProps)
        {
            var templateId = configuration["DataRequestApproverRoleRemoved"];

            SendEmail(userEmail, additionalProps, templateId);
        }

        public void SendDataRequestApproverRoleRequestmail(string? email, Dictionary<string, dynamic> props)
        {
            var templateId = configuration["DataRequestApproverRoleRequest"];

            SendEmail(email, props, templateId);
        }

        public void SendMultipleRoleRequestmail(string? email, Dictionary<string, dynamic> props)
        {
            var templateId = configuration["DataRequestApproverPublisher"];

            SendEmail(email, props, templateId);
        }

        public void OrganisationRequestSubmittedEmail(string? email, Dictionary<string, dynamic> props)
        {
            var templateId = configuration["OrganisationRequestSubmitted"];

            SendEmail(email, props, templateId);
        }

        public void OrganisationRequestSubmittedToSystemAdminEmail(string? email, Dictionary<string, dynamic> props)
        {
            var templateId = configuration["OrganisationRequestSubmittedToSystemAdmin"];

            SendEmail(email, props, templateId);
        }

        public void OrganisationRequestApprovedEmail(string? email, Dictionary<string, dynamic> props)
        {
            var templateId = configuration["OrganisationRequestApproved"];

            SendEmail(email, props, templateId);
        }

        public void OrganisationRequestRejectedEmail(string? email, Dictionary<string, dynamic> props)
        {
            var templateId = configuration["OrganisationRequestRejected"];

            SendEmail(email, props, templateId);
        }
    }
}
