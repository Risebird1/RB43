using Microsoft.AspNetCore.Mvc;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using System;
using System.Threading.Tasks;
using Twilio.AspNet.Core;
using Twilio.TwiML;
using Twilio.TwiML.Voice;

namespace Risebird.Web.Controllers
{
    public class TwilioVoiceController : TwilioController
    {
        private readonly IIRFService _irfService;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public TwilioVoiceController(IIRFService irfService, IApplicationErrorLogger applicationErrorLogger)
        {
            _irfService = irfService;
            _applicationErrorLogger = applicationErrorLogger;
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

        [HttpPost("IvrInterviewerAvailability")]
        public async Task<IActionResult> IVRResponse(string recipientName, string interviewDate, [FromQuery] int retryCount)
        {
            try
            {
                //var link = $"https://localhost:44329/TwilioVoice/handlekeyInterviewerAvailability?recipientName={recipientName}&interviewDate={interviewDate}";
                var link = $"https://portal.risebird.io/TwilioVoice/handlekeyInterviewerAvailability?recipientName={recipientName}&interviewDate={interviewDate}";
                var actionUri = new Uri(link);
                var response = new VoiceResponse();
                //var gather = new Gather(numDigits: 1, action: actionUri, method: "POST");
                var gather = new Gather(
                    numDigits: 1,
                    timeout: 5,
                    action: actionUri,
                    method: "POST"
                );

                gather.Say("Dear Interviewer, greetings from Risebird. This call is to confirm your availability for taking an interview over the next two days. Press 1 if Yes. Press 2 if Not Available. Press 3 to Drop your profile.");
                response.Append(gather);

                // If the user did not press a key and retryCount is less than 2, retry
                if (retryCount < 2)
                {
                    response.Say("You did not press any key. Let me repeat the options.");
                    response.Redirect(new Uri($"https://portal.risebird.io/IvrInterviewerAvailability?recipientName={recipientName}&interviewDate={interviewDate}&retryCount={retryCount + 1}"));
                }
                else
                {
                    response.Say("No input received. Goodbye.");
                    response.Hangup();
                }

                return Content(response.ToString(), "application/xml");
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [HttpPost("handlekeyInterviewerAvailability")]
        public async Task<IActionResult> HandlekeyInterviewerAvailability([FromForm] string Digits, [FromQuery] string recipientName = null, [FromQuery] string interviewDate = null)
        {
            try
            {
                var response = new VoiceResponse();
                //var redirectLink = string.Format("https://localhost:44329/TwilioVoice/IvrInterviewerAvailability?name={0}&interviewdate={1}", recipientName, interviewDate);
                var redirectLink = string.Format("https://portal.risebird.io/TwilioVoice/IvrInterviewerAvailability?name={0}&interviewdate={1}", recipientName, interviewDate);
                var redirectUrl = new Uri(redirectLink);
                switch (Digits)
                {
                    case "1":
                        response.Say("You have selected sales. We will process your request.");
                        //ProcessSales(); // Call the method that handles sales
                        break;
                    case "2":
                        response.Say("You have selected support. We will process your request.");
                        //ProcessSupport(); // Call the method that handles support
                        break;
                    case "3":
                        response.Say("You have selected to speak with an agent. We will process your request.");
                        //ProcessAgent(); // Call the method that handles connecting to an agent
                        break;
                    default:
                        response.Say("Invalid input. Please try again.");
                        response.Redirect(redirectUrl); // Redirect back to the IVR menu
                        return Content(response.ToString(), "application/xml");
                }

                // End the call after the message is played
                response.Pause(length: 2); // Optional pause for better experience
                response.Hangup();

                return Content(response.ToString(), "application/xml");
            }
            catch (Exception ex)
            {

                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
    }
}
