using Microsoft.EntityFrameworkCore.Storage;
using WebApp.Interfaces;
using WebApp.Repositories;

namespace WebApp.Data
{
    /// <summary>
    /// Unit of Work implementation
    /// Coordinates work of multiple repositories and manages transactions
    /// Ensures atomic operations across multiple entities
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IDbContextTransaction? _transaction;

        // Lazy initialization of repositories
        private IVolunteerRepository? _volunteers;
        private IOrganizationRepository? _organizations;
        private IProjectRepository? _projects;
        private IApplicationRepository? _applications;
        private IAdminRepository? _admins;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }

        // Repository properties with lazy initialization
        public IVolunteerRepository Volunteers
        {
            get
            {
                _volunteers ??= new VolunteerRepository(_context);
                return _volunteers;
            }
        }

        public IOrganizationRepository Organizations
        {
            get
            {
                _organizations ??= new OrganizationRepository(_context);
                return _organizations;
            }
        }

        public IProjectRepository Projects
        {
            get
            {
                _projects ??= new ProjectRepository(_context);
                return _projects;
            }
        }

        public IApplicationRepository Applications
        {
            get
            {
                _applications ??= new ApplicationRepository(_context);
                return _applications;
            }
        }

        public IAdminRepository Admins
        {
            get
            {
                _admins ??= new AdminRepository(_context);
                return _admins;
            }
        }

        // Transaction management
        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
                if (_transaction != null)
                {
                    await _transaction.CommitAsync();
                }
            }
            catch
            {
                await RollbackTransactionAsync();
                throw;
            }
            finally
            {
                if (_transaction != null)
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        // Dispose pattern
        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}
