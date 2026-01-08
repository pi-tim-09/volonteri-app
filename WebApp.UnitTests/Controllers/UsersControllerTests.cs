using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WebApp.Controllers;
using WebApp.Interfaces.Services;
using WebApp.Models;
using WebApp.ViewModels;

namespace WebApp.UnitTests.Controllers;

public class UsersControllerTests
{
    [Fact]
    public async Task Index_WhenServiceReturnsViewModel_ReturnsView()
    {
        var userService = new Mock<IUserService>();
        userService
            .Setup(s => s.GetFilteredUsersAsync(null, null, null, 1, 10))
            .ReturnsAsync(new UserFilterViewModel());

        var logger = new Mock<ILogger<UsersController>>();
        var controller = new UsersController(userService.Object, logger.Object);

        var result = await controller.Index(null, null, null, 1, 10);

        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Details_WhenUserDoesNotExist_ReturnsNotFound()
    {
        var userService = new Mock<IUserService>();
        userService.Setup(s => s.GetUserByIdAsync(123)).ReturnsAsync((User?)null);
        var logger = new Mock<ILogger<UsersController>>();

        var controller = new UsersController(userService.Object, logger.Object);

        var result = await controller.Details(123);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Create_Post_WhenModelStateInvalid_ReturnsViewWithModel()
    {
        var userService = new Mock<IUserService>();
        var logger = new Mock<ILogger<UsersController>>();
        var controller = new UsersController(userService.Object, logger.Object);

        controller.ModelState.AddModelError("Email", "Required");

        var vm = new UserVM { Email = "x@x.com", FirstName = "A", LastName = "B", PhoneNumber = "1", Role = UserRole.Volunteer, IsActive = true };

        var result = await controller.Create(vm);

        var view = result.Should().BeOfType<ViewResult>().Subject;
        view.Model.Should().BeSameAs(vm);
    }

    [Fact]
    public async Task Create_Post_WhenEmailExists_AddsModelErrorAndReturnsView()
    {
        var userService = new Mock<IUserService>();
        userService.Setup(s => s.EmailExistsAsync("dup@example.com")).ReturnsAsync(true);

        var logger = new Mock<ILogger<UsersController>>();
        var controller = new UsersController(userService.Object, logger.Object);

        var vm = new UserVM { Email = "dup@example.com", FirstName = "A", LastName = "B", PhoneNumber = "1", Role = UserRole.Volunteer, IsActive = true };

        var result = await controller.Create(vm);

        result.Should().BeOfType<ViewResult>();
        controller.ModelState.ContainsKey("Email").Should().BeTrue();
    }

    [Fact]
    public async Task Edit_Post_WhenIdMismatch_ReturnsNotFound()
    {
        var userService = new Mock<IUserService>();
        var logger = new Mock<ILogger<UsersController>>();
        var controller = new UsersController(userService.Object, logger.Object);

        var vm = new UserVM { Id = 2, Email = "x@x.com", FirstName = "A", LastName = "B", PhoneNumber = "1", Role = UserRole.Volunteer, IsActive = true };

        var result = await controller.Edit(1, vm);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Edit_Post_WhenServiceReturnsFalse_ReturnsNotFound()
    {
        var userService = new Mock<IUserService>();
        userService.Setup(s => s.UpdateUserAsync(5, It.IsAny<UserVM>())).ReturnsAsync(false);

        var logger = new Mock<ILogger<UsersController>>();
        var controller = new UsersController(userService.Object, logger.Object);

        var vm = new UserVM { Id = 5, Email = "x@x.com", FirstName = "A", LastName = "B", PhoneNumber = "1", Role = UserRole.Volunteer, IsActive = true };

        var result = await controller.Edit(5, vm);

        result.Should().BeOfType<NotFoundResult>();
    }
}
