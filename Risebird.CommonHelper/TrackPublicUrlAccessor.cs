
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.CommonHelper
{
    public class TrackPublicUrlAccessor : ITrackPublicUrlAccessor
    {
        private TelemetryConfiguration telemetryConfiguration = null;
        private TelemetryClient telemetryClient = null;
        private readonly IConfiguration _configuration;
        public TrackPublicUrlAccessor(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public void SendTelemetry(string emailId, string requestedUrl)
        {
            if(!string.IsNullOrEmpty(emailId))
            {
                emailId = emailId.Replace("\"", "");
            }
            if(telemetryConfiguration == null)
            {
                telemetryConfiguration = TelemetryConfiguration.CreateDefault();
                telemetryConfiguration.InstrumentationKey = _configuration["ApplicationInsights:InstrumentationKey"];//"";
            }
            if( telemetryClient ==  null)
            {
                telemetryClient = new TelemetryClient(telemetryConfiguration);
            }
            Dictionary<string, string> customProperties = new Dictionary<string, string>
            {
                { "emailId", emailId},
                { "requestedUrl", requestedUrl}
            };

            telemetryClient.TrackTrace("User Details", SeverityLevel.Information, customProperties);
        }
    }
}
