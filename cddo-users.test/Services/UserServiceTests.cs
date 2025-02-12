using AutoFixture.AutoMoq;
using AutoFixture;
using NUnit.Framework;
using cddo_users.Repositories;
using cddo_users.Interface;
using Moq;
using cddo_users.DTOs;
using cddo_users.Logic;

namespace cddo_users.test.Services
{
    [TestFixture]
    public class UserServiceTests
    {
        protected readonly IFixture fixture;

        private readonly UserService _userService;

        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IEmailService> _emailServiceMock = new();

        public UserServiceTests()
        {
            fixture = new Fixture().Customize(new AutoMoqCustomization());

            _userService = new(_userRepositoryMock.Object, _emailServiceMock.Object);

        }

        [Test]
        public async Task CreateUserApproval_CreatesApproval_WhenNoExistingApproval()
        {
            // Arrange
            var approval = fixture.Create<UserRoleApproval>(); 
            _userRepositoryMock
                .Setup(repo => repo.GetUserApprovalAsync(approval))
                .ReturnsAsync((UserRoleApprovalDetail?)null); 

            _userRepositoryMock
                .Setup(repo => repo.CreateUserApproval(approval))
                .Returns(Task.CompletedTask);

            _userRepositoryMock.Invocations.Clear(); 

            // Act
            await _userService.CreateUserApproval(approval);

            // Assert
            _userRepositoryMock.Verify(
                repo => repo.GetUserApprovalAsync(approval),
                Times.Once 
            );
            _userRepositoryMock.Verify(
                repo => repo.CreateUserApproval(approval),
                Times.Once 
            );
            _userRepositoryMock.Verify(
                repo => repo.DeleteUserApprovalAsync(It.IsAny<UserRoleApprovalDetail>()),
                Times.Never 
            );
        }

        [Test]
        public async Task CreateUserApproval_DeletesExistingApproval_AndCreatesNewApproval_WhenApprovalExists()
        {
            // Arrange
            var approval = fixture.Create<UserRoleApproval>(); 
            var existingApproval = fixture.Create<UserRoleApprovalDetail>(); 

            _userRepositoryMock
                .Setup(repo => repo.GetUserApprovalAsync(approval))
                .ReturnsAsync(existingApproval);

            _userRepositoryMock
                .Setup(repo => repo.DeleteUserApprovalAsync(existingApproval))
                .Returns(Task.CompletedTask); 

            _userRepositoryMock
                .Setup(repo => repo.CreateUserApproval(approval))
                .Returns(Task.CompletedTask); 

            _userRepositoryMock.Invocations.Clear(); 

            // Act
            await _userService.CreateUserApproval(approval);

            // Assert
            _userRepositoryMock.Verify(
                repo => repo.GetUserApprovalAsync(approval),
                Times.Once 
            );
            _userRepositoryMock.Verify(
                repo => repo.DeleteUserApprovalAsync(existingApproval),
                Times.Once 
            );
            _userRepositoryMock.Verify(
                repo => repo.CreateUserApproval(approval),
                Times.Once 
            );
        }
        [Test]
        public async Task GetUserApprovalsAsync_ReturnsApprovals_WhenValidRequest()
        {
            // Arrange
            var request = fixture.Create<UserRoleApprovalRequest>();
            var approvals = fixture.CreateMany<UserRoleApprovalDetail>(5); 
            var totalCount = approvals.Count();

            _userRepositoryMock
                .Setup(repo => repo.GetUserApprovalsAsync(request))
                .ReturnsAsync((approvals, totalCount));

            _userRepositoryMock.Invocations.Clear();

            // Act
            var result = await _userService.GetUserApprovalsAsync(request);

            // Assert
            _userRepositoryMock.Verify(
                repo => repo.GetUserApprovalsAsync(request),
                Times.Once 
            );
            Assert.That(result.Approvals, Is.EqualTo(approvals)); 
            Assert.That(result.TotalCount, Is.EqualTo(totalCount));
        }
        [Test]
        public async Task GetUserApprovalsAsync_ReturnsApprovals_WhenValidUserId()
        {
            //Arrange
            var userId = 1;
            var approvals = fixture.CreateMany<UserRoleApprovalDetail>(3);
            _userRepositoryMock
                .Setup(repo => repo.GetUserApprovalsAsync(userId))
                .ReturnsAsync(approvals);

            _userRepositoryMock.Invocations.Clear();

            //Act
            var result = await _userService.GetUserApprovalsAsync(userId);

            //Assert
            _userRepositoryMock.Verify(repo => repo.GetUserApprovalsAsync(userId), Times.Once);
            Assert.That(result, Is.EqualTo(approvals));
        }
        [Test]
        public async Task GetUserPendingApprovalsAsync_ReturnsPendingApprovals_WhenValidParameters()
        {
            // Arrange
            var domainId = 1;
            var organisationId = 2;
            var pendingApprovals = fixture.CreateMany<UserRoleApprovalDetail>(3);
            _userRepositoryMock
                .Setup(repo => repo.GetUserPendingApprovalsAsync(domainId, organisationId))
                .ReturnsAsync(pendingApprovals);

            _userRepositoryMock.Invocations.Clear();

            // Act
            var result = await _userService.GetUserPendingApprovalsAsync(domainId, organisationId);

            // Assert
            _userRepositoryMock.Verify(repo => repo.GetUserPendingApprovalsAsync(domainId, organisationId), Times.Once);
            Assert.That(result, Is.EqualTo(pendingApprovals));
        }

