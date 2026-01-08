using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WebApp.Controllers;
using WebApp.Models;

namespace WebApp.UnitTests.Controllers;

public class HomeControllerTests
{
    [Fact]
    public void Index_ReturnsViewResult()
    {
        var controller = new HomeController(Mock.Of<ILogger<HomeController>>());

        var result = controller.Index();

        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public void Privacy_ReturnsViewResult()
    {
        var controller = new HomeController(Mock.Of<ILogger<HomeController>>());

        var result = controller.Privacy();

        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public void Error_ReturnsView_WithErrorViewModel()
    {
        var controller = new HomeController(Mock.Of<ILogger<HomeController>>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        controller.HttpContext.TraceIdentifier = "trace-123";

        var result = controller.Error();

        var view = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<ErrorViewModel>().Subject;
        model.RequestId.Should().Be("trace-123");
    }
}
