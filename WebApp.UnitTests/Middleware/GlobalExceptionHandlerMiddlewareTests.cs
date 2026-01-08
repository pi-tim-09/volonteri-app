using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using WebApp.Middleware;

namespace WebApp.UnitTests.Middleware;

public class GlobalExceptionHandlerMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenNoException_CallsNextDelegate()
    {
        // Arrange
        var logger = new Mock<ILogger<GlobalExceptionHandlerMiddleware>>();
        var nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new GlobalExceptionHandlerMiddleware(next, logger.Object);
        var context = new DefaultHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WhenExceptionOccurs_Returns500InternalServerError()
    {
        // Arrange
        var logger = new Mock<ILogger<GlobalExceptionHandlerMiddleware>>();
        RequestDelegate next = (ctx) => throw new InvalidOperationException("Test exception");

        var hostEnv = new Mock<IWebHostEnvironment>();
        hostEnv.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(s => s.GetService(typeof(IWebHostEnvironment))).Returns(hostEnv.Object);

        var middleware = new GlobalExceptionHandlerMiddleware(next, logger.Object);
        var context = new DefaultHttpContext
        {
            RequestServices = serviceProvider.Object,
            Response =
            {
                Body = new MemoryStream()
            }
        };

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        context.Response.ContentType.Should().Be("application/json");
    }

    [Fact]
    public async Task InvokeAsync_WhenExceptionOccursInDevelopment_IncludesStackTrace()
    {
        // Arrange
        var logger = new Mock<ILogger<GlobalExceptionHandlerMiddleware>>();
        RequestDelegate next = (ctx) => throw new InvalidOperationException("Test exception");

        var hostEnv = new Mock<IWebHostEnvironment>();
        hostEnv.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(s => s.GetService(typeof(IWebHostEnvironment))).Returns(hostEnv.Object);

        var middleware = new GlobalExceptionHandlerMiddleware(next, logger.Object);
        var context = new DefaultHttpContext
        {
            RequestServices = serviceProvider.Object,
            Response =
            {
                Body = new MemoryStream()
            }
        };

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();

        body.Should().Contain("StackTrace");
    }
}
