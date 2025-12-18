using Microsoft.AspNetCore.Mvc;
using WebApp.Common;
using WebApp.DTOs.Organizations;
using WebApp.Interfaces.Services;
using WebApp.Models;
using WebApp.Services;

namespace WebApp.Controllers.Api
{
    /// <summary>
    /// RESTful API Controller for Organization management
    /// Follows Single Responsibility Principle - handles only HTTP concerns
    /// Business logic delegated to IOrganizationService (Dependency Inversion Principle)
    /// </summary>
    [ApiController]
    [Route("api/organizations")]
    [Produces("application/json")]
    public class OrganizationsApiController : ControllerBase
    {
        private readonly IOrganizationService _organizationService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<OrganizationsApiController> _logger;

        public OrganizationsApiController(
            IOrganizationService organizationService,
            IPasswordHasher passwordHasher,
            ILogger<OrganizationsApiController> logger)
        {
            _organizationService = organizationService;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        /// <summary>
        /// Get all organizations
        /// </summary>
        /// <returns>List of all organizations</returns>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<OrganizationListDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetOrganizations()
        {
            try
            {
                var organizations = await _organizationService.GetAllOrganizationsAsync();
                
                var organizationListDto = new OrganizationListDto
                {
                    Organizations = organizations.Select(MapToOrganizationDto).ToList(),
                    TotalCount = organizations.Count()
                };

                return Ok(ApiResponse<OrganizationListDto>.SuccessResponse(organizationListDto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving organizations");
                return StatusCode(500, ApiResponse.ErrorResponse("Failed to retrieve organizations"));
            }
        }

        /// <summary>
        /// Get a specific organization by ID
        /// </summary>
        /// <param name="id">Organization ID</param>
        /// <returns>Organization details</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<OrganizationDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetOrganization(int id)
        {
            try
            {
                var organization = await _organizationService.GetOrganizationByIdAsync(id);
                
                if (organization == null)
                {
                    return NotFound(ApiResponse.ErrorResponse($"Organization with ID {id} not found"));
                }

                var organizationDto = MapToOrganizationDto(organization);
                return Ok(ApiResponse<OrganizationDto>.SuccessResponse(organizationDto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving organization {OrganizationId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("Failed to retrieve organization"));
            }
        }

        /// <summary>
        /// Create a new organization
        /// </summary>
        /// <param name="request">Organization creation data</param>
        /// <returns>Created organization</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<OrganizationDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateOrganization([FromBody] CreateOrganizationRequest request)
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

                var organization = new Organization
                {
                    Email = request.Email,
                    PasswordHash = passwordHash,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    PhoneNumber = request.PhoneNumber,
                    OrganizationName = request.OrganizationName,
                    Description = request.Description,
                    Address = request.Address,
                    City = request.City,
                    IsActive = true,
                    IsVerified = false,
                    CreatedAt = DateTime.UtcNow
                };

                var createdOrganization = await _organizationService.CreateOrganizationAsync(organization);
                var organizationDto = MapToOrganizationDto(createdOrganization);

                return CreatedAtAction(
                    nameof(GetOrganization),
                    new { id = createdOrganization.Id },
                    ApiResponse<OrganizationDto>.SuccessResponse(organizationDto, "Organization created successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating organization: {OrganizationName}", request.OrganizationName);
                return StatusCode(500, ApiResponse.ErrorResponse("Failed to create organization"));
            }
        }

        /// <summary>
        /// Update an existing organization
        /// </summary>
        /// <param name="id">Organization ID</param>
        /// <param name="request">Organization update data</param>
        /// <returns>Success response</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateOrganization(int id, [FromBody] UpdateOrganizationRequest request)
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
                var organization = new Organization
                {
                    Id = id,
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    PhoneNumber = request.PhoneNumber,
                    OrganizationName = request.OrganizationName,
                    Description = request.Description,
                    Address = request.Address,
                    City = request.City,
                    IsActive = request.IsActive
                };

                var updated = await _organizationService.UpdateOrganizationAsync(id, organization);
                
                if (!updated)
                {
                    return NotFound(ApiResponse.ErrorResponse($"Organization with ID {id} not found"));
                }

                return Ok(ApiResponse.SuccessResponse("Organization updated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating organization {OrganizationId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("Failed to update organization"));
            }
        }

        /// <summary>
        /// Delete an organization
        /// </summary>
        /// <param name="id">Organization ID</param>
        /// <returns>Success response</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteOrganization(int id)
        {
            try
            {
                var organization = await _organizationService.GetOrganizationByIdAsync(id);
                if (organization == null)
                {
                    return NotFound(ApiResponse.ErrorResponse($"Organization with ID {id} not found"));
                }

                var deleted = await _organizationService.DeleteOrganizationAsync(id);
                
                if (!deleted)
                {
                    return NotFound(ApiResponse.ErrorResponse($"Organization with ID {id} not found"));
                }

                return Ok(ApiResponse.SuccessResponse("Organization deleted successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting organization {OrganizationId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("Failed to delete organization"));
            }
        }

        /// <summary>
        /// Verify an organization
        /// </summary>
        /// <param name="id">Organization ID</param>
        /// <returns>Success response</returns>
        [HttpPatch("{id}/verify")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> VerifyOrganization(int id)
        {
            try
            {
                var verified = await _organizationService.VerifyOrganizationAsync(id);
                
                if (!verified)
                {
                    return NotFound(ApiResponse.ErrorResponse($"Organization with ID {id} not found"));
                }

                return Ok(ApiResponse.SuccessResponse("Organization verified successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying organization {OrganizationId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("Failed to verify organization"));
            }
        }

        /// <summary>
        /// Unverify an organization
        /// </summary>
        /// <param name="id">Organization ID</param>
        /// <returns>Success response</returns>
        [HttpPatch("{id}/unverify")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UnverifyOrganization(int id)
        {
            try
            {
                var unverified = await _organizationService.UnverifyOrganizationAsync(id);
                
                if (!unverified)
                {
                    return NotFound(ApiResponse.ErrorResponse($"Organization with ID {id} not found"));
                }

                return Ok(ApiResponse.SuccessResponse("Organization unverified successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unverifying organization {OrganizationId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("Failed to unverify organization"));
            }
        }

        // Helper method to map Organization entity to OrganizationDto
        private static OrganizationDto MapToOrganizationDto(Organization organization)
        {
            return new OrganizationDto
            {
                Id = organization.Id,
                Email = organization.Email,
                FirstName = organization.FirstName,
                LastName = organization.LastName,
                PhoneNumber = organization.PhoneNumber,
                OrganizationName = organization.OrganizationName,
                Description = organization.Description,
                Address = organization.Address,
                City = organization.City,
                VerifiedAt = organization.VerifiedAt,
                IsVerified = organization.IsVerified,
                CreatedAt = organization.CreatedAt,
                LastLoginAt = organization.LastLoginAt,
                IsActive = organization.IsActive
            };
        }
    }
}
