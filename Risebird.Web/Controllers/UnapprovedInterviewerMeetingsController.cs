using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;

namespace Risebird.Web.Controllers
{
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
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Finance")
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
                            
                            modelList = modelList.Where(c => filter.PrimarySkill != null &&  filter.PrimarySkill.Contains(c.PrimarySkill)
                            && filter.SCID.Contains(c.ISCId)
                            && ((filter.ConfirmationStatus != null && filter.ConfirmationStatus.Count() > 0) ? filter.ConfirmationStatus.Contains(c.confirmationStatus) : c.confirmationStatus == null)
                            && ((filter.MeetingStatus != null && filter.MeetingStatus.Count() > 0) ? filter.MeetingStatus.Contains(c.MeetingStatusStatusDesc) : c.MeetingStatusStatusDesc == string.Empty)
                            && ((filter.InterviewStatus != null && filter.InterviewStatus.Count() > 0) ? filter.InterviewStatus.Contains(c.intervieweStatusStatDesc) : c.intervieweStatusStatDesc == string.Empty)
                            && ((filter.Interviewer != null && filter.Interviewer.Count() > 0) ? filter.Interviewer.Contains(c.interviewer) : c.interviewer == null)).ToList();

                        }
                        else
                        {

                            modelList =(await _unapprovedInterviewerMeetingsService.GetAllUnapprovedInterviewerMeetings(DateTime.Now.Date, DateTime.Now.Date.AddDays(1))).ToList();
                            modelList = modelList.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewerApproved)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty; x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty; x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription();  return x; }).ToList();
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
                        var list = new List<SelectListItem>
                        {
                            new SelectListItem { Text = "No Clients", Value = "" }
                        };
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
                    if (profileName == "Admin" || profileName == "HR")
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
                                    TempData["SuccessErrorMessageUnapprovedInterviewer"] = "Meeting is updated Successfully";
                                }
                                else
                                {
                                    TempData["SuccessErrorMessageUnapprovedInterviewer"] = "Meeting is added Successfully";
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
                    if (profileName == "Admin" || profileName == "HR")
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
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Finance")
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
