using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Headers;
using System.Threading.Tasks;
//using Twilio.AspNetCore
using Twilio.Jwt;
using Twilio.Jwt.AccessToken;
using Twilio.Jwt.Client;

namespace Risebird.Web.Controllers
{
    public class TwilioCallsController : Controller
    {
        private IAzureStorageHelper _azureStorageHelper;
        private readonly IFileMimeTypeHelper _fileMimeTypeHelper;
        private readonly IExotelService _exotelService;
        private readonly ExotelWebRtcSettings _exotelWebRtcSettings;
        private readonly IUserRepository _userRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        public TwilioCallsController(IAzureStorageHelper azureStorageHelper, IFileMimeTypeHelper fileMimeTypeHelper,
            IExotelService exotelService, IOptions<ExotelWebRtcSettings> exotelWebRtcSettings,
            IUserRepository userRepository, IApplicationErrorLogger applicationErrorLogger)
        {
            _azureStorageHelper = azureStorageHelper;
            _fileMimeTypeHelper = fileMimeTypeHelper;
            _exotelService = exotelService;
            _exotelWebRtcSettings = exotelWebRtcSettings.Value;
            _userRepository = userRepository;
            _applicationErrorLogger = applicationErrorLogger;
        }

        // POST: TwilioCalls/ExotelToken — SIP account for the signed-in agent's Exotel softphone (ExotelCallMaker.js).
        // Returns only this agent's own subscriber; the Exotel API Key/Token never reach the browser.
        // { sip: null } means no softphone for this agent — the modal then uses click-to-call (agent's phone).
        [HttpPost]
        public IActionResult ExotelToken()
        {
            var email = HttpContext.Session.GetString("email")?.Trim().Replace("\"", "");
            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized();
            }

            var subscriber = _exotelWebRtcSettings.IsConfigured ? _exotelWebRtcSettings.FindSubscriber(email) : null;
            if (subscriber == null)
            {
                return Json(new { sip = (object)null });
            }

            var wssHost = _exotelWebRtcSettings.WssHost.Trim();
            var colon = wssHost.LastIndexOf(':');
            var port = colon > 0 ? wssHost.Substring(colon + 1) : "443";

            return Json(new
            {
                sip = new
                {
                    userName = subscriber.UserName.Trim(),
                    secret = subscriber.Password,
                    sipDomain = _exotelWebRtcSettings.SipDomain.Trim(),
                    domain = colon > 0 ? wssHost : wssHost + ":" + port,
                    port,
                    displayName = HttpContext.Session.GetString("profileName") ?? subscriber.UserName.Trim()
                }
            });
        }

        // POST: TwilioCalls/ExotelStatus — Exotel StatusCallback (StatusCallbackEvents=terminal) for calls placed by
        // User/ExotelConnect. The payload is not trusted: the call is re-read from the Exotel API by Sid and its
        // recording URL / duration stored on the CallRecordingData row. Recordings that are not ready yet are picked
        // up by ExotelCallRecordingBackfillJob. Always returns 200 to Exotel.
        [HttpPost]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ExotelStatus()
        {
            try
            {
                string callSid = null;
                if (Request.HasFormContentType)
                {
                    callSid = Request.Form["CallSid"].ToString();
                }
                else if (Request.ContentType != null && Request.ContentType.Contains("json", StringComparison.OrdinalIgnoreCase))
                {
                    using (var reader = new StreamReader(Request.Body))
                    {
                        var body = JObject.Parse(await reader.ReadToEndAsync());
                        callSid = body["CallSid"]?.ToString();
                    }
                }
                if (string.IsNullOrWhiteSpace(callSid))
                {
                    callSid = Request.Query["CallSid"].ToString();
                }
                if (string.IsNullOrWhiteSpace(callSid))
                {
                    return Ok();
                }

                var details = await _exotelService.GetCallDetailsAsync(callSid);
                if (!string.IsNullOrWhiteSpace(details?.RecordingUrl))
                {
                    // Keyed on CallRecordingData.CallSid — provider-agnostic despite the name.
                    await _userRepository.UpdatePlivoRecordingAsync(callSid.Trim(), details.RecordingUrl, details.DurationSeconds);
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return Ok();
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
        // GET: TwilioCalls/Generate
        //[HttpPost]
        //public JsonResult Generate(string page)
        //{
        //    var token = GenerateToken();
        //    //return Json;
        //    return Json(new { token });
        //}


        //public string GenerateToken()
        //{
        //    var scopes = new HashSet<IScope>
        //    {
        //        new IncomingClientScope("support_agent"),
        //        new OutgoingClientScope("APb82708865faae9acaea7590423e9c965")
        //    };
        //    var capability = new ClientCapability("ACcad9f6682d4a76df9af7cde78dd0867e",
        //                                          "3f15940e73243adb8e75f9689adaa0e2",
        //                                          DateTime.Now.AddSeconds(300),
        //                                          scopes: scopes);

        //    return capability.ToJwt();
        //}

        public string GenerateToken()
        {
            // Twilio credentials
            var accountSid = "ACcad9f6682d4a76df9af7cde78dd0867e";
            var apiKeySid = "SKcd6324df7e715e757fe15b9d2742b867";
            var apiKeySecret = "r6X3tuvGDMk71Wzcq2x2UR9Y6cG3EXmN";

            // Identity of this client
            var identity = "support_agent";

            // Voice grant
            var voiceGrant = new VoiceGrant
            {
                OutgoingApplicationSid = "APb82708865faae9acaea7590423e9c965",
                IncomingAllow = true // allow incoming calls
            };

            var token = new Token(
                accountSid,
                apiKeySid,
                apiKeySecret,
                identity: identity,
                grants: new HashSet<IGrant> { voiceGrant }
            );

            

            return token.ToJwt();
        }

        [HttpPost]
        public async Task<bool> UploadAudio()
        {
            var file = Request.Form.Files;
            var filename = ContentDispositionHeaderValue.Parse(file[0].ContentDisposition).FileName.Trim('"');

            if (file != null)
            {
                var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(file[0]);
                if (fileMimeType == "audio/basic" || fileMimeType == "audio/mpeg" || fileMimeType == "audio/x-wav" || fileMimeType == "video/x-msvideo")
                {
                    await _azureStorageHelper.InsertBlob(filename, "audios", file[0]);
                }
            }

            return true;
        }
    }


}