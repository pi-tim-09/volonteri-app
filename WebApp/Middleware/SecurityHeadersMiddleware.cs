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
            // Check if this is an API or Swagger endpoint BEFORE processing
            var isApiEndpoint = context.Request.Path.StartsWithSegments("/api");
            var isSwaggerEndpoint = context.Request.Path.StartsWithSegments("/swagger");

            // Add security headers BEFORE calling next middleware (before response is sent)
            // This ensures headers can be modified
            
            // Only add CSP to non-API, non-Swagger endpoints
            // API endpoints return JSON and don't need CSP
            if (!isApiEndpoint && !isSwaggerEndpoint)
            {
                var cspPolicy = BuildContentSecurityPolicy();
                context.Response.Headers["Content-Security-Policy"] = cspPolicy;
            }

            // Anti-clickjacking protection - apply to ALL responses
            context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";

            // Prevent MIME type sniffing
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";

            // XSS Protection (legacy but still useful for older browsers)
            context.Response.Headers["X-XSS-Protection"] = "1; mode=block";

            // Referrer Policy - control referrer information
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            // Permissions Policy - control browser features
            context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";

            // Remove server header for security through obscurity
            context.Response.Headers.Remove("Server");
            context.Response.Headers.Remove("X-Powered-By");

            _logger.LogDebug("Security headers added to response for {Path}", context.Request.Path);

            // Call next middleware
            await _next(context);
        }

        private string BuildContentSecurityPolicy()
        {
            // Strict CSP policy without wildcards or unsafe-inline
            var policy = new List<string>
            {
                "default-src 'self'",
                "script-src 'self'",
                "style-src 'self'",
                "img-src 'self' data:",
                "font-src 'self'",
                "connect-src 'self'",
                "frame-ancestors 'self'",
                "base-uri 'self'",
                "form-action 'self'",
                "object-src 'none'",
                "media-src 'self'",
                "worker-src 'self'",
                "manifest-src 'self'"
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