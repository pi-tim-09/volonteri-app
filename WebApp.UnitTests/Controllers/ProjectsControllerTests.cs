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

public class ProjectsControllerTests
{
    private readonly Mock<IProjectService> _projectService = new();
    private readonly Mock<IOrganizationService> _orgService = new();
    private readonly Mock<ILogger<ProjectsController>> _logger = new();

    private ProjectsController CreateSut()
    {
        var controller = new ProjectsController(
            _projectService.Object,
            _orgService.Object,
            _logger.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        };
        return controller;
    }

    #region Existing Tests

    [Fact]
    public async Task Index_WhenNoOrganizationId_ReturnsViewWithAllProjects()
    {
        var sut = CreateSut();

        var projects = new List<Project> { new() { Id = 1, Title = "P1" }, new() { Id = 2, Title = "P2" } };
        _projectService.Setup(s => s.GetAllProjectsAsync()).ReturnsAsync(projects);

        var result = await sut.Index(null);

        var view = result.Should().BeOfType<ViewResult>().Subject;
        view.Model.Should().BeEquivalentTo(projects);
    }

    [Fact]
    public async Task Index_WhenOrganizationId_ReturnsViewAndSetsViewBag()
    {
        var sut = CreateSut();

        var projects = new List<Project> { new() { Id = 1, Title = "P1", OrganizationId = 7 } };
        _projectService.Setup(s => s.GetProjectsByOrganizationAsync(7)).ReturnsAsync(projects);
        _orgService.Setup(s => s.GetOrganizationByIdAsync(7)).ReturnsAsync(new Organization { Id = 7, OrganizationName = "Org7" });

        var result = await sut.Index(7);

        var view = result.Should().BeOfType<ViewResult>().Subject;
        view.Model.Should().BeEquivalentTo(projects);

        // ViewBag uses ViewData internally; assert against ViewData to avoid dynamic binder issues.
        sut.ViewData.ContainsKey("OrganizationName").Should().BeTrue();
        sut.ViewData["OrganizationName"].Should().Be("Org7");
    }

