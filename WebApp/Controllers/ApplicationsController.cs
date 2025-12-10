using Microsoft.AspNetCore.Mvc;
using WebApp.Interfaces;
using WebApp.Models;

namespace WebApp.Controllers
{
    public class ApplicationsController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ApplicationsController> _logger;

        public ApplicationsController(IUnitOfWork unitOfWork, ILogger<ApplicationsController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        // GET: Applications/Manage
        public async Task<IActionResult> Manage(int? projectId, ApplicationStatus? status)
        {
            // For now, return mock data. After DB setup, this will use _unitOfWork.Applications
            var applications = GetMockApplications();

            // Filter by project if specified
            if (projectId.HasValue)
            {
                applications = applications.Where(a => a.ProjectId == projectId.Value).ToList();
                var project = GetMockProjects().FirstOrDefault(p => p.Id == projectId.Value);
                ViewBag.ProjectTitle = project?.Title;
                ViewBag.ProjectId = projectId.Value;
            }

            // Filter by status if specified
            if (status.HasValue)
            {
                applications = applications.Where(a => a.Status == status.Value).ToList();
                ViewBag.FilterStatus = status.Value;
            }

            return View(applications);
        }

        // POST: Applications/Approve
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, string? reviewNotes)
        {
            // After DB setup, this will use _unitOfWork.Applications.GetByIdAsync()
            // For now, just redirect back with success message
            TempData["SuccessMessage"] = $"Application #{id} has been approved successfully.";
            return RedirectToAction(nameof(Manage));
        }

        // POST: Applications/Reject
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string? reviewNotes)
        {
            // After DB setup, this will use _unitOfWork.Applications.GetByIdAsync()
            // For now, just redirect back with success message
            TempData["SuccessMessage"] = $"Application #{id} has been rejected.";
            return RedirectToAction(nameof(Manage));
        }

        // Mock data methods - will be replaced with real data after DB setup
        private List<WebApp.Models.Application> GetMockApplications()
        {
            return new List<WebApp.Models.Application>
            {
                new WebApp.Models.Application
                {
                    Id = 1,
                    VolunteerId = 1,
                    ProjectId = 1,
                    Status = ApplicationStatus.Pending,
                    AppliedAt = DateTime.UtcNow.AddDays(-5),
                    Volunteer = new Volunteer 
                    { 
                        Id = 1, 
                        FirstName = "Marija", 
                        LastName = "Juriæ",
                        Email = "marija.juric@email.com",
                        PhoneNumber = "+385 91 123 4567",
                        City = "Split",
                        Skills = new List<string> { "Physical fitness", "Teamwork", "Leadership" }
                    },
                    Project = GetMockProjects().First(p => p.Id == 1)
                },
                new WebApp.Models.Application
                {
                    Id = 2,
                    VolunteerId = 2,
                    ProjectId = 1,
                    Status = ApplicationStatus.Pending,
                    AppliedAt = DateTime.UtcNow.AddDays(-3),
                    Volunteer = new Volunteer 
                    { 
                        Id = 2, 
                        FirstName = "Luka", 
                        LastName = "Periæ",
                        Email = "luka.peric@email.com",
                        PhoneNumber = "+385 92 234 5678",
                        City = "Split",
                        Skills = new List<string> { "Teamwork", "Communication" }
                    },
                    Project = GetMockProjects().First(p => p.Id == 1)
                },
                new WebApp.Models.Application
                {
                    Id = 3,
                    VolunteerId = 3,
                    ProjectId = 2,
                    Status = ApplicationStatus.Accepted,
                    AppliedAt = DateTime.UtcNow.AddDays(-8),
                    ReviewedAt = DateTime.UtcNow.AddDays(-6),
                    ReviewNotes = "Great experience with similar projects",
                    Volunteer = new Volunteer 
                    { 
                        Id = 3, 
                        FirstName = "Ana", 
                        LastName = "Tomiæ",
                        Email = "ana.tomic@email.com",
                        PhoneNumber = "+385 91 345 6789",
                        City = "Zagreb",
                        Skills = new List<string> { "Communication", "Empathy", "Cooking" }
                    },
                    Project = GetMockProjects().First(p => p.Id == 2)
                },
                new WebApp.Models.Application
                {
                    Id = 4,
                    VolunteerId = 4,
                    ProjectId = 2,
                    Status = ApplicationStatus.Pending,
                    AppliedAt = DateTime.UtcNow.AddDays(-2),
                    Volunteer = new Volunteer 
                    { 
                        Id = 4, 
                        FirstName = "Ivan", 
                        LastName = "Kovaè",
                        Email = "ivan.kovac@email.com",
                        PhoneNumber = "+385 98 456 7890",
                        City = "Zagreb",
                        Skills = new List<string> { "Empathy", "Organization" }
                    },
                    Project = GetMockProjects().First(p => p.Id == 2)
                },
                new WebApp.Models.Application
                {
                    Id = 5,
                    VolunteerId = 5,
                    ProjectId = 3,
                    Status = ApplicationStatus.Rejected,
                    AppliedAt = DateTime.UtcNow.AddDays(-10),
                    ReviewedAt = DateTime.UtcNow.AddDays(-8),
                    ReviewNotes = "Does not meet minimum age requirement",
                    Volunteer = new Volunteer 
                    { 
                        Id = 5, 
                        FirstName = "Petra", 
                        LastName = "Novak",
                        Email = "petra.novak@email.com",
                        PhoneNumber = "+385 95 567 8901",
                        City = "Osijek",
                        Skills = new List<string> { "Animal care" }
                    },
                    Project = GetMockProjects().First(p => p.Id == 3)
                },
                new WebApp.Models.Application
                {
                    Id = 6,
                    VolunteerId = 6,
                    ProjectId = 3,
                    Status = ApplicationStatus.Pending,
                    AppliedAt = DateTime.UtcNow.AddDays(-1),
                    Volunteer = new Volunteer 
                    { 
                        Id = 6, 
                        FirstName = "Mateo", 
                        LastName = "Babiæ",
                        Email = "mateo.babic@email.com",
                        PhoneNumber = "+385 99 678 9012",
                        City = "Osijek",
                        Skills = new List<string> { "Handyman skills", "Painting", "Physical fitness" }
                    },
                    Project = GetMockProjects().First(p => p.Id == 3)
                }
            };
        }

        private List<Project> GetMockProjects()
        {
            return new List<Project>
            {
                new Project { Id = 1, Title = "Beach Cleanup Initiative", OrganizationId = 2 },
                new Project { Id = 2, Title = "Food Distribution for Homeless", OrganizationId = 1 },
                new Project { Id = 3, Title = "Animal Shelter Renovation", OrganizationId = 3 },
                new Project { Id = 4, Title = "Youth Mentorship Program", OrganizationId = 4 }
            };
        }
    }
}
