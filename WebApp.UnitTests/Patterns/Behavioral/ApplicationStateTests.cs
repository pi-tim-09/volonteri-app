using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WebApp.Models;
using WebApp.Patterns.Behavioral;

namespace WebApp.UnitTests.Patterns.Behavioral;

public class ApplicationStateTests
{
    [Fact]
    public void ApplicationStateFactory_WhenUnknownStatus_Throws()
    {
        var factory = new ApplicationStateFactory(Mock.Of<ILogger<ApplicationStateContext>>());

        Action act = () => factory.CreateState((ApplicationStatus)999);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task PendingState_Approve_TransitionsToAccepted_AndSetsReviewFields()
    {
        var app = new Application { Id = 1, Status = ApplicationStatus.Pending };
        var factory = new ApplicationStateFactory(Mock.Of<ILogger<ApplicationStateContext>>());
        var ctx = new ApplicationStateContext(app, factory, Mock.Of<ILogger<ApplicationStateContext>>());

        var ok = await ctx.ApproveAsync("notes");

        ok.Should().BeTrue();
        ctx.CurrentStatus.Should().Be(ApplicationStatus.Accepted);
        app.Status.Should().Be(ApplicationStatus.Accepted);
        app.ReviewNotes.Should().Be("notes");
        app.ReviewedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task AcceptedState_Complete_TransitionsToCompleted()
    {
        var app = new Application { Id = 2, Status = ApplicationStatus.Accepted };
        var factory = new ApplicationStateFactory(Mock.Of<ILogger<ApplicationStateContext>>());
        var ctx = new ApplicationStateContext(app, factory, Mock.Of<ILogger<ApplicationStateContext>>());

        (await ctx.CompleteAsync()).Should().BeTrue();
        ctx.CurrentStatus.Should().Be(ApplicationStatus.Completed);
    }

    [Fact]
    public async Task AcceptedState_Withdraw_TransitionsToWithdrawn()
    {
        var app = new Application { Id = 3, Status = ApplicationStatus.Accepted };
        var factory = new ApplicationStateFactory(Mock.Of<ILogger<ApplicationStateContext>>());
        var ctx = new ApplicationStateContext(app, factory, Mock.Of<ILogger<ApplicationStateContext>>());

        (await ctx.WithdrawAsync()).Should().BeTrue();
        ctx.CurrentStatus.Should().Be(ApplicationStatus.Withdrawn);
    }

    [Fact]
    public async Task RejectedState_CannotApproveRejectWithdrawComplete()
    {
        var app = new Application { Id = 4, Status = ApplicationStatus.Rejected };
        var factory = new ApplicationStateFactory(Mock.Of<ILogger<ApplicationStateContext>>());
        var ctx = new ApplicationStateContext(app, factory, Mock.Of<ILogger<ApplicationStateContext>>());

        ctx.CanApprove().Should().BeFalse();
        ctx.CanReject().Should().BeFalse();
        ctx.CanWithdraw().Should().BeFalse();
        ctx.CanComplete().Should().BeFalse();

        (await ctx.ApproveAsync("x")).Should().BeFalse();
        (await ctx.RejectAsync("x")).Should().BeFalse();
        (await ctx.WithdrawAsync()).Should().BeFalse();
        (await ctx.CompleteAsync()).Should().BeFalse();
    }

    [Fact]
    public void ApplicationStateContext_WhenNullDependencies_Throws()
    {
        var app = new Application { Id = 1, Status = ApplicationStatus.Pending };

        Action a1 = () => new ApplicationStateContext(null!, Mock.Of<IApplicationStateFactory>(), Mock.Of<ILogger<ApplicationStateContext>>());
        Action a2 = () => new ApplicationStateContext(app, null!, Mock.Of<ILogger<ApplicationStateContext>>());
        Action a3 = () => new ApplicationStateContext(app, Mock.Of<IApplicationStateFactory>(), null!);

        a1.Should().Throw<ArgumentNullException>();
        a2.Should().Throw<ArgumentNullException>();
        a3.Should().Throw<ArgumentNullException>();
    }
}
