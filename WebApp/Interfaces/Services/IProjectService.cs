using WebApp.Models;

namespace WebApp.Interfaces.Services
{
    /// <summary>
    /// Service interface for Project-related business logic
    /// Handles business rules, validation, and orchestration for project operations
    /// </summary>
    public interface IProjectService
    {
        // CRUD Operations (with business logic)
        Task<Project> CreateProjectAsync(Project project);
        Task<bool> UpdateProjectAsync(int id, Project project);
        Task<bool> DeleteProjectAsync(int id);
        Task<Project?> GetProjectByIdAsync(int id);
        Task<IEnumerable<Project>> GetAllProjectsAsync();
        Task<IEnumerable<Project>> GetProjectsByOrganizationAsync(int organizationId);
        
        // Business Logic Operations
        Task<bool> PublishProjectAsync(int id);
        Task<bool> CompleteProjectAsync(int id);
        Task<bool> CancelProjectAsync(int id);
        Task<bool> CanAcceptVolunteersAsync(int projectId);
        Task<bool> IncrementVolunteerCountAsync(int projectId);
        Task<bool> DecrementVolunteerCountAsync(int projectId);
        
        // Validation & Business Rules
        Task<bool> ProjectExistsAsync(int id);
        Task<bool> CanEditProjectAsync(int projectId, int organizationId);
        Task<bool> CanDeleteProjectAsync(int projectId);
        Task<bool> IsApplicationDeadlinePassedAsync(int projectId);
    }
}
