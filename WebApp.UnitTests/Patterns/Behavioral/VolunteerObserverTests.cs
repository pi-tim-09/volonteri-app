using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WebApp.Models;
using WebApp.Patterns.Behavioral;

namespace WebApp.UnitTests.Patterns.Behavioral;

public class VolunteerObserverTests
{
    [Fact]
    public void VolunteerEventPublisher_Ctor_WhenLoggerNull_Throws()
    {
        Action act = () => new VolunteerEventPublisher(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Subscribe_WhenNull_Throws()
    {
        var pub = new VolunteerEventPublisher(Mock.Of<ILogger<VolunteerEventPublisher>>());
        Action act = () => pub.Subscribe(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Unsubscribe_WhenNull_Throws()
    {
        var pub = new VolunteerEventPublisher(Mock.Of<ILogger<VolunteerEventPublisher>>());
        Action act = () => pub.Unsubscribe(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task NotifyVolunteerRegisteredAsync_NotifiesAllObservers_EvenIfOneThrows()
    {
        var pub = new VolunteerEventPublisher(Mock.Of<ILogger<VolunteerEventPublisher>>());

        var good = new Mock<IVolunteerObserver>();
        good.Setup(o => o.OnVolunteerRegisteredAsync(It.IsAny<Volunteer>())).Returns(Task.CompletedTask);

        var bad = new Mock<IVolunteerObserver>();
        bad.Setup(o => o.OnVolunteerRegisteredAsync(It.IsAny<Volunteer>())).ThrowsAsync(new InvalidOperationException());

        pub.Subscribe(good.Object);
        pub.Subscribe(bad.Object);

        var volunteer = new Volunteer { Id = 1, Email = "v@x.com" };

        await pub.NotifyVolunteerRegisteredAsync(volunteer);

        good.Verify(o => o.OnVolunteerRegisteredAsync(volunteer), Times.Once);
        bad.Verify(o => o.OnVolunteerRegisteredAsync(volunteer), Times.Once);
    }

    [Fact]
    public async Task NotifyVolunteerSkillsUpdatedAsync_CallsObservers()
    {
        var pub = new VolunteerEventPublisher(Mock.Of<ILogger<VolunteerEventPublisher>>());
        var obs = new Mock<IVolunteerObserver>();
        pub.Subscribe(obs.Object);

        var volunteer = new Volunteer { Id = 2, Email = "v2@x.com" };
        var skills = new List<string> { "C#" };

        await pub.NotifyVolunteerSkillsUpdatedAsync(volunteer, skills);

        obs.Verify(o => o.OnVolunteerSkillsUpdatedAsync(volunteer, skills), Times.Once);
    }

    [Fact]
    public async Task NotifyVolunteerProjectCompletedAsync_CallsObservers()
    {
        var pub = new VolunteerEventPublisher(Mock.Of<ILogger<VolunteerEventPublisher>>());
        var obs = new Mock<IVolunteerObserver>();
        pub.Subscribe(obs.Object);

        var volunteer = new Volunteer { Id = 3, Email = "v3@x.com" };

        await pub.NotifyVolunteerProjectCompletedAsync(volunteer, projectId: 10, hoursLogged: 5);

        obs.Verify(o => o.OnVolunteerProjectCompletedAsync(volunteer, 10, 5), Times.Once);
    }

    [Fact]
    public async Task LoggingVolunteerObserver_DoesNotThrow()
    {
        var obs = new LoggingVolunteerObserver(Mock.Of<ILogger<LoggingVolunteerObserver>>());
        await obs.OnVolunteerRegisteredAsync(new Volunteer { Id = 1, Email = "x" });
        await obs.OnVolunteerSkillsUpdatedAsync(new Volunteer { Id = 1, Email = "x" }, new List<string> { "A" });
        await obs.OnVolunteerProjectCompletedAsync(new Volunteer { Id = 1, Email = "x" }, 1, 2);
    }

    [Fact]
    public async Task NotificationVolunteerObserver_DoesNotThrow()
    {
        var obs = new NotificationVolunteerObserver(Mock.Of<ILogger<NotificationVolunteerObserver>>());
        await obs.OnVolunteerRegisteredAsync(new Volunteer { Id = 1, Email = "x" });
        await obs.OnVolunteerSkillsUpdatedAsync(new Volunteer { Id = 1, Email = "x" }, new List<string> { "A" });
        await obs.OnVolunteerProjectCompletedAsync(new Volunteer { Id = 1, Email = "x" }, 1, 2);
    }

    [Fact]
    public async Task StatisticsVolunteerObserver_IncrementsCounters()
    {
        var obs = new StatisticsVolunteerObserver(Mock.Of<ILogger<StatisticsVolunteerObserver>>());

        await obs.OnVolunteerRegisteredAsync(new Volunteer { Id = 1 });
        await obs.OnVolunteerSkillsUpdatedAsync(new Volunteer { Id = 1 }, new List<string> { "A" });
        await obs.OnVolunteerProjectCompletedAsync(new Volunteer { Id = 1 }, 99, 3);

        var stats = StatisticsVolunteerObserver.GetStatistics();
        stats.Registrations.Should().BeGreaterThan(0);
        stats.SkillUpdates.Should().BeGreaterThan(0);
        stats.Completions.Should().BeGreaterThan(0);
        stats.TotalHours.Should().BeGreaterThan(0);
    }
}
