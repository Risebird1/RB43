using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Twilio.AspNet.Core;
using Twilio.TwiML;

namespace Risebird.Web.Controllers
{
    public class TwilioVoiceController : TwilioController
    {
        //public TwiMLResult Index(string irfName, string client, string primarySkill)
        //{
        //    var response = new VoiceResponse();
        //    //var message = $"Dear Candidate! Greetings from RiseBird. This is a gentle reminder for your upcoming video based interview meeting scheduled. Kindly be available on time.";
        //    var message = string.Format("Dear Candidate! Greetings from RiseBird. This is a gentle reminder on behalf of {0} for you to schedule you interview for {1}.",
        //                 client, primarySkill);
        //    response.Say(message);
        //    return TwiML(response);
        //}
        [HttpPost]
        public TwiMLResult Index(string irfId)
        {
            var response = new VoiceResponse();
            var client = "Emphasis";
            var primarySkill = "python";
            //var message = $"Dear Candidate! Greetings from RiseBird. This is a gentle reminder for your upcoming video based interview meeting scheduled. Kindly be available on time.";
            var message = string.Format("Dear Candidate! Greetings from RiseBird. This is a gentle reminder on behalf of {0} for you to schedule you interview for {1}.",
                         client, primarySkill);
            response.Say(message);
            return TwiML(response);
        }
    }
}
