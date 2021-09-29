using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

namespace Risebird.Web.Controllers
{
    public class InterviewRequestController : Controller
    {
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly INotificationHelper _notificationHelper;
        private readonly IConfiguration _configuration;
        private readonly IInterviewRequestService _interviewRequestService;
        private readonly IInterviewerService _interviewerService;
        private readonly IMeetingService _meetingService;
        private readonly IIRFRepository _irfRepository;
        private readonly IUserService _userService;
        private readonly IIRFService _irfService;
        public InterviewRequestController(IApplicationErrorLogger applicationErrorLogger, IAzureStorageHelper azureStorageHelper, INotificationHelper notificationHelper, IConfiguration configuration, IInterviewRequestService interviewRequestService, IInterviewerService interviewerService, IMeetingService meetingService, IIRFRepository irfRepository, IUserService userService, IIRFService irfService)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _azureStorageHelper = azureStorageHelper;
            _notificationHelper = notificationHelper;
            _configuration = configuration;
            _interviewRequestService = interviewRequestService;
            _interviewerService = interviewerService;
            _meetingService = meetingService;
            _irfRepository = irfRepository;
            _userService = userService;
            _irfService = irfService;
        }
        public async Task<IActionResult> Index()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    ViewBag.ProfileName = profileName;
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SeniorHR")
                    {
                        var model = new List<InterviewRequestVM>();
                        var irfList = _interviewRequestService.GetAllInterviewRequests(emailid, profileName);
                        model = (List<InterviewRequestVM>)irfList;
                        model = model.Select(x =>
                        {
                            x.CountryName = ((InterviewRequestCountry)Convert.ToInt32(x.Country)).GetDescription();
                            x.JobLevelName = ((JobLevels)Convert.ToInt32(x.JobLevel)).GetDescription();
                            x.IsSetToComplete = !String.IsNullOrEmpty(x.Assignto) ? x.Assignto.ToUpper() == emailid.ToUpper() ? true : false : false;
                            return x;
                        }).ToList();
                        if (TempData["SuccessErrorMessageInterview"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageInterview"].ToString();
                            TempData.Remove("SuccessErrorMessageInterview");
                        }
                        if (HttpContext.Session.GetString("InterviewRequestfilters") != null)
                        {
                            var filter = JsonConvert.DeserializeObject<IRFFilter>(HttpContext.Session.GetString("InterviewRequestfilters"));
                            filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = model.Select(x => x.PrimarySkill).ToArray() : filter.PrimarySkill = filter.PrimarySkill;
                            model = model.Where(c => filter.PrimarySkill.Contains(c.PrimarySkill.Split("|")[0])).ToList();
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
        public async Task<IActionResult> CreateInterviewRequest()
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
                        var model = new InterviewRequestVM();
                        var skills = _irfRepository.GetSkills();
                        await SetInterviewRequestSelectLists();
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
        public async Task<IActionResult> CreateInterviewRequest(InterviewRequestVM meeting)
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
                            else
                            {
                                ModelState.AddModelError(String.Empty, "Jd File is required,Please upload Jd File.");
                                await SetInterviewRequestSelectLists();
                                return View(meeting);
                            }
                            if (_interviewRequestService.CreateInerviewRequest(meeting))
                            {
                                //send mail to ashutosh@risebird.io;vaibhaw_sachan@risebird.io;talent@risebird.io
                                Mail _mail = new Mail
                                {
                                    TO = "ashutosh@risebird.io",//;vaibhaw_sachan@risebird.io;talent@risebird.io",
                                    Subject = "New Interview Request: " + meeting.JdTitle + " - " + meeting.PrimarySkill
                                };
                                var emailsend = _notificationHelper.InterviewRequestAddNotification(_mail, meeting.JdTitle, meeting.PrimarySkill);
                                TempData["SuccessErrorMessageInterview"] = "Request Created Successfully.";
                                return RedirectToAction("Index");
                            }
                            else
                            {
                                TempData["SuccessErrorMessageInterview"] = "Failed to Create request.";
                                return RedirectToAction("Index");
                            }

                        }
                        else
                        {
                            return View(meeting);
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
        public async Task<IActionResult> EditInterviewRequest(int id)
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
                        var model = new InterviewRequestVM();
                        model = _interviewRequestService.FetchInterviewRequestById(id);
                        await SetInterviewRequestSelectLists();
                        var skills = _irfRepository.GetSkills();
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
        public async Task<IActionResult> EditInterviewRequest(InterviewRequestVM meeting)
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
                            else if (!String.IsNullOrEmpty(meeting.JDLink))
                            {
                                meeting.JDLink = meeting.JDLink;
                            }
                            else
                            {
                                ModelState.AddModelError(String.Empty, "Jd File is required,Please upload Jd File.");
                                await SetInterviewRequestSelectLists();
                                return View(meeting);
                            }
                            if (_interviewRequestService.CreateInerviewRequest(meeting))
                            {
                                //send mail to ashutosh@risebird.io;vaibhaw_sachan@risebird.io;talent@risebird.io
                                Mail _mail = new Mail
                                {
                                    TO = "ashutosh@risebird.io;vaibhaw_sachan@risebird.io;talent@risebird.io",
                                    Subject = "Interview Request Updates: " + meeting.JdTitle + " - " + meeting.PrimarySkill
                                };
                                var emailsend = _notificationHelper.InterviewRequestEditNotification(_mail, meeting.JdTitle, meeting.PrimarySkill);
                                TempData["SuccessErrorMessageInterview"] = "Request updated Successfully.";
                                return RedirectToAction("Index");
                            }
                            else
                            {
                                TempData["SuccessErrorMessageInterview"] = "Failed to update the request.";
                                return RedirectToAction("Index");
                            }
                        }
                        else
                        {
                            await SetInterviewRequestSelectLists();
                            return View(meeting);
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
                return RedirectToAction("Index");
            }
        }
        public async Task SetInterviewRequestSelectLists()
        {
            ViewBag.LevelTech = new SelectList(_irfRepository.GetLevelTech(), "id", "Name");
            var jobLevels = from JobLevels s in Enum.GetValues(typeof(JobLevels))
                            select new { ID = (int)s, Name = s.GetDescription() };
            ViewBag.JobLevelList = new SelectList(jobLevels, "ID", "Name");
            var _country = from InterviewRequestCountry s in Enum.GetValues(typeof(InterviewRequestCountry))
                           select new { ID = (int)s, Name = s.GetDescription() };
            ViewBag.CountryList = new SelectList(_country, "ID", "Name");
        }
        [HttpPost]
        public JsonResult FetchCoordinators()
        {
            var coordinatorlist = new SelectList(_irfService.GetEligibleChannelOwners(), "id", "EmailId");
            return Json(new { result = coordinatorlist });
        }
        [HttpPost]
        public JsonResult AssignCoordinator(int userid, int id)
        {
            var result = _interviewRequestService.AssignInerviewRequest(userid, id);
            if (result)
            {
                //Send a mail to userid with EventDetails
                var model = _interviewRequestService.FetchInterviewRequestById(id);
                model.CountryName = ((InterviewRequestCountry)Convert.ToInt32(model.Country)).GetDescription();
                model.JobLevelName = ((JobLevels)Convert.ToInt32(model.JobLevel)).GetDescription();
                Mail _mail = new Mail
                {
                    TO = model.Assignto,
                    Subject = "New Interview Request Assignment: " + model.JdTitle + " - " + model.PrimarySkill
                };
                var emailsend = _notificationHelper.InterviewRequestAssignationNotification(_mail, model.JdTitle, model.PrimarySkill, model.CountryName, model.JobLevelName);
                return Json("Successfully assigned.");
            }
            else
            {
                return Json("Fail to assign the request, please try to assign again.");
            }
        }
        public ActionResult SetIRFCreated(int id)
        {
            var result = _interviewRequestService.SetIRFCreated(id);
            if (result)
            {
                //Send mail for confirmation
                var model = _interviewRequestService.FetchInterviewRequestById(id);
                model.CountryName = ((InterviewRequestCountry)Convert.ToInt32(model.Country)).GetDescription();
                model.JobLevelName = ((JobLevels)Convert.ToInt32(model.JobLevel)).GetDescription();
                Mail _mail = new Mail
                {
                    TO = model.RequestedBy,
                    Subject = "New Interview Request Created: " + model.JdTitle + " - " + model.PrimarySkill
                };
                var emailsend = _notificationHelper.InterviewRequestCompletedNotification(_mail, model.JdTitle, model.PrimarySkill, model.CountryName, model.JobLevelName);
                TempData["SuccessErrorMessageInterview"] = "Request marked as created successfully.";
                return RedirectToAction("Index");
            }
            else
            {
                TempData["SuccessErrorMessageInterview"] = "Request marked as created failed, Please try again.";
                return RedirectToAction("Index");
            }
        }
    }
}
