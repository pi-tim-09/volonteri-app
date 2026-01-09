using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WebApp.Models;
using WebApp.Patterns.Structural;

namespace WebApp.UnitTests.Patterns.Structural;

public class NotificationDecoratorTests
{
    [Fact]
    public void NotificationDecorator_Ctor_WhenNullInner_Throws()
    {
        Action act = () => new EmailNotificationDecorator(null!, Mock.Of<ILogger<EmailNotificationDecorator>>());
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void LoggingNotificationDecorator_Ctor_WhenNullLogger_Throws()
    {
        Action act = () => new LoggingNotificationDecorator(Mock.Of<INotificationService>(), null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void EmailNotificationDecorator_Ctor_WhenNullLogger_Throws()
    {
        Action act = () => new EmailNotificationDecorator(Mock.Of<INotificationService>(), null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void StatisticsNotificationDecorator_Ctor_WhenNullLogger_Throws()
    {
        Action act = () => new StatisticsNotificationDecorator(Mock.Of<INotificationService>(), null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task BaseNotificationService_AllMethods_DoNotThrow()
    {
        var svc = new BaseNotificationService();

        await svc.NotifyApplicationSubmittedAsync(new Application());
        await svc.NotifyApplicationApprovedAsync(new Application());
        await svc.NotifyApplicationRejectedAsync(new Application());
        await svc.NotifyApplicationWithdrawnAsync(new Application());
    }

 

    [Fact]
    public async Task LoggingNotificationDecorator_NotifyApplicationSubmittedAsync_DelegatesToInner()
    {
        var inner = new Mock<INotificationService>();
        inner.Setup(x => x.NotifyApplicationSubmittedAsync(It.IsAny<Application>())).Returns(Task.CompletedTask);

        var sut = new LoggingNotificationDecorator(inner.Object, Mock.Of<ILogger<LoggingNotificationDecorator>>());

        var app = new Application { Id = 1, VolunteerId = 10, ProjectId = 20 };
        await sut.NotifyApplicationSubmittedAsync(app);

        inner.Verify(x => x.NotifyApplicationSubmittedAsync(app), Times.Once);
    }

    [Fact]
    public async Task LoggingNotificationDecorator_NotifyApplicationApprovedAsync_DelegatesToInner()
    {
        var inner = new Mock<INotificationService>();
        inner.Setup(x => x.NotifyApplicationApprovedAsync(It.IsAny<Application>())).Returns(Task.CompletedTask);

        var sut = new LoggingNotificationDecorator(inner.Object, Mock.Of<ILogger<LoggingNotificationDecorator>>());

        var app = new Application { Id = 1, VolunteerId = 10, ProjectId = 20 };
        await sut.NotifyApplicationApprovedAsync(app);

        inner.Verify(x => x.NotifyApplicationApprovedAsync(app), Times.Once);
    }

    [Fact]
    public async Task LoggingNotificationDecorator_NotifyApplicationRejectedAsync_DelegatesToInner()
    {
        var inner = new Mock<INotificationService>();
        inner.Setup(x => x.NotifyApplicationRejectedAsync(It.IsAny<Application>())).Returns(Task.CompletedTask);

        var sut = new LoggingNotificationDecorator(inner.Object, Mock.Of<ILogger<LoggingNotificationDecorator>>());

        var app = new Application { Id = 1, VolunteerId = 10, ProjectId = 20 };
        await sut.NotifyApplicationRejectedAsync(app);

        inner.Verify(x => x.NotifyApplicationRejectedAsync(app), Times.Once);
    }

    [Fact]
    public async Task LoggingNotificationDecorator_NotifyApplicationWithdrawnAsync_DelegatesToInner()
    {
        var inner = new Mock<INotificationService>();
        inner.Setup(x => x.NotifyApplicationWithdrawnAsync(It.IsAny<Application>())).Returns(Task.CompletedTask);

        var sut = new LoggingNotificationDecorator(inner.Object, Mock.Of<ILogger<LoggingNotificationDecorator>>());

        var app = new Application { Id = 1, VolunteerId = 10, ProjectId = 20 };
        await sut.NotifyApplicationWithdrawnAsync(app);

        inner.Verify(x => x.NotifyApplicationWithdrawnAsync(app), Times.Once);
    }

   

   

    [Fact]
    public async Task EmailNotificationDecorator_NotifyApplicationApprovedAsync_DelegatesToInner()
    {
        var inner = new Mock<INotificationService>();
        inner.Setup(x => x.NotifyApplicationApprovedAsync(It.IsAny<Application>())).Returns(Task.CompletedTask);

        var sut = new EmailNotificationDecorator(inner.Object, Mock.Of<ILogger<EmailNotificationDecorator>>());

        var app = new Application
        {
            Id = 1,
            Volunteer = new Volunteer { Email = "volunteer@example.com" },
            Project = new Project { Title = "Test Project" }
        };

        await sut.NotifyApplicationApprovedAsync(app);

        inner.Verify(x => x.NotifyApplicationApprovedAsync(app), Times.Once);
    }

    [Fact]
    public async Task EmailNotificationDecorator_NotifyApplicationRejectedAsync_DelegatesToInner()
    {
        var inner = new Mock<INotificationService>();
        inner.Setup(x => x.NotifyApplicationRejectedAsync(It.IsAny<Application>())).Returns(Task.CompletedTask);

        var sut = new EmailNotificationDecorator(inner.Object, Mock.Of<ILogger<EmailNotificationDecorator>>());

        var app = new Application
        {
            Id = 3,
            Volunteer = new Volunteer { Email = "vol@example.com" },
            Project = new Project { Title = "Project" }
        };

        await sut.NotifyApplicationRejectedAsync(app);

        inner.Verify(x => x.NotifyApplicationRejectedAsync(app), Times.Once);
    }

    [Fact]
    public async Task EmailNotificationDecorator_NotifyApplicationSubmittedAsync_DelegatesToInner()
    {
        var inner = new Mock<INotificationService>();
        inner.Setup(x => x.NotifyApplicationSubmittedAsync(It.IsAny<Application>())).Returns(Task.CompletedTask);

        var sut = new EmailNotificationDecorator(inner.Object, Mock.Of<ILogger<EmailNotificationDecorator>>());

        var app = new Application
        {
            Id = 2,
            Volunteer = new Volunteer { Email = "v@example.com" },
            Project = new Project
            {
                Title = "Project",
                Organization = new Organization { Email = "org@example.com" }
            }
        };

        await sut.NotifyApplicationSubmittedAsync(app);

        inner.Verify(x => x.NotifyApplicationSubmittedAsync(app), Times.Once);
    }

    [Fact]
    public async Task EmailNotificationDecorator_NotifyApplicationWithdrawnAsync_DelegatesToInner()
    {
        var inner = new Mock<INotificationService>();
        inner.Setup(x => x.NotifyApplicationWithdrawnAsync(It.IsAny<Application>())).Returns(Task.CompletedTask);

        var sut = new EmailNotificationDecorator(inner.Object, Mock.Of<ILogger<EmailNotificationDecorator>>());

        var app = new Application
        {
            Id = 4,
            Volunteer = new Volunteer { Email = "vol@example.com" }
        };

        await sut.NotifyApplicationWithdrawnAsync(app);

        inner.Verify(x => x.NotifyApplicationWithdrawnAsync(app), Times.Once);
    }

    [Fact]
    public async Task EmailNotificationDecorator_WhenVolunteerEmailNull_HandlesGracefully()
    {
        var inner = new Mock<INotificationService>();
        inner.Setup(x => x.NotifyApplicationApprovedAsync(It.IsAny<Application>())).Returns(Task.CompletedTask);

        var sut = new EmailNotificationDecorator(inner.Object, Mock.Of<ILogger<EmailNotificationDecorator>>());

        var app = new Application { Id = 1 };

        await sut.NotifyApplicationApprovedAsync(app);

        inner.Verify(x => x.NotifyApplicationApprovedAsync(app), Times.Once);
    }

    

    

    [Fact]
    public async Task StatisticsNotificationDecorator_NotifyApplicationApprovedAsync_DelegatesToInner()
    {
        var inner = new Mock<INotificationService>();
        inner.Setup(x => x.NotifyApplicationApprovedAsync(It.IsAny<Application>())).Returns(Task.CompletedTask);

        var sut = new StatisticsNotificationDecorator(inner.Object, Mock.Of<ILogger<StatisticsNotificationDecorator>>());

        var app = new Application { Id = 2 };
        await sut.NotifyApplicationApprovedAsync(app);

        inner.Verify(x => x.NotifyApplicationApprovedAsync(app), Times.Once);
    }

    [Fact]
    public async Task StatisticsNotificationDecorator_NotifyApplicationRejectedAsync_DelegatesToInner()
    {
        var inner = new Mock<INotificationService>();
        inner.Setup(x => x.NotifyApplicationRejectedAsync(It.IsAny<Application>())).Returns(Task.CompletedTask);

        var sut = new StatisticsNotificationDecorator(inner.Object, Mock.Of<ILogger<StatisticsNotificationDecorator>>());

        var app = new Application { Id = 3 };
        await sut.NotifyApplicationRejectedAsync(app);

        inner.Verify(x => x.NotifyApplicationRejectedAsync(app), Times.Once);
    }

    [Fact]
    public async Task StatisticsNotificationDecorator_NotifyApplicationSubmittedAsync_DelegatesToInner()
    {
        var inner = new Mock<INotificationService>();
        inner.Setup(x => x.NotifyApplicationSubmittedAsync(It.IsAny<Application>())).Returns(Task.CompletedTask);

        var sut = new StatisticsNotificationDecorator(inner.Object, Mock.Of<ILogger<StatisticsNotificationDecorator>>());

        var app = new Application { Id = 1 };
        await sut.NotifyApplicationSubmittedAsync(app);

        inner.Verify(x => x.NotifyApplicationSubmittedAsync(app), Times.Once);
    }

    [Fact]
    public async Task StatisticsNotificationDecorator_NotifyApplicationWithdrawnAsync_DelegatesToInner()
    {
        var inner = new Mock<INotificationService>();
        inner.Setup(x => x.NotifyApplicationWithdrawnAsync(It.IsAny<Application>())).Returns(Task.CompletedTask);

        var sut = new StatisticsNotificationDecorator(inner.Object, Mock.Of<ILogger<StatisticsNotificationDecorator>>());

        var app = new Application { Id = 4 };
        await sut.NotifyApplicationWithdrawnAsync(app);

        inner.Verify(x => x.NotifyApplicationWithdrawnAsync(app), Times.Once);
    }

    
}
