using Google.Apis.Calendar.v3.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Risebird.Business;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Twilio.AspNet.Core;
using Twilio.TwiML;
using Twilio.TwiML.Voice;
using Task = System.Threading.Tasks.Task;

namespace Risebird.Web.Controllers
{
    public class TwilioVoiceController : TwilioController
    {
        private readonly IIRFService _irfService;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IMeetingService _meetingService;

        public TwilioVoiceController(IIRFService irfService, IApplicationErrorLogger applicationErrorLogger,
            IMeetingService meetingService)
        {
            _irfService = irfService;
            _applicationErrorLogger = applicationErrorLogger;
            _meetingService = meetingService;
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
        public async Task<TwiMLResult> IVRResponse(string recipientName, string uniqueId, int irfId, string jdTitle, int retryCount)
        {
            var response = new VoiceResponse();
            try
            {
                if (!string.IsNullOrEmpty(recipientName))
                {
                    HttpContext.Session.SetString("recipientName", recipientName);
                }
                else
                {
                    HttpContext.Session.SetString("recipientName", "interviewer");
                }
                if (!string.IsNullOrEmpty(jdTitle))
                {
                    HttpContext.Session.SetString("jdTitle", jdTitle);
                }
                else
                {
                    HttpContext.Session.SetString("jdTitle", "system");
                }
                if (!string.IsNullOrEmpty(uniqueId))
                {
                    HttpContext.Session.SetString("uniqueId", uniqueId);
                }
                if (irfId > 0)
                {
                    HttpContext.Session.SetString("irfId", irfId.ToString());
                }
                var link = "https://portal.risebird.io/TwilioVoice/handlekeyInterviewerAvailability";
                var actionUri = new Uri(link);
                var gather = new Gather(action: actionUri, numDigits: 1, timeout: 5);//new Uri(Url.Action("Show", "Menu"))
                //var gather = new Gather(action: new Uri(Url.Action("handlekeyInterviewerAvailability", "TwilioVoice")), numDigits: 1, timeout : 5);//new Uri(Url.Action("Show", "Menu"))
                gather.Say($"Dear {recipientName}, greetings from Risebird. This call is to confirm your availability for taking an interview based on {jdTitle} over the next two days. Press 1 if Available. Press 2 if Not Available. Press 3 to Drop your profile.");
                response.Append(gather);

                if (retryCount < 2)
                {
                    response.Say("You did not press any key. Let me repeat the options.");
                    response.Redirect(new Uri($"https://portal.risebird.io/IvrInterviewerAvailability?recipientName={recipientName}&uniqueId={uniqueId}&irfId={irfId}&jdTitle={jdTitle}&retryCount={retryCount + 1}"));
                }
                else
                {
                    response.Say("No input received. Goodbye.");
                    response.Hangup();
                }

                return TwiML(response);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                response.Say($"Error in Ivr Interviewer Availability with the message: {ex.Message} on line number {ex.StackTrace}, and source {ex.Source}");
                return TwiML(response);
            }
        }

        [HttpPost]
        public IActionResult handlekeyInterviewerAvailability(string digits)
        {

            try
            {
                string recName = "interviewer";
                string unqId = string.Empty;
                int irfintId = 0;
                string jdTitleVar = string.Empty;
                if (HttpContext.Session.GetString("recipientName") != null)
                {
                    recName = HttpContext.Session.GetString("recipientName");

                }
                if (HttpContext.Session.GetString("uniqueId") != null)
                {
                    unqId = HttpContext.Session.GetString("uniqueId");

                }
                if (HttpContext.Session.GetString("irfId") != null)
                {
                    irfintId = Convert.ToInt16(HttpContext.Session.GetString("irfId"));

                }
                if (HttpContext.Session.GetString("jdTitle") != null)
                {
                    jdTitleVar = HttpContext.Session.GetString("jdTitle");

                }
                var selectedOption = digits;
                if(selectedOption == "1")
                {
                    return UpdateInterviewerAvailbilityStatusForIRF(1).Result;
                }
                else if (selectedOption == "2")
                {
                    return UpdateInterviewerAvailbilityStatusForIRF(2).Result;
                }
                else if (selectedOption == "3")
                {
                    return UpdateInterviewerAvailbilityStatusForIRF(4).Result;
                }
                return RedirectToAction("IvrInterviewerAvailability", new { recipientName = recName, uniqueId = unqId, irfId = irfintId, jdTitle = jdTitleVar });
            //    var optionActions = new Dictionary<string, Func<TwiMLResult>>()
            //{
            //    {"1", UpdateInterviewerAvailbilityStatusForIRF},
            //    {"2", UpdateInterviewerAvailbilityStatusForIRF},
            //    {"3", UpdateInterviewerAvailbilityStatusForIRF}
            //};

                //return optionActions.ContainsKey(selectedOption) ?
                //    optionActions[selectedOption]() :
                //    RedirectToAction("IvrInterviewerAvailability", new { recipientName = recName, uniqueId = unqId, irfId = irfintId, jdTitle = jdTitleVar });
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                var response = new VoiceResponse();
                response.Say($"Error in handle key Interviewer Availability with the message: {ex.Message} on line number {ex.StackTrace}, and source {ex.Source}");
                return TwiML(response);
            }
        }

        private TwiMLResult ReturnInstructions()
        {
            var response = new VoiceResponse();
            response.Say("You chose instructions",
                         voice: Say.VoiceEnum.PollyAmy, language: "en-GB");

            response.Say("Thank you for calling the E.T. Phone Home Service - the " +
                         "adventurous alien's first choice in intergalactic travel. Good bye.");

            response.Hangup();

            return TwiML(response);
        }

        private TwiMLResult Planets()
        {
            var response = new VoiceResponse();
            //var gather = new Gather(action: Url.ActionUri("Interconnect", "PhoneExchange"), numDigits: 1);
            //gather.Say("To call the planet Broh doe As O G, press 2. To call the planet " +
            //         "DuhGo bah, press 3. To call an oober asteroid to your location, press 4. To " +
            //         "go back to the main menu, press the star key ",
            //         voice: Say.VoiceEnum.PollyAmy, language: "en-GB", loop: 3);
            //response.Append(gather);
            response.Say("You chose Planets",
                         voice: Say.VoiceEnum.PollyAmy, language: "en-GB");

            return TwiML(response);
        }
        private async Task<TwiMLResult> UpdateInterviewerAvailbilityStatusForIRF(int status)
        {
            //check link expiry befire hitting db
            try
            {
                var response = new VoiceResponse();
                if(status == 1)
                {
                    response.Say("You chose Available",
                             voice: Say.VoiceEnum.PollyAmy, language: "en-GB");
                }
                else if (status == 2)
                {
                    response.Say("You chose Not Available",
                             voice: Say.VoiceEnum.PollyAmy, language: "en-GB");
                }
                else if (status == 4)
                {
                    response.Say("You chose Drop My Profile",
                             voice: Say.VoiceEnum.PollyAmy, language: "en-GB");
                }

                string recName = "interviewer";
                string unqId = string.Empty;
                int irfintId = 0;
                if (HttpContext.Session.GetString("recipientName") != null)
                {
                    recName = HttpContext.Session.GetString("recipientName");

                }
                if (HttpContext.Session.GetString("uniqueId") != null)
                {
                    unqId = HttpContext.Session.GetString("uniqueId");

                }
                if (HttpContext.Session.GetString("irfId") != null)
                {
                    irfintId = Convert.ToInt16(HttpContext.Session.GetString("irfId"));

                }
                
                var result = await _meetingService.UpdateInterviewerAvailbilityStatusForIRF(status, unqId, irfintId, "ivr");
                //if (result.Item1)
                //{


                    
                //}
                //else
                //{
                //    return Json("Failed to update status, Please try again");
                //}

                response.Say("Thank you for submitting your response. Good bye.");

                response.Hangup();

                return TwiML(response);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                var response = new VoiceResponse();
                response.Say($"Error in Update Interviewer Availbility Status For IRF with the message: {ex.Message} on line number {ex.StackTrace}, and source {ex.Source}");
                return TwiML(response);
            }

        }

    }
}
