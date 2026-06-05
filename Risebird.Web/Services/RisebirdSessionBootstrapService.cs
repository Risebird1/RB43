using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Risebird.Business.Interface;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Risebird.Web.Services
{
    /// <summary>
    /// Rehydrates ASP.NET session keys from the auth cookie when Redis/session data is empty but the user is still signed in.
    /// Prevents false-positive redirects to SignOut/SignIn on hard reload of deep-linked pages (e.g. DM Approval).
    /// </summary>
    public interface IRisebirdSessionBootstrapService
    {
        Task<bool> EnsureSessionAsync(HttpContext httpContext);
    }

    public class RisebirdSessionBootstrapService : IRisebirdSessionBootstrapService
    {
        private readonly IUserService _userService;

        public RisebirdSessionBootstrapService(IUserService userService)
        {
            _userService = userService;
        }

        public async Task<bool> EnsureSessionAsync(HttpContext httpContext)
        {
            if (httpContext?.Session == null)
                return false;

            if (!string.IsNullOrEmpty(httpContext.Session.GetString("email")))
                return true;

            if (httpContext.User?.Identity?.IsAuthenticated != true)
                return false;

            var claims = httpContext.User.Identity as ClaimsIdentity;
            var emailId = claims?.FindFirst("emails")?.Value;
            if (string.IsNullOrWhiteSpace(emailId))
                return false;

            var sessionId = httpContext.Session.Id;
            var menuData = await _userService.GetMenuBasedOnUserEmailld(emailId, sessionId);
            var menus = menuData?
                .GroupBy(p => p.functionName)
                .Select(g => g.First())
                .ToList();

            if (menus == null || menus.Count == 0)
                return false;

            httpContext.Session.SetString("email", JsonConvert.SerializeObject(emailId));
            httpContext.Session.SetString("profileName", JsonConvert.SerializeObject(menus.First().profileName));
            httpContext.Session.SetString("userMenus", JsonConvert.SerializeObject(menus));
            httpContext.Session.SetString("UserLogedIn", "Yes");
            return true;
        }
    }
}
