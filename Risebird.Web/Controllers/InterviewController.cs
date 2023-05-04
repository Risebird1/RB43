using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using OfficeOpenXml;
using RestSharp;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using Risebird.Web.Controllers.AzureMediaservice;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using X.PagedList;

namespace Risebird.Web.Controllers
{

    public class InterviewController : Controller
    {
        private readonly IMeetingService _meetingService;
        private readonly ISettingsService _settingsService;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly ISkillService _skillService;
        private readonly IIRFService _iRFService;
        private readonly IIRFRepository _iRFRepository;
        private readonly INotificationHelper _notificationHelper;
        private readonly IAzureFaceApi _azureFaceApi;
        private readonly IConfiguration _configuration;
        private readonly IUserRepository _userRepository;
        private readonly IUserService _userService;
        private readonly IWhatsAppHelper _whatsAppHelper;
        private readonly ICandidateService _candidateService;
        private readonly IFileMimeTypeHelper _fileMimeTypeHelper;
        private readonly IInterviewSchedulerService _interviewSchedulerservice;
        private readonly IZoomHelper _zoomHelper;
        private readonly IInterviewerService _interviewerService;
        private readonly IInterviewSchedulerRepository _interviewSchedulerRepository;

        //private static List<MeetingVM> _modelDetails;
        //private static List<Meeting> _modelDetail;
        //private static string Searchvalue;


        private readonly HttpClient _httpClient;
        private readonly CancellationToken _cancellationToken;
        public InterviewController(IMeetingService meetingService, IApplicationErrorLogger applicationErrorLogger, IAzureStorageHelper azureStorageHelper,
            ISkillService skillService, IIRFRepository iRFRepository, IIRFService iRFService, ISettingsService settingsService,
            INotificationHelper notificationHelper, IAzureFaceApi azureFaceApi, IConfiguration configuration, HttpClientHelper factory,
            IUserRepository userRepository, IUserService userService, IWhatsAppHelper whatsAppHelper, ICandidateService candidateService,
            IFileMimeTypeHelper fileMimeTypeHelper, IInterviewSchedulerService interviewSchedulerservice,
            IZoomHelper zoomHelper, IInterviewerService interviewerService, IInterviewSchedulerRepository interviewSchedulerRepository)
        {
            _meetingService = meetingService;
            _applicationErrorLogger = applicationErrorLogger;
            _azureStorageHelper = azureStorageHelper;
            _skillService = skillService;
            _iRFRepository = iRFRepository;
            _iRFService = iRFService;
            _settingsService = settingsService;
            _notificationHelper = notificationHelper;
            _azureFaceApi = azureFaceApi;
            _configuration = configuration;
            _userRepository = userRepository;
            _userService = userService;
            _httpClient = factory.GetHttpClient();
            _whatsAppHelper = whatsAppHelper;
            _candidateService = candidateService;
            _fileMimeTypeHelper = fileMimeTypeHelper;
            _interviewSchedulerservice = interviewSchedulerservice;
            _zoomHelper = zoomHelper;
            _interviewerService = interviewerService;
            _interviewSchedulerRepository = interviewSchedulerRepository;
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Index(string type)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {

                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                    ViewBag.emailid = emailId;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Finance" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller")
                    {
                        ViewBag.profile = profileName;
                        if (TempData["SuccessErrorMessageInterview"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageInterview"].ToString();
                            TempData.Remove("SuccessErrorMessageInterview");
                        }
                        List<MeetingVM> modelList = new List<MeetingVM>();
                        if (HttpContext.Session.GetString("Interviewfilters") != null)
                        {
                            var filter = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("Interviewfilters"));
                            if (filter.StartDate != null && filter.EndDate != null)
                            {
                                modelList = await _meetingService.GetAllMeetingList(filter.StartDate.Value, filter.EndDate.Value.AddDays(1), emailId);
                            }
                            modelList = modelList.Select(x =>
                            {
                                x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription();
                                //x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty;
                                x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty;
                                x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription();
                                //x.QCStatusStatusDesc = x.QCStatus.HasValue ? ((QCStatus)x.QCStatus).GetDescription() : string.Empty;
                                return x;
                            }).ToList();

                            foreach (var item in modelList)
                            {
                                item.NextLevelApprovalStat = item.IsApproved.HasValue ? ((NextLevelApprovalStatus)item.IsApproved).GetDescription() : string.Empty;

                                if (item.confirmationStatus.Trim() != ReceiveMeetingStatus.Cancelled.GetDescription())
                                {
                                    if (item.interviewDate < DateTime.Now && item.MeetingStatusStatusDesc == Risebird.Entities.Enum.MeetingStatus.Scheduled.GetDescription())
                                    {
                                        item.MeetingStatusStatusDesc = "Status Pending";

                                    }
                                    //else if (item.MeetingStatus.HasValue && (item.MeetingStatus.Value == (int)MeetingStatus.DroppedSelected || item.MeetingStatus.Value == (int)MeetingStatus.DroppedRejected || item.MeetingStatus.Value == (int)MeetingStatus.DroppedBorderLine))
                                    //{
                                    //    item.MeetingStatusStatusDesc = "Completed";
                                    //}
                                }
                                else
                                {
                                    item.MeetingStatusStatusDesc = string.Empty;

                                }

                                //interview status
                                if (item.InterviewStatus.HasValue)
                                {
                                    item.intervieweStatusStatDesc = ((InterviewStatus)(item.InterviewStatus)).GetDescription();
                                }
                            }
                            //modelList.ForEach(x => x.NextLevelApprovalStat = x.IsApproved.HasValue ? ((NextLevelApprovalStatus)x.IsApproved).GetDescription() : string.Empty);

                            filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = modelList.Select(x => x.JDTiltle.Split("|")[0]).ToArray() : filter.PrimarySkill = filter.PrimarySkill;
                            filter.RFID = filter.RFID == null ? filter.RFID = modelList.Select(x => x.IRFId).ToArray() : filter.RFID = filter.RFID;
                            filter.ClientHR = filter.ClientHR == null ? filter.ClientHR = modelList.Select(x => x.CandidateClientHR).ToArray() : filter.ClientHR = filter.ClientHR;
                            filter.ConfirmationStatus = filter.ConfirmationStatus == null ? filter.ConfirmationStatus = modelList.Select(x => x.confirmationStatus).ToArray() : filter.ConfirmationStatus = filter.ConfirmationStatus;
                            filter.MeetingStatus = filter.MeetingStatus == null ? filter.MeetingStatus = modelList.Select(x => x.MeetingStatusStatusDesc).ToArray() : filter.MeetingStatus = filter.MeetingStatus;
                            filter.InterviewStatus = filter.InterviewStatus == null ? filter.InterviewStatus = modelList.Select(x => x.intervieweStatusStatDesc).ToArray() : filter.InterviewStatus = filter.InterviewStatus;
                            filter.Interviewer = filter.Interviewer == null ? filter.Interviewer = modelList.Select(x => x.interviewer).ToArray() : filter.Interviewer = filter.Interviewer;
                            filter.ClientName = filter.ClientName == null ? filter.ClientName = modelList.Select(x => x.ClientName).ToArray() : filter.ClientName = filter.ClientName;
                            filter.Client = filter.Client == null ? filter.Client = modelList.Select(x => x.Client).ToArray() : filter.Client = filter.Client;
                            filter.IsCandidateResponsed = filter.IsCandidateResponsed == null ? filter.IsCandidateResponsed = modelList.Select(x => x.IsCandidateResponsed).ToArray() : filter.IsCandidateResponsed = filter.IsCandidateResponsed;
                            filter.TemplateName = filter.TemplateName == null ? filter.TemplateName = modelList.Select(x => x.TemplateName).ToArray() : filter.TemplateName;
                            filter.AadhaarVerified = filter.AadhaarVerified == null ? filter.AadhaarVerified = modelList.Select(x => x.AadhaarVerified).ToArray() : filter.AadhaarVerified = filter.AadhaarVerified;

                            modelList = modelList.Where(c => filter.PrimarySkill != null && filter.PrimarySkill.Contains(c.JDTiltle.Split("|")[0])
                            && filter.RFID.Contains(c.IRFId)
                            && ((filter.ClientHR != null && filter.ClientHR.Count() > 0) ? filter.ClientHR.Contains(c.CandidateClientHR) : c.CandidateClientHR == null)
                            && ((filter.ConfirmationStatus != null && filter.ConfirmationStatus.Count() > 0) ? filter.ConfirmationStatus.Contains(c.confirmationStatus) : c.confirmationStatus == null)
                            && ((filter.MeetingStatus != null && filter.MeetingStatus.Count() > 0) ? filter.MeetingStatus.Contains(c.MeetingStatusStatusDesc) : c.MeetingStatusStatusDesc == string.Empty)
                            && ((filter.InterviewStatus != null && filter.InterviewStatus.Count() > 0) ? filter.InterviewStatus.Contains(c.intervieweStatusStatDesc) : c.intervieweStatusStatDesc == string.Empty)
                            && ((filter.Interviewer != null && filter.Interviewer.Count() > 0) ? filter.Interviewer.Contains(c.interviewer) : c.interviewer == null)
                            && ((filter.ClientName != null && filter.ClientName.Count() > 0) ? filter.ClientName.Contains(c.ClientName) : c.ClientName == null)
                            && ((filter.Client != null && filter.Client.Count() > 0) ? filter.Client.Contains(c.Client) : c.Client == null)
                            && ((filter.IsCandidateResponsed != null && filter.IsCandidateResponsed.Count() > 0) ? filter.IsCandidateResponsed.Contains(c.IsCandidateResponsed) : c.IsCandidateResponsed == false)
                            && ((filter.TemplateName != null && filter.TemplateName.Count() > 0) ? filter.TemplateName.Contains(c.TemplateName) : c.TemplateName == null)
                            && ((filter.AadhaarVerified != null && filter.AadhaarVerified.Count() > 0) ? filter.AadhaarVerified.Contains(c.AadhaarVerified) : c.AadhaarVerified == false)).ToList();

                        }
                        else
                        {
                            if (type == "RedoInterviews")
                            {
                                modelList = await _meetingService.GetAllMeetingList(DateTime.Now.AddDays(-30), DateTime.Now.Date.AddDays(1), emailId);
                            }
                            else
                            {
                                modelList = await _meetingService.GetAllMeetingList(DateTime.Now.Date, DateTime.Now.Date.AddDays(1), emailId);
                            }
                            modelList = modelList.Select(x =>
                            {
                                x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription();
                                //x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty; 
                                x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty; x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription();
                                //x.QCStatusStatusDesc = x.QCStatus.HasValue ? ((QCStatus)x.QCStatus).GetDescription() : string.Empty; 
                                return x;
                            }).ToList();
                            foreach (var item in modelList)
                            {
                                item.NextLevelApprovalStat = item.IsApproved.HasValue ? ((NextLevelApprovalStatus)item.IsApproved).GetDescription() : string.Empty;

                                if (item.confirmationStatus.Trim() != ReceiveMeetingStatus.Cancelled.GetDescription())
                                {
                                    if (item.interviewDate < DateTime.Now && item.MeetingStatusStatusDesc == Risebird.Entities.Enum.MeetingStatus.Scheduled.GetDescription())
                                    {
                                        item.MeetingStatusStatusDesc = "Status Pending";

                                    }
                                    //else if (item.MeetingStatus.HasValue && (item.MeetingStatus.Value == (int)MeetingStatus.DroppedSelected || item.MeetingStatus.Value == (int)MeetingStatus.DroppedRejected || item.MeetingStatus.Value == (int)MeetingStatus.DroppedBorderLine))
                                    //{
                                    //    item.MeetingStatusStatusDesc = "Completed";
                                    //}
                                }
                                else
                                {
                                    item.MeetingStatusStatusDesc = string.Empty;

                                }

                                if (item.InterviewStatus.HasValue)
                                {
                                    item.intervieweStatusStatDesc = ((InterviewStatus)(item.InterviewStatus)).GetDescription();
                                }
                            }
                            //modelList.ForEach(x => x.NextLevelApprovalStat = x.IsApproved.HasValue ? ((NextLevelApprovalStatus)x.IsApproved).GetDescription() : string.Empty);
                        }
                        if (type == "RedoInterviews")
                        {
                            ViewBag.Type = "RedoInterviews";
                            modelList = modelList.Where(x => x.RedoInterview == true).ToList();
                        }
                        else if (type == "Interviews")
                        {
                            ViewBag.Type = "Interviews";
                            modelList = modelList.Where(x => string.IsNullOrWhiteSpace(x.TemplateName) && x.RedoInterview == false).ToList();
                        }
                        else
                        {
                            ViewBag.Type = "TemplateInterviews";
                            modelList = modelList.Where(x => !string.IsNullOrWhiteSpace(x.TemplateName) && x.RedoInterview == false).ToList();
                        }
                        return View(modelList);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }

        }

