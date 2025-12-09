using WebApp.Models;

namespace WebApp.Interfaces
{
    /// <summary>
    /// Repository interface for Volunteer entity
    /// Extends generic repository with volunteer-specific operations
    /// </summary>
    public interface IVolunteerRepository : IRepository<Volunteer>
    {
        Task<IEnumerable<Volunteer>> GetVolunteersBySkillAsync(string skill);
        Task<IEnumerable<Volunteer>> GetVolunteersByCityAsync(string city);
        Task<Volunteer?> GetVolunteerWithApplicationsAsync(int id);
        Task<IEnumerable<Volunteer>> GetActiveVolunteersAsync();
        Task<int> GetTotalVolunteerHoursAsync(int volunteerId);
    }
}
