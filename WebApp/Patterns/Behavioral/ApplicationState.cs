using WebApp.Models;

namespace WebApp.Patterns.Behavioral
{
    public interface IApplicationState
    {
        ApplicationStatus Status { get; }

        // Attempt to approve the application
        Task<bool> ApproveAsync(ApplicationStateContext context, string? reviewNotes);


        // Attempt to reject the application
        Task<bool> RejectAsync(ApplicationStateContext context, string? reviewNotes);


        // Attempt to withdraw the application
        Task<bool> WithdrawAsync(ApplicationStateContext context);

        // Attempt to complete the application
        Task<bool> CompleteAsync(ApplicationStateContext context);

        bool CanApprove();

        bool CanReject();

        bool CanWithdraw();

        bool CanComplete();
    }

    //delegates behavior to state objects
    public class ApplicationStateContext
    {
        private IApplicationState _currentState;
        private readonly IApplicationStateFactory _stateFactory;
        private readonly ILogger<ApplicationStateContext> _logger;

        public Application Application { get; }

        public ApplicationStateContext(
            Application application,
            IApplicationStateFactory stateFactory,
            ILogger<ApplicationStateContext> logger)
        {
            Application = application ?? throw new ArgumentNullException(nameof(application));
            _stateFactory = stateFactory ?? throw new ArgumentNullException(nameof(stateFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _currentState = _stateFactory.CreateState(application.Status);
        }

        public ApplicationStatus CurrentStatus => _currentState.Status;

        public void TransitionTo(ApplicationStatus newStatus)
        {
            var previousStatus = _currentState.Status;
            _currentState = _stateFactory.CreateState(newStatus);
            Application.Status = newStatus;

            _logger.LogInformation(
                "Application #{ApplicationId} transitioned from {PreviousStatus} to {NewStatus}",
                Application.Id, previousStatus, newStatus);
        }

        // Delegate methods to current state
        public Task<bool> ApproveAsync(string? reviewNotes) => _currentState.ApproveAsync(this, reviewNotes);
        public Task<bool> RejectAsync(string? reviewNotes) => _currentState.RejectAsync(this, reviewNotes);
        public Task<bool> WithdrawAsync() => _currentState.WithdrawAsync(this);
        public Task<bool> CompleteAsync() => _currentState.CompleteAsync(this);
        
        public bool CanApprove() => _currentState.CanApprove();
        public bool CanReject() => _currentState.CanReject();
        public bool CanWithdraw() => _currentState.CanWithdraw();
        public bool CanComplete() => _currentState.CanComplete();
    }

    //Factory for creating state instances
    public interface IApplicationStateFactory
    {
        IApplicationState CreateState(ApplicationStatus status);
    }

    //Factory implementation that creates appropriate state objects
    public class ApplicationStateFactory : IApplicationStateFactory
    {
        private readonly ILogger<ApplicationStateContext> _contextLogger;

        public ApplicationStateFactory(ILogger<ApplicationStateContext> contextLogger)
        {
            _contextLogger = contextLogger;
        }

        public IApplicationState CreateState(ApplicationStatus status)
        {
            return status switch
            {
                ApplicationStatus.Pending => new PendingState(),
                ApplicationStatus.Accepted => new AcceptedState(),
                ApplicationStatus.Rejected => new RejectedState(),
                ApplicationStatus.Withdrawn => new WithdrawnState(),
                ApplicationStatus.Completed => new CompletedState(),
                _ => throw new ArgumentException($"Unknown application status: {status}", nameof(status))
            };
        }
    }

    // Abstract base state providing default behavior
    public abstract class ApplicationStateBase : IApplicationState
    {
        public abstract ApplicationStatus Status { get; }

        public virtual Task<bool> ApproveAsync(ApplicationStateContext context, string? reviewNotes)
        {
            return Task.FromResult(false);
        }

        public virtual Task<bool> RejectAsync(ApplicationStateContext context, string? reviewNotes)
        {
            return Task.FromResult(false);
        }

        public virtual Task<bool> WithdrawAsync(ApplicationStateContext context)
        {
            return Task.FromResult(false);
        }

        public virtual Task<bool> CompleteAsync(ApplicationStateContext context)
        {
            return Task.FromResult(false);
        }

        public virtual bool CanApprove() => false;
        public virtual bool CanReject() => false;
        public virtual bool CanWithdraw() => false;
        public virtual bool CanComplete() => false;
    }

    public class PendingState : ApplicationStateBase
    {
        public override ApplicationStatus Status => ApplicationStatus.Pending;

        public override Task<bool> ApproveAsync(ApplicationStateContext context, string? reviewNotes)
        {
            context.Application.ReviewedAt = DateTime.UtcNow;
            context.Application.ReviewNotes = reviewNotes;
            context.TransitionTo(ApplicationStatus.Accepted);
            return Task.FromResult(true);
        }

        public override Task<bool> RejectAsync(ApplicationStateContext context, string? reviewNotes)
        {
            context.Application.ReviewedAt = DateTime.UtcNow;
            context.Application.ReviewNotes = reviewNotes;
            context.TransitionTo(ApplicationStatus.Rejected);
            return Task.FromResult(true);
        }

        public override Task<bool> WithdrawAsync(ApplicationStateContext context)
        {
            context.TransitionTo(ApplicationStatus.Withdrawn);
            return Task.FromResult(true);
        }

        public override bool CanApprove() => true;
        public override bool CanReject() => true;
        public override bool CanWithdraw() => true;
    }

    public class AcceptedState : ApplicationStateBase
    {
        public override ApplicationStatus Status => ApplicationStatus.Accepted;

        public override Task<bool> WithdrawAsync(ApplicationStateContext context)
        {
            context.TransitionTo(ApplicationStatus.Withdrawn);
            return Task.FromResult(true);
        }

        public override Task<bool> CompleteAsync(ApplicationStateContext context)
        {
            context.TransitionTo(ApplicationStatus.Completed);
            return Task.FromResult(true);
        }

        public override bool CanWithdraw() => true;
        public override bool CanComplete() => true;
    }


    public class RejectedState : ApplicationStateBase
    {
        public override ApplicationStatus Status => ApplicationStatus.Rejected;
    }

    public class WithdrawnState : ApplicationStateBase
    {
        public override ApplicationStatus Status => ApplicationStatus.Withdrawn;
    }

    public class CompletedState : ApplicationStateBase
    {
        public override ApplicationStatus Status => ApplicationStatus.Completed;
    }

    // Factory interface for creating ApplicationStateContext instances
    // This allows proper dependency injection
    public interface IApplicationStateContextFactory
    {
        ApplicationStateContext CreateContext(Application application);
    }


    // Factory implementation for creating context with proper dependencies
    public class ApplicationStateContextFactory : IApplicationStateContextFactory
    {
        private readonly IApplicationStateFactory _stateFactory;
        private readonly ILogger<ApplicationStateContext> _logger;

        public ApplicationStateContextFactory(
            IApplicationStateFactory stateFactory,
            ILogger<ApplicationStateContext> logger)
        {
            _stateFactory = stateFactory;
            _logger = logger;
        }

        public ApplicationStateContext CreateContext(Application application)
        {
            return new ApplicationStateContext(application, _stateFactory, _logger);
        }
    }
}
