using Microsoft.AspNetCore.Mvc;
using Risebird.Business.Interface;
using System;
using System.Threading.Tasks;
using Twilio.AspNet.Core;
using Twilio.TwiML;

namespace Risebird.Web.Controllers
{
    public class TwilioVoiceController : TwilioController
    {
        private readonly IIRFService _irfService;

        public TwilioVoiceController(IIRFService irfService)
        {
            _irfService = irfService;
        }

        [HttpPost]
        public async Task<TwiMLResult> Index(string irfId)
        {
            var irfDetails = await _irfService.GetIRFForCall(Convert.ToInt32(irfId));
            var response = new VoiceResponse();
            var client = irfDetails.ClientCompName;
            var primarySkill = irfDetails.JDTiltle.Split("|")[0];
            var message = string.Format("Dear Candidate! Greetings from RiseBird. This is a gentle reminder on behalf of {0} for you to schedule your interview for {1}.",
                         client, primarySkill);
            response.Say(message);
            return TwiML(response);
        }
        [HttpPost]
        public TwiMLResult CandidateReminderCall(string name, string interviewdate)
        {
            var response = new VoiceResponse();
            var message = string.Format("Dear {0}! Greetings from RiseBird. This is to remind you that your interview is scheduled at {1}. Please confirm your availability by responding to the mail.",
                name, interviewdate);
            response.Say(message);
            return TwiML(response);
        }
    }
}
