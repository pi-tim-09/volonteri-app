using WebApp.Models;
using WebApp.ViewModels;

namespace WebApp.Interfaces.Services
{
    /// <summary>
    /// Service interface for User-related business logic
    /// Handles business rules, validation, and orchestration for user operations
    /// </summary>
    public interface IUserService
    {
        // CRUD Operations (with business logic)
        Task<User> CreateUserAsync(UserVM userVm);
        Task<bool> UpdateUserAsync(int id, UserVM userVm);
        Task<bool> DeleteUserAsync(int id);
        Task<User?> GetUserByIdAsync(int id);
        
        // Business Logic Operations
        Task<UserFilterViewModel> GetFilteredUsersAsync(
            string? searchTerm, 
            UserRole? roleFilter, 
            bool? isActiveFilter, 
            int pageNumber, 
            int pageSize);
        Task<bool> ActivateUserAsync(int id);
        Task<bool> DeactivateUserAsync(int id);
        
        // Validation & Business Rules
        Task<bool> EmailExistsAsync(string email);
        Task<bool> CanDeleteUserAsync(int id);
    }
}
