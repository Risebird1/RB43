using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Risebird.MicroService
{
    public class SendCampaignMails
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger log;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly INotificationHelper _notificationHelper;
        private readonly IEncryptionHelper _encryptionHelper;
        public SendCampaignMails(ILoggerFactory loggerFactory, IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger, INotificationHelper notificationHelper, IEncryptionHelper encryptionHelper)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _notificationHelper = notificationHelper;
            _encryptionHelper = encryptionHelper;
            log = loggerFactory.CreateLogger<SendCampaignMails>();
        }

        [Function("SendCampaignMails")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "SendCampaignMails")] HttpRequest req)
        {
            try
            {
                log.LogInformation($"SendCampaignMails : C# HTTP trigger function invoked at {DateTime.Now}");

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                log.LogInformation($"SendCampaignMails : Event data recieved with request body :  {requestBody}");

                var data = JsonConvert.DeserializeObject<CampaignMailDTO>(requestBody);

                await SendCampaignLevelMailsToInterviewers(data.interviewers, data.assessentGroupName, data.campagignId);//fire and forget method, keeps sending emails to the interviewers in the background

                return new OkObjectResult(true);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        private async Task SendCampaignLevelMailsToInterviewers(IEnumerable<AssessmentGroupInterviewerDetails> interviewers, string assessmentGroupName, int campaignId)
        {
            string source = "ml";

            var encryptedDate = await _encryptionHelper.EncryptDateTime(DateTime.Now);
            //foreach (var item in interviewers)
            //{
            //    string yesbutton = "https://portal.risebird.io/IRF/CampaignUpdateInterviewerAvailbilityStatusForAssessmentGroup?isAv=true&uniqueid=" + item.uniqueid + "&cmpId=" + campaignId + "&dec=" + encryptedDate;
            //    string nobutton = "https://portal.risebird.io/IRF/CampaignUpdateInterviewerAvailbilityStatusForAssessmentGroup?isAv=false&uniqueid=" + item.uniqueid + "&cmpId=" + campaignId + "&dec=" + encryptedDate;
            //    //string dropMyProfileButton = "https://portal.risebird.io/IRF/CampaignUpdateInterviewerAvailbilityStatusForAssessmentGroup?isAv=4&uniqueid=" + item.uniqueid + "&agId=" + irfId + "&dec=" + encryptedDate;
            //    Mail _mail = new Mail
            //    {
            //        TO = item.EmailId,
            //        Subject = "Current Availability (2 days) check for skills based on: " + assessmentGroupName + " Risebird Interviews"
            //    };

            //    var emailresult = await _notificationHelper.SendCampaignLevelInterviewerAvailabilityMails(item.EmailId, yesbutton + "&rsc=" + source, nobutton + "&rsc=" + source, assessmentGroupName, campaignId);

            //}

            if (interviewers != null)
            {
                Parallel.ForEach(interviewers, async item =>
                {
                    try
                    {
                        string yesbutton = "https://portal.risebird.io/IRF/CampaignUpdateInterviewerAvailbilityStatusForAssessmentGroup?isAv=true&uniqueid=" + item.uniqueid + "&cmpId=" + campaignId + "&dec=" + encryptedDate;
                        string nobutton = "https://portal.risebird.io/IRF/CampaignUpdateInterviewerAvailbilityStatusForAssessmentGroup?isAv=false&uniqueid=" + item.uniqueid + "&cmpId=" + campaignId + "&dec=" + encryptedDate;
                        //string dropMyProfileButton = "https://portal.risebird.io/IRF/CampaignUpdateInterviewerAvailbilityStatusForAssessmentGroup?isAv=4&uniqueid=" + item.uniqueid + "&agId=" + irfId + "&dec=" + encryptedDate;
                        Mail _mail = new Mail
                        {
                            TO = item.EmailId,
                            Subject = "Current Availability (2 days) check for skills based on: " + assessmentGroupName + " Risebird Interviews"
                        };

                        var emailresult = await _notificationHelper.SendCampaignLevelInterviewerAvailabilityMails(item.EmailId, yesbutton + "&rsc=" + source, nobutton + "&rsc=" + source, assessmentGroupName, campaignId);

                    }
                    catch (Exception ex)
                    {
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                        return;
                    }
                });
            }

            //source = "wp";

            //yesbutton = yesbutton + "&rsc=" + source;
            //WhatsAppVM _whatsAppVM = new WhatsAppVM();
            //_whatsAppVM.Emailid = item.EmailId;
            //_whatsAppVM.PrimarySkill = item.AGName;

            //_whatsAppVM.buttonlink = yesbutton.Replace("https://portal.risebird.io/IRF/", "");
            //_whatsAppVM.Event = "SkillChannel Interviewer Availbility";
        }

    }
}
