using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization; // Dodaj ovo!
using WebApp.Interfaces;
using WebApp.Models;
using WebApp.ViewModels;
using WebApp.Services;

namespace WebApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;

        public AccountController(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (await _unitOfWork.Volunteers.AnyAsync(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Email is already registered.");
                return View(model);
            }

            // Use secure PBKDF2 password hashing instead of SHA256
            var passwordHash = _passwordHasher.HashPassword(model.Password);

            var user = new Volunteer
            {
                Email = model.Email,
                PasswordHash = passwordHash,
                FirstName = model.FirstName,
                LastName = model.LastName,
                PhoneNumber = model.PhoneNumber,
                Role = model.Role,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            await _unitOfWork.Volunteers.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Find user by email first
            var user = await _unitOfWork.Volunteers
                .FirstOrDefaultAsync(u => u.Email == model.Email && u.IsActive);

            // Verify password using secure password hasher
            if (user == null || !_passwordHasher.VerifyPassword(user.PasswordHash, model.Password))
            {
                ModelState.AddModelError(string.Empty, "Incorrect email or password.");
                return View(model);
            }

            HttpContext.Session.SetInt32("UserId", user.Id);

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }
    }
}