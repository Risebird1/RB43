using EllipticCurve.Utils;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
//using OfficeOpenXml.Core.ExcelPackage;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.MicroService
{
    public class CandidateStatus90DaysNotifierTurning
    {
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly INotificationHelper _notificationHelper;

        private readonly ICandidateService _candidateService;

        private readonly string connectionString;

        public CandidateStatus90DaysNotifierTurning(IConfiguration configuration, ICandidateService candidateService, IApplicationErrorLogger applicationErrorLogger, INotificationHelper notificationHelper)
        {
            _configuration = configuration;
            _candidateService = candidateService;
            _applicationErrorLogger = applicationErrorLogger;
            
            _notificationHelper = notificationHelper;
            connectionString = _configuration["ConnectionString:DBConnectionString"];

        }

        [FunctionName("CandidateStatus90DaysNotifierTurning")]
        public async Task Run([TimerTrigger("0 0 0 * * *")] TimerInfo myTimer, ILogger log)//0 0 0 * * *
        {
            try
            {
                log.LogInformation($"C# Timer trigger CandidateStatusNotifierTurning executed at: {DateTime.Now}");

                var mailList = new List<string>() { "prem@risebird.io", 
                    //"amika.s@turing.com", 
                    //"annu.s@turing.com", 
                    //"ashutosh@risebird.io",
                    //"danijel.j@turing.com",
                    //"neha.khaitan@turing.com",
                    //"parth.gohel@turing.com",
                    //"priyanka.k@turing.com",
                    //"soumya.m@turing.com",
                    "prashanth_k@risebird.io",
                    "harshit_jajoo@risebird.io"
                };
                ExcelPackage Ep = new ExcelPackage();
                ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Candidate Status");
                string fileName = "Risebird" + DateTime.Now.ToString("dd/MM/yyyy") + "_Candidate_Status" + ".xlsx";
               // var model = new List<CandidateExcelData>();
                var candidateDatawithoutCold = new List<CandidateExcelData>();
                candidateDatawithoutCold = (await _candidateService.GetCandidateStatusDataWithoutColdDate(DateTime.Now.AddDays(-90), DateTime.Now, 177)).ToList();
                var candidateDatawithCold = new List<CandidateExcelData>();
                candidateDatawithCold = (await _candidateService.GetCandidateStatusDataWithColdDate(DateTime.Now.AddDays(-90), DateTime.Now, 177)).ToList();
                log.LogInformation($"C# Timer trigger CandidateStatusNotifierTurning Database SP executed at: {DateTime.Now}");
                candidateDatawithoutCold = candidateDatawithoutCold.Select(x => { x.MeetingStatus = !string.IsNullOrWhiteSpace(x.MeetingStatus) ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty; x.InterviewStatus = !string.IsNullOrWhiteSpace(x.InterviewStatus) ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty; x.confirmationStatus = !string.IsNullOrWhiteSpace(x.confirmationStatus) ? ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription() : string.Empty; return x; }).ToList();
                candidateDatawithCold = candidateDatawithCold.Select(x => { x.MeetingStatus = !string.IsNullOrWhiteSpace(x.MeetingStatus) ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty; x.InterviewStatus = !string.IsNullOrWhiteSpace(x.InterviewStatus) ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty; x.confirmationStatus = !string.IsNullOrWhiteSpace(x.confirmationStatus) ? ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription() : string.Empty; return x; }).ToList();

                log.LogInformation($"C# Timer trigger CandidateStatusNotifierTurning Database Data Count Without COld count: {candidateDatawithoutCold.Count} and with cold count: {candidateDatawithCold.Count} executed at: {DateTime.Now}");


                Sheet.Cells["A1:Z1"].Style.Font.Bold = true;
                Sheet.Cells["A1:Z1"].Style.Font.Size = 12;
                Sheet.Cells["A1:Z1"].Style.WrapText = true;
                int startColumn = 1; int endColumn = 26;
                for (int a = startColumn; a <= endColumn; a++)
                {
                    Sheet.Column(a).Width = 30;
                }
                Sheet.Cells[1, 1].Value = "Candidate name";
                Sheet.Cells[1, 2].Value = "Candidate EmailId";
                Sheet.Cells[1, 3].Value = "Phone";
                Sheet.Cells[1, 4].Value = "Resume";
                Sheet.Cells[1, 5].Value = "JD Title";
                Sheet.Cells[1, 6].Value = "Skill Channel ID";
                Sheet.Cells[1, 7].Value = "Added On";
                Sheet.Cells[1, 8].Value = "Invite Sent On";
                Sheet.Cells[1, 9].Value = "Scheduled On";// note: there could be multiple schedules of a candidate. This date belongs to the first entry of scheduled for the candidate for the skill channel.
                Sheet.Cells[1, 10].Value = "Interview Date";

                Sheet.Cells[1, 11].Value = "Confirmation Status";

                Sheet.Cells[1, 12].Value = "Meeting Status";
                Sheet.Cells[1, 13].Value = "Interview Status";
                Sheet.Cells[1, 14].Value = "Communication Score";
                Sheet.Cells[1, 15].Value = "Video Link";
                Sheet.Cells[1, 16].Value = "Completed On";

                Sheet.Cells[1, 17].Value = "Latest Status";

                Sheet.Cells[1, 18].Value = "Client Hr";
                Sheet.Cells[1, 19].Value = "Interviewer(Encoded)";
                Sheet.Cells[1, 20].Value = "Report Link";
                Sheet.Cells[1, 21].Value = "InterviewerComment";
                int i = 2;
                if (candidateDatawithoutCold.Count() > 0)
                {
                    foreach (var item in candidateDatawithoutCold)
                    {
                        Sheet.Cells[i, 1].Value = item.candidateName;
                        Sheet.Cells[i, 2].Value = item.candidateEmail;
                        Sheet.Cells[i, 3].Value = item.MobileNumber ?? string.Empty;

                        Sheet.Cells[i, 4].Value = item.ResumeLink;
                        Sheet.Cells[i, 5].Value = item.JDTiltle;
                        Sheet.Cells[i, 6].Value = item.IRFId;
                        Sheet.Cells[i, 7].Value = item.AddedOnDate.HasValue ? item.AddedOnDate.Value.ToString() : string.Empty;
                        Sheet.Cells[i, 8].Value = item.InvitationSentOnDate.HasValue ? item.InvitationSentOnDate.Value.ToString() : string.Empty;
                        Sheet.Cells[i, 9].Value = item.FirstScheduledOnDate.HasValue ? item.FirstScheduledOnDate.Value.ToString() : string.Empty;
                        Sheet.Cells[i, 10].Value = item.interviewDate.HasValue ? item.interviewDate.Value.ToString() : string.Empty;

                        Sheet.Cells[i, 11].Value = item.confirmationStatus ?? string.Empty;
                        Sheet.Cells[i, 12].Value = item.MeetingStatus ?? string.Empty;
                        Sheet.Cells[i, 13].Value = item.InterviewStatus ?? string.Empty;
                        Sheet.Cells[i, 14].Value = item.completedDate.HasValue ? item.completedDate.Value.ToString() : string.Empty;
                        Sheet.Cells[i, 15].Value = item.VideoLink;
                        Sheet.Cells[i, 16].Value = item.completedDate;
                        Sheet.Cells[i, 17].Value = item.CandidateLatestStatus;
                        Sheet.Cells[i, 18].Value = item.ClientHrContact;

                        Sheet.Cells[i, 19].Value = item.InterviewerCodedName ?? string.Empty;
                        Sheet.Cells[i, 20].Value = item.ExecutiveReportLink;
                        Sheet.Cells[i, 21].Value = item.InterviewerComment;
                    }

                    log.LogInformation($"C# Timer trigger CandidateStatusNotifierTurning without cold data parsed in excel sheet at: {DateTime.Now}");

                        foreach (var cold in candidateDatawithCold)
                    {

                        Sheet.Cells[i, 1].Value = cold.candidateName;
                        Sheet.Cells[i, 2].Value = cold.candidateEmail;
                        Sheet.Cells[i, 3].Value = cold.MobileNumber ?? string.Empty;

                        Sheet.Cells[i, 4].Value = cold.ResumeLink;
                        Sheet.Cells[i, 5].Value = cold.JDTiltle;
                        Sheet.Cells[i, 6].Value = cold.IRFId;

                        Sheet.Cells[i, 7].Value = cold.AddedOnDate.HasValue ? cold.AddedOnDate.Value.ToString() : string.Empty;
                        Sheet.Cells[i, 8].Value = cold.InvitationSentOnDate.HasValue ? cold.InvitationSentOnDate.Value.ToString() : string.Empty;
                        Sheet.Cells[i, 9].Value = cold.FirstScheduledOnDate.HasValue ? cold.FirstScheduledOnDate.Value.ToString() : string.Empty;
                        Sheet.Cells[i, 10].Value = string.Empty;
                        Sheet.Cells[i, 11].Value = string.Empty;
                        Sheet.Cells[i, 17].Value = cold.CandidateLatestStatus;                       
                        
                    }
                    log.LogInformation($"C# Timer trigger CandidateStatusNotifierTurning with cold data parsed in excel sheet at: {DateTime.Now}");
                }
               

                fileName = fileName.Replace(" ", "_");
                    await _notificationHelper.SendCandidateLastStatusMail(mailList.ToList(), null, Convert.ToBase64String(Ep.GetAsByteArray()), fileName);

                log.LogInformation($"C# Timer trigger CandidateStatusNotifierTurning with notification mail sent at: {DateTime.Now}");
                log.LogInformation($"C# Timer trigger CandidateStatusNotifierTurning completed at: {DateTime.Now}");
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

    }
}
