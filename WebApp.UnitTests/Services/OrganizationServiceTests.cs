using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WebApp.Interfaces;
using WebApp.Models;
using WebApp.Services;

namespace WebApp.UnitTests.Services;


public class OrganizationServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<OrganizationService>> _logger = new();
    private readonly Mock<IOrganizationRepository> _orgRepo = new();
    private readonly Mock<IProjectRepository> _projectRepo = new();

    private OrganizationService CreateSut()
    {
        _unitOfWork.SetupGet(x => x.Organizations).Returns(_orgRepo.Object);
        _unitOfWork.SetupGet(x => x.Projects).Returns(_projectRepo.Object);
        _unitOfWork.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

        return new OrganizationService(_unitOfWork.Object, _logger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WhenUnitOfWorkIsNull_ThrowsArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new OrganizationService(null!, _logger.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("unitOfWork");
    }

    [Fact]
    public void Constructor_WhenLoggerIsNull_ThrowsArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new OrganizationService(_unitOfWork.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region CreateOrganizationAsync Tests

    [Fact]
    public async Task CreateOrganizationAsync_WhenOrganizationIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        Func<Task> act = async () => await sut.CreateOrganizationAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("organization");
    }

    [Fact]
    public async Task CreateOrganizationAsync_WhenValid_SetsBusinessRulesCorrectly()
    {
        // Arrange
        var sut = CreateSut();
        var organization = new Organization
        {
            Email = "org@example.com",
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "123456789",
            OrganizationName = "Test Org",
            Description = "Test Description",
            Address = "123 Main St",
            City = "Test City"
        };

        Organization? capturedOrg = null;
        _orgRepo.Setup(r => r.AddAsync(It.IsAny<Organization>()))
            .Callback<Organization>(o => capturedOrg = o)
            .ReturnsAsync((Organization o) => o);

        // Act
        var result = await sut.CreateOrganizationAsync(organization);

        // Assert
        capturedOrg.Should().NotBeNull();
        capturedOrg!.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        capturedOrg.IsActive.Should().BeTrue("new organizations should be active by default");
        capturedOrg.IsVerified.Should().BeFalse("new organizations should start unverified");
    }

    [Fact]
    public async Task CreateOrganizationAsync_WhenValid_CallsRepositoryAndSavesChanges()
    {
        // Arrange
        var sut = CreateSut();
        var organization = new Organization
        {
            Email = "org@example.com",
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "123456789",
            OrganizationName = "Test Org",
            City = "Test City"
        };

        _orgRepo.Setup(r => r.AddAsync(It.IsAny<Organization>()))
            .ReturnsAsync(organization);

        // Act
        var result = await sut.CreateOrganizationAsync(organization);

        // Assert
        _orgRepo.Verify(r => r.AddAsync(It.IsAny<Organization>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        result.Should().BeSameAs(organization);
    }

    #endregion

    #region UpdateOrganizationAsync Tests

    [Fact]
    public async Task UpdateOrganizationAsync_WhenOrganizationNotFound_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        _orgRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Organization?)null);

        var updateData = new Organization
        {
            Id = 1,
            Email = "updated@example.com",
            FirstName = "Updated",
            LastName = "Name",
            PhoneNumber = "987654321",
            OrganizationName = "Updated Org",
            City = "Updated City"
        };

        // Act
        var result = await sut.UpdateOrganizationAsync(1, updateData);

        // Assert
        result.Should().BeFalse();
        _orgRepo.Verify(r => r.Update(It.IsAny<Organization>()), Times.Never);
    }

    [Fact]
    public async Task UpdateOrganizationAsync_WhenOrganizationFound_UpdatesAllowedFieldsOnly()
    {
        // Arrange
        var sut = CreateSut();
        var existingOrg = new Organization
        {
            Id = 1,
            Email = "old@example.com",
            FirstName = "Old",
            LastName = "Name",
            PhoneNumber = "111111111",
            OrganizationName = "Old Org",
            Description = "Old Description",
            Address = "Old Address",
            City = "Old City",
            IsActive = true,
            IsVerified = true,
            VerifiedAt = DateTime.UtcNow.AddDays(-10)
        };

        _orgRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingOrg);

        var updateData = new Organization
        {
            Id = 1,
            Email = "new@example.com",
            FirstName = "New",
            LastName = "Name",
            PhoneNumber = "222222222",
            OrganizationName = "New Org",
            Description = "New Description",
            Address = "New Address",
            City = "New City",
            IsActive = false,
            IsVerified = false // Should NOT be updated via this method
        };

        // Act
        var result = await sut.UpdateOrganizationAsync(1, updateData);

        // Assert
        result.Should().BeTrue();
        existingOrg.Email.Should().Be("new@example.com");
        existingOrg.FirstName.Should().Be("New");
        existingOrg.LastName.Should().Be("Name");
        existingOrg.PhoneNumber.Should().Be("222222222");
        existingOrg.OrganizationName.Should().Be("New Org");
        existingOrg.Description.Should().Be("New Description");
        existingOrg.Address.Should().Be("New Address");
        existingOrg.City.Should().Be("New City");
        existingOrg.IsActive.Should().BeFalse();
        
        // Business rule: IsVerified should NOT be updated via UpdateOrganizationAsync
        existingOrg.IsVerified.Should().BeTrue("IsVerified should only be changed via VerifyOrganizationAsync");
        
        _orgRepo.Verify(r => r.Update(existingOrg), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    #endregion

    #region DeleteOrganizationAsync Tests

    [Fact]
    public async Task DeleteOrganizationAsync_WhenOrganizationHasProjects_ThrowsInvalidOperationException()
    {
        // Arrange
        var sut = CreateSut();
        _projectRepo.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Project, bool>>>()))
            .ReturnsAsync(2); // Has 2 projects

        // Act
        Func<Task> act = async () => await sut.DeleteOrganizationAsync(1);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Organization cannot be deleted - it has active projects.");
    }

    [Fact]
    public async Task DeleteOrganizationAsync_WhenOrganizationNotFound_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        _projectRepo.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Project, bool>>>()))
            .ReturnsAsync(0);
        _orgRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Organization?)null);

        // Act
        var result = await sut.DeleteOrganizationAsync(1);

        // Assert
        result.Should().BeFalse();
        _orgRepo.Verify(r => r.Remove(It.IsAny<Organization>()), Times.Never);
    }

    [Fact]
    public async Task DeleteOrganizationAsync_WhenOrganizationFoundAndNoProjects_DeletesSuccessfully()
    {
        // Arrange
        var sut = CreateSut();
        var organization = new Organization { Id = 1, OrganizationName = "Test Org" };
        
        _projectRepo.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Project, bool>>>()))
            .ReturnsAsync(0);
        _orgRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(organization);

        // Act
        var result = await sut.DeleteOrganizationAsync(1);

        // Assert
        result.Should().BeTrue();
        _orgRepo.Verify(r => r.Remove(organization), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    #endregion

    #region GetOrganizationByIdAsync Tests

    [Fact]
    public async Task GetOrganizationByIdAsync_WhenOrganizationExists_ReturnsOrganization()
    {
        // Arrange
        var sut = CreateSut();
        var organization = new Organization { Id = 1, OrganizationName = "Test Org" };
        _orgRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(organization);

        // Act
        var result = await sut.GetOrganizationByIdAsync(1);

        // Assert
        result.Should().BeSameAs(organization);
        _orgRepo.Verify(r => r.GetByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetOrganizationByIdAsync_WhenOrganizationNotFound_ReturnsNull()
    {
        // Arrange
        var sut = CreateSut();
        _orgRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Organization?)null);

        // Act
        var result = await sut.GetOrganizationByIdAsync(1);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllOrganizationsAsync Tests

    [Fact]
    public async Task GetAllOrganizationsAsync_ReturnsAllOrganizations()
    {
        // Arrange
        var sut = CreateSut();
        var organizations = new List<Organization>
        {
            new() { Id = 1, OrganizationName = "Org 1" },
            new() { Id = 2, OrganizationName = "Org 2" },
            new() { Id = 3, OrganizationName = "Org 3" }
        };
        _orgRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(organizations);

        // Act
        var result = await sut.GetAllOrganizationsAsync();

        // Assert
        result.Should().BeEquivalentTo(organizations);
        result.Should().HaveCount(3);
        _orgRepo.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllOrganizationsAsync_WhenNoOrganizations_ReturnsEmptyCollection()
    {
        // Arrange
        var sut = CreateSut();
        _orgRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Organization>());

        // Act
        var result = await sut.GetAllOrganizationsAsync();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region VerifyOrganizationAsync Tests

    [Fact]
    public async Task VerifyOrganizationAsync_WhenOrganizationNotFound_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        _orgRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Organization?)null);

        // Act
        var result = await sut.VerifyOrganizationAsync(1);

        // Assert
        result.Should().BeFalse();
        _orgRepo.Verify(r => r.VerifyOrganizationAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task VerifyOrganizationAsync_WhenOrganizationAlreadyVerified_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        var organization = new Organization
        {
            Id = 1,
            OrganizationName = "Test Org",
            IsVerified = true,
            VerifiedAt = DateTime.UtcNow.AddDays(-5)
        };
        _orgRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(organization);

        // Act
        var result = await sut.VerifyOrganizationAsync(1);

        // Assert
        result.Should().BeFalse("organization is already verified");
        _orgRepo.Verify(r => r.VerifyOrganizationAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task VerifyOrganizationAsync_WhenOrganizationNotVerified_VerifiesSuccessfully()
    {
        // Arrange
        var sut = CreateSut();
        var organization = new Organization
        {
            Id = 1,
            OrganizationName = "Test Org",
            IsVerified = false
        };
        _orgRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(organization);
        _orgRepo.Setup(r => r.VerifyOrganizationAsync(1)).ReturnsAsync(true);

        // Act
        var result = await sut.VerifyOrganizationAsync(1);

        // Assert
        result.Should().BeTrue();
        _orgRepo.Verify(r => r.VerifyOrganizationAsync(1), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task VerifyOrganizationAsync_WhenRepositoryVerifyFails_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        var organization = new Organization
        {
            Id = 1,
            OrganizationName = "Test Org",
            IsVerified = false
        };
        _orgRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(organization);
        _orgRepo.Setup(r => r.VerifyOrganizationAsync(1)).ReturnsAsync(false);

        // Act
        var result = await sut.VerifyOrganizationAsync(1);

        // Assert
        result.Should().BeFalse();
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    #endregion

    #region UnverifyOrganizationAsync Tests

    [Fact]
    public async Task UnverifyOrganizationAsync_WhenOrganizationNotFound_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        _orgRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Organization?)null);

        // Act
        var result = await sut.UnverifyOrganizationAsync(1);

        // Assert
        result.Should().BeFalse();
        _orgRepo.Verify(r => r.Update(It.IsAny<Organization>()), Times.Never);
    }

    [Fact]
    public async Task UnverifyOrganizationAsync_WhenOrganizationFound_UnverifiesSuccessfully()
    {
        // Arrange
        var sut = CreateSut();
        var organization = new Organization
        {
            Id = 1,
            OrganizationName = "Test Org",
            IsVerified = true,
            VerifiedAt = DateTime.UtcNow.AddDays(-5)
        };
        _orgRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(organization);

        // Act
        var result = await sut.UnverifyOrganizationAsync(1);

        // Assert
        result.Should().BeTrue();
        organization.IsVerified.Should().BeFalse();
        organization.VerifiedAt.Should().BeNull();
        _orgRepo.Verify(r => r.Update(organization), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    #endregion

    #region CanCreateProjectAsync Tests

    [Fact]
    public async Task CanCreateProjectAsync_WhenOrganizationNotFound_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        _orgRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Organization?)null);

        // Act
        var result = await sut.CanCreateProjectAsync(1);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanCreateProjectAsync_WhenOrganizationNotVerified_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        var organization = new Organization
        {
            Id = 1,
            OrganizationName = "Test Org",
            IsVerified = false,
            IsActive = true
        };
        _orgRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(organization);

        // Act
        var result = await sut.CanCreateProjectAsync(1);

        // Assert
        result.Should().BeFalse("organization is not verified");
    }

    [Fact]
    public async Task CanCreateProjectAsync_WhenOrganizationNotActive_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        var organization = new Organization
        {
            Id = 1,
            OrganizationName = "Test Org",
            IsVerified = true,
            IsActive = false
        };
        _orgRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(organization);

        // Act
        var result = await sut.CanCreateProjectAsync(1);

        // Assert
        result.Should().BeFalse("organization is not active");
    }

    [Fact]
    public async Task CanCreateProjectAsync_WhenOrganizationVerifiedAndActive_ReturnsTrue()
    {
        // Arrange
        var sut = CreateSut();
        var organization = new Organization
        {
            Id = 1,
            OrganizationName = "Test Org",
            IsVerified = true,
            IsActive = true
        };
        _orgRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(organization);

        // Act
        var result = await sut.CanCreateProjectAsync(1);

        // Assert
        result.Should().BeTrue("organization is both verified and active");
    }

    #endregion

    #region OrganizationExistsAsync Tests

    [Fact]
    public async Task OrganizationExistsAsync_WhenOrganizationExists_ReturnsTrue()
    {
        // Arrange
        var sut = CreateSut();
        _orgRepo.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Organization, bool>>>()))
            .ReturnsAsync(true);

        // Act
        var result = await sut.OrganizationExistsAsync(1);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task OrganizationExistsAsync_WhenOrganizationDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        _orgRepo.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Organization, bool>>>()))
            .ReturnsAsync(false);

        // Act
        var result = await sut.OrganizationExistsAsync(1);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region CanDeleteOrganizationAsync Tests

    [Fact]
    public async Task CanDeleteOrganizationAsync_WhenOrganizationHasProjects_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        _projectRepo.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Project, bool>>>()))
            .ReturnsAsync(3);

        // Act
        var result = await sut.CanDeleteOrganizationAsync(1);

        // Assert
        result.Should().BeFalse("organization has 3 projects");
    }

    [Fact]
    public async Task CanDeleteOrganizationAsync_WhenOrganizationHasNoProjects_ReturnsTrue()
    {
        // Arrange
        var sut = CreateSut();
        _projectRepo.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Project, bool>>>()))
            .ReturnsAsync(0);

        // Act
        var result = await sut.CanDeleteOrganizationAsync(1);

        // Assert
        result.Should().BeTrue("organization has no projects");
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task CreateOrganizationAsync_WhenRepositoryThrows_RethrowsException()
    {
        // Arrange
        var sut = CreateSut();
        var org = new Organization { Email = "test@example.com", OrganizationName = "Test", City = "Test" };
        
        _orgRepo.Setup(r => r.AddAsync(It.IsAny<Organization>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        Func<Task> act = async () => await sut.CreateOrganizationAsync(org);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database error");
    }

    [Fact]
    public async Task UpdateOrganizationAsync_WhenRepositoryThrows_RethrowsException()
    {
        // Arrange
        var sut = CreateSut();
        var existing = new Organization { Id = 1, Email = "old@test.com", OrganizationName = "Old" };
        
        _orgRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _orgRepo.Setup(r => r.Update(It.IsAny<Organization>()))
            .Throws(new InvalidOperationException("Update failed"));

        // Act
        Func<Task> act = async () => await sut.UpdateOrganizationAsync(1, new Organization { Email = "new@test.com" });

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Update failed");
    }

    [Fact]
    public async Task GetOrganizationByIdAsync_WhenRepositoryThrows_RethrowsException()
    {
        // Arrange
        var sut = CreateSut();
        
        _orgRepo.Setup(r => r.GetByIdAsync(1))
            .ThrowsAsync(new InvalidOperationException("Get failed"));

        // Act
        Func<Task> act = async () => await sut.GetOrganizationByIdAsync(1);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Get failed");
    }

    [Fact]
    public async Task GetAllOrganizationsAsync_WhenRepositoryThrows_RethrowsException()
    {
        // Arrange
        var sut = CreateSut();
        
        _orgRepo.Setup(r => r.GetAllAsync())
            .ThrowsAsync(new InvalidOperationException("GetAll failed"));

        // Act
        Func<Task> act = async () => await sut.GetAllOrganizationsAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("GetAll failed");
    }

    [Fact]
    public async Task VerifyOrganizationAsync_WhenRepositoryThrows_RethrowsException()
    {
        // Arrange
        var sut = CreateSut();
        var org = new Organization { Id = 1, OrganizationName = "Test", IsVerified = false };
        
        _orgRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(org);
        _orgRepo.Setup(r => r.VerifyOrganizationAsync(1))
            .ThrowsAsync(new InvalidOperationException("Verify failed"));

        // Act
        Func<Task> act = async () => await sut.VerifyOrganizationAsync(1);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Verify failed");
    }

    #endregion
}
