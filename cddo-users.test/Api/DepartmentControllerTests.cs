using AutoFixture;
using AutoFixture.AutoMoq;
using cddo_users.DTOs;
using cddo_users.models;
using cddo_users.test.TestHelpers;
using DocumentFormat.OpenXml.Office2010.Excel;
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
    public class DepartmentControllerTests
    {
        protected readonly IFixture fixture;

        public DepartmentControllerTests()
        {
            fixture = new Fixture().Customize(new AutoMoqCustomization());

        }

        [Test]
        public async Task AssignOrganisationToDepartment_ValidRequest_ReturnsOk()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            int departmentId = 1;
            int organisationId = 10;

            testItems.MockDepartmentService
                .Setup(service => service.AssignOrganisationToDepartmentAsync(departmentId, organisationId))
                .ReturnsAsync(true);

            // Act
            var result = await testItems.DepartmentController.AssignOrganisationToDepartment(departmentId, organisationId);

            // Assert
            testItems.MockDepartmentService.Verify(service => service.AssignOrganisationToDepartmentAsync(departmentId, organisationId), Times.Once);

            var okResult = (OkObjectResult)result;
            Assert.That(200, Is.EqualTo(okResult.StatusCode));
            Assert.That((bool)okResult!.Value!, Is.True);
        }

        [Test]
        public async Task AssignOrganisationToDepartment_ServiceThrowsException_ReturnsOkWithFalse()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            int departmentId = 1;
            int organisationId = 10;

            testItems.MockDepartmentService
                .Setup(service => service.AssignOrganisationToDepartmentAsync(departmentId, organisationId))
                .ThrowsAsync(new Exception("Something went wrong"));

            // Act
            var result = await testItems.DepartmentController.AssignOrganisationToDepartment(departmentId, organisationId);

            // Assert
            testItems.MockDepartmentService.Verify(service => service.AssignOrganisationToDepartmentAsync(departmentId, organisationId), Times.Once);

            var okResult = (OkObjectResult)result;
            Assert.That(200, Is.EqualTo(okResult.StatusCode));
            Assert.That((bool)okResult!.Value!, Is.False);
        }
        [Test]
        public async Task ReAssignOrganisationToDepartment_ValidRequest_ReturnsOk()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            int departmentId = 1;
            int organisationId = 10;

            testItems.MockDepartmentService
                .Setup(service => service.ReAssignOrganisationToDepartmentAsync(departmentId, organisationId))
                .ReturnsAsync(true);

            // Act
            var result = await testItems.DepartmentController.ReAssignOrganisationToDepartment(departmentId, organisationId);

            // Assert
            testItems.MockDepartmentService.Verify(service => service.ReAssignOrganisationToDepartmentAsync(departmentId, organisationId), Times.Once);

            var okResult = (OkObjectResult)result;
            Assert.That(200, Is.EqualTo(okResult.StatusCode));
            Assert.That((bool)okResult!.Value!, Is.True);
        }

        [Test]
        public async Task ReAssignOrganisationToDepartment_ServiceThrowsException_ReturnsOkWithFalse()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            int departmentId = 1;
            int organisationId = 10;

            testItems.MockDepartmentService
                .Setup(service => service.ReAssignOrganisationToDepartmentAsync(departmentId, organisationId))
                .ThrowsAsync(new Exception("Something went wrong"));

            // Act
            var result = await testItems.DepartmentController.ReAssignOrganisationToDepartment(departmentId, organisationId);

            // Assert
            testItems.MockDepartmentService.Verify(service => service.ReAssignOrganisationToDepartmentAsync(departmentId, organisationId), Times.Once);

            var okResult = (OkObjectResult)result;
            Assert.That(200, Is.EqualTo(okResult.StatusCode));
            Assert.That((bool)okResult!.Value!, Is.False);
        }
        [Test]
        public async Task UnAssignOrganisationToDepartment_ValidRequest_ReturnsOk()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            int departmentId = 1;
            int organisationId = 10;

            testItems.MockDepartmentService
                .Setup(service => service.UnAssignOrganisationToDepartmentAsync(departmentId, organisationId))
                .ReturnsAsync(true);

            // Act
            var result = await testItems.DepartmentController.UnAssignOrganisationToDepartment(departmentId, organisationId);

            // Assert
            testItems.MockDepartmentService.Verify(service => service.UnAssignOrganisationToDepartmentAsync(departmentId, organisationId), Times.Once);

            var okResult = (OkObjectResult)result;
            Assert.That(200, Is.EqualTo(okResult.StatusCode));
            Assert.That((bool)okResult!.Value!, Is.True);
        }

        [Test]
        public async Task UnAssignOrganisationToDepartment_ServiceThrowsException_ReturnsOkWithFalse()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            int departmentId = 1;
            int organisationId = 10;

            testItems.MockDepartmentService
                .Setup(service => service.UnAssignOrganisationToDepartmentAsync(departmentId, organisationId))
                .ThrowsAsync(new Exception("Something went wrong"));

            // Act
            var result = await testItems.DepartmentController.UnAssignOrganisationToDepartment(departmentId, organisationId);

            // Assert
            testItems.MockDepartmentService.Verify(service => service.UnAssignOrganisationToDepartmentAsync(departmentId, organisationId), Times.Once);

            var okResult = (OkObjectResult)result;
            Assert.That(200, Is.EqualTo(okResult.StatusCode));
            Assert.That((bool)okResult!.Value!, Is.False);
        }
        [Test]
        public async Task GetUnAssignedOrganisation_ValidRequest_ReturnsOk()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            var orgFixture = fixture.Create<IEnumerable<Organisation>>();

            testItems.MockDepartmentService
                .Setup(service => service.GetUnAssignedOrganisation())
                .ReturnsAsync(orgFixture);

            // Act
            var result = await testItems.DepartmentController.GetUnAssignedOrganisation();

            // Assert
            testItems.MockDepartmentService.Verify(service => service.GetUnAssignedOrganisation(), Times.Once);

            var okResult = (OkObjectResult)result;
            Assert.That(200, Is.EqualTo(okResult.StatusCode));
            Assert.That(okResult!.Value!, Is.EqualTo(orgFixture));
        }

        [Test]
        public async Task GetUnAssignedOrganisation_ServiceThrowsException_ReturnsOkWithNull()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            testItems.MockDepartmentService
                .Setup(service => service.GetUnAssignedOrganisation())
                .ThrowsAsync(new Exception("Something went wrong"));

            // Act
            var result = await testItems.DepartmentController.GetUnAssignedOrganisation();

            // Assert
            testItems.MockDepartmentService.Verify(service => service.GetUnAssignedOrganisation(), Times.Once);

            var okResult = (OkObjectResult)result;
            Assert.That(200, Is.EqualTo(okResult.StatusCode));
            Assert.That(okResult!.Value!, Is.Null);
        }
        [Test]
        public async Task GetAllAssignedOrganisation_ValidRequest_ReturnsOk()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            var orgFixture = fixture.Create<IEnumerable<DepartmentToOrganisationDetail>>();

            testItems.MockDepartmentService
                .Setup(service => service.GetAllAssignedOrganisation())
                .ReturnsAsync(orgFixture);

            // Act
            var result = await testItems.DepartmentController.GetAllAssignedOrganisation();

            // Assert
            testItems.MockDepartmentService.Verify(service => service.GetAllAssignedOrganisation(), Times.Once);

            var okResult = (OkObjectResult)result;
            Assert.That(200, Is.EqualTo(okResult.StatusCode));
            Assert.That(okResult!.Value!, Is.EqualTo(orgFixture));
        }

        [Test]
        public async Task GetAllAssignedOrganisation_ServiceThrowsException_ReturnsOkWithNull()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            testItems.MockDepartmentService
                .Setup(service => service.GetAllAssignedOrganisation())
                .ThrowsAsync(new Exception("Something went wrong"));

            // Act
            var result = await testItems.DepartmentController.GetAllAssignedOrganisation();

            // Assert
            testItems.MockDepartmentService.Verify(service => service.GetAllAssignedOrganisation(), Times.Once);

            var okResult = (OkObjectResult)result;
            Assert.That(200, Is.EqualTo(okResult.StatusCode));
            Assert.That(okResult.Value, Is.Null);
        }
        [Test]
        public async Task GetAssignedOrganisations_ValidRequest_ReturnsOk()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            var orgFixture = fixture.Create<IEnumerable<Organisation>>();
            int orgId = 1;

            testItems.MockDepartmentService
                .Setup(service => service.GetAssignedOrganisations(orgId))
                .ReturnsAsync(orgFixture);

            // Act
            var result = await testItems.DepartmentController.GetAssignedOrganisations(orgId);

            // Assert
            testItems.MockDepartmentService.Verify(service => service.GetAssignedOrganisations(orgId), Times.Once);

            var okResult = (OkObjectResult)result;
            Assert.That(200, Is.EqualTo(okResult.StatusCode));
            Assert.That(okResult.Value, Is.EqualTo(orgFixture));
        }

        [Test]
        public async Task GetAssignedOrganisations_ServiceThrowsException_ReturnsOkWithNull()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            var orgFixture = fixture.Create<IEnumerable<Organisation>>();
            int orgId = 1;

            testItems.MockDepartmentService
                .Setup(service => service.GetAssignedOrganisations(orgId))
                .ThrowsAsync(new Exception("Something went wrong"));

            // Act
            var result = await testItems.DepartmentController.GetAssignedOrganisations(orgId);

            // Assert
            testItems.MockDepartmentService.Verify(service => service.GetAssignedOrganisations(orgId), Times.Once);

            var okResult = (OkObjectResult)result;
            Assert.That(200, Is.EqualTo(okResult.StatusCode));
            Assert.That(okResult.Value, Is.Null);
        }
        [Test]
        public async Task GetDepartmentById_ValidRequest_ReturnsOk()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            var orgFixture = fixture.Create<models.Department>();
            int orgId = 1;

            testItems.MockDepartmentService
                .Setup(service => service.GetDepartmentByIdAsync(orgId))
                .ReturnsAsync(orgFixture);

            // Act
            var result = await testItems.DepartmentController.GetDepartmentById(orgId);

            // Assert
            testItems.MockDepartmentService.Verify(service => service.GetDepartmentByIdAsync(orgId), Times.Once);

            var okResult = (OkObjectResult)result;
            Assert.That(200, Is.EqualTo(okResult.StatusCode));
            Assert.That(okResult.Value, Is.EqualTo(orgFixture));
        }

        [Test]
        public async Task GetDepartmentById_ServiceThrowsException_ReturnsOkWithNull()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            var orgFixture = fixture.Create<models.Department>();
            int orgId = 1;

            testItems.MockDepartmentService
                .Setup(service => service.GetDepartmentByIdAsync(orgId))
                .ThrowsAsync(new Exception("Something went wrong"));

            // Act
            var result = await testItems.DepartmentController.GetDepartmentById(orgId);

            // Assert
            testItems.MockDepartmentService.Verify(service => service.GetDepartmentByIdAsync(orgId), Times.Once);

            var okResult = (OkObjectResult)result;
            Assert.That(200, Is.EqualTo(okResult.StatusCode));
            Assert.That(okResult.Value, Is.Null);
        }
        [Test]
        public async Task GetAllPagedDepartments_ValidRequest_ReturnsOkWithPaginatedResponse()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            int page = 1;
            int pageSize = 5;
            string searchTerm = "test";

            var departments = fixture.Create<(int, IEnumerable<models.Department>)>();

            testItems.MockDepartmentService
                .Setup(service => service.GetAllPagedDepartmentsAsync(page, pageSize, searchTerm))
                .ReturnsAsync(departments);

            // Act
            var response = await testItems.DepartmentController.GetAllPagedDepartments(page, pageSize, searchTerm);

            // Assert
            testItems.MockDepartmentService.Verify(service => service.GetAllPagedDepartmentsAsync(page, pageSize, searchTerm), Times.Once);

            var okResult = (OkObjectResult)response;
            Assert.That(okResult.StatusCode, Is.EqualTo(200));

            var paginatedResponse = (PaginatedDepartments)okResult!.Value!;
            Assert.That(paginatedResponse.CurrentPage, Is.EqualTo(page));
            Assert.That(paginatedResponse.PageSize, Is.EqualTo(pageSize));
        }

        [Test]
        public async Task GetAllPagedDepartments_InvalidPageAndPageSize_UsesDefaultValues()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            int page = 0; // Invalid value
            int pageSize = -1; // Invalid value
            string searchTerm = "test";

            var departments = fixture.Create<(int, IEnumerable<models.Department>)>();

            testItems.MockDepartmentService
                .Setup(service => service.GetAllPagedDepartmentsAsync(page, pageSize, searchTerm))
                .ReturnsAsync(departments);

            // Act
            var response = await testItems.DepartmentController.GetAllPagedDepartments(page, pageSize, searchTerm);

            // Assert
            testItems.MockDepartmentService.Verify(service => service.GetAllPagedDepartmentsAsync(1, 10, searchTerm), Times.Once);

            var okResult = (OkObjectResult)response;
            Assert.That(okResult.StatusCode, Is.EqualTo(200));

            var paginatedResponse = (PaginatedDepartments)okResult!.Value!;
            Assert.That(paginatedResponse.CurrentPage, Is.EqualTo(1));
            Assert.That(paginatedResponse.PageSize, Is.EqualTo(10));
        }

        [Test]
        public async Task GetAllPagedDepartments_ServiceThrowsException_ReturnsOkWithNull()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            int page = 1;
            int pageSize = 10;
            string searchTerm = "test";

            testItems.MockDepartmentService
                .Setup(service => service.GetAllPagedDepartmentsAsync(page, pageSize, searchTerm))
                .ThrowsAsync(new Exception("Something went wrong"));

            // Act
            var response = await testItems.DepartmentController.GetAllPagedDepartments(page, pageSize, searchTerm);

            // Assert
            testItems.MockDepartmentService.Verify(service => service.GetAllPagedDepartmentsAsync(page, pageSize, searchTerm), Times.Once);

            var okResult = (OkObjectResult)response;
            Assert.That(okResult.StatusCode, Is.EqualTo(200));
            Assert.That(okResult.Value, Is.Null);
        }
        [Test]
        public async Task GetAllDepartments_ValidRequest_ReturnsOkWithDepartmentsList()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            var departments = fixture.Create<IEnumerable<models.Department>>();

            testItems.MockDepartmentService
                .Setup(service => service.GetAllDepartmentsAsync())
                .ReturnsAsync(departments);

            // Act
            var response = await testItems.DepartmentController.GetAllDepartments();

            // Assert
            testItems.MockDepartmentService.Verify(service => service.GetAllDepartmentsAsync(), Times.Once);

            var okResult = response as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult!.StatusCode, Is.EqualTo(200));

            var resultList = (IEnumerable<models.Department>)okResult!.Value!;
            Assert.That(resultList, Is.Not.Null);
            Assert.That(resultList.Count, Is.EqualTo(departments.Count()));
        }

        [Test]
        public async Task GetAllDepartments_ServiceThrowsException_ReturnsOkWithNull()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            testItems.MockDepartmentService
                .Setup(service => service.GetAllDepartmentsAsync())
                .ThrowsAsync(new Exception("Something went wrong"));

            // Act
            var response = await testItems.DepartmentController.GetAllDepartments();

            // Assert
            testItems.MockDepartmentService.Verify(service => service.GetAllDepartmentsAsync(), Times.Once);

            var okResult = (OkObjectResult)response;
            Assert.That(okResult.StatusCode, Is.EqualTo(200));
            Assert.That(okResult.Value, Is.Null);
        }
        [Test]
        public async Task CreateDepartment_ValidRequest_ReturnsOkWithResult()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            // Mock User Claims
            var userClaims = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Email, "testuser@test.com")
            }, "mock"));

            testItems.DepartmentController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = userClaims }
            };

            string departmentName = "New Department";
            var user = fixture.Create<UserProfile>();
            var createdDepartment = fixture.Create<models.Department>();

            testItems.MockUserService
                .Setup(service => service.GetUserByEmailAsync("testuser@test.com"))
                .ReturnsAsync(user);

            testItems.MockDepartmentService
                .Setup(service => service.CreateDepartmentAsync(departmentName, user.User.UserId))
                .ReturnsAsync(createdDepartment);

            // Act
            var response = await testItems.DepartmentController.CreateDepartment(departmentName);

            // Assert
            testItems.MockUserService.Verify(service => service.GetUserByEmailAsync("testuser@test.com"), Times.Once);
            testItems.MockDepartmentService.Verify(service => service.CreateDepartmentAsync(departmentName, user.User.UserId), Times.Once);

            var okResult = (OkObjectResult)response;
            Assert.That(okResult.StatusCode, Is.EqualTo(200));

            var result = (models.Department)okResult!.Value!;
            Assert.That(result.Id, Is.EqualTo(createdDepartment.Id));
        }
        [Test]
        public async Task CreateDepartment_ServiceReturnsNull_ReturnsBadRequest()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            // Mock User Claims
            var userClaims = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Email, "testuser@test.com")
            }, "mock"));

            testItems.DepartmentController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = userClaims }
            };

            string departmentName = "New Department";
            var user = fixture.Create<UserProfile>();

            testItems.MockUserService
                .Setup(service => service.GetUserByEmailAsync("testuser@test.com"))
                .ReturnsAsync(user);

            testItems.MockDepartmentService
                .Setup(service => service.CreateDepartmentAsync(departmentName, user.User.UserId))
                .ReturnsAsync((models.Department?)null);

            // Act
            var response = await testItems.DepartmentController.CreateDepartment(departmentName);

            // Assert
            testItems.MockUserService.Verify(service => service.GetUserByEmailAsync("testuser@test.com"), Times.Once);
            testItems.MockDepartmentService.Verify(service => service.CreateDepartmentAsync(departmentName, user.User.UserId), Times.Once);

            var badRequestResult = (BadRequestObjectResult)response;
            Assert.That(badRequestResult.StatusCode, Is.EqualTo(400));
        }
        [Test]
        public async Task CreateDepartment_ExceptionThrown_ReturnsInternalServerError()
        {
            // Arrange
            var testItems = TestSetUp.CreateTestItems();

            // Mock User Claims
            var userClaims = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Email, "testuser@test.com")
            }, "mock"));

            testItems.DepartmentController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = userClaims }
            };

            string departmentName = "New Department";
            var user = fixture.Create<UserProfile>();

            testItems.MockUserService
                .Setup(service => service.GetUserByEmailAsync("testuser@test.com"))
                .ThrowsAsync(new Exception("Something went wrong"));

            // Act
            var response = await testItems.DepartmentController.CreateDepartment(departmentName);

            // Assert
            testItems.MockUserService.Verify(service => service.GetUserByEmailAsync("testuser@test.com"), Times.Once);
            testItems.MockDepartmentService.Verify(service => service.CreateDepartmentAsync(departmentName, user.User.UserId), Times.Never);

            var badRequestResult = (ObjectResult)response;
            Assert.That(badRequestResult.StatusCode, Is.EqualTo(500));
        }
    }
}
