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

        [Authorize]
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
                        if (TempData["SuccessErrorMessageInterview"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageInterview"].ToString();
                            TempData.Remove("SuccessErrorMessageInterview");
                        }

                        var modelList = _meetingService.GetMeetingList();
                        modelList = modelList.Where(x => x.interviewDate.Date == DateTime.Now.Date).ToList();
                        modelList = modelList.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.intervieweStatusStatDesc = x.InterviewStatus.HasValue?((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription():string.Empty; x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue?((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription():string.Empty; x.interviewSlot = ((ReceiveMeetingStatus)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();


                        var list = new SelectList(new[]
{
    new { ID = "Today", Name = "Today"},
    new { ID = "Tomorrow", Name = "Tomorrow"  },
    new { ID = "Yesterday", Name = "Yesterday"},
    new { ID = "Next Seven Days", Name = "Next Seven Days" },
    new { ID = "All", Name = "All"},
},
"ID", "Name", "Today");

                        ViewData["list"] = list;
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

        public async Task<IActionResult> GetFilteredInterviews(string filterValue)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR")
                    {
                        var modelList = _meetingService.GetMeetingList();
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
                        modelList = modelList.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty; x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty; x.interviewSlot = ((ReceiveMeetingStatus)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();
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
                        string uploadedFileURL = string.Empty;
                        var confirmationStatuses = from ReceiveMeetingStatus s in Enum.GetValues(typeof(ReceiveMeetingStatus))
                                                   select new { ID = (int)s, Name = s.GetDescription() };
                        meeting.ConfirmationStatusList = new SelectList(confirmationStatuses, "ID", "Name");

                        var interviewslotList = from InterviewSlots s in Enum.GetValues(typeof(InterviewSlots))
                                                select new { ID = (int)s, Name = s.GetDescription() };
                        meeting.InterviewSlotList = new SelectList(interviewslotList, "ID", "Name");

                        if (ModelState.IsValid)
                        {
                            if (meeting.File == null && (meeting.resumeLink == null))
                            {
                                ModelState.AddModelError(String.Empty, "Please upload a resume.");
                                return View("Add", meeting);
                            }
                            if (meeting.File != null)
                            {
                                await _azureStorageHelper.InsertBlob(meeting.File.FileName, "resumes", meeting.File);
                                var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                                var blob = containerRef.GetBlockBlobReference(meeting.File.FileName);
                                uploadedFileURL = blob.Uri.ToString();

                            }
                            else
                                uploadedFileURL = meeting.resumeLink;
                            List<Skill> skills = _skillService.GetSkills();

                            if (uploadedFileURL == null || uploadedFileURL == string.Empty)
                            {
                                ModelState.AddModelError(String.Empty, "Resume Upload is failed.");

                                return View("Add", meeting);
                            }
                            else
                            {
                                meeting.resumeLink = uploadedFileURL;
                                meeting.candidateEmail = meeting.candidateEmail.ToLower();
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
                    if (profileName == "Admin" || profileName == "HR")
                    {
                        var list = new List<SelectListItem>();
                        list.Add(new SelectListItem { Text = "No Clients", Value = "" });
                        var model = new MeetingVM();
                        var confirmationStatuses = from ReceiveMeetingStatus s in Enum.GetValues(typeof(ReceiveMeetingStatus))
                                                   select new { ID = (int)s, Name = s.GetDescription() };
                        model.ConfirmationStatusList = new SelectList(confirmationStatuses, "ID", "Name");
                        var interviewslotList = from InterviewSlots s in Enum.GetValues(typeof(InterviewSlots))
                                                select new { ID = (int)s, Name = s.GetDescription() };
                        model.InterviewSlotList = new SelectList(interviewslotList, "ID", "Name");
                        model.IRFList = _meetingService.GetIRFList();
                        var clients = _meetingService.GetClientsByRoles(model.IRFList.First().Text);
                        if ((clients.Where(x => x.ClientRoleId == 1) == null) || (clients.Where(x => x.ClientRoleId == 1)).Count() == 0)
                        {
                            model.ClientHrContactList = new SelectList(list, "Value", "Text");
                        }
                        else
                        {
                            model.ClientHrContactList = new SelectList(clients.Where(x => x.ClientRoleId == 1), "EmailId", "EmailId");

                        }
                        if ((clients.Where(x => x.ClientRoleId == 4) == null) || (clients.Where(x => x.ClientRoleId == 4)).Count() == 0)
                        {
                            model.StaffingAgencyList = new SelectList(list, "Value", "Text");
                        }
                        else
                        {
                            model.StaffingAgencyList = new SelectList(clients.Where(x => x.ClientRoleId == 4), "EmailId", "EmailId");

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
                    if (profileName == "Admin" || profileName == "HR")
                    {
                        var list = new List<SelectListItem>();
                        list.Add(new SelectListItem { Text = "No Clients", Value = "" });
                        MeetingVM meetingToUpdate = _meetingService.GetMeeting(id);
                        var confirmationStatuses = from ReceiveMeetingStatus s in Enum.GetValues(typeof(ReceiveMeetingStatus))
                                                   select new { ID = (int)s, Name = s.GetDescription() };
                        meetingToUpdate.ConfirmationStatusList = new SelectList(confirmationStatuses, "ID", "Name");
                        var interviewslotList = from InterviewSlots s in Enum.GetValues(typeof(InterviewSlots))
                                                select new { ID = (int)s, Name = s.GetDescription() };
                        meetingToUpdate.InterviewSlotList = new SelectList(interviewslotList, "ID", "Name");
                        meetingToUpdate.IRFList = _meetingService.GetIRFList();
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

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
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
                            else if (meetingUpdated.resumeLink == null || meetingUpdated.resumeLink == "")
                            {
                                meetingUpdated.resumeLink = TempData["ResumeLink"] != null ? TempData["ResumeLink"].ToString() : null;

                            }

                            meetingUpdated.candidateEmail = meetingUpdated.candidateEmail.ToLower();
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

        [Authorize]
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
                        var meetings = _meetingService.GetAllOpenInterviews(emailid);
                        meetings = meetings.Where(x => x.interviewDate == DateTime.Now.Date).ToList();
                        meetings = meetings.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();

                        var list = new SelectList(new[]
{
    new { ID = "Today", Name = "Today"},
    new { ID = "Tomorrow", Name = "Tomorrow"  },
    new { ID = "Next Seven Days", Name = "Next Seven Days" },
    new { ID = "All", Name = "All"},
},
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

        public async Task<IActionResult> GetFilteredOpenInterviews(string filterValue)
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
                        var meetings = _meetingService.GetAllOpenInterviews(emailid);

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
                        var myInterview = _meetingService.GetAllAssignedInterviews(emailid);
                        myInterview = myInterview.Where(x => x.interviewDate.Date == DateTime.Now.Date).ToList();
                        myInterview = myInterview.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();

                        var list = new SelectList(new[]
{
    new { ID = "Today", Name = "Today"},
    new { ID = "Tomorrow", Name = "Tomorrow"  },
      new { ID = "Yesterday", Name = "Yesterday"},
    new { ID = "Next Seven Days", Name = "Next Seven Days" },
    new { ID = "All", Name = "All"},
},
"ID", "Name", "Today");

                        ViewData["list"] = list;


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

        public async Task<IActionResult> GetFilteredMyInterviews(string filterValue)
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
                        var myInterview = _meetingService.GetAllAssignedInterviews(emailid);

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
        public IActionResult ClientMeetings()
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
                        var meetings = _meetingService.GetClientMeetingList(emailid);//_meetingService.GetClientMeetingList("aneel.tirumalasetty@mphasis.com")

                        meetings = meetings.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((ReceiveMeetingStatus)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();

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
                        var list = new SelectList(new[]
{
    new { ID = "All", Name = "All"},
    new { ID = "Today", Name = "Today"},
    new { ID = "Tomorrow", Name = "Tomorrow"  },
    new { ID = "Yesterday", Name = "Yesterday"  },
    new { ID = "Next Seven Days", Name = "Next Seven Days" },
   
},
"ID", "Name", "All");

                        ViewData["list"] = list;

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
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }

        }

        [Authorize]
        [HttpGet]
        public IActionResult GetClientFilteredMeetings(string filterValue)
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
                        var meetings = _meetingService.GetClientMeetingList(emailid);//_meetingService.GetClientMeetingList("aneel.tirumalasetty@mphasis.com")

                        meetings = meetings.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((ReceiveMeetingStatus)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();

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
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }

        }

        [Authorize]
        [HttpGet]
        public IActionResult ClientSelectedMeetings()
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
                        var meetings = _meetingService.GetClientMeetingList(emailid);//_meetingService.GetClientMeetingList("aneel.tirumalasetty@mphasis.com")
                        meetings = meetings.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((ReceiveMeetingStatus)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();

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
                            selectedMeeting.Add(m);
                        }
                        var list = new SelectList(new[]
{
    new { ID = "Today", Name = "Today"},
    new { ID = "Tomorrow", Name = "Tomorrow"  },
    new { ID = "Yesterday", Name = "Yesterday"  },
    new { ID = "Next Seven Days", Name = "Next Seven Days" },
    new { ID = "All", Name = "All"},
},
"ID", "Name", "All");

                        ViewData["list"] = list;

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
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }

        }

        [Authorize]
        [HttpGet]
        public IActionResult ClientFilteredSelectedMeetings(string filterValue)
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
                        var meetings = _meetingService.GetClientMeetingList(emailid);//_meetingService.GetClientMeetingList("aneel.tirumalasetty@mphasis.com")
                        meetings = meetings.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((ReceiveMeetingStatus)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();

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
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
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
                        if (profileName == "Admin" || profileName == "HR" || profileName == "Interviewer" ||
                        profileName == "Quality Controller")
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
                                TempData["SuccessErrorMessageInterview"] = "Comments added Successfully";
                                return RedirectToAction("AddComments", new { interviewId = interviewComments.InterviewId });
                            }
                            else
                            {
                                TempData["SuccessErrorMessageInterview"] = "Comments add failed";
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
                TempData["SuccessErrorMessageInterview"] = "Comments add failed";
                return RedirectToAction("Action", new { interviewId = interviewComments.InterviewId });
            }
        }

        [Authorize]
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
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Interviewer" ||
                        profileName == "Quality Controller")
                    {
                        var result = _meetingService.AssignYourself(model.id, model.Interviewer.ToLower());
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
        public async Task<IActionResult> UnassignYourself(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var email = HttpContext.Session.GetString("email");
                    email = email.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Interviewer" ||
                        profileName == "Quality Controller")
                    {
                        if (_meetingService.UnassignYourself(id, email.ToLower()))
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
        public IActionResult AddReport(int interviewId, int irfId, bool isClient=false, bool isClientSelected= false)
        {
            try
            {
                string profileName = HttpContext.Session.GetString("profileName");
                if (profileName != null)
                {
                    profileName = profileName.Replace("\"", "");
                }

                var model = _meetingService.GetInteviewReport(interviewId, irfId).Result;
                if (model == null)
                {
                    if (profileName == "Admin" || profileName == "HR")
                    {
                        return RedirectToAction("Index");
                    }
                    else if (profileName != null && (profileName == "Interviewer" ||
                                                     profileName == "Quality Controller"))
                    {
                        return RedirectToAction("MyInterview");
                    }
                }
                if (profileName != null && (profileName == "Interviewer" ||
                                            profileName == "Quality Controller"))
                {
                    model.IsInterviewer = true;
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
                GetStatusList(model, irfId);

                return View(model);

            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
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
                if (HttpContext.Session.GetString("email") != null &&
                    HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Interviewer" ||
                        profileName == "Quality Controller")
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
                        //if (model.VideoFile == null && model.PageVideoLink == null && model.VideoLink == null)
                        //{
                        //   // ModelState.AddModelError(String.Empty, "Video file/link not found.");
                        //    GetStatusList(model);
                        //    return View("AddReport", model);
                        //}

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
                        else if (model.PageVideoLink != null)
                        {
                            model.VideoLink = model.PageVideoLink;
                        }

                        if (_meetingService.AddInterviewReport(model).Result)
                        {
                            TempData["SuccessErrorMessageInterview"] = "Report saved Successfully";
                            if (profileName == "Admin" || profileName == "HR")
                            {
                                return RedirectToAction("Index");
                            }
                            else
                                return RedirectToAction("MyInterview");
                        }
                        else
                        {
                            TempData["SuccessErrorMessageInterview"] = "Report was not saved.";
                            if (profileName == "Admin" || profileName == "HR")
                            {
                                return RedirectToAction("Index");
                            }
                            else
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

            model.QuestionsList = new SelectList(_meetingService.GetTechSkillComments(irfId), "Comment", "Comment");
        }

        [Authorize]
        [HttpGet]
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

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SampleQuestions(TechSkillComments model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Interviewer" || profileName == "Quality Controller")
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

        //GetClientsByRoles
        public SelectList GetClientsByRoles(string val)
        {
            var list = new List<SelectListItem>();
            var clients = _meetingService.GetClientsByRoles(val);
            if (clients != null && clients.Count() > 0)
            {
                return new SelectList(_meetingService.GetClientsByRoles(val), "ClientRoleId", "EmailId");
            }
            else
            {
                list.Add(new SelectListItem { Text = "No Clients", Value = "" });
                return new SelectList(list, "Value", "Text");
            }


        }
    }

}