using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Net;

namespace Risebird.Web.Controllers
{
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public class PlivoVoiceController : Controller
    {
        private readonly IConfiguration _configuration;

        public PlivoVoiceController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        [HttpPost]
        public IActionResult Outbound()
        {
            var destination = FirstNonEmpty(
                Request.Form["To"].FirstOrDefault(),
                Request.Form["Destination"].FirstOrDefault(),
                Request.Form["X-PH-To"].FirstOrDefault(),
                Request.Query["To"].FirstOrDefault(),
                Request.Query["Destination"].FirstOrDefault(),
                Request.Query["X-PH-To"].FirstOrDefault());

            if (string.IsNullOrWhiteSpace(destination))
            {
                return Content("<?xml version=\"1.0\" encoding=\"UTF-8\"?><Response><Hangup/></Response>", "application/xml");
            }

            var callerId = FirstNonEmpty(_configuration["Plivo:FromNumber"], _configuration["Plivo:CallerNumber"]);
            var callbackUrl = $"{Request.Scheme}://{Request.Host}/PlivoVoice/RecordingCallback";
            var callerAttr = string.IsNullOrWhiteSpace(callerId)
                ? string.Empty
                : $" callerId=\"{WebUtility.HtmlEncode(callerId)}\"";

            var xml =
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<Response>" +
                $"<Dial{callerAttr} record=\"true\" recordingCallback=\"{WebUtility.HtmlEncode(callbackUrl)}\" recordingCallbackMethod=\"POST\">" +
                $"<Number>{WebUtility.HtmlEncode(destination.Trim())}</Number>" +
                "</Dial>" +
                "</Response>";

            return Content(xml, "application/xml");
        }

        [HttpGet]
        [HttpPost]
        public IActionResult RecordingCallback()
        {
            return Ok();
        }

        private static string FirstNonEmpty(params string[] values)
        {
            return values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
        }
    }
}
