using Microsoft.AspNetCore.Mvc;
using WebApp.Common;
using WebApp.DTOs.Projects;
using WebApp.Interfaces.Services;
using WebApp.Models;

namespace WebApp.Controllers.Api
{
    /// <summary>
    /// RESTful API Controller for Project management
    /// Follows Single Responsibility Principle - handles only HTTP concerns
    /// Business logic delegated to IProjectService (Dependency Inversion Principle)
    /// </summary>
    [ApiController]
    [Route("api/projects")]
    [Produces("application/json")]
    public class ProjectsApiController : ControllerBase
    {
        private readonly IProjectService _projectService;
        private readonly IOrganizationService _organizationService;
        private readonly ILogger<ProjectsApiController> _logger;

        public ProjectsApiController(
            IProjectService projectService,
            IOrganizationService organizationService,
            ILogger<ProjectsApiController> logger)
        {
            _projectService = projectService;
            _organizationService = organizationService;
            _logger = logger;
        }

        /// <summary>
        /// Get all projects or filter by organization
        /// </summary>
        /// <param name="organizationId">Optional organization ID filter</param>
        /// <returns>List of projects</returns>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<ProjectListDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProjects([FromQuery] int? organizationId)
        {
            try
            {
                IEnumerable<Project> projects;

                if (organizationId.HasValue)
                {
                    projects = await _projectService.GetProjectsByOrganizationAsync(organizationId.Value);
                }
                else
                {
                    projects = await _projectService.GetAllProjectsAsync();
                }

                var projectListDto = new ProjectListDto
                {
                    Projects = new List<ProjectDto>(),
                    TotalCount = projects.Count()
                };

                foreach (var project in projects)
                {
                    projectListDto.Projects.Add(await MapToProjectDtoAsync(project));
                }

                return Ok(ApiResponse<ProjectListDto>.SuccessResponse(projectListDto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving projects");
                return StatusCode(500, ApiResponse.ErrorResponse("Failed to retrieve projects"));
            }
        }

        /// <summary>
        /// Get a specific project by ID
        /// </summary>
        /// <param name="id">Project ID</param>
        /// <returns>Project details</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<ProjectDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProject(int id)
        {
            try
            {
                var project = await _projectService.GetProjectByIdAsync(id);
                
                if (project == null)
                {
                    return NotFound(ApiResponse.ErrorResponse($"Project with ID {id} not found"));
                }

                var projectDto = await MapToProjectDtoAsync(project);
                return Ok(ApiResponse<ProjectDto>.SuccessResponse(projectDto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving project {ProjectId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("Failed to retrieve project"));
            }
        }

        /// <summary>
        /// Create a new project
        /// </summary>
        /// <param name="request">Project creation data</param>
        /// <returns>Created project</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<ProjectDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateProject([FromBody] CreateProjectRequest request)
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
                // Validate organization exists
                if (!await _organizationService.OrganizationExistsAsync(request.OrganizationId))
                {
                    return BadRequest(ApiResponse.ErrorResponse($"Organization with ID {request.OrganizationId} not found"));
                }

                var project = new Project
                {
                    Title = request.Title,
                    Description = request.Description,
                    Location = request.Location,
                    City = request.City,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    ApplicationDeadline = request.ApplicationDeadline,
                    MaxVolunteers = request.MaxVolunteers,
                    RequiredSkills = request.RequiredSkills,
                    Categories = request.Categories,
                    OrganizationId = request.OrganizationId,
                    Status = ProjectStatus.Draft
                };

                var createdProject = await _projectService.CreateProjectAsync(project);
                var projectDto = await MapToProjectDtoAsync(createdProject);

                return CreatedAtAction(
                    nameof(GetProject),
                    new { id = createdProject.Id },
                    ApiResponse<ProjectDto>.SuccessResponse(projectDto, "Project created successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating project: {Title}", request.Title);
                return StatusCode(500, ApiResponse.ErrorResponse("Failed to create project"));
            }
        }

        /// <summary>
        /// Update an existing project
        /// </summary>
        /// <param name="id">Project ID</param>
        /// <param name="request">Project update data</param>
        /// <returns>Success response</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateProject(int id, [FromBody] UpdateProjectRequest request)
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
                // Validate organization exists
                if (!await _organizationService.OrganizationExistsAsync(request.OrganizationId))
                {
                    return BadRequest(ApiResponse.ErrorResponse($"Organization with ID {request.OrganizationId} not found"));
                }

                var project = new Project
                {
                    Id = id,
                    Title = request.Title,
                    Description = request.Description,
                    Location = request.Location,
                    City = request.City,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    ApplicationDeadline = request.ApplicationDeadline,
                    MaxVolunteers = request.MaxVolunteers,
                    RequiredSkills = request.RequiredSkills,
                    Categories = request.Categories,
                    Status = request.Status,
                    OrganizationId = request.OrganizationId,
                    UpdatedAt = DateTime.UtcNow
                };

                var updated = await _projectService.UpdateProjectAsync(id, project);
                
                if (!updated)
                {
                    return NotFound(ApiResponse.ErrorResponse($"Project with ID {id} not found"));
                }

                return Ok(ApiResponse.SuccessResponse("Project updated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating project {ProjectId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("Failed to update project"));
            }
        }

        /// <summary>
        /// Delete a project
        /// </summary>
        /// <param name="id">Project ID</param>
        /// <returns>Success response</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteProject(int id)
        {
            try
            {
                var project = await _projectService.GetProjectByIdAsync(id);
                if (project == null)
                {
                    return NotFound(ApiResponse.ErrorResponse($"Project with ID {id} not found"));
                }

                var deleted = await _projectService.DeleteProjectAsync(id);
                
                if (!deleted)
                {
                    return NotFound(ApiResponse.ErrorResponse($"Project with ID {id} not found"));
                }

                return Ok(ApiResponse.SuccessResponse("Project deleted successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting project {ProjectId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("Failed to delete project"));
            }
        }

        /// <summary>
        /// Publish a project
        /// </summary>
        /// <param name="id">Project ID</param>
        /// <returns>Success response</returns>
        [HttpPatch("{id}/publish")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PublishProject(int id)
        {
            try
            {
                var published = await _projectService.PublishProjectAsync(id);
                
                if (!published)
                {
                    return NotFound(ApiResponse.ErrorResponse($"Project with ID {id} not found or cannot be published"));
                }

                return Ok(ApiResponse.SuccessResponse("Project published successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing project {ProjectId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("Failed to publish project"));
            }
        }

        /// <summary>
        /// Complete a project
        /// </summary>
        /// <param name="id">Project ID</param>
        /// <returns>Success response</returns>
        [HttpPatch("{id}/complete")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CompleteProject(int id)
        {
            try
            {
                var completed = await _projectService.CompleteProjectAsync(id);
                
                if (!completed)
                {
                    return NotFound(ApiResponse.ErrorResponse($"Project with ID {id} not found or cannot be completed"));
                }

                return Ok(ApiResponse.SuccessResponse("Project completed successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing project {ProjectId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("Failed to complete project"));
            }
        }

        /// <summary>
        /// Cancel a project
        /// </summary>
        /// <param name="id">Project ID</param>
        /// <returns>Success response</returns>
        [HttpPatch("{id}/cancel")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CancelProject(int id)
        {
            try
            {
                var cancelled = await _projectService.CancelProjectAsync(id);
                
                if (!cancelled)
                {
                    return NotFound(ApiResponse.ErrorResponse($"Project with ID {id} not found or cannot be cancelled"));
                }

                return Ok(ApiResponse.SuccessResponse("Project cancelled successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling project {ProjectId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("Failed to cancel project"));
            }
        }

        // Helper method to map Project entity to ProjectDto
        private async Task<ProjectDto> MapToProjectDtoAsync(Project project)
        {
            string? organizationName = null;
            if (project.OrganizationId > 0)
            {
                var organization = await _organizationService.GetOrganizationByIdAsync(project.OrganizationId);
                organizationName = organization?.OrganizationName;
            }

            return new ProjectDto
            {
                Id = project.Id,
                Title = project.Title,
                Description = project.Description,
                Location = project.Location,
                City = project.City,
                StartDate = project.StartDate,
                EndDate = project.EndDate,
                ApplicationDeadline = project.ApplicationDeadline,
                MaxVolunteers = project.MaxVolunteers,
                CurrentVolunteers = project.CurrentVolunteers,
                RequiredSkills = project.RequiredSkills,
                Categories = project.Categories,
                Status = project.Status,
                CreatedAt = project.CreatedAt,
                UpdatedAt = project.UpdatedAt,
                OrganizationId = project.OrganizationId,
                OrganizationName = organizationName
            };
        }
    }
}
