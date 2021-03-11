using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using Risebird.Web.Controllers.AzureMediaservice;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using X.PagedList;

namespace Risebird.Web.Controllers
{
    [Authorize]
    public class InterviewerController : Controller
    {
        private IInterviewerService _interviewerService;
        private IClientService _clientService;
        private IApplicationErrorLogger _applicationErrorLogger;
        private IAzureStorageHelper _azureStorageHelper;
        private ISkillService _skillService;
        private IProfileService _profileService;
        private IInterviewerRepository _interviewerRepository;
        private IMeetingService _meetingService;
        private IIRFRepository _iRFRepository;
        private IConfiguration _configuration;
        private ISupportService _supportService;
        private readonly INotificationHelper _notificationHelper;
        private readonly IIRFService _irfService;
        private static List<InterviewerProfileVM> _modelDetails;
        private static List<Meeting> _modelDl;
        private static string Searchvalue;
        private static string Searchvalueprofile;
        private static string SearchvalueInterviewer;
        public InterviewerController(IInterviewerService interviewerService, IClientService clientService, IApplicationErrorLogger applicationErrorLogger, IAzureStorageHelper azureStorageHelper,
                                ISkillService skillService, IProfileService profileService, IInterviewerRepository interviewerRepository, IMeetingService meetingService, IIRFRepository iRFRepository, IConfiguration configuration, ISupportService supportService, INotificationHelper notificationHelper, IIRFService irfService)

        {
            _interviewerService = interviewerService;
            _clientService = clientService;
            _applicationErrorLogger = applicationErrorLogger;
            _azureStorageHelper = azureStorageHelper;
            _skillService = skillService;
            _profileService = profileService;
            _interviewerRepository = interviewerRepository;
            _meetingService = meetingService;
            _iRFRepository = iRFRepository;
            _configuration = configuration;
            _supportService = supportService;
            _notificationHelper = notificationHelper;
            _irfService = irfService;
        }



        #region Interviewer Profile functions

        public async Task<IActionResult> Index(int? Page, string searchValue)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Finance")
                    {
                        if (TempData["MessageInterviewer"] != null)
                        {
                            ViewBag.Message = TempData["MessageInterviewer"].ToString();
                            TempData.Remove("MessageInterviewer");

                        }
                        var model = _interviewerService.GetInterviewers();
                        model = model.Select(x => { x.ProfileType = ((Heuristicsscorelevel)Convert.ToInt32(x.ProfileType)).GetDescription(); x.CompentencyLevel = ((CompetencyLevel)Convert.ToInt32(x.CompentencyLevel)).GetDescription(); return x; }).ToList();
                        if (HttpContext.Session.GetString("InterviewerIndexfilters") != null)
                        {
                            var filter = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("InterviewerIndexfilters"));
                            filter.Senioritylevel = filter.Senioritylevel == null ? filter.Senioritylevel = model.Select(x => x.Senioritylevel).ToArray() : filter.Senioritylevel = filter.Senioritylevel;
                            filter.ProfileType = filter.ProfileType == null ? filter.ProfileType = model.Select(x => x.ProfileType).ToArray() : filter.ProfileType = filter.ProfileType;
                            filter.CompentencyLevel = filter.CompentencyLevel == null ? filter.CompentencyLevel = model.Select(x => x.CompentencyLevel).ToArray() : filter.CompentencyLevel = filter.CompentencyLevel;
                            filter.IndianRate = filter.IndianRate == null ? filter.IndianRate = model.Select(x => x.PerHourRateIndia).ToArray() : filter.IndianRate = filter.IndianRate;
                            filter.OutSideIndiaRate = filter.OutSideIndiaRate == null ? filter.OutSideIndiaRate = model.Select(x => x.PerHourRateOutside).ToArray() : filter.OutSideIndiaRate = filter.OutSideIndiaRate;

                            model = model.Where(c =>
                               (filter.Senioritylevel.Count() > 0 ? filter.Senioritylevel.Contains(c.Senioritylevel) : c.Senioritylevel == null)
                              && (filter.ProfileType.Count() > 0 ? filter.ProfileType.Contains(c.ProfileType) : c.ProfileType == null)
                              && (filter.CompentencyLevel.Count() > 0 ? filter.CompentencyLevel.Contains(c.CompentencyLevel) : c.CompentencyLevel == null)
                               && (filter.IndianRate.Count() > 0 ? filter.IndianRate.Contains(c.PerHourRateIndia) : c.PerHourRateIndia == null)
                          && (filter.OutSideIndiaRate.Count() > 0 ? filter.OutSideIndiaRate.Contains(c.PerHourRateOutside) : c.PerHourRateOutside == null)).ToList();

                        }
                        var Senioritylevel = _iRFRepository.GetSenioritylevel();
                        foreach (var item in model)
                        {
                            if (!String.IsNullOrEmpty(item.Senioritylevel))
                            {
                                int Seniorityid = Convert.ToInt32(item.Senioritylevel);
                                item.Senioritylevel = Senioritylevel.Where(x => x.Id == Seniorityid).FirstOrDefault().Name;
                            }
                        }
                        ViewBag.Senioritylevel = model.Select(x => x.Senioritylevel).Distinct();
                        ViewBag.ProfileType = model.Select(x => x.ProfileType).Distinct();
                        ViewBag.CompentencyLevel = model.Where(x => x.CompentencyLevel != "0").Select(x => x.CompentencyLevel).Distinct();
                        ViewBag.IndianRate = model.Select(x => x.PerHourRateIndia).Distinct();
                        ViewBag.OutSideIndiaRate = model.Select(x => x.PerHourRateOutside).Distinct();

                        if (String.IsNullOrEmpty(searchValue) && Page > 1)
                        {
                            searchValue = Searchvalueprofile;
                        }
                        Searchvalueprofile = searchValue;

                        ViewBag.Searchvalue = searchValue;
                        if (!String.IsNullOrEmpty(searchValue))
                        {
                            searchValue = searchValue.ToUpper();
                            model = model.Where(x => (!String.IsNullOrEmpty(x.FirstName) ? x.FirstName.ToUpper().Contains(searchValue) : x.FirstName == "") || ((x.PrimarySkill != null) ? x.PrimarySkill.ToUpper().Contains(searchValue) : x.PrimarySkill == "") || (!String.IsNullOrEmpty(x.EmailId) ? x.EmailId.ToUpper().Contains(searchValue) : x.EmailId == "")).ToList();

                        }
                        ViewBag.TotalCount = model.Count;

