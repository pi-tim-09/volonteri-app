using Microsoft.AspNetCore.Mvc;
using WebApp.Interfaces.Services;
using WebApp.Models;
using WebApp.ViewModels;

namespace WebApp.Controllers
{
    public class UsersController : Controller
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        // GET: Users
        public async Task<IActionResult> Index(string? searchTerm, UserRole? roleFilter, bool? isActiveFilter, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var viewModel = await _userService.GetFilteredUsersAsync(
                    searchTerm, 
                    roleFilter, 
                    isActiveFilter, 
                    pageNumber, 
                    pageSize);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading users index page");
                TempData["ErrorMessage"] = "An error occurred while loading users.";
                return View(new UserFilterViewModel());
            }
        }

        // GET: Users/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound();
                }
                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user details for ID: {UserId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading user details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Users/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserVM userVm)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Check if email already exists
                    if (await _userService.EmailExistsAsync(userVm.Email))
                    {
                        ModelState.AddModelError("Email", "This email is already registered.");
                        return View(userVm);
                    }

                    var user = await _userService.CreateUserAsync(userVm);
                    TempData["SuccessMessage"] = $"User {user.FirstName} {user.LastName} created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating user: {Email}", userVm.Email);
                    ModelState.AddModelError("", "An error occurred while creating the user. Please try again.");
                }
            }
            return View(userVm);
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user for edit: {UserId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading the user.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UserVM userVm)
        {
            if (id != userVm.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var updated = await _userService.UpdateUserAsync(id, userVm);
                    if (!updated)
                    {
                        return NotFound();
                    }

                    TempData["SuccessMessage"] = $"User {userVm.FirstName} {userVm.LastName} updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating user: {UserId}", id);
                    ModelState.AddModelError("", "An error occurred while updating the user. Please try again.");
                }
            }
            return View(userVm);
        }

        // GET: Users/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound();
                }
                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user for delete: {UserId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading the user.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user != null)
                {
                    var deleted = await _userService.DeleteUserAsync(id);
                    if (deleted)
                    {
                        TempData["SuccessMessage"] = $"User {user.FirstName} {user.LastName} deleted successfully!";
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user: {UserId}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting the user.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
