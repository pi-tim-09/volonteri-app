using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WebApp.Models;
using WebApp.Patterns.Structural;

namespace WebApp.UnitTests.Patterns.Structural;

public class VolunteerProfileDecoratorTests
{
    [Fact]
    public async Task BasicVolunteerProfileService_ReturnsNonEmptySummary()
    {
        var svc = new BasicVolunteerProfileService();
        var v = new Volunteer { FirstName = "Ana", LastName = "Horvat", Email = "a@a.com" };

        var summary = await svc.FormatVolunteerSummaryAsync(v);

        summary.Should().NotBeNullOrWhiteSpace();
        summary.Should().Contain("Ana");
    }

    [Fact]
    public async Task LoggingVolunteerProfileDecorator_DelegatesToInner()
    {
        var inner = new Mock<IVolunteerProfileService>();
        inner.Setup(x => x.FormatVolunteerSummaryAsync(It.IsAny<Volunteer>())).ReturnsAsync("S");

        var sut = new LoggingVolunteerProfileDecorator(inner.Object, Mock.Of<ILogger<LoggingVolunteerProfileDecorator>>());

        var v = new Volunteer { FirstName = "A" };
        (await sut.FormatVolunteerSummaryAsync(v)).Should().Be("S");
        inner.Verify(x => x.FormatVolunteerSummaryAsync(v), Times.Once);
    }

    [Fact]
    public async Task EnrichedVolunteerProfileDecorator_AddsExtraInfo()
    {
        var inner = new Mock<IVolunteerProfileService>();
        inner.Setup(x => x.FormatVolunteerSummaryAsync(It.IsAny<Volunteer>())).ReturnsAsync("BASE");

        var sut = new EnrichedVolunteerProfileDecorator(
            inner.Object,
            Mock.Of<ILogger<EnrichedVolunteerProfileDecorator>>());

        var v = new Volunteer { FirstName = "A", Skills = new List<string> { "C#" }, VolunteerHours = 10 };
        var summary = await sut.FormatVolunteerSummaryAsync(v);

        summary.Should().Contain("BASE");
        summary.Should().Contain("C#");
        summary.Should().Contain("10");
    }

    [Fact]
    public async Task ValidatingVolunteerProfileDecorator_WhenVolunteerIdInvalid_ThrowsArgumentException()
    {
        var inner = new Mock<IVolunteerProfileService>();
        inner.Setup(x => x.GetVolunteerProfileAsync(It.IsAny<int>())).ReturnsAsync(new Volunteer { Id = 1 });

        var sut = new ValidatingVolunteerProfileDecorator(
            inner.Object,
            Mock.Of<ILogger<ValidatingVolunteerProfileDecorator>>());

        Func<Task> act = async () => await sut.GetVolunteerProfileAsync(0);
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
