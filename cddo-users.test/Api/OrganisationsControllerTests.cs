using AutoFixture;
using AutoFixture.AutoMoq;
using cddo_users.Api;
using cddo_users.DTOs;
using cddo_users.Interface;
using cddo_users.Logic;
using cddo_users.models;
using cddo_users.Repositories;
using cddo_users.test.TestHelpers;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System.Collections.Generic;
using System.Security.Claims;

namespace cddo_users.test.Api
{
    [TestFixture]
    public class OrganisationsControllerTests
    {
        protected readonly IFixture fixture;

        public OrganisationsControllerTests()
        { 
            fixture = new Fixture().Customize(new AutoMoqCustomization());
        }

        #region SetDataShareRequestMailboxAddress() Tests
        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase("  ")]
        [TestCase("some dsr mailbox address")]
        public async Task GivenAnyDomainAndDataShareRequestMailboxAddress_WhenISetDataShareRequestMailboxAddress_ThenTheAddressIsSetForTheGivenDomain(
            string? testDataShareRequestMailboxAddress)
        {
            var testItems = TestSetUp.CreateTestItems();

            const int testDomainId = 123;

            await testItems.OrganisationsController.SetDataShareRequestMailboxAddress(
                testDomainId,
                testDataShareRequestMailboxAddress);

            Assert.Multiple(() =>
            {
                testItems.MockOrganisationService.Verify(x => x.SetDataShareRequestMailboxAddressAsync(
                        testDomainId,
                        testDataShareRequestMailboxAddress!),
                    Times.Once);

                testItems.MockOrganisationRepository.VerifyNoOtherCalls();
            });
        }

        [Test]
        public async Task GivenAnEmptyDataShareRequestMailboxAddress_WhenISetDataShareRequestMailboxAddress_ThenNoNotificationEmailIsSent(
            [Values(null, "", "  ")] string? testDataShareRequestMailboxAddress)
        {
            var testItems = TestSetUp.CreateTestItems();

            testItems.MockEmailManager.Invocations.Clear();

            await testItems.OrganisationsController.SetDataShareRequestMailboxAddress(
                It.IsAny<int>(),
                testDataShareRequestMailboxAddress);

            testItems.MockEmailManager.VerifyNoOtherCalls();
        }

        [Test]
        public async Task GivenANonEmptyDataShareRequestMailboxAddress_WhenISetDataShareRequestMailboxAddress_ThenAnNotificationEmailIsSentToTheGivenAddress()
        {
            var testItems = TestSetUp.CreateTestItems();

            testItems.MockEmailManager.Invocations.Clear();

            const string testDataShareRequestMailboxAddress = "some dsr mailbox address";

            await testItems.OrganisationsController.SetDataShareRequestMailboxAddress(
                It.IsAny<int>(),
                testDataShareRequestMailboxAddress);

            Assert.Multiple(() =>
            {
                testItems.MockOrganisationService.Verify(x => x.SendEmailAsync(It.IsAny<DomainDetail>(), It.IsAny<int>(),
                        testDataShareRequestMailboxAddress),
                    Times.Once);

                testItems.MockEmailManager.VerifyNoOtherCalls();
            });
        }

        [Test]
        public async Task GivenANonEmptyDataShareRequestMailboxAddress_WhenISetDataShareRequestMailboxAddress_ThenAnNotificationEmailIsSentWithTheUserNameOfTheInitiatingUserInTheProperties()
        {
            var testItems = TestSetUp.CreateTestItems();

            testItems.MockEmailManager.Invocations.Clear();

            const string testInitiatingUserName = "some user name";
            testItems.MockUserInformationPresenter.Setup(x => x.GetUserNameOfInitiatingUser())
                .Returns(() => testInitiatingUserName);

            await testItems.OrganisationsController.SetDataShareRequestMailboxAddress(
                It.IsAny<int>(),
                "some dsr mailbox address");

            Assert.Multiple(() =>
            {
                testItems.MockOrganisationService.Verify(x => x.SendEmailAsync(It.IsAny<DomainDetail>(), It.IsAny<int>(),
                        "some dsr mailbox address"),
                    Times.Once);

                testItems.MockEmailManager.VerifyNoOtherCalls();
            });
        }

