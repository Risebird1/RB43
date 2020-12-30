using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OfficeOpenXml;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using X.PagedList;

namespace Risebird.Web.Controllers
{
    public class CandidateController : Controller
    {

        private readonly ICandidateService _candidateService;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private static List<CandidateTimelineVM> _modelDetails;
        private readonly IIRFService _irfService;
        public CandidateController(ICandidateService candidateService, IApplicationErrorLogger applicationErrorLogger, IIRFService irfService)
        {
            _candidateService = candidateService;
            _applicationErrorLogger = applicationErrorLogger;
            _irfService = irfService;
        }
        public IActionResult Index()
        {
            return View();
        }
        public async Task<IActionResult> CandidateTimeline(int? Page, string searchValue)
        {
            var profileName = HttpContext.Session.GetString("profileName")?.Trim();
            profileName = profileName.Replace("\"", "");
            var emailid = HttpContext.Session.GetString("email")?.Trim();
            emailid = emailid.Replace("\"", "");
            if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
            {
                var model = new List<CandidateTimelineVM>();
                var candidateData = await _candidateService.GetCandidateTimeline(profileName == "Client" ? emailid : null);
                var distinctCandidates = candidateData.Select(x => (x.IRFId, x.CandidateId, x.Name)).Distinct().OrderBy(x => x.Name);
                foreach (var candidate in distinctCandidates)
                {
                    var data = candidateData.Where(x => (x.CandidateId == candidate.CandidateId && x.IRFId == candidate.IRFId));
                    var firstData = data.FirstOrDefault();
                    var candidateTimeline = new CandidateTimelineVM();

                    candidateTimeline.HREmail = firstData.HREmail;
                    candidateTimeline.CandidateEmailId = firstData.EmailId;
                    candidateTimeline.CandidateName = firstData.Name;
                    candidateTimeline.IRFId = firstData.IRFId;
                    candidateTimeline.InterviewId = firstData.InterviewId;
                    candidateTimeline.CurrentStatus = firstData.CurrentStatus != null ? firstData.CurrentStatus : string.Empty;
                    candidateTimeline.CandidateMasterId = firstData.CandidateMasterId;
                    candidateTimeline.CurrentInterviewStatus = firstData.CurrentInterviewStatus;
                    candidateTimeline.QCStatus = firstData.QCStatus;
                    candidateTimeline.CurrentMeetingStatus = firstData.CurrentMeetingStatus;
                    candidateTimeline.confirmationStatus = firstData.confirmationStatus;
                    candidateTimeline.PrimarySkill = firstData.PrimarySkill;
                    candidateTimeline.ScheduleStatus = new List<TimelineStatus>();

                    //schedule status
                    var scheduleStatusData = data.Select(x => (x.ScheduleStatusDate, x.ScheduleStatusValue, x.ScheduleStatusComments)).Distinct();
                    if (scheduleStatusData.FirstOrDefault().ScheduleStatusDate != DateTime.MinValue)
                    {
                        scheduleStatusData = scheduleStatusData.OrderBy(x => x.ScheduleStatusDate);
                        var scheduleStatusObjList = new List<TimelineStatus>();
                        foreach (var item in scheduleStatusData.Where(x => x.ScheduleStatusValue != "Meeting Status" && x.ScheduleStatusValue != "Interview Status"))
                        {
                            var scheduleStatusObj = new TimelineStatus();
                            scheduleStatusObj.Date = item.ScheduleStatusDate.ToString("dd");
                            scheduleStatusObj.Day = item.ScheduleStatusDate.Date.ToString("dddd");
                            scheduleStatusObj.Time = item.ScheduleStatusDate.ToString("hh:mm tt");
                            scheduleStatusObj.Status = item.ScheduleStatusValue;

                            scheduleStatusObj.StatusDate = item.ScheduleStatusDate.Date.ToString("MMMM yyyy");
                            scheduleStatusObjList.Add(scheduleStatusObj);
                        }
                        candidateTimeline.ScheduleStatus = scheduleStatusObjList;
                        var lastStatus = scheduleStatusData.LastOrDefault();
                        if (lastStatus.ScheduleStatusValue == "Meeting Status" || lastStatus.ScheduleStatusValue == "Interview Status")
                        {
                            candidateTimeline.CurrentStatus = lastStatus.ScheduleStatusComments;
                        }
                        else
                        {
                            candidateTimeline.CurrentStatus = lastStatus.ScheduleStatusValue;

                        }

                    }

                    //meeting status 
                    var meetingStatusData = data.Select(x => (x.MeetingStatusDate, x.MeetingStatus)).Distinct();

                    if (meetingStatusData.FirstOrDefault().MeetingStatusDate != DateTime.MinValue)
                    {
                        meetingStatusData = meetingStatusData.OrderBy(x => x.MeetingStatusDate);
                        var meetingStatusObjList = new List<TimelineStatus>();

                        foreach (var item in meetingStatusData)
                        {
                            var meetingStatusObj = new TimelineStatus();
                            meetingStatusObj.Date = item.MeetingStatusDate.ToString("dd");
                            meetingStatusObj.Day = item.MeetingStatusDate.Date.ToString("dddd");
                            meetingStatusObj.Status = item.MeetingStatus;
                            meetingStatusObj.StatusDate = item.MeetingStatusDate.Date.ToString("MMMM yyyy");
                            meetingStatusObj.Time = item.MeetingStatusDate.ToString("hh:mm tt");

                            meetingStatusObjList.Add(meetingStatusObj);
                        }
                        candidateTimeline.MeetingStatus = meetingStatusObjList;
                    }
                    //interview status
                    var interviewStatusData = data.Select(x => (x.InterviewStatusDate, x.InterviewStatus)).Distinct();
                    if (interviewStatusData.FirstOrDefault().InterviewStatusDate != DateTime.MinValue)
                    {
                        interviewStatusData = interviewStatusData.OrderBy(x => x.InterviewStatusDate);
                        var interviewStatusObjList = new List<TimelineStatus>();

                        foreach (var item in interviewStatusData)
                        {
                            var interviewStatusObj = new TimelineStatus();

                            interviewStatusObj.Date = item.InterviewStatusDate.ToString("dd");
                            interviewStatusObj.Day = item.InterviewStatusDate.Date.ToString("dddd");
                            interviewStatusObj.Status = item.InterviewStatus;
                            interviewStatusObj.StatusDate = item.InterviewStatusDate.Date.ToString("MMMM yyyy");
                            interviewStatusObj.Time = item.InterviewStatusDate.ToString("hh:mm tt");

                            interviewStatusObjList.Add(interviewStatusObj);
                        }
                        candidateTimeline.InterviewStatus = interviewStatusObjList;
                    }
                    //add candidate to final model
                    if (candidateTimeline.InterviewStatus != null || candidateTimeline.MeetingStatus != null || (candidateTimeline.ScheduleStatus != null && candidateTimeline.ScheduleStatus.Count() != 0) || !(string.IsNullOrEmpty(candidateTimeline.CurrentStatus)))
                    {
                        model.Add(candidateTimeline);

                    }
                }


                if (!String.IsNullOrEmpty(searchValue))
                {
                    searchValue = searchValue.ToUpper();
                    model = model.Where(x => (!String.IsNullOrEmpty(x.CandidateName) ? x.CandidateName.ToUpper().Contains(searchValue) : x.CandidateName == "") || (!String.IsNullOrEmpty(x.CandidateEmailId) ? x.CandidateEmailId.ToUpper().Contains(searchValue) : x.CandidateEmailId == "")
                    || (!String.IsNullOrEmpty(x.HREmail) ? x.HREmail.ToUpper().Contains(searchValue) : x.HREmail == "") || (!String.IsNullOrEmpty(x.CurrentStatus) ? x.CurrentStatus.ToUpper().Contains(searchValue) : x.CurrentStatus == "")).ToList();


                }


                ViewBag.TotalCount = model.Count;

                ViewBag.PageStartCount = 10 * ((Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page)) - 1) + 1;
                int a = model.Count / 10;
                if (model.Count % 10 == 0)
                {
                    ViewBag.PageEndCount = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
                }
                else
                {
                    a = a + 1;
                    if (a == Page)
                    {
                        ViewBag.PageEndCount = model.Count;
                    }
                    else
                    {
                        ViewBag.PageEndCount = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));

                    }

                }
                if (model.Count == 0)
                {
                    ViewBag.PageStartCount = 0;
                    ViewBag.PageEndCount = 0;
                }
                _modelDetails = model;
                return View(model.ToPagedList(Page ?? 1, 10));
      
        }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }
        [HttpPost]
        public JsonResult CandidateTimelineJson(string searchValue)
        {
            int Page = 1;
            var model = _modelDetails;
            if (!String.IsNullOrEmpty(searchValue))
            {
                searchValue = searchValue.ToUpper();
                model = model.Where(x => (!String.IsNullOrEmpty(x.CandidateName) ? x.CandidateName.ToUpper().Contains(searchValue) : x.CandidateName == "") || (!String.IsNullOrEmpty(x.CandidateEmailId) ? x.CandidateEmailId.ToUpper().Contains(searchValue) : x.CandidateEmailId == "")
                  || (!String.IsNullOrEmpty(x.HREmail) ? x.HREmail.ToUpper().Contains(searchValue) : x.HREmail == "")|| (!String.IsNullOrEmpty(x.CurrentStatus) ? x.CurrentStatus.ToUpper().Contains(searchValue) : x.CurrentStatus == "")).ToList();

            }
            int totalcount = model.Count;
            int Pagestart = 0;
            int Pageend = 0;
            ViewBag.TotalCount = model.Count;

            Pagestart = 10 * ((Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page)) - 1) + 1;
            int a = model.Count / 10;
            if (model.Count % 10 == 0)
            {
                Pageend = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
            }
            else
            {
                a = a + 1;
                if (a == Page)
                {
                    Pageend = model.Count;
                }
                else
                {
                    Pageend = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));


                }

            }
            if (model.Count == 0)
            {
                Pagestart = 0;
                Pageend = 0;
            }

            return Json(new { result = model.Take(10), startPage = Pagestart, endPage = Pageend, totalPage = a, totalentry = model.Count });
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetCandidates(string id, string irfName)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "Recruiter")
                    {
                        ViewBag.IRFName = irfName;
                        ViewBag.IRFId = id;
                        ViewBag.Profile = profileName;

                        var meetings = (await _irfService.GetIRFInterviewMeetings(Convert.ToInt16(id), profileName, emailid))?.ToList();
                        var candidateList = (await _irfService.GetIRFCandidates(Convert.ToInt32(id)))?.ToList();
                        if (profileName == "Recruiter")
                        {
                            var userId = _irfService.GetUserId(emailid, profileName);
                            candidateList = candidateList.Where(x => x.AddedByUserId == userId).ToList();
                        }

                        if (candidateList != null && candidateList.Count > 0)
                        {

                            candidateList.ForEach(x => x.InviteConfirmation = meetings.Where(y => Convert.ToInt32(y.confirmationStatus) == 1 && y.candidateEmail == x.EmailId).ToList().Count);
                            candidateList.Where(x => x.Status != null).ToList().ForEach(x => x.StatusValue = ((CandidateStatusOptions)x.Status).GetDescription());
                            candidateList.ForEach(x => { x.confirmationStatusDesc = ((ReceiveMeetingStatus)x.confirmationStatus).GetDescription(); x.MeetingStatusDesc = ((MeetingStatus)x.MeetingStatus).GetDescription(); x.InterviewStatusDesc = ((InterviewStatus)x.InterviewStatus).GetDescription(); x.HeuristicsscoreDesc = ((Heuristicsscorelevel)x.Heuristicsscore).GetDescription(); });
                        }

                        if (TempData["SuccessErrorMessageCandidate"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageCandidate"].ToString();
                            TempData.Remove("SuccessErrorMessageCandidate");
                        }
                        return View(candidateList);

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
                return RedirectToAction("Index", "IRF");
            }

        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Add(string irfId, string irfName)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "Recruiter")
                    {
                        var model = new Candidate
                        {
                            IRFId = Convert.ToInt32(irfId),
                            IRFName = irfName
                        };
                        var Heuristics = from Heuristicsscorelevel s in Enum.GetValues(typeof(Heuristicsscorelevel))
                                         select new { ID = (int)s, Name = s.GetDescription() };
                        model.Heuristicsscorelist = new SelectList(Heuristics, "ID", "Name");
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
                return RedirectToAction("GetCandidates", new { id = irfId, irfName = irfName });
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Add(Candidate model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "Recruiter")
                    {
                        var result = _irfService.AddCandidate(model, emailid, profileName).Result;
                        TempData["SuccessErrorMessageCandidate"] = result;
                        return RedirectToAction("GetCandidates", new { id = model.IRFId, irfName = model.IRFName });

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
                TempData["SuccessErrorMessage"] = "Adding candidate failed";
                return RedirectToAction("GetCandidates", new { id = model.IRFId, irfName = model.IRFName });
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Edit(int id, string irfName, string irfId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "Recruiter")
                    {
                        var model = await _irfService.GetCandidate(id, Convert.ToInt32(irfId));
                        model.IRFName = irfName;
                        var Heuristics = from Heuristicsscorelevel s in Enum.GetValues(typeof(Heuristicsscorelevel))
                                         select new { ID = (int)s, Name = s.GetDescription() };
                        model.Heuristicsscorelist = new SelectList(Heuristics, "ID", "Name");
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
                return RedirectToAction("GetCandidates", new { id = irfId, irfName = irfName });
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Edit(Candidate model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");

                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "Recruiter")
                    {
                        if (await _irfService.UpdateCandidate(model, emailid))
                        {
                            TempData["SuccessErrorMessageCandidate"] = "Candidate updated successfully!";
                            return RedirectToAction("GetCandidates", new { id = model.IRFId, irfName = model.IRFName });
                        }
                        else
                        {
                            TempData["SuccessErrorMessageCandidate"] = "Candidate update failed!";
                            return RedirectToAction("GetCandidates", new { id = model.IRFId, irfName = model.IRFName });
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
                TempData["SuccessErrorMessageCandidate"] = "Candidate update failed!";
                return RedirectToAction("GetCandidates", new { id = model.IRFId, irfName = model.IRFName });
            }
        }
        [Authorize]
        [HttpGet]
        public async Task<FileContentResult> ExportCandidateList(string irfId, string irfName)
        {
            ExcelPackage Ep = new ExcelPackage();
            ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Candidate Report");
            string fileName = "Risebird" + DateTime.Now.ToString("dd/MM/yyyy") + "_" + irfName + "_Candidate_Report" + ".xlsx";
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                    {
                        var meetings = (await _irfService.GetIRFInterviewMeetings(Convert.ToInt16(irfId), profileName, emailid))?.ToList();
                        var candidateList = (await _irfService.GetIRFCandidates(Convert.ToInt32(irfId)))?.ToList();
                        if (candidateList != null && candidateList.Count > 0)
                        {
                            candidateList.Where(x => x.Status != null).ToList().ForEach(x => x.StatusValue = ((CandidateStatusOptions)x.Status).GetDescription());
                            candidateList.ForEach(x => x.InviteConfirmation = meetings.Where(y => Convert.ToInt32(y.confirmationStatus) == 1 && y.candidateEmail == x.EmailId).ToList().Count);
                            candidateList.ForEach(x => { x.confirmationStatusDesc = ((ReceiveMeetingStatus)x.confirmationStatus).GetDescription(); x.MeetingStatusDesc = ((MeetingStatus)x.MeetingStatus).GetDescription(); x.InterviewStatusDesc = ((InterviewStatus)x.InterviewStatus).GetDescription(); });

                        }

                        string userEmailId = string.Empty;

                        Sheet.Cells["A1:Z1"].Style.Font.Bold = true;
                        Sheet.Cells["A1:Z1"].Style.Font.Size = 12;
                        Sheet.Cells["A1:Z1"].Style.WrapText = true;
                        //Sheet.Column(1).Width = 30;
                        //Sheet.Column(2).Width = 30;
                        int startColumn = 1; int endColumn = 25;
                        for (int a = startColumn; a <= endColumn; a++)
                        {
                            Sheet.Column(a).Width = 30;
                        }
                        Sheet.Cells[1, 1].Value = "Candidate Name";
                        Sheet.Cells[1, 2].Value = "Candidate Email";
                        Sheet.Cells[1, 3].Value = "Resume";
                        Sheet.Cells[1, 4].Value = "Type";
                        Sheet.Cells[1, 5].Value = "Location";
                        Sheet.Cells[1, 6].Value = "Skill";
                        Sheet.Cells[1, 7].Value = "Work Auth";
                        Sheet.Cells[1, 8].Value = "Current Location";
                        Sheet.Cells[1, 9].Value = "Current Employer";
                        Sheet.Cells[1, 10].Value = "Phone";
                        Sheet.Cells[1, 11].Value = "Total Experience";
                        Sheet.Cells[1, 12].Value = "Current Salary";
                        Sheet.Cells[1, 13].Value = "Expected Salary";
                        Sheet.Cells[1, 14].Value = "Notice";
                        Sheet.Cells[1, 15].Value = "Invites Sent";
                        Sheet.Cells[1, 16].Value = "Invite Confirmation";
                        Sheet.Cells[1, 17].Value = "Schedule Status";
                        Sheet.Cells[1, 18].Value = "Confirmation Status";
                        Sheet.Cells[1, 19].Value = "Meeting Status";
                        Sheet.Cells[1, 20].Value = "Interview Status";
                        Sheet.Cells[1, 21].Value = "Interview Date";
                        Sheet.Cells[1, 22].Value = "Video Link";

                        int i = 2;
                        //string LastAction = null; ;

                        if (candidateList.Count() > 0)
                        {
                            foreach (var item in candidateList)
                            {
                                Sheet.Cells[i, 1].Value = item.Name;
                                Sheet.Cells[i, 2].Value = item.EmailId;
                                if (!string.IsNullOrEmpty(item.ResumeLink))
                                {
                                    try
                                    {
                                        Sheet.Cells[i, 3].Hyperlink = new Uri(item.ResumeLink, UriKind.Absolute);
                                        Sheet.Cells[i, 3].Value = "Resume Link";
                                        Sheet.Cells[i, 3].Style.Font.UnderLine = true;
                                        Sheet.Cells[i, 3].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                                    }
                                    catch (Exception)
                                    {
                                        Sheet.Cells[i, 3].Value = "No Resume";
                                    }
                                }
                                else
                                {
                                    Sheet.Cells[i, 3].Value = "No Resume";
                                }
                                Sheet.Cells[i, 4].Value = item.Type ?? "NA";
                                Sheet.Cells[i, 5].Value = item.Location ?? "NA";

                                Sheet.Cells[i, 6].Value = item.Skill ?? "NA";

                                Sheet.Cells[i, 7].Value = item.WorkAuth ?? "NA";

                                Sheet.Cells[i, 8].Value = item.CurrentLocation ?? "NA";

                                Sheet.Cells[i, 9].Value = item.CurrentEmployer ?? "NA";

                                Sheet.Cells[i, 10].Value = item.Phone ?? "NA";

                                Sheet.Cells[i, 11].Value = Convert.ToString(item.TotalExperience);//item.TotalExperience != 0 ? Convert.ToString(item.TotalExperience) : "NA";
                                Sheet.Cells[i, 12].Value = Convert.ToString(item.CurrentSalary);//item.CurrentSalary != 0 ? Convert.ToString(item.CurrentSalary) : "NA";

                                Sheet.Cells[i, 13].Value = Convert.ToString(item.ExpectedSalary);// item.ExpectedSalary != 0 ? Convert.ToString(item.ExpectedSalary) : "NA";
                                Sheet.Cells[i, 14].Value = Convert.ToString(item.Notice);// item.Notice != 0 ? Convert.ToString(item.Notice) : "NA";

                                Sheet.Cells[i, 15].Value = item.TotalInvites;
                                Sheet.Cells[i, 16].Value = item.InviteConfirmation;
                                Sheet.Cells[i, 17].Value = item.InviteConfirmation > 0 ? "Scheduled" : item.StatusValue;
                                Sheet.Cells[i, 18].Value = item.confirmationStatusDesc != "0" ? item.confirmationStatusDesc : "NA";
                                Sheet.Cells[i, 19].Value = item.MeetingStatusDesc != "0" ? item.MeetingStatusDesc : "NA";
                                Sheet.Cells[i, 20].Value = item.InterviewStatusDesc != "0" ? item.InterviewStatusDesc : "NA";
                                Sheet.Cells[i, 21].Value = item.interviewDate != null ? item.interviewDate?.ToString("dd MMMM yyyy") : "NA";
                                if (!string.IsNullOrEmpty(item.VideoLink))
                                {
                                    try
                                    {
                                        Sheet.Cells[i, 22].Hyperlink = new Uri(item.VideoLink, UriKind.Absolute);
                                        Sheet.Cells[i, 22].Value = "Video Link";
                                        Sheet.Cells[i, 22].Style.Font.UnderLine = true;
                                        Sheet.Cells[i, 22].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                                    }
                                    catch (Exception)
                                    {
                                        Sheet.Cells[i, 22].Value = "No Video";
                                    }
                                }
                                else
                                {
                                    Sheet.Cells[i, 22].Value = "No Video";
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
                    else
                    {
                        ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                        Rng.Value = "Data export failed !!!";
                        return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                    }
                }
                else
                {
                    ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                    Rng.Value = "Data export failed !!!";
                    return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                Rng.Value = "Data export failed !!!";
                return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }

        }

        [Authorize]
        public async Task<IActionResult> Delete(int id, int irfId, string IRFName)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "Recruiter")
                    {
                        if (await _irfService.DeleteCandidate(id))
                        {
                            TempData["SuccessErrorMessageCandidate"] = "Candidate deleted Successfully";
                            return RedirectToAction("GetCandidates", new { id = irfId, irfName = IRFName });
                        }
                        else
                        {
                            TempData["SuccessErrorMessageCandidate"] = "Candidate delete Failed";
                            return RedirectToAction("GetCandidates", new { id = irfId, irfName = IRFName });
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
                TempData["SuccessErrorMessageCandidate"] = "Candidate delete Failed";
                return RedirectToAction("GetCandidates", new { id = irfId, irfName = IRFName });
            }

        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> SendInvite(int irfId, string IRFName, string emailId, DateTime endDate)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var loggedInUserEmail = HttpContext.Session.GetString("email")?.Trim();
                    loggedInUserEmail = loggedInUserEmail.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                    {
                        if (endDate < DateTime.Now)
                        {
                            TempData["SuccessErrorMessageCandidate"] = "Sorry! Could not send invite! The requested Skill Channel positions have been filled.";
                            return RedirectToAction("GetCandidates", new { id = irfId, irfName = IRFName });
                        }
                        if (await _irfService.SendInvite(irfId, emailId, loggedInUserEmail))
                        {
                            TempData["SuccessErrorMessageCandidate"] = "Invitation sent successfully!";
                            return RedirectToAction("GetCandidates", new { id = irfId, irfName = IRFName });
                        }
                        else
                        {
                            TempData["SuccessErrorMessageCandidate"] = "Sorry! Could not send invite!";
                            return RedirectToAction("GetCandidates", new { id = irfId, irfName = IRFName });
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
                TempData["SuccessErrorMessageCandidate"] = "Sorry! Could not send invite!";
                return RedirectToAction("GetCandidates", new { id = irfId, irfName = IRFName });
            }

        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> CancelInvite(int irfId, string IRFName, string emailId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                    {

                        if (await _irfService.CancelInvite(irfId, emailId))
                        {
                            TempData["SuccessErrorMessageCandidate"] = "Invite cancelled successfully!";
                            return RedirectToAction("GetCandidates", new { id = irfId, irfName = IRFName });
                        }
                        else
                        {
                            TempData["SuccessErrorMessageCandidate"] = "Sorry! Could not cancel invite!";
                            return RedirectToAction("GetCandidates", new { id = irfId, irfName = IRFName });
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
                TempData["SuccessErrorMessageCandidate"] = "Sorry! Could not cancel invite!";
                return RedirectToAction("GetCandidates", new { id = irfId, irfName = IRFName });
            }

        }
    }
}
