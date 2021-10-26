using Microsoft.AspNetCore.Mvc;
using Risebird.CommonHelper.Interface;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Threading.Tasks;
//using Twilio.AspNetCore
using Twilio.Jwt;
using Twilio.Jwt.Client;

namespace Risebird.Web.Controllers
{
    public class TwilioCallsController : Controller
    {
        //public TwiMLResult Index()
        //{
        //    var response = new VoiceResponse();
        //    response.Say("Hello World");
        //    return TwiML(response);

        //}
        private IAzureStorageHelper _azureStorageHelper;
        public TwilioCallsController(IAzureStorageHelper azureStorageHelper)
        {
            _azureStorageHelper = azureStorageHelper;

        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
        // GET: TwilioCalls/Generate
        [HttpPost]
        public JsonResult Generate(string page)
        {
            var token = GenerateToken();
            //return Json;
            return Json(new { token });
        }

        
        public string GenerateToken()
        {
            var scopes = new HashSet<IScope>
            {
                new IncomingClientScope("support_agent"),
                new OutgoingClientScope("APb82708865faae9acaea7590423e9c965")
            };
            var capability = new ClientCapability("ACcad9f6682d4a76df9af7cde78dd0867e",
                                                  "3f15940e73243adb8e75f9689adaa0e2",
                                                  scopes: scopes);

            return capability.ToJwt();
        }

        [HttpPost]
        public async Task<bool> UploadAudio()
        {
            var file = Request.Form.Files;

            var filename = ContentDispositionHeaderValue.Parse(file[0].ContentDisposition).FileName.Trim('"');

            if (file != null)
            {
                await _azureStorageHelper.InsertBlob(filename, "audios", file[0]);

            }

            return true;
        }
    }


}