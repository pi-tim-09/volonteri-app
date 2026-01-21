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

        var act1 = async () => await svc.NotifyApplicationSubmittedAsync(new Application());
        var act2 = async () => await svc.NotifyApplicationApprovedAsync(new Application());
        var act3 = async () => await svc.NotifyApplicationRejectedAsync(new Application());
        var act4 = async () => await svc.NotifyApplicationWithdrawnAsync(new Application());

        await act1.Should().NotThrowAsync();
        await act2.Should().NotThrowAsync();
        await act3.Should().NotThrowAsync();
        await act4.Should().NotThrowAsync();
    }

 

    [Fact]
    public async Task LoggingNotificationDecorator_NotifyApplicationSubmittedAsync_DelegatesToInner()
    {
        var innerMock = new Mock<INotificationService>();
        innerMock.Setup(x => x.NotifyApplicationSubmittedAsync(It.IsAny<Application>())).Returns(Task.CompletedTask);

        var decorator = new LoggingNotificationDecorator(innerMock.Object, Mock.Of<ILogger<LoggingNotificationDecorator>>());

        var application = new Application { Id = 1, VolunteerId = 10, ProjectId = 20 };
        await decorator.NotifyApplicationSubmittedAsync(application);

        innerMock.Verify(x => x.NotifyApplicationSubmittedAsync(application), Times.Once);
    }

    [Fact]
    public async Task LoggingNotificationDecorator_NotifyApplicationApprovedAsync_DelegatesToInner()
    {
        var innerMock = new Mock<INotificationService>();
        innerMock.Setup(x => x.NotifyApplicationApprovedAsync(It.IsAny<Application>())).Returns(Task.CompletedTask);

        var decorator = new LoggingNotificationDecorator(innerMock.Object, Mock.Of<ILogger<LoggingNotificationDecorator>>());

        var application = new Application { Id = 1, VolunteerId = 10, ProjectId = 20 };
        await decorator.NotifyApplicationApprovedAsync(application);

        innerMock.Verify(x => x.NotifyApplicationApprovedAsync(application), Times.Once);
    }

    [Fact]
    public async Task LoggingNotificationDecorator_NotifyApplicationRejectedAsync_DelegatesToInner()
    {
        var innerMock = new Mock<INotificationService>();
        innerMock.Setup(x => x.NotifyApplicationRejectedAsync(It.IsAny<Application>())).Returns(Task.CompletedTask);

        var decorator = new LoggingNotificationDecorator(innerMock.Object, Mock.Of<ILogger<LoggingNotificationDecorator>>());

        var application = new Application { Id = 1, VolunteerId = 10, ProjectId = 20 };
        await decorator.NotifyApplicationRejectedAsync(application);

        innerMock.Verify(x => x.NotifyApplicationRejectedAsync(application), Times.Once);
    }

    [Fact]
    public async Task LoggingNotificationDecorator_NotifyApplicationWithdrawnAsync_DelegatesToInner()
    {
        var innerMock = new Mock<INotificationService>();
        innerMock.Setup(x => x.NotifyApplicationWithdrawnAsync(It.IsAny<Application>())).Returns(Task.CompletedTask);

        var decorator = new LoggingNotificationDecorator(innerMock.Object, Mock.Of<ILogger<LoggingNotificationDecorator>>());

        var application = new Application { Id = 1, VolunteerId = 10, ProjectId = 20 };
        await decorator.NotifyApplicationWithdrawnAsync(application);

        innerMock.Verify(x => x.NotifyApplicationWithdrawnAsync(application), Times.Once);
    }

   

   

    [Fact]
    public async Task EmailNotificationDecorator_NotifyApplicationApprovedAsync_DelegatesToInner()
    {
        var innerMock = new Mock<INotificationService>();
        innerMock.Setup(x => x.NotifyApplicationApprovedAsync(It.IsAny<Application>())).Returns(Task.CompletedTask);

        var decorator = new EmailNotificationDecorator(innerMock.Object, Mock.Of<ILogger<EmailNotificationDecorator>>());

        var application = new Application
        {
            Id = 1,
            Volunteer = new Volunteer { Email = "volunteer@example.com" },
            Project = new Project { Title = "Test Project" }
        };

        await decorator.NotifyApplicationApprovedAsync(application);

        innerMock.Verify(x => x.NotifyApplicationApprovedAsync(application), Times.Once);
    }

    [Fact]
    public async Task EmailNotificationDecorator_NotifyApplicationRejectedAsync_DelegatesToInner()
    {
        var innerMock = new Mock<INotificationService>();
        innerMock.Setup(x => x.NotifyApplicationRejectedAsync(It.IsAny<Application>())).Returns(Task.CompletedTask);

        var decorator = new EmailNotificationDecorator(innerMock.Object, Mock.Of<ILogger<EmailNotificationDecorator>>());

        var application = new Application
        {
            Id = 3,
            Volunteer = new Volunteer { Email = "vol@example.com" },
            Project = new Project { Title = "Project" }
        };

        await decorator.NotifyApplicationRejectedAsync(application);

        innerMock.Verify(x => x.NotifyApplicationRejectedAsync(application), Times.Once);
    }

    [Fact]
    public async Task EmailNotificationDecorator_NotifyApplicationSubmittedAsync_DelegatesToInner()
    {
        var innerMock = new Mock<INotificationService>();
        innerMock.Setup(x => x.NotifyApplicationSubmittedAsync(It.IsAny<Application>())).Returns(Task.CompletedTask);

        var decorator = new EmailNotificationDecorator(innerMock.Object, Mock.Of<ILogger<EmailNotificationDecorator>>());

        var application = new Application
        {
            Id = 2,
            Volunteer = new Volunteer { Email = "v@example.com" },
            Project = new Project
            {
                Title = "Project",
                Organization = new Organization { Email = "org@example.com" }
            }
        };

        await decorator.NotifyApplicationSubmittedAsync(application);

        innerMock.Verify(x => x.NotifyApplicationSubmittedAsync(application), Times.Once);
    }

    [Fact]
    public async Task EmailNotificationDecorator_NotifyApplicationWithdrawnAsync_DelegatesToInner()
    {
        var innerMock = new Mock<INotificationService>();
        innerMock.Setup(x => x.NotifyApplicationWithdrawnAsync(It.IsAny<Application>())).Returns(Task.CompletedTask);

        var decorator = new EmailNotificationDecorator(innerMock.Object, Mock.Of<ILogger<EmailNotificationDecorator>>());

        var application = new Application
        {
            Id = 4,
            Volunteer = new Volunteer { Email = "vol@example.com" }
        };

        await decorator.NotifyApplicationWithdrawnAsync(application);

        innerMock.Verify(x => x.NotifyApplicationWithdrawnAsync(application), Times.Once);
    }

    [Fact]
    public async Task EmailNotificationDecorator_WhenVolunteerEmailNull_HandlesGracefully()
    {
        var innerMock = new Mock<INotificationService>();
        innerMock.Setup(x => x.NotifyApplicationApprovedAsync(It.IsAny<Application>())).Returns(Task.CompletedTask);

        var decorator = new EmailNotificationDecorator(innerMock.Object, Mock.Of<ILogger<EmailNotificationDecorator>>());

        var application = new Application { Id = 1 };

        await decorator.NotifyApplicationApprovedAsync(application);

        innerMock.Verify(x => x.NotifyApplicationApprovedAsync(application), Times.Once);
    }

    

    

    [Fact]
    public async Task StatisticsNotificationDecorator_NotifyApplicationApprovedAsync_DelegatesToInner()
    {
        var innerMock = new Mock<INotificationService>();
        innerMock.Setup(x => x.NotifyApplicationApprovedAsync(It.IsAny<Application>())).Returns(Task.CompletedTask);

        var decorator = new StatisticsNotificationDecorator(innerMock.Object, Mock.Of<ILogger<StatisticsNotificationDecorator>>());

        var application = new Application { Id = 2 };
        await decorator.NotifyApplicationApprovedAsync(application);

        innerMock.Verify(x => x.NotifyApplicationApprovedAsync(application), Times.Once);
    }

    [Fact]
    public async Task StatisticsNotificationDecorator_NotifyApplicationRejectedAsync_DelegatesToInner()
    {
        var innerMock = new Mock<INotificationService>();
        innerMock.Setup(x => x.NotifyApplicationRejectedAsync(It.IsAny<Application>())).Returns(Task.CompletedTask);

        var decorator = new StatisticsNotificationDecorator(innerMock.Object, Mock.Of<ILogger<StatisticsNotificationDecorator>>());

        var application = new Application { Id = 3 };
        await decorator.NotifyApplicationRejectedAsync(application);

        innerMock.Verify(x => x.NotifyApplicationRejectedAsync(application), Times.Once);
    }

    [Fact]
    public async Task StatisticsNotificationDecorator_NotifyApplicationSubmittedAsync_DelegatesToInner()
    {
        var innerMock = new Mock<INotificationService>();
        innerMock.Setup(x => x.NotifyApplicationSubmittedAsync(It.IsAny<Application>())).Returns(Task.CompletedTask);

        var decorator = new StatisticsNotificationDecorator(innerMock.Object, Mock.Of<ILogger<StatisticsNotificationDecorator>>());

        var application = new Application { Id = 1 };
        await decorator.NotifyApplicationSubmittedAsync(application);

        innerMock.Verify(x => x.NotifyApplicationSubmittedAsync(application), Times.Once);
    }

    [Fact]
    public async Task StatisticsNotificationDecorator_NotifyApplicationWithdrawnAsync_DelegatesToInner()
    {
        var innerMock = new Mock<INotificationService>();
        innerMock.Setup(x => x.NotifyApplicationWithdrawnAsync(It.IsAny<Application>())).Returns(Task.CompletedTask);

        var decorator = new StatisticsNotificationDecorator(innerMock.Object, Mock.Of<ILogger<StatisticsNotificationDecorator>>());

        var application = new Application { Id = 4 };
        await decorator.NotifyApplicationWithdrawnAsync(application);

        innerMock.Verify(x => x.NotifyApplicationWithdrawnAsync(application), Times.Once);
    }

    
}
