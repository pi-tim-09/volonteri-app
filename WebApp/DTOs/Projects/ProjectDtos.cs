using System.ComponentModel.DataAnnotations;
using WebApp.Models;

namespace WebApp.DTOs.Projects
{
    /// <summary>
    /// DTO for project responses
    /// </summary>
    public class ProjectDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime ApplicationDeadline { get; set; }
        public int MaxVolunteers { get; set; }
        public int CurrentVolunteers { get; set; }
        public List<string> RequiredSkills { get; set; } = new();
        public List<string> Categories { get; set; } = new();
        public ProjectStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int OrganizationId { get; set; }
        public string? OrganizationName { get; set; }
    }

    /// <summary>
    /// DTO for creating a new project
    /// </summary>
    public class CreateProjectRequest
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Location is required")]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "City is required")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Start date is required")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "End date is required")]
        public DateTime EndDate { get; set; }

        [Required(ErrorMessage = "Application deadline is required")]
        public DateTime ApplicationDeadline { get; set; }

        [Required(ErrorMessage = "Max volunteers is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Max volunteers must be at least 1")]
        public int MaxVolunteers { get; set; }

        public List<string> RequiredSkills { get; set; } = new();
        public List<string> Categories { get; set; } = new();

        [Required(ErrorMessage = "Organization ID is required")]
        public int OrganizationId { get; set; }
    }

    /// <summary>
    /// DTO for updating an existing project
    /// </summary>
    public class UpdateProjectRequest
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Location is required")]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "City is required")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Start date is required")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "End date is required")]
        public DateTime EndDate { get; set; }

        [Required(ErrorMessage = "Application deadline is required")]
        public DateTime ApplicationDeadline { get; set; }

        [Required(ErrorMessage = "Max volunteers is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Max volunteers must be at least 1")]
        public int MaxVolunteers { get; set; }

        public List<string> RequiredSkills { get; set; } = new();
        public List<string> Categories { get; set; } = new();

        public ProjectStatus Status { get; set; }

        [Required(ErrorMessage = "Organization ID is required")]
        public int OrganizationId { get; set; }
    }

    /// <summary>
    /// DTO for project list responses
    /// </summary>
    public class ProjectListDto
    {
        public List<ProjectDto> Projects { get; set; } = new();
        public int TotalCount { get; set; }
    }
}
