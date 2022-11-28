using Microsoft.AspNetCore.Mvc;
using Twilio.AspNet.Core;
//using Twilio.AspNetCore
using Twilio.TwiML;
using Twilio.TwiML.Voice;

namespace Risebird.Web.Controllers
{
    public class PhoneCallController : TwilioController
    {
        // POST Call/Connect
        //[HttpPost]
        //public TwiMLResult Connect(string phoneNumber)
        //{
        //    var response = new VoiceResponse();

        //    var dial = new Dial(callerId: "+17274782239");
        //    if (phoneNumber != null)
        //    {
        //        dial.Number(phoneNumber);
        //    }
        //    else
        //    {
        //        dial.Client("support_agent");
        //    }
        //    response.Append(dial);

        //    return TwiML(response);
        //}
    }
}