        [Test]
        public async Task Approve_ReturnsApprovalDetail_WhenValidId()
        {
            // Arrange
            var id = 1;
            var approvalDetail = fixture.Create<UserRoleApprovalDetail>();
            _userRepositoryMock
                .Setup(repo => repo.Approve(id))
                .ReturnsAsync(approvalDetail);

            _userRepositoryMock.Invocations.Clear();

            // Act
            var result = await _userService.Approve(id);

            // Assert
            _userRepositoryMock.Verify(repo => repo.Approve(id), Times.Once);
            Assert.That(result, Is.EqualTo(approvalDetail));
        }
        [Test]
        public async Task ApprovalDecision_ReturnsSuccess_WhenValidApproval()
        {
            // Arrange
            var approval = fixture.Create<UserRoleApproval>();
            var success = true;
            var affectedRows = 1;
            _userRepositoryMock
                .Setup(repo => repo.ApprovalDecision(approval))
                .ReturnsAsync((success, affectedRows));

            _userRepositoryMock.Invocations.Clear();

            // Act
            var result = await _userService.ApprovalDecision(approval);

            // Assert
            _userRepositoryMock.Verify(repo => repo.ApprovalDecision(approval), Times.Once);
            Assert.That(result.Item1, Is.EqualTo(success));
            Assert.That(result.Item2, Is.EqualTo(affectedRows));
        }
        [Test]
        public async Task GetOrganisationIdByDomainAsync_ReturnsDomainInfo_WhenValidDomainName()
        {
            // Arrange
            var domainName = "example.com";
            var expectedDomainInfo = fixture.Create<DomainInfoDto>();
            _userRepositoryMock
                .Setup(repo => repo.GetOrganisationIdByDomainAsync(domainName))
                .ReturnsAsync(expectedDomainInfo);

            _userRepositoryMock.Invocations.Clear();

            // Act
            var result = await _userService.GetOrganisationIdByDomainAsync(domainName);

            // Assert
            _userRepositoryMock.Verify(repo => repo.GetOrganisationIdByDomainAsync(domainName), Times.Once);
            Assert.That(result, Is.EqualTo(expectedDomainInfo));
        }

