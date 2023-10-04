using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using RestSharp;
using Risebird.CommonHelper.Interface;
using Risebird.Repository.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.CommonHelper;
using Risebird.Entities.ViewModel;
using Risebird.Repository;
using System.Text;
using Risebird.Business.Interface;

namespace Risebird.MicroService
{
    public class CheckIRFAvailability
    {
        private readonly IConfiguration _configuration;

        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IIRFService _irfService;
        private readonly INotificationHelper _notificationHelper;
        private readonly IWhatsAppHelper _whatsAppHelper;
        private readonly IEncryptionHelper _encryptionHelper;
        private readonly IIRFRepository _irfRepository;
        
        public CheckIRFAvailability(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger,
            IIRFService irfService, INotificationHelper notificationHelper, IWhatsAppHelper whatsAppHelper,
            IEncryptionHelper encryptionHelper, IIRFRepository irfRepository)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _irfService = irfService;
            _notificationHelper = notificationHelper;
            _whatsAppHelper = whatsAppHelper;
            _encryptionHelper = encryptionHelper;
            _irfRepository = irfRepository;
        }

        [FunctionName("CheckIRFAvailability")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                log.LogInformation($"CheckIRFAvailability : C# HTTP trigger function invoked at {DateTime.Now}");

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                log.LogInformation($"CheckIRFAvailability : Event data recieved with request body :  {requestBody}");
                var obj = JsonConvert.DeserializeObject<AvailabilityCheckDTO>(requestBody);

                var interviewers = await _irfRepository.GetAvailableAssessmentGroupInterviewerMembers(Convert.ToInt16(obj.AssessmentGroupId), obj.id);
                var skills = await _irfService.GetTechSkillswithWeightage(obj.id);

                foreach (var interviewer in interviewers)
                {
                    
                    StringBuilder sb = new StringBuilder();
                    sb.Append("<table style='width: 100%; text-align: center;border-collapse:collapse'>");
                    sb.Append("<tr><td style='border: 1px solid black'>Skills</td><td style='border: 1px solid black'>Weightage</td></tr>");
                    foreach (var item in skills)
                    {
                        sb.Append("<tr><td style='border: 1px solid black'>");
                        sb.Append(item.SkillName);
                        sb.Append("</td>");
                        sb.Append("<td style='border: 1px solid black'>");
                        sb.Append(item.Weightage);
                        sb.Append("</td></tr>");
                    }
                    sb.Append("</table>");
                    try
                    {
                        if (!string.IsNullOrEmpty(interviewer.EmailId))
                        {
                            string source = "ml";

                            var encryptedDate = await _encryptionHelper.EncryptDateTime(DateTime.Now);
                            string yesbutton = "https://portal.risebird.io/IRF/UpdateInterviewerAvailbilityStatusForIRF?status=1&uniqueid=" + interviewer.uniqueid + "&Irfid=" + obj.id + "&dec=" + encryptedDate;
                            string nobutton = "https://portal.risebird.io/IRF/UpdateInterviewerAvailbilityStatusForIRF?status=2&uniqueid=" + interviewer.uniqueid + "&Irfid=" + obj.id + "&dec=" + encryptedDate;
                            string dropMyProfileButton = "https://portal.risebird.io/IRF/UpdateInterviewerAvailbilityStatusForIRF?status=4&uniqueid=" + interviewer.uniqueid + "&Irfid=" + obj.id + "&dec=" + encryptedDate;
                            Mail _mail = new Mail
                            {
                                TO = interviewer.EmailId,
                                Subject = "Risebird: Availability check for Skill Channel " + obj.IRFId + " Interviews"
                            };
                            
                            var emailresult = await _notificationHelper.SendCheckIntervieweravailbility(_mail, interviewer.InterviewerName, yesbutton + "&rsc=" + source, nobutton + "&rsc=" + source, sb.ToString(), obj.FlatRate, dropMyProfileButton + "&rsc=" + source, obj.PrimarySkillInvite, obj.id);//item.PerSuccessRate

                            source = "wp";

                            yesbutton = yesbutton + "&rsc=" + source;

                            WhatsAppVM _whatsAppVM = new WhatsAppVM();
                            _whatsAppVM.Emailid = interviewer.EmailId;
                            _whatsAppVM.PrimarySkill = obj.PrimarySkillInvite;//irf.JDTiltle;
                            _whatsAppVM.PerSuccessRate = obj.FlatRate.ToString();//interviewer.PerSuccessRate.ToString();
                                                                                 
                            _whatsAppVM.buttonlink = yesbutton.Replace("https://portal.risebird.io/IRF/", "");
                            _whatsAppVM.Event = "SkillChannel Interviewer Availbility";
                            await _whatsAppHelper.WhatsAppSkillChannelInterviewerAvailbility(_whatsAppVM);
                        }
                    }
                    catch (Exception ex1)
                    {
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex1);
                    }
                }
                await _irfService.UpdateIntervieweravailbilityDetails(obj.id);

                

                log.LogInformation($"CheckIRFAvailability : C# Http Trigger finished executing at {DateTime.Now}");

                return new OkObjectResult(true);

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                log.LogError($"CheckIRFAvailability :Error executing at: {DateTime.Now}, Message : {ex.Message}");

                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
