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
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using X.PagedList;

namespace Risebird.Web.Controllers
{
    [Authorize]
    public class InterviewerSkillsController : Controller
    {
        private IInterviewerSkillsRepository _interviewerSkillsRepository;
        private IInterviewerSkillsService _iInterviewerSkillsService;
        private IEvaluatorService _evaluatorService;
        private IAzureStorageHelper _azureStorageHelper;
        private IMeetingService _meetingService;
        private IApplicationErrorLogger _applicationErrorLogger;
        private readonly IIRFRepository _iRFRepository;
        private readonly INotificationHelper _notificationHelper;
        private static List<int> Shortlistvideoids = new List<int>();
        private IInterviewerService _interviewerService;
        private readonly IUserService _userService;
        public InterviewerSkillsController(IInterviewerSkillsRepository InterviewerSkillsRepository, IApplicationErrorLogger applicationErrorLogger, IInterviewerSkillsService InterviewerSkillsService, IMeetingService meetingService, IAzureStorageHelper azureStorageHelper, IEvaluatorService evaluatorService, IIRFRepository iRFRepository, INotificationHelper notificationHelper, IInterviewerService interviewerService, IUserService userService)
        {
            _interviewerSkillsRepository = InterviewerSkillsRepository;
            _iInterviewerSkillsService = InterviewerSkillsService;
            _applicationErrorLogger = applicationErrorLogger;
            _meetingService = meetingService;
            _azureStorageHelper = azureStorageHelper;
            _evaluatorService = evaluatorService;
            _iRFRepository = iRFRepository;
            _notificationHelper = notificationHelper;
            _interviewerService = interviewerService;
            _userService = userService;
        }

