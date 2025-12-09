namespace WebApp.Models
{
    public enum ProjectStatus
    {
        Draft,
        Published,
        InProgress,
        Completed,
        Cancelled
    }

    public class Project
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
        public int CurrentVolunteers { get; set; } = 0;
        
        public List<string> RequiredSkills { get; set; } = new();
        public List<string> Categories { get; set; } = new();
        
        public ProjectStatus Status { get; set; } = ProjectStatus.Draft;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        // Foreign key
        public int OrganizationId { get; set; }
        
        // Navigation properties
        public Organization? Organization { get; set; }
        public List<Application> Applications { get; set; } = new();
    }
}
