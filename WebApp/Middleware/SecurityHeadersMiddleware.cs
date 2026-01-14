using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
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
        
        // Context key for storing the CSP nonce
        public const string NonceKey = "csp-nonce";

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

            // Generate a cryptographically secure nonce for this request
            var nonce = GenerateNonce();
            context.Items[NonceKey] = nonce;

            // Add security headers BEFORE calling next middleware (before response is sent)
            // This ensures headers can be modified
            
            // Only add CSP to non-API, non-Swagger endpoints
            // API endpoints return JSON and don't need CSP
            if (!isApiEndpoint && !isSwaggerEndpoint)
            {
                var cspPolicy = BuildContentSecurityPolicy(nonce);
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

            _logger.LogDebug("Security headers added to response for {Path} with nonce", context.Request.Path);

            // Call next middleware
            await _next(context);
        }

        private string BuildContentSecurityPolicy(string nonce)
        {
            // CSP policy with nonce support for inline scripts
            // This is more secure than 'unsafe-inline' while still allowing necessary inline scripts
            var policy = new List<string>
            {
                "default-src 'self'",
                // Allow scripts from self and inline scripts with correct nonce
                // Also allow inline event handlers for Bootstrap components
                $"script-src 'self' 'nonce-{nonce}' 'unsafe-inline'",
                // Allow styles from self and inline styles (needed for Bootstrap)
                "style-src 'self' 'unsafe-inline'",
                "img-src 'self' data: https:",
                "font-src 'self' data:",
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
        
        /// <summary>
        /// Generates a cryptographically secure random nonce for CSP
        /// </summary>
        private static string GenerateNonce()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes);
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