using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using System.Data;

namespace Risebird.AzureTimerFunctions
{
    public class CandidateStatus90DaysNotifierTurning
    {
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly INotificationHelper _notificationHelper;
        private readonly ILogger log;
        private readonly ICandidateService _candidateService;
        private readonly IEncryptionHelper _encryptionHelper;

        private readonly string connectionString;

        public CandidateStatus90DaysNotifierTurning(ILoggerFactory loggerFactory, IConfiguration configuration, ICandidateService candidateService, IApplicationErrorLogger applicationErrorLogger, INotificationHelper notificationHelper, IEncryptionHelper encryptionHelper)
        {
            _configuration = configuration;
            _candidateService = candidateService;
            _applicationErrorLogger = applicationErrorLogger;
            log = loggerFactory.CreateLogger<CandidateStatus90DaysNotifierTurning>();
            _notificationHelper = notificationHelper;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
            _encryptionHelper = encryptionHelper;
        }

        [Function("CandidateStatus90DaysNotifierTurning")]
        public async Task Run([TimerTrigger("0 0 4 * * *")] TimerInfo myTimer)//0 0 0 * * *
        {
            try
            {
                log.LogInformation($"C# Timer trigger CandidateStatusNotifierTurning executed at: {DateTime.Now}");

                var mailList = new List<string>() { "prem@risebird.io",
                    "amika.s@turing.com",
                    "annu.s@turing.com",
                    "ashutosh@risebird.io",
                    "danijel.j@turing.com",
                    "neha.khaitan@turing.com",
                    "parth.gohel@turing.com",
                    "priyanka.k@turing.com",
                    "soumya.m@turing.com",
                    "prashanth_k@risebird.io",
                    "harshit_jajoo@risebird.io",
                    "bhawana@turing.com",
                    "richard.m@turing.com",
                    "anabella.morales@turing.com",
                    "miracle.wikiri@turing.com",
                    "aditi.j@turing.com",
                    "tamara.dokic@turing.com",
                    "usama.nadeem@turing.com",
                    "rishikesh.agrawal@turing.com",
                    "leonardo.rodrigues@turing.com",
                    "suchismita.mukhopadhyay@turing.com",
                    "divyanka.gulati@turing.com",
                    "kasa.anuforo@turing.com",
                    "shubhanshi.srivastava@turing.com",
                    "harshita.chandnani@turing.com"
                };
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                ExcelPackage Ep = new ExcelPackage();
                ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Candidate Status");
                string fileName = "Risebird" + DateTime.Now.ToString("dd/MM/yyyy") + "_Candidate_Status" + ".xlsx";
                // var model = new List<CandidateExcelData>();
                var candidateDatawithoutCold = new List<CandidateExcelData>();
                candidateDatawithoutCold = (await _candidateService.GetCandidateStatusDataWithoutColdDate(DateTime.Now.AddDays(-60), DateTime.Now, 177)).ToList();
                log.LogInformation($"C# Timer trigger CandidateStatusNotifierTurning Database with out Code SP executed at: {DateTime.Now}");
                var candidateDatawithCold = new List<CandidateExcelData>();
                candidateDatawithCold = (await _candidateService.GetCandidateStatusDataWithColdDate(DateTime.Now.AddDays(-60), DateTime.Now, 177)).ToList();
                log.LogInformation($"C# Timer trigger CandidateStatusNotifierTurning Database SP executed at: {DateTime.Now}");
                candidateDatawithoutCold = candidateDatawithoutCold.Select(x => { x.MeetingStatus = !string.IsNullOrWhiteSpace(x.MeetingStatus) ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty; x.InterviewStatus = !string.IsNullOrWhiteSpace(x.InterviewStatus) ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty; x.confirmationStatus = !string.IsNullOrWhiteSpace(x.confirmationStatus) ? ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription() : string.Empty; x.Heuristicsscore = !string.IsNullOrWhiteSpace(x.Heuristicsscore) ? ((Heuristicsscorelevel)Convert.ToInt32(x.Heuristicsscore)).GetDescription() : string.Empty; return x; }).ToList();
                candidateDatawithCold = candidateDatawithCold.Select(x => { x.MeetingStatus = !string.IsNullOrWhiteSpace(x.MeetingStatus) ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty; x.InterviewStatus = !string.IsNullOrWhiteSpace(x.InterviewStatus) ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty; x.confirmationStatus = !string.IsNullOrWhiteSpace(x.confirmationStatus) ? ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription() : string.Empty; x.Heuristicsscore = !string.IsNullOrWhiteSpace(x.Heuristicsscore) ? ((Heuristicsscorelevel)Convert.ToInt32(x.Heuristicsscore)).GetDescription() : string.Empty; return x; }).ToList();

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
                Sheet.Cells[1, 3].Value = "Primary Skill";
                Sheet.Cells[1, 4].Value = "Client Hr";
                Sheet.Cells[1, 5].Value = "Added On";
                Sheet.Cells[1, 6].Value = "Schedule Status";
                Sheet.Cells[1, 7].Value = "Phone";

                Sheet.Cells[1, 8].Value = "Latest Status";
                Sheet.Cells[1, 9].Value = "Current Status Date";
                Sheet.Cells[1, 10].Value = "Profile Source";

                Sheet.Cells[1, 11].Value = "Skill Channel ID";
                Sheet.Cells[1, 12].Value = "Scheduled On";// note: there could be multiple schedules of a candidate. This date belongs to the first entry of scheduled for the candidate for the skill channel.
                Sheet.Cells[1, 13].Value = "Interview Date";
                Sheet.Cells[1, 14].Value = "Reschdeule By Candidate";
                Sheet.Cells[1, 15].Value = "Reschdeule By Turing";
                Sheet.Cells[1, 16].Value = "Reschdeule By Risebird";


                Sheet.Cells[1, 17].Value = "Last Cancelled By";
                Sheet.Cells[1, 18].Value = "Cancellation Source";
                Sheet.Cells[1, 19].Value = "Report Link";
                Sheet.Cells[1, 20].Value = "Cost To Turing";

                Sheet.Cells[1, 21].Value = "Schedule Count";
                Sheet.Cells[1, 22].Value = "Meeting Status";
                Sheet.Cells[1, 23].Value = "Requestor";
                Sheet.Cells[1, 24].Value = "Plan Type";
                //Sheet.Cells[1, 4].Value = "Resume";
                //Sheet.Cells[1, 8].Value = "Invite Sent On";
                //Sheet.Cells[1, 11].Value = "Confirmation Status";
                //Sheet.Cells[1, 13].Value = "Interview Status";
                //Sheet.Cells[1, 14].Value = "Communication Score";
                //Sheet.Cells[1, 15].Value = "Video Link";
                //Sheet.Cells[1, 16].Value = "Completed On";
                //Sheet.Cells[1, 18].Value = "Cancellation Source";
                //Sheet.Cells[1, 20].Value = "Interviewer(Encoded)";
                //Sheet.Cells[1, 22].Value = "InterviewerComment";

                int i = 2;
                if (candidateDatawithoutCold.Count() > 0)
                {
                    foreach (var item in candidateDatawithoutCold)
                    {
                        Sheet.Cells[i, 1].Value = item.candidateName;
                        Sheet.Cells[i, 2].Value = item.candidateEmail;
                        Sheet.Cells[i, 3].Value = item.JDTiltle;
                        Sheet.Cells[i, 4].Value = item.ClientHrContact;
                        Sheet.Cells[i, 5].Value = item.AddedOnDate.HasValue ? item.AddedOnDate.Value.ToString("MM/dd/yyyy HH:mm:ss") : string.Empty;
                        Sheet.Cells[i, 6].Value = "Scheduled";
                        Sheet.Cells[i, 7].Value = item.MobileNumber ?? string.Empty;

                        Sheet.Cells[i, 8].Value = item.CandidateLatestStatus;
                        Sheet.Cells[i, 9].Value = item.CurrentStatusDate.HasValue ? item.CurrentStatusDate.Value.ToString("MM/dd/yyyy HH:mm:ss") : string.Empty;
                        //Sheet.Cells[i, 4].Value = item.ResumeLink;
                        Sheet.Cells[i, 11].Value = item.IRFId;
                        //Sheet.Cells[i, 8].Value = item.InvitationSentOnDate.HasValue ? item.InvitationSentOnDate.Value.ToString() : string.Empty;
                        Sheet.Cells[i, 12].Value = item.FirstScheduledOnDate.HasValue ? item.FirstScheduledOnDate.Value.ToString("MM/dd/yyyy HH:mm:ss") : string.Empty;
                        Sheet.Cells[i, 13].Value = item.interviewDate.HasValue ? item.interviewDate.Value.ToString("MM/dd/yyyy HH:mm:ss") : string.Empty;
                        Sheet.Cells[i, 17].Value = item.CancelledBy;
                        Sheet.Cells[i, 18].Value = item.Cancellationsource;
                        if (item.CandidateLatestStatus.ToUpper().Contains("SELECTED") || item.CandidateLatestStatus.ToUpper().Contains("REJECTED"))
                        {
                            Sheet.Cells[i, 19].Value = await GenerateExecutiveReportURl("https://portal.risebird.io/", item.id, item.Irfintid);
                        }
                        Sheet.Cells[i, 21].Value = item.NoOfInterviews;
                        if (item.confirmationStatus == "Cancelled")
                        {
                            Sheet.Cells[i, 22].Value = "Cancelled";
                        }
                        else
                        {
                            Sheet.Cells[i, 22].Value = item.MeetingStatus ?? string.Empty;
                        }
                        Sheet.Cells[i, 23].Value = !string.IsNullOrWhiteSpace(item.Requestor) ? item.Requestor.Split("@").LastOrDefault() : string.Empty;
                        Sheet.Cells[i, 24].Value = item.Heuristicsscore;
                        //Sheet.Cells[i, 11].Value = item.confirmationStatus ?? string.Empty;
                        //Sheet.Cells[i, 13].Value = item.InterviewStatus ?? string.Empty;
                        //Sheet.Cells[i, 14].Value = item.completedDate.HasValue ? item.completedDate.Value.ToString() : string.Empty;
                        //Sheet.Cells[i, 15].Value = item.VideoLink;
                        //Sheet.Cells[i, 16].Value = item.completedDate;
                        //Sheet.Cells[i, 18].Value = item.Cancellationsource;
                        //Sheet.Cells[i, 20].Value = item.InterviewerCodedName ?? string.Empty;
                        //Sheet.Cells[i, 22].Value = item.InterviewerComment;
                        i++;
                    }

                    log.LogInformation($"C# Timer trigger CandidateStatusNotifierTurning without cold data parsed in excel sheet at: {DateTime.Now}");

                    foreach (var cold in candidateDatawithCold)
                    {

                        Sheet.Cells[i, 1].Value = cold.candidateName;
                        Sheet.Cells[i, 2].Value = cold.candidateEmail;
                        Sheet.Cells[i, 3].Value = cold.JDTiltle;
                        Sheet.Cells[i, 4].Value = cold.ClientHrContact;
                        Sheet.Cells[i, 5].Value = cold.AddedOnDate.HasValue ? cold.AddedOnDate.Value.ToString("MM/dd/yyyy HH:mm:ss") : string.Empty;
                        if (cold.NoOfInterviews > 0)
                        {
                            Sheet.Cells[i, 6].Value = "Scheduled";
                            Sheet.Cells[i, 21].Value = cold.NoOfInterviews;
                        }
                        else
                        {
                            Sheet.Cells[i, 6].Value = "Not Yet Scheduled";
                        }
                        Sheet.Cells[i, 7].Value = cold.MobileNumber ?? string.Empty;
                        Sheet.Cells[i, 8].Value = cold.CurrentStatus;
                        Sheet.Cells[i, 9].Value = cold.CurrentStatusDate.HasValue ? cold.CurrentStatusDate.Value.ToString("MM/dd/yyyy HH:mm:ss") : string.Empty;
                        Sheet.Cells[i, 11].Value = cold.IRFId;
                        Sheet.Cells[i, 12].Value = cold.FirstScheduledOnDate.HasValue ? cold.FirstScheduledOnDate.Value.ToString("MM/dd/yyyy HH:mm:ss") : string.Empty;
                        Sheet.Cells[i, 13].Value = cold.interviewDate.HasValue ? cold.interviewDate.Value.ToString("MM/dd/yyyy HH:mm:ss") : string.Empty;
                        Sheet.Cells[i, 22].Value = cold.CandidateLatestStatus;
                        Sheet.Cells[i, 23].Value = !string.IsNullOrWhiteSpace(cold.Requestor) ? cold.Requestor.Split("@").LastOrDefault() : string.Empty;
                        Sheet.Cells[i, 24].Value = cold.Heuristicsscore;
                        i++;
                    }
                    log.LogInformation($"C# Timer trigger CandidateStatusNotifierTurning with cold data parsed in excel sheet at: {DateTime.Now}");
                }

                fileName = fileName.Replace(" ", "_");
                await _notificationHelper.SendCandidateLastStatusMail(mailList.ToList(), null, Convert.ToBase64String(Ep.GetAsByteArray()), fileName, true);

                log.LogInformation($"C# Timer trigger CandidateStatusNotifierTurning with notification mail sent at: {DateTime.Now}");
                log.LogInformation($"C# Timer trigger CandidateStatusNotifierTurning completed at: {DateTime.Now}");
            }
            catch (Exception ex)
            {
                log.LogInformation($"C# Timer trigger CandidateStatusNotifierTurning with error at: {ex}");
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }
        public async Task<string> GenerateExecutiveReportURl(string portalurl, int interviewid, int irfid)
        {
            var meetingid = await _encryptionHelper.EncryptIntegerValue(interviewid, "ExecutiveReport");
            var irf = await _encryptionHelper.EncryptIntegerValue(irfid, "ExecutiveReport");
            meetingid = meetingid.Replace("+", "_");
            irf = irf.Replace("+", "_");
            var reportlink = string.Empty;
            reportlink = portalurl + "Interview/ExecutiveReport?meetingid=" + meetingid + "&irf=" + irf;
            return reportlink;
        }
    }
}
