using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WebApp.Common;
using WebApp.Controllers.Api;
using WebApp.DTOs.Organizations;
using WebApp.Interfaces.Services;
using WebApp.Models;
using WebApp.Services;

namespace WebApp.UnitTests.Controllers.Api;

public class OrganizationsApiControllerTests
{
    private readonly Mock<IOrganizationService> _organizationService = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ILogger<OrganizationsApiController>> _logger = new();

    private OrganizationsApiController CreateSut()
    {
        return new OrganizationsApiController(
            _organizationService.Object,
            _passwordHasher.Object,
            _logger.Object);
    }

    

    [Fact]
    public async Task GetOrganizations_WhenOrganizationsExist_Returns200WithOrganizations()
    {
        // Arrange
        var sut = CreateSut();
        var organizations = new List<Organization>
        {
            new() { Id = 1, Email = "org1@example.com", FirstName = "Org", LastName = "One", OrganizationName = "Org 1", City = "City1" },
            new() { Id = 2, Email = "org2@example.com", FirstName = "Org", LastName = "Two", OrganizationName = "Org 2", City = "City2" }
        };
        _organizationService.Setup(s => s.GetAllOrganizationsAsync()).ReturnsAsync(organizations);

        // Act
        var result = await sut.GetOrganizations();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        
        var response = okResult.Value.Should().BeOfType<ApiResponse<OrganizationListDto>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Organizations.Should().HaveCount(2);
        response.Data.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetOrganizations_WhenNoOrganizations_Returns200WithEmptyList()
    {
        // Arrange
        var sut = CreateSut();
        _organizationService.Setup(s => s.GetAllOrganizationsAsync()).ReturnsAsync(new List<Organization>());

        // Act
        var result = await sut.GetOrganizations();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<OrganizationListDto>>().Subject;
        response.Data!.Organizations.Should().BeEmpty();
        response.Data.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetOrganizations_WhenExceptionThrown_Returns500()
    {
        // Arrange
        var sut = CreateSut();
        _organizationService.Setup(s => s.GetAllOrganizationsAsync()).ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await sut.GetOrganizations();

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        
        var response = statusCodeResult.Value.Should().BeOfType<ApiResponse>().Subject;
        response.Success.Should().BeFalse();
        response.Message.Should().Be("Failed to retrieve organizations");
    }

    

    

    [Fact]
    public async Task GetOrganization_WhenOrganizationExists_Returns200WithOrganization()
    {
        // Arrange
        var sut = CreateSut();
        var organization = new Organization
        {
            Id = 1,
            Email = "org@example.com",
            FirstName = "Test",
            LastName = "Org",
            PhoneNumber = "123456789",
            OrganizationName = "Test Organization",
            Description = "Test Description",
            Address = "123 Main St",
            City = "Test City",
            IsVerified = true,
            VerifiedAt = DateTime.UtcNow.AddDays(-10),
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            IsActive = true
        };
        _organizationService.Setup(s => s.GetOrganizationByIdAsync(1)).ReturnsAsync(organization);

        // Act
        var result = await sut.GetOrganization(1);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<OrganizationDto>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Id.Should().Be(1);
        response.Data.OrganizationName.Should().Be("Test Organization");
        response.Data.IsVerified.Should().BeTrue();
    }

    [Fact]
    public async Task GetOrganization_WhenOrganizationNotFound_Returns404()
    {
        // Arrange
        var sut = CreateSut();
        _organizationService.Setup(s => s.GetOrganizationByIdAsync(999)).ReturnsAsync((Organization?)null);

        // Act
        var result = await sut.GetOrganization(999);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var response = notFoundResult.Value.Should().BeOfType<ApiResponse>().Subject;
        response.Success.Should().BeFalse();
        response.Message.Should().Contain("Organization with ID 999 not found");
    }

    [Fact]
    public async Task GetOrganization_WhenExceptionThrown_Returns500()
    {
        // Arrange
        var sut = CreateSut();
        _organizationService.Setup(s => s.GetOrganizationByIdAsync(1)).ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await sut.GetOrganization(1);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
    }

    

    

    [Fact]
    public async Task CreateOrganization_WhenModelStateInvalid_Returns400()
    {
        // Arrange
        var sut = CreateSut();
        sut.ModelState.AddModelError("Email", "Email is required");

        var request = new CreateOrganizationRequest
        {
            Email = "",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            FirstName = "Test",
            LastName = "Org",
            PhoneNumber = "123456789",
            OrganizationName = "Test Org",
            City = "Test City"
        };

        // Act
        var result = await sut.CreateOrganization(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequestResult.Value.Should().BeOfType<ApiResponse>().Subject;
        response.Success.Should().BeFalse();
        response.Message.Should().Be("Validation failed");
        response.Errors.Should().Contain("Email is required");
    }

    [Fact]
    public async Task CreateOrganization_WhenValid_HashesPasswordAndCreatesOrganization()
    {
        // Arrange
        var sut = CreateSut();
        var request = new CreateOrganizationRequest
        {
            Email = "neworg@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            FirstName = "New",
            LastName = "Org",
            PhoneNumber = "123456789",
            OrganizationName = "New Organization",
            Description = "Test Description",
            Address = "123 Main St",
            City = "Test City"
        };

        var hashedPassword = "hashed_password_123";
        _passwordHasher.Setup(p => p.HashPassword(request.Password)).Returns(hashedPassword);

        var createdOrg = new Organization
        {
            Id = 1,
            Email = request.Email,
            PasswordHash = hashedPassword,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            OrganizationName = request.OrganizationName,
            Description = request.Description,
            Address = request.Address,
            City = request.City,
            IsActive = true,
            IsVerified = false,
            CreatedAt = DateTime.UtcNow
        };

        _organizationService.Setup(s => s.CreateOrganizationAsync(It.IsAny<Organization>()))
            .ReturnsAsync(createdOrg);

        // Act
        var result = await sut.CreateOrganization(request);

        // Assert
        var createdAtActionResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdAtActionResult.StatusCode.Should().Be(201);
        createdAtActionResult.ActionName.Should().Be(nameof(OrganizationsApiController.GetOrganization));
        
        var response = createdAtActionResult.Value.Should().BeOfType<ApiResponse<OrganizationDto>>().Subject;
        response.Success.Should().BeTrue();
        response.Message.Should().Be("Organization created successfully");
        response.Data!.Id.Should().Be(1);
        response.Data.Email.Should().Be(request.Email);

        _passwordHasher.Verify(p => p.HashPassword(request.Password), Times.Once);
        _organizationService.Verify(s => s.CreateOrganizationAsync(It.Is<Organization>(o =>
            o.Email == request.Email &&
            o.PasswordHash == hashedPassword &&
            o.IsActive == true &&
            o.IsVerified == false
        )), Times.Once);
    }

    [Fact]
    public async Task CreateOrganization_WhenExceptionThrown_Returns500()
    {
        // Arrange
        var sut = CreateSut();
        var request = new CreateOrganizationRequest
        {
            Email = "neworg@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            FirstName = "New",
            LastName = "Org",
            PhoneNumber = "123456789",
            OrganizationName = "New Organization",
            City = "Test City"
        };

        _passwordHasher.Setup(p => p.HashPassword(It.IsAny<string>())).Returns("hashed");
        _organizationService.Setup(s => s.CreateOrganizationAsync(It.IsAny<Organization>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await sut.CreateOrganization(request);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
    }

   

    

    [Fact]
    public async Task UpdateOrganization_WhenModelStateInvalid_Returns400()
    {
        // Arrange
        var sut = CreateSut();
        sut.ModelState.AddModelError("Email", "Invalid email format");

        var request = new UpdateOrganizationRequest
        {
            Email = "invalid-email",
            FirstName = "Updated",
            LastName = "Org",
            PhoneNumber = "987654321",
            OrganizationName = "Updated Org",
            City = "Updated City",
            IsActive = true
        };

        // Act
        var result = await sut.UpdateOrganization(1, request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequestResult.Value.Should().BeOfType<ApiResponse>().Subject;
        response.Success.Should().BeFalse();
        response.Message.Should().Be("Validation failed");
    }

    [Fact]
    public async Task UpdateOrganization_WhenOrganizationNotFound_Returns404()
    {
        // Arrange
        var sut = CreateSut();
        var request = new UpdateOrganizationRequest
        {
            Email = "updated@example.com",
            FirstName = "Updated",
            LastName = "Org",
            PhoneNumber = "987654321",
            OrganizationName = "Updated Org",
            City = "Updated City",
            IsActive = true
        };

        _organizationService.Setup(s => s.UpdateOrganizationAsync(999, It.IsAny<Organization>()))
            .ReturnsAsync(false);

        // Act
        var result = await sut.UpdateOrganization(999, request);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var response = notFoundResult.Value.Should().BeOfType<ApiResponse>().Subject;
        response.Success.Should().BeFalse();
        response.Message.Should().Contain("Organization with ID 999 not found");
    }

    [Fact]
    public async Task UpdateOrganization_WhenValid_UpdatesSuccessfully()
    {
        // Arrange
        var sut = CreateSut();
        var request = new UpdateOrganizationRequest
        {
            Email = "updated@example.com",
            FirstName = "Updated",
            LastName = "Org",
            PhoneNumber = "987654321",
            OrganizationName = "Updated Organization",
            Description = "Updated Description",
            Address = "456 New St",
            City = "Updated City",
            IsActive = false
        };

        _organizationService.Setup(s => s.UpdateOrganizationAsync(1, It.IsAny<Organization>()))
            .ReturnsAsync(true);

        // Act
        var result = await sut.UpdateOrganization(1, request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse>().Subject;
        response.Success.Should().BeTrue();
        response.Message.Should().Be("Organization updated successfully");

        _organizationService.Verify(s => s.UpdateOrganizationAsync(1, It.Is<Organization>(o =>
            o.Id == 1 &&
            o.Email == request.Email &&
            o.OrganizationName == request.OrganizationName &&
            o.IsActive == false
        )), Times.Once);
    }

    [Fact]
    public async Task UpdateOrganization_WhenExceptionThrown_Returns500()
    {
        // Arrange
        var sut = CreateSut();
        var request = new UpdateOrganizationRequest
        {
            Email = "updated@example.com",
            FirstName = "Updated",
            LastName = "Org",
            PhoneNumber = "987654321",
            OrganizationName = "Updated Org",
            City = "Updated City",
            IsActive = true
        };

        _organizationService.Setup(s => s.UpdateOrganizationAsync(1, It.IsAny<Organization>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await sut.UpdateOrganization(1, request);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
    }

    

    

    [Fact]
    public async Task DeleteOrganization_WhenOrganizationNotFound_Returns404()
    {
        // Arrange
        var sut = CreateSut();
        _organizationService.Setup(s => s.GetOrganizationByIdAsync(999)).ReturnsAsync((Organization?)null);

        // Act
        var result = await sut.DeleteOrganization(999);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var response = notFoundResult.Value.Should().BeOfType<ApiResponse>().Subject;
        response.Success.Should().BeFalse();
        response.Message.Should().Contain("Organization with ID 999 not found");
        
        _organizationService.Verify(s => s.DeleteOrganizationAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteOrganization_WhenDeleteFails_Returns404()
    {
        // Arrange
        var sut = CreateSut();
        var organization = new Organization { Id = 1, OrganizationName = "Test Org" };
        _organizationService.Setup(s => s.GetOrganizationByIdAsync(1)).ReturnsAsync(organization);
        _organizationService.Setup(s => s.DeleteOrganizationAsync(1)).ReturnsAsync(false);

        // Act
        var result = await sut.DeleteOrganization(1);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var response = notFoundResult.Value.Should().BeOfType<ApiResponse>().Subject;
        response.Success.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteOrganization_WhenValid_DeletesSuccessfully()
    {
        // Arrange
        var sut = CreateSut();
        var organization = new Organization { Id = 1, OrganizationName = "Test Org" };
        _organizationService.Setup(s => s.GetOrganizationByIdAsync(1)).ReturnsAsync(organization);
        _organizationService.Setup(s => s.DeleteOrganizationAsync(1)).ReturnsAsync(true);

        // Act
        var result = await sut.DeleteOrganization(1);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse>().Subject;
        response.Success.Should().BeTrue();
        response.Message.Should().Be("Organization deleted successfully");
        
        _organizationService.Verify(s => s.DeleteOrganizationAsync(1), Times.Once);
    }

    [Fact]
    public async Task DeleteOrganization_WhenExceptionThrown_Returns500()
    {
        // Arrange
        var sut = CreateSut();
        _organizationService.Setup(s => s.GetOrganizationByIdAsync(1)).ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await sut.DeleteOrganization(1);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
    }

    

    

    [Fact]
    public async Task VerifyOrganization_WhenOrganizationNotFound_Returns404()
    {
        // Arrange
        var sut = CreateSut();
        _organizationService.Setup(s => s.VerifyOrganizationAsync(999)).ReturnsAsync(false);

        // Act
        var result = await sut.VerifyOrganization(999);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var response = notFoundResult.Value.Should().BeOfType<ApiResponse>().Subject;
        response.Success.Should().BeFalse();
        response.Message.Should().Contain("Organization with ID 999 not found");
    }

    [Fact]
    public async Task VerifyOrganization_WhenValid_VerifiesSuccessfully()
    {
        // Arrange
        var sut = CreateSut();
        _organizationService.Setup(s => s.VerifyOrganizationAsync(1)).ReturnsAsync(true);

        // Act
        var result = await sut.VerifyOrganization(1);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse>().Subject;
        response.Success.Should().BeTrue();
        response.Message.Should().Be("Organization verified successfully");
        
        _organizationService.Verify(s => s.VerifyOrganizationAsync(1), Times.Once);
    }

    [Fact]
    public async Task VerifyOrganization_WhenExceptionThrown_Returns500()
    {
        // Arrange
        var sut = CreateSut();
        _organizationService.Setup(s => s.VerifyOrganizationAsync(1)).ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await sut.VerifyOrganization(1);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
    }

    

    

    [Fact]
    public async Task UnverifyOrganization_WhenOrganizationNotFound_Returns404()
    {
        // Arrange
        var sut = CreateSut();
        _organizationService.Setup(s => s.UnverifyOrganizationAsync(999)).ReturnsAsync(false);

        // Act
        var result = await sut.UnverifyOrganization(999);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var response = notFoundResult.Value.Should().BeOfType<ApiResponse>().Subject;
        response.Success.Should().BeFalse();
        response.Message.Should().Contain("Organization with ID 999 not found");
    }

    [Fact]
    public async Task UnverifyOrganization_WhenValid_UnverifiesSuccessfully()
    {
        // Arrange
        var sut = CreateSut();
        _organizationService.Setup(s => s.UnverifyOrganizationAsync(1)).ReturnsAsync(true);

        // Act
        var result = await sut.UnverifyOrganization(1);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse>().Subject;
        response.Success.Should().BeTrue();
        response.Message.Should().Be("Organization unverified successfully");
        
        _organizationService.Verify(s => s.UnverifyOrganizationAsync(1), Times.Once);
    }

    [Fact]
    public async Task UnverifyOrganization_WhenExceptionThrown_Returns500()
    {
        // Arrange
        var sut = CreateSut();
        _organizationService.Setup(s => s.UnverifyOrganizationAsync(1)).ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await sut.UnverifyOrganization(1);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
    }

    
}
