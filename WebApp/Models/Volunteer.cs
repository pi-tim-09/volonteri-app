namespace WebApp.Models
{
    public class Volunteer : User
    {
        public Volunteer()
        {
            Role = UserRole.Volunteer;
        }

        public DateTime? DateOfBirth { get; set; }
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public List<string> Skills { get; set; } = new();
        public List<string> Interests { get; set; } = new();
        public int VolunteerHours { get; set; } = 0;
        
        // Navigation properties
        public List<Application> Applications { get; set; } = new();
    }
}
