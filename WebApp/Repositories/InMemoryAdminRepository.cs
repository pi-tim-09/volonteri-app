using System.Linq.Expressions;
using WebApp.Interfaces;
using WebApp.Models;
using WebApp.Repositories;



public class InMemoryAdminRepository : IAdminRepository
{
    private readonly List<Admin> _admins = new();

    public Task<Admin?> GetByIdAsync(int id) =>
        Task.FromResult(_admins.FirstOrDefault(a => a.Id == id));

    public Task<IEnumerable<Admin>> GetAllAsync() =>
        Task.FromResult(_admins.AsEnumerable());

    public Task<Admin> AddAsync(Admin entity)
    {
        entity.Id = _admins.Count + 1;
        _admins.Add(entity);
        return Task.FromResult(entity);
    }

    public void Update(Admin entity)
    {
        var index = _admins.FindIndex(a => a.Id == entity.Id);
        if (index != -1)
            _admins[index] = entity;
    }

    public void Remove(Admin entity)
    {
        _admins.RemoveAll(a => a.Id == entity.Id);
    }

    public Task<IEnumerable<Admin>> FindAsync(Expression<Func<Admin, bool>> predicate) =>
        Task.FromResult(_admins.Where(predicate.Compile()).AsEnumerable());

    public Task<Admin?> FirstOrDefaultAsync(Expression<Func<Admin, bool>> predicate) =>
        Task.FromResult(_admins.FirstOrDefault(predicate.Compile()));

    public Task AddRangeAsync(IEnumerable<Admin> entities)
    {
        foreach (var entity in entities)
        {
            entity.Id = _admins.Count + 1;
            _admins.Add(entity);
        }
        return Task.CompletedTask;
    }

    public void UpdateRange(IEnumerable<Admin> entities)
    {
        foreach (var entity in entities)
            Update(entity);
    }

    public void RemoveRange(IEnumerable<Admin> entities)
    {
        foreach (var entity in entities)
            Remove(entity);
    }

    public Task<int> CountAsync() =>
        Task.FromResult(_admins.Count);

    public Task<int> CountAsync(Expression<Func<Admin, bool>> predicate) =>
        Task.FromResult(_admins.Count(predicate.Compile()));

    public Task<bool> AnyAsync(Expression<Func<Admin, bool>> predicate) =>
        Task.FromResult(_admins.Any(predicate.Compile()));

    public Task<IEnumerable<Admin>> GetAdminsByDepartmentAsync(string department) =>
        Task.FromResult(_admins.Where(a => a.Department == department).AsEnumerable());

    public Task<Admin?> GetAdminWithPermissionsAsync(int id) =>
        Task.FromResult(_admins.FirstOrDefault(a => a.Id == id));

    public Task<bool> CanManageUsersAsync(int adminId)
    {
        var admin = _admins.FirstOrDefault(a => a.Id == adminId);
        return Task.FromResult(admin?.CanManageUsers ?? false);
    }

    public Task<bool> CanManageOrganizationsAsync(int adminId)
    {
        var admin = _admins.FirstOrDefault(a => a.Id == adminId);
        return Task.FromResult(admin?.CanManageOrganizations ?? false);
    }

    public Task<bool> CanManageProjectsAsync(int adminId)
    {
        var admin = _admins.FirstOrDefault(a => a.Id == adminId);
        return Task.FromResult(admin?.CanManageProjects ?? false);
    }
}