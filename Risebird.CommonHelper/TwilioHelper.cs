using Microsoft.Extensions.Configuration;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using System;
using System.Threading.Tasks;
using System.Web;
using Twilio;
using Twilio.Clients;
using Twilio.Exceptions;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Risebird.CommonHelper
{
    public class TwilioHelper : ITwilioHelper
    {
        //private readonly IHostingEnvironment _hostingEnvironment;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IConfiguration _configuration;
        private readonly string _twilioSMSNumber = string.Empty;
        private readonly string _twilioWhatsappSender = string.Empty;
        private readonly string _twilioCallerNumber = string.Empty;
        private readonly string _twilioAuthToken = string.Empty;
        private readonly string _twilioAccountSID = string.Empty;
        private readonly TwilioRestClient _twilioClient;
        private readonly IActivityTrackerService _activityTrackerService;


        public TwilioHelper(IApplicationErrorLogger applicationErrorLogger, 
            IConfiguration configuration,
            IActivityTrackerService activityTrackerService)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _twilioSMSNumber = _configuration["TwilioSettings:SmsSENDER"];
            _twilioWhatsappSender = _configuration["TwilioSettings:WhatsappSENDER"];
            _twilioCallerNumber = _configuration["TwilioSettings:CallerNumber"];
            _twilioAuthToken = _configuration["TwilioSettings:AUTH_TOKEN"];
            _twilioAccountSID = _configuration["TwilioSettings:ACCOUNT_SID"];
            _twilioClient = new TwilioRestClient(_twilioAccountSID, _twilioAuthToken);
            _activityTrackerService = activityTrackerService;
        }


        /// <summary>
        /// Send Event SMS Async
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="userMobileNumber"></param>
        /// <returns></returns>
        public async Task SendSMSAsync(string msg, string userMobileNumber)
        {
            try
            {
                // Send an SMS message.              
                await MessageResource.CreateAsync(to: new PhoneNumber(userMobileNumber), from: new PhoneNumber(_twilioSMSNumber), body: msg, client: _twilioClient);

            }
            catch (TwilioException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        /// <summary>
        /// Sends message on whatsapp
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="userMobileNumber"></param>
        /// <returns></returns>        
        public async Task SendWhatappMessageAsync(string msg, string userMobileNumber)
        {
            try
            {
                // Send a Whatsapp message.              
                await MessageResource.CreateAsync(to: new PhoneNumber("whatsapp:" + userMobileNumber), from: new PhoneNumber("whatsapp:" + _twilioWhatsappSender), body: msg, client: _twilioClient);

            }
            catch (TwilioException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        /// <summary>
        /// Make a phone call
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="userMobileNumber"></param>
        /// <returns></returns>
        public async Task MakePhoneCall(string userMobileNumber)
        {
            try
            {

                TwilioClient.Init(_configuration["TwilioSettings:ACCOUNT_SID"], _configuration["TwilioSettings:AUTH_TOKEN"]);
                var to = new PhoneNumber(userMobileNumber);
                var from = new PhoneNumber(_twilioCallerNumber);
                var call = await CallResource.CreateAsync(
                    to: to,
                    from: from,
                    url: new Uri("https://beige-vulture-6005.twil.io/assets/phoneCallFile.xml"));


            }
            catch (TwilioException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        public async Task<bool> AutoCallCandidate(string phoneNumber, int irfId, int candidateId, string loggedInEmail)
        {
            try
            {
                var link = string.Format("https://portal.risebird.io/TwilioVoice/Index?irfId={0}", irfId.ToString());
                var url = new Uri(link);
                TwilioClient.Init(_twilioAccountSID, _twilioAuthToken);
                var to = new PhoneNumber(phoneNumber);
                var from = new PhoneNumber(_twilioCallerNumber);

                var call = await CallResource.CreateAsync(
                    to: to,
                    from: from,
                    url: url);

                if (call.Status != null)
                {
                    var result = call.Status.ToString();
                    if (result != CallResource.StatusEnum.Failed.ToString())
                    {
                        //await _irfService.AddCandidateStatus(loggedInEmail, candidateId, irfId, "Placed a reminder call", (int)CandidateStatusOptions.ReminderCall);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                return false;

            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public async Task<string> callrecording(string callSid, string callbackUrl)
        {
            try
            {
                string accountSid = _configuration["TwilioSettings:ACCOUNT_SID"];
                string authToken = _configuration["TwilioSettings:AUTH_TOKEN"];
                TwilioClient.Init(accountSid, authToken);

                var recording = await Twilio.Rest.Api.V2010.Account.Call.RecordingResource.CreateAsync(
                    recordingChannels: "dual",
                    pathCallSid: callSid
                );
                //var recordingStatusCallbackEvent = new List<string> {
                //    "completed"        };

                //recordingStatusCallback: new Uri("https://portal.risebird.io/User/CallRecordingback"),
                //var recording = await Twilio.Rest.Api.V2010.Account.Call.RecordingResource.CreateAsync(
                //    recordingStatusCallback: new Uri(callbackUrl),
                //    recordingStatusCallbackEvent: recordingStatusCallbackEvent,
                //    recordingChannels: "dual",
                //    pathCallSid: callSid
                //);
                return recording.Sid;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return e.ToString();
            }
        }

        public async Task<bool> CandidateReminderCall(string phoneNumber, string FirstName, string interviewdate)
        {
            try
            {
                FirstName = FirstName?.Trim().Replace(" ", "_");
                interviewdate = interviewdate.Replace(" ", "_").Replace("/", "_");
                var link = string.Format("https://portal.risebird.io/TwilioVoice/CandidateReminderCall?name={0}&interviewdate={1}", FirstName, interviewdate);
                var url = new Uri(link);
                TwilioClient.Init(_twilioAccountSID, _twilioAuthToken);
                var to = new PhoneNumber(phoneNumber);
                var from = new PhoneNumber(_twilioCallerNumber);
                var call = await CallResource.CreateAsync(
                    to: to,
                    from: from,
                    url: url);

                if (call.Status != null)
                {
                    var result = call.Status.ToString();
                    if (result != CallResource.StatusEnum.Failed.ToString())
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                return false;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<RecordingResource> FetchCallRecordingDetails(string recordingSid)
        {
            try
            {

                string accountSid = _configuration["TwilioSettings:ACCOUNT_SID"];
                string authToken = _configuration["TwilioSettings:AUTH_TOKEN"];

                TwilioClient.Init(accountSid, authToken);

                var recordingData = await RecordingResource.FetchAsync(
                    pathSid: recordingSid
                );

                return recordingData;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        /// <summary>
        /// Make IVR call common method, the ivrType will determine what menu will be read to the recipient, for eg. ivrType = interviewerAvailability or candidateConfirmation etc.
        /// </summary>
        /// <param name="userPhoneNumber"></param>
        /// <param name="ivrType"></param>
        /// <returns></returns>
        public async Task<bool> MakeIVRCall(string userPhoneNumber, string recipientName, string interviewDate, string ivrType = null)
        {
            try
            {
                TwilioClient.Init(_twilioAccountSID, _twilioAuthToken);
                var uniqueId = "5DF6F746-A820-4129-BE42-34970C6679E7";
                var irfId = 10681;
                int assessmentGroupId = 0;
                string jdTitle = "Python Developer";
                jdTitle = jdTitle?.Trim()?.Replace(" ", "_")?.Replace("/", "_");

                var link = $"https://portal.risebird.io/TwilioVoice/IvrInterviewerAvailability?uniqueId={uniqueId}&irfId={irfId}&assessmentGroupId={assessmentGroupId}&jdTitle={jdTitle}";

                //var link = string.Format("https://portal.risebird.io/TwilioVoice/IvrInterviewerAvailabilityTesting?recipientName={0}&irfId={1}", recipientName, interviewDate);
                //var link = string.Format("https://portal.risebird.io/TwilioVoice/IvrInterviewerAvailabilityTesting?recipientName={0}&interviewDate={1}", recipientName, interviewDate);

                //var link = string.Format("https://portal.risebird.io/TwilioVoice/CandidateReminderCall?name={0}&interviewdate={1}", "FirstName", "interviewdate");
                var url = new Uri(link);

                var call = await CallResource.CreateAsync(
                    to: new PhoneNumber(userPhoneNumber),
                    from: new PhoneNumber(_twilioCallerNumber),
                    url: url // Replace with your public endpoint
                );

                if (call.Status != null)
                {
                    var result = call.Status.ToString();
                    if (result != CallResource.StatusEnum.Failed.ToString())
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<bool> MakeIVRCallToInterviewerForAvailability(string userPhoneNumber, string uniqueId, int irfId, string jdTitle, int assessmentGroupId)
        {
            try
            {
                await _activityTrackerService.DebugLogger($"MakeIVRCallToInterviewerForAvailability twilio function invked succesfully", "ivr", string.Empty);

                TwilioClient.Init(_twilioAccountSID, _twilioAuthToken);
                jdTitle = jdTitle?.Trim()?.Replace(" ", "")?.Replace("/", "_");

                var link = $"https://portal.risebird.io/TwilioVoice/IvrInterviewerAvailability?uniqueId={uniqueId}&irfId={irfId}&assessmentGroupId={assessmentGroupId}&jdTitle={jdTitle}";

                var url = new Uri(link);

                    var call = await CallResource.CreateAsync(
                        to: new PhoneNumber(userPhoneNumber),
                        from: new PhoneNumber(_twilioCallerNumber),
                        url: url // Replace with your public endpoint
                    );

                    if (call.Status != null)
                    {
                        var result = call.Status.ToString();
                        if (result != CallResource.StatusEnum.Failed.ToString())
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }

                return true;
                
                
            }
            catch (Exception ex)
            {
                await _activityTrackerService.DebugLogger($"MakeIVRCallToInterviewerForAvailability function error : {ex.Message}, {ex.StackTrace}", "ivr", string.Empty);

                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        //public async Task<bool> MakeIVRCallForCandidateMeetingReminder(string userPhoneNumber, string candidateFirstName,string uniqueId,  int irfId, string jdTitle, int interviewId, int candidateId, DateTime interviewDate, string timeZoneValue)
        //{
        //    try
        //    {
        //        TwilioClient.Init(_twilioAccountSID, _twilioAuthToken);
        //        var uniqueId = "5DF6F746-A820-4129-BE42-34970C6679E7";
        //        var irfId = 10681;
        //        int assessmentGroupId = 0;
        //        string jdTitle = "Python Developer";
        //        jdTitle = jdTitle?.Trim()?.Replace(" ", "_")?.Replace("/", "_");

        //        var link = $"https://portal.risebird.io/TwilioVoice/IvrInterviewerAvailability?uniqueId={uniqueId}&irfId={irfId}&assessmentGroupId={assessmentGroupId}&jdTitle={jdTitle}";

        //        //var link = string.Format("https://portal.risebird.io/TwilioVoice/IvrInterviewerAvailabilityTesting?recipientName={0}&irfId={1}", recipientName, interviewDate);
        //        //var link = string.Format("https://portal.risebird.io/TwilioVoice/IvrInterviewerAvailabilityTesting?recipientName={0}&interviewDate={1}", recipientName, interviewDate);

        //        //var link = string.Format("https://portal.risebird.io/TwilioVoice/CandidateReminderCall?name={0}&interviewdate={1}", "FirstName", "interviewdate");
        //        var url = new Uri(link);

        //        var call = await CallResource.CreateAsync(
        //            to: new PhoneNumber(userPhoneNumber),
        //            from: new PhoneNumber(_twilioCallerNumber),
        //            url: url // Replace with your public endpoint
        //        );

        //        if (call.Status != null)
        //        {
        //            var result = call.Status.ToString();
        //            if (result != CallResource.StatusEnum.Failed.ToString())
        //            {
        //                return true;
        //            }
        //            else
        //            {
        //                return false;
        //            }
        //        }
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //        return false;
        //    }
        //}

        //public async Task<bool> MakeIVRCallToInterviewerForAvailability(string userPhoneNumber, string uniqueId, int irfId, string jdTitle, int assessmentGroupId)
        //{
        //    try
        //    {
        //        await _activityTrackerService.DebugLogger($"MakeIVRCallToInterviewerForAvailability twilio function invked succesfully", "ivr", string.Empty);

        //        TwilioClient.Init(_twilioAccountSID, _twilioAuthToken);
        //        jdTitle = jdTitle?.Trim()?.Replace(" ", "")?.Replace("/", "_");

        //        var link = $"https://portal.risebird.io/TwilioVoice/IvrInterviewerAvailability?uniqueId={uniqueId}&irfId={irfId}&assessmentGroupId={assessmentGroupId}&jdTitle={jdTitle}";

        //        var url = new Uri(link);

        //            var call = await CallResource.CreateAsync(
        //                to: new PhoneNumber(userPhoneNumber),
        //                from: new PhoneNumber(_twilioCallerNumber),
        //                url: url // Replace with your public endpoint
        //            );

        //            if (call.Status != null)
        //            {
        //                var result = call.Status.ToString();
        //                if (result != CallResource.StatusEnum.Failed.ToString())
        //                {
        //                    return true;
        //                }
        //                else
        //                {
        //                    return false;
        //                }
        //            }

        //        return true;
                
                
        //    }
        //    catch (Exception ex)
        //    {
        //        await _activityTrackerService.DebugLogger($"MakeIVRCallToInterviewerForAvailability function error : {ex.Message}, {ex.StackTrace}", "ivr", string.Empty);

        //        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //        return false;
        //    }
        //}

        public async Task<bool> MakeIVRCallForCandidateMeetingReminder(string userPhoneNumber,string uniqueId,  int irfId, int interviewId, int candidateId)
        {
            try
            {
                await _activityTrackerService.DebugLogger($"IVRCall to Candidate for their Interview Availability Reminder", "IVR", string.Empty);

                TwilioClient.Init(_twilioAccountSID, _twilioAuthToken);

                var link = $"https://portal.risebird.io/TwilioVoice/IvrCandidateInterviewAvailability?uniqueId={uniqueId}&irfId={irfId}&interviewId={interviewId}&candidateId={candidateId}";

                var url = new Uri(link);

                    var call = await CallResource.CreateAsync(
                        to: new PhoneNumber(userPhoneNumber),
                        from: new PhoneNumber(_twilioCallerNumber),
                        url: url // Replace with your public endpoint
                    );

                if (call.Status != null)
                {
                    var result = call.Status.ToString();
                    if (result != CallResource.StatusEnum.Failed.ToString())
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

                return true;


            }
            catch (Exception ex)
            {
                await _activityTrackerService.DebugLogger($"MakeIVRCallForCandidateMeetingReminder function error : {ex.Message}, {ex.StackTrace}", "ivr", string.Empty);

                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }


    }
}
