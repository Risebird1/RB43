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
using Twilio.TwiML.Voice;

namespace Risebird.Web.Controllers
{
    public class PhoneCallController : TwilioController
    {
        // POST Call/Connect
        [HttpPost]
        public TwiMLResult Connect(string phoneNumber)
        {
            var response = new VoiceResponse();

            var dial = new Dial(callerId: "+17274782239");
            if (phoneNumber != null)
            {
                dial.Number(phoneNumber);
            }
            else
            {
                dial.Client("support_agent");
            }
            response.Append(dial);

            return TwiML(response);
        }
    }
}