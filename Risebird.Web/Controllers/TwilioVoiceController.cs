using Microsoft.AspNetCore.Mvc;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Repository.Interface;
using System;
using System.Threading.Tasks;
using System.Web;
using Twilio.AspNet.Core;
using Twilio.TwiML;
using Twilio.TwiML.Voice;

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
        public async Task<TwiMLResult> IvrInterviewerAvailability(string uniqueId, string irfId, string jdTitle, string assessmentGroupId, int retryCount = 0)
        {
            //var response = new VoiceResponse();
            //var message = string.Format("Dear {0}! Empty function worked. Greetings from RiseBird. This is to remind you that your interview is scheduled at {1}. Please confirm your availability by responding to the mail.",
            //    recipientName, interviewdate);
            //response.Say(message);
            //return TwiML(response);
            var debugResult = await _activityTrackerService.DebugLogger($"TwilioVoice/IvrInterviewerAvailability function invked succesfully", "ivr", string.Empty);

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
                var resultDe = await _activityTrackerService.DebugLogger($"TwilioVoice/IvrInterviewerAvailability function error : {ex.Message}, {ex.StackTrace}", "ivr", string.Empty);

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
        public async Task<IActionResult> Show([FromForm] string digits, [FromQuery] string uniqueId, [FromQuery] string irfId, [FromQuery] string jdTitle, [FromQuery] string assessmentGroupId)
        {
            try
            {
                var debugResult = await _activityTrackerService.DebugLogger($"TwilioVoice/Show function invked succesfully", "ivr", string.Empty);

                var irfintId = Convert.ToInt16(irfId);
                var agIdInt = Convert.ToInt16(assessmentGroupId);
                var selectedOption = digits;
                if (selectedOption == "1")
                {
                    if (irfintId > 0)
                    {
                        return await UpdateInterviewerAvailbilityStatusForIRF(1, uniqueId, irfintId);
                    }
                    else
                    {
                        return await UpdateInterviewerAvailbilityStatusForAssessmentGroup(1, uniqueId, agIdInt);
                    }
                }
                else if (selectedOption == "2")
                {
                    if (irfintId > 0)
                    {
                        return await UpdateInterviewerAvailbilityStatusForIRF(2, uniqueId, irfintId);
                    }
                    else
                    {
                        return await UpdateInterviewerAvailbilityStatusForAssessmentGroup(2, uniqueId, agIdInt);
                    }
                }
                else if (selectedOption == "3")
                {
                    if (irfintId > 0)
                    {
                        return await UpdateInterviewerAvailbilityStatusForIRF(4, uniqueId, irfintId);
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
                var debugResult = await _activityTrackerService.DebugLogger($"TwilioVoice/Show function error : {ex.Message}, {ex.StackTrace}", "ivr", string.Empty);

                //_applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                var response = new VoiceResponse();
                //response.Say($"Error in Show Menu with the message: {ex.Message} on line number {ex.StackTrace}, and source {ex.Source}");
                return TwiML(response);
            }
        }


        private async Task<TwiMLResult> UpdateInterviewerAvailbilityStatusForIRF(int status, string unqId, int irfintId)
        {
            //check link expiry befire hitting db
            try
            {
                var debugResult = await _activityTrackerService.DebugLogger($"TwilioVoice/UpdateInterviewerAvailbilityStatusForIRF function invked succesfully", "ivr", string.Empty);

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


                var result = await _meetingService.UpdateInterviewerAvailbilityStatusForIRF(status, unqId, irfintId, "ivr");

                //response.Say($"result is {result.Item1} and mailId is {result.Item2}");

                response.Say($"result is {result.Item1} and mailId is {result.Item2}");

                response.Say("Thank you for submitting your response. Good bye.");

                response.Hangup();

                return TwiML(response);
            }
            catch (Exception ex)
            {
                var debugResult = await _activityTrackerService.DebugLogger($"TwilioVoice/UpdateInterviewerAvailbilityStatusForIRF function error : {ex.Message}, {ex.StackTrace}", "ivr", string.Empty);

                //await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                var response = new VoiceResponse();
                //response.Say($"Error in Update Interviewer Availbility Status For IRF with the message: {ex.Message} on line number {ex.StackTrace}, and source {ex.Source}");
                return TwiML(response);
            }

        }

        private async Task<TwiMLResult> UpdateInterviewerAvailbilityStatusForAssessmentGroup(int status, string unqId, int assessmentGroupId)
        {
            //check link expiry befire hitting db
            try
            {
                var debugResult = await _activityTrackerService.DebugLogger($"TwilioVoice/UpdateInterviewerAvailbilityStatusForAssessmentGroup function invked succesfully", "ivr", string.Empty);

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

                var result = await _meetingService.UpdateInterviewerAvailbilityStatusForAssessmentGroup(status == 1 ? true : false, unqId, assessmentGroupId, "ivr");

                //response.Say($"result is {result}");
                response.Say("Thank you for submitting your response. Good bye.");

                response.Hangup();

                return TwiML(response);
            }
            catch (Exception ex)
            {
                var debugResult = await _activityTrackerService.DebugLogger($"TwilioVoice/UpdateInterviewerAvailbilityStatusForAssessmentGroup function error : {ex.Message}, {ex.StackTrace}", "ivr", string.Empty);

                //await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                var response = new VoiceResponse();
                //response.Say($"Error in Update Interviewer Availbility Status For AssessmentGroup with the message: {ex.Message} on line number {ex.StackTrace}, and source {ex.Source}");
                return TwiML(response);
            }

        }


        [HttpPost]
        public async Task<TwiMLResult> IvrCandidateInterviewAvailability(string uniqueId, int irfId,int interviewId,int candidateId, int retryCount = 0)
        {
            var debugResult = await _activityTrackerService.DebugLogger($"TwilioVoice/IvrCandidateInterviewAvailability function invked succesfully", "ivr", string.Empty);

            var candidateInterviewDetails = await _meetingService.GetCandidateInterviewDetails(uniqueId, irfId, interviewId, candidateId);
            var response = new VoiceResponse();
            try
            {
                //var link = $"https://portal.risebird.io/TwilioVoice/CandidateInterviewAvailabilityShow?uniqueId={uniqueId}&irfId={irfId}&jdTitle={jdTitle}&candidateFirstName={candidateFirstName}&interviewId={interviewId}&candidateId={candidateId}&interviewDate={interviewDate}&timeZoneValue={timeZoneValue}";

                //var link = $"https://portal.risebird.io/TwilioVoice/CandidateInterviewAvailabilityShow?uniqueId={uniqueId}&irfId={irfId}&jdTitle={jdTitle}&jdTitle={candidateFirstName}&interviewId={interviewId}&candidateId={candidateId}&interviewDate={interviewDate}&timeZoneValue={timeZoneValue}";
                var link = $"https://portal.risebird.io/TwilioVoice/CandidateInterviewAvailabilityShow?uniqueId={uniqueId}&irfId={irfId}&interviewId={interviewId}&candidateId={candidateId}";
                var actionUri = new Uri(link);
                var gather = new Gather(action: actionUri, numDigits: 1, timeout: 5);

                gather.Say($"Dear {candidateInterviewDetails.FirstName}, Greetings from Risebird.This is to remind you that your interview is scheduled on {candidateInterviewDetails.interviewDate} {candidateInterviewDetails.TimeZonesValue} for requirement of {candidateInterviewDetails.PrimarySkillInvite}. Kindly confirm your availability by clicking on the appropriate response Press 1 if Available. Press 2 if Not Available or Drop profile. Press 3 to Reschedule my interview.");
                response.Append(gather);

                if (retryCount < 2)
                {

                    response.Say("You did not press any key. Let me repeat the options.");
                    response.Redirect(new Uri($"https://portal.risebird.io/TwilioVoice/IvrCandidateInterviewAvailability?uniqueId={uniqueId}&irfId={irfId}&interviewId={interviewId}&candidateId={candidateId}&retryCount={retryCount + 1}"));
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
                var resultDe = await _activityTrackerService.DebugLogger($"TwilioVoice/IvrCandidateInterviewAvailability function error : {ex.Message}, {ex.StackTrace}", "ivr", string.Empty);
                return TwiML(response);
            }
        }
        [HttpPost]
        public async Task<IActionResult> CandidateInterviewAvailabilityShow([FromForm] string digits, [FromQuery] string uniqueId, [FromQuery] int irfId, [FromQuery] int interviewId, [FromQuery] int candidateId)
        {
            try
            {
                var debugResult = await _activityTrackerService.DebugLogger($"TwilioVoice/CandidateInterviewAvailabilityShow function invked succesfully", "ivr", string.Empty);

                var selectedOption = digits;
                if (selectedOption == "1")
                {
                    return await UpdateCandidateInterviewAvailbilityStatus(1, uniqueId, irfId, interviewId, candidateId); 
                }
                else if (selectedOption == "2")
                {
                    return await UpdateCandidateInterviewAvailbilityStatus(2, uniqueId, irfId, interviewId, candidateId);
                }
                else if (selectedOption == "3")
                {
                    return await UpdateCandidateInterviewAvailbilityStatus(3, uniqueId, irfId, interviewId, candidateId);
                }

                return RedirectToAction("IvrCandidateInterviewAvailability", new { uniqueId = uniqueId, irfId = irfId, interviewId = interviewId, candidateId = candidateId });


            }
            catch (Exception ex)
            {
                var debugResult = await _activityTrackerService.DebugLogger($"TwilioVoice/CandidateInterviewAvailabilityShow function error : {ex.Message}, {ex.StackTrace}", "ivr", string.Empty);

                //_applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                var response = new VoiceResponse();
                //response.Say($"Error in Show Menu with the message: {ex.Message} on line number {ex.StackTrace}, and source {ex.Source}");
                return TwiML(response);
            }

        }

        private async Task<TwiMLResult> UpdateCandidateInterviewAvailbilityStatus(int status, string unqId, int irfintId, int meetingid,  int candidateId)
        {
            //check link expiry befire hitting db
            try
            {
                var debugResult = await _activityTrackerService.DebugLogger($"TwilioVoice/UpdateCandidateInterviewAvailbilityStatus function invked succesfully", "ivr", string.Empty);

                var response = new VoiceResponse();
                if (status == 1)
                {
                    response.Say("You have chosen Available",
                             voice: Say.VoiceEnum.PollyAmy, language: "en-GB");
                }
                else if (status == 2)
                {
                    response.Say("You have chosen Not Available or Drop profile",
                             voice: Say.VoiceEnum.PollyAmy, language: "en-GB");
                }
                else if (status == 3)
                {
                    response.Say("You have chosen Reschedule my interview",
                             voice: Say.VoiceEnum.PollyAmy, language: "en-GB");
                }
                //response.Say($"the irfId is {irfintId} and {unqId}",
                //voice: Say.VoiceEnum.PollyAmy, language: "en-GB");

                var message = await _meetingService.CandidateMeetingsStatus(meetingid, status, unqId, irfintId, candidateId,"");

                //response.Say($"result is {result.Item1} and mailId is {result.Item2}");

                response.Say(message);

                response.Say("Thank you for submitting your response. Good bye and have nice rest of the day.");

                response.Hangup();

                return TwiML(response);
            }
            catch (Exception ex)
            {
                var debugResult = await _activityTrackerService.DebugLogger($"TwilioVoice/UpdateCandidateInterviewAvailbilityStatus function error : {ex.Message}, {ex.StackTrace}", "ivr", string.Empty);
                var response = new VoiceResponse();               
                return TwiML(response);
            }

        }


        [HttpPost]
        public async Task<TwiMLResult> IvrInterviewerMeetingReminder(int interviewId, int retryCount = 0)
        {
            var debugResult = await _activityTrackerService.DebugLogger($"TwilioVoice/IvrInterviewerMeetingReminder function invked succesfully", "ivr", string.Empty);

            var interviewerInterviewDetails = await _meetingService.GetInterviewerInterviewDetails(interviewId);
            var response = new VoiceResponse();
            try
            {
                 var link = $"https://portal.risebird.io/TwilioVoice/InterviewerInterviewAvailabilityShow?emailId={interviewerInterviewDetails.interviewer}&interviewId={interviewId}";
                var actionUri = new Uri(link);
                var gather = new Gather(action: actionUri, numDigits: 1, timeout: 5);

                gather.Say($"Dear {interviewerInterviewDetails.FirstName}, Greetings from Risebird.This is to remind you that you have assinged interview at {interviewerInterviewDetails.interviewDate} {interviewerInterviewDetails.TimeZonesValue} for requirement of {interviewerInterviewDetails.PrimarySkillInvite}. Kindly confirm your availability by clicking on the appropriate response Press 1 if Available to take interview. Press 2 if Not Available to take interview.");
                response.Append(gather);

                if (retryCount < 2)
                {

                    response.Say("You did not press any key. Let me repeat the options.");
                    response.Redirect(new Uri($"https://portal.risebird.io/TwilioVoice/IvrInterviewerMeetingReminder?interviewId={interviewId}&retryCount={retryCount + 1}"));
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
                var resultDe = await _activityTrackerService.DebugLogger($"TwilioVoice/IvrInterviewerMeetingReminder function error : {ex.Message}, {ex.StackTrace}", "ivr", string.Empty);
                return TwiML(response);
            }
        }

        [HttpPost]
        public async Task<IActionResult> InterviewerInterviewAvailabilityShow([FromForm] string digits, [FromQuery] string emailId, [FromQuery] int interviewId)
        {
            try
            {
                var debugResult = await _activityTrackerService.DebugLogger($"TwilioVoice/InterviewerInterviewAvailabilityShow function invked succesfully", "ivr", string.Empty);

                var selectedOption = digits;
                if (selectedOption == "1")
                {
                    return await UpdateInterviewerInterviewAvailbilityStatus(1, emailId, interviewId);
                }
                else if (selectedOption == "2")
                {
                    return await UpdateInterviewerInterviewAvailbilityStatus(2, emailId, interviewId);
                }               

                return RedirectToAction("IvrInterviewerMeetingReminder", new { interviewId = interviewId});


            }
            catch (Exception ex)
            {
                var debugResult = await _activityTrackerService.DebugLogger($"TwilioVoice/InterviewerInterviewAvailabilityShow function error : {ex.Message}, {ex.StackTrace}", "ivr", string.Empty);

                //_applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                var response = new VoiceResponse();
                //response.Say($"Error in Show Menu with the message: {ex.Message} on line number {ex.StackTrace}, and source {ex.Source}");
                return TwiML(response);
            }

        }

        private async Task<TwiMLResult> UpdateInterviewerInterviewAvailbilityStatus(int status, string emailId, int id)
        {
            //check link expiry befire hitting db
            try
            {
                var debugResult = await _activityTrackerService.DebugLogger($"TwilioVoice/UpdateInterviewerInterviewAvailbilityStatus function invked succesfully", "ivr", string.Empty);
                string type = string.Empty;
                var response = new VoiceResponse();
                if (status == 1)
                {
                    response.Say("You have chosen that you will be available to take the interview.",
                             voice: Say.VoiceEnum.PollyAmy, language: "en-GB");
                    type = "Yes";
                }
                else if (status == 2)
                {
                    response.Say("You have chosen that you will not available to take this interview.",
                             voice: Say.VoiceEnum.PollyAmy, language: "en-GB");
                    type = "No";
                }
                
                
                _meetingService.ConfirmInterviewavailability(emailId, id, type);
            

                response.Say("Thank you for submitting your response. Good bye and have nice rest of the day.");

                response.Hangup();

                return TwiML(response);
            }
            catch (Exception ex)
            {
                var debugResult = await _activityTrackerService.DebugLogger($"TwilioVoice/UpdateInterviewerInterviewAvailbilityStatus function error : {ex.Message}, {ex.StackTrace}", "ivr", string.Empty);
                var response = new VoiceResponse();
                return TwiML(response);
            }

        }


    }
}
