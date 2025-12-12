using Microsoft.AspNetCore.Mvc;
using WebApp.Interfaces;
using WebApp.Models;
using WebApp.ViewModels;

namespace WebApp.Controllers
{
    public class UsersController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUnitOfWork unitOfWork, ILogger<UsersController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        // GET: Users
        public IActionResult Index(string? searchTerm, UserRole? roleFilter, bool? isActiveFilter, int pageNumber = 1, int pageSize = 10)
        {
            var allUsers = GetMockUsers();

            // Apply filters using IEnumerable instead of IQueryable to avoid expression tree issues
            IEnumerable<User> filteredUsers = allUsers;
            
            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                filteredUsers = filteredUsers.Where(u => 
                    u.FirstName.ToLower().Contains(searchTerm) ||
                    u.LastName.ToLower().Contains(searchTerm) ||
                    u.Email.ToLower().Contains(searchTerm) ||
                    u.PhoneNumber.Contains(searchTerm) ||
                    (u is Organization org && org.OrganizationName.ToLower().Contains(searchTerm))
                );
            }

            // Apply role filter
            if (roleFilter.HasValue)
            {
                filteredUsers = filteredUsers.Where(u => u.Role == roleFilter.Value);
            }

            // Apply status filter
            if (isActiveFilter.HasValue)
            {
                filteredUsers = filteredUsers.Where(u => u.IsActive == isActiveFilter.Value);
            }

            var filteredList = filteredUsers.ToList();

            // Calculate pagination
            var totalUsers = filteredList.Count;
            var totalPages = (int)Math.Ceiling(totalUsers / (double)pageSize);
            
            // Ensure page number is valid
            pageNumber = Math.Max(1, Math.Min(pageNumber, totalPages == 0 ? 1 : totalPages));

            var pagedUsers = filteredList
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Create view model
            var viewModel = new UserFilterViewModel
            {
                Users = pagedUsers,
                SearchTerm = searchTerm,
                RoleFilter = roleFilter,
                IsActiveFilter = isActiveFilter,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages,
                TotalUsers = totalUsers,
                TotalAdmins = allUsers.Count(u => u.Role == UserRole.Admin),
                TotalOrganizations = allUsers.Count(u => u.Role == UserRole.Organization),
                TotalVolunteers = allUsers.Count(u => u.Role == UserRole.Volunteer)
            };

