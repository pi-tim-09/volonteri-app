using WebApp.Models;

namespace WebApp.Patterns.Behavioral
{
 
    public interface IVolunteerObserver
    {
        Task OnVolunteerRegisteredAsync(Volunteer volunteer);
        Task OnVolunteerSkillsUpdatedAsync(Volunteer volunteer, List<string> newSkills);
        Task OnVolunteerProjectCompletedAsync(Volunteer volunteer, int projectId, int hoursLogged);
    }

   
    public interface IVolunteerEventPublisher
    {
        void Subscribe(IVolunteerObserver observer);
        void Unsubscribe(IVolunteerObserver observer);
        Task NotifyVolunteerRegisteredAsync(Volunteer volunteer);
        Task NotifyVolunteerSkillsUpdatedAsync(Volunteer volunteer, List<string> newSkills);
        Task NotifyVolunteerProjectCompletedAsync(Volunteer volunteer, int projectId, int hoursLogged);
    }

   
    public class VolunteerEventPublisher : IVolunteerEventPublisher
    {
        private readonly List<IVolunteerObserver> _observers = new();
        private readonly ILogger<VolunteerEventPublisher> _logger;

        public VolunteerEventPublisher(ILogger<VolunteerEventPublisher> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Subscribe(IVolunteerObserver observer)
        {
            if (observer == null) throw new ArgumentNullException(nameof(observer));
            
            if (!_observers.Contains(observer))
            {
                _observers.Add(observer);
                _logger.LogInformation("[VOLUNTEER EVENTS] Observer subscribed. Total observers: {Count}", _observers.Count);
            }
        }

        public void Unsubscribe(IVolunteerObserver observer)
        {
            if (observer == null) throw new ArgumentNullException(nameof(observer));
            
            if (_observers.Remove(observer))
            {
                _logger.LogInformation("[VOLUNTEER EVENTS] Observer unsubscribed. Total observers: {Count}", _observers.Count);
            }
        }

        public async Task NotifyVolunteerRegisteredAsync(Volunteer volunteer)
        {
            _logger.LogInformation("[VOLUNTEER EVENTS] Notifying {Count} observers about volunteer registration: {VolunteerId}", 
                _observers.Count, volunteer.Id);

            foreach (var observer in _observers)
            {
                try
                {
                    await observer.OnVolunteerRegisteredAsync(volunteer);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[VOLUNTEER EVENTS] Error notifying observer about registration");
                }
            }
        }

        public async Task NotifyVolunteerSkillsUpdatedAsync(Volunteer volunteer, List<string> newSkills)
        {
            _logger.LogInformation("[VOLUNTEER EVENTS] Notifying {Count} observers about skills update: {VolunteerId}", 
                _observers.Count, volunteer.Id);

            foreach (var observer in _observers)
            {
                try
                {
                    await observer.OnVolunteerSkillsUpdatedAsync(volunteer, newSkills);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[VOLUNTEER EVENTS] Error notifying observer about skills update");
                }
            }
        }

        public async Task NotifyVolunteerProjectCompletedAsync(Volunteer volunteer, int projectId, int hoursLogged)
        {
            _logger.LogInformation("[VOLUNTEER EVENTS] Notifying {Count} observers about project completion: Volunteer {VolunteerId}, Project {ProjectId}", 
                _observers.Count, volunteer.Id, projectId);

            foreach (var observer in _observers)
            {
                try
                {
                    await observer.OnVolunteerProjectCompletedAsync(volunteer, projectId, hoursLogged);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[VOLUNTEER EVENTS] Error notifying observer about project completion");
                }
            }
        }
    }

    
    public class LoggingVolunteerObserver : IVolunteerObserver
    {
        private readonly ILogger<LoggingVolunteerObserver> _logger;

