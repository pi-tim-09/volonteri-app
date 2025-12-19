using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Interfaces.Services;
using WebApp.Models;

namespace WebApp.Controllers
{
    [Authorize]
    public class OrganizationsController : Controller
    {
        private readonly IOrganizationService _organizationService;
        private readonly ILogger<OrganizationsController> _logger;

        public OrganizationsController(IOrganizationService organizationService, ILogger<OrganizationsController> logger)
        {
            _organizationService = organizationService;
            _logger = logger;
        }

        // GET: Organizations
        public async Task<IActionResult> Index()
        {
            try
            {
                var organizations = await _organizationService.GetAllOrganizationsAsync();
                return View(organizations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading organizations index page");
                TempData["ErrorMessage"] = "An error occurred while loading organizations.";
                return View(new List<Organization>());
            }
        }
    }
}
