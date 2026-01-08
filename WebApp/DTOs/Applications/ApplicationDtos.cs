using System.ComponentModel.DataAnnotations;
using WebApp.Models;

namespace WebApp.DTOs.Applications
{
    /// <summary>
    /// DTO for application responses
    /// </summary>
    public class ApplicationDto
    {
        public int Id { get; set; }
        public ApplicationStatus Status { get; set; }
        public DateTime AppliedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewNotes { get; set; }
        public int VolunteerId { get; set; }
        public string? VolunteerName { get; set; }
        public int ProjectId { get; set; }
        public string? ProjectTitle { get; set; }
    }

    /// <summary>
    /// DTO for creating a new application
    /// </summary>
    public class CreateApplicationRequest
    {
        [Required(ErrorMessage = "Volunteer ID is required")]
        public int VolunteerId { get; set; }

        [Required(ErrorMessage = "Project ID is required")]
        public int ProjectId { get; set; }
    }

    /// <summary>
    /// DTO for reviewing an application (approve/reject)
    /// </summary>
    public class ReviewApplicationRequest
    {
        [Required(ErrorMessage = "Status is required")]
        public ApplicationStatus Status { get; set; }

        [StringLength(500, ErrorMessage = "Review notes cannot exceed 500 characters")]
        public string? ReviewNotes { get; set; }
    }

    /// <summary>
    /// DTO for application list responses
    /// </summary>
    public class ApplicationListDto
    {
        public List<ApplicationDto> Applications { get; set; } = new();
        public int TotalCount { get; set; }
    }
}