                        ViewBag.PageStartCount = 10 * ((Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page)) - 1) + 1;
                        int a = model.Count / 10;
                        if (model.Count % 10 == 0)
                        {
                            ViewBag.PageEndCount = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
                        }
                        else
                        {
                            a = a + 1;
                            if (a == Page)
                            {
                                ViewBag.PageEndCount = model.Count;
                            }
                            else
                            {
                                ViewBag.PageEndCount = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));

                            }

                        }
                        if (model.Count == 0)
                        {
                            ViewBag.PageStartCount = 0;
                            ViewBag.PageEndCount = 0;
                        }
                        _modelDetails = model;
                        return View(model.ToPagedList(Page ?? 1, 10));
                        // return View(_interviewerService.GetInterviewers());
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
                return RedirectToAction("Index");
            }
        }
        [HttpPost]
        public async Task<IActionResult> Index(int? Page, InterviewFilter filter, string searchValue)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Finance")
                    {
                        if (TempData["MessageInterviewer"] != null)
                        {
                            ViewBag.Message = TempData["MessageInterviewer"].ToString();
                            TempData.Remove("MessageInterviewer");

                        }
                        var model = _interviewerService.GetInterviewers();
                        if (filter != null)
                        {
                            HttpContext.Session.SetString("InterviewerIndexfilters", JsonConvert.SerializeObject(filter));
                        }
                        model = model.Select(x => { x.ProfileType = ((Heuristicsscorelevel)Convert.ToInt32(x.ProfileType)).GetDescription(); x.CompentencyLevel = ((CompetencyLevel)Convert.ToInt32(x.CompentencyLevel)).GetDescription(); return x; }).ToList();
                        filter.Senioritylevel = filter.Senioritylevel == null ? filter.Senioritylevel = model.Select(x => x.Senioritylevel).ToArray() : filter.Senioritylevel = filter.Senioritylevel;
                        filter.ProfileType = filter.ProfileType == null ? filter.ProfileType = model.Select(x => x.ProfileType).ToArray() : filter.ProfileType = filter.ProfileType;
                        filter.CompentencyLevel = filter.CompentencyLevel == null ? filter.CompentencyLevel = model.Select(x => x.CompentencyLevel).ToArray() : filter.CompentencyLevel = filter.CompentencyLevel;
                        filter.IndianRate = filter.IndianRate == null ? filter.IndianRate = model.Select(x => x.PerHourRateIndia).ToArray() : filter.IndianRate = filter.IndianRate;
                        filter.OutSideIndiaRate = filter.OutSideIndiaRate == null ? filter.OutSideIndiaRate = model.Select(x => x.PerHourRateOutside).ToArray() : filter.OutSideIndiaRate = filter.OutSideIndiaRate;

                        model = model.Where(c =>
                             (filter.Senioritylevel.Count() > 0 ? filter.Senioritylevel.Contains(c.Senioritylevel) : c.Senioritylevel == null)
                          && (filter.ProfileType.Count() > 0 ? filter.ProfileType.Contains(c.ProfileType) : c.ProfileType == null)
                          && (filter.CompentencyLevel.Count() > 0 ? filter.CompentencyLevel.Contains(c.CompentencyLevel) : c.CompentencyLevel == null)
                          && (filter.IndianRate.Count() > 0 ? filter.IndianRate.Contains(c.PerHourRateIndia) : c.PerHourRateIndia == null)
                          && (filter.OutSideIndiaRate.Count() > 0 ? filter.OutSideIndiaRate.Contains(c.PerHourRateOutside) : c.PerHourRateOutside == null)).ToList();

                        var Senioritylevel = _iRFRepository.GetSenioritylevel();
                        foreach (var item in model)
                        {
                            if (!String.IsNullOrEmpty(item.Senioritylevel))
                            {
                                int Seniorityid = Convert.ToInt32(item.Senioritylevel);
                                item.Senioritylevel = Senioritylevel.Where(x => x.Id == Seniorityid).FirstOrDefault().Name;
                            }
                        }
                        ViewBag.Senioritylevel = model.Select(x => x.Senioritylevel).Distinct();
                        ViewBag.ProfileType = model.Select(x => x.ProfileType).Distinct();
                        ViewBag.CompentencyLevel = model.Where(x => x.CompentencyLevel != "0").Select(x => x.CompentencyLevel).Distinct();
                        ViewBag.IndianRate = model.Select(x => x.PerHourRateIndia).Distinct();
                        ViewBag.OutSideIndiaRate = model.Select(x => x.PerHourRateOutside).Distinct();

                        if (String.IsNullOrEmpty(searchValue) && Page > 1)
                        {
                            searchValue = Searchvalueprofile;
                        }
                        Searchvalueprofile = searchValue;

                        ViewBag.Searchvalue = searchValue;
                        if (!String.IsNullOrEmpty(searchValue))
                        {
                            searchValue = searchValue.ToUpper();
                            model = model.Where(x => (!String.IsNullOrEmpty(x.FirstName) ? x.FirstName.ToUpper().Contains(searchValue) : x.FirstName == "") || ((x.PrimarySkill != null) ? x.PrimarySkill.ToUpper().Contains(searchValue) : x.PrimarySkill == "") || (!String.IsNullOrEmpty(x.EmailId) ? x.EmailId.ToUpper().Contains(searchValue) : x.EmailId == "")).ToList();

                        }
                        ViewBag.TotalCount = model.Count;

                        ViewBag.PageStartCount = 10 * ((Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page)) - 1) + 1;
                        int a = model.Count / 10;
                        if (model.Count % 10 == 0)
                        {
                            ViewBag.PageEndCount = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
                        }
                        else
                        {
                            a = a + 1;
                            if (a == Page)
                            {
                                ViewBag.PageEndCount = model.Count;
                            }
                            else
                            {
                                ViewBag.PageEndCount = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));

                            }

                        }
                        if (model.Count == 0)
                        {
                            ViewBag.PageStartCount = 0;
                            ViewBag.PageEndCount = 0;
                        }
                        _modelDetails = model;
                        return View(model.ToPagedList(Page ?? 1, 10));
                        // return View(_interviewerService.GetInterviewers());
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
                return RedirectToAction("Index");
            }
        }
        public async Task<IActionResult> ResetInterviewerIndexFilter()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("InterviewerIndexfilters");
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
                return null;
            }

        }

        [HttpGet]
        public async Task<IActionResult> AddInterviewerProfile()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR")
                    {
                        InterviewerProfileVM model = new InterviewerProfileVM();
                        // Get all the select Lists
                        GetAllSelectList(model);

                        return View("AddInterviewerProfile", model);
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
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddInterviewerProfile(InterviewerProfileVM interviewerProfile)
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

                        if (interviewerProfile.ResumeFile != null || interviewerProfile.CodedResumeFile != null)
                        {
                            if (interviewerProfile.ResumeFile != null)
                            {
                                var resumeFileName = interviewerProfile.ResumeFile.FileName.Replace(" ", "_");
                                await _azureStorageHelper.InsertBlob(resumeFileName, "resumes", interviewerProfile.ResumeFile);
                                var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                                var blob = containerRef.GetBlockBlobReference(resumeFileName);
                                uploadedFileURL = blob.Uri.ToString();
                                interviewerProfile.ResumeLink = uploadedFileURL;

                            }
                            if (interviewerProfile.CodedResumeFile != null)
                            {
                                var codedResumeFileName = interviewerProfile.CodedResumeFile.FileName.Replace(" ", "_");

                                await _azureStorageHelper.InsertBlob(codedResumeFileName, "resumes", interviewerProfile.CodedResumeFile);
                                var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                                var blob = containerRef.GetBlockBlobReference(codedResumeFileName);
                                uploadedFileURL = blob.Uri.ToString();
                                interviewerProfile.CodedResumeLink = uploadedFileURL;
                            }
                        }
                        else
                        {
                            ModelState.AddModelError(string.Empty, "Please add a Resume File");
                            InterviewerProfileVM model = new InterviewerProfileVM();
                            // Get all the select Lists
                            GetAllSelectList(model);
                            return View("AddInterviewerProfile", model);
                        }

                        if (_interviewerService.AddInterviewerProfile(interviewerProfile).Result)
                        {
                            TempData["MessageInterviewer"] = "Interviewer Profile is added Successfully";
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            TempData["MessageInterviewer"] = "Interviewer Profile addition is Failed";
                            return RedirectToAction("Index");
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
                TempData["MessageInterviewer"] = "Interviewer Profile addition is Failed";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditInterviewerProfile(int userId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR")
                    {
                        InterviewerProfileVM model = _interviewerService.GetInterviewerProfileById(userId);
                        // Get all the select Lists
                        GetAllSelectList(model);

                        return View("EditInterviewerProfile", model);
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
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditInterviewerProfile(InterviewerProfileVM interviewerProfile)
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

                        if (interviewerProfile.ResumeFile != null || interviewerProfile.CodedResumeFile != null)
                        {
                            if (interviewerProfile.ResumeFile != null)
                            {
                                var resumeFileName = interviewerProfile.ResumeFile.FileName.Replace(" ", "_");

                                await _azureStorageHelper.InsertBlob(resumeFileName, "resumes", interviewerProfile.ResumeFile);
                                var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                                var blob = containerRef.GetBlockBlobReference(resumeFileName);
                                uploadedFileURL = blob.Uri.ToString();
                                interviewerProfile.ResumeLink = uploadedFileURL;

                            }
                            if (interviewerProfile.CodedResumeFile != null)
                            {
                                var codedResumeFileName = interviewerProfile.CodedResumeFile.FileName.Replace(" ", "_");

                                await _azureStorageHelper.InsertBlob(codedResumeFileName, "resumes", interviewerProfile.CodedResumeFile);
                                var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                                var blob = containerRef.GetBlockBlobReference(codedResumeFileName);
                                uploadedFileURL = blob.Uri.ToString();
                                interviewerProfile.CodedResumeLink = uploadedFileURL;
                            }
                        }

                        if (_interviewerService.EditInterviewerProfile(interviewerProfile).Result)
                        {
                            TempData["MessageInterviewer"] = "Interviewer updated Successfully";
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            TempData["MessageInterviewer"] = "Failed to update the Interviewer";
                            return RedirectToAction("Index");
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
                TempData["MessageInterviewer"] = "Failed to update the Interviewer";
                return RedirectToAction("Index");
            }
        }
        private void GetAllSelectList(InterviewerProfileVM model)
        {
            //=========Availability============

            var Weekday = from Weekday s in Enum.GetValues(typeof(Weekday))
                          select new { ID = (int)s, Name = s.GetDescription() };
            model.WeekDayList = new SelectList(Weekday, "ID", "Name");

            var Weekend = from Weekend s in Enum.GetValues(typeof(Weekend))
                          select new { ID = (int)s, Name = s.GetDescription() };
            model.WeekEndList = new SelectList(Weekend, "ID", "Name");

            //=========Project============

            var NatureOfProject = from NatureOfProject s in Enum.GetValues(typeof(NatureOfProject))
                                  select new { ID = (int)s, Name = s.GetDescription() };
            model.NatureOfProjectList = new SelectList(NatureOfProject, "ID", "Name");

            //=========Education============

            var graduationType = from GraduationType s in Enum.GetValues(typeof(GraduationType))
                                 select new { ID = (int)s, Name = s.GetDescription() };
            model.GraduationTypeList = new SelectList(graduationType, "ID", "Name");

            var EducationMode = from EducationMode s in Enum.GetValues(typeof(EducationMode))
                                select new { ID = (int)s, Name = s.GetDescription() };
            model.EducationModeList = new SelectList(EducationMode, "ID", "Name");

            var Degree = from Degree s in Enum.GetValues(typeof(Degree))
                         select new { ID = (int)s, Name = s.GetDescription() };
            model.DegreeList = new SelectList(Degree, "ID", "Name");

            var collegeType = from CollegeType s in Enum.GetValues(typeof(CollegeType))
                              select new { ID = (int)s, Name = s.GetDescription() };
            model.CollegeTypeList = new SelectList(collegeType, "ID", "Name");

            ViewBag.Years = new SelectList(Enumerable.Range(DateTime.Today.Year - 50, 51).Select(x => new SelectListItem()
            { Text = x.ToString(), Value = x.ToString() }), "Value", "Text");

            //=========Skills============
            var Skillset = _skillService.GetSkills();
            if (Skillset != null)
            {
                Skillset = Skillset.OrderBy(x => x.Name.Trim()).ToList();
            }
            model.SkillTypeList = new SelectList(Skillset, "id", "Name");

         //   model.SkillTypeList = new SelectList(_skillService.GetSkills(), "id", "Name");

            var Skillset = _skillService.GetSkills();
            if (Skillset != null)
            {
                Skillset = Skillset.OrderBy(x => x.Name.Trim()).ToList();
            }
            model.SkillTypeList = new SelectList(Skillset, "id", "Name");

            var SkillCategory = from SkillCategory s in Enum.GetValues(typeof(SkillCategory))
                                select new { ID = (int)s, Name = s.GetDescription() };
            model.SkillCategoryList = new SelectList(SkillCategory, "ID", "Name");
            var SkillExpertiseLevel = from SkillExpertiseLevel s in Enum.GetValues(typeof(SkillExpertiseLevel))
                                      select new { ID = (int)s, Name = s.GetDescription() };
            model.SkillExpertiseLevelList = new SelectList(SkillExpertiseLevel, "ID", "Name");
            var SkillIP = from SkillIP s in Enum.GetValues(typeof(SkillIP))
                          select new { ID = (int)s, Name = s.GetDescription() };
            model.SkillIPList = new SelectList(SkillIP, "ID", "Name");
            var SkillYearsOfExperience = from SkillYearsOfExperience s in Enum.GetValues(typeof(SkillYearsOfExperience))
                                         select new { ID = (int)s, Name = s.GetDescription() };
            model.SkillYearsOfExperienceList = new SelectList(SkillYearsOfExperience, "ID", "Name");
            var SkillNature = from SkillNature s in Enum.GetValues(typeof(SkillNature))
                              select new { ID = (int)s, Name = s.GetDescription() };
            model.SkillNatureList = new SelectList(SkillNature, "ID", "Name");

            //=========Personal============

            var Country = from Country s in Enum.GetValues(typeof(Country))
                          select new { ID = (int)s, Name = s.GetDescription() };
            model.CountryList = new SelectList(Country, "ID", "Name");
            var GenderType = from Gender s in Enum.GetValues(typeof(Gender))
                             select new { ID = (int)s, Name = s.GetDescription() };
            model.GenderList = new SelectList(GenderType, "ID", "Name");
            model.LevelList = new SelectList(_interviewerService.GetLevelTech(), "id", "Name");
            var companyTypes = from CompanyType s in Enum.GetValues(typeof(CompanyType))
                               select new { ID = (int)s, Name = s.GetDescription() };
            model.CompanyTypeList = new SelectList(companyTypes, "ID", "Name");


            var dayTimeSlot = from DayTimeSlot s in Enum.GetValues(typeof(DayTimeSlot))
                              select new { ID = (int)s, Name = s.GetDescription() };
            model.DayTimeSlotList = new SelectList(dayTimeSlot, "ID", "Name");
            var daySlot = from DaySlot s in Enum.GetValues(typeof(DaySlot))
                          select new { ID = (int)s, Name = s.GetDescription() };
            model.DaySlotList = new SelectList(daySlot, "ID", "Name");
            var Assessmentgroup = _interviewerService.FetchAssessmentGroups();
            var tagList = _interviewerService.GetAllTagsFromIRF().Result;
            var selectList = new List<SelectListItem>();
            var AssessmentselectList = new List<SelectListItem>();
            foreach (var item in Assessmentgroup)
            {
                bool selected = false;
                if (model.AssessmentGroupArray != null && model.AssessmentGroupArray.Contains(item.id.ToString()))
                {
                    selected = true;
                }
                var selectItem = new SelectListItem(item.AssessmentGroup, item.id.ToString(), selected);
                AssessmentselectList.Add(selectItem);
            }
            model.AssessmentGroupList = AssessmentselectList;
        }

        public async Task<IActionResult> DeleteInterviewerProfile(int UserId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var result = await _interviewerService.DeleteInterviewerProfile(UserId);
                    return Json(result);
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(false);
            }
        }

        public JsonResult IsNickNameExists(string NickName, int UserId)
        {
            //check if the NickName exists
            IEnumerable<string> NickNamesList = _interviewerService.GetNickNames(UserId);
            if (NickNamesList.Contains(NickName))
            {
                return Json(false, new Newtonsoft.Json.JsonSerializerSettings());
            }
            else
                return Json(true, new Newtonsoft.Json.JsonSerializerSettings());
        }

        public JsonResult IsEmailIdExists(string EmailId, int UserId)
        {
            //check if the Email Id exists
            IEnumerable<string> EmailIdList = _interviewerService.GetEmailIds(UserId);
            if (EmailIdList.Contains(EmailId))
            {
                return Json(false, new Newtonsoft.Json.JsonSerializerSettings());
            }
            else
                return Json(true, new Newtonsoft.Json.JsonSerializerSettings());
        }


        #endregion

        #region Interviewer Employer functions

        [HttpGet]
        public async Task<IActionResult> IndexEmployer(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    List<InterviewerProfessional> EmployerList = _interviewerRepository.GetEmployerListById(id).Result;
                    GetAllEmployerSelectList();

                    if (TempData["InterviewerEmployerMessage"] != null)
                    {
                        ViewBag.Message = TempData["InterviewerEmployerMessage"].ToString();
                        TempData.Remove("InterviewerEmployerMessage");
                    }
                    ViewBag.IProfileId = id;

                    return View(EmployerList);

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

        [HttpGet]
        public async Task<IActionResult> AddEmployerDetails(int IProfileId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    InterviewerProfessional model = new InterviewerProfessional();
                    model.InterviewerProfileId = IProfileId;
                    GetAllEmployerSelectList();

                    return View("AddEmployerDetails", model);
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("IndexEmployer", new { id = IProfileId });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEmployerDetails(InterviewerProfessional interviewerProfessional)
        {

            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {

                    if (_interviewerService.AddEmployerProfile(interviewerProfessional).Result)
                    {
                        TempData["InterviewerEmployerMessage"] = "Adding Interviewer Employer Details is Successful";
                        return RedirectToAction("IndexEmployer", new { id = interviewerProfessional.InterviewerProfileId });
                    }
                    else
                    {
                        TempData["InterviewerEmployerMessage"] = "Adding Interviewer Employer Details is failed";
                        return RedirectToAction("IndexEmployer", new { id = interviewerProfessional.InterviewerProfileId });
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
                return RedirectToAction("IndexEmployer", new { id = interviewerProfessional.InterviewerProfileId });
            }
        }
        private void GetAllEmployerSelectList()
        {
            ViewBag.ExperienceRange = new SelectList(_interviewerService.GetExperienceRange(), "id", "Description");
            ViewBag.JobFunction = new SelectList(_interviewerService.GetJobFunction(), "id", "Description");
            ViewBag.Industry = new SelectList(_interviewerService.GetIndustry(), "id", "Description");

            var companyTypes = from CompanyType s in Enum.GetValues(typeof(CompanyType))
                               select new { ID = (int)s, Name = s.GetDescription() };
            ViewBag.CompanyTypeList = new SelectList(companyTypes, "ID", "Name");
            var CompanyStageTypes = from CompanyStage s in Enum.GetValues(typeof(CompanyStage))
                                    select new { ID = (int)s, Name = s.GetDescription() };
            ViewBag.CompanyStageList = new SelectList(CompanyStageTypes, "ID", "Name");
            var CompanySizeTypes = from CompanySize s in Enum.GetValues(typeof(CompanySize))
                                   select new { ID = (int)s, Name = s.GetDescription() };
            ViewBag.CompanySizeList = new SelectList(CompanySizeTypes, "ID", "Name");
            var Country = from Country s in Enum.GetValues(typeof(Country))
                          select new { ID = (int)s, Name = s.GetDescription() };
            ViewBag.CountryList = new SelectList(Country, "ID", "Name");
            var RoleTypes = from Role s in Enum.GetValues(typeof(Role))
                            select new { ID = (int)s, Name = s.GetDescription() };
            ViewBag.RoleList = new SelectList(RoleTypes, "ID", "Name");
            var SeniorityLevelTypes = from SeniorityLevel s in Enum.GetValues(typeof(SeniorityLevel))
                                      select new { ID = (int)s, Name = s.GetDescription() };
            ViewBag.SeniorityLevelList = new SelectList(SeniorityLevelTypes, "ID", "Name");
            var WorkNatureTypes = from WorkNature s in Enum.GetValues(typeof(WorkNature))
                                  select new { ID = (int)s, Name = s.GetDescription() };
            ViewBag.WorkNatureList = new SelectList(WorkNatureTypes, "ID", "Name");
            var PrimaryResponsibilityTypes = from PrimaryResponsibility s in Enum.GetValues(typeof(PrimaryResponsibility))
                                             select new { ID = (int)s, Name = s.GetDescription() };
            ViewBag.PrimaryResponsibilityList = new SelectList(PrimaryResponsibilityTypes, "ID", "Name");
        }

        [HttpGet]
        public async Task<IActionResult> EditEmployerDetails(int id, int IProfileId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    InterviewerProfessional ModelProfessional = _interviewerService.GetEmployerById(id);
                    // Get all the select Lists
                    GetAllEmployerSelectList();

                    return View("EditEmployerDetails", ModelProfessional);
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("IndexEmployer", new { id = IProfileId });
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> EditEmployerDetails(InterviewerProfessional interviewerProfessional)
        {
            try
            {
                //int UserId = Convert.ToInt32(TempData["UserId"]);

                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (_interviewerService.EditEmployerProfile(interviewerProfessional).Result)
                    {
                        TempData["InterviewerEmployerMessage"] = "Editing Interviewer Employer Details is Successful";
                        return RedirectToAction("IndexEmployer", new { id = interviewerProfessional.InterviewerProfileId });
                    }
                    else
                    {
                        TempData["InterviewerEmployerMessage"] = "Editing Interviewer Employer Details is failed";
                        return RedirectToAction("IndexEmployer", new { id = interviewerProfessional.InterviewerProfileId });
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
                TempData["InterviewerEmployerMessage"] = "Adding Interviewer Employer Details is failed";
                return RedirectToAction("IndexEmployer", new { id = interviewerProfessional.InterviewerProfileId });
            }
        }


        public async Task<IActionResult> DeleteEmployerDetails(int id, int IProfileId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {

                    if (_interviewerService.DeleteEmployerProfile(id).Result)
                    {
                        //TempData["InterviewerEmployerMessage"] = "Successfully Deleted";
                        return Json(new { url = Url.Action("IndexEmployer", "Interviewer", new { id = IProfileId }) });
                        //return RedirectToAction("IndexEmployer", new { id = IProfileId });
                    }
                    else
                    {
                        //TempData["InterviewerEmployerMessage"] = "Failed to Delete";
                        return Json(new { url = Url.Action("IndexEmployer", "Interviewer", new { id = IProfileId }) });
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
                //TempData["InterviewerEmployerMessage"] = "Failed to Delete";
                return Json(new { url = Url.Action("IndexEmployer", "Interviewer", new { id = IProfileId }) });
            }
        }



        #endregion

        #region Interviewer My Profile

        [HttpGet]
        public async Task<IActionResult> MyProfile()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");

                    if (profileName == "Interviewer")
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        InterviewerProfileVM model = await _interviewerService.GetInterviewerProfileByEmailId(emailid, profileName);
                        if (model == null)
                        {
                            TempData["MessageInterviewer"] = "No Information available in the System";
                            return RedirectToAction("MyInterview", "Interview");
                        }
                        // Get all the select Lists
                        GetAllSelectList(model);

                        return View("MyProfile", model);
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
                TempData["MessageInterviewer"] = "Failed";
                return RedirectToAction("Index");
            }
        }
        [HttpPost]
        public async Task<IActionResult> MyProfile(InterviewerProfileVM interviewerProfile)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    string uploadedFileURL = string.Empty;
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");

                    if (profileName == "Interviewer")
                    {
                        if (interviewerProfile.ResumeFile != null)
                        {
                            await _azureStorageHelper.InsertBlob(interviewerProfile.ResumeFile.FileName, "resumes", interviewerProfile.ResumeFile);
                            var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                            var blob = containerRef.GetBlockBlobReference(interviewerProfile.ResumeFile.FileName);
                            uploadedFileURL = blob.Uri.ToString();
                            interviewerProfile.ResumeLink = uploadedFileURL;

                        }

                        if (_interviewerService.AddInterviewerMyProfile(interviewerProfile).Result)
                        {
                            TempData["MessageInterviewer"] = "My Profile is added Successfully";
                            return RedirectToAction("MyProfile");
                        }
                        else
                        {
                            TempData["MessageInterviewer"] = "My Profile addition is Failed";
                            return RedirectToAction("MyProfile");
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
                TempData["SuccessErrorMessageInterviewer"] = "Interviewer My Profile addition is Failed";
                return RedirectToAction("Index");
            }
        }

        #endregion

        #region unapproved interviewer

        [Authorize]
        [HttpGet]
        public IActionResult OpenInterviewerDiscussion()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Interviewer")
                    {
                        var invitesList = _interviewerService.GetInterviewerInvites(emailid)?.ToList();
                        return View(invitesList);
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
                return RedirectToAction("MyProfile");
            }

        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MyScheduledDiscussions()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Interviewer")
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        var myInterview = _interviewerService.GetUnapprovedInterviewerScheduledInterviews(emailid);

                        myInterview = myInterview.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); x.JobLevelDesc = ((JobLevels)Convert.ToInt32(x.JobLevel)).GetDescription(); return x; }).ToList();
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

                            if (item.InterviewStatus.HasValue)
                            {
                                item.InterviewStatusDesc = ((InterviewStatus)(item.InterviewStatus)).GetDescription();
                            }
                            if (item.MeetingStatus.HasValue)
                            {
                                item.MeetingStatusDesc = ((MeetingStatus)(item.MeetingStatus)).GetDescription();

                            }
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


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> CancelInterview(int id, DateTime interviewDate, string intervieweeEmail)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var email = HttpContext.Session.GetString("email");
                    email = email.Replace("\"", "");
                    if (profileName == "Interviewer")
                    {
                        if (_meetingService.CancelInterview(id, interviewDate.ToString("dd MMM yyyy"), intervieweeEmail))
                        {
                            return Json(new { url = Url.Action("MyScheduledDiscussions", "Interviewer") });
                        }
                        else
                        {
                            return Json(new { url = Url.Action("MyScheduledDiscussions", "Interviewer") });
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
                return RedirectToAction("MyScheduledDiscussions");
            }
        }
        #endregion

        #region lead interviewer
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> OpenInterviewerAssessment()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Interviewer")
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        var meetings = _interviewerService.GetOpenInterviewerAssessment(emailid);
                        meetings = meetings.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();
                        foreach (var item in meetings)
                        {
                            if (!string.IsNullOrEmpty(item.JDLink))
                            {
                                try
                                {
                                    item.JDLink = new Uri(item.JDLink, UriKind.Absolute).ToString();
                                }
                                catch (Exception)
                                {
                                    item.JDLink = "";
                                }
                            }
                            else
                            {
                                item.JDLink = "";
                            }
                            if (item.HeuristicProfileType.HasValue)
                            {
                                item.HeuristicProfileTypeDesc = ((Heuristicsscorelevel)(item.HeuristicProfileType)).GetDescription();
                            }
                        }

                        if (TempData["SuccessErrorMessageLeadInterviewer"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageLeadInterviewer"].ToString();
                            TempData.Remove("SuccessErrorMessageLeadInterviewer");
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

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MyInterviewerAssessment(int? Page, string searchValue)
        {
            //add report
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Interviewer")
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        var myInterview = _interviewerService.GetAssignedInterviewerAssessment(emailid);
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
                            if (!string.IsNullOrEmpty(item.JDLink))
                            {
                                try
                                {
                                    item.JDLink = new Uri(item.JDLink, UriKind.Absolute).ToString();
                                }
                                catch (Exception)
                                {
                                    item.JDLink = "";
                                }
                            }
                            else
                            {
                                item.JDLink = "";
                            }
                            if (item.HeuristicProfileType.HasValue)
                            {
                                item.HeuristicProfileTypeDesc = ((Heuristicsscorelevel)(item.HeuristicProfileType)).GetDescription();
                            }
                            if (item.InterviewStatus.HasValue)
                            {
                                item.InterviewStatusDesc = ((InterviewStatus)(item.InterviewStatus)).GetDescription();
                            }
                            if (item.MeetingStatus.HasValue)
                            {
                                item.MeetingStatusDesc = ((MeetingStatus)(item.MeetingStatus)).GetDescription();

                            }
                        }

                        if (TempData["SuccessErrorMessageLeadInterviewer"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageLeadInterviewer"].ToString();
                            TempData.Remove("SuccessErrorMessageLeadInterviewer");
                        }


                        if (String.IsNullOrEmpty(searchValue) && Page > 1)
                        {
                            searchValue = SearchvalueInterviewer;
                        }
                        SearchvalueInterviewer = searchValue;
                        ViewBag.Searchvalue = searchValue;
                        if (!String.IsNullOrEmpty(searchValue))
                        {
                            searchValue = searchValue.ToUpper();
                            myInterview = myInterview.Where(x => (!String.IsNullOrEmpty(x.candidateEmail) ? x.candidateEmail.ToUpper().Contains(searchValue) : x.candidateEmail == "") || (!String.IsNullOrEmpty(x.PrimarySkill) ? x.PrimarySkill.ToUpper().Contains(searchValue) : x.PrimarySkill == "")).ToList();

                        }
                        ViewBag.TotalCount = myInterview.Count;

                        ViewBag.PageStartCount = 10 * ((Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page)) - 1) + 1;
                        int a = myInterview.Count / 10;
                        if (myInterview.Count % 10 == 0)
                        {
                            ViewBag.PageEndCount = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
                        }
                        else
                        {
                            a = a + 1;
                            if (a == Page)
                            {
                                ViewBag.PageEndCount = myInterview.Count;
                            }
                            else
                            {
                                ViewBag.PageEndCount = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));

                            }

                        }
                        if (myInterview.Count == 0)
                        {
                            ViewBag.PageStartCount = 0;
                            ViewBag.PageEndCount = 0;
                        }
                        _modelDl = myInterview;
                        return View(myInterview.ToPagedList(Page ?? 1, 10));
                        //return View(myInterview);
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
        //[HttpPost]
        public async Task<IActionResult> AssignYourself(int id, DateTime InterviewDate)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Interviewer")
                    {
                        var result = _interviewerService.AssignYourself(id, emailid.ToLower(), InterviewDate);
                        TempData["SuccessErrorMessageLeadInterviewer"] = result;
                        ViewBag.Message = result;
                        return RedirectToAction("OpenInterviewerAssessment");
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
                return RedirectToAction("OpenInterviewerAssessment");
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
                    if (profileName == "Interviewer")

                    {
                        if (_interviewerService.UnassignYourself(id, email.ToLower()))
                        {
                            //TempData["SuccessErrorMessageLeadInterviewer"] = "Unassigned Successfully";
                            return Json(new { url = Url.Action("MyInterviewerAssessment", "Interviewer") });
                        }
                        else
                        {
                            //TempData["SuccessErrorMessageLeadInterviewer"] = "Unassignment has Failed";
                            return Json(new { url = Url.Action("MyInterviewerAssessment", "Interviewer") });
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
                TempData["SuccessErrorMessageLeadInterviewer"] = "Unassignment failed.";
                return RedirectToAction("MyInterviewerAssessment");
            }
        }
        #endregion
        public IActionResult AddReport(int interviewId, int iscId, bool isClient = false, bool isClientSelected = false, bool isQC = false, string backUrl = null)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");

                    if (profileName == "Admin" || profileName == "HR" || profileName == "Interviewer")
                    {
                        var model = _interviewerService.GetInterviewerinteviewReport(interviewId, iscId).Result;
                        model.ProfileName = profileName;
                        GetStatusList(model, iscId);
                        model.BackUrl = backUrl;
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
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);

                return RedirectToAction("MyInterviewerAssessment");

            }
        }

        [Authorize]

        [HttpPost]
        [RequestFormLimits(MultipartBodyLengthLimit = 1048576000)]
        [RequestSizeLimit(1048576000)]
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
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    model.UserDetails = emailid;
                    if (profileName == "Admin" || profileName == "Interviewer")
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
                                model.ThumbnailImage = uploadedaudioFileURL;
                            }
                        }
                        string uploadedvideoFileURL = string.Empty;

                        if (model.VideoFile != null)
                        {
                            AzureCall _call = new AzureCall(_configuration);
                            //var resulttype = _call.CallApi(model.VideoFile, model.VideoFile.FileName, model.VideoFile.Length);
                            var resulttype = _call.CallApiUpload(model.VideoFile);
                            resulttype = resulttype.Replace("\"", "");
                            uploadedvideoFileURL = resulttype;

                            if (uploadedvideoFileURL == null)
                            {
                                ModelState.AddModelError(String.Empty, "Video Upload has failed.");
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

                        if (_interviewerService.AddInterviewerReport(model).Result)
                        {
                            TempData["SuccessErrorMessageLeadInterviewer"] = "Report saved Successfully";
                            return RedirectToAction("MyInterviewerAssessment");
                        }
                        else
                        {
                            TempData["SuccessErrorMessageLeadInterviewer"] = "Report was not saved.";
                            return RedirectToAction("MyInterviewerAssessment");
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
                TempData["SuccessErrorMessageLeadInterviewer"] = "Report was not saved.";

                return RedirectToAction("MyInterviewerAssessment");
            }
        }
        private void GetStatusList(InterviewReport model, int irfId = 0)
        {
            var interviewStatuses = from InterviewStatus s in Enum.GetValues(typeof(InterviewStatus))
                                    select new { ID = (int)s, Name = s.GetDescription() };
            model.InterviewStatusList = new SelectList(interviewStatuses, "ID", "Name");

            var meetingStatuses = from MeetingStatus s in Enum.GetValues(typeof(MeetingStatus))
                                  select new { ID = (int)s, Name = s.GetDescription() };
            meetingStatuses = meetingStatuses.Where(x => x.Name != "Scheduled").ToList();
            model.MeetingStatusList = new SelectList(meetingStatuses, "ID", "Name");

            model.Level = new SelectList(_iRFRepository.GetLevels(), "id", "Name");
            model.LevelTech = new SelectList(_iRFRepository.GetLevelTech(), "id", "Name");
            var questionsLevel = from QuestionsLevel s in Enum.GetValues(typeof(QuestionsLevel))
                                 select new { ID = (int)s, Name = s.GetDescription() };
            model.QuestionsLevelList = new SelectList(questionsLevel, "ID", "Name");
            var isApproved = from InterviewerApproved s in Enum.GetValues(typeof(InterviewerApproved))
                             select new { ID = (int)s, Name = s.GetDescription() };
            model.InterviewerApproved = new SelectList(isApproved, "ID", "Name");
            var CompanySizeTypes = from CompanySize s in Enum.GetValues(typeof(CompanySize))
                                   select new { ID = (int)s, Name = s.GetDescription() };
            model.CompanySizeList = new SelectList(CompanySizeTypes, "ID", "Name");
            //var Country = from Country s in Enum.GetValues(typeof(Country))
            //              select new { ID = (int)s, Name = s.GetDescription() };
            var Country = from WorkCountry s in Enum.GetValues(typeof(WorkCountry))
                          select new { ID = (int)s, Name = s.GetDescription() };
            model.CountryList = new SelectList(Country, "ID", "Name");
            var IndiaPrice = from IndiaPrice s in Enum.GetValues(typeof(IndiaPrice))
                             select new { ID = (int)s, Name = s.GetDescription() };
            model.IndianPrice = new SelectList(IndiaPrice, "ID", "Name");
            var OutSideIndiaPrice = from OutSideIndiaPrice s in Enum.GetValues(typeof(OutSideIndiaPrice))
                                    select new { ID = (int)s, Name = s.GetDescription() };
            model.OutSideIndiaPrice = new SelectList(OutSideIndiaPrice, "ID", "Name");
            //model.QuestionsList = new SelectList(_interviewerService.GetTechSkillComments(irfId), "Comment", "Comment");

            model.SkillQualityList = new SelectList(_iRFRepository.GetSkillQuality(), "Id", "Name");
            model.CertificationQualityList = new SelectList(_iRFRepository.GetCertificationQuality(), "Id", "Name");
            model.EducationQualityList = new SelectList(_iRFRepository.GetEducationQuality(), "Id", "Name");
            model.EmployersQualityList = new SelectList(_iRFRepository.GetEmployersQuality(), "Id", "Name");
            model.CareerProgressionList = new SelectList(_iRFRepository.GetCareerProgression(), "Id", "Name");
            model.ProjectsQualityList = new SelectList(_iRFRepository.GetProjectsQuality(), "Id", "Name");
            model.OnlinePresenceList = new SelectList(_iRFRepository.GetOnlinePresence(), "Id", "Name");
            model.BehavioralScoreList = new SelectList(_iRFRepository.GetBehavioralScore(), "Id", "Name");
            model.InterviwerCommunicationList = new SelectList(_iRFRepository.GetInterviwerCommunication(), "Id", "Name");
            model.InterviewexperienceList = new SelectList(_iRFRepository.GetInterviewexperience(), "Id", "Name");

            model.CompanyStageList = new SelectList(_iRFRepository.GetCompanyStage(), "Id", "Name");
            var skillCategories = _iRFRepository.GetSkillCategory();
            var selectList = new List<SelectListItem>();
            foreach (var item in skillCategories)
            {
                bool selected = false;
                if (model.SkillCategoryArray != null && model.SkillCategoryArray.Contains(Convert.ToString(item.Id)))
                {
                    selected = true;
                }
                var selectItem = new SelectListItem(item.Name, Convert.ToString(item.Id), selected);

                selectList.Add(selectItem);
            }
            model.SkillCategoryList = selectList;
            model.YearsofexperienceList = new SelectList(_iRFRepository.GetInterviewerExprienceYear(), "Id", "Name");
            model.SenioritylevelList = new SelectList(_iRFRepository.GetSenioritylevel(), "Id", "Name");
            model.SkillNatureList = new SelectList(_iRFRepository.GetSkillNature(), "Id", "Name");
            var Heuristics = from Heuristicsscorelevel s in Enum.GetValues(typeof(Heuristicsscorelevel))
                             select new { ID = (int)s, Name = s.GetDescription() };
            var Compentencs = from CompetencyLevel s in Enum.GetValues(typeof(CompetencyLevel))
                              select new { ID = (int)s, Name = s.GetDescription() };
            model.ProfileTypeList = new SelectList(Heuristics, "ID", "Name");
            model.CompentencyLevelList = new SelectList(Compentencs, "ID", "Name");
        }

        /// <summary>
        /// Fetch Interviewer Assessments and create Assessments
        /// </summary>
        /// <param name="Page"></param>
        /// <returns></returns>
        public IActionResult InterviewerAssessment(int? Page)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "Interviewer")
                {
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    //Fetch Assessment
                    var model = _interviewerService.GetInterviewerAssessment(emailid);
                    foreach (var item in model)
                    {
                        item.Seniority = ((Senioritylevel)item.Senioritylevel).GetDescription();
                    }
                    //Assessment model = new Assessment();
                    //model._assessment = modellist.ToList();
                    //transfer to view
                    return View("InterviewerAssessment", model.ToPagedList(Page ?? 1, 12));
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
        public IActionResult AddAssessments()
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "Interviewer")
                {
                    //Fetch Assessment Group List
                    var resultset = _interviewerService.FetchAssessmentGroups();
                    //bind Dropdown values
                    var model = new AssignmentVM();
                    model.AssessmentGrouplist = new SelectList(resultset, "id", "AssessmentGroup");
                    var _senioritylevel = from Senioritylevel s in Enum.GetValues(typeof(Senioritylevel))
                                          select new { ID = (int)s, Name = s.GetDescription() };
                    model.Senioritylevellist = new SelectList(_senioritylevel, "ID", "Name");
                    var _assessmentType = from AssessmentType s in Enum.GetValues(typeof(AssessmentType))
                                          select new { ID = (int)s, Name = s.GetDescription() };
                    model.AssessmentTypelist = new SelectList(_assessmentType, "ID", "Name");
                    var _evaluation = from Evaluation s in Enum.GetValues(typeof(Evaluation))
                                      select new { ID = (int)s, Name = s.GetDescription() };
                    model.Evaluationlist = new SelectList(_evaluation, "ID", "Name");
                    var _employerStage = from EmployerStage s in Enum.GetValues(typeof(EmployerStage))
                                         select new { ID = (int)s, Name = s.GetDescription() };
                    model.EmployerStagelist = new SelectList(_employerStage, "ID", "Name");
                    var _employerSize = from EmployerSize s in Enum.GetValues(typeof(EmployerSize))
                                        select new { ID = (int)s, Name = s.GetDescription() };
                    model.EmployerSizelist = new SelectList(_employerSize, "ID", "Name");
                    var _assessmentYearsOfExperience = from AssessmentYearsOfExperience s in Enum.GetValues(typeof(AssessmentYearsOfExperience))
                                                       select new { ID = (int)s, Name = s.GetDescription() };
                    model.YearsofExpriencelist = new SelectList(_assessmentYearsOfExperience, "ID", "Name");
                    var _assessmentLevel = from AssessmentLevel s in Enum.GetValues(typeof(AssessmentLevel))
                                           select new { ID = (int)s, Name = s.GetDescription() };
                    model.AssessmentLevellist = new SelectList(_assessmentLevel, "ID", "Name");
                    return View("AddAssessments", model);
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
        [HttpPost]
        public IActionResult AddAssessments(AssignmentVM model)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "Interviewer")
                {
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    model.Createdby = emailid;
                    var result = _interviewerService.AddAssessment(model);
                    //send mail to HR
                    if (result == "Success")
                    {
                        var hrlist = new List<String>();
                        hrlist = _supportService.GetAllSupportHR().Select(x => x.EmailId).ToList();
                        var Emailids = String.Join(";", hrlist);
                        Mail _mail = new Mail
                        {
                            TO = Emailids,
                            Subject = "Risebird: Interviewer send assessment for approval"
                        };
                        var resultset = _notificationHelper.SendAssessmentNotification(_mail, emailid);
                        return RedirectToAction("InterviewerAssessment");
                    }
                    return View("AddAssessments");
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
        public IActionResult AssessmentForApproval(int id, string EmailAddress)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "Interviewer")
                {
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    //Fetch Assessment
                    var model = _interviewerService.GetInterviewerAssessment(EmailAddress);
                    ViewBag.indexid = id;
                    foreach (var item in model)
                    {
                        item.Seniority = ((Senioritylevel)item.Senioritylevel).GetDescription();
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
        /// <summary>
        /// Update Assessment Status like : Approve or reject
        /// </summary>
        /// <param name="assessmentid"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public JsonResult ChangeStatusAssessment(int assessmentid, int status)
        {
            var model = _interviewerService.ChangeStatusAssessment(assessmentid, status);
            return Json("Success");
        }
        /// <summary>
        /// Send Email to interviewer for update any details in Assessment
        /// </summary>
        /// <param name="assessmentid"></param>
        /// <param name="EmailAddress"></param>
        /// <param name="assessment"></param>
        /// <param name="comments"></param>
        /// <returns></returns>
        public JsonResult ShareAssessmentcomment(int assessmentid, string EmailAddress, string assessment, string comments)
        {
            Mail _mail = new Mail
            {
                TO = EmailAddress,
                Subject = "Risebird: Review Comments on your assessment"
            };
            var resultset = _notificationHelper.SendAssessmentNotificationComments(_mail, assessment, comments);
            return Json("Success");
        }
        /// <summary>
        /// Edit Assessment Details By interviewer
        /// </summary>
        /// <returns></returns>
        public IActionResult EditAssessment(int id)
        {
            var result = _irfService.FetchAssessmentdetails(id);
            var model = result.FirstOrDefault();
            var _senioritylevel = from Senioritylevel s in Enum.GetValues(typeof(Senioritylevel))
                                  select new { ID = (int)s, Name = s.GetDescription() };
            model.Senioritylevellist = new SelectList(_senioritylevel, "ID", "Name");
            var _assessmentType = from AssessmentType s in Enum.GetValues(typeof(AssessmentType))
                                  select new { ID = (int)s, Name = s.GetDescription() };
            model.AssessmentTypelist = new SelectList(_assessmentType, "ID", "Name");
            var _evaluation = from Evaluation s in Enum.GetValues(typeof(Evaluation))
                              select new { ID = (int)s, Name = s.GetDescription() };
            model.Evaluationlist = new SelectList(_evaluation, "ID", "Name");
            var _employerStage = from EmployerStage s in Enum.GetValues(typeof(EmployerStage))
                                 select new { ID = (int)s, Name = s.GetDescription() };
            model.EmployerStagelist = new SelectList(_employerStage, "ID", "Name");
            var _employerSize = from EmployerSize s in Enum.GetValues(typeof(EmployerSize))
                                select new { ID = (int)s, Name = s.GetDescription() };
            model.EmployerSizelist = new SelectList(_employerSize, "ID", "Name");
            var _assessmentYearsOfExperience = from AssessmentYearsOfExperience s in Enum.GetValues(typeof(AssessmentYearsOfExperience))
                                               select new { ID = (int)s, Name = s.GetDescription() };
            model.YearsofExpriencelist = new SelectList(_assessmentYearsOfExperience, "ID", "Name");
            var _assessmentLevel = from AssessmentLevel s in Enum.GetValues(typeof(AssessmentLevel))
                                   select new { ID = (int)s, Name = s.GetDescription() };
            model.AssessmentLevellist = new SelectList(_assessmentLevel, "ID", "Name");
            return View(model);
        }
        [HttpPost]
        public IActionResult EditAssessment(AssignmentVM model)
        {
            var result = _interviewerService.UpdateAssessment(model);
            return RedirectToAction("InterviewerAssessment");
        }


        [HttpPost]

        public JsonResult InterviewerProfileJson(string searchValue)
        {
            try
            {
                int Page = 1;
                var model = _modelDetails;
                Searchvalue = searchValue;
                int totalcount = model.Count;
                int Pagestart = 0;
                int Pageend = 0;

                ViewBag.Searchvalue = searchValue;
                if (!String.IsNullOrEmpty(searchValue))
                {
                    searchValue = searchValue.ToUpper();
                    model = model.Where(x => (!String.IsNullOrEmpty(x.FirstName) ? x.FirstName.ToUpper().Contains(searchValue) : x.FirstName == "") || (!String.IsNullOrEmpty(x.EmailId) ? x.EmailId.ToUpper().Contains(searchValue) : x.EmailId == "") || ((x.PrimarySkill != null) ? x.PrimarySkill.ToUpper().Contains(searchValue) : x.PrimarySkill == "")).ToList();

                }
                ViewBag.TotalCount = model.Count;

                Pagestart = 10 * ((Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page)) - 1) + 1;
                int a = model.Count / 10;
                if (model.Count % 10 == 0)
                {
                    Pageend = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
                }
                else
                {
                    a = a + 1;
                    if (a == Page)
                    {
                        Pageend = model.Count;
                    }
                    else
                    {
                        Pageend = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));

                    }

                }
                if (model.Count == 0)
                {
                    Pagestart = 0;
                    Pageend = 0;
                }

                return Json(new { result = model.Take(10), startPage = Pagestart, endPage = Pageend, totalPage = a, totalentry = model.Count });
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("failed");
            }
        }

        [HttpPost]
        public JsonResult InterviewerAssessmentJson(string searchValue)
        {
            try
            {
                int Page = 1;
                var myInterview = _modelDl;
                Searchvalue = searchValue;
                int totalcount = myInterview.Count;
                int Pagestart = 0;
                int Pageend = 0;

                ViewBag.Searchvalue = searchValue;
                if (!String.IsNullOrEmpty(searchValue))
                {
                    searchValue = searchValue.ToUpper();
                    myInterview = myInterview.Where(x => (!String.IsNullOrEmpty(x.candidateEmail) ? x.candidateEmail.ToUpper().Contains(searchValue) : x.candidateEmail == "") || (!String.IsNullOrEmpty(x.PrimarySkill) ? x.PrimarySkill.ToUpper().Contains(searchValue) : x.PrimarySkill == "")).ToList();

                }
                ViewBag.TotalCount = myInterview.Count;

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

        public async Task<IActionResult> InterviewerRecommendation(int id)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR")
                {
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    //Fetch Assessment
                    var model = await _interviewerService.GetInterviewerRecommendationDetails(id);
                    if (model == null)
                    {
                        model = new InterviewReport();
                    }
                    GetStatusList(model);
                    ViewBag.indexid = id;
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InterviewerRecommendation(InterviewReport model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    model.UserDetails = emailid;
                    if (profileName == "Admin" || profileName == "HR")
                    {
                        if (_interviewerService.InterviewerRecommendation(model).Result)
                        {
                            TempData["MessageInterviewer"] = "Recommendation saved Successfully";
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            TempData["MessageInterviewer"] = "Recommendation was not saved.";
                            return RedirectToAction("Index");
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
                TempData["MessageInterviewer"] = "Recommendation was not saved.";
                return RedirectToAction("Index");
            }
        }
    }


}