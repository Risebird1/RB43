using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.MicroService
{
    public class CreateIRFCheckavailability
    {
        private readonly IIRFService _irfService;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly INotificationHelper _notificationHelper;
        private readonly IWhatsAppHelper _whatsAppHelper;
        private readonly IEncryptionHelper _encryptionHelper;
        private readonly IIRFRepository _irfRepository;

        public CreateIRFCheckavailability(IApplicationErrorLogger applicationErrorLogger, IIRFService irfService, 
            INotificationHelper notificationHelper, IWhatsAppHelper whatsAppHelper, IEncryptionHelper encryptionHelper, IIRFRepository irfRepository)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _irfService = irfService;
            _notificationHelper = notificationHelper;
            _whatsAppHelper = whatsAppHelper;
            _encryptionHelper = encryptionHelper;
            _irfRepository = irfRepository;
        }

        [FunctionName("CreateIRFCheckavailability")]
        public async Task Run([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            try
            {
                var IRFList = await _irfService.GetAllNewCreatedIRFs();
                foreach (var irf in IRFList)
                {
                    //var model = await _irfService.GetIRFInterviewerMembers(irf.id);
                    var interviewers = await _irfRepository.GetAvailableAssessmentGroupInterviewerMembers(Convert.ToInt16(irf.AssessmentGroupId));
                    var skills = await _irfService.GetTechSkillswithWeightage(irf.id);

                    foreach (var interviewer in interviewers)
                    {
                        //await _irfService.UpdateIntervieweravailbilityDetails(irf.id);
                        //var skills = await _irfService.GetTechSkillswithWeightage(irf.id);
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
                                var encryptedDate = await _encryptionHelper.EncryptDateTime(DateTime.Now);
                                string yesbutton = "https://portal.risebird.io/IRF/UpdateInterviewerAvailbilityStatusForIRF?status=1&uniqueid=" + interviewer.uniqueid + "&Irfid=" + irf.id + "&dec=" + encryptedDate;
                                string nobutton = "https://portal.risebird.io/IRF/UpdateInterviewerAvailbilityStatusForIRF?status=2&uniqueid=" + interviewer.uniqueid + "&Irfid=" + irf.id + "&dec=" + encryptedDate;
                                Mail _mail = new Mail
                                {
                                    TO = interviewer.EmailId,
                                    Subject = "Risebird: Availbility check for Skill Channel " + irf.IRFId + " Interviews"
                                };
                                var emailresult = _notificationHelper.SendCheckIntervieweravailbility(_mail, interviewer.InterviewerName, yesbutton, nobutton, sb.ToString(), irf.IRFId, irf.JDLink, irf.FlatRate);//item.PerSuccessRate
                                WhatsAppVM _whatsAppVM = new WhatsAppVM();
                                _whatsAppVM.Emailid = interviewer.EmailId;
                                _whatsAppVM.PrimarySkill = irf.JDTiltle;
                                _whatsAppVM.MessageValue = irf.IRFId;
                                _whatsAppVM.PerSuccessRate = irf.FlatRate.ToString();//interviewer.PerSuccessRate.ToString();
                                _whatsAppVM.Interviewduration = irf.Interviewduration.ToString();
                                _whatsAppVM.buttonlink = yesbutton.Replace("https://portal.risebird.io/IRF/UpdateCheckIntervieweravailbilityStatus?status=1&uniqueid=", "");
                                _whatsAppVM.Event = "SkillChannel Interviewer Availbility";
                                //await _whatsAppHelper.WhatsAppSkillChannelInterviewerAvailbility(_whatsAppVM);
                            }
                        }
                        catch (Exception ex1)
                        {
                            await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex1);
                        }
                        await _irfService.UpdateIntervieweravailbilityDetails(irf.id);
                        //var skills = await _irfService.GetTechSkillswithWeightage(irf.id);
                    }

                    //model = model.Where(x => x.Type == "Interviewer").ToList();
                    //if (model.FirstOrDefault().AvailbilityHour >= 86400 || model.FirstOrDefault().AvailbilityHour == 0)
                    //{
                    //    await _irfService.UpdateIntervieweravailbilityDetails(irf.id);
                    //    var skills = await _irfService.GetTechSkillswithWeightage(irf.id);
                    //    StringBuilder sb = new StringBuilder();
                    //    sb.Append("<table style='width: 100%; text-align: center;border-collapse:collapse'>");
                    //    sb.Append("<tr><td style='border: 1px solid black'>Skills</td><td style='border: 1px solid black'>Weightage</td></tr>");
                    //    foreach (var item in skills)
                    //    {
                    //        sb.Append("<tr><td style='border: 1px solid black'>");
                    //        sb.Append(item.SkillName);
                    //        sb.Append("</td>");
                    //        sb.Append("<td style='border: 1px solid black'>");
                    //        sb.Append(item.Weightage);
                    //        sb.Append("</td></tr>");
                    //    }
                    //    sb.Append("</table>");
                    //    foreach (var item in model)
                    //    {
                    //        try
                    //        {
                    //            if (!string.IsNullOrEmpty(item.EmailId))
                    //            {
                    //                var encryptedDate = _encryptionHelper.EncryptDateTime(DateTime.Now);
                    //                string yesbutton = "https://portal.risebird.io/IRF/UpdateInterviewerAvailbilityStatusForIRF?status=1&uniqueid=" + item.uniqueid + "&Irfid=" + irf.id + "&dec=" + encryptedDate;
                    //                string nobutton = "https://portal.risebird.io/IRF/UpdateInterviewerAvailbilityStatusForIRF?status=2&uniqueid=" + item.uniqueid + "&Irfid=" + irf.id + "&dec=" + encryptedDate;
                    //                Mail _mail = new Mail
                    //                {
                    //                    TO = item.EmailId,
                    //                    Subject = "Risebird: Availbility check for Skill Channel " + irf.IRFId + " Interviews"
                    //                };
                    //                var emailresult = _notificationHelper.SendCheckIntervieweravailbility(_mail, item.InterviewerName, yesbutton, nobutton, sb.ToString(), irf.IRFId, item.JDLink, item.PerSuccessRate);
                    //                WhatsAppVM _whatsAppVM = new WhatsAppVM();
                    //                _whatsAppVM.Emailid = item.EmailId;
                    //                _whatsAppVM.PrimarySkill = item.JDTitle;
                    //                _whatsAppVM.MessageValue = irf.IRFId;
                    //                _whatsAppVM.PerSuccessRate = item.PerSuccessRate.ToString();
                    //                _whatsAppVM.Interviewduration = item.Interviewduration;
                    //                _whatsAppVM.buttonlink = yesbutton.Replace("https://portal.risebird.io/IRF/UpdateCheckIntervieweravailbilityStatus?status=1&uniqueid=", "");
                    //                _whatsAppVM.Event = "SkillChannel Interviewer Availbility";
                    //                //await _whatsAppHelper.WhatsAppSkillChannelInterviewerAvailbility(_whatsAppVM);
                    //            }
                    //        }
                    //        catch (Exception ex1)
                    //        {
                    //            await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex1);
                    //        }
                    //    }
                    //}
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }
    }
}
