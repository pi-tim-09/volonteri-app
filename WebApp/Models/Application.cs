namespace WebApp.Models
{
    public enum ApplicationStatus
    {
        Pending,
        Accepted,
        Rejected,
        Withdrawn,
        Completed
    }

    public class Application
    {
        public int Id { get; set; }
        
        public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;
        
        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewNotes { get; set; }
        
        // Foreign keys
        public int VolunteerId { get; set; }
        public int ProjectId { get; set; }
        
        // Navigation properties
        public Volunteer? Volunteer { get; set; }
        public Project? Project { get; set; }
    }
}
