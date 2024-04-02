using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.OData.Edm;
using OfficeOpenXml;
//using OfficeOpenXml.Core.ExcelPackage;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Twilio.TwiML.Messaging;

namespace Risebird.MicroService
{
    public class CandidateStatusNotifier
    {
        private readonly IConfiguration _configuration;
        private readonly ICandidateRepository _candidateRepository;
        private readonly IIRFRepository _irfRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly INotificationHelper _notificationHelper;
        private readonly IExcelHelper _excelHelper;
        private readonly IClientRepository _clientRepository;

        private readonly string connectionString;

        public CandidateStatusNotifier(IConfiguration configuration, ICandidateRepository candidateRepository, IApplicationErrorLogger applicationErrorLogger, IIRFRepository irfRepository, INotificationHelper notificationHelper, IExcelHelper excelHelper, IClientRepository clientRepository)
        {
            _configuration = configuration;
            _candidateRepository = candidateRepository;
            _applicationErrorLogger = applicationErrorLogger;
            _irfRepository = irfRepository;
            _notificationHelper = notificationHelper;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
            _excelHelper =  excelHelper;
            _clientRepository = clientRepository;

        }

        [FunctionName("CandidateStatusNotifier")]
        public async Task Run([TimerTrigger("0 0 0 * * *")] TimerInfo myTimer, ILogger log)//0 0 0 * * *
        {
            try
            {
                log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

                var data = await _candidateRepository.GetCandidateLastStatus();
                if (data != null)
                {
                    //var irfList = new List<int>();
                    //irfList = data.Select(x => x.IRFintId).Distinct().ToList();

                    //if (irfList != null)
                    //{
                    //    foreach (var irf in irfList)
                    //    {
                    //        var candidates = new List<string>();

                    //        //var mailList = await _irfRepository.GetIRFClientsEmail(irf);
                    //        var mailList = new List<string>();
                    //        mailList.Add("harshit_jajoo@risebird.io");
                    //        mailList.Add("ruma_bose@risebird.io");

                    //        if (mailList == null || mailList.Count() == 0)
                    //        {
                    //            continue;
                    //        }
                    //        foreach (var item in data.Where(x => x.IRFintId == irf && !string.IsNullOrEmpty(x.StatusValue)))
                    //        {
                    //            var status = string.Empty;


                    //            if (item.StatusValue == "Interview Status")
                    //            {
                    //                if ((item.Comments == InterviewStatus.Selected.GetDescription() || item.Comments == InterviewStatus.BorderLine.GetDescription()) && item.QCStatus == null)
                    //                {
                    //                    status = MeetingStatus.Completed.GetDescription();
                    //                }
                    //                else
                    //                {
                    //                    status = item.Comments;
                    //                }
                    //            }

                    //            else if (item.StatusValue == "Meeting Status")
                    //            {
                    //                status = item.Comments;
                    //            }

                    //            else
                    //            {
                    //                status = item.StatusValue;
                    //            }

                    //            candidates.Add(item.candidateName + "|" + item.candidateEmail + "|" + status + "|" + item.MobileNumber + "|" + (item.TotalTechScore.HasValue ? item.TotalTechScore.Value : "NA" )+ "|" + (!string.IsNullOrEmpty(item.ResumeLink) ? item.ResumeLink : ""  )+ "|" + (item.interviewDate.HasValue ? item.interviewDate.Value.ToString() : "NA"));
                    //        }
                    //        if (candidates == null || candidates.Count() == 0)
                    //        {
                    //            continue;
                    //        }
                    //        var details = data.Where(x => x.IRFintId == irf).FirstOrDefault();
                    //        var primarySkill = details.JDTitle.Split('|')[0];

                    //        // Create an Excel file
                    //        byte[] excelFile = _excelHelper.CreateExcelFile(candidates, details.IRFId);
                    //        string fileName = "Risebird_CandidateStatus_" + details.IRFId + "_" + primarySkill + "_" + DateTime.Now.AddDays(-1).ToString("dd MMMM yyyy") + ".xlsx";
                    //        fileName = fileName.Replace(" ", "_");
                    //        await _notificationHelper.SendCandidateLastStatusMail(mailList.ToList(), candidates, details.IRFId, primarySkill, Convert.ToBase64String(excelFile), fileName);
                    //    }
                    //}
                    var clientIdList = new List<int>();
                    clientIdList = data.Select(x => x.ClientId).Distinct().ToList();
                    if(clientIdList != null  && clientIdList.Count()>0)
                    {
                        foreach (var clientId in clientIdList)
                        {
                            var mailList = await _clientRepository.GetClientUsersByClientId(clientId);
                            //var mailList = new List<string>();
                            //mailList.Add("harshit_jajoo@risebird.io");
                            //mailList.Add("ruma_bose@risebird.io");
                            var irfList = new List<int>();
                            irfList = data.Where(x=>x.ClientId == clientId).Select(x => x.IRFintId).Distinct().ToList();
                            if (irfList != null)
                            {
                                var candidates = new List<string>();

                                foreach (var irf in irfList)
                                {
                                    
                                    if (mailList == null || mailList.Count() == 0)
                                    {
                                        continue;
                                    }
                                    foreach (var item in data.Where(x => x.IRFintId == irf && !string.IsNullOrEmpty(x.StatusValue)))
                                    {
                                        var status = string.Empty;


                                        if (item.StatusValue == "Interview Status")
                                        {
                                            if ((item.Comments == InterviewStatus.Selected.GetDescription() || item.Comments == InterviewStatus.BorderLine.GetDescription()) && item.QCStatus == null)
                                            {
                                                status = MeetingStatus.Completed.GetDescription();
                                            }
                                            else
                                            {
                                                status = item.Comments;
                                            }
                                        }

                                        else if (item.StatusValue == "Meeting Status")
                                        {
                                            status = item.Comments;
                                        }

                                        else
                                        {
                                            status = item.StatusValue;
                                        }

                                        candidates.Add(item.candidateName + "|" + item.candidateEmail + "|" + status + "|" + item.MobileNumber + "|" + (item.TotalTechScore.HasValue ? item.TotalTechScore.Value : "NA") + "|" + (!string.IsNullOrEmpty(item.ResumeLink) ? item.ResumeLink : "") + "|" + (item.interviewDate.HasValue ? item.interviewDate.Value.ToString() : "NA") + "|" + item.IRFId + "|" + item.JDTitle?.Split('|')[0]);
                                    }
                                    //if (candidates == null || candidates.Count() == 0)
                                    //{
                                    //    continue;
                                    //}
                                    //var details = data.Where(x => x.IRFintId == irf).FirstOrDefault();
                                    //var primarySkill = details.JDTitle.Split('|')[0];

                                    // Create an Excel file
                                    //byte[] excelFile = _excelHelper.CreateExcelFile(candidates, details.IRFId);

                                }
                                if (candidates != null && candidates.Count() != 0)
                                {
                                    byte[] excelFile = _excelHelper.CreateExcelFile(candidates);
                                    string fileName = "Risebird_CandidateStatus_" + DateTime.Now.AddDays(-1).ToString("dd MMMM yyyy") + ".xlsx";
                                    fileName = fileName.Replace(" ", "_");
                                    await _notificationHelper.SendCandidateLastStatusMail(mailList.ToList(), candidates, Convert.ToBase64String(excelFile), fileName);
                                }

                            }
                        }
                    }
                    
                }

                log.LogInformation($"C# Timer trigger function completed at: {DateTime.Now}");
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

    }
}
