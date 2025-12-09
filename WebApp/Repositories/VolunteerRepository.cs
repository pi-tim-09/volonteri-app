using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Interfaces;
using WebApp.Models;

namespace WebApp.Repositories
{
    /// <summary>
    /// Volunteer repository implementation
    /// Provides volunteer-specific data access operations
    /// </summary>
    public class VolunteerRepository : Repository<Volunteer>, IVolunteerRepository
    {
        public VolunteerRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Volunteer>> GetVolunteersBySkillAsync(string skill)
        {
            return await _dbSet
                .Where(v => v.Skills.Contains(skill))
                .ToListAsync();
        }

        public async Task<IEnumerable<Volunteer>> GetVolunteersByCityAsync(string city)
        {
            return await _dbSet
                .Where(v => v.City == city && v.IsActive)
                .ToListAsync();
        }

        public async Task<Volunteer?> GetVolunteerWithApplicationsAsync(int id)
        {
            return await _dbSet
                .Include(v => v.Applications)
                    .ThenInclude(a => a.Project)
                .FirstOrDefaultAsync(v => v.Id == id);
        }

        public async Task<IEnumerable<Volunteer>> GetActiveVolunteersAsync()
        {
            return await _dbSet
                .Where(v => v.IsActive)
                .ToListAsync();
        }

        public async Task<int> GetTotalVolunteerHoursAsync(int volunteerId)
        {
            var volunteer = await _dbSet
                .FirstOrDefaultAsync(v => v.Id == volunteerId);
            
            return volunteer?.VolunteerHours ?? 0;
        }
    }
}
