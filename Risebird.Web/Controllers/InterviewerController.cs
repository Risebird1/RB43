using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using OfficeOpenXml;
using RestSharp;
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
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using X.PagedList.Extensions;
using static iTextSharp.text.pdf.AcroFields;

namespace Risebird.Web.Controllers
{
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
        private readonly IUserRepository _userRepository;
        private static List<InterviewerProfileVM> _modelDetails;
        private static List<Meeting> _modelDl;
        private static string Searchvalue;
        private static string Searchvalueprofile;
        private static string SearchvalueInterviewer;
        private readonly IFileMimeTypeHelper _fileMimeTypeHelper;
        private readonly string codeapiendpoint;
        private readonly IAadhaarService _aadhaarService;
        private readonly IWhatsAppHelper _whatsAppHelper;
        private readonly ISASUrlHelper _sasUrlHelper;
        private readonly IAffIndaResumeParserHelper _affIndaResumeParserHelper;
        private readonly IEncryptionHelper _encryptionHelper;
        private readonly IAssistantServiceHelper _assistantServiceHelper;
        private readonly IPdfReaderHelper _pdfReaderHelper;

        public InterviewerController(IInterviewerService interviewerService, IClientService clientService, IApplicationErrorLogger applicationErrorLogger, IAzureStorageHelper azureStorageHelper,
        ISkillService skillService, IProfileService profileService,
        IInterviewerRepository interviewerRepository, IMeetingService meetingService,
        IIRFRepository iRFRepository, IConfiguration configuration, ISupportService supportService,
        INotificationHelper notificationHelper, IIRFService irfService, IUserRepository userRepository,
        IFileMimeTypeHelper fileMimeTypeHelper, IAadhaarService aadhaarService,
        IWhatsAppHelper whatsAppHelper, ISASUrlHelper sasUrlHelper,
        IAffIndaResumeParserHelper affIndaResumeParserHelper, IEncryptionHelper encryptionHelper,
        IAssistantServiceHelper assistantServiceHelper, IPdfReaderHelper pdfReaderHelper)

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
            _userRepository = userRepository;
            _fileMimeTypeHelper = fileMimeTypeHelper;
            codeapiendpoint = _configuration["CodeApiEndPoint"];
            _aadhaarService = aadhaarService;
            _whatsAppHelper = whatsAppHelper;
            _sasUrlHelper = sasUrlHelper;
            _affIndaResumeParserHelper = affIndaResumeParserHelper;
            _encryptionHelper = encryptionHelper;
            _assistantServiceHelper = assistantServiceHelper;
            _pdfReaderHelper = pdfReaderHelper;
        }



        #region Interviewer Profile functions
        [HttpGet]
        public async Task<IActionResult> Index(int? Page, string searchValue)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Finance" || profileName == "SeniorInterviewerManagement" || profileName == "SupportAdmin" || profileName == "MappingAdmin")//|| profileName == "InterviewerManagement" 
                    {
                        ViewBag.profile = profileName;
                        if (TempData["MessageInterviewer"] != null)
                        {
                            ViewBag.Message = TempData["MessageInterviewer"].ToString();
                            TempData.Remove("MessageInterviewer");

                        }
                        var model = _interviewerService.GetInterviewers();

                        // ── STEP 1: restore paginated search value, then filter raw fields first ──
                        if (string.IsNullOrEmpty(searchValue) && Page > 1)
                        {
                            searchValue = Searchvalueprofile;
                        }
                        Searchvalueprofile = searchValue;
                        ViewBag.Searchvalue = searchValue;
                        if (!string.IsNullOrEmpty(searchValue))
                        {
                            var sv = searchValue.ToUpper().Trim();
                            model = model.Where(x =>
                                (!string.IsNullOrEmpty(x.FirstName) && x.FirstName.ToUpper().Contains(sv)) ||
                                (!string.IsNullOrEmpty(x.LastName) && x.LastName.ToUpper().Contains(sv)) ||
                                (!string.IsNullOrEmpty(x.EmailId) && x.EmailId.ToUpper().Contains(sv)) ||
                                (!string.IsNullOrEmpty(x.NickName) && x.NickName.ToUpper().Contains(sv)) ||
                                (!string.IsNullOrEmpty(x.MobileNumber) && x.MobileNumber.Contains(sv)) ||
                                (x.PrimarySkill != null && x.PrimarySkill.ToUpper().Contains(sv))
                            ).ToList();
                        }

                        // ── STEP 2: convert enum fields (only matching rows) ──────────────────
                        model = model.Select(x => { x.ProfileType = ((Heuristicsscorelevel)Convert.ToInt32(x.ProfileType)).GetDescription(); x.CompentencyLevel = ((CompetencyLevel)Convert.ToInt32(x.CompentencyLevel)).GetDescription(); x.AvailableDetails = ((InterviewerAvailabilityStatus)Convert.ToInt32(x.Isavailable)).GetDescription(); return x; }).ToList();

                        // ── STEP 3: AssessmentGroupId IDs → names (O(1) lookup, only matching rows)
                        var AssessmentsGroups = _interviewerService.FetchAssessmentGroups();
                        var assessmentGroupDictIdx = AssessmentsGroups.ToDictionary(x => x.id, x => x.AssessmentGroup);
                        foreach (var item in model)
                        {
                            if (!string.IsNullOrEmpty(item.AssessmentGroupId))
                            {
                                item.AssessmentGroupId = string.Join(" , ",
                                    item.AssessmentGroupId.Split(',')
                                        .Where(g => !string.IsNullOrWhiteSpace(g))
                                        .Select(g => {
                                            int id = int.TryParse(g.Trim(), out int v) ? v : 0;
                                            return assessmentGroupDictIdx.TryGetValue(id, out var name) ? name : string.Empty;
                                        })
                                        .Where(n => !string.IsNullOrEmpty(n)));
                            }
                        }

                        // ── STEP 4: session column filters ────────────────────────────────────
                        if (HttpContext.Session.GetString("InterviewerIndexfilters") != null)
                        {
                            var filter = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("InterviewerIndexfilters"));
                            filter.Senioritylevel = filter.Senioritylevel == null ? filter.Senioritylevel = model.Select(x => x.Senioritylevel).ToArray() : filter.Senioritylevel = filter.Senioritylevel;
                            filter.ProfileType = filter.ProfileType == null ? filter.ProfileType = model.Select(x => x.ProfileType).ToArray() : filter.ProfileType = filter.ProfileType;
                            filter.CompentencyLevel = filter.CompentencyLevel == null ? filter.CompentencyLevel = model.Select(x => x.CompentencyLevel).ToArray() : filter.CompentencyLevel = filter.CompentencyLevel;
                            filter.IndianRate = filter.IndianRate == null ? filter.IndianRate = model.Select(x => x.PerHourRateIndia).ToArray() : filter.IndianRate = filter.IndianRate;
                            filter.OutSideIndiaRate = filter.OutSideIndiaRate == null ? filter.OutSideIndiaRate = model.Select(x => x.PerHourRateOutside).ToArray() : filter.OutSideIndiaRate = filter.OutSideIndiaRate;
                            filter.CompanyType = filter.CompanyType == null ? filter.CompanyType = model.Select(x => x.CompanyType).ToArray() : filter.CompanyType = filter.CompanyType;
                            filter.Domain = filter.Domain == null ? filter.Domain = model.Select(x => x.Country).ToArray() : filter.Domain = filter.Domain;
                            filter.AssessmentGroup = filter.AssessmentGroup == null ? filter.AssessmentGroup = model.Select(x => x.AssessmentGroupId).ToArray() : filter.AssessmentGroup = filter.AssessmentGroup;
                            filter.IsLeadInterviewer = filter.IsLeadInterviewer == null ? filter.IsLeadInterviewer = model.Select(x => x.IsLeadInterviewer.ToString()).ToArray() : filter.IsLeadInterviewer = filter.IsLeadInterviewer;
                            filter.IsActive = filter.IsActive == null ? filter.IsActive = model.Select(x => x.IsActive).ToArray() : filter.IsActive = filter.IsActive;
                            filter.IsInterested = filter.IsInterested == null ? filter.IsInterested = model.Select(x => x.IsInterested).ToArray() : filter.IsInterested = filter.IsInterested;

                            model = model.Where(c =>
                               (filter.Senioritylevel.Count() > 0 ? filter.Senioritylevel.Contains(c.Senioritylevel) : c.Senioritylevel == null)
                              && (filter.ProfileType.Count() > 0 ? filter.ProfileType.Contains(c.ProfileType) : c.ProfileType == null)
                              && (filter.CompentencyLevel.Count() > 0 ? filter.CompentencyLevel.Contains(c.CompentencyLevel) : c.CompentencyLevel == null)
                               && (filter.IndianRate.Count() > 0 ? filter.IndianRate.Contains(c.PerHourRateIndia) : c.PerHourRateIndia == null)
                              && (filter.OutSideIndiaRate.Count() > 0 ? filter.OutSideIndiaRate.Contains(c.PerHourRateOutside) : c.PerHourRateOutside == null)
                              && (filter.CompanyType.Count() > 0 ? filter.CompanyType.Contains(c.CompanyType) : c.CompanyType == null)
                              && (filter.Domain.Count() > 0 ? filter.Domain.Contains(c.Country) : c.Country == null)
                              && (filter.AssessmentGroup.Count() > 0 ? filter.AssessmentGroup.Contains(c.AssessmentGroupId) : c.AssessmentGroupId == null)
                              && (filter.IsLeadInterviewer.Count() > 0 ? filter.IsLeadInterviewer.Contains(c.IsLeadInterviewer.ToString()) : c.IsLeadInterviewer.ToString() == null)
                              && (filter.IsActive.Count() > 0 ? filter.IsActive.Contains(c.IsActive) : c.IsActive == true)
                              && (filter.IsInterested.Count() > 0 ? filter.IsInterested.Contains(c.IsInterested) : c.IsInterested == true)).ToList();
                        }
                        else
                        {
                            model = model.Where(x => x.IsActive == true).ToList();
                        }

                        // ── STEP 5: Seniority IDs → names (O(1) lookup, only rows after all filters)
                        var Senioritylevel = _iRFRepository.GetSenioritylevel();
                        var seniorityDictIdx = Senioritylevel.ToDictionary(x => x.Id, x => x.Name);
                        foreach (var item in model)
                        {
                            if (!string.IsNullOrEmpty(item.Senioritylevel))
                            {
                                item.Senioritylevel = string.Join(" , ",
                                    item.Senioritylevel.Split(',')
                                        .Where(s => !string.IsNullOrWhiteSpace(s))
                                        .Select(s => {
                                            int id = int.TryParse(s.Trim(), out int v) ? v : 0;
                                            return seniorityDictIdx.TryGetValue(id, out var name) ? name : string.Empty;
                                        })
                                        .Where(n => !string.IsNullOrEmpty(n)));
                            }
                        }
                        var Country = from WorkCountry s in Enum.GetValues(typeof(WorkCountry))
                                      select new { ID = (int)s, Name = s.GetDescription() };
                        var companyTypes = from CompanyType s in Enum.GetValues(typeof(CompanyType))
                                           select new { ID = (int)s, Name = s.GetDescription() };
                        var IndiaPrice = from IndiaPrice s in Enum.GetValues(typeof(IndiaPrice))
                                         select new { ID = (int)s, Name = s.GetDescription() };
                        var OutSideIndiaPrice = from OutSideIndiaPrice s in Enum.GetValues(typeof(OutSideIndiaPrice))
                                                select new { ID = (int)s, Name = s.GetDescription() };
                        var Heuristics = from Heuristicsscorelevel s in Enum.GetValues(typeof(Heuristicsscorelevel))
                                         select new { ID = (int)s, Name = s.GetDescription() };
                        var _isleadinterviewer = from LeadInterviewerenum s in Enum.GetValues(typeof(LeadInterviewerenum))
                                                 select new { ID = (int)s, Name = s.GetDescription() };
                        //ViewBag.Senioritylevel = model.Select(x => x.Senioritylevel).Distinct();
                        ViewBag.Senioritylevel = Senioritylevel.OrderBy(x => x.Name).Select(x => x.Name).Distinct();
                        ViewBag.ProfileType = Heuristics.Select(x => x.Name).Distinct();
                        ViewBag.CompentencyLevel = model.Where(x => x.CompentencyLevel != "0").Select(x => x.CompentencyLevel).Distinct();
                        ViewBag.IndianRate = IndiaPrice.Select(x => x.Name).Distinct();
                        ViewBag.OutSideIndiaRate = OutSideIndiaPrice.Select(x => x.Name).Distinct();
                        ViewBag.CompanyType = companyTypes.Select(x => x.Name).Distinct();
                        ViewBag.Country = Country.Select(x => x.Name).Distinct();
                        //ViewBag.AssessmentGroup = model.Select(x => x.AssessmentGroupId).Distinct();
                        ViewBag.AssessmentGroup = AssessmentsGroups.OrderBy(x => x.AssessmentGroup).Select(x => x.AssessmentGroup).Distinct();
                        ViewBag.IsLeadInterviewer = _isleadinterviewer.OrderBy(x => x.Name).Select(x => x.Name).Distinct();

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
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Finance" || profileName == "SeniorInterviewerManagement" || profileName == "SupportAdmin" || profileName == "MappingAdmin")
                    {
                        if (filter != null)
                        {
                            HttpContext.Session.SetString("InterviewerIndexfilters", JsonConvert.SerializeObject(filter));
                        }
                        return RedirectToAction("Index", new { Page = Page, searchValue = searchValue });
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

        [HttpGet]
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
                    if (profileName == "Admin" || profileName == "HR" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement" || profileName == "SupportAdmin" || profileName == "MappingAdmin")
                    {
                        InterviewerProfileVM model = new InterviewerProfileVM();
                        model.Country = "26";
                        // Get all the select Lists
                        await GetAllSelectList(model);
                        if (TempData["MessageInterviewerManagement"] != null)
                        {
                            ViewBag.Message = TempData["MessageInterviewerManagement"].ToString();
                            TempData.Remove("MessageInterviewerManagement");

                        }
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
                    var emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement" || profileName == "SupportAdmin" || profileName == "MappingAdmin")
                    {
                        string uploadedFileURL = string.Empty;

                        //if (string.IsNullOrEmpty(interviewerProfile.ResumeLink) && (interviewerProfile.ResumeFile != null || interviewerProfile.CodedResumeFile != null))
                        //{
                        //    if (interviewerProfile.ResumeFile != null)
                        //    {
                        //        var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(interviewerProfile.ResumeFile);
                        //        if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                        //                            || fileMimeType == "application/pdf" || fileMimeType == "application/x-java-archive" || fileMimeType == "application/vnd.ms-powerpoint")
                        //        {
                        //            var resumeFileName = interviewerProfile.ResumeFile.FileName.Replace(" ", "_");
                        //            await _azureStorageHelper.InsertBlob(resumeFileName, "resumes", interviewerProfile.ResumeFile);
                        //            var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                        //            var blob = containerRef.GetBlockBlobReference(resumeFileName);
                        //            uploadedFileURL = blob.Uri.ToString();
                        //            interviewerProfile.ResumeLink = uploadedFileURL;
                        //        }


                        //    }
                        //    if (interviewerProfile.CodedResumeFile != null)
                        //    {
                        //        var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(interviewerProfile.CodedResumeFile);
                        //        if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                        //                            || fileMimeType == "application/pdf" || fileMimeType == "application/x-java-archive" || fileMimeType == "application/vnd.ms-powerpoint")
                        //        {
                        //            var codedResumeFileName = interviewerProfile.CodedResumeFile.FileName.Replace(" ", "_");

                        //            await _azureStorageHelper.InsertBlob(codedResumeFileName, "resumes", interviewerProfile.CodedResumeFile);
                        //            var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                        //            var blob = containerRef.GetBlockBlobReference(codedResumeFileName);
                        //            uploadedFileURL = blob.Uri.ToString();
                        //            interviewerProfile.CodedResumeLink = uploadedFileURL;
                        //        }

                        //    }
                        //}
                        //else
                        //{
                        //    ModelState.AddModelError(string.Empty, "Please add a Resume File");
                        //    InterviewerProfileVM model = new InterviewerProfileVM();
                        //    // Get all the select Lists
                        //    await GetAllSelectList(model);
                        //    return View("AddInterviewerProfile", model);
                        //}

                        if (!string.IsNullOrEmpty(interviewerProfile.FirstName))
                        {
                            interviewerProfile.FirstName = interviewerProfile.FirstName.Trim();
                        }
                        if (!string.IsNullOrEmpty(interviewerProfile.LastName))
                        {
                            interviewerProfile.LastName = interviewerProfile.LastName.Trim();
                        }
                        if (!string.IsNullOrEmpty(interviewerProfile.EmailId))
                        {
                            interviewerProfile.EmailId = interviewerProfile.EmailId.Trim().ToLower();
                        }
                        string assessmentGroupList;
                        int userId;
                        string uniqueid;
                        interviewerProfile.ResourceUrl = Request.Scheme + "://" + Request.Host.Value + Request.Path + Request.QueryString;
                        if (_interviewerService.AddInterviewerProfile(interviewerProfile, emailId, out assessmentGroupList, out userId, out uniqueid))
                        {
                            TempData["MessageInterviewerOnboarding"] = "Assessment Groups mapped successfully. Kindly select the target Skill Channel from the drop-down to finish onboarding!";
                            return RedirectToAction("EditInterviewerProfile", new { userId = userId, showAddToSkillChannelOption = true });
                            //return RedirectToAction("Index");
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(assessmentGroupList))
                            {
                                if (profileName == "InterviewerManagement")
                                {
                                    //TempData["MessageAddInterviewer"] = $"Failed to add Interviewer Profile. Cannot map interviewer to the following Assessment Groups as 100 interviewers have already been mapped to the Assessment Group(s) : \n '{assessmentGroupList}' ";
                                    ViewBag.Message = $"Failed to add Interviewer Profile. Cannot map interviewer to the following Assessment Groups as 100 interviewers have already been mapped to the Assessment Group(s) : \n '{assessmentGroupList}' ";
                                    return View(interviewerProfile);
                                }
                                else
                                {
                                    TempData["MessageInterviewer"] = $"Failed to add Interviewer Profile. Cannot map interviewer to the following Assessment Groups as 100 interviewers have already been mapped to the Assessment Group(s) : \n '{assessmentGroupList}' ";

                                }
                            }
                            else
                            {
                                if (profileName == "InterviewerManagement")
                                {
                                    //TempData["MessageAddInterviewer"] = "Failed to add Interviewer Profile.";
                                    ViewBag.Message = "Failed to add Interviewer Profile.";
                                    return View(interviewerProfile);
                                }
                                else
                                {
                                    TempData["MessageInterviewer"] = "Failed to add Interviewer Profile.";
                                }
                            }

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
        public async Task<IActionResult> EditInterviewerProfile(int userId, bool showAddToSkillChannelOption = false)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Finance" || profileName == "SeniorInterviewerManagement" || profileName == "SupportAdmin" || profileName == "MappingAdmin" || (showAddToSkillChannelOption && profileName == "InterviewerManagement"))
                    {
                        InterviewerProfileVM model = _interviewerService.GetInterviewerProfileById(userId);
                        model.ShowAddToSkillChannelOption = showAddToSkillChannelOption;

                        // Get all the select Lists
                        await GetAllSelectList(model);
                        if (TempData["MessageInterviewerOnboarding"] != null)
                        {
                            ViewBag.Message = TempData["MessageInterviewerOnboarding"].ToString();
                            TempData.Remove("MessageInterviewerOnboarding");

                        }
                        ViewBag.Profile = profileName;
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

        /// <summary>
        /// Verify interviewer bank account via Razorpay and update TransInterviewerAccDetails (IsVerified, RazorpayValidationId).
        /// Admin/HR/Finance only.
        /// </summary>
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> VerifyBankAccount(int interviewerProfileId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null || HttpContext.Session.GetString("profileName") == null)
                    return RedirectToAction("SignOut", "Session");
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName != "Admin" && profileName != "HR" && profileName != "Finance")
                    return RedirectToAction("SignOut", "Session");

                var (success, message) = await _interviewerService.VerifyBankAccountViaRazorpay(interviewerProfileId);
                TempData["MessageInterviewer"] = message;
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["MessageInterviewer"] = "Bank account verification failed.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditInterviewerProfile(InterviewerProfileVM interviewerProfile)
        {
            string profileName = string.Empty;
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorInterviewerManagement" || profileName == "SupportAdmin" || profileName == "MappingAdmin" || (interviewerProfile.ShowAddToSkillChannelOption && profileName == "InterviewerManagement"))
                    {
                        if (interviewerProfile.ShowAddToSkillChannelOption)
                        {

                            if (await _interviewerService.AddTargetSkillChannelToInterviewerPreOnboarding(interviewerProfile))
                            {
                                TempData["MessageInterviewerManagement"] = "Interviewer onboarded successfully!";
                                //return RedirectToAction("Index");
                                return RedirectToAction("AddInterviewerProfile");
                                //return RedirectToAction("EditInterviewerProfile", new { userId = userId, showAddToSkillChannelOption = true });

                            }
                            else
                            {
                                TempData["MessageInterviewerManagement"] = "Failed to update the Interviewer";
                                return RedirectToAction("AddInterviewerProfile");
                                //return RedirectToAction("Index");

                                //if (!string.IsNullOrEmpty(assessmentGroupList))
                                //{
                                //    TempData["MessageInterviewer"] = $"Failed to update Interviewer Profile. Cannot map interviewer to the following Assessment Groups as 100 interviewers have already been mapped to the Assessment Group(s) : \n '{assessmentGroupList}' ";
                                //}
                                //else
                                //{
                                //    TempData["MessageInterviewer"] = "Failed to update the Interviewer";

                                //}
                                //return RedirectToAction("Index");
                            }

                        }
                        else
                        {
                            string uploadedFileURL = string.Empty;

                            if (interviewerProfile.ResumeFile != null || interviewerProfile.CodedResumeFile != null)
                            {
                                if (interviewerProfile.ResumeFile != null)
                                {
                                    var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(interviewerProfile.ResumeFile);
                                    if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                                                        || fileMimeType == "application/pdf" || fileMimeType == "application/x-java-archive" || fileMimeType == "application/vnd.ms-powerpoint")
                                    {
                                        var resumeFileName = interviewerProfile.ResumeFile.FileName.Replace(" ", "_");

                                        await _azureStorageHelper.InsertBlob(resumeFileName, "resumes", interviewerProfile.ResumeFile);
                                        var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                                        var blob = containerRef.GetBlockBlobReference(resumeFileName);
                                        uploadedFileURL = blob.Uri.ToString();
                                        interviewerProfile.ResumeLink = uploadedFileURL;
                                    }


                                }
                                if (interviewerProfile.CodedResumeFile != null)
                                {
                                    var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(interviewerProfile.CodedResumeFile);
                                    if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                                                        || fileMimeType == "application/pdf" || fileMimeType == "application/x-java-archive" || fileMimeType == "application/vnd.ms-powerpoint")
                                    {
                                        var codedResumeFileName = interviewerProfile.CodedResumeFile.FileName.Replace(" ", "_");

                                        await _azureStorageHelper.InsertBlob(codedResumeFileName, "resumes", interviewerProfile.CodedResumeFile);
                                        var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                                        var blob = containerRef.GetBlockBlobReference(codedResumeFileName);
                                        uploadedFileURL = blob.Uri.ToString();
                                        interviewerProfile.CodedResumeLink = uploadedFileURL;
                                    }

                                }
                            }
                            string assessmentGroupList;
                            string uniqueid;
                            interviewerProfile.ResourceUrl = Request.Scheme + "://" + Request.Host.Value + Request.Path + Request.QueryString;
                            if (_interviewerService.EditInterviewerProfile(interviewerProfile, emailId, out assessmentGroupList, out uniqueid))
                            {
                                TempData["MessageInterviewer"] = "Interviewer has been updated Successfully!";
                                //TempData["Message"] = "Interviewer has been updated Successfully!";
                                return RedirectToAction("Index");
                                //ViewBag.Message = "Interviewer has been updated Successfully!";
                                //return View(interviewerProfile);
                            }
                            else
                            {
                                if (!string.IsNullOrEmpty(assessmentGroupList))
                                {
                                    TempData["MessageInterviewer"] = $"Failed to update Interviewer Profile. Cannot map interviewer to the following Assessment Groups as 100 interviewers have already been mapped to the Assessment Group(s) : \n '{assessmentGroupList}' ";
                                }
                                else
                                {
                                    TempData["MessageInterviewer"] = "Failed to update the Interviewer";

                                }
                                return RedirectToAction("Index");
                            }
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
                if ((interviewerProfile != null && interviewerProfile.ShowAddToSkillChannelOption && profileName == "InterviewerManagement"))
                {
                    TempData["MessageInterviewerManagement"] = "Failed to update the Interviewer";
                    return RedirectToAction("Index");

                }
                else
                {
                    TempData["MessageInterviewer"] = "Failed to update the Interviewer";
                    return RedirectToAction("Index");

                }
            }
        }


        [HttpGet]
        public async Task<IActionResult> EditApplicant(int applicantId, bool showAddToSkillChannelOption)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "Finance" || profileName == "SeniorInterviewerManagement" || profileName == "SupportAdmin" || profileName == "MappingAdmin" || (/*showAddToSkillChannelOption && */profileName == "InterviewerManagement"))
                    {
                        InterviewerDirectApplicationVM model = await _interviewerService.GetApplicantById(applicantId);
                        model.ShowAddToSkillChannelOption = showAddToSkillChannelOption;

                        // Get all the select Lists
                        await GetApplicantSpecificSelectList(model);
                        if (TempData["MessageApplicantUpdate"] != null)
                        {
                            ViewBag.Message = TempData["MessageApplicantUpdate"].ToString();
                            TempData.Remove("MessageApplicantUpdate");

                        }
                        ViewBag.Profile = profileName;
                        return View("EditApplicant", model);
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
        public async Task<IActionResult> EditApplicant(InterviewerDirectApplicationVM model)//, int irf
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement" || profileName == "SupportAdmin" || profileName == "MappingAdmin")
                    {
                        if (!model.ShowAddToSkillChannelOption)
                        {
                            if (model.ResumeFile != null)
                            {
                                string uploadedFileURL = string.Empty;
                                var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(model.ResumeFile);
                                if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                                                    || fileMimeType == "application/pdf" || fileMimeType == "application/x-java-archive" || fileMimeType == "application/vnd.ms-powerpoint")
                                {
                                    var resumeFileName = model.ResumeFile.FileName.Replace(" ", "_");

                                    await _azureStorageHelper.InsertBlob(resumeFileName, "resumes", model.ResumeFile);
                                    var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                                    var blob = containerRef.GetBlockBlobReference(resumeFileName);
                                    uploadedFileURL = blob.Uri.ToString();
                                    model.ResumeLink = uploadedFileURL;
                                }


                            }
                            if (model.MAGIdArray != null)
                            {
                                var result = await _interviewerService.UpdateApplicantById(model, emailId);//, irf
                                if (result)
                                {
                                    TempData["MessageApplicantUpdate"] = "Updated and Assessment Groups mapped successfully. Kindly select the target Skill Channel from the drop-down to finish onboarding!";
                                    return RedirectToAction("EditApplicant", new { applicantId = model.id, showAddToSkillChannelOption = true });
                                }
                                else
                                {
                                    TempData["MessageApplicants"] = "Failed to update applicants.";
                                    return RedirectToAction("Applicants");
                                }
                            }
                            else
                            {
                                ModelState.AddModelError(string.Empty, "MAG not selected.");
                                return View(model);
                            }



                        }
                        else // update target skill channel for the applicant
                        {
                            var saveResult = await _interviewerService.UpdateApplicantSkillChannelById(model, emailId);
                            if (saveResult)
                            {
                                TempData["MessageApplicants"] = "Successfully edited";
                                return RedirectToAction("Applicants");
                            }
                            else
                            {
                                TempData["MessageApplicants"] = "Failed to update applicants.";
                                return RedirectToAction("Applicants");
                            }

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
                return RedirectToAction("SignOut", "Session");
                //return Json("Something's wrong. Please try again later.");
            }

        }

        [HttpGet]
        public async Task<IActionResult> ApplicationForm(string id, string upt = null)//, int irf
        {
            //check link expiry befire hitting db

            try
            {
                int resultId;
                int? irfId = new int?();
                // Try to convert the string to an integer
                bool success = int.TryParse(id, out resultId);
                string decodedEncryptedId = string.Empty;
                // Check if the conversion was successful
                if (success)
                {
                    irfId = resultId;
                }
                else
                {
                    decodedEncryptedId = System.Net.WebUtility.UrlDecode(id);
                    irfId = Convert.ToInt32((await _encryptionHelper.DecryptStringValue(decodedEncryptedId)));

                }


                if (irfId.HasValue && irfId.Value > 0)//!string.IsNullOrEmpty(irfId)
                {


                    InterviewerDirectApplicationVM model = new InterviewerDirectApplicationVM();
                    model.IRFId = irfId.Value;
                    model.EncryptedAssessmentGroupId = decodedEncryptedId;

                    //if (TempData["MessageApplicantUpdate"] != null)
                    //{
                    //    ViewBag.Message = TempData["MessageApplicantUpdate"].ToString();
                    //    TempData.Remove("MessageApplicantUpdate");

                    //}
                    return View("ApplyRenewed", model);

                }
                else
                {
                    return Json("Bad Request!");
                }

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("Something's wrong. Please try again later.");
            }

        }

        //[HttpPost]
        //public async Task<IActionResult> ApplyRenewed(InterviewerDirectApplicationVM model)//, int irf
        //{
        //    try
        //    {

        //        if (model.ResumeFile != null)
        //        {
        //            string uploadedFileURL = string.Empty;
        //            var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(model.ResumeFile);
        //            if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        //                                || fileMimeType == "application/pdf" || fileMimeType == "application/x-java-archive" || fileMimeType == "application/vnd.ms-powerpoint")
        //            {
        //                var resumeFileName = model.ResumeFile.FileName.Replace(" ", "_");

        //                await _azureStorageHelper.InsertBlob(resumeFileName, "resumes", model.ResumeFile);
        //                var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
        //                var blob = containerRef.GetBlockBlobReference(resumeFileName);
        //                uploadedFileURL = blob.Uri.ToString();
        //                model.ResumeLink = uploadedFileURL;
        //            }


        //        }

        //        int? irfId = model.IRFId;
        //        //var irfId = await _encryptionHelper.DecryptInteger(model.EncryptedIrfId);
        //        //var irfId = model.EncryptedIrfId;
        //        if (irfId.HasValue && irfId.Value > 0)//!string.IsNullOrEmpty(irfId)
        //        {
        //            model.IRFId = Convert.ToInt32(irfId);//irfId.Value;
        //            var result = await _interviewerService.SubmitInterviewerApplication(model);//, irf
        //            if (result)
        //            {
        //                return Json("Your application has been submitted successfully!");
        //            }
        //            else
        //            {
        //                return Json("Submission failed!");
        //            }
        //        }
        //        else
        //        {
        //            return Json("Failed!");
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //        return Json("Something's wrong. Please try again later.");
        //    }

        //}


        [HttpGet]
        private async Task GetAllSelectList(InterviewerProfileVM model)
        {
            //=========Availability============

            var Weekday = from Weekday s in Enum.GetValues(typeof(Weekday))
                          select new { ID = (int)s, Name = s.GetDescription() };
            model.WeekDayList = new SelectList(Weekday, "ID", "Name");

            var Weekend = from Weekend s in Enum.GetValues(typeof(Weekend))
                          select new { ID = (int)s, Name = s.GetDescription() };
            model.WeekEndList = new SelectList(Weekend, "ID", "Name");

            ////=========Project============

            //var NatureOfProject = from NatureOfProject s in Enum.GetValues(typeof(NatureOfProject))
            //                      select new { ID = (int)s, Name = s.GetDescription() };
            //model.NatureOfProjectList = new SelectList(NatureOfProject, "ID", "Name");

            ////=========Education============

            //var graduationType = from GraduationType s in Enum.GetValues(typeof(GraduationType))
            //                     select new { ID = (int)s, Name = s.GetDescription() };
            //model.GraduationTypeList = new SelectList(graduationType, "ID", "Name");

            //var EducationMode = from EducationMode s in Enum.GetValues(typeof(EducationMode))
            //                    select new { ID = (int)s, Name = s.GetDescription() };
            //model.EducationModeList = new SelectList(EducationMode, "ID", "Name");

            //var Degree = from Entities.Enum.Degree s in Enum.GetValues(typeof(Entities.Enum.Degree))
            //             select new { ID = (int)s, Name = s.GetDescription() };
            //model.DegreeList = new SelectList(Degree, "ID", "Name");

            //var collegeType = from CollegeType s in Enum.GetValues(typeof(CollegeType))
            //                  select new { ID = (int)s, Name = s.GetDescription() };
            //model.CollegeTypeList = new SelectList(collegeType, "ID", "Name");

            //ViewBag.Years = new SelectList(Enumerable.Range(DateTime.Today.Year - 50, 51).Select(x => new SelectListItem()
            //{ Text = x.ToString(), Value = x.ToString() }), "Value", "Text");


            //var Skillset = await _skillService.GetSkills();
            //if (Skillset != null)
            //{
            //    Skillset = Skillset.OrderBy(x => !string.IsNullOrEmpty(x.Name) ? x.Name.Trim() : "").ToList();
            //}
            //model.SkillTypeList = new SelectList(Skillset, "id", "Name");

            //var SkillCategory = from SkillCategory s in Enum.GetValues(typeof(SkillCategory))
            //                    select new { ID = (int)s, Name = s.GetDescription() };
            //model.SkillCategoryList = new SelectList(SkillCategory, "ID", "Name");
            //var SkillExpertiseLevel = from SkillExpertiseLevel s in Enum.GetValues(typeof(SkillExpertiseLevel))
            //                          select new { ID = (int)s, Name = s.GetDescription() };
            //model.SkillExpertiseLevelList = new SelectList(SkillExpertiseLevel, "ID", "Name");
            //var SkillIP = from SkillIP s in Enum.GetValues(typeof(SkillIP))

            //              select new { ID = (int)s, Name = s.GetDescription() };
            //model.SkillIPList = new SelectList(SkillIP, "ID", "Name");
            //var SkillYearsOfExperience = from SkillYearsOfExperience s in Enum.GetValues(typeof(SkillYearsOfExperience))
            //                             select new { ID = (int)s, Name = s.GetDescription() };
            //model.SkillYearsOfExperienceList = new SelectList(SkillYearsOfExperience, "ID", "Name");
            //var SkillNature = from SkillNature s in Enum.GetValues(typeof(SkillNature))
            //                  select new { ID = (int)s, Name = s.GetDescription() };
            //model.SkillNatureList = new SelectList(SkillNature, "ID", "Name");

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


            //var dayTimeSlot = from DayTimeSlot s in Enum.GetValues(typeof(DayTimeSlot))
            //                  select new { ID = (int)s, Name = s.GetDescription() };
            //model.DayTimeSlotList = new SelectList(dayTimeSlot, "ID", "Name");
            //var daySlot = from DaySlot s in Enum.GetValues(typeof(DaySlot))
            //              select new { ID = (int)s, Name = s.GetDescription() };
            //model.DaySlotList = new SelectList(daySlot, "ID", "Name");
            var Assessmentgroup = _interviewerService.FetchAssessmentGroups();

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
            var IndiaPrice = from IndiaPrice s in Enum.GetValues(typeof(IndiaPrice))
                             select new { ID = (int)s, Name = s.GetDescription() };
            model.IndiaServiceRateList = new SelectList(IndiaPrice, "ID", "Name");

            var OutSideIndiaPrice = from OutSideIndiaPrice s in Enum.GetValues(typeof(OutSideIndiaPrice))
                                    select new { ID = (int)s, Name = s.GetDescription() };
            model.OutsideIndiaServiceRateList = new SelectList(OutSideIndiaPrice, "ID", "Name");
            var AvailabilityStatus = from InterviewerAvailabilityStatus s in Enum.GetValues(typeof(InterviewerAvailabilityStatus))
                                     select new { ID = (int)s, Name = s.GetDescription() };
            model.AvailabilityStatus = new SelectList(AvailabilityStatus, "ID", "Name");
            model.SkillChannelList = new SelectList(await _interviewerService.GetApplicableSkillChannelsForInterviewerPostAdding(model.UserId), "id", "IRFId");
        }

        [HttpGet]
        private async Task GetApplicantSpecificSelectList(InterviewerDirectApplicationVM model)
        {

            //var Assessmentgroup = _interviewerService.FetchAssessmentGroups();
            var Assessmentgroup = await _irfService.GetAllMAGs();

            var selectList = new List<SelectListItem>();
            var AssessmentselectList = new List<SelectListItem>();
            foreach (var item in Assessmentgroup)
            {
                //bool selected = false;
                //if (model.AssessmentGroupArray != null && model.AssessmentGroupArray.Contains(item.id.ToString()))
                //{
                //    selected = true;
                //}
                //var selectItem = new SelectListItem(item.AssessmentGroup, item.id.ToString(), selected);
                var selectItem = new SelectListItem(item.AssessmentGroup, item.id.ToString(), false);
                AssessmentselectList.Add(selectItem);
            }
            model.MAGList = AssessmentselectList;
            if (model.ShowAddToSkillChannelOption)
            {
                model.SkillChannelList = new SelectList(await _interviewerService.GetApplicableSkillChannelsForApplicantUponEdit(model.id), "id", "IRFId");

            }
        }

        [HttpPost]
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

        [HttpGet]
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

        [HttpGet]
        public JsonResult IsEmailIdExists(string EmailId, int UserId, bool IsEdit)
        {
            if (IsEdit)
            {

                return Json(true, new Newtonsoft.Json.JsonSerializerSettings());
            }
            //check if the Email Id exists
            IEnumerable<string> EmailIdList = _interviewerService.GetEmailIds(UserId);
            if (EmailIdList.Contains(EmailId.ToUpper()))
            {
                return Json(false, new Newtonsoft.Json.JsonSerializerSettings());
            }
            else
                return Json(true, new Newtonsoft.Json.JsonSerializerSettings());
        }

        [HttpPost]
        public JsonResult EmailIdVilidation(string EmailId, int UserId)
        {
            //IEnumerable<string> EmailIdList = _interviewerService.GetEmailIds(UserId);
            IEnumerable<string> EmailIdList = _interviewerService.CheckEmailIdExists(EmailId);
            if (EmailIdList.Contains(EmailId.ToUpper()))
            {
                var role = _interviewerService.GetUserRolebyEmailid(EmailId);
                return Json("EmailId already in use as " + role + ".");
            }
            else
                return Json("Not");
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

        [HttpGet]
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

        [HttpGet]
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
                            TempData["SuccessErrorMessage"] = "No Information available in the System";
                            return RedirectToAction("MyInterview", "Interview");
                        }
                        if (TempData["MyProfileMessage"] != null)
                        {
                            ViewBag.Message = TempData["MyProfileMessage"].ToString();
                            TempData.Remove("MyProfileMessage");

                        }
                        // Get all the select Lists
                        await GetAllSelectList(model);

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
                TempData["SuccessErrorMessage"] = "Error retrieving Profile Data!";
                return RedirectToAction("MyInterview", "Interview");
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
                            var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(interviewerProfile.ResumeFile);
                            if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                                                || fileMimeType == "application/pdf" || fileMimeType == "application/x-java-archive" || fileMimeType == "application/vnd.ms-powerpoint")
                            {
                                await _azureStorageHelper.InsertBlob(interviewerProfile.ResumeFile.FileName, "resumes", interviewerProfile.ResumeFile);
                                var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                                var blob = containerRef.GetBlockBlobReference(interviewerProfile.ResumeFile.FileName);
                                uploadedFileURL = blob.Uri.ToString();
                                interviewerProfile.ResumeLink = uploadedFileURL;
                            }


                        }
                        if (interviewerProfile.ImageUpload != null)
                        {
                            var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(interviewerProfile.ImageUpload);
                            if (fileMimeType == "image/jpeg" || fileMimeType == "image/x-icon"
                                                || fileMimeType == "image/tiff")
                            {
                                await _azureStorageHelper.InsertBlob(interviewerProfile.ImageUpload.FileName, "profileimages", interviewerProfile.ImageUpload);
                                var containerRef = _azureStorageHelper.GetCloudBlobContainer("profileimages");
                                var blob = containerRef.GetBlockBlobReference(interviewerProfile.ImageUpload.FileName);
                                uploadedFileURL = blob.Uri.ToString();
                                interviewerProfile.ImageURL = uploadedFileURL;
                            }

                        }
                        if (_interviewerService.AddInterviewerMyProfile(interviewerProfile).Result)
                        {
                            TempData["MyProfileMessage"] = "Profile has been updated Successfully!";
                            return RedirectToAction("MyProfile");
                        }
                        else
                        {
                            TempData["MyProfileMessage"] = "Failed to update Profile";
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
                TempData["MyProfileMessage"] = "Profile update failed";
                return RedirectToAction("MyProfile");
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
                        if (await _meetingService.CancelInterview(id, interviewDate.ToString("dd MMM yyyy"), intervieweeEmail))
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


                        if (string.IsNullOrEmpty(searchValue) && Page > 1)
                        {
                            searchValue = SearchvalueInterviewer;
                        }
                        SearchvalueInterviewer = searchValue;
                        ViewBag.Searchvalue = searchValue;
                        if (!string.IsNullOrEmpty(searchValue))
                        {
                            searchValue = searchValue.ToUpper().Trim();
                            myInterview = myInterview.Where(x => (!string.IsNullOrEmpty(x.candidateEmail) ? x.candidateEmail.ToUpper().Contains(searchValue) : x.candidateEmail == "") || (!string.IsNullOrEmpty(x.PrimarySkill) ? x.PrimarySkill.ToUpper().Contains(searchValue) : x.PrimarySkill == "")).ToList();

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
        [HttpGet]
        public FileContentResult MyAssessmentsExportToExcel()
        {
            string userEmailId = string.Empty;
            ExcelPackage Ep = new ExcelPackage();
            ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Interviews");
            string fileName = "Risebird" + DateTime.Now.ToString("dd/MM/yyyy") + "_Interviewer_MyAssessments" + ".xlsx";
            try
            {
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                var myInterviews = _interviewerService.GetAssignedInterviewerAssessment(emailid);
                myInterviews = myInterviews.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();
                foreach (var item in myInterviews)
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

                Sheet.Cells["A1:Z1"].Style.Font.Bold = true;
                Sheet.Cells["A1:Z1"].Style.Font.Size = 12;
                Sheet.Cells["A1:Z1"].Style.WrapText = true;
                //Sheet.Column(1).Width = 30;
                //Sheet.Column(2).Width = 30;
                int startColumn = 1; int endColumn = 15;
                for (int a = startColumn; a <= endColumn; a++)
                {
                    Sheet.Column(a).Width = 40;
                }
                Sheet.Cells[1, 1].Value = "Discussion Date";
                Sheet.Cells[1, 2].Value = "Slot";
                Sheet.Cells[1, 3].Value = "Duration";
                Sheet.Cells[1, 4].Value = "Zoom Id";
                Sheet.Cells[1, 5].Value = "Coded Name";
                Sheet.Cells[1, 6].Value = "Resume";
                Sheet.Cells[1, 7].Value = "Primary Skill";
                Sheet.Cells[1, 8].Value = "Profile type";
                Sheet.Cells[1, 9].Value = "JD Link";
                Sheet.Cells[1, 10].Value = "Risebird HR";
                Sheet.Cells[1, 11].Value = "Meeting Status";
                Sheet.Cells[1, 12].Value = "Interview Status";



                //ExcelHyperLink excelHyperLink = new ExcelHyperLink();
                int i = 2;
                //string LastAction = null; ;

                if (myInterviews.Count() > 0)
                {
                    foreach (var item in myInterviews)
                    {
                        Sheet.Cells[i, 1].Value = item.interviewDate.ToString("MM/dd/yyyy HH:mm");
                        Sheet.Cells[i, 2].Value = item.interviewSlot != null ? item.interviewSlot : string.Empty;
                        Sheet.Cells[i, 3].Value = item.interviewDuration != null ? item.interviewDuration : string.Empty;
                        //Sheet.Cells[i, 4].Value = item.NickName;
                        Sheet.Cells[i, 4].Value = item.ZoomId != null ? item.ZoomId : string.Empty;
                        Sheet.Cells[i, 5].Value = item.NickName != null ? item.NickName : string.Empty;

                        if (!string.IsNullOrEmpty(item.resumeLink))
                        {
                            try
                            {
                                Sheet.Cells[i, 6].Hyperlink = new Uri(item.resumeLink.Replace(" ", "%20"), UriKind.Absolute);
                                Sheet.Cells[i, 6].Value = "Resume";
                                Sheet.Cells[i, 6].Style.Font.UnderLine = true;
                                Sheet.Cells[i, 6].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                            }
                            catch (Exception)
                            {
                                Sheet.Cells[i, 6].Value = "No Resume";
                            }
                        }
                        else
                        {
                            Sheet.Cells[i, 6].Value = "No Resume";
                        }

                        //Sheet.Cells[i, 6].Value = item.interviewer != null ? item.interviewer : string.Empty;
                        Sheet.Cells[i, 7].Value = item.PrimarySkill != null ? item.PrimarySkill : string.Empty;
                        Sheet.Cells[i, 8].Value = item.HeuristicProfileTypeDesc != null ? item.HeuristicProfileTypeDesc : string.Empty;

                        if (!string.IsNullOrEmpty(item.JDLink))
                        {
                            try
                            {
                                Sheet.Cells[i, 9].Hyperlink = new Uri(item.JDLink.Replace(" ", "%20"), UriKind.Absolute);
                                Sheet.Cells[i, 9].Value = "JD Link";
                                Sheet.Cells[i, 9].Style.Font.UnderLine = true;
                                Sheet.Cells[i, 9].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                            }
                            catch (Exception)
                            {
                                Sheet.Cells[i, 9].Value = string.Empty;
                            }
                        }
                        else
                        {
                            Sheet.Cells[i, 9].Value = string.Empty;
                        }

                        Sheet.Cells[i, 10].Value = item.HRCoordinatorEmailId != null ? item.HRCoordinatorEmailId : string.Empty;
                        Sheet.Cells[i, 11].Value = item.MeetingStatusDesc != null ? item.MeetingStatusDesc : string.Empty;
                        Sheet.Cells[i, 12].Value = item.InterviewStatusDesc != null ? item.InterviewStatusDesc : string.Empty;

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
        [HttpGet]
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

        [HttpGet]
        public async Task<IActionResult> AddReport(int interviewId, int iscId, bool isClient = false, bool isClientSelected = false, bool isQC = false, string backUrl = null)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");

                    if (profileName == "Admin" || profileName == "HR" || profileName == "Interviewer" || profileName == "InterviewerManagement" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "SeniorInterviewerManagement")
                    {
                        var model = _interviewerService.GetInterviewerinteviewReport(interviewId, iscId).Result;
                        model.profileName = profileName;
                        await GetStatusList(model, iscId);
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
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);

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
                    if (profileName == "Admin" || profileName == "Interviewer" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "SeniorInterviewerManagement")//|| profileName == "InterviewerManagement"
                    {
                        if (model.AudioFile != null)
                        {
                            var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(model.AudioFile);
                            if (fileMimeType == "audio/basic" || fileMimeType == "audio/mpeg" || fileMimeType == "audio/x-wav" || fileMimeType == "video/x-msvideo")
                            {
                                await _azureStorageHelper.InsertBlob(model.AudioFile.FileName, "audios", model.AudioFile);
                                var containerRef = _azureStorageHelper.GetCloudBlobContainer("audios");
                                var blob = containerRef.GetBlockBlobReference(model.AudioFile.FileName);
                                string uploadedaudioFileURL = blob.Uri.ToString();
                                if (uploadedaudioFileURL == null)
                                {
                                    ModelState.AddModelError(string.Empty, "Audio Upload is failed.");
                                    await GetStatusList(model);
                                    return View("AddReport", model);
                                }
                                else
                                {
                                    model.ThumbnailImage = uploadedaudioFileURL;
                                }
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
                                ModelState.AddModelError(string.Empty, "Video Upload has failed.");
                                await GetStatusList(model);
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
                            TempData["SuccessErrorMessageLeadInterviewer"] = "Report has been saved Successfully!";
                            if (profileName == "Admin")
                            {
                                return RedirectToAction("Index", "UnapprovedInterviewerMeetingsController");
                            }
                            else
                            {
                                return RedirectToAction("MyInterviewerAssessment");
                            }
                        }
                        else
                        {
                            TempData["SuccessErrorMessageLeadInterviewer"] = "Report was not saved.";
                            if (profileName == "Admin")
                            {
                                return RedirectToAction("Index", "UnapprovedInterviewerMeetingsController");
                            }
                            else
                            {
                                return RedirectToAction("MyInterviewerAssessment");
                            }
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

        [HttpGet]
        private async Task GetStatusList(InterviewReport model, int irfId = 0)
        {
            var interviewStatuses = from InterviewStatus s in Enum.GetValues(typeof(InterviewStatus))
                                    select new { ID = (int)s, Name = s.GetDescription() };
            model.InterviewStatusList = new SelectList(interviewStatuses, "ID", "Name");

            var meetingStatuses = from MeetingStatus s in Enum.GetValues(typeof(MeetingStatus))
                                  select new { ID = (int)s, Name = s.GetDescription() };
            meetingStatuses = meetingStatuses.Where(x => x.Name != "Scheduled").ToList();
            model.MeetingStatusList = new SelectList(meetingStatuses, "ID", "Name");

            model.Level = new SelectList(await _iRFRepository.GetLevels(), "id", "Name");
            model.LevelTech = new SelectList(await _iRFRepository.GetLevelTech(), "id", "Name");
            var questionsLevel = from QuestionsLevel s in Enum.GetValues(typeof(QuestionsLevel))
                                 select new { ID = (int)s, Name = s.GetDescription() };
            model.QuestionsLevelList = new SelectList(questionsLevel, "ID", "Name");
            var isApproved = from InterviewerApproved s in Enum.GetValues(typeof(InterviewerApproved))
                             select new { ID = (int)s, Name = s.GetDescription() };
            model.InterviewerApproved = new SelectList(isApproved, "ID", "Name");
            var CompanySizeTypes = from CompanySize s in Enum.GetValues(typeof(CompanySize))
                                   select new { ID = (int)s, Name = s.GetDescription() };
            var selectListforsize = new List<SelectListItem>();
            foreach (var item in CompanySizeTypes)
            {
                bool selected = false;
                if (model.CompanySizeArray != null && model.CompanySizeArray.Contains(Convert.ToString(item.ID)))
                {
                    selected = true;
                }
                var selectItem = new SelectListItem(item.Name, Convert.ToString(item.ID), selected);
                selectListforsize.Add(selectItem);
            }
            model.CompanySizeList = selectListforsize;
            //model.CompanySizeList = new SelectList(CompanySizeTypes, "ID", "Name");
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
            var dominlist = _iRFRepository.GetDomainList();
            if (dominlist != null)
            {
                dominlist = dominlist.OrderBy(x => x.Name);
            }
            model.DomainList = new SelectList(dominlist, "Name", "Name");
            var Companystage = _iRFRepository.GetCompanyStage();
            var selectListforstage = new List<SelectListItem>();
            foreach (var item in Companystage)
            {
                bool selected = false;
                if (model.CompanyStageArray != null && model.CompanyStageArray.Contains(Convert.ToString(item.Id)))
                {
                    selected = true;
                }
                var selectItem = new SelectListItem(item.Name, Convert.ToString(item.Id), selected);
                selectListforstage.Add(selectItem);
            }
            model.CompanyStageList = selectListforstage;
            //model.CompanyStageList = new SelectList(_iRFRepository.GetCompanyStage(), "Id", "Name");
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
            var yearsofexprience = _iRFRepository.GetInterviewerExprienceYear();
            selectList = new List<SelectListItem>();
            foreach (var item in yearsofexprience)
            {
                bool selected = false;
                if (model.YearsofexperienceArray != null && model.YearsofexperienceArray.Contains(Convert.ToString(item.Id)))
                {
                    selected = true;
                }
                var selectItem = new SelectListItem(item.Name, Convert.ToString(item.Id), selected);
                selectList.Add(selectItem);
            }
            model.YearsofexperienceList = selectList;
            //model.YearsofexperienceList = new SelectList(_iRFRepository.GetInterviewerExprienceYear(), "Id", "Name");
            var senioritylevel = _iRFRepository.GetSenioritylevel();
            selectList = new List<SelectListItem>();
            foreach (var item in senioritylevel)
            {
                bool selected = false;
                if (model.SenioritylevelArray != null && model.SenioritylevelArray.Contains(Convert.ToString(item.Id)))
                {
                    selected = true;
                }
                var selectItem = new SelectListItem(item.Name, Convert.ToString(item.Id), selected);
                selectList.Add(selectItem);
            }
            model.SenioritylevelList = selectList;
            //model.SenioritylevelList = new SelectList(_iRFRepository.GetSenioritylevel(), "Id", "Name");
            model.SkillNatureList = new SelectList(_iRFRepository.GetSkillNature(), "Id", "Name");
            var Heuristics = from Heuristicsscorelevel s in Enum.GetValues(typeof(Heuristicsscorelevel))
                             select new { ID = (int)s, Name = s.GetDescription() };
            var Compentencs = from CompetencyLevel s in Enum.GetValues(typeof(CompetencyLevel))
                              select new { ID = (int)s, Name = s.GetDescription() };
            model.ProfileTypeList = new SelectList(Heuristics, "ID", "Name");
            model.CompentencyLevelList = new SelectList(Compentencs, "ID", "Name");
            var Assessmentgroup = _interviewerService.FetchAssessmentGroups();
            var AssessmentselectList = new List<SelectListItem>();
            foreach (var item in Assessmentgroup)
            {
                bool selected = false;
                if (model.AssessmentGroupIdArray != null && model.AssessmentGroupIdArray.Contains(item.id.ToString()))
                {
                    selected = true;
                }
                var selectListassissment = new SelectListItem(item.AssessmentGroup, item.id.ToString(), selected);
                AssessmentselectList.Add(selectListassissment);
            }
            model.AssessmentGroupList = AssessmentselectList;
        }

        /// <summary>
        /// Fetch Interviewer Assessments and create Assessments
        /// </summary>
        /// <param name="Page"></param>
        /// <returns></returns>
        [HttpGet]
        public IActionResult InterviewerAssessment(int? Page)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "Interviewer" || profileName == "InterviewerManagement")
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

        [HttpGet]
        public IActionResult AddAssessments()
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "Interviewer" || profileName == "InterviewerManagement" || profileName == "MappingAdmin")
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
        public async Task<IActionResult> AddAssessments(AssignmentVM model)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "Interviewer" || profileName == "InterviewerManagement" || profileName == "MappingAdmin")
                {
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    model.Createdby = emailid;
                    var result = _interviewerService.AddAssessment(model);
                    //send mail to HR
                    if (result == "Success")
                    {
                        var hrlist = new List<string>();
                        var hrlistdata = await _supportService.GetAllSupportHR(emailid);
                        hrlist = hrlistdata.Select(x => x.EmailId).ToList();
                        var Emailids = string.Join(";", hrlist);
                        Mail _mail = new Mail
                        {
                            TO = Emailids,
                            Subject = "Risebird: Interviewer send assessment for approval"
                        };
                        var resultset = await _notificationHelper.SendAssessmentNotification(_mail, emailid);
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

        [HttpGet]
        public IActionResult AssessmentForApproval(int id, string EmailAddress)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "Interviewer" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement" || profileName == "MappingAdmin")
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
        [HttpGet]
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
        [HttpGet]
        public async Task<JsonResult> ShareAssessmentcomment(int assessmentid, string EmailAddress, string assessment, string comments)
        {
            Mail _mail = new Mail
            {
                TO = EmailAddress,
                Subject = "Risebird: Review Comments on your assessment"
            };
            var resultset = await _notificationHelper.SendAssessmentNotificationComments(_mail, assessment, comments);
            return Json("Success");
        }
        /// <summary>
        /// Edit Assessment Details By interviewer
        /// </summary>
        /// <returns></returns>
        [HttpGet]
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
                var model = _interviewerService.GetInterviewers();

                // ── STEP 1: search filter on raw fields (no processing cost yet) ──────────
                // Applying this first means every expensive step below only runs on the
                // records that actually match, not on the entire table.
                Searchvalue = searchValue;
                ViewBag.Searchvalue = searchValue;
                if (!string.IsNullOrEmpty(searchValue))
                {
                    var sv = searchValue.ToUpper().Trim();
                    model = model.Where(x =>
                        (!string.IsNullOrEmpty(x.FirstName) && x.FirstName.ToUpper().Contains(sv)) ||
                        (!string.IsNullOrEmpty(x.LastName) && x.LastName.ToUpper().Contains(sv)) ||
                        (!string.IsNullOrEmpty(x.EmailId) && x.EmailId.ToUpper().Contains(sv)) ||
                        (!string.IsNullOrEmpty(x.NickName) && x.NickName.ToUpper().Contains(sv)) ||
                        (!string.IsNullOrEmpty(x.MobileNumber) && x.MobileNumber.Contains(sv)) ||
                        (x.PrimarySkill != null && x.PrimarySkill.ToUpper().Contains(sv))
                    ).ToList();
                }

                // ── STEP 2: convert enum codes → display strings (only matching rows) ─────
                model = model.Select(x =>
                {
                    x.ProfileType = ((Heuristicsscorelevel)Convert.ToInt32(x.ProfileType)).GetDescription();
                    x.CompentencyLevel = ((CompetencyLevel)Convert.ToInt32(x.CompentencyLevel)).GetDescription();
                    return x;
                }).ToList();

                // ── STEP 3: resolve AssessmentGroupId IDs → names (only matching rows) ────
                var assessmentGroupDict = _interviewerService.FetchAssessmentGroups()
                    .ToDictionary(x => x.id, x => x.AssessmentGroup);
                foreach (var item in model)
                {
                    if (!string.IsNullOrEmpty(item.AssessmentGroupId))
                    {
                        item.AssessmentGroupId = string.Join(" , ",
                            item.AssessmentGroupId.Split(',')
                                .Where(g => !string.IsNullOrWhiteSpace(g))
                                .Select(g => {
                                    int id = int.TryParse(g.Trim(), out int v) ? v : 0;
                                    return assessmentGroupDict.TryGetValue(id, out var name) ? name : string.Empty;
                                })
                                .Where(n => !string.IsNullOrEmpty(n)));
                    }
                }

                // ── STEP 4: apply session-based column filters ────────────────────────────
                if (HttpContext.Session.GetString("InterviewerIndexfilters") != null)
                {
                    var filter = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("InterviewerIndexfilters"));
                    filter.Senioritylevel = filter.Senioritylevel == null ? filter.Senioritylevel = model.Select(x => x.Senioritylevel).ToArray() : filter.Senioritylevel = filter.Senioritylevel;
                    filter.ProfileType = filter.ProfileType == null ? filter.ProfileType = model.Select(x => x.ProfileType).ToArray() : filter.ProfileType = filter.ProfileType;
                    filter.CompentencyLevel = filter.CompentencyLevel == null ? filter.CompentencyLevel = model.Select(x => x.CompentencyLevel).ToArray() : filter.CompentencyLevel = filter.CompentencyLevel;
                    filter.IndianRate = filter.IndianRate == null ? filter.IndianRate = model.Select(x => x.PerHourRateIndia).ToArray() : filter.IndianRate = filter.IndianRate;
                    filter.OutSideIndiaRate = filter.OutSideIndiaRate == null ? filter.OutSideIndiaRate = model.Select(x => x.PerHourRateOutside).ToArray() : filter.OutSideIndiaRate = filter.OutSideIndiaRate;
                    filter.CompanyType = filter.CompanyType == null ? filter.CompanyType = model.Select(x => x.CompanyType).ToArray() : filter.CompanyType = filter.CompanyType;
                    filter.Domain = filter.Domain == null ? filter.Domain = model.Select(x => x.Country).ToArray() : filter.Domain = filter.Domain;
                    filter.AssessmentGroup = filter.AssessmentGroup == null ? filter.AssessmentGroup = model.Select(x => x.AssessmentGroupId).ToArray() : filter.AssessmentGroup = filter.AssessmentGroup;
                    filter.IsLeadInterviewer = filter.IsLeadInterviewer == null ? filter.IsLeadInterviewer = model.Select(x => x.IsLeadInterviewer.ToString()).ToArray() : filter.IsLeadInterviewer = filter.IsLeadInterviewer;
                    filter.IsActive = filter.IsActive == null ? filter.IsActive = model.Select(x => x.IsActive).ToArray() : filter.IsActive = filter.IsActive;
                    filter.IsInterested = filter.IsInterested == null ? filter.IsInterested = model.Select(x => x.IsInterested).ToArray() : filter.IsInterested = filter.IsInterested;

                    model = model.Where(c =>
                       (filter.Senioritylevel.Count() > 0 ? filter.Senioritylevel.Contains(c.Senioritylevel) : c.Senioritylevel == null)
                      && (filter.ProfileType.Count() > 0 ? filter.ProfileType.Contains(c.ProfileType) : c.ProfileType == null)
                      && (filter.CompentencyLevel.Count() > 0 ? filter.CompentencyLevel.Contains(c.CompentencyLevel) : c.CompentencyLevel == null)
                       && (filter.IndianRate.Count() > 0 ? filter.IndianRate.Contains(c.PerHourRateIndia) : c.PerHourRateIndia == null)
                      && (filter.OutSideIndiaRate.Count() > 0 ? filter.OutSideIndiaRate.Contains(c.PerHourRateOutside) : c.PerHourRateOutside == null)
                      && (filter.CompanyType.Count() > 0 ? filter.CompanyType.Contains(c.CompanyType) : c.CompanyType == null)
                      && (filter.Domain.Count() > 0 ? filter.Domain.Contains(c.Country) : c.Country == null)
                      && (filter.AssessmentGroup.Count() > 0 ? filter.AssessmentGroup.Contains(c.AssessmentGroupId) : c.AssessmentGroupId == null)
                      && (filter.IsLeadInterviewer.Count() > 0 ? filter.IsLeadInterviewer.Contains(c.IsLeadInterviewer.ToString()) : c.IsLeadInterviewer.ToString() == null)
                      && (filter.IsActive.Count() > 0 ? filter.IsActive.Contains(c.IsActive) : c.IsActive == true)
                      && (filter.IsInterested.Count() > 0 ? filter.IsInterested.Contains(c.IsInterested) : c.IsInterested == true)).ToList();
                }

                // ── STEP 5: resolve Seniority IDs → names (only rows that passed all filters)
                var seniorityDict = _iRFRepository.GetSenioritylevel()
                    .ToDictionary(x => x.Id, x => x.Name);
                foreach (var item in model)
                {
                    if (!string.IsNullOrEmpty(item.Senioritylevel))
                    {
                        item.Senioritylevel = string.Join(" , ",
                            item.Senioritylevel.Split(',')
                                .Where(s => !string.IsNullOrWhiteSpace(s))
                                .Select(s => {
                                    int id = int.TryParse(s.Trim(), out int v) ? v : 0;
                                    return seniorityDict.TryGetValue(id, out var name) ? name : string.Empty;
                                })
                                .Where(n => !string.IsNullOrEmpty(n)));
                    }
                }

                // ── STEP 6: paginate and return ───────────────────────────────────────────
                int Pagestart = 0;
                int Pageend = 0;
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
                if (!string.IsNullOrEmpty(searchValue))
                {
                    searchValue = searchValue.ToUpper().Trim();
                    myInterview = myInterview.Where(x => (!string.IsNullOrEmpty(x.candidateEmail) ? x.candidateEmail.ToUpper().Contains(searchValue) : x.candidateEmail == "") || (!string.IsNullOrEmpty(x.PrimarySkill) ? x.PrimarySkill.ToUpper().Contains(searchValue) : x.PrimarySkill == "")).ToList();

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

        [HttpGet]
        public async Task<IActionResult> InterviewerRecommendation(int id)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement" || profileName == "SupportAdmin" || profileName == "MappingAdmin")
                {
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    //Fetch Assessment
                    var model = await _interviewerService.GetInterviewerRecommendationDetails(id);
                    if (model == null)
                    {
                        model = new InterviewReport();
                        model.Userid = id;
                    }
                    if (!string.IsNullOrEmpty(model.SkillCategory))
                    {
                        model.SkillCategoryArray = model.SkillCategory.Split(',');
                    }
                    if (!string.IsNullOrEmpty(model.CompanySize))
                    {
                        model.CompanySizeArray = model.CompanySize.Split(',');
                    }
                    if (!string.IsNullOrEmpty(model.CompanyStage))
                    {
                        model.CompanyStageArray = model.CompanyStage.Split(',');
                    }
                    if (!string.IsNullOrEmpty(model.Yearsofexperience))
                    {
                        model.YearsofexperienceArray = model.Yearsofexperience.Split(',');
                    }
                    if (!string.IsNullOrEmpty(model.Senioritylevel))
                    {
                        model.SenioritylevelArray = model.Senioritylevel.Split(',');
                    }
                    if (!string.IsNullOrEmpty(model.AssessmentGroupId))
                    {
                        model.AssessmentGroupIdArray = model.AssessmentGroupId.Split(',');
                    }
                    await GetStatusList(model);
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
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorInterviewerManagement" || profileName == "SupportAdmin" || profileName == "MappingAdmin")//|| profileName == "InterviewerManagement"
                    {
                        if (model.CodedResumeFile != null)
                        {
                            var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(model.CodedResumeFile);
                            if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                                                || fileMimeType == "application/pdf" || fileMimeType == "application/x-java-archive" || fileMimeType == "application/vnd.ms-powerpoint")
                            {
                                var codedResumeFileName = model.CodedResumeFile.FileName.Replace(" ", "_");
                                await _azureStorageHelper.InsertBlob(codedResumeFileName, "resumes", model.CodedResumeFile);
                                var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                                var blob = containerRef.GetBlockBlobReference(codedResumeFileName);
                                var uploadedFileURL = blob.Uri.ToString();
                                model.CodedResumeLink = uploadedFileURL;
                            }

                        }
                        if (_interviewerService.InterviewerRecommendation(model).Result)
                        {
                            TempData["MessageInterviewer"] = "Recommendation has been saved Successfully!";
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

        [HttpPost]
        public JsonResult BankDetailsJson(int interviewerid)
        {
            var results = _interviewerService.fetchBankDetails(interviewerid);
            return Json(results.Result);

        }

        [HttpPost]
        public async Task<JsonResult> AvailDetailsJson(int interviewerid)
        {
            var results = await _interviewerService.fetchAvailDetails(interviewerid);
            if (results != null)
            {
                foreach (var item in results)
                {
                    item.Weekday = ((Weekday)Convert.ToInt32(item.Weekday)).GetDescription();
                    item.Weekend = ((Weekend)Convert.ToInt32(item.Weekend)).GetDescription();
                }
            }
            return Json(results);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> FeedbackDetails(string type)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                int interviewerid = 0;
                var modelData = new List<InterviewerRating>();
                ViewBag.type = "Client";
                if (profileName == "Interviewer")
                {
                    ViewBag.OverallRating = 0;
                    if (type == "Candidate")
                    {
                        ViewBag.type = "Candidate";
                        modelData = await _interviewerService.fetchClientFeedbackbyInterviewer(emailid, "Candidate");
                        //foreach (var item in modelData)
                        //{
                        //    item.StarRating = item.Question2;
                        //}
                        if (modelData.Count() != 0)
                        {
                            ViewBag.OverallRating = Math.Round(modelData.Select(x => x.Rating).Average(), 2);
                        }
                    }
                    else
                    {
                        var results = await _interviewerService.fetchClientFeedbackbyInterviewer(emailid, "Client");
                        if (results != null)
                            modelData = results;
                        if (modelData.Count() != 0)
                        {
                            ViewBag.OverallRating = Math.Round(modelData.Select(x => x.Rating).Average(), 2);
                        }
                    }
                    return View(modelData);
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
        public async Task<JsonResult> SendPublicURLInterviewer(string Emailid)
        {
            try
            {
                Guid uniqueid = Guid.NewGuid();
                var _uniqueid = await _clientService.PublicInterviewerUrlSave(Emailid, uniqueid.ToString());
                string sendreporturl = "https://" + Request.Host.ToString() + "/Interview/PublicOpenInterviewInvite?id=" + uniqueid;
                if (!string.IsNullOrEmpty(_uniqueid))
                {
                    sendreporturl = "https://" + Request.Host.ToString() + "/Interview/PublicOpenInterviewInvite?id=" + _uniqueid;
                }
                Mail _mail1 = new Mail
                {
                    TO = Emailid,
                    CC = "talent@risebird.io",
                    Subject = "Open Interviews URL"
                };
                var emailsend1 = await _notificationHelper.SendPublicUrlToInterviewer(_mail1, sendreporturl);
                if (emailsend1.Contains("successfully"))
                    return Json("Success");
                else
                    return Json("Failed");
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("Failed");
            }
        }

        [HttpPost]
        public async Task<JsonResult> CopyPublicURLInterviewer(string Emailid)
        {
            try
            {
                Guid uniqueid = Guid.NewGuid();
                var _uniqueid = await _clientService.PublicInterviewerUrlSave(Emailid, uniqueid.ToString());
                string sendreporturl = "https://" + Request.Host.ToString() + "/Interview/PublicOpenInterviewInvite?id=" + uniqueid;
                if (!string.IsNullOrEmpty(_uniqueid))
                {
                    sendreporturl = "https://" + Request.Host.ToString() + "/Interview/PublicOpenInterviewInvite?id=" + _uniqueid;
                }
                return Json(sendreporturl);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("Failed");
            }
        }
        public IActionResult LeadInterviewer(string uniqueid)
        {
            var model = _interviewerService.GetInterviewers();
            model = model.Where(x => x.IsLeadInterviewer == true).ToList();
            model = model.Select(x => { x.ProfileType = ((Heuristicsscorelevel)Convert.ToInt32(x.ProfileType)).GetDescription(); x.CompentencyLevel = ((CompetencyLevel)Convert.ToInt32(x.CompentencyLevel)).GetDescription(); return x; }).ToList();
            var AssessmentsGroups = _interviewerService.FetchAssessmentGroups();
            foreach (var item in model)
            {
                if (!string.IsNullOrEmpty(item.AssessmentGroupId))
                {
                    var Assessmentgroups = string.Empty;
                    foreach (var groups in item.AssessmentGroupId.Split(","))
                    {
                        if (string.IsNullOrWhiteSpace(groups))
                            continue;

                        int assesmsnetid = Convert.ToInt32(groups);
                        if (string.IsNullOrEmpty(Assessmentgroups))
                        {
                            Assessmentgroups = AssessmentsGroups.Where(x => x.id == assesmsnetid).Select(x => x.AssessmentGroup).FirstOrDefault();
                        }
                        else
                        {
                            Assessmentgroups = Assessmentgroups + " , " + AssessmentsGroups.Where(x => x.id == assesmsnetid).Select(x => x.AssessmentGroup).FirstOrDefault();
                        }
                    }
                    item.AssessmentGroupId = Assessmentgroups;
                }
            }
            if (uniqueid == "74206543283756")
            {
                ViewBag.retureParameter = "IRF";
            }
            else
            {
                ViewBag.retureParameter = "Interview";
            }
            return View(model);
        }

        public async Task<IActionResult> TdsCertificateInterviewers(string emailid, string year)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "Interviewer" || profileName == "HR" || profileName == "Finance" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement" || profileName == "SupportAdmin")
                {
                    var _emailid = HttpContext.Session.GetString("email")?.Trim();
                    _emailid = _emailid.Replace("\"", "");
                    if (profileName == "Interviewer")
                    {
                        emailid = _emailid;
                    }
                    ViewBag.profile = profileName;
                    InterviewerTdsVM model = new InterviewerTdsVM();
                    List<string> _listyear = new List<string>();
                    var currentmonth = DateTime.Now.Month;
                    if (currentmonth > 3)
                    {
                        var yearstring = DateTime.Now.Year.ToString() + "-" + DateTime.Now.AddYears(1).Year.ToString();
                        _listyear.Add(yearstring);
                        if (string.IsNullOrEmpty(year))
                        {
                            year = yearstring;
                        }
                    }
                    var currentyear = DateTime.Now.Year;
                    var counter = currentyear - 2018;
                    for (int i = 0; i < counter; i++)
                    {
                        _listyear.Add(DateTime.Now.AddYears(-(i + 1)).Year.ToString() + "-" + DateTime.Now.AddYears(-i).Year.ToString());
                    }
                    var selectListforsize = new List<SelectListItem>();
                    foreach (var item in _listyear)
                    {
                        bool selected = false;
                        if (item == year)
                        {
                            selected = true;
                        }
                        var selectItem = new SelectListItem(item, Convert.ToString(item), selected);
                        selectListforsize.Add(selectItem);
                    }
                    model.TdsYears = selectListforsize;
                    var _listcollection = await _interviewerService.FetchTdsCertificatesbyEmailid(emailid);
                    if (_listcollection != null)
                    {
                        model.TdsCertificateCollection = _listcollection.Where(x => x.year == year).ToList();
                    }
                    model.InterviewerEmailid = emailid;
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
        public IActionResult AddTdsCertificate(int certificateid, string interviewerid, string year)
        {
            TdsCertificateReport model = new TdsCertificateReport();
            if (certificateid != 0)
            {
                model = _interviewerService.FetchTdsCertificatebyid(certificateid);
            }
            var selectListforsize = new List<SelectListItem>();
            selectListforsize.Add(new SelectListItem("Quarter 1", Convert.ToString("Quarter 1"), false));
            selectListforsize.Add(new SelectListItem("Quarter 2", Convert.ToString("Quarter 2"), false));
            selectListforsize.Add(new SelectListItem("Quarter 3", Convert.ToString("Quarter 3"), false));
            selectListforsize.Add(new SelectListItem("Quarter 4", Convert.ToString("Quarter 4"), false));
            selectListforsize.Add(new SelectListItem("Additional", Convert.ToString("Additional"), false));
            model.MonthsList = selectListforsize;
            model.emailid = interviewerid;
            model.year = year;
            model.id = certificateid;
            return PartialView(model);
        }

        [HttpPost]
        [RequestFormLimits(MultipartBodyLengthLimit = 1048576000)]
        [RequestSizeLimit(1048576000)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TdsCertificateInterviewers(TdsCertificateReport model)
        {
            if (model.cretificate != null)
            {
                var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(model.cretificate);
                if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                                    || fileMimeType == "application/pdf" || fileMimeType == "application/x-java-archive" || fileMimeType == "application/vnd.ms-powerpoint")
                {
                    var codedResumeFileName = model.cretificate.FileName.Replace(" ", "_");
                    await _azureStorageHelper.InsertBlob(codedResumeFileName, "resumes", model.cretificate);
                    var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                    var blob = containerRef.GetBlockBlobReference(codedResumeFileName);
                    var uploadedFileURL = blob.Uri.ToString();
                    model.certificatelink = uploadedFileURL;
                }
                else
                {
                    return RedirectToAction("TdsCertificateInterviewers", new { emailid = model.emailid, year = model.year });
                }
            }
            else
            {
                return RedirectToAction("TdsCertificateInterviewers", new { emailid = model.emailid, year = model.year });
            }
            if (_interviewerService.AddTdsCertificates(model))
            {
                return RedirectToAction("TdsCertificateInterviewers", new { emailid = model.emailid, year = model.year });
            }
            else
            {
                return RedirectToAction("TdsCertificateInterviewers", new { emailid = model.emailid, year = model.year });
            }
        }
        #region IRFInterviewerMapping
        public async Task<IActionResult> IRFInterviewerMapping()
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "Interviewer")
                {
                    var _emailid = HttpContext.Session.GetString("email")?.Trim();
                    _emailid = _emailid.Replace("\"", "");
                    var model = await _interviewerService.IRFInterviewerMappingByInterviewerid(_emailid);
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
        [HttpGet]
        public JsonResult SetInterviewerAvailability(int transid, int isavailable)
        {
            var result = _meetingService.SetInterviewerAvailability(transid, isavailable);
            return Json(result);
        }
        #endregion
        [HttpPost]
        public async Task<JsonResult> CheckAllIntervieweravailbility()
        {
            try
            {
                var model = await _interviewerService.GetAllInterviewerMembers();
                foreach (var item in model)
                {
                    if (!string.IsNullOrEmpty(item.EmailId))
                    {
                        string yesbutton = "https://portal.risebird.io/Interviewer/UpdateIntervieweravailbilityStatus?status=1&uniqueid=" + item.uniqueid;
                        string nobutton = "https://portal.risebird.io/Interviewer/UpdateIntervieweravailbilityStatus?status=2&uniqueid=" + item.uniqueid;
                        string cleritybutton = "https://portal.risebird.io/Interviewer/UpdateIntervieweravailbilityStatus?status=3&uniqueid=" + item.uniqueid;
                        string nodropbutton = "https://portal.risebird.io/Interviewer/UpdateIntervieweravailbilityStatus?status=4&uniqueid=" + item.uniqueid;
                        Mail _mail = new Mail
                        {
                            TO = item.EmailId,
                            Subject = "Risebird: Availability check for Upcoming Interviews"
                        };
                        var emailresult = await _notificationHelper.SendAllIntervieweravailbility(_mail, item.InterviewerName, yesbutton, nobutton, cleritybutton, nodropbutton);
                    }
                }
                return Json("Success");
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("Fail");
            }
        }
        [HttpGet]
        public JsonResult UpdateIntervieweravailbilityStatus(int status, string uniqueid)
        {
            //InterviewerAvailabilityStatus
            if (_interviewerService.UpdateAllIntervieweravailbilityStatus(status, uniqueid))
            {
                return Json("Successfully status updated");
            }
            else
            {
                return Json("Fail to Update Status, Please try again");
            }
        }
        public async Task<IActionResult> GetClientFeedForInterviewer(int? interviewId, string postWhichDate = null, string meetingLink = null, int? diffOffset = null)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Interviewer")
                    {
                        var emailId = HttpContext.Session.GetString("email")?.Trim();
                        emailId = emailId.Replace("\"", "");
                        DateTime? postWhichDateValue = null;
                        if (!string.IsNullOrEmpty(postWhichDate))
                        {
                            //if(diffOffset.HasValue && diffOffset.Value != 0)//it is not Indian Time
                            //{
                            //    postWhichDateValue = DateTime.Now;//this, therefore is the indian time we need to pass
                            //}
                            //else
                            //{
                            //}
                            postWhichDateValue = Convert.ToDateTime(postWhichDate);

                        }
                        ClientFeedDataWrapper model = new ClientFeedDataWrapper();
                        //IEnumerable<ClientFeedDataVM> model = await _interviewerService.GetClientFeedForInterviewer(emailId, postWhichDateValue, interviewId);
                        model.ClientFeeds = await _interviewerService.GetClientFeedForInterviewer(emailId, postWhichDateValue, interviewId);
                        if ((model.ClientFeeds != null && model.ClientFeeds.Count() > 0) || !string.IsNullOrEmpty(meetingLink) || interviewId.HasValue)
                        {
                            model.InterviewId = interviewId;
                            model.MeetingLink = meetingLink;
                            return PartialView("GetClientFeedForInterviewer", model);
                        }
                        else
                        {
                            return Json(null);
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
                return Json("Error retrieving data! Please try again.");
            }
        }
        #region CodeConsole
        [HttpGet]
        public async Task<IActionResult> SendInviteCode()
        {
            try
            {
                CodeInviteVM model = new CodeInviteVM();
                var programmingLanguage = from ProgrammingLanguageenum s in Enum.GetValues(typeof(ProgrammingLanguageenum))
                                          select new { ID = s.GetDescription(), Name = s.GetDescription() };
                model.ProgrammingLanguageList = new SelectList(programmingLanguage, "ID", "Name");
                return PartialView("InviteCode", model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["MessageInterviewerCode"] = "Invitecode has been failed";
                return RedirectToAction("InviteCodeList");
            }
        }
        [HttpGet]
        public async Task<IActionResult> InviteCodeList()
        {
            try
            {
                var model = await _interviewerService.GetAllIntviteDetails();
                if (TempData["MessageInterviewerCode"] != null)
                {
                    ViewBag.Message = TempData["MessageInterviewerCode"].ToString();
                    TempData.Remove("MessageInterviewerCode");
                }
                return View(model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendInviteCode(CodeInviteVM codeInviteVM)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                    codeInviteVM.userName = emailId;
                    var programmingLanguage = from ProgrammingLanguageenum s in Enum.GetValues(typeof(ProgrammingLanguageenum))
                                              select new { ID = s.GetDescription(), Name = s.GetDescription() };
                    codeInviteVM.ProgrammingLanguageList = new SelectList(programmingLanguage, "ID", "Name");
                    await GenerateCodeInviteURL(codeInviteVM);
                    if (await _interviewerService.SendInviteCode(codeInviteVM, emailId))
                    {
                        TempData["MessageInterviewerCode"] = "Invitecode has been sent Successfully!";
                        return RedirectToAction("InviteCodeList");
                    }
                    else
                    {
                        TempData["MessageInterviewerCode"] = "Invitecode has been failed";
                        return RedirectToAction("InviteCodeList");
                    }
                }
                else
                {
                    TempData["MessageInterviewerCode"] = "Invitecode has been failed";
                    return RedirectToAction("InviteCodeList");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["MessageInterviewerCode"] = "InviteCode is Failed";
                return RedirectToAction("InviteCodeList");
            }
        }

        public async Task<IActionResult> CodeInviteReport(string inviteid, int Id)
        {
            var _reportmodel = await _interviewerService.CheckCodeConsoleReport(inviteid);
            if (string.IsNullOrWhiteSpace(_reportmodel.submissionId))
            {
                var resultset = await GetReportSummary(inviteid);
                _reportmodel = resultset;
                await _interviewerService.AddCodeConsoleReport(resultset);
                var resultreport = await GetReportDetails(resultset.submissionId);
                _reportmodel.codeinvitereport = resultreport;
                await _interviewerService.AddCodeConsoleReportDetails(_reportmodel);
            }
            else
            {
                var resultreport = new List<CodeInviteReportDetails>();
                var questions = _reportmodel.codeinvitereport.Select(x => x.questionid).ToList();
                foreach (var item in questions)
                {
                    var modeltestcase = new List<CodeInviteReportTestCases>();
                    var testcases = _reportmodel.codeinvitereport.Where(x => x.questionid == item).ToList();
                    var questiondetails = testcases.FirstOrDefault();
                    foreach (var _testcase in testcases)
                    {
                        modeltestcase.Add(new CodeInviteReportTestCases { input = _testcase.input, expectedOutput = _testcase.expectedOutput, actualOutput = _testcase.actualOutput, status = _testcase.testcasestatus });
                    }
                    resultreport.Add(new CodeInviteReportDetails
                    {
                        questionName = questiondetails.questionName,
                        questionDescription = questiondetails.questionDescription,
                        candidateSolution = questiondetails.candidateSolution,
                        achievedScore = questiondetails.achievedScore,
                        questionScore = questiondetails.questionScore,
                        status = questiondetails.questionstatus,
                        testCaseResults = modeltestcase
                    });
                }
                _reportmodel.codeinvitereport = resultreport;
            }
            return View(_reportmodel);
        }
        public async Task GenerateCodeInviteURL(CodeInviteVM _codeinvite)
        {
            var model = new CodeInviteURLRequest();
            model.userName = _codeinvite.userName;
            model.candidateEmail = _codeinvite.candidateEmail;
            model.programmingLanguage = _codeinvite.programmingLanguage;
            model.experience = _codeinvite.experience;
            model.inviteStartTime = _codeinvite.inviteStartTime.ToString("yyyy-MM-dd HH:mm:ss");
            model.inviteEndTime = _codeinvite.inviteEndTime.ToString("yyyy-MM-dd HH:mm:ss");
            model.zone = "IST";
            model.difficultyLevel = _codeinvite.difficultyLevel.ToLower();
            model.assignmentType = "QUICK";
            var client = new RestClient(codeapiendpoint + "invited-assignment");
            var request = new RestRequest(codeapiendpoint + "invited-assignment", Method.Post);
            request.AddHeader("content-type", "application/json");
            request.AddJsonBody(model);
            RestResponse response = await client.ExecuteAsync(request);
            var convertedResponse = JsonConvert.DeserializeObject<CodeInviteURLResponse>(response.Content);
            _codeinvite.invite_UrL = convertedResponse.inviteUrl;
            _codeinvite.inviteId = convertedResponse.inviteUrl.Split("/").LastOrDefault();
            var botcodeurl = await GenerateBotCodeLink(_codeinvite.candidateEmail, _codeinvite.invite_UrL);
            if (botcodeurl != null)
                _codeinvite.botURL = botcodeurl.emailBotUrls.FirstOrDefault().botTestLink;
        }
        public async Task<CodeInviteReportVM> GetReportSummary(string inviteid)
        {
            var client = new RestClient(codeapiendpoint + "submissions/invite/" + inviteid);
            var request = new RestRequest(codeapiendpoint + "submissions/invite/" + inviteid, Method.Post);
            request.AddHeader("content-type", "application/json");
            RestResponse response = await client.ExecuteAsync(request);
            var convertedResponse = JsonConvert.DeserializeObject<CodeInviteReportVM>(response.Content);
            return convertedResponse;
        }
        public async Task<List<CodeInviteReportDetails>> GetReportDetails(string submissionid)
        {
            var client = new RestClient(codeapiendpoint + "submissions/" + submissionid);
            var request = new RestRequest(codeapiendpoint + "submissions/" + submissionid, Method.Post);
            request.AddHeader("content-type", "application/json");
            RestResponse response = await client.ExecuteAsync(request);
            var convertedResponse = JsonConvert.DeserializeObject<List<CodeInviteReportDetails>>(response.Content);
            return convertedResponse;
        }
        public async Task<MCQBotResponse> GenerateBotCodeLink(string emailds, string CodeTestLink)
        {
            var model = new MCQBotRequest();
            model.tenantUuid = "e5f37005-42a3-4510-b211-0a3c0ee675b7";
            model.email = emailds;
            model.aadharVerification = "Y";
            model.faceVerification = "Y";
            model.interviewerTraining = "N";
            model.companyEmailVerification = "Y";
            model.videoBio = "N";
            model.careerQuestions = "N";
            model.testLink.mcq = "";
            model.testLink.code = CodeTestLink;
            var client = new RestClient("https://bot.risebird.io/api/v1/bot/invite");
            var request = new RestRequest("https://bot.risebird.io/api/v1/bot/invite", Method.Post);
            request.AddHeader("content-type", "application/json");
            request.AddJsonBody(model);
            RestResponse response = await client.ExecuteAsync(request);
            var convertedResponse = JsonConvert.DeserializeObject<MCQBotResponse>(response.Content);
            return convertedResponse;
        }
        #endregion
        [AllowAnonymous]
        public async Task<IActionResult> ShowInterest(string uniqueid)
        {
            var model = new InterviewerProfileVM();
            var _list = await _aadhaarService.FetchCandidateEmailidByUniqueId(uniqueid);
            if (_list == null)
            {
                ViewBag.status = 3;
                return View(model);
            }
            model.EmailId = _list.EmailId;
            model.uniqueid = uniqueid;
            ViewBag.status = 2;
            return View(model);
        }
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> ShowInterest(InterviewerProfileVM model)
        {
            if (model.ResumeFile != null)
            {
                var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(model.ResumeFile);
                if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                                    || fileMimeType == "application/pdf" || fileMimeType == "application/x-java-archive" || fileMimeType == "application/vnd.ms-powerpoint")
                {
                    var resumeFileName = model.ResumeFile.FileName.Replace(" ", "_");
                    await _azureStorageHelper.InsertBlob(resumeFileName, "resumes", model.ResumeFile);
                    var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                    var blob = containerRef.GetBlockBlobReference(resumeFileName);
                    model.ResumeLink = blob.Uri.ToString();
                }
            }
            else
            {
                ViewBag.status = 2;
                ModelState.AddModelError(string.Empty, "Please add a Resume File");
                return View(model);
            }
            if (model.type == "HMUpload")
            {
                var result = await _interviewerService.SaveInterviewerCodedResume(model);
                return RedirectToAction("DeliveryHeadInstruction", "IRF", new { irfid = model.id });
            }
            else
            {
                var result = await _interviewerService.SaveInterviewerResume(model);
                ViewBag.status = 1;
                if (model.type == "InterviewIndex")
                {
                    return RedirectToAction("InterviewerIndex", "Dashboard");
                }
            }
            return View(model);
        }
        [HttpGet]
        public async Task<JsonResult> UpdateInterestedStatus(int status, string uniqueid)
        {
            var result = await _interviewerService.UpdateInterviewerInterest(status, uniqueid);
            if (result)
            {
                return Json("Success");
            }
            else
            {
                return Json("Fail");
            }
        }

        #region SME

        [HttpGet]
        public async Task<IActionResult> AssignedSkillChannelsAsSME()
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
                        var model = await _interviewerService.GetSkillChannelsAssignedToSME(emailid);
                        if (TempData["SMESuccessErrorMessage"] != null)
                        {
                            ViewBag.Message = TempData["SMESuccessErrorMessage"].ToString();
                            TempData.Remove("SMESuccessErrorMessage");
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
                TempData["SuccessErrorMessage"] = "Error retrieving Profile Data!";
                return RedirectToAction("MyInterview", "Interview");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSkillChannelDetailsForSMEByIdToUpdate(int irfId, int interviewerProfileId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                    if (profileName == "Interviewer")
                    {
                        var model = await _interviewerService.GetSkillChannelDetailsForSMEByIdToUpdate(interviewerProfileId, irfId);
                        ViewBag.InterviewerProfileId = interviewerProfileId;
                        return PartialView("UpdateSkillChannelRequirementsSMEPartial", model);

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
                return Json("Error retrieving data! Please try again.");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SMESkillChannelUpdate(IRFVm model, int interviewerProfileId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Interviewer")
                    {
                        ViewBag.InterviewerProfileId = interviewerProfileId;
                        var emailId = HttpContext.Session.GetString("email")?.Trim();
                        emailId = emailId.Replace("\"", "");
                        bool isValidModel = true;
                        if (model.SkillCollection != null && model.SkillCollection.Any(x => x.LevelId == 0 || x.Weightage == 0 || x.SkillId == 0 || x.Timelimit == 0 || string.IsNullOrEmpty(x.Concepts) || string.IsNullOrEmpty(x.Natureofquestion)))
                        {
                            ModelState.AddModelError("", "All the fields under the Tech Skill section are required with weightage greater than 0.");
                            isValidModel = false;
                        }
                        if (model.SkillCollection != null && model.SkillCollection.Sum(x => x.Weightage) != 100)
                        {
                            ModelState.AddModelError("", "The Weightage should add up to an exact 100 under Tech Skill Section.");
                            isValidModel = false;
                        }
                        if (!isValidModel)
                        {
                            model.IsValidCustom = isValidModel;

                            return PartialView("UpdateSkillChannelRequirementsSMEPartial", model);
                        }
                        string fileName = string.Empty;
                        var files = Request.Form.Files;
                        if (files != null && files.Count() > 0)
                        {

                            fileName = ContentDispositionHeaderValue.Parse(files[0].ContentDisposition).FileName.Trim('"');

                        }

                        if (files != null && files.Count() > 0)
                        {
                            var jdFile = files[0];
                            var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(jdFile);
                            if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                                                || fileMimeType == "application/pdf" || fileMimeType == "application/x-java-archive" || fileMimeType == "application/vnd.ms-powerpoint")
                            {
                                await _azureStorageHelper.InsertBlob(fileName, "resumes", jdFile);
                                var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                                var blob = containerRef.GetBlockBlobReference(fileName);
                                string uploadedFileURL = blob.Uri.ToString();
                                if (uploadedFileURL == null)
                                {
                                    ModelState.AddModelError("", "Could not upload JD File.");
                                    model.IsValidCustom = false;
                                    return PartialView("UpdateSkillChannelRequirementsSMEPartial", model);

                                }
                                model.JDLink = uploadedFileURL;
                            }
                        }
                        else
                        {
                            model.JDLink = null;
                        }

                        await _interviewerService.UpdateSkillChannelDetailsBySME(model, interviewerProfileId);

                        return PartialView("UpdateSkillChannelRequirementsSMEPartial", model);

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
                return Json(false);
            }
        }

        [HttpPost]
        public async Task<string> UploadSMEAudio()
        {
            try
            {
                var file = Request.Form.Files;
                var nvc = Request.Form;
                int irfId = 0;
                int interviewerProfileId = 0;
                //string currentUserEmail = string.Empty;
                if (!string.IsNullOrEmpty(nvc["irfId"]))
                {
                    irfId = Convert.ToInt32(nvc["irfId"]);
                }

                if (!string.IsNullOrEmpty(nvc["smeProfileId"]))
                {
                    interviewerProfileId = Convert.ToInt32(nvc["smeProfileId"]);
                }

                var fileName = ContentDispositionHeaderValue.Parse(file[0].ContentDisposition).FileName.Trim('"');
                string uploadedaudioFileURL = string.Empty;
                if (file != null)
                {
                    var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(file[0]);
                    if (fileMimeType == "audio/basic" || fileMimeType == "audio/mpeg" || fileMimeType == "audio/x-wav" || fileMimeType == "video/x-msvideo")
                    {
                        await _azureStorageHelper.InsertBlob(fileName, "audios", file[0]);
                        var containerRef = _azureStorageHelper.GetCloudBlobContainer("audios");
                        var blob = containerRef.GetBlockBlobReference(fileName);
                        uploadedaudioFileURL = blob.Uri.ToString();
                    }
                    else
                    {
                        return null;
                    }

                }
                _ = await _interviewerService.SaveSMEAudio(irfId, interviewerProfileId, uploadedaudioFileURL);
                return uploadedaudioFileURL;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [HttpGet]
        public async Task<IActionResult> SkillChannelInterviewers(int irfId, int interviewerProfileId, string skillChannelName)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                    if (profileName == "Interviewer")
                    {
                        var model = await _interviewerService.GetSkillChannelInterviewersToVerify(irfId);
                        if (model != null)
                        {
                            model = (IList<TransInterviewer>)model;
                        }
                        ViewBag.IRFId = irfId;
                        ViewBag.InterviewerProfileId = interviewerProfileId;
                        ViewBag.SkillChannelName = skillChannelName;
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
                return Json("Error retrieving data! Please try again.");
            }
        }

        [HttpPost]
        public async Task<IActionResult> VerifyIRFInterviewersBySME(IList<TransInterviewer> model, int irfId, int interviewerProfileId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Interviewer")
                    {
                        var emailId = HttpContext.Session.GetString("email")?.Trim();
                        emailId = emailId.Replace("\"", "");
                        //bool isValidModel = true;
                        //if (model.SkillCollection != null && model.SkillCollection.Any(x => x.LevelId == 0 || x.Weightage == 0 || x.SkillId == 0 || x.Timelimit == 0 || string.IsNullOrEmpty(x.Concepts) || string.IsNullOrEmpty(x.Natureofquestion)))
                        //{
                        //    ModelState.AddModelError("", "All the fields under the Tech Skill section are required with weightage greater than 0.");
                        //    isValidModel = false;
                        //}


                        if (await _interviewerService.UpdateVerifiedInterviewers(model, emailId))
                        {
                            TempData["SMESuccessErrorMessage"] = "Interviewers' profile verified successfully!";
                        }
                        else
                        {
                            TempData["SMESuccessErrorMessage"] = "Failed to verify interviewers' profile. Please try again!";
                        }
                        return RedirectToAction("AssignedSkillChannelsAsSME");

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
                return Json(false);
            }
        }

        #endregion
        public async Task<IActionResult> InterviewerResumeupdate(int userid, int irfid, string resumeLink = null)
        {
            var model = new InterviewerProfileVM();
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");

                if (profileName == "Interviewer")
                {
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    model.EmailId = emailid;
                    model.type = "InterviewIndex";
                }
                else if (profileName != "Client" && profileName != "Public")
                {
                    model.type = "HMUpload";
                    model.UserId = userid;
                    model.id = irfid;
                }
                if (!string.IsNullOrEmpty(resumeLink) && userid != 0)
                {
                    //call method to redact resume and save the redacted resume
                    var redactedResumeLink = await RedactResume(resumeLink, userid);
                    if (!string.IsNullOrEmpty(redactedResumeLink))
                    {
                        return Json(redactedResumeLink);
                    }
                    else
                    {
                        return Json("failed");
                    }
                }
            }
            return PartialView(model);
        }

        private async Task<string> RedactResume(string resumeLink, int userId)
        {
            try
            {
                var redactedResumeLink = await _affIndaResumeParserHelper.UploadResumeForRedaction(resumeLink);
                var resumeFileName = resumeLink?.Split('/')?.Last();
                if (!string.IsNullOrEmpty(redactedResumeLink))
                {
                    var model = new InterviewerProfileVM();

                    HttpClient myWebClient = new HttpClient();
                    string uploadedCodedResumeLink = string.Empty;
                    using (Stream myStream = myWebClient.GetStreamAsync(redactedResumeLink).Result)
                    {
                        long unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                        var fileName = "Redacted_" + unixTime.ToString() + "_" + GenerateRandomString(10);
                        //var fileName = resumeFileName + "_redacted" + "_" + unixTime.ToString();
                        try
                        {
                            uploadedCodedResumeLink = _azureStorageHelper.InsertBlobCloudCompare(fileName, "resumes", null, myStream, contentType: "application/pdf").Result;

                        }
                        catch (Exception ex)
                        {
                            await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);

                        }



                        //generate SAS URL from the uploaded video url
                        var sas = _sasUrlHelper.CreateSasUrl("resumes");

                        if (!string.IsNullOrEmpty(uploadedCodedResumeLink) && !string.IsNullOrEmpty(sas))
                        {
                            uploadedCodedResumeLink = uploadedCodedResumeLink + "?" + sas;
                        }
                    }

                    model.UserId = userId;
                    model.CodedResumeLink = uploadedCodedResumeLink;
                    model.IsResumeRedactAttempted = true;
                    model.IsResumeRedactFailed = false;

                    //save the codedResumeLink and the resume link to database;

                    var result = await _interviewerService.SaveInterviewerCodedResume(model);
                    if (result)
                    {
                        return uploadedCodedResumeLink;

                    }
                    else
                        return null;

                }
                return null;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        static string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            Random random = new Random();
            char[] randomArray = new char[length];

            for (int i = 0; i < length; i++)
            {
                randomArray[i] = chars[random.Next(chars.Length)];
            }

            return new string(randomArray);
        }
        public async Task<IActionResult> FetchInterviewerFlagDetails(string InterviewerEmailid)
        {
            try
            {
                var result = await _interviewerService.GetInterviewerFlagDetails(InterviewerEmailid);
                return PartialView(result);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<JsonResult> InterviewerFlagUpdate(string Emailid, bool? LeadInterviewer, bool? IsApproved, bool? IsActive, bool? IsBlocked)
        {
            var result = await _interviewerService.UpdateInterviewerFlag(Emailid, LeadInterviewer, IsApproved, IsActive, IsBlocked);
            return Json("success");
        }
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> InterviewerStatsAndRatings(int? irfId = null, string irfName = null)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                //var emailid = HttpContext.Session.GetString("email")?.Trim();
                //emailid = emailid.Replace("\"", "");

                if (profileName == "Admin" || profileName == "Finance" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "SeniorInterviewerManagement")
                {
                    IEnumerable<InterviewerStatsVM> statsList;
                    statsList = await _interviewerService.GetInterviewerStatsAndRatings(irfId);
                    ViewBag.Profile = profileName;
                    if (!string.IsNullOrEmpty(irfName))
                    {
                        ViewBag.IRFName = irfName;
                    }
                    if (irfId != null && irfId.Value > 0)
                    {
                        ViewBag.irfId = irfId.Value;
                    }
                    return View(statsList);
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


        [AllowAnonymous]
        [HttpGet]
        ///usepl is usePublicLayout
        public async Task<IActionResult> InterviewerStatsAndRatingsForClientByIRF(string clientEmailId, bool usepl, int? irfId = null, string irfName = null)
        {
            try
            {
                IEnumerable<InterviewerStatsVM> statsList;
                statsList = await _interviewerService.GetInterviewerStatsAndRatings(irfId, clientEmailId);
                ViewBag.ClientEmailId = clientEmailId;
                if (!string.IsNullOrEmpty(irfName))
                {
                    ViewBag.IRFName = irfName;
                }
                if (irfId != null && irfId.Value > 0)
                {
                    ViewBag.irfId = irfId.Value;
                }
                ViewBag.Usepl = "";
                if (usepl)
                {
                    ViewBag.Usepl = "Public";
                }
                return View("InterviewerStatsAndRatings", statsList);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> InterviewerStatsAndRatingsByIRF(int? irfId = null)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                //var emailid = HttpContext.Session.GetString("email")?.Trim();
                //emailid = emailid.Replace("\"", "");

                if (profileName == "Admin" || profileName == "Finance" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "SeniorInterviewerManagement")
                {
                    IEnumerable<InterviewerStatsVM> statsList;
                    statsList = await _interviewerService.GetInterviewerStatsAndRatings(irfId);
                    ViewBag.Profile = profileName;
                    ViewBag.DisplayType = "popup";
                    return PartialView("InterviewerStatsAndRatingsPartialView", statsList);
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

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> DeactivateSkillChannelInterviewer(int irfId, string interviewerEmail)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        var result = await _interviewerService.DeactivateSkillChannelInterviewer(irfId, interviewerEmail);
                        return Json(result);
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
                return Json(false);
            }

        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> InterviewerStatsDetails(int userId, string statType, string interviewerEmailId = null, int? irfId = null, string irfName = null, string usePl = null)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");

                if (profileName == "Admin" || profileName == "Finance" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "SeniorInterviewerManagement")
                {
                    IEnumerable<StatsDetailsVM> statsDetailsList;
                    statsDetailsList = await _interviewerService.GetInterviewerStatsAndRatingsDetails(userId, statType, irfId);
                    ViewBag.Profile = profileName;
                    ViewBag.StatType = statType;
                    ViewBag.EmailId = interviewerEmailId + "(Interviewer)";
                    //ViewBag.DisplayType = "popup";
                    if (irfId.HasValue)
                    {
                        ViewBag.IRFId = irfId.Value;
                        ViewBag.IRFName = irfName;
                    }
                    ViewBag.UsePl = string.Empty;
                    if (!string.IsNullOrEmpty(usePl))
                    {
                        ViewBag.UsePl = usePl;
                    }
                    return View("StatsDetails", statsDetailsList);

                    //else
                    //{
                    //    return PartialView("StatsDetailsTablePartialView", statsDetailsList);
                    //}
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

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> InterviewerStatsDetailsForClient(int userId, string statType, string clientEmailId, string interviewerEmailId = null, int? irfId = null, string irfName = null, string usePl = null)
        {
            try
            {
                IEnumerable<StatsDetailsVM> statsDetailsList;
                statsDetailsList = await _interviewerService.GetInterviewerStatsAndRatingsDetails(userId, statType, irfId, clientEmailId);
                statsDetailsList = statsDetailsList.Select(x =>
                {
                    x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty;
                    return x;
                }).ToList();
                ViewBag.StatType = statType;
                ViewBag.EmailId = interviewerEmailId + "(Interviewer)";
                ViewBag.ClientEmailId = clientEmailId;
                //ViewBag.DisplayType = "popup";
                if (irfId.HasValue)
                {
                    ViewBag.IRFId = irfId.Value;
                    ViewBag.IRFName = irfName;
                }
                ViewBag.UsePl = string.Empty;
                if (!string.IsNullOrEmpty(usePl))
                {
                    ViewBag.UsePl = usePl;
                    if (usePl == "Public")
                    {
                        statsDetailsList = statsDetailsList.OrderByDescending(x => x.interviewDate).Take(5);
                    }
                }
                return View("StatsDetails", statsDetailsList);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [HttpPost]
        public async Task<JsonResult> AddSuspiciousTypeComment(string Email, string Comment)
        {
            try
            {
                SuspiciousInterviewerVM model = new SuspiciousInterviewerVM();
                var emailId = HttpContext.Session.GetString("email")?.Trim();
                emailId = emailId.Replace("\"", "");
                model.EmailId = Email;
                model.Comment = Comment;
                model.Createddate = DateTime.Now;
                model.CreatedBy = emailId;
                var data = await _interviewerService.AddSuspiciousTypeService(model);
                if (data == true)
                {
                    var result = new TransInterviewersReliabilityScoresVm();
                    result.InterviewMeetingId = 0;
                    result.InterviewerEmail = Email;
                    result.ReliabilityScoreTypeId = (int)ReliabilityScopeTypeEnum.SuspiciousInterviewer;
                    await _meetingService.AddTransInterviewersReliabilityScoresService(result);
                    return Json("Suspicious Interviewer marked Successfully");
                }
                else
                {
                    return Json("Fail to marked Suspicious Interviewer, Try again after sometime.");
                }

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        #region Open Interview Notification: Public URL
        [HttpPost]
        public async Task<JsonResult> SendOpenURLNotification(int? irfId = null)
        {
            var _AllInterviewerInterview = await _interviewerService.FetchAllInterviewerallUpcomingInterview("Verified", irfId);
            foreach (var item in _AllInterviewerInterview)
            {
                if (!string.IsNullOrEmpty(item.EmailId))
                {
                    string sendreporturl = string.Empty;
                    if (string.IsNullOrEmpty(item.uniqueid))
                    {
                        Guid uniqueid = Guid.NewGuid();
                        var _uniqueid = await _clientService.PublicInterviewerUrlSave(item.EmailId, uniqueid.ToString());
                        sendreporturl = "https://" + Request.Host.ToString() + "/Interview/PublicOpenInterviewInvite?id=" + uniqueid;
                        if (!string.IsNullOrEmpty(_uniqueid))
                        {
                            sendreporturl = "https://" + Request.Host.ToString() + "/Interview/PublicOpenInterviewInvite?id=" + _uniqueid;
                        }
                    }
                    else
                    {
                        sendreporturl = "https://" + Request.Host.ToString() + "/Interview/PublicOpenInterviewInvite?id=" + item.uniqueid;
                    }
                    try
                    {
                        //Email Message Send
                        Mail _mail1 = new Mail
                        {
                            TO = item.EmailId,
                            CC = "talent@risebird.io",
                            Subject = "New interviews have come up! Please assign."
                        };
                        var emailsend1 = await _notificationHelper.SendInterviewerCommonInterviewNotification(_mail1, sendreporturl, item.Total, item.uniqueid, irfId);
                        //What's App Message Send
                        WhatsAppVM _whatsAppVM = new WhatsAppVM();
                        _whatsAppVM.Emailid = item.EmailId;
                        _whatsAppVM.MessageValue = item.Total.ToString();
                        _whatsAppVM.buttonlink = !string.IsNullOrEmpty(sendreporturl) ? sendreporturl.Replace("https://portal.risebird.io/", "") : "";
                        _whatsAppVM.Event = "Interviewer Common Interview";
                        _whatsAppVM.irfId = irfId.HasValue ? irfId.Value : 0;
                        await _whatsAppHelper.WhatsAppSendInterviewerCommonInterviewDetails(_whatsAppVM);
                    }
                    catch (Exception ex)
                    {
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                    }
                }
            }
            return Json("Success");
        }
        #endregion
        #region Interviewer Video Confirmation
        [HttpPost]
        public async Task<JsonResult> UpdateVideoConfirmationStatus(int responsestatus)
        {
            if (HttpContext.Session.GetString("email") != null)
            {
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                var result = await _interviewerService.UpdateVideoConfirmationStatus(responsestatus, emailid);
                if (result)
                {
                    var resultReliability = new TransInterviewersReliabilityScoresVm();
                    resultReliability.InterviewMeetingId = 0;
                    resultReliability.InterviewerEmail = emailid;
                    if (responsestatus == 1)
                    {
                        resultReliability.ReliabilityScoreTypeId = (int)ReliabilityScopeTypeEnum.VideoConfirmationYes;
                    }
                    else
                    {
                        resultReliability.ReliabilityScoreTypeId = (int)ReliabilityScopeTypeEnum.VideoConfirmationNo;
                    }
                    await _meetingService.AddTransInterviewersReliabilityScoresService(resultReliability);
                }
                return Json("Successfully updated your response.");
            }
            else
            {
                return Json("Fail to update the response,please try again.");
            }
        }
        #endregion
        #region Interviewer Insights
        public async Task<IActionResult> InterviewerInsights(string interviewer, string IrfId, string type = null)
        {
            var profileName = HttpContext.Session.GetString("profileName")?.Trim();
            profileName = profileName?.Replace("\"", "");
            ViewBag.RegularAccess = "";
            if (profileName == "Admin" || profileName == "HR" || profileName == "Finance" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller")
            {
                ViewBag.RegularAccess = "1";
            }
            var result = await _interviewerService.GetInterviewerInsights(interviewer, IrfId);
            if (result == null)
            {
                result = new InterviewerInsightsVM();
            }
            if (!string.IsNullOrEmpty(result.ResumeLink))
            {
                result.ResumeLink = result.ResumeLink.Replace(" ", "%20");
            }
            result.ProfileType = ((Heuristicsscorelevel)Convert.ToInt32(result.ProfileType)).GetDescription();
            if (result.ProfileType == "0")
            {
                result.ProfileType = "";
            }
            if (result.ProfileType?.Length > 1)
                result.ProfileType = result.ProfileType.Substring(0, 1);
            result.IRFId = IrfId;
            ViewBag.type = type;
            return PartialView(result);
        }
        public async Task<IActionResult> ReliabilityScoreDetails(string interviewer, string IrfId)
        {
            var result = await _interviewerService.GetReliabilityScoreDetails(interviewer, IrfId);
            return PartialView(result);
        }
        public async Task<IActionResult> InterviewerInsightsMarkersDetails(string interviewer, string type, string Irfid)
        {
            ViewBag.Type = type;
            var result = await _interviewerService.GetInterviewerInsightsMarkersDetails(interviewer, type, Irfid);
            return PartialView(result);
        }
        public async Task<JsonResult> AcknowledgeRedmarkers(string uniqueid)
        {
            var result = await _interviewerService.updateRedmarkerAcknowledge(uniqueid);
            return Json("Acknowledgement updated successfully");
        }
        [HttpPost]
        public async Task<IActionResult> InterviewerRedMarkerReport()
        {
            if (HttpContext.Session.GetString("email") != null)
            {
                var emailId = HttpContext.Session.GetString("email")?.Trim();
                emailId = emailId.Replace("\"", "");
                var _redmarkers = await _interviewerRepository.GetInterviewerInsights(emailId, string.Empty);
                return PartialView(_redmarkers);
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }
        #endregion
        [HttpPost]
        public async Task<JsonResult> UploadVideoKYC()
        {
            if (HttpContext.Session.GetString("email") != null)
            {
                var emailId = HttpContext.Session.GetString("email")?.Trim();
                emailId = emailId.Replace("\"", "");
                if (Request.Form.Files.Count() > 0)
                {
                    var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(Request.Form.Files[0]);
                    if (fileMimeType == "video/mpeg" || fileMimeType == "video/mp4" || fileMimeType == "video/quicktime" || fileMimeType == "video/x-matroska" || fileMimeType == "video/webm")
                    {
                        var file = Request.Form.Files[0];
                        var filename = emailId.Replace(".", "_").Replace("@", "a").ToString() + ".mp4";
                        //await _azureStorageHelper.InsertBlob(filename, "videokyc", file);
                        await _azureStorageHelper.InsertImageBlob(filename, "videokyc", file, "video/mp4");
                        var containerRef = _azureStorageHelper.GetCloudBlobContainer("videokyc");
                        var blob = containerRef.GetBlockBlobReference(filename);
                        string uploadedvideoFileURL = blob.Uri.ToString();
                        //var sas = _sasUrlHelper.CreateSasUrl("videokyc");
                        var VideoLink = string.Empty;
                        //if (!string.IsNullOrEmpty(uploadedvideoFileURL) && !string.IsNullOrEmpty(sas))
                        //{
                        //    VideoLink = uploadedvideoFileURL + "?" + sas;
                        //}
                        if (!string.IsNullOrEmpty(uploadedvideoFileURL))
                        {
                            VideoLink = uploadedvideoFileURL;
                        }

                        var result = await _interviewerService.UpdateVideoKYCDetails(VideoLink, emailId);
                        return Json("Video KYC uploaded successfully.");
                    }
                }
                return Json("Fail to upload, Please try again");
            }
            else
            {
                return Json("Fail to upload, Please try after some time.");
            }
        }

        [HttpPost]
        public async Task<JsonResult> GetInterviwerExists(string interviewer)
        {
            try
            {
                var role = _interviewerService.GetUserRolebyEmailid(interviewer);

                if (!string.IsNullOrWhiteSpace(role))
                {
                    return Json("This mail id already exists in our system as " + role + ". Please check and add accordingly.");
                }
                else
                {
                    return Json(null);
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("Sorry! Some error occured. Please try again.");
            }
        }
        [HttpGet]
        public async Task<JsonResult> InterviewerAcknowledgementResponse(int status, string user)//, int irf
        {
            //check link expiry befire hitting db

            try
            {
                //dec = dec.Replace(" ", "+");

                int id;
                int? userId = new int?();
                // Try to convert the string to an integer
                bool success = int.TryParse(user, out id);

                // Check if the conversion was successful
                if (success)
                {
                    userId = id;
                }
                else
                {
                    string decodedEncryptedId = System.Net.WebUtility.UrlDecode(user);
                    userId = await _encryptionHelper.DecryptInteger(decodedEncryptedId);
                }

                //var userId = await _encryptionHelper.DecryptData(user);

                if (userId.HasValue && userId.Value > 0)//!string.IsNullOrEmpty(userId)
                {
                    var result = await _interviewerService.UpdateInterviewerActiveStatus(status, userId.Value);//, irf

                    if (result != null)
                    {

                        if (!string.IsNullOrEmpty(result.EmailId))
                        {
                            var _AllInterviewerInterview = await _interviewerService.FetchAllInterviewerallUpcomingInterview("Verified");
                            foreach (var item in _AllInterviewerInterview?.Where(x => x.EmailId == result.EmailId))
                            {
                                string sendreporturl = string.Empty;
                                if (string.IsNullOrEmpty(result.uniqueid))
                                {
                                    Guid uniqueid = Guid.NewGuid();
                                    var _uniqueid = await _clientService.PublicInterviewerUrlSave(result.EmailId, uniqueid.ToString());
                                    sendreporturl = "https://" + Request.Host.ToString() + "/Interview/PublicOpenInterviewInvite?id=" + uniqueid;
                                    if (!string.IsNullOrEmpty(_uniqueid))
                                    {
                                        sendreporturl = "https://" + Request.Host.ToString() + "/Interview/PublicOpenInterviewInvite?id=" + _uniqueid;
                                    }
                                }
                                else
                                {
                                    sendreporturl = "https://" + Request.Host.ToString() + "/Interview/PublicOpenInterviewInvite?id=" + result.uniqueid;
                                }
                                try
                                {
                                    //Email Message Send
                                    Mail _mail1 = new Mail
                                    {
                                        TO = result.EmailId,
                                        CC = "talent@risebird.io",
                                        Subject = "New interviews have come up! Please assign."
                                    };
                                    var emailsend1 = await _notificationHelper.SendInterviewerCommonInterviewNotification(_mail1, sendreporturl, item.Total, result.uniqueid);
                                    //What's App Message Send
                                    //WhatsAppVM _whatsAppVM = new WhatsAppVM();
                                    //_whatsAppVM.Emailid = item.EmailId;
                                    //_whatsAppVM.MessageValue = item.Total.ToString();
                                    //_whatsAppVM.buttonlink = !string.IsNullOrEmpty(sendreporturl) ? sendreporturl.Replace("https://portal.risebird.io/", "") : "";
                                    //_whatsAppVM.Event = "Interviewer Common Interview";
                                    //_whatsAppVM.irfId = irfId.HasValue ? irfId.Value : 0;
                                    //await _whatsAppHelper.WhatsAppSendInterviewerCommonInterviewDetails(_whatsAppVM);
                                }
                                catch (Exception ex)
                                {
                                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                                }
                            }

                        }
                        return Json("Status updated successfully.");
                    }
                    else
                    {
                        return Json("Failed to update status. Please try again.");
                    }
                }
                else
                {
                    return Json("Bad Request.");
                }

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("Failed to update status, Please try again");
            }

        }

        [HttpGet]
        public async Task<IActionResult> GetFLNData()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "SupportAdmin" || profileName == "Finance" || profileName == "SeniorHR" || profileName == "SeniorInterviewerManagement")//|| profileName== "InterviewerManagement"
                    {
                        var model = await _interviewerService.GetFLNConversionData(emailId);
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
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Apply(string id, string upt = null)//, int irf
        {
            //check link expiry befire hitting db

            try
            {
                int resultId;
                int? irfId = new int?();
                // Try to convert the string to an integer
                bool success = int.TryParse(id, out resultId);
                string decodedEncryptedId = string.Empty;
                // Check if the conversion was successful
                if (success)
                {
                    irfId = resultId;
                }
                else
                {
                    decodedEncryptedId = System.Net.WebUtility.UrlDecode(id);
                    irfId = Convert.ToInt32((await _encryptionHelper.DecryptStringValue(decodedEncryptedId)));

                }

                //dec = dec.Replace(" ", "+");
                //string decodedEncryptedId = System.Net.WebUtility.UrlDecode(id);
                //var irfId = await _encryptionHelper.DecryptInteger(decodedEncryptedId);
                //var irfId = id;
                if (irfId.HasValue && irfId.Value > 0)//!string.IsNullOrEmpty(irfId)
                {
                    //first check if the irf id is valid
                    bool isValid = true;
                    if (isValid)
                    {
                        return View(new InterviewerDirectApplicationVM { IRFId = irfId.Value, EncryptedIrfId = decodedEncryptedId });

                    }
                    else
                    {
                        return Json("Invalid");
                    }

                }
                else
                {
                    return Json("Bad Request!");
                }

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("Something's wrong. Please try again later.");
            }

        }

        [HttpPost]
        public async Task<IActionResult> Apply(InterviewerDirectApplicationVM model)//, int irf
        {
            try
            {

                if (model.ResumeFile != null)
                {
                    string uploadedFileURL = string.Empty;
                    var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(model.ResumeFile);
                    if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                                        || fileMimeType == "application/pdf" || fileMimeType == "application/x-java-archive" || fileMimeType == "application/vnd.ms-powerpoint")
                    {
                        var resumeFileName = model.ResumeFile.FileName.Replace(" ", "_");

                        await _azureStorageHelper.InsertBlob(resumeFileName, "resumes", model.ResumeFile);
                        var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                        var blob = containerRef.GetBlockBlobReference(resumeFileName);
                        uploadedFileURL = blob.Uri.ToString();
                        model.ResumeLink = uploadedFileURL;
                    }


                }

                int? irfId = model.IRFId;
                //var irfId = await _encryptionHelper.DecryptInteger(model.EncryptedIrfId);
                //var irfId = model.EncryptedIrfId;
                if (irfId.HasValue && irfId.Value > 0)//!string.IsNullOrEmpty(irfId)
                {
                    model.IRFId = Convert.ToInt32(irfId);//irfId.Value;
                    var result = await _interviewerService.SubmitInterviewerApplication(model);//, irf
                    if (result)
                    {
                        return Json("Your application has been submitted successfully!");
                    }
                    else
                    {
                        return Json("Submission failed!");
                    }
                }
                else
                {
                    return Json("Failed!");
                }

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("Something's wrong. Please try again later.");
            }

        }

        [HttpGet]
        public async Task<IActionResult> FTApply(string id, string upt = null)//, int irf
        {
            //check link expiry befire hitting db

            try
            {
                int resultId;
                int? irfId = new int?();
                // Try to convert the string to an integer
                bool success = int.TryParse(id, out resultId);
                string decodedEncryptedId = string.Empty;
                // Check if the conversion was successful
                if (success)
                {
                    irfId = resultId;
                }
                else
                {
                    decodedEncryptedId = System.Net.WebUtility.UrlDecode(id);
                    //irfId = await _encryptionHelper.DecryptInteger(decodedEncryptedId);
                    irfId = Convert.ToInt32((await _encryptionHelper.DecryptStringValue(decodedEncryptedId)));
                }

                //dec = dec.Replace(" ", "+");
                //string decodedEncryptedId = System.Net.WebUtility.UrlDecode(id);
                //var irfId = await _encryptionHelper.DecryptInteger(decodedEncryptedId);
                //var irfId = id;
                if (irfId.HasValue && irfId.Value > 0)//!string.IsNullOrEmpty(irfId)
                {
                    //first check if the irf id is valid
                    bool isValid = true;
                    if (isValid)
                    {
                        return View(new InterviewerDirectApplicationVM { IRFId = irfId.Value, EncryptedIrfId = decodedEncryptedId });

                    }
                    else
                    {
                        return Json("Invalid");
                    }

                }
                else
                {
                    return Json("Bad Request!");
                }

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("Something's wrong. Please try again later.");
            }

        }

        [HttpPost]
        public async Task<IActionResult> FTApply(InterviewerDirectApplicationVM model)//, int irf
        {
            try
            {

                //first check if this interviewer is already present in the transInterviewer table, it could be either a yes or a no verdict
                var existenceCheck = await _interviewerService.CheckIfInterviewerExistsInSystemAndOrSkillChannel(model.EmailId, model.IRFId);
                if (existenceCheck.IsExistsInSystem && !existenceCheck.AlreadyExistsInSkillChannel)
                {
                    if (model.ResumeFile != null)
                    {
                        string uploadedFileURL = string.Empty;
                        var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(model.ResumeFile);
                        if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                                            || fileMimeType == "application/pdf" || fileMimeType == "application/x-java-archive" || fileMimeType == "application/vnd.ms-powerpoint")
                        {
                            var resumeFileName = model.ResumeFile.FileName.Replace(" ", "_");

                            await _azureStorageHelper.InsertBlob(resumeFileName, "resumes", model.ResumeFile);
                            var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                            var blob = containerRef.GetBlockBlobReference(resumeFileName);
                            uploadedFileURL = blob.Uri.ToString();
                            model.ResumeLink = uploadedFileURL;
                        }


                    }

                    int? irfId = model.IRFId;
                    //var irfId = await _encryptionHelper.DecryptInteger(model.EncryptedIrfId);
                    //var irfId = model.EncryptedIrfId;
                    if (irfId.HasValue && irfId.Value > 0 && !string.IsNullOrEmpty(model.ResumeLink))//!string.IsNullOrEmpty(irfId)
                    {
                        //model.IRFId = Convert.ToInt32(irfId);//irfId.Value;
                        model.AssessmentGroup = await _irfService.GetAssessmentGroupFromIRFId(model.IRFId);

                        //first screen the interviewer's resume
                        var resumeStringContent = await _pdfReaderHelper.ReadPdfFromUrlAsync(model.ResumeLink);
                        var response = await _assistantServiceHelper.GenerateResumeAnalysis(resumeStringContent, model.AssessmentGroup);

                        bool saveOperationResult = true;
                        var aiAnalysisModel = new InterviewerResumeFitmentAnalysisSchema();
                        if (response != null)
                        {
                            aiAnalysisModel = JsonConvert.DeserializeObject<InterviewerResumeFitmentAnalysisSchema>(response.Data?.First()?.Content?.FirstOrDefault()?.Text?.Value);
                            if (aiAnalysisModel != null)
                            {
                                saveOperationResult = await _interviewerService.AddInternalInterviewerToIRFPostPositiveVerdict(new InterviewerDirectApplicationVM { EmailId = model.EmailId, IRFId = model.IRFId, OpenAIResponse = aiAnalysisModel.completeFitmentAnalysis, AIVerdict = aiAnalysisModel.verdict, AIAnalysisScore = aiAnalysisModel.fitmentScore, ResumeLink = model.ResumeLink });//, irf
                                if (saveOperationResult)
                                {
                                    if (aiAnalysisModel.verdict.ToLower() == "yes")
                                    {
                                        //_notificationHelper.SendUnassignedInterviewsNotifications();

                                        try
                                        {
                                            var _AllInterviewerInterview = await _interviewerService.FetchAllInterviewerallUpcomingInterview("Verified", model.IRFId);
                                            foreach (var item in _AllInterviewerInterview?.Where(x => x.EmailId == model.EmailId))
                                            {
                                                if (!string.IsNullOrEmpty(item.EmailId))
                                                {
                                                    string sendreporturl = string.Empty;
                                                    if (string.IsNullOrEmpty(item.uniqueid))
                                                    {
                                                        Guid uniqueid = Guid.NewGuid();
                                                        var _uniqueid = await _clientService.PublicInterviewerUrlSave(item.EmailId, uniqueid.ToString());
                                                        sendreporturl = "https://" + Request.Host.ToString() + "/Interview/PublicOpenInterviewInvite?id=" + uniqueid;
                                                        if (!string.IsNullOrEmpty(_uniqueid))
                                                        {
                                                            sendreporturl = "https://" + Request.Host.ToString() + "/Interview/PublicOpenInterviewInvite?id=" + _uniqueid;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        sendreporturl = "https://" + Request.Host.ToString() + "/Interview/PublicOpenInterviewInvite?id=" + item.uniqueid;
                                                    }
                                                    try
                                                    {
                                                        //Email Message Send
                                                        Mail _mail1 = new Mail
                                                        {
                                                            TO = item.EmailId,
                                                            CC = "talent@risebird.io",
                                                            Subject = "New interviews have come up! Please assign."
                                                        };
                                                        var emailsend1 = await _notificationHelper.SendInterviewerCommonInterviewNotification(_mail1, sendreporturl, item.Total, item.uniqueid, irfId);
                                                        //What's App Message Send
                                                        WhatsAppVM _whatsAppVM = new WhatsAppVM();
                                                        _whatsAppVM.Emailid = item.EmailId;
                                                        _whatsAppVM.MessageValue = item.Total.ToString();
                                                        _whatsAppVM.buttonlink = !string.IsNullOrEmpty(sendreporturl) ? sendreporturl.Replace("https://portal.risebird.io/", "") : "";
                                                        _whatsAppVM.Event = "Interviewer Common Interview";
                                                        _whatsAppVM.irfId = irfId.HasValue ? irfId.Value : 0;
                                                        await _whatsAppHelper.WhatsAppSendInterviewerCommonInterviewDetails(_whatsAppVM);
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                                                    }
                                                }

                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);

                                        }
                                        return Json("Your profile matches the requirement. You will be recieving an invitation to assign open interviews(as and when they come up) over mail, shortly. Thankyou!");

                                    }
                                    else if (aiAnalysisModel.verdict.ToLower() == "no")
                                    {
                                        return Json("Your interest has been recorded. We will get back to you shortly. Thankyou!");
                                    }
                                }
                                else
                                {
                                    return Json("Oops! An error occured. Please try again later.");
                                }

                            }
                        }

                    }
                    else
                    {
                        return Json("Unexpected Error! Please try later.");
                    }
                }
                else
                {
                    return Json("Not Applicable");
                }

                return Json("An unexpected error occured. Please try again later.");
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("Something went wrong. Please try again later.");
            }

        }

        [HttpGet]
        public async Task<IActionResult> MAGApply(string id, string upt = null)//, int irf
        {
            //check link expiry befire hitting db

            try
            {
                int resultId;
                int? assessmentGroupId = new int?();
                // Try to convert the string to an integer
                bool success = int.TryParse(id, out resultId);
                string decodedEncryptedId = string.Empty;
                // Check if the conversion was successful
                if (success)
                {
                    assessmentGroupId = resultId;
                }
                else
                {
                    decodedEncryptedId = System.Net.WebUtility.UrlDecode(id);
                    //irfId = await _encryptionHelper.DecryptInteger(decodedEncryptedId);
                    assessmentGroupId = Convert.ToInt32((await _encryptionHelper.DecryptStringValue(decodedEncryptedId)));
                }

                //dec = dec.Replace(" ", "+");
                //string decodedEncryptedId = System.Net.WebUtility.UrlDecode(id);
                //var irfId = await _encryptionHelper.DecryptInteger(decodedEncryptedId);
                //var irfId = id;
                if (assessmentGroupId.HasValue && assessmentGroupId.Value > 0)//!string.IsNullOrEmpty(irfId)
                {
                    //first check if the irf id is valid
                    bool isValid = true;
                    if (isValid)
                    {
                        return View(new InterviewerDirectApplicationVM { AssessmentGroupId = assessmentGroupId.Value, EncryptedAssessmentGroupId = decodedEncryptedId });

                    }
                    else
                    {
                        return Json("Invalid");
                    }

                }
                else
                {
                    return Json("Bad Request!");
                }

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("Something's wrong. Please try again later.");
            }

        }

        [HttpPost]
        public async Task<IActionResult> MAGApply(InterviewerDirectApplicationVM model)//, int irf
        {
            try
            {

                //first check if this interviewer is already present in the transInterviewer table, it could be either a yes or a no verdict
                //var existenceCheck = await _interviewerService.CheckIfInterviewerExistsInSystemAndOrSkillChannel(model.EmailId, model.IRFId);
                var role = _interviewerService.GetUserRolebyEmailid(model.EmailId);
                if (!string.IsNullOrEmpty(role) && role.ToLower() == "interviewer") //if the variable 'role' is empty here, it means the emailId exists in the system as an Interviewer. For further clarity, check the GetInterviwerExists function above. It is used in a few forms too, like Apply and is operational.
                {
                    if (model.ResumeFile != null)
                    {
                        string uploadedFileURL = string.Empty;
                        var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(model.ResumeFile);
                        if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                                            || fileMimeType == "application/pdf" || fileMimeType == "application/x-java-archive" || fileMimeType == "application/vnd.ms-powerpoint")
                        {
                            var resumeFileName = model.ResumeFile.FileName.Replace(" ", "_");

                            await _azureStorageHelper.InsertBlob(resumeFileName, "resumes", model.ResumeFile);
                            var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                            var blob = containerRef.GetBlockBlobReference(resumeFileName);
                            uploadedFileURL = blob.Uri.ToString();
                            model.ResumeLink = uploadedFileURL;
                        }


                    }

                    int? assessmentGroupId = model.AssessmentGroupId;

                    if (assessmentGroupId.HasValue && assessmentGroupId.Value > 0 && !string.IsNullOrEmpty(model.ResumeLink))//!string.IsNullOrEmpty(irfId)
                    {
                        //model.IRFId = Convert.ToInt32(irfId);//irfId.Value;
                        model.AssessmentGroup = await _irfService.GetAssessmentGroupById(model.AssessmentGroupId);

                        //first screen the interviewer's resume
                        var resumeStringContent = await _pdfReaderHelper.ReadPdfFromUrlAsync(model.ResumeLink);
                        var response = await _assistantServiceHelper.GenerateResumeAnalysis(resumeStringContent, model.AssessmentGroup);

                        bool saveOperationResult = true;
                        var aiAnalysisModel = new InterviewerResumeFitmentAnalysisSchema();
                        if (response != null)
                        {
                            aiAnalysisModel = JsonConvert.DeserializeObject<InterviewerResumeFitmentAnalysisSchema>(response.Data?.First()?.Content?.FirstOrDefault()?.Text?.Value);
                            if (aiAnalysisModel != null)
                            {
                                saveOperationResult = await _interviewerService.AddInternalInterviewerToIRFPostPositiveVerdict(new InterviewerDirectApplicationVM { EmailId = model.EmailId, AssessmentGroupId = model.AssessmentGroupId, OpenAIResponse = aiAnalysisModel.completeFitmentAnalysis, AIVerdict = aiAnalysisModel.verdict, AIAnalysisScore = aiAnalysisModel.fitmentScore, ResumeLink = model.ResumeLink, DirectApplicationFor = DirectApplicationForTypes.MAG });//, irf
                                if (saveOperationResult)
                                {
                                    if (aiAnalysisModel.verdict.ToLower() == "yes")
                                    {
                                        //_notificationHelper.SendUnassignedInterviewsNotifications();

                                        try
                                        {
                                            var _AllInterviewerInterview = await _interviewerService.FetchAllInterviewerallUpcomingInterview("Verified", null, model.AssessmentGroupId);
                                            foreach (var item in _AllInterviewerInterview?.Where(x => x.EmailId == model.EmailId))
                                            {
                                                if (!string.IsNullOrEmpty(item.EmailId))
                                                {
                                                    string sendreporturl = string.Empty;
                                                    if (string.IsNullOrEmpty(item.uniqueid))
                                                    {
                                                        Guid uniqueid = Guid.NewGuid();
                                                        var _uniqueid = await _clientService.PublicInterviewerUrlSave(item.EmailId, uniqueid.ToString());
                                                        sendreporturl = "https://" + Request.Host.ToString() + "/Interview/PublicOpenInterviewInvite?id=" + uniqueid;
                                                        if (!string.IsNullOrEmpty(_uniqueid))
                                                        {
                                                            sendreporturl = "https://" + Request.Host.ToString() + "/Interview/PublicOpenInterviewInvite?id=" + _uniqueid;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        sendreporturl = "https://" + Request.Host.ToString() + "/Interview/PublicOpenInterviewInvite?id=" + item.uniqueid;
                                                    }
                                                    try
                                                    {
                                                        //Email Message Send
                                                        Mail _mail1 = new Mail
                                                        {
                                                            TO = item.EmailId,
                                                            CC = "talent@risebird.io",
                                                            Subject = "New interviews have come up! Please assign."
                                                        };
                                                        var emailsend1 = await _notificationHelper.SendInterviewerCommonInterviewNotification(_mail1, sendreporturl, item.Total, item.uniqueid, agId: model.AssessmentGroupId);
                                                        //What's App Message Send
                                                        //WhatsAppVM _whatsAppVM = new WhatsAppVM();
                                                        //_whatsAppVM.Emailid = item.EmailId;
                                                        //_whatsAppVM.MessageValue = item.Total.ToString();
                                                        //_whatsAppVM.buttonlink = !string.IsNullOrEmpty(sendreporturl) ? sendreporturl.Replace("https://portal.risebird.io/", "") : "";
                                                        //_whatsAppVM.Event = "Interviewer Common Interview";
                                                        //_whatsAppVM.irfId = irfId.HasValue ? irfId.Value : 0;
                                                        //await _whatsAppHelper.WhatsAppSendInterviewerCommonInterviewDetails(_whatsAppVM);
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                                                    }
                                                }

                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);

                                        }
                                        return Json("Your profile matches the requirement. You will be recieving an invitation to assign open interviews(as and when they come up) over mail, shortly. Thankyou!");

                                    }
                                    else if (aiAnalysisModel.verdict.ToLower() == "no")
                                    {
                                        return Json("Your interest has been recorded. We will get back to you shortly. Thankyou!");
                                    }
                                }
                                else
                                {
                                    return Json("Oops! An error occured. Please try again later.");
                                }

                            }
                        }

                    }
                    else
                    {
                        return Json("Unexpected Error! Please try later.");
                    }
                }
                else
                {
                    return Json("Not Applicable");
                }

                return Json("An unexpected error occured. Please try again later.");
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("Something went wrong. Please try again later.");
            }

        }

        [HttpGet]
        public async Task<IActionResult> Applicants()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "SupportAdmin" || profileName == "Finance" || profileName == "SeniorHR" || profileName == "SeniorInterviewerManagement" || profileName == "InterviewerManagement" || profileName == "HR")//|| profileName== "InterviewerManagement"
                    {
                        if (TempData["MessageApplicants"] != null)
                        {
                            ViewBag.Message = TempData["MessageApplicants"].ToString();
                            TempData.Remove("MessageApplicants");

                        }
                        var model = await _interviewerService.GetInterviewerApplicants(emailId);
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
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> FLNInterviewersAcknowledgementStatus(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "SupportAdmin" || profileName == "Finance" || profileName == "SeniorHR" || profileName == "SeniorInterviewerManagement" || profileName == "InterviewerManagement" || profileName == "HR")//|| profileName== "InterviewerManagement"
                    {
                        if (!startDate.HasValue && !endDate.HasValue)
                        {
                            endDate = DateTime.Now.Date;
                            startDate = endDate.Value.AddDays(-7);
                        }
                        
                        var model = await _interviewerService.GetFLNAcknowledgementStatus(emailId, startDate, endDate);
                        ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
                        ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
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
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<FileContentResult> ExportFLNAcknowledgementStatusToExcel(DateTime? startDate = null, DateTime? endDate = null)
        {
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
            ExcelPackage Ep = new ExcelPackage();
            ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("FLN Acknowledgement Status");
            string fileName = "FLN_Acknowledgement_Status_" + DateTime.Now.ToString("ddMMyyyy_HHmmss") + ".xlsx";
            
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    
                    if (profileName == "Admin" || profileName == "SupportAdmin" || profileName == "Finance" || profileName == "SeniorHR" || profileName == "SeniorInterviewerManagement" || profileName == "InterviewerManagement" || profileName == "HR")
                    {
                        if (!startDate.HasValue && !endDate.HasValue)
                        {
                            endDate = DateTime.Now.Date;
                            startDate = endDate.Value.AddDays(-7);
                        }
                        
                        var modelList = await _interviewerService.GetFLNAcknowledgementStatus(emailId, startDate, endDate);
                        
                        if (modelList != null && modelList.Count() > 0)
                        {
                            Sheet.Cells["A1:L1"].Style.Font.Bold = true;
                            Sheet.Cells["A1:L1"].Style.Font.Size = 12;
                            Sheet.Cells["A1:L1"].Style.WrapText = true;
                            
                            for (int a = 1; a <= 12; a++)
                            {
                                Sheet.Column(a).Width = 25;
                            }
                            
                            Sheet.Cells[1, 1].Value = "Email Id";
                            Sheet.Cells[1, 2].Value = "Primary Specialization";
                            Sheet.Cells[1, 3].Value = "AI Verdict";
                            Sheet.Cells[1, 4].Value = "FLN";
                            Sheet.Cells[1, 5].Value = "Added On";
                            Sheet.Cells[1, 6].Value = "Company Name";
                            Sheet.Cells[1, 7].Value = "Resume Link";
                            Sheet.Cells[1, 8].Value = "Mobile Number";
                            Sheet.Cells[1, 9].Value = "Name";
                            Sheet.Cells[1, 10].Value = "Acknowledgement Status";
                            Sheet.Cells[1, 11].Value = "Target Skill Channel";
                            Sheet.Cells[1, 12].Value = "Assessment Group";
                            
                            int i = 2;
                            foreach (var item in modelList)
                            {
                                Sheet.Cells[i, 1].Value = item.EmailId;
                                Sheet.Cells[i, 2].Value = item.PrimarySpecialization;
                                Sheet.Cells[i, 3].Value = item.AIVerdict;
                                Sheet.Cells[i, 4].Value = item.FLN;
                                Sheet.Cells[i, 5].Value = item.AddedOn.ToString("yyyy-MM-dd HH:mm:ss");
                                Sheet.Cells[i, 6].Value = item.CompanyName;
                                Sheet.Cells[i, 7].Value = item.ResumeLink;
                                Sheet.Cells[i, 8].Value = item.MobileNumber;
                                Sheet.Cells[i, 9].Value = item.Name;
                                Sheet.Cells[i, 10].Value = item.AcknowledgementStatus;
                                Sheet.Cells[i, 11].Value = item.TargetSkillChannel;
                                Sheet.Cells[i, 12].Value = item.AssessmentGroup;
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
                        Rng.Value = "Access Denied !!!";
                        return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                    }
                }
                else
                {
                    ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                    Rng.Value = "Session Expired !!!";
                    return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                Rng.Value = "An error occurred while exporting data !!!";
                return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        [HttpGet]
        public async Task<IActionResult> InterviewersOnboarding()
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                ViewBag.Emailid = emailid;
                if (profileName == "Interviewer")
                {
                    return View();
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
        [HttpGet]
        public async Task<IActionResult> InterviewersOnboardingConfirmation(string value)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                ViewBag.Emailid = emailid;
                if (profileName == "Interviewer")
                {
                    var model = await _interviewerService.UpdateInterviewerAcknowledgementStatus(emailid, value);
                    return RedirectToAction("InterviewerIndex", "Dashboard");
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
    }
}

