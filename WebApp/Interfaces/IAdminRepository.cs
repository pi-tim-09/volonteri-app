using WebApp.Models;

namespace WebApp.Interfaces
{
    /// <summary>
    /// Repository interface for Admin entity
    /// Extends generic repository with admin-specific operations
    /// </summary>
    public interface IAdminRepository : IRepository<Admin>
    {
        Task<IEnumerable<Admin>> GetAdminsByDepartmentAsync(string department);
        Task<Admin?> GetAdminWithPermissionsAsync(int id);
        Task<bool> CanManageUsersAsync(int adminId);
        Task<bool> CanManageOrganizationsAsync(int adminId);
        Task<bool> CanManageProjectsAsync(int adminId);
    }
}
