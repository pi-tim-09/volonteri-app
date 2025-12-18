using Microsoft.AspNetCore.Mvc;
using WebApp.Interfaces.Services;
using WebApp.Models;

namespace WebApp.Controllers
{
    public class ProjectsController : Controller
    {
        private readonly IProjectService _projectService;
        private readonly IOrganizationService _organizationService;
        private readonly ILogger<ProjectsController> _logger;

        public ProjectsController(
            IProjectService projectService,
            IOrganizationService organizationService,
            ILogger<ProjectsController> logger)
        {
            _projectService = projectService;
            _organizationService = organizationService;
            _logger = logger;
        }

        // GET: Projects
        public async Task<IActionResult> Index(int? organizationId)
        {
            try
            {
                IEnumerable<Project> projects;

                if (organizationId.HasValue)
                {
                    // Use service layer - add this method to IProjectService
                    projects = await _projectService.GetProjectsByOrganizationAsync(organizationId.Value);
                    var organization = await _organizationService.GetOrganizationByIdAsync(organizationId.Value);
                    ViewBag.OrganizationName = organization?.OrganizationName;
                }
                else
                {
                    projects = await _projectService.GetAllProjectsAsync();
                }

                return View(projects);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading projects index page");
                TempData["ErrorMessage"] = "An error occurred while loading projects.";
                return View(new List<Project>());
            }
        }

        // GET: Projects/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var project = await _projectService.GetProjectByIdAsync(id.Value);

                if (project == null)
                {
                    return NotFound();
                }

                // Load organizations for dropdown
                ViewBag.Organizations = await _organizationService.GetAllOrganizationsAsync();

                return View(project);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading project for edit: {ProjectId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading the project.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Projects/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Project project)
        {
            if (id != project.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var updated = await _projectService.UpdateProjectAsync(id, project);
                    if (!updated)
                    {
                        return NotFound();
                    }

                    TempData["SuccessMessage"] = $"Project '{project.Title}' has been updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating project {ProjectId}", id);
                    ModelState.AddModelError("", "Unable to save changes. Please try again.");
                }
            }

            // If we got here, something failed, reload organizations and redisplay form
            try
            {
                ViewBag.Organizations = await _organizationService.GetAllOrganizationsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading organizations for project edit form");
            }
            
            return View(project);
        }
    }
}
