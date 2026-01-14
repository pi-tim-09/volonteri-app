using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace WebApp.Middleware
{
    /// <summary>
    /// Middleware that adds security headers to all HTTP responses
    /// Implements OWASP security best practices
    /// </summary>
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SecurityHeadersMiddleware> _logger;
        private readonly IWebHostEnvironment _environment;

        public SecurityHeadersMiddleware(
            RequestDelegate next,
            ILogger<SecurityHeadersMiddleware> logger,
            IWebHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Content Security Policy - prevents XSS and injection attacks
            var cspPolicy = BuildContentSecurityPolicy();
            context.Response.Headers.Append("Content-Security-Policy", cspPolicy);

            // Anti-clickjacking protection
            context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");

            // Prevent MIME type sniffing
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

            // XSS Protection (legacy but still useful for older browsers)
            context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

            // Referrer Policy - control referrer information
            context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

            // Permissions Policy - control browser features
            context.Response.Headers.Append("Permissions-Policy",
                "geolocation=(), microphone=(), camera=()");

            // Remove server header for security through obscurity
            context.Response.Headers.Remove("Server");
            context.Response.Headers.Remove("X-Powered-By");

            _logger.LogDebug("Security headers added to response for {Path}", context.Request.Path);

            await _next(context);
        }

        private string BuildContentSecurityPolicy()
        {
            // Base policy that works with your current setup
            var policy = new List<string>
            {
                "default-src 'self'",
                "script-src 'self' 'unsafe-inline'", // Allow inline scripts (for now)
                "style-src 'self' 'unsafe-inline'", // Bootstrap Icons CDN
                "img-src 'self' data: https:",
                "font-src 'self'", // Bootstrap Icons fonts
                "connect-src 'self'",
                "frame-ancestors 'self'", // Additional clickjacking protection
                "base-uri 'self'",
                "form-action 'self'"
            };

            // In development, add more permissive rules for debugging
            if (_environment.IsDevelopment())
            {
                policy.Add("upgrade-insecure-requests"); // Comment out if testing HTTP
            }
            else
            {
                policy.Add("upgrade-insecure-requests"); // Force HTTPS in production
            }

            return string.Join("; ", policy);
        }
    }

    /// <summary>
    /// Extension method for easy middleware registration
    /// </summary>
    public static class SecurityHeadersMiddlewareExtensions
    {
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SecurityHeadersMiddleware>();
        }
    }
}