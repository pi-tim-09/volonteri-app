using Microsoft.AspNetCore.Mvc;
using WebApp.ViewModels;
using WebApp.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using WebApp.Services;
using WebApp.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace WebApp.Controllers
{
    public class AccountController : Controller 
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly IPasswordHasher _passwordHasher;

        public AccountController(IUnitOfWork unitOfWork, IConfiguration configuration, IPasswordHasher passwordHasher)
        {
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _passwordHasher = passwordHasher;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                if (await _unitOfWork.Volunteers.AnyAsync(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email is already registered.");
                    return View(model);
                }

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
                
                return RedirectToAction("Login");
            }
            catch 
            {

                ModelState.AddModelError(
                    string.Empty,
                    "Registration is currently unavailable. Please try again later."
                );
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var user = await _unitOfWork.Volunteers.FirstOrDefaultAsync(u => u.Email == model.Email && u.IsActive);
                if (user == null || !_passwordHasher.VerifyPassword(user.PasswordHash, model.Password))
                {
                    ModelState.AddModelError(string.Empty, "Invalid email or password.");

                    return View(model);
                }

                
                var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

                var claimsIdentity = new ClaimsIdentity(claims, "MyCookieAuth");
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true
                };

                await HttpContext.SignInAsync("MyCookieAuth", new ClaimsPrincipal(claimsIdentity), authProperties);

                return RedirectToAction("Index", "Home");
            }
            catch 
            {

                ModelState.AddModelError(
                    string.Empty,
                    "Login is currently unavailable. Please try again later."
                );
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("MyCookieAuth");
            return RedirectToAction("Login");
        }
    }
}