        [Test]
        public async Task AddUserToRoleAsync_ReturnsTrue_WhenUserRoleAddedSuccessfully()
        {
            // Arrange
            var userId = 1;
            var roleId = 2;
            var approverUserId = 3;

            _userRepositoryMock
                .Setup(repo => repo.UpdateUserRoleRequestAsync(userId, roleId, approverUserId))
                .ReturnsAsync(true);
            _userRepositoryMock
                .Setup(repo => repo.AddUserToRoleAsync(userId, roleId))
                .ReturnsAsync(true);

            _userRepositoryMock.Invocations.Clear();

            // Act
            var result = await _userService.AddUserToRoleAsync(userId, roleId, approverUserId);

            // Assert
            _userRepositoryMock.Verify(repo => repo.UpdateUserRoleRequestAsync(userId, roleId, approverUserId), Times.Once);
            _userRepositoryMock.Verify(repo => repo.AddUserToRoleAsync(userId, roleId), Times.Once);
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task AddUserToRoleAsync_ReturnsFalse_WhenUpdateUserRoleRequestFails()
        {
            // Arrange
            var userId = 1;
            var roleId = 2;
            var approverUserId = 3;

            _userRepositoryMock
                .Setup(repo => repo.UpdateUserRoleRequestAsync(userId, roleId, approverUserId))
                .ThrowsAsync(new Exception("Error updating user role request"));

            _userRepositoryMock.Invocations.Clear();

            // Act
            var result = await _userService.AddUserToRoleAsync(userId, roleId, approverUserId);

            // Assert
            _userRepositoryMock.Verify(repo => repo.UpdateUserRoleRequestAsync(userId, roleId, approverUserId), Times.Once);
            _userRepositoryMock.Verify(repo => repo.AddUserToRoleAsync(userId, roleId), Times.Never);
            Assert.That(result, Is.False);
        }
        [Test]
        public async Task UpdatePreferences_ReturnsTrue_WhenPreferencesUpdatedSuccessfully()
        {
            // Arrange
            var userId = fixture.Create<int>();
            var emailNotification = fixture.Create<bool>();

            _userRepositoryMock
                .Setup(repo => repo.UpdatePreferences(userId, emailNotification))
                .ReturnsAsync(true);

            _userRepositoryMock.Invocations.Clear();

            // Act
            var result = await _userService.UpdatePreferences(userId, emailNotification);

            // Assert
            _userRepositoryMock.Verify(repo => repo.UpdatePreferences(userId, emailNotification), Times.Once);
            Assert.That(result, Is.True);
        }
        [Test]
        public async Task RemoveUserFromRoleAsync_ReturnsTrue_WhenUserRemovedSuccessfully()
        {
            // Arrange
            var userId = fixture.Create<int>();
            var roleId = fixture.Create<int>();

            _userRepositoryMock
                .Setup(repo => repo.RemoveUserFromRoleAsync(userId, roleId))
                .ReturnsAsync(true);

            _userRepositoryMock.Invocations.Clear();

            // Act
            var result = await _userService.RemoveUserFromRoleAsync(userId, roleId);

            // Assert
            _userRepositoryMock.Verify(repo => repo.RemoveUserFromRoleAsync(userId, roleId), Times.Once);
            Assert.That(result, Is.True);
        }
        [Test]
        public async Task GetUserByEmailAsync_ReturnsUserProfile_WhenUserExists()
        {
            // Arrange
            var email = fixture.Create<string>();
            var expectedUserProfile = fixture.Create<UserProfile>();

            _userRepositoryMock
                .Setup(repo => repo.GetUserByEmailAsync(email))
                .ReturnsAsync(expectedUserProfile);

            _userRepositoryMock.Invocations.Clear();

            // Act
            var result = await _userService.GetUserByEmailAsync(email);

            // Assert
            _userRepositoryMock.Verify(repo => repo.GetUserByEmailAsync(email), Times.Once);
            Assert.That(result, Is.EqualTo(expectedUserProfile));
        }
        [Test]
        public async Task GetUserByIdAsync_ReturnsUserProfile_WhenUserExists()
        {
            // Arrange
            var userId = fixture.Create<string>();
            var expectedUserProfile = fixture.Create<UserProfile>();

            _userRepositoryMock
                .Setup(repo => repo.GetUserByIdAsync(userId))
                .ReturnsAsync(expectedUserProfile);

            _userRepositoryMock.Invocations.Clear();

            // Act
            var result = await _userService.GetUserByIdAsync(userId);

            // Assert
            _userRepositoryMock.Verify(repo => repo.GetUserByIdAsync(userId), Times.Once);
            Assert.That(result, Is.EqualTo(expectedUserProfile));
        }
        [Test]
        public async Task CreateUserAsync_ReturnsUserId_WhenUserCreatedSuccessfully()
        {
            // Arrange
            var userProfile = fixture.Create<UserProfile>();
            var expectedUserId = fixture.Create<int>();

            _userRepositoryMock
                .Setup(repo => repo.CreateUserAsync(userProfile))
                .ReturnsAsync(expectedUserId);

            _userRepositoryMock.Invocations.Clear();

            // Act
            var result = await _userService.CreateUserAsync(userProfile);

            // Assert
            _userRepositoryMock.Verify(repo => repo.CreateUserAsync(userProfile), Times.Once);
            Assert.That(result, Is.EqualTo(expectedUserId));
        }
        [Test]
        public async Task UpdateLastLogin_UpdatesLoginTime_WhenEmailExists()
        {
            // Arrange
            var email = fixture.Create<string>();

            _userRepositoryMock
                .Setup(repo => repo.UpdateLastLogin(email))
                .Returns(Task.CompletedTask);

            _userRepositoryMock.Invocations.Clear();

            // Act
            await _userService.UpdateLastLogin(email);

            // Assert
            _userRepositoryMock.Verify(repo => repo.UpdateLastLogin(email), Times.Once);
        }
        [Test]
        public async Task DeleteUserAsync_DeletesUser_WhenUserExists()
        {
            // Arrange
            var user = fixture.Create<models.User>();

            _userRepositoryMock
                .Setup(repo => repo.DeleteUserAsync(user))
                .Returns(Task.CompletedTask);

            _userRepositoryMock.Invocations.Clear();

            // Act
            await _userService.DeleteUserAsync(user);

            // Assert
            _userRepositoryMock.Verify(repo => repo.DeleteUserAsync(user), Times.Once);
        }
        [Test]
        public async Task CheckPublisherApproval_ReturnsApprovalStatus_WhenUserExists()
        {
            // Arrange
            var userId = fixture.Create<int>();
            var expectedApprovalStatus = fixture.Create<ApprovalStatus>();

            _userRepositoryMock
                .Setup(repo => repo.CheckPublisherApproval(userId))
                .ReturnsAsync(expectedApprovalStatus);

            _userRepositoryMock.Invocations.Clear();

            // Act
            var result = await _userService.CheckPublisherApproval(userId);

            // Assert
            _userRepositoryMock.Verify(repo => repo.CheckPublisherApproval(userId), Times.Once);
            Assert.That(result, Is.EqualTo(expectedApprovalStatus));
        }
        [Test]
        public async Task GetAllRolesAsync_ReturnsListOfRoles_WhenRolesExist()
        {
            // Arrange
            var roles = fixture.Create<List<DTOs.Role>>();

            _userRepositoryMock
                .Setup(repo => repo.GetAllRolesAsync())
                .ReturnsAsync(roles);

            _userRepositoryMock.Invocations.Clear();

            // Act
            var result = await _userService.GetAllRolesAsync();

            // Assert
            _userRepositoryMock.Verify(repo => repo.GetAllRolesAsync(), Times.Once);
            Assert.That(result, Is.EqualTo(roles));
        }
        [Test]
        public async Task GetOrgAdminsByOrgId_ReturnsListOfAdmins_WhenAdminsExist()
        {
            // Arrange
            int organisationId = fixture.Create<int>();
            var admins = fixture.Create<Task<IEnumerable<EmailUserName>>?>();

            _userRepositoryMock
                .Setup(repo => repo.GetOrgAdminsByOrgId(organisationId))
                .Returns(admins);

            _userRepositoryMock.Invocations.Clear();

            // Act
            var result = await _userService.GetOrgAdminsByOrgId(organisationId)!;

            // Assert
            _userRepositoryMock.Verify(repo => repo.GetOrgAdminsByOrgId(organisationId), Times.Once);
            Assert.That(result, Is.EqualTo(admins!.Result));
        }
        [Test]
        public async Task GetUserApprovalAsync_ReturnsApprovalDetail_WhenApprovalExists()
        {
            // Arrange
            var approval = fixture.Create<UserRoleApproval>();
            var approvalDetail = fixture.Create<UserRoleApprovalDetail>();

            _userRepositoryMock
                .Setup(repo => repo.GetUserApprovalAsync(approval))
                .ReturnsAsync(approvalDetail);

            _userRepositoryMock.Invocations.Clear();

            // Act
            var result = await _userService.GetUserApprovalAsync(approval);

            // Assert
            _userRepositoryMock.Verify(repo => repo.GetUserApprovalAsync(approval), Times.Once);
            Assert.That(result, Is.EqualTo(approvalDetail));
        }
        [Test]
        public async Task GetAllUsersByRoleType_ReturnsListOfUsers_WhenUsersExist()
        {
            // Arrange
            var roleType = fixture.Create<string>();
            var users = fixture.Create<List<UserAdminDto>>();

            _userRepositoryMock
                .Setup(repo => repo.GetAllUsersByRoleTypeAsync(roleType))
                .ReturnsAsync(users);

            _userRepositoryMock.Invocations.Clear();

            // Act
            var result = await _userService.GetAllUsersByRoleType(roleType);

            // Assert
            _userRepositoryMock.Verify(repo => repo.GetAllUsersByRoleTypeAsync(roleType), Times.Once);
            Assert.That(result, Is.EqualTo(users));
        }
        [Test]
        public async Task GetUserInfo_ReturnsUser_WhenUserExists()
        {
            // Arrange
            var userEmail = fixture.Create<string>();
            var userProfile = fixture.Create<UserProfile>();

            _userRepositoryMock
                .Setup(repo => repo.GetUserByEmailAsync(userEmail))
                .ReturnsAsync(userProfile);

            _userRepositoryMock.Invocations.Clear();

            // Act
            var result = await _userService.GetUserInfo(userEmail);

            // Assert
            _userRepositoryMock.Verify(repo => repo.GetUserByEmailAsync(userEmail), Times.Once);
            Assert.That(result, Is.EqualTo(userProfile));
        }

        [Test]
        public async Task GetUserInfo_ReturnsNull_WhenUserEmailIsNullOrEmpty()
        {
            // Arrange
            var userEmail = string.Empty;

            _userRepositoryMock.Invocations.Clear();


            // Act
            var result = await _userService.GetUserInfo(userEmail);

            // Assert
            _userRepositoryMock.Verify(repo => repo.GetUserByEmailAsync(It.IsAny<string>()), Times.Never);
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GetUserInfo_ReturnsNull_WhenUserNotFound()
        {
            // Arrange
            var userEmail = fixture.Create<string>();

            _userRepositoryMock
                .Setup(repo => repo.GetUserByEmailAsync(userEmail))
                .ReturnsAsync((UserProfile?)null); // user not found

            _userRepositoryMock.Invocations.Clear();

            // Act
            var result = await _userService.GetUserInfo(userEmail);

            // Assert
            _userRepositoryMock.Verify(repo => repo.GetUserByEmailAsync(userEmail), Times.Once);
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GetUserInfo_ShouldReturnNull_WhenEmailIsNull()
        {
            // Arrange
            _emailServiceMock.Setup(X => X.SendWelcomeEmail(It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>()));

            _userRepositoryMock.Invocations.Clear();
            _emailServiceMock.Invocations.Clear();
            // Act

            var result = await _userService.GetUserInfo(null, null);

            // Assert
            Assert.That(result, Is.Null);
            _userRepositoryMock.VerifyNoOtherCalls();
            _emailServiceMock.VerifyNoOtherCalls();
        }

        [Test]
        public async Task GetUserInfo_ShouldReturnUser_WhenUserExists()
        {
            // Arrange
            var userEmail = "existinguser@test.com";
            var user = fixture.Create<UserProfile>();

            _userRepositoryMock.Setup(x => x.GetUserByEmailAsync(userEmail)).ReturnsAsync(user);
            _userRepositoryMock.Invocations.Clear();
            _emailServiceMock.Invocations.Clear();

            // Act
            var result = await _userService.GetUserInfo(null, userEmail);

            // Assert
            Assert.That(result, Is.EqualTo(user));
            _userRepositoryMock.Verify(x => x.GetUserByEmailAsync(userEmail), Times.Once);
            _emailServiceMock.VerifyNoOtherCalls();
        }

        [Test]
        public async Task GetUserInfo_ShouldCreateUser_WhenUserNotFoundAndDomainHasValidOrganisationId()
        {
            // Arrange
            var userEmail = "newuser@test.com";
            var domainInfo = fixture.Create<DomainInfoDto>();

            var userProfile = fixture.Create<UserProfile>();

            _userRepositoryMock.SetupSequence(x => x.GetUserByEmailAsync(userEmail)).ReturnsAsync((UserProfile?)null).ReturnsAsync(userProfile);
            _userRepositoryMock.Setup(x => x.GetOrganisationIdByDomainAsync(It.IsAny<string>())).ReturnsAsync(domainInfo);
            _userRepositoryMock.Setup(x => x.CreateUserAsync(It.IsAny<UserProfile>())).ReturnsAsync(999);
            _userRepositoryMock.Setup(x => x.AddUserToRoleAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(true);
            _emailServiceMock.Setup(X => X.SendWelcomeEmail(It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>()));


            _userRepositoryMock.Invocations.Clear();
            _emailServiceMock.Invocations.Clear();


            // Act
            var result = await _userService.GetUserInfo("New User", userEmail);

            // Assert
            Assert.That(result, Is.Not.Null);
            _userRepositoryMock.Verify(x => x.GetUserByEmailAsync(userEmail), Times.Exactly(2));
            _userRepositoryMock.Verify(x => x.GetOrganisationIdByDomainAsync(It.IsAny<string>()), Times.Once);
            _userRepositoryMock.Verify(x => x.CreateUserAsync(It.IsAny<UserProfile>()), Times.Once);
            _userRepositoryMock.Verify(x => x.AddUserToRoleAsync(999, 5), Times.Once);
            _emailServiceMock.Verify(x => x.SendWelcomeEmail(userEmail, It.IsAny<Dictionary<string, dynamic>>()), Times.Once);
        }
        [Test]
        public async Task GetUserInfo_ShouldReturnNull_WhenDomainIsNull()
        {
            // Arrange
            var userEmail = "newuser@test.com";
            var domainInfo = fixture.Create<DomainInfoDto>();

            var userProfile = fixture.Create<UserProfile>();

            _userRepositoryMock.SetupSequence(x => x.GetUserByEmailAsync(userEmail)).ReturnsAsync((UserProfile?)null).ReturnsAsync(userProfile);
            _userRepositoryMock.Setup(x => x.GetOrganisationIdByDomainAsync(It.IsAny<string>())).ReturnsAsync((DomainInfoDto?)null);
            _userRepositoryMock.Setup(x => x.CreateUserAsync(It.IsAny<UserProfile>())).ReturnsAsync(999);
            _userRepositoryMock.Setup(x => x.AddUserToRoleAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(true);
            _emailServiceMock.Setup(X => X.SendWelcomeEmail(It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>()));


            _userRepositoryMock.Invocations.Clear();
            _emailServiceMock.Invocations.Clear();


            // Act
            var result = await _userService.GetUserInfo("New User", userEmail);

            // Assert
            Assert.That(result, Is.Null);
            _userRepositoryMock.Verify(x => x.GetUserByEmailAsync(userEmail), Times.Exactly(1));
            _userRepositoryMock.Verify(x => x.GetOrganisationIdByDomainAsync(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task GetUserInfo_ShouldFilterVisibleRoles_WhenUserExists()
        {
            // Arrange
            var user = fixture.Create<UserProfile>();

            _userRepositoryMock.Setup(x => x.GetUserByEmailAsync(user.User.UserEmail)).ReturnsAsync(user);

            _userRepositoryMock.Invocations.Clear();


            // Act
            var result = await _userService.GetUserInfo(null, user.User.UserEmail);

            // Assert
            Assert.That(result!.Roles.Count, Is.EqualTo(2));
            Assert.That(result.Roles[0].Visible, Is.True);
            _userRepositoryMock.Verify(x => x.GetUserByEmailAsync(user.User.UserEmail), Times.Once);
        }
        [Test]
        public async Task GetUserInfo_ShouldLogException_WhenSendEmailFails()
        {
            // Arrange
            var userEmail = "newuser@test.com";
            var domainInfo = fixture.Create<DomainInfoDto>();

            var userProfile = fixture.Create<UserProfile>();

            _userRepositoryMock.SetupSequence(x => x.GetUserByEmailAsync(userEmail)).ReturnsAsync((UserProfile?)null).ReturnsAsync(userProfile);
            _userRepositoryMock.Setup(x => x.GetOrganisationIdByDomainAsync(It.IsAny<string>())).ReturnsAsync(domainInfo);
            _userRepositoryMock.Setup(x => x.CreateUserAsync(It.IsAny<UserProfile>())).ReturnsAsync(999);
            _userRepositoryMock.Setup(x => x.AddUserToRoleAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(true);
            _emailServiceMock.Setup(X => X.SendWelcomeEmail(It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>()))
                .Throws(new Exception("Email sending failed")); 


            _userRepositoryMock.Invocations.Clear();
            _emailServiceMock.Invocations.Clear();


            // Act
            var result = await _userService.GetUserInfo("New User", userEmail);

            // Assert
            Assert.That(result, Is.Not.Null);
            _userRepositoryMock.Verify(x => x.GetUserByEmailAsync(userEmail), Times.Exactly(2));
            _userRepositoryMock.Verify(x => x.GetOrganisationIdByDomainAsync(It.IsAny<string>()), Times.Once);
            _userRepositoryMock.Verify(x => x.CreateUserAsync(It.IsAny<UserProfile>()), Times.Once);
            _userRepositoryMock.Verify(x => x.AddUserToRoleAsync(999, 5), Times.Once);
            _emailServiceMock.Verify(x => x.SendWelcomeEmail(userEmail, It.IsAny<Dictionary<string, dynamic>>()), Times.Once);
        }
        [Test]
        public async Task GetUserInfo_ShouldAddRoles_WhenEmailIsDeclan()
        {
            // Arrange
            var userEmail = "declan.kavanagh@digital.cabinet-office.gov.uk";
            var domainInfo = fixture.Create<DomainInfoDto>();

            var userProfile = fixture.Create<UserProfile>();

            _userRepositoryMock.SetupSequence(x => x.GetUserByEmailAsync(userEmail)).ReturnsAsync((UserProfile?)null).ReturnsAsync(userProfile);
            _userRepositoryMock.Setup(x => x.GetOrganisationIdByDomainAsync(It.IsAny<string>())).ReturnsAsync(domainInfo);
            _userRepositoryMock.Setup(x => x.CreateUserAsync(It.IsAny<UserProfile>())).ReturnsAsync(999);
            _userRepositoryMock.Setup(x => x.AddUserToRoleAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(true);
            _emailServiceMock.Setup(X => X.SendWelcomeEmail(It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>()))
                .Throws(new Exception("Email sending failed"));


            _userRepositoryMock.Invocations.Clear();
            _emailServiceMock.Invocations.Clear();


            // Act
            var result = await _userService.GetUserInfo("New User", userEmail);

            // Assert
            Assert.That(result, Is.Not.Null);
            _userRepositoryMock.Verify(x => x.GetUserByEmailAsync(userEmail), Times.Exactly(2));
            _userRepositoryMock.Verify(x => x.GetOrganisationIdByDomainAsync(It.IsAny<string>()), Times.Once);
            _userRepositoryMock.Verify(x => x.CreateUserAsync(It.IsAny<UserProfile>()), Times.Once);
            _userRepositoryMock.Verify(x => x.AddUserToRoleAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(3));
            _emailServiceMock.Verify(x => x.SendWelcomeEmail(userEmail, It.IsAny<Dictionary<string, dynamic>>()), Times.Once);
        }
        [Test]
        public async Task SignInOrUpdateUser_ShouldReturnExistingUser_WhenUserExists()
        {
            // Arrange
            var userEmail = "existing.user@example.com";
            var user = fixture.Create<UserProfile>();
            var domain = fixture.Create<DomainInfoDto>();

            _userRepositoryMock.Setup(x => x.GetUserByEmailAsync(userEmail)).ReturnsAsync(user);

            _userRepositoryMock.Invocations.Clear();
            _emailServiceMock.Invocations.Clear();

            // Act
            var result = await _userService.SignInOrUpdateUser(userEmail, "Existing User", domain);

            // Assert
            Assert.That(result, Is.EqualTo(user));
            _userRepositoryMock.Verify(x => x.GetUserByEmailAsync(userEmail), Times.Once);
            _emailServiceMock.VerifyNoOtherCalls();
        }

        [Test]
        public async Task SignInOrUpdateUser_ShouldCreateUserAndAssignRoles_WhenUserDoesNotExist()
        {
            // Arrange
            var userEmail = "new.user@validdomain.com";
            var domainInfo = fixture.Create<DomainInfoDto>();
            var userProfile = fixture.Create<UserProfile>();

            _userRepositoryMock.SetupSequence(x => x.GetUserByEmailAsync(userEmail)).ReturnsAsync((UserProfile?)null).ReturnsAsync(userProfile);
            _userRepositoryMock.Setup(x => x.CreateUserAsync(It.IsAny<UserProfile>())).ReturnsAsync(999);
            _userRepositoryMock.Setup(x => x.AddUserToRoleAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(true);


            _userRepositoryMock.Invocations.Clear();
            _emailServiceMock.Invocations.Clear();

            // Act
            var result = await _userService.SignInOrUpdateUser(userEmail, "New User", domainInfo);

            // Assert
            Assert.That(result, Is.Not.Null);
            _userRepositoryMock.Verify(x => x.GetUserByEmailAsync(userEmail), Times.Exactly(2));
            _userRepositoryMock.Verify(x => x.CreateUserAsync(It.IsAny<UserProfile>()), Times.Once);
            _userRepositoryMock.Verify(x => x.AddUserToRoleAsync(999, 5), Times.Once);
            _emailServiceMock.Verify(x => x.SendWelcomeEmail(userEmail, It.IsAny<Dictionary<string, dynamic>>()), Times.Once);
        }

        [Test]
        public async Task SignInOrUpdateUser_ShouldHandleEmailSendingFailure_WhenUserDoesNotExist()
        {
            // Arrange
            var userEmail = "new.user@validdomain.com";
            var domainInfo = fixture.Create<DomainInfoDto>();
            var userProfile = fixture.Create<UserProfile>();


            _userRepositoryMock.SetupSequence(x => x.GetUserByEmailAsync(userEmail)).ReturnsAsync((UserProfile?)null).ReturnsAsync(userProfile);
            _userRepositoryMock.Setup(x => x.CreateUserAsync(It.IsAny<UserProfile>())).ReturnsAsync(999);
            _userRepositoryMock.Setup(x => x.AddUserToRoleAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(true);

            _userRepositoryMock.Invocations.Clear();
            _emailServiceMock.Invocations.Clear();

            // Simulate email sending failure
            _emailServiceMock.Setup(x => x.SendWelcomeEmail(It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>()))
                .Throws(new Exception("Failed to send email"));

            // Act
            var result = await _userService.SignInOrUpdateUser(userEmail, "New User", domainInfo);

            // Assert
            Assert.That(result, Is.Not.Null);
            _userRepositoryMock.Verify(x => x.GetUserByEmailAsync(userEmail), Times.Exactly(2));
            _userRepositoryMock.Verify(x => x.CreateUserAsync(It.IsAny<UserProfile>()), Times.Once);
            _userRepositoryMock.Verify(x => x.AddUserToRoleAsync(999, 5), Times.Once);
            _emailServiceMock.Verify(x => x.SendWelcomeEmail(userEmail, It.IsAny<Dictionary<string, dynamic>>()), Times.Once);
        }

        [Test]
        public async Task SignInOrUpdateUser_ShouldAssignAdminRoles_WhenAdminUserIsCreated()
        {
            // Arrange
            var userEmail = "declan.kavanagh@digital.cabinet-office.gov.uk";
            var domainInfo = fixture.Create<DomainInfoDto>();
            var userProfile = fixture.Create<UserProfile>();


            _userRepositoryMock.SetupSequence(x => x.GetUserByEmailAsync(userEmail)).ReturnsAsync((UserProfile?)null).ReturnsAsync(userProfile);
            _userRepositoryMock.Setup(x => x.CreateUserAsync(It.IsAny<UserProfile>())).ReturnsAsync(999);
            _userRepositoryMock.Setup(x => x.AddUserToRoleAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(true);

            _userRepositoryMock.Invocations.Clear();
            _emailServiceMock.Invocations.Clear();

            // Act
            var result = await _userService.SignInOrUpdateUser(userEmail, "Declan Kavanagh", domainInfo);

            // Assert
            Assert.That(result, Is.Not.Null);
            _userRepositoryMock.Verify(x => x.GetUserByEmailAsync(userEmail), Times.Exactly(2));
            _userRepositoryMock.Verify(x => x.CreateUserAsync(It.IsAny<UserProfile>()), Times.Once);
            _userRepositoryMock.Verify(x => x.AddUserToRoleAsync(999, 5), Times.Once);
            _userRepositoryMock.Verify(x => x.AddUserToRoleAsync(999, 6), Times.Once); // Special admin roles
            _userRepositoryMock.Verify(x => x.AddUserToRoleAsync(999, 2), Times.Once);
            _userRepositoryMock.Verify(x => x.AddUserToRoleAsync(999, 1), Times.Once);
        }
        [Test]
        public async Task GetFilteredUsersAsync_ShouldMapValidSortBy_WhenSortByIsValid()
        {
            // Arrange
            var queryParams = fixture.Build<UserQueryParameters>()
                .With(x => x.SortBy, "lastlogin") // Valid column name
                .Create();

            var expectedResponse = fixture.Create<UserResponseDto>();
            _userRepositoryMock.Setup(x => x.GetFilteredUsers(queryParams)).ReturnsAsync(expectedResponse);

            // Act
            var result = await _userService.GetFilteredUsersAsync(queryParams);

            // Assert
            Assert.That(result, Is.EqualTo(expectedResponse));
            Assert.That(queryParams.SortBy, Is.EqualTo("u.LastLogin"));
        }

        [Test]
        public async Task GetFilteredUsersAsync_ShouldUseDefaultSortOrder_WhenSortOrderIsInvalid()
        {
            // Arrange
            var queryParams = fixture.Build<UserQueryParameters>()
                .With(x => x.SortOrder, "invalidOrder") // Invalid order
                .Create();

            var expectedResponse = fixture.Create<UserResponseDto>();
            _userRepositoryMock.Setup(x => x.GetFilteredUsers(queryParams)).ReturnsAsync(expectedResponse);

            // Act
            var result = await _userService.GetFilteredUsersAsync(queryParams);

            // Assert
            Assert.That(result, Is.EqualTo(expectedResponse));
            Assert.That(queryParams.SortOrder, Is.EqualTo("ASC"));
        }

        [Test]
        public async Task GetFilteredUsersAsync_ShouldUseDescSortOrder_WhenSortOrderIsDesc()
        {
            // Arrange
            var queryParams = fixture.Build<UserQueryParameters>()
                .With(x => x.SortOrder, "desc") // Valid order
                .Create();

            var expectedResponse = fixture.Create<UserResponseDto>();
            _userRepositoryMock.Setup(x => x.GetFilteredUsers(queryParams)).ReturnsAsync(expectedResponse);

            // Act
            var result = await _userService.GetFilteredUsersAsync(queryParams);

            // Assert
            Assert.That(result, Is.EqualTo(expectedResponse));
            Assert.That(queryParams.SortOrder, Is.EqualTo("DESC"));
        }

        [Test]
        public async Task GetFilteredUsersAsync_ShouldUseDefaultSortByAndSortOrder_WhenNotProvided()
        {
            // Arrange
            var queryParams = fixture.Build<UserQueryParameters>()
                .With(x => x.SortBy, null as string) // No SortBy provided
                .With(x => x.SortOrder, null as string) // No SortOrder provided
                .Create();

            var expectedResponse = fixture.Create<UserResponseDto>();
            _userRepositoryMock.Setup(x => x.GetFilteredUsers(queryParams)).ReturnsAsync(expectedResponse);

            // Act
            var result = await _userService.GetFilteredUsersAsync(queryParams);

            // Assert
            Assert.That(result, Is.EqualTo(expectedResponse));
            Assert.That(queryParams.SortBy, Is.EqualTo("u.UserID"));
            Assert.That(queryParams.SortOrder, Is.EqualTo("ASC"));
        }

        [Test]
        public async Task GetFilteredUsersAsync_ShouldReturnCorrectResponse_WhenValidParamsArePassed()
        {
            // Arrange
            var queryParams = fixture.Build<UserQueryParameters>()
                .With(x => x.SortBy, "UserName") // Valid column name
                .With(x => x.SortOrder, "asc") // Valid sort order
                .Create();

            var expectedResponse = fixture.Create<UserResponseDto>();
            _userRepositoryMock.Setup(x => x.GetFilteredUsers(queryParams)).ReturnsAsync(expectedResponse);

            // Act
            var result = await _userService.GetFilteredUsersAsync(queryParams);

            // Assert
            Assert.That(result, Is.EqualTo(expectedResponse));
            Assert.That(queryParams.SortBy, Is.EqualTo("u.UserName"));
            Assert.That(queryParams.SortOrder, Is.EqualTo("ASC"));
        }
    }
}