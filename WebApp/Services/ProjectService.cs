using WebApp.Interfaces;
using WebApp.Interfaces.Services;
using WebApp.Models;

namespace WebApp.Services
{
    /// <summary>
    /// Service implementation for Project-related business logic
    /// Follows Single Responsibility Principle - handles only project-related business operations
    /// Delegates data access to repositories
    /// </summary>
    public class ProjectService : IProjectService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ProjectService> _logger;

        public ProjectService(IUnitOfWork unitOfWork, ILogger<ProjectService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // CRUD Operations with Business Logic
        public async Task<Project> CreateProjectAsync(Project project)
        {
            try
            {
                if (project == null)
                    throw new ArgumentNullException(nameof(project));

                // Business rules
                project.CreatedAt = DateTime.UtcNow;
                project.Status = ProjectStatus.Draft; // Business rule: projects start as drafts
                project.CurrentVolunteers = 0;

                // Delegate to repository
                var created = await _unitOfWork.Projects.AddAsync(project);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Created new project: {ProjectTitle}", project.Title);
                return created;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating project: {ProjectTitle}", project?.Title);
                throw;
            }
        }

        public async Task<bool> UpdateProjectAsync(int id, Project project)
        {
            try
            {
                // Delegate to repository
                var existingProject = await _unitOfWork.Projects.GetByIdAsync(id);
                if (existingProject == null)
                    return false;

                // Business logic: update allowed fields
                existingProject.Title = project.Title;
                existingProject.Description = project.Description;
                existingProject.Location = project.Location;
                existingProject.City = project.City;
                existingProject.StartDate = project.StartDate;
                existingProject.EndDate = project.EndDate;
                existingProject.ApplicationDeadline = project.ApplicationDeadline;
                existingProject.MaxVolunteers = project.MaxVolunteers;
                existingProject.RequiredSkills = project.RequiredSkills;
                existingProject.Categories = project.Categories;
                existingProject.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Projects.Update(existingProject);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Updated project: {ProjectId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating project: {ProjectId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteProjectAsync(int id)
        {
            try
            {
                // Business validation
                if (!await CanDeleteProjectAsync(id))
                {
                    throw new InvalidOperationException("Project cannot be deleted - it has accepted applications.");
                }

                // Delegate to repository
                var project = await _unitOfWork.Projects.GetByIdAsync(id);
                if (project == null)
                    return false;

                _unitOfWork.Projects.Remove(project);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Deleted project: {ProjectId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting project: {ProjectId}", id);
                throw;
            }
        }

        public async Task<Project?> GetProjectByIdAsync(int id)
        {
            try
            {
                // Delegate to repository
                return await _unitOfWork.Projects.GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving project by ID: {ProjectId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<Project>> GetAllProjectsAsync()
        {
            try
            {
                // Delegate to repository
                return await _unitOfWork.Projects.GetAllAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all projects");
                throw;
            }
        }

        public async Task<IEnumerable<Project>> GetProjectsByOrganizationAsync(int organizationId)
        {
            try
            {
                // Delegate to repository - simple data query
                return await _unitOfWork.Projects.GetProjectsByOrganizationAsync(organizationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving projects by organization: {OrganizationId}", organizationId);
                throw;
            }
        }

        // Business Logic Operations
        public async Task<bool> PublishProjectAsync(int id)
        {
            try
            {
                // Delegate to repository
                var project = await _unitOfWork.Projects.GetByIdAsync(id);
                if (project == null)
                    return false;

                // Business rule: can only publish drafts
                if (project.Status != ProjectStatus.Draft)
                {
                    _logger.LogWarning("Cannot publish project {ProjectId} - not in draft status", id);
                    return false;
                }

                // Business rule: validate project is ready to publish
                if (project.MaxVolunteers <= 0 || project.ApplicationDeadline <= DateTime.UtcNow)
                {
                    _logger.LogWarning("Cannot publish project {ProjectId} - invalid configuration", id);
                    return false;
                }

                project.Status = ProjectStatus.Published;
                _unitOfWork.Projects.Update(project);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Published project: {ProjectId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing project: {ProjectId}", id);
                throw;
            }
        }

        public async Task<bool> CompleteProjectAsync(int id)
        {
            try
            {
                // Delegate to repository
                var project = await _unitOfWork.Projects.GetByIdAsync(id);
                if (project == null)
                    return false;

                // Business rule: can only complete published or in-progress projects
                if (project.Status != ProjectStatus.Published && project.Status != ProjectStatus.InProgress)
                {
                    _logger.LogWarning("Cannot complete project {ProjectId} - invalid status", id);
                    return false;
                }

                project.Status = ProjectStatus.Completed;
                _unitOfWork.Projects.Update(project);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Completed project: {ProjectId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing project: {ProjectId}", id);
                throw;
            }
        }

        public async Task<bool> CancelProjectAsync(int id)
        {
            try
            {
                // Delegate to repository
                var project = await _unitOfWork.Projects.GetByIdAsync(id);
                if (project == null)
                    return false;

                project.Status = ProjectStatus.Cancelled;
                _unitOfWork.Projects.Update(project);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Cancelled project: {ProjectId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling project: {ProjectId}", id);
                throw;
            }
        }

        public async Task<bool> CanAcceptVolunteersAsync(int projectId)
        {
            try
            {
                // Delegate to repository
                var project = await _unitOfWork.Projects.GetByIdAsync(projectId);
                if (project == null)
                    return false;

                // Business rules for accepting volunteers
                return project.Status == ProjectStatus.Published &&
                       project.ApplicationDeadline > DateTime.UtcNow &&
                       project.CurrentVolunteers < project.MaxVolunteers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if project can accept volunteers: {ProjectId}", projectId);
                throw;
            }
        }

        public async Task<bool> IncrementVolunteerCountAsync(int projectId)
        {
            try
            {
                // Delegate to repository
                var project = await _unitOfWork.Projects.GetByIdAsync(projectId);
                if (project == null)
                    return false;

                // Business rule: validate we can increment
                if (project.CurrentVolunteers >= project.MaxVolunteers)
                {
                    _logger.LogWarning("Cannot increment volunteer count for project {ProjectId} - already at maximum", projectId);
                    return false;
                }

                project.CurrentVolunteers++;
                _unitOfWork.Projects.Update(project);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Incremented volunteer count for project: {ProjectId}", projectId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementing volunteer count for project: {ProjectId}", projectId);
                throw;
            }
        }

        public async Task<bool> DecrementVolunteerCountAsync(int projectId)
        {
            try
            {
                // Delegate to repository
                var project = await _unitOfWork.Projects.GetByIdAsync(projectId);
                if (project == null)
                    return false;

                // Business rule: validate we can decrement
                if (project.CurrentVolunteers <= 0)
                {
                    _logger.LogWarning("Cannot decrement volunteer count for project {ProjectId} - already at zero", projectId);
                    return false;
                }

                project.CurrentVolunteers--;
                _unitOfWork.Projects.Update(project);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Decremented volunteer count for project: {ProjectId}", projectId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decrementing volunteer count for project: {ProjectId}", projectId);
                throw;
            }
        }

        // Validation & Business Rules
        public async Task<bool> ProjectExistsAsync(int id)
        {
            try
            {
                // Delegate to repository
                return await _unitOfWork.Projects.AnyAsync(p => p.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if project exists: {ProjectId}", id);
                throw;
            }
        }

        public async Task<bool> CanEditProjectAsync(int projectId, int organizationId)
        {
            try
            {
                // Delegate to repository
                var project = await _unitOfWork.Projects.GetByIdAsync(projectId);
                if (project == null)
                    return false;

                // Business rule: only the owning organization can edit
                return project.OrganizationId == organizationId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if project can be edited: {ProjectId}", projectId);
                throw;
            }
        }

        public async Task<bool> CanDeleteProjectAsync(int projectId)
        {
            try
            {
                // Business rule: Can't delete projects with accepted applications
                var acceptedCount = await _unitOfWork.Applications.GetAcceptedApplicationsCountAsync(projectId);
                return acceptedCount == 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if project can be deleted: {ProjectId}", projectId);
                throw;
            }
        }

        public async Task<bool> IsApplicationDeadlinePassedAsync(int projectId)
        {
            try
            {
                // Delegate to repository
                var project = await _unitOfWork.Projects.GetByIdAsync(projectId);
                if (project == null)
                    return true;

                // Business logic
                return project.ApplicationDeadline <= DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if application deadline passed for project: {ProjectId}", projectId);
                throw;
            }
        }
    }
}
