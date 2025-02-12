using AutoFixture;
using AutoFixture.AutoMoq;
using cddo_users.DTOs;
using cddo_users.DTOs.EventLogs;
using cddo_users.models;
using cddo_users.test.TestHelpers;
using FluentAssertions;
using IdentityServer4.Endpoints.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace cddo_users.test.Api
{
    [TestFixture]
    public class UserControllerTests
    {
        protected readonly IFixture fixture;
        public UserControllerTests() 
        {
            fixture = new Fixture().Customize(new AutoMoqCustomization());
        }

        [Test]
        public async Task GivenAnExceptionIsThrown_WhenPostUserRoleDecision_Return500()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            var testException = new Exception("oh noes!");

            //Act
            testItems.MockUserService.Setup(u=>u.ApprovalDecision(It.IsAny<UserRoleApproval>())).Throws(testException);

            var result = (ObjectResult)await testItems.UserController.PostUserRoleDecision(It.IsAny<UserRoleApproval>());

            result.Should().NotBeNull();
            Assert.That(500, Is.EqualTo(result.StatusCode));

        }

        [Test]
        public async Task GivenAnApprovalDecision_HasBeenMade_EmailsAreSent()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            var userProfile = fixture.Create<UserProfile>();
            userProfile.EmailNotification = true;

            //Act
            testItems.MockUserService.Setup(u => u.ApprovalDecision(It.IsAny<UserRoleApproval>())).ReturnsAsync((true, 1));
            testItems.MockUserService.Setup(u => u.GetUserByIdAsync(It.IsAny<string>())).ReturnsAsync(userProfile);

            var result = (OkResult)await testItems.UserController.PostUserRoleDecision(It.IsAny<UserRoleApproval>());

            testItems.MockEmailManager.Verify(e=>e.SendRoleRequestDecisionEmails(It.IsAny<UserProfile>(), It.IsAny<UserRoleApproval>()), Times.Once);
            result.Should().NotBeNull();
        }

        [Test]
        public async Task GivenAnExceptionIsThrown_WhenPostUserRoleApprovalRequest_Return500()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            var testException = new Exception("oh noes!");

            //Act
            testItems.MockUserService.Setup(u => u.CreateUserApproval(It.IsAny<UserRoleApproval>())).Throws(testException);

            var result = (ObjectResult)await testItems.UserController.PostUserRoleApprovalRequest(It.IsAny<UserRoleApproval>());

            result.Should().NotBeNull();
            Assert.That(500, Is.EqualTo(result.StatusCode));

        }

        [Test]
        [TestCase(ApprovalStatus.Pending)]
        [TestCase(ApprovalStatus.NotRequested)]
        public async Task GivenRequestStatusIsPendingOrNotRequested_WhenPostUserRoleApprovalRequest_SendEmailsToOrgAdmins(ApprovalStatus status)
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            var userProfile = fixture.Create<UserProfile>();
            var orgAdmins = fixture.Create<Task<IEnumerable<EmailUserName>>?>();
            var userRoleApproval = new UserRoleApproval() 
            { 
                ApprovalID = 1,
                ApprovalStatus = status,
                OrganisationID = 1,
                RoleID = 3
            };


            //Act
            testItems.MockUserService.Setup(u => u.CreateUserApproval(It.IsAny<UserRoleApproval>()));
            testItems.MockUserService.Setup(u => u.GetUserByIdAsync(It.IsAny<string>())).ReturnsAsync(userProfile);
            testItems.MockUserService.Setup(u => u.GetOrgAdminsByOrgId(It.IsAny<int>())).Returns(orgAdmins);

            var result = (OkResult)await testItems.UserController.PostUserRoleApprovalRequest(userRoleApproval);

            testItems.MockEmailManager.Verify(e => e.SendApprovalRequestEmails(It.IsAny<int>(), It.IsAny<UserProfile>(), orgAdmins!.Result.ToList()), Times.Once);
            
            result.Should().NotBeNull();

        }

        [Test]
        public async Task GivenAnExceptionIsThrown_WhenPostUserRoleApprovalRequestMultiple_Return500()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            var testException = new Exception("oh noes!");
            var multipleRequests = fixture.Create<List<UserRoleApproval>>();

            //Act
            testItems.MockUserService.Setup(u => u.CreateUserApproval(It.IsAny<UserRoleApproval>())).Throws(testException);

            var result = (ObjectResult)await testItems.UserController.PostUserRoleApprovalRequestMultiple(multipleRequests);

            result.Should().NotBeNull();
            Assert.That(500, Is.EqualTo(result.StatusCode));

        }

        [Test]
        public async Task GivenAMultipleApprovalsAreRequested_WhenPostUserRoleApprovalRequestMultiple_MultipleRequestsAreCreatedAndEmails()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            var testException = new Exception("oh noes!");
            var multipleRequests = fixture.Create<List<UserRoleApproval>>();
            var userProfile = fixture.Create<UserProfile>();
            var orgAdmins = fixture.Create<Task<IEnumerable<EmailUserName>>?>();

            //Act
            testItems.MockUserService.Setup(u => u.CreateUserApproval(It.IsAny<UserRoleApproval>()));
            testItems.MockUserService.Setup(u => u.GetUserByIdAsync(It.IsAny<string>())).ReturnsAsync(userProfile);
            testItems.MockUserService.Setup(u => u.GetOrgAdminsByOrgId(It.IsAny<int>())).Returns(orgAdmins);

            var result = (OkResult)await testItems.UserController.PostUserRoleApprovalRequestMultiple(multipleRequests);

            testItems.MockUserService.Verify(u => u.CreateUserApproval(It.IsAny<UserRoleApproval>()), Times.Exactly(multipleRequests.Count));
            testItems.MockEmailManager.Verify(u => u.SendApprovalRequestEmailsMultipleRoles(It.IsAny<UserProfile>(), orgAdmins!.Result.ToList()), Times.Once);
            result.Should().NotBeNull();

        }

        [Test]
        public async Task GivenASingleApprovalsRequested_WhenPostUserRoleApprovalRequestMultiple_SingleRequestsAreCreatedAndEmail()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            var testException = new Exception("oh noes!");
            var userProfile = fixture.Create<UserProfile>();
            var orgAdmins = fixture.Create<Task<IEnumerable<EmailUserName>>?>();
            var singleRequest = new List<UserRoleApproval>() { new UserRoleApproval() { RoleID = 4} };

            //Act
            testItems.MockUserService.Setup(u => u.CreateUserApproval(It.IsAny<UserRoleApproval>()));
            testItems.MockUserService.Setup(u => u.GetUserByIdAsync(It.IsAny<string>())).ReturnsAsync(userProfile);
            testItems.MockUserService.Setup(u => u.GetOrgAdminsByOrgId(It.IsAny<int>())).Returns(orgAdmins!);

            var result = (OkResult)await testItems.UserController.PostUserRoleApprovalRequestMultiple(singleRequest);

            testItems.MockUserService.Verify(u => u.CreateUserApproval(It.IsAny<UserRoleApproval>()), Times.Exactly(singleRequest.Count));
            testItems.MockEmailManager.Verify(u => u.SendApprovalRequestEmails(It.IsAny<int>(), It.IsAny<UserProfile>(), orgAdmins!.Result.ToList()), Times.Once);
            result.Should().NotBeNull();

        }

        [Test]
        public async Task GivenLoginStatusHas_UpdateLastLogin()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim(ClaimTypes.Email, "test@email.com");
            var clientRequest = fixture.Create<ClientRequest>();

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.UserController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var result = (OkObjectResult)await testItems.UserController.UpdateLastLogin();

            testItems.MockUserService.Verify(u => u.UpdateLastLogin("test@email.com"), Times.Once);

            result.Should().NotBeNull();
        }

        [Test]
        public async Task GetUserUpproval_WithApprovalId()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            var userApprovalDetail = fixture.Create<UserRoleApprovalDetail>();
            //Act
            testItems.MockUserService.Setup(u => u.Approve(It.IsAny<int>())).ReturnsAsync(userApprovalDetail);
            var result = (OkObjectResult)await testItems.UserController.GetUserRoleApproval(It.IsAny<int>());

            result.Should().NotBeNull();
            result.Value.Should().BeEquivalentTo(userApprovalDetail);
        }

        [Test]
        public async Task GivenAnExceptionIsThrown_WhenGetUserApprovals_Return500()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            var testException = new Exception("oh noes!");
            var multipleRequests = fixture.Create<UserRoleApprovalRequest>();

            //Act
            testItems.MockUserService.Setup(u => u.GetUserApprovalsAsync(It.IsAny<UserRoleApprovalRequest>())).Throws(testException);

            var result = (ObjectResult)await testItems.UserController.GetUserApprovals(multipleRequests);

            result.Should().NotBeNull();
            Assert.That(500, Is.EqualTo(result.StatusCode));

        }

        [Test]
        public async Task GetUserApprovals_WhenUserAppovalsAreEmpty_NoContent()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            var multipleRequests = fixture.Create<UserRoleApprovalRequest>();

            //Act
            testItems.MockUserService.Setup(u => u.GetUserApprovalsAsync(It.IsAny<UserRoleApprovalRequest>())).ReturnsAsync((null, 0));

            var result = (NotFoundObjectResult)await testItems.UserController.GetUserApprovals(multipleRequests);

            result.Should().NotBeNull();

        }

        [Test]
        public async Task GetUserApprovals_WhenApprovalsAvailable_PaginatedResults()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            var multipleRequests = fixture.Create<UserRoleApprovalRequest>();
            var userApprovalDetails = fixture.Create<IEnumerable<UserRoleApprovalDetail>>();
            //Act
            testItems.MockUserService.Setup(u => u.GetUserApprovalsAsync(It.IsAny<UserRoleApprovalRequest>())).ReturnsAsync((userApprovalDetails, userApprovalDetails.Count()));

            var result = (OkObjectResult)await testItems.UserController.GetUserApprovals(multipleRequests);

            result.Should().NotBeNull();
            var paginatedResult = result.Value as PaginatedUserRoleApprovalDetails;

            paginatedResult!.TotalCount.Should().Be(userApprovalDetails.Count());
        }

        [Test]
        public async Task GivenAnExceptionIsThrown_WhenGetUserPendingApprovals_Return500()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            var testException = new Exception("oh noes!");
            var multipleRequests = fixture.Create<UserRoleApprovalRequest>();

            //Act
            testItems.MockUserService.Setup(u => u.GetUserPendingApprovalsAsync(It.IsAny<int>(), It.IsAny<int>())).Throws(testException);

            var result = (ObjectResult)await testItems.UserController.GetUserPendingApprovals(1, 1);

            result.Should().NotBeNull();
            Assert.That(500, Is.EqualTo(result.StatusCode));

        }

        [Test]
        public async Task GetUserApprovals_WhenPendingUserAppovalsAreEmpty_NoContent()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            var multipleRequests = fixture.Create<UserRoleApprovalRequest>();

            //Act
            testItems.MockUserService.Setup(u => u.GetUserPendingApprovalsAsync(It.IsAny<int>(), It.IsAny<int>()));

            var result = (NotFoundObjectResult)await testItems.UserController.GetUserApprovals(multipleRequests);

            result.Should().NotBeNull();

        }

        [Test]
        public async Task GetPendingUserApprovals_WhenPendingApprovalsAvailable_PaginatedResults()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            var multipleRequests = fixture.Create<UserRoleApprovalRequest>();
            var userApprovalDetails = fixture.Create<IEnumerable<UserRoleApprovalDetail>>();
            //Act
            testItems.MockUserService.Setup(u => u.GetUserPendingApprovalsAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync((userApprovalDetails));

            var result = (OkObjectResult)await testItems.UserController.GetUserPendingApprovals(It.IsAny<int>(), It.IsAny<int>());

            result.Should().NotBeNull();
            var paginatedResult = result.Value;

            paginatedResult.Should().Be(userApprovalDetails);
        }

        [Test]
        public async Task GetPublisherRequestStatus_WithUseId_Status()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            var userStatus = ApprovalStatus.Approved;
            //Act
            testItems.MockUserService.Setup(u => u.CheckPublisherApproval(It.IsAny<int>())).ReturnsAsync(userStatus);

            var result = (OkObjectResult)await testItems.UserController.GetPublisherRequestStatus(It.IsAny<int>());
            
            result.Should().NotBeNull();
            result.Value.Should().Be(userStatus.ToString());
        }

        [Test]
        public async Task GivenAnExceptionIsThrown_WhenGetEventLogs_Return500()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            var testException = new Exception("oh noes!");
            var multipleRequests = fixture.Create<UserRoleApprovalRequest>();

            //Act
            testItems.MockApplicationInsightsService.Setup(u => u.GetEventLogsAsync(It.IsAny<int>(), It.IsAny<string>())).Throws(testException);

            var result = (ObjectResult)await testItems.UserController.GetEventLogs(It.IsAny<int>());

            result.Should().NotBeNull();
            Assert.That(500, Is.EqualTo(result.StatusCode));

        }

        [Test]
        public async Task GetEventLogs_WhenNoEventLogsMatchFilter_NoContent()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            var multipleRequests = fixture.Create<UserRoleApprovalRequest>();

            //Act
            testItems.MockApplicationInsightsService.Setup(u => u.GetEventLogsAsync(It.IsAny<int>(), It.IsAny<string>()));

            var result = (NotFoundObjectResult)await testItems.UserController.GetEventLogs(It.IsAny<int>());

            result.Should().NotBeNull();
        }

        [Test]
        public async Task GetEventLogs_WhenEventLogsMatchFilter_EventLogs()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            var multipleRequests = fixture.Create<UserRoleApprovalRequest>();
            var eventLogs = fixture.Create<EventLogResponse>();

            //Act
            testItems.MockApplicationInsightsService.Setup(u => u.GetEventLogsAsync(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(eventLogs);

            var result = (OkObjectResult)await testItems.UserController.GetEventLogs(It.IsAny<int>());

            result.Should().NotBeNull();
            result.Value.Should().BeEquivalentTo(eventLogs);
        }

        [Test]
        public async Task GetEventLogs_ByTimeRangeWithInValidTimeRange_BadRequest()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            
            //Act
            var result = (BadRequestObjectResult)await testItems.UserController.GetEventLogs(It.IsAny<string>(), "invalidTime");

            result.Should().NotBeNull();
            Assert.That(400, Is.EqualTo(result.StatusCode));
        }

        [Test]
        public async Task GetEventLogs_ByTimeRangeWithValidTimeRangeNoUser_BadRequest()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim(ClaimTypes.Email, "test@email.com");
            var userNameClaim = new Claim(ClaimTypes.Email, "Test User");

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.UserController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var result = (BadRequestObjectResult)await testItems.UserController.GetEventLogs(It.IsAny<string>(), string.Empty);

            result.Should().NotBeNull();
            Assert.That(400, Is.EqualTo(result.StatusCode));
        }

        [Test]
        public async Task GetEventLogs_ByTimeRangeWithValidTimeRangeAndValidUserFromRawQuery_EventLogsByUser()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim(ClaimTypes.Email, "test@email.com");
            var userNameClaim = new Claim("display_name", "Test User");
            var userProfile = fixture.Create<UserProfile>();
            var logs = fixture.Create<ILogsQueryDataResult>();

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim, userNameClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.UserController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            testItems.MockUserService.Setup(u => u.GetUserInfo(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(userProfile);
            testItems.MockApplicationInsightsService.Setup(a => a.GetEventLogsFromRawQueryAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), userProfile)).ReturnsAsync(logs);
            var result = (OkObjectResult)await testItems.UserController.GetEventLogs(It.IsAny<string>(), string.Empty);

            result.Should().NotBeNull();
            result.Value.Should().NotBeNull();
            result.Value.Should().BeEquivalentTo(logs);
        }

        [Test]
        public async Task GetEventLogs_ByTimeRangeWithValidTimeRangeValidUser_EventLogsByUser()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim(ClaimTypes.Email, "test@email.com");
            var userNameClaim = new Claim("display_name", "Test User");
            var userProfile = fixture.Create<UserProfile>();
            var logs = fixture.Create<ILogsQueryDataResult>();
            var searchClasses = fixture.Create<IEnumerable<string>>();
            var timeSpan = fixture.Create<TimeSpan>(); 

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim, userNameClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.UserController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            testItems.MockUserService.Setup(u => u.GetUserInfo(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(userProfile);
            testItems.MockApplicationInsightsService.Setup(a => a.GetEventLogsAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<TimeSpan>(), userProfile)).ReturnsAsync(logs);
            var result = (OkObjectResult)await testItems.UserController.GetEventLogsEx("testTable", searchClasses, timeSpan.ToString());

            result.Should().NotBeNull();
            result.Value.Should().NotBeNull();
            result.Value.Should().BeEquivalentTo(logs);
        }

        [Test]
        public async Task GetEventLogs_ByTimeRangeWithValidTimeRangeValidUserInputTimeSpan_EventLogsByUser()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim(ClaimTypes.Email, "test@email.com");
            var userNameClaim = new Claim("display_name", "Test User");
            var userProfile = fixture.Create<UserProfile>();
            var logs = fixture.Create<ILogsQueryDataResult>();
            var timeSpan = fixture.Create<TimeSpan>();

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim, userNameClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.UserController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            testItems.MockUserService.Setup(u => u.GetUserInfo(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(userProfile);
            testItems.MockApplicationInsightsService.Setup(a => a.GetEventLogsAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<TimeSpan>(), userProfile)).ReturnsAsync(logs);
            var result = (OkObjectResult)await testItems.UserController.GetEventLogsEx(null, It.IsAny<IEnumerable<string>>(), timeSpan.ToString());

            result.Should().NotBeNull();
            result.Value.Should().NotBeNull();
            result.Value.Should().BeEquivalentTo(logs);
        }

        [Test]
        public async Task GetEventLogsEx_ByTimeRangeWithValidTimeRangeNoUser_BadRequest()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim(ClaimTypes.Email, string.Empty);
            var userNameClaim = new Claim("display_name", "Test User");
            var userProfile = fixture.Create<UserProfile>();
            var logs = fixture.Create<ILogsQueryDataResult>();

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim, userNameClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.UserController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            testItems.MockApplicationInsightsService.Setup(a => a.GetEventLogsAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<TimeSpan>(), userProfile)).ReturnsAsync(logs);
            var result = (BadRequestObjectResult)await testItems.UserController.GetEventLogsEx(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>());

            result.Should().NotBeNull();
            Assert.That(400, Is.EqualTo(result.StatusCode));
        }
        [Test]
        public async Task GivenEmailAddressIsNotProvided_WhenSignInOrUpdateUser_BadRequest()
        {
            var testItems = TestSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim(ClaimTypes.Email, string.Empty);
            var userNameClaim = new Claim("name", "Test Name");

            mockUser.Setup(u => u.Claims)
               .Returns(new List<Claim> { emailClaim, userNameClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.UserController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var result = (BadRequestObjectResult)await testItems.UserController.SignInOrUpdateUser();

            result.Should().NotBeNull();
            Assert.That(400, Is.EqualTo(result.StatusCode));
        }

        [Test]
        public async Task GivenDomainIsNotValid_WhenSignInOrUpdateUser_NotFound()
        {
            var testItems = TestSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim(ClaimTypes.Email, "test@domain.com");
            var userNameClaim = new Claim("name", "Test Name");

            mockUser.Setup(u => u.Claims)
               .Returns(new List<Claim> { emailClaim, userNameClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.UserController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var result = (NotFoundObjectResult)await testItems.UserController.SignInOrUpdateUser();

            result.Should().NotBeNull();
            Assert.That(404, Is.EqualTo(result.StatusCode));
        }

        [Test]
        public async Task GivenOrganasationForTheDomainIsInvalid_WhenSignInOrUpdateUser_NotFound()
        {
            var testItems = TestSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim(ClaimTypes.Email, "test@domain.com");
            var userNameClaim = new Claim("name", "Test Name");
            var domainInfo = fixture.Create<DomainInfoDto>();
            domainInfo.OrganisationId = 0;

            mockUser.Setup(u => u.Claims)
               .Returns(new List<Claim> { emailClaim, userNameClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.UserController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            testItems.MockUserService.Setup(u => u.GetOrganisationIdByDomainAsync(It.IsAny<string>())).ReturnsAsync(domainInfo);

            var result = (NotFoundObjectResult)await testItems.UserController.SignInOrUpdateUser();

            result.Should().NotBeNull();
            Assert.That(404, Is.EqualTo(result.StatusCode));
        }

        [Test]
        public async Task GivenCorrectDomainAndOrganisation_WhenSignInOrUpdateUser_UserProfile()
        {
            var testItems = TestSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim(ClaimTypes.Email, "test@domain.com");
            var userNameClaim = new Claim("name", "Test Name");
            var domainInfo = fixture.Create<DomainInfoDto>();
            var userProfile = fixture.Create<UserProfile>();
            mockUser.Setup(u => u.Claims)
               .Returns(new List<Claim> { emailClaim, userNameClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.UserController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            testItems.MockUserService.Setup(u => u.GetOrganisationIdByDomainAsync(It.IsAny<string>())).ReturnsAsync(domainInfo);
            testItems.MockUserService.Setup(u => u.SignInOrUpdateUser(It.IsAny<string>(), It.IsAny<string>(), domainInfo)).ReturnsAsync(userProfile);

            var result = (OkObjectResult)await testItems.UserController.SignInOrUpdateUser();

            result.Should().NotBeNull();
            result.Value.Should().NotBeNull();
            result.Value.Should().BeEquivalentTo(userProfile);
        }

        [Test]
        public async Task AllRoles_WhenRequested()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            var roles = fixture.Create<List<DTOs.Role>>();

            //Act
            testItems.MockUserService.Setup(u => u.GetAllRolesAsync()).ReturnsAsync(roles);

            var result = (OkObjectResult)await testItems.UserController.AllRoles();

            //Assert
            result.Should().NotBeNull();
            result.Value.Should().BeEquivalentTo(roles);
        }

        [Test]
        public async Task GetUserById_WhenRequested()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            var userProfile = fixture.Create<UserProfile>();

            //Act
            testItems.MockUserService.Setup(u => u.GetUserByIdAsync(It.IsAny<string>())).ReturnsAsync(userProfile);

            var result = (OkObjectResult)await testItems.UserController.GetUser(It.IsAny<string>());

            //Assert
            result.Should().NotBeNull();
            result.Value.Should().BeEquivalentTo(userProfile);
        }

        [Test]
        public async Task GetUserByEmail_WhenNoUserExistsRequested_NotFound()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            var userProfile = fixture.Create<UserProfile>();

            //Act
            testItems.MockUserService.Setup(u => u.GetUserByEmailAsync(It.IsAny<string>()));

            var result = await testItems.UserController.GetUserByEmail(It.IsAny<string>()) as NotFoundResult;

            //Assert
            result.Should().NotBeNull();
        }

        [Test]
        public async Task GetUserByEmail_WhenUserExistsRequested_UserProfile()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            var userProfile = fixture.Create<UserProfile>();

            //Act
            testItems.MockUserService.Setup(u => u.GetUserByEmailAsync(It.IsAny<string>())).ReturnsAsync(userProfile);

            var result = (OkObjectResult)await testItems.UserController.GetUserByEmail(It.IsAny<string>());

            //Assert
            result.Should().NotBeNull();
            result.Value.Should().BeEquivalentTo(userProfile);
        }

        [Test]
        public async Task GetUserInfoWithAToken_WhenUserDoesntExist_BadRequest()
        {
            var testItems = TestSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim(ClaimTypes.Email, "test@domain.com");
            var userNameClaim = new Claim("name", "Test Name");
            
            mockUser.Setup(u => u.Claims)
               .Returns(new List<Claim> { emailClaim, userNameClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.UserController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var result = (BadRequestObjectResult)await testItems.UserController.GetUserInfo();

            result.Should().NotBeNull();
            Assert.That(400, Is.EqualTo(result.StatusCode));
        }

        [Test]
        public async Task GetUserInfoWithAToken_WhenUserProfileCanBeSetFromToken_UserProfile()
        {
            var testItems = TestSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim(ClaimTypes.Email, "test@domain.com");
            var userNameClaim = new Claim("name", "Test Name");
            var userProfile = fixture.Create<UserProfile>();
            mockUser.Setup(u => u.Claims)
               .Returns(new List<Claim> { emailClaim, userNameClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.UserController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            testItems.MockUserService.Setup(u=>u.GetUserInfo(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(userProfile);
            var result = (OkObjectResult)await testItems.UserController.GetUserInfo();

            result.Should().NotBeNull();
            result.Value.Should().NotBeNull();
            result.Value.Should().BeEquivalentTo(userProfile);
        }

        [Test]
        public async Task UpdatePreferences_FromNotificationPreferencesWhenUserIsInvalid_BadRequest()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            var userProfile = fixture.Create<UserProfile>();
            var notificationPreferenceRequest = fixture.Create<NotificationPreferences>();

            //Act
            testItems.MockUserService.Setup(u => u.UpdatePreferences(It.IsAny<int>(), It.IsAny<bool>())).ReturnsAsync(false);

            var result = (BadRequestResult)await testItems.UserController.UpdatePreferences(notificationPreferenceRequest);

            //Assert
            result.Should().NotBeNull();
            Assert.That(400, Is.EqualTo(result.StatusCode));
        }

        [Test]
        public async Task UpdatePreferences_FromNotificationPreferencesWhenUserIsInvalid_Updated()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            var pefs = fixture.Create<NotificationPreferences>();

            //Act
            testItems.MockUserService.Setup(u => u.UpdatePreferences(It.IsAny<int>(), It.IsAny<bool>())).ReturnsAsync(true);

            var result = (OkResult)await testItems.UserController.UpdatePreferences(pefs);

            //Assert
            result.Should().NotBeNull();
        }

        [Test]
        public async Task AddUserToRole_WhenUserIdIsEmpty_BadRequest()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();

            //Act
            var result = (BadRequestObjectResult)await testItems.UserController.AddUserToRole(It.IsAny<string>(), string.Empty);

            //Assert
            result.Should().NotBeNull();
            Assert.That(400, Is.EqualTo(result.StatusCode));
        }

        [Test]
        public async Task AddUserToRole_WhenUserEmailIsEmpty_BadRequest()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim(ClaimTypes.Email, string.Empty);

            mockUser.Setup(u => u.Claims)
               .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.UserController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            //Act
            var result = (BadRequestObjectResult)await testItems.UserController.AddUserToRole(It.IsAny<string>(), "test@email.com");

            //Assert
            result.Should().NotBeNull();
            Assert.That(400, Is.EqualTo(result.StatusCode));
        }

        [Test]
        public async Task AddUserToRole_WhenUserEmailExistsButUserDoesnt_BadRequest()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim(ClaimTypes.Email, "test@domain.com");

            mockUser.Setup(u => u.Claims)
               .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.UserController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            testItems.MockUserService.Setup(u => u.GetUserInfo(It.IsAny<string>(), It.IsAny<string>()));

            //Act
            var result = (BadRequestObjectResult)await testItems.UserController.AddUserToRole(It.IsAny<string>(), "test@email.com");

            //Assert
            result.Should().NotBeNull();
            Assert.That(400, Is.EqualTo(result.StatusCode));
        }

        [Test]
        public async Task AddUserToRole_WhenThereIsAnError_BadRequest()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim(ClaimTypes.Email, "test@domain.com");
            var userProfile = fixture.Create<UserProfile>();
            int userId = 1;
            int roleId = 2;
            int approverUserId = 3;

            mockUser.Setup(u => u.Claims)
               .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.UserController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            testItems.MockUserService.Setup(u => u.GetUserInfo(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(userProfile);
            testItems.MockUserService.Setup(u => u.AddUserToRoleAsync(userId, roleId, approverUserId)).ReturnsAsync(false);

            //Act
            var result = (BadRequestObjectResult)await testItems.UserController.AddUserToRole(It.IsAny<string>(), "test@email.com");

            //Assert
            result.Should().NotBeNull();
            Assert.That(400, Is.EqualTo(result.StatusCode));
        }

        [Test]
        public async Task AddUserToRole_WhenThereAreNoErrors_RoleAdded()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim(ClaimTypes.Email, "test@domain.com");
            var userProfile = fixture.Create<UserProfile>();

            mockUser.Setup(u => u.Claims)
               .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.UserController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            testItems.MockUserService.Setup(u => u.GetUserByEmailAsync(It.IsAny<string>())).ReturnsAsync(userProfile);
            testItems.MockUserService.Setup(u => u.AddUserToRoleAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(true);

            //Act
            var result = (OkObjectResult)await testItems.UserController.AddUserToRole("1", "4");

            //Assert
            result.Should().NotBeNull();
        }

        [Test]
        public async Task RemoveUserFromRole_WhenThereUserIdIsEmpty_BadRequest()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
           
            //Act
            var result = (BadRequestObjectResult)await testItems.UserController.RemoveUserFromRole(It.IsAny<string>(), string.Empty);

            //Assert
            result.Should().NotBeNull();
            result.Value.Should().BeEquivalentTo("User ID is missing.");
        }

        [Test]
        public async Task RemoveUserFromRole_WhenUserRoleCantBeFound_NotFound()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();

            //Act
            var result = (NotFoundObjectResult)await testItems.UserController.RemoveUserFromRole("2", "6");

            //Assert
            result.Should().NotBeNull();
            result.Value.Should().BeEquivalentTo("User not found or error removing from role.");
        }

        [Test]
        public async Task RemoveUserFromRole_WhenUserCantBeFound_RemoveRole()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();

            //Act
            testItems.MockUserService.Setup(u => u.RemoveUserFromRoleAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(false);
            var result = (NotFoundObjectResult)await testItems.UserController.RemoveUserFromRole("2", "4");

            //Assert
            result.Should().NotBeNull();
            result.Value.Should().BeEquivalentTo("User not found or error removing from role.");
        }

        [Test]
        public async Task RemoveUserFromRole_WithUserDetailsToSendEmail_EmailSent()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            var userProfile = fixture.Create<UserProfile>();
            userProfile.EmailNotification = true;
            //Act
            testItems.MockUserService.Setup(u => u.RemoveUserFromRoleAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(true);
            testItems.MockUserService.Setup(u => u.GetUserByIdAsync(It.IsAny<string>())).ReturnsAsync(userProfile);
            var result = (OkObjectResult)await testItems.UserController.RemoveUserFromRole("2", "4");

            //Assert
            result.Should().NotBeNull();
            testItems.MockEmailManager.Verify(m => m.SendRoleRemovedEmails(It.IsAny<string>(), It.IsAny<UserProfile>()), Times.Once());
            result.Value.Should().BeEquivalentTo("User removed from role successfully.");
        }

        [Test]
        public async Task GivenAnExceptionIsThrown_WhenGetUsers_ReturnEmptyListOfUserProfiles()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            var testException = new Exception("oh noes!");
            var userIds = fixture.Create<IEnumerable<string>>();

            //Act
            testItems.MockUserService.Setup(u => u.GetUserByIdAsync(It.IsAny<string>())).Throws(testException);

            var result = (ObjectResult)await testItems.UserController.GetUsers(userIds);

            result.Should().NotBeNull();
            result.Value.Should().BeOfType<List<UserProfile>>();
            var  returnedUserProfiles = result.Value as List<UserProfile>;
            returnedUserProfiles!.Count.Should().Be(0);
        }

        [Test]
        public async Task GivenUserIdsRequestedExists_WhenGetUsers_ReturnMatchingUserProfiles()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            var userIds = fixture.Create<IEnumerable<string>>();
            var userProfiles = fixture.Create<UserProfile>();

            //Act
            testItems.MockUserService.Setup(u => u.GetUserByIdAsync(It.IsAny<string>())).ReturnsAsync(userProfiles);

            var result = (ObjectResult)await testItems.UserController.GetUsers(userIds);

            result.Should().NotBeNull();
            result.Value.Should().BeOfType<List<UserProfile>>();
            var returnedUserProfiles = result.Value as List<UserProfile>;
            returnedUserProfiles!.Count.Should().Be(userIds.Count());
        }

        [Test]
        public async Task GetUsers_WhenRequested()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            var roles = fixture.Create<UserResponseDto>();

            //Act
            testItems.MockUserService.Setup(u => u.GetFilteredUsersAsync(It.IsAny<UserQueryParameters>())).ReturnsAsync(roles);

            var result = (OkObjectResult)await testItems.UserController.GetUsers(It.IsAny<UserQueryParameters>());

            //Assert
            result.Should().NotBeNull();
            result.Value.Should().BeEquivalentTo(roles);
        }

        [Test]
        public async Task GetUserApprovalsByUserId_WhenUserIdIsEmpty_ReturnNull()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();

            //Act
            var result = (OkObjectResult)await testItems.UserController.GetUserApprovalsByUserId(0);

            //Assert
            result.Should().NotBeNull();
            result.Value.Should().Be(null);
        }

        [Test]
        public async Task GetUserApprovalsByUserId_WhenGetUserApprovalsAsyncThrowsException_BadRequest()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            var testException = new Exception("oh noes!");

            //Act
            testItems.MockUserService.Setup(u => u.GetUserApprovalsAsync(It.IsAny<int>())).Throws(testException);
            var result = (BadRequestResult)await testItems.UserController.GetUserApprovalsByUserId(1);

            //Assert
            result.Should().NotBeNull();
            Assert.That(400, Is.EqualTo(result.StatusCode));
        }

        [Test]
        public async Task GetUserApprovalsByUserId_WhenApprovalsExists_UserApprovals()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            var userApprovals = fixture.Create<IEnumerable<UserRoleApprovalDetail>>();

            //Act
            testItems.MockUserService.Setup(u => u.GetUserApprovalsAsync(It.IsAny<int>())).ReturnsAsync(userApprovals);
            var result = (OkObjectResult)await testItems.UserController.GetUserApprovalsByUserId(1);

            //Assert
            result.Should().NotBeNull();
            result.Value.Should().NotBeNull();
            result.Value.Should().BeEquivalentTo(userApprovals);
        }
    }
}
