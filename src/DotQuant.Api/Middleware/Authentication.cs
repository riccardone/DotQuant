using Microsoft.AspNetCore.Authentication;

namespace DotQuant.Api.Middleware
{
    public class Authentication
    {
        private readonly RequestDelegate _next;

        public Authentication(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Extract tenantId from route data (adjust if tenantId comes from another source)
            var tenantId = context.Request.RouteValues["tenantId"]?.ToString();

            if (!string.IsNullOrEmpty(tenantId))
            {
                // Construct the scheme name dynamically based on tenantId
                var schemeName = $"Bearer_{tenantId}";

                // Authenticate using the specific scheme
                var authResult = await context.AuthenticateAsync(schemeName);
                if (authResult.Succeeded)
                {
                    // If authentication succeeds, set the principal to the current User
                    context.User = authResult.Principal;
                }
                else
                {
                    // If authentication fails, return Unauthorized
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.Headers.WWWAuthenticate = authResult.Failure?.Message;
                    return;
                }
            }

            // Proceed to the next middleware if authentication succeeds
            await _next(context);
        }
    }
}
