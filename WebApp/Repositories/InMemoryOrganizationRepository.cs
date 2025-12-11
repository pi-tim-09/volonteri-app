using WebApp.Models;
using WebApp.Interfaces;
using System.Linq.Expressions;

public class InMemoryOrganizationRepository : IOrganizationRepository
{
    private readonly List<Organization> _organizations = new();

    public Task<Organization?> GetByIdAsync(int id) =>
        Task.FromResult(_organizations.FirstOrDefault(o => o.Id == id));

    public Task<IEnumerable<Organization>> GetAllAsync() =>
        Task.FromResult(_organizations.AsEnumerable());

    public Task<Organization> AddAsync(Organization entity)
    {
        entity.Id = _organizations.Count + 1;
        _organizations.Add(entity);
        return Task.FromResult(entity);
    }

    public void Update(Organization entity)
    {
        var index = _organizations.FindIndex(o => o.Id == entity.Id);
        if (index != -1)
            _organizations[index] = entity;
    }

    public void Remove(Organization entity)
    {
        _organizations.RemoveAll(o => o.Id == entity.Id);
    }

    public Task<IEnumerable<Organization>> GetVerifiedOrganizationsAsync() =>
        Task.FromResult(_organizations.Where(o => o.IsVerified).AsEnumerable());

    public Task<Organization?> GetOrganizationWithProjectsAsync(int id) =>
        Task.FromResult(_organizations.FirstOrDefault(o => o.Id == id));

    public Task<IEnumerable<Organization>> GetOrganizationsByCityAsync(string city) =>
        Task.FromResult(_organizations.Where(o => o.City == city).AsEnumerable());

    public Task<bool> VerifyOrganizationAsync(int id)
    {
        var org = _organizations.FirstOrDefault(o => o.Id == id);
        if (org != null)
        {
            org.IsVerified = true;
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<IEnumerable<Organization>> SearchOrganizationsAsync(string searchTerm) =>
        Task.FromResult(_organizations.Where(o => o.OrganizationName.Contains(searchTerm)).AsEnumerable());

    public Task<IEnumerable<Organization>> FindAsync(Expression<Func<Organization, bool>> predicate) =>
        Task.FromResult(_organizations.Where(predicate.Compile()).AsEnumerable());

    public Task<Organization?> FirstOrDefaultAsync(Expression<Func<Organization, bool>> predicate) =>
        Task.FromResult(_organizations.FirstOrDefault(predicate.Compile()));

    public Task AddRangeAsync(IEnumerable<Organization> entities)
    {
        foreach (var entity in entities)
        {
            entity.Id = _organizations.Count + 1;
            _organizations.Add(entity);
        }
        return Task.CompletedTask;
    }

    public void UpdateRange(IEnumerable<Organization> entities)
    {
        foreach (var entity in entities)
            Update(entity);
    }

    public void RemoveRange(IEnumerable<Organization> entities)
    {
        foreach (var entity in entities)
            Remove(entity);
    }

    public Task<int> CountAsync() =>
        Task.FromResult(_organizations.Count);

    public Task<int> CountAsync(Expression<Func<Organization, bool>> predicate) =>
        Task.FromResult(_organizations.Count(predicate.Compile()));

    public Task<bool> AnyAsync(Expression<Func<Organization, bool>> predicate) =>
        Task.FromResult(_organizations.Any(predicate.Compile()));
}