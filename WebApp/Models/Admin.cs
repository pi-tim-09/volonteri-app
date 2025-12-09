namespace WebApp.Models
{
    public class Admin : User
    {
        public Admin()
        {
            Role = UserRole.Admin;
        }

        public string Department { get; set; } = string.Empty;
        public bool CanManageUsers { get; set; } = true;
        public bool CanManageOrganizations { get; set; } = true;
        public bool CanManageProjects { get; set; } = true;
    }
}
