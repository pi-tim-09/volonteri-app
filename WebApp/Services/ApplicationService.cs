using WebApp.Interfaces;
using WebApp.Interfaces.Services;
using WebApp.Models;
using WebApp.Patterns.Behavioral;
using WebApp.Patterns.Structural;

namespace WebApp.Services
{
    /// <summary>
    /// Service implementation for Application-related business logic
    /// Follows Single Responsibility Principle - handles only application-related business operations
    /// Depends on IProjectService for project-related validation (Dependency Inversion Principle)
    /// Delegates data access to repositories
    /// </summary>
    public class ApplicationService : IApplicationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IProjectService _projectService;
        private readonly IApplicationStateContextFactory _stateContextFactory;
        private readonly INotificationService _notificationService;
        private readonly ILogger<ApplicationService> _logger;

        public ApplicationService(
            IUnitOfWork unitOfWork,
            IProjectService projectService,
            IApplicationStateContextFactory stateContextFactory,
            INotificationService notificationService,
            ILogger<ApplicationService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _projectService = projectService ?? throw new ArgumentNullException(nameof(projectService));
            _stateContextFactory = stateContextFactory ?? throw new ArgumentNullException(nameof(stateContextFactory));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // CRUD Operations with Business Logic
        public async Task<Application> CreateApplicationAsync(int volunteerId, int projectId)
        {
            try
            {
                // Business validation
                if (!await CanApplyToProjectAsync(volunteerId, projectId))
                {
                    throw new InvalidOperationException("Volunteer cannot apply to this project.");
                }

                var application = new Application
                {
                    VolunteerId = volunteerId,
                    ProjectId = projectId,
                    AppliedAt = DateTime.UtcNow,
                    Status = ApplicationStatus.Pending
                };

                // Delegate to repository
                var created = await _unitOfWork.Applications.AddAsync(application);
                await _unitOfWork.SaveChangesAsync();

                // Decorator Pattern
                await _notificationService.NotifyApplicationSubmittedAsync(created);

                _logger.LogInformation("Created new application for volunteer {VolunteerId} to project {ProjectId}",
                    volunteerId, projectId);
                return created;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating application");
                throw;
            }
        }

        public async Task<bool> DeleteApplicationAsync(int id)
        {
            try
            {
                // Delegate to repository
                var application = await _unitOfWork.Applications.GetByIdAsync(id);
                if (application == null)
                    return false;

                // Business rule: if application was accepted, decrement volunteer count
                if (application.Status == ApplicationStatus.Accepted)
                {
                    await _projectService.DecrementVolunteerCountAsync(application.ProjectId);
                }

                _unitOfWork.Applications.Remove(application);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Deleted application: {ApplicationId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting application: {ApplicationId}", id);
                throw;
            }
        }

        public async Task<Application?> GetApplicationByIdAsync(int id)
        {
            try
            {
                // Delegate to repository
                return await _unitOfWork.Applications.GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving application by ID: {ApplicationId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<Application>> GetFilteredApplicationsAsync(int? projectId, ApplicationStatus? status)
        {
            try
            {
                // Delegate to repository
                IEnumerable<Application> applications;

                if (projectId.HasValue && status.HasValue)
                {
                    var projectApps = await _unitOfWork.Applications.GetApplicationsByProjectAsync(projectId.Value);
                    applications = projectApps.Where(a => a.Status == status.Value);
                }
                else if (projectId.HasValue)
                {
                    applications = await _unitOfWork.Applications.GetApplicationsByProjectAsync(projectId.Value);
                }
                else if (status.HasValue)
                {
                    applications = await _unitOfWork.Applications.GetApplicationsByStatusAsync(status.Value);
                }
                else
                {
                    applications = await _unitOfWork.Applications.GetAllAsync();
                }

                return applications;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filtering applications");
                throw;
            }
        }

        // Business Logic Operations 
        public async Task<bool> ApproveApplicationAsync(int id, string? reviewNotes)
        {
            try
            {
                // Business validation
                if (!await CanApproveApplicationAsync(id))
                {
                    return false;
                }

                // Delegate to repository
                var application = await _unitOfWork.Applications.GetByIdAsync(id);
                if (application == null)
                    return false;

                // State Pattern
                var stateContext = _stateContextFactory.CreateContext(application);
                var approved = await stateContext.ApproveAsync(reviewNotes);

                if (!approved)
                {
                    _logger.LogWarning("State transition to Approved failed for application {ApplicationId}", id);
                    return false;
                }

                _unitOfWork.Applications.Update(application);

                // Business logic: increment volunteer count in project
                await _projectService.IncrementVolunteerCountAsync(application.ProjectId);

                await _unitOfWork.SaveChangesAsync();

                // Decorator Pattern
                await _notificationService.NotifyApplicationApprovedAsync(application);

                _logger.LogInformation("Approved application: {ApplicationId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving application: {ApplicationId}", id);
                throw;
            }
        }

        public async Task<bool> RejectApplicationAsync(int id, string? reviewNotes)
        {
            try
            {
                // Business validation
                if (!await CanRejectApplicationAsync(id))
                {
                    return false;
                }

                // Delegate to repository
                var application = await _unitOfWork.Applications.GetByIdAsync(id);
                if (application == null)
                    return false;

                // State Pattern
                var stateContext = _stateContextFactory.CreateContext(application);
                var rejected = await stateContext.RejectAsync(reviewNotes);

                if (!rejected)
                {
                    _logger.LogWarning("State transition to Rejected failed for application {ApplicationId}", id);
                    return false;
                }

                _unitOfWork.Applications.Update(application);
                await _unitOfWork.SaveChangesAsync();

                // Decorator Pattern
                await _notificationService.NotifyApplicationRejectedAsync(application);

                _logger.LogInformation("Rejected application: {ApplicationId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting application: {ApplicationId}", id);
                throw;
            }
        }

        public async Task<bool> WithdrawApplicationAsync(int id)
        {
            try
            {
                // Delegate to repository
                var application = await _unitOfWork.Applications.GetByIdAsync(id);
                if (application == null)
                    return false;

                var previousStatus = application.Status;

                // State Pattern
                var stateContext = _stateContextFactory.CreateContext(application);
                var withdrawn = await stateContext.WithdrawAsync();

                if (!withdrawn)
                {
                    _logger.LogWarning("State transition to Withdrawn failed for application {ApplicationId}", id);
                    return false;
                }

                _unitOfWork.Applications.Update(application);

                // Business rule: if application was accepted, decrement volunteer count
                if (previousStatus == ApplicationStatus.Accepted)
                {
                    await _projectService.DecrementVolunteerCountAsync(application.ProjectId);
                }

                await _unitOfWork.SaveChangesAsync();

                // Decorator Pattern
                await _notificationService.NotifyApplicationWithdrawnAsync(application);

                _logger.LogInformation("Withdrew application: {ApplicationId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error withdrawing application: {ApplicationId}", id);
                throw;
            }
        }

        // Validation & Business Rules
        public async Task<bool> CanApplyToProjectAsync(int volunteerId, int projectId)
        {
            try
            {
                // Business rule: check if project can accept volunteers
                if (!await _projectService.CanAcceptVolunteersAsync(projectId))
                    return false;

                // Business rule: check if volunteer exists and is active
                var volunteer = await _unitOfWork.Volunteers.GetByIdAsync(volunteerId);
                if (volunteer == null || !volunteer.IsActive)
                    return false;

                // Business rule: check if volunteer has already applied
                var hasApplied = await _unitOfWork.Applications.HasVolunteerAppliedAsync(volunteerId, projectId);
                if (hasApplied)
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if volunteer can apply to project");
                throw;
            }
        }

        public async Task<bool> CanApproveApplicationAsync(int id)
        {
            try
            {
                // Delegate to repository
                var application = await _unitOfWork.Applications.GetByIdAsync(id);
                if (application == null)
                    return false;

                // State Pattern
                var stateContext = _stateContextFactory.CreateContext(application);
                if (!stateContext.CanApprove())
                {
                    _logger.LogWarning("Cannot approve application {ApplicationId} - state {Status} does not allow approval", 
                        id, application.Status);
                    return false;
                }

                // Check if project has available slots (delegate to repository)
                var hasSlots = await _unitOfWork.Projects.HasAvailableSlotsAsync(application.ProjectId);
                if (!hasSlots)
                {
                    _logger.LogWarning("Cannot approve application {ApplicationId} - project has no available slots", id);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if application can be approved: {ApplicationId}", id);
                throw;
            }
        }

        public async Task<bool> CanRejectApplicationAsync(int id)
        {
            try
            {
                // Delegate to repository
                var application = await _unitOfWork.Applications.GetByIdAsync(id);
                if (application == null)
                    return false;

                // State Pattern
                var stateContext = _stateContextFactory.CreateContext(application);
                return stateContext.CanReject();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if application can be rejected: {ApplicationId}", id);
                throw;
            }
        }

        public async Task<bool> CanWithdrawApplicationAsync(int id, int volunteerId)
        {
            try
            {
                // Delegate to repository
                var application = await _unitOfWork.Applications.GetByIdAsync(id);
                if (application == null)
                    return false;

                // Business rules: volunteer can only withdraw their own applications
                if (application.VolunteerId != volunteerId)
                    return false;

                // State Pattern
                var stateContext = _stateContextFactory.CreateContext(application);
                return stateContext.CanWithdraw();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if application can be withdrawn: {ApplicationId}", id);
                throw;
            }
        }
    }
}
