using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using OfficeOpenXml;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using X.PagedList.Extensions;

namespace Risebird.Web.Controllers
{
    [Authorize]
    public class HRController : Controller
    {
        private readonly IMeetingService _meetingService;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IAzureStorageHelper _azureStorageHelper;
        //private readonly IConfiguration _configuration;
        private static List<Meeting> _modelDetail;
        private static string Searchvalue;
        private readonly IFileMimeTypeHelper _fileMimeTypeHelper;


        public HRController(IMeetingService meetingService, IApplicationErrorLogger applicationErrorLogger, IAzureStorageHelper azureStorageHelper, IFileMimeTypeHelper fileMimeTypeHelper)
        {
            _meetingService = meetingService;
            _applicationErrorLogger = applicationErrorLogger;
            _azureStorageHelper = azureStorageHelper;
            _fileMimeTypeHelper = fileMimeTypeHelper;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> OpenInterviews(int? Page, string searchValue)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                    if (profileName == "HR")
                    {
                        var meetings = _meetingService.GetHROpenInterviews(emailId);
                        if (HttpContext.Session.GetString("OpenHRfilters") != null)
                        {
                            var filter = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("OpenHRfilters"));

                            if (filter.StartDate != null && filter.EndDate != null)
                            {
                                meetings = meetings.Where(t => t.interviewDate.Date >= filter.StartDate && t.interviewDate.Date <= filter.EndDate).ToList();
                            }
                            else if (filter.StartDate != null && filter.EndDate == null)
                            {
                                meetings = meetings.Where(t => t.interviewDate.Date >= filter.StartDate).ToList();
                            }
                            else if (filter.StartDate == null && filter.EndDate != null)
                            {
                                meetings = meetings.Where(t => t.interviewDate.Date <= filter.EndDate).ToList();
                            }

                            filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = meetings.Select(x => x.JDTiltle.Split("|")[0]).ToArray() : filter.PrimarySkill = filter.PrimarySkill;
                            filter.Interviewer = filter.Interviewer == null ? filter.Interviewer = meetings.Select(x => x.interviewer).ToArray() : filter.Interviewer = filter.Interviewer;
                            filter.ClientName = filter.ClientName == null ? filter.ClientName = meetings.Select(x => x.ClientName).ToArray() : filter.ClientName = filter.ClientName;
                            filter.Client = filter.Client == null ? filter.Client = meetings.Select(x => x.ClientCompName).ToArray() : filter.Client = filter.Client;
                            filter.RFID = filter.RFID == null ? filter.RFID = meetings.Select(x => x.IRFID).ToArray() : filter.RFID = filter.RFID;
                            filter.ClientHR = filter.ClientHR == null ? filter.ClientHR = meetings.Select(x => x.CandidateClientHR).ToArray() : filter.ClientHR = filter.ClientHR;

                            meetings = meetings.Where(c => filter.PrimarySkill != null && filter.PrimarySkill.Contains(c.JDTiltle.Split("|")[0])
                            && filter.RFID.Contains(c.IRFID)
                            && ((filter.ClientHR != null && filter.ClientHR.Count() > 0) ? filter.ClientHR.Contains(c.CandidateClientHR) : c.CandidateClientHR == null)
                            && ((filter.ClientName != null && filter.ClientName.Count() > 0) ? filter.ClientName.Contains(c.ClientName) : c.ClientName == null)
                            && ((filter.Client != null && filter.Client.Count() > 0) ? filter.Client.Contains(c.ClientCompName) : c.ClientCompName == null)
                            && ((filter.Interviewer != null && filter.Interviewer.Count() > 0) ? filter.Interviewer.Contains(c.interviewer) : c.interviewer == null)).ToList();

                        }

                        meetings = meetings.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();


                        if (TempData["SuccessErrorMessageHR"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageHR"].ToString();
                            TempData.Remove("SuccessErrorMessageHR");
                        }

                        if (!string.IsNullOrEmpty(searchValue))
                        {
                            searchValue = searchValue.ToUpper().Trim();
                            meetings = meetings.Where(x => (!string.IsNullOrEmpty(x.candidateName) ? x.candidateName.ToUpper().Contains(searchValue) : x.candidateName == "") || (!string.IsNullOrEmpty(x.interviewer) ? x.interviewer.ToUpper().Contains(searchValue) : x.interviewer == "") || (!string.IsNullOrEmpty(x.ClientHrContact) ? x.ClientHrContact.ToUpper().Contains(searchValue) : x.ClientHrContact == "")).ToList();

                        }
                        ViewBag.TotalCount = meetings.Count();

                        ViewBag.PageStartCount = 10 * ((Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page)) - 1) + 1;
                        int a = meetings.Count() / 10;
                        if (meetings.Count() % 10 == 0)
                        {
                            ViewBag.PageEndCount = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
                        }
                        else
                        {
                            a = a + 1;
                            if (a == Page)
                            {
                                ViewBag.PageEndCount = meetings.Count();
                            }
                            else
                            {
                                ViewBag.PageEndCount = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));

                            }

                        }
                        if (meetings.Count() == 0)
                        {
                            ViewBag.PageStartCount = 0;
                            ViewBag.PageEndCount = 0;
                        }
                        _modelDetail = (List<Meeting>)meetings;
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
                return null;
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> OpenInterviews(InterviewFilter filter, int? Page, string searchValue)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (filter != null)
                    {
                        HttpContext.Session.SetString("OpenHRfilters", JsonConvert.SerializeObject(filter));
                    }
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "HR")
                    {
                        var emailId = HttpContext.Session.GetString("email")?.Trim();
                        emailId = emailId.Replace("\"", "");
                        var meetings = _meetingService.GetHROpenInterviews(emailId);

                        if (filter.StartDate != null && filter.EndDate != null)
                        {
                            meetings = meetings.Where(t => t.interviewDate.Date >= filter.StartDate && t.interviewDate.Date <= filter.EndDate).ToList();
                        }
                        else if (filter.StartDate != null && filter.EndDate == null)
                        {
                            meetings = meetings.Where(t => t.interviewDate.Date >= filter.StartDate).ToList();
                        }
                        else if (filter.StartDate == null && filter.EndDate != null)
                        {
                            meetings = meetings.Where(t => t.interviewDate.Date <= filter.EndDate).ToList();
                        }

                        filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = meetings.Select(x => x.JDTiltle.Split("|")[0]).ToArray() : filter.PrimarySkill = filter.PrimarySkill;
                        filter.Interviewer = filter.Interviewer == null ? filter.Interviewer = meetings.Select(x => x.interviewer).ToArray() : filter.Interviewer = filter.Interviewer;
                        filter.ClientName = filter.ClientName == null ? filter.ClientName = meetings.Select(x => x.ClientName).ToArray() : filter.ClientName = filter.ClientName;
                        filter.Client = filter.Client == null ? filter.Client = meetings.Select(x => x.ClientCompName).ToArray() : filter.Client = filter.Client;
                        filter.RFID = filter.RFID == null ? filter.RFID = meetings.Select(x => x.IRFID).ToArray() : filter.RFID = filter.RFID;
                        filter.ClientHR = filter.ClientHR == null ? filter.ClientHR = meetings.Select(x => x.CandidateClientHR).ToArray() : filter.ClientHR = filter.ClientHR;

                        meetings = meetings.Where(c => filter.PrimarySkill != null && filter.PrimarySkill.Contains(c.JDTiltle.Split("|")[0])
                        && filter.RFID.Contains(c.IRFID)
                        && ((filter.ClientHR != null && filter.ClientHR.Count() > 0) ? filter.ClientHR.Contains(c.CandidateClientHR) : c.CandidateClientHR == null)
                        && ((filter.ClientName != null && filter.ClientName.Count() > 0) ? filter.ClientName.Contains(c.ClientName) : c.ClientName == null)
                        && ((filter.Client != null && filter.Client.Count() > 0) ? filter.Client.Contains(c.ClientCompName) : c.ClientCompName == null)
                        && ((filter.Interviewer != null && filter.Interviewer.Count() > 0) ? filter.Interviewer.Contains(c.interviewer) : c.interviewer == null)).ToList();

                        meetings = meetings.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();


                        if (TempData["SuccessErrorMessageHR"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageHR"].ToString();
                            TempData.Remove("SuccessErrorMessageHR");
                        }

                        if (!string.IsNullOrEmpty(searchValue))
                        {
                            searchValue = searchValue.ToUpper().Trim();
                            meetings = meetings.Where(x => (!string.IsNullOrEmpty(x.candidateName) ? x.candidateName.ToUpper().Contains(searchValue) : x.candidateName == "") || (!string.IsNullOrEmpty(x.interviewer) ? x.interviewer.ToUpper().Contains(searchValue) : x.interviewer == "") || (!string.IsNullOrEmpty(x.ClientHrContact) ? x.ClientHrContact.ToUpper().Contains(searchValue) : x.ClientHrContact == "")).ToList();

                        }
                        ViewBag.TotalCount = meetings.Count();

                        ViewBag.PageStartCount = 10 * ((Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page)) - 1) + 1;
                        int a = meetings.Count() / 10;
                        if (meetings.Count() % 10 == 0)
                        {
                            ViewBag.PageEndCount = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
                        }
                        else
                        {
                            a = a + 1;
                            if (a == Page)
                            {
                                ViewBag.PageEndCount = meetings.Count();
                            }
                            else
                            {
                                ViewBag.PageEndCount = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));

                            }

                        }
                        if (meetings.Count() == 0)
                        {
                            ViewBag.PageStartCount = 0;
                            ViewBag.PageEndCount = 0;
                        }
                        _modelDetail = (List<Meeting>)meetings;
                        return View(meetings.ToPagedList(Page ?? 1, 10));

                        //  return View(myInterview);
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
        public async Task<IActionResult> HRAssignYourself(int id, string InterviewDate)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "HR")
                    {
                        var result = _meetingService.HRAssignYourself(id, emailid.ToLower());
                        TempData["SuccessErrorMessageHR"] = result;
                        ViewBag.Message = result;
                        return RedirectToAction("OpenInterviews");
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
                return RedirectToAction("OpenInterviews");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ResetHROpenInterviewFilter()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("OpenHRfilters");
                    return RedirectToAction("OpenInterviews");
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

        [HttpPost]
        public JsonResult HROpenInterviewJson(string searchValue)
        {
            try
            {
                int Page = 1;
                var meetings = _modelDetail;
                Searchvalue = searchValue;
                if (!string.IsNullOrEmpty(searchValue))
                {
                    searchValue = searchValue.ToUpper().Trim();
                    meetings = meetings.Where(x => (!string.IsNullOrEmpty(x.candidateName) ? x.candidateName.ToUpper().Contains(searchValue) : x.candidateName == "") || (!string.IsNullOrEmpty(x.interviewer) ? x.interviewer.ToUpper().Contains(searchValue) : x.interviewer == "") || (!string.IsNullOrEmpty(x.ClientHrContact) ? x.ClientHrContact.ToUpper().Contains(searchValue) : x.ClientHrContact == "")).ToList();

                }
                int totalcount = meetings.Count();
                int Pagestart = 0;
                int Pageend = 0;

                Pagestart = 10 * ((Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page)) - 1) + 1;
                int a = meetings.Count() / 10;
                if (meetings.Count() % 10 == 0)
                {
                    Pageend = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
                }
                else
                {
                    a = a + 1;
                    if (a == Page)
                    {
                        Pageend = meetings.Count();
                    }
                    else
                    {
                        Pageend = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
                    }
                }
                if (meetings.Count() == 0)
                {
                    Pagestart = 0;
                    Pageend = 0;
                }
                return Json(new { result = meetings.Take(10), startPage = Pagestart, endPage = Pageend, totalPage = a, totalentry = meetings.Count });
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("failed");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MyInterviews(int? Page, string searchValue)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "HR")
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        var myInterview = _meetingService.GetHRAssignedInterviews(emailid);
                        if (HttpContext.Session.GetString("HRfilters") != null)
                        {
                            var filter = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("HRfilters"));

                            if (filter.StartDate != null && filter.EndDate != null)
                            {
                                myInterview = myInterview.Where(t => t.interviewDate.Date >= filter.StartDate && t.interviewDate.Date <= filter.EndDate).ToList();
                            }
                            else if (filter.StartDate != null && filter.EndDate == null)
                            {
                                myInterview = myInterview.Where(t => t.interviewDate.Date >= filter.StartDate).ToList();
                            }
                            else if (filter.StartDate == null && filter.EndDate != null)
                            {

                                myInterview = myInterview.Where(t => t.interviewDate.Date <= filter.EndDate).ToList();

                            }

                            filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = myInterview.Select(x => x.JDTiltle.Split("|")[0]).ToArray() : filter.PrimarySkill = filter.PrimarySkill;
                            filter.Interviewer = filter.Interviewer == null ? filter.Interviewer = myInterview.Select(x => x.interviewer).ToArray() : filter.Interviewer = filter.Interviewer;
                            filter.ClientName = filter.ClientName == null ? filter.ClientName = myInterview.Select(x => x.ClientName).ToArray() : filter.ClientName = filter.ClientName;
                            filter.Client = filter.Client == null ? filter.Client = myInterview.Select(x => x.ClientCompName).ToArray() : filter.Client = filter.Client;
                            filter.RFID = filter.RFID == null ? filter.RFID = myInterview.Select(x => x.IRFID).ToArray() : filter.RFID = filter.RFID;
                            filter.ClientHR = filter.ClientHR == null ? filter.ClientHR = myInterview.Select(x => x.CandidateClientHR).ToArray() : filter.ClientHR = filter.ClientHR;

                            myInterview = myInterview.Where(c => filter.PrimarySkill != null && filter.PrimarySkill.Contains(c.JDTiltle.Split("|")[0])
                            && filter.RFID.Contains(c.IRFID)
                            && ((filter.ClientHR != null && filter.ClientHR.Count() > 0) ? filter.ClientHR.Contains(c.CandidateClientHR) : c.CandidateClientHR == null)
                            && ((filter.ClientName != null && filter.ClientName.Count() > 0) ? filter.ClientName.Contains(c.ClientName) : c.ClientName == null)
                            && ((filter.Client != null && filter.Client.Count() > 0) ? filter.Client.Contains(c.ClientCompName) : c.ClientCompName == null)
                            && ((filter.Interviewer != null && filter.Interviewer.Count() > 0) ? filter.Interviewer.Contains(c.interviewer) : c.interviewer == null)).ToList();

                        }
                        else
                        {
                            myInterview = myInterview.Where(x => x.interviewDate.Date >= DateTime.Now.Date).ToList();

                        }
                        myInterview = myInterview.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); x.MeetingStatusDesc = ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription(); x.intervieweStatus = ((InterviewStatus)Convert.ToInt32(x.intervieweStatus)).GetDescription(); x.QCStatusDesc = ((QCStatus)Convert.ToInt32(x.QCStatus)).GetDescription(); return x; }).ToList();
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

                        }
                        if (TempData["SuccessErrorMessageHR"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageHR"].ToString();
                            TempData.Remove("SuccessErrorMessageHR");
                        }

                        if (!string.IsNullOrEmpty(searchValue))
                        {
                            searchValue = searchValue.ToUpper().Trim();
                            myInterview = myInterview.Where(x => (!string.IsNullOrEmpty(x.candidateName) ? x.candidateName.ToUpper().Contains(searchValue) : x.candidateName == "") || (!string.IsNullOrEmpty(x.interviewer) ? x.interviewer.ToUpper().Contains(searchValue) : x.interviewer == "") || (!string.IsNullOrEmpty(x.candidateEmail) ? x.candidateEmail.ToUpper().Contains(searchValue) : x.candidateEmail == "")).ToList();

                        }
                        ViewBag.TotalCount = myInterview.Count();

                        ViewBag.PageStartCount = 10 * ((Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page)) - 1) + 1;
                        int a = myInterview.Count() / 10;
                        if (myInterview.Count() % 10 == 0)
                        {
                            ViewBag.PageEndCount = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
                        }
                        else
                        {
                            a = a + 1;
                            if (a == Page)
                            {
                                ViewBag.PageEndCount = myInterview.Count();
                            }
                            else
                            {
                                ViewBag.PageEndCount = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));

                            }

                        }
                        if (myInterview.Count() == 0)
                        {
                            ViewBag.PageStartCount = 0;
                            ViewBag.PageEndCount = 0;
                        }
                        _modelDetail = (List<Meeting>)myInterview;
                        return View(myInterview.ToPagedList(Page ?? 1, 10));

                        // return View(myInterview);
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
        public async Task<IActionResult> MyInterviews(InterviewFilter filter, int? Page, string searchValue)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (filter != null)
                    {
                        HttpContext.Session.SetString("HRfilters", JsonConvert.SerializeObject(filter));
                    }
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "HR")
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        var myInterview = _meetingService.GetHRAssignedInterviews(emailid);

                        if (filter.StartDate != null && filter.EndDate != null)
                        {
                            myInterview = myInterview.Where(t => t.interviewDate.Date >= filter.StartDate && t.interviewDate.Date <= filter.EndDate).ToList();
                        }
                        else if (filter.StartDate != null && filter.EndDate == null)
                        {
                            myInterview = myInterview.Where(t => t.interviewDate.Date >= filter.StartDate).ToList();
                        }
                        else if (filter.StartDate == null && filter.EndDate != null)
                        {

                            myInterview = myInterview.Where(t => t.interviewDate.Date <= filter.EndDate).ToList();

                        }

                        filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = myInterview.Select(x => x.JDTiltle.Split("|")[0]).ToArray() : filter.PrimarySkill = filter.PrimarySkill;
                        filter.Interviewer = filter.Interviewer == null ? filter.Interviewer = myInterview.Select(x => x.interviewer).ToArray() : filter.Interviewer = filter.Interviewer;
                        filter.ClientName = filter.ClientName == null ? filter.ClientName = myInterview.Select(x => x.ClientName).ToArray() : filter.ClientName = filter.ClientName;
                        filter.Client = filter.Client == null ? filter.Client = myInterview.Select(x => x.ClientCompName).ToArray() : filter.Client = filter.Client;
                        filter.RFID = filter.RFID == null ? filter.RFID = myInterview.Select(x => x.IRFID).ToArray() : filter.RFID = filter.RFID;
                        filter.ClientHR = filter.ClientHR == null ? filter.ClientHR = myInterview.Select(x => x.CandidateClientHR).ToArray() : filter.ClientHR = filter.ClientHR;

                        myInterview = myInterview.Where(c => filter.PrimarySkill != null && filter.PrimarySkill.Contains(c.JDTiltle.Split("|")[0])
                        && filter.RFID.Contains(c.IRFID)
                        && ((filter.ClientHR != null && filter.ClientHR.Count() > 0) ? filter.ClientHR.Contains(c.CandidateClientHR) : c.CandidateClientHR == null)
                        && ((filter.ClientName != null && filter.ClientName.Count() > 0) ? filter.ClientName.Contains(c.ClientName) : c.ClientName == null)
                        && ((filter.Client != null && filter.Client.Count() > 0) ? filter.Client.Contains(c.ClientCompName) : c.ClientCompName == null)
                        && ((filter.Interviewer != null && filter.Interviewer.Count() > 0) ? filter.Interviewer.Contains(c.interviewer) : c.interviewer == null)).ToList();

                        myInterview = myInterview.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); x.MeetingStatusDesc = ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription(); x.intervieweStatus = ((InterviewStatus)Convert.ToInt32(x.intervieweStatus)).GetDescription(); x.QCStatusDesc = ((QCStatus)Convert.ToInt32(x.QCStatus)).GetDescription(); return x; }).ToList();
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

                        }

                        if (TempData["SuccessErrorMessageHR"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageHR"].ToString();
                            TempData.Remove("SuccessErrorMessageHR");
                        }

                        if (!string.IsNullOrEmpty(searchValue))
                        {
                            searchValue = searchValue.ToUpper().Trim();
                            myInterview = myInterview.Where(x => (!string.IsNullOrEmpty(x.candidateName) ? x.candidateName.ToUpper().Contains(searchValue) : x.candidateName == "") || (!string.IsNullOrEmpty(x.interviewer) ? x.interviewer.ToUpper().Contains(searchValue) : x.interviewer == "") || (!string.IsNullOrEmpty(x.candidateEmail) ? x.candidateEmail.ToUpper().Contains(searchValue) : x.candidateEmail == "")).ToList();

                        }
                        ViewBag.TotalCount = myInterview.Count();

                        ViewBag.PageStartCount = 10 * ((Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page)) - 1) + 1;
                        int a = myInterview.Count() / 10;
                        if (myInterview.Count() % 10 == 0)
                        {
                            ViewBag.PageEndCount = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
                        }
                        else
                        {
                            a = a + 1;
                            if (a == Page)
                            {
                                ViewBag.PageEndCount = myInterview.Count();
                            }
                            else
                            {
                                ViewBag.PageEndCount = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));

                            }

                        }
                        if (myInterview.Count() == 0)
                        {
                            ViewBag.PageStartCount = 0;
                            ViewBag.PageEndCount = 0;
                        }
                        _modelDetail = (List<Meeting>)myInterview;
                        return View(myInterview.ToPagedList(Page ?? 1, 10));

                        //  return View(myInterview);
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
        public async Task<IActionResult> HRUnassignYourself(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var email = HttpContext.Session.GetString("email");
                    email = email.Replace("\"", "");
                    if (profileName == "HR")
                    {
                        if (_meetingService.HRUnassignYourself(id))
                        {
                            return Json(new { url = Url.Action("MyInterviews", "Interview") });

                        }
                        else
                        {
                            return Json(new { url = Url.Action("MyInterviews", "Interview") });
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
                return Json(new { url = Url.Action("MyInterviews", "Interview") });
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ResetHRInterviewFilter()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("HRfilters");
                    return RedirectToAction("MyInterviews");
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

        [HttpPost]
        public JsonResult HRMyInterviewJson(string searchValue)
        {
            try
            {
                int Page = 1;
                var myInterview = _modelDetail;
                Searchvalue = searchValue;
                if (!string.IsNullOrEmpty(searchValue))
                {
                    searchValue = searchValue.ToUpper().Trim();
                    myInterview = myInterview.Where(x => (!string.IsNullOrEmpty(x.candidateName) ? x.candidateName.ToUpper().Contains(searchValue) : x.candidateName == "") || (!string.IsNullOrEmpty(x.interviewer) ? x.interviewer.ToUpper().Contains(searchValue) : x.interviewer == "") || (!string.IsNullOrEmpty(x.candidateEmail) ? x.candidateEmail.ToUpper().Contains(searchValue) : x.candidateEmail == "")).ToList();
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
                return Json(new { result = myInterview.Take(10), startPage = Pagestart, endPage = Pageend, totalPage = a, totalentry = myInterview.Count });
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("failed");
            }
        }


        [Authorize]
        [HttpGet]
        public FileContentResult HRInterviewsExporttoExcel()
        {
            string userEmailId = string.Empty;
            ExcelPackage Ep = new ExcelPackage();
            ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("My Interviews");
            string fileName = "Risebird" + DateTime.Now.ToString("dd/MM/yyyy") + "_Interviews" + ".xlsx";
            try
            {

                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");


                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                var myInterview = _meetingService.GetHRAssignedInterviews(emailid);
                if (HttpContext.Session.GetString("HRfilters") != null)
                {
                    var filter = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("HRfilters"));

                    if (filter.StartDate != null && filter.EndDate != null)
                    {
                        myInterview = myInterview.Where(t => t.interviewDate.Date >= filter.StartDate && t.interviewDate.Date <= filter.EndDate).ToList();
                    }
                    else if (filter.StartDate != null && filter.EndDate == null)
                    {
                        myInterview = myInterview.Where(t => t.interviewDate.Date >= filter.StartDate).ToList();
                    }
                    else if (filter.StartDate == null && filter.EndDate != null)
                    {

                        myInterview = myInterview.Where(t => t.interviewDate.Date <= filter.EndDate).ToList();

                    }

                    filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = myInterview.Select(x => x.JDTiltle.Split("|")[0]).ToArray() : filter.PrimarySkill = filter.PrimarySkill;
                    filter.Interviewer = filter.Interviewer == null ? filter.Interviewer = myInterview.Select(x => x.interviewer).ToArray() : filter.Interviewer = filter.Interviewer;
                    filter.ClientName = filter.ClientName == null ? filter.ClientName = myInterview.Select(x => x.ClientName).ToArray() : filter.ClientName = filter.ClientName;
                    filter.Client = filter.Client == null ? filter.Client = myInterview.Select(x => x.ClientCompName).ToArray() : filter.Client = filter.Client;
                    filter.RFID = filter.RFID == null ? filter.RFID = myInterview.Select(x => x.IRFID).ToArray() : filter.RFID = filter.RFID;
                    filter.ClientHR = filter.ClientHR == null ? filter.ClientHR = myInterview.Select(x => x.CandidateClientHR).ToArray() : filter.ClientHR = filter.ClientHR;

                    myInterview = myInterview.Where(c => filter.PrimarySkill != null && filter.PrimarySkill.Contains(c.JDTiltle.Split("|")[0])
                    && filter.RFID.Contains(c.IRFID)
                    && ((filter.ClientHR != null && filter.ClientHR.Count() > 0) ? filter.ClientHR.Contains(c.CandidateClientHR) : c.CandidateClientHR == null)
                    && ((filter.ClientName != null && filter.ClientName.Count() > 0) ? filter.ClientName.Contains(c.ClientName) : c.ClientName == null)
                    && ((filter.Client != null && filter.Client.Count() > 0) ? filter.Client.Contains(c.ClientCompName) : c.ClientCompName == null)
                    && ((filter.Interviewer != null && filter.Interviewer.Count() > 0) ? filter.Interviewer.Contains(c.interviewer) : c.interviewer == null)).ToList();


                }
                else
                {
                    myInterview = myInterview.Where(x => x.interviewDate.Date >= DateTime.Now.Date).ToList();

                }
                myInterview = myInterview.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();
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

                }
                var modelList = myInterview;
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
                Sheet.Cells[1, 1].Value = "Date";
                Sheet.Cells[1, 2].Value = "Slot";
                Sheet.Cells[1, 3].Value = "Candidate Name";
                Sheet.Cells[1, 4].Value = "Interviewer";
                Sheet.Cells[1, 5].Value = "Candidate Email";
                Sheet.Cells[1, 6].Value = "Primary Skill";
                Sheet.Cells[1, 7].Value = "Resume";
                Sheet.Cells[1, 8].Value = "JD Link";
                Sheet.Cells[1, 9].Value = "Client Hr";
                Sheet.Cells[1, 10].Value = "QCInterviewer";
                Sheet.Cells[1, 11].Value = "HR Comment";
                Sheet.Cells[1, 12].Value = "Client";
                Sheet.Cells[1, 13].Value = "End Client Name";
                Sheet.Cells[1, 14].Value = "Emergency Interview";

                //ExcelHyperLink excelHyperLink = new ExcelHyperLink();
                int i = 2;
                //string LastAction = null; ;

                if (modelList.Count() > 0)
                {
                    foreach (var item in modelList)
                    {
                        Sheet.Cells[i, 1].Value = item.interviewDate.ToString("MM/dd/yyyy HH:mm"); ;
                        Sheet.Cells[i, 2].Value = item.interviewSlot ?? string.Empty;
                        Sheet.Cells[i, 3].Value = item.candidateName ?? string.Empty;
                        Sheet.Cells[i, 4].Value = item.interviewer ?? string.Empty;
                        Sheet.Cells[i, 5].Value = item.candidateEmail ?? string.Empty;
                        Sheet.Cells[i, 6].Value = item.JDTiltle.Split("|")[0];
                        if (!string.IsNullOrEmpty(item.resumeLink))
                        {
                            try
                            {
                                Sheet.Cells[i, 7].Hyperlink = new Uri(item.resumeLink, UriKind.Absolute);
                                Sheet.Cells[i, 7].Value = "Resume";
                                Sheet.Cells[i, 7].Style.Font.UnderLine = true;
                                Sheet.Cells[i, 7].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                            }
                            catch (Exception)
                            {
                                Sheet.Cells[i, 7].Value = "No Resume";
                            }
                        }
                        else
                        {
                            Sheet.Cells[i, 7].Value = "No Resume";
                        }
                        if (!string.IsNullOrEmpty(item.JDLink))
                        {
                            try
                            {
                                Sheet.Cells[i, 8].Hyperlink = new Uri(item.JDLink, UriKind.Absolute);
                                Sheet.Cells[i, 8].Value = "JD Link";
                                Sheet.Cells[i, 8].Style.Font.UnderLine = true;
                                Sheet.Cells[i, 8].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                            }
                            catch (Exception)
                            {
                                Sheet.Cells[i, 8].Value = "No Link";
                            }
                        }
                        else
                        {
                            Sheet.Cells[i, 8].Value = "No Link";
                        }
                        Sheet.Cells[i, 9].Value = item.CandidateClientHR ?? string.Empty;
                        Sheet.Cells[i, 10].Value = item.QCInterviewer ?? string.Empty;
                        Sheet.Cells[i, 11].Value = item.HRComment ?? string.Empty;
                        Sheet.Cells[i, 12].Value = item.ClientCompName ?? string.Empty;
                        Sheet.Cells[i, 13].Value = item.ClientName ?? string.Empty;
                        Sheet.Cells[i, 14].Value = item.IsEmergency ? "Yes" : "No";

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
        public async Task<bool> SaveHRComment()
        {
            try
            {
                var loggedInUser = HttpContext.Session.GetString("email");
                loggedInUser = loggedInUser.Replace("\"", "").Trim();

                var nvc = Request.Form;
                string comment = string.Empty;
                string candidateDetails = string.Empty;
                int id = 0;
                if (!string.IsNullOrEmpty(nvc["id"]))
                {
                    id = Convert.ToInt32(nvc["id"]);
                }

                if (!string.IsNullOrEmpty(nvc["comment"]))
                {
                    comment = nvc["comment"];
                }
                if (!string.IsNullOrEmpty(nvc["candidateDetails"]))
                {
                    candidateDetails = nvc["candidateDetails"];
                }

                string uploadedFileURL = string.Empty;

                if (id != 0 && !string.IsNullOrEmpty(comment))
                {
                    if (Request.Form.Files.Count() > 0)
                    {
                        var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(Request.Form.Files[0]);

                        if (fileMimeType == "image/ief" || fileMimeType == "image/jpeg"
                                || fileMimeType == "image/tiff")
                        {
                            var imageFile = Request.Form.Files[0];
                            await _azureStorageHelper.InsertBlob(imageFile.FileName, "images", imageFile);
                            var containerRef = _azureStorageHelper.GetCloudBlobContainer("images");
                            var blob = containerRef.GetBlockBlobReference(imageFile.FileName);
                            uploadedFileURL = blob.Uri.ToString();
                            if (string.IsNullOrEmpty(uploadedFileURL))
                            {
                                TempData["SuccessErrorMessageHR"] = "Error uploading the image!";
                                return false;
                            }
                        }

                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(nvc["uploadedFileURL"]))
                        {
                            uploadedFileURL = nvc["uploadedFileURL"];
                        }
                    }
                    var result = await _meetingService.SaveHRComment(id, comment, uploadedFileURL, loggedInUser, candidateDetails);
                    if (result)
                    {
                        TempData["SuccessErrorMessageHR"] = "HR comments has been saved Successfully!";

                    }
                    else
                        TempData["SuccessErrorMessageHR"] = "Failed to save HR comments!";

                    return result;
                }
                else
                {
                    TempData["SuccessErrorMessageHR"] = "Failed to save HR comments!";
                    return false;
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }




        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> CallCatalogue(string type)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.type = "Candidate";
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        if (TempData["SuccessErrorMessageCandidate"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageCandidate"].ToString();
                            TempData.Remove("SuccessErrorMessageCandidate");
                        }
                        var meetings = new List<CallCatalogueVM>();
                        InterviewFilter filter = null;
                        if (HttpContext.Session.GetString("CallCataloguefilters") != null)
                            filter = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("CallCataloguefilters"));

                        if (type == "Interviewer")
                        {
                            ViewBag.type = "Interviewer";
                            meetings = _meetingService.GetInterviewerCallCatalogueList(filter);
                        }
                        else
                        {
                            meetings = _meetingService.GetCallCatalogueList(profileName, emailid, filter);
                        }

                        if (filter != null)
                            filter = _meetingService.PopulateCallCatalogueFilterFacets(meetings, filter);
                        ViewBag.profile = profileName;
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
                return RedirectToAction("SignOut", "Session");
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CallCatalogue(InterviewFilter filter, string type)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.profile = profileName;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        if (filter != null)
                        {
                            HttpContext.Session.SetString("CallCataloguefilters", JsonConvert.SerializeObject(filter));
                        }
                        return RedirectToAction("CallCatalogue", new { type = type });
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

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ResetCallCatalogueFilter()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("CallCataloguefilters");
                    return RedirectToAction("CallCatalogue");
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
        public FileContentResult CallCatalogueExporttoExcel()
        {

            ExcelPackage Ep = new ExcelPackage();
            ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("CallCatalogue");
            string fileName = "Risebird_" + DateTime.Now.ToString("dd/MM/yyyy") + "_CallCatalogue" + ".xlsx";
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {

                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        InterviewFilter filter = null;
                        if (HttpContext.Session.GetString("CallCataloguefilters") != null)
                            filter = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("CallCataloguefilters"));

                        var meetings = _meetingService.GetCallCatalogueList(profileName, emailid, filter);
                        if (filter != null)
                            filter = _meetingService.PopulateCallCatalogueFilterFacets(meetings, filter);
                        if (meetings != null && meetings.Count() > 0)
                        {
                            Sheet.Cells["A1:AI1"].Style.Font.Bold = true;
                            Sheet.Cells["A1:AI1"].Style.Font.Size = 12;
                            Sheet.Cells["A1:AI1"].Style.WrapText = true;
                            //Sheet.Column(1).Width = 30;
                            //Sheet.Column(2).Width = 30;
                            int startColumn = 1; int endColumn = 12;
                            for (int a = startColumn; a <= endColumn; a++)
                            {
                                Sheet.Column(a).Width = 40;
                            }
                            Sheet.Cells[1, 1].Value = "Skill Channel";
                            Sheet.Cells[1, 2].Value = "Candidate Name";
                            Sheet.Cells[1, 3].Value = "Candidate Email";
                            Sheet.Cells[1, 4].Value = "JD Title";
                            Sheet.Cells[1, 5].Value = "Client";
                            Sheet.Cells[1, 6].Value = "Last Called At";
                            Sheet.Cells[1, 7].Value = "Last Called By";

                            //ExcelHyperLink excelHyperLink = new ExcelHyperLink();
                            int i = 2;

                            foreach (var item in meetings)
                            {
                                Sheet.Cells[i, 1].Value = item.IRFId;
                                Sheet.Cells[i, 2].Value = item.CandidateName;
                                Sheet.Cells[i, 3].Value = item.CandidateEmail;
                                Sheet.Cells[i, 4].Value = item.JDTiltle;
                                Sheet.Cells[i, 5].Value = item.ClientName;
                                Sheet.Cells[i, 6].Value = item.CalledOn.ToString("dddd, dd MMMM yyyy HH:mm:ss");
                                Sheet.Cells[i, 7].Value = item.WhoCalled;

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
                        Rng.Value = "No data available in table !!!";
                        return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                    }
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
        public async Task<IActionResult> CallRecordingsDetails(string type)
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
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "SeniorInterviewerManagement" || profileName == "InterviewerManagement")
                    {
                        if (string.IsNullOrEmpty(type))
                        {
                            type = "Candidate";
                        }

                        ViewBag.type = type;

                        //if (TempData["SuccessErrorMessageCandidate"] != null)
                        //{
                        //    ViewBag.Message = TempData["SuccessErrorMessageCandidate"].ToString();
                        //    TempData.Remove("SuccessErrorMessageCandidate");
                        //}
                        //var recordings = new List<CallRecordingDataVM>();
                        IEnumerable<CallRecordingDataVM> recordings;
                        //if (type == "Interviewer")
                        //{
                        //    ViewBag.type = "Interviewer";
                        //    recordings = (await _meetingService.GetCallRecordingData(profileName, emailid)).Where(x => x.RecipientProfileType?.ToLower() == "interviewer");
                        //}
                        //else
                        //{
                        //    recordings = (await _meetingService.GetCallRecordingData(profileName, emailid)).Where(x => x.RecipientProfileType?.ToLower() == "interviewer");
                        //}
                        recordings = await _meetingService.GetCallRecordingData(profileName, emailid);
                        if (type == "Candidate" || type == "Interviewer")
                        {
                            recordings = recordings?.Where(x => x.RecipientProfileType?.ToLower() == type.ToLower());
                        }
                        else
                        {
                            recordings = recordings.Where(x => x.RecipientProfileType?.ToLower() != "candidate" && x.RecipientProfileType?.ToLower() != "interviewer");

                        }

                        if (HttpContext.Session.GetString("CallRecordingsfilters") != null)
                        {
                            var filter = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("CallRecordingsfilters"));
                            if (filter.StartDate != null && filter.EndDate != null)
                            {
                                filter.EndDate = Convert.ToDateTime(filter.EndDate).AddDays(1);
                                recordings = recordings.Where(t => t.DateCreated >= filter.StartDate && t.DateCreated <= filter.EndDate).ToList();
                            }
                            else if (filter.StartDate != null && filter.EndDate == null)
                            {
                                recordings = recordings.Where(t => t.DateCreated >= filter.StartDate).ToList();
                            }
                            else if (filter.StartDate == null && filter.EndDate != null)
                            {
                                filter.EndDate = Convert.ToDateTime(filter.EndDate).AddDays(1);
                                recordings = recordings.Where(t => t.DateCreated <= filter.EndDate).ToList();
                            }

                        }
                        else
                        {
                            recordings = recordings.Where(x => x.DateCreated > DateTime.Now.AddDays(-7));
                        }
                        recordings = recordings.Where(x => x.DateCreated > DateTime.Now.AddDays(-7));
                        recordings = FillMissingRecordingUris(recordings);
                        ViewBag.profile = profileName;
                        return View(recordings);
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

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CallRecordingsDetails(InterviewFilter filter, string type)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.profile = profileName;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "SeniorInterviewerManagement" || profileName == "InterviewerManagement")
                    {
                        if (filter != null)
                        {
                            HttpContext.Session.SetString("CallRecordingsfilters", JsonConvert.SerializeObject(filter));
                        }
                        return RedirectToAction("CallRecordingsDetails", new { type = type });
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

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ResetCallRecordingsDetails(string type)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("CallRecordingsfilters");
                    return RedirectToAction("CallRecordingsDetails", new { type = type });
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

        // ----------------------------
        // Call Analytics: calls per Risebird user, split by recipient type.
        // Same data and role gate as CallRecordingsDetails; defaults to the last 30 days.
        // ----------------------------
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> CallAnalytics(string type, DateTime? from, DateTime? to)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim().Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "SeniorInterviewerManagement" || profileName == "InterviewerManagement")
                    {
                        var calls = await GetCallAnalyticsData(profileName, emailid, type, from, to);

                        var summary = calls
                            .GroupBy(x => x.Caller ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                            .Select(g => new CallAnalyticsSummaryVM
                            {
                                Caller = g.Key,
                                TotalCalls = g.Count(),
                                CandidateCalls = g.Count(x => x.RecipientProfileType?.ToLower() == "candidate"),
                                InterviewerCalls = g.Count(x => x.RecipientProfileType?.ToLower() == "interviewer"),
                                OtherCalls = g.Count(x => x.RecipientProfileType?.ToLower() != "candidate" && x.RecipientProfileType?.ToLower() != "interviewer"),
                                CallsWithRecording = g.Count(x => !string.IsNullOrEmpty(x.RecordingUri)),
                                TotalDurationSeconds = g.Sum(x => ParseDurationSeconds(x.Duration)),
                                LastCallOn = g.Max(x => (DateTime?)x.DateCreated)
                            })
                            .OrderByDescending(x => x.TotalCalls)
                            .ToList();

                        ViewBag.Profile = profileName;
                        return View(summary);
                    }
                }
                return RedirectToAction("SignOut", "Session");
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> CallAnalyticsByUser(string caller, string type, DateTime? from, DateTime? to)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim().Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "SeniorInterviewerManagement" || profileName == "InterviewerManagement")
                    {
                        var calls = (await GetCallAnalyticsData(profileName, emailid, type, from, to))
                            .Where(x => string.Equals(x.Caller ?? string.Empty, caller ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                            .OrderByDescending(x => x.DateCreated)
                            .ToList();

                        ViewBag.Profile = profileName;
                        ViewBag.Caller = caller;
                        return View(calls);
                    }
                }
                return RedirectToAction("SignOut", "Session");
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }

        // Loads call rows and applies the recipient-type and date filters; sets ViewBag.Type/From/To for the filter form.
        private async Task<List<CallRecordingDataVM>> GetCallAnalyticsData(string profileName, string emailid, string type, DateTime? from, DateTime? to)
        {
            if (type != "Candidate" && type != "Interviewer" && type != "Other")
            {
                type = "All";
            }
            var fromDate = (from ?? DateTime.Today.AddDays(-30)).Date;
            var toDate = (to ?? DateTime.Today).Date;

            ViewBag.Type = type;
            ViewBag.From = fromDate.ToString("yyyy-MM-dd");
            ViewBag.To = toDate.ToString("yyyy-MM-dd");

            var recordings = await _meetingService.GetCallRecordingData(profileName, emailid) ?? Enumerable.Empty<CallRecordingDataVM>();
            recordings = recordings.Where(x => x.DateCreated >= fromDate && x.DateCreated < toDate.AddDays(1));

            if (type == "Candidate" || type == "Interviewer")
            {
                recordings = recordings.Where(x => x.RecipientProfileType?.ToLower() == type.ToLower());
            }
            else if (type == "Other")
            {
                recordings = recordings.Where(x => x.RecipientProfileType?.ToLower() != "candidate" && x.RecipientProfileType?.ToLower() != "interviewer");
            }
            return FillMissingRecordingUris(recordings);
        }

        // Twilio calls since the RecordingUri job was switched off have a RecordingSid ('RE...') but no
        // RecordingUri. Build the playable link from the sid (same URL User/FetchRecordings uses) so the
        // Call Recordings / Call Analytics pages can play them. Display only — nothing is written back.
        private List<CallRecordingDataVM> FillMissingRecordingUris(IEnumerable<CallRecordingDataVM> recordings)
        {
            var list = recordings?.ToList() ?? new List<CallRecordingDataVM>();
            var configuration = HttpContext?.RequestServices?.GetService(typeof(Microsoft.Extensions.Configuration.IConfiguration))
                                as Microsoft.Extensions.Configuration.IConfiguration;
            var accountSid = configuration?["TwilioSettings:ACCOUNT_SID"];
            if (string.IsNullOrWhiteSpace(accountSid))
            {
                return list;
            }

            foreach (var item in list)
            {
                if (string.IsNullOrWhiteSpace(item.RecordingUri)
                    && !string.IsNullOrWhiteSpace(item.RecordingSid)
                    && item.RecordingSid.StartsWith("RE", StringComparison.Ordinal))
                {
                    item.RecordingUri = $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Recordings/{item.RecordingSid.Trim()}.mp3";
                }
            }
            return list;
        }

        // Duration is stored as text, e.g. "125" or "00:02:05".
        public static int ParseDurationSeconds(string duration)
        {
            if (string.IsNullOrWhiteSpace(duration))
            {
                return 0;
            }
            if (double.TryParse(duration.Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var seconds))
            {
                return seconds > 0 ? (int)Math.Round(seconds) : 0;
            }
            if (TimeSpan.TryParse(duration.Trim(), System.Globalization.CultureInfo.InvariantCulture, out var span))
            {
                return span.TotalSeconds > 0 ? (int)span.TotalSeconds : 0;
            }
            return 0;
        }
    }
}
