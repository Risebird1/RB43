using FileTypeInterrogator;
using iTextSharp.text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using OfficeOpenXml.FormulaParsing.Excel.Functions.RefAndLookup;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using RestSharp;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Web.Controllers
{
    public class InterviewSchedulerController : Controller
    {
        private readonly IInterviewSchedulerService _interviewSchedulerservice;
        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly ISASUrlHelper _sasUrlHelper;
        private readonly IConfiguration _configuration;
        private readonly IRisebirdMeetingsRepository _risebirdMeetings;
        private readonly INotificationHelper _notificationHelper;
        private readonly IRBMeetHelper _rBMeetHelper;

        public InterviewSchedulerController(IInterviewSchedulerService interviewSchedulerservice, IAzureStorageHelper azureStorageHelper, ISASUrlHelper sasUrlHelper, IConfiguration configuration, IRisebirdMeetingsRepository risebirdMeetings, INotificationHelper notificationHelper, IRBMeetHelper rBMeetHelper)
        {
            _interviewSchedulerservice = interviewSchedulerservice;
            _azureStorageHelper = azureStorageHelper;
            _sasUrlHelper = sasUrlHelper;
            _configuration = configuration;
            _risebirdMeetings = risebirdMeetings;
            _notificationHelper = notificationHelper;
            _rBMeetHelper = rBMeetHelper;
        }
        public IActionResult Index()
        {
            return View();
        }
        #region Scheduling Confirmation Page
        public IActionResult SchedulerList()
        {
            var model = new List<daytimes_report>();
            model = _interviewSchedulerservice.FetchEventList();
            foreach (var item in model)
            {
                item.EventUrl = "https://" + Request.Host.ToString() + "/InterviewScheduler/ScheduleEvent?id=" + item.Uniqueid;
            }
            return View(model);
        }
        public IActionResult AddEvent()
        {
            var model_Second = new List<daytimes_second>();
            var sun = new List<perdaytime>();
            var mon = new List<perdaytime>();
            sun.Add(new perdaytime { Start = "8", End = "23" });
            mon.Add(new perdaytime { Start = "6", End = "9" });
            mon.Add(new perdaytime { Start = "18", End = "23" });
            model_Second.Add(new daytimes_second { Daytype = "sunday", dayCheck = true, dayvalue = sun });
            model_Second.Add(new daytimes_second { Daytype = "monday", dayCheck = true, dayvalue = mon });
            model_Second.Add(new daytimes_second { Daytype = "tuesday", dayCheck = true, dayvalue = mon });
            model_Second.Add(new daytimes_second { Daytype = "wednesday", dayCheck = true, dayvalue = mon });
            model_Second.Add(new daytimes_second { Daytype = "thursday", dayCheck = true, dayvalue = mon });
            model_Second.Add(new daytimes_second { Daytype = "friday", dayCheck = true, dayvalue = mon });
            model_Second.Add(new daytimes_second { Daytype = "saturday", dayCheck = true, dayvalue = sun });
            var model_report = new daytimes_report();
            model_report.setvalue = model_Second;
            model_report.StartDate = DateTime.Now.AddHours(12);
            model_report.EndDate = DateTime.Now.AddDays(60);
            model_report.Concurrency = 10;
            SetDropDownTime(model_report);
            return View(model_report);
        }
        [HttpPost]
        public IActionResult AddEvent(daytimes_report model)
        {
            var result = _interviewSchedulerservice.CreateEvent(model);
            if (result)
            {
                return RedirectToAction("SchedulerList");
            }
            else
            {
                SetDropDownTime(model);
                return View(model);
            }
        }
        public IActionResult EditEvent(int eventid, int eventtype)
        {
            var model = _interviewSchedulerservice.FetchEventById(eventid);
            if (model != null)
            {
                var timeslots = _interviewSchedulerservice.FetchTimeslotsByEventId(eventid);
                var daytypes = (from data in timeslots
                                select new daytimes_second { Daytype = data.Daytype, dayCheck = data.dayCheck }).ToList();
                var model_Second = new List<daytimes_second>();
                foreach (var itemday in daytypes.Select(x => x.Daytype).Distinct())
                {
                    var sun = new List<perdaytime>();
                    foreach (var item in timeslots)
                    {
                        if (itemday == item.Daytype)
                        {
                            sun.Add(new perdaytime { Start = item.Start, End = item.End });
                        }
                    }
                    model_Second.Add(new daytimes_second { Daytype = itemday, dayCheck = daytypes.Where(x => x.Daytype == itemday).Select(x => x.dayCheck).FirstOrDefault(), dayvalue = sun });
                }
                model.setvalue = model_Second;
            }
            SetDropDownTime(model);
            model.Eventtype = eventtype;
            return View(model);
        }
        [HttpPost]
        public IActionResult EditEvent(daytimes_report model)
        {
            var result = _interviewSchedulerservice.CreateEvent(model);
            if (result)
            {
                if (model.Eventtype == 1)
                {
                    return RedirectToAction("Index", "IRF");
                }
                else
                {
                    return RedirectToAction("SchedulerList");
                }
            }
            else
            {
                SetDropDownTime(model);
                return View(model);
            }
        }
        public IActionResult DeactivateEvent(int eventid)
        {
            if (_interviewSchedulerservice.DeactivateEventById(eventid, 0))
            {

            }
            else
            {

            }
            return RedirectToAction("SchedulerList");
        }
        public IActionResult ActivateEvent(int eventid)
        {
            if (_interviewSchedulerservice.DeactivateEventById(eventid, 1))
            {

            }
            else
            {

            }
            return RedirectToAction("SchedulerList");
        }
        public void SetDropDownTime(daytimes_report model)
        {
            var timeslot = new List<TimeslotsVM>();
            timeslot.Add(new TimeslotsVM { id = 1, Name = "1:00" });
            timeslot.Add(new TimeslotsVM { id = 2, Name = "2:00" });
            timeslot.Add(new TimeslotsVM { id = 3, Name = "3:00" });
            timeslot.Add(new TimeslotsVM { id = 4, Name = "4:00" });
            timeslot.Add(new TimeslotsVM { id = 5, Name = "5:00" });
            timeslot.Add(new TimeslotsVM { id = 6, Name = "6:00" });
            timeslot.Add(new TimeslotsVM { id = 7, Name = "7:00" });
            timeslot.Add(new TimeslotsVM { id = 8, Name = "8:00" });
            timeslot.Add(new TimeslotsVM { id = 9, Name = "9:00" });
            timeslot.Add(new TimeslotsVM { id = 10, Name = "10:00" });
            timeslot.Add(new TimeslotsVM { id = 11, Name = "11:00" });
            timeslot.Add(new TimeslotsVM { id = 12, Name = "12:00" });
            timeslot.Add(new TimeslotsVM { id = 13, Name = "13:00" });
            timeslot.Add(new TimeslotsVM { id = 14, Name = "14:00" });
            timeslot.Add(new TimeslotsVM { id = 15, Name = "15:00" });
            timeslot.Add(new TimeslotsVM { id = 16, Name = "16:00" });
            timeslot.Add(new TimeslotsVM { id = 17, Name = "17:00" });
            timeslot.Add(new TimeslotsVM { id = 18, Name = "18:00" });
            timeslot.Add(new TimeslotsVM { id = 19, Name = "19:00" });
            timeslot.Add(new TimeslotsVM { id = 20, Name = "20:00" });
            timeslot.Add(new TimeslotsVM { id = 21, Name = "21:00" });
            timeslot.Add(new TimeslotsVM { id = 22, Name = "22:00" });
            timeslot.Add(new TimeslotsVM { id = 23, Name = "23:00" });
            timeslot.Add(new TimeslotsVM { id = 24, Name = "24:00" });
            model.timeslots = new SelectList(timeslot, "id", "Name");
            model.IRFList = _interviewSchedulerservice.GetIRFList();
            var IntervalsList = new List<TimeslotsVM>();
            IntervalsList.Add(new TimeslotsVM { id = 30, Name = "30" });
            IntervalsList.Add(new TimeslotsVM { id = 60, Name = "60" });
            model.IntervalList = new SelectList(IntervalsList, "id", "Name");
        }
        #endregion
        #region Schedule Interview
        public async Task<IActionResult> ScheduleEvent(string id)
        {
            var model = new ScheculeInterview();
            var result = _interviewSchedulerservice.FetchDetailsByUniqueId(id);
            ViewBag.id = id;
            bool isPaused = false;
            if (result != null && result.Count() > 0)
            {
                if (!string.IsNullOrEmpty(result.FirstOrDefault().SkillChannel))
                {
                    isPaused = await _interviewSchedulerservice.CheckSkillChannelPaused(result.FirstOrDefault().SkillChannel);
                }
                else if (!string.IsNullOrEmpty(result.FirstOrDefault().SkillChannelRelation))
                {
                    isPaused = await _interviewSchedulerservice.CheckSkillChannelPaused(result.FirstOrDefault().SkillChannelRelation);
                }
                if (!isPaused)
                {
                    model.InterviewChannelId = result.FirstOrDefault().SkillChannel;
                    if (string.IsNullOrEmpty(result.FirstOrDefault().SkillChannel))
                    {
                        model.InterviewChannelId = result.FirstOrDefault().SkillChannelRelation;
                    }
                    model.Uniqueid = result.FirstOrDefault().Uniqueid;
                    ViewBag.Start = 1;
                    model._IRFDetails = _interviewSchedulerservice.fetchIRFClientDetails(model.InterviewChannelId);
                    var _listhr = _interviewSchedulerservice.fetchSkillChannelHRDetails(model.InterviewChannelId);
                    var _staffing = _interviewSchedulerservice.fetchSkillChannelStaffingDetails(model.InterviewChannelId);
                    if (_staffing != null)
                    {
                        model._liststaffing = new SelectList(_staffing);
                    }
                    if (_listhr != null)
                    {
                        model._listhr = new SelectList(_listhr);
                    }
                    if (model._IRFDetails == null)
                    {
                        model._IRFDetails = new ScheculeInterviewIRFVM();
                    }
                    model.ClientName = model._IRFDetails.Name;
                    model.MeetingPlatform = result.FirstOrDefault().MeetingPlatform;
                    var timeZones = TimeZoneInfo.GetSystemTimeZones();
                    List<SelectListItem> items = new List<SelectListItem>();
                    foreach (var tzi in timeZones)
                    {
                        items.Add(new SelectListItem { Text = tzi.DisplayName, Value = tzi.BaseUtcOffset.TotalHours.ToString() });
                    }
                    model.TimeZonesList = items;
                }
                else
                {
                    model._IRFDetails = new ScheculeInterviewIRFVM();
                    ViewBag.Start = 3;
                }
            }
            else
            {
                model._IRFDetails = new ScheculeInterviewIRFVM();
                ViewBag.Start = 3;
            }
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> ScheduleEvent(ScheculeInterview model)
        {
            var timeZones1 = TimeZoneInfo.GetSystemTimeZones();
            List<SelectListItem> items = new List<SelectListItem>();
            foreach (var tzi in timeZones1)
            {
                items.Add(new SelectListItem { Text = tzi.DisplayName, Value = tzi.BaseUtcOffset.TotalHours.ToString() });
            }
            model.TimeZonesList = items;
            if (ModelState.IsValid)
            {
                ViewBag.Start = 2;
                DateTime interviewdatetime = new DateTime();
                if (!string.IsNullOrEmpty(model.InterviewDate))
                {
                    var _date = model.InterviewDate.Split(" ");
                    var _time = _date[3].Split(":");
                    DateTime value = new DateTime(Convert.ToInt32(_date[2]), Convert.ToInt32(_date[1]), Convert.ToInt32(_date[0]), Convert.ToInt32(_time[0]), Convert.ToInt32(_time[1]), 0);
                    interviewdatetime = value;
                    model.TimeZoneoffset = FetchCurrentoffset(model.offset, value, model.TimeZonesValue);
                    model.scheduleDate = value.AddMinutes(model.TimeZoneoffset);
                }
                if (model.ResumeFile != null)
                {
                    var fileName = model.ResumeFile.FileName.Replace(" ", "_");
                    FileInfo fi = new FileInfo(fileName);
                    if (fi.Extension == ".docx" || fi.Extension == ".doc" || fi.Extension == ".pdf")
                    {
                        var fileMimeType = GetFileMimeType(model.ResumeFile);
                        if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                            || fileMimeType == "application/pdf" || fileMimeType == "application/x-java-archive" || fileMimeType == "application/vnd.ms-powerpoint")
                        {
                            await _azureStorageHelper.InsertResumeBlob(fileName, "resumes", model.ResumeFile, fi.Extension);
                            var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                            var blob = containerRef.GetBlockBlobReference(fileName);
                            string uploadedaudioFileURL = blob.Uri.ToString();
                            string sasUrl = string.Empty;
                            if (uploadedaudioFileURL == null)
                            {
                                ModelState.AddModelError("ResumeFile", "Resume upload has failed.");
                                return View(model);
                            }
                            else
                            {
                                var sas = _sasUrlHelper.CreateSasUrl("resumes");
                                sasUrl = uploadedaudioFileURL + "?" + sas;
                                model.Resume = sasUrl;
                            }
                        }
                        else
                        {
                            ModelState.AddModelError("ResumeFile", "Please select valid file(.pdf, .doc, .docx)!");
                            return View(model);
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("ResumeFile", "Please select valid file(.pdf, .doc, .docx)!");
                        return View(model);
                    }
                }
                else
                {
                    ModelState.AddModelError("ResumeFile", "Please upload resume file(.pdf, .doc, .docx)!");
                    return View(model);
                }
                string interviewOnDevice = string.Empty;
                if (model.Checkbox1)
                {
                    interviewOnDevice = interviewOnDevice + " " + "Laptop or machine on which i can code";
                }
                if (model.Checkbox2)
                {
                    interviewOnDevice = interviewOnDevice + " " + "Quality Headphone";
                }
                if (model.Checkbox3)
                {
                    interviewOnDevice = interviewOnDevice + " " + "High Speed Internet";
                }
                if (model.Checkbox4)
                {
                    interviewOnDevice = interviewOnDevice + " " + "Working webcam";
                }
                if (model.Checkbox5)
                {
                    interviewOnDevice = interviewOnDevice + " " + "Chrome Browser";
                }
                if (model.Checkbox6)
                {
                    interviewOnDevice = interviewOnDevice + " " + "Silent place with appropriate lighting";
                }
                var JDTitle = _interviewSchedulerservice.FetchJDTitle(model.InterviewChannelId);
                TimeZoneInfo INDIAN_ZONE = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                DateTime indianTime = TimeZoneInfo.ConvertTimeFromUtc(model.scheduleDate, INDIAN_ZONE);
                var _checkdetails = await _interviewSchedulerservice.CheckInterviewConcurrency(indianTime, model.InterviewChannelId, model.Uniqueid);
                if (_checkdetails)
                {
                    //string meetingLink;// = string.Empty;
                    var zoomLink = new InstaZoomMeetingResponse();
                    var rbMeetingLinkResponse = new RBMeetCreateMeetingResponse();
                    if (string.IsNullOrEmpty(model.MeetingPlatform) || model.MeetingPlatform == MeetingPlatforms.Zoom)
                    {
                        zoomLink = await CreateZoomMeeting(model.Name, JDTitle.Result, indianTime);
                        //meetingLink = await CreateZoomMeeting(model.Name, JDTitle.Result, indianTime);

                    }
                    else if (model.MeetingPlatform == MeetingPlatforms.RisebirdMeet)
                    {
                        rbMeetingLinkResponse = await _rBMeetHelper.CreateMeeting("Risebird", model.Name, indianTime);
                    }
                    string textreminder = string.Empty;
                    textreminder = model.CountryCode + model.MobileNumber;

                    bool result = false;
                    if (string.IsNullOrEmpty(model.MeetingPlatform) || model.MeetingPlatform == MeetingPlatforms.Zoom)
                    {
                        result = _risebirdMeetings.AddOrUpdateMeeting(indianTime, "60", model.Name, model.Email, model.Resume, zoomLink.join_url, model.InterviewChannelId, zoomLink.id.ToString(), interviewOnDevice, textreminder, model.StaffingAgiencies, model.HRCoordinator, model.TimeZoneoffset, model.TimeZonesValue, zoomLink.accountemail);
                    }
                    else if (model.MeetingPlatform == MeetingPlatforms.RisebirdMeet)
                    {
                        result = _risebirdMeetings.AddOrUpdateMeeting(indianTime, "60", model.Name, model.Email, model.Resume, rbMeetingLinkResponse.meetingAgent, model.InterviewChannelId, rbMeetingLinkResponse.roomId, interviewOnDevice, textreminder, model.StaffingAgiencies, model.HRCoordinator, model.TimeZoneoffset, model.TimeZonesValue);
                    }
                    if (result)
                    {
                        _ = _interviewSchedulerservice.UpdateSchedulingLinkDetails(model.Uniqueid);
                        string timezonevalue = string.Empty;
                        string hh = string.Empty;
                        var valuecheck = (-model.offset / 60);
                        if (valuecheck < 10 && valuecheck > 0)
                        {
                            hh = "0" + valuecheck;
                        }
                        if (valuecheck > -10 && valuecheck < 0)
                        {
                            hh = "-0" + valuecheck.ToString().Replace("-", "");
                        }
                        var offsetvalue = (hh + ":" + ((model.offset % 60) < 0 ? (-model.offset % 60) : (model.offset % 60))).ToString();
                        var timeZones = TimeZoneInfo.GetSystemTimeZones();
                        foreach (TimeZoneInfo timeZone in timeZones)
                        {
                            //if (timeZone.DisplayName.Contains(offsetvalue))
                            //{
                            //    timezonevalue = timeZone.DisplayName;
                            //    break;
                            //}
                            if (timeZone.DisplayName.Contains(model.TimeZonesValue))
                            {
                                if (timeZone.DisplayName.Contains(offsetvalue))
                                {
                                    if (timeZone.IsDaylightSavingTime(interviewdatetime))
                                    {
                                        timezonevalue = timeZone.DaylightName;
                                    }
                                    else
                                    {
                                        timezonevalue = timeZone.StandardName;
                                    }
                                }
                            }
                        }

                        //if (zoomLink.accountemail == "me")
                        //{
                        //    zoomLink.accountemail = "host@risebird.io";
                        //}
                        string accountEmail = "hire@risebird.io";
                        //Send Mail Notification
                        Mail _mail = new Mail
                        {
                            TO = model.Email,
                            CC = accountEmail,//zoomLink.accountemail,
                            Subject = "Invitation: " + model.Name + " @ " + interviewdatetime + "(" + timezonevalue + ")"
                        };
                        if (string.IsNullOrEmpty(model.MeetingPlatform) || model.MeetingPlatform == MeetingPlatforms.Zoom)
                        {
                            _ = _notificationHelper.SendInterviewInvite(_mail, model.Email, zoomLink.join_url, model.StaffingAgiencies, model.HRCoordinator, accountEmail, interviewdatetime, model.Name, timezonevalue, model.ClientName, model.TimeZoneoffset);
                        }
                        else if (model.MeetingPlatform == MeetingPlatforms.RisebirdMeet)
                        {
                            _ = _notificationHelper.SendInterviewInvite(_mail, model.Email, rbMeetingLinkResponse.meetingAttendee, model.StaffingAgiencies, model.HRCoordinator, accountEmail, interviewdatetime, model.Name, timezonevalue, model.ClientName, model.TimeZoneoffset);
                        }
                        ViewBag.Start = 4;
                        ViewBag.interviewDate = interviewdatetime;
                        return View(model);
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Please try again to schedule the event.");
                        return View(model);
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Please change the interview date time, selected time slots are not available.");
                    return View(model);
                }
            }
            else
            {
                return View(model);
            }
        }
        public string GetFileMimeType(IFormFile file)
        {
            IFileTypeInterrogator interrogator = new FileTypeInterrogator.FileTypeInterrogator();
            byte[] fileBytes;
            using (var stream = new MemoryStream())
            {
                file.CopyTo(stream);
                fileBytes = stream.ToArray();
            }
            FileTypeInfo fileTypeInfo = interrogator.DetectType(fileBytes);
            return fileTypeInfo.MimeType.ToLower();
        }
        public JsonResult FetchSlots(int day, int month, int year, string id, int offset, string TimeZonevalue)
        {
            DateTime value = new DateTime(year, month, day);
            var result = _interviewSchedulerservice.FetchSlots(value, id);
            string dayofweek = value.DayOfWeek.ToString().ToLower();
            var result0 = result.Where(x => x.Daytype == dayofweek).ToList();
            string dayofweek1 = value.AddDays(1).DayOfWeek.ToString().ToLower();
            var result1 = result.Where(x => x.Daytype == dayofweek1).ToList();
            string dayofweek2 = value.AddDays(-1).DayOfWeek.ToString().ToLower();
            var result2 = result.Where(x => x.Daytype == dayofweek2).ToList();
            offset = FetchCurrentoffset(offset, value, TimeZonevalue);
            int addontime = offset + 330;
            var model = new List<string>();

            if (result != null)
            {
                foreach (var item in result0)
                {
                    if (dayofweek != DateTime.Now.DayOfWeek.ToString().ToLower())
                    {
                        int valueset = Convert.ToInt32(item.End) - Convert.ToInt32(item.Start);

                        for (int i = 0; i < valueset; i++)
                        {
                            if (addontime == 0)
                            {
                                var slots = Convert.ToInt32(item.Start) + i;
                                DateTime value1 = new DateTime(year, month, day, slots, 0, 0);
                                if (result.FirstOrDefault().StartDate <= value1)
                                    model.Add(slots.ToString() + ":00");
                            }
                            else
                            {
                                var slots = Convert.ToInt32(item.Start) * 60 + (i * 60) - addontime;
                                if (offset > 0 && (slots / 60) < 0)
                                {
                                    var newslot = 24 + (slots / 60);
                                    foreach (var item2 in result1)
                                    {
                                        if (Convert.ToInt32(item2.Start) <= Convert.ToInt32(newslot) && Convert.ToInt32(item2.End) > Convert.ToInt32(newslot))
                                        {
                                            DateTime value1 = new DateTime(year, month, (day - 1), newslot, 0, 0);
                                            if (result.FirstOrDefault().StartDate <= value1)
                                                model.Add(((slots / 60) + 24).ToString().Replace("-", "") + ":" + (slots % 60).ToString().Replace("-", ""));
                                        }
                                    }
                                }
                                else if (offset < 0 && (slots / 60) > 24)
                                {
                                    var newslot = (slots + addontime) / 60;
                                    foreach (var item2 in result2)
                                    {
                                        if (Convert.ToInt32(item2.Start) <= Convert.ToInt32(newslot) && Convert.ToInt32(item2.End) > Convert.ToInt32(newslot))
                                        {
                                            DateTime value1 = new DateTime(year, month, (day + 1), newslot, 0, 0);
                                            if (result.FirstOrDefault().StartDate <= value1)
                                                model.Add(((slots / 60) - 24).ToString().Replace("-", "") + ":" + (slots % 60).ToString().Replace("-", ""));
                                        }
                                    }
                                }
                                else
                                {
                                    DateTime value1 = new DateTime(year, month, day, (Convert.ToInt32(item.Start) + i), 0, 0);
                                    if (result.FirstOrDefault().StartDate <= value1)
                                        model.Add((slots / 60).ToString().Replace("-", "") + ":" + (slots % 60).ToString().Replace("-", ""));
                                }
                            }
                        }
                    }
                    else
                    {
                        if (result.FirstOrDefault().BufferTime > 0)
                        {
                            int buffertimevalue = result.FirstOrDefault().BufferTime;
                            var comdate = DateTime.Now.AddMinutes(-addontime).AddHours(buffertimevalue);
                            if (comdate.Date == DateTime.Now.Date)
                            {
                                int offsetbuffer = DateTime.Now.AddMinutes(-addontime).AddHours(buffertimevalue).Hour;
                                int valueset = Convert.ToInt32(item.End) - Convert.ToInt32(item.Start);
                                for (int i = 0; i < valueset; i++)
                                {
                                    if (addontime == 0)
                                    {
                                        var slots = Convert.ToInt32(item.Start) + i;
                                        DateTime value1 = new DateTime(year, month, day, slots, 0, 0);
                                        if (result.FirstOrDefault().StartDate <= value1)
                                        {
                                            if (slots > offsetbuffer)
                                            {
                                                model.Add(slots.ToString() + ":00");
                                            }
                                        }
                                    }
                                    else
                                    {
                                        var slots = Convert.ToInt32(item.Start) * 60 + (i * 60) - addontime;
                                        if (offset > 0 && (slots / 60) < 0)
                                        {
                                            var newslot = 24 + (slots / 60);
                                            foreach (var item2 in result1)
                                            {
                                                if (Convert.ToInt32(item2.Start) <= Convert.ToInt32(newslot) && Convert.ToInt32(item2.End) > Convert.ToInt32(newslot))
                                                {
                                                    DateTime value1 = new DateTime(year, month, (day - 1), newslot, 0, 0);
                                                    if (result.FirstOrDefault().StartDate <= value1)
                                                    {
                                                        if (slots > (offsetbuffer * 60))
                                                        {
                                                            model.Add(((slots / 60) + 24).ToString().Replace("-", "") + ":" + (slots % 60).ToString().Replace("-", ""));
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        else if (offset < 0 && (slots / 60) > 24)
                                        {
                                            var newslot = (slots + addontime) / 60;
                                            foreach (var item2 in result2)
                                            {
                                                if (Convert.ToInt32(item2.Start) <= Convert.ToInt32(newslot) && Convert.ToInt32(item2.End) > Convert.ToInt32(newslot))
                                                {
                                                    DateTime value1 = new DateTime(year, month, (day + 1), newslot, 0, 0);
                                                    if (result.FirstOrDefault().StartDate <= value1)
                                                    {
                                                        if (slots > (offsetbuffer * 60))
                                                        {
                                                            model.Add(((slots / 60) - 24).ToString().Replace("-", "") + ":" + (slots % 60).ToString().Replace("-", ""));
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            DateTime value1 = new DateTime(year, month, day, (Convert.ToInt32(item.Start) + i), 0, 0);
                                            if (result.FirstOrDefault().StartDate <= value1)
                                            {
                                                if (slots > (offsetbuffer * 60))
                                                {
                                                    model.Add((slots / 60).ToString().Replace("-", "") + ":" + (slots % 60).ToString().Replace("-", ""));
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    //else
                    //{
                    //    if (Convert.ToInt32(item.Start) <= Convert.ToInt32(DateTime.Now.Hour) && Convert.ToInt32(item.End) > Convert.ToInt32(DateTime.Now.Hour))
                    //    {
                    //        int valueset = Convert.ToInt32(item.End) - DateTime.Now.Hour;
                    //        for (int i = 0; i < valueset; i++)
                    //        {
                    //            if (addontime == 0)
                    //            {
                    //                var slots = DateTime.Now.AddHours(i + 1).Hour;
                    //                DateTime value1 = new DateTime(year, month, day, slots, 0, 0);
                    //                if (result.FirstOrDefault().StartDate <= value1)
                    //                    model.Add(slots.ToString() + ":00");
                    //            }
                    //            else
                    //            {
                    //                var slots = (DateTime.Now.AddHours(i + 1).Hour * 60) - addontime;
                    //                if (offset > 0 && (slots / 60) < 0)
                    //                {
                    //                    var newslot = 24 + (slots / 60);
                    //                    foreach (var item2 in result1)
                    //                    {
                    //                        if (Convert.ToInt32(item2.Start) <= Convert.ToInt32(newslot) && Convert.ToInt32(item2.End) > Convert.ToInt32(newslot))
                    //                        {
                    //                            DateTime value1 = new DateTime(year, month, day - 1, slots, 0, 0);
                    //                            if (result.FirstOrDefault().StartDate <= value1)
                    //                                model.Add(((slots / 60) + 24).ToString().Replace("-", "") + ":" + (slots % 60).ToString().Replace("-", ""));
                    //                        }
                    //                    }
                    //                }
                    //                else if (offset < 0 && (slots / 60) > 24)
                    //                {
                    //                    var newslot = (slots + addontime) / 60;
                    //                    foreach (var item2 in result2)
                    //                    {
                    //                        if (Convert.ToInt32(item2.Start) <= Convert.ToInt32(newslot) && Convert.ToInt32(item2.End) > Convert.ToInt32(newslot))
                    //                        {
                    //                            DateTime value1 = new DateTime(year, month, day + 1, slots, 0, 0);
                    //                            if (result.FirstOrDefault().StartDate <= value1)
                    //                                model.Add(((slots / 60) - 24).ToString().Replace("-", "") + ":" + (slots % 60).ToString().Replace("-", ""));
                    //                        }
                    //                    }
                    //                }
                    //                else
                    //                {
                    //                    DateTime value1 = new DateTime(year, month, day, DateTime.Now.AddHours(i + 1).Hour, 0, 0);
                    //                    if (result.FirstOrDefault().StartDate <= value1)
                    //                        model.Add((slots / 60).ToString().Replace("-", "") + ":" + (slots % 60).ToString().Replace("-", ""));
                    //                }
                    //            }
                    //        }
                    //    }
                    //    else if (Convert.ToInt32(item.End) > Convert.ToInt32(DateTime.Now.Hour))
                    //    {
                    //        int valueset = Convert.ToInt32(item.End) - Convert.ToInt32(item.Start);
                    //        for (int i = 0; i < valueset; i++)
                    //        {
                    //            if (addontime == 0)
                    //            {
                    //                var slots = Convert.ToInt32(item.Start) + i;
                    //                DateTime value1 = new DateTime(year, month, day, slots, 0, 0);
                    //                if (result.FirstOrDefault().StartDate <= value1)
                    //                    model.Add(slots.ToString() + ":00");
                    //            }
                    //            else
                    //            {
                    //                //var slots = (DateTime.Now.AddHours(i + 1).Hour * 60) - addontime;
                    //                var slots = Convert.ToInt32(item.Start) * 60 + (i * 60) - addontime;
                    //                if (offset > 0 && (slots / 60) < 0)
                    //                {
                    //                    var newslot = 24 + (slots / 60);
                    //                    foreach (var item2 in result1)
                    //                    {
                    //                        if (Convert.ToInt32(item2.Start) <= Convert.ToInt32(newslot) && Convert.ToInt32(item2.End) > Convert.ToInt32(newslot))
                    //                        {
                    //                            DateTime value1 = new DateTime(year, month, day - 1, slots, 0, 0);
                    //                            if (result.FirstOrDefault().StartDate <= value1)
                    //                                model.Add(((slots / 60) + 24).ToString().Replace("-", "") + ":" + (slots % 60).ToString().Replace("-", ""));
                    //                        }
                    //                    }
                    //                }
                    //                else if (offset < 0 && (slots / 60) > 24)
                    //                {
                    //                    var newslot = (slots + addontime) / 60;
                    //                    foreach (var item2 in result2)
                    //                    {
                    //                        if (Convert.ToInt32(item2.Start) <= Convert.ToInt32(newslot) && Convert.ToInt32(item2.End) > Convert.ToInt32(newslot))
                    //                        {
                    //                            DateTime value1 = new DateTime(year, month, day + 1, slots, 0, 0);
                    //                            if (result.FirstOrDefault().StartDate <= value1)
                    //                                model.Add(((slots / 60) - 24).ToString().Replace("-", "") + ":" + (slots % 60).ToString().Replace("-", ""));
                    //                        }
                    //                    }
                    //                }
                    //                else
                    //                {
                    //                    DateTime value1 = new DateTime(year, month, day, DateTime.Now.AddHours(i + 1).Hour, 0, 0);
                    //                    if (result.FirstOrDefault().StartDate <= value1)
                    //                        model.Add((slots / 60).ToString().Replace("-", "") + ":" + (slots % 60).ToString().Replace("-", ""));
                    //                }
                    //            }
                    //        }

                    //    }
                    //}
                }
            }
            return Json(model);
        }
        #endregion
        #region Create Zoom Meeting
        public async Task<InstaZoomMeetingResponse> CreateZoomMeeting(string CandidateName, string Role, DateTime indianTime)
        {
            InstaZoomMeetingVM _zoommetingobject = new InstaZoomMeetingVM();
            _zoommetingobject.topic = CandidateName + "| " + Role + "| Video Interview";
            //TimeZoneInfo INDIAN_ZONE = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            //DateTime indianTime = TimeZoneInfo.ConvertTimeFromUtc(interviewDate, INDIAN_ZONE);
            _zoommetingobject.start_time = indianTime.ToString("yyyy-MM-ddThh:mm:ss");
            //var accountEmail = await _interviewSchedulerservice.CheckInstaMeetingAccount(indianTime, indianTime.AddMinutes(60));
            var accountEmail = await _interviewSchedulerservice.GetAvailableZoomAccount(indianTime.AddMinutes(-30), indianTime.AddMinutes(30));
            if (string.IsNullOrEmpty(accountEmail))
                accountEmail = "me";
            var zoomlink = CreateZoomMeetings(_zoommetingobject, accountEmail, indianTime);
            zoomlink.accountemail = accountEmail;
            var succeresult = _interviewSchedulerservice.UpdateInstaZoomDetails(indianTime, indianTime.AddMinutes(60), accountEmail);
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
            convertedResponse.accountemail = accountEmail;
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
        #endregion
        public IActionResult IRFSchedulerEventEdit(string id)
        {
            var eventid = _interviewSchedulerservice.FetchEventidByUniqueid(id);
            return RedirectToAction("EditEvent", new { eventid = eventid, eventtype = 1 });
        }
        public string DeleteMeetingLink(string meetingid)
        {
            string token = ZoomToken();
            var client = new RestClient(string.Concat(_configuration["ZoomAppDetails:endPoint"], $"meetings/" + meetingid));
            var request = new RestRequest(Method.DELETE);
            request.AddHeader("content-type", "application/json");
            request.AddHeader("authorization", "Bearer " + token);
            IRestResponse response = client.Execute(request);
            var content = response.Content;
            return "Delete successfully";
        }
        public int FetchCurrentoffset(int offset, DateTime value, string TimeZonevalue)
        {
            int currentOffset = offset;
            string timezonevalue = string.Empty;
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
                var flag = false;
                if (timeZone.DisplayName.Contains(TimeZonevalue))
                {
                    if (timeZone.DisplayName.Contains(offsetvalue))
                    {
                        flag = timeZone.IsDaylightSavingTime(value);
                        if (flag)
                        {
                            TimeSpan currentOffsetvalue = timeZone.GetUtcOffset(value);
                            currentOffset = -(currentOffsetvalue.Hours * 60);
                        }
                    }
                }
            }
            return currentOffset;
        }
    }
}
