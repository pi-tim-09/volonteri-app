using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Interfaces;
using ApplicationModel = WebApp.Models.Application;

namespace WebApp.Repositories
{
    /// <summary>
    /// Application repository implementation
    /// Provides application-specific data access operations
    /// </summary>
    public class ApplicationRepository : Repository<ApplicationModel>, IApplicationRepository
    {
        public ApplicationRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<ApplicationModel>> GetApplicationsByVolunteerAsync(int volunteerId)
        {
            return await _dbSet
                .Where(a => a.VolunteerId == volunteerId)
                .Include(a => a.Project)
                    .ThenInclude(p => p.Organization)
                .ToListAsync();
        }

        public async Task<IEnumerable<ApplicationModel>> GetApplicationsByProjectAsync(int projectId)
        {
            return await _dbSet
                .Where(a => a.ProjectId == projectId)
                .Include(a => a.Volunteer)
                .ToListAsync();
        }

        public async Task<ApplicationModel?> GetApplicationWithDetailsAsync(int id)
        {
            return await _dbSet
                .Include(a => a.Volunteer)
                .Include(a => a.Project)
                    .ThenInclude(p => p.Organization)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<IEnumerable<ApplicationModel>> GetPendingApplicationsAsync()
        {
            return await _dbSet
                .Where(a => a.Status == Models.ApplicationStatus.Pending)
                .Include(a => a.Volunteer)
                .Include(a => a.Project)
                .ToListAsync();
        }

        public async Task<IEnumerable<ApplicationModel>> GetApplicationsByStatusAsync(Models.ApplicationStatus status)
        {
            return await _dbSet
                .Where(a => a.Status == status)
                .Include(a => a.Volunteer)
                .Include(a => a.Project)
                .ToListAsync();
        }

        public async Task<bool> HasVolunteerAppliedAsync(int volunteerId, int projectId)
        {
            return await _dbSet
                .AnyAsync(a => a.VolunteerId == volunteerId && a.ProjectId == projectId);
        }

        public async Task<int> GetAcceptedApplicationsCountAsync(int projectId)
        {
            return await _dbSet
                .CountAsync(a => a.ProjectId == projectId && 
                                a.Status == Models.ApplicationStatus.Accepted);
        }
    }
}