        [Authorize]
        [HttpGet]
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR")
                {
                    var model = new InterviewerSkillsViewModel();
                    var InterviewerSkillsList = _iInterviewerSkillsService.GetAllInterviewerSkillsChannel(profileName, emailid);
                    model.InterviewerSkillsChannelModelList = InterviewerSkillsList.Where(x => x.EndDate.Date > DateTime.Now).OrderByDescending(y => y.EndDate);
                    model.InterviewerSkillsChannelModelList = model.InterviewerSkillsChannelModelList.Select(x => { x.HeuristicProfileType = ((Heuristicsscorelevel)Convert.ToInt32(x.HeuristicProfileType)).GetDescription(); return x; }).ToList();
                    foreach (var item in model.InterviewerSkillsChannelModelList)
                    {
                        if (item.IsPaused)
                        {
                            item.ImageUrl = "/img/Cards_Png/01_Grey-White.png";
                            item.InterviewerSkillStatus = "Paused";
                        }
                        else
                        {
                            item.ImageUrl = "/img/Cards_Png/01_Green-White.png";
                            item.InterviewerSkillStatus = "Active";
                        }
                    }
                    if (TempData["SuccessErrorMessageISCInterviewer"] != null)
                    {
                        model.Message = TempData["SuccessErrorMessageISCInterviewer"].ToString();
                        TempData.Remove("SuccessErrorMessageISCInterviewer");
                    }
                    if (HttpContext.Session.GetString("InterviewerSkillsFilter") != null)
                    {
                        var filter = JsonConvert.DeserializeObject<InterviewerSkillsFilter>(HttpContext.Session.GetString("InterviewerSkillsFilter"));
                        if (filter.StartDate != null && filter.EndDate != null)
                        {
                            model.InterviewerSkillsChannelModelList = InterviewerSkillsList.Where(t => t.StartDate >= filter.StartDate && t.EndDate <= filter.EndDate).ToList();
                        }
                        else
                        {
                            model.InterviewerSkillsChannelModelList = InterviewerSkillsList.Where(x => x.EndDate >= DateTime.Now);
                        }
                        filter.ISID = filter.ISID == null ? filter.ISID = model.InterviewerSkillsChannelModelList.Select(x => x.InterviewerSkillId).ToArray() : filter.ISID = filter.ISID;

                        model.InterviewerSkillsChannelModelList = model.InterviewerSkillsChannelModelList.Where(c => filter.ISID.Contains(c.InterviewerSkillId)).ToList();

                        if (model.InterviewerSkillsChannelModelList.Count() == 0 && filter.StartDate != null && filter.EndDate != null)
                        {
                            model.InterviewerSkillsChannelModelList = InterviewerSkillsList.Where(t => t.StartDate.Date >= filter.StartDate && t.EndDate.Date <= filter.EndDate).ToList();
                        }
                        foreach (var item in model.InterviewerSkillsChannelModelList)
                        {
                            if (item.IsPaused)
                            {
                                item.ImageUrl = "/img/Cards_Png/01_Grey-White.png";
                                item.InterviewerSkillStatus = "Paused";
                            }
                            else if (item.EndDate >= DateTime.Now)
                            {
                                item.ImageUrl = "/img/Cards_Png/01_Green-White.png";
                                item.InterviewerSkillStatus = "Active";
                            }
                            else
                            {
                                item.ImageUrl = "/img/Cards_Png/01_Black-White.png";
                                item.InterviewerSkillStatus = "Archived";
                            }
                        }
                        model.InterviewerSkillsChannelModelList = model.InterviewerSkillsChannelModelList.OrderByDescending(x => x.EndDate).ToList();
                    }

                    model.Profile = profileName;
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

        [Authorize]
        [HttpPost]
        public IActionResult Index(InterviewerSkillsFilter filter)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                if (filter != null)
                {
                    HttpContext.Session.SetString("InterviewerSkillsFilter", JsonConvert.SerializeObject(filter));
                }
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" /*|| profileName == "Client" || profileName == "Recruiter"*/)
                {
                    var model = new InterviewerSkillsViewModel();
                    var InterviewerSkillsList = _iInterviewerSkillsService.GetAllInterviewerSkillsChannel(profileName, emailid);

                    if (filter.StartDate != null && filter.EndDate != null)
                    {
                        InterviewerSkillsList = InterviewerSkillsList.Where(t => t.StartDate.Date >= filter.StartDate && t.EndDate.Date <= filter.EndDate).ToList();
                    }
                    else
                    {
                        InterviewerSkillsList = InterviewerSkillsList.Where(x => x.EndDate.Date > DateTime.Now);
                    }

                    filter.ISID = filter.ISID == null ? filter.ISID = InterviewerSkillsList.Select(x => x.InterviewerSkillId).ToArray() : filter.ISID = filter.ISID;
                    InterviewerSkillsList = InterviewerSkillsList.Where(c => filter.ISID.Contains(c.InterviewerSkillId)).ToList();

                    if (InterviewerSkillsList.Count() == 0 && filter.StartDate != null && filter.EndDate != null)
                        InterviewerSkillsList = _iInterviewerSkillsService.GetAllInterviewerSkillsChannel(profileName, emailid).Where(x => x.StartDate.Date >= filter.StartDate && x.EndDate.Date <= filter.EndDate);
                    foreach (var item in InterviewerSkillsList)
                    {
                        if (item.IsPaused)
                        {
                            item.ImageUrl = "/img/Cards_Png/01_Grey-White.png";
                            item.InterviewerSkillStatus = "Paused";
                        }
                        else if (item.EndDate >= DateTime.Now)
                        {
                            item.ImageUrl = "/img/Cards_Png/01_Green-White.png";
                            item.InterviewerSkillStatus = "Active";
                        }
                        else
                        {
                            item.ImageUrl = "/img/Cards_Png/01_Black-White.png";
                            item.InterviewerSkillStatus = "Archived";
                        }
                    }
                    model.InterviewerSkillsChannelModelList = InterviewerSkillsList.OrderByDescending(x => x.EndDate).ToList();
                    model.InterviewerSkillsChannelModelList = model.InterviewerSkillsChannelModelList.Select(x => { x.HeuristicProfileType = ((Heuristicsscorelevel)Convert.ToInt32(x.HeuristicProfileType)).GetDescription(); return x; }).ToList();
                    model.Profile = profileName;
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

        [Authorize]
        [HttpGet]
        public IActionResult Add()
        {
            if (HttpContext.Session.GetString("email") != null)
            {
                var model = new InterviewerSkillsVM();
                var interviewers = _interviewerSkillsRepository.GetInterviewers();
                ViewBag.RiseBirdHR = new SelectList(_iInterviewerSkillsService.GetRiseBirdHR(), "id", "EmailId");
                ViewBag.Level = new SelectList(_interviewerSkillsRepository.GetLevels(), "id", "Name");
                ViewBag.Interviewers = new SelectList(interviewers, "id", "EmailId");
                ViewBag.LevelTech = new SelectList(_interviewerSkillsRepository.GetLevelTech(), "id", "Name");
                //  ViewBag.Skill = new SelectList(_interviewerSkillsRepository.GetSkills(), "id", "Name");

                var Skillset = _interviewerSkillsRepository.GetSkills();
                if (Skillset != null)
                {
                    Skillset = Skillset.OrderBy(x => x.Name.Trim()).ToList();
                }
                ViewBag.Skill = new SelectList(Skillset, "id", "Name");
                var AssessmentsGroups = _interviewerService.FetchAssessmentGroups();
                ViewBag.AssessmentGroupList = new SelectList(AssessmentsGroups, "id", "AssessmentGroup");
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
        public async Task<IActionResult> AddInterviewerSkills(InterviewerSkillsVM InterviewerSkillsDetails)
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
                        if (InterviewerSkillsDetails.JDFile != null)
                        {
                            var fileName = InterviewerSkillsDetails.JDFile.FileName.Replace(" ", "_");

                            await _azureStorageHelper.InsertBlob(fileName, "resumes", InterviewerSkillsDetails.JDFile);
                            var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                            var blob = containerRef.GetBlockBlobReference(fileName);
                            uploadedFileURL = blob.Uri.ToString();
                            InterviewerSkillsDetails.JDLink = uploadedFileURL;
                        }
                        if (_iInterviewerSkillsService.AddInterviewerSkillsDetails(InterviewerSkillsDetails).Result)
                        {
                            TempData["SuccessErrorMessageISCInterviewer"] = "Interviewer Skills Channel is added successfully!";
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            TempData["SuccessErrorMessageISCInterviewer"] = "Adding Interviewer Skills Channel has failed!";
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
                TempData["SuccessErrorMessageISCInterviewer"] = "Adding Interviewer Skills has failed";
                return RedirectToAction("Index");
            }
        }


        [Authorize]
        [HttpGet]
        public IActionResult EditInterviewerSkills(string id, bool isClone = false)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null)
                {
                    var model = _iInterviewerSkillsService.GetEditInterviewerSkills(Convert.ToInt16(id)).Result;
                    var AssessmentsGroups = _interviewerService.FetchAssessmentGroups();
                    //string skills = string.Empty;
                    //skills = string.Join(",", from item in model.SkillCollection select item.SkillId);
                    //var interviewers = _iInterviewerSkillsService.GetInterviewers(model.PrimarySkill);
                    if (!String.IsNullOrEmpty(model.AssessmentGroupId))
                    {
                        var interviewers = _iInterviewerSkillsService.GetInterviewersByAssessmentGroup(model.AssessmentGroupId);
                        ViewBag.Interviewers = new SelectList(interviewers, "UserId", "EmailId");
                    }
                    else
                    {
                        var interviewers = new List<SelectListItem>();
                        interviewers.Add(new SelectListItem { Text = "No Interviewer", Value = "" });
                        ViewBag.Interviewers = new SelectList(interviewers, "UserId", "EmailId");
                    }
                    ViewBag.RiseBirdHR = new SelectList(_iInterviewerSkillsService.GetRiseBirdHR(), "id", "EmailId");
                    ViewBag.Level = new SelectList(_interviewerSkillsRepository.GetLevels(), "id", "Name");

                    ViewBag.LevelTech = new SelectList(_interviewerSkillsRepository.GetLevelTech(), "id", "Name");
                    // ViewBag.Skill = new SelectList(_interviewerSkillsRepository.GetSkills(), "id", "Name");
                    var Skillset = _interviewerSkillsRepository.GetSkills();
                    if (Skillset != null)
                    {
                        Skillset = Skillset.OrderBy(x => x.Name.Trim()).ToList();
                    }
                    ViewBag.Skill = new SelectList(Skillset, "id", "Name");

                    ViewBag.AssessmentGroupList = new SelectList(AssessmentsGroups, "id", "AssessmentGroup");
                    TempData["EditInterviewerSkillId"] = model.InterviewerSkillId;
                    TempData["InterviewerJDLink"] = model.JDLink;
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    model.LoggedEmail = emailid;
                    model.IsClone = isClone;
                    return View(model);
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
        [HttpPost]
        public async Task<IActionResult> EditInterviewerSkills(InterviewerSkillsVM model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR")
                    {

                        if ((model.JDFile != null))
                        {
                            var fileName = model.JDFile.FileName.Replace(" ", "_");
                            await _azureStorageHelper.InsertBlob(fileName, "resumes", model.JDFile);
                            var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                            var blob = containerRef.GetBlockBlobReference(fileName);
                            string uploadedFileURL = blob.Uri.ToString();
                            if (uploadedFileURL == null)
                            {
                                TempData["SuccessErrorMessageISCInterviewer"] = "Could not upload JD file.";
                                return RedirectToAction("Index");

                            }

                            model.JDLink = uploadedFileURL;
                        }
                        else if (model.JDLink == null || model.JDLink == "")
                        {
                            model.JDLink = TempData["InterviewerJDLink"]?.ToString();
                        }
                        if (TempData["EditInterviewerSkillId"] != null && (TempData["EditInterviewerSkillId"].ToString() != model.InterviewerSkillId))
                        {
                            if (!(_iInterviewerSkillsService.CheckUniqueness(model.InterviewerSkillId)))
                            {
                                TempData["SuccessErrorMessageISCInterviewer"] = "Sorry! An Interviewer Skills Channel with this name already exits!";
                                return RedirectToAction("Index");
                            }
                            if (_iInterviewerSkillsService.AddInterviewerSkillsDetails(model).Result)
                            {
                                TempData["SuccessErrorMessageISCInterviewer"] = "Interviewer Skills Channel saved Successfully";
                                return RedirectToAction("Index");
                            }
                            else
                            {
                                TempData["SuccessErrorMessageISCInterviewer"] = "Interviewer Skills Channel dits were not saved!";
                                return RedirectToAction("Index");
                            }

                        }
                        else
                        {
                            if (_iInterviewerSkillsService.UpdateInterviewerSkills(model))
                            {
                                TempData["SuccessErrorMessageISCInterviewer"] = "Interviewer Skills Channel saved successfully!";
                                return RedirectToAction("Index");
                            }
                            else
                            {
                                TempData["SuccessErrorMessageISCInterviewer"] = "Interviewer Skills Channel edits were not saved!";
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
                TempData["SuccessErrorMessageISCInterviewer"] = "Interviewer Skills Channel edits were not saved";
                return RedirectToAction("Index");
            }
        }


        [Authorize]
        public string GetInterviewerSkills(int id)
        {
            var name = _iInterviewerSkillsService.GetInterviewerSkills(id);
            return name;
        }
        [Authorize]
        public SelectList GetInterviewers(string skills)
        {

            var list = new List<SelectListItem>();
            var interviewers = _iInterviewerSkillsService.GetInterviewers(skills);

            if (interviewers != null && interviewers.Count() > 0)
            {
                return new SelectList(interviewers, "UserId", "EmailId");
            }
            else
                list.Add(new SelectListItem { Text = "No Interviewer", Value = "" });
            //clientUsers.ToList().Add(new Users { id = 0, FirstName = "No Client User" });
            return new SelectList(list, "Value", "Text");
        }

        [Authorize]
        public bool CheckUniqueness(string value)
        {
            return _iInterviewerSkillsService.CheckUniqueness(value);
        }


        [Authorize]
        public IActionResult DeleteInterviewerSkills(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR")
                    {
                        if (_iInterviewerSkillsService.DeleteInterviewerSkillsChannel(id).Result)
                        {
                            return RedirectToAction("Index");
                        }
                        else
                        {
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
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }

        }
        [Authorize]
        public IActionResult UnArchiveISC(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR")
                    {
                        if (_iInterviewerSkillsService.UnArchiveISC(id).Result)
                        {
                            TempData["SuccessErrorMessageISCInterviewer"] = "Interviewer Skills Channel restored Successfully.";
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            TempData["SuccessErrorMessageISCInterviewer"] = "Interviewer Skills Channel restore Failed.";
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
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessageISCInterviewer"] = "Interviewer Skills Channel restore Failed.";
                return RedirectToAction("Index");
            }

        }
        [Authorize]
        [HttpGet]
        public IActionResult GetISCInterviewers(int id, string InterviewerSkillidName)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" /*|| profileName == "Client" || profileName == "Recruiter"*/)
                    {
                        ViewBag.ISCName = InterviewerSkillidName;
                        ViewBag.ISCId = id;
                        ViewBag.Profile = profileName;
                        ViewBag.CurrentUrl = HttpContext.Request.Path + HttpContext.Request.QueryString;
                        var interviewerList = _iInterviewerSkillsService.GetISCInterviewers(id)?.ToList();


                        if (TempData["SuccessErrorMessageISCInterviewerList"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageISCInterviewerList"].ToString();
                            TempData.Remove("SuccessErrorMessageISCInterviewerList");
                        }
                        return View(interviewerList);

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
        public IActionResult DeleteISCInterviewer(int id, int iscId, string iscName)
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
                        if (_iInterviewerSkillsService.DeleteISCInterviewer(id).Result)
                        {
                            //TempData["SuccessErrorMessageISCInterviewer"] = "Interviewer removed Successfully.";
                            return RedirectToAction("GetISCInterviewers", new { id = iscId, InterviewerSkillidName = iscName });
                        }
                        else
                        {
                            //TempData["SuccessErrorMessageISCInterviewer"] = "Could not remove Interviewer.";
                            return RedirectToAction("GetISCInterviewers", new { id = iscId, InterviewerSkillidName = iscName });
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
                TempData["SuccessErrorMessageISCInterviewer"] = "Could not remove Interviewer.";
                return RedirectToAction("GetISCInterviewers", new { id = iscId, InterviewerSkillidName = iscName });
            }

        }

        public async Task<IActionResult> ResetInterviewerSkillsFilter()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("InterviewerSkillsFilter");
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
        public IActionResult SelectUnApprovedInterviewers(int iscId, string iscName)
        {

            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR")
                {
                    var model = new UnapprovedInterviewersCollectionVM();
                    model.UnApprovedInterviewersCollection = (IList<UnApprovedInterviewer>)_iInterviewerSkillsService.GetUnApprovedInterviewerList(iscId);
                    model.ISCId = iscId;
                    model.ISCName = iscName;
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

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SelectUnApprovedInterviewers(UnapprovedInterviewersCollectionVM model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR")
                    {

                        if (_iInterviewerSkillsService.AddUnApprovedInterviewer(model.UnApprovedInterviewersCollection, model.ISCId))
                        {
                            TempData["SuccessErrorMessageISCInterviewerList"] = "Interviewer added Successfully";
                            return RedirectToAction("GetISCInterviewers", new { id = model.ISCId, InterviewerSkillidName = model.ISCName });
                        }
                        else
                        {
                            TempData["SuccessErrorMessageISCInterviewerList"] = "Interviewer could not be added.";
                            return RedirectToAction("GetISCInterviewers", new { id = model.ISCId, InterviewerSkillidName = model.ISCName });
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
                TempData["SuccessErrorMessageISCInterviewerList"] = "Interviewer could not be added.";
                return RedirectToAction("GetISCInterviewers", new { id = model.ISCId, InterviewerSkillidName = model.ISCName });
            }
        }


        public IActionResult ShowCalendly(string InterviewerSkillName, string calendlyLink)
        {
            ViewBag.IRFName = InterviewerSkillName;
            ViewBag.CalendlyLink = calendlyLink;
            return View();
        }
        [Authorize]
        public IActionResult PauseInterviewerSkills(int InterviewerSkillId, string InterviewerSkillName)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR")
                    {
                        if (_iInterviewerSkillsService.PauseInterviewerSkills(InterviewerSkillId, InterviewerSkillName))
                        {
                            return RedirectToAction("Index");
                        }
                        else
                        {
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
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }

        }
        [Authorize]
        public IActionResult PlayInterviewerSkills(int InterviewerSkillId, string InterviewerSkillName)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR")
                    {
                        if (_iInterviewerSkillsService.PlayInterviewerSkills(InterviewerSkillId, InterviewerSkillName))
                        {
                            return RedirectToAction("Index");
                        }
                        else
                        {
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
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        [HttpGet]
        public IActionResult SendInvite(int iscId, string iscName, string emailId, DateTime endDate, int interviewerProfileId)
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
                        if (endDate < DateTime.Now)
                        {
                            TempData["SuccessErrorMessageISCInterviewerList"] = "Sorry! Could not send invite! The requested ISC has been terminated.";
                            return RedirectToAction("GetISCInterviewers", new { id = iscId, InterviewerSkillidName = iscName });
                        }
                        if (_iInterviewerSkillsService.SendInvite(iscId, emailId, interviewerProfileId))
                        {
                            TempData["SuccessErrorMessageISCInterviewerList"] = "Invitation sent successfully!";
                            return RedirectToAction("GetISCInterviewers", new { id = iscId, InterviewerSkillidName = iscName });
                        }
                        else
                        {
                            TempData["SuccessErrorMessageISCInterviewerList"] = "Sorry! Could not send invite!";
                            return RedirectToAction("GetISCInterviewers", new { id = iscId, InterviewerSkillidName = iscName });
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
                TempData["SuccessErrorMessageISCInterviewerList"] = "Sorry! Could not send invite!";
                return RedirectToAction("GetISCInterviewers", new { id = iscId, InterviewerSkillidName = iscName });
            }

        }
        [Authorize]
        [HttpGet]
        public IActionResult CancelInvite(int iscId, string iscName, string emailId)
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

                        if (_iInterviewerSkillsService.CancelInvite(iscId, emailId))
                        {
                            TempData["SuccessErrorMessageISCInterviewerList"] = "Invite cancelled successfully!";
                            return RedirectToAction("GetISCInterviewers", new { id = iscId, InterviewerSkillidName = iscName });
                        }
                        else
                        {
                            TempData["SuccessErrorMessageISCInterviewerList"] = "Sorry! Could not cancel invite!";
                            return RedirectToAction("GetISCInterviewers", new { id = iscId, InterviewerSkillidName = iscName });
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
                TempData["SuccessErrorMessageCandidate"] = "Sorry! Could not send invite!";
                return RedirectToAction("GetISCInterviewers", new { id = iscId, InterviewerSkillidName = iscName });
            }

        }
        public IActionResult InterviewerSearch(int? Page, string searchvalue)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "CuratedUser")
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        if (TempData["SuccessErrorMessageInterview"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageInterview"].ToString();
                            TempData.Remove("SuccessErrorMessageInterview");
                        }
                        var modelList = new List<MeetingVM>();
                        if (Page == null || Page == 0)
                        {
                            Page = 1;
                        }
                        int totalMeetings = 0;

                        modelList = _iInterviewerSkillsService.GetInterviewerRiseTubeMeetingsPaged(Page.Value, searchvalue).Result;
                        modelList = modelList.Select(x => { x.ProfileType = ((Heuristicsscorelevel)Convert.ToInt32(x.ProfileType)).GetDescription(); x.CompentencyLevel = ((CompetencyLevel)Convert.ToInt32(x.CompentencyLevel)).GetDescription(); return x; }).ToList();
                        if (HttpContext.Session.GetString("InterviewerRiseTubefilters") != null)
                        {
                            var filter = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("InterviewerRiseTubefilters"));
                            if (filter.StartDate != null && filter.EndDate != null)
                            {
                                modelList = modelList.Where(t => t.interviewDate.Date >= filter.StartDate && t.interviewDate.Date <= filter.EndDate).ToList();
                            }
                            filter.AssessmentGroup = filter.AssessmentGroup == null ? filter.AssessmentGroup = modelList.Select(x => x.AssessmentGroup).ToArray() : filter.AssessmentGroup = filter.AssessmentGroup;
                            filter.Senioritylevel = filter.Senioritylevel == null ? filter.Senioritylevel = modelList.Select(x => x.Senioritylevel).ToArray() : filter.Senioritylevel = filter.Senioritylevel;
                            filter.ProfileType = filter.ProfileType == null ? filter.ProfileType = modelList.Select(x => x.ProfileType).ToArray() : filter.ProfileType = filter.ProfileType;
                            filter.CompentencyLevel = filter.CompentencyLevel == null ? filter.CompentencyLevel = modelList.Select(x => x.CompentencyLevel).ToArray() : filter.CompentencyLevel = filter.CompentencyLevel;
                            filter.IndianRate = filter.IndianRate == null ? filter.IndianRate = modelList.Select(x => x.PerHourRateIndia).ToArray() : filter.IndianRate = filter.IndianRate;
                            filter.OutSideIndiaRate = filter.OutSideIndiaRate == null ? filter.OutSideIndiaRate = modelList.Select(x => x.PerHourRateOutside).ToArray() : filter.OutSideIndiaRate = filter.OutSideIndiaRate;
                            filter.CompanyType = filter.CompanyType == null ? filter.CompanyType = modelList.Select(x => x.CompanyType).ToArray() : filter.CompanyType = filter.CompanyType;
                            filter.Domain = filter.Domain == null ? filter.Domain = modelList.Select(x => x.Domain).ToArray() : filter.Domain = filter.Domain;

                            modelList = modelList.Where(c =>
                               (filter.AssessmentGroup.Count() > 0 ? filter.AssessmentGroup.Contains(c.AssessmentGroup) : c.AssessmentGroup == null)
                              && (filter.Senioritylevel.Count() > 0 ? filter.Senioritylevel.Contains(c.Senioritylevel) : c.Senioritylevel == null)
                              && (filter.ProfileType.Count() > 0 ? filter.ProfileType.Contains(c.ProfileType) : c.ProfileType == null)
                              && (filter.CompentencyLevel.Count() > 0 ? filter.CompentencyLevel.Contains(c.CompentencyLevel) : c.CompentencyLevel == null)
                               && (filter.IndianRate.Count() > 0 ? filter.IndianRate.Contains(c.PerHourRateIndia) : c.PerHourRateIndia == null)
                          && (filter.OutSideIndiaRate.Count() > 0 ? filter.OutSideIndiaRate.Contains(c.PerHourRateOutside) : c.PerHourRateOutside == null)
                          && (filter.CompanyType.Count() > 0 ? filter.CompanyType.Contains(c.CompanyType) : c.CompanyType == null)
                          && (filter.Domain.Count() > 0 ? filter.Domain.Contains(c.Domain) : c.Domain == null)).ToList();

                        }
                        //totalMeetings = _iInterviewerSkillsService.GetInterviewerRiseTubeMeetingsCount(searchvalue).Result;

                        var Senioritylevel = _iRFRepository.GetSenioritylevel();
                        foreach (var item in modelList)
                        {
                            if (!String.IsNullOrEmpty(item.Senioritylevel))
                            {
                                int Seniorityid = Convert.ToInt32(item.Senioritylevel);
                                item.Senioritylevel = Senioritylevel.Where(x => x.Id == Seniorityid).FirstOrDefault().Name;
                            }
                        }
                        ViewBag.Senioritylevel = modelList.Select(x => x.Senioritylevel).Distinct();
                        ViewBag.ProfileType = modelList.Where(x => x.ProfileType != "0").Select(x => x.ProfileType).Distinct();
                        ViewBag.CompentencyLevel = modelList.Where(x => x.CompentencyLevel != "0").Select(x => x.CompentencyLevel).Distinct();
                        ViewBag.AssessmentGroup = modelList.Select(x => x.AssessmentGroup).Distinct();
                        ViewBag.IndianRate = modelList.Select(x => x.PerHourRateIndia).Distinct();
                        ViewBag.OutSideIndiaRate = modelList.Select(x => x.PerHourRateOutside).Distinct();
                        ViewBag.CompanyType = modelList.Select(x => x.CompanyType).Distinct();
                        ViewBag.Domain = modelList.Select(x => x.Domain).Distinct();

                        modelList = modelList.Take(12).ToList();
                        totalMeetings = modelList.Count();



                        ViewBag.ProfileName = profileName;

                        if (!String.IsNullOrEmpty(searchvalue))
                        {
                            ViewBag.searchvalue = searchvalue;
                        }
                        else
                        {
                            ViewBag.searchvalue = "";
                        }


                        var remainder = totalMeetings % 12;
                        if (remainder == 0)
                        {
                            ViewBag.Pages = totalMeetings / 12;

                        }
                        else
                        {
                            ViewBag.Pages = (totalMeetings / 12) + 1;

                        }

                        ViewBag.Page = Page.Value;
                        ViewBag.MeetingsCount = totalMeetings;
                        ViewBag.Remainder = remainder;
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
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        [HttpPost]
        public IActionResult InterviewerSearch(int? Page, InterviewFilter filter, string searchvalue)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "CuratedUser")
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        if (TempData["SuccessErrorMessageInterview"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageInterview"].ToString();
                            TempData.Remove("SuccessErrorMessageInterview");
                        }
                        var modelList = new List<MeetingVM>();
                        if (Page == null || Page == 0)
                        {
                            Page = 1;
                        }
                        int totalMeetings = 0;
                        if (filter != null)
                        {
                            HttpContext.Session.SetString("InterviewerRiseTubefilters", JsonConvert.SerializeObject(filter));
                        }
                        modelList = _iInterviewerSkillsService.GetInterviewerRiseTubeMeetingsPaged(Page.Value, searchvalue).Result;
                        modelList = modelList.Select(x => { x.ProfileType = ((Heuristicsscorelevel)Convert.ToInt32(x.ProfileType)).GetDescription(); x.CompentencyLevel = ((CompetencyLevel)Convert.ToInt32(x.CompentencyLevel)).GetDescription(); return x; }).ToList();
                        if (filter.StartDate != null && filter.EndDate != null)
                        {
                            modelList = modelList.Where(t => t.interviewDate.Date >= filter.StartDate && t.interviewDate.Date <= filter.EndDate).ToList();
                        }
                        filter.AssessmentGroup = filter.AssessmentGroup == null ? filter.AssessmentGroup = modelList.Select(x => x.AssessmentGroup).ToArray() : filter.AssessmentGroup = filter.AssessmentGroup;
                        filter.Senioritylevel = filter.Senioritylevel == null ? filter.Senioritylevel = modelList.Select(x => x.Senioritylevel).ToArray() : filter.Senioritylevel = filter.Senioritylevel;
                        filter.ProfileType = filter.ProfileType == null ? filter.ProfileType = modelList.Select(x => x.ProfileType).ToArray() : filter.ProfileType = filter.ProfileType;
                        filter.CompentencyLevel = filter.CompentencyLevel == null ? filter.CompentencyLevel = modelList.Select(x => x.CompentencyLevel).ToArray() : filter.CompentencyLevel = filter.CompentencyLevel;
                        filter.IndianRate = filter.IndianRate == null ? filter.IndianRate = modelList.Select(x => x.PerHourRateIndia).ToArray() : filter.IndianRate = filter.IndianRate;
                        filter.OutSideIndiaRate = filter.OutSideIndiaRate == null ? filter.OutSideIndiaRate = modelList.Select(x => x.PerHourRateOutside).ToArray() : filter.OutSideIndiaRate = filter.OutSideIndiaRate;
                        filter.CompanyType = filter.CompanyType == null ? filter.CompanyType = modelList.Select(x => x.CompanyType).ToArray() : filter.CompanyType = filter.CompanyType;
                        filter.Domain = filter.Domain == null ? filter.Domain = modelList.Select(x => x.Domain).ToArray() : filter.Domain = filter.Domain;

                        modelList = modelList.Where(c =>
                           (filter.AssessmentGroup.Count() > 0 ? filter.AssessmentGroup.Contains(c.AssessmentGroup) : c.AssessmentGroup == null)
                          && (filter.Senioritylevel.Count() > 0 ? filter.Senioritylevel.Contains(c.Senioritylevel) : c.Senioritylevel == null)
                          && (filter.ProfileType.Count() > 0 ? filter.ProfileType.Contains(c.ProfileType) : c.ProfileType == null)
                          && (filter.CompentencyLevel.Count() > 0 ? filter.CompentencyLevel.Contains(c.CompentencyLevel) : c.CompentencyLevel == null)
                          && (filter.IndianRate.Count() > 0 ? filter.IndianRate.Contains(c.PerHourRateIndia) : c.PerHourRateIndia == null)
                          && (filter.OutSideIndiaRate.Count() > 0 ? filter.OutSideIndiaRate.Contains(c.PerHourRateOutside) : c.PerHourRateOutside == null)
                           && (filter.CompanyType.Count() > 0 ? filter.CompanyType.Contains(c.CompanyType) : c.CompanyType == null)
                            && (filter.Domain.Count() > 0 ? filter.Domain.Contains(c.Domain) : c.Domain == null)).ToList();

                        //totalMeetings = _iInterviewerSkillsService.GetInterviewerRiseTubeMeetingsCount(searchvalue).Result;
                        var Senioritylevel = _iRFRepository.GetSenioritylevel();
                        foreach (var item in modelList)
                        {
                            if (!String.IsNullOrEmpty(item.Senioritylevel))
                            {
                                int Seniorityid = Convert.ToInt32(item.Senioritylevel);
                                item.Senioritylevel = Senioritylevel.Where(x => x.Id == Seniorityid).FirstOrDefault().Name;
                            }
                        }
                        ViewBag.Senioritylevel = modelList.Select(x => x.Senioritylevel).Distinct();
                        ViewBag.ProfileType = modelList.Where(x => x.ProfileType != "0").Select(x => x.ProfileType).Distinct();
                        ViewBag.CompentencyLevel = modelList.Where(x => x.CompentencyLevel != "0").Select(x => x.CompentencyLevel).Distinct();
                        ViewBag.AssessmentGroup = modelList.Select(x => x.AssessmentGroup).Distinct();
                        ViewBag.IndianRate = modelList.Select(x => x.PerHourRateIndia).Distinct();
                        ViewBag.OutSideIndiaRate = modelList.Select(x => x.PerHourRateOutside).Distinct();
                        ViewBag.CompanyType = modelList.Select(x => x.CompanyType).Distinct();
                        ViewBag.Domain = modelList.Select(x => x.Domain).Distinct();

                        modelList = modelList.Take(12).ToList();
                        totalMeetings = modelList.Count();


                        ViewBag.ProfileName = profileName;
                        if (!String.IsNullOrEmpty(searchvalue))
                        {
                            ViewBag.searchvalue = searchvalue;
                        }
                        else
                        {
                            ViewBag.searchvalue = "";
                        }


                        var remainder = totalMeetings % 12;
                        if (remainder == 0)
                        {
                            ViewBag.Pages = totalMeetings / 12;

                        }
                        else
                        {
                            ViewBag.Pages = (totalMeetings / 12) + 1;

                        }

                        ViewBag.Page = Page.Value;
                        ViewBag.MeetingsCount = totalMeetings;
                        ViewBag.Remainder = remainder;
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
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<IActionResult> ResetInterviewerRisetubeFilter()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("InterviewerRiseTubefilters");
                    return RedirectToAction("InterviewerSearch");
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
        [AllowAnonymous]
        public async Task<IActionResult> InterviewerVideoPage(int id, string irfid, string actionstatus)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    ViewBag.actionstatus = "Risetube";
                    ViewBag.ResultStatus = "VideoPresent";
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "CuratedUser" || profileName == "ExternalUser")
                    {
                        var emailId = HttpContext.Session.GetString("email")?.Trim();
                        emailId = emailId.Replace("\"", "");
                        ViewBag.Emailaddress = emailId;
                        if (TempData["SuccessErrorMessageInterview"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageInterview"].ToString();
                            TempData.Remove("SuccessErrorMessageInterview");
                        }
                        var modelList1 = new List<MeetingVM>();
                        MeetingVM model = new MeetingVM();
                        model = await _iInterviewerSkillsService.GetMeetingbyid(id, emailId);
                        if (model == null)
                        {
                            model = new MeetingVM();
                            ViewBag.ResultStatus = "VideoNotPresent";
                            return View(model);
                        }
                        modelList1 = await _iInterviewerSkillsService.GetInterviewerRiseTubeMeetingsByJD(0, emailId, model.JDTiltle.Split("|")[0]);
                        modelList1 = modelList1.Select(x =>
                        {
                            x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty;
                            return x;
                        }).ToList();
                        var IRListModel = _interviewerService.GetInterviewerinteviewReport(model.imid, model.irfIntId).Result;
                        var exprienceyears = _iRFRepository.GetInterviewerExprienceYear();
                        var CompanyStageList = _iRFRepository.GetCompanyStage();
                        var SkillNatureList = _iRFRepository.GetSkillNature();

                        if (IRListModel != null)
                        {
                            if (String.IsNullOrEmpty(IRListModel.ProfileType))
                            {
                                IRListModel.ProfileType = "";
                            }
                            else
                            {
                                IRListModel.ProfileType = ((Heuristicsscorelevel)Convert.ToInt32(IRListModel.ProfileType)).GetDescription();
                            }
                            if (String.IsNullOrEmpty(IRListModel.CompentencyLevel))
                            {
                                IRListModel.CompentencyLevel = "";
                            }
                            else
                            {
                                IRListModel.CompentencyLevel = ((CompetencyLevel)Convert.ToInt32(IRListModel.CompentencyLevel)).GetDescription();
                            }
                            IRListModel.Country = ((WorkCountry)Convert.ToInt32(IRListModel.Country)).GetDescription();
                            int SkillNature = Convert.ToInt32(IRListModel.SkillNature);
                            var Skill_Nature = SkillNatureList.Where(x => x.Id == SkillNature).FirstOrDefault();
                            if (Skill_Nature != null)
                                IRListModel.SkillNature = Skill_Nature.Description;
                            int CompanyStage = Convert.ToInt32(IRListModel.CompanyStage);
                            var Company_Stage = CompanyStageList.Where(x => x.Id == CompanyStage).FirstOrDefault();
                            if (Company_Stage != null)
                                IRListModel.CompanyStage = Company_Stage.Description;
                            int expreinece_year = Convert.ToInt32(IRListModel.Yearsofexperience);
                            var years_expreince = exprienceyears.Where(x => x.Id == expreinece_year).FirstOrDefault();
                            if (years_expreince != null)
                                IRListModel.Yearsofexperience = years_expreince.Description;
                        }
                        var modelList = modelList1;

                        MeetingVM NewModel = new MeetingVM();
                        NewModel.SubVideo_List = modelList1.ToList().Where(x => x.imid != id).ToList();

                        NewModel.candidateName = model.candidateName;
                        NewModel.AssessmentGroup = model.AssessmentGroup;
                        ViewBag.ProfileName = profileName;
                        NewModel.CandidateId = model.CandidateId;
                        NewModel.interviewDate = model.interviewDate;
                        NewModel.imid = model.imid;
                        NewModel.IRFId = model.IRFId;
                        NewModel.ClientName = model.ClientName;
                        NewModel.InterviewerComment = model.InterviewerComment;
                        NewModel.InterviewStatus = model.InterviewStatus;
                        NewModel.JDTiltle = model.JDTiltle;
                        NewModel.JDLink = model.JDLink;
                        NewModel.MeetingStatus = model.MeetingStatus;
                        NewModel.confirmationStatus = model.confirmationStatus;
                        NewModel.VideoLink = model.VideoLink;
                        NewModel.candidateEmail = model.candidateEmail;
                        NewModel.reportLink = model.reportLink;
                        NewModel.resumeLink = model.resumeLink;

                        NewModel.zoomLink = model.zoomLink;
                        var Senioritylevel = _iRFRepository.GetSenioritylevel();
                        var SkillCategoryList = _iRFRepository.GetSkillCategory();

                        if (IRListModel != null)
                        {
                            int Seniorityid = Convert.ToInt32(IRListModel.Senioritylevel);
                            if (!String.IsNullOrEmpty(IRListModel.SkillCategory))
                            {
                                var SkillCategorys = IRListModel.SkillCategory.Split(",");
                                var Skillset = "";
                                foreach (var item in SkillCategorys)
                                {
                                    int SkillCategoryid = Convert.ToInt32(item);
                                    if (!String.IsNullOrEmpty(Skillset))
                                    {
                                        Skillset = Skillset + "," + SkillCategoryList.Where(x => x.Id == SkillCategoryid).FirstOrDefault().Name;
                                    }
                                    else
                                    {
                                        Skillset = SkillCategoryList.Where(x => x.Id == SkillCategoryid).FirstOrDefault().Name;
                                    }
                                }
                                IRListModel.SkillCategory = Skillset;
                                var Seniority_level = Senioritylevel.Where(x => x.Id == Seniorityid).FirstOrDefault();
                                if ((Seniority_level != null))
                                    IRListModel.Senioritylevel = Seniority_level.Name;

                            }
                        }
                        NewModel.IreportModel = IRListModel ?? new InterviewReport();
                        NewModel.ShortListCount = model.ShortListCount;
                        NewModel.IsShortlisted = model.IsShortlisted;
                        NewModel.ThumbnailImage = model.ThumbnailImage;
                        NewModel.IsOnline = model.IsOnline;
                        NewModel.candidateEmail = model.candidateEmail;
                        NewModel.ClientHrContact = model.ClientHrContact;
                        // to get videocount details
                        NewModel.FeedbackAudioLink = model.FeedbackAudioLink;
                        NewModel.IsBiometricEnabled = model.IsBiometricEnabled;
                        NewModel.MeetingPatch = model.MeetingPatch;
                        if (model.MeetingPatch.Count() > 0)
                        {
                            NewModel.VideoLink = model.MeetingPatch.FirstOrDefault().VideoLink;
                        }
                        if (NewModel.IreportModel.InterviewerQuestionsCollection == null)
                        {
                            ViewBag.QuestionsDiscussedCount = 0;
                        }
                        else
                        {
                            ViewBag.QuestionsDiscussedCount = NewModel.IreportModel.InterviewerQuestionsCollection?.Count();
                        }
                        var comments = _meetingService.GetvedioComments(id);
                        NewModel.Comments = comments;
                        bool isQC = false;
                        NewModel.IreportModel.IsQC = isQC;
                        NewModel.irfIntId = model.irfIntId;
                        if (IRListModel != null)
                        {
                            GetStatusList(IRListModel, model.irfIntId);
                        }

                        ViewBag.VideoStatus = "Private";
                        ViewBag.Shortlist = "false";
                        if (Shortlistvideoids.Contains(id))
                        {
                            ViewBag.Shortlist = "true";
                        }
                        return View(NewModel);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    ViewBag.actionstatus = "Risetube";
                    ViewBag.ResultStatus = "VideoPresent";
                    ViewBag.Emailaddress = "";
                    if (TempData["SuccessErrorMessageInterview"] != null)
                    {
                        ViewBag.Message = TempData["SuccessErrorMessageInterview"].ToString();
                        TempData.Remove("SuccessErrorMessageInterview");
                    }
                    var modelList1 = new List<MeetingVM>();
                    MeetingVM model = new MeetingVM();
                    model = await _iInterviewerSkillsService.GetMeetingbyid(id, "");
                    if (model == null)
                    {
                        model = new MeetingVM();
                        ViewBag.ResultStatus = "VideoNotPresent";
                        return View(model);
                    }
                    modelList1 = await _iInterviewerSkillsService.GetInterviewerRiseTubeMeetingsByJD(0, "", model.JDTiltle.Split("|")[0]);

                    modelList1 = modelList1.Select(x =>
                    {
                        x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty;
                        return x;
                    }).ToList();
                    var IRListModel = _interviewerService.GetInterviewerinteviewReport(model.imid, model.irfIntId).Result;
                    var exprienceyears = _iRFRepository.GetInterviewerExprienceYear();
                    var CompanyStageList = _iRFRepository.GetCompanyStage();
                    var SkillNatureList = _iRFRepository.GetSkillNature();
                    if (IRListModel != null)
                    {
                        if (String.IsNullOrEmpty(IRListModel.ProfileType))
                        {
                            IRListModel.ProfileType = "";
                        }
                        else
                        {
                            IRListModel.ProfileType = ((Heuristicsscorelevel)Convert.ToInt32(IRListModel.ProfileType)).GetDescription();
                        }
                        if (String.IsNullOrEmpty(IRListModel.CompentencyLevel))
                        {
                            IRListModel.CompentencyLevel = "";
                        }
                        else
                        {
                            IRListModel.CompentencyLevel = ((CompetencyLevel)Convert.ToInt32(IRListModel.CompentencyLevel)).GetDescription();
                        }
                        IRListModel.Country = ((WorkCountry)Convert.ToInt32(IRListModel.Country)).GetDescription();
                        int SkillNature = Convert.ToInt32(IRListModel.SkillNature);
                        var Skill_Nature = SkillNatureList.Where(x => x.Id == SkillNature).FirstOrDefault();
                        if (Skill_Nature != null)
                            IRListModel.SkillNature = Skill_Nature.Description;
                        int CompanyStage = Convert.ToInt32(IRListModel.CompanyStage);
                        var Company_Stage = CompanyStageList.Where(x => x.Id == CompanyStage).FirstOrDefault();
                        if (Company_Stage != null)
                            IRListModel.CompanyStage = Company_Stage.Description;
                        int expreinece_year = Convert.ToInt32(IRListModel.Yearsofexperience);
                        var years_expreince = exprienceyears.Where(x => x.Id == expreinece_year).FirstOrDefault();
                        if (years_expreince != null)
                            IRListModel.Yearsofexperience = years_expreince.Description;
                    }
                    var modelList = modelList1;

                    MeetingVM NewModel = new MeetingVM();
                    NewModel.SubVideo_List = modelList1.ToList().Where(x => x.imid != id).ToList();

                    var usernames = model.candidateName.Trim().Split(" ");
                    NewModel.candidateName = model.candidateName;
                    NewModel.AssessmentGroup = model.AssessmentGroup;
                    NewModel.CandidateId = model.CandidateId;
                    NewModel.interviewDate = model.interviewDate;
                    NewModel.imid = model.imid;
                    NewModel.IRFId = model.IRFId;
                    NewModel.ClientName = model.ClientName;
                    NewModel.InterviewerComment = model.InterviewerComment;
                    NewModel.InterviewStatus = model.InterviewStatus;
                    NewModel.JDTiltle = model.JDTiltle;
                    NewModel.JDLink = model.JDLink;
                    NewModel.MeetingStatus = model.MeetingStatus;
                    NewModel.confirmationStatus = model.confirmationStatus;
                    NewModel.VideoLink = model.VideoLink;
                    NewModel.candidateEmail = model.candidateEmail;
                    NewModel.reportLink = model.reportLink;
                    NewModel.resumeLink = model.resumeLink;
                    NewModel.zoomLink = model.zoomLink;
                    var Senioritylevel = _iRFRepository.GetSenioritylevel();
                    var SkillCategoryList = _iRFRepository.GetSkillCategory();
                    if (IRListModel != null)
                    {
                        int Seniorityid = Convert.ToInt32(IRListModel.Senioritylevel);
                        if (!String.IsNullOrEmpty(IRListModel.SkillCategory))
                        {
                            var SkillCategorys = IRListModel.SkillCategory.Split(",");
                            var Skillset = "";
                            foreach (var item in SkillCategorys)
                            {
                                int SkillCategoryid = Convert.ToInt32(item);
                                if (!String.IsNullOrEmpty(Skillset))
                                {
                                    Skillset = Skillset + "," + SkillCategoryList.Where(x => x.Id == SkillCategoryid).FirstOrDefault().Name;
                                }
                                else
                                {
                                    Skillset = SkillCategoryList.Where(x => x.Id == SkillCategoryid).FirstOrDefault().Name;
                                }
                            }
                            IRListModel.SkillCategory = Skillset;
                            var Seniority_level = Senioritylevel.Where(x => x.Id == Seniorityid).FirstOrDefault();
                            if ((Seniority_level != null))
                                IRListModel.Senioritylevel = Seniority_level.Name;

                        }
                    }
                    NewModel.IreportModel = IRListModel ?? new InterviewReport();
                    NewModel.ShortListCount = model.ShortListCount;
                    NewModel.IsShortlisted = model.IsShortlisted;
                    NewModel.ThumbnailImage = model.ThumbnailImage;
                    NewModel.IsOnline = model.IsOnline;
                    NewModel.candidateEmail = model.candidateEmail;
                    NewModel.ClientHrContact = model.ClientHrContact;
                    // to get videocount details
                    NewModel.FeedbackAudioLink = model.FeedbackAudioLink;
                    NewModel.IsBiometricEnabled = model.IsBiometricEnabled;
                    NewModel.MeetingPatch = model.MeetingPatch;
                    if (model.MeetingPatch.Count() > 0)
                    {
                        NewModel.VideoLink = model.MeetingPatch.FirstOrDefault().VideoLink;
                    }
                    if (NewModel.IreportModel.InterviewerQuestionsCollection == null)
                    {
                        ViewBag.QuestionsDiscussedCount = 0;
                    }
                    else
                    {
                        ViewBag.QuestionsDiscussedCount = NewModel.IreportModel.InterviewerQuestionsCollection?.Count();
                    }
                    var comments = _meetingService.GetvedioComments(id);
                    NewModel.Comments = comments;
                    bool isQC = false;
                    NewModel.IreportModel.IsQC = isQC;
                    NewModel.irfIntId = model.irfIntId;
                    var menus = _userService.GetMenuBasedOnUserEmailld("ExternalUser").GroupBy(p => p.functionName).Select(g => g.First()).ToList();
                    if (menus != null)
                    {
                        menus = menus.Where(x => x.functionName != "Support" && x.functionName != "Common Pool" && x.functionName != "Risetube").ToList();
                        HttpContext.Session.SetString("userMenus", JsonConvert.SerializeObject(menus));
                    }
                    if (IRListModel != null)
                    {
                        GetStatusList(IRListModel, model.irfIntId);
                    }

                    ViewBag.VideoStatus = "Public";
                    ViewBag.Shortlist = "false";
                    if (Shortlistvideoids.Contains(id))
                    {
                        ViewBag.Shortlist = "true";
                    }
                    return View(NewModel);
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        [HttpPost]
        public JsonResult CandidateShortList(int videoid, string status)
        {
            if (status == "Shortlist")
            {
                var itemToRemove = Shortlistvideoids.Single(r => r == videoid);
                Shortlistvideoids.Remove(itemToRemove);
            }
            else
            {
                Shortlistvideoids.Add(videoid);
            }
            return Json("Success");
        }
        [HttpPost]
        public JsonResult FetchCandidateShortList()
        {
            string currenturl = Request.Host.ToString() + "/InterviewerSkills/InterviewerVideoPage";
            return Json(new { result = Shortlistvideoids, currenturl = currenturl });
        }
        [HttpPost]
        public async Task<JsonResult> CandidateShortListNotification(string videoids, string email, string comment)
        {
            var videoidlist = videoids.Split(";");
            string currenturl = Request.Host.ToString() + "/InterviewerSkills/InterviewerVideoPage/";
            var Emailds = await _iInterviewerSkillsService.FetchAdminList();
            string Emailaddress = string.Join(";", Emailds);
            Mail _mail = new Mail
            {
                TO = Emailaddress,
                Subject = "Risebird: Interview video is shortlisted"
            };
            var result = _notificationHelper.CandidateShortlistNotification(_mail, currenturl, comment, email, videoidlist);
            if (result.Contains("Mail sent successfully"))
            {
                return Json("ShortListed Successfully.");
            }
            else
            {
                return Json("Fail to Shortlist");
            }
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

            model.QuestionsList = new SelectList(_meetingService.GetTechSkillComments(irfId).Result, "Comment", "Comment");
        }
        public async Task<IActionResult> ISCInterviewMeetings(string id, string irfId, int? Page, string searchValue)
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
                        var meetings = await _iInterviewerSkillsService.GetInterviewerSkillInterviewMeetings(Convert.ToInt16(id));
                        meetings = meetings.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((ReceiveMeetingStatus)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();
                        ViewBag.IRF = irfId;
                        ViewBag.IRFintId = id;

                        foreach (var m in meetings)
                        {
                            if (m.InterviewStatus.HasValue)
                            {
                                m.intervieweStatusStatDesc = ((InterviewerApproved)(m.InterviewStatus)).GetDescription();
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
    new { ID = "Today", Name = "Today"},
    new { ID = "Tomorrow", Name = "Tomorrow"  },
    new { ID = "Yesterday", Name = "Yesterday"  },
    new { ID = "Next Seven Days", Name = "Next Seven Days" },
    new { ID = "All", Name = "All"},
},
"ID", "Name", "All");

                        ViewData["list"] = list;
                        meetings = meetings.OrderByDescending(x => x.interviewDate).ToList();

                        if (!String.IsNullOrEmpty(searchValue))
                        {
                            searchValue = searchValue.ToUpper().Trim();
                            meetings = meetings.Where(x => (!String.IsNullOrEmpty(x.candidateName) ? x.candidateName.ToUpper().Contains(searchValue) : x.candidateName == "") || (!String.IsNullOrEmpty(x.ClientHrContact) ? x.ClientHrContact.ToUpper().Contains(searchValue) : x.ClientHrContact == "")
                            || (!String.IsNullOrEmpty(x.confirmationStatus) ? x.confirmationStatus.ToUpper().Contains(searchValue) : x.confirmationStatus == "") || (!String.IsNullOrEmpty(x.MeetingStatusStatusDesc) ? x.MeetingStatusStatusDesc.ToUpper().Contains(searchValue) : x.MeetingStatusStatusDesc == "")
                            || (!String.IsNullOrEmpty(x.intervieweStatusStatDesc) ? x.intervieweStatusStatDesc.ToUpper().Contains(searchValue) : x.intervieweStatusStatDesc == "")).ToList();

                        }
                        ViewBag.TotalCount = meetings.Count;

                        ViewBag.PageStartCount = 10 * ((Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page)) - 1) + 1;
                        int a = meetings.Count / 10;
                        if (meetings.Count % 10 == 0)
                        {
                            ViewBag.PageEndCount = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
                        }
                        else
                        {
                            a = a + 1;
                            if (a == Page)
                            {
                                ViewBag.PageEndCount = meetings.Count;
                            }
                            else
                            {
                                ViewBag.PageEndCount = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));

                            }

                        }
                        if (meetings.Count == 0)
                        {
                            ViewBag.PageStartCount = 0;
                            ViewBag.PageEndCount = 0;
                        }
                        //_modelDetail = meetings;
                        //ViewBag.IRFId =id;
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
                return RedirectToAction("Index");
            }

        }
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ISCSelectedInterviewMeetings(string id, string irfId, int? Page, string searchValue)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                    {
                        var selectedMeeting = new List<MeetingVM>();
                        var meetings = (await _iInterviewerSkillsService.GetInterviewerSkillInterviewMeetings(Convert.ToInt16(id)));
                        meetings = meetings.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((ReceiveMeetingStatus)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();
                        ViewBag.IRF = irfId;
                        ViewBag.IRFintId = id;

                        foreach (var m in meetings)
                        {
                            if (m.InterviewStatus.HasValue && m.InterviewStatus.Value == 2)
                            {
                                m.intervieweStatusStatDesc = ((InterviewerApproved)(m.InterviewStatus)).GetDescription();
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
                            if (m.IsApproved.HasValue)
                            {
                                m.NextLevelApprovalStat = ((NextLevelApprovalStatus)(m.IsApproved)).GetDescription();
                            }
                            if (m.IsSelected.HasValue)
                            {
                                m.FinalSelectionStat = ((FinalSelectionStatus)(m.IsSelected)).GetDescription();
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
                        ViewBag.ProfileName = profileName.ToLower();
                        if (!String.IsNullOrEmpty(searchValue))
                        {
                            searchValue = searchValue.ToUpper().Trim();
                            selectedMeeting = selectedMeeting.Where(x => (!String.IsNullOrEmpty(x.candidateName) ? x.candidateName.ToUpper().Contains(searchValue) : x.candidateName == "") || (!String.IsNullOrEmpty(x.ClientHrContact) ? x.ClientHrContact.ToUpper().Contains(searchValue) : x.ClientHrContact == "") || (!String.IsNullOrEmpty(x.confirmationStatus) ? x.confirmationStatus.ToUpper().Contains(searchValue) : x.confirmationStatus == "") || (!String.IsNullOrEmpty(x.MeetingStatusStatusDesc) ? x.MeetingStatusStatusDesc.ToUpper().Contains(searchValue) : x.MeetingStatusStatusDesc == "") || (!String.IsNullOrEmpty(x.intervieweStatusStatDesc) ? x.intervieweStatusStatDesc.ToUpper().Contains(searchValue) : x.intervieweStatusStatDesc == "")).ToList();

                        }
                        ViewBag.TotalCount = selectedMeeting.Count;

                        ViewBag.PageStartCount = 10 * ((Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page)) - 1) + 1;
                        int a = selectedMeeting.Count / 10;
                        if (selectedMeeting.Count % 10 == 0)
                        {
                            ViewBag.PageEndCount = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
                        }
                        else
                        {
                            a = a + 1;
                            if (a == Page)
                            {
                                ViewBag.PageEndCount = selectedMeeting.Count;
                            }
                            else
                            {
                                ViewBag.PageEndCount = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));

                            }

                        }
                        if (selectedMeeting.Count == 0)
                        {
                            ViewBag.PageStartCount = 0;
                            ViewBag.PageEndCount = 0;
                        }
                        //_modelDetail = selectedMeeting;
                        ViewBag.IRFId = id;
                        return View(selectedMeeting.ToPagedList(Page ?? 1, 10));


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
        [Authorize]
        public SelectList GetInterviewersByAssessmentGroup(string assessmentgroupid)
        {

            var list = new List<SelectListItem>();
            var interviewers = _iInterviewerSkillsService.GetInterviewersByAssessmentGroup(assessmentgroupid);

            if (interviewers != null && interviewers.Count() > 0)
            {
                return new SelectList(interviewers, "UserId", "EmailId");
            }
            else
                list.Add(new SelectListItem { Text = "No Interviewer", Value = "" });
            //clientUsers.ToList().Add(new Users { id = 0, FirstName = "No Client User" });
            return new SelectList(list, "Value", "Text");
        }

    }
}