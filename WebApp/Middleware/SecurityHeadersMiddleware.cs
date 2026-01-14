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
            // Execute the next middleware first
            await _next(context);

            // Check if this is an API endpoint - API responses don't need CSP
            var isApiEndpoint = context.Request.Path.StartsWithSegments("/api");
            var isSwaggerEndpoint = context.Request.Path.StartsWithSegments("/swagger");

            // Only add CSP to HTML responses, not to API/JSON responses
            if (!isApiEndpoint && !isSwaggerEndpoint && 
                context.Response.ContentType?.Contains("text/html") == true)
            {
                var cspPolicy = BuildContentSecurityPolicy();
                context.Response.Headers.Append("Content-Security-Policy", cspPolicy);
            }

            // Anti-clickjacking protection - apply to ALL responses
            if (!context.Response.Headers.ContainsKey("X-Frame-Options"))
            {
                context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
            }

            // Prevent MIME type sniffing
            if (!context.Response.Headers.ContainsKey("X-Content-Type-Options"))
            {
                context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            }

            // XSS Protection (legacy but still useful for older browsers)
            if (!context.Response.Headers.ContainsKey("X-XSS-Protection"))
            {
                context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
            }

            // Referrer Policy - control referrer information
            if (!context.Response.Headers.ContainsKey("Referrer-Policy"))
            {
                context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
            }

            // Permissions Policy - control browser features
            if (!context.Response.Headers.ContainsKey("Permissions-Policy"))
            {
                context.Response.Headers.Append("Permissions-Policy",
                    "geolocation=(), microphone=(), camera=()");
            }

            // Remove server header for security through obscurity
            context.Response.Headers.Remove("Server");
            context.Response.Headers.Remove("X-Powered-By");

            _logger.LogDebug("Security headers added to response for {Path}", context.Request.Path);
        }

        private string BuildContentSecurityPolicy()
        {
            // Strict CSP policy without wildcards or unsafe-inline
            var policy = new List<string>
            {
                "default-src 'self'",
                "script-src 'self'", // Removed 'unsafe-inline' - all scripts must be in external files
                "style-src 'self'", // Removed 'unsafe-inline' - all styles must be in external CSS files or classes
                "img-src 'self' data:", // Only allow self-hosted images and data URIs (for inline SVG in CSS)
                "font-src 'self'", // Only self-hosted fonts
                "connect-src 'self'", // AJAX/fetch requests only to same origin
                "frame-ancestors 'self'", // Additional clickjacking protection
                "base-uri 'self'", // Prevent base tag injection
                "form-action 'self'", // Forms can only submit to same origin
                "object-src 'none'", // Disallow plugins (Flash, Java, etc.)
                "media-src 'self'", // Audio/video only from same origin
                "worker-src 'self'", // Web workers only from same origin
                "manifest-src 'self'" // PWA manifest only from same origin
            };

            // Only add upgrade-insecure-requests in production (HTTPS)
            // Don't add it locally as it breaks HTTP testing
            if (!_environment.IsDevelopment())
            {
                policy.Add("upgrade-insecure-requests");
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