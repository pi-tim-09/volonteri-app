using Microsoft.AspNetCore.Mvc;
using WebApp.Interfaces;
using WebApp.Models;

namespace WebApp.Controllers
{
    public class OrganizationsController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<OrganizationsController> _logger;

        public OrganizationsController(IUnitOfWork unitOfWork, ILogger<OrganizationsController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        // GET: Organizations
        public async Task<IActionResult> Index()
        {
            // For now, return mock data. After DB setup, this will use _unitOfWork.Organizations.GetAllAsync()
            var organizations = GetMockOrganizations();
            return View(organizations);
        }

        // Mock data method - will be replaced with real data after DB setup
        private List<Organization> GetMockOrganizations()
        {
            return new List<Organization>
            {
                new Organization
                {
                    Id = 1,
                    OrganizationName = "Red Cross Croatia",
                    Email = "info@redcross.hr",
                    FirstName = "Ana",
                    LastName = "Kovaèiæ",
                    PhoneNumber = "+385 1 234 5678",
                    City = "Zagreb",
                    Address = "Ulica grada Vukovara 37",
                    Description = "Humanitarian organization providing emergency assistance and health services",
                    IsVerified = true,
                    VerifiedAt = DateTime.UtcNow.AddDays(-30),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddMonths(-6)
                },
                new Organization
                {
                    Id = 2,
                    OrganizationName = "Green Action",
                    Email = "contact@greenaction.hr",
                    FirstName = "Marko",
                    LastName = "Horvat",
                    PhoneNumber = "+385 1 987 6543",
                    City = "Split",
                    Address = "Obala Hrvatskog narodnog preporoda 12",
                    Description = "Environmental protection and sustainability initiatives",
                    IsVerified = true,
                    VerifiedAt = DateTime.UtcNow.AddDays(-15),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddMonths(-3)
                },
                new Organization
                {
                    Id = 3,
                    OrganizationName = "Animal Shelter Osijek",
                    Email = "shelter@azil-osijek.hr",
                    FirstName = "Petra",
                    LastName = "Novak",
                    PhoneNumber = "+385 31 555 777",
                    City = "Osijek",
                    Address = "Retfala 1",
                    Description = "Animal rescue and adoption center",
                    IsVerified = false,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                },
                new Organization
                {
                    Id = 4,
                    OrganizationName = "Youth Development Foundation",
                    Email = "info@youthdev.hr",
                    FirstName = "Ivan",
                    LastName = "Matiæ",
                    PhoneNumber = "+385 21 444 888",
                    City = "Rijeka",
                    Address = "Korzo 15",
                    Description = "Educational programs and mentorship for young people",
                    IsVerified = true,
                    VerifiedAt = DateTime.UtcNow.AddDays(-45),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddMonths(-8)
                }
            };
        }
    }
}
