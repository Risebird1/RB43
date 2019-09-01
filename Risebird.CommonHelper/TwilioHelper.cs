using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Twilio;
using Twilio.Clients;
using Twilio.Exceptions;
using Twilio.Rest.Api.V2010.Account;
using Twilio.TwiML;
using Twilio.Types;

namespace Risebird.CommonHelper
{
    public class TwilioHelper :ITwilioHelper
    {
        //private readonly IHostingEnvironment _hostingEnvironment;
        private IApplicationErrorLogger _applicationErrorLogger;
        private readonly IConfiguration _configuration;
        private readonly string _twilioSMSNumber = string.Empty;
        private readonly string _twilioWhatsappSender = string.Empty;
        private readonly string _twilioCallerNumber = string.Empty;
        private readonly TwilioRestClient _twilioClient;

        public TwilioHelper(IApplicationErrorLogger applicationErrorLogger, IConfiguration configuration)
        {
            //_hostingEnvironment = hostingEnvironment;
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _twilioSMSNumber = _configuration["TwilioSettings:SmsSENDER"];
            _twilioWhatsappSender = _configuration["TwilioSettings:WhatsappSENDER"];
            _twilioCallerNumber = _configuration["TwilioSettings:CallerNumber"];
            var twilioAccountSID = _configuration["TwilioSettings:ACCOUNT_SID"];
            var twilioAuthToken = _configuration["TwilioSettings:AUTH_TOKEN"];
            _twilioClient = new TwilioRestClient(twilioAccountSID, twilioAuthToken);
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
                await MessageResource.CreateAsync(to: new PhoneNumber("whatsapp:"+userMobileNumber), from: new PhoneNumber("whatsapp:"+_twilioWhatsappSender), body: msg, client: _twilioClient);

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


    }
}
