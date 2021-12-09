using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using OfficeOpenXml;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Web.Controllers
{
    [Authorize]
    public class UnapprovedInterviewerMeetingsController : Controller
    {
        private readonly IUnapprovedInterviewerMeetingsService _unapprovedInterviewerMeetingsService;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        public UnapprovedInterviewerMeetingsController(IUnapprovedInterviewerMeetingsService unapprovedInterviewerMeetingsService, IApplicationErrorLogger applicationErrorLogger)
        {
            _unapprovedInterviewerMeetingsService = unapprovedInterviewerMeetingsService;
            _applicationErrorLogger = applicationErrorLogger;
        }
        public async Task<IActionResult> Index()
        {

            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Finance" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement")
                    {
                        if (TempData["SuccessErrorMessageUnapprovedInterviewer"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageUnapprovedInterviewer"].ToString();
                            TempData.Remove("SuccessErrorMessageUnapprovedInterviewer");
                        }
                        List<MeetingVM> modelList = new List<MeetingVM>();
                        if (HttpContext.Session.GetString("UnapprovedInterviewsFilter") != null)
                        {
                            var filter = JsonConvert.DeserializeObject<UnapprovedInterviewsFilter>(HttpContext.Session.GetString("UnapprovedInterviewsFilter"));
                            if (filter.StartDate != null && filter.EndDate != null)
                            {
                                modelList = (await _unapprovedInterviewerMeetingsService.GetAllUnapprovedInterviewerMeetings(filter.StartDate.Value, filter.EndDate.Value.AddDays(1))).ToList();
                            }
                            modelList = modelList.Select(x =>
                            {
                                x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription();
                                x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewerApproved)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty;
                                x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty;
                                x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription();
                                return x;
                            }).ToList();

                            filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = modelList.Select(x => x.PrimarySkill).ToArray() : filter.PrimarySkill = filter.PrimarySkill;
                            filter.SCID = filter.SCID == null ? filter.SCID = modelList.Select(x => x.ISCId).ToArray() : filter.SCID = filter.SCID;
                            filter.ConfirmationStatus = filter.ConfirmationStatus == null ? filter.ConfirmationStatus = modelList.Select(x => x.confirmationStatus).ToArray() : filter.ConfirmationStatus = filter.ConfirmationStatus;
                            filter.MeetingStatus = filter.MeetingStatus == null ? filter.MeetingStatus = modelList.Select(x => x.MeetingStatusStatusDesc).ToArray() : filter.MeetingStatus = filter.MeetingStatus;
                            filter.InterviewStatus = filter.InterviewStatus == null ? filter.InterviewStatus = modelList.Select(x => x.intervieweStatusStatDesc).ToArray() : filter.InterviewStatus = filter.InterviewStatus;
                            filter.Interviewer = filter.Interviewer == null ? filter.Interviewer = modelList.Select(x => x.interviewer).ToArray() : filter.Interviewer = filter.Interviewer;

                            modelList = modelList.Where(c => filter.PrimarySkill != null && filter.PrimarySkill.Contains(c.PrimarySkill)
                            && filter.SCID.Contains(c.ISCId)
                            && ((filter.ConfirmationStatus != null && filter.ConfirmationStatus.Count() > 0) ? filter.ConfirmationStatus.Contains(c.confirmationStatus) : c.confirmationStatus == null)
                            && ((filter.MeetingStatus != null && filter.MeetingStatus.Count() > 0) ? filter.MeetingStatus.Contains(c.MeetingStatusStatusDesc) : c.MeetingStatusStatusDesc == string.Empty)
                            && ((filter.InterviewStatus != null && filter.InterviewStatus.Count() > 0) ? filter.InterviewStatus.Contains(c.intervieweStatusStatDesc) : c.intervieweStatusStatDesc == string.Empty)
                            && ((filter.Interviewer != null && filter.Interviewer.Count() > 0) ? filter.Interviewer.Contains(c.interviewer) : c.interviewer == null)).ToList();

                        }
                        else
                        {

                            modelList = (await _unapprovedInterviewerMeetingsService.GetAllUnapprovedInterviewerMeetings(DateTime.Now.Date, DateTime.Now.Date.AddDays(1))).ToList();
                            modelList = modelList.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewerApproved)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty; x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty; x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();
                        }
                        ViewBag.CurrentUrl = HttpContext.Request.Path + HttpContext.Request.QueryString;

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
        public async Task<FileContentResult> ExportMeetingsToExcel()
        {
            string userEmailId = string.Empty;
            ExcelPackage Ep = new ExcelPackage();
            ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Interviews");
            string fileName = "Risebird" + DateTime.Now.ToString("dd/MM/yyyy") + "_Interviewer_Discussons" + ".xlsx";
            try
            {
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                List<MeetingVM> modelList = new List<MeetingVM>();
                if (HttpContext.Session.GetString("UnapprovedInterviewsFilter") != null)
                {
                    var filter = JsonConvert.DeserializeObject<UnapprovedInterviewsFilter>(HttpContext.Session.GetString("UnapprovedInterviewsFilter"));
                    if (filter.StartDate != null && filter.EndDate != null)
                    {
                        modelList = (await _unapprovedInterviewerMeetingsService.GetAllUnapprovedInterviewerMeetings(filter.StartDate.Value, filter.EndDate.Value.AddDays(1))).ToList();
                    }
                    modelList = modelList.Select(x =>
                    {
                        x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription();
                        x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewerApproved)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty;
                        x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty;
                        x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription();
                        return x;
                    }).ToList();

                    filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = modelList.Select(x => x.PrimarySkill).ToArray() : filter.PrimarySkill = filter.PrimarySkill;
                    filter.SCID = filter.SCID == null ? filter.SCID = modelList.Select(x => x.ISCId).ToArray() : filter.SCID = filter.SCID;
                    filter.ConfirmationStatus = filter.ConfirmationStatus == null ? filter.ConfirmationStatus = modelList.Select(x => x.confirmationStatus).ToArray() : filter.ConfirmationStatus = filter.ConfirmationStatus;
                    filter.MeetingStatus = filter.MeetingStatus == null ? filter.MeetingStatus = modelList.Select(x => x.MeetingStatusStatusDesc).ToArray() : filter.MeetingStatus = filter.MeetingStatus;
                    filter.InterviewStatus = filter.InterviewStatus == null ? filter.InterviewStatus = modelList.Select(x => x.intervieweStatusStatDesc).ToArray() : filter.InterviewStatus = filter.InterviewStatus;
                    filter.Interviewer = filter.Interviewer == null ? filter.Interviewer = modelList.Select(x => x.interviewer).ToArray() : filter.Interviewer = filter.Interviewer;

                    modelList = modelList.Where(c => filter.PrimarySkill != null && filter.PrimarySkill.Contains(c.PrimarySkill)
                    && filter.SCID.Contains(c.ISCId)
                    && ((filter.ConfirmationStatus != null && filter.ConfirmationStatus.Count() > 0) ? filter.ConfirmationStatus.Contains(c.confirmationStatus) : c.confirmationStatus == null)
                    && ((filter.MeetingStatus != null && filter.MeetingStatus.Count() > 0) ? filter.MeetingStatus.Contains(c.MeetingStatusStatusDesc) : c.MeetingStatusStatusDesc == string.Empty)
                    && ((filter.InterviewStatus != null && filter.InterviewStatus.Count() > 0) ? filter.InterviewStatus.Contains(c.intervieweStatusStatDesc) : c.intervieweStatusStatDesc == string.Empty)
                    && ((filter.Interviewer != null && filter.Interviewer.Count() > 0) ? filter.Interviewer.Contains(c.interviewer) : c.interviewer == null)).ToList();

                }
                else
                {

                    modelList = (await _unapprovedInterviewerMeetingsService.GetAllUnapprovedInterviewerMeetings(DateTime.Now.Date, DateTime.Now.Date.AddDays(1))).ToList();
                    modelList = modelList.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewerApproved)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty; x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty; x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();
                }

                Sheet.Cells["A1:Z1"].Style.Font.Bold = true;
                Sheet.Cells["A1:Z1"].Style.Font.Size = 12;
                Sheet.Cells["A1:Z1"].Style.WrapText = true;
                //Sheet.Column(1).Width = 30;
                //Sheet.Column(2).Width = 30;
                int startColumn = 1; int endColumn = 40;
                for (int a = startColumn; a <= endColumn; a++)
                {
                    Sheet.Column(a).Width = 40;
                }
                Sheet.Cells[1, 1].Value = "ISC ID";
                Sheet.Cells[1, 2].Value = "Interview Date";
                Sheet.Cells[1, 3].Value = "Candidate Name";
                //Sheet.Cells[1, 4].Value = "Coded Name";
                Sheet.Cells[1, 4].Value = "Candidate Email";
                Sheet.Cells[1, 5].Value = "Primary Skill";
                Sheet.Cells[1, 6].Value = "Interviewer";
                Sheet.Cells[1, 7].Value = "Confirmation Status";
                Sheet.Cells[1, 8].Value = "Meeting Status";
                Sheet.Cells[1, 9].Value = "Interview Status";
                Sheet.Cells[1, 10].Value = "Encoded Resume";
                Sheet.Cells[1, 11].Value = "Zoom Link";
                Sheet.Cells[1, 12].Value = "Zoom Id";
                Sheet.Cells[1, 13].Value = "Video Link";
                Sheet.Cells[1, 14].Value = "Interview Slot";
                Sheet.Cells[1, 15].Value = "Co-ordinating HR";
                Sheet.Cells[1, 16].Value = "HR Comments";
                Sheet.Cells[1, 17].Value = "Interview Duration";


                //ExcelHyperLink excelHyperLink = new ExcelHyperLink();
                int i = 2;
                //string LastAction = null; ;

                if (modelList.Count() > 0)
                {
                    foreach (var item in modelList)
                    {
                        Sheet.Cells[i, 1].Value = item.ISCId;
                        Sheet.Cells[i, 2].Value = item.interviewDate.ToString("MM/dd/yyyy HH:mm");
                        Sheet.Cells[i, 3].Value = item.candidateName;
                        //Sheet.Cells[i, 4].Value = item.NickName;
                        Sheet.Cells[i, 4].Value = item.candidateEmail;
                        Sheet.Cells[i, 5].Value = item.PrimarySkill;
                        Sheet.Cells[i, 6].Value = item.interviewer != null ? item.interviewer : string.Empty;
                        Sheet.Cells[i, 7].Value = item.confirmationStatus;

                        if (item.interviewDate > DateTime.Now && item.confirmationStatus.Trim() != "Cancelled")
                        {
                            Sheet.Cells[i, 8].Value = "Scheduled";
                        }
                        else if (item.confirmationStatus.Trim() == ReceiveMeetingStatus.Cancelled.GetDescription())
                        {
                            Sheet.Cells[i, 8].Value = string.Empty;
                        }
                        else if (item.interviewDate < DateTime.Now && item.MeetingStatusStatusDesc == "Scheduled")
                        {
                            Sheet.Cells[i, 8].Value = "Status Pending";
                        }
                        else
                        {
                            Sheet.Cells[i, 8].Value = item.MeetingStatusStatusDesc;
                        }

                        Sheet.Cells[i, 9].Value = item.intervieweStatusStatDesc;



                        if (!string.IsNullOrEmpty(item.encodedresumeLink))
                        {
                            try
                            {
                                Sheet.Cells[i, 10].Hyperlink = new Uri(item.encodedresumeLink, UriKind.Absolute);
                                Sheet.Cells[i, 10].Value = "Encoded Resume";
                                Sheet.Cells[i, 10].Style.Font.UnderLine = true;
                                Sheet.Cells[i, 10].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                            }
                            catch (Exception)
                            {
                                Sheet.Cells[i, 10].Value = "No Resume";
                            }
                        }
                        else
                        {
                            Sheet.Cells[i, 10].Value = "No Resume";
                        }


                        if (!string.IsNullOrEmpty(item.zoomLink))
                        {
                            try
                            {
                                Sheet.Cells[i, 11].Hyperlink = new Uri(item.zoomLink, UriKind.Absolute);
                                Sheet.Cells[i, 11].Value = "Zoom link";
                                Sheet.Cells[i, 11].Style.Font.UnderLine = true;
                                Sheet.Cells[i, 11].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                            }
                            catch (Exception)
                            {
                                Sheet.Cells[i, 11].Value = "No link";
                            }
                        }
                        else
                        {
                            Sheet.Cells[i, 11].Value = "No link";
                        }
                        Sheet.Cells[i, 12].Value = item.ZoomId != null ? item.ZoomId : string.Empty;

                        if (!string.IsNullOrEmpty(item.VideoLink))
                        {
                            try
                            {
                                Sheet.Cells[i, 13].Hyperlink = new Uri(item.VideoLink, UriKind.Absolute);
                                Sheet.Cells[i, 13].Value = "Video link";
                                Sheet.Cells[i, 13].Style.Font.UnderLine = true;
                                Sheet.Cells[i, 13].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                            }
                            catch (Exception)
                            {
                                Sheet.Cells[i, 13].Value = "No Video";
                            }
                        }
                        else
                        {
                            Sheet.Cells[i, 13].Value = "No Video";
                        }

                        Sheet.Cells[i, 14].Value = item.interviewSlot != null ? item.interviewSlot : "No Interview Slot";
                        Sheet.Cells[i, 15].Value = item.HRCoordinatorEmailId != null ? item.HRCoordinatorEmailId : "NA";
                        Sheet.Cells[i, 16].Value = item.HRComment != null ? item.HRComment : "NA";
                        Sheet.Cells[i, 17].Value = item.interviewDuration.ToString();


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
        public async Task<IActionResult> Add()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement")
                    {
                        var model = new InterviewerMeetingVM();
                        var confirmationStatuses = from ReceiveMeetingStatus s in Enum.GetValues(typeof(ReceiveMeetingStatus))
                                                   select new { ID = (int)s, Name = s.GetDescription() };
                        model.ConfirmationStatusList = new SelectList(confirmationStatuses, "ID", "Name");
                        var interviewslotList = from InterviewSlots s in Enum.GetValues(typeof(InterviewSlots))
                                                select new { ID = (int)s, Name = s.GetDescription() };
                        model.InterviewSlotList = new SelectList(interviewslotList, "ID", "Name");
                        model.ISCList = _unapprovedInterviewerMeetingsService.GetISCList();

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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(InterviewerMeetingVM meeting)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement")
                    {
                        var confirmationStatuses = from ReceiveMeetingStatus s in Enum.GetValues(typeof(ReceiveMeetingStatus))
                                                   select new { ID = (int)s, Name = s.GetDescription() };
                        meeting.ConfirmationStatusList = new SelectList(confirmationStatuses, "ID", "Name");

                        var interviewslotList = from InterviewSlots s in Enum.GetValues(typeof(InterviewSlots))
                                                select new { ID = (int)s, Name = s.GetDescription() };
                        meeting.InterviewSlotList = new SelectList(interviewslotList, "ID", "Name");
                        meeting.ISCList = _unapprovedInterviewerMeetingsService.GetISCList();
                        if (ModelState.IsValid)
                        {
                            meeting.candidateEmail = meeting.candidateEmail.ToLower();
                            meeting.sCreatedBy = emailid;
                            if (_unapprovedInterviewerMeetingsService.AddorupdateMeeting(meeting))
                            {
                                if (meeting.IsEditable)
                                {
                                    TempData["SuccessErrorMessageUnapprovedInterviewer"] = "Meeting has been updated Successfully!";
                                }
                                else
                                {
                                    TempData["SuccessErrorMessageUnapprovedInterviewer"] = "Meeting has been added Successfully!";
                                }
                                return RedirectToAction("Index");
                            }
                            else
                            {
                                if (meeting.IsEditable)
                                {
                                    TempData["SuccessErrorMessageUnapprovedInterviewer"] = "Fail to update the meeting";
                                }
                                else
                                {
                                    TempData["SuccessErrorMessageUnapprovedInterviewer"] = "Meeting addition is Failed";
                                }
                                return RedirectToAction("Index");
                            }
                        }
                        else
                        {
                            ModelState.AddModelError("", "Please check all the fields for errors!");
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
                if (meeting.IsEditable)
                {
                    TempData["SuccessErrorMessageUnapprovedInterviewer"] = "Fail to update the meeting";
                }
                else
                {
                    TempData["SuccessErrorMessageUnapprovedInterviewer"] = "Meeting addition is Failed";
                }
                return RedirectToAction("Index");
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
                    if (profileName == "Admin" || profileName == "HR" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement")
                    {

                        InterviewerMeetingVM meetingToUpdate = _unapprovedInterviewerMeetingsService.GetMeeting(id);
                        if (meetingToUpdate == null)
                            return RedirectToAction("Index");
                        var confirmationStatuses = from ReceiveMeetingStatus s in Enum.GetValues(typeof(ReceiveMeetingStatus))
                                                   select new { ID = (int)s, Name = s.GetDescription() };
                        meetingToUpdate.ConfirmationStatusList = new SelectList(confirmationStatuses, "ID", "Name");
                        var interviewslotList = from InterviewSlots s in Enum.GetValues(typeof(InterviewSlots))
                                                select new { ID = (int)s, Name = s.GetDescription() };
                        meetingToUpdate.InterviewSlotList = new SelectList(interviewslotList, "ID", "Name");
                        meetingToUpdate.ISCList = _unapprovedInterviewerMeetingsService.GetISCList();
                        meetingToUpdate.IsEditable = true;
                        if (meetingToUpdate != null)
                        {
                            return View("Add", meetingToUpdate);
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
        public async Task<IActionResult> Index(UnapprovedInterviewsFilter filter)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Finance" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement")
                    {
                        if (TempData["SuccessErrorMessageUnapprovedInterviewer"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageUnapprovedInterviewer"].ToString();
                            TempData.Remove("SuccessErrorMessageUnapprovedInterviewer");
                        }
                        if (filter != null)
                        {
                            HttpContext.Session.SetString("UnapprovedInterviewsFilter", JsonConvert.SerializeObject(filter));
                        }
                        List<MeetingVM> modelList = new List<MeetingVM>();

                        if (filter.StartDate != null && filter.EndDate != null)
                        {
                            modelList = (await _unapprovedInterviewerMeetingsService.GetAllUnapprovedInterviewerMeetings(filter.StartDate.Value, filter.EndDate.Value.AddDays(1))).ToList();
                        }
                        else
                        {
                            modelList = (await _unapprovedInterviewerMeetingsService.GetAllUnapprovedInterviewerMeetings(DateTime.Now.Date, DateTime.Now.Date.AddDays(1))).ToList();
                        }

                        modelList = modelList.Select(x =>
                        {
                            x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription();
                            x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewerApproved)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty;
                            x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty;
                            x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription();
                            return x;
                        }).ToList();

                        filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = modelList.Select(x => x.PrimarySkill).ToArray() : filter.PrimarySkill = filter.PrimarySkill;
                        filter.SCID = filter.SCID == null ? filter.SCID = modelList.Select(x => x.ISCId).ToArray() : filter.SCID = filter.SCID;
                        filter.ConfirmationStatus = filter.ConfirmationStatus == null ? filter.ConfirmationStatus = modelList.Select(x => x.confirmationStatus).ToArray() : filter.ConfirmationStatus = filter.ConfirmationStatus;
                        filter.MeetingStatus = filter.MeetingStatus == null ? filter.MeetingStatus = modelList.Select(x => x.MeetingStatusStatusDesc).ToArray() : filter.MeetingStatus = filter.MeetingStatus;
                        filter.InterviewStatus = filter.InterviewStatus == null ? filter.InterviewStatus = modelList.Select(x => x.intervieweStatusStatDesc).ToArray() : filter.InterviewStatus = filter.InterviewStatus;
                        filter.Interviewer = filter.Interviewer == null ? filter.Interviewer = modelList.Select(x => x.interviewer).ToArray() : filter.Interviewer = filter.Interviewer;

                        modelList = modelList.Where(c => filter.PrimarySkill != null && filter.PrimarySkill.Contains(c.PrimarySkill)
                        && filter.SCID.Contains(c.ISCId)
                        && ((filter.ConfirmationStatus != null && filter.ConfirmationStatus.Count() > 0) ? filter.ConfirmationStatus.Contains(c.confirmationStatus) : c.confirmationStatus == null)
                        && ((filter.MeetingStatus != null && filter.MeetingStatus.Count() > 0) ? filter.MeetingStatus.Contains(c.MeetingStatusStatusDesc) : c.MeetingStatusStatusDesc == string.Empty)
                        && ((filter.InterviewStatus != null && filter.InterviewStatus.Count() > 0) ? filter.InterviewStatus.Contains(c.intervieweStatusStatDesc) : c.intervieweStatusStatDesc == string.Empty)
                        && ((filter.Interviewer != null && filter.Interviewer.Count() > 0) ? filter.Interviewer.Contains(c.interviewer) : c.interviewer == null)).ToList();

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
        public async Task<IActionResult> ResetUnapprovedInterviewsFilter()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("UnapprovedInterviewsFilter");
                    return RedirectToAction("Index");
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


    }
}
