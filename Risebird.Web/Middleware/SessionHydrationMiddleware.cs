using Microsoft.AspNetCore.Http;
using Risebird.Web.Services;
using System;
using System.Threading.Tasks;

namespace Risebird.Web.Middleware
{
    /// <summary>
    /// Runs after authentication: restores session from claims before controllers treat empty session as signed-out.
    /// </summary>
    public class SessionHydrationMiddleware
    {
        private readonly RequestDelegate _next;

        public SessionHydrationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IRisebirdSessionBootstrapService sessionBootstrap)
        {
            if (ShouldAttemptHydration(context))
            {
                await sessionBootstrap.EnsureSessionAsync(context);
            }

            await _next(context);
        }

        private static bool ShouldAttemptHydration(HttpContext context)
        {
            if (context.User?.Identity?.IsAuthenticated != true)
                return false;

            if (!string.IsNullOrEmpty(context.Session.GetString("email")))
                return false;

            var path = context.Request.Path.Value ?? string.Empty;
            if (path.StartsWith("/Session/SignIn", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("/Session/SignOut", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("/Session/SignedOut", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }
    }
}
