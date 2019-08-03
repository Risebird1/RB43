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
        private IIRFRepository _iRFRepository;
        public InterviewController(IMeetingService meetingService, IApplicationErrorLogger applicationErrorLogger, IAzureStorageHelper azureStorageHelper, ISkillService skillService, IIRFRepository iRFRepository)
        {
            _meetingService = meetingService;
            _applicationErrorLogger = applicationErrorLogger;
            _azureStorageHelper = azureStorageHelper;
            _skillService = skillService;
            _iRFRepository = iRFRepository;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null)
                {
                    ViewBag.Message = TempData["SuccessErrorMessageInterview"];
                    return View(_meetingService.GetMeetingList());
                }
                else
                {
                    return RedirectToAction("SignOut", "Account");
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
                if (HttpContext.Session.GetString("email") != null)
                {
                    if (ModelState.IsValid)
                    {
                        if (meeting.File == null)
                        {
                            ModelState.AddModelError(String.Empty, "Please upload a resume.");
                            var model = new MeetingVM();

                            return View("AddInterview", model);
                        }

                        byte[] fileData = new byte[meeting.File.Length];
                        string uploadedFileURL = _azureStorageHelper.InsertBlob(meeting.File.FileName, "resumes", fileData);
                        List<Skill> skills = _skillService.GetSkills();

                        if (uploadedFileURL == null)
                        {
                            ModelState.AddModelError(String.Empty, "Resume Upload is failed.");
                            var model = new MeetingVM();

                            return View("AddInterview", model);
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
                        var model = new MeetingVM();
                        return View("Add", model);
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Account");
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
                var model = new MeetingVM();
                return View(model);
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
                if (HttpContext.Session.GetString("email") != null)
                {
                    MeetingVM meetingToUpdate = _meetingService.GetMeeting(id);

                    if (meetingToUpdate != null)
                        return View("Edit", meetingToUpdate);
                    else
                        return RedirectToAction("Index");
                }
                else
                {
                    return RedirectToAction("SignOut", "Account");
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
                if (HttpContext.Session.GetString("email") != null)
                {
                    if (ModelState.IsValid)
                    {
                        //String uploadedFileURL = String.Empty;
                        if ((meetingUpdated.File!=null))
                        {
                            byte[] fileData = new byte[meetingUpdated.File.Length];
                            string uploadedFileURL = _azureStorageHelper.InsertBlob(meetingUpdated.File.FileName, "resumes", fileData);
                            if (uploadedFileURL == null)
                            {
                                ModelState.AddModelError(String.Empty, "Resume Upload is failed.");
                                var model = new MeetingVM();

                                return View("Edit");

                            }

                            meetingUpdated.resumeLink = uploadedFileURL;
                        }

                        //if (uploadedFileURL == null)
                        //{
                        //    ModelState.AddModelError(String.Empty, "Resume Upload is failed.");
                        //    var model = new MeetingVM();

                        //    return View("Edit");

                        //}
                        if (_meetingService.UpdateMeeting(meetingUpdated))
                        {
                            TempData["SuccessErrorMessageInterview"] = "Meeting is updated Successfully";
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            TempData["SuccessErrorMessageInterview"] = "Updating meeting is Failed";
                            return RedirectToAction("Index");
                        }
                    }
                    else
                    {
                        return RedirectToAction("Edit");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Account");
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
                    //    return RedirectToAction("SignOut", "Account");
                    //}
                }
                else
                {
                    return RedirectToAction("SignOut", "Account");
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
                        return View(myInterview);
                    }
                    //else
                    //{
                    //    return RedirectToAction("SignOut", "Account");
                    //}
                }
                else
                {
                    return RedirectToAction("SignOut", "Account");
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
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                return View(_meetingService.GetAllQCInterviews(emailid));
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
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                var clientInterviews = _meetingService.GetAllClientInterviews(emailid);
                foreach (var m in clientInterviews)
                {
                    if (m.L2InterviewStatus.HasValue)
                    {
                        m.L2InterviewStatDesc = ((L2InterviewStatus)(m.L2InterviewStatus)).GetDescription();
                    }
                    if (m.L2InterviewStatus.HasValue)
                    {
                        m.ClientInterviewStatDesc = ((ClientInterviewStatus)(m.ClientInterviewStatus)).GetDescription();
                    }

                }
                return View(clientInterviews);
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
            ViewBag.Message = TempData["SuccessErrorMessageInterviewComment"];
            try
            {
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
                if (ModelState.IsValid)
                {

                    if (interviewComments.AudioFile != null)
                    {
                        byte[] audiofileData = new byte[interviewComments.AudioFile.Length];
                        string uploadedaudioFileURL = _azureStorageHelper.InsertBlob(interviewComments.AudioFile.FileName, "audios", audiofileData);
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
                        byte[] videofileData = new byte[interviewComments.VideoFile.Length];
                        string uploadedvideoFileURL = _azureStorageHelper.InsertBlob(interviewComments.VideoFile.FileName, "videos", videofileData);

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
                AssignYourselfVM assinAssignYourselfVm = new AssignYourselfVM();
                assinAssignYourselfVm.id = id;
                assinAssignYourselfVm.InterviewDate = InterviewDate;
                assinAssignYourselfVm.CandidateEmail = CandidateEmail;
                return View(assinAssignYourselfVm);
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
                var result = _meetingService.AssignYourself(model.id, model.Interviewer);
                ViewBag.Message = result;
                return RedirectToAction("OpenInterview");
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
                if (prof != null && prof.Contains("Client"))
                {
                    ViewBag.Client = "Client";
                }
                ViewBag.Level = new SelectList(_iRFRepository.GetLevels(), "id", "Name");
                ViewBag.LevelTech = new SelectList(_iRFRepository.GetLevelTech(), "id", "Name");
                var model = _meetingService.GetInteviewReport(interviewId, irfId).Result;
                //model.IsEdited = false;
                //ViewBag.TechSkillsCount = model.TechSkillReportCollection.Count();
                //ViewBag.SkillGeneralCount = model.SkillGeneralReportCollection.Count();
                //ViewBag.JoinabilityCount = model.JoinabilityReportCollection.Count();
                //ViewBag.SelectabilityCount = model.SelectabilityReportCollection.Count();
                return View(model);

            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("MyInterview");
            }

        }

        [HttpPost]
        public IActionResult AddReport(InterviewReport model)
        {
            try
            {
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
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
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
    }

}