        public LoggingVolunteerObserver(ILogger<LoggingVolunteerObserver> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task OnVolunteerRegisteredAsync(Volunteer volunteer)
        {
            await Task.CompletedTask;
            _logger.LogInformation("[VOLUNTEER LOG] New volunteer registered: {VolunteerId} - {Email}", 
                volunteer.Id, volunteer.Email);
        }

        public async Task OnVolunteerSkillsUpdatedAsync(Volunteer volunteer, List<string> newSkills)
        {
            await Task.CompletedTask;
            _logger.LogInformation("[VOLUNTEER LOG] Skills updated for volunteer {VolunteerId}: {Skills}", 
                volunteer.Id, string.Join(", ", newSkills));
        }

        public async Task OnVolunteerProjectCompletedAsync(Volunteer volunteer, int projectId, int hoursLogged)
        {
            await Task.CompletedTask;
            _logger.LogInformation("[VOLUNTEER LOG] Volunteer {VolunteerId} completed project {ProjectId} - {Hours} hours logged", 
                volunteer.Id, projectId, hoursLogged);
        }
    }

    
    public class StatisticsVolunteerObserver : IVolunteerObserver
    {
        private readonly ILogger<StatisticsVolunteerObserver> _logger;
        private static int _totalRegistrations = 0;
        private static int _totalSkillUpdates = 0;
        private static int _totalProjectCompletions = 0;
        private static int _totalHoursLogged = 0;

        public StatisticsVolunteerObserver(ILogger<StatisticsVolunteerObserver> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task OnVolunteerRegisteredAsync(Volunteer volunteer)
        {
            await Task.CompletedTask;
            Interlocked.Increment(ref _totalRegistrations);
            _logger.LogDebug("[VOLUNTEER STATS] Total registrations: {Count}", _totalRegistrations);
        }

        public async Task OnVolunteerSkillsUpdatedAsync(Volunteer volunteer, List<string> newSkills)
        {
            await Task.CompletedTask;
            Interlocked.Increment(ref _totalSkillUpdates);
            _logger.LogDebug("[VOLUNTEER STATS] Total skill updates: {Count}", _totalSkillUpdates);
        }

        public async Task OnVolunteerProjectCompletedAsync(Volunteer volunteer, int projectId, int hoursLogged)
        {
            await Task.CompletedTask;
            Interlocked.Increment(ref _totalProjectCompletions);
            Interlocked.Add(ref _totalHoursLogged, hoursLogged);
            _logger.LogDebug("[VOLUNTEER STATS] Total completions: {Completions}, Total hours: {Hours}", 
                _totalProjectCompletions, _totalHoursLogged);
        }

        public static (int Registrations, int SkillUpdates, int Completions, int TotalHours) GetStatistics()
        {
            return (_totalRegistrations, _totalSkillUpdates, _totalProjectCompletions, _totalHoursLogged);
        }
    }

    
    public class NotificationVolunteerObserver : IVolunteerObserver
    {
        private readonly ILogger<NotificationVolunteerObserver> _logger;

        public NotificationVolunteerObserver(ILogger<NotificationVolunteerObserver> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task OnVolunteerRegisteredAsync(Volunteer volunteer)
        {
            await Task.CompletedTask;
            
            _logger.LogInformation("[VOLUNTEER NOTIFICATION] Sending welcome email to {Email}", volunteer.Email);
        }

        public async Task OnVolunteerSkillsUpdatedAsync(Volunteer volunteer, List<string> newSkills)
        {
            await Task.CompletedTask;
            
            _logger.LogInformation("[VOLUNTEER NOTIFICATION] Sending skills update confirmation to {Email}", volunteer.Email);
        }

        public async Task OnVolunteerProjectCompletedAsync(Volunteer volunteer, int projectId, int hoursLogged)
        {
            await Task.CompletedTask;
            
            _logger.LogInformation("[VOLUNTEER NOTIFICATION] Sending completion certificate to {Email} for project {ProjectId}", 
                volunteer.Email, projectId);
        }
    }
}
