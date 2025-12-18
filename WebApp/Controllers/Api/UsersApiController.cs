using Microsoft.AspNetCore.Mvc;
using WebApp.Common;
using WebApp.DTOs.Users;
using WebApp.Interfaces.Services;
using WebApp.Models;
using WebApp.ViewModels;
using WebApp.Services;

namespace WebApp.Controllers.Api
{
    /// <summary>
    /// RESTful API Controller for User management
    /// Follows Single Responsibility Principle - handles only HTTP concerns
    /// Business logic delegated to IUserService (Dependency Inversion Principle)
    /// </summary>
    [ApiController]
    [Route("api/users")]
    [Produces("application/json")]
    public class UsersApiController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<UsersApiController> _logger;

        public UsersApiController(
            IUserService userService, 
            IPasswordHasher passwordHasher,
            ILogger<UsersApiController> logger)
        {
            _userService = userService;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        /// <summary>
        /// Get all users with optional filtering and pagination
        /// </summary>
        /// <param name="searchTerm">Search term for filtering users</param>
        /// <param name="roleFilter">Filter by user role</param>
        /// <param name="isActiveFilter">Filter by active status</param>
        /// <param name="pageNumber">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 10)</param>
        /// <returns>Paginated list of users</returns>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<UserListDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUsers(
            [FromQuery] string? searchTerm,
            [FromQuery] UserRole? roleFilter,
            [FromQuery] bool? isActiveFilter,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var viewModel = await _userService.GetFilteredUsersAsync(
                    searchTerm, roleFilter, isActiveFilter, pageNumber, pageSize);

                var userListDto = new UserListDto
                {
                    Users = viewModel.Users.Select(MapToUserDto).ToList(),
                    TotalCount = viewModel.TotalUsers,
                    PageNumber = viewModel.PageNumber,
                    PageSize = viewModel.PageSize,
                    TotalPages = viewModel.TotalPages
                };

                return Ok(ApiResponse<UserListDto>.SuccessResponse(userListDto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                return StatusCode(500, ApiResponse.ErrorResponse("Failed to retrieve users"));
            }
        }

        /// <summary>
        /// Get a specific user by ID
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>User details</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUser(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                
                if (user == null)
                {
                    return NotFound(ApiResponse.ErrorResponse($"User with ID {id} not found"));
                }

                var userDto = MapToUserDto(user);
                return Ok(ApiResponse<UserDto>.SuccessResponse(userDto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user {UserId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("Failed to retrieve user"));
            }
        }

        /// <summary>
        /// Create a new user
        /// </summary>
        /// <param name="request">User creation data</param>
        /// <returns>Created user</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse.ErrorResponse("Validation failed", errors));
            }

            try
            {
                // Check if email already exists
                if (await _userService.EmailExistsAsync(request.Email))
                {
                    return BadRequest(ApiResponse.ErrorResponse("Email is already registered"));
                }

                // Hash the password using secure PBKDF2
                var passwordHash = _passwordHasher.HashPassword(request.Password);

                // Create user with password hash
                User newUser = request.Role switch
                {
                    UserRole.Volunteer => new Volunteer
                    {
                        Email = request.Email,
                        PasswordHash = passwordHash,
                        FirstName = request.FirstName,
                        LastName = request.LastName,
                        PhoneNumber = request.PhoneNumber,
                        Role = request.Role,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    UserRole.Organization => new Organization
                    {
                        Email = request.Email,
                        PasswordHash = passwordHash,
                        FirstName = request.FirstName,
                        LastName = request.LastName,
                        PhoneNumber = request.PhoneNumber,
                        Role = request.Role,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        IsVerified = false
                    },
                    UserRole.Admin => new Admin
                    {
                        Email = request.Email,
                        PasswordHash = passwordHash,
                        FirstName = request.FirstName,
                        LastName = request.LastName,
                        PhoneNumber = request.PhoneNumber,
                        Role = request.Role,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    _ => throw new ArgumentException("Invalid user role")
                };

                // Note: This bypasses UserService and uses repositories directly
                // This is necessary because UserService.CreateUserAsync doesn't handle passwords
                // Consider refactoring UserService to accept password parameter
                var userVm = new UserVM
                {
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    PhoneNumber = request.PhoneNumber,
                    Role = request.Role,
                    IsActive = true
                };

                var user = await _userService.CreateUserAsync(userVm);
                
                // Update the password hash directly (temporary workaround)
                user.PasswordHash = passwordHash;

                var userDto = MapToUserDto(user);

                return CreatedAtAction(
                    nameof(GetUser),
                    new { id = user.Id },
                    ApiResponse<UserDto>.SuccessResponse(userDto, "User created successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user: {Email}", request.Email);
                return StatusCode(500, ApiResponse.ErrorResponse("Failed to create user"));
            }
        }

        /// <summary>
        /// Update an existing user
        /// </summary>
        /// <param name="id">User ID</param>
        /// <param name="request">User update data</param>
        /// <returns>Success response</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse.ErrorResponse("Validation failed", errors));
            }

            try
            {
                var userVm = new UserVM
                {
                    Id = id,
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    PhoneNumber = request.PhoneNumber,
                    Role = request.Role,
                    IsActive = request.IsActive
                };

                var updated = await _userService.UpdateUserAsync(id, userVm);
                
                if (!updated)
                {
                    return NotFound(ApiResponse.ErrorResponse($"User with ID {id} not found"));
                }

                return Ok(ApiResponse.SuccessResponse("User updated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("Failed to update user"));
            }
        }

        /// <summary>
        /// Delete a user
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>Success response</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound(ApiResponse.ErrorResponse($"User with ID {id} not found"));
                }

                var deleted = await _userService.DeleteUserAsync(id);
                
                if (!deleted)
                {
                    return NotFound(ApiResponse.ErrorResponse($"User with ID {id} not found"));
                }

                return Ok(ApiResponse.SuccessResponse("User deleted successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("Failed to delete user"));
            }
        }

        /// <summary>
        /// Activate a user account
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>Success response</returns>
        [HttpPatch("{id}/activate")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ActivateUser(int id)
        {
            try
            {
                var activated = await _userService.ActivateUserAsync(id);
                
                if (!activated)
                {
                    return NotFound(ApiResponse.ErrorResponse($"User with ID {id} not found"));
                }

                return Ok(ApiResponse.SuccessResponse("User activated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating user {UserId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("Failed to activate user"));
            }
        }

        /// <summary>
        /// Deactivate a user account
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>Success response</returns>
        [HttpPatch("{id}/deactivate")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeactivateUser(int id)
        {
            try
            {
                var deactivated = await _userService.DeactivateUserAsync(id);
                
                if (!deactivated)
                {
                    return NotFound(ApiResponse.ErrorResponse($"User with ID {id} not found"));
                }

                return Ok(ApiResponse.SuccessResponse("User deactivated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating user {UserId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("Failed to deactivate user"));
            }
        }

        // Helper method to map User entity to UserDto
        private static UserDto MapToUserDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                IsActive = user.IsActive
            };
        }
    }
}
