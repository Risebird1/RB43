using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using Risebird.CommonHelper;
using System.Data;
using Microsoft.AspNetCore.Authorization;
using OfficeOpenXml;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Routing;
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
        public InterviewerSkillsController(IInterviewerSkillsRepository InterviewerSkillsRepository, IApplicationErrorLogger applicationErrorLogger, IInterviewerSkillsService InterviewerSkillsService, IMeetingService meetingService, IAzureStorageHelper azureStorageHelper, IEvaluatorService evaluatorService)
        {
            _interviewerSkillsRepository = InterviewerSkillsRepository;
            _iInterviewerSkillsService = InterviewerSkillsService;
            _applicationErrorLogger = applicationErrorLogger;
            _meetingService = meetingService;
            _azureStorageHelper = azureStorageHelper;
            _evaluatorService = evaluatorService;
        }

        [Authorize]
        [HttpGet]
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid =  HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" )
                {
                    var model = new InterviewerSkillsViewModel();
                    var InterviewerSkillsList = _iInterviewerSkillsService.GetAllInterviewerSkillsChannel(profileName, emailid);
                    model.InterviewerSkillsChannelModelList = InterviewerSkillsList.Where(x => x.EndDate.Date > DateTime.Now).OrderByDescending(y => y.EndDate);
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
                model.SkillGeneralTypeList = new SelectList(_iInterviewerSkillsService.GetGeneralSkillMatrix(), "id", "Name");
                model.SelectabilityTypeList = new SelectList(_iInterviewerSkillsService.GetSelectabilityMatrix(), "id", "Name");
                ViewBag.RiseBirdHR = new SelectList(_iInterviewerSkillsService.GetRiseBirdHR(), "id", "EmailId");
                ViewBag.Level = new SelectList(_interviewerSkillsRepository.GetLevels(), "id", "Name");
                ViewBag.Interviewers = new SelectList(interviewers, "id", "EmailId");
                ViewBag.LevelTech = new SelectList(_interviewerSkillsRepository.GetLevelTech(), "id", "Name");
                ViewBag.Skill = new SelectList(_interviewerSkillsRepository.GetSkills(), "id", "Name");

                
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
        public IActionResult EditInterviewerSkills(string id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null)
                {
                    var model = _iInterviewerSkillsService.GetEditInterviewerSkills(Convert.ToInt16(id)).Result;
                    //string skills = string.Empty;
                    //skills = string.Join(",", from item in model.SkillCollection select item.SkillId);
                    var interviewers = _iInterviewerSkillsService.GetInterviewers(model.PrimarySkill);
                    model.SkillGeneralTypeList = new SelectList(_iInterviewerSkillsService.GetGeneralSkillMatrix(), "id", "Name");
                    model.SelectabilityTypeList = new SelectList(_iInterviewerSkillsService.GetSelectabilityMatrix(), "id", "Name");
                    ViewBag.RiseBirdHR = new SelectList(_iInterviewerSkillsService.GetRiseBirdHR(), "id", "EmailId");
                    ViewBag.Level = new SelectList(_interviewerSkillsRepository.GetLevels(), "id", "Name");
                    ViewBag.Interviewers = new SelectList(interviewers, "UserId", "EmailId");
                    ViewBag.LevelTech = new SelectList(_interviewerSkillsRepository.GetLevelTech(), "id", "Name");
                    ViewBag.Skill = new SelectList(_interviewerSkillsRepository.GetSkills(), "id", "Name");
                    TempData["EditInterviewerSkillId"] = model.InterviewerSkillId;
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    model.LoggedEmail = emailid;
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
                    if(profileName == "Admin" || profileName == "HR")
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

    }
}