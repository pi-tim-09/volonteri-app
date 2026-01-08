using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WebApp.Models;
using WebApp.Patterns.Structural;

namespace WebApp.UnitTests.Patterns.Structural;

public class NotificationDecoratorTests
{
    [Fact]
    public void EmailNotificationDecorator_Ctor_WhenNullInner_Throws()
    {
        Action act = () => new EmailNotificationDecorator(null!, Mock.Of<ILogger<EmailNotificationDecorator>>());
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task BasicNotificationService_AllMethods_DoNotThrow()
    {
        var svc = new BaseNotificationService();

        await svc.NotifyApplicationSubmittedAsync(new Application());
        await svc.NotifyApplicationApprovedAsync(new Application());
        await svc.NotifyApplicationRejectedAsync(new Application());
        await svc.NotifyApplicationWithdrawnAsync(new Application());
    }

    [Fact]
    public async Task LoggingNotificationDecorator_DelegatesToInner()
    {
        var inner = new Mock<INotificationService>();
        inner.Setup(x => x.NotifyApplicationSubmittedAsync(It.IsAny<Application>())).Returns(Task.CompletedTask);

        var sut = new LoggingNotificationDecorator(inner.Object, Mock.Of<ILogger<LoggingNotificationDecorator>>());

        var app = new Application { Id = 1 };
        await sut.NotifyApplicationSubmittedAsync(app);

        inner.Verify(x => x.NotifyApplicationSubmittedAsync(app), Times.Once);
    }

    [Fact]
    public async Task StatisticsNotificationDecorator_DelegatesToInner()
    {
        var inner = new Mock<INotificationService>();
        inner.Setup(x => x.NotifyApplicationApprovedAsync(It.IsAny<Application>())).Returns(Task.CompletedTask);

        var sut = new StatisticsNotificationDecorator(inner.Object, Mock.Of<ILogger<StatisticsNotificationDecorator>>());

        var app = new Application { Id = 2 };
        await sut.NotifyApplicationApprovedAsync(app);

        inner.Verify(x => x.NotifyApplicationApprovedAsync(app), Times.Once);
    }

    [Fact]
    public async Task EmailNotificationDecorator_DelegatesToInner()
    {
        var inner = new Mock<INotificationService>();
        inner.Setup(x => x.NotifyApplicationRejectedAsync(It.IsAny<Application>())).Returns(Task.CompletedTask);

        var sut = new EmailNotificationDecorator(inner.Object, Mock.Of<ILogger<EmailNotificationDecorator>>());

        var app = new Application { Id = 3 };
        await sut.NotifyApplicationRejectedAsync(app);

        inner.Verify(x => x.NotifyApplicationRejectedAsync(app), Times.Once);
    }
}
