using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Interfaces;
using WebApp.Models;

namespace WebApp.Repositories
{
    /// <summary>
    /// Organization repository implementation
    /// Provides organization-specific data access operations
    /// </summary>
    public class OrganizationRepository : Repository<Organization>, IOrganizationRepository
    {
        public OrganizationRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Organization>> GetVerifiedOrganizationsAsync()
        {
            return await _dbSet
                .Where(o => o.IsVerified && o.IsActive)
                .ToListAsync();
        }

        public async Task<Organization?> GetOrganizationWithProjectsAsync(int id)
        {
            return await _dbSet
                .Include(o => o.Projects)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<IEnumerable<Organization>> GetOrganizationsByCityAsync(string city)
        {
            return await _dbSet
                .Where(o => o.City == city && o.IsActive)
                .ToListAsync();
        }

        public async Task<bool> VerifyOrganizationAsync(int id)
        {
            var organization = await _dbSet.FindAsync(id);
            if (organization == null)
                return false;

            organization.IsVerified = true;
            organization.VerifiedAt = DateTime.UtcNow;
            _dbSet.Update(organization);
            
            return true;
        }

        public async Task<IEnumerable<Organization>> SearchOrganizationsAsync(string searchTerm)
        {
            return await _dbSet
                .Where(o => o.IsActive && 
                    (o.OrganizationName.Contains(searchTerm) || 
                     o.Description.Contains(searchTerm)))
                .ToListAsync();
        }
    }
}
