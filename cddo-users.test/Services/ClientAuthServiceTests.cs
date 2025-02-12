using AutoFixture;
using AutoFixture.AutoMoq;
using cddo_users.DTOs;
using cddo_users.Interface;
using cddo_users.Logic;
using cddo_users.models;
using cddo_users.Repositories;
using cddo_users.test.TestHelpers;
using FluentAssertions;
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
    public class ClientAuthServiceTests
    {
        private readonly ClientAuthService _clientService;
        private readonly Mock<IUserService> _userService = new Mock<IUserService>();
        private readonly Mock<IClientAuthRepository> _clientAuthRepository = new Mock<IClientAuthRepository>();
        protected readonly IFixture fixture;

        public ClientAuthServiceTests()
        {
            _clientService = new ClientAuthService(_userService.Object, _clientAuthRepository.Object);
            fixture = new Fixture().Customize(new AutoMoqCustomization());
        }

        [Test]
        public async Task GenerateClientAsync_WhenDomainIsNull_ErrorMessage()
        { 
            //Arrange
            var clientRequest = new ClientRequest();
            var requestingUserEmail = "";
            var configurationSection = new Mock<IConfigurationSection>();

            //Act
            var result = await _clientService.GenerateClientAsync(clientRequest, requestingUserEmail, configurationSection.Object);

            //Assert
            result.Should().NotBeNull();
            result.Message.Should().Be("Domain is required to generate API credentials.");
        }

        [Test]
        public async Task GenerateClientAsync_WhenClientNameIsNull_ErrorMessage()
        {
            //Arrange
            var clientRequest = new ClientRequest() 
            { 
                Domain = "email.com"
            };

            var requestingUserEmail = "test@email.com";
            var configurationSection = new Mock<IConfigurationSection>();

            //Act
            var result = await _clientService.GenerateClientAsync(clientRequest, requestingUserEmail, configurationSection.Object);

            //Assert
            result.Should().NotBeNull();
            result.Message.Should().Be("AppName is required to create an API user.");
        }

        [Test]
        public async Task GenerateClientAsync_WhenRequestingInvalidScopes_ErrorMessage()
        {
            //Arrange
            var clientRequest = new ClientRequest()
            {
                Domain = "email.com",
                AppName = "My Test App",
                Scope = "editor, senior, master"
            };

            var requestingUserEmail = "test@email.com";
            var configurationSection = new Mock<IConfigurationSection>();
            configurationSection.Setup(x => x.Value).Returns("scope1,scope2,scope3");

            //Act
            var result = await _clientService.GenerateClientAsync(clientRequest, requestingUserEmail, configurationSection.Object);

            //Assert
            result.Should().NotBeNull();
            result.Message.Should().Be("Invalid scopes. Allowed scopes are: scope1, scope2, scope3");
        }

        [Test]
        public async Task GenerateClientAsync_WhenRequestingInvalidEnvironment_ErrorMessage()
        {
            //Arrange
            var clientRequest = new ClientRequest()
            {
                Domain = "email.com",
                AppName = "My Test App",
                Scope = "scope1, scope2, scope3",
                Environment = "preProd"
            };

            var requestingUserEmail = "test@email.com";
            var configurationSection = new Mock<IConfigurationSection>();
            configurationSection.Setup(x => x.Value).Returns("scope1,scope2,scope3");

            //Act
            var result = await _clientService.GenerateClientAsync(clientRequest, requestingUserEmail, configurationSection.Object);

            //Assert
            result.Should().NotBeNull();
            result.Message.Should().Be("Invalid environment. It must be either 'Test' or 'Production'.");
        }

        [Test]
        public async Task GenerateClientAsync_WhenCurrentUserOrganisationDomainIsInvalid_ErrorMessage()
        {
            //Arrange
            var clientRequest = new ClientRequest()
            {
                Domain = "email.com",
                AppName = "My Test App",
                Scope = "scope1, scope2, scope3",
                Environment = "test"
            };

            var requestingUserEmail = "test@email.com";
            var configurationSection = new Mock<IConfigurationSection>();
            configurationSection.Setup(x => x.Value).Returns("scope1,scope2,scope3");
            //returns null domain info
            _userService.Setup(u => u.GetOrganisationIdByDomainAsync(It.IsAny<string>()));

            //Act
            var result = await _clientService.GenerateClientAsync(clientRequest, requestingUserEmail, configurationSection.Object);

            //Assert
            result.Should().NotBeNull();
            result.Message.Should().Be("Invalid domain.");
        }

        [Test]
        public async Task GenerateClientAsync_ValidParametersAndNewUser_GeneratesClientCredentials()
        {
            //Arrange
            var clientRequest = new ClientRequest()
            {
                Domain = "email.com",
                AppName = "My Test App",
                Scope = "scope1, scope2, scope3",
                Environment = "test"
            };

            var domainInfo = fixture.Create<DomainInfoDto>();
            var existingUser = fixture.Create<UserProfile>();

            var requestingUserEmail = "test@email.com";
            var configurationSection = new Mock<IConfigurationSection>();
            configurationSection.Setup(x => x.Value).Returns("scope1,scope2,scope3");

            var squence = _userService.SetupSequence(u=>u.GetUserByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((UserProfile?) null)
                .ReturnsAsync(existingUser);

            //int callCount = 0;
            //_userService
            //    .Setup(u => u.GetUserByEmailAsync(It.IsAny<string>()))
            //    .ReturnsAsync(() =>
            //    {
            //        callCount++;
            //        return callCount == 1 ? null : existingUser;
            //    });


            //returns null domain info
            _userService.Setup(u => u.GetOrganisationIdByDomainAsync(It.IsAny<string>())).ReturnsAsync(domainInfo);
            
            //Create new user
            _userService.Setup(u => u.CreateUserAsync(It.IsAny<UserProfile>())).ReturnsAsync(1);
            //Assignes a role to new user
            _userService.Setup(u => u.AddUserToRoleAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()));

            _clientAuthRepository.Setup(u => u.CreateClientCredentials(existingUser, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), clientRequest, It.IsAny<string>()));
            //Act
            //Spin up a new instance of system under test
            //var sut = new ClientAuthService(_userService.Object, _clientAuthRepository.Object);
            var result = await _clientService.GenerateClientAsync(clientRequest, requestingUserEmail, configurationSection.Object);

            //Assert
            result.Should().NotBeNull();
            _clientAuthRepository.Verify(u => u.CreateClientCredentials(existingUser, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), clientRequest, It.IsAny<string>()), Times.Once);
            result.Environment.Should().Be(clientRequest.Environment);
        }

        [Test]
        public async Task GenerateClientAsync_ValidParametersExistingUser_GeneratesClientCredentials()
        {
            //Arrange
            var clientRequest = new ClientRequest()
            {
                Domain = "email.com",
                AppName = "My Test App",
                Scope = "scope1, scope2, scope3",
                Environment = "test"
            };

            var domainInfo = fixture.Create<DomainInfoDto>();
            var existingUser = fixture.Create<UserProfile>();

            var requestingUserEmail = "test@email.com";
            var configurationSection = new Mock<IConfigurationSection>();
            configurationSection.Setup(x => x.Value).Returns("scope1,scope2,scope3");
            //returns null domain info
            _userService.Setup(u => u.GetOrganisationIdByDomainAsync(It.IsAny<string>())).ReturnsAsync(domainInfo);
            //existing user is null
            _userService.Setup(u => u.GetUserByEmailAsync(It.IsAny<string>())).ReturnsAsync(existingUser);

            _clientAuthRepository.Setup(u => u.CreateClientCredentials(existingUser, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), clientRequest, It.IsAny<string>()));
            //Act
            var result = await _clientService.GenerateClientAsync(clientRequest, requestingUserEmail, configurationSection.Object);

            //Assert
            result.Should().NotBeNull();
            _clientAuthRepository.Verify(u => u.CreateClientCredentials(existingUser, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), clientRequest, It.IsAny<string>()), Times.Once);
            result.Environment.Should().Be(clientRequest.Environment);
        }

        [Test]
        public async Task GetClientAsync_WhenUserDoesntExists_ErrorMessage()
        {
            //Arrange
            var requestingUserEmail = "test@user.com";
            var configurationSection = new Mock<IConfigurationSection>();

            //Act
            //existing user is null
            _userService.Setup(u => u.GetUserByEmailAsync(requestingUserEmail));

            var result = await _clientService.GetCredentialAsync(requestingUserEmail);

            //Assert
            result.Should().NotBeNull();
            result.Message.Should().Be("User not found.");
        }

        [Test]
        public async Task GetClientAsync_WhenCredentialsDoesntExists_ErrorMessage()
        {
            //Arrange
            var requestingUserEmail = "test@user.com";
            var userProfile = fixture.Create<UserProfile>();

            //Act
            //existing user is null
            _userService.Setup(u => u.GetUserByEmailAsync(requestingUserEmail)).ReturnsAsync(userProfile);
            _clientAuthRepository.Setup(u => u.GetCredentials(It.IsAny<int>())).ReturnsAsync((IEnumerable<ClientCredential>?)null);

            var result = await _clientService.GetCredentialAsync(requestingUserEmail);

            //Assert
            result.Should().NotBeNull();
            result.Message.Should().Be("Client credentials not found.");
        }

        [Test]
        public async Task GetClientAsync_WhenUserAndCredentailsExist_ClientCredentialsForUser()
        {
            //Arrange
            var requestingUserEmail = "test@user.com";
            var userProfile = fixture.Create<UserProfile>();
            var clientCredentials = fixture.Create<IEnumerable<ClientCredential>?>();

            //Act
            //existing user is null
            _userService.Setup(u => u.GetUserByEmailAsync(requestingUserEmail)).ReturnsAsync(userProfile);

            _clientAuthRepository.Setup(u => u.GetCredentials(It.IsAny<int>())).ReturnsAsync(clientCredentials);

            var result = await _clientService.GetCredentialAsync(requestingUserEmail);

            //Assert
            result.Should().NotBeNull();
            result.ClientCredentialsList.Should().BeEquivalentTo(clientCredentials);
        }

        [Test]
        public async Task GetClientCredentialsByIdAsync_WhenUserAndCredentailsExist_ClientCredentialsForUser()
        {
            //Arrange
            var credentialsId = 5;
            var userProfile = fixture.Create<UserProfile>();
            var clientCredentials = fixture.Create<ClientCredential?>();

            //Act
            //existing user is null
            _clientAuthRepository.Setup(u => u.GetClientCredentialsById(credentialsId)).ReturnsAsync(clientCredentials);

            var result = await _clientService.GetClientCredentialsByIdAsync(credentialsId);

            //Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(clientCredentials);
        }

        [Test]
        public async Task UpdateCredentialDetailsAsync_WhenUserAndCredentailsExist_UpdatedClientCredentialsForUser()
        {
            //Arrange
            var credentialsId = 5;
            var userProfile = fixture.Create<UserProfile>();
            var clientCredentials = fixture.Create<ClientCredential>();

            //Act
            //existing user is null
            _clientAuthRepository.Setup(u => u.UpdateCredentialDetails(credentialsId, clientCredentials)).ReturnsAsync(credentialsId);

            var result = await _clientService.UpdateCredentialDetailsAsync(credentialsId, clientCredentials);

            //Assert
            result.Should().Be(credentialsId);
        }

        [Test]
        public async Task RevokeCredentialDetailsAsync_WhenUserAndCredentailsExist_RevokedClientCredentialsForUser()
        {
            //Arrange
            var credentialsId = 5;
            var userProfile = fixture.Create<UserProfile>();
            var clientCredentials = fixture.Create<ClientCredential>();

            //Act
            //existing user is null
            _clientAuthRepository.Setup(u => u.RevokeCredential(credentialsId)).ReturnsAsync(credentialsId);

            var result = await _clientService.RevokeCredentialAsync(credentialsId);

            //Assert
            result.Should().Be(credentialsId);
        }

        [Test]
        public async Task GetUserToken_WithNoCredentailsInTheDatabase_ReturnNull()
        {
            //Arrange
            var request = fixture.Create<TokenRequest>();
            string? issuer = "cddo";
            string? audience = "cabinet";
            string? appKey = "cddoappkey";

            //Act
            _clientAuthRepository.Setup(u => u.GetToken(request));

            var result = await _clientService.GetTokenAsync(request, issuer, audience, appKey);

            //Assert
            result.Should().Be((null, null));
        }

        [Test]
        public async Task GetUserToken_WithValidRequestAndPresentToken_GenerateToken()
        {
            //Arrange
            var request = fixture.Create<TokenRequest>();
            request.ClientId = "ClientId";
            request.ClientSecret = "my-secret";
            string? issuer = "cddo";
            string? audience = "cabinet";
            string? appKey = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
            var clientCredentials = fixture.Create<ClientCredential>();

            //Act
            _clientAuthRepository.Setup(u => u.GetToken(request)).ReturnsAsync(clientCredentials);

            var result = await _clientService.GetTokenAsync(request, issuer, audience, appKey);

            //Assert
            result.Should().NotBeNull();
        }
    }
}
