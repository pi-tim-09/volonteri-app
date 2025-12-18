using WebApp.Models;

namespace WebApp.Interfaces.Services
{
    /// <summary>
    /// Service interface for Organization-related business logic
    /// Handles business rules, validation, and orchestration for organization operations
    /// </summary>
    public interface IOrganizationService
    {
        // CRUD Operations (with business logic)
        Task<Organization> CreateOrganizationAsync(Organization organization);
        Task<bool> UpdateOrganizationAsync(int id, Organization organization);
        Task<bool> DeleteOrganizationAsync(int id);
        Task<Organization?> GetOrganizationByIdAsync(int id);
        Task<IEnumerable<Organization>> GetAllOrganizationsAsync();
        
        // Business Logic Operations
        Task<bool> VerifyOrganizationAsync(int id);
        Task<bool> UnverifyOrganizationAsync(int id);
        Task<bool> CanCreateProjectAsync(int organizationId);
        
        // Validation & Business Rules
        Task<bool> OrganizationExistsAsync(int id);
        Task<bool> CanDeleteOrganizationAsync(int id);
    }
}
