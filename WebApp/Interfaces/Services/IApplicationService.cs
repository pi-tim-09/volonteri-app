using WebApp.Models;

namespace WebApp.Interfaces.Services
{
    /// <summary>
    /// Service interface for Application-related business logic
    /// Handles business rules, validation, and orchestration for application operations
    /// </summary>
    public interface IApplicationService
    {
        // CRUD Operations (with business logic)
        Task<Application> CreateApplicationAsync(int volunteerId, int projectId);
        Task<bool> DeleteApplicationAsync(int id);
        Task<Application?> GetApplicationByIdAsync(int id);
        Task<IEnumerable<Application>> GetFilteredApplicationsAsync(int? projectId, ApplicationStatus? status);
        
        // Business Logic Operations
        Task<bool> ApproveApplicationAsync(int id, string? reviewNotes);
        Task<bool> RejectApplicationAsync(int id, string? reviewNotes);
        Task<bool> WithdrawApplicationAsync(int id);
        
        // Validation & Business Rules
        Task<bool> CanApplyToProjectAsync(int volunteerId, int projectId);
        Task<bool> CanApproveApplicationAsync(int id);
        Task<bool> CanRejectApplicationAsync(int id);
        Task<bool> CanWithdrawApplicationAsync(int id, int volunteerId);
    }
}
