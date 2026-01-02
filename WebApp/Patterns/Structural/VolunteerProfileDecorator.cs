using WebApp.Models;

namespace WebApp.Patterns.Structural
{

    public interface IVolunteerProfileService
    {
        Task<Volunteer> GetVolunteerProfileAsync(int volunteerId);
        Task<string> FormatVolunteerSummaryAsync(Volunteer volunteer);
    }

    
    public class BasicVolunteerProfileService : IVolunteerProfileService
    {
        public async Task<Volunteer> GetVolunteerProfileAsync(int volunteerId)
        {
            
            await Task.CompletedTask;
            return new Volunteer { Id = volunteerId };
        }

        public async Task<string> FormatVolunteerSummaryAsync(Volunteer volunteer)
        {
            await Task.CompletedTask;
            return $"{volunteer.FirstName} {volunteer.LastName} - {volunteer.Email}";
        }
    }

   
    public abstract class VolunteerProfileDecorator : IVolunteerProfileService
    {
        protected readonly IVolunteerProfileService _wrappedService;

        protected VolunteerProfileDecorator(IVolunteerProfileService wrappedService)
        {
            _wrappedService = wrappedService ?? throw new ArgumentNullException(nameof(wrappedService));
        }

        public virtual async Task<Volunteer> GetVolunteerProfileAsync(int volunteerId)
        {
            return await _wrappedService.GetVolunteerProfileAsync(volunteerId);
        }

        public virtual async Task<string> FormatVolunteerSummaryAsync(Volunteer volunteer)
        {
            return await _wrappedService.FormatVolunteerSummaryAsync(volunteer);
        }
    }

   
    public class LoggingVolunteerProfileDecorator : VolunteerProfileDecorator
    {
        private readonly ILogger<LoggingVolunteerProfileDecorator> _logger;

        public LoggingVolunteerProfileDecorator(
            IVolunteerProfileService wrappedService,
            ILogger<LoggingVolunteerProfileDecorator> logger)
            : base(wrappedService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task<Volunteer> GetVolunteerProfileAsync(int volunteerId)
        {
            _logger.LogInformation("[VOLUNTEER PROFILE] Retrieving profile for volunteer {VolunteerId}", volunteerId);
            var volunteer = await base.GetVolunteerProfileAsync(volunteerId);
            _logger.LogInformation("[VOLUNTEER PROFILE] Successfully retrieved profile for {VolunteerId}", volunteerId);
            return volunteer;
        }

        public override async Task<string> FormatVolunteerSummaryAsync(Volunteer volunteer)
        {
            _logger.LogDebug("[VOLUNTEER PROFILE] Formatting summary for volunteer {VolunteerId}", volunteer.Id);
            var summary = await base.FormatVolunteerSummaryAsync(volunteer);
            _logger.LogDebug("[VOLUNTEER PROFILE] Summary formatted: {Summary}", summary);
            return summary;
        }
    }

    
    public class EnrichedVolunteerProfileDecorator : VolunteerProfileDecorator
    {
        private readonly ILogger<EnrichedVolunteerProfileDecorator> _logger;

        public EnrichedVolunteerProfileDecorator(
            IVolunteerProfileService wrappedService,
            ILogger<EnrichedVolunteerProfileDecorator> logger)
            : base(wrappedService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task<string> FormatVolunteerSummaryAsync(Volunteer volunteer)
        {
            var baseSummary = await base.FormatVolunteerSummaryAsync(volunteer);
            
            
            var skillsSummary = volunteer.Skills?.Any() == true 
                ? $" | Skills: {string.Join(", ", volunteer.Skills)}" 
                : " | Skills: None";
            
            var hoursSummary = $" | Hours: {volunteer.VolunteerHours}";
            
            var enrichedSummary = baseSummary + skillsSummary + hoursSummary;
            
            _logger.LogDebug("[VOLUNTEER PROFILE] Enriched summary created for volunteer {VolunteerId}", volunteer.Id);
            
            return enrichedSummary;
        }
    }

    
    public class ValidatingVolunteerProfileDecorator : VolunteerProfileDecorator
    {
        private readonly ILogger<ValidatingVolunteerProfileDecorator> _logger;

        public ValidatingVolunteerProfileDecorator(
            IVolunteerProfileService wrappedService,
            ILogger<ValidatingVolunteerProfileDecorator> logger)
            : base(wrappedService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task<Volunteer> GetVolunteerProfileAsync(int volunteerId)
        {
            if (volunteerId <= 0)
            {
                _logger.LogWarning("[VOLUNTEER PROFILE] Invalid volunteer ID: {VolunteerId}", volunteerId);
                throw new ArgumentException("Volunteer ID must be positive", nameof(volunteerId));
            }

            var volunteer = await base.GetVolunteerProfileAsync(volunteerId);

            if (volunteer == null)
            {
                _logger.LogWarning("[VOLUNTEER PROFILE] Volunteer not found: {VolunteerId}", volunteerId);
                throw new InvalidOperationException($"Volunteer {volunteerId} not found");
            }

            _logger.LogDebug("[VOLUNTEER PROFILE] Validation passed for volunteer {VolunteerId}", volunteerId);
            return volunteer;
        }
    }
}
