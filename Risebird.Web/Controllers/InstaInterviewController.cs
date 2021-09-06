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
using Risebird.Repository.Interface;
using Risebird.Web.Controllers.AzureMediaservice;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using RestSharp;

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
        private readonly IIRFRepository _irfRepository;
        public InstaInterviewController(IApplicationErrorLogger applicationErrorLogger, IAzureStorageHelper azureStorageHelper, INotificationHelper notificationHelper, IConfiguration configuration, IInstaInterviewService instaInterviewService, IInterviewerService interviewerService, IMeetingService meetingService, IIRFRepository irfRepository)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _azureStorageHelper = azureStorageHelper;
            _notificationHelper = notificationHelper;
            _configuration = configuration;
            _instaInterviewService = instaInterviewService;
            _interviewerService = interviewerService;
            _meetingService = meetingService;
            _irfRepository = irfRepository;
        }
        public async Task<IActionResult> InstaIRF()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SeniorHR")
                    {
                        var model = new List<InstaMeetingVM>();
                        var irfList = _instaInterviewService.GetAllInstaRequests(emailid, profileName);
                        model = (List<InstaMeetingVM>)irfList;
                        if (TempData["SuccessErrorMessageInterview"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageInterview"].ToString();
                            TempData.Remove("SuccessErrorMessageInterview");
                        }
                        if (HttpContext.Session.GetString("InstaRequestfilters") != null)
                        {
                            var filter = JsonConvert.DeserializeObject<IRFFilter>(HttpContext.Session.GetString("InstaRequestfilters"));
                            filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = model.Select(x => x.RequiredSkill).ToArray() : filter.PrimarySkill = filter.PrimarySkill;
                            model = model.Where(c => filter.PrimarySkill.Contains(c.RequiredSkill.Split("|")[0])).ToList();
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
        public async Task<IActionResult> CreateRequest()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    ViewBag.Profile = profileName;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SeniorHR")
                    {
                        var model = new InstaMeetingVM();
                        var Assessmentgroups = _interviewerService.FetchAssessmentGroups();
                        if (profileName == "SeniorHR")
                            Assessmentgroups = Assessmentgroups.Where(x => x.Isinstaavailable == true).ToList();
                        model.AssessmentGroupList = new SelectList(Assessmentgroups, "id", "AssessmentGroup");
                        var skills = _irfRepository.GetSkills();
                        ViewBag.LevelTech = new SelectList(_irfRepository.GetLevelTech(), "id", "Name");
                        var Skillset = skills;
                        if (Skillset != null)
                        {
                            Skillset = Skillset.OrderBy(x => x.Name.Trim()).ToList();
                        }
                        ViewBag.Skill = new SelectList(Skillset, "id", "Name");
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRequest(InstaMeetingVM meeting)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    ViewBag.Profile = profileName;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SeniorHR")
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        if (profileName == "Client")
                        {
                            meeting.RequestedBy = emailid;
                        }
                        if (ModelState.IsValid)
                        {
                            if (meeting.JDFile != null)
                            {
                                await _azureStorageHelper.InsertBlob(meeting.JDFile.FileName.Replace(" ", ""), "resumes", meeting.JDFile);
                                var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                                var blob = containerRef.GetBlockBlobReference(meeting.JDFile.FileName.Replace(" ", ""));
                                meeting.JDLink = blob.Uri.ToString();
                            }
                            if (_instaInterviewService.CreateRequest(meeting))
                            {
                                TempData["SuccessErrorMessageInterview"] = "Request Created Successfully.";
                                return RedirectToAction("InstaIRF");
                            }
                            else
                            {
                                TempData["SuccessErrorMessageInterview"] = "Failed to Create request.";
                                return RedirectToAction("InstaIRF");
                            }

                        }
                        else
                        {
                            var Assessmentgroups = _interviewerService.FetchAssessmentGroups();
                            if (profileName == "SeniorHR")
                                Assessmentgroups = Assessmentgroups.Where(x => x.Isinstaavailable == true).ToList();
                            meeting.AssessmentGroupList = new SelectList(Assessmentgroups, "id", "AssessmentGroup");
                            return View("CreateRequest", meeting);
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
                TempData["SuccessErrorMessageInterview"] = "Failed to Create request!";
                return RedirectToAction("Index");
            }
        }
        public async Task<IActionResult> EditRequest(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    ViewBag.Profile = profileName;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SeniorHR")
                    {
                        var model = new InstaMeetingVM();
                        model = _instaInterviewService.FetchRequestById(id);
                        var Assessmentgroups = _interviewerService.FetchAssessmentGroups();
                        if (profileName == "SeniorHR")
                            Assessmentgroups = Assessmentgroups.Where(x => x.Isinstaavailable == true).ToList();
                        model.AssessmentGroupList = new SelectList(Assessmentgroups, "id", "AssessmentGroup");
                        var skills = _irfRepository.GetSkills();
                        ViewBag.LevelTech = new SelectList(_irfRepository.GetLevelTech(), "id", "Name");
                        var Skillset = skills;
                        if (Skillset != null)
                        {
                            Skillset = Skillset.OrderBy(x => x.Name.Trim()).ToList();
                        }
                        ViewBag.Skill = new SelectList(Skillset, "id", "Name");
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRequest(InstaMeetingVM meeting)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    ViewBag.Profile = profileName;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SeniorHR")
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        if (profileName == "Client")
                        {
                            meeting.RequestedBy = emailid;
                        }
                        if (ModelState.IsValid)
                        {
                            if (meeting.JDFile != null)
                            {
                                await _azureStorageHelper.InsertBlob(meeting.JDFile.FileName.Replace(" ", ""), "resumes", meeting.JDFile);
                                var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                                var blob = containerRef.GetBlockBlobReference(meeting.JDFile.FileName.Replace(" ", ""));
                                meeting.JDLink = blob.Uri.ToString();
                            }
                            if (_instaInterviewService.CreateRequest(meeting))
                            {
                                TempData["SuccessErrorMessageInterview"] = "Request updated Successfully.";
                                return RedirectToAction("InstaIRF");
                            }
                            else
                            {
                                TempData["SuccessErrorMessageInterview"] = "Failed to update the request.";
                                return RedirectToAction("InstaIRF");
                            }

                        }
                        else
                        {
                            var Assessmentgroups = _interviewerService.FetchAssessmentGroups();
                            if (profileName == "SeniorHR")
                                Assessmentgroups = Assessmentgroups.Where(x => x.Isinstaavailable == true).ToList();
                            meeting.AssessmentGroupList = new SelectList(Assessmentgroups, "id", "AssessmentGroup");
                            return View("EditRequest", meeting);
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
                TempData["SuccessErrorMessageInterview"] = "Failed to update the request!";
                return RedirectToAction("InstaIRF");
            }
        }
        public async Task<IActionResult> Index(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SeniorHR")
                    {
                        ViewBag.profile = profileName;
                        if (TempData["SuccessErrorMessageInterview"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageInterview"].ToString();
                            TempData.Remove("SuccessErrorMessageInterview");
                        }
                        if (TempData["SuccessErrorMessage"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessage"].ToString();
                            TempData.Remove("SuccessErrorMessage");
                        }
                        List<InstaMeetingVM> modelList = new List<InstaMeetingVM>();
                        if (HttpContext.Session.GetString("InstaInterviewfilters") != null)
                        {
                            var filter = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("InstaInterviewfilters"));
                            if (filter.StartDate != null && filter.EndDate != null)
                            {
                                modelList = _instaInterviewService.GetAllMeetingList(filter.StartDate.Value, filter.EndDate.Value.AddDays(1), id, emailId);
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
                            modelList = _instaInterviewService.GetAllMeetingList(DateTime.Now.Date, DateTime.Now.Date.AddDays(1), id, emailId);
                            modelList = modelList.Select(x => { x.ConfirmationStatus = ((InstaReceiveMeetingStatus)Convert.ToInt32(x.ConfirmationStatus)).GetDescription(); x.intervieweStatusStatDesc = ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription(); x.MeetingStatusStatusDesc = ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription(); return x; }).ToList();
                        }
                        ViewBag.Requestid = id;
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
        public async Task<IActionResult> Index(int id, InterviewFilter filter)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SeniorHR")
                    {
                        ViewBag.profile = profileName;
                        if (filter != null)
                        {
                            HttpContext.Session.SetString("InstaInterviewfilters", JsonConvert.SerializeObject(filter));
                        }
                        List<InstaMeetingVM> modelList = new List<InstaMeetingVM>();
                        if (filter.StartDate != null && filter.EndDate != null)
                        {
                            modelList = _instaInterviewService.GetAllMeetingList(filter.StartDate.Value, filter.EndDate.Value.AddDays(1), id, emailId);
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
                        ViewBag.Requestid = id;
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
        public async Task<IActionResult> Add(int Requestedid)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    ViewBag.Profile = profileName;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SeniorHR")
                    {
                        var model = new InstaMeetingVM();
                        model.RequestId = Requestedid;
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
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SeniorHR")
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
                                    return RedirectToAction("Index", new { id = meeting.RequestId });
                                }
                                else
                                {
                                    TempData["SuccessErrorMessageInterview"] = "Meeting addition has Failed";
                                    return RedirectToAction("Index", new { id = meeting.RequestId });
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
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SeniorHR")
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
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR")
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
        public async Task<IActionResult> AssignYourself(int id, string InterviewDate, string CandidateName, string Role)
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
                        if (result.Contains("Successfully"))
                        {
                            InstaZoomMeetingVM _zoommetingobject = new InstaZoomMeetingVM();
                            _zoommetingobject.topic = CandidateName + "| " + Role + "| Video Interview";
                            TimeZoneInfo INDIAN_ZONE = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                            DateTime indianTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow.AddMinutes(15), INDIAN_ZONE);
                            _zoommetingobject.start_time = indianTime.ToString("yyyy-MM-ddThh:mm:ss");
                            var accountEmail = await _instaInterviewService.CheckInstaMeetingAccount(indianTime, indianTime.AddMinutes(60));
                            if (String.IsNullOrEmpty(accountEmail))
                                accountEmail = "me";
                            var zoomlink = CreateZoomMeetings(_zoommetingobject, accountEmail, indianTime);
                            var clientname = _instaInterviewService.UpdateInstaZoomLink(id, zoomlink.join_url, indianTime, indianTime.AddMinutes(60), accountEmail);
                            //Send Mail to Client And Interviewer
                            Mail _mail = new Mail
                            {
                                TO = emailid,
                                Subject = "Risebird: Insta interview has been scheduled for " + CandidateName
                            };
                            var emailsend = _notificationHelper.InstaInterviewwithzoomlink(_mail, CandidateName, zoomlink.join_url, zoomlink.InterviewDate);
                            Mail _mail1 = new Mail
                            {
                                TO = clientname,
                                Subject = "Risebird: Insta interview has been scheduled for " + CandidateName
                            };
                            var emailsend1 = _notificationHelper.InstaClientInterviewwithzoomlink(_mail1, CandidateName, zoomlink.join_url, zoomlink.InterviewDate);

                        }
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
        [HttpGet]
        public IActionResult AddReport(int interviewId, int Requestedid, bool isClient = false, string actiontype = null)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var email = HttpContext.Session.GetString("email");
                    email = email.Replace("\"", "");
                    bool isAdminHR = false;
                    bool isInterviewer = false;
                    ViewBag.actiontype = actiontype;
                    string profileName = HttpContext.Session.GetString("profileName");
                    ViewBag.Profile = profileName.Replace("\"", "");
                    if (profileName != null)
                    {
                        profileName = profileName.Replace("\"", "");
                        if (profileName == "HR" || profileName == "Client" || profileName == "SeniorHR")
                        {
                            isAdminHR = true;
                        }
                        else if (profileName == "Admin" || profileName == "Interviewer" || profileName == "Quality Controller")
                        {
                            isInterviewer = true;
                        }
                    }
                    var model = _instaInterviewService.GetInstaInteviewReport(interviewId).Result;
                    if (model == null)
                    {
                        if (isAdminHR || profileName == "Admin")
                        {
                            return RedirectToAction("Index", new { id = Requestedid });
                        }
                        else if (isInterviewer)
                        {
                            return RedirectToAction("MyInterview");
                        }
                    }
                    var interviewStatuses = from InterviewStatus s in Enum.GetValues(typeof(InterviewStatus))
                                            select new { ID = (int)s, Name = s.GetDescription() };
                    model.InterviewStatusList = new SelectList(interviewStatuses, "ID", "Name");
                    var meetingStatuses = from MeetingStatus s in Enum.GetValues(typeof(MeetingStatus))
                                          select new { ID = (int)s, Name = s.GetDescription() };
                    model.MeetingStatusList = new SelectList(meetingStatuses, "ID", "Name");
                    ViewBag.LevelTech = new SelectList(_irfRepository.GetLevelTech(), "id", "Name");
                    ViewBag.isAdminHR = isAdminHR;
                    //model.IsInterviewer = isInterviewer;
                    //model.PreviousMeetingStatus = model.MeetingStatus;
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
                return RedirectToAction("MyInterview");
            }
        }
        [HttpPost]
        [DisableRequestSizeLimit]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReport(InstaMeetingVM model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.PageStatus = "Mainurl";
                    ViewBag.Profile = profileName.Replace("\"", "");
                    bool isAdminHR = false;
                    bool isInterviewer = false;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Interviewer" || profileName == "SeniorHR")
                    {
                        if (profileName != null)
                        {
                            profileName = profileName.Replace("\"", "");
                            if (profileName == "HR" || profileName == "Client" || profileName == "SeniorHR")
                            {
                                isAdminHR = true;
                            }
                            else if (profileName == "Admin" || profileName == "Interviewer" || profileName == "Quality Controller")
                            {
                                isInterviewer = true;
                            }
                        }
                        var interviewStatuses = from InterviewStatus s in Enum.GetValues(typeof(InterviewStatus))
                                                select new { ID = (int)s, Name = s.GetDescription() };
                        model.InterviewStatusList = new SelectList(interviewStatuses, "ID", "Name");
                        var meetingStatuses = from MeetingStatus s in Enum.GetValues(typeof(MeetingStatus))
                                              select new { ID = (int)s, Name = s.GetDescription() };
                        model.MeetingStatusList = new SelectList(meetingStatuses, "ID", "Name");
                        ViewBag.LevelTech = new SelectList(_irfRepository.GetLevelTech(), "id", "Name");
                        ViewBag.isAdminHR = isAdminHR;
                        //if (model.VideoFile == null && model.PageVideoLink == null && model.VideoLink == null)
                        //{
                        //    ModelState.AddModelError(String.Empty, "Video file/link not found.");
                        //    return View("AddReport", model);
                        //}
                        if (model.VideoFile != null)
                        {
                            AzureCall _call = new AzureCall(_configuration);
                            var resulttype = _call.CallApiUpload(model.VideoFile);
                            resulttype = resulttype.Replace("\"", "");
                            var uploadedvideoFileURL = resulttype;
                            if (uploadedvideoFileURL == null)
                            {
                                ModelState.AddModelError(String.Empty, "Video Upload is failed.");
                                return View("AddReport", model);
                            }
                            else
                            {
                                model.VideoLink = uploadedvideoFileURL;
                            }
                        }
                        else if (model.PageVideoLink != null)
                        {
                            model.VideoLink = model.PageVideoLink;
                        }
                        if (_instaInterviewService.AddInstaInterviewReport(model, emailid).Result)
                        {
                            TempData["SuccessErrorMessage"] = "Report saved Successfully";
                            //return RedirectToAction("AddReport", new { interviewId = model.id, Requestedid = model.RequestId });
                            if (profileName == "Admin")
                            {
                                return RedirectToAction("Index", new { id = model.RequestId });
                            }
                            else
                            {
                                return RedirectToAction("MyInterview");
                            }
                        }
                        else
                        {
                            TempData["SuccessErrorMessage"] = "Report was not saved.";
                            if (profileName == "Admin")
                            {
                                return RedirectToAction("Index", new { id = model.RequestId });
                            }
                            else
                            {
                                return RedirectToAction("MyInterview");
                            }
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
                TempData["SuccessErrorMessage"] = "Report was not saved.";
                return RedirectToAction("MyInterview");
            }
        }
        [HttpGet]
        public IActionResult PublicReport(int interviewId, int Requestedid, bool isClient = false, string actiontype = null)
        {
            try
            {
                bool isAdminHR = false;
                ViewBag.actiontype = actiontype;
                string profileName = HttpContext.Session.GetString("profileName");
                ViewBag.Profile = profileName.Replace("\"", "");
                ViewBag.PageStatus = "Public";
                ViewBag.PageReport = "";
                var model = _instaInterviewService.GetInstaInteviewReport(interviewId).Result;
                var interviewStatuses = from InterviewStatus s in Enum.GetValues(typeof(InterviewStatus))
                                        select new { ID = (int)s, Name = s.GetDescription() };
                model.InterviewStatusList = new SelectList(interviewStatuses, "ID", "Name");
                var meetingStatuses = from MeetingStatus s in Enum.GetValues(typeof(MeetingStatus))
                                      select new { ID = (int)s, Name = s.GetDescription() };
                model.MeetingStatusList = new SelectList(meetingStatuses, "ID", "Name");
                ViewBag.LevelTech = new SelectList(_irfRepository.GetLevelTech(), "id", "Name");
                ViewBag.isAdminHR = isAdminHR;
                return View("AddReport", model);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("MyInterview");
            }
        }
        [HttpGet]
        public IActionResult InstaAddReport(int interviewId, int Requestedid, bool isClient = false, string actiontype = null)
        {
            try
            {
                bool isAdminHR = false;
                ViewBag.actiontype = actiontype;
                string profileName = HttpContext.Session.GetString("profileName");
                ViewBag.Profile = profileName.Replace("\"", "");
                ViewBag.PageStatus = "Public";
                ViewBag.PageReport = "Insta";
                var model = _instaInterviewService.GetInstaInteviewReport(interviewId).Result;
                var interviewStatuses = from InterviewStatus s in Enum.GetValues(typeof(InterviewStatus))
                                        select new { ID = (int)s, Name = s.GetDescription() };
                model.InterviewStatusList = new SelectList(interviewStatuses, "ID", "Name");
                var meetingStatuses = from MeetingStatus s in Enum.GetValues(typeof(MeetingStatus))
                                      select new { ID = (int)s, Name = s.GetDescription() };
                model.MeetingStatusList = new SelectList(meetingStatuses, "ID", "Name");
                ViewBag.LevelTech = new SelectList(_irfRepository.GetLevelTech(), "id", "Name");
                ViewBag.isAdminHR = isAdminHR;
                return View("AddReport", model);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("MyInterview");
            }
        }
        public InstaZoomMeetingResponse CreateZoomMeetings(InstaZoomMeetingVM _zoommetingobject, string accountEmail, DateTime indianTime)
        {
            _zoommetingobject.type = 2;
            _zoommetingobject.duration = 60;
            _zoommetingobject.timezone = "Asia/Calcutta";
            _zoommetingobject.settings.host_video = true;
            _zoommetingobject.settings.participant_video = true;
            _zoommetingobject.settings.join_before_host = true;
            _zoommetingobject.settings.jbh_time = 5;
            _zoommetingobject.settings.auto_recording = "cloud";
            string token = ZoomToken();

            var client = new RestClient(string.Concat(_configuration["ZoomAppDetails:endPoint"], $"users/" + accountEmail + "/meetings"));

            var request = new RestRequest(Method.POST);
            request.AddHeader("content-type", "application/json");
            request.AddHeader("authorization", "Bearer " + token);
            var body = JsonConvert.SerializeObject(_zoommetingobject);
            request.AddParameter("application/json", body, ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);
            var content = response.Content;
            var convertedResponse = JsonConvert.DeserializeObject<InstaZoomMeetingResponse>(content);
            convertedResponse.InterviewDate = indianTime;
            return convertedResponse;
        }
        public string ZoomToken()
        {
            DateTime Expiry = DateTime.UtcNow.AddDays(1);
            string ApiKey = _configuration["ZoomAppDetails:apiKey"];
            string ApiSecret = _configuration["ZoomAppDetails:secretKey"];
            int ts = (int)(Expiry - new DateTime(1970, 1, 1)).TotalSeconds;
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ApiSecret));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var header = new JwtHeader(credentials);
            var payload = new JwtPayload
            {
                { "iss", ApiKey},
                { "exp", ts },
            };
            var secToken = new JwtSecurityToken(header, payload);
            var handler = new JwtSecurityTokenHandler();
            var tokenString = handler.WriteToken(secToken);
            return tokenString;
        }

    }
}
