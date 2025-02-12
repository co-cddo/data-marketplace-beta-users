using AutoFixture;
using AutoFixture.AutoMoq;
using cddo_users.Api;
using cddo_users.DTOs;
using cddo_users.Interface;
using cddo_users.Logic;
using cddo_users.models;
using cddo_users.Repositories;
using cddo_users.test.TestHelpers;
using Microsoft.Extensions.Logging;
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
    public class OrganisationServiceTests
    {
        protected readonly IFixture fixture;

        private readonly Mock<IOrganisationRepository> _organisationRepositoryMock = new();
        private readonly Mock<IUserInformationPresenter> _userInformationPresenterMock = new();
        private readonly Mock<IEmailManager> _emailManagerMock = new();
        private readonly Mock<ILogger<OrganisationService>> _loggerMock = new();

        private readonly OrganisationService _organisationService;

        public OrganisationServiceTests()
        {
            fixture = new Fixture().Customize(new AutoMoqCustomization());

            _organisationService = new(_organisationRepositoryMock.Object, _userInformationPresenterMock.Object, _emailManagerMock.Object, _loggerMock.Object);
        }
        public void clearInvocations()
        {
            _organisationRepositoryMock.Invocations.Clear();
            _userInformationPresenterMock.Invocations.Clear();
            _emailManagerMock.Invocations.Clear();
            _loggerMock.Invocations.Clear();
        }

        [Test]
        public async Task CreateOrganisation_OrganisationWithSameNameExists_ReturnsErrorMessage()
        {
            // Arrange
            clearInvocations();

            var organisationDetail = fixture.Create<OrganisationDetail>();
            var organisation = fixture.Create<Organisation>();

            _organisationRepositoryMock
                .Setup(repo => repo.GetOrganisationByNameAsync(organisationDetail.OrganisationName!))
                .ReturnsAsync(organisation);

            // Act
            var result = await _organisationService.CreateOrganisation(organisationDetail);

            // Assert
            Assert.That(result.Id, Is.EqualTo(0));
            Assert.That(result.Message, Is.EqualTo("An organisation with the same name already exists."));
            _organisationRepositoryMock.Verify(repo => repo.GetOrganisationByNameAsync(organisationDetail.OrganisationName!), Times.Once);
        }

        [Test]
        public async Task CreateOrganisation_DomainWithSameNameExists_ReturnsErrorMessage()
        {
            // Arrange
            clearInvocations();

            var organisationDetail = fixture.Create<OrganisationDetail>();
            var domainDetail = fixture.Create<DomainDetail>();

            _organisationRepositoryMock
                .Setup(repo => repo.GetOrganisationByNameAsync(organisationDetail.OrganisationName!))
                .ReturnsAsync((Organisation?)null);

            _organisationRepositoryMock
                .Setup(repo => repo.GetOrganisationDomainByNameAsync(It.IsAny<string>()))
                .ReturnsAsync(domainDetail);

            // Act
            var result = await _organisationService.CreateOrganisation(organisationDetail);

            // Assert
            Assert.That(result.Id, Is.EqualTo(0));
            Assert.That(result.Message, Is.EqualTo("A domain with the same name already exists."));
            _organisationRepositoryMock.Verify(repo => repo.GetOrganisationByNameAsync(organisationDetail.OrganisationName!), Times.Once);
            _organisationRepositoryMock.Verify(repo => repo.GetOrganisationDomainByNameAsync(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task CreateOrganisation_CreationFails_ReturnsErrorMessage()
        {
            // Arrange
            clearInvocations();

            var domainDetail = fixture.Create<DomainDetail>();
            var organisationDetail = fixture.Create<OrganisationDetail>();

            _organisationRepositoryMock
                .Setup(repo => repo.GetOrganisationByNameAsync(organisationDetail.OrganisationName!))
                .ReturnsAsync((Organisation?)null);

            _organisationRepositoryMock
                .Setup(repo => repo.GetOrganisationDomainByNameAsync(It.IsAny<string>()))
                .ReturnsAsync((DomainDetail?)null);

            _organisationRepositoryMock
                .Setup(repo => repo.CreateDepartmentOrganisationAsync(organisationDetail))
                .ReturnsAsync(0);

            // Act
            var result = await _organisationService.CreateOrganisation(organisationDetail);

            // Assert
            Assert.That(result.Id, Is.EqualTo(0));
            Assert.That(result.Message, Is.EqualTo("An error occurred while creating the organisation."));
            _organisationRepositoryMock.Verify(repo => repo.CreateDepartmentOrganisationAsync(organisationDetail), Times.Once);
        }

        [Test]
        public async Task CreateOrganisation_SuccessfullyCreatesOrganisation_ReturnsOrganisationId()
        {
            // Arrange
            clearInvocations();

            var domainDetail = fixture.Create<DomainDetail>();
            var organisationDetail = fixture.Create<OrganisationDetail>();

            int createdId = 1;

            _organisationRepositoryMock
                .Setup(repo => repo.GetOrganisationByNameAsync(organisationDetail.OrganisationName!))
                .ReturnsAsync((Organisation?)null);

            _organisationRepositoryMock
                .Setup(repo => repo.GetOrganisationDomainByNameAsync(It.IsAny<string>()))
                .ReturnsAsync((DomainDetail?)null);

            _organisationRepositoryMock
                .Setup(repo => repo.CreateDepartmentOrganisationAsync(organisationDetail))
                .ReturnsAsync(createdId);

            // Act
            var result = await _organisationService.CreateOrganisation(organisationDetail);

            // Assert
            Assert.That(result.Id, Is.EqualTo(createdId));
            Assert.That(result.Message, Is.Empty);
            _organisationRepositoryMock.Verify(repo => repo.CreateDepartmentOrganisationAsync(organisationDetail), Times.Once);
        }
        [Test]
        public async Task getOrganisationsByPage_NoOrganisationsExist_ReturnsEmptyResponse()
        {
            // Arrange
            clearInvocations();

            var filter = new OrganisationFilter { Page = 1, PageSize = 10 };
            _organisationRepositoryMock
                .Setup(repo => repo.GetAllOrganisationDetailsAsync(filter))
                .ReturnsAsync((Enumerable.Empty<OrganisationDetail>(), 0));

            // Act
            var result = await _organisationService.getOrganisationsByPage(filter);

            // Assert
            Assert.That(result.Orgs, Is.Empty);
            Assert.That(result.TotalCount, Is.EqualTo(0));
            Assert.That(result.TotalPages, Is.EqualTo(0));
            _organisationRepositoryMock.Verify(repo => repo.GetAllOrganisationDetailsAsync(filter), Times.Once);
        }

        [Test]
        public async Task getOrganisationsByPage_OrganisationsExistWithoutDomains_ReturnsOrganisations()
        {
            // Arrange
            clearInvocations();

            var filter = new OrganisationFilter { Page = 1, PageSize = 10 };
            var organisations = fixture.Create<List<OrganisationDetail>>();

            _organisationRepositoryMock
                .Setup(repo => repo.GetAllOrganisationDetailsAsync(filter))
                .ReturnsAsync((organisations, organisations.Count()));

            // Act
            var result = await _organisationService.getOrganisationsByPage(filter);

            // Assert
            Assert.That(result.Orgs, Has.Count.EqualTo(organisations.Count()));
            Assert.That(result.TotalCount, Is.EqualTo(organisations.Count()));
            Assert.That(result.TotalPages, Is.EqualTo(1));
            _organisationRepositoryMock.Verify(repo => repo.GetAllOrganisationDetailsAsync(filter), Times.Once);
        }

        [Test]
        public async Task getOrganisationsByPage_OrganisationsExistWithMultipleDomains_FetchesAndUpdatesDomains()
        {
            // Arrange
            clearInvocations();

            var filter = new OrganisationFilter { Page = 1, PageSize = 10 };
            var organisations = fixture.Create<List<OrganisationDetail>>();

            var domainsForOrg = fixture.Create<List<DomainDetail>>();

            _organisationRepositoryMock
                .Setup(repo => repo.GetAllOrganisationDetailsAsync(filter))
                .ReturnsAsync((organisations, organisations.Count()));

            _organisationRepositoryMock
                .Setup(repo => repo.GetDomainsByOrganisationId(It.IsAny<int>()))
                .ReturnsAsync(domainsForOrg);

            // Act
            var result = await _organisationService.getOrganisationsByPage(filter);

            // Assert
            Assert.That(result.Orgs, Has.Count.EqualTo(organisations.Count()));
            Assert.That(result.Orgs.First().Domains.Count(), Is.EqualTo(domainsForOrg.Count()));
            _organisationRepositoryMock.Verify(repo => repo.GetDomainsByOrganisationId((int)organisations.First().OrganisationId), Times.Once);
        }
        [Test]
        public async Task SendEmailAsync_EmailSentSuccessfully_CallsEmailManager()
        {
            // Arrange
            clearInvocations();

            var updatedDomainDetail =fixture.Create<DomainDetail>();
            var domainId = 1;
            var mailboxAddress = "test@test.com";

            _userInformationPresenterMock
                .Setup(presenter => presenter.GetUserNameOfInitiatingUser())
                .Returns("Test");

            // Act
            await _organisationService.SendEmailAsync(updatedDomainDetail, domainId, mailboxAddress);

            // Assert
            _emailManagerMock.Verify(
                emailManager => emailManager.SendDomainDsrMailboxAddressChangedEmailAsync(
                    mailboxAddress,
                    It.IsAny<Dictionary<string, dynamic>>()),
                Times.Once);
        }
        [Test]
        public async Task SendEmailAsync_ExceptionDuringEmailSending_LogsError()
        {
            // Arrange
            clearInvocations();

            var updatedDomainDetail = fixture.Create<DomainDetail>();
            var domainId = 1;
            var mailboxAddress = "test@test.com";

            _userInformationPresenterMock
                .Setup(presenter => presenter.GetUserNameOfInitiatingUser())
                .Returns("test");

            _emailManagerMock
                .Setup(emailManager => emailManager.SendDomainDsrMailboxAddressChangedEmailAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>()))
                .ThrowsAsync(new Exception("Email sending failed"));

            // Act
            await _organisationService.SendEmailAsync(updatedDomainDetail, domainId, mailboxAddress);

            // Assert
            _loggerMock.VerifyLog(LogLevel.Error, "Failed to send notification of update to Data Share Request notifications email address");

        }
        [Test]
        public async Task CreateOrganisationRequest_ShouldReturnError_WhenOrganisationAlreadyExists()
        {
            // Arrange
            clearInvocations();

            var organisationRequest = fixture.Create<OrganisationRequest>();
            var organisation = fixture.Create<Organisation>();

            _organisationRepositoryMock.Setup(r => r.GetOrganisationByNameAsync(organisationRequest.OrganisationName!))
                .ReturnsAsync(organisation);

            // Act
            var result = await _organisationService.createOrganisationRequest(organisationRequest);

            // Assert
            Assert.That(result.Id, Is.EqualTo(0));
            Assert.That(result.Message, Is.EqualTo("An organisation with the same name already exists."));
        }

        [Test]
        public async Task CreateOrganisationRequest_ShouldReturnError_WhenDomainAlreadyExists()
        {
            // Arrange
            clearInvocations();

            var organisationRequest = fixture.Create<OrganisationRequest>();
            var domainDetail = fixture.Create<DomainDetail>();

            _organisationRepositoryMock.Setup(r => r.GetOrganisationByNameAsync(organisationRequest.OrganisationName!))
                .ReturnsAsync((Organisation?)null); // No organisation exists
            _organisationRepositoryMock.Setup(r => r.GetOrganisationDomainByNameAsync(organisationRequest.DomainName!))
                .ReturnsAsync(domainDetail); // Domain already exists

            // Act
            var result = await _organisationService.createOrganisationRequest(organisationRequest);

            // Assert
            Assert.That(result.Id, Is.EqualTo(0));
            Assert.That(result.Message, Is.EqualTo("A domain with the same name already exists."));
        }

        [Test]
        public async Task CreateOrganisationRequest_ShouldReturnError_WhenRequestAlreadyExists()
        {
            // Arrange
            clearInvocations();

            var organisationRequest = fixture.Create<OrganisationRequest>();

            _organisationRepositoryMock.Setup(r => r.GetOrganisationByNameAsync(organisationRequest.OrganisationName!))
                .ReturnsAsync((Organisation?)null); // No organisation exists
            _organisationRepositoryMock.Setup(r => r.GetOrganisationDomainByNameAsync(organisationRequest.DomainName!))
                .ReturnsAsync((DomainDetail?)null); // No domain exists
            _organisationRepositoryMock.Setup(r => r.GetOrganisationRequestByOrganisationNameAsync(organisationRequest.OrganisationName!, organisationRequest.DomainName!))
                .ReturnsAsync(organisationRequest); // Request already exists

            // Act
            var result = await _organisationService.createOrganisationRequest(organisationRequest);

            // Assert
            Assert.That(result.Id, Is.EqualTo(0));
            Assert.That(result.Message, Is.EqualTo("A request for this organisation or domain already exists."));
        }

        [Test]
        public async Task CreateOrganisationRequest_ShouldCreateRequestAndSendEmails_WhenSuccessful()
        {
            // Arrange
            clearInvocations();

            var organisationRequest = fixture.Create<OrganisationRequest>();

            _organisationRepositoryMock.Setup(r => r.GetOrganisationByNameAsync(organisationRequest.OrganisationName!))
                .ReturnsAsync((Organisation?)null); // No organisation exists
            _organisationRepositoryMock.Setup(r => r.GetOrganisationDomainByNameAsync(organisationRequest.DomainName!))
                .ReturnsAsync((DomainDetail?)null); // No domain exists
            _organisationRepositoryMock.Setup(r => r.GetOrganisationRequestByOrganisationNameAsync(organisationRequest.OrganisationName!, organisationRequest.DomainName!))
                .ReturnsAsync((OrganisationRequest?)null); // No existing request

            _organisationRepositoryMock.Setup(r => r.CreateOrganisationRequestAsync(organisationRequest))
                .ReturnsAsync(1); // Successfully create the organisation request

            _organisationRepositoryMock.Setup(r => r.UpdateOrganisationRequestIdAsync(organisationRequest, 1))
                .Returns(Task.CompletedTask); // Successfully update

            // Act
            var result = await _organisationService.createOrganisationRequest(organisationRequest);

            // Assert
            Assert.That(result.Id, Is.EqualTo(1));
            Assert.That(result.Message, Is.Empty);

            // Verify emails were sent
            _emailManagerMock.Verify(manager => manager.OrganisationRequestSubmitted(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            _emailManagerMock.Verify(manager => manager.OrganisationRequestSubmittedToSystemAdmin(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task CreateOrganisationRequest_ShouldReturnError_WhenRequestCreationFails()
        {
            // Arrange
            clearInvocations();

            var organisationRequest = fixture.Create<OrganisationRequest>();

            _organisationRepositoryMock.Setup(r => r.GetOrganisationByNameAsync(organisationRequest.OrganisationName!))
                .ReturnsAsync((Organisation?)null); // No organisation exists
            _organisationRepositoryMock.Setup(r => r.GetOrganisationDomainByNameAsync(organisationRequest.DomainName!))
                .ReturnsAsync((DomainDetail?)null); // No domain exists
            _organisationRepositoryMock.Setup(r => r.GetOrganisationRequestByOrganisationNameAsync(organisationRequest.OrganisationName!, organisationRequest.DomainName!))
                .ReturnsAsync((OrganisationRequest?)null); // No existing request

            _organisationRepositoryMock.Setup(r => r.CreateOrganisationRequestAsync(organisationRequest))
                .ReturnsAsync(0); // Failed to create the organisation request

            // Act
            var result = await _organisationService.createOrganisationRequest(organisationRequest);

            // Assert
            Assert.That(result.Id, Is.EqualTo(0));
            Assert.That(result.Message, Is.EqualTo("An error occurred while creating the organisation request."));
        }
        [Test]
        public async Task UpdateOrganisationRequest_ShouldReturnError_WhenOrganisationRequestNotFound()
        {
            // Arrange
            clearInvocations();

            var id = 1;
            var updatedRequest = fixture.Create<OrganisationRequest>();

            _organisationRepositoryMock.Setup(r => r.UpdateOrganisationRequestAsync(id, updatedRequest))
                .ReturnsAsync(false);

            // Act
            var result = await _organisationService.UpdateOrganisationRequest(id, updatedRequest);

            // Assert
            Assert.That(result.Id, Is.EqualTo(0));
            Assert.That(result.Message, Is.EqualTo("Organisation request not found or no changes made."));
        }

        [Test]
        public async Task UpdateOrganisationRequest_ShouldSendApprovalEmail_WhenStatusIsApproved()
        {
            // Arrange
            clearInvocations();

            var id = 1;
            var updatedRequest = fixture.Create<OrganisationRequest>();
            updatedRequest.Status = "Approved";

            _organisationRepositoryMock.Setup(r => r.UpdateOrganisationRequestAsync(id, updatedRequest))
                .ReturnsAsync(true);

            // Act
            var result = await _organisationService.UpdateOrganisationRequest(id, updatedRequest);

            // Assert
            Assert.That(result.Id, Is.EqualTo(id));
            _emailManagerMock.Verify(manager => manager.OrganisationRequestApproved(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task UpdateOrganisationRequest_ShouldSendRejectionEmail_WhenStatusIsRejected()
        {
            // Arrange
            clearInvocations();

            var id = 1;
            var updatedRequest = fixture.Create<OrganisationRequest>();
            updatedRequest.Status = "Rejected";

            _organisationRepositoryMock.Setup(r => r.UpdateOrganisationRequestAsync(id, updatedRequest))
                .ReturnsAsync(true);

            // Act
            var result = await _organisationService.UpdateOrganisationRequest(id, updatedRequest);

            // Assert
            Assert.That(result.Id, Is.EqualTo(id));
            _emailManagerMock.Verify(em => em.OrganisationRequestRejected(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task UpdateOrganisationRequest_ShouldReturnSuccess_WhenUpdateIsSuccessful()
        {
            // Arrange
            clearInvocations();

            var id = 1;
            var updatedRequest = fixture.Create<OrganisationRequest>();

            _organisationRepositoryMock.Setup(r => r.UpdateOrganisationRequestAsync(id, updatedRequest))
                .ReturnsAsync(true);

            // Act
            var result = await _organisationService.UpdateOrganisationRequest(id, updatedRequest);

            // Assert
            Assert.That(result.Id, Is.EqualTo(id));
            _emailManagerMock.Verify(manager => manager.OrganisationRequestApproved(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _emailManagerMock.Verify(manager => manager.OrganisationRequestRejected(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
        [Test]
        public async Task UpdateOrganisationAsync_CallsUpdateOrganisationAsync_OnRepository()
        {
            // Arrange
            clearInvocations();

            var organisationDetail = fixture.Create<OrganisationDetail>();

            // Act
            await _organisationService.UpdateOrganisationAsync(organisationDetail);

            // Assert
            _organisationRepositoryMock.Verify(
                repo => repo.UpdateOrganisationAsync(It.Is<OrganisationDetail>(od => od == organisationDetail)),
                Times.Once);
        }

        [Test]
        public async Task GetAllOrganisationRequestsAsync_ReturnsOrganisationRequests()
        {
            // Arrange
            clearInvocations();

            var organisationRequests = fixture.Create<List<OrganisationRequest>>();

            _organisationRepositoryMock
                .Setup(repo => repo.GetAllOrganisationRequestsAsync())
                .ReturnsAsync(organisationRequests);

            // Act
            var result = await _organisationService.GetAllOrganisationRequestsAsync();

            // Assert);
            Assert.That(result.Count(), Is.EqualTo(organisationRequests.Count()));
            Assert.That(result, Is.EquivalentTo(organisationRequests));
        }

        [Test]
        public async Task GetOrganisationRequestByIdAsync_ReturnsOrganisationRequest_WhenIdExists()
        {
            // Arrange
            clearInvocations();

            var organisationRequest = fixture.Create<OrganisationRequest>();
            int id = (int)organisationRequest.OrganisationRequestID!; // Use the ID from the created object

            _organisationRepositoryMock
                .Setup(repo => repo.GetOrganisationRequestByIdAsync(id))
                .ReturnsAsync(organisationRequest);

            // Act
            var result = await _organisationService.GetOrganisationRequestByIdAsync(id);

            // Assert
            Assert.That(result?.OrganisationRequestID, Is.EqualTo(id));
        }


        [Test]
        public async Task GetOrganisationTypeSummariesAsync_ReturnsOrganisationTypeSummaries()
        {
            // Arrange
            clearInvocations();

            var organisationTypeSummaries = fixture.Create<List<OrganisationTypeSummaryDto>>();
            _organisationRepositoryMock
                .Setup(repo => repo.GetOrganisationTypeSummariesAsync())
                .ReturnsAsync(organisationTypeSummaries);

            // Act
            var result = await _organisationService.GetOrganisationTypeSummariesAsync();

            // Assert
            Assert.That(result.Count(), Is.EqualTo(organisationTypeSummaries.Count));
            Assert.That(result, Is.EquivalentTo(organisationTypeSummaries));
        }
        [Test]
        public async Task GetDomainsGroupedByTypeAsync_ReturnsDomainsGroupedByType()
        {
            // Arrange
            clearInvocations();

            var groupedDomains = fixture.Create<List<OrganisationDomainsGrouped>>();
            _organisationRepositoryMock
                .Setup(repo => repo.GetDomainsGroupedByTypeAsync())
                .ReturnsAsync(groupedDomains);

            // Act
            var result = await _organisationService.GetDomainsGroupedByTypeAsync();

            // Assert
            Assert.That(result.Count(), Is.EqualTo(groupedDomains.Count));
            Assert.That(result, Is.EquivalentTo(groupedDomains));
        }
        [Test]
        public async Task GetDomainsGroupedByFormatAsync_ReturnsDomainsGroupedByFormat()
        {
            // Arrange
            clearInvocations();

            var groupedByFormat = fixture.Create<List<GroupedByFormatDto>>();
            _organisationRepositoryMock
                .Setup(repo => repo.GetDomainsGroupedByFormatAsync())
                .ReturnsAsync(groupedByFormat);

            // Act
            var result = await _organisationService.GetDomainsGroupedByFormatAsync();

            // Assert
            Assert.That(result.Count(), Is.EqualTo(groupedByFormat.Count));
            Assert.That(result, Is.EquivalentTo(groupedByFormat));
        }
        [Test]
        public async Task GetOrganisationDetailByIdAsync_ReturnsOrganisationDetail_WhenIdExists()
        {
            // Arrange
            clearInvocations();

            var organisationDetail = fixture.Create<OrganisationDetail>();
            var id = organisationDetail.OrganisationId ?? 1; 
            _organisationRepositoryMock
                .Setup(repo => repo.GetOrganisationDetailByIdAsync((int)id))
                .ReturnsAsync(organisationDetail);

            // Act
            var result = await _organisationService.GetOrganisationDetailByIdAsync((int)id);

            // Assert
            Assert.That(result!.OrganisationId, Is.EqualTo(id));
        }
        [Test]
        public async Task SearchOrganisationsAndDomainsAsync_ReturnsOrganisationsAndDomains_WhenQueryMatches()
        {
            // Arrange
            clearInvocations();

            var organisationsAndDomains = fixture.Create<List<OrganisationDetail>>();
            var query = "test";

            _organisationRepositoryMock
                .Setup(repo => repo.SearchOrganisationsAndDomainsAsync(query))
                .ReturnsAsync(organisationsAndDomains);

            // Act
            var result = await _organisationService.SearchOrganisationsAndDomainsAsync(query);

            // Assert
            Assert.That(result.Count(), Is.EqualTo(organisationsAndDomains.Count));
            Assert.That(result, Is.EquivalentTo(organisationsAndDomains));
        }

        [Test]
        public async Task DeleteOrganisationAsync_DeletesOrganisation_WhenIdExists()
        {
            // Arrange
            clearInvocations();

            var organisationId = 1; 
            _organisationRepositoryMock
                .Setup(repo => repo.DeleteOrganisationAsync(organisationId))
                .Returns(Task.CompletedTask);

            // Act
            await _organisationService.DeleteOrganisationAsync(organisationId);

            // Assert
            _organisationRepositoryMock.Verify(
                repo => repo.DeleteOrganisationAsync(organisationId),
                Times.Once
            );
        }
        [Test]
        public async Task UpdateOrganisationModifiedDate_UpdatesDate_WhenValidIdAndUserId()
        {
            // Arrange
            clearInvocations();

            var organisationId = 1; 
            var userId = 1;
            _organisationRepositoryMock
                .Setup(repo => repo.UpdateOrganisationModifiedDate(organisationId, userId))
                .Returns(Task.CompletedTask);

            // Act
            await _organisationService.UpdateOrganisationModifiedDate(organisationId, userId);

            // Assert
            _organisationRepositoryMock.Verify(
                repo => repo.UpdateOrganisationModifiedDate(organisationId, userId),
                Times.Once
            );
        }

        [Test]
        public async Task GetOrganisationDomainByNameAsync_ReturnsDomainDetail_WhenDomainExists()
        {
            // Arrange
            clearInvocations();

            var domainDetail = fixture.Create<DomainDetail>(); 
            _organisationRepositoryMock
                .Setup(repo => repo.GetOrganisationDomainByNameAsync(domainDetail.DomainName))
                .ReturnsAsync(domainDetail);

            // Act
            var result = await _organisationService.GetOrganisationDomainByNameAsync(domainDetail.DomainName);

            // Assert
            Assert.That(result!.DomainName, Is.EqualTo(domainDetail.DomainName));
        }
        [Test]
        public async Task AddDomainToOrganisationAsync_AddsDomain_WhenValidOrganisationIdAndDomainDetail()
        {
            // Arrange
            clearInvocations();

            var organisationId = 1;
            var domainDetail = fixture.Create<DomainDetail>();
            _organisationRepositoryMock
                .Setup(repo => repo.AddDomainToOrganisationAsync(organisationId, domainDetail))
                .Returns(Task.CompletedTask);

            // Act
            await _organisationService.AddDomainToOrganisationAsync(organisationId, domainDetail);

            // Assert
            _organisationRepositoryMock.Verify(
                repo => repo.AddDomainToOrganisationAsync(organisationId, domainDetail),
                Times.Once
            );
        }
        [Test]
        public async Task RemoveDomainFromOrganisationAsync_RemovesDomain_WhenValidDomainId()
        {
            // Arrange
            clearInvocations();

            var domainId = 1; 
            _organisationRepositoryMock
                .Setup(repo => repo.RemoveDomainFromOrganisationAsync(domainId))
                .Returns(Task.CompletedTask);

            // Act
            await _organisationService.RemoveDomainFromOrganisationAsync(domainId);

            // Assert
            _organisationRepositoryMock.Verify(
                repo => repo.RemoveDomainFromOrganisationAsync(domainId),
                Times.Once
            );
        }
        [Test]
        public async Task SetOrganisationAllowListAsync_SetsAllowList_WhenValidOrganisationId()
        {
            // Arrange
            clearInvocations();

            var organisationId = 1; 
            var allowList = true;
            _organisationRepositoryMock
                .Setup(repo => repo.SetOrganisationAllowListAsync(organisationId, allowList))
                .Returns(Task.CompletedTask);

            // Act
            await _organisationService.SetOrganisationAllowListAsync(organisationId, allowList);

            // Assert
            _organisationRepositoryMock.Verify(
                repo => repo.SetOrganisationAllowListAsync(organisationId, allowList),
                Times.Once
            );
        }
        [Test]
        public async Task SetDomainAllowListAsync_SetsAllowList_WhenValidDomainId()
        {
            // Arrange
            clearInvocations();

            var domainId = 1; 
            var allowList = true; 
            _organisationRepositoryMock
                .Setup(repo => repo.SetDomainAllowListAsync(domainId, allowList))
                .Returns(Task.CompletedTask);

            // Act
            await _organisationService.SetDomainAllowListAsync(domainId, allowList);

            // Assert
            _organisationRepositoryMock.Verify(
                repo => repo.SetDomainAllowListAsync(domainId, allowList),
                Times.Once
            );
        }
        [Test]
        public async Task SetDataShareRequestMailboxAddressAsync_SetsMailboxAddress_WhenValidDomainIdAndAddress()
        {
            // Arrange
            clearInvocations();

            var domainId = 1; 
            var dataShareRequestMailboxAddress = "test@test.com"; 
            var domainDetail = fixture.Create<DomainDetail>(); 

            _organisationRepositoryMock
                .Setup(repo => repo.SetDataShareRequestMailboxAddressAsync(domainId, dataShareRequestMailboxAddress))
                .ReturnsAsync(domainDetail); 

            // Act
            var result = await _organisationService.SetDataShareRequestMailboxAddressAsync(domainId, dataShareRequestMailboxAddress);

            // Assert
            _organisationRepositoryMock.Verify(
                repo => repo.SetDataShareRequestMailboxAddressAsync(domainId, dataShareRequestMailboxAddress),
                Times.Once
            );
            Assert.That(result, Is.EqualTo(domainDetail));
        }
    }
}
