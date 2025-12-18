using WebApp.Interfaces;
using WebApp.Interfaces.Services;
using WebApp.Models;

namespace WebApp.Services
{
    /// <summary>
    /// Service implementation for Admin-related business logic
    /// Follows Single Responsibility Principle - handles only admin-related business operations
    /// Delegates data access to repositories
    /// </summary>
    public class AdminService : IAdminService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AdminService> _logger;

        public AdminService(IUnitOfWork unitOfWork, ILogger<AdminService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // CRUD Operations with Business Logic
        public async Task<Admin> CreateAdminAsync(Admin admin)
        {
            try
            {
                if (admin == null)
                    throw new ArgumentNullException(nameof(admin));

                // Business rules
                admin.CreatedAt = DateTime.UtcNow;
                admin.IsActive = true;
                admin.Role = UserRole.Admin;
                // New admins have no permissions by default (business rule)
                admin.CanManageUsers = false;
                admin.CanManageOrganizations = false;
                admin.CanManageProjects = false;

                // Delegate to repository
                var created = await _unitOfWork.Admins.AddAsync(admin);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Created new admin: {Email}", admin.Email);
                return created;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating admin: {Email}", admin?.Email);
                throw;
            }
        }

        public async Task<bool> UpdateAdminAsync(int id, Admin admin)
        {
            try
            {
                // Delegate to repository
                var existingAdmin = await _unitOfWork.Admins.GetByIdAsync(id);
                if (existingAdmin == null)
                    return false;

                // Business logic: update allowed fields
                existingAdmin.Email = admin.Email;
                existingAdmin.FirstName = admin.FirstName;
                existingAdmin.LastName = admin.LastName;
                existingAdmin.PhoneNumber = admin.PhoneNumber;
                existingAdmin.Department = admin.Department;
                existingAdmin.IsActive = admin.IsActive;
                // Note: Permissions are NOT updated here - use Grant/Revoke methods instead

                _unitOfWork.Admins.Update(existingAdmin);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Updated admin: {AdminId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating admin: {AdminId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteAdminAsync(int id)
        {
            try
            {
                // Business validation
                if (!await CanDeleteAdminAsync(id))
                {
                    throw new InvalidOperationException("Cannot delete the last admin in the system.");
                }

                // Delegate to repository
                var admin = await _unitOfWork.Admins.GetByIdAsync(id);
                if (admin == null)
                    return false;

                _unitOfWork.Admins.Remove(admin);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Deleted admin: {AdminId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting admin: {AdminId}", id);
                throw;
            }
        }

        public async Task<Admin?> GetAdminByIdAsync(int id)
        {
            try
            {
                // Delegate to repository
                return await _unitOfWork.Admins.GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving admin by ID: {AdminId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<Admin>> GetAllAdminsAsync()
        {
            try
            {
                // Delegate to repository
                return await _unitOfWork.Admins.GetAllAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all admins");
                throw;
            }
        }

        // Permission Management (Business Logic)
        public async Task<bool> GrantUserManagementPermissionAsync(int adminId)
        {
            try
            {
                // Delegate to repository
                var admin = await _unitOfWork.Admins.GetByIdAsync(adminId);
                if (admin == null)
                    return false;

                admin.CanManageUsers = true;
                _unitOfWork.Admins.Update(admin);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Granted user management permission to admin: {AdminId}", adminId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error granting user management permission to admin: {AdminId}", adminId);
                throw;
            }
        }

        public async Task<bool> RevokeUserManagementPermissionAsync(int adminId)
        {
            try
            {
                // Delegate to repository
                var admin = await _unitOfWork.Admins.GetByIdAsync(adminId);
                if (admin == null)
                    return false;

                admin.CanManageUsers = false;
                _unitOfWork.Admins.Update(admin);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Revoked user management permission from admin: {AdminId}", adminId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking user management permission from admin: {AdminId}", adminId);
                throw;
            }
        }

        public async Task<bool> GrantOrganizationManagementPermissionAsync(int adminId)
        {
            try
            {
                // Delegate to repository
                var admin = await _unitOfWork.Admins.GetByIdAsync(adminId);
                if (admin == null)
                    return false;

                admin.CanManageOrganizations = true;
                _unitOfWork.Admins.Update(admin);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Granted organization management permission to admin: {AdminId}", adminId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error granting organization management permission to admin: {AdminId}", adminId);
                throw;
            }
        }

        public async Task<bool> RevokeOrganizationManagementPermissionAsync(int adminId)
        {
            try
            {
                // Delegate to repository
                var admin = await _unitOfWork.Admins.GetByIdAsync(adminId);
                if (admin == null)
                    return false;

                admin.CanManageOrganizations = false;
                _unitOfWork.Admins.Update(admin);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Revoked organization management permission from admin: {AdminId}", adminId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking organization management permission from admin: {AdminId}", adminId);
                throw;
            }
        }

        public async Task<bool> GrantProjectManagementPermissionAsync(int adminId)
        {
            try
            {
                // Delegate to repository
                var admin = await _unitOfWork.Admins.GetByIdAsync(adminId);
                if (admin == null)
                    return false;

                admin.CanManageProjects = true;
                _unitOfWork.Admins.Update(admin);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Granted project management permission to admin: {AdminId}", adminId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error granting project management permission to admin: {AdminId}", adminId);
                throw;
            }
        }

        public async Task<bool> RevokeProjectManagementPermissionAsync(int adminId)
        {
            try
            {
                // Delegate to repository
                var admin = await _unitOfWork.Admins.GetByIdAsync(adminId);
                if (admin == null)
                    return false;

                admin.CanManageProjects = false;
                _unitOfWork.Admins.Update(admin);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Revoked project management permission from admin: {AdminId}", adminId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking project management permission from admin: {AdminId}", adminId);
                throw;
            }
        }

        // Validation & Business Rules
        public async Task<bool> CanManageUsersAsync(int adminId)
        {
            try
            {
                // Delegate to repository
                return await _unitOfWork.Admins.CanManageUsersAsync(adminId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if admin can manage users: {AdminId}", adminId);
                throw;
            }
        }

        public async Task<bool> CanManageOrganizationsAsync(int adminId)
        {
            try
            {
                // Delegate to repository
                return await _unitOfWork.Admins.CanManageOrganizationsAsync(adminId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if admin can manage organizations: {AdminId}", adminId);
                throw;
            }
        }

        public async Task<bool> CanManageProjectsAsync(int adminId)
        {
            try
            {
                // Delegate to repository
                return await _unitOfWork.Admins.CanManageProjectsAsync(adminId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if admin can manage projects: {AdminId}", adminId);
                throw;
            }
        }

        public async Task<bool> CanDeleteAdminAsync(int adminId)
        {
            try
            {
                // Business rule: Can't delete the last admin
                var totalAdmins = await _unitOfWork.Admins.CountAsync(a => a.IsActive);
                return totalAdmins > 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if admin can be deleted: {AdminId}", adminId);
                throw;
            }
        }
    }
}
