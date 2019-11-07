using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using Risebird.CommonHelper;
using System.Data;
using Microsoft.AspNetCore.Authorization;
using OfficeOpenXml;
using Newtonsoft.Json;
//using Twilio.AspNetCore
using Twilio.TwiML;
using Twilio.AspNet.Core;
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
        public IActionResult Index()
        {
            return View();
        }
        // GET: TwilioCalls/Generate
        public JsonResult Generate(string page)
        {
            var token = GenerateToken();
            //return Json;
            return Json(new {token});
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
    }


}