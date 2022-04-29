using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using OfficeOpenXml;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;

namespace Risebird.Web.Controllers
{
    public class RecommendedIRFController : Controller
    {
        private IApplicationErrorLogger _applicationErrorLogger;
        private ISkillService _skillService;
        private IConfiguration _configuration;
        private readonly IIRFService _irfService;
        private readonly IInterviewerService _interviewerService;
        private readonly IIRFRepository _irfRepository;
        private readonly IInterviewSchedulerService _interviewSchedulerservice;
        private readonly IUserRepository _userRepository;
        private readonly IRecommendedIRFService _recommendedIRFService;
        private readonly IFileMimeTypeHelper _fileMimeTypeHelper;
        private readonly IClonableSkillChannelTemplateService _clonableSkillChannelTemplateService;

        public RecommendedIRFController(IApplicationErrorLogger applicationErrorLogger, ISkillService skillService, IConfiguration configuration, IIRFService irfService, IInterviewerService interviewerService, IIRFRepository irfRepository, IInterviewSchedulerService interviewSchedulerservice, IUserRepository userRepository, IRecommendedIRFService recommendedIRFService, IFileMimeTypeHelper fileMimeTypeHelper, IClonableSkillChannelTemplateService clonableSkillChannelTemplateService)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _skillService = skillService;
            _configuration = configuration;
            _recommendedIRFService = recommendedIRFService;
            _interviewerService = interviewerService;
            _irfRepository = irfRepository;
            _interviewSchedulerservice = interviewSchedulerservice;
            _userRepository = userRepository;
            _irfService = irfService;
            _fileMimeTypeHelper = fileMimeTypeHelper;
            _clonableSkillChannelTemplateService = clonableSkillChannelTemplateService;
        }
        [HttpGet]
        public IActionResult Index(string emailid, string uniqueid)
        {
            var Emailid = String.Empty;
            if (HttpContext.Session.GetString("email") != null)
            {
                Emailid = HttpContext.Session.GetString("email")?.Trim();
                Emailid = Emailid.Replace("\"", "");
            }
            ViewBag.emailid = emailid;
            ViewBag.uniqueid = uniqueid;
            HttpContext.Session.SetString("email", JsonConvert.SerializeObject(emailid));
            var model = new IRFViewModel();
            var irfList = _recommendedIRFService.GetAllRecommendedIRFs(emailid);
            model.IRFList = irfList;
            foreach (var item in model.IRFList)
            {
                if (item.IsPaused)
                {
                    item.ImageUrl = "/img/Cards_Png/01_Grey-White.png";
                    item.IRFStatus = "Paused";
                }
                else if (item.IsDeleted)
                {
                    item.ImageUrl = "/img/Cards_Png/01_Black-White.png";
                    item.IRFStatus = "Archived";
                }
                else
                {
                    item.ImageUrl = "/img/Cards_Png/01_Green-White.png";
                    item.IRFStatus = "Active";
                }
            }
            if (TempData["SuccessErrorCloneSkillChannnelMessage"] != null)
            {
                ViewBag.Message = TempData["SuccessErrorCloneSkillChannnelMessage"].ToString();
                TempData.Remove("SuccessErrorCloneSkillChannnelMessage");
            }
            if (Emailid != emailid)
            {
                return RedirectToAction("PublicAllMeetings", "Candidate", new { id = uniqueid });
            }
            return View(model);
        }
        [HttpGet]
        public async Task<IActionResult> RecommendatedIRFFilter(string emailid, string uniqueid)
        {
            ViewBag.emailid = emailid;
            ViewBag.uniqueid = uniqueid;
            HttpContext.Session.SetString("email", JsonConvert.SerializeObject(emailid));
            ClonableTemplateListVM model = new ClonableTemplateListVM();
            model.LoggedInUser = emailid;
            model.ClonableTemplates = await _clonableSkillChannelTemplateService.GetAllClonableSkillChannels();

            model.ClonableTemplates?.ToList().ForEach(x => { x.CompetencyLevelDesc = ((Heuristicsscorelevel)x.CompetencyLevelId.Value).GetDescription(); x.SeniorityLevelDesc = ((JobLevels)x.SeniorityLevelId.Value).GetDescription(); x.CountryTypeDesc = ((CountryTypeEnum)x.CountryType.Value).GetDescription(); x.CompanyTypeDesc = ((CompanyType)x.CompanyTypeId.Value).GetDescription(); });

            if (HttpContext.Session.GetString("ClonableRTemplatesFilters") != null)
            {
                var filter = JsonConvert.DeserializeObject<ClonableTemplatesFilter>(HttpContext.Session.GetString("ClonableRTemplatesFilters"));
                filter.AssessmentGroup = filter.AssessmentGroup ?? model.ClonableTemplates.Select(x => x.AssessmentGroupName).ToArray();
                filter.CompetencyLevel = filter.CompetencyLevel ?? model.ClonableTemplates.Select(x => x.CompetencyLevelDesc).ToArray();
                filter.CompanyType = filter.CompanyType ?? model.ClonableTemplates.Select(x => x.CompanyTypeDesc).ToArray();
                filter.CountryType = filter.CountryType ?? model.ClonableTemplates.Select(x => x.CountryTypeDesc).ToArray();
                filter.SeniorityLevel = filter.SeniorityLevel ?? model.ClonableTemplates.Select(x => x.SeniorityLevelDesc).ToArray();

                model.ClonableTemplates = model.ClonableTemplates.Where(c => (filter.AssessmentGroup != null ? filter.AssessmentGroup.Contains(c.AssessmentGroupName) : c.AssessmentGroupName == null)
                && (filter.CompetencyLevel != null ? filter.CompetencyLevel.Contains(c.CompetencyLevelDesc) : c.CompetencyLevelDesc == null)
                && (filter.CountryType != null ? filter.CountryType.Contains(c.CountryTypeDesc) : c.CountryTypeDesc == null)
                && (filter.CompanyType != null ? filter.CompanyType.Contains(c.CompanyTypeDesc) : c.CompanyTypeDesc == null)
                && (filter.SeniorityLevel != null ? filter.SeniorityLevel.Contains(c.SeniorityLevelDesc) : c.SeniorityLevelDesc == null)).ToList();
            }
            if (TempData["SuccessErrorCloneSkillChannnelMessage"] != null)
            {
                model.Message = TempData["SuccessErrorCloneSkillChannnelMessage"].ToString();
                TempData.Remove("SuccessErrorCloneSkillChannnelMessage");
            }
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> RecommendatedIRFFilter(ClonableTemplatesFilter filter, string emailid, string uniqueid)
        {
            try
            {
                if (filter != null)
                {
                    HttpContext.Session.SetString("ClonableRTemplatesFilters", JsonConvert.SerializeObject(filter));
                }
                return RedirectToAction("RecommendatedIRFFilter", "RecommendedIRF", new { emailid = emailid, uniqueid = uniqueid });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("PublicAllMeetings", "Candidate", new { id = uniqueid });
            }
        }
        public async Task<IActionResult> ResetClonableTemplateFilter()
        {
            try
            {
                HttpContext.Session.Remove("ClonableRTemplatesFilters");
                return Json(true);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(false);
            }
        }
        [HttpPost]
        public IActionResult FetchRecommendedIRF(IRFRecommendations model, string emailid, string uniqueid)
        {
            ViewBag.emailid = emailid;
            ViewBag.uniqueid = uniqueid;
            var irfList = _recommendedIRFService.GetFetchRecommendedIRF();
            if (model.AssessmentGroupId != 0 && model.AssessmentGroupId != null)
            {
                irfList = irfList.Where(x => x.AssessmentGroupId == model.AssessmentGroupId).ToList();
            }
            if (model.CompetencyLevelId != 0 && model.CompetencyLevelId != null)
            {
                irfList = irfList.Where(x => x.CompetencyLevelId == model.CompetencyLevelId).ToList();
            }
            if (model.CountryType != 0 && model.CountryType != null)
            {
                irfList = irfList.Where(x => x.CountryType == model.CountryType).ToList();
            }
            if (model.SeniorityLevelId != 0 && model.SeniorityLevelId != null)
            {
                irfList = irfList.Where(x => x.SeniorityLevelId == model.SeniorityLevelId).ToList();
            }
            if (TempData["SuccessErrorCloneSkillChannnelMessage"] != null)
            {
                ViewBag.Message = TempData["SuccessErrorCloneSkillChannnelMessage"].ToString();
                TempData.Remove("SuccessErrorCloneSkillChannnelMessage");
            }
            return View(irfList);
        }
        [HttpGet]
        public async Task<IActionResult> CreateRecommendationIRF(int recommirfid, string emailid, string uniqueid, bool isClone = false, bool useAsCloneTemplate = false)
        {
            try
            {
                var Emailid = String.Empty;
                if (HttpContext.Session.GetString("email") != null)
                {
                    Emailid = HttpContext.Session.GetString("email")?.Trim();
                    Emailid = Emailid.Replace("\"", "");
                }
                ViewBag.emailid = emailid;
                ViewBag.uniqueid = uniqueid;

                var client = await _userRepository.GetClientUserDetails(emailid);
                int ClientId = client.ClientId;
                var model = await _recommendedIRFService.GetRecommendationIRFByIRFid(recommirfid, isClone);
                foreach (var item in model.QuesionList)
                {
                    item.UserId = client.UserId;
                }
                model.IsEdit = false;
                model.ClientId = ClientId;
                model.ClientName = client.ClientName;
                model.ClientCompName = client.ClientName;
                model.IsClone = isClone;
                model.UseAsCloneTemplate = useAsCloneTemplate;
                SetEditIRFProperties(model);
                model.OldJDLink = model.JDLink;
                model.OldCompanyDetails = model.CompanyDetails;
                if (Emailid != emailid)
                {
                    return RedirectToAction("PublicAllMeetings", "Candidate", new { id = uniqueid });
                }
                return View(model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index", "RecommendedIRF", new { emailid = emailid, uniqueid = uniqueid });
            }
        }
        [HttpPost]
        public async Task<IActionResult> CreateRecommendationIRF(IRFVm model, string emailid, string uniqueid)
        {
            try
            {
                ViewBag.emailid = emailid;
                ViewBag.uniqueid = uniqueid;
                int billingowner = 0;
                if (model.id == 0)
                {
                    var irfflag = false;
                    var IRFId = string.Empty;
                    if (String.IsNullOrEmpty(model.IRFId))
                    {
                        while (!irfflag)
                        {
                            if (!String.IsNullOrEmpty(model.ClientName))
                            {
                                IRFId = model.ClientName.Substring(0, 3);
                                var _irfassessment = model.OldAssessmentGroupId;
                                IRFId = IRFId + _irfassessment.Substring(0, 2) + GenerateRandomstring();
                            }
                            irfflag = CheckUniqueness(IRFId, false);
                        }
                        model.IRFId = IRFId.Replace(".", "a");
                        var client = await _userRepository.GetClientUserDetails(emailid);
                        billingowner = client.UserId;
                        foreach (var item in model.QuesionList)
                        {
                            item.UserId = client.UserId;
                        }
                    }
                    model.StartDate = DateTime.Now.AddDays(1);
                    model.EndDate = DateTime.Now.AddDays(120);
                    model.PriorityLevel = 0;
                    model.RisebirdHr = 381;
                    model.BillingOwner = billingowner;

                }
                bool isDuplicate = false;
                var interviewerCollection = model.InterviewerCollection.Select(x => (x.UserId));
                if (interviewerCollection.Count() != interviewerCollection.Distinct().Count())
                {
                    isDuplicate = true;
                    ModelState.AddModelError("", "Please remove duplicate entries from Interviewers' list.");
                }
                var questionsCollection = model.QuesionList.Select(x => (x.Comment, x.Skill, x.UserId));
                if (questionsCollection.Count() != questionsCollection.Distinct().Count())
                {
                    isDuplicate = true;
                    ModelState.AddModelError("", "The skill-question combination added by you should contain unique entries.");
                }
                if (isDuplicate)
                {
                    model.isDuplicateEntries = isDuplicate;
                    SetEditIRFProperties(model);
                    return View(model);
                }
                if (!model.IsClone)
                {
                    model.ResourceUrl = Request.Scheme + "://" + Request.Host.Value + Request.Path + Request.QueryString;
                }
                if (model.id == 0)
                {
                    if (!(_irfService.CheckUniqueness(model.IRFId)))
                    {
                        TempData["SuccessErrorCloneSkillChannnelMessage"] = "Sorry! Skills Channel with this name already exists!";
                        return RedirectToAction("RecommendatedIRFFilter", "RecommendedIRF", new { emailid = emailid, uniqueid = uniqueid });
                    }
                    var domain = Request.Scheme + "://" + Request.Host.Value;
                    if (String.IsNullOrEmpty(model.OldCalendlyLink))
                    {
                        //Start: Auto Generate Scheduling Link 
                        var _schedulinglink = AutoGenerateSchedulingLink(model.StartDate, model.IRFId, model.JDTiltle + "_Scheduling");
                        model.CalendlyLink = _schedulinglink;
                        //End: Auto Generate Scheduling Link
                    }

                    model.id = 0;
                    if (await _recommendedIRFService.AddRecommendationIRFDetails(model, emailid, domain))
                    {
                        TempData["SuccessErrorCloneSkillChannnelMessage"] = "Skills Channel has been saved Successfully! \n Skill Channel: " + model.IRFId + "\n " + model.CalendlyLink;
                        return RedirectToAction("RecommendatedIRFFilter", "RecommendedIRF", new { emailid = emailid, uniqueid = uniqueid });
                    }
                    else
                    {
                        TempData["SuccessErrorCloneSkillChannnelMessage"] = "Skills Channel were not saved.";
                        return RedirectToAction("RecommendatedIRFFilter", "RecommendedIRF", new { emailid = emailid, uniqueid = uniqueid });
                    }
                }
                else
                {
                    //Create update function
                    if (await _recommendedIRFService.UpdateRecommendationIRFDetails(model, emailid))
                    {
                        TempData["SuccessErrorCloneSkillChannnelMessage"] = "Skills Channel has been saved Successfully!\n Skill Channel: " + model.IRFId + "\n " + model.CalendlyLink;
                        return RedirectToAction("Index", "RecommendedIRF", new { emailid = emailid, uniqueid = uniqueid });
                    }
                    else
                    {
                        TempData["SuccessErrorCloneSkillChannnelMessage"] = "Skills Channel edits were not saved.";
                        return RedirectToAction("Index", "RecommendedIRF", new { emailid = emailid, uniqueid = uniqueid });
                    }
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorCloneSkillChannnelMessage"] = "Skills Channel were not saved";
                return RedirectToAction("Index", "RecommendedIRF", new { emailid = emailid, uniqueid = uniqueid });
            }
        }
        [HttpGet]
        public async Task<IActionResult> EditRecommendationIRF(int irfid, string emailid, string uniqueid, bool isClone = false, bool useAsCloneTemplate = false)
        {
            try
            {
                var Emailid = String.Empty;
                if (HttpContext.Session.GetString("email") != null)
                {
                    Emailid = HttpContext.Session.GetString("email")?.Trim();
                    Emailid = Emailid.Replace("\"", "");
                }
                ViewBag.emailid = emailid;
                ViewBag.uniqueid = uniqueid;
                //var client = await _userRepository.GetClientUserDetails(emailid);
                //int ClientId = client.ClientId;
                var model = await _irfService.GetEditIRF(irfid, null, isClone);
                var result = _irfService.GetTechSkillComments(irfid, null);
                var audiofilelink = _irfService.FetchIRFAudioLink(irfid);
                model.QuesionList = result;
                model.IsClone = isClone;
                model.UseAsCloneTemplate = useAsCloneTemplate;
                SetEditIRFProperties(model);
                model.OldJDLink = model.JDLink;
                model.OldCompanyDetails = model.CompanyDetails;
                model.AudioComment = audiofilelink;
                model.IsEdit = true;
                //if (Emailid != emailid)
                //{
                //    return RedirectToAction("PublicAllMeetings", "Candidate", new { id = id });
                //}
                return View("CreateRecommendationIRF", model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index", "RecommendedIRF", new { emailid = emailid, uniqueid = uniqueid });
            }
        }
        [HttpGet]
        private void SetEditIRFProperties(IRFVm model)
        {
            string prof = HttpContext.Session.GetString("profileName");
            if (prof != null && prof.Contains("Client"))
            {
                ViewBag.IsClientUser = true;
            }
            else
            {
                ViewBag.IsClientUser = false;
            }
            if (model.InterviewerCollection != null)
            {
                foreach (var item in model.InterviewerCollection)
                {
                    if (item.PerSuccessRate == 0)
                    {
                        item.PerSuccessRate = Convert.ToInt32(model.PerMintuteRate);
                    }
                }
            }
            var AssessmentsGroups = _interviewerService.FetchAssessmentGroups();
            var interviewers = _irfService.GetInterviewersForIRF(model.AssessmentGroupId);
            var _assessmentid = Convert.ToInt32(model.AssessmentGroupId);
            var _listassessment = AssessmentsGroups.Where(x => x.id == _assessmentid).ToList();
            model.OldAssessmentGroupId = _listassessment.FirstOrDefault().AssessmentGroup;
            AssessmentsGroups = AssessmentsGroups.Where(x => x.IsRelevant == true).ToList();
            if (AssessmentsGroups.Where(x => x.id == _assessmentid).Count() == 0)
            {
                AssessmentsGroups = AssessmentsGroups.Union(_listassessment);
            }
            ViewBag.Clients = new SelectList(_irfRepository.GetClients(), "id", "Name");
            ViewBag.Level = new SelectList(_irfRepository.GetLevels(), "id", "Name");
            ViewBag.LevelTech = new SelectList(_irfRepository.GetLevelTech(), "id", "Name");
            ViewBag.Skills = new SelectList(model.SkillCollection, "SkillId", "SkillName");

            var Skillset = _irfRepository.GetSkills();
            if (Skillset != null)
            {
                Skillset = Skillset.OrderBy(x => !String.IsNullOrEmpty(x.Name) ? x.Name.Trim() : "").ToList();
            }
            ViewBag.Skill = new SelectList(Skillset, "id", "Name");
            ViewBag.ClientUser = new SelectList(_irfService.GetClientUser(model.ClientId), "id", "EmailId");
            ViewBag.ClientRole = new SelectList(_irfRepository.GetClientRoles(), "id", "Name");
            ViewBag.Interviewers = new SelectList(interviewers, "UserId", "NickName");
            ViewBag.ChannelOwnerOptions = new SelectList(_irfService.GetEligibleChannelOwners(), "id", "EmailId");
            ViewBag.HRs = new SelectList(_irfRepository.GetHRs(), "UserId", "EmailId");
            var jobLevels = from JobLevels s in Enum.GetValues(typeof(JobLevels))
                            select new { ID = (int)s, Name = s.GetDescription() };
            ViewBag.JobLevelList = new SelectList(jobLevels, "ID", "Name");
            var Heuristics = from Heuristicsscorelevel s in Enum.GetValues(typeof(Heuristicsscorelevel))
                             select new { ID = (int)s, Name = s.GetDescription() };
            ViewBag.Heuristicsscorelist = new SelectList(Heuristics, "ID", "Name");
            model.AssessmentGroupList = new SelectList(AssessmentsGroups, "id", "AssessmentGroup");
            model.JoinabilityTypeList = new SelectList(_irfService.GetJoinabilityMatrix(), "id", "Name");
            model.SkillGeneralTypeList = new SelectList(_irfService.GetGeneralSkillMatrix(), "id", "Name");
            model.SelectabilityTypeList = new SelectList(_irfService.GetSelectabilityMatrix(), "id", "Name");

            var companyTypes = from CompanyType s in Enum.GetValues(typeof(CompanyType))
                               select new { ID = (int)s, Name = s.GetDescription() };
            model.CompanyTypeList = new SelectList(companyTypes, "ID", "Name");
            var priorityTypes = from PriorityLevel s in Enum.GetValues(typeof(PriorityLevel))
                                select new { ID = (int)s, Name = s.GetDescription() };
            model.PriorityLevelList = new SelectList(priorityTypes, "ID", "Name");

            var emailid = HttpContext.Session.GetString("email")?.Trim();
            emailid = emailid.Replace("\"", "");
            model.LoggedEmail = emailid;
            if (string.IsNullOrEmpty(model.Type))
            {
                model.Type = "B2B";
            }
            if (String.IsNullOrEmpty(model.AssessmentGroupId))
            {
                model.InterviewerMembers = _irfService.GetIRFInterviewerMembers(model.id).Result;
                model.InterviewerCollection = new List<TransInterviewer>();
                model.EmergencyInterviewerCollection = new List<TransEmergencyInterviewer>();
                model.QualityController = new List<TransQualityController>();
            }
            else
            {
                model.InterviewerMembers = new List<IRFInterviewerMembers>();
            }
            model.IRFRecommendations = new IRFRecommendations();
            if (model.UseAsCloneTemplate)
            {
                model.IRFRecommendations.CategoryList = new SelectList(_skillService.GetAssessmentGroupCategory(), "id", "Name");
                if (model.IRFRecommendations.CategoryId != null && model.IRFRecommendations.CategoryId.HasValue)
                {
                    model.IRFRecommendations.AssessmentGroupsList = new SelectList(_skillService.GetAssessmentGroupsByCategory(model.IRFRecommendations.CategoryId.Value).Result, "id", "AssessmentGroup");
                }
            }
            return;
        }
        public string AutoGenerateSchedulingLink(DateTime eventStartdate, string SkillChannel, string EventName)
        {
            var model_Second = new List<daytimes_second>();
            var sun = new List<perdaytime>();
            var mon = new List<perdaytime>();
            sun.Add(new perdaytime { Start = "8", End = "23" });
            mon.Add(new perdaytime { Start = "6", End = "9" });
            mon.Add(new perdaytime { Start = "19", End = "24" });
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
            model_report.BufferTime = 12;
            var eventid = _interviewSchedulerservice.AutoCreateEvent(model_report);
            var model = _interviewSchedulerservice.FetchEventById(eventid);
            if (model != null)
            {
                var eventurl = "https://" + Request.Host.ToString() + "/InterviewScheduler/ScheduleEvent?id=" + model.Uniqueid;
                return eventurl;
            }
            return "";
        }

        public bool CheckUniqueness(string value, bool isCloneTemplate = false)
        {
            return _irfService.CheckUniqueness(value, isCloneTemplate);
        }
        public string GenerateRandomstring()
        {
            //4char date or month and 1char secondUnique
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
            //var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            //var stringChars = new char[5];
            //var random = new Random();
            //for (int i = 0; i < stringChars.Length; i++)
            //{
            //    stringChars[i] = chars[random.Next(chars.Length)];
            //}
            //var finalString = new String(stringChars);
            //return finalString;
        }
        [HttpPost]
        public string RecommendedImportQuickCandidates(IFormCollection formCollection)
        {
            var IRFId = Convert.ToInt32((formCollection["IRFId"].ToString()));
            var isSendInvite = true;
            var IRFName = formCollection["IRFName"].ToString();
            try
            {
                if (HttpContext.Session.GetString("email") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (Request != null && formCollection.Files.Count > 0)
                    {
                        var file = formCollection.Files[0];

                        if ((file != null) && (file.Length > 0) && !string.IsNullOrEmpty(file.FileName))
                        {
                            FileInfo fi = new FileInfo(file.FileName);
                            if (fi.Extension == ".xls" || fi.Extension == ".xlsx")
                            {
                                var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(file);
                                if (fileMimeType == "application/x-java-archive" || fileMimeType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                                    || fileMimeType == "application/vnd.ms-excel")
                                {
                                    _applicationErrorLogger.LogMessage(LogLevelInfo.Info, $"Mime type of file {file.FileName} is {fileMimeType}");
                                    string fileName = file.FileName;
                                    string fileContentType = file.ContentType;
                                    byte[] fileBytes = new byte[file.Length];
                                    var data = file.OpenReadStream();
                                    var candidateList = new List<CandidateQuickAddModel>();
                                    using (var package = new ExcelPackage(file.OpenReadStream()))
                                    {
                                        var currentSheet = package.Workbook.Worksheets;
                                        var workSheet = currentSheet.First();
                                        var noOfCol = workSheet.Dimension.End.Column;
                                        var noOfRow = workSheet.Dimension.End.Row;
                                        for (int rowIterator = 2; rowIterator <= noOfRow; rowIterator++)
                                        {
                                            if (workSheet.Cells[rowIterator, 2].Value != null)
                                            {
                                                var candidate = new CandidateQuickAddModel
                                                {
                                                    Name = workSheet.Cells[rowIterator, 1] != null ? workSheet.Cells[rowIterator, 1].Value.ToString() : string.Empty,
                                                    EmailId = workSheet.Cells[rowIterator, 2].Value.ToString().Trim().ToLower(),
                                                    ResumeLink = (workSheet.Cells[rowIterator, 3].Value != null)
                                                        ? workSheet.Cells[rowIterator, 3].Value.ToString()
                                                        : string.Empty,
                                                    Phone = (workSheet.Cells[rowIterator, 4].Value != null)
                                                        ? ("+" + workSheet.Cells[rowIterator, 4].Value)
                                                        : string.Empty,
                                                    WhatsappNumber = workSheet.Cells[rowIterator, 5].Value != null
                                                        ? workSheet.Cells[rowIterator, 5].Value.ToString()
                                                        : string.Empty,
                                                    ClientHREmail = workSheet.Cells[rowIterator, 6].Value.ToString().ToLower(),
                                                    ProfileSource = workSheet.Cells[rowIterator, 7].Value != null ? workSheet.Cells[rowIterator, 7].Value.ToString() : string.Empty,
                                                    IRFId = IRFId
                                                };
                                                candidateList.Add(candidate);
                                            }
                                        }
                                    }
                                    StringBuilder result = new StringBuilder();
                                    if (candidateList != null && candidateList.Any())
                                    {
                                        result = _irfService.AddQuickMultipleCandidates(candidateList, emailid, profileName, IRFId, isSendInvite).Result;
                                    }
                                    else
                                    {
                                        result.Append("No candidates to add!");
                                    }
                                    return result.ToString();
                                }
                                else
                                {
                                    return "Please select valid file for import(.xls, .xslx)!";
                                }
                            }
                            else
                            {
                                return "Please select valid file for import(.xls, .xslx)!";
                            }
                        }
                        else
                        {
                            return "Failed to import candidates!";
                        }
                    }
                    return "Please select valid file for import!";
                }
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to import candidates!"; ;
            }
            return null;
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
        [HttpPost]
        public IActionResult FetchTemplateInterviewersDetails(int id)
        {
            try
            {
                var interviewers = _clonableSkillChannelTemplateService.GetClonableIRFTransInterviewer(id);
                return Json(interviewers);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("Sorry! Failed to retrive Data.");
            }
        }
        [HttpPost]
        public IActionResult FetchTemplateTechSkillDetails(int id)
        {
            try
            {
                var transTechSkills = _clonableSkillChannelTemplateService.GetClonableTechSkillsbyId(id);
                return Json(transTechSkills);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("Sorry! Failed to retrive Data.");
            }
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
    }
}
