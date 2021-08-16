using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
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
    public class InstaInterviewController : Controller
    {
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly INotificationHelper _notificationHelper;
        private readonly IConfiguration _configuration;
        private readonly IInstaInterviewService _instaInterviewService;
        private readonly IInterviewerService _interviewerService;
        private readonly IMeetingService _meetingService;
        public InstaInterviewController(IApplicationErrorLogger applicationErrorLogger, IAzureStorageHelper azureStorageHelper, INotificationHelper notificationHelper, IConfiguration configuration, IInstaInterviewService instaInterviewService, IInterviewerService interviewerService, IMeetingService meetingService)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _azureStorageHelper = azureStorageHelper;
            _notificationHelper = notificationHelper;
            _configuration = configuration;
            _instaInterviewService = instaInterviewService;
            _interviewerService = interviewerService;
            _meetingService = meetingService;
        }
        public async Task<IActionResult> Index()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                    {
                        ViewBag.profile = profileName;
                        if (TempData["SuccessErrorMessageInterview"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageInterview"].ToString();
                            TempData.Remove("SuccessErrorMessageInterview");
                        }
                        List<InstaMeetingVM> modelList = new List<InstaMeetingVM>();
                        if (HttpContext.Session.GetString("InstaInterviewfilters") != null)
                        {
                            var filter = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("InstaInterviewfilters"));
                            if (filter.StartDate != null && filter.EndDate != null)
                            {
                                modelList = _instaInterviewService.GetAllMeetingList(filter.StartDate.Value, filter.EndDate.Value.AddDays(1), emailId);
                            }
                            modelList = modelList.Select(x =>
                            {
                                x.intervieweStatusStatDesc = ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription();
                                x.MeetingStatusStatusDesc = ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription();
                                return x;
                            }).ToList();

                            filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = modelList.Select(x => x.RequiredSkill.Split("|")[0]).ToArray() : filter.PrimarySkill = filter.PrimarySkill;
                            //filter.ConfirmationStatus = filter.ConfirmationStatus == null ? filter.ConfirmationStatus = modelList.Select(x => x.ConfirmationStatus).ToArray() : filter.ConfirmationStatus = filter.ConfirmationStatus;
                            filter.MeetingStatus = filter.MeetingStatus == null ? filter.MeetingStatus = modelList.Select(x => x.MeetingStatusStatusDesc).ToArray() : filter.MeetingStatus = filter.MeetingStatus;
                            filter.InterviewStatus = filter.InterviewStatus == null ? filter.InterviewStatus = modelList.Select(x => x.intervieweStatusStatDesc).ToArray() : filter.InterviewStatus = filter.InterviewStatus;
                            filter.Interviewer = filter.Interviewer == null ? filter.Interviewer = modelList.Select(x => x.Interviewer).ToArray() : filter.Interviewer = filter.Interviewer;

                            modelList = modelList.Where(c => filter.PrimarySkill != null && filter.PrimarySkill.Contains(c.RequiredSkill.Split("|")[0])
                            //&& ((filter.ConfirmationStatus != null && filter.ConfirmationStatus.Count() > 0) ? filter.ConfirmationStatus.Contains(c.ConfirmationStatus) : c.ConfirmationStatus == null)
                            && ((filter.MeetingStatus != null && filter.MeetingStatus.Count() > 0) ? filter.MeetingStatus.Contains(c.MeetingStatusStatusDesc) : c.MeetingStatusStatusDesc == string.Empty)
                            && ((filter.InterviewStatus != null && filter.InterviewStatus.Count() > 0) ? filter.InterviewStatus.Contains(c.intervieweStatusStatDesc) : c.intervieweStatusStatDesc == string.Empty)
                            && ((filter.Interviewer != null && filter.Interviewer.Count() > 0) ? filter.Interviewer.Contains(c.Interviewer) : c.Interviewer == null)).ToList();

                        }
                        else
                        {

                            modelList = _instaInterviewService.GetAllMeetingList(DateTime.Now.Date, DateTime.Now.Date.AddDays(1), emailId);
                            modelList = modelList.Select(x => { x.ConfirmationStatus = ((InstaReceiveMeetingStatus)Convert.ToInt32(x.ConfirmationStatus)).GetDescription(); x.intervieweStatusStatDesc = ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription(); x.MeetingStatusStatusDesc = ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription(); return x; }).ToList();
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
        public async Task<IActionResult> Index(InterviewFilter filter)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                    {
                        ViewBag.profile = profileName;
                        if (filter != null)
                        {
                            HttpContext.Session.SetString("InstaInterviewfilters", JsonConvert.SerializeObject(filter));
                        }
                        List<InstaMeetingVM> modelList = new List<InstaMeetingVM>();
                        if (filter.StartDate != null && filter.EndDate != null)
                        {
                            modelList = _instaInterviewService.GetAllMeetingList(filter.StartDate.Value, filter.EndDate.Value.AddDays(1), emailId);
                        }
                        modelList = modelList.Select(x =>
                        {
                            x.intervieweStatusStatDesc = ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription();
                            x.MeetingStatusStatusDesc = ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription();
                            return x;
                        }).ToList();

                        filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = modelList.Select(x => x.RequiredSkill.Split("|")[0]).ToArray() : filter.PrimarySkill = filter.PrimarySkill;
                        filter.ConfirmationStatus = filter.ConfirmationStatus == null ? filter.ConfirmationStatus = modelList.Select(x => x.ConfirmationStatus).ToArray() : filter.ConfirmationStatus = filter.ConfirmationStatus;
                        filter.MeetingStatus = filter.MeetingStatus == null ? filter.MeetingStatus = modelList.Select(x => x.MeetingStatusStatusDesc).ToArray() : filter.MeetingStatus = filter.MeetingStatus;
                        filter.InterviewStatus = filter.InterviewStatus == null ? filter.InterviewStatus = modelList.Select(x => x.intervieweStatusStatDesc).ToArray() : filter.InterviewStatus = filter.InterviewStatus;
                        filter.Interviewer = filter.Interviewer == null ? filter.Interviewer = modelList.Select(x => x.Interviewer).ToArray() : filter.Interviewer = filter.Interviewer;

                        modelList = modelList.Where(c => filter.PrimarySkill != null && filter.PrimarySkill.Contains(c.RequiredSkill.Split("|")[0])
                        && ((filter.ConfirmationStatus != null && filter.ConfirmationStatus.Count() > 0) ? filter.ConfirmationStatus.Contains(c.ConfirmationStatus) : c.ConfirmationStatus == null)
                        && ((filter.MeetingStatus != null && filter.MeetingStatus.Count() > 0) ? filter.MeetingStatus.Contains(c.MeetingStatusStatusDesc) : c.MeetingStatusStatusDesc == string.Empty)
                        && ((filter.InterviewStatus != null && filter.InterviewStatus.Count() > 0) ? filter.InterviewStatus.Contains(c.intervieweStatusStatDesc) : c.intervieweStatusStatDesc == string.Empty)
                        && ((filter.Interviewer != null && filter.Interviewer.Count() > 0) ? filter.Interviewer.Contains(c.Interviewer) : c.Interviewer == null)).ToList();

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

        [HttpGet]
        public async Task<IActionResult> Add()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    ViewBag.Profile = profileName;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                    {
                        var model = new InstaMeetingVM();
                        var Assessmentgroups = _interviewerService.FetchAssessmentGroups();
                        model.AssessmentGroupList = new SelectList(Assessmentgroups, "id", "AssessmentGroup");
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(InstaMeetingVM meeting)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    ViewBag.Profile = profileName;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        if (profileName == "Client")
                        {
                            meeting.RequestedBy = emailid;
                        }
                        if (ModelState.IsValid)
                        {
                            string uploadedFileURL = string.Empty;
                            if (meeting.ResumeFile == null)
                            {
                                ModelState.AddModelError(String.Empty, "Please upload a Resume or add a Resume link.");
                                var Assessmentgroups = _interviewerService.FetchAssessmentGroups();
                                meeting.AssessmentGroupList = new SelectList(Assessmentgroups, "id", "AssessmentGroup");
                                return View("Add", meeting);
                            }
                            if (meeting.ResumeFile != null)
                            {
                                await _azureStorageHelper.InsertBlob(meeting.ResumeFile.FileName.Replace(" ", ""), "resumes", meeting.ResumeFile);
                                var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                                var blob = containerRef.GetBlockBlobReference(meeting.ResumeFile.FileName.Replace(" ", ""));
                                uploadedFileURL = blob.Uri.ToString();
                            }
                            if (meeting.JDFile != null)
                            {
                                await _azureStorageHelper.InsertBlob(meeting.JDFile.FileName.Replace(" ", ""), "resumes", meeting.JDFile);
                                var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                                var blob = containerRef.GetBlockBlobReference(meeting.JDFile.FileName.Replace(" ", ""));
                                meeting.JDLink = blob.Uri.ToString();
                            }
                            if (uploadedFileURL == null || uploadedFileURL == string.Empty)
                            {
                                ModelState.AddModelError(String.Empty, "Resume Upload has failed.");
                                var Assessmentgroups = _interviewerService.FetchAssessmentGroups();
                                meeting.AssessmentGroupList = new SelectList(Assessmentgroups, "id", "AssessmentGroup");
                                return View("Add", meeting);
                            }
                            else
                            {
                                meeting.ResumeLink = uploadedFileURL;
                                if (_instaInterviewService.AddInstaMeeting(meeting))
                                {
                                    TempData["SuccessErrorMessageInterview"] = "Meeting is added Successfully";
                                    return RedirectToAction("Index");
                                }
                                else
                                {
                                    TempData["SuccessErrorMessageInterview"] = "Meeting addition has Failed";
                                    return RedirectToAction("Index");
                                }
                            }
                        }
                        else
                        {
                            var Assessmentgroups = _interviewerService.FetchAssessmentGroups();
                            meeting.AssessmentGroupList = new SelectList(Assessmentgroups, "id", "AssessmentGroup");
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
                TempData["SuccessErrorMessageInterview"] = "Meeting addition has Failed!";
                return RedirectToAction("Index");
            }
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                    {
                        InstaMeetingVM meetingToUpdate = await _instaInterviewService.GetInstaMeeting(id);
                        TempData["ResumeLink"] = meetingToUpdate.ResumeLink;
                        var Assessmentgroups = _interviewerService.FetchAssessmentGroups();
                        meetingToUpdate.AssessmentGroupList = new SelectList(Assessmentgroups, "id", "AssessmentGroup");
                        return View(meetingToUpdate);
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(InstaMeetingVM meetingUpdated)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR")
                    {
                        if (ModelState.IsValid)
                        {
                            if ((meetingUpdated.ResumeFile != null))
                            {
                                await _azureStorageHelper.InsertBlob(meetingUpdated.ResumeFile.FileName.Replace(" ", ""), "resumes", meetingUpdated.ResumeFile);
                                var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                                var blob = containerRef.GetBlockBlobReference(meetingUpdated.ResumeFile.FileName.Replace(" ", ""));
                                string uploadedFileURL = blob.Uri.ToString();
                                if (uploadedFileURL == null)
                                {
                                    var model = new AddUpdateMeetingVM();
                                    ModelState.AddModelError(String.Empty, "Resume Upload has failed.");
                                    var Assessmentgroups = _interviewerService.FetchAssessmentGroups();
                                    meetingUpdated.AssessmentGroupList = new SelectList(Assessmentgroups, "id", "AssessmentGroup");
                                    return View(model);
                                }
                                meetingUpdated.ResumeLink = uploadedFileURL;
                            }
                            else if (meetingUpdated.ResumeLink == null || meetingUpdated.ResumeLink == "")
                            {
                                meetingUpdated.ResumeLink = TempData["ResumeLink"]?.ToString();
                            }
                            if (_instaInterviewService.UpdateInstaMeeting(meetingUpdated))
                            {
                                TempData["SuccessErrorMessageUpdatedInterview"] = "Meeting has been updated successfully";
                                return RedirectToAction("Edit", new { id = meetingUpdated.id });
                            }
                            else
                            {
                                TempData["SuccessErrorMessageUpdatedInterview"] = "Meeting update has Failed";
                                return RedirectToAction("Edit", new { id = meetingUpdated.id });
                            }
                        }
                        else
                        {
                            var Assessmentgroups = _interviewerService.FetchAssessmentGroups();
                            meetingUpdated.AssessmentGroupList = new SelectList(Assessmentgroups, "id", "AssessmentGroup");
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
                TempData["SuccessErrorMessageUpdatedInterview"] = "Meeting update has Failed";
                return RedirectToAction("Edit", new { id = meetingUpdated.id });
            }
        }
        //Fetch Skills On AssessmentGroupId
        public JsonResult FetchAssessmentGroupSkills(int id)
        {
            var Skills = _instaInterviewService.FetchSkillsByAssessmentGroup(id);
            if (Skills == null)
            {
                Skills = "";
            }
            return Json(Skills);
        }
        [HttpGet]
        public async Task<IActionResult> OpenInterview()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Interviewer" || profileName == "Quality Controller")
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        var meetings = _instaInterviewService.GetAllInstaOpenInterviews(emailid);
                        meetings = meetings.Select(x => { x.ConfirmationStatus = ((InstaReceiveMeetingStatus)Convert.ToInt32(x.ConfirmationStatus)).GetDescription(); return x; }).ToList();
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
                return View();
            }
        }
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
                    if (profileName == "Interviewer" || profileName == "Quality Controller")
                    {
                        var result = _instaInterviewService.InstaAssignYourself(id, emailid.ToLower(), Convert.ToDateTime(InterviewDate));
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
        [HttpGet]
        public async Task<IActionResult> MyInterview()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Interviewer" || profileName == "Quality Controller")
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        var myInterview = _instaInterviewService.GetAllInstaAssignedInterviews(emailid);
                        var codedname = _meetingService.FetchInterviewerCodedName(emailid);
                        ViewBag.CodedName = codedname;
                        myInterview = myInterview.Select(x => { x.ConfirmationStatus = ((InstaReceiveMeetingStatus)Convert.ToInt32(x.ConfirmationStatus)).GetDescription(); return x; }).ToList();
                        foreach (var item in myInterview)
                        {
                            if (item.InterviewStatus > 0)
                            {
                                item.intervieweStatusStatDesc = ((InterviewStatus)(item.InterviewStatus)).GetDescription();
                            }
                            if (item.MeetingStatus > 0)
                            {
                                item.MeetingStatusStatusDesc = ((MeetingStatus)(item.MeetingStatus)).GetDescription();
                            }
                        }
                        if (HttpContext.Session.GetString("MyInstaInterviewfilters") != null)
                        {
                            var filter = JsonConvert.DeserializeObject<MyInterviewFilter>(HttpContext.Session.GetString("MyInstaInterviewfilters"));

                            if (filter.StartDate != null && filter.EndDate != null)
                            {
                                myInterview = myInterview.Where(t => t.RequestedDate.Date >= filter.StartDate && t.RequestedDate.Date <= filter.EndDate).ToList();
                            }
                            else if (filter.StartDate != null && filter.EndDate == null)
                            {
                                myInterview = myInterview.Where(t => t.RequestedDate.Date >= filter.StartDate /*&& t.interviewDate.Date <= filter.EndDate*/).ToList();
                            }
                            else if (filter.StartDate == null && filter.EndDate != null)
                            {
                                myInterview = myInterview.Where(t => /*t.interviewDate.Date >= filter.StartDate &&*/ t.RequestedDate.Date <= filter.EndDate).ToList();
                            }
                            //filter.InterviewDuration = filter.InterviewDuration == null ? filter.InterviewDuration = myInterview.Select(x => x.interviewDuration).ToArray() : filter.InterviewDuration = filter.InterviewDuration;
                            filter.InterviewDate = filter.InterviewDate == null ? filter.InterviewDate = myInterview.Select(x => Convert.ToString(x.RequestedDate)).ToArray() : filter.InterviewDate = filter.InterviewDate;
                            filter.CandidateName = filter.CandidateName == null ? filter.CandidateName = myInterview.Select(x => x.CandidateName).ToArray() : filter.CandidateName = filter.CandidateName;
                            //filter.InterviewDuration.Contains(c.interviewDuration)
                            myInterview = myInterview.Where(c => (filter.InterviewDate.Count() > 0 ? filter.InterviewDate.Contains(Convert.ToString(c.RequestedDate)) : c.RequestedDate == null)
                           && (filter.CandidateName.Count() > 0 ? filter.CandidateName.Contains(c.CandidateName) : c.CandidateName == null)).ToList();
                        }
                        if (HttpContext.Session.GetString("MyInterviewfilters") == null)
                        {
                            myInterview = myInterview.Where(x => x.RequestedDate.Date >= DateTime.Now.Date || (x.MeetingStatus == 0 && x.RequestedDate.Date < DateTime.Now.Date)).ToList();
                        }
                        if (TempData["SuccessErrorMessage"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessage"].ToString();
                            TempData.Remove("SuccessErrorMessage");
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
        [HttpPost]
        public async Task<IActionResult> MyInterview(MyInterviewFilter filter)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (filter != null)
                    {
                        HttpContext.Session.SetString("MyInstaInterviewfilters", JsonConvert.SerializeObject(filter));
                    }
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Interviewer" || profileName == "Quality Controller")
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        var myInterview = _instaInterviewService.GetAllInstaAssignedInterviews(emailid);
                        var codedname = _meetingService.FetchInterviewerCodedName(emailid);
                        ViewBag.CodedName = codedname;
                        myInterview = myInterview.Select(x => { x.ConfirmationStatus = ((InstaReceiveMeetingStatus)Convert.ToInt32(x.ConfirmationStatus)).GetDescription(); return x; }).ToList();
                        foreach (var item in myInterview)
                        {
                            if (item.InterviewStatus > 0)
                            {
                                item.intervieweStatusStatDesc = ((InterviewStatus)(item.InterviewStatus)).GetDescription();
                            }
                            if (item.MeetingStatus > 0)
                            {
                                item.MeetingStatusStatusDesc = ((MeetingStatus)(item.MeetingStatus)).GetDescription();
                            }
                        }
                        if (filter.StartDate != null && filter.EndDate != null)
                        {
                            myInterview = myInterview.Where(t => t.RequestedDate.Date >= filter.StartDate && t.RequestedDate.Date <= filter.EndDate).ToList();
                        }
                        else if (filter.StartDate != null && filter.EndDate == null)
                        {
                            myInterview = myInterview.Where(t => t.RequestedDate.Date >= filter.StartDate /*&& t.interviewDate.Date <= filter.EndDate*/).ToList();
                        }
                        else if (filter.StartDate == null && filter.EndDate != null)
                        {
                            myInterview = myInterview.Where(t => /*t.interviewDate.Date >= filter.StartDate &&*/ t.RequestedDate.Date <= filter.EndDate).ToList();
                        }

                        filter.InterviewDate = filter.InterviewDate == null ? filter.InterviewDate = myInterview.Select(x => Convert.ToString(x.RequestedDate)).ToArray() : filter.InterviewDate = filter.InterviewDate;
                        filter.CandidateName = filter.CandidateName == null ? filter.CandidateName = myInterview.Select(x => x.CandidateName).ToArray() : filter.CandidateName = filter.CandidateName;

                        myInterview = myInterview.Where(c => (filter.InterviewDate.Count() > 0 ? filter.InterviewDate.Contains(Convert.ToString(c.RequestedDate)) : c.RequestedDate == null)
                       && (filter.CandidateName.Count() > 0 ? filter.CandidateName.Contains(c.CandidateName) : c.CandidateName == null)).ToList();
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
        public IActionResult ResetMyInterviewFilter()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("MyInstaInterviewfilters");
                    return RedirectToAction("MyInterview");
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("MyInterview");
            }
        }
    }
}
