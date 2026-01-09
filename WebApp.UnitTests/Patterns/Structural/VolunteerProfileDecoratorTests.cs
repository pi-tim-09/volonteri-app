using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WebApp.Models;
using WebApp.Patterns.Structural;

namespace WebApp.UnitTests.Patterns.Structural;

public class VolunteerProfileDecoratorTests
{
    [Fact]
    public void VolunteerProfileDecorator_Ctor_WhenNullInner_Throws()
    {
        Action act = () => new LoggingVolunteerProfileDecorator(null!, Mock.Of<ILogger<LoggingVolunteerProfileDecorator>>());
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void LoggingVolunteerProfileDecorator_Ctor_WhenNullLogger_Throws()
    {
        Action act = () => new LoggingVolunteerProfileDecorator(Mock.Of<IVolunteerProfileService>(), null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void EnrichedVolunteerProfileDecorator_Ctor_WhenNullLogger_Throws()
    {
        Action act = () => new EnrichedVolunteerProfileDecorator(Mock.Of<IVolunteerProfileService>(), null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ValidatingVolunteerProfileDecorator_Ctor_WhenNullLogger_Throws()
    {
        Action act = () => new ValidatingVolunteerProfileDecorator(Mock.Of<IVolunteerProfileService>(), null!);
        act.Should().Throw<ArgumentNullException>();
    }

    

    [Fact]
    public async Task BasicVolunteerProfileService_GetVolunteerProfileAsync_ReturnsVolunteer()
    {
        var svc = new BasicVolunteerProfileService();
        
        var result = await svc.GetVolunteerProfileAsync(123);

        result.Should().NotBeNull();
        result.Id.Should().Be(123);
    }

    [Fact]
    public async Task BasicVolunteerProfileService_FormatVolunteerSummaryAsync_ReturnsNonEmptySummary()
    {
        var svc = new BasicVolunteerProfileService();
        var v = new Volunteer { FirstName = "Ana", LastName = "Horvat", Email = "a@a.com" };

        var summary = await svc.FormatVolunteerSummaryAsync(v);

        summary.Should().NotBeNullOrWhiteSpace();
        summary.Should().Contain("Ana");
        summary.Should().Contain("Horvat");
        summary.Should().Contain("a@a.com");
    }

    

   

    [Fact]
    public async Task LoggingVolunteerProfileDecorator_GetVolunteerProfileAsync_DelegatesToInner()
    {
        var inner = new Mock<IVolunteerProfileService>();
        var expectedVolunteer = new Volunteer { Id = 5 };
        inner.Setup(x => x.GetVolunteerProfileAsync(5)).ReturnsAsync(expectedVolunteer);

        var sut = new LoggingVolunteerProfileDecorator(inner.Object, Mock.Of<ILogger<LoggingVolunteerProfileDecorator>>());

        var result = await sut.GetVolunteerProfileAsync(5);

        result.Should().BeSameAs(expectedVolunteer);
        inner.Verify(x => x.GetVolunteerProfileAsync(5), Times.Once);
    }

    [Fact]
    public async Task LoggingVolunteerProfileDecorator_FormatVolunteerSummaryAsync_DelegatesToInner()
    {
        var inner = new Mock<IVolunteerProfileService>();
        inner.Setup(x => x.FormatVolunteerSummaryAsync(It.IsAny<Volunteer>())).ReturnsAsync("SUMMARY");

        var sut = new LoggingVolunteerProfileDecorator(inner.Object, Mock.Of<ILogger<LoggingVolunteerProfileDecorator>>());

        var v = new Volunteer { Id = 1, FirstName = "A" };
        var result = await sut.FormatVolunteerSummaryAsync(v);
        
        result.Should().Be("SUMMARY");
        inner.Verify(x => x.FormatVolunteerSummaryAsync(v), Times.Once);
    }

   

  

    [Fact]
    public async Task EnrichedVolunteerProfileDecorator_GetVolunteerProfileAsync_DelegatesToInner()
    {
        var inner = new Mock<IVolunteerProfileService>();
        var expectedVolunteer = new Volunteer { Id = 10 };
        inner.Setup(x => x.GetVolunteerProfileAsync(10)).ReturnsAsync(expectedVolunteer);

        var sut = new EnrichedVolunteerProfileDecorator(inner.Object, Mock.Of<ILogger<EnrichedVolunteerProfileDecorator>>());

        var result = await sut.GetVolunteerProfileAsync(10);

        result.Should().BeSameAs(expectedVolunteer);
        inner.Verify(x => x.GetVolunteerProfileAsync(10), Times.Once);
    }

    [Fact]
    public async Task EnrichedVolunteerProfileDecorator_FormatVolunteerSummaryAsync_AddsSkillsAndHours()
    {
        var inner = new Mock<IVolunteerProfileService>();
        inner.Setup(x => x.FormatVolunteerSummaryAsync(It.IsAny<Volunteer>())).ReturnsAsync("BASE");

        var sut = new EnrichedVolunteerProfileDecorator(
            inner.Object,
            Mock.Of<ILogger<EnrichedVolunteerProfileDecorator>>());

        var v = new Volunteer 
        { 
            Id = 1,
            FirstName = "A", 
            Skills = new List<string> { "C#", "ASP.NET" }, 
            VolunteerHours = 10 
        };
        
        var summary = await sut.FormatVolunteerSummaryAsync(v);

        summary.Should().Contain("BASE");
        summary.Should().Contain("C#");
        summary.Should().Contain("ASP.NET");
        summary.Should().Contain("10");
        summary.Should().Contain("Skills:");
        summary.Should().Contain("Hours:");
    }

    [Fact]
    public async Task EnrichedVolunteerProfileDecorator_WhenNoSkills_ShowsNone()
    {
        var inner = new Mock<IVolunteerProfileService>();
        inner.Setup(x => x.FormatVolunteerSummaryAsync(It.IsAny<Volunteer>())).ReturnsAsync("BASE");

        var sut = new EnrichedVolunteerProfileDecorator(
            inner.Object,
            Mock.Of<ILogger<EnrichedVolunteerProfileDecorator>>());

        var v = new Volunteer 
        { 
            Id = 1,
            FirstName = "A",
            Skills = new List<string>(),
            VolunteerHours = 0
        };
        
        var summary = await sut.FormatVolunteerSummaryAsync(v);

        summary.Should().Contain("BASE");
        summary.Should().Contain("Skills: None");
        summary.Should().Contain("Hours: 0");
    }

 

    

    [Fact]
    public async Task ValidatingVolunteerProfileDecorator_GetVolunteerProfileAsync_WhenIdZero_ThrowsArgumentException()
    {
        var inner = new Mock<IVolunteerProfileService>();
        inner.Setup(x => x.GetVolunteerProfileAsync(It.IsAny<int>())).ReturnsAsync(new Volunteer { Id = 1 });

        var sut = new ValidatingVolunteerProfileDecorator(
            inner.Object,
            Mock.Of<ILogger<ValidatingVolunteerProfileDecorator>>());

        Func<Task> act = async () => await sut.GetVolunteerProfileAsync(0);
        
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Volunteer ID must be positive*");
    }

    [Fact]
    public async Task ValidatingVolunteerProfileDecorator_GetVolunteerProfileAsync_WhenIdNegative_ThrowsArgumentException()
    {
        var inner = new Mock<IVolunteerProfileService>();

        var sut = new ValidatingVolunteerProfileDecorator(
            inner.Object,
            Mock.Of<ILogger<ValidatingVolunteerProfileDecorator>>());

        Func<Task> act = async () => await sut.GetVolunteerProfileAsync(-5);
        
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ValidatingVolunteerProfileDecorator_GetVolunteerProfileAsync_WhenVolunteerNull_ThrowsInvalidOperationException()
    {
        var inner = new Mock<IVolunteerProfileService>();
        inner.Setup(x => x.GetVolunteerProfileAsync(It.IsAny<int>())).ReturnsAsync((Volunteer?)null);

        var sut = new ValidatingVolunteerProfileDecorator(
            inner.Object,
            Mock.Of<ILogger<ValidatingVolunteerProfileDecorator>>());

        Func<Task> act = async () => await sut.GetVolunteerProfileAsync(10);
        
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Volunteer 10 not found*");
    }

    [Fact]
    public async Task ValidatingVolunteerProfileDecorator_GetVolunteerProfileAsync_WhenValid_ReturnsVolunteer()
    {
        var inner = new Mock<IVolunteerProfileService>();
        var expectedVolunteer = new Volunteer { Id = 5 };
        inner.Setup(x => x.GetVolunteerProfileAsync(5)).ReturnsAsync(expectedVolunteer);

        var sut = new ValidatingVolunteerProfileDecorator(
            inner.Object,
            Mock.Of<ILogger<ValidatingVolunteerProfileDecorator>>());

        var result = await sut.GetVolunteerProfileAsync(5);

        result.Should().BeSameAs(expectedVolunteer);
        inner.Verify(x => x.GetVolunteerProfileAsync(5), Times.Once);
    }

    [Fact]
    public async Task ValidatingVolunteerProfileDecorator_FormatVolunteerSummaryAsync_DelegatesToInner()
    {
        var inner = new Mock<IVolunteerProfileService>();
        inner.Setup(x => x.FormatVolunteerSummaryAsync(It.IsAny<Volunteer>())).ReturnsAsync("SUMMARY");

        var sut = new ValidatingVolunteerProfileDecorator(
            inner.Object,
            Mock.Of<ILogger<ValidatingVolunteerProfileDecorator>>());

        var v = new Volunteer { Id = 1, FirstName = "Test" };
        var result = await sut.FormatVolunteerSummaryAsync(v);

        result.Should().Be("SUMMARY");
        inner.Verify(x => x.FormatVolunteerSummaryAsync(v), Times.Once);
    }

    
}
