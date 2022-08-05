using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Web.Hepler;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading;

namespace Risebird.Web.Controllers
{
    public class SessionController : Controller
    {
        private IApplicationErrorLogger _applicationErrorLogger;
        private readonly HttpClient _httpClient;
        private IAccountService _accountService;

        public SessionController(HttpClientHelper factory, IOptions<AzureAdB2COptions> b2cOptions, IApplicationErrorLogger applicationErrorLogger, IAccountService accountService)
        {
            _httpClient = factory.GetHttpClient();
            AzureAdB2COptions = b2cOptions.Value;
            _applicationErrorLogger = applicationErrorLogger;
            _accountService = accountService;
        }

        public AzureAdB2COptions AzureAdB2COptions { get; set; }

        /// <summary>
        /// Sign In
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult SignIn()
        {
            string userEmailId = string.Empty;
            try
            {
                var userClaims = User.Identity as ClaimsIdentity;
                userEmailId = userClaims?.FindFirst(ClaimTypes.Email)?.Value;
                var redirectUrl = Url.Action(nameof(HomeController.Index), "Home");
                var logindetails = Challenge(
                    new AuthenticationProperties { RedirectUri = redirectUrl },
                    OpenIdConnectDefaults.AuthenticationScheme);
                return logindetails;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex, userEmailId);
                return null;
            }
        }

        /// <summary>
        /// Password Reset
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult ResetPassword()
        {
            string userEmailId = string.Empty;
            try
            {
                var redirectUrl = Url.Action(nameof(HomeController.Index), "Home");
                var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
                properties.Items[AzureAdB2COptions.PolicyAuthenticationProperty] = AzureAdB2COptions.ResetPasswordPolicyId;
                return Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex, userEmailId);
                return null;
            }
        }

        /// <summary>
        /// Edit Profile
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult EditProfile()
        {
            string userEmailId = string.Empty;
            try
            {
                var userClaims = User.Identity as ClaimsIdentity;
                userEmailId = userClaims?.FindFirst(ClaimTypes.Email)?.Value;
                var redirectUrl = Url.Action(nameof(HomeController.Index), "Home");
                var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
                properties.Items[AzureAdB2COptions.PolicyAuthenticationProperty] = AzureAdB2COptions.EditProfilePolicyId;
                return Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex, userEmailId);
                return null;
            }
        }

        /// <summary>
        /// Sign Out
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult SignOut()
        {
            string userEmailId = string.Empty;
            CancellationToken ct;
            try
            {
                if (User.Identity.IsAuthenticated)
                {
                    //to db change
                    var sessionId = HttpContext.Session.Id;
                    var userClaims = User.Identity as ClaimsIdentity;
                    string emailId = userClaims?.FindFirst("emails")?.Value;
                    var updated = _accountService.Signout(emailId, sessionId);
                    var callbackUrl = Url.Action(nameof(SignedOut), "Session", values: null, protocol: Request.Scheme);
                    var token = HttpContext.Session.GetString("Token");
                    var path = $"https://{AzureAdB2COptions.TenantName}.b2clogin.com/{AzureAdB2COptions.TenantName}.onmicrosoft.com/{AzureAdB2COptions.SignUpSignInPolicyId}/oauth2/v2.0/logout?post_logout_redirect_uri={AzureAdB2COptions.RedirectUri}";
                    var request = new HttpRequestMessage(HttpMethod.Get, path);
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var response = _httpClient.SendAsync(request, ct).Result;

                    if (token != null)
                    {
                        HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
                        HttpContext.Session.Clear();
                        CookieOptions option = new CookieOptions();
                        if (Request.Cookies["Risebirdportal.Session"] != null)
                        {
                            option.Expires = DateTime.Now.AddDays(-1);
                            Response.Cookies.Append("Risebirdportal.Session", "", option);
                        }

                    }
                    else
                    {
                        // Removing Session
                        HttpContext.Session.Clear();
                        CookieOptions option = new CookieOptions();
                        if (Request.Cookies["Risebirdportal.Session"] != null)
                        {
                            option.Expires = DateTime.Now.AddDays(-1);
                            Response.Cookies.Append("Risebirdportal.Session", "", option);
                        }
                    }

                    return SignOut(new AuthenticationProperties { RedirectUri = callbackUrl },
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        OpenIdConnectDefaults.AuthenticationScheme);
                }
                else
                {
                    return RedirectToAction("SignIn", "Session");
                }

            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex, userEmailId);
                return null;
            }
        }

        /// <summary>
        /// Signed out
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult SignedOut()
        {
            try
            {
                if (User != null && User.Identity != null && User.Identity.IsAuthenticated)
                {
                    // Redirect to home page if the user is authenticated.
                    return RedirectToAction(nameof(HomeController.Index), "Home");
                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex, "System");
                return null;
            }
        }
    }
}