using Microsoft.AspNetCore.Mvc;
using WebApp.Common;
using WebApp.DTOs.Auth;
using WebApp.Interfaces;
using WebApp.Services;

namespace WebApp.Controllers.Api
{
    /// <summary>
    /// RESTful API Controller for Authentication
    /// Provides JWT token generation for API authorization
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    [Produces("application/json")]
    public class AuthApiController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthApiController> _logger;

        public AuthApiController(
            IUnitOfWork unitOfWork,
            IPasswordHasher passwordHasher,
            IConfiguration configuration,
            ILogger<AuthApiController> logger)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Authenticate user and get JWT token
        /// </summary>
        /// <param name="request">Login credentials</param>
        /// <returns>JWT token for API authorization</returns>
        /// <remarks>
        /// Demo credentials (for testing when database is empty):
        /// - Admin: demo.admin@example.com / Demo123!
        /// - Organization: demo.org@example.com / Demo123!
        /// - Volunteer: demo.volunteer@example.com / Demo123!
        /// </remarks>
        [HttpPost("login")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse.ErrorResponse("Validation failed", errors));
            }

            try
            {
                // Check for demo/mock credentials first (for demonstration purposes)
                var demoAuthResult = TryAuthenticateWithDemoCredentials(request.Email, request.Password);
                if (demoAuthResult != null)
                {
                    _logger.LogInformation("Demo login successful for: {Email}", request.Email);
                    return Ok(demoAuthResult);
                }

                // Try to find user in all repositories (Volunteer, Organization, Admin)
                var volunteer = await _unitOfWork.Volunteers
                    .FirstOrDefaultAsync(u => u.Email == request.Email);
                
                var organization = await _unitOfWork.Organizations
                    .FirstOrDefaultAsync(u => u.Email == request.Email);
                
                var admin = await _unitOfWork.Admins
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                // Get the user (whichever is not null)
                var user = (volunteer as object) ?? (organization as object) ?? (admin as object);

                if (user == null)
                {
                    _logger.LogWarning("Login attempt with non-existent email: {Email}", request.Email);
                    return Unauthorized(ApiResponse.ErrorResponse("Invalid email or password"));
                }

                // Extract common properties using pattern matching
                var (passwordHash, isActive, role) = user switch
                {
                    Models.Volunteer v => (v.PasswordHash, v.IsActive, v.Role.ToString()),
                    Models.Organization o => (o.PasswordHash, o.IsActive, o.Role.ToString()),
                    Models.Admin a => (a.PasswordHash, a.IsActive, a.Role.ToString()),
                    _ => throw new InvalidOperationException("Unknown user type")
                };

                // Verify password
                if (!_passwordHasher.VerifyPassword(passwordHash, request.Password))
                {
                    _logger.LogWarning("Failed login attempt for email: {Email}", request.Email);
                    return Unauthorized(ApiResponse.ErrorResponse("Invalid email or password"));
                }

                // Check if user is active
                if (!isActive)
                {
                    _logger.LogWarning("Login attempt for inactive user: {Email}", request.Email);
                    return Unauthorized(ApiResponse.ErrorResponse("Account is not active"));
                }

                // Generate JWT token
                var secureKey = _configuration["JWT:SecureKey"] 
                    ?? throw new InvalidOperationException("JWT SecureKey not configured");
                var expirationMinutes = 60; // Token valid for 60 minutes
                
                var token = JwtTokenProvider.CreateToken(
                    secureKey,
                    expirationMinutes,
                    request.Email,
                    role
                );

                var authResponse = new AuthResponse
                {
                    Token = token,
                    Email = request.Email,
                    Role = role,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes)
                };

                _logger.LogInformation("Successful login for user: {Email}", request.Email);
                return Ok(ApiResponse<AuthResponse>.SuccessResponse(authResponse, "Login successful"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for email: {Email}", request.Email);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred during login"));
            }
        }

        /// <summary>
        /// Try to authenticate with demo/mock credentials for demonstration purposes
        /// </summary>
        private ApiResponse<AuthResponse>? TryAuthenticateWithDemoCredentials(string email, string password)
        {
            // Demo credentials configuration
            var demoUsers = new Dictionary<string, (string Password, string Role)>
            {
                { "admin@volonteriapp.hr", ("admin", "Admin") },
                { "org@volonteriapp.hr", ("org", "Organization") },
                { "volonter@volonteriapp.hr", ("volonter", "Volunteer") }
            };

            // Check if this is a demo user
            if (!demoUsers.TryGetValue(email.ToLower(), out var demoUser))
            {
                return null; // Not a demo user, continue with normal authentication
            }

            // Verify demo password
            if (password != demoUser.Password)
            {
                return null; // Wrong password for demo user
            }

            // Generate JWT token for demo user
            var secureKey = _configuration["JWT:SecureKey"] 
                ?? throw new InvalidOperationException("JWT SecureKey not configured");
            var expirationMinutes = 60;
            
            var token = JwtTokenProvider.CreateToken(
                secureKey,
                expirationMinutes,
                email,
                demoUser.Role
            );

            var authResponse = new AuthResponse
            {
                Token = token,
                Email = email,
                Role = demoUser.Role,
                ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes)
            };

            return ApiResponse<AuthResponse>.SuccessResponse(
                authResponse, 
                "Demo login successful (mock data)");
        }
    }
}
