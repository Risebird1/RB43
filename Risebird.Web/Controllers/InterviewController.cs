using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Risebird.Entities.Data;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Risebird.Repository.Interface;
using Microsoft.AspNetCore.Http;
using Risebird.CommonHelper;


namespace Risebird.Web.Controllers
{
    [Authorize]
    public class InterviewController : Controller
    {
        private IMeetingService _meetingService;
        private IApplicationErrorLogger _applicationErrorLogger;
        private IAzureStorageHelper _azureStorageHelper;
        private ISkillService _skillService;
        private IIRFService _iRFService;
        private IIRFRepository _iRFRepository;
        public InterviewController(IMeetingService meetingService, IApplicationErrorLogger applicationErrorLogger, IAzureStorageHelper azureStorageHelper, ISkillService skillService, IIRFRepository iRFRepository, IIRFService iRFService)
        {
            _meetingService = meetingService;
            _applicationErrorLogger = applicationErrorLogger;
            _azureStorageHelper = azureStorageHelper;
            _skillService = skillService;
            _iRFRepository = iRFRepository;
            _iRFService = iRFService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR")
                    {
                        ViewBag.Message = TempData["SuccessErrorMessageInterview"];
                        var modelList = _meetingService.GetMeetingList();
                        modelList = modelList.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((ReceiveMeetingStatus)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();

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
        [HttpPost]
        public async Task<IActionResult> Add(MeetingVM meeting)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR")
                    {
                        var confirmationStatuses = from ReceiveMeetingStatus s in Enum.GetValues(typeof(ReceiveMeetingStatus))
                                                   select new { ID = (int)s, Name = s.GetDescription() };
                        meeting.ConfirmationStatusList = new SelectList(confirmationStatuses, "ID", "Name");

                        var interviewslotList = from InterviewSlots s in Enum.GetValues(typeof(InterviewSlots))
                                                select new { ID = (int)s, Name = s.GetDescription() };
                        meeting.InterviewSlotList = new SelectList(interviewslotList, "ID", "Name");

                        if (ModelState.IsValid)
                        {
                            if (meeting.File == null)
                            {
                                ModelState.AddModelError(String.Empty, "Please upload a resume.");
                                return View("Add", meeting);
                            }

                            await _azureStorageHelper.InsertBlob(meeting.File.FileName, "resumes", meeting.File);
                            var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                            var blob = containerRef.GetBlockBlobReference(meeting.File.FileName);
                            string uploadedFileURL = blob.Uri.ToString();
                            List<Skill> skills = _skillService.GetSkills();

                            if (uploadedFileURL == null)
                            {
                                ModelState.AddModelError(String.Empty, "Resume Upload is failed.");

                                return View("Add", meeting);
                            }

                            else
                            {
                                meeting.resumeLink = uploadedFileURL;

                                if (_meetingService.AddMeeting(meeting))
                                {
                                    TempData["SuccessErrorMessageInterview"] = "Meeting is added Successfully";
                                    return RedirectToAction("Index");
                                }
                                else
                                {
                                    TempData["SuccessErrorMessageInterview"] = "Meeting addition is Failed";
                                    return RedirectToAction("Index");
                                }
                            }
                        }
                        else
                        {

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
                TempData["SuccessErrorMessageInterview"] = "Meeting addition is Failed";
                return RedirectToAction("Index");
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
                    if (profileName == "Admin" || profileName == "HR")
                    {
                        var model = new MeetingVM();
                        var confirmationStatuses = from ReceiveMeetingStatus s in Enum.GetValues(typeof(ReceiveMeetingStatus))
                                                   select new { ID = (int)s, Name = s.GetDescription() };
                        model.ConfirmationStatusList = new SelectList(confirmationStatuses, "ID", "Name");
                        var interviewslotList = from InterviewSlots s in Enum.GetValues(typeof(InterviewSlots))
                                                select new { ID = (int)s, Name = s.GetDescription() };
                        model.InterviewSlotList = new SelectList(interviewslotList, "ID", "Name");
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

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR")
                    {
                        MeetingVM meetingToUpdate = _meetingService.GetMeeting(id);
                        var confirmationStatuses = from ReceiveMeetingStatus s in Enum.GetValues(typeof(ReceiveMeetingStatus))
                                                   select new { ID = (int)s, Name = s.GetDescription() };
                        meetingToUpdate.ConfirmationStatusList = new SelectList(confirmationStatuses, "ID", "Name");
                        var interviewslotList = from InterviewSlots s in Enum.GetValues(typeof(InterviewSlots))
                                                select new { ID = (int)s, Name = s.GetDescription() };
                        meetingToUpdate.InterviewSlotList = new SelectList(interviewslotList, "ID", "Name");
                        if (meetingToUpdate != null)
                        {
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

        [HttpPost]
        public async Task<IActionResult> Edit(MeetingVM meetingUpdated)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR")
                    {
                        var confirmationStatuses = from ReceiveMeetingStatus s in Enum.GetValues(typeof(ReceiveMeetingStatus))
                                                   select new { ID = (int)s, Name = s.GetDescription() };
                        meetingUpdated.ConfirmationStatusList = new SelectList(confirmationStatuses, "ID", "Name");
                        var interviewslotList = from InterviewSlots s in Enum.GetValues(typeof(InterviewSlots))
                                                select new { ID = (int)s, Name = s.GetDescription() };
                        meetingUpdated.InterviewSlotList = new SelectList(interviewslotList, "ID", "Name");
                        if (ModelState.IsValid)
                        {
                            meetingUpdated.resumeLink = TempData["ResumeLink"] != null ? TempData["ResumeLink"].ToString() : null;
                            //String uploadedFileURL = String.Empty;
                            if ((meetingUpdated.File != null))
                            {

                                await _azureStorageHelper.InsertBlob(meetingUpdated.File.FileName, "resumes", meetingUpdated.File);
                                var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                                var blob = containerRef.GetBlockBlobReference(meetingUpdated.File.FileName);
                                string uploadedFileURL = blob.Uri.ToString();
                                if (uploadedFileURL == null)
                                {
                                    ModelState.AddModelError(String.Empty, "Resume Upload is failed.");
                                    var model = new MeetingVM();

                                    return View("Edit");

                                }

                                meetingUpdated.resumeLink = uploadedFileURL;
                            }

                            if (_meetingService.UpdateMeeting(meetingUpdated))
                            {
                                TempData["SuccessErrorMessageInterview"] = "Meeting has been updated Successfully";
                                return RedirectToAction("Index");
                            }
                            else
                            {
                                TempData["SuccessErrorMessageInterview"] = "Meeting update has Failed";
                                return RedirectToAction("Index");
                            }
                        }
                        else
                        {
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
                TempData["SuccessErrorMessageInterview"] = "Updating meeting Level is Failed";
                return View("Index");
            }
        }


        public async Task<IActionResult> OpenInterview()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    //if (profileName == "Interviewer" || profileName == "Quality Controller")
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        return View(_meetingService.GetAllOpenInterviews(emailid));
                    }
                    //else
                    //{
                    //    return RedirectToAction("SignOut", "Session");
                    //}
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
        public async Task<IActionResult> MyInterview()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    //if (profileName == "Interviewer" || profileName == "Quality Controller")
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        var myInterview = _meetingService.GetAllAssignedInterviews(emailid);
                        if (TempData["SuccessErrorMessage"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessage"].ToString();
                        }
                        return View(myInterview);
                    }
                    //else
                    //{
                    //    return RedirectToAction("SignOut", "Session");
                    //}
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

        public async Task<IActionResult> ClientInterview()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");

                    var meetings = _meetingService.GetAllClientInterviews(emailid);

                    foreach (var m in meetings)
                    {
                        if (m.intervieweStatus.HasValue)
                        {
                            m.intervieweStatusStatDesc = ((InterviewStatus)(m.intervieweStatus)).GetDescription();
                        }
                        if (m.L2InterviewStatus.HasValue)
                        {
                            m.L2InterviewStatDesc = ((L2InterviewStatus)(m.L2InterviewStatus)).GetDescription();
                        }
                        if (m.ClientInterviewStatus.HasValue)
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
                    return View(meetings);
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
        public async Task<IActionResult> AddComments(int interviewId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    ViewBag.Message = TempData["SuccessErrorMessageInterviewComment"];
                    InterviewComments comments = _meetingService.GetInterviewComments(interviewId);
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

        [HttpPost]
        public async Task<IActionResult> AddComments(InterviewComments interviewComments)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (ModelState.IsValid)
                    {

                        if (interviewComments.AudioFile != null)
                        {

                            await _azureStorageHelper.InsertBlob(interviewComments.AudioFile.FileName, "audios", interviewComments.AudioFile);
                            var containerRef = _azureStorageHelper.GetCloudBlobContainer("audios");
                            var blob = containerRef.GetBlockBlobReference(interviewComments.AudioFile.FileName);
                            string uploadedaudioFileURL = blob.Uri.ToString();

                            if (uploadedaudioFileURL == null)
                            {
                                ModelState.AddModelError(String.Empty, "Audio Upload is failed.");
                                GetStatusList(interviewComments);
                                return View("InterviewComments", interviewComments);
                            }
                            else
                            {
                                interviewComments.AudioLink = uploadedaudioFileURL;
                            }
                        }
                        if (interviewComments.VideoFile != null)
                        {

                            await _azureStorageHelper.InsertBlob(interviewComments.VideoFile.FileName, "videos", interviewComments.VideoFile);
                            var containerRef = _azureStorageHelper.GetCloudBlobContainer("videos");
                            var blob = containerRef.GetBlockBlobReference(interviewComments.VideoFile.FileName);
                            string uploadedvideoFileURL = blob.Uri.ToString();
                            if (uploadedvideoFileURL == null)
                            {
                                ModelState.AddModelError(String.Empty, "Video Upload is failed.");
                                GetStatusList(interviewComments);
                                //return View("InterviewComments", interviewComments);
                            }
                            else
                            {
                                interviewComments.VideoLink = uploadedvideoFileURL;
                            }
                        }

                        if (_meetingService.UpdateComments(interviewComments))
                        {
                            TempData["SuccessErrorMessageInterviewComment"] = "Report are updated Successfully";
                            return RedirectToAction("AddComments", new { interviewId = interviewComments.InterviewId });
                        }
                        else
                        {
                            TempData["SuccessErrorMessageInterviewComment"] = "Report update is failed";
                            return RedirectToAction("Action", new { interviewId = interviewComments.InterviewId });
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
                TempData["SuccessErrorMessageInterviewComment"] = "Report update is failed";
                return RedirectToAction("Action", new { interviewId = interviewComments.InterviewId });
            }
        }

        [HttpGet]
        public async Task<IActionResult> AssignYourself(int id, string InterviewDate, string CandidateEmail)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    AssignYourselfVM assinAssignYourselfVm = new AssignYourselfVM();
                    assinAssignYourselfVm.id = id;
                    assinAssignYourselfVm.InterviewDate = InterviewDate;
                    assinAssignYourselfVm.CandidateEmail = CandidateEmail;
                    return View(assinAssignYourselfVm);
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
        public async Task<IActionResult> AssignYourself(AssignYourselfVM model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var result = _meetingService.AssignYourself(model.id, model.Interviewer);
                    ViewBag.Message = result;
                    return RedirectToAction("OpenInterview");
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

        public async Task<IActionResult> UnassignYourself(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (_meetingService.UnassignYourself(id))
                    {
                        TempData["SuccessErrorMessageMyInterview"] = "Unassignment is done Successfully";
                        return Json(new { url = Url.Action("MyInterview", "Interview") });
                    }
                    else
                    {
                        TempData["SuccessErrorMessageMyInterview"] = "Unassignment is Failed";
                        return Json(new { url = Url.Action("MyInterview", "Interview") });
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
        public IActionResult AddReport(int interviewId, int irfId)
        {
            try
            {
                string prof = HttpContext.Session.GetString("profileName");
               
                var model = _meetingService.GetInteviewReport(interviewId, irfId).Result;
                if (prof != null && prof.Contains("Client"))
                {
                    model.IsClientUser = true;
                }
                
                GetStatusList(model, irfId);
                return View(model);

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
        public async Task<IActionResult> AddReport(InterviewReport model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (model.AudioFile != null)
                {
                    await _azureStorageHelper.InsertBlob(model.AudioFile.FileName, "audios", model.AudioFile);
                    var containerRef = _azureStorageHelper.GetCloudBlobContainer("audios");
                    var blob = containerRef.GetBlockBlobReference(model.AudioFile.FileName);
                    string uploadedaudioFileURL = blob.Uri.ToString();
                    if (uploadedaudioFileURL == null)
                    {
                        ModelState.AddModelError(String.Empty, "Audio Upload is failed.");
                        GetStatusList(model);
                        return View("AddReport", model);
                    }
                    else
                    {
                        model.AudioLink = uploadedaudioFileURL;
                    }
                }
                if (model.VideoFile != null)
                {
                    // byte[] videofileData = new byte[model.VideoFile.Length];
                    // string uploadedvideoFileURL = _azureStorageHelper.InsertBlob(model.VideoFile.FileName, "videos", videofileData);
                    await _azureStorageHelper.InsertBlob(model.VideoFile.FileName, "videos", model.VideoFile);
                    var containerRef = _azureStorageHelper.GetCloudBlobContainer("videos");
                    var blob = containerRef.GetBlockBlobReference(model.VideoFile.FileName);
                    string uploadedvideoFileURL = blob.Uri.ToString();
                    if (uploadedvideoFileURL == null)
                    {
                        ModelState.AddModelError(String.Empty, "Video Upload is failed.");
                        GetStatusList(model);
                        return View("AddReport", model);
                    }
                    else
                    {
                        model.VideoLink = uploadedvideoFileURL;
                    }
                }
                if (_meetingService.AddInterviewReport(model).Result)
                {
                    TempData["SuccessErrorMessage"] = "Report saved Successfully";
                    return RedirectToAction("MyInterview");
                }
                else
                {
                    TempData["SuccessErrorMessage"] = "Report was not saved.";
                    return RedirectToAction("MyInterview");
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
                return RedirectToAction("Index");
            }
        }

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

            var qcStatuses = from QCStatus s in Enum.GetValues(typeof(QCStatus))
                             select new { ID = (int)s, Name = s.GetDescription() };
            model.QCStatusList = new SelectList(fraudCheckStatuses, "ID", "Name");

            var trainingStatuses = from TrainingStatus s in Enum.GetValues(typeof(TrainingStatus))
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

        private void GetStatusList(InterviewReport model, int irfId = 0)
        {
            var interviewStatuses = from InterviewStatus s in Enum.GetValues(typeof(InterviewStatus))
                                    select new { ID = (int)s, Name = s.GetDescription() };
            model.InterviewStatusList = new SelectList(interviewStatuses, "ID", "Name");

            var meetingStatuses = from MeetingStatus s in Enum.GetValues(typeof(MeetingStatus))
                                  select new { ID = (int)s, Name = s.GetDescription() };

            model.MeetingStatusList = new SelectList(meetingStatuses, "ID", "Name");

            model.Level = new SelectList(_iRFRepository.GetLevels(), "id", "Name"); 
            model.LevelTech = new SelectList(_iRFRepository.GetLevelTech(), "id", "Name");
            var questionsLevel = from QuestionsLevel s in Enum.GetValues(typeof(QuestionsLevel))
                                 select new { ID = (int)s, Name = s.GetDescription() };
            model.QuestionsLevelList = new SelectList(questionsLevel, "ID", "Name");
            var confirmationTypes = from ConfirmationStatus s in Enum.GetValues(typeof(ConfirmationStatus))
                                    select new { ID = (int)s, Name = s.GetDescription() };
            model.ConfirmationTypeList= new SelectList(confirmationTypes, "ID", "Name"); ;
            model.QuestionsList = new SelectList(_meetingService.GetTechSkillComments(irfId), "Comment", "Comment");

        }

        public IActionResult SampleQuestions(string id)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                TechSkillComments model = new TechSkillComments();
                var result = _iRFService.GetTechSkillComments(Convert.ToInt16(id));
                //if (result.Count() > 0)
                model.SkillCollection = result;
                ViewBag.Skills = new SelectList(_iRFService.GetTechSkills(Convert.ToInt16(id)), "id", "Name");
                model.IRFId = Convert.ToInt16(id);
                return View(model);
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SampleQuestions(TechSkillComments model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (_iRFService.AddSkillComments(model))
                    {
                        TempData["SuccessErrorMessage"] = "Questions saved Successfully";
                        return RedirectToAction("MyInterview");
                    }
                    else
                    {
                        TempData["SuccessErrorMessage"] = "Questions were not saved.";
                        return RedirectToAction("MyInterview");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }

            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessage"] = "Questions were not saved";
                return RedirectToAction("MyInterview");
            }
        }
    }

}