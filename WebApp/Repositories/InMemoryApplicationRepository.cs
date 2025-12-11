using WebApp.Models;
using WebApp.Interfaces;
using System.Linq.Expressions;

public class InMemoryApplicationRepository : IApplicationRepository
{
    private readonly List<Application> _applications = new();

    public Task<Application?> GetByIdAsync(int id) =>
        Task.FromResult(_applications.FirstOrDefault(a => a.Id == id));

    public Task<IEnumerable<Application>> GetAllAsync() =>
        Task.FromResult(_applications.AsEnumerable());

    public Task<Application> AddAsync(Application entity)
    {
        entity.Id = _applications.Count + 1;
        _applications.Add(entity);
        return Task.FromResult(entity);
    }

    public void Update(Application entity)
    {
        var index = _applications.FindIndex(a => a.Id == entity.Id);
        if (index != -1)
            _applications[index] = entity;
    }

    public void Remove(Application entity)
    {
        _applications.RemoveAll(a => a.Id == entity.Id);
    }

    public Task<IEnumerable<Application>> GetApplicationsByVolunteerAsync(int volunteerId) =>
        Task.FromResult(_applications.Where(a => a.VolunteerId == volunteerId).AsEnumerable());

    public Task<IEnumerable<Application>> GetApplicationsByProjectAsync(int projectId) =>
        Task.FromResult(_applications.Where(a => a.ProjectId == projectId).AsEnumerable());

    public Task<Application?> GetApplicationWithDetailsAsync(int id) =>
        Task.FromResult(_applications.FirstOrDefault(a => a.Id == id));

    public Task<IEnumerable<Application>> GetPendingApplicationsAsync() =>
        Task.FromResult(_applications.Where(a => a.Status == ApplicationStatus.Pending).AsEnumerable());

    public Task<IEnumerable<Application>> GetApplicationsByStatusAsync(ApplicationStatus status) =>
        Task.FromResult(_applications.Where(a => a.Status == status).AsEnumerable());

    public Task<bool> HasVolunteerAppliedAsync(int volunteerId, int projectId) =>
        Task.FromResult(_applications.Any(a => a.VolunteerId == volunteerId && a.ProjectId == projectId));

    public Task<int> GetAcceptedApplicationsCountAsync(int projectId) =>
        Task.FromResult(_applications.Count(a => a.ProjectId == projectId && a.Status == ApplicationStatus.Accepted));

    public Task<IEnumerable<Application>> FindAsync(Expression<Func<Application, bool>> predicate) =>
        Task.FromResult(_applications.Where(predicate.Compile()).AsEnumerable());

    public Task<Application?> FirstOrDefaultAsync(Expression<Func<Application, bool>> predicate) =>
        Task.FromResult(_applications.FirstOrDefault(predicate.Compile()));

    public Task AddRangeAsync(IEnumerable<Application> entities)
    {
        foreach (var entity in entities)
        {
            entity.Id = _applications.Count + 1;
            _applications.Add(entity);
        }
        return Task.CompletedTask;
    }

    public void UpdateRange(IEnumerable<Application> entities)
    {
        foreach (var entity in entities)
            Update(entity);
    }

    public void RemoveRange(IEnumerable<Application> entities)
    {
        foreach (var entity in entities)
            Remove(entity);
    }

    public Task<int> CountAsync() =>
        Task.FromResult(_applications.Count);

    public Task<int> CountAsync(Expression<Func<Application, bool>> predicate) =>
        Task.FromResult(_applications.Count(predicate.Compile()));

    public Task<bool> AnyAsync(Expression<Func<Application, bool>> predicate) =>
        Task.FromResult(_applications.Any(predicate.Compile()));
}