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
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" /*|| profileName == "Client" || profileName == "Recruiter"*/)
                {
                    var model = new InterviewerSkillsViewModel();
                    var InterviewerSkillsList = _iInterviewerSkillsService.GetAllInterviewerSkills(profileName);
                    model.InterviewerSkillsModelList = InterviewerSkillsList.Where(x => x.EndDate.Date > DateTime.Now).OrderByDescending(y => y.EndDate);
                    foreach (var item in model.InterviewerSkillsModelList)
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
                    if (TempData["SuccessErrorMessage"] != null)
                    {
                        model.Message = TempData["SuccessErrorMessage"].ToString();
                        TempData.Remove("SuccessErrorMessage");
                    }
                    if (HttpContext.Session.GetString("InterviewerSkillsFilter") != null)
                    {
                        var filter = JsonConvert.DeserializeObject<InterviewerSkillsFilter>(HttpContext.Session.GetString("InterviewerSkillsFilter"));
                        if (filter.StartDate != null && filter.EndDate != null)
                        {
                            model.InterviewerSkillsModelList = InterviewerSkillsList.Where(t => t.StartDate >= filter.StartDate && t.EndDate <= filter.EndDate).ToList();
                        }
                        else
                        {
                            model.InterviewerSkillsModelList = InterviewerSkillsList.Where(x => x.EndDate >= DateTime.Now);
                        }
                        filter.ISID = filter.ISID == null ? filter.ISID = model.InterviewerSkillsModelList.Select(x => x.InterviewerSkillId).ToArray() : filter.ISID = filter.ISID;

                        model.InterviewerSkillsModelList = model.InterviewerSkillsModelList.Where(c => filter.ISID.Contains(c.InterviewerSkillId)).ToList();

                        if (model.InterviewerSkillsModelList.Count() == 0 && filter.StartDate != null && filter.EndDate != null)
                        {
                            model.InterviewerSkillsModelList = InterviewerSkillsList.Where(t => t.StartDate.Date >= filter.StartDate && t.EndDate.Date <= filter.EndDate).ToList();
                        }
                        foreach (var item in model.InterviewerSkillsModelList)
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
                        model.InterviewerSkillsModelList = model.InterviewerSkillsModelList.OrderByDescending(x => x.EndDate).ToList();
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
                    var InterviewerSkillsList = _iInterviewerSkillsService.GetAllInterviewerSkills(profileName);
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
                        InterviewerSkillsList = _iInterviewerSkillsService.GetAllInterviewerSkills(profileName).Where(x => x.StartDate.Date >= filter.StartDate && x.EndDate.Date <= filter.EndDate);
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
                    model.InterviewerSkillsModelList = InterviewerSkillsList.OrderByDescending(x => x.EndDate).ToList();
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
                //ViewBag.LoginMailId = "interview@risebird.io";
                var model = new InterviewerSkillsVM();
                var interviewers = _interviewerSkillsRepository.GetInterviewers();
                model.SkillGeneralTypeList = new SelectList(_iInterviewerSkillsService.GetGeneralSkillMatrix(), "id", "Name");
                model.SelectabilityTypeList = new SelectList(_iInterviewerSkillsService.GetSelectabilityMatrix(), "id", "Name");
                ViewBag.RiseBirdHR = new SelectList(_iInterviewerSkillsService.GetRiseBirdHR(), "id", "EmailId");
                ViewBag.Level = new SelectList(_interviewerSkillsRepository.GetLevels(), "id", "Name");
                ViewBag.Interviewers = new SelectList(interviewers, "id", "EmailId");
                ViewBag.LevelTech = new SelectList(_interviewerSkillsRepository.GetLevelTech(), "id", "Name");
                ViewBag.Skill = new SelectList(_interviewerSkillsRepository.GetSkills(), "id", "Name");

                //ViewBag.Clients = new SelectList(_interviewerSkillsRepository.GetClients(), "id", "Name");
                //ViewBag.ClientUser = new SelectList(_interviewerSkillsRepository.GetCientUsers(), "id", "EmailId");
                //ViewBag.ClientRole = new SelectList(_interviewerSkillsRepository.GetClientRoles(), "id", "Name");
                //ViewBag.Recruiters = new SelectList(_interviewerSkillsRepository.GetRecruiters(), "UserId", "EmailId");
                //ViewBag.ResumeEvaluators = new SelectList(_interviewerSkillsRepository.GetResumeEvaluators(), "UserId", "EmailId");
                //ViewBag.HRs = new SelectList(_interviewerSkillsRepository.GetHRs(), "UserId", "EmailId");
                //ViewBag.QualityController = new SelectList(interviewers, "id", "EmailId");
                //ViewBag.EmergencyInterviewers = new SelectList(interviewers, "id", "EmailId");
                //var jobLevels = from JobLevels s in Enum.GetValues(typeof(JobLevels))
                //                select new { ID = (int)s, Name = s.GetDescription() };
                //ViewBag.JobLevelList = new SelectList(jobLevels, "ID", "Name"); ;

                //var companyTypes = from CompanyType s in Enum.GetValues(typeof(CompanyType))
                //                   select new { ID = (int)s, Name = s.GetDescription() };
                //model.CompanyTypeList = new SelectList(companyTypes, "ID", "Name");
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
                            TempData["SuccessErrorMessage"] = "Interviewer Skills is added Successfully";
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            TempData["SuccessErrorMessage"] = "Adding Interviewer Skills has Failed";
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
                TempData["SuccessErrorMessage"] = "Adding Interviewer Skills has failed";
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
                    string skills = string.Empty;
                    skills = string.Join(",", from item in model.SkillCollection select item.SkillId);
                    var interviewers = _iInterviewerSkillsService.GetInterviewers(skills);
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
                                TempData["SuccessErrorMessage"] = "Sorry! Interviewer Skills with this name already exits!";
                                return RedirectToAction("Index");
                            }
                            if (_iInterviewerSkillsService.AddInterviewerSkillsDetails(model).Result)
                            {
                                TempData["SuccessErrorMessage"] = "Interviewer Skills saved Successfully";
                                return RedirectToAction("Index");
                            }
                            else
                            {
                                TempData["SuccessErrorMessage"] = "Interviewer Skills edits were not saved.";
                                return RedirectToAction("Index");
                            }

                        }
                        else
                        {
                            if (_iInterviewerSkillsService.UpdateInterviewerSkills(model))
                            {
                                TempData["SuccessErrorMessage"] = "Interviewer Skills saved Successfully";
                                return RedirectToAction("Index");
                            }
                            else
                            {
                                TempData["SuccessErrorMessage"] = "Interviewer Skills edits were not saved.";
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
                TempData["SuccessErrorMessage"] = "Interviewer Skills edits were not saved";
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
        public IActionResult DeleteInterviewerSkills(string id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR")
                    {
                        if (_iInterviewerSkillsService.DeleteInterviewerSkills(Convert.ToInt16(id)).Result)
                        {
                            TempData["SuccessErrorMessage"] = "InterviewerSkills deleted Successfully";
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            TempData["SuccessErrorMessage"] = "InterviewerSkills delete Failed";
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
                TempData["SuccessErrorMessage"] = "InterviewerSkills delete Failed";
                return RedirectToAction("Index");
            }

        }
        //[Authorize]
        //public IActionResult UnArchiveDeleteInterviewerSkills(string id)
        //{
        //    try
        //    {
        //        if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
        //        {
        //            var profileName = HttpContext.Session.GetString("profileName")?.Trim();
        //            profileName = profileName.Replace("\"", "");
        //            if (profileName == "Admin" || profileName == "HR")
        //            {
        //                if (_iInterviewerSkillsService.UnArchiveDeleteInterviewerSkills(Convert.ToInt16(id)).Result)
        //                {
        //                    TempData["SuccessErrorMessage"] = "InterviewerSkills restored Successfully";
        //                    return RedirectToAction("Index");
        //                }
        //                else
        //                {
        //                    TempData["SuccessErrorMessage"] = "InterviewerSkills restore Failed";
        //                    return RedirectToAction("Index");
        //                }
        //            }
        //            else
        //            {
        //                return RedirectToAction("SignOut", "Session");
        //            }
        //        }
        //        else
        //        {
        //            return RedirectToAction("SignOut", "Session");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //        TempData["SuccessErrorMessage"] = "InterviewerSkills restore Failed";
        //        return RedirectToAction("Index");
        //    }

        //}
        [Authorize]
        [HttpGet]
        public IActionResult Interviewer(string id, string InterviewerSkillidName)
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
                        ViewBag.InterviewerName = InterviewerSkillidName;
                        ViewBag.InterviewerSkillId = id;
                        ViewBag.Profile = profileName;
                        var model = new InterviewerSkillsVM();

                        var InterviewerList = _iInterviewerSkillsService.GetInterviewerSkillsInterviewer(Convert.ToInt32(id))?.ToList();
                        var objList = new List<InterviewerVM>();
                        foreach (var item in InterviewerList)
                        {
                            var obj = new InterviewerVM();
                            obj.userId = item.id;
                            obj.EmailId = item.EmailId;
                            obj.PrimarySkill = item.PrimarySkill;
                            obj.FirstName = item.FirstName;
                            obj.ResumeLink = item.ResumeLink;
                            objList.Add(obj);

                            ViewBag.InterviewerId = item.UserId;
                        }
                        model.InterviewersCollection = (IList<InterviewerVM>)(objList);

                        var InterviewerSkillsList = _iInterviewerSkillsService.GetUnApprovedInterviewerList(Convert.ToInt32(id));
                        var objLists = new List<InterviewerListsVM>();
                        foreach (var item in InterviewerSkillsList)
                        {
                            var obj = new InterviewerListsVM();
                            obj.UserId = item.id;
                            obj.EmailId = item.EmailId;
                            obj.FirstName = item.FirstName;
                            obj.InterviewerSkillId = item.InterviewerSkillId;
                            obj.IsAdded = false;
                            objLists.Add(obj);
                        }
                        model.InterviewerListsCollection = (IList<InterviewerListsVM>)(objLists);
                        if (TempData["SuccessErrorMessageCandidate"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageCandidate"].ToString();
                            TempData.Remove("SuccessErrorMessageCandidate");
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
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }

        }
        [Authorize]
        public IActionResult DeleteInterviewerList(string id, string SkillId, string SkillName)
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
                        if (_iInterviewerSkillsService.DeleteInterviewerList(Convert.ToInt16(id)).Result)
                        {
                            TempData["SuccessErrorMessageCandidate"] = "Candidate deleted Successfully";
                            return RedirectToAction("Interviewer", new { id = Convert.ToInt32(SkillId), InterviewerSkillidName = SkillName });
                        }
                        else
                        {
                            TempData["SuccessErrorMessageCandidate"] = "Candidate delete Failed";
                            return RedirectToAction("Interviewer", new { id = Convert.ToInt32(SkillId), InterviewerSkillidName = SkillName });
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
                TempData["SuccessErrorMessageCandidate"] = "Candidate delete Failed";
                return RedirectToAction("Index");
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
        public IActionResult AddInterviewerList(int id, string SkillName)
        {

            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR")
                {
                    TempData["InterviewerSkillName"] = SkillName;
                    TempData["id"] = id;
                    ViewBag.InterviewerName = SkillName;
                    ViewBag.Id = id;
                    var model = new InterviewerSkillsVM();
                    var InterviewerSkillsList = _iInterviewerSkillsService.GetUnApprovedInterviewerList(id);
                    var objList = new List<InterviewerListsVM>();
                    foreach (var item in InterviewerSkillsList)
                    {
                        var obj = new InterviewerListsVM();
                        obj.UserId = item.id;
                        obj.EmailId = item.EmailId;
                        obj.FirstName = item.FirstName;
                        obj.InterviewerSkillId = item.InterviewerSkillId;
                        obj.IsAdded = false;
                        objList.Add(obj);
                    }
                    model.InterviewerListsCollection = (IList<InterviewerListsVM>)(objList);
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
        public async Task<IActionResult> AddInterviewer(InterviewerSkillsVM Model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR")
                    {
                        if (_iInterviewerSkillsService.AddInterviewer(Model))
                        {
                            TempData["SuccessErrorMessage"] = "Selected Interviewer Added Successfully";
                            return RedirectToAction("Interviewer", new RouteValueDictionary(
    new { controller = "InterviewerSkills", action = "Interviewer", id = TempData["id"], InterviewerSkillidName = TempData["InterviewerSkillName"] }));
                        }
                        else
                        {
                            TempData["SuccessErrorMessage"] = "Selected Interviewer Added has Failed";
                            return RedirectToAction("Interviewer", new RouteValueDictionary(
  new { controller = "InterviewerSkills", action = "Interviewer", id = TempData["id"], InterviewerSkillidName = TempData["InterviewerSkillName"] }));
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
                TempData["SuccessErrorMessage"] = "Selected Interviewer Added has failed";
                return RedirectToAction("Interviewer");
            }
        }

        //      public async Task<IActionResult> AddInterviewer(int id, bool IsChecked)
        //      {
        //          try
        //          {
        //              if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
        //              {
        //                  var profileName = HttpContext.Session.GetString("profileName")?.Trim();
        //                  profileName = profileName.Replace("\"", "");
        //                  if (profileName == "Admin" || profileName == "HR")
        //                  {
        //                      if (_iInterviewerSkillsService.AddInterviewer(id, IsChecked))
        //                      {
        //                          TempData["SuccessErrorMessage"] = "Selected Interviewer is Approved Successfully";
        //                          return RedirectToAction("Interviewer", new RouteValueDictionary(
        //  new { controller = "InterviewerSkills", action = "Interviewer", id = TempData["id"], InterviewerSkillidName = TempData["InterviewerSkillName"] }));
        //                      }
        //                      else
        //                      {
        //                          TempData["SuccessErrorMessage"] = "Selected Interviewer Approved has Failed";
        //                          return RedirectToAction("Interviewer", new RouteValueDictionary(
        //new { controller = "InterviewerSkills", action = "Interviewer", id = TempData["id"], InterviewerSkillidName = TempData["InterviewerSkillName"] }));
        //                      }
        //                  }
        //                  else
        //                  {
        //                      return RedirectToAction("SignOut", "Session");
        //                  }
        //              }
        //              else
        //              {
        //                  return RedirectToAction("SignOut", "Session");
        //              }
        //          }
        //          catch (Exception ex)
        //          {
        //              await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //              TempData["SuccessErrorMessage"] = "Selected Interviewer Approved has failed";
        //              return RedirectToAction("Interviewer");
        //          }
        //      }

   
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
                            TempData["SuccessErrorMessage"] = "Interviewer Skills paused Successfully";
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            TempData["SuccessErrorMessage"] = "Interviewer Skills pause Failed";
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
                TempData["SuccessErrorMessage"] = "Interviewer Skills pause Failed";
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
                            TempData["SuccessErrorMessage"] = "Interviewer Skills enabled Successfully";
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            TempData["SuccessErrorMessage"] = "Interviewer Skills enable Failed";
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
                TempData["SuccessErrorMessage"] = "Interviewer Skills play Failed";
                return RedirectToAction("Index");
            }
        }
    }
}