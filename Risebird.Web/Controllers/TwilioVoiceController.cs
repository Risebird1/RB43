using Google.Apis.Calendar.v3.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Risebird.Business;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
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
        private readonly IUserRepository _userRepository;
        private readonly IActivityTrackerService _activityTrackerService;


        public TwilioVoiceController(IIRFService irfService,
            IApplicationErrorLogger applicationErrorLogger,
            IMeetingService meetingService, IUserRepository userRepository, IActivityTrackerService activityTrackerService)
        {
            _irfService = irfService;
            _applicationErrorLogger = applicationErrorLogger;
            _meetingService = meetingService;
            _userRepository = userRepository;
            _activityTrackerService = activityTrackerService;
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

        [HttpPost]
        public TwiMLResult IvrInterviewerAvailability(string uniqueId, string irfId, string jdTitle, string assessmentGroupId, int retryCount = 0)
        {
            //var response = new VoiceResponse();
            //var message = string.Format("Dear {0}! Empty function worked. Greetings from RiseBird. This is to remind you that your interview is scheduled at {1}. Please confirm your availability by responding to the mail.",
            //    recipientName, interviewdate);
            //response.Say(message);
            //return TwiML(response);
            var debugResult = _activityTrackerService.DebugLogger($"TwilioVoice/IvrInterviewerAvailability function invked succesfully", "ivr", string.Empty).Result;

            var response = new VoiceResponse();
            try
            {

                var link = $"https://portal.risebird.io/TwilioVoice/Show?uniqueId={uniqueId}&irfId={irfId}&assessmentGroupId={assessmentGroupId}&jdTitle={jdTitle}";
                var actionUri = new Uri(link);
                var gather = new Gather(action: actionUri, numDigits: 1, timeout: 5);

                if (Convert.ToInt16(irfId) > 0)
                {
                    gather.Say($"Dear Interviewer, greetings from Risebird. This call is to confirm your availability for taking an interview based on {jdTitle} over the next two days. Press 1 if Available. Press 2 if Not Available. Press 3 to Drop your profile.");
                }
                else
                {
                    gather.Say($"Dear Interviewer, greetings from Risebird. This call is to confirm your availability for taking an interview based on {jdTitle} over the next two days. Press 1 if Available. Press 2 if Not Available.");

                }
                response.Append(gather);

                if (retryCount < 2)
                {
                    response.Say("You did not press any key. Let me repeat the options.");
                    response.Redirect(new Uri($"https://portal.risebird.io/TwilioVoice/IvrInterviewerAvailability?uniqueId={uniqueId}&irfId={irfId}&assessmentGroupId={assessmentGroupId}&jdTitle={jdTitle}&retryCount={retryCount + 1}"));
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
                var resultDe = _activityTrackerService.DebugLogger($"TwilioVoice/IvrInterviewerAvailability function error : {ex.Message}, {ex.StackTrace}", "ivr", string.Empty).Result;

                //response.Say($"Error in Ivr Interviewer Availability with the message: {ex.Message} on line number {ex.StackTrace}, and source {ex.Source}");
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


        [HttpPost]
        public IActionResult Show([FromForm] string digits, [FromQuery] string uniqueId, [FromQuery] string irfId, [FromQuery] string jdTitle, [FromQuery] string assessmentGroupId)
        {
            try
            {
                var debugResult = _activityTrackerService.DebugLogger($"TwilioVoice/Show function invked succesfully", "ivr", string.Empty).Result;

                var irfintId = Convert.ToInt16(irfId);
                var agIdInt = Convert.ToInt16(assessmentGroupId);
                var selectedOption = digits;
                if (selectedOption == "1")
                {
                    if (irfintId > 0)
                    {
                        return UpdateInterviewerAvailbilityStatusForIRF(1, uniqueId, irfintId);
                    }
                    else
                    {
                        return UpdateInterviewerAvailbilityStatusForAssessmentGroup(1, uniqueId, agIdInt);
                    }
                }
                else if (selectedOption == "2")
                {
                    if (irfintId > 0)
                    {
                        return UpdateInterviewerAvailbilityStatusForIRF(2, uniqueId, irfintId);
                    }
                    else
                    {
                        return UpdateInterviewerAvailbilityStatusForAssessmentGroup(2, uniqueId, agIdInt);
                    }
                }
                else if (selectedOption == "3")
                {
                    if (irfintId > 0)
                    {
                        return UpdateInterviewerAvailbilityStatusForIRF(4, uniqueId, irfintId);
                    }

                }

                return RedirectToAction("IvrInterviewerAvailability", new { irfId = irfId, jdTitle = jdTitle, assessmentGroupId = assessmentGroupId, uniqueId = uniqueId });


                //    var optionActions = new Dictionary<string, Func<TwiMLResult>>()
                //{
                //    {"1", ReturnInstructions},
                //    {"2", Planets}
                //};

                //    return optionActions.ContainsKey(selectedOption) ?
                //        optionActions[selectedOption]() :
                //        RedirectToAction("IvrInterviewerAvailabilityTesting", new { uniqueId = uniqueId, irfId = irfId, jdTitle = jdTitle, assessmentGroupId= assessmentGroupId });
            }
            catch (Exception ex)
            {
                var debugResult = _activityTrackerService.DebugLogger($"TwilioVoice/Show function error : {ex.Message}, {ex.StackTrace}", "ivr", string.Empty).Result;

                //_applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                var response = new VoiceResponse();
                //response.Say($"Error in Show Menu with the message: {ex.Message} on line number {ex.StackTrace}, and source {ex.Source}");
                return TwiML(response);
            }
        }

        private TwiMLResult UpdateInterviewerAvailbilityStatusForIRF(int status, string unqId, int irfintId)
        {
            //check link expiry befire hitting db
            try
            {
                var debugResult = _activityTrackerService.DebugLogger($"TwilioVoice/UpdateInterviewerAvailbilityStatusForIRF function invked succesfully", "ivr", string.Empty).Result;

                var response = new VoiceResponse();
                if (status == 1)
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
                //response.Say($"the irfId is {irfintId} and {unqId}",
                //voice: Say.VoiceEnum.PollyAmy, language: "en-GB");


                var result = _meetingService.UpdateInterviewerAvailbilityStatusForIRF(status, unqId, irfintId, "ivr").Result;

                //response.Say($"result is {result.Item1} and mailId is {result.Item2}");

                response.Say($"result is {result.Item1} and mailId is {result.Item2}");

                response.Say("Thank you for submitting your response. Good bye.");

                response.Hangup();

                return TwiML(response);
            }
            catch (Exception ex)
            {
                var debugResult = _activityTrackerService.DebugLogger($"TwilioVoice/UpdateInterviewerAvailbilityStatusForIRF function error : {ex.Message}, {ex.StackTrace}", "ivr", string.Empty).Result;

                //await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                var response = new VoiceResponse();
                //response.Say($"Error in Update Interviewer Availbility Status For IRF with the message: {ex.Message} on line number {ex.StackTrace}, and source {ex.Source}");
                return TwiML(response);
            }

        }

        private TwiMLResult UpdateInterviewerAvailbilityStatusForAssessmentGroup(int status, string unqId, int assessmentGroupId)
        {
            try
            {
                var debugResult = _activityTrackerService.DebugLogger($"TwilioVoice/UpdateInterviewerAvailbilityStatusForAssessmentGroup function invked succesfully", "ivr", string.Empty).Result;

                var response = new VoiceResponse();
                if (status == 1)
                {
                    response.Say("You chose Available",
                             voice: Say.VoiceEnum.PollyAmy, language: "en-GB");
                }
                else if (status == 2)
                {
                    response.Say("You chose Not Available",
                             voice: Say.VoiceEnum.PollyAmy, language: "en-GB");
                }
                //response.Say($"the assessmentgroupid is {assessmentGroupId} and {unqId}",
                //voice: Say.VoiceEnum.PollyAmy, language: "en-GB");
                //else if (status == 4)
                //{
                //    response.Say("You chose Drop My Profile",
                //             voice: Say.VoiceEnum.PollyAmy, language: "en-GB");
                //}

                var result = _meetingService.UpdateInterviewerAvailbilityStatusForAssessmentGroup(status == 1 ? true : false, unqId, assessmentGroupId, "ivr").Result;

                //response.Say($"result is {result}");
                response.Say("Thank you for submitting your response. Good bye.");

                response.Hangup();

                return TwiML(response);
            }
            catch (Exception ex)
            {
                var debugResult = _activityTrackerService.DebugLogger($"TwilioVoice/UpdateInterviewerAvailbilityStatusForAssessmentGroup function error : {ex.Message}, {ex.StackTrace}", "ivr", string.Empty).Result;

                //await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                var response = new VoiceResponse();
                //response.Say($"Error in Update Interviewer Availbility Status For AssessmentGroup with the message: {ex.Message} on line number {ex.StackTrace}, and source {ex.Source}");
                return TwiML(response);
            }

        }

    }
}