        [Authorize]
        [HttpGet]
        public async Task<FileContentResult> InterviewsExporttoExcel(string type)
        {
            string userEmailId = string.Empty;
            ExcelPackage Ep = new ExcelPackage();
            ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Interviews");
            string fileName = "Risebird" + DateTime.Now.ToString("dd/MM/yyyy") + "_Interviews" + ".xlsx";
            try
            {
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                List<MeetingVM> modelList = new List<MeetingVM>();
                if (HttpContext.Session.GetString("Interviewfilters") != null)
                {
                    var filter = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("Interviewfilters"));
                    if (filter.StartDate != null && filter.EndDate != null)
                    {
                        modelList = await _meetingService.GetAllMeetingList(filter.StartDate.Value, filter.EndDate.Value.AddDays(1), emailid);
                    }
                    modelList = modelList.Select(x =>
                    {
                        x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription();
                        //x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty;
                        x.MeetingStatusStatusDesc
                        = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty;
                        x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription();
                        //x.QCStatusStatusDesc = x.QCStatus.HasValue ? ((QCStatus)x.QCStatus).GetDescription() : string.Empty;
                        return x;
                    }).ToList();
                    //modelList.ForEach(x => x.NextLevelApprovalStat = x.IsApproved.HasValue ? ((NextLevelApprovalStatus)x.IsApproved).GetDescription() : string.Empty);

                    foreach (var item in modelList)
                    {
                        item.NextLevelApprovalStat = item.IsApproved.HasValue ? ((NextLevelApprovalStatus)item.IsApproved).GetDescription() : string.Empty;

                        if (item.confirmationStatus.Trim() != ReceiveMeetingStatus.Cancelled.GetDescription())
                        {
                            if (item.interviewDate < DateTime.Now && item.MeetingStatusStatusDesc == Risebird.Entities.Enum.MeetingStatus.Scheduled.GetDescription())
                            {
                                item.MeetingStatusStatusDesc = "Status Pending";

                            }
                        }
                        else
                        {
                            item.MeetingStatusStatusDesc = string.Empty;
                        }

                        //interview status
                        if (item.InterviewStatus.HasValue)
                        {
                            item.intervieweStatusStatDesc = ((InterviewStatus)(item.InterviewStatus)).GetDescription();
                        }
                    }

                    filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = modelList.Select(x => x.JDTiltle.Split("|")[0]).ToArray() : filter.PrimarySkill = filter.PrimarySkill;
                    filter.RFID = filter.RFID == null ? filter.RFID = modelList.Select(x => x.IRFId).ToArray() : filter.RFID = filter.RFID;
                    filter.ClientHR = filter.ClientHR == null ? filter.ClientHR = modelList.Select(x => x.CandidateClientHR).ToArray() : filter.ClientHR = filter.ClientHR;
                    filter.ConfirmationStatus = filter.ConfirmationStatus == null ? filter.ConfirmationStatus = modelList.Select(x => x.confirmationStatus).ToArray() : filter.ConfirmationStatus = filter.ConfirmationStatus;
                    filter.MeetingStatus = filter.MeetingStatus == null ? filter.MeetingStatus = modelList.Select(x => x.MeetingStatusStatusDesc).ToArray() : filter.MeetingStatus = filter.MeetingStatus;
                    filter.InterviewStatus = filter.InterviewStatus == null ? filter.InterviewStatus = modelList.Select(x => x.intervieweStatusStatDesc).ToArray() : filter.InterviewStatus = filter.InterviewStatus;
                    filter.Interviewer = filter.Interviewer == null ? filter.Interviewer = modelList.Select(x => x.interviewer).ToArray() : filter.Interviewer = filter.Interviewer;
                    filter.Client = filter.Client == null ? filter.Client = modelList.Select(x => x.Client).ToArray() : filter.Client = filter.Client;
                    filter.ClientName = filter.ClientName == null ? filter.ClientName = modelList.Select(x => x.ClientName).ToArray() : filter.ClientName = filter.ClientName;
                    filter.TemplateName = filter.TemplateName == null ? filter.TemplateName = modelList.Select(x => x.TemplateName).ToArray() : filter.TemplateName;

                    modelList = modelList.Where(c => filter.PrimarySkill.Contains(c.JDTiltle.Split("|")[0])
                    && filter.RFID.Contains(c.IRFId)
                    && (filter.ClientHR.Count() > 0 ? filter.ClientHR.Contains(c.CandidateClientHR) : c.CandidateClientHR == null)
                    && (filter.ConfirmationStatus.Count() > 0 ? filter.ConfirmationStatus.Contains(c.confirmationStatus) : c.confirmationStatus == null)
                    && (filter.MeetingStatus.Count() > 0 ? filter.MeetingStatus.Contains(c.MeetingStatusStatusDesc) : c.MeetingStatusStatusDesc == string.Empty)
                    && (filter.InterviewStatus.Count() > 0 ? filter.InterviewStatus.Contains(c.intervieweStatusStatDesc) : c.intervieweStatusStatDesc == string.Empty)
                    && (filter.Interviewer.Count() > 0 ? filter.Interviewer.Contains(c.interviewer) : c.interviewer == null)
                    && ((filter.ClientName != null && filter.ClientName.Count() > 0) ? filter.ClientName.Contains(c.ClientName) : c.ClientName == null)
                    && ((filter.Client != null && filter.Client.Count() > 0) ? filter.Client.Contains(c.Client) : c.Client == null)
                    && ((filter.TemplateName != null && filter.TemplateName.Count() > 0) ? filter.TemplateName.Contains(c.TemplateName) : c.TemplateName == null)).ToList();

                    if (modelList.Count == 0 && filter.StartDate != null && filter.EndDate != null)
                    {
                        modelList = await _meetingService.GetAllMeetingList(filter.StartDate.Value, filter.EndDate.Value.AddDays(1), emailid);
                        modelList = modelList.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty; x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty; x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); x.QCStatusStatusDesc = x.QCStatus.HasValue ? ((QCStatus)x.QCStatus).GetDescription() : string.Empty; return x; }).ToList();
                        modelList.ForEach(x => x.NextLevelApprovalStat = x.IsApproved.HasValue ? ((NextLevelApprovalStatus)x.IsApproved).GetDescription() : string.Empty);
                        modelList = modelList.Where(t => t.interviewDate.Date >= filter.StartDate && t.interviewDate.Date <= filter.EndDate).ToList();
                    }
                }
                else
                {
                    if (type == "RedoInterviews")
                    {
                        modelList = await _meetingService.GetAllMeetingList(DateTime.Now.AddDays(-30), DateTime.Now.Date.AddDays(1), emailid);
                    }
                    else
                    {
                        modelList = await _meetingService.GetAllMeetingList(DateTime.Now.Date, DateTime.Now.Date.AddDays(1), emailid);
                    }
                    modelList = modelList.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty; x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty; x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); x.QCStatusStatusDesc = x.QCStatus.HasValue ? ((QCStatus)x.QCStatus).GetDescription() : string.Empty; return x; }).ToList();
                    modelList.ForEach(x => x.NextLevelApprovalStat = x.IsApproved.HasValue ? ((NextLevelApprovalStatus)x.IsApproved).GetDescription() : string.Empty);
                }
                if (type == "RedoInterviews")
                {
                    modelList = modelList.Where(x => x.RedoInterview == true).ToList();
                }
                else if (type == "Interviews")
                {
                    modelList = modelList.Where(x => string.IsNullOrWhiteSpace(x.TemplateName) && x.RedoInterview == false).ToList();
                }
                else
                {
                    modelList = modelList.Where(x => !string.IsNullOrWhiteSpace(x.TemplateName) && x.RedoInterview == false).ToList();
                }
                Sheet.Cells["A1:AI1"].Style.Font.Bold = true;
                Sheet.Cells["A1:AI1"].Style.Font.Size = 12;
                Sheet.Cells["A1:AI1"].Style.WrapText = true;
                int startColumn = 1; int endColumn = 40;
                for (int a = startColumn; a <= endColumn; a++)
                {
                    Sheet.Column(a).Width = 40;
                }
                Sheet.Cells[1, 1].Value = "IRF ID";
                Sheet.Cells[1, 2].Value = "Interview Date";
                Sheet.Cells[1, 3].Value = "Interview Slot";
                Sheet.Cells[1, 4].Value = "Candidate Name";
                Sheet.Cells[1, 5].Value = "Candidate Email";
                Sheet.Cells[1, 6].Value = "Primary Skill";
                Sheet.Cells[1, 7].Value = "Interviewer";
                Sheet.Cells[1, 8].Value = "Confirmation Status";
                Sheet.Cells[1, 9].Value = "Meeting Status";
                Sheet.Cells[1, 10].Value = "Interview Status";
                Sheet.Cells[1, 11].Value = "Resume";
                Sheet.Cells[1, 12].Value = "JD Link";
                Sheet.Cells[1, 13].Value = "Zoom Link";
                Sheet.Cells[1, 14].Value = "Risebird HR";
                Sheet.Cells[1, 15].Value = "Candidate Contact";
                Sheet.Cells[1, 16].Value = "Client";
                Sheet.Cells[1, 17].Value = "Client HR";
                Sheet.Cells[1, 18].Value = "End Client Name";
                Sheet.Cells[1, 19].Value = "Video Link";
                Sheet.Cells[1, 20].Value = "Video Duration";
                Sheet.Cells[1, 21].Value = "Emergency Interview";
                Sheet.Cells[1, 22].Value = "Interviewer Comment";
                Sheet.Cells[1, 23].Value = "Staffing Agency";
                Sheet.Cells[1, 24].Value = "Interview Duration";
                Sheet.Cells[1, 25].Value = "Expected Joining Time";
                Sheet.Cells[1, 26].Value = "Preferred Location";
                Sheet.Cells[1, 27].Value = "Expected Salary";
                Sheet.Cells[1, 28].Value = "Current Employer";
                Sheet.Cells[1, 29].Value = "Approval Status";
                Sheet.Cells[1, 30].Value = "Approval Comments";
                Sheet.Cells[1, 31].Value = "Final Selection Status";
                Sheet.Cells[1, 32].Value = "Final Selection Comments";
                Sheet.Cells[1, 33].Value = "QC Interviewer";
                Sheet.Cells[1, 34].Value = "QC Status";
                Sheet.Cells[1, 35].Value = "Coordinating HR";
                Sheet.Cells[1, 36].Value = "Job Country";
                Sheet.Cells[1, 37].Value = "Billing Owner";
                if (profileName == "Client")
                {
                    Sheet.Cells[1, 38].Value = "Executive URL";
                }
                //ExcelHyperLink excelHyperLink = new ExcelHyperLink();
                int i = 2;
                //string LastAction = null; ;

                if (modelList.Count() > 0)
                {
                    foreach (var item in modelList)
                    {
                        Sheet.Cells[i, 1].Value = item.IRFId;
                        Sheet.Cells[i, 2].Value = item.interviewDate.ToString("MM/dd/yyyy HH:mm");
                        Sheet.Cells[i, 3].Value = item.interviewSlot;
                        Sheet.Cells[i, 4].Value = item.candidateName;
                        Sheet.Cells[i, 5].Value = item.candidateEmail;
                        Sheet.Cells[i, 6].Value = item.JDTiltle.Split("|")[0];
                        Sheet.Cells[i, 7].Value = item.interviewer;
                        Sheet.Cells[i, 8].Value = item.confirmationStatus;
                        if (item.confirmationStatus.Trim() != ReceiveMeetingStatus.Cancelled.GetDescription())
                        {
                            if (item.interviewDate < DateTime.Now && item.MeetingStatusStatusDesc == Risebird.Entities.Enum.MeetingStatus.Scheduled.GetDescription())
                            {
                                Sheet.Cells[i, 9].Value = "Status Pending";
                            }
                            else
                            {
                                Sheet.Cells[i, 9].Value = item.MeetingStatusStatusDesc;
                            }
                        }
                        else
                        {
                            Sheet.Cells[i, 9].Value = string.Empty;
                        }

                        if (!string.IsNullOrEmpty(item.intervieweStatusStatDesc))
                        {
                            Sheet.Cells[i, 10].Value = item.intervieweStatusStatDesc;
                        }
                        else
                        {
                            Sheet.Cells[i, 10].Value = string.Empty;

                        }

                        if (!string.IsNullOrEmpty(item.resumeLink))
                        {
                            try
                            {
                                Sheet.Cells[i, 11].Hyperlink = new Uri(item.resumeLink, UriKind.Absolute);
                                Sheet.Cells[i, 11].Value = "Resume";
                                Sheet.Cells[i, 11].Style.Font.UnderLine = true;
                                Sheet.Cells[i, 11].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                            }
                            catch (Exception)
                            {
                                Sheet.Cells[i, 11].Value = "No Resume";
                            }
                        }
                        else
                        {
                            Sheet.Cells[i, 11].Value = "No Resume";
                        }
                        if (!string.IsNullOrEmpty(item.JDLink))
                        {
                            try
                            {
                                Sheet.Cells[i, 12].Hyperlink = new Uri(item.JDLink, UriKind.Absolute);
                                Sheet.Cells[i, 12].Value = "JD Link";
                                Sheet.Cells[i, 12].Style.Font.UnderLine = true;
                                Sheet.Cells[i, 12].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                            }
                            catch (Exception)
                            {
                                Sheet.Cells[i, 12].Value = "No Link";
                            }
                        }
                        else
                        {
                            Sheet.Cells[i, 12].Value = "No Link";
                        }
                        if (!string.IsNullOrEmpty(item.zoomLink))
                        {
                            try
                            {
                                Sheet.Cells[i, 13].Hyperlink = new Uri(item.zoomLink, UriKind.Absolute);
                                Sheet.Cells[i, 13].Value = "Zoom Link";
                                Sheet.Cells[i, 13].Style.Font.UnderLine = true;
                                Sheet.Cells[i, 13].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                            }
                            catch (Exception)
                            {
                                Sheet.Cells[i, 13].Value = "No Link";
                            }
                        }
                        else
                        {
                            Sheet.Cells[i, 13].Value = "No Link";
                        }
                        Sheet.Cells[i, 14].Value = item.risebirdHR;
                        Sheet.Cells[i, 15].Value = item.MobileNumber;
                        Sheet.Cells[i, 16].Value = item.Client;
                        Sheet.Cells[i, 17].Value = item.CandidateClientHR;
                        Sheet.Cells[i, 18].Value = item.ClientName;
                        if (!string.IsNullOrEmpty(item.VideoLink))
                        {
                            try
                            {
                                Sheet.Cells[i, 19].Hyperlink = new Uri(item.VideoLink, UriKind.Absolute);
                                Sheet.Cells[i, 19].Value = "Video Link";
                                Sheet.Cells[i, 19].Style.Font.UnderLine = true;
                                Sheet.Cells[i, 19].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                                Sheet.Cells[i, 20].Value = item.VideoDuration == 0 ? string.Empty : item.VideoDuration.ToString();
                            }
                            catch (Exception)
                            {
                                Sheet.Cells[i, 19].Value = "No Video";
                                Sheet.Cells[i, 20].Value = "NA";
                            }
                        }
                        else
                        {
                            Sheet.Cells[i, 19].Value = "No Video";
                            Sheet.Cells[i, 20].Value = "NA";
                        }
                        Sheet.Cells[i, 21].Value = item.IsEmergency ? "Yes" : "No";
                        Sheet.Cells[i, 22].Value = item.InterviewerComment;
                        Sheet.Cells[i, 23].Value = item.staffingAgency;
                        Sheet.Cells[i, 24].Value = item.interviewDuration;
                        Sheet.Cells[i, 25].Value = item.joiningTime;
                        Sheet.Cells[i, 26].Value = item.preferredLocation;
                        Sheet.Cells[i, 27].Value = item.expectedPerHourRate;
                        Sheet.Cells[i, 28].Value = item.currentEmployerName;
                        Sheet.Cells[i, 29].Value = item.NextLevelApprovalStat ?? "";
                        Sheet.Cells[i, 30].Value = item.ApprovalComments ?? "Comments not submitted.";
                        Sheet.Cells[i, 31].Value = item.FinalSelectionStat ?? "";
                        Sheet.Cells[i, 32].Value = item.FinalComments ?? "Comments not submitted.";
                        Sheet.Cells[i, 33].Value = item.QCInterviewer;
                        Sheet.Cells[i, 34].Value = item.QCStatusStatusDesc;
                        Sheet.Cells[i, 35].Value = item.HRCoordinatorEmailId;
                        Sheet.Cells[i, 36].Value = item.JobCountry;
                        Sheet.Cells[i, 37].Value = item.BillingOwner;
                        if (profileName == "Client")
                        {
                            var executiveurl = "https://portal.risebird.io/Interview/ExecutiveReport?interviewId=" + item.Interviewid + "&irfId=" + item.irfIntId + "&isClient=true";
                            try
                            {
                                Sheet.Cells[i, 38].Hyperlink = new Uri(executiveurl, UriKind.Absolute);
                                Sheet.Cells[i, 38].Value = "Executive Report";
                                Sheet.Cells[i, 38].Style.Font.UnderLine = true;
                                Sheet.Cells[i, 38].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                            }
                            catch (Exception)
                            {
                                Sheet.Cells[i, 38].Value = "No Report";
                            }
                        }
                        i++;
                    }
                    return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
                else
                {
                    ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                    Rng.Value = "No data available in table !!!";
                    return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception)
            {
                ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                Rng.Value = "Data export failed !!!";
                return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MultipleCandidateInvites()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {

                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {

                        var modelList = _meetingService.MultipleCandidateInvites(emailid);
                        if (profileName == "Admin" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                        {
                            modelList.ForEach(x => x.IsAdmin = true);
                        }
                        modelList = modelList.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty; x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty; x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();

                        return View(modelList);
                    }
                    else
                    {
                        return RedirectToAction("Index", "Interview");
                    }
                }
                else
                {
                    return RedirectToAction("Index", "Interview");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index", "Interview");
            }

        }

        /// <summary>
        /// Export selected candidates data to excel
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet]
        public FileContentResult MultipleCandidateInvitesExporttoExcel()
        {
            string userEmailId = string.Empty;
            ExcelPackage Ep = new ExcelPackage();
            ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Multliple Candidate Invite Report");
            string fileName = "Risebird" + DateTime.Now.ToString("dd/MM/yyyy") + "_Multiple_Candidate_Invite_Report" + ".xlsx";
            try
            {
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");

                var selectedMeeting = new List<MeetingVM>();
                var modelList = _meetingService.MultipleCandidateInvites(emailid);

                modelList = modelList.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty; x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty; x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();

                Sheet.Cells["A1:P1"].Style.Font.Bold = true;
                Sheet.Cells["A1:P1"].Style.Font.Size = 12;
                Sheet.Cells["A1:P1"].Style.WrapText = true;
                int startColumn = 1; int endColumn = 14;
                for (int a = startColumn; a <= endColumn; a++)
                {
                    Sheet.Column(a).Width = 30;
                }
                Sheet.Cells[1, 1].Value = "Invite Count";
                Sheet.Cells[1, 2].Value = "Interview Date";
                Sheet.Cells[1, 3].Value = "Candidate Name";
                Sheet.Cells[1, 4].Value = "Candidate Email";
                Sheet.Cells[1, 5].Value = "Primary Skill";
                Sheet.Cells[1, 6].Value = "Interviewer";
                Sheet.Cells[1, 7].Value = "Confirmation Status";
                Sheet.Cells[1, 8].Value = "Meeting Status";
                Sheet.Cells[1, 9].Value = "Interview Status";
                Sheet.Cells[1, 10].Value = "IRF ID";
                Sheet.Cells[1, 11].Value = "Risebird HR";
                Sheet.Cells[1, 12].Value = "Candidate Contact";
                Sheet.Cells[1, 13].Value = "Client HR";
                Sheet.Cells[1, 14].Value = "Staffing Agenecy";
                //ExcelHyperLink excelHyperLink = new ExcelHyperLink();
                int i = 2;

                if (modelList.Count() > 0)
                {
                    foreach (var item in modelList)
                    {
                        Sheet.Cells[i, 1].Value = item.InviteCount;
                        Sheet.Cells[i, 2].Value = item.interviewDate.ToString("dd/MM/yyyy HH:mm");
                        Sheet.Cells[i, 3].Value = item.candidateName;
                        Sheet.Cells[i, 4].Value = item.candidateEmail;
                        Sheet.Cells[i, 5].Value = item.JDTiltle.Split("|")[0];
                        Sheet.Cells[i, 6].Value = item.interviewer;
                        Sheet.Cells[i, 7].Value = item.confirmationStatus;
                        Sheet.Cells[i, 8].Value = item.MeetingStatusStatusDesc;
                        Sheet.Cells[i, 9].Value = item.intervieweStatusStatDesc;
                        Sheet.Cells[i, 10].Value = item.IRFId;
                        Sheet.Cells[i, 11].Value = item.risebirdHR;
                        Sheet.Cells[i, 12].Value = item.MobileNumber;
                        Sheet.Cells[i, 13].Value = item.ClientHrContact;
                        Sheet.Cells[i, 14].Value = item.staffingAgency;
                        i++;
                    }
                    return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
                else
                {
                    ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                    Rng.Value = "No data available in table !!!";
                    return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception)
            {
                ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                Rng.Value = "Data export failed !!!";
                return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }


        [Authorize]
        [HttpPost]
        public IActionResult ClientSelectedMeetings(SelectedPriorityViewFilter filter)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.SetString("ClientSelectedMeetingsfilters", JsonConvert.SerializeObject(filter));
                    return RedirectToAction("ClientSelectedMeetings");
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Index(InterviewFilter filter, string type)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.SetString("Interviewfilters", JsonConvert.SerializeObject(filter));
                    return RedirectToAction("Index", new { type = type });
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ResetInterviewFilter(string type)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("Interviewfilters");
                    return RedirectToAction("Index", new { type = type });
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ResetMyInterviewFilter()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("MyInterviewfilters");
                    return RedirectToAction("MyInterview");
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("MyInterview");
            }

        }



        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ResetClientSelectedMeetingsfilters()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("ClientSelectedMeetingsfilters");
                    return RedirectToAction("ClientSelectedMeetings");
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("ClientSelectedMeetings");
            }

        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ResetClientMeetingsFilter()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("ClientMeetingsfilters");
                    return RedirectToAction("ClientMeetings");
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("ClientMeetings");
            }

        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ResetAllPriorityClientMeetingsFilter()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("AllPriorityClientMeetingsfilters");
                    return RedirectToAction("AllPriorityClientMeetings");
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("AllPriorityClientMeetings");
            }

        }

        [Authorize]
        [HttpPost]
        public IActionResult ClientMeetings(InterviewFilter filter)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.SetString("ClientMeetingsfilters", JsonConvert.SerializeObject(filter));
                    return RedirectToAction("ClientMeetings");
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetFilteredInterviews(string filterValue)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement")
                    {
                        var modelList = await _meetingService.GetMeetingList();

                        if (filterValue == "Today")
                        {
                            modelList = modelList.Where(x => x.interviewDate.Date == DateTime.Now.Date).ToList();

                            if (modelList.Count() == 0)
                            {
                                return new EmptyResult();
                            }
                        }
                        else if (filterValue == "Tomorrow")
                        {
                            modelList = modelList.Where(x => x.interviewDate.Date == DateTime.Now.AddDays(1).Date).ToList();
                            //modelList.Where(x => x.interviewDate == DateTime.Now.AddDays(1).Date).ToList();
                            if (modelList.Count() == 0)
                            {
                                return new EmptyResult();
                            }
                        }
                        else if (filterValue == "Yesterday")
                        {
                            modelList = modelList.Where(x => x.interviewDate.Date == DateTime.Now.AddDays(-1).Date).ToList();
                            //modelList.Where(x => x.interviewDate == DateTime.Now.AddDays(1).Date).ToList();
                            if (modelList.Count() == 0)
                            {
                                return new EmptyResult();
                            }
                        }
                        else if (filterValue == "Next Seven Days")
                        {
                            modelList = modelList.Where(x => x.interviewDate.Date <= DateTime.Now.AddDays(7).Date && x.interviewDate.Date >= DateTime.Now.Date).ToList();
                            if (modelList.Count() == 0)
                            {
                                return new EmptyResult();
                            }
                        }
                        // modelList = modelList.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((ReceiveMeetingStatus)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();
                        modelList = modelList.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty; x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty; x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();
                        foreach (var m in modelList)
                        {

                            if (m.IsApproved.HasValue)
                            {
                                m.NextLevelApprovalStat = ((NextLevelApprovalStatus)(m.IsApproved)).GetDescription();
                            }
                            if (m.IsSelected.HasValue)
                            {
                                m.FinalSelectionStat = ((FinalSelectionStatus)(m.IsSelected)).GetDescription();
                            }
                        }
                        return PartialView("GetFilteredInterviews", modelList);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(AddUpdateMeetingVM meeting)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        if (ModelState.IsValid)
                        {
                            if (!string.IsNullOrEmpty(meeting.zoomLink))
                            {
                                try
                                {
                                    meeting.ZoomMeetingId = (meeting.zoomLink.Split('/').LastOrDefault()).Split('?').FirstOrDefault();
                                }
                                catch (Exception)
                                {
                                    try
                                    {
                                        var link = meeting.zoomLink.Split('?').First();
                                        meeting.ZoomMeetingId = link.Split('/').Last();
                                    }
                                    catch (Exception)
                                    {
                                        //meeting.ZoomMeetingId = 0;
                                    }
                                }
                            }
                            string uploadedFileURL = string.Empty;
                            if (meeting.File == null && (meeting.resumeLink == null))
                            {
                                ModelState.AddModelError(string.Empty, "Please upload a Resume or add a Resume link.");
                                await SetAddUpdateMeetingParameters(meeting);
                                return View("Add", meeting);
                            }
                            if (meeting.File != null)
                            {
                                var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(meeting.File);
                                if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                                                    || fileMimeType == "application/pdf" || fileMimeType == "application/x-java-archive" || fileMimeType == "application/vnd.ms-powerpoint")
                                {
                                    await _azureStorageHelper.InsertBlob(meeting.File.FileName, "resumes", meeting.File);
                                    var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                                    var blob = containerRef.GetBlockBlobReference(meeting.File.FileName);
                                    uploadedFileURL = blob.Uri.ToString();
                                }
                                else
                                    uploadedFileURL = meeting.resumeLink;

                            }
                            else
                                uploadedFileURL = meeting.resumeLink;
                            if (uploadedFileURL == null || uploadedFileURL == string.Empty)
                            {
                                ModelState.AddModelError(string.Empty, "Resume Upload has been failed.");
                                await SetAddUpdateMeetingParameters(meeting);
                                return View("Add", meeting);
                            }
                            else
                            {
                                meeting.resumeLink = uploadedFileURL;
                                meeting.candidateEmail = meeting.candidateEmail.ToLower();
                                if (await _meetingService.AddMeeting(meeting))
                                {
                                    TempData["SuccessErrorMessageInterview"] = "Meeting has been added Successfully!";
                                    return RedirectToAction("Index");
                                }
                                else
                                {
                                    TempData["SuccessErrorMessageInterview"] = "Failed to add Meeting!";
                                    return RedirectToAction("Index");
                                }
                            }
                        }
                        else
                        {
                            //ModelState.AddModelError("", "Please check all the fields for errors!");
                            await SetAddUpdateMeetingParameters(meeting);
                            return View("Add", meeting);
                        }
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessageInterview"] = "Failed to add Meeting.";
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Add()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        var model = new AddUpdateMeetingVM();
                        await SetAddUpdateMeetingParameters(model);
                        return View(model);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return View();
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller")
                    {
                        AddUpdateMeetingVM meetingToUpdate = await _meetingService.GetMeeting(id);
                        if (meetingToUpdate == null)
                            return RedirectToAction("Index");
                        if (meetingToUpdate.MeetingPlatform == null)
                        {
                            meetingToUpdate.MeetingPlatform = MeetingPlatforms.Zoom;
                        }
                        await SetAddUpdateMeetingParameters(meetingToUpdate);

                        if (meetingToUpdate != null)
                        {

                            if (TempData["SuccessErrorMessageUpdatedInterview"] != null)
                            {
                                meetingToUpdate.Message = TempData["SuccessErrorMessageUpdatedInterview"].ToString();
                                TempData.Remove("SuccessErrorMessageUpdatedInterview");
                            }

                            TempData["ResumeLink"] = meetingToUpdate.resumeLink;
                            return View("Edit", meetingToUpdate);
                        }
                        else
                            return RedirectToAction("Index");
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AddUpdateMeetingVM meetingUpdated)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller")
                    {
                        if (!string.IsNullOrEmpty(meetingUpdated.interviewer))
                        {
                            meetingUpdated.interviewer = meetingUpdated.interviewer.Trim();
                        }
                        if (!string.IsNullOrEmpty(meetingUpdated.QCInterviewer))
                        {
                            meetingUpdated.QCInterviewer = meetingUpdated.QCInterviewer.Trim();
                        }
                        string zoomMeetingId = string.Empty;
                        if (!string.IsNullOrEmpty(meetingUpdated.zoomLink))
                        {
                            try
                            {
                                meetingUpdated.ZoomMeetingId = (meetingUpdated.zoomLink.Split('/').LastOrDefault()).Split('?').FirstOrDefault();
                            }
                            catch (Exception)
                            {
                                try
                                {
                                    var link = meetingUpdated.zoomLink.Split('?').First();
                                    zoomMeetingId = link.Split('/').Last();
                                    meetingUpdated.ZoomMeetingId = zoomMeetingId;
                                }
                                catch (Exception)
                                {
                                    //meetingUpdated.ZoomMeetingId = 0;
                                }
                            }
                        }

                        if (ModelState.IsValid)
                        {
                            //string uploadedFileURL = string.Empty;
                            if ((meetingUpdated.File != null))
                            {
                                var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(meetingUpdated.File);
                                if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                                                    || fileMimeType == "application/pdf" || fileMimeType == "application/x-java-archive" || fileMimeType == "application/vnd.ms-powerpoint")
                                {
                                    await _azureStorageHelper.InsertBlob(meetingUpdated.File.FileName, "resumes", meetingUpdated.File);
                                    var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                                    var blob = containerRef.GetBlockBlobReference(meetingUpdated.File.FileName);
                                    string uploadedFileURL = blob.Uri.ToString();
                                    if (uploadedFileURL == null)
                                    {
                                        var model = new AddUpdateMeetingVM();
                                        ModelState.AddModelError(string.Empty, "Resume Upload has been Failed.");
                                        await SetAddUpdateMeetingParameters(model);
                                        return View("Edit", model);
                                    }
                                    meetingUpdated.resumeLink = uploadedFileURL;
                                }
                            }
                            else if (meetingUpdated.resumeLink == null || meetingUpdated.resumeLink == "")
                            {
                                meetingUpdated.resumeLink = TempData["ResumeLink"]?.ToString();
                            }

                            meetingUpdated.candidateEmail = meetingUpdated.candidateEmail.ToLower();
                            meetingUpdated.LoggedInUser = emailId;
                            meetingUpdated.ResourceUrl = Request.Scheme + "://" + Request.Host.Value + Request.Path;

                            if (await _meetingService.UpdateMeeting(meetingUpdated))
                            {
                                if (!string.IsNullOrEmpty(zoomMeetingId) && meetingUpdated.confirmationStatus == "2")
                                {
                                    DeleteMeetingLink(zoomMeetingId);
                                }
                                TempData["SuccessErrorMessageUpdatedInterview"] = "Meeting has been updated Successfully!";
                                return RedirectToAction("Edit", new { id = meetingUpdated.imid });
                            }
                            else
                            {
                                TempData["SuccessErrorMessageUpdatedInterview"] = "Failed to update Meeting!";
                                return RedirectToAction("Edit", new { id = meetingUpdated.imid });
                            }
                        }
                        else
                        {
                            await SetAddUpdateMeetingParameters(meetingUpdated);
                            return View("Edit", meetingUpdated);
                        }
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessageUpdatedInterview"] = "Meeting update has been Failed";
                return RedirectToAction("Edit", new { id = meetingUpdated.imid });
            }
        }


        [HttpGet]
        private async Task SetAddUpdateMeetingParameters(AddUpdateMeetingVM meetingToUpdate)
        {
            var list = new List<SelectListItem>
            {
                new SelectListItem { Text = "No Clients", Value = "" }
            };
            var confirmationStatuses = from ReceiveMeetingStatus s in Enum.GetValues(typeof(ReceiveMeetingStatus))
                                       select new { ID = (int)s, Name = s.GetDescription() };
            meetingToUpdate.ConfirmationStatusList = new SelectList(confirmationStatuses, "ID", "Name");
            var interviewslotList = from InterviewSlots s in Enum.GetValues(typeof(InterviewSlots))
                                    select new { ID = (int)s, Name = s.GetDescription() };
            meetingToUpdate.InterviewSlotList = new SelectList(interviewslotList, "ID", "Name");
            meetingToUpdate.IRFList = _meetingService.GetIRFList();
            meetingToUpdate.InterviewingMembersList = await _meetingService.GetIRFInterviewingMembersEmails(meetingToUpdate.IRFId);
            meetingToUpdate.QualityControllersList = await _meetingService.GetIRFQualityControllersEmails(meetingToUpdate.IRFId);
            var clients = _meetingService.GetClientsByRoles(meetingToUpdate.IRFId);
            if ((clients.Where(x => x.ClientRoleId == 1) == null) || (clients.Where(x => x.ClientRoleId == 1)).Count() == 0)
            {
                meetingToUpdate.ClientHrContactList = new SelectList(list, "Value", "Text");
            }
            else
            {
                meetingToUpdate.ClientHrContactList = new SelectList(clients.Where(x => x.ClientRoleId == 1), "EmailId", "EmailId");

            }
            if ((clients.Where(x => x.ClientRoleId == 4) == null) || (clients.Where(x => x.ClientRoleId == 4)).Count() == 0)
            {
                meetingToUpdate.StaffingAgencyList = new SelectList(list, "Value", "Text");
            }
            else
            {
                meetingToUpdate.StaffingAgencyList = new SelectList(clients.Where(x => x.ClientRoleId == 4), "EmailId", "EmailId");

            }
            return;
        }

        [HttpGet]
        public async Task<List<ChannelMembersForInterviewData>> GetIRFMembersEmailsForInterviewDetails(string IRFId)
        {
            try
            {
                return await _meetingService.GetIRFMembersEmailsForInterviewDetails(IRFId);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return new List<ChannelMembersForInterviewData>();
            }
        }

        //public async Task<SelectList> GetIRFQualityControllersEmails(string IRFId)
        //{
        //    try
        //    {
        //        return await _meetingService.GetIRFQualityControllersEmails(IRFId);
        //    }

        //    catch (Exception e)
        //    {
        //        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
        //        return new SelectList(new List<string>() { string.Empty }); ;
        //    }
        //}

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> OpenInterview(int? Page, string searchValue)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Interviewer")
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        var meetings = await _meetingService.GetAllOpenInterviews(emailid);
                        //meetings = meetings.Where(x => x.interviewDate == DateTime.Now.Date).ToList();
                        meetings = meetings.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();
                        ViewBag.emailid = emailid;
                        //var codedname = await _meetingService.FetchInterviewerCodedName(emailid);
                        ViewBag.CodedName = "";
                        if (HttpContext.Session.GetString("InterviewerCodedName") != null)
                        {
                            ViewBag.CodedName = HttpContext.Session.GetString("InterviewerCodedName")?.Trim().Replace("\"", "");
                        }
                        var list = new SelectList(new[]
                        {   new { ID = "Today", Name = "Today"},
                            new { ID = "Tomorrow", Name = "Tomorrow"  },
                            new { ID = "Next Seven Days", Name = "Next Seven Days" },
                            new { ID = "All", Name = "All"},}, "ID", "Name", "All");

                        ViewData["list"] = list;
                        if (TempData["SuccessErrorMessage"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessage"].ToString();
                            TempData.Remove("SuccessErrorMessage");
                        }

                        if (!string.IsNullOrEmpty(searchValue))
                        {
                            searchValue = searchValue.ToUpper();
                            meetings = meetings.Where(x => (!string.IsNullOrEmpty(x.candidateName) ? x.candidateName.ToUpper().Contains(searchValue) : x.candidateName == "") || (!string.IsNullOrEmpty(x.risebirdHR) ? x.risebirdHR.ToUpper().Contains(searchValue) : x.risebirdHR == "")).ToList();

                        }
                        ViewBag.TotalCount = meetings.Count;

                        ViewBag.PageStartCount = 10 * ((Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page)) - 1) + 1;
                        int a = meetings.Count / 10;
                        if (meetings.Count % 10 == 0)
                        {
                            ViewBag.PageEndCount = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
                        }
                        else
                        {
                            a = a + 1;
                            if (a == Page)
                            {
                                ViewBag.PageEndCount = meetings.Count;
                            }
                            else
                            {
                                ViewBag.PageEndCount = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));

                            }
                        }
                        return View(meetings.ToPagedList(Page ?? 1, 10));
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return View();
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> OpenInterviewerAssessments()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Interviewer")
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        var meetings = await _meetingService.GetAllOpenInterviews(emailid);
                        //meetings = meetings.Where(x => x.interviewDate == DateTime.Now.Date).ToList();
                        meetings = meetings.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();

                        var list = new SelectList(new[]{
                            new { ID = "Today", Name = "Today"},
                            new { ID = "Tomorrow", Name = "Tomorrow"  },
                            new { ID = "Next Seven Days", Name = "Next Seven Days" },
                            new { ID = "All", Name = "All"},},
                            "ID", "Name", "All");

                        ViewData["list"] = list;
                        if (TempData["SuccessErrorMessage"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessage"].ToString();
                            TempData.Remove("SuccessErrorMessage");
                        }
                        return View(meetings);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetFilteredOpenInterviews(string filterValue)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Interviewer")
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        var meetings = await _meetingService.GetAllOpenInterviews(emailid);

                        if (filterValue == "Today")
                        {
                            meetings = meetings.Where(x => x.interviewDate.Date == DateTime.Now.Date).ToList();
                            if (meetings.Count() == 0)
                            {
                                return new EmptyResult();
                            }
                        }
                        else if (filterValue == "Tomorrow")
                        {
                            meetings = meetings.Where(x => x.interviewDate.Date == DateTime.Now.AddDays(1).Date).ToList();
                            if (meetings.Count() == 0)
                            {
                                return new EmptyResult();
                            }
                        }
                        else if (filterValue == "Next Seven Days")
                        {
                            meetings = meetings.Where(x => x.interviewDate.Date <= DateTime.Now.AddDays(7).Date && x.interviewDate.Date >= DateTime.Now.Date).ToList();
                            if (meetings.Count() == 0)
                            {
                                return new EmptyResult();
                            }
                        }
                        meetings = meetings.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();
                        return PartialView("GetFilteredOpenInterviews", meetings);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MyInterview(int? Page, string searchValue, string meetingLineUpType = null)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Interviewer")
                    {
                        if (string.IsNullOrEmpty(meetingLineUpType))
                        {
                            meetingLineUpType = MeetingLineUpType.Pending;
                        }
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        //var myInterview = await _meetingService.GetAllAssignedInterviews(emailid);
                        var myInterview = await _meetingService.GetInterviewerAssignedInterviews(emailid, meetingLineUpType);
                        var codedname = await _meetingService.FetchInterviewerCodedName(emailid);
                        var isSME = await _interviewerService.CheckIfSME(emailid);
                        ViewBag.isNotificationAccepted = codedname.IsNotificationAccepted;
                        ViewBag.CodedName = codedname.NickName;
                        if (string.IsNullOrWhiteSpace(codedname.CodedResumeLink))
                        {
                            ViewBag.isCodedResumePresent = false;
                        }
                        else
                        {
                            ViewBag.isCodedResumePresent = true;
                        }
                        ViewBag.TimeExpire = _configuration["InterviewerReport:ExpireTime"];
                        ViewBag.IsSME = isSME;
                        myInterview = myInterview.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); x.JobLevelDesc = ((JobLevels)Convert.ToInt32(x.JobLevel)).GetDescription(); x.HeuristicProfileTypeDesc = ((Heuristicsscorelevel)Convert.ToInt32(x.HeuristicProfileType)).GetDescription(); return x; }).ToList();
                        foreach (var item in myInterview)
                        {
                            if (item.HeuristicProfileTypeDesc == "0")
                            {
                                item.HeuristicProfileTypeDesc = "";
                            }
                            if (!string.IsNullOrEmpty(item.resumeLink))
                            {
                                try
                                {
                                    item.resumeLink = new Uri(item.resumeLink, UriKind.Absolute).ToString();
                                }
                                catch (Exception)
                                {
                                    item.resumeLink = "";
                                }
                            }
                            else
                            {
                                item.resumeLink = "";
                            }

                            if (item.InterviewStatus.HasValue)
                            {
                                item.InterviewStatusDesc = ((InterviewStatus)(item.InterviewStatus)).GetDescription();
                            }
                            if (item.MeetingStatus.HasValue)
                            {
                                item.MeetingStatusDesc = ((MeetingStatus)(item.MeetingStatus)).GetDescription();
                            }
                        }
                        if (HttpContext.Session.GetString("MyInterviewfilters") != null)
                        {
                            var filter = JsonConvert.DeserializeObject<MyInterviewFilter>(HttpContext.Session.GetString("MyInterviewfilters"));

                            if (filter.StartDate != null && filter.EndDate != null)
                            {
                                myInterview = myInterview.Where(t => t.interviewDate.Date >= filter.StartDate && t.interviewDate.Date <= filter.EndDate).ToList();
                            }
                            else if (filter.StartDate != null && filter.EndDate == null)
                            {
                                //filter.EndDate = filter.StartDate;
                                myInterview = myInterview.Where(t => t.interviewDate.Date >= filter.StartDate /*&& t.interviewDate.Date <= filter.EndDate*/).ToList();
                            }
                            else if (filter.StartDate == null && filter.EndDate != null)
                            {
                                //filter.StartDate = filter.EndDate;
                                myInterview = myInterview.Where(t => /*t.interviewDate.Date >= filter.StartDate &&*/ t.interviewDate.Date <= filter.EndDate).ToList();
                            }
                            //filter.InterviewDuration = filter.InterviewDuration == null ? filter.InterviewDuration = myInterview.Select(x => x.interviewDuration).ToArray() : filter.InterviewDuration = filter.InterviewDuration;
                            filter.InterviewSlot = filter.InterviewSlot == null ? filter.InterviewSlot = myInterview.Select(x => x.interviewSlot).ToArray() : filter.InterviewSlot = filter.InterviewSlot;
                            filter.JobLevel = filter.JobLevel == null ? filter.JobLevel = myInterview.Select(x => x.JobLevelDesc).ToArray() : filter.JobLevel = filter.JobLevel;
                            filter.InterviewDate = filter.InterviewDate == null ? filter.InterviewDate = myInterview.Select(x => Convert.ToString(x.interviewDate)).ToArray() : filter.InterviewDate = filter.InterviewDate;
                            filter.CandidateName = filter.CandidateName == null ? filter.CandidateName = myInterview.Select(x => x.candidateName).ToArray() : filter.CandidateName = filter.CandidateName;
                            filter.RisebirdHR = filter.RisebirdHR == null ? filter.RisebirdHR = myInterview.Select(x => x.risebirdHR).ToArray() : filter.RisebirdHR = filter.RisebirdHR;
                            //filter.InterviewDuration.Contains(c.interviewDuration)
                            myInterview = myInterview.Where(c => (filter.InterviewSlot.Count() > 0 ? filter.InterviewSlot.Contains(c.interviewSlot) : c.interviewSlot == null)
                            && (filter.JobLevel.Count() > 0 ? filter.JobLevel.Contains(c.JobLevelDesc) : c.JobLevelDesc == null)
                           && (filter.InterviewDate.Count() > 0 ? filter.InterviewDate.Contains(Convert.ToString(c.interviewDate)) : c.interviewDate == DateTime.MinValue)
                           && (filter.CandidateName.Count() > 0 ? filter.CandidateName.Contains(c.candidateName) : c.candidateName == null)
                           && (filter.RisebirdHR.Count() > 0 ? filter.RisebirdHR.Contains(c.risebirdHR) : c.risebirdHR == null)).ToList();
                        }
                        if (HttpContext.Session.GetString("MyInterviewfilters") == null)
                        {
                            myInterview = myInterview.Where(x => x.interviewDate.Date >= DateTime.Now.Date || (x.MeetingStatus == null && x.interviewDate.Date < DateTime.Now.Date)).ToList();
                        }
                        if (TempData["SuccessErrorMessage"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessage"].ToString();
                            TempData.Remove("SuccessErrorMessage");
                            //TempData.Clear();
                        }
                        if (!string.IsNullOrEmpty(searchValue))
                        {
                            myInterview = myInterview.Where(x => (!string.IsNullOrEmpty(x.candidateName) ? x.candidateName.Contains(searchValue) : x.candidateName == "") || (!string.IsNullOrEmpty(x.MeetingStatusDesc) ? x.MeetingStatusDesc.Contains(searchValue) : x.MeetingStatusDesc == "")
                            || (!string.IsNullOrEmpty(x.InterviewStatusDesc) ? x.InterviewStatusDesc.Contains(searchValue) : x.InterviewStatusDesc == "")).ToList();
                        }
                        ViewBag.TotalCount = myInterview.Count;
                        ViewBag.PageStartCount = 10 * ((Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page)) - 1) + 1;
                        int a = myInterview.Count / 10;
                        if (myInterview.Count % 10 == 0)
                        {
                            ViewBag.PageEndCount = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
                        }
                        else
                        {
                            if (Page == null)
                            {
                                Page = 1;
                            }
                            a = a + 1;
                            if (a == Page)
                            {
                                ViewBag.PageEndCount = myInterview.Count;
                            }
                            else
                            {
                                ViewBag.PageEndCount = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
                            }
                        }

                        if (myInterview.Count == 0)
                        {
                            ViewBag.PageStartCount = 0;
                            ViewBag.PageEndCount = 0;
                        }

                        foreach (var item in myInterview)
                        {
                            var time = item.interviewDate.AddHours(2);
                            var currentTime = DateTime.UtcNow;
                            TimeZoneInfo INDIAN_ZONE = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                            DateTime indianTime = TimeZoneInfo.ConvertTimeFromUtc(currentTime, INDIAN_ZONE);
                            var timediff = time - indianTime;
                            if (timediff.TotalMinutes >= 0 && (item.MeetingStatus == 2 || item.MeetingStatus == null))
                            {
                                item.Timer = Convert.ToInt32(timediff.TotalMinutes);

                            }
                            // item.Timer = 60;
                        }
                        ViewBag.MeetingLineUpType = meetingLineUpType;
                        return View(myInterview.ToPagedList(Page ?? 1, 10));
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MyInterviewerAssessments()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Interviewer")
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        var myInterview = await _meetingService.GetAllAssignedInterviews(emailid);
                        myInterview = myInterview.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); x.JobLevelDesc = ((JobLevels)Convert.ToInt32(x.JobLevel)).GetDescription(); return x; }).ToList();
                        foreach (var item in myInterview)
                        {
                            if (!string.IsNullOrEmpty(item.resumeLink))
                            {
                                try
                                {
                                    item.resumeLink = new Uri(item.resumeLink, UriKind.Absolute).ToString();

                                }
                                catch (Exception)
                                {
                                    item.resumeLink = "";
                                }
                            }
                            else
                            {
                                item.resumeLink = "";
                            }

                            if (item.InterviewStatus.HasValue)
                            {
                                item.InterviewStatusDesc = ((InterviewStatus)(item.InterviewStatus)).GetDescription();
                            }
                            if (item.MeetingStatus.HasValue)
                            {
                                item.MeetingStatusDesc = ((MeetingStatus)(item.MeetingStatus)).GetDescription();

                            }
                        }
                        if (HttpContext.Session.GetString("MyInterviewfilters") != null)
                        {
                            var filter = JsonConvert.DeserializeObject<MyInterviewFilter>(HttpContext.Session.GetString("MyInterviewfilters"));

                            if (filter.StartDate != null && filter.EndDate != null)
                            {
                                myInterview = myInterview.Where(t => t.interviewDate.Date >= filter.StartDate && t.interviewDate.Date <= filter.EndDate).ToList();
                            }
                            else if (filter.StartDate != null && filter.EndDate == null)
                            {
                                //filter.EndDate = filter.StartDate;

                                myInterview = myInterview.Where(t => t.interviewDate.Date >= filter.StartDate /*&& t.interviewDate.Date <= filter.EndDate*/).ToList();

                            }
                            else if (filter.StartDate == null && filter.EndDate != null)
                            {
                                //filter.StartDate = filter.EndDate;

                                myInterview = myInterview.Where(t => /*t.interviewDate.Date >= filter.StartDate &&*/ t.interviewDate.Date <= filter.EndDate).ToList();

                            }

                            //filter.InterviewDuration = filter.InterviewDuration == null ? filter.InterviewDuration = myInterview.Select(x => x.interviewDuration).ToArray() : filter.InterviewDuration = filter.InterviewDuration;
                            filter.InterviewSlot = filter.InterviewSlot == null ? filter.InterviewSlot = myInterview.Select(x => x.interviewSlot).ToArray() : filter.InterviewSlot = filter.InterviewSlot;
                            filter.JobLevel = filter.JobLevel == null ? filter.JobLevel = myInterview.Select(x => x.JobLevelDesc).ToArray() : filter.JobLevel = filter.JobLevel;
                            filter.InterviewDate = filter.InterviewDate == null ? filter.InterviewDate = myInterview.Select(x => Convert.ToString(x.interviewDate)).ToArray() : filter.InterviewDate = filter.InterviewDate;
                            filter.CandidateName = filter.CandidateName == null ? filter.CandidateName = myInterview.Select(x => x.candidateName).ToArray() : filter.CandidateName = filter.CandidateName;
                            filter.RisebirdHR = filter.RisebirdHR == null ? filter.RisebirdHR = myInterview.Select(x => x.risebirdHR).ToArray() : filter.RisebirdHR = filter.RisebirdHR;
                            //filter.InterviewDuration.Contains(c.interviewDuration)
                            myInterview = myInterview.Where(c => (filter.InterviewSlot.Count() > 0 ? filter.InterviewSlot.Contains(c.interviewSlot) : c.interviewSlot == null)
                            && (filter.JobLevel.Count() > 0 ? filter.JobLevel.Contains(c.JobLevelDesc) : c.JobLevelDesc == null)
                           && (filter.InterviewDate.Count() > 0 ? filter.InterviewDate.Contains(Convert.ToString(c.interviewDate)) : c.interviewDate == DateTime.MinValue)
                           && (filter.CandidateName.Count() > 0 ? filter.CandidateName.Contains(c.candidateName) : c.candidateName == null)
                           && (filter.RisebirdHR.Count() > 0 ? filter.RisebirdHR.Contains(c.risebirdHR) : c.risebirdHR == null)).ToList();


                        }
                        if (HttpContext.Session.GetString("MyInterviewfilters") == null)
                        {
                            myInterview = myInterview.Where(x => x.interviewDate.Date >= DateTime.Now.Date).ToList();

                        }
                        if (TempData["SuccessErrorMessage"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessage"].ToString();
                            TempData.Remove("SuccessErrorMessage");
                            //TempData.Clear();
                        }
                        return View(myInterview);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> MyInterview(MyInterviewFilter filter, int? Page, string searchValue, string meetingLineUpType = null)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (filter != null)
                    {
                        HttpContext.Session.SetString("MyInterviewfilters", JsonConvert.SerializeObject(filter));
                    }
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Interviewer")
                    {
                        return RedirectToAction("MyInterview", new { Page = Page, searchValue = searchValue, meetingLineUpType = meetingLineUpType });
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetFilteredMyInterviews(string filterValue)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Interviewer")
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        var myInterview = await _meetingService.GetAllAssignedInterviews(emailid);

                        if (filterValue == "Today")
                        {
                            myInterview = myInterview.Where(x => x.interviewDate.Date == DateTime.Now.Date).ToList();
                            if (myInterview.Count() == 0)
                            {
                                return new EmptyResult();
                            }

                        }
                        else if (filterValue == "Tomorrow")
                        {
                            myInterview = myInterview.Where(x => x.interviewDate.Date == DateTime.Now.AddDays(1).Date).ToList();
                            if (myInterview.Count() == 0)
                            {
                                return new EmptyResult();
                            }
                        }
                        else if (filterValue == "Yesterday")
                        {
                            myInterview = myInterview.Where(x => x.interviewDate.Date == DateTime.Now.AddDays(-1).Date).ToList();
                            if (myInterview.Count() == 0)
                            {
                                return new EmptyResult();
                            }
                        }
                        else if (filterValue == "Next Seven Days")
                        {
                            myInterview = myInterview.Where(x => x.interviewDate.Date <= DateTime.Now.AddDays(7).Date && x.interviewDate.Date >= DateTime.Now.Date).ToList();
                            if (myInterview.Count() == 0)
                            {
                                return new EmptyResult();
                            }
                        }


                        myInterview = myInterview.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();

                        return PartialView("GetFilteredMyInterviews", myInterview);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> QCInterview()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    return View(_meetingService.GetAllQCInterviews(emailid));
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ClientMeetings()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Client")
                    {
                        var meetings = await _meetingService.GetClientMeetingList(emailid);
                        if (HttpContext.Session.GetString("ClientMeetingsfilters") != null)
                        {
                            var filter = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("ClientMeetingsfilters"));
                            if (filter.StartDate == null && filter.EndDate == null)
                            {
                                meetings = meetings.Where(x => x.interviewDate.Date >= DateTime.Now.Date.AddDays(-30)).ToList();
                            }
                        }
                        else
                        {
                            meetings = meetings.Where(x => x.interviewDate.Date >= DateTime.Now.Date.AddDays(-30)).ToList();
                        }
                        meetings = meetings.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();

                        //ViewBag.IsSelected = false;
                        foreach (var m in meetings)
                        {
                            if (m.InterviewStatus.HasValue)
                            {
                                m.intervieweStatusStatDesc = ((InterviewStatus)(m.InterviewStatus)).GetDescription();
                            }
                            if (m.L2InterviewStatus.HasValue)
                            {
                                m.L2InterviewStatDesc = ((L2InterviewStatus)(m.L2InterviewStatus)).GetDescription();
                            }
                            if (m.L2InterviewStatus.HasValue)
                            {
                                m.ClientInterviewStatDesc = ((ClientInterviewStatus)(m.ClientInterviewStatus)).GetDescription();
                            }
                            if (m.JoiningStatus.HasValue && m.JoiningStatus != -1)
                            {
                                m.JoiningStatusStatDesc = ((JoiningStatus)(m.JoiningStatus)).GetDescription();
                            }
                            if (m.PreparationStatus.HasValue)
                            {
                                m.PreparationStatusDesc = ((PreparationStatus)(m.PreparationStatus)).GetDescription();
                            }
                            if (m.OfferStatus.HasValue && m.OfferStatus != -1)
                            {
                                m.OfferStatusStatDesc = ((OfferStatus)(m.OfferStatus)).GetDescription();
                            }
                            if (m.MeetingStatus.HasValue)
                            {
                                m.MeetingStatusStatusDesc = ((MeetingStatus)(m.MeetingStatus)).GetDescription();
                            }
                            if (m.QCStatus.HasValue)
                            {
                                m.QCStatusStatusDesc = ((QCStatus)(m.QCStatus)).GetDescription();
                            }
                            if (m.IsApproved.HasValue && m.IsApproved != -1)
                            {
                                m.NextLevelApprovalStat = ((NextLevelApprovalStatus)(m.IsApproved)).GetDescription();
                            }
                            if (m.IsSelected.HasValue && m.IsSelected != -1)
                            {
                                m.FinalSelectionStat = ((FinalSelectionStatus)(m.IsSelected)).GetDescription();
                            }
                        }

                        if (HttpContext.Session.GetString("ClientMeetingsfilters") != null)
                        {
                            var filter = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("ClientMeetingsfilters"));
                            if (filter.StartDate != null && filter.EndDate != null)
                            {
                                meetings = meetings.Where(t => t.interviewDate.Date >= filter.StartDate && t.interviewDate.Date <= filter.EndDate).ToList();
                            }

                            filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = meetings.Select(x => x.JDTiltle.Split("|")[0]).ToArray() : filter.PrimarySkill = filter.PrimarySkill;
                            filter.RFID = filter.RFID == null ? filter.RFID = meetings.Select(x => x.IRFId).ToArray() : filter.RFID = filter.RFID;
                            filter.ClientHR = filter.ClientHR == null ? filter.ClientHR = meetings.Select(x => x.ClientHrContact).ToArray() : filter.ClientHR = filter.ClientHR;
                            filter.ConfirmationStatus = filter.ConfirmationStatus == null ? filter.ConfirmationStatus = meetings.Select(x => x.confirmationStatus).ToArray() : filter.ConfirmationStatus = filter.ConfirmationStatus;
                            filter.MeetingStatus = filter.MeetingStatus == null ? filter.MeetingStatus = meetings.Select(x => x.MeetingStatusStatusDesc).ToArray() : filter.MeetingStatus = filter.MeetingStatus;
                            filter.InterviewStatus = filter.InterviewStatus == null ? filter.InterviewStatus = meetings.Select(x => x.intervieweStatusStatDesc).ToArray() : filter.InterviewStatus = filter.InterviewStatus;
                            //filter.Interviewer = filter.Interviewer == null ? filter.Interviewer = meetings.Select(x => x.interviewer).ToArray() : filter.Interviewer = filter.Interviewer;
                            filter.ApprovalStatus = filter.ApprovalStatus == null ? filter.ApprovalStatus = meetings.Select(x => x.NextLevelApprovalStat).ToArray() : filter.ApprovalStatus = filter.ApprovalStatus;
                            filter.SelectionStatus = filter.SelectionStatus == null ? filter.SelectionStatus = meetings.Select(x => x.FinalSelectionStat).ToArray() : filter.SelectionStatus = filter.SelectionStatus;
                            filter.JoiningStatus = filter.JoiningStatus == null ? filter.JoiningStatus = meetings.Select(x => x.JoiningStatusStatDesc).ToArray() : filter.JoiningStatus = filter.JoiningStatus;
                            filter.OfferStatus = filter.OfferStatus == null ? filter.OfferStatus = meetings.Select(x => x.OfferStatusStatDesc).ToArray() : filter.OfferStatus = filter.OfferStatus;
                            filter.ClientName = filter.ClientName == null ? filter.ClientName = meetings.Select(x => x.ClientName).ToArray() : filter.ClientName = filter.ClientName;

                            meetings = meetings.Where(c => filter.PrimarySkill.Contains(c.JDTiltle.Split("|")[0])
                            && filter.RFID.Contains(c.IRFId)
                            && (filter.ClientHR.Count() > 0 ? filter.ClientHR.Contains(c.ClientHrContact) : c.ClientHrContact == null)
                            && (filter.ConfirmationStatus.Count() > 0 ? filter.ConfirmationStatus.Contains(c.confirmationStatus) : c.confirmationStatus == null)
                            && (filter.MeetingStatus.Count() > 0 ? filter.MeetingStatus.Contains(c.MeetingStatusStatusDesc) : c.MeetingStatusStatusDesc == string.Empty)
                            && (filter.InterviewStatus.Count() > 0 ? filter.InterviewStatus.Contains(c.intervieweStatusStatDesc) : c.intervieweStatusStatDesc == string.Empty)
                            && (filter.ClientName.Count() > 0 ? filter.ClientName.Contains(c.ClientName) : c.ClientName == null)
                            && (filter.ApprovalStatus.Count() > 0 ? filter.ApprovalStatus.Contains(c.NextLevelApprovalStat) : c.NextLevelApprovalStat == string.Empty)
                            && (filter.SelectionStatus.Count() > 0 ? filter.SelectionStatus.Contains(c.FinalSelectionStat) : c.FinalSelectionStat == string.Empty)
                            && (filter.OfferStatus.Count() > 0 ? filter.OfferStatus.Contains(c.OfferStatusStatDesc) : c.OfferStatusStatDesc == string.Empty)
                            && (filter.JoiningStatus.Count() > 0 ? filter.JoiningStatus.Contains(c.JoiningStatusStatDesc) : c.JoiningStatusStatDesc == string.Empty)).ToList();
                        }
                        ViewBag.ClientHRList = _meetingService.GetClientHRContactList(emailid, meetings);
                        ViewBag.ClientNameList = _meetingService.GetEndClientList(emailid, meetings);
                        ViewBag.IRFList = _meetingService.GetIRFList(emailid, meetings);
                        return View(meetings);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetClientFilteredMeetings(string filterValue)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Client")
                    {
                        var meetings = await _meetingService.GetClientMeetingList(emailid);

                        meetings = meetings.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();

                        if (filterValue == "Today")
                        {
                            meetings = meetings.Where(x => x.interviewDate.Date == DateTime.Now.Date).ToList();
                            if (meetings.Count() == 0)
                            {
                                return new EmptyResult();
                            }
                        }
                        else if (filterValue == "Tomorrow")
                        {
                            meetings = meetings.Where(x => x.interviewDate.Date == DateTime.Now.AddDays(1).Date).ToList();
                            if (meetings.Count() == 0)
                            {
                                return new EmptyResult();
                            }
                        }
                        else if (filterValue == "Yesterday")
                        {
                            meetings = meetings.Where(x => x.interviewDate.Date == DateTime.Now.AddDays(-1).Date).ToList();
                            if (meetings.Count() == 0)
                            {
                                return new EmptyResult();
                            }
                        }
                        else if (filterValue == "Next Seven Days")
                        {
                            meetings = meetings.Where(x => x.interviewDate.Date <= DateTime.Now.AddDays(7).Date && x.interviewDate.Date >= DateTime.Now.Date).ToList();
                            if (meetings.Count() == 0)
                            {
                                return new EmptyResult();
                            }
                        }

                        //ViewBag.IsSelected = false;
                        foreach (var m in meetings)
                        {
                            if (m.InterviewStatus.HasValue)
                            {
                                m.intervieweStatusStatDesc = ((InterviewStatus)(m.InterviewStatus)).GetDescription();
                            }
                            if (m.L2InterviewStatus.HasValue)
                            {
                                m.L2InterviewStatDesc = ((L2InterviewStatus)(m.L2InterviewStatus)).GetDescription();
                            }
                            if (m.L2InterviewStatus.HasValue)
                            {
                                m.ClientInterviewStatDesc = ((ClientInterviewStatus)(m.ClientInterviewStatus)).GetDescription();
                            }
                            if (m.JoiningStatus.HasValue)
                            {
                                m.JoiningStatusStatDesc = ((JoiningStatus)(m.JoiningStatus)).GetDescription();
                            }
                            if (m.PreparationStatus.HasValue)
                            {
                                m.PreparationStatusDesc = ((PreparationStatus)(m.PreparationStatus)).GetDescription();
                            }
                            if (m.OfferStatus.HasValue)
                            {
                                m.OfferStatusStatDesc = ((PreparationStatus)(m.OfferStatus)).GetDescription();
                            }
                            if (m.MeetingStatus.HasValue)
                            {
                                m.MeetingStatusStatusDesc = ((MeetingStatus)(m.MeetingStatus)).GetDescription();
                            }
                            if (m.QCStatus.HasValue)
                            {
                                m.QCStatusStatusDesc = ((QCStatus)(m.QCStatus)).GetDescription();
                            }
                        }
                        return PartialView("GetClientFilteredMeetings", meetings);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ClientSelectedMeetings()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Client")
                    {
                        var selectedMeeting = new List<MeetingVM>();
                        var meetings = (await _meetingService.GetClientMeetingList(emailid))?.Where(x => x.QCInterview > 0);
                        if (HttpContext.Session.GetString("ClientSelectedMeetingsfilters") != null)
                        {
                            var filter = JsonConvert.DeserializeObject<SelectedPriorityViewFilter>(HttpContext.Session.GetString("ClientSelectedMeetingsfilters"));
                            if (filter.StartDate == null && filter.EndDate == null)
                            {
                                meetings = meetings.Where(x => x.interviewDate.Date >= DateTime.Now.Date.AddDays(-30)).ToList();
                            }
                        }
                        else
                        {
                            meetings = meetings.Where(x => x.interviewDate.Date >= DateTime.Now.Date.AddDays(-30)).ToList();
                        }
                        meetings = meetings.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();

                        //ViewBag.IsSelected = true;
                        foreach (var m in meetings)
                        {
                            if (m.InterviewStatus.HasValue && (m.InterviewStatus.Value == 2 || m.InterviewStatus.Value == 4))
                            {
                                m.intervieweStatusStatDesc = ((InterviewStatus)(m.InterviewStatus)).GetDescription();
                            }
                            else
                            {
                                continue;
                            }

                            if (m.L2InterviewStatus.HasValue)
                            {
                                m.L2InterviewStatDesc = ((L2InterviewStatus)(m.L2InterviewStatus)).GetDescription();
                            }
                            if (m.L2InterviewStatus.HasValue)
                            {
                                m.ClientInterviewStatDesc = ((ClientInterviewStatus)(m.ClientInterviewStatus)).GetDescription();
                            }
                            if (m.JoiningStatus.HasValue && m.JoiningStatus != -1)
                            {
                                m.JoiningStatusStatDesc = ((JoiningStatus)(m.JoiningStatus)).GetDescription();
                            }
                            if (m.PreparationStatus.HasValue)
                            {
                                m.PreparationStatusDesc = ((PreparationStatus)(m.PreparationStatus)).GetDescription();
                            }
                            if (m.OfferStatus.HasValue && m.OfferStatus != -1)
                            {
                                m.OfferStatusStatDesc = ((OfferStatus)(m.OfferStatus)).GetDescription();
                            }
                            if (m.MeetingStatus.HasValue)
                            {
                                m.MeetingStatusStatusDesc = ((MeetingStatus)(m.MeetingStatus)).GetDescription();
                            }
                            if (m.QCStatus.HasValue)
                            {
                                m.QCStatusStatusDesc = ((QCStatus)(m.QCStatus)).GetDescription();
                            }
                            if (m.IsApproved.HasValue && m.IsApproved != -1)
                            {
                                m.NextLevelApprovalStat = ((NextLevelApprovalStatus)(m.IsApproved)).GetDescription();
                            }
                            if (m.IsSelected.HasValue && m.IsSelected != -1)
                            {
                                m.FinalSelectionStat = ((FinalSelectionStatus)(m.IsSelected)).GetDescription();
                            }
                            selectedMeeting.Add(m);
                        }


                        if (HttpContext.Session.GetString("ClientSelectedMeetingsfilters") != null)
                        {
                            var filter = JsonConvert.DeserializeObject<SelectedPriorityViewFilter>(HttpContext.Session.GetString("ClientSelectedMeetingsfilters"));
                            if (filter.StartDate != null && filter.EndDate != null)
                            {
                                selectedMeeting = selectedMeeting.Where(t => t.interviewDate.Date >= filter.StartDate && t.interviewDate.Date <= filter.EndDate).ToList();
                            }
                            filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = selectedMeeting.Select(x => x.JDTiltle.Split("|")[0]).ToArray() : filter.PrimarySkill = filter.PrimarySkill;
                            filter.ClientHR = filter.ClientHR == null ? filter.ClientHR = selectedMeeting.Select(x => x.ClientHrContact).ToArray() : filter.ClientHR = filter.ClientHR;
                            filter.InterviewDate = filter.InterviewDate == null ? filter.InterviewDate = selectedMeeting.Select(x => Convert.ToString(x.interviewDate)).ToArray() : filter.InterviewDate = filter.InterviewDate;
                            filter.CandidateName = filter.CandidateName == null ? filter.CandidateName = selectedMeeting.Select(x => x.candidateName).ToArray() : filter.CandidateName = filter.CandidateName;
                            filter.ClientName = filter.ClientName == null ? filter.ClientName = selectedMeeting.Select(x => x.ClientName).ToArray() : filter.ClientName = filter.ClientName;
                            filter.ApprovalStatus = filter.ApprovalStatus == null ? filter.ApprovalStatus = meetings.Select(x => x.NextLevelApprovalStat).ToArray() : filter.ApprovalStatus = filter.ApprovalStatus;
                            filter.SelectionStatus = filter.SelectionStatus == null ? filter.SelectionStatus = meetings.Select(x => x.FinalSelectionStat).ToArray() : filter.SelectionStatus = filter.SelectionStatus;
                            filter.JoiningStatus = filter.JoiningStatus == null ? filter.JoiningStatus = meetings.Select(x => x.JoiningStatusStatDesc).ToArray() : filter.JoiningStatus = filter.JoiningStatus;
                            filter.OfferStatus = filter.OfferStatus == null ? filter.OfferStatus = meetings.Select(x => x.OfferStatusStatDesc).ToArray() : filter.OfferStatus = filter.OfferStatus;

                            selectedMeeting = selectedMeeting.Where(c => filter.PrimarySkill.Contains(c.JDTiltle.Split("|")[0])
                            && (filter.ClientHR.Count() > 0 ? filter.ClientHR.Contains(c.ClientHrContact) : c.ClientHrContact == null)
                            && (filter.InterviewDate.Count() > 0 ? filter.InterviewDate.Contains(Convert.ToString(c.interviewDate)) : c.interviewDate == DateTime.MinValue)
                            && (filter.CandidateName.Count() > 0 ? filter.CandidateName.Contains(c.candidateName) : c.candidateName == null)
                            && (filter.ApprovalStatus.Count() > 0 ? filter.ApprovalStatus.Contains(c.NextLevelApprovalStat) : c.NextLevelApprovalStat == string.Empty)
                            && (filter.SelectionStatus.Count() > 0 ? filter.SelectionStatus.Contains(c.FinalSelectionStat) : c.FinalSelectionStat == string.Empty)
                            && (filter.OfferStatus.Count() > 0 ? filter.OfferStatus.Contains(c.OfferStatusStatDesc) : c.OfferStatusStatDesc == string.Empty)
                            && (filter.JoiningStatus.Count() > 0 ? filter.JoiningStatus.Contains(c.JoiningStatusStatDesc) : c.JoiningStatusStatDesc == string.Empty)
                            && (filter.ClientName.Count() > 0 ? filter.ClientName.Contains(c.ClientName) : c.ClientName == null)).ToList();
                        }
                        return View(selectedMeeting);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ClientFilteredSelectedMeetings(string filterValue)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Client")
                    {
                        var selectedMeeting = new List<MeetingVM>();
                        var meetings = await _meetingService.GetClientMeetingList(emailid);
                        meetings = meetings.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();

                        if (filterValue == "Today")
                        {
                            meetings = meetings.Where(x => x.interviewDate.Date == DateTime.Now.Date).ToList();
                            if (meetings.Count() == 0)
                            {
                                return new EmptyResult();
                            }
                        }
                        else if (filterValue == "Tomorrow")
                        {
                            meetings = meetings.Where(x => x.interviewDate.Date == DateTime.Now.AddDays(1).Date).ToList();
                            if (meetings.Count() == 0)
                            {
                                return new EmptyResult();
                            }
                        }
                        else if (filterValue == "Yesterday")
                        {
                            meetings = meetings.Where(x => x.interviewDate.Date == DateTime.Now.AddDays(-1).Date).ToList();
                            if (meetings.Count() == 0)
                            {
                                return new EmptyResult();
                            }
                        }
                        else if (filterValue == "Next Seven Days")
                        {
                            meetings = meetings.Where(x => x.interviewDate.Date <= DateTime.Now.AddDays(7).Date && x.interviewDate.Date >= DateTime.Now.Date).ToList();
                            if (meetings.Count() == 0)
                            {
                                return new EmptyResult();
                            }
                        }

                        //ViewBag.IsSelected = true;
                        foreach (var m in meetings)
                        {
                            if (m.InterviewStatus.HasValue && (m.InterviewStatus.Value == 2 || m.InterviewStatus.Value == 4))
                            {
                                m.intervieweStatusStatDesc = ((InterviewStatus)(m.InterviewStatus)).GetDescription();
                            }
                            else
                            {
                                continue;
                            }

                            if (m.L2InterviewStatus.HasValue)
                            {
                                m.L2InterviewStatDesc = ((L2InterviewStatus)(m.L2InterviewStatus)).GetDescription();
                            }
                            if (m.L2InterviewStatus.HasValue)
                            {
                                m.ClientInterviewStatDesc = ((ClientInterviewStatus)(m.ClientInterviewStatus)).GetDescription();
                            }
                            if (m.JoiningStatus.HasValue)
                            {
                                m.JoiningStatusStatDesc = ((JoiningStatus)(m.JoiningStatus)).GetDescription();
                            }
                            if (m.PreparationStatus.HasValue)
                            {
                                m.PreparationStatusDesc = ((PreparationStatus)(m.PreparationStatus)).GetDescription();
                            }
                            if (m.OfferStatus.HasValue)
                            {
                                m.OfferStatusStatDesc = ((PreparationStatus)(m.OfferStatus)).GetDescription();
                            }
                            if (m.MeetingStatus.HasValue)
                            {
                                m.MeetingStatusStatusDesc = ((MeetingStatus)(m.MeetingStatus)).GetDescription();
                            }
                            if (m.QCStatus.HasValue)
                            {
                                m.QCStatusStatusDesc = ((QCStatus)(m.QCStatus)).GetDescription();
                            }
                            if (m.IsApproved.HasValue)
                            {
                                m.NextLevelApprovalStat = ((NextLevelApprovalStatus)(m.IsApproved)).GetDescription();
                            }
                            if (m.IsSelected.HasValue)
                            {
                                m.FinalSelectionStat = ((FinalSelectionStatus)(m.IsSelected)).GetDescription();
                            }
                            selectedMeeting.Add(m);
                        }
                        return PartialView("ClientFilteredSelectedMeetings", selectedMeeting);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAllPriorityClientFilteredMeetings(string filterValue)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Client")
                    {
                        var meetings = await _meetingService.GetClientMeetingList(emailid);
                        meetings = meetings.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();
                        if (filterValue == "Today")
                        {
                            meetings = meetings.Where(x => x.interviewDate.Date == DateTime.Now.Date).ToList();
                            if (meetings.Count() == 0)
                            {
                                return new EmptyResult();
                            }
                        }
                        else if (filterValue == "Tomorrow")
                        {
                            meetings = meetings.Where(x => x.interviewDate.Date == DateTime.Now.AddDays(1).Date).ToList();
                            if (meetings.Count() == 0)
                            {
                                return new EmptyResult();
                            }
                        }
                        else if (filterValue == "Yesterday")
                        {
                            meetings = meetings.Where(x => x.interviewDate.Date == DateTime.Now.AddDays(-1).Date).ToList();
                            if (meetings.Count() == 0)
                            {
                                return new EmptyResult();
                            }
                        }
                        else if (filterValue == "Next Seven Days")
                        {
                            meetings = meetings.Where(x => x.interviewDate.Date <= DateTime.Now.AddDays(7).Date && x.interviewDate.Date >= DateTime.Now.Date).ToList();
                            if (meetings.Count() == 0)
                            {
                                return new EmptyResult();
                            }
                        }

                        //ViewBag.IsSelected = false;
                        foreach (var m in meetings)
                        {
                            if (m.InterviewStatus.HasValue)
                            {
                                m.intervieweStatusStatDesc = ((InterviewStatus)(m.InterviewStatus)).GetDescription();
                            }
                            if (m.L2InterviewStatus.HasValue)
                            {
                                m.L2InterviewStatDesc = ((L2InterviewStatus)(m.L2InterviewStatus)).GetDescription();
                            }
                            if (m.L2InterviewStatus.HasValue)
                            {
                                m.ClientInterviewStatDesc = ((ClientInterviewStatus)(m.ClientInterviewStatus)).GetDescription();
                            }
                            if (m.JoiningStatus.HasValue)
                            {
                                m.JoiningStatusStatDesc = ((JoiningStatus)(m.JoiningStatus)).GetDescription();
                            }
                            if (m.PreparationStatus.HasValue)
                            {
                                m.PreparationStatusDesc = ((PreparationStatus)(m.PreparationStatus)).GetDescription();
                            }
                            if (m.OfferStatus.HasValue)
                            {
                                m.OfferStatusStatDesc = ((PreparationStatus)(m.OfferStatus)).GetDescription();
                            }
                            if (m.MeetingStatus.HasValue)
                            {
                                m.MeetingStatusStatusDesc = ((MeetingStatus)(m.MeetingStatus)).GetDescription();
                            }
                            if (m.QCStatus.HasValue)
                            {
                                m.QCStatusStatusDesc = ((QCStatus)(m.QCStatus)).GetDescription();
                            }
                        }
                        return PartialView("GetAllPriorityClientFilteredMeetings", meetings);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetSelectedPriorityClientFilteredMeetings(string filterValue)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Client")
                    {
                        var selectedMeeting = new List<MeetingVM>();
                        var meetings = await _meetingService.GetClientMeetingList(emailid);
                        meetings = meetings.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();
                        foreach (var meeting in meetings)
                        {
                            if (meeting.InterviewStatus != null)
                            {
                                meeting.InterviewStatusDescription = ((InterviewStatus)meeting.InterviewStatus).GetDescription();
                            }
                            if (meeting.MeetingStatus != null)
                            {
                                meeting.MeetingStatusDescription = ((MeetingStatus)meeting.MeetingStatus).GetDescription();
                            }
                        }
                        if (filterValue == "Today")
                        {
                            meetings = meetings.Where(x => x.interviewDate.Date == DateTime.Now.Date).ToList();
                            if (meetings.Count() == 0)
                            {
                                return new EmptyResult();
                            }
                        }
                        else if (filterValue == "Tomorrow")
                        {
                            meetings = meetings.Where(x => x.interviewDate.Date == DateTime.Now.AddDays(1).Date).ToList();
                            if (meetings.Count() == 0)
                            {
                                return new EmptyResult();
                            }
                        }
                        else if (filterValue == "Yesterday")
                        {
                            meetings = meetings.Where(x => x.interviewDate.Date == DateTime.Now.AddDays(-1).Date).ToList();
                            if (meetings.Count() == 0)
                            {
                                return new EmptyResult();
                            }
                        }
                        else if (filterValue == "Next Seven Days")
                        {
                            meetings = meetings.Where(x => x.interviewDate.Date <= DateTime.Now.AddDays(7).Date && x.interviewDate.Date >= DateTime.Now.Date).ToList();
                            if (meetings.Count() == 0)
                            {
                                return new EmptyResult();
                            }
                        }

                        //ViewBag.IsSelected = true;
                        foreach (var m in meetings)
                        {
                            if (m.InterviewStatus.HasValue && (m.InterviewStatus.Value == 2 || m.InterviewStatus.Value == 4))
                            {
                                m.intervieweStatusStatDesc = ((InterviewStatus)(m.InterviewStatus)).GetDescription();
                            }
                            else
                            {
                                continue;
                            }

                            if (m.L2InterviewStatus.HasValue)
                            {
                                m.L2InterviewStatDesc = ((L2InterviewStatus)(m.L2InterviewStatus)).GetDescription();
                            }
                            if (m.L2InterviewStatus.HasValue)
                            {
                                m.ClientInterviewStatDesc = ((ClientInterviewStatus)(m.ClientInterviewStatus)).GetDescription();
                            }
                            if (m.JoiningStatus.HasValue)
                            {
                                m.JoiningStatusStatDesc = ((JoiningStatus)(m.JoiningStatus)).GetDescription();
                            }
                            if (m.PreparationStatus.HasValue)
                            {
                                m.PreparationStatusDesc = ((PreparationStatus)(m.PreparationStatus)).GetDescription();
                            }
                            if (m.OfferStatus.HasValue)
                            {
                                m.OfferStatusStatDesc = ((PreparationStatus)(m.OfferStatus)).GetDescription();
                            }
                            if (m.MeetingStatus.HasValue)
                            {
                                m.MeetingStatusStatusDesc = ((MeetingStatus)(m.MeetingStatus)).GetDescription();
                            }
                            if (m.QCStatus.HasValue)
                            {
                                m.QCStatusStatusDesc = ((QCStatus)(m.QCStatus)).GetDescription();
                            }
                            if (m.IsApproved.HasValue)
                            {
                                m.NextLevelApprovalStat = ((NextLevelApprovalStatus)(m.IsApproved)).GetDescription();
                            }
                            if (m.IsSelected.HasValue)
                            {
                                m.FinalSelectionStat = ((FinalSelectionStatus)(m.IsSelected)).GetDescription();
                            }
                            selectedMeeting.Add(m);
                        }
                        return PartialView("GetSelectedPriorityClientFilteredMeetings", selectedMeeting);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> AddComments(int interviewId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    ViewBag.Message = TempData["SuccessErrorMessageInterviewComment"];
                    InterviewComments comments = await _meetingService.GetInterviewComments(interviewId);
                    //comments.OldQCStatus = comments.QCStatus;
                    string prof = HttpContext.Session.GetString("profileName");
                    if (prof != null && prof.Contains("Client"))
                    {
                        ViewBag.Client = "Client";
                    }
                    if (comments == null)
                        comments = new InterviewComments();
                    GetStatusList(comments);
                    return View("InterviewComments", comments);
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AddComments(InterviewComments interviewComments)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (ModelState.IsValid)
                    {
                        var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                        profileName = profileName.Replace("\"", "");
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        if (profileName == "Admin" || profileName == "HR" || profileName == "Interviewer" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                        {

                            if (interviewComments.AadharFile != null)
                            {
                                var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(interviewComments.AadharFile);
                                if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                                                    || fileMimeType == "application/pdf" || fileMimeType == "application/x-java-archive" || fileMimeType == "application/vnd.ms-powerpoint")
                                {
                                    await _azureStorageHelper.InsertBlob(interviewComments.AadharFile.FileName, "documents", interviewComments.AadharFile);
                                    var containerRef = _azureStorageHelper.GetCloudBlobContainer("documents");
                                    var blob = containerRef.GetBlockBlobReference(interviewComments.AadharFile.FileName);
                                    string uploadedAadharFileURL = blob.Uri.ToString();
                                    if (uploadedAadharFileURL == null)
                                    {
                                        ModelState.AddModelError(string.Empty, "Aadhar Upload has failed.");
                                        GetStatusList(interviewComments);
                                        return View("InterviewComments", interviewComments);
                                    }
                                    else
                                    {
                                        interviewComments.AadharFileLink = uploadedAadharFileURL;
                                    }
                                }
                            }
                            if (interviewComments.AudioFile != null)
                            {
                                var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(interviewComments.AudioFile);
                                if (fileMimeType == "audio/basic" || fileMimeType == "audio/mpeg" || fileMimeType == "audio/x-wav" || fileMimeType == "video/x-msvideo")
                                {
                                    await _azureStorageHelper.InsertBlob(interviewComments.AudioFile.FileName, "audios", interviewComments.AudioFile);
                                    var containerRef = _azureStorageHelper.GetCloudBlobContainer("audios");
                                    var blob = containerRef.GetBlockBlobReference(interviewComments.AudioFile.FileName);
                                    string uploadedaudioFileURL = blob.Uri.ToString();
                                    if (uploadedaudioFileURL == null)
                                    {
                                        ModelState.AddModelError(string.Empty, "Audio Upload is failed.");
                                        GetStatusList(interviewComments);
                                        return View("InterviewComments", interviewComments);
                                    }
                                    else
                                    {
                                        interviewComments.AudioLink = uploadedaudioFileURL;
                                    }
                                }
                            }
                            if (interviewComments.VideoFile != null)
                            {
                                var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(interviewComments.VideoFile);
                                if (fileMimeType == "video/mpeg" || fileMimeType == "video/mp4"
                                    || fileMimeType == "video/quicktime")
                                {
                                    await _azureStorageHelper.InsertBlob(interviewComments.VideoFile.FileName, "videos", interviewComments.VideoFile);
                                    var containerRef = _azureStorageHelper.GetCloudBlobContainer("videos");
                                    var blob = containerRef.GetBlockBlobReference(interviewComments.VideoFile.FileName);
                                    string uploadedvideoFileURL = blob.Uri.ToString();
                                    if (uploadedvideoFileURL == null)
                                    {
                                        ModelState.AddModelError(string.Empty, "Video Upload has been Failed.");
                                        GetStatusList(interviewComments);
                                        //return View("InterviewComments", interviewComments);
                                    }
                                    else
                                    {
                                        interviewComments.VideoLink = uploadedvideoFileURL;
                                    }
                                }
                            }

                            if (await _meetingService.UpdateComments(interviewComments, emailid))
                            {
                                TempData["SuccessErrorMessageInterviewComment"] = "Comments added Successfully!";
                                return RedirectToAction("AddComments", new { interviewId = interviewComments.InterviewId });
                            }
                            else
                            {
                                TempData["SuccessErrorMessageInterview"] = "Failed to add Comments!";
                                return RedirectToAction("Action", new { interviewId = interviewComments.InterviewId });
                            }
                        }
                        else
                        {
                            return RedirectToAction("SignOut", "Session");
                        }

                    }
                    else
                    {
                        InterviewComments comments = new InterviewComments();
                        GetStatusList(comments);
                        return View("InterviewComments", comments);
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                TempData["SuccessErrorMessageInterview"] = "Failed to add Comments.";
                return RedirectToAction("Action", new { interviewId = interviewComments.InterviewId });
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> AssignYourself(int id, string InterviewDate)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Interviewer" || profileName == "InterviewerManagement")
                    {
                        var result = await _meetingService.AssignYourself(id, emailid.ToLower(), Convert.ToDateTime(InterviewDate));
                        TempData["SuccessErrorMessage"] = result;
                        ViewBag.Message = result;
                        return RedirectToAction("OpenInterview");
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("OpenInterview");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> UnassignYourself(int id, bool isPenalty)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var email = HttpContext.Session.GetString("email");
                    email = email.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Interviewer" || profileName == "InterviewerManagement")
                    {
                        if (await _meetingService.UnassignYourself(id, email.ToLower(), isPenalty))
                        {
                            TempData["SuccessErrorMessageMyInterview"] = "Unassignment has been done Successfully!";
                            return Json(new { url = Url.Action("MyInterview", "Interview") });
                        }
                        else
                        {
                            TempData["SuccessErrorMessageMyInterview"] = "Sorry! Could not unassign!";
                            return Json(new { url = Url.Action("MyInterview", "Interview") });
                        }
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessageMyInterview"] = "Sorry! Could not unassign!";
                return RedirectToAction("MyInterview");
            }
        }

        [HttpGet]
        public async Task<IActionResult> AddReport(int interviewId, int irfId, bool isClient = false, bool isClientSelected = false, bool isQC = false, string actiontype = null)
        {
            try
            {
                ViewBag.PageStatus = "Mainurl";
                string userMenus = HttpContext.Session.GetString("userMenus");

                if (userMenus == null)
                {
                    return RedirectToAction("Index", "Home", new
                    {
                        url = "AddReport/Interview/" + interviewId + "/" + irfId + "/" + isClient + "/" + isClientSelected
                    });
                }
                var email = HttpContext.Session.GetString("email");
                email = email.Replace("\"", "");
                ViewBag.EmailId = email;
                bool isAdminHR = false;
                bool isInterviewer = false;
                ViewBag.actiontype = actiontype;
                ViewBag.IRFID = irfId;
                string profileName = HttpContext.Session.GetString("profileName");
                ViewBag.profile = profileName.Replace("\"", "");
                if (profileName != null)
                {
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller")
                    {
                        isAdminHR = true;
                    }
                    else if (profileName == "Interviewer")
                    {
                        isInterviewer = true;
                    }
                }

                var model = await _meetingService.GetInteviewReport(interviewId, irfId);
                if (model == null)
                {
                    if (isAdminHR)
                    {
                        return RedirectToAction("Index");
                    }
                    else if (isInterviewer)
                    {
                        return RedirectToAction("MyInterview");
                    }
                }
                if (profileName == "Client")
                {
                    var client = await _userRepository.GetClientUserDetails(email);
                    model.IsQCRequired = client.IsQCRequired;
                    model.IsRedoInterview = client.IsRedoInterview;
                }
                else
                {
                    model.IsQCRequired = false;
                }
                if (model.QuestionsCollection == null)
                {
                    ViewBag.QuestionsDiscussedCount = 0;
                }
                else
                {
                    foreach (var question in model.QuestionsCollection)
                    {
                        question.Comment = question.Comment.Replace("\t", " ").Replace("\n", " ").Replace("\r", " ");
                    }
                    ViewBag.QuestionsDiscussedCount = model.QuestionsCollection?.Count();
                }
                ViewBag.IsClient = isClient;
                ViewBag.IsClientSelected = isClientSelected;
                model.IsQC = isQC;
                model.IsInterviewer = isInterviewer;
                model.PreviousMeetingStatus = model.MeetingStatus;
                model.Userid = _iRFService.GetUserId(email, profileName);
                if (model.Userid != 0)
                {
                    await GetStatusList(model, irfId, model.Userid);
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
                model.ResourceUrl = Request.Scheme + "://" + Request.Host.Value + Request.Path + Request.QueryString;
                ViewBag.ClientDetails = "";
                if (TempData["SuccessErrorMessageQCForm"] != null)
                {
                    ViewBag.Message = TempData["SuccessErrorMessageQCForm"].ToString();
                    TempData.Remove("SuccessErrorMessageQCForm");
                }
                if (isClient || isClientSelected)
                {
                    ViewBag.ClientDetails = "client";
                    if (model.IsProctor)
                    {
                        return PartialView("ClientProctorReport", model);
                    }
                    else
                    {
                        return PartialView("ClientPartialReport", model);
                    }
                }
                else
                {
                    if (model.IsProctor)
                    {
                        return View("ProctorReport", model);
                    }
                    else
                    {
                        return View(model);
                    }
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("MyInterview");
            }
        }

        [Authorize]
        [HttpPost]
        [DisableRequestSizeLimit]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReport(InterviewReport model)
        {
            try
            {
                ViewBag.PageStatus = "Mainurl";
                ViewBag.ClientDetails = "";
                string OrginalProfileName = string.Empty;
                if (HttpContext.Session.GetString("email") != null &&
                    HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();

                    emailid = emailid.Replace("\"", "");
                    model.UserDetails = emailid;
                    if (profileName == "Admin" || profileName == "Interviewer" || profileName == "SupportAdmin" || profileName == "Quality Controller")
                    {
                        if (model.AudioFile != null)
                        {
                            var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(model.AudioFile);
                            if (fileMimeType == "audio/basic" || fileMimeType == "audio/mpeg" || fileMimeType == "audio/x-wav" || fileMimeType == "video/x-msvideo")
                            {
                                await _azureStorageHelper.InsertBlob(model.AudioFile.FileName, "audios", model.AudioFile);
                                var containerRef = _azureStorageHelper.GetCloudBlobContainer("audios");
                                var blob = containerRef.GetBlockBlobReference(model.AudioFile.FileName);
                                string uploadedaudioFileURL = blob.Uri.ToString();
                                if (uploadedaudioFileURL == null)
                                {
                                    ModelState.AddModelError(string.Empty, "Audio Upload is failed.");
                                    await GetStatusList(model);
                                    return View("AddReport", model);
                                }
                                else
                                {
                                    model.ThumbnailImage = uploadedaudioFileURL;
                                }
                            }
                        }
                        string uploadedvideoFileURL = string.Empty;

                        if (model.VideoFile != null)
                        {
                            var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(model.VideoFile);
                            if (fileMimeType == "video/mpeg" || fileMimeType == "video/mp4"
                                || fileMimeType == "video/quicktime")
                            {
                                AzureCall _call = new AzureCall(_configuration);
                                //var resulttype = _call.CallApi(model.VideoFile, model.VideoFile.FileName, model.VideoFile.Length);
                                var resulttype = _call.CallApiUpload(model.VideoFile);
                                resulttype = resulttype.Replace("\"", "");
                                uploadedvideoFileURL = resulttype;

                                if (uploadedvideoFileURL == null)
                                {
                                    ModelState.AddModelError(string.Empty, "Video Upload is failed.");
                                    await GetStatusList(model);
                                    return View("AddReport", model);
                                }
                                else
                                {
                                    model.VideoLink = uploadedvideoFileURL;
                                }
                            }
                        }
                        else if (model.PageVideoLink != null)
                        {
                            model.VideoLink = model.PageVideoLink;
                        }
                        else if (model.VideoFile != null)
                        {

                            //Start:Generate Thumbnail from Vedio
                            //string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", model.VideoFile.FileName);
                            //using (var stream = new FileStream(path, FileMode.Create))
                            //{
                            //    await model.VideoFile.CopyToAsync(stream);
                            //}

                            //var inputFile = new MediaFile { Filename = path };
                            //string outputFileName = path.Substring(0, path.LastIndexOf(".")) + ".jpg";
                            //var outputFile = new MediaFile { Filename = @outputFileName };
                            //for (int i = 0; i < 1000; i++)
                            //{
                            //    using (var engine = new Engine())
                            //    {
                            //        engine.GetMetadata(inputFile);
                            //        var options = new ConversionOptions { Seek = TimeSpan.FromSeconds(i) };
                            //        if (System.IO.File.Exists(outputFileName))
                            //        {
                            //            System.IO.File.Delete(outputFileName);
                            //            engine.GetThumbnail(inputFile, outputFile, options);
                            //        }
                            //        else
                            //        {
                            //            engine.GetThumbnail(inputFile, outputFile, options);
                            //        }
                            //    }
                            //    var resultset = _azureFaceApi.FaceDetectionResult(outputFileName);
                            //    if (resultset.Contains("faceId"))
                            //    {
                            //        break;
                            //    }
                            //}
                            ////End:Generate Thumbnail from Vedio
                            //using (var stream = System.IO.File.OpenRead(outputFileName))
                            //{
                            //    var imagefile = new FormFile(stream, 0, stream.Length, null, Path.GetFileName(stream.Name))
                            //    {
                            //        Headers = new HeaderDictionary(),
                            //        ContentType = "application/jpg"
                            //    };
                            //    await _azureStorageHelper.InsertBlob(model.VideoFile.FileName, "videos", imagefile);
                            //    var containerImageRef = _azureStorageHelper.GetCloudBlobContainer("videos");
                            //    var imageblob = containerImageRef.GetBlockBlobReference(outputFileName);
                            //    string uploadedImageFileURL = imageblob.Uri.ToString();
                            //    model.ThumbnailImage = uploadedImageFileURL;
                            //    System.IO.File.Delete(path);
                            //    System.IO.File.Delete(outputFileName);
                            //}
                        }
                        if (await _meetingService.AddInterviewReport(model, emailid))
                        {
                            if (model.MeetingStatus == 4 && model.PreviousMeetingStatus != model.MeetingStatus)
                            {
                                //Send mail to client for report
                                //find all IRFowner and Users
                                //var clientlist = _meetingService.FetchClientListByIRFID(model.IRFId);
                                //var maillist = string.Join(";", clientlist);
                                //string sendreporturl = "https://" + Request.Host.ToString() + "/IRF/InstaAddReport?interviewId=" + model.InterviewId + "&irfId=" + model.IRFId;
                                //Mail _mail1 = new Mail
                                //{
                                //    TO = maillist,
                                //    Subject = "Insta Report URL of " + model.CandidateName + " - " + model.PrimarySkill
                                //};
                                //var emailsend1 = _notificationHelper.InstaReportClientNotification(_mail1, sendreporturl, model.CandidateName);
                                //send mail to candiate or interviewer
                                string currenturl = "https://" + Request.Host.ToString() + "/Survey/Index?id=" + model.InterviewId;
                                Mail _mail = new Mail
                                {
                                    TO = model.CandidateEmail,
                                    Subject = "Risebird: Feedback form"
                                };
                                var emailsend = await _notificationHelper.SurveyNotification(_mail, currenturl);
                                WhatsAppVM _whatsAppVM = new WhatsAppVM();
                                _whatsAppVM.Emailid = model.CandidateEmail;
                                _whatsAppVM.buttonlink = !string.IsNullOrEmpty(currenturl) ? currenturl.Replace("https://" + Request.Host.ToString() + "/", "") : "";
                                _whatsAppVM.Event = "Feedback Remainder";
                                await _whatsAppHelper.WhatsAppSendFeedbackRemainder(_whatsAppVM);
                            }
                            TempData["SuccessErrorMessageInterview"] = "Report has been saved Successfully!";
                            if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller")
                            {
                                return RedirectToAction("Index");
                            }
                            return RedirectToAction("MyInterview");
                        }
                        else
                        {
                            TempData["SuccessErrorMessageInterview"] = "Report was not saved.";
                            if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                            {
                                return RedirectToAction("Index");
                            }
                            return RedirectToAction("MyInterview");
                        }
                    }
                    else
                    {
                        return RedirectToAction("Index", "Interview");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessageInterview"] = "Report was not saved.";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        private void GetStatusList(InterviewComments model)
        {
            var interviewStatuses = from InterviewStatus s in Enum.GetValues(typeof(InterviewStatus))
                                    select new { ID = (int)s, Name = s.GetDescription() };
            model.InterviewStatusList = new SelectList(interviewStatuses, "ID", "Name");

            var meetingStatuses = from MeetingStatus s in Enum.GetValues(typeof(MeetingStatus))
                                  select new { ID = (int)s, Name = s.GetDescription() };

            model.MeetingStatusList = new SelectList(meetingStatuses, "ID", "Name");

            var fraudCheckStatuses = from FraudCheckStatus s in Enum.GetValues(typeof(FraudCheckStatus))
                                     select new { ID = (int)s, Name = s.GetDescription() };
            model.FraudCheckStatusList = new SelectList(fraudCheckStatuses, "ID", "Name");

            //var qcStatuses = from QCStatus s in Enum.GetValues(typeof(QCStatus))
            //                 select new { ID = (int)s, Name = s.GetDescription() };
            //model.QCStatusList = new SelectList(fraudCheckStatuses, "ID", "Name");

            var trainingStatuses = from Entities.Enum.TrainingStatus s in Enum.GetValues(typeof(Entities.Enum.TrainingStatus))
                                   select new { ID = (int)s, Name = s.GetDescription() };
            model.TrainingStatusList = new SelectList(fraudCheckStatuses, "ID", "Name");

            var finalInterviewMeetingStatuses = from FinalInterviewMeetingStatus s in Enum.GetValues(typeof(FinalInterviewMeetingStatus))
                                                select new { ID = (int)s, Name = s.GetDescription() };
            model.FinalInterviewMeetingStatusList = new SelectList(finalInterviewMeetingStatuses, "ID", "Name");

            var l2InterviewStatuses = from L2InterviewStatus s in Enum.GetValues(typeof(L2InterviewStatus))
                                      select new { ID = (int)s, Name = s.GetDescription() };
            model.L2InterviewStatusList = new SelectList(l2InterviewStatuses, "ID", "Name");

            var clntInterviewStatuses = from ClientInterviewStatus s in Enum.GetValues(typeof(ClientInterviewStatus))
                                        select new { ID = (int)s, Name = s.GetDescription() };
            model.ClientInterviewStatusList = new SelectList(clntInterviewStatuses, "ID", "Name");

            var offerStatuses = from OfferStatus s in Enum.GetValues(typeof(OfferStatus))
                                select new { ID = (int)s, Name = s.GetDescription() };
            model.OfferStatusList = new SelectList(offerStatuses, "ID", "Name");

            var joiningStatuses = from JoiningStatus s in Enum.GetValues(typeof(JoiningStatus))
                                  select new { ID = (int)s, Name = s.GetDescription() };
            model.JoiningStatusList = new SelectList(joiningStatuses, "ID", "Name");

            var preparationStatuses = from PreparationStatus s in Enum.GetValues(typeof(PreparationStatus))
                                      select new { ID = (int)s, Name = s.GetDescription() };
            model.PreparationStatusList = new SelectList(preparationStatuses, "ID", "Name");
        }

        [HttpGet]
        private async Task GetStatusList(InterviewReport model, int irfId = 0, int userId = 0)
        {
            var interviewStatuses = from InterviewStatus s in Enum.GetValues(typeof(InterviewStatus))
                                    select new { ID = (int)s, Name = s.GetDescription() };
            model.InterviewStatusList = new SelectList(interviewStatuses, "ID", "Name");

            var meetingStatuses = from MeetingStatus s in Enum.GetValues(typeof(MeetingStatus))
                                  select new { ID = (int)s, Name = s.GetDescription() };

            model.MeetingStatusList = new SelectList(meetingStatuses, "ID", "Name");

            model.Level = new SelectList(await _iRFRepository.GetLevels(), "id", "Name");
            model.LevelTech = new SelectList(await _iRFRepository.GetLevelTech(), "id", "Name");
            var questionsLevel = from QuestionsLevel s in Enum.GetValues(typeof(QuestionsLevel))
                                 select new { ID = (int)s, Name = s.GetDescription() };
            model.QuestionsLevelList = new SelectList(questionsLevel, "ID", "Name");
            var questions = _meetingService.GetTechSkillComments(irfId, userId).Result;
            model.QuestionsList = new SelectList(questions, "Comment", "Comment");
            model.QuestionsSkillMapping = questions;
            model.QuestionsListData = JsonConvert.SerializeObject(questions.Select(x => (x.Comment.Replace("\r", "").Replace("\n", ""), x.SkillName)));
        }

        //[Authorize]
        //[HttpGet]
        //public IActionResult SampleQuestions(int id)
        //{
        //    if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
        //    {
        //        var profileName = HttpContext.Session.GetString("profileName")?.Trim();
        //        profileName = profileName.Replace("\"", "");
        //        var emailid = HttpContext.Session.GetString("email")?.Trim();
        //        emailid = emailid.Replace("\"", "");
        //        if (profileName == "Admin" || profileName == "HR" || profileName=="Interviewer" || profileName == "Quality Controller")
        //        {
        //            TechSkillComments model = new TechSkillComments();
        //            var result = _iRFService.GetTechSkillComments(id);
        //            //if (result.Count() > 0)
        //            model.SkillCollection = result;
        //            var skills = _iRFService.GetTechSkills(id);
        //            ViewBag.Skills = new SelectList(skills, "id", "Name");
        //            model.SkillsOptionsData = JsonConvert.SerializeObject(skills);
        //            model.IRFId = id;
        //            model.Profile = profileName;
        //            model.UserId = _iRFService.GetUserId(emailid);
        //            return View(model);
        //        }
        //        else
        //        {
        //            return RedirectToAction("SignOut", "Session");
        //        }

        //    }
        //    else
        //    {
        //        return RedirectToAction("SignOut", "Session");
        //    }
        //}

        //[Authorize]
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public IActionResult SampleQuestions(TechSkillComments model)
        //{
        //    try
        //    {
        //        if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
        //        {
        //            var profileName = HttpContext.Session.GetString("profileName")?.Trim();
        //            profileName = profileName.Replace("\"", "");
        //            if (profileName == "Admin" || profileName == "HR" || profileName == "Interviewer" || profileName == "Quality Controller")
        //            {
        //                if (ModelState.IsValid)
        //                {
        //                    if (_iRFService.AddSkillComments(model))
        //                    {
        //                        TempData["SuccessErrorMessage"] = "Questions saved Successfully";
        //                        return RedirectToAction("MyInterview");
        //                    }
        //                    else
        //                    {
        //                        TempData["SuccessErrorMessage"] = "Questions were not saved.";
        //                        return RedirectToAction("MyInterview");
        //                    }
        //                }
        //                else
        //                {
        //                    var skills = _iRFService.GetTechSkills(model.IRFId);
        //                    ViewBag.Skills = new SelectList(skills, "id", "Name");
        //                    model.SkillsOptionsData = JsonConvert.SerializeObject(skills);

        //                    model.Profile = profileName;
        //                    //ModelState.AddModelError
        //                    return View(model);
        //                }


        //            }
        //            else
        //            {
        //                return RedirectToAction("SignOut", "Session");
        //            }

        //        }
        //        else
        //        {
        //            return RedirectToAction("SignOut", "Session");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //        TempData["SuccessErrorMessage"] = "Questions were not saved";
        //        return RedirectToAction("MyInterview");
        //    }
        //}

        //GetClientsByRoles
        [HttpGet]
        public SelectList GetClientsByRoles(string val)
        {
            var list = new List<SelectListItem>();
            var clients = _meetingService.GetClientsByRoles(val);
            if (clients != null && clients.Count() > 0)
            {
                return new SelectList(clients, "ClientRoleId", "EmailId");
            }
            else
            {
                list.Add(new SelectListItem { Text = "No Clients", Value = "" });
                return new SelectList(list, "Value", "Text");
            }
        }

        [HttpGet]
        public bool ChangeApproval(string imid, string value, string comment)
        {
            var result = _meetingService.PostApproval(imid, value, comment);
            if (result)
            {
                TempData["SuccessErrorMessage"] = "Saved Successfully!";
            }
            else
                TempData["SuccessErrorMessage"] = "Failed";

            return result;
        }

        [HttpGet]
        public bool SaveClientFeedback(int imid, int isApproved, string approvalComment, int isSelected, string selectionComment, int joiningStatus, string joiningComment, int offerStatus, string offerComment, bool movetoCP)
        {
            try
            {
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                var result = _meetingService.SaveClientFeedback(imid, isApproved, approvalComment, isSelected, selectionComment, joiningStatus, joiningComment, offerStatus, offerComment, movetoCP, emailid);
                if (result)
                {
                    TempData["SuccessErrorMessage"] = "Saved Successfully!";
                }
                else
                    TempData["SuccessErrorMessage"] = "Failed";

                return result;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<bool> SaveQCFeedback(string imid, string qcStatus, string comment, bool FraudLipSync, bool QuestionsRelevance, bool HandsOnCoding, bool BiasAndCandidateHandling, bool TechnicalCommunicationSkills, bool ChapteringCompleted, bool QuestionsFilled, bool TechSkillsMetric, bool SelectabilityMetric, bool JoinabilityMetric, bool GeneralSkillMetric, bool CommentsFilled, bool MeaningfulAssessments, bool CorrectEnglishGrammar, bool ProctoringReportFilled, string oldQcStatus)
        {
            var emailid = HttpContext.Session.GetString("email")?.Trim();
            emailid = emailid.Replace("\"", "");
            var result = await _meetingService.SaveQCFeedback(imid, qcStatus, comment, FraudLipSync, QuestionsRelevance, HandsOnCoding, BiasAndCandidateHandling, TechnicalCommunicationSkills, ChapteringCompleted, QuestionsFilled, TechSkillsMetric, SelectabilityMetric, JoinabilityMetric, GeneralSkillMetric, CommentsFilled, MeaningfulAssessments, CorrectEnglishGrammar, ProctoringReportFilled, oldQcStatus, emailid);
            if (result)
            {
                TempData["SuccessErrorMessage"] = "Saved Successfully!";
            }
            else
                TempData["SuccessErrorMessage"] = "Failed";

            return result;
        }

        [HttpGet]
        public ActionResult getApprovalStatusList()
        {
            var approvalStatuses = from NextLevelApprovalStatus s in Enum.GetValues(typeof(NextLevelApprovalStatus))
                                   select new { ID = (int)s, Name = s.GetDescription() };
            //model.OfferStatusList = new SelectList(offerStatuses, "ID", "Name");
            return Json((approvalStatuses.Select(x => new
            {
                Value = x.ID,
                Description = x.Name
            }).ToList()));
        }

        [HttpGet]
        public ActionResult getQCStatusList()
        {
            var qcStatuses = from QCStatus s in Enum.GetValues(typeof(QCStatus))
                             select new { ID = (int)s, Name = s.GetDescription() };
            //model.OfferStatusList = new SelectList(offerStatuses, "ID", "Name");
            return Json((qcStatuses.Select(x => new
            {
                Value = x.ID,
                Description = x.Name
            }).ToList()));
        }

        [HttpGet]
        public ActionResult getSelectionStatusList()
        {
            var selectionStatuses = from FinalSelectionStatus s in Enum.GetValues(typeof(FinalSelectionStatus))
                                    select new { ID = (int)s, Name = s.GetDescription() };
            //model.OfferStatusList = new SelectList(offerStatuses, "ID", "Name");
            return Json((selectionStatuses.Select(x => new
            {
                Value = x.ID,
                Description = x.Name
            }).ToList()));
        }

        [HttpGet]
        public ActionResult getOfferStatusList()
        {
            var selectionStatuses = from OfferStatus s in Enum.GetValues(typeof(OfferStatus))
                                    select new { ID = (int)s, Name = s.GetDescription() };
            return Json((selectionStatuses.Select(x => new
            {
                Value = x.ID,
                Description = x.Name
            }).ToList()));
        }

        [HttpGet]
        public ActionResult getJoiningStatusList()
        {
            var selectionStatuses = from JoiningStatus s in Enum.GetValues(typeof(JoiningStatus))
                                    select new { ID = (int)s, Name = s.GetDescription() };
            //model.OfferStatusList = new SelectList(offerStatuses, "ID", "Name");
            return Json((selectionStatuses.Select(x => new
            {
                Value = x.ID,
                Description = x.Name
            }).ToList()));
        }

        [HttpGet]
        public bool ChangeSelection(string imid, string value, string comment)
        {
            var result = _meetingService.PostSelectionStatus(imid, value, comment);
            if (result)
            {
                TempData["SuccessErrorMessageInterview"] = "Saved Successfully!";

            }
            else
                TempData["SuccessErrorMessageInterview"] = "Failed";

            return result;

        }

        /// <summary>
        /// Export selected candidates data to excel
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet]
        public async Task<FileContentResult> AllExporttoExcel()
        {
            string userEmailId = string.Empty;
            ExcelPackage Ep = new ExcelPackage();
            ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("All Candidate Report");
            string fileName = "Risebird" + DateTime.Now.ToString("dd/MM/yyyy") + "_All_Candidate_Status_Report" + ".xlsx";
            try
            {
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");

                var selectedMeeting = new List<MeetingVM>();
                var meetings = await _meetingService.GetClientMeetingList(emailid);

                meetings = meetings.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();
                foreach (var meeting in meetings)
                {
                    if (meeting.InterviewStatus != null)
                    {
                        meeting.InterviewStatusDescription = ((InterviewStatus)meeting.InterviewStatus).GetDescription();
                    }
                    if (meeting.MeetingStatus != null)
                    {
                        meeting.MeetingStatusDescription = ((MeetingStatus)meeting.MeetingStatus).GetDescription();
                    }
                }
                foreach (var m in meetings)
                {
                    if (m.InterviewStatus.HasValue)
                    {
                        m.intervieweStatusStatDesc = ((InterviewStatus)(m.InterviewStatus)).GetDescription();
                    }
                    if (m.L2InterviewStatus.HasValue)
                    {
                        m.L2InterviewStatDesc = ((L2InterviewStatus)(m.L2InterviewStatus)).GetDescription();
                    }
                    if (m.L2InterviewStatus.HasValue)
                    {
                        m.ClientInterviewStatDesc = ((ClientInterviewStatus)(m.ClientInterviewStatus)).GetDescription();
                    }
                    if (m.JoiningStatus.HasValue)
                    {
                        m.JoiningStatusStatDesc = ((JoiningStatus)(m.JoiningStatus)).GetDescription();
                    }
                    if (m.PreparationStatus.HasValue)
                    {
                        m.PreparationStatusDesc = ((PreparationStatus)(m.PreparationStatus)).GetDescription();
                    }
                    if (m.OfferStatus.HasValue)
                    {
                        m.OfferStatusStatDesc = ((PreparationStatus)(m.OfferStatus)).GetDescription();
                    }
                    if (m.MeetingStatus.HasValue)
                    {
                        m.MeetingStatusStatusDesc = ((MeetingStatus)(m.MeetingStatus)).GetDescription();
                    }
                    if (m.QCStatus.HasValue)
                    {
                        m.QCStatusStatusDesc = ((QCStatus)(m.QCStatus)).GetDescription();
                    }
                }

                Sheet.Cells["A1:S1"].Style.Font.Bold = true;
                Sheet.Cells["A1:S1"].Style.Font.Size = 12;
                Sheet.Cells["A1:S1"].Style.WrapText = true;
                //Sheet.Column(1).Width = 30;
                //Sheet.Column(2).Width = 30;
                int startColumn = 1; int endColumn = 18;
                for (int a = startColumn; a <= endColumn; a++)
                {
                    Sheet.Column(a).Width = 30;
                }
                Sheet.Cells[1, 1].Value = "Interview Date";
                Sheet.Cells[1, 2].Value = "Candidate name";
                Sheet.Cells[1, 3].Value = "Client HR";
                Sheet.Cells[1, 4].Value = "End Client Name";
                Sheet.Cells[1, 5].Value = "Primary Skill";
                Sheet.Cells[1, 6].Value = "Confirmation Status";
                Sheet.Cells[1, 7].Value = "Meeting Status";
                Sheet.Cells[1, 8].Value = "Interview Status";
                Sheet.Cells[1, 9].Value = "Video Link";
                Sheet.Cells[1, 10].Value = "Interviewer Comment";
                Sheet.Cells[1, 11].Value = "Report Link";
                Sheet.Cells[1, 12].Value = "Resume";
                Sheet.Cells[1, 13].Value = "Candidate Contact";
                Sheet.Cells[1, 14].Value = "Current Employer";
                Sheet.Cells[1, 15].Value = "Expected Salary";
                Sheet.Cells[1, 16].Value = "Expected Joining Time";
                Sheet.Cells[1, 17].Value = "Preferred Location";
                Sheet.Cells[1, 18].Value = "Staffing Agency";

                int i = 2;
                //string LastAction = null; ;
                meetings = meetings.OrderBy(x => x.interviewDate).ToList();
                if (meetings.Count() > 0)
                {
                    foreach (var item in meetings)
                    {
                        Sheet.Cells[i, 1].Value = item.interviewDate.ToString("MM/dd/yyyy HH:mm");
                        Sheet.Cells[i, 2].Value = item.candidateName;
                        Sheet.Cells[i, 3].Value = item.ClientHrContact;
                        Sheet.Cells[i, 4].Value = item.ClientName;
                        Sheet.Cells[i, 5].Value = item.JDTiltle.Split("|")[0];
                        Sheet.Cells[i, 6].Value = item.confirmationStatus;
                        Sheet.Cells[i, 7].Value = item.MeetingStatusDescription;
                        if (item.QCInterview > 0)
                        {
                            Sheet.Cells[i, 8].Value = item.intervieweStatusStatDesc;
                        }
                        else
                        {
                            Sheet.Cells[i, 8].Value = "";
                        }
                        if (!string.IsNullOrEmpty(item.VideoLink) && item.QCInterview > 0)
                        {
                            try
                            {
                                Sheet.Cells[i, 9].Hyperlink = new Uri(item.VideoLink, UriKind.Absolute);
                                Sheet.Cells[i, 9].Value = "Video Link";
                                Sheet.Cells[i, 9].Style.Font.UnderLine = true;
                                Sheet.Cells[i, 9].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                            }
                            catch (Exception)
                            {
                                Sheet.Cells[i, 9].Value = "No Video";
                            }
                        }
                        else
                        {
                            Sheet.Cells[i, 9].Value = "No Video";
                        }
                        Sheet.Cells[i, 10].Value = item.InterviewerComment;
                        if (item.QCInterview > 0)
                        {
                            Sheet.Cells[i, 11].Hyperlink = new Uri("https://portal.risebird.io/Interview/AddReport?interviewId=" + item.imid + "&irfId=" + item.irfIntId + "&isClient=" + true, UriKind.Absolute);
                            Sheet.Cells[i, 11].Value = "Report Link";
                            Sheet.Cells[i, 11].Style.Font.UnderLine = true;
                            Sheet.Cells[i, 11].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                        }
                        else
                        {
                            Sheet.Cells[i, 11].Value = "No Report";
                        }
                        if (!string.IsNullOrEmpty(item.resumeLink))
                        {
                            try
                            {
                                Sheet.Cells[i, 12].Hyperlink = new Uri(item.resumeLink, UriKind.Absolute);
                                Sheet.Cells[i, 12].Value = "Resume Link";
                                Sheet.Cells[i, 12].Style.Font.UnderLine = true;
                                Sheet.Cells[i, 12].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                            }
                            catch (Exception)
                            {
                                Sheet.Cells[i, 12].Value = "No Resume";
                            }
                        }
                        else
                        {
                            Sheet.Cells[i, 12].Value = "No Resume";
                        }
                        Sheet.Cells[i, 13].Value = item.MobileNumber;
                        Sheet.Cells[i, 14].Value = item.currentEmployerName;
                        Sheet.Cells[i, 15].Value = item.expectedPerHourRate;
                        Sheet.Cells[i, 16].Value = item.joiningTime;
                        Sheet.Cells[i, 17].Value = item.preferredLocation;
                        Sheet.Cells[i, 18].Value = item.staffingAgency;
                        i++;
                    }
                    return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
                else
                {
                    ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                    Rng.Value = "No data available in table !!!";
                    return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception)
            {
                ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                Rng.Value = "Data export failed !!!";
                return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<FileContentResult> MyInterviewsExporttoExcel()
        {
            string userEmailId = string.Empty;
            ExcelPackage Ep = new ExcelPackage();
            ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("My Interviews");
            string fileName = "Risebird" + DateTime.Now.ToString("dd/MM/yyyy") + "_My_Interviews" + ".xlsx";
            try
            {
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                var myInterview = await _meetingService.GetAllAssignedInterviews(emailid);
                foreach (var item in myInterview)
                {
                    if (item.IsApproved.HasValue)
                    {
                        item.NextLevelApprovalStat = ((NextLevelApprovalStatus)item.IsApproved).GetDescription();
                    }
                }
                //myInterview = myInterview.Where(x => x.interviewDate.Date == DateTime.Now.Date).ToList();
                myInterview = myInterview.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();
                myInterview = myInterview.Select(x => { x.InterviewStatusDesc = ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription(); x.MeetingStatusDesc = ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription(); return x; }).ToList();
                if (HttpContext.Session.GetString("MyInterviewfilters") != null)
                {
                    var filter = JsonConvert.DeserializeObject<MyInterviewFilter>(HttpContext.Session.GetString("MyInterviewfilters"));

                    if (filter.StartDate != null && filter.EndDate != null)
                    {
                        myInterview = myInterview.Where(t => t.interviewDate.Date >= filter.StartDate && t.interviewDate.Date <= filter.EndDate).ToList();
                    }
                    else if (filter.StartDate != null && filter.EndDate == null)
                    {
                        filter.EndDate = filter.StartDate;
                        myInterview = myInterview.Where(t => t.interviewDate.Date >= filter.StartDate && t.interviewDate.Date <= filter.EndDate).ToList();
                    }
                    else if (filter.StartDate == null && filter.EndDate != null)
                    {
                        filter.StartDate = filter.EndDate;
                        myInterview = myInterview.Where(t => t.interviewDate.Date >= filter.StartDate && t.interviewDate.Date <= filter.EndDate).ToList();
                    }

                    //filter.InterviewDuration = filter.InterviewDuration == null ? filter.InterviewDuration = myInterview.Select(x => x.interviewDuration).ToArray() : filter.InterviewDuration = filter.InterviewDuration;
                    filter.InterviewSlot = filter.InterviewSlot == null ? filter.InterviewSlot = myInterview.Select(x => x.interviewSlot).ToArray() : filter.InterviewSlot = filter.InterviewSlot;
                    filter.JobLevel = filter.JobLevel == null ? filter.JobLevel = myInterview.Select(x => x.JobLevelDesc).ToArray() : filter.JobLevel = filter.JobLevel;
                    filter.InterviewDate = filter.InterviewDate == null ? filter.InterviewDate = myInterview.Select(x => Convert.ToString(x.interviewDate)).ToArray() : filter.InterviewDate = filter.InterviewDate;
                    filter.CandidateName = filter.CandidateName == null ? filter.CandidateName = myInterview.Select(x => x.candidateName).ToArray() : filter.CandidateName = filter.CandidateName;
                    filter.RisebirdHR = filter.RisebirdHR == null ? filter.RisebirdHR = myInterview.Select(x => x.risebirdHR).ToArray() : filter.RisebirdHR = filter.RisebirdHR;
                    //filter.InterviewDuration.Contains(c.interviewDuration)
                    myInterview = myInterview.Where(c => (filter.InterviewSlot.Count() > 0 ? filter.InterviewSlot.Contains(c.interviewSlot) : c.interviewSlot == null)
                    && (filter.JobLevel.Count() > 0 ? filter.JobLevel.Contains(c.JobLevelDesc) : c.JobLevelDesc == null)
                   && (filter.InterviewDate.Count() > 0 ? filter.InterviewDate.Contains(Convert.ToString(c.interviewDate)) : c.interviewDate == DateTime.MinValue)
                   && (filter.CandidateName.Count() > 0 ? filter.CandidateName.Contains(c.candidateName) : c.candidateName == null)
                   && (filter.RisebirdHR.Count() > 0 ? filter.RisebirdHR.Contains(c.risebirdHR) : c.risebirdHR == null)).ToList();

                    if (myInterview.Count == 0 && filter.StartDate != null && filter.EndDate != null)
                    {
                        myInterview = await _meetingService.GetAllAssignedInterviews(emailid);
                        foreach (var item in myInterview)
                        {
                            if (item.InterviewStatus.HasValue)
                            {
                                item.InterviewStatusDesc = ((InterviewStatus)(item.InterviewStatus)).GetDescription();
                            }
                            if (item.MeetingStatus.HasValue)
                            {
                                item.MeetingStatusDesc = ((MeetingStatus)(item.MeetingStatus)).GetDescription();
                            }
                        }
                        myInterview = myInterview.Where(t => t.interviewDate.Date >= filter.StartDate && t.interviewDate.Date <= filter.EndDate).ToList();
                    }
                    else if (myInterview.Count == 0 && filter.StartDate != null && filter.EndDate == null)
                    {
                        filter.EndDate = filter.StartDate;
                        myInterview = await _meetingService.GetAllAssignedInterviews(emailid);
                        foreach (var item in myInterview)
                        {
                            if (item.InterviewStatus.HasValue)
                            {
                                item.InterviewStatusDesc = ((InterviewStatus)(item.InterviewStatus)).GetDescription();
                            }
                            if (item.MeetingStatus.HasValue)
                            {
                                item.MeetingStatusDesc = ((MeetingStatus)(item.MeetingStatus)).GetDescription();
                            }
                        }
                        myInterview = myInterview.Where(t => t.interviewDate.Date >= filter.StartDate && t.interviewDate.Date <= filter.EndDate).ToList();
                    }
                    else if (myInterview.Count == 0 && filter.StartDate == null && filter.EndDate != null)
                    {
                        filter.StartDate = filter.EndDate;
                        myInterview = await _meetingService.GetAllAssignedInterviews(emailid);
                        foreach (var item in myInterview)
                        {
                            if (item.InterviewStatus.HasValue)
                            {
                                item.InterviewStatusDesc = ((InterviewStatus)(item.InterviewStatus)).GetDescription();
                            }
                            if (item.MeetingStatus.HasValue)
                            {
                                item.MeetingStatusDesc = ((MeetingStatus)(item.MeetingStatus)).GetDescription();
                            }
                        }
                        myInterview = myInterview.Where(t => t.interviewDate.Date >= filter.StartDate && t.interviewDate.Date <= filter.EndDate).ToList();
                    }
                }
                if (HttpContext.Session.GetString("MyInterviewfilters") == null)
                {
                    myInterview = myInterview.Where(x => x.interviewDate.Date >= DateTime.Now.Date).ToList();
                }
                Sheet.Cells["A1:P1"].Style.Font.Bold = true;
                Sheet.Cells["A1:P1"].Style.Font.Size = 12;
                Sheet.Cells["A1:P1"].Style.WrapText = true;
                //Sheet.Column(1).Width = 30;
                //Sheet.Column(2).Width = 30;
                int startColumn = 1; int endColumn = 14;
                for (int a = startColumn; a <= endColumn; a++)
                {
                    Sheet.Column(a).Width = 35;
                }
                Sheet.Cells[1, 1].Value = "Interview Date";
                Sheet.Cells[1, 2].Value = "Slot";
                Sheet.Cells[1, 3].Value = "Duration";
                Sheet.Cells[1, 4].Value = "Candidate Name";
                Sheet.Cells[1, 5].Value = "Candidate Email";
                Sheet.Cells[1, 6].Value = "Meeting Status";
                Sheet.Cells[1, 7].Value = "Interview Status";
                Sheet.Cells[1, 8].Value = "Resume";
                Sheet.Cells[1, 9].Value = "JD Link";
                Sheet.Cells[1, 10].Value = "Zoom ID";
                Sheet.Cells[1, 11].Value = "Video Link";
                Sheet.Cells[1, 12].Value = "Video Duration";
                Sheet.Cells[1, 13].Value = "Approval Comments";

                //ExcelHyperLink excelHyperLink = new ExcelHyperLink();
                int i = 2;
                //string LastAction = null; ;
                if (myInterview.Count() > 0)
                {
                    foreach (var item in myInterview)
                    {
                        Sheet.Cells[i, 1].Value = item.interviewDate.ToString("MM/dd/yyyy HH:mm");
                        Sheet.Cells[i, 2].Value = item.interviewSlot;
                        Sheet.Cells[i, 3].Value = item.interviewDuration;
                        Sheet.Cells[i, 4].Value = item.candidateName;
                        Sheet.Cells[i, 5].Value = item.candidateEmail;
                        Sheet.Cells[i, 6].Value = item.MeetingStatusDesc;
                        Sheet.Cells[i, 7].Value = item.InterviewStatusDesc;
                        if (!string.IsNullOrEmpty(item.resumeLink))
                        {
                            try
                            {
                                Sheet.Cells[i, 8].Hyperlink = new Uri(item.resumeLink, UriKind.Absolute);
                                Sheet.Cells[i, 8].Value = "Resume";
                                Sheet.Cells[i, 8].Style.Font.UnderLine = true;
                                Sheet.Cells[i, 8].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                            }
                            catch (Exception)
                            {
                                Sheet.Cells[i, 8].Value = "No Resume";
                            }
                        }
                        else
                        {
                            Sheet.Cells[i, 8].Value = "No Resume";
                        }
                        if (!string.IsNullOrEmpty(item.JDLink))
                        {
                            try
                            {
                                Sheet.Cells[i, 9].Hyperlink = new Uri(item.JDLink, UriKind.Absolute);
                                Sheet.Cells[i, 9].Value = "JD Link";
                                Sheet.Cells[i, 9].Style.Font.UnderLine = true;
                                Sheet.Cells[i, 9].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                            }
                            catch (Exception)
                            {
                                Sheet.Cells[i, 9].Value = "No Link";
                            }
                        }
                        else
                        {
                            Sheet.Cells[i, 9].Value = "No Link";
                        }
                        Sheet.Cells[i, 10].Value = item.risebirdHR;
                        if (!string.IsNullOrEmpty(item.videoLink))
                        {
                            try
                            {
                                Sheet.Cells[i, 11].Hyperlink = new Uri(item.videoLink, UriKind.Absolute);
                                Sheet.Cells[i, 11].Value = "Video Link";
                                Sheet.Cells[i, 11].Style.Font.UnderLine = true;
                                Sheet.Cells[i, 11].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                                Sheet.Cells[i, 12].Value = item.VideoDuration == 0 ? string.Empty : item.VideoDuration.ToString();
                            }
                            catch (Exception)
                            {
                                Sheet.Cells[i, 11].Value = "No Video";
                                Sheet.Cells[i, 12].Value = "NA";
                            }
                        }
                        else
                        {
                            Sheet.Cells[i, 11].Value = "No Video";
                            Sheet.Cells[i, 12].Value = "NA";
                        }
                        Sheet.Cells[i, 13].Value = item.NextLevelApprovalStat ?? "Comments not submitted.";
                        i++;
                    }
                    return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
                else
                {
                    ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                    Rng.Value = "No data available in table !!!";
                    return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception)
            {
                ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                Rng.Value = "Data export failed !!!";
                return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        //[Authorize]
        //[HttpGet]
        //public async Task<IActionResult> CandidateInterviews(string status)
        //{
        //    try
        //    {
        //        if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
        //        {
        //            var profileName = HttpContext.Session.GetString("profileName")?.Trim();
        //            profileName = profileName.Replace("\"", "");
        //            if (profileName == "Candidate")
        //            {
        //                var emailid = HttpContext.Session.GetString("email")?.Trim();
        //                emailid = emailid.Replace("\"", "");
        //                var myInterview = _meetingService.GetCandidateInterviews(emailid, status);
        //                myInterview = myInterview.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); x.JobLevelDesc = ((JobLevels)Convert.ToInt32(x.JobLevel)).GetDescription(); return x; }).ToList();
        //                foreach (var item in myInterview)
        //                {
        //                    if (!string.IsNullOrEmpty(item.resumeLink))
        //                    {
        //                        try
        //                        {
        //                            item.resumeLink = new Uri(item.resumeLink, UriKind.Absolute).ToString();

        //                        }
        //                        catch (Exception)
        //                        {
        //                            item.resumeLink = "";
        //                        }
        //                    }
        //                    else
        //                    {
        //                        item.resumeLink = "";
        //                    }

        //                    if (item.InterviewStatus.HasValue)
        //                    {
        //                        item.InterviewStatusDesc = ((InterviewStatus)(item.InterviewStatus)).GetDescription();
        //                    }
        //                    if (item.MeetingStatus.HasValue)
        //                    {
        //                        item.MeetingStatusDesc = ((MeetingStatus)(item.MeetingStatus)).GetDescription();

        //                    }

        //                    if (item.interviewDate <= DateTime.Now)
        //                    {
        //                        item.CancelationStatus = "N";
        //                    }
        //                    else
        //                    {
        //                        item.CancelationStatus = "Y";
        //                    }
        //                }
        //                return View(myInterview);
        //            }
        //            else
        //            {
        //                return RedirectToAction("SignOut", "Session");
        //            }
        //        }
        //        else
        //        {
        //            return RedirectToAction("SignOut", "Session");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //        return null;
        //    }
        //}

        [HttpGet]
        public IActionResult UploadAudio(int candidateId, int irfId, string rfName)
        {
            try
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "Interviewer")
                {
                    return View(new CandidateAudio());
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Candidates", new { id = irfId.ToString(), irfName = rfName });
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> CancelInterview(int id, DateTime interviewDate, string candidateEmail)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var email = HttpContext.Session.GetString("email");
                    email = email.Replace("\"", "");
                    if (profileName == "Candidate")
                    {
                        //if (_meetingService.CancelInterview(id, interviewDate.ToString("dd MMM yyyy"), candidateEmail))
                        if (await _meetingService.PublicCancelInterview(id, email))
                        {
                            var updatedStatus = await _iRFRepository.UpdateCancellationSource(id, email);
                            var result = await _candidateService.FetchChannelOwnerandInterviewer(id);
                            var irfOwnersAndRestrictedIrfOwner = await _candidateService.GetCandidateStaffingAgencyAndClientHRByInterviewId(id);
                            List<string> interviewers = new List<string>();
                            if (!string.IsNullOrEmpty(result.interviewer))
                            {
                                interviewers.Add(result.interviewer);
                            }
                            if (!string.IsNullOrEmpty(result.EmailId))
                            {
                                interviewers.Add(result.EmailId);
                            }
                            await _notificationHelper.SendInterviewCancelledMail(interviewers, result.interviewDate.ToString(), irfOwnersAndRestrictedIrfOwner?.ToList(), null, id);
                            //TempData["SuccessErrorMessage"] = "Interview cancelled successfully";
                            return Json(new { url = Url.Action("CandidateInterviews", "Interview") });
                        }
                        else
                        {
                            //TempData["SuccessErrorMessage"] = "Sorry! Could not cancel interview!";
                            return Json(new { url = Url.Action("CandidateInterviews", "Interview") });
                        }
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessageMyInterview"] = "Unassignment is Failed";
                return RedirectToAction("MyInterview");
            }
        }

        [HttpGet]
        public async Task<IActionResult> VerifyAudio(int candidateId, int irfId, int meetingId)
        {
            try
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                {
                    var model = await _meetingService.GetCandidateAudios(candidateId, irfId);
                    model.MeetingId = meetingId;
                    return View(model);
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        [HttpPost]
        [DisableRequestSizeLimit]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyAudio(VerifyCandidateAudio model)
        {

            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        if (model.AudioFile != null)
                        {
                            var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(model.AudioFile);
                            if (fileMimeType == "audio/basic" || fileMimeType == "audio/mpeg" || fileMimeType == "audio/x-wav" || fileMimeType == "video/x-msvideo")
                            {
                                await _azureStorageHelper.InsertBlob(model.AudioFile.FileName, "audios", model.AudioFile);
                                var containerRef = _azureStorageHelper.GetCloudBlobContainer("audios");
                                var blob = containerRef.GetBlockBlobReference(model.AudioFile.FileName);
                                string uploadedaudioFileURL = blob.Uri.ToString();
                                if (uploadedaudioFileURL == null)
                                {
                                    ModelState.AddModelError(string.Empty, "Audio Upload failed.");
                                    return View("VerifyAudio", model);
                                }
                                else
                                {
                                    model.AudioFileLink = uploadedaudioFileURL;
                                }
                            }
                        }
                        var result = _meetingService.VerifyCandidateVoice(model.VoiceItUserId, model.AudioFileLink, model.PhraseDetail, model.MeetingId, "interview");
                        TempData["SuccessErrorMessageInterview"] = result;
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessageInterview"] = "Audio verification has been Failed.";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> VerifyMyInterviewAudio(int candidateId, int irfId, int meetingId)
        {
            try
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Interviewer")
                {
                    var model = await _meetingService.GetCandidateAudios(candidateId, irfId);
                    model.MeetingId = meetingId;
                    return View(model);
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("MyInterview");
            }
        }

        [Authorize]
        [HttpPost]
        [DisableRequestSizeLimit]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyMyInterviewAudio(VerifyCandidateAudio model)
        {

            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Interviewer")
                    {
                        if (model.AudioFile != null)
                        {
                            var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(model.AudioFile);
                            if (fileMimeType == "audio/basic" || fileMimeType == "audio/mpeg" || fileMimeType == "audio/x-wav" || fileMimeType == "video/x-msvideo")
                            {
                                await _azureStorageHelper.InsertBlob(model.AudioFile.FileName, "audios", model.AudioFile);
                                var containerRef = _azureStorageHelper.GetCloudBlobContainer("audios");
                                var blob = containerRef.GetBlockBlobReference(model.AudioFile.FileName);
                                string uploadedaudioFileURL = blob.Uri.ToString();
                                if (uploadedaudioFileURL == null)
                                {
                                    ModelState.AddModelError(string.Empty, "Audio Upload failed.");
                                    return View("VerifyMyInterviewAudio", model);
                                }
                                else
                                {
                                    model.AudioFileLink = uploadedaudioFileURL;
                                }
                            }
                        }
                        var result = _meetingService.VerifyCandidateVoice(model.VoiceItUserId, model.AudioFileLink, model.PhraseDetail, model.MeetingId, "interview");
                        TempData["SuccessErrorMessage"] = result;
                        return RedirectToAction("MyInterview");
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessage"] = "Audio verification has been Failed.";
                return RedirectToAction("MyInterview");
            }
        }

        [HttpGet]
        public bool GetTimeDifference(string interviewDate, int interviewUnassignedMinutes)
        {
            //var date2 = DateTime.Now.AddMinutes(interviewUnassignedMinutes);
            //var result = DateTime.Compare(date2, Convert.ToDateTime(interviewDate));
            if ((DateTime.Now.AddMinutes(interviewUnassignedMinutes)) >= Convert.ToDateTime(interviewDate))
            {
                return true;
            }
            else
                return false;
        }

        [HttpGet]
        public async Task<int> GetInterviewUnassignedMinutes()
        {
            return await _settingsService.GetInterviewUnassignedMinutes();
        }

        [HttpPost]
        public async Task<JsonResult> Intrestreaction(string vedioid, string Status)
        {
            var Email = await _meetingService.Intrestreaction(vedioid, Status);
            if (Email != null)
            {
                Mail _mail = new Mail();
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                _mail.TO = Email.Comment;
                if (Status == "Interest Accepted")
                {
                    _mail.Subject = "Candidate has accepted your interest";
                }
                else
                {
                    _mail.Subject = "Candidate has passed your interest";
                }
                StringBuilder bulider = new StringBuilder();
                if (Status == "Interest Accepted")
                {
                    bulider.Append("Candidate has accepted your interest");
                }
                else
                {
                    bulider.Append("Candidate has passed your interest");
                }
                await _notificationHelper.CuretatedNotification(_mail, "User", bulider.ToString());
            }
            return Json("Success");
        }

        [HttpPost]
        public JsonResult ShortListreaction(string vedioid, string Status)
        {
            _meetingService.ShortListreaction(vedioid, Status);
            return Json("Success");
        }

        [HttpPost]
        public async Task<JsonResult> ShortListList(string IRFID)
        {
            var emailid = HttpContext.Session.GetString("email")?.Trim();
            emailid = emailid.Replace("\"", "");
            var model = await _meetingService.ShortListedClient(emailid, IRFID);
            foreach (var item in model)
            {
                item.ClientName = item.ClientName.Split("@")[1].Split(".")[0].ToString();
                item.DateRequired = item.interviewDate.ToString("dd/MMM/yyyy");
            }
            return Json(model);
        }

        [HttpPost]
        public async Task<JsonResult> InterestList(string IRFID)
        {
            var emailid = HttpContext.Session.GetString("email")?.Trim();
            emailid = emailid.Replace("\"", "");
            var model = await _meetingService.InterestListClient(emailid, IRFID);
            foreach (var item in model)
            {
                item.ClientName = item.ClientName.Split("@")[1].Split(".")[0].ToString();
                item.DateRequired = item.interviewDate.ToString("dd/MMM/yyyy");
            }
            return Json(model);
        }

        [HttpGet]
        public async Task<IActionResult> DeleteInterview(int id, string controllerName, string irfId)
        {
            try
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin")
                {
                    if (await _meetingService.DeleteInterview(id))
                    {
                        TempData["SuccessErrorMessageUser"] = "Interview has been deleted Successfully!";
                        if (controllerName == "IRF")
                            return RedirectToAction("MultipleCandidateInvites", controllerName, new { irfId });
                        else
                            return RedirectToAction("MultipleCandidateInvites", controllerName);
                    }
                    else
                    {
                        TempData["SuccessErrorMessageUser"] = "Failed to delete Interview!";
                        if (controllerName == "IRF")
                            return RedirectToAction("MultipleCandidateInvites", controllerName, new { irfId });
                        else
                            return RedirectToAction("MultipleCandidateInvites", controllerName);
                    }
                }
                else
                {
                    if (controllerName == "IRF")
                        return RedirectToAction("MultipleCandidateInvites", controllerName, new { irfId });
                    else
                        return RedirectToAction("MultipleCandidateInvites", controllerName);
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessageUser"] = "Failed to delete Interview";
                if (controllerName == "IRF")
                    return RedirectToAction("MultipleCandidateInvites", controllerName, new { irfId });
                else
                    return RedirectToAction("MultipleCandidateInvites", controllerName);
            }
        }

        [HttpPost]
        public async Task<string> UploadAudio()
        {
            var file = Request.Form.Files;
            var nvc = Request.Form;
            int candidateType = 0;
            string irfid = "";
            bool isClientFeedback = false;
            string currentUserEmail = string.Empty;
            if (!string.IsNullOrEmpty(nvc["candidateType"]))
            {
                candidateType = Convert.ToInt32(nvc["candidateType"]);
            }
            if (!string.IsNullOrEmpty(nvc["isClientFeedback"]))
            {
                isClientFeedback = Convert.ToBoolean(nvc["isClientFeedback"]);
            }
            if (!string.IsNullOrEmpty(nvc["irfid"]))
            {
                irfid = nvc["irfid"].ToString();
            }
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                currentUserEmail = HttpContext.Session.GetString("email")?.Trim();
                currentUserEmail = currentUserEmail.Replace("\"", "");
            }
            var fileName = ContentDispositionHeaderValue.Parse(file[0].ContentDisposition).FileName.Trim('"');
            var interviewId = fileName.Split("_")[2];
            string uploadedfbaudioFileURL = string.Empty;
            if (file != null)
            {
                var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(file[0]);
                if (fileMimeType == "audio/basic" || fileMimeType == "audio/mpeg" || fileMimeType == "audio/x-wav" || fileMimeType == "video/x-msvideo")
                {
                    await _azureStorageHelper.InsertBlob(fileName, "audios", file[0]);
                    var containerRef = _azureStorageHelper.GetCloudBlobContainer("audios");
                    var blob = containerRef.GetBlockBlobReference(fileName);
                    uploadedfbaudioFileURL = blob.Uri.ToString();
                }
                else
                {
                    return null;
                }

            }
            _meetingService.SaveFeedbackAudio(Convert.ToInt32(interviewId), irfid, uploadedfbaudioFileURL, candidateType, isClientFeedback, currentUserEmail);
            return uploadedfbaudioFileURL;
        }

        [HttpPost]
        public async Task<JsonResult> Broadcast()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var subject = Request.Form["subject"].ToString();
                    var message = Request.Form["message"].ToString();
                    var receiver = Request.Form["receiver"].ToString();
                    var irfid = Convert.ToInt32(Request.Form["irfid"].ToString());
                    if (profileName == "Admin" || profileName == "SupportAdmin")
                    {
                        var model = new BroadcastModel
                        {
                            BroadcastReciever = receiver,
                            Message = message,
                            Subject = subject
                        };
                        if (receiver != "IRF")
                        {
                            var result = await _meetingService.Broadcast(model);
                            TempData["SuccessErrorMessageInterview"] = result;
                            return Json(result);
                        }
                        else
                        {
                            model.IRFId = irfid;
                            var result = await _meetingService.BroadcastIRFmembers(model);
                            TempData["SuccessErrorMessageInterview"] = result;
                            return Json(result);
                        }
                    }
                    else
                    {
                        return Json("SignOut");
                    }
                }
                else
                {
                    return Json("SignOut");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessageInterview"] = "Broadcast has been Failed.";
                return Json("Index");
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> CandidateVideoPage(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    ViewBag.actionstatus = "Risetube";
                    ViewBag.ResultStatus = "VideoPresent";
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Candidate")
                    {
                        var emailId = HttpContext.Session.GetString("email")?.Trim();
                        emailId = emailId.Replace("\"", "");
                        if (TempData["SuccessErrorMessageInterview"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageInterview"].ToString();
                            TempData.Remove("SuccessErrorMessageInterview");
                        }
                        //Start:Save View counts of vedio
                        if (HttpContext.Session.GetString("UserLogedIn") != null)
                        {
                            string values = string.Empty;
                            var valuestatus = HttpContext.Session.GetString("UserLogedIn")?.Trim();
                            if (valuestatus == "Yes")
                            {
                                var emailaddress = HttpContext.Session.GetString("email")?.Trim();
                                emailaddress = emailaddress.Replace("\"", "");
                                await _meetingService.AddTotalViewCount(id, emailaddress, "Public");
                                //save into database to increase view count save into main table from where data is picked
                                HttpContext.Session.SetString("UserLogedIn", "No");
                            }
                            else
                            {
                                if (HttpContext.Session.GetString("UserLogedInVedioId") != null)
                                {
                                    values = HttpContext.Session.GetString("UserLogedInVedioId")?.Trim();
                                    var _lst = values.Split(";");
                                    var flag = true;
                                    for (int i = 0; i < _lst.Count(); i++)
                                    {
                                        if (_lst[i] == id.ToString())
                                        {
                                            flag = false;
                                        }
                                    }
                                    if (flag == true)
                                    {
                                        var emailaddress = HttpContext.Session.GetString("email")?.Trim();
                                        emailaddress = emailaddress.Replace("\"", "");
                                        await _meetingService.AddTotalViewCount(id, emailaddress, "Public");
                                        //save into database to increase view count save into main table from where data is picked
                                    }
                                }
                            }
                            values = values + ";" + id;
                            HttpContext.Session.SetString("UserLogedInVedioId", values);
                        }
                        //End:Save View counts of vedio

                        MeetingVM model = await _meetingService.GetMeetingbyid(id, emailId);

                        if (model == null)
                        {
                            model = new MeetingVM();
                            ViewBag.ResultStatus = "VideoNotPresent";
                            return View(model);

                        }
                        var IRListModel = await _meetingService.GetInteviewReport(model.imid, model.irfIntId);

                        MeetingVM NewModel = new MeetingVM();
                        NewModel.candidateName = model.candidateName;
                        ViewBag.profileName = profileName;
                        NewModel.CandidateId = model.CandidateId;
                        NewModel.interviewDate = model.interviewDate;
                        NewModel.imid = model.imid;
                        NewModel.IRFId = model.IRFId;
                        NewModel.ClientName = model.ClientName;
                        NewModel.InterviewerComment = model.InterviewerComment;
                        NewModel.InterviewStatus = model.InterviewStatus;
                        NewModel.JDTiltle = model.JDTiltle;
                        NewModel.JDLink = model.JDLink;
                        NewModel.MeetingStatus = model.MeetingStatus;
                        NewModel.confirmationStatus = model.confirmationStatus;
                        NewModel.VideoLink = model.VideoLink;
                        NewModel.candidateEmail = model.candidateEmail;
                        NewModel.reportLink = model.reportLink;
                        NewModel.resumeLink = model.resumeLink;
                        NewModel.zoomLink = model.zoomLink;
                        NewModel.IreportModel = IRListModel ?? new InterviewReport();
                        NewModel.IsLike = model.IsLike;
                        NewModel.TotalView = model.TotalView;
                        NewModel.IsShared = model.IsShared;
                        NewModel.TotalVedioLike = model.TotalVedioLike;
                        NewModel.TotalVedioDisLike = model.TotalVedioDisLike;
                        NewModel.ShortListCount = model.ShortListCount;
                        NewModel.IsShortlisted = model.IsShortlisted;
                        NewModel.ThumbnailImage = model.ThumbnailImage;
                        NewModel.IsOnline = model.IsOnline;
                        NewModel.candidateEmail = model.candidateEmail;
                        NewModel.MeetingPatch = model.MeetingPatch;
                        if (model.MeetingPatch.Count() > 0)
                        {
                            NewModel.VideoLink = model.MeetingPatch.FirstOrDefault().VideoLink;
                        }
                        if (NewModel.IreportModel.QuestionsCollection.Where(x => x.VideoLink != null && x.VideoLink != "").Count() > 0)
                        {
                            NewModel.IreportModel.QuestionsCollection = NewModel.IreportModel.QuestionsCollection.Where(x => x.VideoLink != null && x.VideoLink != "").ToList();
                        }
                        // to get videocount details
                        var videodetailscount = await _meetingService.GetTotalViewCount(id);
                        NewModel.VideoCountDetail = videodetailscount.ToList();
                        NewModel.FeedbackAudioLink = model.FeedbackAudioLink;
                        if (NewModel.IreportModel.QuestionsCollection == null)
                        {
                            ViewBag.QuestionsDiscussedCount = 0;
                        }
                        else
                        {
                            ViewBag.QuestionsDiscussedCount = NewModel.IreportModel.QuestionsCollection?.Count();
                        }
                        var comments = await _meetingService.GetvedioComments(id);
                        NewModel.Comments = comments;
                        bool isQC = false;
                        NewModel.IreportModel.IsQC = isQC;
                        if (IRListModel != null)
                        {
                            await GetStatusList(IRListModel, model.irfIntId);
                        }
                        ViewBag.VideoStatus = "Private";
                        return View(NewModel);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    //Start: Share button functionaly
                    ViewBag.ResultStatus = "VideoPresent";
                    MeetingVM model = await _meetingService.GetMeetingbyid(id, "");
                    if (model != null)
                    {
                        var IRListModel = await _meetingService.GetInteviewReport(model.imid, model.irfIntId);
                        if (IRListModel == null)
                        {
                            IRListModel = new InterviewReport();
                        }

                        MeetingVM NewModel = new MeetingVM
                        {
                            SubVideo_List = new List<MeetingVM>()
                        };
                        NewModel.candidateName = model.candidateName;
                        ViewBag.profileName = "";
                        NewModel.CandidateId = model.CandidateId;
                        NewModel.interviewDate = model.interviewDate;
                        NewModel.imid = model.imid;
                        NewModel.IRFId = model.IRFId;
                        NewModel.ClientName = model.ClientName;

                        NewModel.InterviewerComment = model.InterviewerComment;
                        NewModel.InterviewStatus = model.InterviewStatus;
                        NewModel.JDTiltle = model.JDTiltle;
                        NewModel.JDLink = model.JDLink;
                        NewModel.MeetingStatus = model.MeetingStatus;
                        NewModel.confirmationStatus = model.confirmationStatus;
                        NewModel.VideoLink = model.VideoLink;
                        NewModel.candidateEmail = model.candidateEmail;
                        NewModel.reportLink = model.reportLink;
                        NewModel.resumeLink = model.resumeLink;
                        NewModel.zoomLink = model.zoomLink;
                        NewModel.IreportModel = IRListModel ?? new InterviewReport();
                        NewModel.IsLike = model.IsLike;
                        NewModel.TotalView = model.TotalView;
                        NewModel.IsShared = model.IsShared;
                        NewModel.TotalVedioLike = model.TotalVedioLike;
                        NewModel.TotalVedioDisLike = model.TotalVedioDisLike;
                        NewModel.ShortListCount = model.ShortListCount;
                        NewModel.IsShortlisted = model.IsShortlisted;
                        NewModel.ThumbnailImage = model.ThumbnailImage;
                        NewModel.IsIntrested = model.IsIntrested;
                        NewModel.IsOnline = model.IsOnline;
                        NewModel.candidateEmail = model.candidateEmail;
                        NewModel.FeedbackAudioLink = model.FeedbackAudioLink;
                        var videodetailscount = await _meetingService.GetTotalViewCount(id);
                        NewModel.VideoCountDetail = videodetailscount.ToList();
                        if (NewModel.IreportModel.QuestionsCollection == null)
                        {
                            ViewBag.QuestionsDiscussedCount = 0;
                        }
                        else
                        {
                            ViewBag.QuestionsDiscussedCount = NewModel.IreportModel.QuestionsCollection?.Count();
                        }
                        bool isQC = false;
                        NewModel.IreportModel.IsQC = isQC;
                        ViewBag.VideoStatus = "Public";
                        var menuData = await _userService.GetMenuBasedOnUserEmailld("ExternalUser");
                        var menus = menuData.GroupBy(p => p.functionName).Select(g => g.First()).ToList();
                        if (menus != null)
                        {
                            menus = menus.Where(x => x.functionName != "Support").ToList();
                            HttpContext.Session.SetString("userMenus", JsonConvert.SerializeObject(menus));
                        }
                        if (IRListModel != null)
                        {
                            await GetStatusList(IRListModel, model.irfIntId);
                        }
                        //desible Like and dilisk button and share button and comment opetion
                        return View(NewModel);
                        //END: Share button functionaly
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }

        }

        [Authorize]
        [HttpPost]
        public async Task<JsonResult> VideoShare(string value, string videoid, string name, string Emailids)
        {
            var email = HttpContext.Session.GetString("email");
            email = email.Replace("\"", "");
            var modelList = _meetingService.AddVedioShare(email, videoid, Emailids);
            //send a email
            string currenturl = Request.Host.ToString() + "/Interview/CandidateVideoPage?id=" + videoid;
            Mail _mail = new Mail
            {
                TO = Emailids,
                Subject = $"Risebird: {email} Interview video is shared with you"
            };
            var result = await _notificationHelper.SharedVideoNofiticationExternal(_mail, name, currenturl);
            if (result.Contains("Mail has been sent Successfully!"))
            {
                return Json("Success");
            }
            else
            {
                return Json("Fail");
            }
        }

        [HttpPost]
        public async Task<JsonResult> VideoShareEmail(string videoid, string Emailid, string CurrentUrl, string Candidateemail)
        {
            //Start:Save View counts of vedio
            Emailid = Emailid.Replace("\"", "");
            //Check Video is shared with current user or not
            var ispresent = await _meetingService.CheckVedioShare(videoid, Emailid);

            if (ispresent > 0)
            {
                await _meetingService.AddTotalViewCount(Convert.ToInt32(videoid), Emailid, "Public");
                return Json("Success");
            }
            else
            {
                //send email to candidate
                Mail _mail = new Mail
                {
                    TO = Candidateemail,
                    Subject = $"Risebird: {Emailid} is requesting access of your Interview Video"
                };
                var result = await _notificationHelper.VideoAccessRequest(_mail, CurrentUrl, Emailid);
                return Json("Fail");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<JsonResult> VideoDetailsJsonView(int interviewid)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Candidate")
                    {
                        var email = HttpContext.Session.GetString("email");
                        email = email.Replace("\"", "");
                        var model = _meetingService.GetVideoCandidateSumaryDetails(interviewid, email);
                        return Json(model);
                    }
                    else
                    {
                        return Json("No Data");
                    }
                }
                else
                {
                    return Json("No Data");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("No Data");
            }
        }

        //public JsonResult ChangeCandidateStatus(int videoid, string Status)
        //{
        //    try
        //    {
        //        if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
        //        {
        //            var profileName = HttpContext.Session.GetString("profileName")?.Trim();
        //            profileName = profileName.Replace("\"", "");
        //            if (profileName == "Candidate")
        //            {
        //                //To update candidate status
        //                var model = _meetingService.UpdateCandidateStatus(videoid, Status);
        //            }
        //            return Json("Success");
        //        }
        //        else
        //        {
        //            return Json("Fail");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //        return Json("Fail");
        //    }
        //}

        [HttpGet]
        public async Task<IActionResult> AnalyseSentiment(int imid)
        {
            try
            {
                SentimentDTO obj = new SentimentDTO();
                obj.imid = imid;
                if (TempData["SuccessErrorMessageSentiment"] != null)
                {
                    ViewBag.Message = TempData["SuccessErrorMessageSentiment"].ToString();
                    TempData.Remove("SuccessErrorMessageSentiment");
                }
                return View(obj);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("Failed");
            }
        }

        [HttpPost]
        public async Task<IActionResult> AnalyseSentiment(IFormCollection formCollection, int sentimentImid)
        {
            try
            {
                var file = formCollection.Files[0];
                if (file != null)
                {
                    var model = new SentimentDTO();
                    model.imid = sentimentImid;
                    //var path = $"http://localhost:7071/api/SentimentAnalysis";
                    var path = $"https://risebirdcognitiveservice.azurewebsites.net/api/SentimentAnalysis";
                    var functionKey = "jiNbkIadJuGB/ms6U4KIWBmFdCTh6DLLku8MrEaaNCLjquJAkZSHbw==";

                    model.VideoFileName = model.imid + "_" + file.FileName;
                    var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(file);
                    if (fileMimeType == "video/mpeg" || fileMimeType == "video/mp4"
                        || fileMimeType == "video/quicktime")
                    {
                        await _azureStorageHelper.InsertBlob(model.VideoFileName, "sentimentvideos", file);
                    }
                    else
                    {
                        TempData["SuccessErrorMessageInterview"] = "Failed!";

                    }
                    HttpClient httpClient = new HttpClient();
                    httpClient.Timeout = TimeSpan.FromMilliseconds(Timeout.Infinite);
                    await httpClient.SendRequestAsync(HttpMethod.Post, path, model, functionKey, "", _cancellationToken);
                    TempData["SuccessErrorMessageInterview"] = "Sentiment analysis has been started Successfully!";
                }
                else
                {
                    TempData["SuccessErrorMessageInterview"] = "Failed!";
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessageInterview"] = "Failed!";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> UploadInterviewVideoes(int id)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller")
                {
                    //fetch question details
                    var model1 = await _meetingService.FetchQuestionDetails(id);
                    Question model = new Question();
                    model.questionset = model1;
                    return View(model);
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }

        [Authorize]
        [HttpPost]
        [DisableRequestSizeLimit]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadInterviewVideoes(IFormCollection model)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                {
                    List<QuestionDetails> _question = new List<QuestionDetails>();
                    var norows = model.Keys.Count - 1 + model.Files.Count;
                    int Interviewid = 0;
                    for (int i = 0; i < norows / 9; i++)
                    {
                        _question.Add(new QuestionDetails { Question = model["questionset[" + i + "].Question"][0], VideoLink = model["questionset[" + i + "].VideoLink"][0], VideoTitle = model["questionset[" + i + "].VideoTitle"][0], Priority = Convert.ToInt32(model["questionset[" + i + "].Priority"][0]), Id = Convert.ToInt32(model["questionset[" + i + "].Id"][0]), InterviewId = Convert.ToInt32(model["questionset[" + i + "].InterviewId"][0]) });
                        Interviewid = Convert.ToInt32(model["questionset[" + i + "].InterviewId"][0]);
                    }

                    IFormFileCollection files1 = model.Files;
                    foreach (IFormFile item in files1)
                    {
                        string uploadedvideoFileURL = null;
                        var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(item);
                        if (fileMimeType == "video/mpeg" || fileMimeType == "video/mp4"
                            || fileMimeType == "video/quicktime")
                        {
                            await _azureStorageHelper.InsertBlob(item.FileName, "videos", item);
                            var containerRef = _azureStorageHelper.GetCloudBlobContainer("videos");
                            var blob = containerRef.GetBlockBlobReference(item.FileName);
                            uploadedvideoFileURL = blob.Uri.ToString();
                        }

                        var rowvalue = Convert.ToInt32(item.Name.Split("[")[1].Split("]")[0]);
                        if (uploadedvideoFileURL == null)
                        {
                            if (item.Name.Contains("Thumbnail"))
                            {
                                _question[rowvalue].ThumbnailLink = "";
                            }
                            else
                            {
                                _question[rowvalue].VideoLink = "";
                            }
                        }
                        else
                        {
                            if (item.Name.Contains("Thumbnail"))
                            {
                                _question[rowvalue].ThumbnailLink = uploadedvideoFileURL;
                            }
                            else
                            {
                                _question[rowvalue].VideoLink = uploadedvideoFileURL;
                            }
                        }
                    }
                    var model1 = _meetingService.UpdateQuestionDetails(_question);
                    return RedirectToAction("UploadInterviewVideoes", new { id = Interviewid });
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }

        [HttpPost]
        public JsonResult FetchCandidateFeedback(int interviewid)
        {
            AzureStorageHelper _azureTableStorageService = new AzureStorageHelper(_configuration);
            var result = _azureTableStorageService.GetAllEntities<SurveyVM>("RisebirdSurvey");
            var resultset = result.Where(x => x.interviewid == interviewid).FirstOrDefault();
            return Json(resultset);
        }

        [HttpPost]
        public async Task<JsonResult> ResendFeedbackform(int interviewid, string CandidateEmail)
        {
            string currenturl = "https://" + Request.Host.ToString() + "/Survey/Index?id=" + interviewid;
            Mail _mail = new Mail
            {
                TO = CandidateEmail,
                Subject = "Risebird: Feedback form"
            };
            var emailsend = await _notificationHelper.SurveyNotification(_mail, currenturl);
            WhatsAppVM _whatsAppVM = new WhatsAppVM();
            _whatsAppVM.Emailid = CandidateEmail;
            _whatsAppVM.buttonlink = !string.IsNullOrEmpty(currenturl) ? currenturl.Replace("https://" + Request.Host.ToString() + "/", "") : "";
            _whatsAppVM.Event = "Feedback Remainder";
            await _whatsAppHelper.WhatsAppSendFeedbackRemainder(_whatsAppVM);
            if (emailsend == "Mail has been sent Successfully!")
            {
                return Json("Feedback Form has been sent Successfully!");
            }
            else
            {
                return Json("Failed to send feedback form, please try again.");
            }
        }

        [HttpPost]
        public async Task<IActionResult> MyInterviewJson(string searchValue, string meetingLineUpType = null)
        {
            try
            {
                int Page = 1;
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Interviewer")
                    {
                        if (string.IsNullOrEmpty(meetingLineUpType))
                        {
                            meetingLineUpType = MeetingLineUpType.Pending;
                        }
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        //var myInterview = await _meetingService.GetAllAssignedInterviews(emailid);
                        var myInterview = await _meetingService.GetInterviewerAssignedInterviews(emailid, meetingLineUpType);
                        myInterview = myInterview.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); x.JobLevelDesc = ((JobLevels)Convert.ToInt32(x.JobLevel)).GetDescription(); return x; }).ToList();
                        foreach (var item in myInterview)
                        {
                            if (!string.IsNullOrEmpty(item.resumeLink))
                            {
                                try
                                {
                                    item.resumeLink = new Uri(item.resumeLink, UriKind.Absolute).ToString();
                                }
                                catch (Exception)
                                {
                                    item.resumeLink = "";
                                }
                            }
                            else
                            {
                                item.resumeLink = "";
                            }

                            if (item.InterviewStatus.HasValue)
                            {
                                item.InterviewStatusDesc = ((InterviewStatus)(item.InterviewStatus)).GetDescription();
                            }
                            if (item.MeetingStatus.HasValue)
                            {
                                item.MeetingStatusDesc = ((MeetingStatus)(item.MeetingStatus)).GetDescription();
                            }
                        }
                        if (HttpContext.Session.GetString("MyInterviewfilters") != null)
                        {
                            var filter = JsonConvert.DeserializeObject<MyInterviewFilter>(HttpContext.Session.GetString("MyInterviewfilters"));

                            if (filter.StartDate != null && filter.EndDate != null)
                            {
                                myInterview = myInterview.Where(t => t.interviewDate.Date >= filter.StartDate && t.interviewDate.Date <= filter.EndDate).ToList();
                            }
                            else if (filter.StartDate != null && filter.EndDate == null)
                            {
                                myInterview = myInterview.Where(t => t.interviewDate.Date >= filter.StartDate /*&& t.interviewDate.Date <= filter.EndDate*/).ToList();
                            }
                            else if (filter.StartDate == null && filter.EndDate != null)
                            {
                                myInterview = myInterview.Where(t => /*t.interviewDate.Date >= filter.StartDate &&*/ t.interviewDate.Date <= filter.EndDate).ToList();
                            }
                            //filter.InterviewDuration = filter.InterviewDuration == null ? filter.InterviewDuration = myInterview.Select(x => x.interviewDuration).ToArray() : filter.InterviewDuration = filter.InterviewDuration;
                            filter.InterviewSlot = filter.InterviewSlot == null ? filter.InterviewSlot = myInterview.Select(x => x.interviewSlot).ToArray() : filter.InterviewSlot = filter.InterviewSlot;
                            filter.JobLevel = filter.JobLevel == null ? filter.JobLevel = myInterview.Select(x => x.JobLevelDesc).ToArray() : filter.JobLevel = filter.JobLevel;
                            filter.InterviewDate = filter.InterviewDate == null ? filter.InterviewDate = myInterview.Select(x => Convert.ToString(x.interviewDate)).ToArray() : filter.InterviewDate = filter.InterviewDate;
                            filter.CandidateName = filter.CandidateName == null ? filter.CandidateName = myInterview.Select(x => x.candidateName).ToArray() : filter.CandidateName = filter.CandidateName;
                            filter.RisebirdHR = filter.RisebirdHR == null ? filter.RisebirdHR = myInterview.Select(x => x.risebirdHR).ToArray() : filter.RisebirdHR = filter.RisebirdHR;
                            //filter.InterviewDuration.Contains(c.interviewDuration)
                            myInterview = myInterview.Where(c => (filter.InterviewSlot.Count() > 0 ? filter.InterviewSlot.Contains(c.interviewSlot) : c.interviewSlot == null)
                            && (filter.JobLevel.Count() > 0 ? filter.JobLevel.Contains(c.JobLevelDesc) : c.JobLevelDesc == null)
                           && (filter.InterviewDate.Count() > 0 ? filter.InterviewDate.Contains(Convert.ToString(c.interviewDate)) : c.interviewDate == null)
                           && (filter.CandidateName.Count() > 0 ? filter.CandidateName.Contains(c.candidateName) : c.candidateName == null)
                           && (filter.RisebirdHR.Count() > 0 ? filter.RisebirdHR.Contains(c.risebirdHR) : c.risebirdHR == null)).ToList();
                        }
                        if (HttpContext.Session.GetString("MyInterviewfilters") == null)
                        {
                            myInterview = myInterview.Where(x => x.interviewDate.Date >= DateTime.Now.Date || (x.MeetingStatus == null && x.interviewDate.Date < DateTime.Now.Date)).ToList();
                        }
                        if (!string.IsNullOrEmpty(searchValue))
                        {
                            searchValue = searchValue.ToUpper();
                            myInterview = myInterview.Where(x => (!string.IsNullOrEmpty(x.candidateName) ? x.candidateName.ToUpper().Contains(searchValue) : x.candidateName == "") || (!string.IsNullOrEmpty(x.MeetingStatusDesc) ? x.MeetingStatusDesc.ToUpper().Contains(searchValue) : x.MeetingStatusDesc == "")
                            || (!string.IsNullOrEmpty(x.InterviewStatusDesc) ? x.InterviewStatusDesc.ToUpper().Contains(searchValue) : x.InterviewStatusDesc == "")).ToList();
                        }
                        int totalcount = myInterview.Count;
                        int Pagestart = 0;
                        int Pageend = 0;

                        Pagestart = 10 * ((Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page)) - 1) + 1;
                        int a = myInterview.Count / 10;
                        if (myInterview.Count % 10 == 0)
                        {
                            Pageend = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
                        }
                        else
                        {
                            a = a + 1;
                            if (a == Page)
                            {
                                Pageend = myInterview.Count;
                            }
                            else
                            {
                                Pageend = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
                            }
                        }
                        if (myInterview.Count == 0)
                        {
                            Pagestart = 0;
                            Pageend = 0;
                        }

                        foreach (var item in myInterview)
                        {
                            var time = item.interviewDate.AddHours(2);
                            var currentTime = DateTime.UtcNow;
                            TimeZoneInfo INDIAN_ZONE = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                            DateTime indianTime = TimeZoneInfo.ConvertTimeFromUtc(currentTime, INDIAN_ZONE);
                            var timediff = time - indianTime;
                            if (timediff.TotalMinutes >= 0 && (item.MeetingStatus == 2 || item.MeetingStatus == null))
                            {
                                item.Timer = Convert.ToInt32(timediff.TotalMinutes);
                            }
                        }
                        ViewBag.startPage = Pagestart;
                        ViewBag.endPage = Pageend;
                        ViewBag.totalPage = a;
                        ViewBag.totalentry = myInterview.Count;
                        ViewBag.MeetingLineUpType = meetingLineUpType;
                        return PartialView("MyInterviewPartial", myInterview?.Take(10)?.ToList());
                        //return Json(new { result = myInterview.Take(10), startPage = Pagestart, endPage = Pageend, totalPage = a, totalentry = myInterview.Count });
                    }
                    else
                    {
                        return Json("failed");
                    }
                }
                else
                {
                    return Json("failed");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("failed");
            }
        }

        [HttpGet]
        public IActionResult UploadInterviewAudio(int interviewId, string candidateEmail, string irfName, DateTime interviewDate, string candidateName)
        {
            try
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                {
                    var model = new SpeechToTextDTO();
                    model.InterviewId = interviewId;
                    model.InterviewDate = interviewDate;
                    model.CandidateEmailId = candidateEmail;
                    model.IRFName = irfName;
                    model.CandidateName = candidateName;

                    var tableEntries = _azureStorageHelper.GetAllEntities<SpeechToTextServiceModel>("SpeechToTextTbl");
                    var requiredEntry = new SpeechToTextServiceModel();
                    if (tableEntries != null && tableEntries.Count() != 0)
                    {
                        requiredEntry = tableEntries.Where(x => x.PartitionKey == interviewId.ToString()).FirstOrDefault();
                    }
                    if (requiredEntry != null)
                    {
                        model.UploadedAudioFileUrl = requiredEntry.AudioFileUrl;
                        model.ConvertedText = requiredEntry.ConvertedText;
                    }
                    return View(model);
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        [HttpPost]
        [DisableRequestSizeLimit]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadInterviewAudio(SpeechToTextDTO model)
        {

            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        if (model.AudioFile != null)
                        {
                            var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(model.AudioFile);
                            if (fileMimeType == "audio/basic" || fileMimeType == "audio/mpeg" || fileMimeType == "audio/x-wav" || fileMimeType == "video/x-msvideo")
                            {
                                //string fileName = model.InterviewId.ToString() + "_InterviewAudio.wav";
                                await _azureStorageHelper.InsertBlob(model.AudioFile.FileName, "audios", model.AudioFile);
                                var containerRef = _azureStorageHelper.GetCloudBlobContainer("audios");
                                var blob = containerRef.GetBlockBlobReference(model.AudioFile.FileName);
                                string uploadedaudioFileURL = blob.Uri.ToString();
                                if (uploadedaudioFileURL == null)
                                {
                                    ModelState.AddModelError(string.Empty, "Audio Upload failed.");
                                    return View("UploadInterviewAudio", model);
                                }
                                else
                                {
                                    model.AudioFileUrl = uploadedaudioFileURL;
                                }
                            }

                            //var path = $"http://localhost:7071/api/SpeechToTextService";
                            var path = $"https://risebirdmicroservicededicated.azurewebsites.net/api/SpeechToTextService";
                            var functionKey = "VFkRiakLwh93q4fzZ9xx69AvKnTSbNVKQSzIaHswCMO/CgCiiOsNNQ==";

                            HttpClient httpClient = new HttpClient();
                            await httpClient.SendRequestAsync(HttpMethod.Post, path, model, functionKey, "", _cancellationToken);
                            TempData["SuccessErrorMessageInterview"] = "Audio has been uploaded Successfully!";
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            var modelData = new SpeechToTextServiceModel();
                            modelData.PartitionKey = model.InterviewId.ToString();
                            modelData.RowKey = model.InterviewId.ToString();
                            modelData.AudioFileUrl = model.UploadedAudioFileUrl;
                            modelData.ConvertedText = model.ConvertedText;
                            modelData.InterviewId = model.InterviewId;
                            modelData.KeyWords = model.KeyWords;
                            await _azureStorageHelper.InsertEntity("SpeechToTextTbl", modelData);
                            TempData["SuccessErrorMessageInterview"] = "Keywords updated successfully!";
                            return RedirectToAction("Index");
                        }
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessageInterview"] = "Failed to upload Audio.";
                return RedirectToAction("Index");
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> ReassignMeetingByEI(string id)
        {
            var meetings = await _meetingService.FetchOpenMeetingById(id);
            var menuData = await _userService.GetMenuBasedOnUserEmailld("ExternalUser");
            var menus = menuData.GroupBy(p => p.functionName).Select(g => g.First()).ToList();
            if (menus != null)
            {
                menus = menus.Where(x => x.functionName != "Support" && x.functionName != "Common Pool").ToList();
                HttpContext.Session.SetString("userMenus", JsonConvert.SerializeObject(menus));
            }
            return View(meetings);
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<JsonResult> ReassignMeeting(string id, string meetingid, string Emailid)
        {
            var meetings = _meetingService.ReassignMeeting(id, meetingid, Emailid);
            var codedname = (await _meetingService.FetchInterviewerCodedName(Emailid)).NickName;
            return Json(new { meetings = meetings, codedname = codedname });
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> InstaAddReport(int interviewId, int irfId, bool isClient = false, bool isClientSelected = false, bool isQC = false, string actiontype = null)
        {
            try
            {
                bool isInterviewer = false;
                ViewBag.actiontype = actiontype;
                ViewBag.IRFID = irfId;
                var model = await _meetingService.GetInteviewReport(interviewId, irfId);
                if (model.QuestionsCollection == null)
                {
                    ViewBag.QuestionsDiscussedCount = 0;
                }
                else
                {
                    ViewBag.QuestionsDiscussedCount = model.QuestionsCollection?.Count();
                }
                ViewBag.IsClient = isClient;
                ViewBag.IsClientSelected = isClientSelected;
                model.IsQC = isQC;
                model.IsInterviewer = isInterviewer;
                model.PreviousMeetingStatus = model.MeetingStatus;
                model.ResourceUrl = Request.Scheme + "://" + Request.Host.Value + Request.Path + Request.QueryString;
                await GetStatusList(model, irfId, 0);
                var menuData = await _userService.GetMenuBasedOnUserEmailld("ExternalUser");
                var menus = menuData.GroupBy(p => p.functionName).Select(g => g.First()).ToList();
                if (menus != null)
                {
                    menus = menus.Where(x => x.functionName != "Support" && x.functionName != "Common Pool" && x.functionName != "Risetube").ToList();
                    HttpContext.Session.SetString("userMenus", JsonConvert.SerializeObject(menus));
                }
                ViewBag.PageStatus = "PublicExternal";
                ViewBag.PageReport = "Insta";
                ViewBag.ClientDetails = "";
                if (isClient || isClientSelected)
                {
                    ViewBag.ClientDetails = "client";
                    if (model.IsProctor)
                    {
                        return PartialView("ClientProctorReport", model);
                    }
                    else
                    {
                        return PartialView("ClientPartialReport", model);
                    }
                }
                else
                {
                    if (model.IsProctor)
                    {
                        return View("ProctorReport", model);
                    }
                    else
                    {
                        return View("AddReport", model);
                    }
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("MyInterview");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExecutiveReport(int interviewId, int irfId, bool isClient = false, bool isClientSelected = false)
        {
            try
            {
                bool isInterviewer = false;
                ViewBag.IRFID = irfId;
                var model = await _meetingService.GetInteviewReport(interviewId, irfId);
                if (model != null)
                {
                    if (model.QuestionsCollection == null)
                    {
                        ViewBag.QuestionsDiscussedCount = 0;
                    }
                    else
                    {
                        ViewBag.QuestionsDiscussedCount = model.QuestionsCollection?.Count();
                    }
                    ViewBag.IsClient = isClient;
                    ViewBag.IsClientSelected = isClientSelected;
                    model.IsInterviewer = isInterviewer;
                    model.PreviousMeetingStatus = model.MeetingStatus;
                    model.ResourceUrl = Request.Scheme + "://" + Request.Host.Value + Request.Path + Request.QueryString;
                    await GetStatusList(model, irfId, 0);
                    var menuData = await _userService.GetMenuBasedOnUserEmailld("ExternalUser");
                    var menus = menuData.GroupBy(p => p.functionName).Select(g => g.First()).ToList();
                    if (menus != null)
                    {
                        menus = menus.Where(x => x.functionName != "Support" && x.functionName != "Common Pool" && x.functionName != "Risetube").ToList();
                        HttpContext.Session.SetString("userMenus", JsonConvert.SerializeObject(menus));
                    }
                    ViewBag.PageStatus = "PublicExternal";
                    ViewBag.PageReport = "";
                    if (model.IsProctor)
                    {
                        return View("ProctorReport", model);
                    }
                    else
                    {
                        return View("AddReport", model);
                    }
                }
                else
                {
                    return RedirectToAction("AllMeetings");
                }

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("AllMeetings");
            }

        }

        [HttpGet]
        public async Task<IActionResult> PublicAddReport(int interviewId, int irfId, bool isClient = false, bool isClientSelected = false, bool isQC = false, string actiontype = null, string currentUser = null)
        {
            try
            {
                ViewBag.PageStatus = "PublicExternal";
                bool isAdminHR = false;
                bool isInterviewer = false;
                ViewBag.actiontype = actiontype;
                ViewBag.IRFID = irfId;
                var model = await _meetingService.GetInteviewReport(interviewId, irfId);
                if (model == null)
                {
                    if (isAdminHR)
                    {
                        return RedirectToAction("Index");
                    }
                    else if (isInterviewer)
                    {
                        return RedirectToAction("MyInterview");
                    }
                }
                if (model.QuestionsCollection == null)
                {
                    ViewBag.QuestionsDiscussedCount = 0;
                }
                else
                {
                    ViewBag.QuestionsDiscussedCount = model.QuestionsCollection?.Count();
                }
                ViewBag.IsClient = isClient;
                ViewBag.IsClientSelected = isClientSelected;
                model.IsQC = isQC;
                model.IsInterviewer = isInterviewer;
                model.PreviousMeetingStatus = model.MeetingStatus;
                await GetStatusList(model, irfId, 0);
                model.ResourceUrl = Request.Scheme + "://" + Request.Host.Value + Request.Path + Request.QueryString;
                var menuData = await _userService.GetMenuBasedOnUserEmailld("ExternalUser");
                var menus = menuData.GroupBy(p => p.functionName).Select(g => g.First()).ToList();
                if (menus != null)
                {
                    menus = menus.Where(x => x.functionName != "Support" && x.functionName != "Common Pool" && x.functionName != "Risetube").ToList();
                    HttpContext.Session.SetString("userMenus", JsonConvert.SerializeObject(menus));
                }
                ViewBag.PageReport = "External";
                ViewBag.ClientDetails = "";

                ViewBag.CurrentUser = currentUser;

                if (isClient || isClientSelected)
                {
                    ViewBag.ClientDetails = "client";
                    var client = await _userRepository.GetClientDetailsByIRFId(model.IRFName);
                    model.IsQCRequired = client.IsQCRequired;
                    model.IsRedoInterview = client.IsRedoInterview;
                    if (model.IsProctor)
                    {
                        return PartialView("ClientProctorReport", model);
                    }
                    else
                    {
                        return PartialView("ClientPartialReport", model);
                    }
                }
                else
                {
                    if (model.IsProctor)
                    {
                        return View("ProctorReport", model);
                    }
                    else
                    {
                        return View("AddReport", model);
                    }
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("MyInterview");
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> PublicOpenInterview(string emailid, string id)
        {
            try
            {
                ViewBag.emailid = emailid;
                ViewBag.id = id;
                var Emailid = string.Empty;
                if (HttpContext.Session.GetString("email") != null)
                {
                    Emailid = HttpContext.Session.GetString("email")?.Trim();
                    Emailid = Emailid.Replace("\"", "");
                }
                var meetings = await _meetingService.GetAllOpenInterviews(emailid);
                meetings = meetings.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();

                var list = new SelectList(new[]{ new { ID = "Today", Name = "Today"},
                                                         new { ID = "Tomorrow", Name = "Tomorrow"  },
                                                         new { ID = "Next Seven Days", Name = "Next Seven Days" },
                                                         new { ID = "All", Name = "All"},
                        }, "ID", "Name", "All");

                ViewData["list"] = list;
                if (TempData["SuccessErrorMessage"] != null)
                {
                    ViewBag.Message = TempData["SuccessErrorMessage"].ToString();
                    TempData.Remove("SuccessErrorMessage");
                }
                if (Emailid != emailid)
                {
                    return RedirectToAction("PublicOpenInterviewInvite", new { id = id });
                }
                return View(meetings);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("PublicOpenInterviewInvite", new { id = id });
            }
        }

        [HttpGet]
        public async Task<IActionResult> PublicOpenInterviewInvite(string id)
        {
            var model = new List<Meeting>();
            ViewBag.Id = id;
            ViewBag.PageType = "Public";
            var list = new SelectList(new[]{ new { ID = "Today", Name = "Today"},
                                                         new { ID = "Tomorrow", Name = "Tomorrow"  },
                                                         new { ID = "Next Seven Days", Name = "Next Seven Days" },
                                                         new { ID = "All", Name = "All"},
                        }, "ID", "Name", "All");

            ViewData["list"] = list;
            var menuData = await _userService.GetMenuBasedOnUserEmailld("ExternalUser");
            var menus = menuData.GroupBy(p => p.functionName).Select(g => g.First()).ToList();
            if (menus != null)
            {
                menus = menus.Where(x => x.functionName != "Support" && x.functionName != "Common Pool" && x.functionName != "Risetube").ToList();
                HttpContext.Session.SetString("userMenus", JsonConvert.SerializeObject(menus));
            }
            return View("PublicOpenInterview", model);
        }

        [HttpPost]
        public async Task<JsonResult> PublicOpenInterviewInvite(string id, string Emailid)
        {
            HttpContext.Session.SetString("email", JsonConvert.SerializeObject(Emailid));
            HttpContext.Session.SetString("profileName", JsonConvert.SerializeObject("Interviewer"));
            var role = await _meetingService.GetUniqueInterviewerByEmailId(Emailid, id);
            if (role > 0)
            {
                return Json("success");
            }
            else
            {
                return Json("Emailid don't have access.");
            }
        }

        [HttpGet]
        public async Task<IActionResult> PublicGetFilteredOpenInterviews(string filterValue, string emailid)
        {
            try
            {
                var meetings = await _meetingService.GetAllOpenInterviews(emailid);

                if (filterValue == "Today")
                {
                    meetings = meetings.Where(x => x.interviewDate.Date == DateTime.Now.Date).ToList();
                    if (meetings.Count() == 0)
                    {
                        return new EmptyResult();
                    }
                }
                else if (filterValue == "Tomorrow")
                {
                    meetings = meetings.Where(x => x.interviewDate.Date == DateTime.Now.AddDays(1).Date).ToList();
                    if (meetings.Count() == 0)
                    {
                        return new EmptyResult();
                    }
                }
                else if (filterValue == "Next Seven Days")
                {
                    meetings = meetings.Where(x => x.interviewDate.Date <= DateTime.Now.AddDays(7).Date && x.interviewDate.Date >= DateTime.Now.Date).ToList();
                    if (meetings.Count() == 0)
                    {
                        return new EmptyResult();
                    }
                }
                meetings = meetings.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();
                return PartialView("GetFilteredOpenInterviews", meetings);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> PublicAssignYourself(int id, string InterviewDate, string emailid, string uniqueid)
        {
            try
            {
                var Emailid = string.Empty;
                if (HttpContext.Session.GetString("email") != null)
                {
                    Emailid = HttpContext.Session.GetString("email")?.Trim();
                    Emailid = Emailid.Replace("\"", "");
                }
                if (Emailid != emailid)
                {
                    return RedirectToAction("PublicOpenInterviewInvite", new { id = id });
                }
                var result = await _meetingService.AssignYourself(id, emailid.ToLower(), Convert.ToDateTime(InterviewDate));
                TempData["SuccessErrorMessage"] = result;
                ViewBag.Message = result;
                return RedirectToAction("PublicOpenInterview", new { emailid = emailid, id = uniqueid });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("PublicOpenInterview", new { emailid = emailid, id = uniqueid });
            }
        }
        [HttpGet]
        public async Task<JsonResult> CandidateMeetingsStatus(int meetingid, int status, string uniqueid, int Irfid, int candidateId)
        {
            if (_meetingService.CheckInterviewCancellable(meetingid))
            {
                var response = true;
                //if (status == 4)
                //{
                //    response = _meetingService.CheckInterviewClerityTime(meetingid);
                //}
                var results = _meetingService.UpdateCandidateMeetingsStatus(meetingid, status, uniqueid, Irfid, candidateId, response);
                if (results == "Success")
                {
                    //if (status == 4)
                    //{
                    //    var clientHr = await _candidateService.FetchChannelOwnerByInterviewid(meetingid);
                    //    var irfOwnersAndRestrictedIrfOwner = await _candidateService.GetCandidateStaffingAgencyAndClientHRByInterviewId(meetingid);
                    //    var result = await _candidateService.FetchChannelOwnerandInterviewer(meetingid);
                    //    var _result = await _notificationHelper.SendInterviewClerityClientNotification(result.candidateEmail, result.interviewDate.ToString(), clientHr.EmailId, result.CandidateName, irfOwnersAndRestrictedIrfOwner?.ToList());
                    //}
                    //if (status == 2 || status == 3 || (status == 4 && response == false))
                    if(status == 1) // availability confirmed
                    {
                        //get irfId and interviewdate, candidateEmail and clientHrEmail

                        var meetingDetails = await _meetingService.GetMeetingDetails(meetingid);
                        await _notificationHelper.SendCandidateConfirmedNotificationToClient(meetingDetails.candidateEmail, meetingDetails.interviewDate.ToString(), meetingDetails.ClientHrContact, meetingDetails.IRFID);
                    }
                    if (status == 2 || status == 3)
                    {
                        var value = string.Empty;
                        if (status == 2)
                        {
                            value = "NO";
                        }
                        else if (status == 3)
                        {
                            value = "RESCHEDULE";
                        }
                        //else
                        //{
                        //    value = "NEED MORE CLARITY";
                        //}
                        //send mail scheduling link
                        var result1 = await _iRFService.AddCandidateStatus("Candidate", candidateId, Irfid, "Candidate Cancelled", (int)CandidateStatusOptions.Cancelled, meetingid);
                        var result = await _candidateService.FetchChannelOwnerandInterviewer(meetingid);
                        var irfOwnersAndRestrictedIrfOwner = await _candidateService.GetCandidateStaffingAgencyAndClientHRByInterviewId(meetingid);
                        List<string> interviewers = new List<string>();
                        if (!string.IsNullOrEmpty(result.interviewer))
                        {
                            interviewers.Add(result.interviewer);
                        }
                        if (!string.IsNullOrEmpty(result.EmailId))
                        {
                            interviewers.Add(result.EmailId);
                        }
                        var emailresult = await _notificationHelper.SendInterviewCancelledMail(interviewers, result.interviewDate.ToString(), irfOwnersAndRestrictedIrfOwner?.ToList(), Irfid, meetingid);
                        var emailresult1 = await _notificationHelper.SendInterviewCancelledMailClient(result.interviewDate.ToString(), result.CandidateName, irfOwnersAndRestrictedIrfOwner?.ToList(), Irfid, meetingid);
                        //var emailresult2 = await _notificationHelper.SendInterviewCancelledMailCandidateWithSchedulingLink(result.candidateEmail, result.interviewDate.ToString(), result.CandidateName, result.CalendlyLink, Irfid, meetingid);
                        DeleteMeetingLink(result.ZoomMeetingId);
                        if (status == 3 && candidateId == 0)
                        {
                            string stepfirst = result.CalendlyLink.Split("?").LastOrDefault();
                            var olduniqueid = stepfirst.Split("=").LastOrDefault();
                            var oneTimeSchedulingLink = _interviewSchedulerRepository.GenerateOneTimeEvent(olduniqueid, result.candidateEmail).Result;
                            oneTimeSchedulingLink = result.CalendlyLink.Split("?").FirstOrDefault() + "?id=" + oneTimeSchedulingLink;
                            return Json(oneTimeSchedulingLink);
                        }
                        return Json("Successfully status updated, " + value);
                    }
                    return Json("Thank you for confirming your interview.");
                }
                else if (results == "Already Present")
                {
                    return Json("Your interview is already cancelled. Kindly reschedule your interview or contact to HR/Admin.");
                }
                else
                {
                    return Json("Status update failed. Please try again!");
                }
            }
            else
            {
                return Json("Fail to Update Status, Please try again");
            }
        }

        #region Proctor Report
        [Authorize]
        [HttpPost]
        [DisableRequestSizeLimit]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProctorReport(InterviewReport model)
        {
            try
            {
                ViewBag.PageStatus = "Mainurl";
                if (HttpContext.Session.GetString("email") != null &&
                    HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    model.UserDetails = emailid;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Interviewer" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {

                        if (model.AudioFile != null)
                        {
                            var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(model.AudioFile);
                            if (fileMimeType == "audio/basic" || fileMimeType == "audio/mpeg" || fileMimeType == "audio/x-wav" || fileMimeType == "video/x-msvideo")
                            {
                                await _azureStorageHelper.InsertBlob(model.AudioFile.FileName, "audios", model.AudioFile);
                                var containerRef = _azureStorageHelper.GetCloudBlobContainer("audios");
                                var blob = containerRef.GetBlockBlobReference(model.AudioFile.FileName);
                                string uploadedaudioFileURL = blob.Uri.ToString();
                                if (uploadedaudioFileURL == null)
                                {
                                    ModelState.AddModelError(string.Empty, "Audio Upload is failed.");
                                    await GetStatusList(model);
                                    return View("ProctorReport", model);
                                }
                                else
                                {
                                    model.ThumbnailImage = uploadedaudioFileURL;
                                }
                            }
                        }
                        string uploadedvideoFileURL = string.Empty;
                        if (model.VideoFile != null)
                        {
                            var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(model.VideoFile);
                            if (fileMimeType == "video/mpeg" || fileMimeType == "video/mp4" || fileMimeType == "video/quicktime")
                            {
                                AzureCall _call = new AzureCall(_configuration);
                                //var resulttype = _call.CallApi(model.VideoFile, model.VideoFile.FileName, model.VideoFile.Length);
                                var resulttype = _call.CallApiUpload(model.VideoFile);
                                resulttype = resulttype.Replace("\"", "");
                                uploadedvideoFileURL = resulttype;
                                if (uploadedvideoFileURL == null)
                                {
                                    ModelState.AddModelError(string.Empty, "Video Upload is failed.");
                                    await GetStatusList(model);
                                    return View("ProctorReport", model);
                                }
                                else
                                {
                                    model.VideoLink = uploadedvideoFileURL;
                                }
                            }
                        }
                        else if (model.PageVideoLink != null)
                        {
                            model.VideoLink = model.PageVideoLink;
                        }
                        if (await _meetingService.AddInterviewReport(model, emailid))
                        {
                            //if (model.MeetingStatus == 4 && model.PreviousMeetingStatus != model.MeetingStatus)
                            //{
                            //    //Send mail to client for report
                            //    //find all IRFowner and Users
                            //    var clientlist = _meetingService.FetchClientListByIRFID(model.IRFId);
                            //    var maillist = string.Join(";", clientlist);
                            //    string sendreporturl = "https://" + Request.Host.ToString() + "/IRF/InstaAddReport?interviewId=" + model.InterviewId + "&irfId=" + model.IRFId;
                            //    Mail _mail1 = new Mail
                            //    {
                            //        TO = maillist,
                            //        Subject = "Insta Report URL of " + model.CandidateName + " - " + model.PrimarySkill
                            //    };
                            //    var emailsend1 = _notificationHelper.InstaReportClientNotification(_mail1, sendreporturl, model.CandidateName);
                            //    //send mail to candiate or interviewer
                            //    string currenturl = "https://" + Request.Host.ToString() + "/Survey/Index?id=" + model.InterviewId;
                            //    Mail _mail = new Mail
                            //    {
                            //        TO = model.CandidateEmail,
                            //        Subject = "Risebird: Feedback form"
                            //    };
                            //    var emailsend = _notificationHelper.SurveyNotification(_mail, currenturl);
                            //    WhatsAppVM _whatsAppVM = new WhatsAppVM();
                            //    _whatsAppVM.Emailid = model.CandidateEmail;
                            //    _whatsAppVM.buttonlink = !string.IsNullOrEmpty(currenturl) ? currenturl.Replace("https://" + Request.Host.ToString() + "/", "") : "";
                            //    _whatsAppVM.Event = "Feedback Remainder";
                            //    _whatsAppHelper.WhatsAppSendFeedbackRemainder(_whatsAppVM);
                            //}
                            TempData["SuccessErrorMessageInterview"] = "Report has been saved Successfully!";
                            if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                            {
                                return RedirectToAction("Index");
                            }
                            else
                            {
                                return RedirectToAction("MyInterview");
                            }
                        }
                        else
                        {
                            TempData["SuccessErrorMessageInterview"] = "Report was not saved.";
                            if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                            {
                                return RedirectToAction("Index");
                            }
                            return RedirectToAction("MyInterview");
                        }
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessageInterview"] = "Report was not saved.";
                return RedirectToAction("Index");
            }
        }
        #endregion
        public string DeleteMeetingLink(string meetingid)
        {
            string token = _zoomHelper.ZoomToken();//ZoomToken();
            var client = new RestClient(string.Concat(_configuration["ZoomAppDetails:endPoint"], $"meetings/" + meetingid));
            var request = new RestRequest(string.Concat(_configuration["ZoomAppDetails:endPoint"], $"meetings/" + meetingid), Method.Delete);
            request.AddHeader("content-type", "application/json");
            request.AddHeader("authorization", "Bearer " + token);
            RestResponse response = client.Execute(request);
            var content = response.Content;
            return "Deleted successfully";
        }
        //public string ZoomToken()
        //{
        //    DateTime Expiry = DateTime.UtcNow.AddDays(1);
        //    string ApiKey = _configuration["ZoomAppDetails:apiKey"];
        //    string ApiSecret = _configuration["ZoomAppDetails:secretKey"];
        //    int ts = (int)(Expiry - new DateTime(1970, 1, 1)).TotalSeconds;
        //    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ApiSecret));
        //    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        //    var header = new JwtHeader(credentials);
        //    var payload = new JwtPayload
        //    {
        //        { "iss", ApiKey},
        //        { "exp", ts },
        //    };
        //    var secToken = new JwtSecurityToken(header, payload);
        //    var handler = new JwtSecurityTokenHandler();
        //    var tokenString = handler.WriteToken(secToken);
        //    return tokenString;
        //}
        [HttpPost]
        public JsonResult ChangeInterviewernotificationStatus(int status)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Interviewer")
                {
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    var myInterview = _meetingService.ChangeInterviewerNotificationStatus(emailid, status);
                    return Json("Success");
                }
                else
                {
                    return Json("Fail");
                }
            }
            else
            {
                return Json("Fail");
            }
        }

        #region For QC
        public async Task<IActionResult> CompletedInterviews()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                    ViewBag.emailid = emailId;
                    ViewBag.PageFrom = "Completed";

                    if (profileName == "Quality Controller" || profileName == "Admin" || profileName == "SupportAdmin")
                    {
                        ViewBag.profile = profileName;
                        if (TempData["SuccessErrorMessageQCForm"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageQCForm"].ToString();
                            TempData.Remove("SuccessErrorMessageQCForm");
                        }
                        List<MeetingVM> modelList = new List<MeetingVM>();
                        if (HttpContext.Session.GetString("CompletedInterviewfilters") != null)
                        {
                            var filter = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("CompletedInterviewfilters"));
                            if (filter.StartDate != null && filter.EndDate != null)
                            {
                                modelList = await _meetingService.GetCompletedInterviewList(filter.StartDate.Value, filter.EndDate.Value.AddDays(1));
                            }
                            modelList = modelList.Select(x =>
                            {
                                x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty;
                                return x;
                            }).ToList();

                            filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = modelList.Select(x => x.JDTiltle.Split("|")[0]).ToArray() : filter.PrimarySkill = filter.PrimarySkill;
                            filter.RFID = filter.RFID == null ? filter.RFID = modelList.Select(x => x.IRFId).ToArray() : filter.RFID = filter.RFID;
                            filter.InterviewStatus = filter.InterviewStatus == null ? filter.InterviewStatus = modelList.Select(x => x.intervieweStatusStatDesc).ToArray() : filter.InterviewStatus = filter.InterviewStatus;
                            filter.Interviewer = filter.Interviewer == null ? filter.Interviewer = modelList.Select(x => x.interviewer).ToArray() : filter.Interviewer = filter.Interviewer;
                            filter.ClientName = filter.ClientName == null ? filter.ClientName = modelList.Select(x => x.ClientName).ToArray() : filter.ClientName = filter.ClientName;
                            filter.Client = filter.Client == null ? filter.Client = modelList.Select(x => x.Client).ToArray() : filter.Client = filter.Client;

                            modelList = modelList.Where(c => filter.PrimarySkill != null && filter.PrimarySkill.Contains(c.JDTiltle.Split("|")[0])
                            && filter.RFID.Contains(c.IRFId)
                            && ((filter.InterviewStatus != null && filter.InterviewStatus.Count() > 0) ? filter.InterviewStatus.Contains(c.intervieweStatusStatDesc) : c.intervieweStatusStatDesc == string.Empty)
                            && ((filter.Interviewer != null && filter.Interviewer.Count() > 0) ? filter.Interviewer.Contains(c.interviewer) : c.interviewer == null)
                            && ((filter.ClientName != null && filter.ClientName.Count() > 0) ? filter.ClientName.Contains(c.ClientName) : c.ClientName == null)
                            && ((filter.Client != null && filter.Client.Count() > 0) ? filter.Client.Contains(c.Client) : c.Client == null)).ToList();
                        }
                        else
                        {
                            modelList = await _meetingService.GetCompletedInterviewList(DateTime.Now.Date.AddDays(-2), DateTime.Now.Date.AddDays(1));
                            modelList = modelList.Select(x =>
                            {
                                x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty;
                                return x;
                            }).ToList();
                        }
                        return View(modelList);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        [HttpPost]
        public async Task<IActionResult> CompletedInterviews(InterviewFilter filter)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (filter != null)
                    {
                        HttpContext.Session.SetString("CompletedInterviewfilters", JsonConvert.SerializeObject(filter));
                    }
                    return RedirectToAction("CompletedInterviews");
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<IActionResult> ResetCompletedInterviewsFilter()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("CompletedInterviewfilters");
                    return RedirectToAction("CompletedInterviews");
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        //Create QCForm
        public async Task<IActionResult> QCReportForm(int interviewid, string interviewer, string type, string uniqueid)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                    ViewBag.emailid = emailId;
                    if (profileName == "Quality Controller" || profileName == "Interviewer" || profileName == "Admin" || profileName == "SupportAdmin" || profileName == "Client")
                    {
                        ViewBag.profile = profileName;
                        var model = await _meetingService.FetchQCFormDetailsById(interviewid);
                        if (model == null)
                        {
                            model = new QCFormVM();
                            setDefaultFlagsValue(model);
                            model.Isedit = false;
                        }
                        else
                        {
                            model.Isedit = true;
                        }
                        if (model != null)
                        {
                            model.uniqueid = uniqueid;
                            model.Emailid = emailId;
                            model.Interviewer = interviewer;
                            model.Profile = profileName;
                            model.Type = type;
                        }
                        return View(model);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        [HttpPost]
        public async Task<IActionResult> QCReportForm(QCFormVM model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                    ViewBag.emailid = emailId;
                    if (profileName == "Quality Controller" || profileName == "Admin" || profileName == "SupportAdmin" || profileName == "Client")
                    {
                        ViewBag.profile = profileName; ViewBag.profile = profileName;
                        model.Emailid = emailId;
                        var redflag = CheckFlagCount(model);
                        model.Redflags = redflag;
                        if (await _meetingService.AddorUpdateQCFormReport(model))
                        {
                            if (!model.Isedit)
                            {
                                if (redflag > 0)
                                {
                                    await _meetingService.ApplyPenaltyQCForm(model.Interviewid, model.Interviewer, redflag);
                                }
                            }
                            else
                            {
                                await _meetingService.ApplyPenaltyQCForm(model.Interviewid, model.Interviewer, redflag);
                            }
                            var _clienthr = await _meetingService.GetClientHRContactByInterviewid(model.Interviewid);
                            int rating = 0;
                            if (redflag > 16)
                            {
                                rating = 0;
                            }
                            else
                            {
                                rating = 5 - (redflag / 4);
                            }
                            var _model = new InterviewerRating();
                            _model.StarRating = rating;
                            _model.Comment = "Redflags raised: " + redflag;
                            _model.Emailid = _clienthr;
                            _model.Interviewid = model.Interviewid;
                            _model.ClientAction = "";
                            var result = await _meetingService.SaveRatingDetails(_model);
                            TempData["SuccessErrorMessageQCForm"] = "QC Form successfully saved.";
                        }
                        else
                        {
                            TempData["SuccessErrorMessageQCForm"] = "Fail to save QC Form.";
                        }
                        if (model.Type == "Completed")
                        {
                            return RedirectToAction("CompletedInterviews");
                        }
                        else if (model.Type == "ClientAll")
                        {
                            return RedirectToAction("AllMeetings", "Client");
                        }
                        else if (model.Type == "ClientSelect")
                        {
                            return RedirectToAction("SelectedMeetings", "Client");
                        }
                        else if (model.Type == "ClientReject")
                        {
                            return RedirectToAction("RejectedMeetings", "Client");
                        }
                        else if (model.Type == "ClientPublic")
                        {
                            return RedirectToAction("PublicCandidateStatus", "Candidate", new { emailid = model.Emailid, id = model.uniqueid });
                        }
                        else if (model.Type == "ClientAddReport")
                        {
                            return RedirectToAction("Report", "Client", new { interviewId = model.Interviewid, irfId = model.IRFId });
                        }
                        else if (model.Type == "InterviewAddReport")
                        {
                            return RedirectToAction("AddReport", "Interview", new { interviewId = model.Interviewid, irfId = model.IRFId });
                        }
                        else if (model.Type == "SecureAddReport")
                        {
                            return RedirectToAction("SecureReport", "Interview", new { interviewId = model.Interviewid, irfId = model.IRFId, emailid = model.Emailid, id = model.uniqueid });
                        }
                        else
                        {
                            return RedirectToAction("ResolvedRedFlagsInterviews");
                        }
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessageQCForm"] = "Fail to save QC Form.";
                if (model.Type == "Completed")
                {
                    return RedirectToAction("CompletedInterviews");
                }
                else if (model.Type == "ClientAll")
                {
                    return RedirectToAction("AllMeetings", "Client");
                }
                else if (model.Type == "ClientSelect")
                {
                    return RedirectToAction("SelectedMeetings", "Client");
                }
                else if (model.Type == "ClientReject")
                {
                    return RedirectToAction("RejectedMeetings", "Client");
                }
                else if (model.Type == "ClientPublic")
                {
                    return RedirectToAction("PublicCandidateStatus", "Candidate", new { emailid = model.Emailid, id = model.uniqueid });
                }
                else if (model.Type == "ClientAddReport")
                {
                    return RedirectToAction("Report", "Client", new { interviewId = model.Interviewid, irfId = model.IRFId });
                }
                else if (model.Type == "InterviewAddReport")
                {
                    return RedirectToAction("AddReport", "Interview", new { interviewId = model.Interviewid, irfId = model.IRFId });
                }
                else if (model.Type == "SecureAddReport")
                {
                    return RedirectToAction("SecureReport", "Interview", new { interviewId = model.Interviewid, irfId = model.IRFId, emailid = model.Emailid, id = model.uniqueid });
                }
                else
                {
                    return RedirectToAction("ResolvedRedFlagsInterviews");
                }
            }
        }
        //Save QCForm
        public int CheckFlagCount(QCFormVM model)
        {
            int flags = 0;
            if (!model.IsCodingPresent)
            {
                flags = flags + 1;
            }
            if (!model.IsVideoClearQuality)
            {
                flags = flags + 1;
            }
            if (!model.IsInterviewerCommunicationClear)
            {
                flags = flags + 1;
            }
            if (!model.IsSufficientVideoDuration)
            {
                flags = flags + 1;
            }
            if (!model.IsInterviewStructured)
            {
                flags = flags + 1;
            }
            //
            if (!model.IsScoringrubriccorrect)
            {
                flags = flags + 1;
            }
            if (!model.IsQuestionswithtime)
            {
                flags = flags + 1;
            }
            if (!model.IsCommentfilled)
            {
                flags = flags + 1;
            }
            if (!model.IsMeetingStatusfilled)
            {
                flags = flags + 1;
            }
            if (!model.IsRelevancequestions)
            {
                flags = flags + 1;
            }
            //
            if (model.IsDelayresuminginterview)
            {
                flags = flags + 1;
            }
            if (model.IsBackgroundnoise)
            {
                flags = flags + 1;
            }
            if (model.IsPoorinterviewerinfrastructure)
            {
                flags = flags + 1;
            }
            //
            if (model.IsPoorhandlingcandidatequery)
            {
                flags = flags + 1;
            }
            if (model.IsPersonalquestions)
            {
                flags = flags + 1;
            }
            if (model.IsAugmenting)
            {
                flags = flags + 1;
            }
            if (model.IsBiasedView)
            {
                flags = flags + 1;
            }
            if (!model.IsLettingcandidatespeak)
            {
                flags = flags + 1;
            }
            //
            if (model.IsSuspiciousappfound)
            {
                flags = flags + 1;
            }
            if (model.IsLipsyncissue)
            {
                flags = flags + 1;
            }
            return flags;
        }
        [Authorize]
        [HttpGet]
        public async Task<FileContentResult> ExportQCReportCompleted()
        {
            ExcelPackage Ep = new ExcelPackage();
            ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("QC Report Form");
            string fileName = "Risebird" + DateTime.Now.ToString("dd/MM/yyyy") + "_Interviews" + ".xlsx";
            try
            {
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                List<QCFormVM> modelList = new List<QCFormVM>();
                if (HttpContext.Session.GetString("CompletedInterviewfilters") != null)
                {
                    var filter = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("CompletedInterviewfilters"));
                    if (filter.StartDate != null && filter.EndDate != null)
                    {
                        modelList = await _meetingService.FetchAllQCFormDetails(filter.StartDate.Value, filter.EndDate.Value.AddDays(1));
                    }
                }
                else
                {
                    modelList = await _meetingService.FetchAllQCFormDetails(DateTime.Now.Date, DateTime.Now.Date.AddDays(1));
                }
                Sheet.Cells["A1:AI1"].Style.Font.Bold = true;
                Sheet.Cells["A1:AI1"].Style.Font.Size = 12;
                Sheet.Cells["A1:AI1"].Style.WrapText = true;
                int startColumn = 1; int endColumn = 40;
                for (int a = startColumn; a <= endColumn; a++)
                {
                    Sheet.Column(a).Width = 40;
                }
                Sheet.Cells[1, 1].Value = "IRF ID";
                Sheet.Cells[1, 2].Value = "Client";
                Sheet.Cells[1, 3].Value = "Interviewer";
                Sheet.Cells[1, 4].Value = "QCFormedBy";
                Sheet.Cells[1, 5].Value = "Candidate Email";
                Sheet.Cells[1, 6].Value = "Interview Date";

                Sheet.Cells[1, 7].Value = "Whether hands-on coding is present";
                Sheet.Cells[1, 8].Value = "Whether candidate video is clearly visible";
                Sheet.Cells[1, 9].Value = "Clarity in Interviewer’s communication";
                Sheet.Cells[1, 10].Value = "Whether video duration is sufficient";
                Sheet.Cells[1, 11].Value = "Is the interview properly structured";
                Sheet.Cells[1, 12].Value = "Whether scoring rubrics is filled correctly";
                Sheet.Cells[1, 13].Value = "Whether questions are filled with timeline";
                Sheet.Cells[1, 14].Value = "Whether appropriate/fitting comments are filled";
                Sheet.Cells[1, 15].Value = "Whether appropriate meeting and interview status are filled";
                Sheet.Cells[1, 16].Value = "Relevance of the questions asked";
                Sheet.Cells[1, 17].Value = "Delay in resuming an interview";
                Sheet.Cells[1, 18].Value = "Background Noise";
                Sheet.Cells[1, 19].Value = "Poor interviewer infrastructure";
                Sheet.Cells[1, 20].Value = "Poor handling of candidate’s queries";
                Sheet.Cells[1, 21].Value = "Asking personal questions or making personal remarks";
                Sheet.Cells[1, 22].Value = "Getting into arguments";
                Sheet.Cells[1, 23].Value = "Biased View";
                Sheet.Cells[1, 24].Value = "Letting candidate speak";
                Sheet.Cells[1, 25].Value = "Suspicious app found";
                Sheet.Cells[1, 26].Value = "Lip-sync issue noticed";
                Sheet.Cells[1, 27].Value = "Total Red Flags";
                int i = 2;
                if (modelList.Count() > 0)
                {
                    foreach (var item in modelList)
                    {
                        Sheet.Cells[i, 1].Value = item.IRFId;
                        Sheet.Cells[i, 2].Value = item.Client;
                        Sheet.Cells[i, 3].Value = item.Interviewer;
                        Sheet.Cells[i, 4].Value = item.QCFormedBy;
                        Sheet.Cells[i, 5].Value = item.Emailid;
                        Sheet.Cells[i, 6].Value = item.InterviewDate.ToString("MM/dd/yyyy HH:mm");

                        Sheet.Cells[i, 7].Value = item.IsCodingPresent;
                        Sheet.Cells[i, 8].Value = item.IsVideoClearQuality;
                        Sheet.Cells[i, 9].Value = item.IsInterviewerCommunicationClear;
                        Sheet.Cells[i, 10].Value = item.IsSufficientVideoDuration;
                        Sheet.Cells[i, 11].Value = item.IsInterviewStructured;
                        Sheet.Cells[i, 12].Value = item.IsScoringrubriccorrect;
                        Sheet.Cells[i, 13].Value = item.IsQuestionswithtime;
                        Sheet.Cells[i, 14].Value = item.IsCommentfilled;
                        Sheet.Cells[i, 15].Value = item.IsMeetingStatusfilled;
                        Sheet.Cells[i, 16].Value = item.IsRelevancequestions;
                        Sheet.Cells[i, 17].Value = item.IsDelayresuminginterview;
                        Sheet.Cells[i, 18].Value = item.IsBackgroundnoise;
                        Sheet.Cells[i, 19].Value = item.IsPoorinterviewerinfrastructure;
                        Sheet.Cells[i, 20].Value = item.IsPoorhandlingcandidatequery;
                        Sheet.Cells[i, 21].Value = item.IsPersonalquestions;
                        Sheet.Cells[i, 22].Value = item.IsAugmenting;
                        Sheet.Cells[i, 23].Value = item.IsBiasedView;
                        Sheet.Cells[i, 24].Value = item.IsLettingcandidatespeak;
                        Sheet.Cells[i, 25].Value = item.IsSuspiciousappfound;
                        Sheet.Cells[i, 26].Value = item.IsLipsyncissue;
                        Sheet.Cells[i, 27].Value = CheckFlagCount(item);
                        i++;
                    }
                    return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
                else
                {
                    ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                    Rng.Value = "No data available in table !!!";
                    return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception)
            {
                ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                Rng.Value = "Data export failed !!!";
                return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> QCReportDetails()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                    ViewBag.emailid = emailId;
                    if (profileName == "Interviewer" || profileName == "Admin" || profileName == "SupportAdmin")
                    {
                        ViewBag.profile = profileName;
                        List<MeetingVM> modelList = new List<MeetingVM>();
                        modelList = await _meetingService.QCReportDetailsByInterviewer(emailId);
                        modelList = modelList.Select(x =>
                        {
                            x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty;
                            x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty;
                            return x;
                        }).ToList();
                        return View(modelList);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        [HttpPost]
        public async Task<JsonResult> QCReportRedFlagsResolved(int interviewid)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Interviewer" || profileName == "Admin" || profileName == "SupportAdmin")
                    {
                        var _updatelist = await _meetingService.UpdateQCReportRedFlagsResolved(interviewid);
                        return Json("Successfully Updated");
                    }
                    else
                    {
                        return Json("failed");
                    }
                }
                else
                {
                    return Json("failed");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("failed");
            }
        }
        [HttpGet]
        public async Task<IActionResult> ResolvedRedFlagsInterviews()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                    ViewBag.emailid = emailId;
                    ViewBag.PageFrom = "Resloved";
                    if (profileName == "Quality Controller" || profileName == "Admin" || profileName == "SupportAdmin")
                    {
                        ViewBag.profile = profileName;
                        if (TempData["SuccessErrorMessageQCForm"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageQCForm"].ToString();
                            TempData.Remove("SuccessErrorMessageQCForm");
                        }
                        List<MeetingVM> modelList = new List<MeetingVM>();
                        modelList = await _meetingService.GetReslovedRedFlagsInterviewList();
                        modelList = modelList.Select(x =>
                        {
                            x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty;
                            return x;
                        }).ToList();
                        return View("CompletedInterviews", modelList);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public void setDefaultFlagsValue(QCFormVM model)
        {
            model.IsCodingPresent = true;
            model.IsVideoClearQuality = true;
            model.IsInterviewerCommunicationClear = true;
            model.IsSufficientVideoDuration = true;
            model.IsInterviewStructured = true;

            model.IsScoringrubriccorrect = true;
            model.IsQuestionswithtime = true;
            model.IsCommentfilled = true;
            model.IsMeetingStatusfilled = true;
            model.IsRelevancequestions = true;

            model.IsDelayresuminginterview = false;
            model.IsBackgroundnoise = false;
            model.IsPoorinterviewerinfrastructure = false;

            model.IsPoorhandlingcandidatequery = false;
            model.IsPersonalquestions = false;
            model.IsAugmenting = false;
            model.IsBiasedView = false;
            model.IsLettingcandidatespeak = true;

            model.IsSuspiciousappfound = false;
            model.IsLipsyncissue = false;
        }
        #endregion
        [HttpPost]
        public async Task<JsonResult> FetchInterviewVideoDetails(int interviewid)
        {
            var model = await _meetingService.GetInterviewMeetingById(interviewid);
            return Json(model);
        }
        [HttpPost]
        public async Task<JsonResult> SaveVideoDetails(int interviewid, string VideoURL, int videoduration, string CandidateEmail)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var emailId = HttpContext.Session.GetString("email")?.Trim();
                emailId = emailId.Replace("\"", "");
                if (string.IsNullOrEmpty(VideoURL))
                {
                    VideoURL = "";
                }
                var model = await _meetingService.SaveVideoDetailsById(interviewid, VideoURL, videoduration, CandidateEmail, emailId);
                if (model)
                {
                    return Json("Video details are updated successfully.");
                }
                else
                {
                    return Json("Failed to update video details, please try again.");
                }
            }
            else
            {
                return Json("Failed to update video details, please try again.");
            }
        }
        #region Redo Interviews
        [HttpGet]
        public async Task<IActionResult> RedoInterviewDetails(int interviewid, string interviewstatus, string type, string uniqueid)
        {
            var model = new RedoInterviewVM();
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var emailId = HttpContext.Session.GetString("email")?.Trim();
                emailId = emailId.Replace("\"", "");
                var interviewdetails = await _meetingService.GetInterviewDetailsById(interviewid);
                model.interviewid = interviewid;
                model.RequiredDuration = interviewdetails.RequiredDuration;
                model.ActualDuration = interviewdetails.ActualDuration;
                model.type = type;
                model.ModifiedBy = emailId;
                model.uniqueid = uniqueid;
                if (interviewstatus.ToLower() == "Selected")
                {
                    model.Inappropriate = "Selection";
                }
                else
                {
                    model.Inappropriate = "Rejection";
                }
                var _interview = await _meetingService.GetRedoInterviewDetailsById(interviewid);
                if (_interview != null)
                {
                    model = _interview;
                }
                else
                {
                    model.CandidateFace = "Not Visible";
                    model.HandsOfCoding = "Notasperrequirement";
                }
                model.IRFId = interviewdetails.IRFId;
            }
            return PartialView(model);
        }
        [HttpPost]
        public async Task<IActionResult> RedoInterviewDetails(RedoInterviewVM model)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var emailId = HttpContext.Session.GetString("email")?.Trim();
                emailId = emailId.Replace("\"", "");
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                model.ModifiedBy = emailId;
                var result = await _meetingService.SaveRedoInterviewDetails(model);
                if (profileName == "Client")
                {
                    AddUpdateMeetingVM meetingToUpdate = await _meetingService.GetMeeting(model.interviewid);
                    await SendRescheduleMeetingInvite(meetingToUpdate.irfIntId, meetingToUpdate.candidateEmail, meetingToUpdate.imid);
                }
                if (result)
                {
                    TempData["SuccessErrorMessageQCForm"] = "Interview request for redo is successfully.";
                }
            }
            else
            {
                TempData["SuccessErrorMessageQCForm"] = "Interview request for redo is failed.";
            }
            if (model.type == "Completed")
            {
                return RedirectToAction("CompletedInterviews");
            }
            else if (model.type == "ClientAll")
            {
                return RedirectToAction("AllMeetings", "Client");
            }
            else if (model.type == "ClientSelect")
            {
                return RedirectToAction("SelectedMeetings", "Client");
            }
            else if (model.type == "ClientReject")
            {
                return RedirectToAction("RejectedMeetings", "Client");
            }
            else if (model.type == "ClientPublic")
            {
                return RedirectToAction("PublicCandidateStatus", "Candidate", new { emailid = model.ModifiedBy, id = model.uniqueid });
            }
            else if (model.type == "ClientAddReport")
            {
                return RedirectToAction("Report", "Client", new { interviewId = model.interviewid, irfId = model.IRFId });
            }
            else if (model.type == "InterviewAddReport")
            {
                return RedirectToAction("AddReport", "Interview", new { interviewId = model.interviewid, irfId = model.IRFId });
            }
            else if (model.type == "SecureAddReport")
            {
                return RedirectToAction("SecureReport", "Interview", new { interviewId = model.interviewid, irfId = model.IRFId, emailid = model.ModifiedBy, id = model.uniqueid });
            }
            return RedirectToAction("CompletedInterviews");
        }
        [HttpGet]
        public async Task<IActionResult> RejectRedoInterviewDetails(int interviewid)
        {
            var interviewdetails = await _meetingService.GetRedoInterviewDetailsById(interviewid);
            return PartialView(interviewdetails);
        }
        [HttpPost]
        public async Task<IActionResult> RejectRedoInterviewDetails(RedoInterviewVM model)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var emailId = HttpContext.Session.GetString("email")?.Trim();
                emailId = emailId.Replace("\"", "");
                model.ModifiedBy = emailId;
                var result = await _meetingService.UpdateRejectRedoInterviewDetails(model);
                if (result)
                {
                    TempData["SuccessErrorMessageInterview"] = "Interview request for redo is successfully.";
                    return RedirectToAction("Index", new { type = "RedoInterviews" });
                }
            }
            TempData["SuccessErrorMessageInterview"] = "Interview request for redo is failed.";
            return RedirectToAction("Index", new { type = "RedoInterviews" });
        }
        public async Task<IActionResult> SendRescheduleInvite(int irfId, string CandidateemailId, int interviewid)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var loggedInUserEmail = HttpContext.Session.GetString("email")?.Trim();
                    loggedInUserEmail = loggedInUserEmail.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        var linkurl = "https://" + Request.Host.ToString();
                        if (await _iRFService.SendRedoInvite(irfId, CandidateemailId, loggedInUserEmail, linkurl))
                        {
                            await _iRFService.SendClientRedoInvite(irfId, interviewid, CandidateemailId);
                            var result = await _meetingService.ResetInterviewPayment(interviewid);
                            TempData["SuccessErrorMessageInterview"] = "Invitation has been sent Successfully!";
                            return RedirectToAction("Index", new { type = "RedoInterviews" });
                        }
                        else
                        {
                            TempData["SuccessErrorMessageInterview"] = "Sorry! Could not send invite!";
                            return RedirectToAction("Index", new { type = "RedoInterviews" });
                        }
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessageInterview"] = "Sorry! Could not send invite!";
                return RedirectToAction("Index", new { type = "RedoInterviews" });
            }
        }
        public async Task<IActionResult> RedoInterviews()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                    ViewBag.emailid = emailId;
                    ViewBag.PageFrom = "Completed";
                    if (profileName == "Quality Controller" || profileName == "Admin" || profileName == "SupportAdmin")
                    {
                        List<MeetingVM> modelList = new List<MeetingVM>();
                        modelList = await _meetingService.GetRedoInterviewList();
                        modelList = modelList.Select(x =>
                        {
                            x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty;
                            return x;
                        }).ToList();
                        return View(modelList);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        #endregion
        [HttpGet]
        public async Task<JsonResult> GetMeetingPlatform(string irfid)
        {
            try
            {
                var meetingplatform = await _meetingService.GetMeetingPlatformbyIRFId(irfid);
                if (meetingplatform == null)
                {
                    meetingplatform = MeetingPlatforms.Zoom;
                }
                return Json(meetingplatform);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("failed");
            }
        }
        public async Task<JsonResult> GenerateZoomMeeting(int interviewid, string IRFId, string clientname, int irfIntId)
        {
            AddUpdateMeetingVM meetingToUpdate = await _meetingService.GetMeeting(interviewid);
            var jdDetails = (await _interviewSchedulerservice.FetchJDTitle(IRFId));
            //var JDTitle = (await  _interviewSchedulerservice.FetchJDTitle(IRFId)).JDTiltle;
            var zoomLink = new InstaZoomMeetingResponse();
            zoomLink = await CreateZoomMeeting(meetingToUpdate.candidateName, jdDetails.JDTiltle, meetingToUpdate.interviewDate, meetingToUpdate.risebirdHR);
            if (zoomLink == null)
            {
                //Send Rescheule mail to candidtae
                await SendbackupRescheduleMeetingInvite(irfIntId, meetingToUpdate.candidateEmail, interviewid);
                return Json("FailToSet");
            }
            _ = await _interviewSchedulerservice.UpdateMeetingLinkDetails(interviewid, zoomLink.join_url, zoomLink.id.ToString());
            string timezonevalue = string.Empty;
            var offset = -330;
            string hh = string.Empty;
            var valuecheck = (-offset / 60);
            if (valuecheck < 10 && valuecheck > 0)
            {
                hh = "0" + valuecheck;
            }
            if (valuecheck > -10 && valuecheck < 0)
            {
                hh = "-0" + valuecheck.ToString().Replace("-", "");
            }
            var offsetvalue = (hh + ":" + ((offset % 60) < 0 ? (-offset % 60) : (offset % 60))).ToString();
            var timeZones = TimeZoneInfo.GetSystemTimeZones();
            foreach (TimeZoneInfo timeZone in timeZones)
            {
                if (timeZone.DisplayName.Contains(offsetvalue))
                {
                    timezonevalue = timeZone.DisplayName;
                    break;
                }
            }
            Mail _mail = new Mail
            {
                TO = meetingToUpdate.candidateEmail,
                //CC = "hire@risebird.io",
                Subject = "Invitation: " + meetingToUpdate.candidateName + " @ " + meetingToUpdate.interviewDate + "(" + timezonevalue + ")"
            };
            _ = await _notificationHelper.SendInterviewInvite(_mail, meetingToUpdate.candidateEmail, zoomLink.join_url, meetingToUpdate.staffingAgency, meetingToUpdate.ClientHrContact, "hire@risebird.io", meetingToUpdate.interviewDate, meetingToUpdate.candidateName, timezonevalue, clientname, offset, interviewid != 0 ? interviewid : new int?(), jdDetails.JDLink);
            return Json(zoomLink.join_url);
        }
        public async Task<InstaZoomMeetingResponse> CreateZoomMeeting(string CandidateName, string Role, DateTime indianTime, string accountEmail)
        {
            InstaZoomMeetingVM _zoommetingobject = new InstaZoomMeetingVM();
            _zoommetingobject.topic = CandidateName + "| " + Role + "| Video Interview";
            //TimeZoneInfo INDIAN_ZONE = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            //DateTime indianTime = TimeZoneInfo.ConvertTimeFromUtc(interviewDate, INDIAN_ZONE);
            _zoommetingobject.start_time = indianTime.ToString("yyyy-MM-ddThh:mm:ss");
            accountEmail = await _interviewSchedulerservice.GetAvailableZoomAccount(indianTime.AddMinutes(-30), indianTime.AddMinutes(30), accountEmail);
            if (string.IsNullOrEmpty(accountEmail))
            {
                return null;
            }
            var zoomlink = CreateZoomMeetings(_zoommetingobject, accountEmail, indianTime);
            zoomlink.accountemail = accountEmail;
            var succeresult = await _interviewSchedulerservice.UpdateInstaZoomDetails(indianTime, indianTime.AddMinutes(60), accountEmail);
            return zoomlink;
        }
        private InstaZoomMeetingResponse CreateZoomMeetings(InstaZoomMeetingVM _zoommetingobject, string accountEmail, DateTime indianTime)
        {
            _zoommetingobject.type = 2;
            _zoommetingobject.duration = 60;
            _zoommetingobject.timezone = "Asia/Calcutta";
            _zoommetingobject.settings.host_video = true;
            _zoommetingobject.settings.participant_video = true;
            _zoommetingobject.settings.join_before_host = true;
            _zoommetingobject.settings.jbh_time = 5;
            _zoommetingobject.settings.auto_recording = "cloud";
            string token = _zoomHelper.ZoomToken();//ZoomToken();

            var client = new RestClient(string.Concat(_configuration["ZoomAppDetails:endPoint"], $"users/" + accountEmail + "/meetings"));

            var request = new RestRequest(string.Concat(_configuration["ZoomAppDetails:endPoint"], $"users/" + accountEmail + "/meetings"), Method.Post);
            request.AddHeader("content-type", "application/json");
            request.AddHeader("authorization", "Bearer " + token);
            var body = JsonConvert.SerializeObject(_zoommetingobject);
            request.AddParameter("application/json", body, ParameterType.RequestBody);
            RestResponse response = client.Execute(request);
            var content = response.Content;
            var convertedResponse = JsonConvert.DeserializeObject<InstaZoomMeetingResponse>(content);
            convertedResponse.InterviewDate = indianTime;
            convertedResponse.accountemail = accountEmail;
            return convertedResponse;
        }
        public async Task<string> SendRescheduleMeetingInvite(int irfId, string CandidateemailId, int interviewid)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var loggedInUserEmail = HttpContext.Session.GetString("email")?.Trim();
                loggedInUserEmail = loggedInUserEmail.Replace("\"", "");
                var linkurl = "https://" + Request.Host.ToString();
                if (await _iRFService.SendRedoInvite(irfId, CandidateemailId, loggedInUserEmail, linkurl))
                {
                    await _iRFService.SendClientRedoInvite(irfId, interviewid, CandidateemailId);
                    var result = await _meetingService.ResetInterviewPayment(interviewid);
                    return "Success";
                }
                else
                {
                    return "Fail";
                }
            }
            else
            {
                return "Fail";
            }
        }
        [HttpGet]
        public async Task<IActionResult> SecureReport(int interviewId, int irfId, string uniqueid, string emailid)
        {
            try
            {
                ViewBag.Id = uniqueid;
                ViewBag.interviewid = interviewId;
                ViewBag.irfId = irfId;
                var email = HttpContext.Session.GetString("email");
                if (string.IsNullOrWhiteSpace(email))
                {
                    return RedirectToAction("AddSecureReport", new { interviewId = interviewId, irfId = irfId, uniqueid = uniqueid });
                }

                email = email.Replace("\"", "");
                if (emailid != email)
                {
                    return RedirectToAction("AddSecureReport", new { interviewId = interviewId, irfId = irfId, uniqueid = uniqueid });
                }
                var model = await _meetingService.GetInteviewReport(interviewId, irfId);
                if (model == null)
                {
                    return RedirectToAction("AddSecureReport", new { interviewId = interviewId, irfId = irfId, uniqueid = uniqueid });
                }
                if (model.QuestionsCollection == null)
                {
                    ViewBag.QuestionsDiscussedCount = 0;
                }
                else
                {
                    foreach (var question in model.QuestionsCollection)
                    {
                        question.Comment = question.Comment.Replace("\t", " ").Replace("\n", " ").Replace("\r", " ");
                    }
                    ViewBag.QuestionsDiscussedCount = model.QuestionsCollection?.Count();
                }
                model.PreviousMeetingStatus = model.MeetingStatus;
                await GetStatusList(model, irfId, 0);
                model.ResourceUrl = Request.Scheme + "://" + Request.Host.Value + Request.Path + Request.QueryString;
                var client = await _userRepository.GetClientUserDetails(email);
                if (client != null)
                {
                    model.IsQCRequired = client.IsQCRequired;
                    model.IsRedoInterview = client.IsRedoInterview;
                }
                if (TempData["SuccessErrorMessageQCForm"] != null)
                {
                    ViewBag.Message = TempData["SuccessErrorMessageQCForm"].ToString();
                    TempData.Remove("SuccessErrorMessageQCForm");
                }
                if (model.IsProctor)
                {
                    return PartialView("SecureProctorReport", model);
                }
                else
                {
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("AddSecureReport", new { interviewId = interviewId, irfId = irfId, uniqueid = uniqueid });
            }
        }
        [HttpGet]
        public async Task<IActionResult> AddSecureReport(int interviewId, int irfId, string uniqueid)
        {
            var model = new InterviewReport();
            ViewBag.PageType = "Public";
            ViewBag.Id = uniqueid;
            ViewBag.interviewid = interviewId;
            ViewBag.irfId = irfId;
            await GetStatusList(model, irfId, 0);
            return View("SecureReport", model);
        }

        [HttpPost]
        public async Task<JsonResult> AddSecureReport(string uniqueid, string Emailid, int Interviewid)
        {
            HttpContext.Session.SetString("email", JsonConvert.SerializeObject(Emailid));
            HttpContext.Session.SetString("profileName", "Client");
            var _checkdetails = await _meetingService.CheckReportClientShareDetails(Emailid, uniqueid, Interviewid);
            if (_checkdetails)
            {
                return Json("success");
            }
            else
            {
                return Json("Emailid don't have access.");
            }
        }
        public async Task<JsonResult> ShareSecureReport(int interviewId, string irfId, string emailid)
        {
            try
            {
                Guid uniqueid = Guid.NewGuid();
                var _uniqueid = await _meetingService.CheckReportClientUrlSave(emailid, uniqueid.ToString(), interviewId);
                string sendreporturl = "https://" + Request.Host.ToString() + "/Interview/AddSecureReport?interviewId=" + interviewId + "&irfId=" + irfId + "&uniqueid=" + uniqueid;
                if (!string.IsNullOrEmpty(_uniqueid))
                {
                    sendreporturl = "https://" + Request.Host.ToString() + "/Interview/AddSecureReport?interviewId=" + interviewId + "&irfId=" + irfId + "&uniqueid=" + _uniqueid;
                }
                return Json(sendreporturl);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("Failed");
            }
        }
        public async Task<string> SendbackupRescheduleMeetingInvite(int irfId, string CandidateemailId, int interviewid)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var loggedInUserEmail = HttpContext.Session.GetString("email")?.Trim();
                loggedInUserEmail = loggedInUserEmail.Replace("\"", "");
                var linkurl = "https://" + Request.Host.ToString();
                if (await _iRFService.SendRedoInvite(irfId, CandidateemailId, loggedInUserEmail, linkurl))
                {
                    var result = await _meetingService.BacklinkFailInterviewStatusUpdate(interviewid);
                    return "Success";
                }
                else
                {
                    return "Fail";
                }
            }
            else
            {
                return "Fail";
            }
        }
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ProbableNoShowCandidates()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.Profile = profileName;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller")
                    {
                        var model = await _meetingService.GetProbableNoShowCandidates();
                        model = model.Select(x =>
                        {
                            x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty;
                            return x;
                        }).ToList();
                        return View(model);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }
        [HttpPost]
        public async Task<JsonResult> CaptureJoinMeetingTime(int interviewid)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Interviewer")
                    {
                        var model = await _meetingService.SaveCapturedJoinMeetingTime(interviewid, emailid);
                        return Json("success");
                    }
                    else
                    {
                        return Json("fail");
                    }
                }
                else
                {
                    return Json("fail");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("fail");
            }
        }
    }
}