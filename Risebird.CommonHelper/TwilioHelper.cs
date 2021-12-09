using Microsoft.Extensions.Configuration;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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


        public TwilioHelper(IApplicationErrorLogger applicationErrorLogger, IConfiguration configuration)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _twilioSMSNumber = _configuration["TwilioSettings:SmsSENDER"];
            _twilioWhatsappSender = _configuration["TwilioSettings:WhatsappSENDER"];
            _twilioCallerNumber = _configuration["TwilioSettings:CallerNumber"];
            _twilioAuthToken = _configuration["TwilioSettings:AUTH_TOKEN"];
            _twilioAccountSID = _configuration["TwilioSettings:ACCOUNT_SID"];
            _twilioClient = new TwilioRestClient(_twilioAccountSID, _twilioAuthToken);
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
    }
}
