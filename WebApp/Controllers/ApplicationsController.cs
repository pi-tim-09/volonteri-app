    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using WebApp.Interfaces.Services;
    using WebApp.Models;

    namespace WebApp.Controllers
    {
        [Authorize] 
        public class ApplicationsController : Controller
        {
            private readonly IApplicationService _applicationService;
            private readonly IProjectService _projectService;
            private readonly ILogger<ApplicationsController> _logger;

            public ApplicationsController(
                IApplicationService applicationService,
                IProjectService projectService,
                ILogger<ApplicationsController> logger)
            {
                _applicationService = applicationService;
                _projectService = projectService;
                _logger = logger;
            }

            // GET: Applications/Manage
            [HttpGet]
            public async Task<IActionResult> Manage(int? projectId, ApplicationStatus? status)
            {
                try
                {
                    var applications = await _applicationService.GetFilteredApplicationsAsync(projectId, status);

                    // Set view data for filtering context
                    if (projectId.HasValue)
                    {
                        var project = await _projectService.GetProjectByIdAsync(projectId.Value);
                        ViewBag.ProjectTitle = project?.Title;
                        ViewBag.ProjectId = projectId.Value;
                    }

                    if (status.HasValue)
                    {
                        ViewBag.FilterStatus = status.Value;
                    }

                    return View(applications);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading applications manage page");
                    TempData["ErrorMessage"] = "An error occurred while loading applications.";
                    return View(new List<Application>());
                }
            }

            // POST: Applications/Approve
            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> Approve(int id, string? reviewNotes)
            {
                try
                {
                    var approved = await _applicationService.ApproveApplicationAsync(id, reviewNotes);

                    if (approved)
                    {
                        TempData["SuccessMessage"] = $"Application #{id} has been approved successfully.";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = $"Unable to approve application #{id}. It may not be pending or project is full.";
                    }

                    return RedirectToAction(nameof(Manage));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error approving application: {ApplicationId}", id);
                    TempData["ErrorMessage"] = "An error occurred while approving the application.";
                    return RedirectToAction(nameof(Manage));
                }
            }

            // POST: Applications/Reject
            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> Reject(int id, string? reviewNotes)
            {
                try
                {
                    var rejected = await _applicationService.RejectApplicationAsync(id, reviewNotes);

                    if (rejected)
                    {
                        TempData["SuccessMessage"] = $"Application #{id} has been rejected.";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = $"Unable to reject application #{id}. It may not be pending.";
                    }

                    return RedirectToAction(nameof(Manage));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error rejecting application: {ApplicationId}", id);
                    TempData["ErrorMessage"] = "An error occurred while rejecting the application.";
                    return RedirectToAction(nameof(Manage));
                }
            }
        }
    }
