using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Interfaces;
using WebApp.Models;

namespace WebApp.Repositories
{
    /// <summary>
    /// Admin repository implementation
    /// Provides admin-specific data access operations
    /// </summary>
    public class AdminRepository : Repository<Admin>, IAdminRepository
    {
        public AdminRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Admin>> GetAdminsByDepartmentAsync(string department)
        {
            return await _dbSet
                .Where(a => a.Department == department && a.IsActive)
                .ToListAsync();
        }

        public async Task<Admin?> GetAdminWithPermissionsAsync(int id)
        {
            return await _dbSet
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<bool> CanManageUsersAsync(int adminId)
        {
            var admin = await _dbSet.FindAsync(adminId);
            return admin?.CanManageUsers ?? false;
        }

        public async Task<bool> CanManageOrganizationsAsync(int adminId)
        {
            var admin = await _dbSet.FindAsync(adminId);
            return admin?.CanManageOrganizations ?? false;
        }

        public async Task<bool> CanManageProjectsAsync(int adminId)
        {
            var admin = await _dbSet.FindAsync(adminId);
            return admin?.CanManageProjects ?? false;
        }
    }
}
