using WebApp.Middleware;

namespace WebApp.Extensions
{
    /// <summary>
    /// Extension methods for HttpContext to access security-related values
    /// </summary>
    public static class HttpContextExtensions
    {
        /// <summary>
        /// Gets the CSP nonce for the current request
        /// Use this in views to add nonce attribute to inline scripts
        /// </summary>
        /// <param name="context">The HTTP context</param>
        /// <returns>The CSP nonce value or empty string if not found</returns>
        public static string GetCspNonce(this HttpContext context)
        {
            return context.Items.TryGetValue(SecurityHeadersMiddleware.NonceKey, out var nonce)
                ? nonce?.ToString() ?? string.Empty
                : string.Empty;
        }
    }
}
