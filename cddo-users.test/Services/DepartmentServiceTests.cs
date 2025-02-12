using AutoFixture.AutoMoq;
using AutoFixture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using cddo_users.Repositories;
using cddo_users.Services.Database;
using cddo_users.Interface;
using Moq;
using cddo_users.Services;
using cddo_users.DTOs;
using cddo_users.models;

namespace cddo_users.test.Services
{
    [TestFixture]
    public class DepartmentServiceTests
    {
        protected readonly IFixture fixture;

        private readonly DepartmentService _departmentService;

        private readonly Mock<IDepartmentRepository> _departmentRepositoryMock = new();
        private readonly Mock<IOrganisationRepository> _organisationRepositoryMock = new();

        public DepartmentServiceTests()
        {
            fixture = new Fixture().Customize(new AutoMoqCustomization());

            _departmentService = new(_departmentRepositoryMock.Object, _organisationRepositoryMock.Object);

        }

        [Test]
        public async Task AssignOrganisationToDepartmentAsync_DepartmentDoesNotExist_ReturnsFalse()
        {
            // Arrange
            int departmentId = 1;
            int organisationId = 1;

            _departmentRepositoryMock.Invocations.Clear();
            _organisationRepositoryMock.Invocations.Clear();

            _departmentRepositoryMock
                .Setup(repo => repo.GetDepartmentByIdAsync(departmentId))
                .ReturnsAsync((models.Department?)null);

            // Act
            var result = await _departmentService.AssignOrganisationToDepartmentAsync(departmentId, organisationId);

            // Assert
            Assert.That(result, Is.False);
            _departmentRepositoryMock.Verify(repo => repo.GetDepartmentByIdAsync(departmentId), Times.Once);
            _organisationRepositoryMock.Verify(repo => repo.GetOrganisationByIdAsync(It.IsAny<int>()), Times.Never);
        }
        [Test]
        public async Task AssignOrganisationToDepartmentAsync_OrganisationDoesNotExist_ReturnsFalse()
        {
            // Arrange
            int departmentId = 1;
            int organisationId = 1;

            _departmentRepositoryMock.Invocations.Clear();
            _organisationRepositoryMock.Invocations.Clear();

            var department = fixture.Create<models.Department>();

            _departmentRepositoryMock
                .Setup(repo => repo.GetDepartmentByIdAsync(departmentId))
                .ReturnsAsync(department);

            _organisationRepositoryMock
                .Setup(repo => repo.GetOrganisationByIdAsync(organisationId))
                .ReturnsAsync((Organisation?)null);

            // Act
            var result = await _departmentService.AssignOrganisationToDepartmentAsync(departmentId, organisationId);

            // Assert
            Assert.That(result, Is.False);
            _departmentRepositoryMock.Verify(repo => repo.GetDepartmentByIdAsync(departmentId), Times.Once);
            _organisationRepositoryMock.Verify(repo => repo.GetOrganisationByIdAsync(organisationId), Times.Once);

        }
        [Test]
        public async Task AssignOrganisationToDepartmentAsync_AlreadyAssigned_ReassignsOrganisation_ReturnsTrue()
        {
            // Arrange
            int departmentId = 1;
            int organisationId = 1;

            _departmentRepositoryMock.Invocations.Clear();
            _organisationRepositoryMock.Invocations.Clear();

            var department = fixture.Create<models.Department>();
            var organisation = fixture.Create<models.Organisation>();

            _departmentRepositoryMock
                .Setup(repo => repo.GetDepartmentByIdAsync(departmentId))
                .ReturnsAsync(department);

            _organisationRepositoryMock
                .Setup(repo => repo.GetOrganisationByIdAsync(organisationId))
                .ReturnsAsync(organisation);

            _departmentRepositoryMock
                .Setup(repo => repo.CheckAssigned(departmentId, organisationId))
                .ReturnsAsync(true);

            _departmentRepositoryMock
                .Setup(repo => repo.ReAssignOrganisationToDepartmentAsync(departmentId, organisationId))
                .ReturnsAsync(true);

            // Act
            var result = await _departmentService.AssignOrganisationToDepartmentAsync(departmentId, organisationId);

            // Assert
            Assert.That(result, Is.True);
            _departmentRepositoryMock.Verify(repo => repo.CheckAssigned(departmentId, organisationId), Times.Once);
            _departmentRepositoryMock.Verify(repo => repo.ReAssignOrganisationToDepartmentAsync(departmentId, organisationId), Times.Once);
        }

