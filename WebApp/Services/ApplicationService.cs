using WebApp.Interfaces;
using WebApp.Interfaces.Services;
using WebApp.Models;

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
        private readonly ILogger<ApplicationService> _logger;

        public ApplicationService(
            IUnitOfWork unitOfWork,
            IProjectService projectService,
            ILogger<ApplicationService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _projectService = projectService ?? throw new ArgumentNullException(nameof(projectService));
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

                application.Status = ApplicationStatus.Accepted;
                application.ReviewedAt = DateTime.UtcNow;
                application.ReviewNotes = reviewNotes;

                _unitOfWork.Applications.Update(application);

                // Business logic: increment volunteer count in project
                await _projectService.IncrementVolunteerCountAsync(application.ProjectId);

                await _unitOfWork.SaveChangesAsync();

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

                application.Status = ApplicationStatus.Rejected;
                application.ReviewedAt = DateTime.UtcNow;
                application.ReviewNotes = reviewNotes;

                _unitOfWork.Applications.Update(application);
                await _unitOfWork.SaveChangesAsync();

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
                application.Status = ApplicationStatus.Withdrawn;

                _unitOfWork.Applications.Update(application);

                // Business rule: if application was accepted, decrement volunteer count
                if (previousStatus == ApplicationStatus.Accepted)
                {
                    await _projectService.DecrementVolunteerCountAsync(application.ProjectId);
                }

                await _unitOfWork.SaveChangesAsync();

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

                // Business rules for approval
                if (application.Status != ApplicationStatus.Pending)
                {
                    _logger.LogWarning("Cannot approve application {ApplicationId} - not in pending status", id);
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

                // Business rule: can only reject pending applications
                return application.Status == ApplicationStatus.Pending;
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

                // Can only withdraw pending or accepted applications
                return application.Status == ApplicationStatus.Pending ||
                       application.Status == ApplicationStatus.Accepted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if application can be withdrawn: {ApplicationId}", id);
                throw;
            }
        }
    }
}
