using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Interfaces;
using WebApp.Models;

namespace WebApp.Repositories
{
    /// <summary>
    /// Project repository implementation
    /// Provides project-specific data access operations
    /// </summary>
    public class ProjectRepository : Repository<Project>, IProjectRepository
    {
        public ProjectRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Project>> GetPublishedProjectsAsync()
        {
            return await _dbSet
                .Where(p => p.Status == ProjectStatus.Published)
                .Include(p => p.Organization)
                .ToListAsync();
        }

        public async Task<IEnumerable<Project>> GetProjectsByOrganizationAsync(int organizationId)
        {
            return await _dbSet
                .Where(p => p.OrganizationId == organizationId)
                .ToListAsync();
        }

        public async Task<Project?> GetProjectWithApplicationsAsync(int id)
        {
            return await _dbSet
                .Include(p => p.Applications)
                    .ThenInclude(a => a.Volunteer)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Project?> GetProjectWithOrganizationAsync(int id)
        {
            return await _dbSet
                .Include(p => p.Organization)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<IEnumerable<Project>> GetProjectsByCityAsync(string city)
        {
            return await _dbSet
                .Where(p => p.City == city && p.Status == ProjectStatus.Published)
                .Include(p => p.Organization)
                .ToListAsync();
        }

        public async Task<IEnumerable<Project>> GetProjectsByStatusAsync(ProjectStatus status)
        {
            return await _dbSet
                .Where(p => p.Status == status)
                .Include(p => p.Organization)
                .ToListAsync();
        }

        public async Task<IEnumerable<Project>> SearchProjectsAsync(string searchTerm)
        {
            return await _dbSet
                .Where(p => p.Status == ProjectStatus.Published &&
                    (p.Title.Contains(searchTerm) || 
                     p.Description.Contains(searchTerm)))
                .Include(p => p.Organization)
                .ToListAsync();
        }

        public async Task<IEnumerable<Project>> GetAvailableProjectsAsync()
        {
            var today = DateTime.UtcNow;
            return await _dbSet
                .Where(p => p.Status == ProjectStatus.Published &&
                           p.ApplicationDeadline > today &&
                           p.CurrentVolunteers < p.MaxVolunteers)
                .Include(p => p.Organization)
                .ToListAsync();
        }

        public async Task<bool> HasAvailableSlotsAsync(int projectId)
        {
            var project = await _dbSet.FindAsync(projectId);
            return project != null && project.CurrentVolunteers < project.MaxVolunteers;
        }
    }
}
