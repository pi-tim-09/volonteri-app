using Microsoft.AspNetCore.Mvc;
using WebApp.Interfaces;
using WebApp.Models;

namespace WebApp.Controllers
{
    public class ProjectsController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ProjectsController> _logger;

        public ProjectsController(IUnitOfWork unitOfWork, ILogger<ProjectsController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        // GET: Projects
        public async Task<IActionResult> Index(int? organizationId)
        {
            // For now, return mock data. After DB setup, this will use _unitOfWork.Projects.GetAllAsync()
            var projects = GetMockProjects();

            // Filter by organization if specified
            if (organizationId.HasValue)
            {
                projects = projects.Where(p => p.OrganizationId == organizationId.Value).ToList();
                ViewBag.OrganizationName = GetMockOrganizations()
                    .FirstOrDefault(o => o.Id == organizationId.Value)?.OrganizationName;
            }

            return View(projects);
        }

        // Mock data method - will be replaced with real data after DB setup
        private List<Project> GetMockProjects()
        {
            return new List<Project>
            {
                new Project
                {
                    Id = 1,
                    Title = "Beach Cleanup Initiative",
                    Description = "Join us for a coastal cleanup to protect marine life and keep our beaches pristine. We'll provide all equipment and refreshments.",
                    Location = "Baèvice Beach",
                    City = "Split",
                    StartDate = DateTime.UtcNow.AddDays(15),
                    EndDate = DateTime.UtcNow.AddDays(15),
                    ApplicationDeadline = DateTime.UtcNow.AddDays(7),
                    MaxVolunteers = 30,
                    CurrentVolunteers = 12,
                    RequiredSkills = new List<string> { "Physical fitness", "Teamwork" },
                    Categories = new List<string> { "Environment", "Community" },
                    Status = ProjectStatus.Published,
                    OrganizationId = 2,
                    CreatedAt = DateTime.UtcNow.AddDays(-20)
                },
                new Project
                {
                    Id = 2,
                    Title = "Food Distribution for Homeless",
                    Description = "Help distribute meals and care packages to homeless individuals in the city center. Compassion and reliability are essential.",
                    Location = "Main Square",
                    City = "Zagreb",
                    StartDate = DateTime.UtcNow.AddDays(5),
                    EndDate = DateTime.UtcNow.AddDays(5),
                    ApplicationDeadline = DateTime.UtcNow.AddDays(2),
                    MaxVolunteers = 20,
                    CurrentVolunteers = 18,
                    RequiredSkills = new List<string> { "Communication", "Empathy" },
                    Categories = new List<string> { "Social", "Community" },
                    Status = ProjectStatus.Published,
                    OrganizationId = 1,
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                },
                new Project
                {
                    Id = 3,
                    Title = "Animal Shelter Renovation",
                    Description = "Assist in renovating shelter facilities including painting, repairs, and landscaping to create a better environment for our furry friends.",
                    Location = "Shelter Grounds",
                    City = "Osijek",
                    StartDate = DateTime.UtcNow.AddDays(30),
                    EndDate = DateTime.UtcNow.AddDays(32),
                    ApplicationDeadline = DateTime.UtcNow.AddDays(20),
                    MaxVolunteers = 15,
                    CurrentVolunteers = 5,
                    RequiredSkills = new List<string> { "Handyman skills", "Painting", "Physical fitness" },
                    Categories = new List<string> { "Animals", "Construction" },
                    Status = ProjectStatus.Published,
                    OrganizationId = 3,
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                },
                new Project
                {
                    Id = 4,
                    Title = "Youth Mentorship Program",
                    Description = "Mentor young students in STEM subjects and career development. Weekly commitment required for 3 months.",
                    Location = "Community Center",
                    City = "Rijeka",
                    StartDate = DateTime.UtcNow.AddDays(45),
                    EndDate = DateTime.UtcNow.AddDays(135),
                    ApplicationDeadline = DateTime.UtcNow.AddDays(30),
                    MaxVolunteers = 10,
                    CurrentVolunteers = 3,
                    RequiredSkills = new List<string> { "Teaching", "STEM knowledge", "Patience" },
                    Categories = new List<string> { "Education", "Youth" },
                    Status = ProjectStatus.Published,
                    OrganizationId = 4,
                    CreatedAt = DateTime.UtcNow.AddDays(-15)
                },
                new Project
                {
                    Id = 5,
                    Title = "Tree Planting Campaign",
                    Description = "Large-scale reforestation project. Help plant 1000 trees to combat climate change and restore natural habitats.",
                    Location = "Medvednica Mountain",
                    City = "Zagreb",
                    StartDate = DateTime.UtcNow.AddDays(60),
                    EndDate = DateTime.UtcNow.AddDays(62),
                    ApplicationDeadline = DateTime.UtcNow.AddDays(45),
                    MaxVolunteers = 50,
                    CurrentVolunteers = 8,
                    RequiredSkills = new List<string> { "Physical fitness", "Outdoor work" },
                    Categories = new List<string> { "Environment", "Conservation" },
                    Status = ProjectStatus.Draft,
                    OrganizationId = 2,
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                }
            };
        }

        private List<Organization> GetMockOrganizations()
        {
            return new List<Organization>
            {
                new Organization { Id = 1, OrganizationName = "Red Cross Croatia" },
                new Organization { Id = 2, OrganizationName = "Green Action" },
                new Organization { Id = 3, OrganizationName = "Animal Shelter Osijek" },
                new Organization { Id = 4, OrganizationName = "Youth Development Foundation" }
            };
        }
    }
}
