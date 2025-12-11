using WebApp.Models;
using WebApp.Interfaces;
using System.Linq.Expressions;

public class InMemoryProjectRepository : IProjectRepository
{
    private readonly List<Project> _projects = new();

    public Task<Project?> GetByIdAsync(int id) =>
        Task.FromResult(_projects.FirstOrDefault(p => p.Id == id));

    public Task<IEnumerable<Project>> GetAllAsync() =>
        Task.FromResult(_projects.AsEnumerable());

    public Task<Project> AddAsync(Project entity)
    {
        entity.Id = _projects.Count + 1;
        _projects.Add(entity);
        return Task.FromResult(entity);
    }

    public void Update(Project entity)
    {
        var index = _projects.FindIndex(p => p.Id == entity.Id);
        if (index != -1)
            _projects[index] = entity;
    }

    public void Remove(Project entity)
    {
        _projects.RemoveAll(p => p.Id == entity.Id);
    }

    public Task<IEnumerable<Project>> GetPublishedProjectsAsync() =>
        Task.FromResult(_projects.Where(p => p.Status == ProjectStatus.Cancelled).AsEnumerable());

    public Task<IEnumerable<Project>> GetProjectsByOrganizationAsync(int organizationId) =>
        Task.FromResult(_projects.Where(p => p.OrganizationId == organizationId).AsEnumerable());

    public Task<Project?> GetProjectWithApplicationsAsync(int id) =>
        Task.FromResult(_projects.FirstOrDefault(p => p.Id == id));

    public Task<Project?> GetProjectWithOrganizationAsync(int id) =>
        Task.FromResult(_projects.FirstOrDefault(p => p.Id == id));

    public Task<IEnumerable<Project>> GetProjectsByCityAsync(string city) =>
        Task.FromResult(_projects.Where(p => p.City == city).AsEnumerable());

    public Task<IEnumerable<Project>> GetProjectsByStatusAsync(ProjectStatus status) =>
        Task.FromResult(_projects.Where(p => p.Status == status).AsEnumerable());

    public Task<IEnumerable<Project>> SearchProjectsAsync(string searchTerm) =>
        Task.FromResult(_projects.Where(p => p.Title.Contains(searchTerm)).AsEnumerable());

    public Task<IEnumerable<Project>> GetAvailableProjectsAsync() =>
        Task.FromResult(_projects.Where(p => p.Status == ProjectStatus.Completed).AsEnumerable());

    public Task<bool> HasAvailableSlotsAsync(int projectId)
    {
        var project = _projects.FirstOrDefault(p => p.Id == projectId);
        if (project != null && project.Applications != null && project.MaxVolunteers > 0)
            return Task.FromResult(project.Applications.Count < project.MaxVolunteers);
        return Task.FromResult(false);
    }

    public Task<IEnumerable<Project>> FindAsync(Expression<Func<Project, bool>> predicate) =>
        Task.FromResult(_projects.Where(predicate.Compile()).AsEnumerable());

    public Task<Project?> FirstOrDefaultAsync(Expression<Func<Project, bool>> predicate) =>
        Task.FromResult(_projects.FirstOrDefault(predicate.Compile()));

    public Task AddRangeAsync(IEnumerable<Project> entities)
    {
        foreach (var entity in entities)
        {
            entity.Id = _projects.Count + 1;
            _projects.Add(entity);
        }
        return Task.CompletedTask;
    }

    public void UpdateRange(IEnumerable<Project> entities)
    {
        foreach (var entity in entities)
            Update(entity);
    }

    public void RemoveRange(IEnumerable<Project> entities)
    {
        foreach (var entity in entities)
            Remove(entity);
    }

    public Task<int> CountAsync() =>
        Task.FromResult(_projects.Count);

    public Task<int> CountAsync(Expression<Func<Project, bool>> predicate) =>
        Task.FromResult(_projects.Count(predicate.Compile()));

    public Task<bool> AnyAsync(Expression<Func<Project, bool>> predicate) =>
        Task.FromResult(_projects.Any(predicate.Compile()));
}