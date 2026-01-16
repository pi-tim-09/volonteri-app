using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace WebApp.Middleware
{
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
            var isApiEndpoint = context.Request.Path.StartsWithSegments("/api");
            var isSwaggerEndpoint = context.Request.Path.StartsWithSegments("/swagger");

            context.Response.OnStarting(() =>
            {
                if (!isApiEndpoint && !isSwaggerEndpoint)
                {
                    var cspPolicy = BuildContentSecurityPolicy();
                    if (!context.Response.Headers.ContainsKey("Content-Security-Policy"))
                    {
                        context.Response.Headers["Content-Security-Policy"] = cspPolicy;
                    }
                }

                context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
                context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
                context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
                if (!_environment.IsDevelopment())
                {
                    context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";
                }

                context.Response.Headers.Remove("Server");
                context.Response.Headers.Remove("X-Powered-By");

                _logger.LogDebug("Security headers added to response for {Path}", context.Request.Path);

                return Task.CompletedTask;
            });

            await _next(context);
        }

        private string BuildContentSecurityPolicy()
        {
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

            if (!_environment.IsDevelopment())
            {
                policy.Add("upgrade-insecure-requests");
            }

            return string.Join("; ", policy);
        }
    }

    public static class SecurityHeadersMiddlewareExtensions
    {
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SecurityHeadersMiddleware>();
        }
    }
}