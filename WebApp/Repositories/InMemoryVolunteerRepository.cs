using WebApp.Models;
using WebApp.Interfaces;
using System.Linq.Expressions;

public class InMemoryVolunteerRepository : IVolunteerRepository
{
    private readonly List<Volunteer> _volunteers = new();

    public Task<Volunteer?> GetByIdAsync(int id) =>
        Task.FromResult(_volunteers.FirstOrDefault(v => v.Id == id));

    public Task<IEnumerable<Volunteer>> GetAllAsync() =>
        Task.FromResult(_volunteers.AsEnumerable());

    public Task<Volunteer> AddAsync(Volunteer entity)
    {
        entity.Id = _volunteers.Count + 1;
        _volunteers.Add(entity);
        return Task.FromResult(entity);
    }

    public void Update(Volunteer entity)
    {
        var index = _volunteers.FindIndex(v => v.Id == entity.Id);
        if (index != -1)
            _volunteers[index] = entity;
    }

    public void Remove(Volunteer entity)
    {
        _volunteers.RemoveAll(v => v.Id == entity.Id);
    }

    public Task<bool> AnyAsync(Expression<Func<Volunteer, bool>> predicate) =>
        Task.FromResult(_volunteers.Any(predicate.Compile()));

    public Task<IEnumerable<Volunteer>> GetVolunteersBySkillAsync(string skill) =>
        Task.FromResult(_volunteers.Where(v => v.Skills.Contains(skill)).AsEnumerable());

    public Task<IEnumerable<Volunteer>> GetVolunteersByCityAsync(string city) =>
        Task.FromResult(_volunteers.Where(v => v.City == city).AsEnumerable());

    public Task<Volunteer?> GetVolunteerWithApplicationsAsync(int id) =>
        Task.FromResult(_volunteers.FirstOrDefault(v => v.Id == id));

    public Task<IEnumerable<Volunteer>> GetActiveVolunteersAsync() =>
        Task.FromResult(_volunteers.Where(v => v.IsActive).AsEnumerable());

    public Task<int> GetTotalVolunteerHoursAsync(int volunteerId)
    {
        var volunteer = _volunteers.FirstOrDefault(v => v.Id == volunteerId);
        return Task.FromResult(volunteer?.VolunteerHours ?? 0);
    }

    public Task<IEnumerable<Volunteer>> FindAsync(Expression<Func<Volunteer, bool>> predicate) =>
        Task.FromResult(_volunteers.Where(predicate.Compile()).AsEnumerable());

    public Task<Volunteer?> FirstOrDefaultAsync(Expression<Func<Volunteer, bool>> predicate) =>
        Task.FromResult(_volunteers.FirstOrDefault(predicate.Compile()));

    public Task AddRangeAsync(IEnumerable<Volunteer> entities)
    {
        foreach (var entity in entities)
        {
            entity.Id = _volunteers.Count + 1;
            _volunteers.Add(entity);
        }
        return Task.CompletedTask;
    }

    public void UpdateRange(IEnumerable<Volunteer> entities)
    {
        foreach (var entity in entities)
            Update(entity);
    }

    public void RemoveRange(IEnumerable<Volunteer> entities)
    {
        foreach (var entity in entities)
            Remove(entity);
    }

    public Task<int> CountAsync() =>
        Task.FromResult(_volunteers.Count);

    public Task<int> CountAsync(Expression<Func<Volunteer, bool>> predicate) =>
        Task.FromResult(_volunteers.Count(predicate.Compile()));
}