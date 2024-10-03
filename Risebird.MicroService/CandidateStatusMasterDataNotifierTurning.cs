using Aspose.Html;
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
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.MicroService
{
    public class CandidateStatusMasterDataNotifierTurning
    {
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly INotificationHelper _notificationHelper;
        private readonly IIRFService _iRFService;
        private readonly ICandidateService _candidateService;

        private readonly string connectionString;

        public CandidateStatusMasterDataNotifierTurning(IConfiguration configuration, IIRFService iRFService, ICandidateService candidateService, IApplicationErrorLogger applicationErrorLogger, INotificationHelper notificationHelper)
        {
            _configuration = configuration;
            _candidateService = candidateService;
            _applicationErrorLogger = applicationErrorLogger;
            _iRFService = iRFService;
            _notificationHelper = notificationHelper;
            connectionString = _configuration["ConnectionString:DBConnectionString"];

        }

        [FunctionName("CandidateStatusMasterDataNotifierTurning")]
        public async Task Run([TimerTrigger("0 0 5 * * *")] TimerInfo myTimer, ILogger log)//0 0 0 * * *
        {
            try
            {
                log.LogInformation($"C# Timer trigger CandidateStatusMasterDataNotifierTurning executed at: {DateTime.Now}");
                ExcelPackage Ep = new ExcelPackage();

                ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("MasterCandidate Report");
                string fileName = "Risebird_" + DateTime.Now.ToString("dd/MM/yyyy") + "_MasterCandidateUpdates_.xlsx";

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

                Sheet.Cells[1, 13].Value = "Completed On";

                Sheet.Cells[1, 14].Value = "Latest Status";

                Sheet.Cells[1, 15].Value = "Client Hr";
                Sheet.Cells[1, 16].Value = "Interviewer(Encoded)";
                int colNumber = 16;


                Sheet.Cells[1, ++colNumber].Value = "Report Link";
                //Sheet.Cells[1, 17].Value = "Pb Report Link";

                Sheet.Cells[1, ++colNumber].Value = "Communication Score";

              
                int j = colNumber;
             
                {
                    Sheet.Cells[1, ++j].Value = $"Tech Score";
                }
                Sheet.Cells[1, ++j].Value = $"Interview Status";
                Sheet.Cells[1, ++j].Value = $"Interviewer Comment";
        

                //var mailList = new List<string>() { "prem@risebird.io", 
                //    "amika.s@turing.com", 
                //    "annu.s@turing.com", 
                //    "ashutosh@risebird.io",
                //    "danijel.j@turing.com",
                //    "neha.khaitan@turing.com",
                //    "parth.gohel@turing.com",
                //    "priyanka.k@turing.com",
                //    "soumya.m@turing.com" };
                var mailList = new List<string>() { "prem@risebird.io",
                    "ashutosh@risebird.io",};
                var irfids = await _iRFService.GetIRFList(177);
                foreach (var irfId in irfids)
                {


                    var model = await _candidateService.GetIRFCandidateStatsForExcelDownLoad(irfId);
                    if (model == null)
                    {
                        model = new List<CandidateExcelData>();
                    }
                    model = model.Select(x => { x.MeetingStatus = !string.IsNullOrWhiteSpace(x.MeetingStatus) ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty; x.InterviewStatus = !string.IsNullOrWhiteSpace(x.InterviewStatus) ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty; x.confirmationStatus = !string.IsNullOrWhiteSpace(x.confirmationStatus) ? ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription() : string.Empty; return x; }).ToList();
                    log.LogInformation($"C# Timer trigger CandidateStatusMasterDataNotifierTurning data extracted for IRF {irfId} at: {DateTime.Now}");
                    var skillslist = model?.FirstOrDefault().TechSkillReportCollection.Select(x => x.SkillName).Distinct();
                    var skillsTwolist = model?.FirstOrDefault().TechSkillReportTwoCollection?.Select(x => x.SkillName).Distinct();

                    string jdtitle = string.Empty;
                    jdtitle = skillslist.FirstOrDefault();

                    string jdtitleTwo = string.Empty;
                    jdtitleTwo = skillsTwolist.FirstOrDefault();
                    int? clientId = model?.FirstOrDefault()?.ClientId;

                    



                    int i = 2;
                    if (model.Count() > 0)
                    {
                        foreach (var item in model.Where(x => x.CandidateType == CandidateSingleSourceTruthTypes.Completed))
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

                            Sheet.Cells[i, 13].Value = item.completedDate.HasValue ? item.completedDate.Value.ToString() : string.Empty;

                            //if (item.ClientId == 177)
                            //{
                            //    if (item.isQcDone.HasValue && item.isQcDone.Value == true)
                            //    {
                            //        Sheet.Cells[i, 14].Value = item.CandidateLatestStatus;
                            //    }
                            //    else
                            //    {
                            //        Sheet.Cells[i, 14].Value = "QCPending(" + item.CandidateLatestStatus + ")";
                            //    }
                            //}
                            //else
                            //{
                            //    Sheet.Cells[i, 14].Value = item.CandidateLatestStatus;
                            //}

                            Sheet.Cells[i, 14].Value = item.CandidateLatestStatus;

                            Sheet.Cells[i, 15].Value = item.ClientHrContact;
                            Sheet.Cells[i, 16].Value = item.InterviewerCodedName ?? string.Empty;
                            int colDataNumber = 16;

                            var executiveurl = "https://portal.risebird.io/Interview/ExecutiveReport?interviewId=" + item.id + "&irfId=" + irfId + "&isClient=true";
                            try
                            {
                                ++colDataNumber;
                                Sheet.Cells[i, colDataNumber].Hyperlink = new Uri(executiveurl);
                                Sheet.Cells[i, colDataNumber].Value = "Executive Report";
                                Sheet.Cells[i, colDataNumber].Style.Font.UnderLine = true;
                                Sheet.Cells[i, colDataNumber].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                            }
                            catch (Exception)
                            {
                                Sheet.Cells[i, colDataNumber].Value = "No Report";
                            }

                            //var publicReportLink = "https://portal.risebird.io/Interview/ExecutiveReport?interviewId=" + item.id + "&irfId=" + irfId + "&isClient=true";
                            //try
                            //{
                            //    Sheet.Cells[i, 17].Hyperlink = new Uri(publicReportLink, UriKind.Absolute);
                            //    Sheet.Cells[i, 17].Value = "Report";
                            //    Sheet.Cells[i, 17].Style.Font.UnderLine = true;
                            //    Sheet.Cells[i, 17].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                            //}
                            //catch (Exception)
                            //{
                            //    Sheet.Cells[i, 17].Value = "No Report";
                            //}
                            Sheet.Cells[i, ++colDataNumber].Value = item.CommunicationalProficiency != 0 ? ((item.CommunicationalProficiency * 10).ToString()) + "%" : "NA";
                            var totalskillscore = 0.0;
                            var totalskillscount = 0;
                            int jV = colDataNumber;
                           
                            
                            {
                                foreach (var skillvalue in skillslist)
                                {

                                    var skilldata = item.TechSkillReportCollection.Where(x => x.SkillName == skillvalue).FirstOrDefault();
                                    if (skilldata != null)
                                    {
                                        totalskillscount++;
                                        totalskillscore += (skilldata.WeightageActual);// * 100 / skilldata.WeightageRequired);
                                    }
                                }
                                Sheet.Cells[i, ++jV].Value = totalskillscore; //  / totalskillscount;
                            }
                            //if (item.ClientId == 177)
                            //{
                            //    if (item.isQcDone.HasValue && item.isQcDone.Value == true)
                            //    {
                            //        Sheet.Cells[i, ++jV].Value = item.CandidateLatestStatus;
                            //    }
                            //    else
                            //    {
                            //        Sheet.Cells[i, ++jV].Value = "QCPending(" + item.CandidateLatestStatus + ")";
                            //    }
                            //}
                            //else
                            //{
                            //    Sheet.Cells[i, ++jV].Value = item.CandidateLatestStatus;
                            //}
                            Sheet.Cells[i, ++jV].Value = item.CandidateLatestStatus;
                            if (!string.IsNullOrWhiteSpace(item.InterviewerComment))
                            {
                                var interviewerComent = System.Net.WebUtility.HtmlDecode(item.InterviewerComment);

                                var document = new HTMLDocument(interviewerComent);
                                var intComment = GetContent(document);
                                Sheet.Cells[i, ++jV].Value = intComment;
                            }
                           

                            i++;
                        }


                        //return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                    }
                    else
                    {
                        ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                        Rng.Value = "No data available!";
                        // return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                    }
                }
                    log.LogInformation($"C# Timer trigger CandidateStatusNotifierTurning completed at: {DateTime.Now}");
                fileName = fileName.Replace(" ", "_");
                await _notificationHelper.SendCandidateLastStatusMail(mailList.ToList(), null, Convert.ToBase64String(Ep.GetAsByteArray()), fileName,true);
                log.LogInformation($"C# Timer trigger CandidateStatusNotifierTurning mail sent at: {DateTime.Now}");
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                log.LogError($" CandidateStatusMasterDataNotifierTurning failed to execute: {ex}");
                throw ex;
            }
        }
        public string GetContent(Aspose.Html.Dom.Node node)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var n in node.ChildNodes)
            {
                if (n.NodeType == Aspose.Html.Dom.Node.ELEMENT_NODE)
                    sb.Append(GetContent(n));
                else if (n.NodeType == Aspose.Html.Dom.Node.TEXT_NODE)
                    sb.Append(n.NodeValue);
            }
            return sb.ToString();
        }

    }
}