        [Test]
        public async Task GivenTheUserNameOfTheInitiatingUserCannotBeDetermined_WhenISetDataShareRequestMailboxAddress_ThenAnNotificationEmailIsSentWithAnEmptyUserNameInTheProperties()
        {
            var testItems = TestSetUp.CreateTestItems();

            testItems.MockEmailManager.Invocations.Clear();

            testItems.MockUserInformationPresenter.Setup(x => x.GetUserNameOfInitiatingUser())
                .Returns(() => null);

            await testItems.OrganisationsController.SetDataShareRequestMailboxAddress(
                It.IsAny<int>(),
                "some dsr mailbox address");

            Assert.Multiple(() =>
            {
                testItems.MockOrganisationService.Verify(x => x.SendEmailAsync(It.IsAny<DomainDetail>(), It.IsAny<int>(),
                        "some dsr mailbox address"),
                    Times.Once);

                testItems.MockEmailManager.VerifyNoOtherCalls();
            });
        }

        [Test]
        public async Task GivenANonEmptyDataShareRequestMailboxAddress_WhenISetDataShareRequestMailboxAddress_ThenAnNotificationEmailIsSentWithTheNameOfTheUpdatedDomainInTheProperties()
        {
            var testItems = TestSetUp.CreateTestItems();

            const int testDomainId = 123;
            const string testDomainName = "test domain name";
            const string testDataShareRequestMailboxAddress = "some dsr mailbox address";

            testItems.MockEmailManager.Invocations.Clear();

            testItems.MockOrganisationRepository.Setup(x => x.SetDataShareRequestMailboxAddressAsync(
                    testDomainId,
                    testDataShareRequestMailboxAddress))
                .ReturnsAsync(() => new DomainDetail
                {
                    DomainName = testDomainName
                });

            await testItems.OrganisationsController.SetDataShareRequestMailboxAddress(
                testDomainId,
                testDataShareRequestMailboxAddress);

            Assert.Multiple(() =>
            {
                testItems.MockOrganisationService.Verify(x => x.SendEmailAsync(It.IsAny<DomainDetail>(), It.IsAny<int>(),
                        "some dsr mailbox address"),
                    Times.Once);

                testItems.MockEmailManager.VerifyNoOtherCalls();
            });
        }

        [Test]
        public async Task GivenTheDomainNameOfTheAffectedDomainCannotBeDetermined_WhenISetDataShareRequestMailboxAddress_ThenAnNotificationEmailIsSentWithAnEmptyDomainNameInTheProperties()
        {
            var testItems = TestSetUp.CreateTestItems();

            testItems.MockEmailManager.Invocations.Clear();

            testItems.MockOrganisationRepository.Setup(x => x.SetDataShareRequestMailboxAddressAsync(
                    It.IsAny<int>(),
                    It.IsAny<string>()))
                .ReturnsAsync(() => new DomainDetail
                {
                    DomainName = null!
                });

            await testItems.OrganisationsController.SetDataShareRequestMailboxAddress(
                It.IsAny<int>(),
                "some dsr mailbox address");

            Assert.Multiple(() =>
            {
                testItems.MockOrganisationService.Verify(x => x.SendEmailAsync(It.IsAny<DomainDetail>(), It.IsAny<int>(),
                        "some dsr mailbox address"),
                    Times.Once);

                testItems.MockEmailManager.VerifyNoOtherCalls();
            });
        }

        [Test]
        public async Task GivenANonEmptyDataShareRequestMailboxAddress_WhenISetDataShareRequestMailboxAddress_ThenTheDomainNameInTheNotificationEmailHasZeroWidthSpacesIncludedInTheDomainNameToPreventEmailClientsTreatingItAsAHyperlink()
        {
            var testItems = TestSetUp.CreateTestItems();

            const int testDomainId = 123;
            const string testDomainName = "test domain name";
            const string testDataShareRequestMailboxAddress = "some dsr mailbox address";

            testItems.MockEmailManager.Invocations.Clear();

            testItems.MockOrganisationRepository.Setup(x => x.SetDataShareRequestMailboxAddressAsync(
                    testDomainId,
                    testDataShareRequestMailboxAddress))
                .ReturnsAsync(() => new DomainDetail
                {
                    DomainName = testDomainName
                });

            await testItems.OrganisationsController.SetDataShareRequestMailboxAddress(
                testDomainId,
                testDataShareRequestMailboxAddress);

            Assert.Multiple(() =>
            {
                testItems.MockOrganisationService.Verify(x => x.SendEmailAsync(It.IsAny<DomainDetail>(), It.IsAny<int>(),
                        "some dsr mailbox address"),
                    Times.Once);

                testItems.MockEmailManager.VerifyNoOtherCalls();
            });
        }

        #endregion

        #region tests

        [Test]
        public async Task CreateOrganisation_InvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();
            testItems.OrganisationsController.ModelState.AddModelError("Name", "Name is required");

            // Act
            var result = await testItems.OrganisationsController.CreateOrganisation(new OrganisationDetail() { OrganisationId = 0 });