    [Fact]
    public async Task Edit_Get_WhenIdNull_ReturnsNotFound()
    {
        var sut = CreateSut();

        var result = await sut.Edit(null);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Edit_Get_WhenProjectNotFound_ReturnsNotFound()
    {
        var sut = CreateSut();
        _projectService.Setup(s => s.GetProjectByIdAsync(1)).ReturnsAsync((Project?)null);

        var result = await sut.Edit(1);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Edit_Post_WhenIdMismatch_ReturnsNotFound()
    {
        var sut = CreateSut();

        var result = await sut.Edit(5, new Project { Id = 6 });

        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region Additional Index Tests

    [Fact]
    public async Task Index_WhenOrganizationNotFound_StillReturnsProjects()
    {
        // Arrange
        var sut = CreateSut();
        var projects = new List<Project> { new() { Id = 1, Title = "P1", OrganizationId = 5 } };
        _projectService.Setup(s => s.GetProjectsByOrganizationAsync(5)).ReturnsAsync(projects);
        _orgService.Setup(s => s.GetOrganizationByIdAsync(5)).ReturnsAsync((Organization?)null);

        // Act
        var result = await sut.Index(5);

        // Assert
        var view = result.Should().BeOfType<ViewResult>().Subject;
        view.Model.Should().BeEquivalentTo(projects);
    }

    [Fact]
    public async Task Index_WhenExceptionThrown_ReturnsViewWithEmptyList()
    {
        // Arrange
        var sut = CreateSut();
        _projectService.Setup(s => s.GetAllProjectsAsync())
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await sut.Index(null);

        // Assert
        var view = result.Should().BeOfType<ViewResult>().Subject;
        view.Model.Should().BeOfType<List<Project>>();
        ((List<Project>)view.Model!).Should().BeEmpty();
        sut.TempData["ErrorMessage"].Should().NotBeNull();
    }

    #endregion

    #region Additional Edit GET Tests

    [Fact]
    public async Task Edit_Get_WhenProjectExists_ReturnsViewWithProject()
    {
        // Arrange
        var sut = CreateSut();
        var project = new Project { Id = 1, Title = "Test Project" };
        var organizations = new List<Organization> { new() { Id = 1, OrganizationName = "Org1" } };
        
        _projectService.Setup(s => s.GetProjectByIdAsync(1)).ReturnsAsync(project);
        _orgService.Setup(s => s.GetAllOrganizationsAsync()).ReturnsAsync(organizations);

        // Act
        var result = await sut.Edit(1);

        // Assert
        var view = result.Should().BeOfType<ViewResult>().Subject;
        view.Model.Should().BeSameAs(project);
        sut.ViewData.ContainsKey("Organizations").Should().BeTrue();
    }

    [Fact]
    public async Task Edit_Get_WhenExceptionThrown_RedirectsToIndex()
    {
        // Arrange
        var sut = CreateSut();
        _projectService.Setup(s => s.GetProjectByIdAsync(1))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await sut.Edit(1);

        // Assert
        var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirectResult.ActionName.Should().Be("Index");
        sut.TempData["ErrorMessage"].Should().NotBeNull();
    }

    #endregion

    #region Edit POST Tests

    [Fact]
    public async Task Edit_Post_WhenModelInvalid_ReturnsViewWithModel()
    {
        // Arrange
        var sut = CreateSut();
        sut.ModelState.AddModelError("Title", "Required");
        var project = new Project { Id = 1 };
        var organizations = new List<Organization> { new() { Id = 1 } };
        _orgService.Setup(s => s.GetAllOrganizationsAsync()).ReturnsAsync(organizations);

        // Act
        var result = await sut.Edit(1, project);

        // Assert
        var view = result.Should().BeOfType<ViewResult>().Subject;
        view.Model.Should().BeSameAs(project);
    }

    [Fact]
    public async Task Edit_Post_WhenValid_RedirectsToIndex()
    {
        // Arrange
        var sut = CreateSut();
        var project = new Project { Id = 1, Title = "Updated Project" };
        _projectService.Setup(s => s.UpdateProjectAsync(1, project)).ReturnsAsync(true);

        // Act
        var result = await sut.Edit(1, project);

        // Assert
        var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirectResult.ActionName.Should().Be("Index");
        sut.TempData["SuccessMessage"].Should().NotBeNull();
    }

    [Fact]
    public async Task Edit_Post_WhenProjectNotFound_ReturnsNotFound()
    {
        // Arrange
        var sut = CreateSut();
        var project = new Project { Id = 1, Title = "Test" };
        _projectService.Setup(s => s.UpdateProjectAsync(1, project)).ReturnsAsync(false);

        // Act
        var result = await sut.Edit(1, project);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Edit_Post_WhenExceptionThrown_ReturnsViewWithModelError()
    {
        // Arrange
        var sut = CreateSut();
        var project = new Project { Id = 1, Title = "Test" };
        var organizations = new List<Organization> { new() { Id = 1 } };
        
        _projectService.Setup(s => s.UpdateProjectAsync(1, project))
            .ThrowsAsync(new Exception("Database error"));
        _orgService.Setup(s => s.GetAllOrganizationsAsync()).ReturnsAsync(organizations);

        // Act
        var result = await sut.Edit(1, project);

        // Assert
        var view = result.Should().BeOfType<ViewResult>().Subject;
        view.Model.Should().BeSameAs(project);
        sut.ModelState.ErrorCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Edit_Post_WhenOrganizationServiceFails_StillReturnsView()
    {
        // Arrange
        var sut = CreateSut();
        sut.ModelState.AddModelError("Title", "Required");
        var project = new Project { Id = 1 };
        
        _orgService.Setup(s => s.GetAllOrganizationsAsync())
            .ThrowsAsync(new Exception("Org service error"));

        // Act
        var result = await sut.Edit(1, project);

        // Assert
        var view = result.Should().BeOfType<ViewResult>().Subject;
        view.Model.Should().BeSameAs(project);
    }

    #endregion
}
