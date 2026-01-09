using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using WebApp.Controllers;
using WebApp.Interfaces.Services;
using WebApp.Models;
using WebApp.ViewModels;

namespace WebApp.UnitTests.Controllers;

public class UsersControllerTests
{
    private readonly Mock<IUserService> _userService = new();
    private readonly Mock<ILogger<UsersController>> _logger = new();

    private UsersController CreateSut()
    {
        var controller = new UsersController(_userService.Object, _logger.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        };
        return controller;
    }

    #region Existing Tests

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

    #endregion

    #region Additional Index Tests

    [Fact]
    public async Task Index_WhenExceptionThrown_ReturnsViewWithEmptyModel()
    {
        // Arrange
        var sut = CreateSut();
        _userService.Setup(s => s.GetFilteredUsersAsync(It.IsAny<string>(), It.IsAny<UserRole?>(), 
                It.IsAny<bool?>(), It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await sut.Index(null, null, null, 1, 10);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().BeOfType<UserFilterViewModel>();
        sut.TempData["ErrorMessage"].Should().NotBeNull();
    }

    [Fact]
    public async Task Index_WithAllFilters_PassesCorrectParameters()
    {
        // Arrange
        var sut = CreateSut();
        var viewModel = new UserFilterViewModel();
        _userService.Setup(s => s.GetFilteredUsersAsync("search", UserRole.Volunteer, true, 2, 20))
            .ReturnsAsync(viewModel);

        // Act
        await sut.Index("search", UserRole.Volunteer, true, 2, 20);

        // Assert
        _userService.Verify(s => s.GetFilteredUsersAsync("search", UserRole.Volunteer, true, 2, 20), Times.Once);
    }

    #endregion

    #region Additional Details Tests

    [Fact]
    public async Task Details_WhenUserExists_ReturnsViewWithUser()
    {
        // Arrange
        var sut = CreateSut();
        var user = new Volunteer { Id = 1, FirstName = "Test", LastName = "User" };
        _userService.Setup(s => s.GetUserByIdAsync(1)).ReturnsAsync(user);

        // Act
        var result = await sut.Details(1);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().BeSameAs(user);
    }

    [Fact]
    public async Task Details_WhenExceptionThrown_RedirectsToIndex()
    {
        // Arrange
        var sut = CreateSut();
        _userService.Setup(s => s.GetUserByIdAsync(1))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await sut.Details(1);

        // Assert
        var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirectResult.ActionName.Should().Be("Index");
        sut.TempData["ErrorMessage"].Should().NotBeNull();
    }

    #endregion

    #region Additional Create Tests

    [Fact]
    public void Create_Get_ReturnsView()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = sut.Create();

        // Assert
        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Create_Post_WhenValid_RedirectsToIndex()
    {
        // Arrange
        var sut = CreateSut();
        var userVm = new UserVM { Email = "new@test.com", FirstName = "New", LastName = "User" };
        var createdUser = new Volunteer { Id = 1, FirstName = "New", LastName = "User" };
        
        _userService.Setup(s => s.EmailExistsAsync("new@test.com")).ReturnsAsync(false);
        _userService.Setup(s => s.CreateUserAsync(userVm)).ReturnsAsync(createdUser);

        // Act
        var result = await sut.Create(userVm);

        // Assert
        var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirectResult.ActionName.Should().Be("Index");
        sut.TempData["SuccessMessage"].Should().NotBeNull();
    }

    [Fact]
    public async Task Create_Post_WhenExceptionThrown_ReturnsViewWithModelError()
    {
        // Arrange
        var sut = CreateSut();
        var userVm = new UserVM { Email = "new@test.com", FirstName = "New", LastName = "User" };
        
        _userService.Setup(s => s.EmailExistsAsync("new@test.com")).ReturnsAsync(false);
        _userService.Setup(s => s.CreateUserAsync(userVm))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await sut.Create(userVm);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        sut.ModelState.ErrorCount.Should().BeGreaterThan(0);
    }

    #endregion

    #region Additional Edit Tests

    [Fact]
    public async Task Edit_Get_WhenUserExists_ReturnsViewWithUserVM()
    {
        // Arrange
        var sut = CreateSut();
        var user = new Volunteer 
        { 
            Id = 1, 
            Email = "test@test.com",
            FirstName = "Test", 
            LastName = "User",
            PhoneNumber = "123",
            Role = UserRole.Volunteer,
            IsActive = true
        };
        _userService.Setup(s => s.GetUserByIdAsync(1)).ReturnsAsync(user);

        // Act
        var result = await sut.Edit(1);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<UserVM>().Subject;
        model.Id.Should().Be(1);
        model.Email.Should().Be("test@test.com");
    }

    [Fact]
    public async Task Edit_Get_WhenUserNotFound_ReturnsNotFound()
    {
        // Arrange
        var sut = CreateSut();
        _userService.Setup(s => s.GetUserByIdAsync(1)).ReturnsAsync((User?)null);

        // Act
        var result = await sut.Edit(1);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Edit_Get_WhenExceptionThrown_RedirectsToIndex()
    {
        // Arrange
        var sut = CreateSut();
        _userService.Setup(s => s.GetUserByIdAsync(1))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await sut.Edit(1);

        // Assert
        var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirectResult.ActionName.Should().Be("Index");
        sut.TempData["ErrorMessage"].Should().NotBeNull();
    }

    [Fact]
    public async Task Edit_Post_WhenValid_RedirectsToIndex()
    {
        // Arrange
        var sut = CreateSut();
        var userVm = new UserVM { Id = 1, FirstName = "Updated", LastName = "User" };
        _userService.Setup(s => s.UpdateUserAsync(1, userVm)).ReturnsAsync(true);

        // Act
        var result = await sut.Edit(1, userVm);

        // Assert
        var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirectResult.ActionName.Should().Be("Index");
        sut.TempData["SuccessMessage"].Should().NotBeNull();
    }

    [Fact]
    public async Task Edit_Post_WhenExceptionThrown_ReturnsViewWithModelError()
    {
        // Arrange
        var sut = CreateSut();
        var userVm = new UserVM { Id = 1, FirstName = "Test" };
        _userService.Setup(s => s.UpdateUserAsync(1, userVm))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await sut.Edit(1, userVm);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        sut.ModelState.ErrorCount.Should().BeGreaterThan(0);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_Get_WhenUserExists_ReturnsViewWithUser()
    {
        // Arrange
        var sut = CreateSut();
        var user = new Volunteer { Id = 1, FirstName = "Test" };
        _userService.Setup(s => s.GetUserByIdAsync(1)).ReturnsAsync(user);

        // Act
        var result = await sut.Delete(1);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().BeSameAs(user);
    }

    [Fact]
    public async Task Delete_Get_WhenUserNotFound_ReturnsNotFound()
    {
        // Arrange
        var sut = CreateSut();
        _userService.Setup(s => s.GetUserByIdAsync(1)).ReturnsAsync((User?)null);

        // Act
        var result = await sut.Delete(1);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Delete_Get_WhenExceptionThrown_RedirectsToIndex()
    {
        // Arrange
        var sut = CreateSut();
        _userService.Setup(s => s.GetUserByIdAsync(1))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await sut.Delete(1);

        // Assert
        var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirectResult.ActionName.Should().Be("Index");
        sut.TempData["ErrorMessage"].Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteConfirmed_WhenUserExists_DeletesAndRedirects()
    {
        // Arrange
        var sut = CreateSut();
        var user = new Volunteer { Id = 1, FirstName = "Test", LastName = "User" };
        _userService.Setup(s => s.GetUserByIdAsync(1)).ReturnsAsync(user);
        _userService.Setup(s => s.DeleteUserAsync(1)).ReturnsAsync(true);

        // Act
        var result = await sut.DeleteConfirmed(1);

        // Assert
        var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirectResult.ActionName.Should().Be("Index");
        sut.TempData["SuccessMessage"].Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteConfirmed_WhenUserNotFound_RedirectsToIndex()
    {
        // Arrange
        var sut = CreateSut();
        _userService.Setup(s => s.GetUserByIdAsync(1)).ReturnsAsync((User?)null);

        // Act
        var result = await sut.DeleteConfirmed(1);

        // Assert
        var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirectResult.ActionName.Should().Be("Index");
    }

    [Fact]
    public async Task DeleteConfirmed_WhenExceptionThrown_RedirectsToIndexWithError()
    {
        // Arrange
        var sut = CreateSut();
        var user = new Volunteer { Id = 1 };
        _userService.Setup(s => s.GetUserByIdAsync(1)).ReturnsAsync(user);
        _userService.Setup(s => s.DeleteUserAsync(1))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await sut.DeleteConfirmed(1);

        // Assert
        var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirectResult.ActionName.Should().Be("Index");
        sut.TempData["ErrorMessage"].Should().NotBeNull();
    }

    #endregion
}
