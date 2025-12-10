using WebApp.Models;
using ApplicationModel = WebApp.Models.Application;

namespace WebApp.Interfaces
{
    /// <summary>
    /// Repository interface for Application entity
    /// Extends generic repository with application-specific operations
    /// </summary>
    public interface IApplicationRepository : IRepository<ApplicationModel>
    {
        Task<IEnumerable<ApplicationModel>> GetApplicationsByVolunteerAsync(int volunteerId);
        Task<IEnumerable<ApplicationModel>> GetApplicationsByProjectAsync(int projectId);
        Task<ApplicationModel?> GetApplicationWithDetailsAsync(int id);
        Task<IEnumerable<ApplicationModel>> GetPendingApplicationsAsync();
        Task<IEnumerable<ApplicationModel>> GetApplicationsByStatusAsync(ApplicationStatus status);
        Task<bool> HasVolunteerAppliedAsync(int volunteerId, int projectId);
        Task<int> GetAcceptedApplicationsCountAsync(int projectId);
    }
}
