namespace WebApp.Models
{
    public class Organization : User
    {
        public Organization()
        {
            Role = UserRole.Organization;
        }

        public string OrganizationName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public DateTime? VerifiedAt { get; set; }
        public bool IsVerified { get; set; } = false;
        
        // Navigation properties
        public List<Project> Projects { get; set; } = new();
    }
}
