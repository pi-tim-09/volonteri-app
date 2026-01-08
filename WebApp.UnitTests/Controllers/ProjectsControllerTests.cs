using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
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

    private ProjectsController CreateSut() => new(
        _projectService.Object,
        _orgService.Object,
        _logger.Object);

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
}
