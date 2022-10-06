using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;
using System;
using System.Security.Claims;

namespace Risebird.CommonHelper
{

    public class TelemetryInitializer : ITelemetryInitializer
    {
        private IHttpContextAccessor _httpContextAccessor;
        public TelemetryInitializer(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException("httpContextAccessor");
        }
        public void Initialize(ITelemetry telemetry)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext != null && httpContext.User != null && httpContext.User.Identity != null && httpContext.User.Identity is ClaimsIdentity)
                {
                    var userClaims = httpContext.User.Identity as ClaimsIdentity;

                    string emailId = userClaims?.FindFirst("emails")?.Value;

                    if (httpContext != null && httpContext.User.Identity.IsAuthenticated == true && (httpContext.User.Identity.Name != null || !string.IsNullOrEmpty(emailId)))
                    {
                        if (!string.IsNullOrEmpty(emailId))
                        {
                            telemetry.Context.User.AuthenticatedUserId = emailId;
                        }
                        else
                        {
                            telemetry.Context.User.AuthenticatedUserId = httpContext.User.Identity.Name;
                        }
                    }
                    return;
                    //else if (httpContext != null && httpContext.Session != null && httpContext.Session.GetString("PublicUrlAccessor") != null)
                    //{
                    //    telemetry.Context.User.AuthenticatedUserId = httpContext.Session.GetString("PublicUrlAccessor").ToString();
                    //}
                }
            }
            catch (Exception)
            {
                return;
            }
        }
    }
}
