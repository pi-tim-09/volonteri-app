using Microsoft.AspNetCore.Mvc;
using WebApp.Common;
using WebApp.DTOs.Applications;
using WebApp.Interfaces.Services;
using WebApp.Models;

namespace WebApp.Controllers.Api
{
    /// <summary>
    /// RESTful API Controller for Application management
    /// Follows Single Responsibility Principle - handles only HTTP concerns
    /// Business logic delegated to IApplicationService (Dependency Inversion Principle)
    /// </summary>
    [ApiController]
    [Route("api/applications")]
    [Produces("application/json")]
    public class ApplicationsApiController : ControllerBase
    {
        private readonly IApplicationService _applicationService;
        private readonly IProjectService _projectService;
        private readonly ILogger<ApplicationsApiController> _logger;

        public ApplicationsApiController(
            IApplicationService applicationService,
            IProjectService projectService,
            ILogger<ApplicationsApiController> logger)
        {
            _applicationService = applicationService;
            _projectService = projectService;
            _logger = logger;
        }

        /// <summary>
        /// Get all applications with optional filtering
        /// </summary>
        /// <param name="projectId">Optional project ID filter</param>
        /// <param name="status">Optional application status filter</param>
        /// <returns>List of applications</returns>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<ApplicationListDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetApplications(
            [FromQuery] int? projectId,
            [FromQuery] ApplicationStatus? status)
        {
            try
            {
                var applications = await _applicationService.GetFilteredApplicationsAsync(projectId, status);
                
                var applicationListDto = new ApplicationListDto
                {
                    Applications = new List<ApplicationDto>(),
                    TotalCount = applications.Count()
                };

                foreach (var application in applications)
                {
                    applicationListDto.Applications.Add(await MapToApplicationDtoAsync(application));
                }

                return Ok(ApiResponse<ApplicationListDto>.SuccessResponse(applicationListDto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving applications");
                return StatusCode(500, ApiResponse.ErrorResponse("Failed to retrieve applications"));
            }
        }

        /// <summary>
        /// Get a specific application by ID
        /// </summary>
        /// <param name="id">Application ID</param>
        /// <returns>Application details</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<ApplicationDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetApplication(int id)
        {
            try
            {
                var application = await _applicationService.GetApplicationByIdAsync(id);
                
                if (application == null)
                {
                    return NotFound(ApiResponse.ErrorResponse($"Application with ID {id} not found"));
                }

                var applicationDto = await MapToApplicationDtoAsync(application);
                return Ok(ApiResponse<ApplicationDto>.SuccessResponse(applicationDto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving application {ApplicationId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("Failed to retrieve application"));
            }
        }

        /// <summary>
        /// Create a new application
        /// </summary>
        /// <param name="request">Application creation data</param>
        /// <returns>Created application</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<ApplicationDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateApplication([FromBody] CreateApplicationRequest request)
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
                // Validate if volunteer can apply
                if (!await _applicationService.CanApplyToProjectAsync(request.VolunteerId, request.ProjectId))
                {
                    return BadRequest(ApiResponse.ErrorResponse("Cannot apply to this project. Either the project doesn't exist, deadline has passed, or you've already applied."));
                }

                var application = await _applicationService.CreateApplicationAsync(
                    request.VolunteerId, 
                    request.ProjectId);

                var applicationDto = await MapToApplicationDtoAsync(application);

                return CreatedAtAction(
                    nameof(GetApplication),
                    new { id = application.Id },
                    ApiResponse<ApplicationDto>.SuccessResponse(applicationDto, "Application created successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating application for volunteer {VolunteerId} and project {ProjectId}", 
                    request.VolunteerId, request.ProjectId);
                return StatusCode(500, ApiResponse.ErrorResponse("Failed to create application"));
            }
        }

        /// <summary>
        /// Approve an application
        /// </summary>
        /// <param name="id">Application ID</param>
        /// <param name="request">Review data</param>
        /// <returns>Success response</returns>
        [HttpPatch("{id}/approve")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ApproveApplication(int id, [FromBody] ReviewApplicationRequest request)
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
                if (!await _applicationService.CanApproveApplicationAsync(id))
                {
                    return BadRequest(ApiResponse.ErrorResponse("Application cannot be approved"));
                }

                var approved = await _applicationService.ApproveApplicationAsync(id, request.ReviewNotes);
                
                if (!approved)
                {
                    return NotFound(ApiResponse.ErrorResponse($"Application with ID {id} not found"));
                }

                return Ok(ApiResponse.SuccessResponse("Application approved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving application {ApplicationId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("Failed to approve application"));
            }
        }

        /// <summary>
        /// Reject an application
        /// </summary>
        /// <param name="id">Application ID</param>
        /// <param name="request">Review data</param>
        /// <returns>Success response</returns>
        [HttpPatch("{id}/reject")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RejectApplication(int id, [FromBody] ReviewApplicationRequest request)
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
                if (!await _applicationService.CanRejectApplicationAsync(id))
                {
                    return BadRequest(ApiResponse.ErrorResponse("Application cannot be rejected"));
                }

                var rejected = await _applicationService.RejectApplicationAsync(id, request.ReviewNotes);
                
                if (!rejected)
                {
                    return NotFound(ApiResponse.ErrorResponse($"Application with ID {id} not found"));
                }

                return Ok(ApiResponse.SuccessResponse("Application rejected successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting application {ApplicationId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("Failed to reject application"));
            }
        }

        /// <summary>
        /// Withdraw an application
        /// </summary>
        /// <param name="id">Application ID</param>
        /// <param name="volunteerId">Volunteer ID (for validation)</param>
        /// <returns>Success response</returns>
        [HttpPatch("{id}/withdraw")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> WithdrawApplication(int id, [FromQuery] int volunteerId)
        {
            try
            {
                if (!await _applicationService.CanWithdrawApplicationAsync(id, volunteerId))
                {
                    return BadRequest(ApiResponse.ErrorResponse("Application cannot be withdrawn"));
                }

                var withdrawn = await _applicationService.WithdrawApplicationAsync(id);
                
                if (!withdrawn)
                {
                    return NotFound(ApiResponse.ErrorResponse($"Application with ID {id} not found"));
                }

                return Ok(ApiResponse.SuccessResponse("Application withdrawn successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error withdrawing application {ApplicationId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("Failed to withdraw application"));
            }
        }

        /// <summary>
        /// Delete an application
        /// </summary>
        /// <param name="id">Application ID</param>
        /// <returns>Success response</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteApplication(int id)
        {
            try
            {
                var application = await _applicationService.GetApplicationByIdAsync(id);
                if (application == null)
                {
                    return NotFound(ApiResponse.ErrorResponse($"Application with ID {id} not found"));
                }

                var deleted = await _applicationService.DeleteApplicationAsync(id);
                
                if (!deleted)
                {
                    return NotFound(ApiResponse.ErrorResponse($"Application with ID {id} not found"));
                }

                return Ok(ApiResponse.SuccessResponse("Application deleted successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting application {ApplicationId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("Failed to delete application"));
            }
        }

        // Helper method to map Application entity to ApplicationDto
        private async Task<ApplicationDto> MapToApplicationDtoAsync(Application application)
        {
            string? volunteerName = null;
            if (application.Volunteer != null)
            {
                volunteerName = $"{application.Volunteer.FirstName} {application.Volunteer.LastName}";
            }

            string? projectTitle = null;
            if (application.Project != null)
            {
                projectTitle = application.Project.Title;
            }
            else if (application.ProjectId > 0)
            {
                var project = await _projectService.GetProjectByIdAsync(application.ProjectId);
                projectTitle = project?.Title;
            }

            return new ApplicationDto
            {
                Id = application.Id,
                Status = application.Status,
                AppliedAt = application.AppliedAt,
                ReviewedAt = application.ReviewedAt,
                ReviewNotes = application.ReviewNotes,
                VolunteerId = application.VolunteerId,
                VolunteerName = volunteerName,
                ProjectId = application.ProjectId,
                ProjectTitle = projectTitle
            };
        }
    }
}
