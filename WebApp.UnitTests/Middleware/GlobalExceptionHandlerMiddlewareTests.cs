using FluentAssertions;
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
    public async Task InvokeAsync_WhenExceptionOccurs_ForApiRequest_Returns500Json()
    {
        // Arrange
        var logger = new Mock<ILogger<GlobalExceptionHandlerMiddleware>>();
        RequestDelegate next = (ctx) => throw new InvalidOperationException("Test exception");

        var middleware = new GlobalExceptionHandlerMiddleware(next, logger.Object);
        var context = new DefaultHttpContext
        {
            Response = { Body = new MemoryStream() }
        };
        context.Request.Path = "/api/test";
        context.Request.Headers.Accept = "application/json";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        context.Response.ContentType.Should().Be("application/json");

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        body.Should().NotContain("Test exception");
        body.Should().NotContain("StackTrace");
    }

    [Fact]
    public async Task InvokeAsync_WhenExceptionOccurs_ForBrowserRequest_RedirectsToErrorPage()
    {
        // Arrange
        var logger = new Mock<ILogger<GlobalExceptionHandlerMiddleware>>();
        RequestDelegate next = (ctx) => throw new InvalidOperationException("Test exception");

        var middleware = new GlobalExceptionHandlerMiddleware(next, logger.Object);
        var context = new DefaultHttpContext
        {
            Response = { Body = new MemoryStream() }
        };
        context.Request.Path = "/Account/Login";
        context.Request.Headers.Accept = "text/html";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status302Found);
        context.Response.Headers.Location.ToString().Should().Be("/Home/Error");
    }
}
