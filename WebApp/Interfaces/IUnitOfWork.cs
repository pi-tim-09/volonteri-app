namespace WebApp.Interfaces
{
    /// <summary>
    /// Unit of Work interface following the Unit of Work pattern
    /// Provides centralized access to all repositories and transaction management
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        // Repository properties
        IVolunteerRepository Volunteers { get; }
        IOrganizationRepository Organizations { get; }
        IProjectRepository Projects { get; }
        IApplicationRepository Applications { get; }
        IAdminRepository Admins { get; }
        
        // Transaction management
        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
