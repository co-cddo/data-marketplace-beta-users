using AutoFixture;
using AutoFixture.AutoMoq;
using cddo_users.DTOs;
using cddo_users.Interface;
using cddo_users.Logic;
using FluentAssertions.Execution;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cddo_users.test.Services
{
    [TestFixture]
    public class EmailManagerTests
    {
        protected readonly IFixture fixture;
        public EmailManagerTests()
        {
            fixture = new Fixture().Customize(new AutoMoqCustomization());
        }

        [Test]
        public void SendApprovalRequestEmails_WhenUserIsNull_NoNotificationsSent()
        {
            ///Arrange
            var mockEmailService = new Mock<IEmailService>();
            var mockUserService = new Mock<IUserService>();
            var mockConfiguration = new Mock<IConfiguration>();

            var sut = new EmailManager(mockEmailService.Object, mockUserService.Object, mockConfiguration.Object);

            ///Act
            sut.SendApprovalRequestEmails(1, null, null);

            ///Assert
            mockEmailService.Verify(x => x.SendPublisherRoleRequestmail(It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>()), Times.Never);

        }

        [Test]
        public void SendApprovalRequestEmails_WhenRoleId4WithUserProfileNoAdmins_NotificationSentToUser()
        {
            ///Arrange
            var mockEmailService = new Mock<IEmailService>();
            var mockUserService = new Mock<IUserService>();
            var mockConfiguration = new Mock<IConfiguration>();

            var userProfile = fixture.Create<UserProfile>();
            userProfile.EmailNotification = true;

            var sut = new EmailManager(mockEmailService.Object, mockUserService.Object, mockConfiguration.Object);

            ///Act
            sut.SendApprovalRequestEmails(4, userProfile, null);

            ///Assert
            mockEmailService.Verify(x => x.SendUserPublisherRoleRequestmail(It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>()), Times.Once);

        }

        [Test]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(10)]
        public void SendApprovalRequestEmails_WhenRoleIdHasNoLogicWithUserProfileNoAdmins_NotificationSentToUser(int roleId)
        {
            ///Arrange
            var mockEmailService = new Mock<IEmailService>();
            var mockUserService = new Mock<IUserService>();
            var mockConfiguration = new Mock<IConfiguration>();

            var userProfile = fixture.Create<UserProfile>();
            userProfile.EmailNotification = true;

            var sut = new EmailManager(mockEmailService.Object, mockUserService.Object, mockConfiguration.Object);

            ///Act
            sut.SendApprovalRequestEmails(roleId, userProfile, null);

            ///Assert
            mockEmailService.Verify(x => x.SendPublisherRoleRequestmail(It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>()), Times.Never);

        }

        [Test]
        public void SendApprovalRequestEmails_WhenRoleId4WithUserProfileAndAdmins_NotificationSentToUserAndAdmin()
        {
            ///Arrange
            var mockEmailService = new Mock<IEmailService>();
            var mockUserService = new Mock<IUserService>();
            var mockConfiguration = new Mock<IConfiguration>();

            var admins = fixture.Create<List<EmailUserName>>();
            var userProfile = fixture.Create<UserProfile>();
            userProfile.EmailNotification = true;

            var sut = new EmailManager(mockEmailService.Object, mockUserService.Object, mockConfiguration.Object);

            var adminCount = admins.Where(x=>x.EmailNotification = true).Count();

            ///Act
            sut.SendApprovalRequestEmails(4, userProfile, admins);

            ///Assert
            mockEmailService.Verify(x => x.SendPublisherRoleRequestmail(It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>()), Times.Exactly(adminCount));
            mockEmailService.Verify(x => x.SendUserPublisherRoleRequestmail(It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>()), Times.Once);

        }
        [Test]
        public void SendApprovalRequestEmails_WhenRoleId7WithUserProfileNoAdmins_NotificationSentToUser()
        {
            ///Arrange
            var mockEmailService = new Mock<IEmailService>();
            var mockUserService = new Mock<IUserService>();
            var mockConfiguration = new Mock<IConfiguration>();

            var userProfile = fixture.Create<UserProfile>();
            userProfile.EmailNotification = true;

            var sut = new EmailManager(mockEmailService.Object, mockUserService.Object, mockConfiguration.Object);

            ///Act
            sut.SendApprovalRequestEmails(7, userProfile, null);

            ///Assert
            mockEmailService.Verify(x => x.SendUserDataRequestApproverRoleRequestmail(It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>()), Times.Once);

        }
        [Test]
        public void SendApprovalRequestEmails_WhenRoleId7WithUserProfileAndAdmins_NotificationSentToUserAndAdmin()
        {
            ///Arrange
            var mockEmailService = new Mock<IEmailService>();
            var mockUserService = new Mock<IUserService>();
            var mockConfiguration = new Mock<IConfiguration>();

            var admins = fixture.Create<List<EmailUserName>>();
            var userProfile = fixture.Create<UserProfile>();
            userProfile.EmailNotification = true;

            var sut = new EmailManager(mockEmailService.Object, mockUserService.Object, mockConfiguration.Object);

            var adminCount = admins.Where(x => x.EmailNotification = true).Count();

            ///Act
            sut.SendApprovalRequestEmails(7, userProfile, admins);

            ///Assert
            mockEmailService.Verify(x => x.SendDataRequestApproverRoleRequestmail(It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>()), Times.Exactly(adminCount));
            mockEmailService.Verify(x => x.SendUserDataRequestApproverRoleRequestmail(It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>()), Times.Once);

        }

        [Test]
        public void SendRoleRequestDecisionEmailsRoleId4ApprovalStatusApproved_NotificationSentToUser()
        {
            ///Arrange
            var mockEmailService = new Mock<IEmailService>();
            var mockUserService = new Mock<IUserService>();
            var mockConfiguration = new Mock<IConfiguration>();

            var userProfile = fixture.Create<UserProfile>();
            var userRoleApproval = fixture.Create<UserRoleApproval>();
            userRoleApproval.ApprovalStatus = ApprovalStatus.Approved;
            userRoleApproval.RoleID = 4;

            var sut = new EmailManager(mockEmailService.Object, mockUserService.Object, mockConfiguration.Object);

            ///Act 
            sut.SendRoleRequestDecisionEmails(userProfile, userRoleApproval);

            ///Assert
            mockEmailService.Verify(x => x.SendUserApprovedRoleDecisionmail(It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>()), Times.Once);

        }
        [Test]
        public void SendRoleRequestDecisionEmailsRoleId4ApprovalStatusRejected_NotificationSentToUser()
        {
            ///Arrange
            var mockEmailService = new Mock<IEmailService>();
            var mockUserService = new Mock<IUserService>();
            var mockConfiguration = new Mock<IConfiguration>();

            var userProfile = fixture.Create<UserProfile>();
            var userRoleApproval = fixture.Create<UserRoleApproval>();
            userRoleApproval.ApprovalStatus = ApprovalStatus.Rejected;
            userRoleApproval.RoleID = 4;

            var sut = new EmailManager(mockEmailService.Object, mockUserService.Object, mockConfiguration.Object);

            ///Act 
            sut.SendRoleRequestDecisionEmails(userProfile, userRoleApproval);

            ///Assert
            mockEmailService.Verify(x => x.SendUserRejectRoleDecisionmail(It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>(), It.IsAny<string>()), Times.Once);

        }
        [Test]
        public void SendRoleRequestDecisionEmailsRoleId4ApprovalStatusRevoked_NotificationSentToUser()
        {
            ///Arrange
            var mockEmailService = new Mock<IEmailService>();
            var mockUserService = new Mock<IUserService>();
            var mockConfiguration = new Mock<IConfiguration>();

            var userProfile = fixture.Create<UserProfile>();
            var userRoleApproval = fixture.Create<UserRoleApproval>();
            userRoleApproval.ApprovalStatus = ApprovalStatus.Revoked;
            userRoleApproval.RoleID = 4;

            var sut = new EmailManager(mockEmailService.Object, mockUserService.Object, mockConfiguration.Object);

            ///Act 
            sut.SendRoleRequestDecisionEmails(userProfile, userRoleApproval);

            ///Assert
            mockEmailService.Verify(x => x.SendUserRejectRoleDecisionmail(It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>(), It.IsAny<string>()), Times.Once);

        }        
        [Test]
        public void SendRoleRequestDecisionEmailsRoleId7ApprovalStatusApproved_NotificationSentToUser()
        {
            ///Arrange
            var mockEmailService = new Mock<IEmailService>();
            var mockUserService = new Mock<IUserService>();
            var mockConfiguration = new Mock<IConfiguration>();

            var userProfile = fixture.Create<UserProfile>();
            var userRoleApproval = fixture.Create<UserRoleApproval>();
            userRoleApproval.ApprovalStatus = ApprovalStatus.Approved;
            userRoleApproval.RoleID = 7;

            var sut = new EmailManager(mockEmailService.Object, mockUserService.Object, mockConfiguration.Object);

            ///Act 
            sut.SendRoleRequestDecisionEmails(userProfile, userRoleApproval);

            ///Assert
            mockEmailService.Verify(x => x.SendUserApprovedDataRequestApprover(It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>()), Times.Once);

        }
        [Test]
        public void SendRoleRequestDecisionEmailsRoleId7ApprovalStatusRejected_NotificationSentToUser()
        {
            ///Arrange
            var mockEmailService = new Mock<IEmailService>();
            var mockUserService = new Mock<IUserService>();
            var mockConfiguration = new Mock<IConfiguration>();

            var userProfile = fixture.Create<UserProfile>();
            var userRoleApproval = fixture.Create<UserRoleApproval>();
            userRoleApproval.ApprovalStatus = ApprovalStatus.Rejected;
            userRoleApproval.RoleID = 7;

            var sut = new EmailManager(mockEmailService.Object, mockUserService.Object, mockConfiguration.Object);

            ///Act 
            sut.SendRoleRequestDecisionEmails(userProfile, userRoleApproval);

            ///Assert
            mockEmailService.Verify(x => x.SendUserRejectDataRequestApprover(It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>()), Times.Once);

        }
        [Test]
        public void SendRoleRequestDecisionEmailsRoleId7ApprovalStatusRevoked_NotificationSentToUser()
        {
            ///Arrange
            var mockEmailService = new Mock<IEmailService>();
            var mockUserService = new Mock<IUserService>();
            var mockConfiguration = new Mock<IConfiguration>();

            var userProfile = fixture.Create<UserProfile>();
            var userRoleApproval = fixture.Create<UserRoleApproval>();
            userRoleApproval.ApprovalStatus = ApprovalStatus.Revoked;
            userRoleApproval.RoleID = 4;

            var sut = new EmailManager(mockEmailService.Object, mockUserService.Object, mockConfiguration.Object);

            ///Act 
            sut.SendRoleRequestDecisionEmails(userProfile, userRoleApproval);

            ///Assert
            mockEmailService.Verify(x => x.SendUserRejectRoleDecisionmail(It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>(), It.IsAny<string>()), Times.Once);

        }
        [Test]
        public void SendRoleRemovedEmailsId4_NotificationSentToUser()
        {
            ///Arrange
            var mockEmailService = new Mock<IEmailService>();
            var mockUserService = new Mock<IUserService>();
            var mockConfiguration = new Mock<IConfiguration>();

            var userProfile = fixture.Create<UserProfile>();
            var userRoleApproval = fixture.Create<UserRoleApproval>();
            string roleId = "4";

            var sut = new EmailManager(mockEmailService.Object, mockUserService.Object, mockConfiguration.Object);

            ///Act
            sut.SendRoleRemovedEmails(roleId, userProfile);

            ///Assert 
            mockEmailService.Verify(x => x.SendUserMetadataPublisherRemoved(It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>()), Times.Once);

        }
        [Test]
        public void SendRoleRemovedEmailsId7_NotificationSentToUser()
        {
            ///Arrange
            var mockEmailService = new Mock<IEmailService>();
            var mockUserService = new Mock<IUserService>();
            var mockConfiguration = new Mock<IConfiguration>();

            var userProfile = fixture.Create<UserProfile>();
            var userRoleApproval = fixture.Create<UserRoleApproval>();
            string roleId = "7";

            var sut = new EmailManager(mockEmailService.Object, mockUserService.Object, mockConfiguration.Object);

            ///Act
            sut.SendRoleRemovedEmails(roleId, userProfile);

            ///Assert 
            mockEmailService.Verify(x => x.SendDataRequestApproverRemoved(It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>()), Times.Once);

        }
        [Test]
        public void SendApprovalRequestEmailsMultipleRoles_MultipleEmailsSent()
        {
            ///Arrange
            var mockEmailService = new Mock<IEmailService>();
            var mockUserService = new Mock<IUserService>();
            var mockConfiguration = new Mock<IConfiguration>();

            var admins = fixture.Create<List<EmailUserName>>();
            var userProfile = fixture.Create<UserProfile>();
            userProfile.EmailNotification = true;

            var sut = new EmailManager(mockEmailService.Object, mockUserService.Object, mockConfiguration.Object);

            var adminCount = admins.Where(x => x.EmailNotification = true).Count();

            ///Act
            sut.SendApprovalRequestEmailsMultipleRoles(userProfile, admins);

            ///Assert
            mockEmailService.Verify(x => x.SendMultipleRoleRequestmail(It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>()), Times.Exactly(adminCount));

        }
        [Test]
        public void OrganisationRequestSubmitted_EmailSent()
        {
            ///Arrange
            var mockEmailService = new Mock<IEmailService>();
            var mockUserService = new Mock<IUserService>();
            var mockConfiguration = new Mock<IConfiguration>();

            string username = "username";
            string email = "email";
            string organisatonName = "orgName";

            var sut = new EmailManager(mockEmailService.Object, mockUserService.Object, mockConfiguration.Object);

            ///Act
            sut.OrganisationRequestSubmitted(username, email, organisatonName);

            ///Assert
            mockEmailService.Verify(x => x.OrganisationRequestSubmittedEmail(It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>()), Times.Once);

        }
        [Test]
        public async Task OrganisationRequestSubmittedToSystemAdmin_multiple_emails_sent()
        {
            ///Arrange
            var mockEmailService = new Mock<IEmailService>();
            var mockUserService = new Mock<IUserService>();
            var mockConfiguration = new Mock<IConfiguration>();

            var sut = new EmailManager(mockEmailService.Object, mockUserService.Object, mockConfiguration.Object);

            string orgUsername = "username";
            string organisatonName = "orgName";
            var admins = fixture.Create<List<UserAdminDto>>();
            admins.First().EmailNotification = true;

            var adminCount = admins.Count(x => x.EmailNotification == true);

            mockUserService.Setup(x => x.GetAllUsersByRoleType("System Administrator")).ReturnsAsync(admins);

            ///Act
            await sut.OrganisationRequestSubmittedToSystemAdmin(organisatonName, orgUsername);

            ///Assert
            mockEmailService.Verify(x => x.OrganisationRequestSubmittedToSystemAdminEmail(It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>()), Times.Exactly(adminCount));

        }
        [Test]
        public void OrganisationRequestApproved_EmailSent()
        {
            ///Arrange
            var mockEmailService = new Mock<IEmailService>();
            var mockUserService = new Mock<IUserService>();
            var mockConfiguration = new Mock<IConfiguration>();

            string username = "username";
            string email = "email";
            string organisatonName = "orgName";

            var sut = new EmailManager(mockEmailService.Object, mockUserService.Object, mockConfiguration.Object);

            ///Act
            sut.OrganisationRequestApproved(username, email, organisatonName);

            ///Assert
            mockEmailService.Verify(x => x.OrganisationRequestApprovedEmail(It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>()), Times.Once);

        }
        [Test]
        public void OrganisationRequestRejected_EmailSent()
        {
            ///Arrange
            var mockEmailService = new Mock<IEmailService>();
            var mockUserService = new Mock<IUserService>();
            var mockConfiguration = new Mock<IConfiguration>();

            string username = "username";
            string email = "email";
            string organisatonName = "orgName";
            string reason = "reason";

            var sut = new EmailManager(mockEmailService.Object, mockUserService.Object, mockConfiguration.Object);

            ///Act
            sut.OrganisationRequestRejected(username, email, organisatonName,reason);

            ///Assert
            mockEmailService.Verify(x => x.OrganisationRequestRejectedEmail(It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>()), Times.Once);

        }

        [Test]
        public async Task SendDomainDsrMailboxAddressChangedEmailAsync_EmailSent()
        {
            ///Arrange
            var mockEmailService = new Mock<IEmailService>();
            var mockUserService = new Mock<IUserService>();
            var mockConfiguration = new Mock<IConfiguration>();

            string emailAddress = "test@email.com"; 
            Dictionary<string, dynamic> personalisation = new Dictionary<string, dynamic>() 
            {
                { "reason-for-rejection", "reason" }
            };

            var sut = new EmailManager(mockEmailService.Object, mockUserService.Object, mockConfiguration.Object);

            ///Act
            await sut.SendDomainDsrMailboxAddressChangedEmailAsync(emailAddress, personalisation);

            ///Assert
            mockEmailService.Verify(x => x.SendDomainDsrMailboxAddressChangedEmailAsync(emailAddress, It.IsAny<Dictionary<string, dynamic>>()), Times.Once);

        }
    }
}
