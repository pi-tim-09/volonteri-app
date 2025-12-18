using Microsoft.AspNetCore.Mvc;
using WebApp.Common;
using WebApp.DTOs.Admins;
using WebApp.Interfaces.Services;
using WebApp.Models;
using WebApp.Services;

namespace WebApp.Controllers.Api
{
    /// <summary>
    /// RESTful API Controller for Admin management
    /// Follows Single Responsibility Principle - handles only HTTP concerns
    /// Business logic delegated to IAdminService (Dependency Inversion Principle)
    /// </summary>
    [ApiController]
    [Route("api/admins")]
    [Produces("application/json")]
    public class AdminsApiController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<AdminsApiController> _logger;

        public AdminsApiController(
            IAdminService adminService,
            IPasswordHasher passwordHasher,
            ILogger<AdminsApiController> logger)
        {
            _adminService = adminService;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        /// <summary>
        /// Get all admins
        /// </summary>
        /// <returns>List of all admins</returns>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<AdminListDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAdmins()
        {
            try
            {
                var admins = await _adminService.GetAllAdminsAsync();
                
                var adminListDto = new AdminListDto
                {
                    Admins = admins.Select(MapToAdminDto).ToList(),
                    TotalCount = admins.Count()
                };

                return Ok(ApiResponse<AdminListDto>.SuccessResponse(adminListDto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving admins");
                return StatusCode(500, ApiResponse.ErrorResponse("Failed to retrieve admins"));
            }
        }

        /// <summary>
        /// Get a specific admin by ID
        /// </summary>
        /// <param name="id">Admin ID</param>
        /// <returns>Admin details</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<AdminDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAdmin(int id)
        {
            try
            {
                var admin = await _adminService.GetAdminByIdAsync(id);
                
                if (admin == null)
                {
                    return NotFound(ApiResponse.ErrorResponse($"Admin with ID {id} not found"));
                }

                var adminDto = MapToAdminDto(admin);
                return Ok(ApiResponse<AdminDto>.SuccessResponse(adminDto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving admin {AdminId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("Failed to retrieve admin"));
            }
        }

        /// <summary>
        /// Create a new admin
        /// </summary>
        /// <param name="request">Admin creation data</param>
        /// <returns>Created admin</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<AdminDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateAdmin([FromBody] CreateAdminRequest request)
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
                // Hash the password using secure PBKDF2
                var passwordHash = _passwordHasher.HashPassword(request.Password);

                var admin = new Admin
                {
                    Email = request.Email,
                    PasswordHash = passwordHash,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    PhoneNumber = request.PhoneNumber,
                    Department = request.Department,
                    CanManageUsers = request.CanManageUsers,
                    CanManageOrganizations = request.CanManageOrganizations,
                    CanManageProjects = request.CanManageProjects,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var createdAdmin = await _adminService.CreateAdminAsync(admin);
                var adminDto = MapToAdminDto(createdAdmin);

                return CreatedAtAction(
                    nameof(GetAdmin),
                    new { id = createdAdmin.Id },
                    ApiResponse<AdminDto>.SuccessResponse(adminDto, "Admin created successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating admin: {Email}", request.Email);
                return StatusCode(500, ApiResponse.ErrorResponse("Failed to create admin"));
            }
        }

        /// <summary>
        /// Update an existing admin
        /// </summary>
        /// <param name="id">Admin ID</param>
        /// <param name="request">Admin update data</param>
        /// <returns>Success response</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateAdmin(int id, [FromBody] UpdateAdminRequest request)
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
                var admin = new Admin
                {
                    Id = id,
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    PhoneNumber = request.PhoneNumber,
                    Department = request.Department,
                    CanManageUsers = request.CanManageUsers,
                    CanManageOrganizations = request.CanManageOrganizations,
                    CanManageProjects = request.CanManageProjects,
                    IsActive = request.IsActive
                };

                var updated = await _adminService.UpdateAdminAsync(id, admin);
                
                if (!updated)
                {
                    return NotFound(ApiResponse.ErrorResponse($"Admin with ID {id} not found"));
                }

                return Ok(ApiResponse.SuccessResponse("Admin updated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating admin {AdminId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("Failed to update admin"));
            }
        }

        /// <summary>
        /// Delete an admin
        /// </summary>
        /// <param name="id">Admin ID</param>
        /// <returns>Success response</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteAdmin(int id)
        {
            try
            {
                var admin = await _adminService.GetAdminByIdAsync(id);
                if (admin == null)
                {
                    return NotFound(ApiResponse.ErrorResponse($"Admin with ID {id} not found"));
                }

                var deleted = await _adminService.DeleteAdminAsync(id);
                
                if (!deleted)
                {
                    return NotFound(ApiResponse.ErrorResponse($"Admin with ID {id} not found"));
                }

                return Ok(ApiResponse.SuccessResponse("Admin deleted successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting admin {AdminId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("Failed to delete admin"));
            }
        }

        /// <summary>
        /// Grant user management permission to an admin
        /// </summary>
        /// <param name="id">Admin ID</param>
        /// <returns>Success response</returns>
        [HttpPatch("{id}/permissions/users/grant")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GrantUserManagementPermission(int id)
        {
            try
            {
                var granted = await _adminService.GrantUserManagementPermissionAsync(id);
                
                if (!granted)
                {
                    return NotFound(ApiResponse.ErrorResponse($"Admin with ID {id} not found"));
                }

                return Ok(ApiResponse.SuccessResponse("User management permission granted successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error granting user management permission to admin {AdminId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("Failed to grant permission"));
            }
        }

        /// <summary>
        /// Revoke user management permission from an admin
        /// </summary>
        /// <param name="id">Admin ID</param>
        /// <returns>Success response</returns>
        [HttpPatch("{id}/permissions/users/revoke")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RevokeUserManagementPermission(int id)
        {
            try
            {
                var revoked = await _adminService.RevokeUserManagementPermissionAsync(id);
                
                if (!revoked)
                {
                    return NotFound(ApiResponse.ErrorResponse($"Admin with ID {id} not found"));
                }

                return Ok(ApiResponse.SuccessResponse("User management permission revoked successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking user management permission from admin {AdminId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("Failed to revoke permission"));
            }
        }

        /// <summary>
        /// Grant organization management permission to an admin
        /// </summary>
        /// <param name="id">Admin ID</param>
        /// <returns>Success response</returns>
        [HttpPatch("{id}/permissions/organizations/grant")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GrantOrganizationManagementPermission(int id)
        {
            try
            {
                var granted = await _adminService.GrantOrganizationManagementPermissionAsync(id);
                
                if (!granted)
                {
                    return NotFound(ApiResponse.ErrorResponse($"Admin with ID {id} not found"));
                }

                return Ok(ApiResponse.SuccessResponse("Organization management permission granted successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error granting organization management permission to admin {AdminId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("Failed to grant permission"));
            }
        }

        /// <summary>
        /// Revoke organization management permission from an admin
        /// </summary>
        /// <param name="id">Admin ID</param>
        /// <returns>Success response</returns>
        [HttpPatch("{id}/permissions/organizations/revoke")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RevokeOrganizationManagementPermission(int id)
        {
            try
            {
                var revoked = await _adminService.RevokeOrganizationManagementPermissionAsync(id);
                
                if (!revoked)
                {
                    return NotFound(ApiResponse.ErrorResponse($"Admin with ID {id} not found"));
                }

                return Ok(ApiResponse.SuccessResponse("Organization management permission revoked successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking organization management permission from admin {AdminId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("Failed to revoke permission"));
            }
        }

        /// <summary>
        /// Grant project management permission to an admin
        /// </summary>
        /// <param name="id">Admin ID</param>
        /// <returns>Success response</returns>
        [HttpPatch("{id}/permissions/projects/grant")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GrantProjectManagementPermission(int id)
        {
            try
            {
                var granted = await _adminService.GrantProjectManagementPermissionAsync(id);
                
                if (!granted)
                {
                    return NotFound(ApiResponse.ErrorResponse($"Admin with ID {id} not found"));
                }

                return Ok(ApiResponse.SuccessResponse("Project management permission granted successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error granting project management permission to admin {AdminId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("Failed to grant permission"));
            }
        }

        /// <summary>
        /// Revoke project management permission from an admin
        /// </summary>
        /// <param name="id">Admin ID</param>
        /// <returns>Success response</returns>
        [HttpPatch("{id}/permissions/projects/revoke")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RevokeProjectManagementPermission(int id)
        {
            try
            {
                var revoked = await _adminService.RevokeProjectManagementPermissionAsync(id);
                
                if (!revoked)
                {
                    return NotFound(ApiResponse.ErrorResponse($"Admin with ID {id} not found"));
                }

                return Ok(ApiResponse.SuccessResponse("Project management permission revoked successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking project management permission from admin {AdminId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("Failed to revoke permission"));
            }
        }

        // Helper method to map Admin entity to AdminDto
        private static AdminDto MapToAdminDto(Admin admin)
        {
            return new AdminDto
            {
                Id = admin.Id,
                Email = admin.Email,
                FirstName = admin.FirstName,
                LastName = admin.LastName,
                PhoneNumber = admin.PhoneNumber,
                Department = admin.Department,
                CanManageUsers = admin.CanManageUsers,
                CanManageOrganizations = admin.CanManageOrganizations,
                CanManageProjects = admin.CanManageProjects,
                CreatedAt = admin.CreatedAt,
                LastLoginAt = admin.LastLoginAt,
                IsActive = admin.IsActive
            };
        }
    }
}
