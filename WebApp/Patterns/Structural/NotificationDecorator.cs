using WebApp.Models;

namespace WebApp.Patterns.Structural
{

    public interface INotificationService
    {

        Task NotifyApplicationApprovedAsync(Application application);
        Task NotifyApplicationRejectedAsync(Application application);      
        Task NotifyApplicationSubmittedAsync(Application application);
        Task NotifyApplicationWithdrawnAsync(Application application);
    }
    public class BaseNotificationService : INotificationService
    {
        public virtual Task NotifyApplicationApprovedAsync(Application application)
        {
            return Task.CompletedTask;
        }

        public virtual Task NotifyApplicationRejectedAsync(Application application)
        {
            return Task.CompletedTask;
        }

        public virtual Task NotifyApplicationSubmittedAsync(Application application)
        {
            return Task.CompletedTask;
        }

        public virtual Task NotifyApplicationWithdrawnAsync(Application application)
        {
            return Task.CompletedTask;
        }
    }

    // Abstract decorator base class
    // All concrete decorators inherit from this
    public abstract class NotificationDecorator : INotificationService
    {
        protected readonly INotificationService _wrappedService;

        protected NotificationDecorator(INotificationService wrappedService)
        {
            _wrappedService = wrappedService ?? throw new ArgumentNullException(nameof(wrappedService));
        }

        public virtual async Task NotifyApplicationApprovedAsync(Application application)
        {
            await _wrappedService.NotifyApplicationApprovedAsync(application);
        }

        public virtual async Task NotifyApplicationRejectedAsync(Application application)
        {
            await _wrappedService.NotifyApplicationRejectedAsync(application);
        }

        public virtual async Task NotifyApplicationSubmittedAsync(Application application)
        {
            await _wrappedService.NotifyApplicationSubmittedAsync(application);
        }

        public virtual async Task NotifyApplicationWithdrawnAsync(Application application)
        {
            await _wrappedService.NotifyApplicationWithdrawnAsync(application);
        }
    }

    public class LoggingNotificationDecorator : NotificationDecorator
    {
        private readonly ILogger<LoggingNotificationDecorator> _logger;

        public LoggingNotificationDecorator(
            INotificationService wrappedService,
            ILogger<LoggingNotificationDecorator> logger) 
            : base(wrappedService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task NotifyApplicationApprovedAsync(Application application)
        {
            _logger.LogInformation(
                "[NOTIFICATION] Application #{ApplicationId} APPROVED - Volunteer: {VolunteerId}, Project: {ProjectId}",
                application.Id, application.VolunteerId, application.ProjectId);
            
            await base.NotifyApplicationApprovedAsync(application);
        }

        public override async Task NotifyApplicationRejectedAsync(Application application)
        {
            _logger.LogInformation(
                "[NOTIFICATION] Application #{ApplicationId} REJECTED - Volunteer: {VolunteerId}, Project: {ProjectId}",
                application.Id, application.VolunteerId, application.ProjectId);
            
            await base.NotifyApplicationRejectedAsync(application);
        }

        public override async Task NotifyApplicationSubmittedAsync(Application application)
        {
            _logger.LogInformation(
                "[NOTIFICATION] Application #{ApplicationId} SUBMITTED - Volunteer: {VolunteerId}, Project: {ProjectId}",
                application.Id, application.VolunteerId, application.ProjectId);
            
            await base.NotifyApplicationSubmittedAsync(application);
        }

        public override async Task NotifyApplicationWithdrawnAsync(Application application)
        {
            _logger.LogInformation(
                "[NOTIFICATION] Application #{ApplicationId} WITHDRAWN - Volunteer: {VolunteerId}, Project: {ProjectId}",
                application.Id, application.VolunteerId, application.ProjectId);
            
            await base.NotifyApplicationWithdrawnAsync(application);
        }
    }


    public class EmailNotificationDecorator : NotificationDecorator
    {
        private readonly ILogger<EmailNotificationDecorator> _logger;