            return View(viewModel);
        }

        // GET: Users/Details/5
        public IActionResult Details(int id)
        {
            var user = GetMockUsers().FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        // GET: Users/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(UserVM userVm)
        {
            if (ModelState.IsValid)
            {
                // In real implementation, this would save to database
                TempData["SuccessMessage"] = $"User {userVm.FirstName} {userVm.LastName} created successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(userVm);
        }

        // GET: Users/Edit/5
        public IActionResult Edit(int id)
        {
            var user = GetMockUsers().FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            var userVm = new UserVM
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                IsActive = user.IsActive
            };

            return View(userVm);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, UserVM userVm)
        {
            if (id != userVm.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // In real implementation, this would update the database
                TempData["SuccessMessage"] = $"User {userVm.FirstName} {userVm.LastName} updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(userVm);
        }

        // GET: Users/Delete/5
        public IActionResult Delete(int id)
        {
            var user = GetMockUsers().FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var user = GetMockUsers().FirstOrDefault(u => u.Id == id);
            if (user != null)
            {
                // In real implementation, this would delete from database
                TempData["SuccessMessage"] = $"User {user.FirstName} {user.LastName} deleted successfully!";
            }
            return RedirectToAction(nameof(Index));
        }

        // Mock data method - will be replaced with real data after DB setup
        private List<User> GetMockUsers()
        {
            return new List<User>
            {
                new Admin
                {
                    Id = 1,
                    Email = "admin@volonteri.hr",
                    FirstName = "Marko",
                    LastName = "Marković",
                    PhoneNumber = "+385 1 234 5678",
                    Role = UserRole.Admin,
                    Department = "IT",
                    CanManageUsers = true,
                    CanManageOrganizations = true,
                    CanManageProjects = true,
                    CreatedAt = DateTime.UtcNow.AddMonths(-12),
                    LastLoginAt = DateTime.UtcNow.AddHours(-2),
                    IsActive = true,
                    PasswordHash = "hashed_password"
                },
                new Admin
                {
                    Id = 2,
                    Email = "ana.admin@volonteri.hr",
                    FirstName = "Ana",
                    LastName = "Kovačić",
                    PhoneNumber = "+385 1 345 6789",
                    Role = UserRole.Admin,
                    Department = "Support",
                    CanManageUsers = true,
                    CanManageOrganizations = false,
                    CanManageProjects = false,
                    CreatedAt = DateTime.UtcNow.AddMonths(-6),
                    LastLoginAt = DateTime.UtcNow.AddDays(-1),
                    IsActive = true,
                    PasswordHash = "hashed_password"
                },
                new Volunteer
                {
                    Id = 3,
                    Email = "ivan.volunteer@gmail.com",
                    FirstName = "Ivan",
                    LastName = "Horvat",
                    PhoneNumber = "+385 91 234 5678",
                    Role = UserRole.Volunteer,
                    DateOfBirth = new DateTime(1995, 5, 15),
                    Address = "Trg bana Jelačića 5",
                    City = "Zagreb",
                    Bio = "Passionate about helping the community and environmental causes.",
                    Skills = new List<string> { "First Aid", "Event Management", "Social Media" },
                    Interests = new List<string> { "Environment", "Education", "Animals" },
                    VolunteerHours = 120,
                    CreatedAt = DateTime.UtcNow.AddMonths(-8),
                    LastLoginAt = DateTime.UtcNow.AddHours(-5),
                    IsActive = true,
                    PasswordHash = "hashed_password"
                },
                new Volunteer
                {
                    Id = 4,
                    Email = "petra.novak@gmail.com",
                    FirstName = "Petra",
                    LastName = "Novak",
                    PhoneNumber = "+385 92 345 6789",
                    Role = UserRole.Volunteer,
                    DateOfBirth = new DateTime(1998, 8, 22),
                    Address = "Maksimirska 10",
                    City = "Zagreb",
                    Bio = "Student looking to gain experience while helping others.",
                    Skills = new List<string> { "Photography", "Writing", "Translation" },
                    Interests = new List<string> { "Culture", "Youth", "Education" },
                    VolunteerHours = 45,
                    CreatedAt = DateTime.UtcNow.AddMonths(-3),
                    LastLoginAt = DateTime.UtcNow.AddDays(-3),
                    IsActive = true,
                    PasswordHash = "hashed_password"
                },
                new Organization
                {
                    Id = 5,
                    Email = "info@redcross.hr",
                    FirstName = "Marija",
                    LastName = "Jurić",
                    PhoneNumber = "+385 1 234 5678",
                    Role = UserRole.Organization,
                    OrganizationName = "Red Cross Croatia",
                    Description = "Humanitarian organization providing emergency assistance and health services",
                    Address = "Ulica grada Vukovara 37",
                    City = "Zagreb",
                    IsVerified = true,
                    VerifiedAt = DateTime.UtcNow.AddDays(-30),
                    CreatedAt = DateTime.UtcNow.AddMonths(-24),
                    LastLoginAt = DateTime.UtcNow.AddDays(-1),
                    IsActive = true,
                    PasswordHash = "hashed_password"
                },
                new Organization
                {
                    Id = 6,
                    Email = "contact@greenaction.hr",
                    FirstName = "Tomislav",
                    LastName = "Matić",
                    PhoneNumber = "+385 1 987 6543",
                    Role = UserRole.Organization,
                    OrganizationName = "Green Action",
                    Description = "Environmental protection and sustainability initiatives",
                    Address = "Obala Hrvatskog narodnog preporoda 12",
                    City = "Split",
                    IsVerified = true,
                    VerifiedAt = DateTime.UtcNow.AddDays(-15),
                    CreatedAt = DateTime.UtcNow.AddMonths(-12),
                    LastLoginAt = DateTime.UtcNow.AddHours(-6),
                    IsActive = true,
                    PasswordHash = "hashed_password"
                },
                new Volunteer
                {
                    Id = 7,
                    Email = "luka.novak@gmail.com",
                    FirstName = "Luka",
                    LastName = "Novak",
                    PhoneNumber = "+385 98 765 4321",
                    Role = UserRole.Volunteer,
                    DateOfBirth = new DateTime(2000, 3, 10),
                    Address = "Strossmayerova 15",
                    City = "Osijek",
                    Bio = "Sports enthusiast eager to work with youth programs.",
                    Skills = new List<string> { "Coaching", "Team Building", "Public Speaking" },
                    Interests = new List<string> { "Sports", "Youth", "Health" },
                    VolunteerHours = 85,
                    CreatedAt = DateTime.UtcNow.AddMonths(-5),
                    LastLoginAt = DateTime.UtcNow.AddDays(-2),
                    IsActive = true,
                    PasswordHash = "hashed_password"
                },
                new Organization
                {
                    Id = 8,
                    Email = "info@animalrescue.hr",
                    FirstName = "Nina",
                    LastName = "Babić",
                    PhoneNumber = "+385 31 555 777",
                    Role = UserRole.Organization,
                    OrganizationName = "Animal Rescue Croatia",
                    Description = "Dedicated to rescuing and rehoming abandoned animals",
                    Address = "Retfala 1",
                    City = "Osijek",
                    IsVerified = false,
                    CreatedAt = DateTime.UtcNow.AddMonths(-2),
                    LastLoginAt = DateTime.UtcNow.AddDays(-5),
                    IsActive = true,
                    PasswordHash = "hashed_password"
                }
            };
        }
    }
}
