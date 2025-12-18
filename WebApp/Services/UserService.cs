using WebApp.Interfaces;
using WebApp.Interfaces.Services;
using WebApp.Models;
using WebApp.ViewModels;

namespace WebApp.Services
{
    /// <summary>
    /// Service implementation for User-related business logic
    /// Follows Single Responsibility Principle - handles only user-related business operations
    /// </summary>
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UserService> _logger;

        public UserService(IUnitOfWork unitOfWork, ILogger<UserService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // CRUD Operations with Business Logic
        public async Task<User> CreateUserAsync(UserVM userVm)
        {
            try
            {
                // Business validation
                if (await EmailExistsAsync(userVm.Email))
                {
                    throw new InvalidOperationException($"Email {userVm.Email} is already registered.");
                }

                User newUser = userVm.Role switch
                {
                    UserRole.Volunteer => new Volunteer
                    {
                        Email = userVm.Email,
                        FirstName = userVm.FirstName,
                        LastName = userVm.LastName,
                        PhoneNumber = userVm.PhoneNumber,
                        Role = userVm.Role,
                        IsActive = userVm.IsActive,
                        CreatedAt = DateTime.UtcNow
                    },
                    UserRole.Organization => new Organization
                    {
                        Email = userVm.Email,
                        FirstName = userVm.FirstName,
                        LastName = userVm.LastName,
                        PhoneNumber = userVm.PhoneNumber,
                        Role = userVm.Role,
                        IsActive = userVm.IsActive,
                        CreatedAt = DateTime.UtcNow,
                        IsVerified = false // Business rule: new orgs are not verified
                    },
                    UserRole.Admin => new Admin
                    {
                        Email = userVm.Email,
                        FirstName = userVm.FirstName,
                        LastName = userVm.LastName,
                        PhoneNumber = userVm.PhoneNumber,
                        Role = userVm.Role,
                        IsActive = userVm.IsActive,
                        CreatedAt = DateTime.UtcNow
                    },
                    _ => throw new ArgumentException("Invalid user role")
                };

                // Delegate to repository
                switch (newUser.Role)
                {
                    case UserRole.Volunteer:
                        await _unitOfWork.Volunteers.AddAsync((Volunteer)newUser);
                        break;
                    case UserRole.Organization:
                        await _unitOfWork.Organizations.AddAsync((Organization)newUser);
                        break;
                    case UserRole.Admin:
                        await _unitOfWork.Admins.AddAsync((Admin)newUser);
                        break;
                }

                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("Created new user: {Email} with role {Role}", newUser.Email, newUser.Role);
                return newUser;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user: {Email}", userVm.Email);
                throw;
            }
        }

        public async Task<bool> UpdateUserAsync(int id, UserVM userVm)
        {
            try
            {
                var user = await GetUserByIdAsync(id);
                if (user == null)
                    return false;

                // Business logic: Update user properties
                user.Email = userVm.Email;
                user.FirstName = userVm.FirstName;
                user.LastName = userVm.LastName;
                user.PhoneNumber = userVm.PhoneNumber;
                user.IsActive = userVm.IsActive;

                // Delegate to repository
                switch (user.Role)
                {
                    case UserRole.Volunteer:
                        _unitOfWork.Volunteers.Update((Volunteer)user);
                        break;
                    case UserRole.Organization:
                        _unitOfWork.Organizations.Update((Organization)user);
                        break;
                    case UserRole.Admin:
                        _unitOfWork.Admins.Update((Admin)user);
                        break;
                }

                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("Updated user: {UserId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user: {UserId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            try
            {
                // Business validation
                if (!await CanDeleteUserAsync(id))
                {
                    throw new InvalidOperationException("User cannot be deleted at this time.");
                }

                var user = await GetUserByIdAsync(id);
                if (user == null)
                    return false;

                // Delegate to repository
                switch (user.Role)
                {
                    case UserRole.Volunteer:
                        _unitOfWork.Volunteers.Remove((Volunteer)user);
                        break;
                    case UserRole.Organization:
                        _unitOfWork.Organizations.Remove((Organization)user);
                        break;
                    case UserRole.Admin:
                        _unitOfWork.Admins.Remove((Admin)user);
                        break;
                }

                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("Deleted user: {UserId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user: {UserId}", id);
                throw;
            }
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            try
            {
                // Delegate to repositories
                var volunteer = await _unitOfWork.Volunteers.GetByIdAsync(id);
                if (volunteer != null) return volunteer;

                var organization = await _unitOfWork.Organizations.GetByIdAsync(id);
                if (organization != null) return organization;

                var admin = await _unitOfWork.Admins.GetByIdAsync(id);
                return admin;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by ID: {UserId}", id);
                throw;
            }
        }

        // Business Logic Operations
        public async Task<UserFilterViewModel> GetFilteredUsersAsync(
            string? searchTerm,
            UserRole? roleFilter,
            bool? isActiveFilter,
            int pageNumber,
            int pageSize)
        {
            try
            {
                // Delegate to repositories to get all users
                var volunteers = await _unitOfWork.Volunteers.GetAllAsync();
                var organizations = await _unitOfWork.Organizations.GetAllAsync();
                var admins = await _unitOfWork.Admins.GetAllAsync();

                var allUsers = volunteers.Cast<User>()
                    .Concat(organizations.Cast<User>())
                    .Concat(admins.Cast<User>())
                    .ToList();

                // Business logic: Apply filters
                IEnumerable<User> filteredUsers = allUsers;

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    filteredUsers = filteredUsers.Where(u =>
                        u.FirstName.ToLower().Contains(searchTerm) ||
                        u.LastName.ToLower().Contains(searchTerm) ||
                        u.Email.ToLower().Contains(searchTerm) ||
                        u.PhoneNumber.Contains(searchTerm) ||
                        (u is Organization org && org.OrganizationName.ToLower().Contains(searchTerm))
                    );
                }

                if (roleFilter.HasValue)
                {
                    filteredUsers = filteredUsers.Where(u => u.Role == roleFilter.Value);
                }

                if (isActiveFilter.HasValue)
                {
                    filteredUsers = filteredUsers.Where(u => u.IsActive == isActiveFilter.Value);
                }

                var filteredList = filteredUsers.ToList();

                // Business logic: Calculate pagination
                var totalUsers = filteredList.Count;
                var totalPages = (int)Math.Ceiling(totalUsers / (double)pageSize);
                pageNumber = Math.Max(1, Math.Min(pageNumber, totalPages == 0 ? 1 : totalPages));

                var pagedUsers = filteredList
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return new UserFilterViewModel
                {
                    Users = pagedUsers,
                    SearchTerm = searchTerm,
                    RoleFilter = roleFilter,
                    IsActiveFilter = isActiveFilter,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = totalPages,
                    TotalUsers = totalUsers,
                    TotalAdmins = allUsers.Count(u => u.Role == UserRole.Admin),
                    TotalOrganizations = allUsers.Count(u => u.Role == UserRole.Organization),
                    TotalVolunteers = allUsers.Count(u => u.Role == UserRole.Volunteer)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filtering users");
                throw;
            }
        }

        public async Task<bool> ActivateUserAsync(int id)
        {
            try
            {
                var user = await GetUserByIdAsync(id);
                if (user == null)
                    return false;

                user.IsActive = true;

                switch (user.Role)
                {
                    case UserRole.Volunteer:
                        _unitOfWork.Volunteers.Update((Volunteer)user);
                        break;
                    case UserRole.Organization:
                        _unitOfWork.Organizations.Update((Organization)user);
                        break;
                    case UserRole.Admin:
                        _unitOfWork.Admins.Update((Admin)user);
                        break;
                }

                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("Activated user: {UserId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating user: {UserId}", id);
                throw;
            }
        }

        public async Task<bool> DeactivateUserAsync(int id)
        {
            try
            {
                var user = await GetUserByIdAsync(id);
                if (user == null)
                    return false;

                user.IsActive = false;

                switch (user.Role)
                {
                    case UserRole.Volunteer:
                        _unitOfWork.Volunteers.Update((Volunteer)user);
                        break;
                    case UserRole.Organization:
                        _unitOfWork.Organizations.Update((Organization)user);
                        break;
                    case UserRole.Admin:
                        _unitOfWork.Admins.Update((Admin)user);
                        break;
                }

                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("Deactivated user: {UserId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating user: {UserId}", id);
                throw;
            }
        }

        // Validation & Business Rules
        public async Task<bool> EmailExistsAsync(string email)
        {
            try
            {
                // Delegate to repositories
                var volunteerExists = await _unitOfWork.Volunteers.AnyAsync(v => v.Email == email);
                if (volunteerExists) return true;

                var organizationExists = await _unitOfWork.Organizations.AnyAsync(o => o.Email == email);
                if (organizationExists) return true;

                var adminExists = await _unitOfWork.Admins.AnyAsync(a => a.Email == email);
                return adminExists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if email exists: {Email}", email);
                throw;
            }
        }

        public async Task<bool> CanDeleteUserAsync(int id)
        {
            try
            {
                var user = await GetUserByIdAsync(id);
                if (user == null)
                    return false;

                // Business rule: Add specific deletion rules here
                // For example: Can't delete users with active applications, etc.
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user can be deleted: {UserId}", id);
                throw;
            }
        }
    }
}
