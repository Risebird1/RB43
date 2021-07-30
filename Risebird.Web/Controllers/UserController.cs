using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
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
using X.PagedList;
namespace Risebird.Web.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private IUserService _userService;
        private IClientService _clientService;
        private IApplicationErrorLogger _applicationErrorLogger;
        private IAzureStorageHelper _azureStorageHelper;
        private ISkillService _skillService;
        private IProfileService _profileService;
        private static List<InterviewerVM> _modelDetails;
        private static List<Recruiter> _modelDetail;
        private static List<ResumeEvaluator> _modelDl;
        private static string Searchvalue;
        private static string SearchInterviewers;
        private ITwilioHelper _twiliohelper;
        private readonly IConfiguration _configuration;
        public UserController(IUserService userService, IClientService clientService, IApplicationErrorLogger applicationErrorLogger, IAzureStorageHelper azureStorageHelper,
                                ISkillService skillService, IProfileService profileService, ITwilioHelper twiliohelper, IConfiguration configuration)

        {
            _userService = userService;
            _clientService = clientService;
            _applicationErrorLogger = applicationErrorLogger;
            _azureStorageHelper = azureStorageHelper;
            _skillService = skillService;
            _profileService = profileService;
            _twiliohelper = twiliohelper;
            _configuration = configuration;
        }

        #region User Fucntions

        public IActionResult Index()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (TempData["SuccessErrorMessageUser"] != null)
                    {
                        ViewBag.Message = TempData["SuccessErrorMessageUser"].ToString();
                        TempData.Remove("SuccessErrorMessageUser");
                    }
                    return View(_userService.GetRisebirdUsers());
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        [HttpGet]
        public async Task<IActionResult> Add()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var model = new UserList();
                    List<Profile> profiles = _profileService.GetProfile();

                    List<String> profilesToExclude = new List<string> { "Interviewer", "Client", "Quality Controller", "Trainer", "Candidate", "ResumeEvaluator" };
                    profiles = profiles.Where(x => !profilesToExclude.Contains(x.Name.ToString())).ToList();

                    model.UserProfiles = new SelectList(profiles, "id", "Name");

                    return View("UserAdd", model);
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
        public async Task<IActionResult> Add(UserList user)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (ModelState.IsValid)
                    {
                        user.FirstName = user.FirstName.Trim();
                        user.LastName = user.LastName.Trim();
                        user.EmailId = user.EmailId.Trim();
                        user.EmailId = user.EmailId.ToLower();
                        if (_userService.AddUser(user))
                        {
                            TempData["SuccessErrorMessageUser"] = "User is added Successfully";
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            TempData["SuccessErrorMessageUser"] = "User addition is Failed";
                            return RedirectToAction("Index");
                        }
                    }

                    else
                    {
                        List<Profile> profiles = _profileService.GetProfile();
                        profiles = profiles.Where(x => x.Name != "Interviewer").ToList();
                        profiles = profiles.Where(x => x.Name != "Client").ToList();

                        user.UserProfiles = new SelectList(profiles, "id", "Name");

                        return View("UserAdd", user);
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
                TempData["SuccessErrorMessageUser"] = "User addition is Failed";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    UserList user = _userService.GetUser(id);

                    if (user != null)
                    {
                        List<Profile> profiles = _profileService.GetProfile();

                        List<String> profilesToExclude = new List<string> { "Interviewer", "Client", "Quality Controller", "Trainer", "Candidate", "ResumeEvaluator", "Recruiter" };
                        profiles = profiles.Where(x => !profilesToExclude.Contains(x.Name.ToString())).ToList();

                        user.UserProfiles = new SelectList(profiles, "id", "Name");
                        return View("UserUpdate", user);
                    }
                    else
                        return RedirectToAction("Index");
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Edit(UserList user)
        {

            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (ModelState.IsValid)
                    {
                        user.FirstName = user.FirstName.Trim();
                        user.LastName = user.LastName.Trim();
                        user.EmailId = user.EmailId.Trim();
                        user.EmailId = user.EmailId.ToLower();
                        if (_userService.UpdateUser(user))
                        {
                            TempData["SuccessErrorMessageUser"] = "User is updated Successfully";
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            TempData["SuccessErrorMessageUser"] = "Updating User is Failed";
                            return RedirectToAction("Index");
                        }
                    }
                    else
                    {
                        List<Profile> profiles = _profileService.GetProfile();
                        profiles = profiles.Where(x => x.Name != "Interviewer").ToList();
                        profiles = profiles.Where(x => x.Name != "Client").ToList();

                        user.UserProfiles = new SelectList(profiles, "id", "Name");

                        return View("UserUpdate", user);
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
                TempData["SuccessErrorMessageUser"] = "Updating User is Failed"; ;
                return RedirectToAction("Index");
            }

        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin")
                    {
                        var result = _userService.DeleteUser(id);
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

        #endregion

        #region Client User Functions

        public IActionResult ClientUsers()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null)
                {
                    if (TempData["SuccessErrorMessageClientUser"] != null)
                    {
                        ViewBag.Message = TempData["SuccessErrorMessageClientUser"].ToString();
                        TempData.Remove("SuccessErrorMessageClientUser");

                    }

                    return View(_userService.GetClientUsers());
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        [HttpGet]
        public async Task<IActionResult> AddClientUser()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null)
                {
                    var model = new ClientUserVM();
                    model.Clients = new SelectList(_clientService.GetClients(), "id", "Name");


                    model.ClientRoles = new SelectList(_clientService.GetClientRoles(), "id", "Name");
                    return View("ClientUserAdd", model);
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("ClientUsers");
            }

        }

        [HttpPost]
        public async Task<IActionResult> AddClientUser(ClientUserVM clientUserVM)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null)
                {
                    if (ModelState.IsValid)
                    {
                        if (!string.IsNullOrEmpty(clientUserVM.FirstName))
                        {
                            clientUserVM.FirstName = clientUserVM.FirstName.Trim();
                        }
                        if (!string.IsNullOrEmpty(clientUserVM.LastName))
                        {
                            clientUserVM.LastName = clientUserVM.LastName.Trim();
                        }
                        if (!string.IsNullOrEmpty(clientUserVM.EmailId))
                        {
                            clientUserVM.EmailId = clientUserVM.EmailId.Trim().ToLower();
                        }
                        if (_userService.AddClientUser(clientUserVM))
                        {
                            TempData["SuccessErrorMessageClientUser"] = "Client User is added Successfully";
                            return RedirectToAction("ClientUsers");
                        }
                        else
                        {
                            TempData["SuccessErrorMessageClientUser"] = "Client User addition is Failed";
                            return RedirectToAction("ClientUsers");
                        }
                    }

                    else
                    {

                        var model = new ClientUserVM();
                        model.Clients = new SelectList(_clientService.GetClients(), "id", "Name");


                        model.ClientRoles = new SelectList(_clientService.GetClientRoles(), "id", "Name");
                        return View("ClientUserAdd", model);
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
                TempData["SuccessErrorMessageClientUser"] = "Client User addition is Failed";
                return RedirectToAction("ClientUsers");
            }

        }

        public async Task<IActionResult> DeleteClientUser(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin")
                    {
                        if (_userService.DeleteClientUser(id))
                        {
                            //TempData["SuccessErrorMessageClientUser"] = "Client User is deleted Successfully";
                            return Json(new { url = Url.Action("ClientUsers", "User") });
                        }
                        else
                        {
                            //TempData["SuccessErrorMessageClientUser"] = "Deletion of Client User Failed";
                            return Json(new { url = Url.Action("ClientUsers", "User") });
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
                TempData["SuccessErrorMessageClientUser"] = "Deletion of Client User Failed";
                return RedirectToAction("ClientUsers");
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditClientUser(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null)
                {
                    ClientUserVM clientUserVM = _userService.GetClientUser(id);

                    if (clientUserVM != null)
                    {
                        clientUserVM.Clients = new SelectList(_clientService.GetClients(), "id", "Name");
                        clientUserVM.ClientRoles = new SelectList(_clientService.GetClientRoles(), "id", "Name");
                        return View("ClientUserUpdate", clientUserVM);
                    }
                    else
                        return RedirectToAction("ClientUsers");
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return RedirectToAction("ClientUsers");
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditClientUser(ClientUserVM clientUserVM)
        {

            try
            {
                if (HttpContext.Session.GetString("email") != null)
                {
                    if (ModelState.IsValid)
                    {
                        if (!string.IsNullOrEmpty(clientUserVM.FirstName))
                        {
                            clientUserVM.FirstName = clientUserVM.FirstName.Trim();

                        }
                        if (!string.IsNullOrEmpty(clientUserVM.LastName))
                        {
                            clientUserVM.LastName = clientUserVM.LastName.Trim();

                        }
                        if (!string.IsNullOrEmpty(clientUserVM.EmailId))
                        {
                            clientUserVM.EmailId = clientUserVM.EmailId.Trim();
                            clientUserVM.EmailId = clientUserVM.EmailId.ToLower();

                        }
                        if (_userService.UpdateClientUser(clientUserVM))
                        {
                            TempData["SuccessErrorMessageClientUser"] = "Client User is updated Successfully";
                            return RedirectToAction("ClientUsers");
                        }
                        else
                        {
                            TempData["SuccessErrorMessageClientUser"] = "Updating Client User Failed";
                            return RedirectToAction("ClientUsers");
                        }
                    }
                    else
                    {
                        clientUserVM.Clients = new SelectList(_clientService.GetClients(), "id", "Name");
                        clientUserVM.ClientRoles = new SelectList(_clientService.GetClientRoles(), "id", "Name");
                        return View("ClientUserUpdate", clientUserVM);
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
                TempData["SuccessErrorMessageClientUser"] = "Updating Client User is Failed"; ;
                return RedirectToAction("ClientUsers");
            }

        }

        public async Task<IActionResult> ResetNavigation(int userId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null)
                {
                    if (await _userService.ResetNavigation(userId))
                    {
                        return Json(true);
                    }
                    else
                    {
                        return Json(false);
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

        #region Interviewer Functions

        public IActionResult Interviewers(int? Page, string searchValue)
        {
            try
            {

                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (TempData["SuccessErrorMessageInterviewer"] != null)
                    {
                        ViewBag.Message = TempData["SuccessErrorMessageInterviewer"].ToString();
                        TempData.Remove("SuccessErrorMessageInterviewer");

                    }
                    ViewBag.Profile = profileName;
                    var model = _userService.GetInterviewers();
                    if (String.IsNullOrEmpty(searchValue) && Page > 1)
                    {
                        searchValue = SearchInterviewers;
                    }

                    ViewBag.Searchvalue = searchValue;
                    if (!String.IsNullOrEmpty(searchValue))
                    {
                        searchValue = searchValue.ToUpper().Trim();
                        model = model.Where(x => (!String.IsNullOrEmpty(x.FirstName) ? x.FirstName.ToUpper().Contains(searchValue) : x.FirstName == "") || (!String.IsNullOrEmpty(x.PrimarySkill) ? x.PrimarySkill.ToUpper().Contains(searchValue) : x.PrimarySkill == "") || (!String.IsNullOrEmpty(x.EmailId) ? x.EmailId.ToUpper().Contains(searchValue) : x.EmailId == "")).ToList();

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
                    //return View(_userService.GetInterviewers());
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        [HttpGet]
        public async Task<IActionResult> AddInterviewer()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var model = new InterviewerVM();
                    model.PrimarySkills = new SelectList(_skillService.GetSkills(), "id", "Name");
                    var Heuristics = from Heuristicsscorelevel s in Enum.GetValues(typeof(Heuristicsscorelevel))
                                     select new { ID = (int)s, Name = s.GetDescription() };
                    model.Heuristicsscorelist = new SelectList(Heuristics, "ID", "Name");
                    return View("AddInterviewer", model);
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Interviewers");
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddInterviewer(InterviewerVM interviewer)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    string uploadedFileURL = string.Empty;
                    if (interviewer.File == null && (interviewer.ResumeLink == null || interviewer.ResumeLink == ""))
                    {
                        ModelState.AddModelError(String.Empty, "Please upload a resume.");
                        var model = new InterviewerVM();
                        model.PrimarySkills = new SelectList(_skillService.GetSkills(), "id", "Name");
                        var Heuristics = from Heuristicsscorelevel s in Enum.GetValues(typeof(Heuristicsscorelevel))
                                         select new { ID = (int)s, Name = s.GetDescription() };
                        model.Heuristicsscorelist = new SelectList(Heuristics, "ID", "Name");
                        return View("AddInterviewer", model);
                    }

                    if (interviewer.File != null)
                    {
                        await _azureStorageHelper.InsertBlob(interviewer.File.FileName, "resumes", interviewer.File);
                        var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                        var blob = containerRef.GetBlockBlobReference(interviewer.File.FileName);
                        uploadedFileURL = blob.Uri.ToString();
                    }
                    else
                        uploadedFileURL = interviewer.ResumeLink;

                    List<Skill> skills = _skillService.GetSkills();

                    if (uploadedFileURL == null || uploadedFileURL == string.Empty)
                    {
                        ModelState.AddModelError(String.Empty, "Resume Upload is failed.");
                        var model = new InterviewerVM();
                        model.PrimarySkills = new SelectList(_skillService.GetSkills(), "id", "Name");

                        return View("AddInterviewer", model);
                    }

                    else
                    {
                        interviewer.ResumeLink = uploadedFileURL;
                        List<String> selectedSkill = skills.Where(x => interviewer.SelectedSkillIds.Contains(x.id)).Select(x => x.Name).ToList();
                        interviewer.PrimarySkill = String.Join(",", selectedSkill);
                        interviewer.EmailId = interviewer.EmailId.Trim().ToLower();
                        if (_userService.AddInterviewer(interviewer))
                        {
                            TempData["SuccessErrorMessageInterviewer"] = "Interviewer is added Successfully";
                            return RedirectToAction("Interviewers");
                        }
                        else
                        {
                            TempData["SuccessErrorMessageInterviewer"] = "Interviewer addition is Failed";
                            return RedirectToAction("Interviewers");
                        }
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
                TempData["SuccessErrorMessageInterviewer"] = "Interviewer addition is Failed";
                return RedirectToAction("Interviewers");
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditInterviewer(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    InterviewerVM interviewerVM = _userService.GetInterviewer(id);

                    if (interviewerVM != null)
                    {
                        interviewerVM.PrimarySkills = new SelectList(_skillService.GetSkills(), "id", "Name");
                        var Heuristics = from Heuristicsscorelevel s in Enum.GetValues(typeof(Heuristicsscorelevel))
                                         select new { ID = (int)s, Name = s.GetDescription() };
                        interviewerVM.Heuristicsscorelist = new SelectList(Heuristics, "ID", "Name");
                        TempData["ResumeLink"] = interviewerVM.ResumeLink;
                        return View("InterviewerUpdate", interviewerVM);
                    }
                    else
                        return RedirectToAction("Interviewers");
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return RedirectToAction("Interviewers");
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditInterviewer(InterviewerVM interviewer)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    string uploadedFileURL = null;

                    if (interviewer.File != null)
                    {

                        await _azureStorageHelper.InsertBlob(interviewer.File.FileName, "resumes", interviewer.File);
                        var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                        var blob = containerRef.GetBlockBlobReference(interviewer.File.FileName);
                        uploadedFileURL = blob.Uri.ToString();

                        if (uploadedFileURL == null)
                        {
                            ModelState.AddModelError(String.Empty, "Resume Upload is failed.");
                            var model = new InterviewerVM();
                            model.PrimarySkills = new SelectList(_skillService.GetSkills(), "id", "Name");
                            var Heuristics = from Heuristicsscorelevel s in Enum.GetValues(typeof(Heuristicsscorelevel))
                                             select new { ID = (int)s, Name = s.GetDescription() };
                            model.Heuristicsscorelist = new SelectList(Heuristics, "ID", "Name");
                            return View("InterviewerUpdate", model);
                        }
                        else
                            interviewer.ResumeLink = uploadedFileURL;
                    }
                    else
                    {
                        interviewer.ResumeLink = TempData["ResumeLink"]?.ToString();
                    }


                    if (string.IsNullOrEmpty(interviewer.PrimarySkill))
                    {
                        ModelState.AddModelError(string.Empty, "Please mention Skill");
                        var model = new InterviewerVM();
                        model.PrimarySkills = new SelectList(_skillService.GetSkills(), "id", "Name");
                        var Heuristics = from Heuristicsscorelevel s in Enum.GetValues(typeof(Heuristicsscorelevel))
                                         select new { ID = (int)s, Name = s.GetDescription() };
                        model.Heuristicsscorelist = new SelectList(Heuristics, "ID", "Name");
                        return View("InterviewerUpdate", model);
                    }
                    List<Skill> skills = _skillService.GetSkills();
                    if (interviewer.SelectedSkillIds != null)
                    {
                        List<string> selectedSkill = skills.Where(x => interviewer.SelectedSkillIds.Contains(x.id))
                            .Select(x => x.Name).ToList();
                        if (selectedSkill != null)
                        {
                            foreach (var skill in selectedSkill)
                            {
                                if (!interviewer.PrimarySkill.Contains(skill))
                                {
                                    if (string.IsNullOrEmpty(interviewer.PrimarySkill))
                                    {
                                        interviewer.PrimarySkill = string.Empty;
                                    }
                                    else
                                    {
                                        interviewer.PrimarySkill += ",";
                                    }

                                    interviewer.PrimarySkill += skill;
                                }
                            }
                        }
                    }

                    interviewer.EmailId = interviewer.EmailId.ToLower();

                    if (_userService.UpdateInterviewer(interviewer))
                    {
                        TempData["SuccessErrorMessageInterviewer"] = "Interviewer is Updated Successfully";
                        return RedirectToAction("Interviewers");
                    }
                    else
                    {
                        TempData["SuccessErrorMessageInterviewer"] = "Interviewer update is Failed";
                        return RedirectToAction("Interviewers");
                    }
                    //if (ModelState.IsValid)
                    //{

                    //}

                    //else
                    //{
                    //    var model = new InterviewerVM();
                    //    model.PrimarySkills = new SelectList(_skillService.GetSkills(), "id", "Name");

                    //    //ModelStateEntry i;
                    //    //ModelState.TryGetValue("SelectedSkillIds", out i);
                    //    //// = i.AttemptedValue.Split(",").Cast<Int64>().ToArray();
                    //    //model.SelectedSkillIds = Array.ConvertAll(i.AttemptedValue.Split(","), s => int.Parse(s));

                    //    return View("InterviewerUpdate", model);
                    //}
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessageInterviewer"] = "Interviewer addition is Failed";
                return RedirectToAction("Interviewers");
            }
        }

        public async Task<IActionResult> DeleteInterviewer(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin")
                    {
                        if (_userService.DeleteInterviewer(id))
                        {
                            TempData["SuccessErrorMessageInterviewer"] = "Interviewer is deleted Successfully";
                            return Json(new { url = Url.Action("Interviewers", "User") });
                        }
                        else
                        {
                            TempData["SuccessErrorMessageInterviewer"] = "Deletion of Interviewer Failed";
                            return Json(new { url = Url.Action("Interviewers", "User") });
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
                TempData["SuccessErrorMessageInterviewer"] = "Deletion of Interviewer Failed";
                return RedirectToAction("Interviewers");
            }
        }

        [Authorize]
        [HttpGet]
        public FileContentResult InterviewwersExporttoExcel()
        {
            ExcelPackage Ep = new ExcelPackage();
            ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("All Interviewers");
            string fileName = "Risebird" + DateTime.Now.ToString("dd/MM/yyyy") + "_All_Interviewers" + ".xlsx";
            try
            {

                var interviewers = _userService.GetInterviewers();

                Sheet.Cells["A1:K1"].Style.Font.Bold = true;
                Sheet.Cells["A1:K1"].Style.Font.Size = 12;
                Sheet.Cells["A1:K1"].Style.WrapText = true;

                int startColumn = 1; int endColumn = 10;
                for (int a = startColumn; a <= endColumn; a++)
                {
                    Sheet.Column(a).Width = 30;
                }
                Sheet.Cells[1, 1].Value = "First Name";
                Sheet.Cells[1, 2].Value = "Last Name";
                Sheet.Cells[1, 3].Value = "Email Id";
                Sheet.Cells[1, 4].Value = "Mobile No";
                Sheet.Cells[1, 5].Value = "Primary Skills";
                Sheet.Cells[1, 6].Value = "Resume Link";
                Sheet.Cells[1, 7].Value = "Approved";
                Sheet.Cells[1, 8].Value = "Slots";
                Sheet.Cells[1, 9].Value = "No of Interviews";
                Sheet.Cells[1, 10].Value = "Success rate";

                int i = 2;
                if (interviewers.Count() > 0)
                {
                    foreach (var item in interviewers)
                    {
                        Sheet.Cells[i, 1].Value = item.FirstName;
                        Sheet.Cells[i, 2].Value = item.LastName;
                        Sheet.Cells[i, 3].Value = item.EmailId;
                        Sheet.Cells[i, 4].Value = item.MobileNumber;
                        Sheet.Cells[i, 5].Value = item.PrimarySkill;

                        if (!string.IsNullOrEmpty(item.ResumeLink))
                        {
                            try
                            {
                                Sheet.Cells[i, 6].Hyperlink = new Uri(item.ResumeLink, UriKind.Absolute);
                                Sheet.Cells[i, 6].Value = "Resume Link";
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
                        var approvalStatus = item.IsApproved ? "Approved" : "Not Approved";
                        Sheet.Cells[i, 7].Value = approvalStatus;

                        Sheet.Cells[i, 8].Value = item.Slots;
                        Sheet.Cells[i, 9].Value = item.NoOfInterview.ToString();
                        Sheet.Cells[i, 10].Value = item.SuccessRate.ToString();

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

        #endregion

        #region QCInterviewer Functions
        public IActionResult QCInterviewers()
        {
            try
            {
                if (TempData["SuccessErrorMessageInterviewer"] != null)
                {
                    ViewBag.Message = TempData["SuccessErrorMessageInterviewer"].ToString();
                    TempData.Remove("SuccessErrorMessageInterviewer");

                }
                return View(_userService.GetQCInterviewers());
            }
            catch (Exception)
            {
                return null;
            }
        }

        [HttpGet]
        public async Task<IActionResult> AddQCInterviewer()
        {
            try
            {
                var model = new QCInterviewerVM();
                model.PrimarySkills = new SelectList(_skillService.GetSkills(), "id", "Name");

                return View("AddQCInterviewer", model);

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("QCInterviewers");
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddQCInterviewer(QCInterviewerVM qcinterviewer)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    string uploadedFileURL = string.Empty;
                    if (qcinterviewer.File == null && qcinterviewer.ResumeLink == null)
                    {
                        ModelState.AddModelError(String.Empty, "Please upload a resume.");
                        qcinterviewer.PrimarySkills = new SelectList(_skillService.GetSkills(), "id", "Name");

                        return View("AddQCInterviewer", qcinterviewer);
                    }

                    if (qcinterviewer.File != null)
                    {
                        await _azureStorageHelper.InsertBlob(qcinterviewer.File.FileName, "resumes", qcinterviewer.File);
                        var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                        var blob = containerRef.GetBlockBlobReference(qcinterviewer.File.FileName);
                        uploadedFileURL = blob.Uri.ToString();

                    }
                    else if (qcinterviewer.ResumeLink != null)
                    {
                        uploadedFileURL = qcinterviewer.ResumeLink;
                    }

                    List<Skill> skills = _skillService.GetSkills();

                    if (uploadedFileURL == null)
                    {
                        ModelState.AddModelError(String.Empty, "Resume Upload is failed.");
                        qcinterviewer.PrimarySkills = new SelectList(_skillService.GetSkills(), "id", "Name");

                        return View("AddQCInterviewer", qcinterviewer);
                    }

                    else
                    {
                        qcinterviewer.ResumeLink = uploadedFileURL;
                        List<String> selectedSkill = skills.Where(x => qcinterviewer.SelectedSkillIds.Contains(x.id)).Select(x => x.Name).ToList();
                        qcinterviewer.PrimarySkill = String.Join(",", selectedSkill);
                        qcinterviewer.EmailId = qcinterviewer.EmailId.ToLower();
                        if (_userService.AddQCInterviewer(qcinterviewer))
                        {
                            TempData["SuccessErrorMessageInterviewer"] = "QC Interviewer is added Successfully";
                            return RedirectToAction("QCInterviewers");
                        }
                        else
                        {
                            TempData["SuccessErrorMessageInterviewer"] = "QC Interviewer addition is Failed";
                            return RedirectToAction("QCInterviewers");
                        }
                    }
                }
                else
                {
                    qcinterviewer.PrimarySkills = new SelectList(_skillService.GetSkills(), "id", "Name");
                    return View("AddQCInterviewer", qcinterviewer);
                }

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessageInterviewer"] = "QC Interviewer addition is Failed";
                return RedirectToAction("QCInterviewers");
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditQCInterviewer(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    QCInterviewerVM qcinterviewerVM = _userService.GetQCInterviewer(id);

                    if (qcinterviewerVM != null)
                    {
                        qcinterviewerVM.PrimarySkills = new SelectList(_skillService.GetSkills(), "id", "Name");
                        TempData["ResumeLink"] = qcinterviewerVM.ResumeLink;
                        return View("QCInterviewerUpdate", qcinterviewerVM);
                    }
                    else
                        return RedirectToAction("QCInterviewers");
                }
                else
                {
                    return RedirectToAction("SignOut", "Account");
                }
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return RedirectToAction("QCInterviewers");
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditQCInterviewer(QCInterviewerVM qcInterviewer)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    qcInterviewer.ResumeLink = TempData["ResumeLink"].ToString();
                    string uploadedFileURL = null;
                    if (ModelState.IsValid)
                    {
                        if (qcInterviewer.File != null)
                        {

                            await _azureStorageHelper.InsertBlob(qcInterviewer.File.FileName, "resumes", qcInterviewer.File);
                            var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                            var blob = containerRef.GetBlockBlobReference(qcInterviewer.File.FileName);
                            uploadedFileURL = blob.Uri.ToString();

                            if (uploadedFileURL == null)
                            {
                                ModelState.AddModelError(String.Empty, "Resume Upload is failed.");
                                var model = new InterviewerVM();
                                model.PrimarySkills = new SelectList(_skillService.GetSkills(), "id", "Name");

                                return View("QCInterviewerUpdate", model);
                            }
                            else
                                qcInterviewer.ResumeLink = uploadedFileURL;
                        }
                        else if (qcInterviewer.PageResumeLink != null)
                        {
                            qcInterviewer.ResumeLink = qcInterviewer.PageResumeLink;
                        }

                        if (String.IsNullOrEmpty(qcInterviewer.PrimarySkill))
                        {
                            ModelState.AddModelError(String.Empty, "Please mention Skill");
                            var model = new InterviewerVM();
                            model.PrimarySkills = new SelectList(_skillService.GetSkills(), "id", "Name");

                            return View("QCInterviewerUpdate", model);
                        }

                        qcInterviewer.EmailId = qcInterviewer.EmailId.ToLower();

                        if (_userService.UpdateQCInterviewer(qcInterviewer))
                        {
                            TempData["SuccessErrorMessageQCInterviewer"] = "QCInterviewer is Updated Successfully";
                            return RedirectToAction("QCInterviewers");
                        }
                        else
                        {
                            TempData["SuccessErrorMessageQCInterviewer"] = "QCInterviewer update is Failed";
                            return RedirectToAction("Interviewers");
                        }
                    }

                    else
                    {
                        var model = new InterviewerVM();
                        model.PrimarySkills = new SelectList(_skillService.GetSkills(), "id", "Name");

                        //ModelStateEntry i;
                        //ModelState.TryGetValue("SelectedSkillIds", out i);
                        //// = i.AttemptedValue.Split(",").Cast<Int64>().ToArray();
                        //model.SelectedSkillIds = Array.ConvertAll(i.AttemptedValue.Split(","), s => int.Parse(s));

                        return View("QCInterviewerUpdate", model);
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Account");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessageQCInterviewer"] = "QCInterviewer update is Failed";
                return RedirectToAction("QCInterviewers");
            }
        }

        public async Task<IActionResult> DeleteQCInterviewer(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin")
                    {
                        if (_userService.DeleteQCInterviewer(id))
                        {
                            TempData["SuccessErrorMessageQCInterviewer"] = "QC Interviewer is deleted Successfully";
                            return Json(new { url = Url.Action("QCInterviewers", "User") });
                        }
                        else
                        {
                            TempData["SuccessErrorMessageQCInterviewer"] = "Deletion of QCInterviewer Failed";
                            return Json(new { url = Url.Action("QCInterviewers", "User") });
                        }

                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Account");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Account");
                }
            }

            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessageInterviewer"] = "Deletion of Interviewer Failed";
                return RedirectToAction("Interviewers");
            }
        }

        #endregion

        #region Resume Evaluator functions

        public IActionResult ResumeEvaluators(int? Page, string searchValue)

        {

            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (TempData["SuccessErrorMessageEvaluate"] != null)
                    {
                        ViewBag.Message = TempData["SuccessErrorMessageEvaluate"].ToString();
                        TempData.Remove("SuccessErrorMessageEvaluate");

                    }

                    var model = _userService.GetResumeEvaluators();
                    if (!String.IsNullOrEmpty(searchValue))
                    {
                        searchValue = searchValue.ToUpper().Trim();
                        model = model.Where(x => (!String.IsNullOrEmpty(x.FirstName) ? x.FirstName.ToUpper().Contains(searchValue) : x.FirstName == "") || (!String.IsNullOrEmpty(x.EmailId) ? x.EmailId.ToUpper().Contains(searchValue) : x.EmailId == "")).ToList();

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
                    _modelDl = model;
                    return View(model.ToPagedList(Page ?? 1, 10));

                    //  return View(_userService.GetResumeEvaluators());
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        [HttpGet]
        public async Task<IActionResult> AddResumeEvaluator()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var model = new ResumeEvaluator();
                    return View(model);
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("ResumeEvaluators");
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddResumeEvaluator(ResumeEvaluator resumeEvaluator)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {

                    if (_userService.AddResumeEvaluator(resumeEvaluator))
                    {
                        TempData["SuccessErrorMessageEvaluate"] = "Resume Evaluator is added successfully.";
                        return RedirectToAction("ResumeEvaluators");
                    }
                    else
                    {
                        TempData["SuccessErrorMessageEvaluate"] = "Sorry! Could not add Resume Evaluator!";
                        return RedirectToAction("ResumeEvaluators");
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
                TempData["SuccessErrorMessageEvaluate"] = "Sorry! Could not add Resume Evaluator!";
                return RedirectToAction("ResumeEvaluators");
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditResumeEvaluator(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var resumeEvaluator = _userService.GetResumeEvaluator(id);
                    return View(resumeEvaluator);
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return RedirectToAction("ResumeEvaluators");
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditResumeEvaluator(ResumeEvaluator resumeEvaluator)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {

                    if (_userService.EditResumeEvaluator(resumeEvaluator))
                    {
                        TempData["SuccessErrorMessageEvaluate"] = "Resume Evaluator updated successfully.";
                        return RedirectToAction("ResumeEvaluators");
                    }
                    else
                    {
                        TempData["SuccessErrorMessageEvaluate"] = "Sorry! Could not update Resume Evaluator!";
                        return RedirectToAction("ResumeEvaluators");
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
                TempData["SuccessErrorMessageEvaluate"] = "Sorry! Could not update Resume Evaluator!";
                return RedirectToAction("ResumeEvaluators");
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteResumeEvaluator(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin")
                    {
                        var result = _userService.DeleteResumeEvaluator(id);
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

        #endregion


        #region Recruiter functions

        public IActionResult Recruiters(int? Page, string searchValue)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (TempData["SuccessErrorMessageRecruiter"] != null)
                    {
                        ViewBag.Message = TempData["SuccessErrorMessageRecruiter"].ToString();
                        TempData.Remove("SuccessErrorMessageRecruiter");

                    }
                    var model = _userService.GetRecruiters();
                    if (!String.IsNullOrEmpty(searchValue))
                    {
                        searchValue = searchValue.ToUpper().Trim();
                        model = model.Where(x => (!String.IsNullOrEmpty(x.FirstName) ? x.FirstName.ToUpper().Contains(searchValue) : x.FirstName == "") || (!String.IsNullOrEmpty(x.EmailId) ? x.EmailId.ToUpper().Contains(searchValue) : x.EmailId == "")).ToList();

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
                    _modelDetail = model;
                    return View(model.ToPagedList(Page ?? 1, 10));

                    //return View(_userService.GetRecruiters());
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        [HttpGet]
        public async Task<IActionResult> AddRecruiter()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var model = new Recruiter();
                    return View(model);
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Recruiters");
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddRecruiter(Recruiter recruiter)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {

                    if (_userService.AddRecruiter(recruiter))
                    {
                        TempData["SuccessErrorMessageRecruiter"] = "Recruiter is added successfully.";
                        return RedirectToAction("Recruiters");
                    }
                    else
                    {
                        TempData["SuccessErrorMessageRecruiter"] = "Sorry! Could not add Recruiter!";
                        return RedirectToAction("Recruiters");
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
                TempData["SuccessErrorMessageRecruiter"] = "Sorry! Could not add Recruiter!";
                return RedirectToAction("Recruiters");
            }
        }

        public async Task<IActionResult> EditRecruiter(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var recruiter = _userService.GetRecruiter(id);
                    return View(recruiter);
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return RedirectToAction("Recruiters");
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditRecruiter(Recruiter recruiter)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {

                    if (_userService.EditRecruiter(recruiter))
                    {
                        TempData["SuccessErrorMessageRecruiter"] = "Recruiter updated successfully.";
                        return RedirectToAction("Recruiters");
                    }
                    else
                    {
                        TempData["SuccessErrorMessageRecruiter"] = "Sorry! Could not update Recruiter!";
                        return RedirectToAction("Recruiters");
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
                TempData["SuccessErrorMessageRecruiter"] = "Sorry! Could not update Recruiter!";
                return RedirectToAction("Recruiters");
            }
        }

        public async Task<IActionResult> DeleteRecruiter(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin")
                    {
                        if (_userService.DeleteRecruiter(id))
                        {
                            //TempData["SuccessErrorMessageEvaluate"] = "Resume Evaluator deleted successfully.";
                            return Json(new { url = Url.Action("Recruiters", "User") });
                        }
                        else
                        {
                            //TempData["SuccessErrorMessageEvaluate"] = "Sorry! Could not delete Resume Evaluator!";
                            return Json(new { url = Url.Action("Recruiters", "User") });
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
                //TempData["SuccessErrorMessageEvaluate"] = "Sorry! Could not delete Resume Evaluator!";
                return RedirectToAction("Recruiters");
            }
        }

        #endregion



        [HttpPost]
        public JsonResult InterviewersJson(string searchValue)
        {
            try
            {
                int Page = 1;
                var model = _modelDetails;
                SearchInterviewers = searchValue;
                int totalcount = model.Count;
                int Pagestart = 0;
                int Pageend = 0;

                ViewBag.Searchvalue = searchValue;
                if (!String.IsNullOrEmpty(searchValue))
                {
                    searchValue = searchValue.ToUpper().Trim();
                    model = model.Where(x => (!String.IsNullOrEmpty(x.FirstName) ? x.FirstName.ToUpper().Contains(searchValue) : x.FirstName == "") || (!String.IsNullOrEmpty(x.EmailId) ? x.EmailId.ToUpper().Contains(searchValue) : x.EmailId == "")).ToList();

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
        public JsonResult RecruitersJson(string searchValue)
        {
            try
            {
                int Page = 1;
                var model = _modelDetail;
                Searchvalue = searchValue;
                int totalcount = model.Count;
                int Pagestart = 0;
                int Pageend = 0;

                ViewBag.Searchvalue = searchValue;
                if (!String.IsNullOrEmpty(searchValue))
                {
                    searchValue = searchValue.ToUpper().Trim();
                    model = model.Where(x => (!String.IsNullOrEmpty(x.FirstName) ? x.FirstName.ToUpper().Contains(searchValue) : x.FirstName == "") || (!String.IsNullOrEmpty(x.EmailId) ? x.EmailId.ToUpper().Contains(searchValue) : x.EmailId == "")).ToList();

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
        public JsonResult ResumeEvaluatorsJson(string searchValue)
        {
            try
            {
                int Page = 1;
                var model = _modelDl;
                Searchvalue = searchValue;
                int totalcount = model.Count;
                int Pagestart = 0;
                int Pageend = 0;

                ViewBag.Searchvalue = searchValue;
                if (!String.IsNullOrEmpty(searchValue))
                {
                    searchValue = searchValue.ToUpper().Trim();
                    model = model.Where(x => (!String.IsNullOrEmpty(x.FirstName) ? x.FirstName.ToUpper().Contains(searchValue) : x.FirstName == "") || (!String.IsNullOrEmpty(x.EmailId) ? x.EmailId.ToUpper().Contains(searchValue) : x.EmailId == "")).ToList();

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
        public async Task<JsonResult> makecall(string sid, int interviewid, string IRFId, string CandidateEmail)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                //var callbackUrl = "https://" + Request.Host.ToString() + "/User/CallRecordingback";
                var callbackUrl = "";
                var result = await _twiliohelper.callrecording(sid, callbackUrl);
                var resultset = _userService.SaveCallRecording(interviewid, IRFId, CandidateEmail, emailid, sid, result);
                return Json(result);
            }
            return Json("Fail");
        }
        //[HttpPost]
        //public JsonResult CallRecordingback(string RecordingUrl, string CallSid)
        //{
        //    var result = _userService.SaveCallRecording(0, "", "", "", CallSid, RecordingUrl);
        //    return Json(new { CallSid, result });
        //}
        [HttpPost]
        public JsonResult FetchRecordings(string candidateEmailid, string irfid)
        {
            string accountSid = _configuration["TwilioSettings:ACCOUNT_SID"];
            var resultset = _userService.FetchCallRecording(candidateEmailid, irfid);
            foreach (var item in resultset)
            {
                item.RecordingUrl = "https://api.twilio.com/2010-04-01/Accounts/" + accountSid + "/Recordings/" + item.RecordingUrl;
                item.CalledOn = Convert.ToDateTime(item.CalledOn.ToString("dd-MM-yyyy hh:mm:ss tt"));
            }
            return Json(resultset);
        }
    }
}


