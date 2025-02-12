using cddo_users.Api;
using cddo_users.DTOs;
using cddo_users.Interface;
using cddo_users.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cddo_users.test.TestHelpers
{
    public static class TestSetUp
    {
        #region Test Item Creation
        public static TestItems CreateTestItems()
        {
            var mockLogger = new Mock<ILogger<OrganisationsController>>();
            var mockUserInformationPresenter = new Mock<IUserInformationPresenter>();
            var mockEmailManager = new Mock<IEmailManager>();
            var mockOrganisationRepository = new Mock<IOrganisationRepository>();
            var mockUserService = new Mock<IUserService>();
            var mockOrganisationService = new Mock<IOrganisationService>();
            var mockDepartmentService = new Mock<IDepartmentService>();
            var mockConfiguration = new Mock<IConfiguration>();
            var mockClientAuthService = new Mock<IClientAuthService>();
            var mockApplicationInsightService = new Mock<IApplicationInsightsService>();

            ConfigureHappyPathTesting();

            var organisationsController = new OrganisationsController(
                mockUserService.Object,
                mockOrganisationService.Object);

            var clientAuthController = new ClientAuthController(
               mockConfiguration.Object,
               mockClientAuthService.Object);

            var usersController = new UserController(
                mockUserService.Object,
                mockApplicationInsightService.Object,
                mockEmailManager.Object);

            var departmentController = new DepartmentController(
                mockDepartmentService.Object,
                mockUserService.Object);

            return new TestItems(
                organisationsController,
                departmentController,
                clientAuthController,
                mockLogger,
                mockUserInformationPresenter,
                mockEmailManager,
                mockOrganisationRepository,
                mockUserService,
                mockOrganisationService,
                mockDepartmentService,
                mockClientAuthService,
                mockConfiguration,
                usersController,
                mockApplicationInsightService);

            void ConfigureHappyPathTesting()
            {
                mockOrganisationRepository.Setup(x => x.SetDataShareRequestMailboxAddressAsync(
                        It.IsAny<int>(),
                        It.IsAny<string?>()))
                    .ReturnsAsync((int domainId, string? dataShareRequestMailboxAddress) => new DomainDetail
                    {
                        DomainId = domainId,
                        DataShareRequestMailboxAddress = dataShareRequestMailboxAddress,
                        DomainName = string.Empty
                    });
            }
        }

       
        #endregion
    }

    public class TestItems(
           OrganisationsController organisationsController,
           DepartmentController departmentController,
           ClientAuthController clientAuthController,
           Mock<ILogger<OrganisationsController>> mockLogger,
           Mock<IUserInformationPresenter> mockUserInformationPresenter,
           Mock<IEmailManager> mockEmailManager,
           Mock<IOrganisationRepository> mockOrganisationRepository,
           Mock<IUserService> mockUserService,
           Mock<IOrganisationService> mockOrganisationService,
           Mock<IDepartmentService> mockDepartmentService,
           Mock<IClientAuthService> mockClientAuthService,
           Mock<IConfiguration> mockConfiguration,
           UserController userController,
           Mock<IApplicationInsightsService> mockApplicationInsightService
        )
    {
        public OrganisationsController OrganisationsController { get; } = organisationsController;
        public DepartmentController DepartmentController { get; } = departmentController;
        public ClientAuthController ClientAuthController { get; } = clientAuthController;
        public UserController UserController { get; } = userController;
        public Mock<ILogger<OrganisationsController>> MockLogger { get; } = mockLogger;
        public Mock<IUserInformationPresenter> MockUserInformationPresenter { get; } = mockUserInformationPresenter;
        public Mock<IEmailManager> MockEmailManager { get; } = mockEmailManager;
        public Mock<IOrganisationRepository> MockOrganisationRepository { get; } = mockOrganisationRepository;
        public Mock<IUserService> MockUserService { get; } = mockUserService;
        public Mock<IDepartmentService> MockDepartmentService { get; } = mockDepartmentService;
        public Mock<IOrganisationService> MockOrganisationService { get; } = mockOrganisationService;
        public Mock<IClientAuthService> MockClientAuthService { get; } = mockClientAuthService;
        public Mock<IConfiguration> MockConfiguration { get; } = mockConfiguration;
        public Mock<IApplicationInsightsService> MockApplicationInsightsService { get; } = mockApplicationInsightService;
    }
}
