using WebApp.Models;

namespace WebApp.Interfaces
{
    /// <summary>
    /// Repository interface for Organization entity
    /// Extends generic repository with organization-specific operations
    /// </summary>
    public interface IOrganizationRepository : IRepository<Organization>
    {
        Task<IEnumerable<Organization>> GetVerifiedOrganizationsAsync();
        Task<Organization?> GetOrganizationWithProjectsAsync(int id);
        Task<IEnumerable<Organization>> GetOrganizationsByCityAsync(string city);
        Task<bool> VerifyOrganizationAsync(int id);
        Task<IEnumerable<Organization>> SearchOrganizationsAsync(string searchTerm);
    }
}
