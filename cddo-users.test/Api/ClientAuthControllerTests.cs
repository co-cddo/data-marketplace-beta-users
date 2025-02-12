using AutoFixture;
using AutoFixture.AutoMoq;
using cddo_users.models;
using cddo_users.test.TestHelpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace cddo_users.test.Api
{
    [TestFixture]
    public class ClientAuthControllerTests
    {
        protected readonly IFixture fixture;

        public ClientAuthControllerTests() 
        {
            fixture = new Fixture().Customize(new AutoMoqCustomization());
        }

        [Test]
        public async Task GetAllCredentials_WhenEmailIsEmpty_BadRequest()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim(ClaimTypes.Email, "");

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.ClientAuthController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var result = await testItems.ClientAuthController.GetAllCredentials();

            // Assert
            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());

        }

        [Test]
        public async Task GetAllCredentials_WhenNoResultsFound_BadRequest()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim(ClaimTypes.Email, "test@email.com");

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.ClientAuthController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var result = await testItems.ClientAuthController.GetAllCredentials();

            // Assert
            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());

        }

        [Test]
        public async Task GetAllCredentials_WhenNoCredentialsFound_NotFound()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim(ClaimTypes.Email, "test@email.com");

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.ClientAuthController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            testItems.MockClientAuthService.Setup(a => a.GetCredentialAsync(It.IsAny<string>())).ReturnsAsync(new ClientCredentials());

            var result = await testItems.ClientAuthController.GetAllCredentials();

            // Assert
            Assert.That(result, Is.TypeOf<NotFoundObjectResult>());

        }

        [Test]
        public async Task GetAllCredentials_WithCorrectCredentials_ListOfClientCreds()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim(ClaimTypes.Email, "test@email.com");

            var clientCreds = fixture.Create<ClientCredentials>();


            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.ClientAuthController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            testItems.MockClientAuthService.Setup(a => a.GetCredentialAsync(It.IsAny<string>())).ReturnsAsync(clientCreds);

            var result = (OkObjectResult)await testItems.ClientAuthController.GetAllCredentials();

            // Assert
            result.Value.Should().NotBeNull();
            result.Value.Should().BeEquivalentTo(clientCreds.ClientCredentialsList);
        }

        [Test]
        public async Task GetCredentialDetails_WhenNoCredentialsFound_NotFound()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            //Act

            var result = await testItems.ClientAuthController.GetCredentialDetails(It.IsAny<int>());

            // Assert
            Assert.That(result, Is.TypeOf<NotFoundObjectResult>());

        }

        [Test]
        public async Task GetCredentialDetails_WhenGettingById_CredentialDetails()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            var clientCreds = fixture.Create<ClientCredential>();

            //Act
            testItems.MockClientAuthService.Setup(a => a.GetClientCredentialsByIdAsync(It.IsAny<int>())).ReturnsAsync(clientCreds);
            var result = (OkObjectResult)await testItems.ClientAuthController.GetCredentialDetails(It.IsAny<int>());

            // Assert
            result.Value.Should().NotBeNull();
        }

        [Test]
        public async Task UpdateCredentialDetails_WhenThereIsError_NotFound()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            //Act
            testItems.MockClientAuthService.Setup(a => a.UpdateCredentialDetailsAsync(It.IsAny<int>(), It.IsAny<ClientCredential>())).ReturnsAsync(0);
            var result = await testItems.ClientAuthController.UpdateCredentialDetails(It.IsAny<int>(), It.IsAny<ClientCredential>());

            // Assert
            Assert.That(result, Is.TypeOf<NotFoundObjectResult>());

        }

        [Test]
        public async Task UpdateCredentialDetails_WhenThereIsAValidRecord_NoContent()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            //Act
            testItems.MockClientAuthService.Setup(a => a.UpdateCredentialDetailsAsync(It.IsAny<int>(), It.IsAny<ClientCredential>())).ReturnsAsync(1);
            var result = await testItems.ClientAuthController.UpdateCredentialDetails(It.IsAny<int>(), It.IsAny<ClientCredential>());

            // Assert
            Assert.That(result, Is.TypeOf<NoContentResult>());
        }

        [Test]
        public async Task RevokeCredentialDetails_WhenThereIsError_NotFound()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            //Act
            testItems.MockClientAuthService.Setup(a => a.RevokeCredentialAsync(It.IsAny<int>())).ReturnsAsync(0);
            var result = await testItems.ClientAuthController.RevokeCredential(It.IsAny<int>());

            // Assert
            Assert.That(result, Is.TypeOf<NotFoundObjectResult>());

        }

        [Test]
        public async Task RevokeCredentialDetails_WhenRecordExists_NoContent()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            //Act
            testItems.MockClientAuthService.Setup(a => a.RevokeCredentialAsync(It.IsAny<int>())).ReturnsAsync(1);

            var result = await testItems.ClientAuthController.RevokeCredential(It.IsAny<int>());

            // Assert
            Assert.That(result, Is.TypeOf<NoContentResult>());
        }

        [Test]
        public async Task GenerateClientCredentials_WithoutUserEmail_BadRequest()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim(ClaimTypes.Email, "");
            var clientRequest = fixture.Create<ClientRequest>();

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.ClientAuthController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var result = await testItems.ClientAuthController.GenerateClientCredentials(clientRequest);

            // Assert
            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task GenerateClientCredentials_WithoutClientDetails_BadRequest()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim(ClaimTypes.Email, "test@email.com");
            var clientRequest = fixture.Create<ClientRequest>();
            var configurationSection = new Mock<IConfigurationSection>();

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.ClientAuthController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            configurationSection.Setup(x => x.Value).Returns("scope1 scope2 scope3");
            testItems.MockConfiguration.Setup(x => x.GetSection("Authentication:AllowedScopes")).Returns(configurationSection.Object);

            //Mock the client service
            testItems.MockClientAuthService.Setup(c => c.GenerateClientAsync(It.IsAny<ClientRequest>(), It.IsAny<string>(), configurationSection.Object)).ReturnsAsync(new ClientCredential() { Message = "No results found"});

            var result = await testItems.ClientAuthController.GenerateClientCredentials(clientRequest);

            // Assert
            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task GenerateClientCredentials_ForExistingClient_ClientDetails()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim(ClaimTypes.Email, "test@email.com");
            var clientRequest = fixture.Create<ClientRequest>();
            var configurationSection = new Mock<IConfigurationSection>();
            var clientDetailsResults = fixture.Create<ClientCredential>();

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.ClientAuthController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            configurationSection.Setup(x => x.Value).Returns("scope1 scope2 scope3");
            testItems.MockConfiguration.Setup(x => x.GetSection("Authentication:AllowedScopes")).Returns(configurationSection.Object);

            //Mock the client service and set Message to null
            clientDetailsResults.Message = null;
            testItems.MockClientAuthService.Setup(c => c.GenerateClientAsync(It.IsAny<ClientRequest>(), It.IsAny<string>(), configurationSection.Object)).ReturnsAsync(clientDetailsResults);

            var result = (OkObjectResult)await testItems.ClientAuthController.GenerateClientCredentials(clientRequest);

            // Assert
            result.Should().NotBeNull();
            dynamic obj = result!.Value!;
            Assert.That(obj.ClientId, Is.EquivalentTo(clientDetailsResults.ClientId));
            Assert.That(obj.ClientSecret, Is.EquivalentTo(clientDetailsResults.ClientSecret));
            Assert.That(obj.Scopes, Is.EquivalentTo(clientDetailsResults.Scopes));
            Assert.That(obj.environment, Is.EquivalentTo(clientRequest.Environment));
        }

        [Test]
        public async Task GetToken_WithInvalidDetails_NoContent()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            //Act
            testItems.MockConfiguration.Setup(config => config["Authentication:ApiKey"]).Returns("my-api-key");
            testItems.MockConfiguration.Setup(config => config["Authentication:ClientId"]).Returns("my-audience");
            testItems.MockConfiguration.Setup(config => config["Authentication:ApiIssuer"]).Returns("my-issuer");

            var result = await testItems.ClientAuthController.GetToken(It.IsAny<TokenRequest>());

            // Assert
            Assert.That(result, Is.TypeOf<UnauthorizedObjectResult>());
        }

        [Test]
        public async Task GetToken_WithValidDetails_TokenAndExpiryIssued()
        {
            //Arrange
            var testItems = TestSetUp.CreateTestItems();
            var token = fixture.Create<string>();
            var expiryDate = fixture.Create<DateTime>();
            //Act
            testItems.MockConfiguration.Setup(config => config["Authentication:ApiKey"]).Returns("my-api-key");
            testItems.MockConfiguration.Setup(config => config["Authentication:ClientId"]).Returns("my-audience");
            testItems.MockConfiguration.Setup(config => config["Authentication:ApiIssuer"]).Returns("my-issuer");

            testItems.MockClientAuthService.Setup(c => c.GetTokenAsync(It.IsAny<TokenRequest>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync((token, expiryDate));

            var result = (OkObjectResult)await testItems.ClientAuthController.GetToken(It.IsAny<TokenRequest>());



            // Assert
            result.Should().NotBeNull();
            dynamic obj = result!.Value!;
            Assert.That(obj.token, Is.EquivalentTo(token));
        }
    }
}