            // Assert
            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());

        }

        [Test]
        public async Task CreateOrganisation_ServiceReturnsIdZero_ReturnsBadRequest()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();
            
            var organisationDetail = fixture.Create<OrganisationDetail>();
            testItems.MockOrganisationService
                .Setup(service => service.CreateOrganisation(organisationDetail))
                .ReturnsAsync(new OrganisationControllerResponseDto { Id = 0, Message = "Test" });

            // Act
            var result = await testItems.OrganisationsController.CreateOrganisation(organisationDetail);

            // Assert
            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());

        }

        [Test]
        public async Task CreateOrganisation_ServiceReturnsId1_ReturnsId()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();
            
            var organisationDetail = fixture.Create<OrganisationDetail>();
            testItems.MockOrganisationService
                .Setup(service => service.CreateOrganisation(organisationDetail))
                .ReturnsAsync(new OrganisationControllerResponseDto { Id = organisationDetail.OrganisationId });

            // Act
            var result = await testItems.OrganisationsController.CreateOrganisation(organisationDetail);

            // Assert
            Assert.That(result, Is.TypeOf<OkObjectResult>());
            var resultAsOkObjectResult = (OkObjectResult)result;
            Assert.That(resultAsOkObjectResult!.Value, Is.EqualTo(organisationDetail.OrganisationId));
        }
        [Test]
        public async Task CreateOrganisation_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            var organisationDetail = fixture.Create<OrganisationDetail>();
            testItems.MockOrganisationService
                .Setup(service => service.CreateOrganisation(organisationDetail))
                .ThrowsAsync(new System.Exception("Test exception"));

            // Act
            var result = await testItems.OrganisationsController.CreateOrganisation(organisationDetail);

            // Assert
            var statusCodeResult = (ObjectResult)result;
            Assert.That(500, Is.EqualTo(statusCodeResult!.StatusCode));
        }

        [Test]
        public async Task UpdateOrganisation_UpdateOrganisationAsync_ReturnsOk()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            var organisationDetail = fixture.Create<OrganisationDetail>();

            testItems.MockOrganisationService
                .Setup(service => service.UpdateOrganisationAsync(organisationDetail))
                .Returns(Task.CompletedTask);

            // Act
            var result = await testItems.OrganisationsController.UpdateOrganisation(organisationDetail);

            // Assert
            Assert.That(result, Is.InstanceOf<OkResult>());
        }

        [Test]
        public async Task UpdateOrganisation_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            var organisationDetail = fixture.Create<OrganisationDetail>();

            testItems.MockOrganisationService
                .Setup(service => service.UpdateOrganisationAsync(organisationDetail))
                .ThrowsAsync(new System.Exception("Test exception"));

            // Act
            var result = await testItems.OrganisationsController.UpdateOrganisation(organisationDetail);

            // Assert
            var statusCodeResult = (ObjectResult)result;
            Assert.That(500, Is.EqualTo(statusCodeResult!.StatusCode));
        }
        [Test]
        public async Task GetOrganisationsByPage_ValidFilter_ReturnsOk()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();
            var filter = fixture.Create<OrganisationFilter>();
            var organisationsResponse = fixture.Create<OrganisationsResponse>();

            testItems.MockOrganisationService
                .Setup(service => service.getOrganisationsByPage(filter))
                .ReturnsAsync(organisationsResponse);

            // Act
            var result = await testItems.OrganisationsController.GetOrganisationsByPage(filter);

            // Assert
            var okResult = (OkObjectResult)result;
            Assert.That(200, Is.EqualTo(okResult!.StatusCode));
        }
        [Test]
        public async Task CreateOrganisationRequest_InvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            testItems.OrganisationsController.ModelState.AddModelError("Name", "Required");
            var organisationRequest = fixture.Create<OrganisationRequest>();

            // Act
            var result = await testItems.OrganisationsController.CreateOrganisationRequest(organisationRequest);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task CreateOrganisationRequest_IdIsZero_ReturnsBadRequest()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            var organisationRequest = fixture.Create<OrganisationRequest>();
            var responseDto = fixture.Create<OrganisationControllerResponseDto>();
            responseDto.Id = 0;
            testItems.MockOrganisationService
                .Setup(s => s.createOrganisationRequest(It.IsAny<OrganisationRequest>()))
                .ReturnsAsync(responseDto);

            // Act
            var result = await testItems.OrganisationsController.CreateOrganisationRequest(organisationRequest);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result;
            Assert.That(responseDto.Message, Is.EqualTo(badRequestResult!.Value));
        }
        [Test]
        public async Task CreateOrganisationRequest_ValidRequest_ReturnsOk()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            var organisationRequest = fixture.Create<OrganisationRequest>();
            var responseDto = fixture.Create<OrganisationControllerResponseDto>();

            testItems.MockOrganisationService
                .Setup(service => service.createOrganisationRequest(It.IsAny<OrganisationRequest>()))
                .ReturnsAsync(responseDto);

            // Act
            var result = await testItems.OrganisationsController.CreateOrganisationRequest(organisationRequest);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = (OkObjectResult)result;
            Assert.That(responseDto.Id, Is.EqualTo(okResult!.Value));
        }

        [Test]
        public async Task CreateOrganisationRequest_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            var organisationRequest = fixture.Create<OrganisationRequest>();

            testItems.MockOrganisationService
                .Setup(service => service.createOrganisationRequest(It.IsAny<OrganisationRequest>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await testItems.OrganisationsController.CreateOrganisationRequest(organisationRequest);

            // Assert
            Assert.That(result, Is.InstanceOf<ObjectResult>());
            var objectResult = (ObjectResult)result;
            Assert.That(500, Is.EqualTo(objectResult!.StatusCode));
            Assert.That(objectResult!.Value!.ToString()!.Contains("Test exception"), Is.True);
        }
        [Test]
        public async Task UpdateOrganisationRequest_InvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            testItems.OrganisationsController.ModelState.AddModelError("Name", "Required");
            var updatedRequest = fixture.Create<OrganisationRequest>();
            int id = 1;

            // Act
            var result = await testItems.OrganisationsController.UpdateOrganisationRequest(id, updatedRequest);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task UpdateOrganisationRequest_ValidRequest_ReturnsNoContent()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            int id = 1;
            var updatedRequest = fixture.Create<OrganisationRequest>();
            var responseDto = fixture.Create<OrganisationControllerResponseDto>();

            testItems.MockOrganisationService
                .Setup(service => service.UpdateOrganisationRequest(It.IsAny<int>(), It.IsAny<OrganisationRequest>()))
                .ReturnsAsync(responseDto);


            // Act
            var result = await testItems.OrganisationsController.UpdateOrganisationRequest(id, updatedRequest);

            // Assert
            Assert.That(result, Is.InstanceOf<NoContentResult>());
        }

        [Test]
        public async Task UpdateOrganisationRequest_IdIsZero_ReturnsNotFound()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            int id = 1;
            var updatedRequest = fixture.Create<OrganisationRequest>();
            var responseDto = fixture.Create<OrganisationControllerResponseDto>();
            responseDto.Id = 0;

            testItems.MockOrganisationService
                .Setup(service => service.UpdateOrganisationRequest(It.IsAny<int>(), It.IsAny<OrganisationRequest>()))
                .ReturnsAsync(responseDto);


            // Act
            var result = await testItems.OrganisationsController.UpdateOrganisationRequest(id, updatedRequest);

            // Assert
            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task UpdateOrganisationRequest_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            int id = 1;
            var updatedRequest = fixture.Create<OrganisationRequest>();

            testItems.MockOrganisationService
                .Setup(service => service.UpdateOrganisationRequest(It.IsAny<int>(), It.IsAny<OrganisationRequest>()))
                .ThrowsAsync(new Exception("Test exception"));


            // Act
            var result = await testItems.OrganisationsController.UpdateOrganisationRequest(id, updatedRequest);

            // Assert
            Assert.That(result, Is.InstanceOf<ObjectResult>());
            var objectResult = (ObjectResult)result;
            Assert.That(500, Is.EqualTo(objectResult!.StatusCode));
            Assert.That(objectResult!.Value!.ToString()!.Contains("Test exception"), Is.True);
        }
        [Test]
        public async Task GetAllOrganisationRequests_ValidRequest_ReturnsOkResultWithRequests()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();
            var organisationRequests = fixture.Create<List<OrganisationRequest>>();
            

            testItems.MockOrganisationService
                .Setup(service => service.GetAllOrganisationRequestsAsync())
                .ReturnsAsync(organisationRequests);

            // Act
            var result = await testItems.OrganisationsController.GetAllOrganisationRequests();

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = (OkObjectResult)result;
            Assert.That(organisationRequests, Is.EqualTo(okResult!.Value));
        }

        [Test]
        public async Task GetAllOrganisationRequests_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            testItems.MockOrganisationService
                .Setup(service => service.GetAllOrganisationRequestsAsync())
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await testItems.OrganisationsController.GetAllOrganisationRequests();

            // Assert
            Assert.That(result, Is.InstanceOf<ObjectResult>());
            var objectResult = (ObjectResult)result;
            Assert.That(500, Is.EqualTo(objectResult!.StatusCode));
            Assert.That(objectResult!.Value!.ToString()!.Contains("Test exception"),Is.True);
        }
        [Test]
        public async Task GetOrganisationRequest_ValidId_ReturnsOkResult()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            var organisationRequest = fixture.Create<OrganisationRequest>();
            int id = 1;

            testItems.MockOrganisationService
                .Setup(service => service.GetOrganisationRequestByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(organisationRequest);


            // Act
            var result = await testItems.OrganisationsController.GetOrganisationRequest(id);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = (OkObjectResult)result;
            Assert.That(organisationRequest, Is.EqualTo(okResult.Value));
        }

        [Test]
        public async Task GetOrganisationRequest_OrgNotFound_ReturnsNotFound()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            testItems.MockOrganisationService
                .Setup(service => service.GetOrganisationRequestByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((OrganisationRequest?)null);

            int id = -1; // Non-existent ID

            // Act
            var result = await testItems.OrganisationsController.GetOrganisationRequest(id);

            // Assert
            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task GetOrganisationRequest_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            testItems.MockOrganisationService
                .Setup(service => service.GetOrganisationRequestByIdAsync(It.IsAny<int>()))
                .ThrowsAsync(new Exception("Test exception"));

            int id = 1;

            // Act
            var result = await testItems.OrganisationsController.GetOrganisationRequest(id);

            // Assert
            Assert.That(result, Is.InstanceOf<ObjectResult>());
            var objectResult = (ObjectResult)result;
            Assert.That(500, Is.EqualTo(objectResult!.StatusCode));
            Assert.That(objectResult!.Value!.ToString()!.Contains("Test exception"), Is.True);
        }
        [Test]
        public async Task GetOrganisationTypeSummaries_ReturnsOkResultWithSummaries()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            var mockSummaries = fixture.Create<IEnumerable<OrganisationTypeSummaryDto>>();

            testItems.MockOrganisationService
                .Setup(service => service.GetOrganisationTypeSummariesAsync())
                .ReturnsAsync(mockSummaries);

            // Act
            var result = await testItems.OrganisationsController.GetOrganisationTypeSummaries();

            // Assert
            var okResult = (OkObjectResult)result!.Result!;
            Assert.That(200, Is.EqualTo(okResult!.StatusCode));

            var returnedSummaries = (IEnumerable<OrganisationTypeSummaryDto>)okResult!.Value!;
            Assert.That(mockSummaries, Is.EqualTo(returnedSummaries));
        }        
        [Test]
        public async Task GetDomainsGroupedByType_ReturnsOkResultWithData()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            var mockOrgDomains = fixture.Create<IEnumerable<OrganisationDomainsGrouped>>();

            testItems.MockOrganisationService
                .Setup(service => service.GetDomainsGroupedByTypeAsync())
                .ReturnsAsync(mockOrgDomains);

            // Act
            var result = await testItems.OrganisationsController.GetDomainsGroupedByType();

            // Assert
            var okResult = (OkObjectResult)result;
            Assert.That(200, Is.EqualTo(okResult!.StatusCode));

            var returnedDomains = okResult.Value as IEnumerable<OrganisationDomainsGrouped>;
            Assert.That(mockOrgDomains, Is.EqualTo(returnedDomains));
        }
        [Test]
        public async Task GetDomainsGroupedByFormatAsync_ReturnsOkResultWithData()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            var mockFormatDto = fixture.Create<IEnumerable<GroupedByFormatDto>>();

            testItems.MockOrganisationService
                .Setup(service => service.GetDomainsGroupedByFormatAsync())
                .ReturnsAsync(mockFormatDto);

            // Act
            var result = await testItems.OrganisationsController.GetDomainsGroupedByFormat();

            // Assert
            var okResult = (OkObjectResult)result!.Result!;
            Assert.That(200, Is.EqualTo(okResult!.StatusCode));

            var returnOrgFormat = (IEnumerable<GroupedByFormatDto>)okResult!.Value!;
            Assert.That(mockFormatDto, Is.EqualTo(returnOrgFormat));
        }
        [Test]
        public async Task GetOrganisationWithDomains_ValidId_ReturnsOkResultWithOrganisation()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            var mockOrganisation = fixture.Create<OrganisationDetail>();
            mockOrganisation.OrganisationId = 1;

            testItems.MockOrganisationService
                .Setup(service => service.GetOrganisationDetailByIdAsync((int)mockOrganisation.OrganisationId))
                .ReturnsAsync(mockOrganisation);

            // Act
            var result = await testItems.OrganisationsController.GetOrganisationWithDomains((int)mockOrganisation.OrganisationId);

            // Assert
            var okResult = (OkObjectResult)result!.Result!;
            Assert.That(200, Is.EqualTo(okResult.StatusCode));

            var returnedOrganisation = (OrganisationDetail)okResult.Value!;
            Assert.That(mockOrganisation.OrganisationId, Is.EqualTo(returnedOrganisation.OrganisationId));
        }

        [Test]
        public async Task GetOrganisationWithDomains_InvalidId_ReturnsNotFoundResult()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            int organisationId = 1;

            testItems.MockOrganisationService
                .Setup(service => service.GetOrganisationDetailByIdAsync(organisationId))
                .ReturnsAsync((OrganisationDetail?)null);

            // Act
            var result = await testItems.OrganisationsController.GetOrganisationWithDomains(organisationId);

            // Assert
            var notFoundResult = (NotFoundObjectResult)result!.Result!;
            Assert.That(404, Is.EqualTo(notFoundResult.StatusCode));
        }
        [Test]
        public async Task SearchOrganisationsAndDomains_ReturnsOkResultWithData()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            var mockOrgDetail = fixture.Create<IEnumerable<OrganisationDetail>>();
            var query = "test";

            testItems.MockOrganisationService
                .Setup(service => service.SearchOrganisationsAndDomainsAsync(query))
                .ReturnsAsync(mockOrgDetail);

            // Act
            var result = await testItems.OrganisationsController.SearchOrganisationsAndDomains(query);

            // Assert
            var okResult = (OkObjectResult)result!.Result!;
            Assert.That(200, Is.EqualTo(okResult!.StatusCode));

            var returnOrgDetail = (IEnumerable<OrganisationDetail>)okResult!.Value!;
            Assert.That(mockOrgDetail, Is.EqualTo(returnOrgDetail));
        }
        private void SetUserClaims(TestItems testItems, string email)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Email, email)
            };

            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            testItems.OrganisationsController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        [Test]
        public async Task DeleteOrganisation_UserExists_CallsServicesAndReturnsNoContent()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            var organisationId = 1;
            var userEmail = "test@test.com";
            var userProfile = new Fixture().Create<UserProfile>();

            SetUserClaims(testItems, userEmail);

            testItems.MockUserService
                .Setup(service => service.GetUserInfo(userEmail))
                .ReturnsAsync(userProfile);

            // Act
            var result = await testItems.OrganisationsController.DeleteOrganisation(organisationId);

            // Assert
            testItems.MockOrganisationService.Verify(service => service.DeleteOrganisationAsync(organisationId), Times.Once);
            testItems.MockOrganisationService.Verify(service => service.UpdateOrganisationModifiedDate(organisationId, userProfile.User.UserId), Times.Once);

            var noContentResult = (NoContentResult)result;
            Assert.That(204, Is.EqualTo(noContentResult!.StatusCode));
        }

        [Test]
        public async Task DeleteOrganisation_UserDoesNotExist_SkipsServiceCallsAndReturnsNoContent()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            var organisationId = 1;
            var userEmail = "test@test.com";

            SetUserClaims(testItems, userEmail);

            testItems.MockUserService
                .Setup(service => service.GetUserInfo(userEmail))
                .ReturnsAsync((UserProfile?)null);

            // Act
            var result = await testItems.OrganisationsController.DeleteOrganisation(organisationId);

            // Assert
            testItems.MockOrganisationService.Verify(service => service.DeleteOrganisationAsync(It.IsAny<int>()), Times.Never);
            testItems.MockOrganisationService.Verify(service => service.UpdateOrganisationModifiedDate(It.IsAny<int>(), It.IsAny<int>()), Times.Never);

            var noContentResult = (NoContentResult)result;
            Assert.That(204, Is.EqualTo(noContentResult.StatusCode));
        }
        [Test]
        public async Task AddDomainToOrganisation_ValidRequest_ReturnsCreatedResult()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            int organisationId = 1;
            var userEmail = "test@test.com";
            var domainDetail = fixture.Create<DomainDetail>();
            var userProfile = fixture.Create<UserProfile>();

            SetUserClaims(testItems, userEmail);

            testItems.MockUserService
                .Setup(service => service.GetUserInfo(userEmail))
                .ReturnsAsync(userProfile);

            testItems.MockOrganisationService
                .Setup(service => service.GetOrganisationDomainByNameAsync(domainDetail.DomainName))
                .ReturnsAsync((DomainDetail?)null);

            // Act
            var result = await testItems.OrganisationsController.AddDomainToOrganisation(organisationId, domainDetail);

            // Assert
            testItems.MockUserService.Verify(service => service.GetUserInfo(userEmail), Times.Once);
            testItems.MockOrganisationService.Verify(service => service.GetOrganisationDomainByNameAsync(domainDetail.DomainName), Times.Once);
            testItems.MockOrganisationService.Verify(service => service.AddDomainToOrganisationAsync(organisationId, domainDetail), Times.Once);
            testItems.MockOrganisationService.Verify(service => service.UpdateOrganisationModifiedDate(organisationId, userProfile.User.UserId), Times.Once);

            Assert.That(result, Is.InstanceOf<ActionResult<DomainDetail>>());
        }

        [Test]
        public async Task AddDomainToOrganisation_DomainAlreadyExists_ReturnsBadRequest()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            int organisationId = 1;
            var userEmail = "test@test.com";
            var domainDetail = fixture.Create<DomainDetail>();
            var userProfile = fixture.Create<UserProfile>();

            SetUserClaims(testItems, userEmail);

            testItems.MockUserService
                .Setup(service => service.GetUserInfo(userEmail))
                .ReturnsAsync(userProfile);

            testItems.MockOrganisationService
                .Setup(service => service.GetOrganisationDomainByNameAsync(domainDetail.DomainName))
                .ReturnsAsync(domainDetail);

            // Act
            var result = await testItems.OrganisationsController.AddDomainToOrganisation(organisationId, domainDetail);

            // Assert
            testItems.MockUserService.Verify(service => service.GetUserInfo(userEmail), Times.Once);
            testItems.MockOrganisationService.Verify(service => service.GetOrganisationDomainByNameAsync(domainDetail.DomainName), Times.Once);
            testItems.MockOrganisationService.Verify(service => service.AddDomainToOrganisationAsync(It.IsAny<int>(), It.IsAny<DomainDetail>()), Times.Never);
            testItems.MockOrganisationService.Verify(service => service.UpdateOrganisationModifiedDate(It.IsAny<int>(), It.IsAny<int>()), Times.Never);

            var badRequestResult = (BadRequestObjectResult)(result!.Result!);
            Assert.That(400, Is.EqualTo(badRequestResult.StatusCode));
        }

        [Test]
        public async Task AddDomainToOrganisation_UserNotFound_ReturnsNoContent()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            int organisationId = 1;
            var userEmail = "test@test.com";
            var domainDetail = fixture.Create<DomainDetail>();
            var userProfile = fixture.Create<UserProfile>();

            SetUserClaims(testItems, userEmail);

            testItems.MockUserService
                .Setup(service => service.GetUserInfo(userEmail))
                .ReturnsAsync((UserProfile?)null);

            // Act
            var result = await testItems.OrganisationsController.AddDomainToOrganisation(organisationId, domainDetail);

            // Assert
            testItems.MockUserService.Verify(service => service.GetUserInfo(userEmail), Times.Once);
            testItems.MockOrganisationService.Verify(service => service.GetOrganisationDomainByNameAsync(It.IsAny<string>()), Times.Never);
            testItems.MockOrganisationService.Verify(service => service.AddDomainToOrganisationAsync(It.IsAny<int>(), It.IsAny<DomainDetail>()), Times.Never);
            testItems.MockOrganisationService.Verify(service => service.UpdateOrganisationModifiedDate(It.IsAny<int>(), It.IsAny<int>()), Times.Never);

            var noContentResult = (NoContentResult)result!.Result!;
            Assert.That(204, Is.EqualTo(noContentResult.StatusCode));
        }

        [Test]
        public async Task AddDomainToOrganisation_ThrowsException_ReturnsBadRequest()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            int organisationId = 1;
            var userEmail = "test@test.com";
            var domainDetail = fixture.Create<DomainDetail>();
            var userProfile = fixture.Create<UserProfile>();

            testItems.MockUserService
                .Setup(service => service.GetUserInfo(userEmail))
                .ThrowsAsync(new Exception("Something went wrong"));

            // Act
            var result = await testItems.OrganisationsController.AddDomainToOrganisation(organisationId, domainDetail);

            // Assert
            testItems.MockOrganisationService.Verify(service => service.GetOrganisationDomainByNameAsync(It.IsAny<string>()), Times.Never);
            testItems.MockOrganisationService.Verify(service => service.AddDomainToOrganisationAsync(It.IsAny<int>(), It.IsAny<DomainDetail>()), Times.Never);
            testItems.MockOrganisationService.Verify(service => service.UpdateOrganisationModifiedDate(It.IsAny<int>(), It.IsAny<int>()), Times.Never);

            var badRequestResult = (BadRequestResult)result!.Result!;
            Assert.That(400, Is.EqualTo(badRequestResult.StatusCode));
        }
        [Test]
        public async Task RemoveDomain_ValidDomainId_CallsServiceAndReturnsNoContent()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            var domainId = 1;

            testItems.MockOrganisationService
                .Setup(service => service.RemoveDomainFromOrganisationAsync(domainId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await testItems.OrganisationsController.RemoveDomain(domainId);

            // Assert
            testItems.MockOrganisationService.Verify(service => service.RemoveDomainFromOrganisationAsync(domainId), Times.Once);

            var noContentResult = (NoContentResult)result;
            Assert.That(204, Is.EqualTo(noContentResult.StatusCode));
        }
        [Test]
        public async Task SetOrganisationAllowList_ValidRequest_CallsServiceAndReturnsNoContent()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            int organisationId = 1;
            bool allowList = true;
            var userEmail = "test@example.com";
            var userProfile = fixture.Create<UserProfile>();

            SetUserClaims(testItems, userEmail);

            testItems.MockUserService
                .Setup(service => service.GetUserInfo(userEmail))
                .ReturnsAsync(userProfile);

            // Act
            var result = await testItems.OrganisationsController.SetOrganisationAllowList(organisationId, allowList);

            // Assert
            testItems.MockOrganisationService.Verify(service => service.SetOrganisationAllowListAsync(organisationId, allowList), Times.Once);
            testItems.MockOrganisationService.Verify(service => service.UpdateOrganisationModifiedDate(organisationId, userProfile.User.UserId), Times.Once);

            var noContentResult = (NoContentResult)result;
            Assert.That(204, Is.EqualTo(noContentResult.StatusCode));
        }

        [Test]
        public async Task SetOrganisationAllowList_UserNotFound_ReturnsNoContent()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            int organisationId = 1;
            bool allowList = true;
            var userEmail = "test@example.com";
            var userProfile = fixture.Create<UserProfile>();

            SetUserClaims(testItems, userEmail);

            testItems.MockUserService
                .Setup(service => service.GetUserInfo(userEmail))
                .ReturnsAsync((UserProfile?)null);

            // Act
            var result = await testItems.OrganisationsController.SetOrganisationAllowList(organisationId, allowList);

            // Assert
            testItems.MockOrganisationService.Verify(service => service.SetOrganisationAllowListAsync(It.IsAny<int>(), It.IsAny<bool>()), Times.Never);
            testItems.MockOrganisationService.Verify(service => service.UpdateOrganisationModifiedDate(It.IsAny<int>(), It.IsAny<int>()), Times.Never);

            var noContentResult = (NoContentResult)result;
            Assert.That(204, Is.EqualTo(noContentResult.StatusCode));
        }

        [Test]
        public async Task SetOrganisationAllowList_ThrowsException_ReturnsBadRequest()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            int organisationId = 1;
            bool allowList = true;
            var userEmail = "test@example.com";
            var userProfile = fixture.Create<UserProfile>();

            SetUserClaims(testItems, userEmail);

            testItems.MockUserService
                .Setup(service => service.GetUserInfo(userEmail))
                .ThrowsAsync(new Exception("Something went wrong"));

            // Act
            var result = await testItems.OrganisationsController.SetOrganisationAllowList(organisationId, allowList);

            // Assert
            testItems.MockOrganisationService.Verify(service => service.SetOrganisationAllowListAsync(It.IsAny<int>(), It.IsAny<bool>()), Times.Never);
            testItems.MockOrganisationService.Verify(service => service.UpdateOrganisationModifiedDate(It.IsAny<int>(), It.IsAny<int>()), Times.Never);

            var badRequestResult = (BadRequestResult)result;
            Assert.That(400, Is.EqualTo(badRequestResult.StatusCode));
        }
        [Test]
        public async Task SetDomainAllowList_ValidRequest_CallsServiceAndReturnsNoContent()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            int domainId = 1;
            bool allowList = true;

            testItems.MockOrganisationService
                .Setup(service => service.SetDomainAllowListAsync(domainId, allowList))
                .Returns(Task.CompletedTask);

            // Act
            var result = await testItems.OrganisationsController.SetDomainAllowList(domainId, allowList);

            // Assert
            testItems.MockOrganisationService.Verify(service => service.SetDomainAllowListAsync(domainId, allowList), Times.Once);

            var noContentResult = (NoContentResult)result;
            Assert.That(204, Is.EqualTo(noContentResult.StatusCode));
        }
        #endregion
    }
}
