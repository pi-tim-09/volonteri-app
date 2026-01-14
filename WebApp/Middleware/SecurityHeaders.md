# Security Headers Implementation

## Overview
The `SecurityHeadersMiddleware` implements OWASP security best practices by adding security headers to all HTTP responses.

## Content Security Policy (CSP)

### Current Implementation
The CSP is configured to:
- Allow resources from the same origin (`'self'`)
- Allow inline styles (required for Bootstrap)
- Allow inline scripts with nonce support
- Block object/embed elements
- Upgrade insecure requests in production

### CSP Directives Explained

```
default-src 'self'              - Only load resources from same origin by default
script-src 'self' 'nonce-{...}' 'unsafe-inline'  - Scripts from same origin, with nonce, or inline
style-src 'self' 'unsafe-inline'                  - Styles from same origin or inline (for Bootstrap)
img-src 'self' data: https:     - Images from same origin, data URIs, or HTTPS
font-src 'self' data:           - Fonts from same origin or data URIs
connect-src 'self'              - AJAX/WebSocket only to same origin
frame-ancestors 'self'          - Can only be embedded in same origin
base-uri 'self'                 - Base tag can only use same origin
form-action 'self'              - Forms can only submit to same origin
object-src 'none'               - No plugins (Flash, etc.)
```

### Using Nonces in Views

If you need to add inline scripts that should be allowed by CSP, use the nonce:

```razor
@using WebApp.Extensions

<script nonce="@Context.GetCspNonce()">
    // Your inline JavaScript here
    console.log('This script is allowed by CSP');
</script>
```

### Why 'unsafe-inline' is Still Present

While we generate nonces, we keep `'unsafe-inline'` for backward compatibility with older browsers and Bootstrap's inline event handlers. Modern browsers will ignore `'unsafe-inline'` when nonces are present, making this configuration secure.

## Other Security Headers

### X-Frame-Options
Prevents clickjacking attacks by controlling if the page can be embedded in frames.
- Value: `SAMEORIGIN` (only allow embedding from same origin)

### X-Content-Type-Options
Prevents MIME type sniffing.
- Value: `nosniff`

### X-XSS-Protection
Legacy XSS protection for older browsers.
- Value: `1; mode=block`

### Referrer-Policy
Controls how much referrer information is sent with requests.
- Value: `strict-origin-when-cross-origin`

### Permissions-Policy
Controls which browser features can be used.
- Current: Disables geolocation, microphone, and camera

## Endpoints Without CSP

The following endpoints are excluded from CSP headers:
- `/api/*` - API endpoints that return JSON
- `/swagger/*` - Swagger UI endpoints

## ZAP Scan Compliance

This implementation addresses the "Content Security Policy (CSP) Header Not Set" alert by:
1. ✅ Adding CSP header to all HTML pages
2. ✅ Using secure CSP directives
3. ✅ Supporting nonces for inline scripts
4. ✅ Excluding API endpoints that don't need CSP
5. ✅ Using `upgrade-insecure-requests` in production

## Testing CSP

To test the CSP implementation:

1. **Browser DevTools**
   - Open the Network tab
   - Check response headers for `Content-Security-Policy`
   - Check Console for any CSP violations

2. **Online Tools**
   - [CSP Evaluator](https://csp-evaluator.withgoogle.com/)
   - [SecurityHeaders.com](https://securityheaders.com/)

3. **ZAP Scan**
   - Run OWASP ZAP automated scan
   - Verify "Content Security Policy (CSP) Header Not Set" alert is resolved

## Future Improvements

For even stricter security, consider:
1. Remove `'unsafe-inline'` from script-src once all inline scripts use nonces
2. Add `'strict-dynamic'` for better script loading security
3. Use CSP reporting to monitor violations
4. Implement Subresource Integrity (SRI) for external libraries
