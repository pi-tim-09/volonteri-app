using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WebApp.Common;
using WebApp.Controllers.Api;
using WebApp.DTOs.Users;
using WebApp.Interfaces.Services;
using WebApp.Models;
using WebApp.Services;
using WebApp.ViewModels;

namespace WebApp.UnitTests.Controllers;

public class UsersApiControllerTests
{
    private readonly Mock<IUserService> _userService = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ILogger<UsersApiController>> _logger = new();

    private UsersApiController CreateSut()
    {
        return new UsersApiController(_userService.Object, _passwordHasher.Object, _logger.Object);
    }

    

    [Fact]
    public async Task GetUser_WhenNotFound_Returns404()
    {
        var userService = new Mock<IUserService>();
        userService.Setup(s => s.GetUserByIdAsync(5)).ReturnsAsync((User?)null);

        var passwordHasher = new Mock<IPasswordHasher>();
        var logger = new Mock<ILogger<UsersApiController>>();

        var controller = new UsersApiController(userService.Object, passwordHasher.Object, logger.Object);

        var result = await controller.GetUser(5);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetUsers_WhenServiceReturns_ReturnsOk()
    {
        var userService = new Mock<IUserService>();
        userService.Setup(s => s.GetFilteredUsersAsync(null, null, null, 1, 10))
            .ReturnsAsync(new WebApp.ViewModels.UserFilterViewModel { Users = new List<User>(), TotalUsers = 0 });

        var passwordHasher = new Mock<IPasswordHasher>();
        var logger = new Mock<ILogger<UsersApiController>>();
        var controller = new UsersApiController(userService.Object, passwordHasher.Object, logger.Object);

        var result = await controller.GetUsers(null, null, null, 1, 10);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CreateUser_WhenEmailExists_ReturnsBadRequest()
    {
        var userService = new Mock<IUserService>();
        userService.Setup(s => s.EmailExistsAsync("dup@example.com")).ReturnsAsync(true);

        var passwordHasher = new Mock<IPasswordHasher>();
        var logger = new Mock<ILogger<UsersApiController>>();
        var controller = new UsersApiController(userService.Object, passwordHasher.Object, logger.Object);

        var req = new CreateUserRequest
        {
            Email = "dup@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            FirstName = "A",
            LastName = "B",
            PhoneNumber = "1",
            Role = UserRole.Volunteer
        };

        var result = await controller.CreateUser(req);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

   

    

    [Fact]
    public async Task GetUsers_WithAllFilters_ReturnsFilteredResults()
    {
        // Arrange
        var sut = CreateSut();
        var viewModel = new UserFilterViewModel
        {
            Users = new List<User> { new Volunteer { Id = 1, Role = UserRole.Volunteer } },
            TotalUsers = 1,
            PageNumber = 2,
            PageSize = 5,
            TotalPages = 3
        };
        _userService.Setup(s => s.GetFilteredUsersAsync("test", UserRole.Volunteer, true, 2, 5))
            .ReturnsAsync(viewModel);

        // Act
        var result = await sut.GetUsers("test", UserRole.Volunteer, true, 2, 5);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<UserListDto>>().Subject;
        response.Success.Should().BeTrue();
        response.Data!.TotalCount.Should().Be(1);
        response.Data.PageNumber.Should().Be(2);
        response.Data.PageSize.Should().Be(5);
        response.Data.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task GetUsers_WhenExceptionThrown_Returns500()
    {
        // Arrange
        var sut = CreateSut();
        _userService.Setup(s => s.GetFilteredUsersAsync(It.IsAny<string>(), It.IsAny<UserRole?>(), 
                It.IsAny<bool?>(), It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await sut.GetUsers(null, null, null, 1, 10);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        var response = statusCodeResult.Value.Should().BeOfType<ApiResponse>().Subject;
        response.Success.Should().BeFalse();
        response.Message.Should().Be("Failed to retrieve users");
    }

   

    

    [Fact]
    public async Task GetUser_WhenUserExists_Returns200WithUser()
    {
        // Arrange
        var sut = CreateSut();
        var user = new Volunteer
        {
            Id = 1,
            Email = "test@test.com",
            FirstName = "Test",
            LastName = "User",
            Role = UserRole.Volunteer,
            IsActive = true
        };
        _userService.Setup(s => s.GetUserByIdAsync(1)).ReturnsAsync(user);

        // Act
        var result = await sut.GetUser(1);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<UserDto>>().Subject;
        response.Success.Should().BeTrue();
        response.Data!.Id.Should().Be(1);
        response.Data.Email.Should().Be("test@test.com");
    }

    [Fact]
    public async Task GetUser_WhenExceptionThrown_Returns500()
    {
        // Arrange
        var sut = CreateSut();
        _userService.Setup(s => s.GetUserByIdAsync(1))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await sut.GetUser(1);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
    }

    

   

    [Fact]
    public async Task CreateUser_WhenModelStateInvalid_Returns400()
    {
        // Arrange
        var sut = CreateSut();
        sut.ModelState.AddModelError("Email", "Invalid email");
        var request = new CreateUserRequest { Email = "invalid", Password = "pass", ConfirmPassword = "pass" };

        // Act
        var result = await sut.CreateUser(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequestResult.Value.Should().BeOfType<ApiResponse>().Subject;
        response.Success.Should().BeFalse();
        response.Message.Should().Be("Validation failed");
        response.Errors.Should().Contain("Invalid email");
    }

    [Fact]
    public async Task CreateUser_WhenCreatingVolunteer_HashesPasswordAndCreates()
    {
        // Arrange 
        var sut = CreateSut();
        var request = new CreateUserRequest
        {
            Email = "volunteer@test.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            FirstName = "Vol",
            LastName = "Unteer",
            PhoneNumber = "123",
            Role = UserRole.Volunteer
        };

        _userService.Setup(s => s.EmailExistsAsync(request.Email)).ReturnsAsync(false);
        _passwordHasher.Setup(p => p.HashPassword(request.Password)).Returns("HASHED_PWD");
        _userService.Setup(s => s.CreateUserAsync(It.IsAny<UserVM>()))
            .ReturnsAsync(new Volunteer { Id = 1, Email = request.Email, PasswordHash = "HASHED_PWD" });

        // Act
        var result = await sut.CreateUser(request);

        // Assert
        var createdAtActionResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdAtActionResult.StatusCode.Should().Be(201);
        var response = createdAtActionResult.Value.Should().BeOfType<ApiResponse<UserDto>>().Subject;
        response.Success.Should().BeTrue();
        response.Message.Should().Be("User created successfully");
        
        _passwordHasher.Verify(p => p.HashPassword(request.Password), Times.Once);
        _userService.Verify(s => s.CreateUserAsync(It.Is<UserVM>(vm => 
            vm.Email == request.Email && 
            vm.Role == UserRole.Volunteer)), Times.Once);
    }

    [Fact]
    public async Task CreateUser_WhenCreatingOrganization_HashesPasswordAndCreates()
    {
        // Arrange 
        var sut = CreateSut();
        var request = new CreateUserRequest
        {
            Email = "org@test.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            FirstName = "Org",
            LastName = "Name",
            PhoneNumber = "123",
            Role = UserRole.Organization
        };

        _userService.Setup(s => s.EmailExistsAsync(request.Email)).ReturnsAsync(false);
        _passwordHasher.Setup(p => p.HashPassword(request.Password)).Returns("HASHED_PWD");
        _userService.Setup(s => s.CreateUserAsync(It.IsAny<UserVM>()))
            .ReturnsAsync(new Organization { Id = 1, Email = request.Email, PasswordHash = "HASHED_PWD", IsVerified = false });

        // Act
        var result = await sut.CreateUser(request);

        // Assert
        var createdAtActionResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdAtActionResult.StatusCode.Should().Be(201);
        
        _userService.Verify(s => s.CreateUserAsync(It.Is<UserVM>(vm => 
            vm.Role == UserRole.Organization)), Times.Once);
    }

    [Fact]
    public async Task CreateUser_WhenCreatingAdmin_HashesPasswordAndCreates()
    {
        // Arrange 
        var sut = CreateSut();
        var request = new CreateUserRequest
        {
            Email = "admin@test.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            FirstName = "Admin",
            LastName = "User",
            PhoneNumber = "123",
            Role = UserRole.Admin
        };

        _userService.Setup(s => s.EmailExistsAsync(request.Email)).ReturnsAsync(false);
        _passwordHasher.Setup(p => p.HashPassword(request.Password)).Returns("HASHED_PWD");
        _userService.Setup(s => s.CreateUserAsync(It.IsAny<UserVM>()))
            .ReturnsAsync(new Admin { Id = 1, Email = request.Email, PasswordHash = "HASHED_PWD" });

        // Act
        var result = await sut.CreateUser(request);

        // Assert
        var createdAtActionResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdAtActionResult.StatusCode.Should().Be(201);
        
        _userService.Verify(s => s.CreateUserAsync(It.Is<UserVM>(vm => 
            vm.Role == UserRole.Admin)), Times.Once);
    }

    [Fact]
    public async Task CreateUser_WhenExceptionThrown_Returns500()
    {
        // Arrange 
        var sut = CreateSut();
        var request = new CreateUserRequest
        {
            Email = "test@test.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            FirstName = "Test",
            LastName = "User",
            PhoneNumber = "123",
            Role = UserRole.Volunteer
        };

        _userService.Setup(s => s.EmailExistsAsync(request.Email)).ReturnsAsync(false);
        _passwordHasher.Setup(p => p.HashPassword(It.IsAny<string>())).Returns("HASH");
        _userService.Setup(s => s.CreateUserAsync(It.IsAny<UserVM>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await sut.CreateUser(request);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
    }

    

    

    [Fact]
    public async Task UpdateUser_WhenModelStateInvalid_Returns400()
    {
        // Arrange
        var sut = CreateSut();
        sut.ModelState.AddModelError("Email", "Required");
        var request = new UpdateUserRequest
        {
            Email = "",
            FirstName = "Test",
            LastName = "User",
            PhoneNumber = "123",
            Role = UserRole.Volunteer,
            IsActive = true
        };

        // Act
        var result = await sut.UpdateUser(1, request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequestResult.Value.Should().BeOfType<ApiResponse>().Subject;
        response.Success.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateUser_WhenUserNotFound_Returns404()
    {
        // Arrange 
        var sut = CreateSut();
        var request = new UpdateUserRequest
        {
            Email = "test@test.com",
            FirstName = "Test",
            LastName = "User",
            PhoneNumber = "123",
            Role = UserRole.Volunteer,
            IsActive = true
        };
        _userService.Setup(s => s.UpdateUserAsync(999, It.IsAny<UserVM>())).ReturnsAsync(false);

        // Act
        var result = await sut.UpdateUser(999, request);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var response = notFoundResult.Value.Should().BeOfType<ApiResponse>().Subject;
        response.Success.Should().BeFalse();
        response.Message.Should().Contain("User with ID 999 not found");
    }

    [Fact]
    public async Task UpdateUser_WhenValid_Returns200()
    {
        // Arrange 
        var sut = CreateSut();
        var request = new UpdateUserRequest
        {
            Email = "updated@test.com",
            FirstName = "Updated",
            LastName = "User",
            PhoneNumber = "987",
            Role = UserRole.Volunteer,
            IsActive = false
        };
        _userService.Setup(s => s.UpdateUserAsync(1, It.IsAny<UserVM>())).ReturnsAsync(true);

        // Act
        var result = await sut.UpdateUser(1, request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse>().Subject;
        response.Success.Should().BeTrue();
        response.Message.Should().Be("User updated successfully");
    }

    [Fact]
    public async Task UpdateUser_WhenExceptionThrown_Returns500()
    {
        // Arrange
        var sut = CreateSut();
        var request = new UpdateUserRequest
        {
            Email = "test@test.com",
            FirstName = "Test",
            LastName = "User",
            PhoneNumber = "123",
            Role = UserRole.Volunteer,
            IsActive = true
        };
        _userService.Setup(s => s.UpdateUserAsync(1, It.IsAny<UserVM>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await sut.UpdateUser(1, request);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
    }

    

    

    [Fact]
    public async Task DeleteUser_WhenUserNotFound_Returns404()
    {
        // Arrange 
        var sut = CreateSut();
        _userService.Setup(s => s.GetUserByIdAsync(999)).ReturnsAsync((User?)null);

        // Act
        var result = await sut.DeleteUser(999);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var response = notFoundResult.Value.Should().BeOfType<ApiResponse>().Subject;
        response.Success.Should().BeFalse();
        response.Message.Should().Contain("User with ID 999 not found");
        
        _userService.Verify(s => s.DeleteUserAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteUser_WhenDeleteFails_Returns404()
    {
        // Arrange 
        var sut = CreateSut();
        _userService.Setup(s => s.GetUserByIdAsync(1)).ReturnsAsync(new Volunteer { Id = 1 });
        _userService.Setup(s => s.DeleteUserAsync(1)).ReturnsAsync(false);

        // Act
        var result = await sut.DeleteUser(1);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var response = notFoundResult.Value.Should().BeOfType<ApiResponse>().Subject;
        response.Success.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteUser_WhenValid_Returns200()
    {
        // Arrange 
        var sut = CreateSut();
        _userService.Setup(s => s.GetUserByIdAsync(1)).ReturnsAsync(new Volunteer { Id = 1 });
        _userService.Setup(s => s.DeleteUserAsync(1)).ReturnsAsync(true);

        // Act
        var result = await sut.DeleteUser(1);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse>().Subject;
        response.Success.Should().BeTrue();
        response.Message.Should().Be("User deleted successfully");
    }

    [Fact]
    public async Task DeleteUser_WhenExceptionThrown_Returns500()
    {
        // Arrange
        var sut = CreateSut();
        _userService.Setup(s => s.GetUserByIdAsync(1))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await sut.DeleteUser(1);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
    }

   

    

    [Fact]
    public async Task ActivateUser_WhenUserNotFound_Returns404()
    {
        // Arrange 
        var sut = CreateSut();
        _userService.Setup(s => s.ActivateUserAsync(999)).ReturnsAsync(false);

        // Act
        var result = await sut.ActivateUser(999);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var response = notFoundResult.Value.Should().BeOfType<ApiResponse>().Subject;
        response.Success.Should().BeFalse();
        response.Message.Should().Contain("User with ID 999 not found");
    }

    [Fact]
    public async Task ActivateUser_WhenValid_Returns200()
    {
        // Arrange 
        var sut = CreateSut();
        _userService.Setup(s => s.ActivateUserAsync(1)).ReturnsAsync(true);

        // Act
        var result = await sut.ActivateUser(1);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse>().Subject;
        response.Success.Should().BeTrue();
        response.Message.Should().Be("User activated successfully");
    }

    [Fact]
    public async Task ActivateUser_WhenExceptionThrown_Returns500()
    {
        // Arrange
        var sut = CreateSut();
        _userService.Setup(s => s.ActivateUserAsync(1))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await sut.ActivateUser(1);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
    }

    

    

    [Fact]
    public async Task DeactivateUser_WhenUserNotFound_Returns404()
    {
        // Arrange 
        var sut = CreateSut();
        _userService.Setup(s => s.DeactivateUserAsync(999)).ReturnsAsync(false);

        // Act
        var result = await sut.DeactivateUser(999);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var response = notFoundResult.Value.Should().BeOfType<ApiResponse>().Subject;
        response.Success.Should().BeFalse();
        response.Message.Should().Contain("User with ID 999 not found");
    }

    [Fact]
    public async Task DeactivateUser_WhenValid_Returns200()
    {
        // Arrange 
        var sut = CreateSut();
        _userService.Setup(s => s.DeactivateUserAsync(1)).ReturnsAsync(true);

        // Act
        var result = await sut.DeactivateUser(1);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse>().Subject;
        response.Success.Should().BeTrue();
        response.Message.Should().Be("User deactivated successfully");
    }

    [Fact]
    public async Task DeactivateUser_WhenExceptionThrown_Returns500()
    {
        // Arrange
        var sut = CreateSut();
        _userService.Setup(s => s.DeactivateUserAsync(1))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await sut.DeactivateUser(1);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
    }

    
}
