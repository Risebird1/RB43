using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Web.Controllers
{
    /// <summary>
    /// Plivo webhooks for browser calls placed via PlivoCallMaker.js.
    /// Plivo Application Answer URL = https://portal.risebird.io/PlivoVoice/Outbound (POST).
    /// </summary>
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public class PlivoVoiceController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IUserRepository _userRepository;
        private readonly IPlivoCXService _plivoCXService;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public PlivoVoiceController(IConfiguration configuration, IUserRepository userRepository,
            IPlivoCXService plivoCXService, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _userRepository = userRepository;
            _plivoCXService = plivoCXService;
            _applicationErrorLogger = applicationErrorLogger;
        }

        // POST: PlivoVoice/Outbound — dials the number the browser asked for and records the call.
        [HttpPost]
        public IActionResult Outbound()
        {
            var destination = ToDialNumber(GetParam("X-PH-To") ?? GetParam("To") ?? GetParam("Destination"));
            var callerNumber = _configuration["PlivoSettings:CallerNumber"];

            var xml = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?><Response>");
            if (string.IsNullOrEmpty(destination))
            {
                xml.Append("<Hangup/>");
            }
            else
            {
                // Plivo's <Dial> has no record attribute; recording is done with <Record startOnDialAnswer="true">
                // placed before <Dial>. redirect="false" lets the call continue to the Dial.
                var callbackUrl = SecurityElement.Escape("https://" + Request.Host + "/PlivoVoice/RecordingCallback");
                xml.Append("<Record action=\"").Append(callbackUrl)
                   .Append("\" method=\"POST\" redirect=\"false\" startOnDialAnswer=\"true\" maxLength=\"3600\" callbackUrl=\"")
                   .Append(callbackUrl).Append("\" callbackMethod=\"POST\" />");

                xml.Append("<Dial");
                if (!string.IsNullOrWhiteSpace(callerNumber))
                {
                    xml.Append(" callerId=\"").Append(SecurityElement.Escape(callerNumber.Trim().TrimStart('+'))).Append('"');
                }
                xml.Append("><Number>").Append(SecurityElement.Escape(destination)).Append("</Number></Dial>");
            }
            xml.Append("</Response>");

            return Content(xml.ToString(), "application/xml", Encoding.UTF8);
        }

        // POST: PlivoVoice/RecordingCallback — Plivo delivers the recording details here
        // (the <Record> action / callbackUrl in Outbound). Stores the recording URL on the
        // CallRecordingData row logged by User/makecall (CallSid = Plivo CallUUID), so the
        // Call Recordings / Call Analytics pages can play it. Always returns 200 to Plivo.
        [HttpPost]
        public async Task<IActionResult> RecordingCallback()
        {
            try
            {
                var callUuid = GetParam("CallUUID") ?? GetParam("call_uuid");
                if (string.IsNullOrWhiteSpace(callUuid))
                {
                    return Ok();
                }

                var recordingUrl = GetParam("RecordUrl") ?? GetParam("RecordingUrl") ?? GetParam("recording_url");
                int? duration = null;
                if (int.TryParse(GetParam("RecordingDuration") ?? GetParam("recording_duration"), out var d) && d > 0)
                {
                    duration = d;
                }

                // No URL in the payload: look it up by call UUID (recording-only, no transcription cost).
                if (string.IsNullOrWhiteSpace(recordingUrl))
                {
                    var info = await _plivoCXService.GetRecordingInfoAsync(callUuid);
                    recordingUrl = info.RecordingUrl;
                    duration ??= info.DurationSeconds;
                }

                if (!string.IsNullOrWhiteSpace(recordingUrl))
                {
                    await _userRepository.UpdatePlivoRecordingAsync(callUuid, recordingUrl, duration);
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return Ok();
        }

        private string GetParam(string name)
        {
            string value = null;
            if (Request.HasFormContentType && Request.Form.ContainsKey(name))
            {
                value = Request.Form[name].ToString();
            }
            else if (Request.Query.ContainsKey(name))
            {
                value = Request.Query[name].ToString();
            }
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        // Plivo dials international format only (country code + number, no "+").
        // Candidate/interviewer numbers are often stored as 10-digit Indian numbers without a country code;
        // sent as-is, Plivo reads e.g. "98..." as Iran (+98) and rejects the call ("Destination Country Barred").
        private const string DefaultCountryCode = "91";

        // "sip:919876543210@phone.plivo.com" / "+91 98765-43210" / "9876543210" / "09876543210" → "919876543210"
        private static string ToDialNumber(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }
            var value = raw.Trim();
            if (value.StartsWith("sip:", StringComparison.OrdinalIgnoreCase))
            {
                value = value.Substring(4);
            }
            var at = value.IndexOf('@');
            if (at >= 0)
            {
                value = value.Substring(0, at);
            }
            var hasPlus = value.StartsWith("+");
            var digits = new string(value.Where(char.IsDigit).ToArray());
            if (digits.Length == 0)
            {
                return null;
            }

            if (!hasPlus)
            {
                if (digits.StartsWith("00"))
                {
                    digits = digits.Substring(2); // international prefix: 00919876543210
                }
                else if (digits.Length == 11 && digits.StartsWith("0"))
                {
                    digits = digits.Substring(1); // local trunk prefix: 09876543210
                }
                if (digits.Length == 10)
                {
                    digits = DefaultCountryCode + digits; // national number without country code
                }
            }
            return digits;
        }
    }
}
