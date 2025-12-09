using WebApp.Models;

namespace WebApp.Interfaces
{
    /// <summary>
    /// Repository interface for Project entity
    /// Extends generic repository with project-specific operations
    /// </summary>
    public interface IProjectRepository : IRepository<Project>
    {
        Task<IEnumerable<Project>> GetPublishedProjectsAsync();
        Task<IEnumerable<Project>> GetProjectsByOrganizationAsync(int organizationId);
        Task<Project?> GetProjectWithApplicationsAsync(int id);
        Task<Project?> GetProjectWithOrganizationAsync(int id);
        Task<IEnumerable<Project>> GetProjectsByCityAsync(string city);
        Task<IEnumerable<Project>> GetProjectsByStatusAsync(ProjectStatus status);
        Task<IEnumerable<Project>> SearchProjectsAsync(string searchTerm);
        Task<IEnumerable<Project>> GetAvailableProjectsAsync();
        Task<bool> HasAvailableSlotsAsync(int projectId);
    }
}
