using WebApp.Models;

namespace WebApp.Interfaces.Services
{
    /// <summary>
    /// Service interface for Admin-related business logic
    /// Handles business rules, validation, and orchestration for admin operations
    /// </summary>
    public interface IAdminService
    {
        // CRUD Operations (with business logic)
        Task<Admin> CreateAdminAsync(Admin admin);
        Task<bool> UpdateAdminAsync(int id, Admin admin);
        Task<bool> DeleteAdminAsync(int id);
        Task<Admin?> GetAdminByIdAsync(int id);
        Task<IEnumerable<Admin>> GetAllAdminsAsync();
        
        // Permission Management (Business Logic)
        Task<bool> GrantUserManagementPermissionAsync(int adminId);
        Task<bool> RevokeUserManagementPermissionAsync(int adminId);
        Task<bool> GrantOrganizationManagementPermissionAsync(int adminId);
        Task<bool> RevokeOrganizationManagementPermissionAsync(int adminId);
        Task<bool> GrantProjectManagementPermissionAsync(int adminId);
        Task<bool> RevokeProjectManagementPermissionAsync(int adminId);
        
        // Validation & Business Rules
        Task<bool> CanManageUsersAsync(int adminId);
        Task<bool> CanManageOrganizationsAsync(int adminId);
        Task<bool> CanManageProjectsAsync(int adminId);
        Task<bool> CanDeleteAdminAsync(int adminId);
    }
}
