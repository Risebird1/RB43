using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Risebird.Web.Hepler
{
    public static class ClaimsPrincipalExtensions
    {
        public static string FindFirstValue(this ClaimsPrincipal principal, string claimType, bool throwIfNotFound = false)
        {
            Guard.ArgumentNotNull(principal, nameof(principal));

            var value = principal.FindFirst(claimType)?.Value;
            if (throwIfNotFound && string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(
                    string.Format(CultureInfo.InvariantCulture, "The supplied principal does not contain a claim of type {0}", claimType));
            }

            return value;
        }

        public static string GetIssuerValue(this ClaimsPrincipal principal, bool throwIfNotFound = true)
        {
            return principal.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier, throwIfNotFound);
        }

        

       

        public static string GetDisplayNameValue(this ClaimsPrincipal principal)
        {
            return principal.FindFirstValue(ClaimTypes.Name, true);
        }

        public static string GetEmailValue(this ClaimsPrincipal principal)
        {
            return principal.FindFirstValue(ClaimTypes.Email, true);
        }

        public static string GetUserName(this ClaimsPrincipal principal)
        {
            return principal.FindFirstValue(ClaimsIdentity.DefaultNameClaimType);
        }

        public static bool IsSignedInToApplication(this ClaimsPrincipal principal)
        {
            Guard.ArgumentNotNull(principal, nameof(principal));
            return principal.Identity != null && principal.Identity.IsAuthenticated;
        }
    }
}
