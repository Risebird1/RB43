using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Web.Hepler;
using System;
using System.Security.Claims;

namespace Risebird.Web.Controllers
{
    public class SessionController : Controller
    {
        private IApplicationErrorLogger _applicationErrorLogger;
        public SessionController(IOptions<AzureAdB2COptions> b2cOptions, IApplicationErrorLogger applicationErrorLogger)
        {
            AzureAdB2COptions = b2cOptions.Value;
            _applicationErrorLogger = applicationErrorLogger;
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
            try
            {
                var callbackUrl = Url.Action(nameof(SignedOut), "Session", values: null, protocol: Request.Scheme);
                var token = HttpContext.Session.GetString("Token");
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
                
                 SignOut(new AuthenticationProperties { RedirectUri = callbackUrl },
                    CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme);
                return RedirectToAction("SignIn", "Session");
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
                if (User!= null && User.Identity!=null && User.Identity.IsAuthenticated)
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