        [Test]
        public async Task AssignOrganisationToDepartmentAsync_NotAssigned_AssignsOrganisation_ReturnsTrue()
        {
            // Arrange
            int departmentId = 1;
            int organisationId = 1;

            _departmentRepositoryMock.Invocations.Clear();
            _organisationRepositoryMock.Invocations.Clear();

            var department = fixture.Create<models.Department>();
            var organisation = fixture.Create<models.Organisation>();

            _departmentRepositoryMock
                .Setup(repo => repo.GetDepartmentByIdAsync(departmentId))
                .ReturnsAsync(department);

            _organisationRepositoryMock
                .Setup(repo => repo.GetOrganisationByIdAsync(organisationId))
                .ReturnsAsync(organisation);

            _departmentRepositoryMock
                .Setup(repo => repo.CheckAssigned(departmentId, organisationId))
                .ReturnsAsync(false);

            _departmentRepositoryMock
                .Setup(repo => repo.AssignOrganisationToDepartmentAsync(departmentId, organisationId))
                .ReturnsAsync(true);

            // Act
            var result = await _departmentService.AssignOrganisationToDepartmentAsync(departmentId, organisationId);

            // Assert
            Assert.That(result, Is.True);
            _departmentRepositoryMock.Verify(repo => repo.CheckAssigned(departmentId, organisationId), Times.Once);
            _departmentRepositoryMock.Verify(repo => repo.ReAssignOrganisationToDepartmentAsync(departmentId, organisationId), Times.Never);
            _departmentRepositoryMock.Verify(repo => repo.AssignOrganisationToDepartmentAsync(departmentId, organisationId), Times.Once);
        }
        [Test]
        public async Task ReAssignOrganisationToDepartmentAsync_DepartmentDoesNotExist_ReturnsFalse()
        {
            // Arrange
            int departmentId = 1;
            int organisationId = 1;

            _departmentRepositoryMock.Invocations.Clear();
            _organisationRepositoryMock.Invocations.Clear();

            var department = fixture.Create<models.Department>();
            var organisation = fixture.Create<models.Organisation>();

            _departmentRepositoryMock
                .Setup(repo => repo.GetDepartmentByIdAsync(departmentId))
                .ReturnsAsync((models.Department?)null);

            // Act
            var result = await _departmentService.ReAssignOrganisationToDepartmentAsync(departmentId, organisationId);

            // Assert
            Assert.That(result, Is.False);
            _departmentRepositoryMock.Verify(repo => repo.GetDepartmentByIdAsync(departmentId), Times.Once);
            _organisationRepositoryMock.Verify(repo => repo.GetOrganisationByIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Test]
        public async Task ReAssignOrganisationToDepartmentAsync_OrganisationDoesNotExist_ReturnsFalse()
        {
            // Arrange
            int departmentId = 1;
            int organisationId = 1;

            _departmentRepositoryMock.Invocations.Clear();
            _organisationRepositoryMock.Invocations.Clear();

            var department = fixture.Create<models.Department>();
            var organisation = fixture.Create<models.Organisation>();

            _departmentRepositoryMock
                .Setup(repo => repo.GetDepartmentByIdAsync(departmentId))
                .ReturnsAsync(department);

            _organisationRepositoryMock
                .Setup(repo => repo.GetOrganisationByIdAsync(organisationId))
                .ReturnsAsync((Organisation?)null);

            // Act
            var result = await _departmentService.ReAssignOrganisationToDepartmentAsync(departmentId, organisationId);

            // Assert
            Assert.That(result, Is.False);
            _departmentRepositoryMock.Verify(repo => repo.GetDepartmentByIdAsync(departmentId), Times.Once);
            _organisationRepositoryMock.Verify(repo => repo.GetOrganisationByIdAsync(organisationId), Times.Once);
        }

        [Test]
        public async Task ReAssignOrganisationToDepartmentAsync_BothExist_ReassignSuccessful_ReturnsTrue()
        {
            // Arrange
            int departmentId = 1;
            int organisationId = 1;

            _departmentRepositoryMock.Invocations.Clear();
            _organisationRepositoryMock.Invocations.Clear();

            var department = fixture.Create<models.Department>();
            var organisation = fixture.Create<models.Organisation>();

            _departmentRepositoryMock
                .Setup(repo => repo.GetDepartmentByIdAsync(departmentId))
                .ReturnsAsync(department);

            _organisationRepositoryMock
                .Setup(repo => repo.GetOrganisationByIdAsync(organisationId))
                .ReturnsAsync(organisation);

            _departmentRepositoryMock
                .Setup(repo => repo.ReAssignOrganisationToDepartmentAsync(departmentId, organisationId))
                .ReturnsAsync(true);

            // Act
            var result = await _departmentService.ReAssignOrganisationToDepartmentAsync(departmentId, organisationId);

            // Assert
            Assert.That(result, Is.True);
            _departmentRepositoryMock.Verify(repo => repo.GetDepartmentByIdAsync(departmentId), Times.Once);
            _organisationRepositoryMock.Verify(repo => repo.GetOrganisationByIdAsync(organisationId), Times.Once);
            _departmentRepositoryMock.Verify(repo => repo.ReAssignOrganisationToDepartmentAsync(departmentId, organisationId), Times.Once);
        }

        [Test]
        public async Task ReAssignOrganisationToDepartmentAsync_BothExist_ReassignFails_ReturnsFalse()
        {
            // Arrange
            int departmentId = 1;
            int organisationId = 1;

            _departmentRepositoryMock.Invocations.Clear();
            _organisationRepositoryMock.Invocations.Clear();

            var department = fixture.Create<models.Department>();
            var organisation = fixture.Create<models.Organisation>();

            _departmentRepositoryMock
                .Setup(repo => repo.GetDepartmentByIdAsync(departmentId))
                .ReturnsAsync(department);

            _organisationRepositoryMock
                .Setup(repo => repo.GetOrganisationByIdAsync(organisationId))
                .ReturnsAsync(organisation);

            _departmentRepositoryMock
                .Setup(repo => repo.ReAssignOrganisationToDepartmentAsync(departmentId, organisationId))
                .ReturnsAsync(false);

            // Act
            var result = await _departmentService.ReAssignOrganisationToDepartmentAsync(departmentId, organisationId);

            // Assert
            Assert.That(result, Is.False);
            _departmentRepositoryMock.Verify(repo => repo.GetDepartmentByIdAsync(departmentId), Times.Once);
            _organisationRepositoryMock.Verify(repo => repo.GetOrganisationByIdAsync(organisationId), Times.Once);
            _departmentRepositoryMock.Verify(repo => repo.ReAssignOrganisationToDepartmentAsync(departmentId, organisationId), Times.Once);
        }
        [Test]
        public async Task UnAssignOrganisationToDepartmentAsync_DepartmentDoesNotExist_ReturnsFalse()
        {
            // Arrange
            int departmentId = 1;
            int organisationId = 1;

            _departmentRepositoryMock.Invocations.Clear();
            _organisationRepositoryMock.Invocations.Clear();

            var department = fixture.Create<models.Department>();
            var organisation = fixture.Create<models.Organisation>();

            _departmentRepositoryMock
                .Setup(repo => repo.GetDepartmentByIdAsync(departmentId))
                .ReturnsAsync((models.Department?)null);

            // Act
            var result = await _departmentService.UnAssignOrganisationToDepartmentAsync(departmentId, organisationId);

            // Assert
            Assert.That(result, Is.False);
            _departmentRepositoryMock.Verify(repo => repo.GetDepartmentByIdAsync(departmentId), Times.Once);
            _organisationRepositoryMock.Verify(repo => repo.GetOrganisationByIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Test]
        public async Task UnAssignOrganisationToDepartmentAsync_OrganisationDoesNotExist_ReturnsFalse()
        {
            // Arrange
            int departmentId = 1;
            int organisationId = 1;

            _departmentRepositoryMock.Invocations.Clear();
            _organisationRepositoryMock.Invocations.Clear();

            var department = fixture.Create<models.Department>();
            var organisation = fixture.Create<models.Organisation>();

            _departmentRepositoryMock
                .Setup(repo => repo.GetDepartmentByIdAsync(departmentId))
                .ReturnsAsync(department);

            _organisationRepositoryMock
                .Setup(repo => repo.GetOrganisationByIdAsync(organisationId))
                .ReturnsAsync((Organisation?)null);

            // Act
            var result = await _departmentService.UnAssignOrganisationToDepartmentAsync(departmentId, organisationId);

            // Assert
            Assert.That(result, Is.False);
            _departmentRepositoryMock.Verify(repo => repo.GetDepartmentByIdAsync(departmentId), Times.Once);
            _organisationRepositoryMock.Verify(repo => repo.GetOrganisationByIdAsync(organisationId), Times.Once);
        }

        [Test]
        public async Task UnAssignOrganisationToDepartmentAsync_BothExist_UnassignSuccessful_ReturnsTrue()
        {
            // Arrange
            int departmentId = 1;
            int organisationId = 1;

            _departmentRepositoryMock.Invocations.Clear();
            _organisationRepositoryMock.Invocations.Clear();

            var department = fixture.Create<models.Department>();
            var organisation = fixture.Create<models.Organisation>();

            _departmentRepositoryMock
                .Setup(repo => repo.GetDepartmentByIdAsync(departmentId))
                .ReturnsAsync(department);

            _organisationRepositoryMock
                .Setup(repo => repo.GetOrganisationByIdAsync(organisationId))
                .ReturnsAsync(organisation);

            _departmentRepositoryMock
                .Setup(repo => repo.UnAssignOrganisationToDepartmentAsync(departmentId, organisationId))
                .ReturnsAsync(true);

            // Act
            var result = await _departmentService.UnAssignOrganisationToDepartmentAsync(departmentId, organisationId);

            // Assert
            Assert.That(result, Is.True);
            _departmentRepositoryMock.Verify(repo => repo.GetDepartmentByIdAsync(departmentId), Times.Once);
            _organisationRepositoryMock.Verify(repo => repo.GetOrganisationByIdAsync(organisationId), Times.Once);
            _departmentRepositoryMock.Verify(repo => repo.UnAssignOrganisationToDepartmentAsync(departmentId, organisationId), Times.Once);
        }

        [Test]
        public async Task UnAssignOrganisationToDepartmentAsync_BothExist_UnassignFails_ReturnsFalse()
        {
            // Arrange
            int departmentId = 1;
            int organisationId = 1;

            _departmentRepositoryMock.Invocations.Clear();
            _organisationRepositoryMock.Invocations.Clear();

            var department = fixture.Create<models.Department>();
            var organisation = fixture.Create<models.Organisation>();

            _departmentRepositoryMock
                .Setup(repo => repo.GetDepartmentByIdAsync(departmentId))
                .ReturnsAsync(department);

            _organisationRepositoryMock
                .Setup(repo => repo.GetOrganisationByIdAsync(organisationId))
                .ReturnsAsync(organisation);

            _departmentRepositoryMock
                .Setup(repo => repo.UnAssignOrganisationToDepartmentAsync(departmentId, organisationId))
                .ReturnsAsync(false);

            // Act
            var result = await _departmentService.UnAssignOrganisationToDepartmentAsync(departmentId, organisationId);

            // Assert
            Assert.That(result, Is.False);
            _departmentRepositoryMock.Verify(repo => repo.GetDepartmentByIdAsync(departmentId), Times.Once);
            _organisationRepositoryMock.Verify(repo => repo.GetOrganisationByIdAsync(organisationId), Times.Once);
            _departmentRepositoryMock.Verify(repo => repo.UnAssignOrganisationToDepartmentAsync(departmentId, organisationId), Times.Once);
        }
        [Test]
        public async Task GetUnAssignedOrganisation_NoUnassignedOrganisations_ReturnsEmptyList()
        {
            // Arrange
            _departmentRepositoryMock.Invocations.Clear();
            _organisationRepositoryMock.Invocations.Clear();

            var department = fixture.Create<models.Department>();
            var organisations = fixture.Create<List<models.Organisation>>();

            _departmentRepositoryMock
                .Setup(repo => repo.GetUnAssignedOrganisationsAsync())
                .ReturnsAsync(new List<Organisation>());

            // Act
            var result = await _departmentService.GetUnAssignedOrganisation();

            // Assert
            Assert.That(result, Is.Empty);
            _departmentRepositoryMock.Verify(repo => repo.GetUnAssignedOrganisationsAsync(), Times.Once);
        }

        [Test]
        public async Task GetUnAssignedOrganisation_UnassignedOrganisationsExist_ReturnsOrganisationList()
        {
            // Arrange
            _departmentRepositoryMock.Invocations.Clear();
            _organisationRepositoryMock.Invocations.Clear();

            var department = fixture.Create<models.Department>();
            var organisations = fixture.Create<List<models.Organisation>>();

            _departmentRepositoryMock
                .Setup(repo => repo.GetUnAssignedOrganisationsAsync())
                .ReturnsAsync(organisations);

            // Act
            var result = await _departmentService.GetUnAssignedOrganisation();

            // Assert
            Assert.That(result, Is.EquivalentTo(organisations));
            Assert.That(result.Count(), Is.EqualTo(organisations.Count()));
            _departmentRepositoryMock.Verify(repo => repo.GetUnAssignedOrganisationsAsync(), Times.Once);
        }
        [Test]
        public async Task GetAssignedOrganisation_NoAssignedOrganisations_ReturnsEmptyList()
        {
            // Arrange
            _departmentRepositoryMock.Invocations.Clear();
            _organisationRepositoryMock.Invocations.Clear();

            int id = 1;

            var department = fixture.Create<models.Department>();
            var organisations = fixture.Create<List<models.Organisation>>();

            _departmentRepositoryMock
                .Setup(repo => repo.GetAssignedOrganisationsAsync(id))
                .ReturnsAsync(new List<Organisation>());

            // Act
            var result = await _departmentService.GetAssignedOrganisations(id);

            // Assert
            Assert.That(result, Is.Empty);
            _departmentRepositoryMock.Verify(repo => repo.GetAssignedOrganisationsAsync(id), Times.Once);
        }

        [Test]
        public async Task GetAllAssignedOrganisations_assignedOrganisationsExist_ReturnsOrganisationList()
        {
            // Arrange
            _departmentRepositoryMock.Invocations.Clear();
            _organisationRepositoryMock.Invocations.Clear();

            int id = 1;

            var department = fixture.Create<models.Department>();
            var organisations = fixture.Create<List<models.Organisation>>();

            _departmentRepositoryMock
                .Setup(repo => repo.GetAssignedOrganisationsAsync(id))
                .ReturnsAsync(organisations);

            // Act
            var result = await _departmentService.GetAssignedOrganisations(id);

            // Assert
            Assert.That(result, Is.EquivalentTo(organisations));
            Assert.That(result.Count(), Is.EqualTo(organisations.Count()));
            _departmentRepositoryMock.Verify(repo => repo.GetAssignedOrganisationsAsync(id), Times.Once);
        }
        [Test]
        public async Task GetAllAssignedOrganisation_NoUnassignedOrganisations_ReturnsEmptyList()
        {
            // Arrange
            _departmentRepositoryMock.Invocations.Clear();
            _organisationRepositoryMock.Invocations.Clear();

            var department = fixture.Create<models.Department>();
            var organisations = fixture.Create<List<models.Organisation>>();

            _departmentRepositoryMock
                .Setup(repo => repo.GetAllAssignedOrganisationsAsync())
                .ReturnsAsync(new List<DepartmentToOrganisationDetail>());

            // Act
            var result = await _departmentService.GetAllAssignedOrganisation();

            // Assert
            Assert.That(result, Is.Empty);
            _departmentRepositoryMock.Verify(repo => repo.GetAllAssignedOrganisationsAsync(), Times.Once);
        }

        [Test]
        public async Task GetAllAssignedOrganisation_AssignedOrganisationsExist_ReturnsOrganisationList()
        {
            // Arrange
            _departmentRepositoryMock.Invocations.Clear();
            _organisationRepositoryMock.Invocations.Clear();

            var department = fixture.Create<models.Department>();
            var organisations = fixture.Create<List<DepartmentToOrganisationDetail>>();

            _departmentRepositoryMock
                .Setup(repo => repo.GetAllAssignedOrganisationsAsync())
                .ReturnsAsync(organisations);

            // Act
            var result = await _departmentService.GetAllAssignedOrganisation();

            // Assert
            Assert.That(result, Is.EquivalentTo(organisations));
            Assert.That(result.Count(), Is.EqualTo(organisations.Count()));
            _departmentRepositoryMock.Verify(repo => repo.GetAllAssignedOrganisationsAsync(), Times.Once);
        }
        [Test]
        public async Task GetDepartmentByIdAsync_DepartmentDoesNotExist_ReturnsNull()
        {
            // Arrange
            _departmentRepositoryMock.Invocations.Clear();
            _organisationRepositoryMock.Invocations.Clear();

            int departmentId = 1;

            _departmentRepositoryMock
                .Setup(repo => repo.GetDepartmentByIdAsync(departmentId))
                .ReturnsAsync((models.Department?)null);

            // Act
            var result = await _departmentService.GetDepartmentByIdAsync(departmentId);

            // Assert
            Assert.That(result, Is.Null);
            _departmentRepositoryMock.Verify(repo => repo.GetDepartmentByIdAsync(departmentId), Times.Once);
        }

        [Test]
        public async Task GetDepartmentByIdAsync_DepartmentExists_ReturnsDepartment()
        {
            // Arrange
            _departmentRepositoryMock.Invocations.Clear();
            _organisationRepositoryMock.Invocations.Clear();

            int departmentId = 1;
            var expectedDepartment = fixture.Create<models.Department>();

            _departmentRepositoryMock
                .Setup(repo => repo.GetDepartmentByIdAsync(departmentId))
                .ReturnsAsync(expectedDepartment);

            // Act
            var result = await _departmentService.GetDepartmentByIdAsync(departmentId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.EqualTo(expectedDepartment));
            _departmentRepositoryMock.Verify(repo => repo.GetDepartmentByIdAsync(departmentId), Times.Once);
        }
        [Test]
        public async Task GetAllPagedDepartmentsAsync_ValidRequest_ReturnsPagedDepartments()
        {
            // Arrange
            _departmentRepositoryMock.Invocations.Clear();
            _organisationRepositoryMock.Invocations.Clear();

            int page = 1;
            int pageSize = 5;
            string searchTerm = "test";

            var departments = fixture.Create<List<models.Department>>();
            int totalCount = departments.Count(); 

            _departmentRepositoryMock
                .Setup(repo => repo.GetAllPagedDepartmentsAsync(page, pageSize, searchTerm))
                .ReturnsAsync((totalCount, departments));

            // Act
            var result = await _departmentService.GetAllPagedDepartmentsAsync(page, pageSize, searchTerm);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Item1, Is.EqualTo(totalCount));
            Assert.That(result.Item2, Is.EquivalentTo(departments));
            _departmentRepositoryMock.Verify(repo => repo.GetAllPagedDepartmentsAsync(page, pageSize, searchTerm), Times.Once);
        }

        [Test]
        public async Task GetAllPagedDepartmentsAsync_NoMatchingDepartmentsExist_ReturnsEmptyList()
        {
            // Arrange
            _departmentRepositoryMock.Invocations.Clear();
            _organisationRepositoryMock.Invocations.Clear();

            int page = 1;
            int pageSize = 5;
            string searchTerm = "test";

            var departments = fixture.Create<List<models.Department>>();
            int totalCount = departments.Count();

            _departmentRepositoryMock
                .Setup(repo => repo.GetAllPagedDepartmentsAsync(page, pageSize, searchTerm))
                .ReturnsAsync((0, new List<models.Department>()));

            // Act
            var result = await _departmentService.GetAllPagedDepartmentsAsync(page, pageSize, searchTerm);

            // Assert
            Assert.That(result.Item1, Is.EqualTo(0));
            Assert.That(result.Item2, Is.Empty);
            _departmentRepositoryMock.Verify(repo => repo.GetAllPagedDepartmentsAsync(page, pageSize, searchTerm), Times.Once);
        }
        [Test]
        public async Task GetAllDepartmentsAsync_NoDepartmentsExist_ReturnsEmptyList()
        {
            // Arrange
            _departmentRepositoryMock.Invocations.Clear();
            _organisationRepositoryMock.Invocations.Clear();

            _departmentRepositoryMock
                .Setup(repo => repo.GetAllDepartmentsAsync())
                .ReturnsAsync(new List<models.Department>());

            // Act
            var result = await _departmentService.GetAllDepartmentsAsync();

            // Assert
            Assert.That(result, Is.Empty);
            _departmentRepositoryMock.Verify(repo => repo.GetAllDepartmentsAsync(), Times.Once);
        }

        [Test]
        public async Task GetAllDepartmentsAsync_DepartmentsExist_ReturnsDepartmentList()
        {
            // Arrange
            _departmentRepositoryMock.Invocations.Clear();
            _organisationRepositoryMock.Invocations.Clear();

            var departments = fixture.Create<List<models.Department>>();

            _departmentRepositoryMock
                .Setup(repo => repo.GetAllDepartmentsAsync())
                .ReturnsAsync(departments);

            // Act
            var result = await _departmentService.GetAllDepartmentsAsync();

            // Assert
            Assert.That(result, Is.EquivalentTo(departments));
            Assert.That(result, Has.Count.EqualTo(departments.Count()));
            _departmentRepositoryMock.Verify(repo => repo.GetAllDepartmentsAsync(), Times.Once);
        }
        [Test]
        public async Task CreateDepartmentAsync_ValidInput_ReturnsCreatedDepartment()
        {
            // Arrange
            _departmentRepositoryMock.Invocations.Clear();
            _organisationRepositoryMock.Invocations.Clear();

            string departmentName = "test";
            int userId = 1;
            var expectedDepartment = fixture.Create<models.Department>();

            _departmentRepositoryMock
                .Setup(repo => repo.CreateDepartment(departmentName, userId))
                .ReturnsAsync(expectedDepartment);

            // Act
            var result = await _departmentService.CreateDepartmentAsync(departmentName, userId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.EqualTo(expectedDepartment));
            _departmentRepositoryMock.Verify(repo => repo.CreateDepartment(departmentName, userId), Times.Once);
        }

        [Test]
        public async Task CreateDepartmentAsync_RepositoryReturnsNull_ReturnsNull()
        {
            // Arrange
            string departmentName = "test";
            int userId = 1;

            _departmentRepositoryMock
                .Setup(repo => repo.CreateDepartment(departmentName, userId))
                .ReturnsAsync((models.Department?)null);

            // Act
            var result = await _departmentService.CreateDepartmentAsync(departmentName, userId);

            // Assert
            Assert.That(result, Is.Null);
            _departmentRepositoryMock.Verify(repo => repo.CreateDepartment(departmentName, userId), Times.Once);
        }
    }
}
