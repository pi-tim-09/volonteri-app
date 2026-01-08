using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WebApp.Controllers.Api;
using WebApp.DTOs.Users;
using WebApp.Interfaces.Services;
using WebApp.Models;
using WebApp.Services;

namespace WebApp.UnitTests.Controllers;

public class UsersApiControllerTests
{
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
}
