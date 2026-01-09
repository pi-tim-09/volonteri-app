using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using WebApp.Controllers;
using WebApp.Interfaces.Services;
using WebApp.Models;

namespace WebApp.UnitTests.Controllers;

public class ApplicationsControllerTests
{
    private readonly Mock<IApplicationService> _appService = new();
    private readonly Mock<IProjectService> _projectService = new();
    private readonly Mock<ILogger<ApplicationsController>> _logger = new();

    private ApplicationsController CreateSut()
    {
        var httpContext = new DefaultHttpContext();
        var tempDataProvider = Mock.Of<ITempDataProvider>();

        var controller = new ApplicationsController(
            _appService.Object,
            _projectService.Object,
            _logger.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, tempDataProvider)
        };

        return controller;
    }

    [Fact]
    public async Task Manage_WhenNoFilters_ReturnsViewWithAllApplications()
    {
        var sut = CreateSut();
        var applications = new List<Application> { new() { Id = 1 }, new() { Id = 2 } };
        _appService.Setup(s => s.GetFilteredApplicationsAsync(null, null)).ReturnsAsync(applications);

        var result = await sut.Manage(null, null);

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().BeEquivalentTo(applications);
    }

    [Fact]
    public async Task Manage_WhenProjectIdFilter_SetsViewBagAndReturnsView()
    {
        var sut = CreateSut();
        var applications = new List<Application> { new() { Id = 1, ProjectId = 5 } };
        _appService.Setup(s => s.GetFilteredApplicationsAsync(5, null)).ReturnsAsync(applications);
        _projectService.Setup(s => s.GetProjectByIdAsync(5)).ReturnsAsync(new Project { Id = 5, Title = "Project 5" });

        var result = await sut.Manage(5, null);

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().BeEquivalentTo(applications);
        sut.ViewData.ContainsKey("ProjectTitle").Should().BeTrue();
        sut.ViewData["ProjectTitle"].Should().Be("Project 5");
        sut.ViewData.ContainsKey("ProjectId").Should().BeTrue();
        sut.ViewData["ProjectId"].Should().Be(5);
    }

    [Fact]
    public async Task Approve_WhenSuccessful_RedirectsToManageWithSuccessMessage()
    {
        var sut = CreateSut();
        _appService.Setup(s => s.ApproveApplicationAsync(1, "Good fit")).ReturnsAsync(true);

        var result = await sut.Approve(1, "Good fit");

        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("Manage");
        sut.TempData.Should().ContainKey("SuccessMessage");
    }

    [Fact]
    public async Task Approve_WhenFails_RedirectsToManageWithErrorMessage()
    {
        var sut = CreateSut();
        _appService.Setup(s => s.ApproveApplicationAsync(1, "Notes")).ReturnsAsync(false);

        var result = await sut.Approve(1, "Notes");

        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("Manage");
        sut.TempData.Should().ContainKey("ErrorMessage");
    }

    [Fact]
    public async Task Reject_WhenSuccessful_RedirectsToManageWithSuccessMessage()
    {
        var sut = CreateSut();
        _appService.Setup(s => s.RejectApplicationAsync(1, "Not qualified")).ReturnsAsync(true);

        var result = await sut.Reject(1, "Not qualified");

        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("Manage");
        sut.TempData.Should().ContainKey("SuccessMessage");
    }

    [Fact]
    public async Task Reject_WhenFails_RedirectsToManageWithErrorMessage()
    {
        var sut = CreateSut();
        _appService.Setup(s => s.RejectApplicationAsync(1, "Notes")).ReturnsAsync(false);

        var result = await sut.Reject(1, "Notes");

        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("Manage");
        sut.TempData.Should().ContainKey("ErrorMessage");
    }

    [Fact]
    public async Task Manage_WhenStatusFilter_SetsViewBagFilterStatus()
    {
        // Arrange
        var sut = CreateSut();
        var applications = new List<Application> { new() { Id = 1, Status = ApplicationStatus.Pending } };
        _appService.Setup(s => s.GetFilteredApplicationsAsync(null, ApplicationStatus.Pending))
            .ReturnsAsync(applications);

        // Act
        var result = await sut.Manage(null, ApplicationStatus.Pending);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        sut.ViewData.ContainsKey("FilterStatus").Should().BeTrue();
        sut.ViewData["FilterStatus"].Should().Be(ApplicationStatus.Pending);
    }

    [Fact]
    public async Task Manage_WhenProjectIdAndStatusFilter_SetsBothViewBagValues()
    {
        // Arrange
        var sut = CreateSut();
        var applications = new List<Application> { new() { Id = 1, ProjectId = 5, Status = ApplicationStatus.Accepted } };
        _appService.Setup(s => s.GetFilteredApplicationsAsync(5, ApplicationStatus.Accepted))
            .ReturnsAsync(applications);
        _projectService.Setup(s => s.GetProjectByIdAsync(5))
            .ReturnsAsync(new Project { Id = 5, Title = "Project 5" });

        // Act
        var result = await sut.Manage(5, ApplicationStatus.Accepted);

        // Assert
        sut.ViewData.ContainsKey("ProjectId").Should().BeTrue();
        sut.ViewData.ContainsKey("ProjectTitle").Should().BeTrue();
        sut.ViewData.ContainsKey("FilterStatus").Should().BeTrue();
    }

    [Fact]
    public async Task Manage_WhenProjectNotFound_StillReturnsView()
    {
        // Arrange
        var sut = CreateSut();
        var applications = new List<Application> { new() { Id = 1, ProjectId = 999 } };
        _appService.Setup(s => s.GetFilteredApplicationsAsync(999, null)).ReturnsAsync(applications);
        _projectService.Setup(s => s.GetProjectByIdAsync(999)).ReturnsAsync((Project?)null);

        // Act
        var result = await sut.Manage(999, null);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        sut.ViewData["ProjectTitle"].Should().BeNull();
        sut.ViewData["ProjectId"].Should().Be(999);
    }

    [Fact]
    public async Task Manage_WhenExceptionThrown_ReturnsViewWithEmptyListAndErrorMessage()
    {
        // Arrange
        var sut = CreateSut();
        _appService.Setup(s => s.GetFilteredApplicationsAsync(It.IsAny<int?>(), It.IsAny<ApplicationStatus?>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await sut.Manage(null, null);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<List<Application>>().Subject;
        model.Should().BeEmpty();
        sut.TempData["ErrorMessage"].Should().Be("An error occurred while loading applications.");
    }

    [Fact]
    public async Task Approve_WithNullReviewNotes_StillApproves()
    {
        // Arrange
        var sut = CreateSut();
        _appService.Setup(s => s.ApproveApplicationAsync(1, null)).ReturnsAsync(true);

        // Act
        var result = await sut.Approve(1, null);

        // Assert
        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        sut.TempData["SuccessMessage"].Should().NotBeNull();
    }

    [Fact]
    public async Task Approve_WhenExceptionThrown_RedirectsWithErrorMessage()
    {
        // Arrange
        var sut = CreateSut();
        _appService.Setup(s => s.ApproveApplicationAsync(1, It.IsAny<string?>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await sut.Approve(1, "Notes");

        // Assert
        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("Manage");
        sut.TempData["ErrorMessage"].Should().Be("An error occurred while approving the application.");
    }

    [Fact]
    public async Task Reject_WithNullReviewNotes_StillRejects()
    {
        // Arrange
        var sut = CreateSut();
        _appService.Setup(s => s.RejectApplicationAsync(1, null)).ReturnsAsync(true);

        // Act
        var result = await sut.Reject(1, null);

        // Assert
        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        sut.TempData["SuccessMessage"].Should().NotBeNull();
    }

    [Fact]
    public async Task Reject_WhenExceptionThrown_RedirectsWithErrorMessage()
    {
        // Arrange
        var sut = CreateSut();
        _appService.Setup(s => s.RejectApplicationAsync(1, It.IsAny<string?>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await sut.Reject(1, "Notes");

        // Assert
        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("Manage");
        sut.TempData["ErrorMessage"].Should().Be("An error occurred while rejecting the application.");
    }
}
