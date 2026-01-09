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

public class OrganizationsControllerTests
{
    private readonly Mock<IOrganizationService> _organizationService = new();
    private readonly Mock<ILogger<OrganizationsController>> _logger = new();

    private OrganizationsController CreateSut()
    {
        var controller = new OrganizationsController(_organizationService.Object, _logger.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        };
        return controller;
    }

    [Fact]
    public async Task Index_ReturnsViewWithOrganizations()
    {
        // Arrange
        var sut = CreateSut();
        var organizations = new List<Organization>
        {
            new() { Id = 1, OrganizationName = "Org 1" },
            new() { Id = 2, OrganizationName = "Org 2" }
        };
        _organizationService.Setup(s => s.GetAllOrganizationsAsync()).ReturnsAsync(organizations);

        // Act
        var result = await sut.Index();

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().BeEquivalentTo(organizations);
    }

    [Fact]
    public async Task Index_WhenNoOrganizations_ReturnsViewWithEmptyList()
    {
        // Arrange
        var sut = CreateSut();
        _organizationService.Setup(s => s.GetAllOrganizationsAsync())
            .ReturnsAsync(new List<Organization>());

        // Act
        var result = await sut.Index();

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<List<Organization>>().Subject;
        model.Should().BeEmpty();
    }

    [Fact]
    public async Task Index_WhenExceptionThrown_ReturnsViewWithEmptyListAndErrorMessage()
    {
        // Arrange
        var sut = CreateSut();
        _organizationService.Setup(s => s.GetAllOrganizationsAsync())
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await sut.Index();

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<List<Organization>>().Subject;
        model.Should().BeEmpty();
        sut.TempData["ErrorMessage"].Should().NotBeNull();
        sut.TempData["ErrorMessage"].Should().Be("An error occurred while loading organizations.");
    }
}