        public EmailNotificationDecorator(
            INotificationService wrappedService,
            ILogger<EmailNotificationDecorator> logger) 
            : base(wrappedService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task NotifyApplicationApprovedAsync(Application application)
        {
            // Simulate sending email
            await SendEmailAsync(
                application.Volunteer?.Email ?? "unknown@email.com",
                "Application Approved!",
                $"Congratulations! Your application #{application.Id} for project '{application.Project?.Title}' has been approved.");

            await base.NotifyApplicationApprovedAsync(application);
        }

        public override async Task NotifyApplicationRejectedAsync(Application application)
        {
            await SendEmailAsync(
                application.Volunteer?.Email ?? "unknown@email.com",
                "Application Status Update",
                $"Your application #{application.Id} for project '{application.Project?.Title}' was not selected at this time.");

            await base.NotifyApplicationRejectedAsync(application);
        }

        public override async Task NotifyApplicationSubmittedAsync(Application application)
        {
            // Notify volunteer about submission
            await SendEmailAsync(
                application.Volunteer?.Email ?? "unknown@email.com",
                "Application Received",
                $"Thank you for applying! Your application #{application.Id} has been submitted for review.");

            // Notify organization about new application
            await SendEmailAsync(
                application.Project?.Organization?.Email ?? "unknown@email.com",
                "New Volunteer Application",
                $"A new volunteer has applied for your project '{application.Project?.Title}'.");

            await base.NotifyApplicationSubmittedAsync(application);
        }

        public override async Task NotifyApplicationWithdrawnAsync(Application application)
        {
            await SendEmailAsync(
                application.Volunteer?.Email ?? "unknown@email.com",
                "Application Withdrawn",
                $"Your application #{application.Id} has been successfully withdrawn.");

            await base.NotifyApplicationWithdrawnAsync(application);
        }

        private Task SendEmailAsync(string to, string subject, string body)
        {
            // Simulate email sending
            _logger.LogDebug(
                "[EMAIL] To: {To}, Subject: {Subject}, Body: {Body}",
                to, subject, body);
            
            return Task.CompletedTask;
        }
    }

    public class StatisticsNotificationDecorator : NotificationDecorator
    {
        private readonly ILogger<StatisticsNotificationDecorator> _logger;
        private static int _approvedCount;
        private static int _rejectedCount;
        private static int _submittedCount;
        private static int _withdrawnCount;

        public StatisticsNotificationDecorator(
            INotificationService wrappedService,
            ILogger<StatisticsNotificationDecorator> logger)
            : base(wrappedService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task NotifyApplicationApprovedAsync(Application application)
        {
            Interlocked.Increment(ref _approvedCount);
            _logger.LogDebug("[STATS] Total approved applications: {Count}", _approvedCount);
            await base.NotifyApplicationApprovedAsync(application);
        }

        public override async Task NotifyApplicationRejectedAsync(Application application)
        {
            Interlocked.Increment(ref _rejectedCount);
            _logger.LogDebug("[STATS] Total rejected applications: {Count}", _rejectedCount);
            await base.NotifyApplicationRejectedAsync(application);
        }

        public override async Task NotifyApplicationSubmittedAsync(Application application)
        {
            Interlocked.Increment(ref _submittedCount);
            _logger.LogDebug("[STATS] Total submitted applications: {Count}", _submittedCount);
            await base.NotifyApplicationSubmittedAsync(application);
        }

        public override async Task NotifyApplicationWithdrawnAsync(Application application)
        {
            Interlocked.Increment(ref _withdrawnCount);
            _logger.LogDebug("[STATS] Total withdrawn applications: {Count}", _withdrawnCount);
            await base.NotifyApplicationWithdrawnAsync(application);
        }

        public static (int Approved, int Rejected, int Submitted, int Withdrawn) GetStatistics()
        {
            return (_approvedCount, _rejectedCount, _submittedCount, _withdrawnCount);
        }
    }

}
