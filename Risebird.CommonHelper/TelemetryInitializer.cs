using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

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
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null && httpContext.User.Identity.IsAuthenticated == true && httpContext.User.Identity.Name != null)
            {
                telemetry.Context.User.AuthenticatedUserId = httpContext.User.Identity.Name;
                //telemetry.Context.User.AuthenticatedUserId = httpContext.Session.GetString("email");
            }
        }
    }
}
