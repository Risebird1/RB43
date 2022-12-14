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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
        private readonly IFileMimeTypeHelper _fileMimeTypeHelper;
        private readonly IClientService _clientService;
        private readonly ISkillService _skillService;
        private readonly IInterviewSchedulerService _interviewSchedulerservice;

        public InterviewRequestController(IApplicationErrorLogger applicationErrorLogger, IAzureStorageHelper azureStorageHelper, INotificationHelper notificationHelper, IConfiguration configuration, IInterviewRequestService interviewRequestService, IInterviewerService interviewerService, IMeetingService meetingService, IIRFRepository irfRepository, IUserService userService, IIRFService irfService, IFileMimeTypeHelper fileMimeTypeHelper, IClientService clientService, ISkillService skillService, IInterviewSchedulerService interviewSchedulerservice)
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
            _fileMimeTypeHelper = fileMimeTypeHelper;
            _clientService = clientService;
            _skillService = skillService;
            _interviewSchedulerservice = interviewSchedulerservice;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    ViewBag.profileName = profileName;
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        var model = new List<InterviewRequestVM>();
                        var irfList = _interviewRequestService.GetAllInterviewRequests(emailid, profileName);
                        model = (List<InterviewRequestVM>)irfList;
                        model = model.Select(x =>
                        {
                            x.CountryName = ((InterviewRequestCountry)Convert.ToInt32(x.Country)).GetDescription();
                            x.JobLevelName = ((JobLevels)Convert.ToInt32(x.JobLevel)).GetDescription();
                            x.IsSetToComplete = !string.IsNullOrEmpty(x.Assignto) ? x.Assignto.ToUpper() == emailid.ToUpper() ? true : false : false;
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

        [HttpGet]
        public async Task<IActionResult> CreateInterviewRequest()
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
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        var model = new InterviewRequestVM();
                        model.RequestedBy = emailid;
                        var skills = _irfRepository.GetSkills();
                        await SetInterviewRequestSelectLists();
                        var Skillset = skills;
                        if (Skillset != null)
                        {
                            Skillset = Skillset.OrderBy(x => x.Name.Trim()).ToList();
                        }
                        ViewBag.Skill = new SelectList(Skillset, "id", "Name");
                        int clientId = await _clientService.GetClientByEmailId(emailid);
                        model.ClientId = clientId;
                        if (clientId > 0)
                        {
                            model.ClientCompanyName = (await _clientService.GetClient(clientId))?.Name;
                        }
                        await SetBillingOwnerList(clientId);
                        if (TempData["SuccessMessageInterviewCreation"] != null)
                        {
                            ViewBag.Message = TempData["SuccessMessageInterviewCreation"].ToString();
                            TempData.Remove("SuccessMessageInterviewCreation");
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateInterviewRequest(InterviewRequestVM meeting)
        {
            string emailid = string.Empty;
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    ViewBag.profile = profileName;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        if (ModelState.IsValid)
                        {
                            var model = new IRFVm();
                            if (meeting.JDFile != null)
                            {
                                var fileName = meeting.JDFile.FileName.Replace(" ", "_");
                                FileInfo fi = new FileInfo(fileName);
                                if (fi.Extension == ".docx" || fi.Extension == ".doc" || fi.Extension == ".pdf")
                                {
                                    var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(meeting.JDFile);
                                    if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                                                      || fileMimeType == "application/pdf" || fileMimeType == "application/x-java-archive" || fileMimeType == "application/vnd.ms-powerpoint")
                                    {
                                        await _azureStorageHelper.InsertBlob(fileName, "resumes", meeting.JDFile);
                                        var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                                        var blob = containerRef.GetBlockBlobReference(fileName);
                                        meeting.JDLink = blob.Uri.ToString();
                                    }
                                    else
                                    {
                                        ModelState.AddModelError(string.Empty, "Please select valid file for import(.pdf, .doc, .docx)!");
                                        await SetInterviewRequestSelectLists();
                                        await SetBillingOwnerList(meeting.ClientId);
                                        return View(meeting);
                                    }
                                }
                                else
                                {
                                    ModelState.AddModelError(string.Empty, "Please select valid file for import(.pdf, .doc, .docx)!");
                                    await SetInterviewRequestSelectLists();
                                    await SetBillingOwnerList(meeting.ClientId);
                                    return View(meeting);
                                }
                            }
                            else
                            {
                                ModelState.AddModelError("JDFile", "Jd File is required.");
                                await SetInterviewRequestSelectLists();
                                await SetBillingOwnerList(meeting.ClientId);
                                return View(meeting);
                            }
                            if (meeting.TechSkills.Count() < 2)
                            {
                                ModelState.AddModelError(string.Empty, "Please Add atleast 2 skills!");
                                await SetInterviewRequestSelectLists();
                                await SetBillingOwnerList(meeting.ClientId);
                                return View(meeting);
                            }
                            var assessmentGroups = await _skillService.GetAssessmentGroupsByCategory(meeting.Categoryid);
                            var assessmentName = assessmentGroups.Where(x => x.id == meeting.AssessmentGroupId).Select(x => x.AssessmentGroup).FirstOrDefault();
                            var irfid = GenerateSkillChannelName(assessmentName, meeting.ClientCompanyName);
                            model.IRFId = irfid;
                            model.JDLink = meeting.JDLink;
                            model.AssessmentGroupId = meeting.AssessmentGroupId.ToString();
                            model.ClientId = meeting.ClientId;
                            model.ClientName = meeting.ClientName;
                            model.JDTiltle = meeting.JdTitle;
                            model.BillingOwner = meeting.BillingOwner;
                            model.JobCountry = meeting.Country.ToString();
                            model.JobLevel = meeting.JobLevel;
                            model.CompanyType = meeting.CompanyTypes.ToString();
                            model.Heuristicsscore = meeting.CompetencyLevel;
                            model.SkillCollection = meeting.TechSkills;
                            model.StartDate = DateTime.Now;
                            model.EndDate = DateTime.Now.AddMonths(3);
                            var _schedulinglink = await AutoGenerateSchedulingLink(model.StartDate, model.IRFId, model.JDTiltle + "_Scheduling");
                            model.CalendlyLink = _schedulinglink;
                            if (model.ClientId == 0)
                                model.ClientId = await _clientService.GetClientByEmailId(emailid);
                            if (await _irfService.CreateSkillChannelUsingNewInterviewRequest(model, emailid, "Client"))
                            {
                                TempData["SuccessMessageInterviewCreation"] = $"Skills Channel '{model.IRFId}' created Successfully! Scheduling Link : {model.CalendlyLink} ";
                                return RedirectToAction("CreateInterviewRequest");
                            }
                            else
                            {
                                TempData["SuccessMessageInterviewCreation"] = "Failed to create Skills Channel!";
                                return RedirectToAction("CreateInterviewRequest");
                            }
                        }
                        else
                        {
                            TempData["SuccessMessageInterviewCreation"] = "Failed to Create request.";
                            return RedirectToAction("CreateInterviewRequest");
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
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, $"Message : {ex.Message}; StackTrace : {ex.StackTrace};  Source : {ex.Source}; userEmailId : {emailid}");
                //await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessMessageInterviewCreation"] = "Failed to Create request!";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditInterviewRequest(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    ViewBag.profile = profileName;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SeniorHR" || profileName == "SupportAdmin")
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
                    ViewBag.profile = profileName;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SeniorHR" || profileName == "SupportAdmin")
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
                                var fileName = meeting.JDFile.FileName.Replace(" ", "_");
                                FileInfo fi = new FileInfo(fileName);
                                if (fi.Extension == ".docx" || fi.Extension == ".doc" || fi.Extension == ".pdf")
                                {
                                    var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(meeting.JDFile);
                                    if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                                               || fileMimeType == "application/pdf" || fileMimeType == "application/x-java-archive" || fileMimeType == "application/vnd.ms-powerpoint")
                                    {
                                        await _azureStorageHelper.InsertBlob(fileName, "resumes", meeting.JDFile);
                                        var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                                        var blob = containerRef.GetBlockBlobReference(fileName);
                                        meeting.JDLink = blob.Uri.ToString();
                                    }
                                    else
                                    {
                                        ModelState.AddModelError(string.Empty, "Please select valid file for import(.pdf, .doc, .docx)!");
                                        await SetInterviewRequestSelectLists();
                                        return View(meeting);
                                    }
                                }
                                else
                                {
                                    ModelState.AddModelError(string.Empty, "Please select valid file for import(.pdf, .doc, .docx)!");
                                    await SetInterviewRequestSelectLists();
                                    return View(meeting);
                                }
                            }
                            else if (!string.IsNullOrEmpty(meeting.JDLink))
                            {
                                meeting.JDLink = meeting.JDLink;
                            }
                            else
                            {
                                ModelState.AddModelError("JDFile", "Jd File is required.");
                                await SetInterviewRequestSelectLists();
                                return View(meeting);
                            }
                            if (meeting.TechSkill.Count() < 2)
                            {
                                ModelState.AddModelError(string.Empty, "Please Add atleast 2 skills!");
                                return View(meeting);
                            }
                            if (_interviewRequestService.CreateInerviewRequest(meeting))
                            {
                                //send mail to ashutosh@risebird.io;vaibhaw_sachan@risebird.io;talent@risebird.io
                                var emaildids = "ashutosh@risebird.io;vaibhaw_sachan@risebird.io;talent@risebird.io;parvathi_sg@risebird.io";
                                if (!string.IsNullOrEmpty(meeting.Assignto))
                                {
                                    emaildids = emaildids + ";" + meeting.Assignto;
                                }
                                Mail _mail = new Mail
                                {
                                    TO = emaildids,
                                    Subject = "Interview Request has been updated: " + meeting.JdTitle
                                };
                                var emailsend = await _notificationHelper.InterviewRequestEditNotification(_mail, meeting.JdTitle, meeting.PrimarySkill, meeting.RequestedBy);
                                TempData["SuccessErrorMessageInterview"] = "Request has been updated Successfully!";
                                return RedirectToAction("Index");
                            }
                            else
                            {
                                TempData["SuccessErrorMessageInterview"] = "Failed to update Request!";
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

        [HttpGet]
        public async Task SetInterviewRequestSelectLists()
        {
            ViewBag.LevelTech = new SelectList(_irfRepository.GetLevelTech(), "id", "Name");
            var jobLevels = from JobLevels s in Enum.GetValues(typeof(JobLevels))
                            select new { ID = (int)s, Name = s.GetDescription() };
            ViewBag.JobLevelList = new SelectList(jobLevels, "ID", "Name");
            var _country = from InterviewRequestCountry s in Enum.GetValues(typeof(InterviewRequestCountry))
                           select new { ID = (int)s, Name = s.GetDescription() };
            ViewBag.CountryList = new SelectList(_country, "ID", "Name");
            ViewBag.AssessmentCategory = new SelectList(_skillService.GetAssessmentGroupCategory(), "id", "Name");
            var companyTypes = from CompanyType s in Enum.GetValues(typeof(CompanyType))
                               select new { ID = (int)s, Name = s.GetDescription() };
            ViewBag.CompanyTypeList = new SelectList(companyTypes, "ID", "Name");
            var competencyLevels = from CompetencyLevel s in Enum.GetValues(typeof(CompetencyLevel))
                                   select new { ID = (int)s, Name = s.GetDescription() };
            ViewBag.CompetencyLevelList = new SelectList(competencyLevels, "ID", "Name");
            var skills = _irfRepository.GetSkills();
            ViewBag.Skill = new SelectList(skills, "id", "Name");
            var clients = _irfRepository.GetClients();
            ViewBag.Clients = new SelectList(clients, "id", "Name");
        }
        [HttpPost]
        public JsonResult FetchCoordinators()
        {
            var coordinatorlist = new SelectList(_irfService.GetEligibleChannelOwners(), "id", "EmailId");
            return Json(new { result = coordinatorlist });
        }

        [HttpPost]
        public async Task<JsonResult> AssignCoordinator(int userid, int id)
        {
            var result = _interviewRequestService.AssignInerviewRequest(userid, id);
            if (result)
            {
                //Send a mail to userid with EventDetails
                var model = _interviewRequestService.FetchInterviewRequestById(id);
                model.CountryName = ((InterviewRequestCountry)Convert.ToInt32(model.Country)).GetDescription();
                model.JobLevelName = ((JobLevels)Convert.ToInt32(model.JobLevel)).GetDescription();
                var interviewername = _interviewRequestService.FetchInterviewerNameByEmailid(model.Assignto);
                Mail _mail = new Mail
                {
                    TO = model.Assignto,
                    Subject = "New Interview Request Assigned: " + model.JdTitle
                };
                var emailsend = await _notificationHelper.InterviewRequestAssignationNotification(_mail, model.JdTitle, model.PrimarySkill, interviewername);
                return Json("Successfully assigned.");
            }
            else
            {
                return Json("Fail to assign the request, please try to assign again.");
            }
        }


        [HttpGet]
        public ActionResult SetIRFCreated(int id, string IRFId)
        {
            var result = _interviewRequestService.SetIRFCreated(id, IRFId.Trim());
            if (result)
            {
                //Send mail for confirmation
                var model = _interviewRequestService.FetchInterviewRequestById(id);
                model.CountryName = ((InterviewRequestCountry)Convert.ToInt32(model.Country)).GetDescription();
                model.JobLevelName = ((JobLevels)Convert.ToInt32(model.JobLevel)).GetDescription();
                var CalendlyLink = string.Empty;
                if (!string.IsNullOrEmpty(model.CalendlyLink))
                {
                    CalendlyLink = model.CalendlyLink;
                }
                Mail _mail = new Mail
                {
                    TO = model.RequestedBy,
                    Subject = "New Interview Request has been created: " + model.JdTitle
                };
                var emailsend = _notificationHelper.InterviewRequestCompletedNotification(_mail, model.JdTitle, model.PrimarySkill, model.CountryName, model.JobLevelName, CalendlyLink, IRFId);
                TempData["SuccessErrorMessageInterview"] = "Request has been marked as created Successfully!";
                return RedirectToAction("Index");
            }
            else
            {
                TempData["SuccessErrorMessageInterview"] = "Request marked as created failed, Please try again.";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> PublicCreateInterviewRequest(string emailid, string id)
        {
            try
            {
                var Emailid = string.Empty;
                if (HttpContext.Session.GetString("email") != null)
                {
                    Emailid = HttpContext.Session.GetString("email")?.Trim();
                    Emailid = Emailid.Replace("\"", "");
                }
                ViewBag.emailid = emailid;
                ViewBag.id = id;
                var model = new InterviewRequestVM();
                model.uniqueid = id;
                model.RequestedBy = emailid;
                var skills = _irfRepository.GetSkills();
                await SetInterviewRequestSelectLists();
                var Skillset = skills;
                if (Skillset != null)
                {
                    Skillset = Skillset.OrderBy(x => x.Name.Trim()).ToList();
                }
                ViewBag.Skill = new SelectList(Skillset, "id", "Name");
                int clientId = await _clientService.GetClientByEmailId(emailid);
                model.ClientId = clientId;
                if (clientId > 0)
                {
                    model.ClientCompanyName = (await _clientService.GetClient(clientId))?.Name;
                }
                await SetBillingOwnerList(clientId);
                if (TempData["SuccessErrorMessageInterview"] != null)
                {
                    ViewBag.Message = TempData["SuccessErrorMessageInterview"].ToString();
                    ViewBag.Message2 = "";
                    if (TempData["SuccessErrorMessageInterview2"] != null)
                    {
                        ViewBag.Message2 = TempData["SuccessErrorMessageInterview2"].ToString();
                        TempData.Remove("SuccessErrorMessageInterview2");
                        ViewBag.IRFId = TempData["SuccessErrorCloneSkillChannnelMessageIRFId"].ToString();
                        TempData.Remove("SuccessErrorCloneSkillChannnelMessageIRFId");
                    }
                    TempData.Remove("SuccessErrorMessageInterview");
                }
                if (!string.IsNullOrEmpty(emailid))
                {
                    if (!string.IsNullOrEmpty(Emailid) && Emailid != emailid)
                    {
                        return RedirectToAction("PublicAllMeetings", new { id = id });
                    }
                }
                if (!string.IsNullOrEmpty(id))
                {
                    ViewBag.Status = "Public";
                }
                return View(model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PublicCreateInterviewRequest(InterviewRequestVM meeting)
        {
            try
            {
                ViewBag.emailid = meeting.RequestedBy;
                ViewBag.id = meeting.uniqueid;
                var emailId = meeting.RequestedBy;
                if (!string.IsNullOrEmpty(meeting.uniqueid))
                {
                    ViewBag.Status = "Public";
                }
                if (ModelState.IsValid)
                {
                    var model = new IRFVm();
                    if (meeting.JDFile != null)
                    {
                        var fileName = meeting.JDFile.FileName.Replace(" ", "_");
                        FileInfo fi = new FileInfo(fileName);
                        if (fi.Extension == ".docx" || fi.Extension == ".doc" || fi.Extension == ".pdf")
                        {
                            var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(meeting.JDFile);
                            if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                                              || fileMimeType == "application/pdf" || fileMimeType == "application/x-java-archive" || fileMimeType == "application/vnd.ms-powerpoint")
                            {
                                await _azureStorageHelper.InsertBlob(fileName, "resumes", meeting.JDFile);
                                var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                                var blob = containerRef.GetBlockBlobReference(fileName);
                                meeting.JDLink = blob.Uri.ToString();
                            }
                            else
                            {
                                ModelState.AddModelError(string.Empty, "Please select valid file for import(.pdf, .doc, .docx)!");
                                await SetInterviewRequestSelectLists();
                                await SetBillingOwnerList(meeting.ClientId);
                                return View(meeting);
                            }
                        }
                        else
                        {
                            ModelState.AddModelError(string.Empty, "Please select valid file for import(.pdf, .doc, .docx)!");
                            await SetInterviewRequestSelectLists();
                            await SetBillingOwnerList(meeting.ClientId);
                            return View(meeting);
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("JDFile", "Jd File is required.");
                        await SetInterviewRequestSelectLists();
                        await SetBillingOwnerList(meeting.ClientId);
                        return View(meeting);
                    }
                    if (meeting.TechSkills.Count() < 2)
                    {
                        ModelState.AddModelError(string.Empty, "Please Add atleast 2 skills!");
                        await SetInterviewRequestSelectLists();
                        await SetBillingOwnerList(meeting.ClientId);
                        return View(meeting);
                    }
                    var assessmentGroups = await _skillService.GetAssessmentGroupsByCategory(meeting.Categoryid);
                    var assessmentName = assessmentGroups.Where(x => x.id == meeting.AssessmentGroupId).Select(x => x.AssessmentGroup).FirstOrDefault();
                    var irfid = GenerateSkillChannelName(assessmentName, meeting.ClientCompanyName);
                    model.IRFId = irfid;
                    model.JDLink = meeting.JDLink;
                    model.AssessmentGroupId = meeting.AssessmentGroupId.ToString();
                    model.ClientId = meeting.ClientId;
                    model.ClientName = meeting.ClientName;
                    model.JDTiltle = meeting.JdTitle;
                    model.BillingOwner = meeting.BillingOwner;
                    model.JobCountry = meeting.Country.ToString();
                    model.JobLevel = meeting.JobLevel;
                    model.CompanyType = meeting.CompanyTypes.ToString();
                    model.Heuristicsscore = meeting.CompetencyLevel;
                    model.SkillCollection = meeting.TechSkills;
                    model.StartDate = DateTime.Now;
                    model.EndDate = DateTime.Now.AddMonths(3);
                    var _schedulinglink = await AutoGenerateSchedulingLink(model.StartDate, model.IRFId, model.JDTiltle + "_Scheduling");
                    model.CalendlyLink = _schedulinglink;
                    if (await _irfService.CreateSkillChannelUsingNewInterviewRequest(model, emailId, "Client"))
                    {
                        TempData["SuccessErrorMessageInterview"] = $"Skills Channel '{model.IRFId}' created Successfully! Scheduling Link : {model.CalendlyLink} ";
                        TempData["SuccessErrorMessageInterview2"] = model.CalendlyLink;
                        TempData["SuccessErrorCloneSkillChannnelMessageIRFId"] = model.IRFId;
                        return RedirectToAction("PublicCreateInterviewRequest", new { emailid = emailId, id = meeting.uniqueid });
                    }
                    else
                    {
                        TempData["SuccessErrorMessageInterview"] = "Failed to create Skills Channel!";
                        return RedirectToAction("PublicCreateInterviewRequest", new { emailid = emailId, id = meeting.uniqueid });
                    }
                }
                else
                {
                    TempData["SuccessErrorMessageInterview"] = "Failed to Create request.";
                    return RedirectToAction("PublicCreateInterviewRequest", new { emailid = meeting.RequestedBy, id = meeting.uniqueid });
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, $"Message : {ex.Message}; StackTrace : {ex.StackTrace};  Source : {ex.Source}; userEmailId : {meeting.RequestedBy}");
                //await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessageInterview"] = "Failed to Create request!";
                return RedirectToAction("PublicCreateInterviewRequest", new { emailid = meeting.RequestedBy, id = meeting.uniqueid });
            }
        }
        public async Task SetBillingOwnerList(int ClientId)
        {
            var clientUsers = _irfService.GetClientUser(ClientId);
            if (clientUsers != null && clientUsers.Count() > 0)
            {
                ViewBag.BillingOwner = new SelectList(clientUsers, "id", "EmailId");
            }
            else
            {
                var list = new List<SelectListItem>();
                list.Add(new SelectListItem { Text = "No Client User", Value = "0" });
                ViewBag.BillingOwner = new SelectList(list, "Value", "Text");
            }
        }
        public string GenerateSkillChannelName(string assessmentGroupId, string clientCompanyName)
        {
            bool flag = false;
            string skillChannelName = string.Empty;
            while (!flag)
            {
                if (!string.IsNullOrEmpty(clientCompanyName))
                {
                    skillChannelName = clientCompanyName.Substring(0, 3);
                    skillChannelName = skillChannelName + assessmentGroupId.Substring(0, 2) + GenerateRandomstring();
                }
                flag = CheckUniqueness(skillChannelName, false);
            }
            skillChannelName = skillChannelName.Replace(".", "a").Replace(" ", "-");
            return skillChannelName;
        }
        public string GenerateRandomstring()
        {
            DateTime _dt = DateTime.Now;
            var month = _dt.Month.ToString();
            if (month.Length == 1)
            {
                month = "0" + month;
            }
            var _date = _dt.Day.ToString();
            if (_date.Length == 1)
            {
                _date = "0" + _date;
            }
            var sec = digSum(_dt.Second);
            return _date + month + sec.ToString();
        }
        public int digSum(int n)
        {
            int sum = 0;
            while (n > 0 || sum > 9)
            {
                if (n == 0)
                {
                    n = sum;
                    sum = 0;
                }
                sum += n % 10;
                n /= 10;
            }
            return sum;
        }
        public bool CheckUniqueness(string value, bool isCloneTemplate = false)
        {
            return _irfService.CheckUniqueness(value, isCloneTemplate);
        }
        public async Task<string> AutoGenerateSchedulingLink(DateTime eventStartdate, string SkillChannel, string EventName)
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
            model_report.StartDate = eventStartdate.AddHours(16);
            model_report.EndDate = eventStartdate.AddDays(60);
            model_report.EventName = EventName;
            model_report.SkillChannel = SkillChannel;
            model_report.duration = 60;
            model_report.Concurrency = 10;
            model_report.BufferTime = 18;
            var eventid = await _interviewSchedulerservice.AutoCreateEvent(model_report);
            var model = await _interviewSchedulerservice.FetchEventById(eventid);
            if (model != null)
            {
                var eventurl = "https://" + Request.Host.ToString() + "/InterviewScheduler/ScheduleEvent?id=" + model.Uniqueid;
                return eventurl;
            }
            return "";
        }
        //[HttpGet]
        //public async Task<SelectList> GetAssessmentGroupsByCategory(int categoryId, int companytype, int countryid, int jobLevelid, int competencyid)
        //{
        //    var list = new List<SelectListItem>();
        //    var assessmentGroups = await _skillService.GetAssessmentGroupsByCategory(categoryId);
        //    if (assessmentGroups != null && assessmentGroups.Count() > 0)
        //    {
        //        if (companytype != 0)
        //        {
        //            assessmentGroups = assessmentGroups.Where(x => x.CompanyType == companytype).ToList();
        //        }
        //        if (countryid != 0)
        //        {
        //            assessmentGroups = assessmentGroups.Where(x => x.Country == countryid).ToList();
        //        }
        //        if (jobLevelid != 0)
        //        {
        //            assessmentGroups = assessmentGroups.Where(x => x.ProfileType == jobLevelid.ToString()).ToList();
        //        }
        //        if (competencyid != 0)
        //        {
        //            assessmentGroups = assessmentGroups.Where(x => x.CompetencyLevel == competencyid).ToList();
        //        }
        //        if (assessmentGroups.Count() == 0)
        //        {
        //            list.Add(new SelectListItem { Text = "No Assessment Groups for this Category", Value = "" });
        //            return new SelectList(list, "Value", "Text");
        //        }
        //        return new SelectList(assessmentGroups, "id", "AssessmentGroup");
        //    }
        //    else
        //    {
        //        list.Add(new SelectListItem { Text = "No Assessment Groups for this Category", Value = "" });
        //    }
        //    return new SelectList(list, "Value", "Text");
        //}

        [HttpGet]
        public async Task<SelectList> GetAssessmentGroupsByCategory(int categoryId, int companytype, int countryid, int jobLevelid, int employmentType, int difficultyLevel, bool isInstaInterviewRequest = false, string primarySkills = null)
        {
            var profileName = HttpContext.Session.GetString("profileName")?.Trim();
            profileName = profileName?.Replace("\"", "");
            var list = new List<SelectListItem>();
            var assessmentGroups = await _skillService.GetAssessmentGroupsByCategory(categoryId);
            if (isInstaInterviewRequest == true && !string.IsNullOrEmpty(profileName) && profileName == "SeniorHR" || profileName == "SupportAdmin" && assessmentGroups != null)
            {
                assessmentGroups = assessmentGroups.Where(x => x.Isinstaavailable == true);
            }
            if (assessmentGroups != null && assessmentGroups.Count() > 0)
            {
                bool continueApplyingFilter = true;
                if (countryid != 0)
                {
                    IEnumerable<AssignmentVM> assessmentGroupsFilteredByCountry = null;

                    assessmentGroupsFilteredByCountry = assessmentGroups.Where(x => x.Country == countryid).ToList();
                    assessmentGroups = assessmentGroupsFilteredByCountry;
                    if (assessmentGroupsFilteredByCountry == null || assessmentGroupsFilteredByCountry.Count() == 0)
                    {
                        continueApplyingFilter = false;
                    }
                }
                if (!string.IsNullOrEmpty(primarySkills) && continueApplyingFilter == true)
                {
                    var primarySkillsArray = primarySkills.Split(',');
                    IEnumerable<AssignmentVM> assessmentGroupsFilteredByPrimarySkills = null;
                    assessmentGroupsFilteredByPrimarySkills = assessmentGroups.Where(x => !string.IsNullOrEmpty(x.PrimarySkills) && primarySkillsArray.Intersect(x.PrimarySkills.Split(","))?.Count() > 0).ToList();

                    if (assessmentGroupsFilteredByPrimarySkills != null && assessmentGroupsFilteredByPrimarySkills.Count() > 0)
                    {
                        assessmentGroups = assessmentGroupsFilteredByPrimarySkills;
                    }
                    else
                    {
                        continueApplyingFilter = false;
                    }
                }

                if (companytype != 0 && continueApplyingFilter == true)
                {
                    IEnumerable<AssignmentVM> assessmentGroupsFilteredByCompany = null;
                    assessmentGroupsFilteredByCompany = assessmentGroups.Where(x => x.CompanyType == companytype).ToList();

                    if (assessmentGroupsFilteredByCompany != null && assessmentGroupsFilteredByCompany.Count() > 0)
                    {
                        assessmentGroups = assessmentGroupsFilteredByCompany;
                    }
                    else
                    {
                        continueApplyingFilter = false;
                    }
                }

                if (jobLevelid != 0)
                {
                    IEnumerable<AssignmentVM> assessmentGroupsFilteredByJobLevel = null;
                    assessmentGroupsFilteredByJobLevel = assessmentGroups.Where(x => x.SeniorityLevel != null && x.SeniorityLevel.Contains(jobLevelid.ToString())).ToList();
                    //assessmentGroups = assessmentGroups.Where(x => x.SeniorityLevel != null && x.SeniorityLevel.Contains(jobLevelid.ToString())).ToList();

                    if (assessmentGroupsFilteredByJobLevel != null && assessmentGroupsFilteredByJobLevel.Count() > 0)
                    {
                        assessmentGroups = assessmentGroupsFilteredByJobLevel;
                    }
                    else
                    {
                        continueApplyingFilter = false;
                    }
                }
                if (difficultyLevel != 0)
                {
                    IEnumerable<AssignmentVM> assessmentGroupsFilteredByDifficultyLevel = null;
                    assessmentGroupsFilteredByDifficultyLevel = assessmentGroups.Where(x => x.ProfileType != null && x.ProfileType.Contains(difficultyLevel.ToString())).ToList();
                    //assessmentGroups = assessmentGroups.Where(x => x.ProfileType != null && x.ProfileType.Contains(difficultyLevel.ToString())).ToList();

                    if (assessmentGroupsFilteredByDifficultyLevel != null && assessmentGroupsFilteredByDifficultyLevel.Count() > 0)
                    {
                        assessmentGroups = assessmentGroupsFilteredByDifficultyLevel;
                    }
                    else
                    {
                        continueApplyingFilter = false;
                    }
                }
                if (employmentType != 0)
                {
                    IEnumerable<AssignmentVM> assessmentGroupsFilteredByEmploymentType = null;
                    assessmentGroupsFilteredByEmploymentType = assessmentGroups.Where(x => x.EmploymentType != null && x.EmploymentType.Contains(employmentType.ToString())).ToList();
                    //assessmentGroups = assessmentGroups.Where(x => x.EmploymentType != null && x.EmploymentType.Contains(employmentType.ToString())).ToList();

                    if (assessmentGroupsFilteredByEmploymentType != null && assessmentGroupsFilteredByEmploymentType.Count() > 0)
                    {
                        assessmentGroups = assessmentGroupsFilteredByEmploymentType;
                    }
                }

                if (assessmentGroups == null || assessmentGroups.Count() == 0)
                {
                    //list.Add(new SelectListItem { Text = "No Assessment Groups for the selected combination above!", Value = "" });
                    //return new SelectList(list, "Value", "Text");
                    return null;
                }
                return new SelectList(assessmentGroups, "id", "AssessmentGroup");
            }
            else
            {
                return null;
                //list.Add(new SelectListItem { Text = "No Assessment Groups for the selected combination above!", Value = "" });
            }
        }

        [HttpGet]
        public async Task<SelectList> GetSkillsByAssessmentGroups(int assessmentgroupid)
        {
            var list = new List<SelectListItem>();
            var modellist = _userService.GetInterviewerEntitleById(assessmentgroupid);
            var skillset = _skillService.GetSkills();
            var primaryskilllist = new List<SelectListItem>();
            foreach (var item in skillset)
            {
                if (modellist != null)
                {
                    if (modellist.PrimarySkills != null && modellist.PrimarySkills.Contains(item.Name))
                    {
                        var selectItem = new SelectListItem(item.Name, item.id.ToString());
                        primaryskilllist.Add(selectItem);
                    }
                }
            }
            if (primaryskilllist != null && primaryskilllist.Count() > 0)
            {
                return new SelectList(primaryskilllist, "Value", "Text");
            }
            else
            {
                list.Add(new SelectListItem { Text = "No Skills for selected Assessment Group", Value = "" });
            }
            return new SelectList(list, "Value", "Text");
        }

        public async Task<List<string>> GetPrimarySkillsBasedOnCategory(int categoryId)
        {
            try
            {
                return (await _skillService.GetPrimarySkillsBasedOnCategory(categoryId))?.ToList();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, $"Message : {ex.Message}; StackTrace : {ex.StackTrace};  Source : {ex.Source}");
                return null;
            }
        }
    }
}
