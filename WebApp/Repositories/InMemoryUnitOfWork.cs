using WebApp.Interfaces;

public class InMemoryUnitOfWork : IUnitOfWork
{
    public IVolunteerRepository Volunteers { get; } = new InMemoryVolunteerRepository();
    public IOrganizationRepository Organizations { get; } = new InMemoryOrganizationRepository();
    public IProjectRepository Projects { get; } = new InMemoryProjectRepository();
    public IApplicationRepository Applications { get; } = new InMemoryApplicationRepository();
    public IAdminRepository Admins { get; } = new InMemoryAdminRepository();

    public Task<int> SaveChangesAsync() => Task.FromResult(0);
    public Task BeginTransactionAsync() => Task.CompletedTask;
    public Task CommitTransactionAsync() => Task.CompletedTask;
    public Task RollbackTransactionAsync() => Task.CompletedTask;
    public void Dispose() { }
}