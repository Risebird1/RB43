using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
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
        private readonly IFileMimeTypeHelper _fileMimeTypeHelper;
        public UserController(IUserService userService, IClientService clientService, IApplicationErrorLogger applicationErrorLogger, IAzureStorageHelper azureStorageHelper,
                                ISkillService skillService, IProfileService profileService, ITwilioHelper twiliohelper, IConfiguration configuration, IFileMimeTypeHelper fileMimeTypeHelper)

        {
            _userService = userService;
            _clientService = clientService;
            _applicationErrorLogger = applicationErrorLogger;
            _azureStorageHelper = azureStorageHelper;
            _skillService = skillService;
            _profileService = profileService;
            _twiliohelper = twiliohelper;
            _configuration = configuration;
            _fileMimeTypeHelper = fileMimeTypeHelper;
        }

        #region User Fucntions
        [HttpGet]
        public async Task<IActionResult> Index()
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
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    ViewBag.profileName = profileName;
                    var model = await _userService.GetRisebirdUsers();
                    List<Profile> profiles = _profileService.GetProfile();
                    List<string> profilesToExclude = new List<string> { "Interviewer", "Client", "Trainer", "Candidate", "ResumeEvaluator" };
                    ViewBag.profiles = profiles.Where(x => !profilesToExclude.Contains(x.Name.ToString())).ToList();
                    if (HttpContext.Session.GetString("RiseBirdUserfilters") != null)
                    {
                        var filter = JsonConvert.DeserializeObject<SelectedPriorityViewFilter>(HttpContext.Session.GetString("RiseBirdUserfilters"));
                        filter.Profile = filter.Profile == null ? filter.Profile = model.Select(x => x.profileName).ToArray() : filter.Profile = filter.Profile;
                        filter.IsActive = filter.IsActive == null ? filter.IsActive = model.Select(x => x.IsActive).ToArray() : filter.IsActive = filter.IsActive;
                        model = model.Where(c => filter.Profile.Contains(c.profileName)
                        && ((filter.IsActive != null && filter.IsActive.Count() > 0) ? filter.IsActive.Contains(c.IsActive) : c.IsActive == false)).ToList();
                    }
                    if (profileName == "Admin" || profileName == "SupportAdmin")
                    {
                        return View(model);
                    }
                    else
                    {
                        return View(new List<UserList>());
                    }
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
        [HttpPost]
        public IActionResult Index(SelectedPriorityViewFilter filter)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (filter != null)
                    {
                        HttpContext.Session.SetString("RiseBirdUserfilters", JsonConvert.SerializeObject(filter));
                    }
                    return RedirectToAction("Index");
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
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ResetIndex()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("RiseBirdUserfilters");
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
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Add()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var model = new UserList();
                    List<Profile> profiles = _profileService.GetProfile();

                    List<string> profilesToExclude = new List<string> { "Interviewer", "Client", "Trainer", "Candidate", "ResumeEvaluator", "Freemium Client" };
                    List<string> profilesToInclude = new List<string> { "HR", "SeniorHR", "Quality Controller" };
                    if (profileName != "Admin")
                    {
                        profilesToExclude.Add("SupportAdmin");
                    }
                    profiles = profiles.Where(x => !profilesToExclude.Contains(x.Name.ToString())).ToList();
                    if (profileName == "SeniorHR")
                    {
                        profiles = profiles.Where(x => profilesToInclude.Contains(x.Name.ToString())).ToList();
                    }
                    model.UserProfiles = new SelectList(profiles, "id", "Name");
                    GetClientsSelectList(model);
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
                        if (!string.IsNullOrEmpty(user.EmailId))
                        {
                            user.EmailId = user.EmailId.ToLower();
                        }
                        if (await _userService.AddUser(user))
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
                        GetClientsSelectList(user);
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
                    UserList user = await _userService.GetUser(id);
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    ViewBag.profileName = profileName;
                    if (user != null)
                    {
                        List<Profile> profiles = _profileService.GetProfile();

                        List<string> profilesToExclude = new List<string> { "Interviewer", "Client", "Trainer", "Candidate", "ResumeEvaluator", "Recruiter" };
                        profiles = profiles.Where(x => !profilesToExclude.Contains(x.Name.ToString())).ToList();

                        user.UserProfiles = new SelectList(profiles, "id", "Name");
                        GetClientsSelectList(user);
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
                        if (!string.IsNullOrEmpty(user.EmailId))
                        {
                            user.EmailId = user.EmailId.ToLower();
                        }
                        if (await _userService.UpdateUser(user))
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
                        var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                        profileName = profileName.Replace("\"", "");
                        ViewBag.profileName = profileName;
                        user.UserProfiles = new SelectList(profiles, "id", "Name");
                        GetClientsSelectList(user);
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
        [HttpGet]
        public async Task<IActionResult> ClientUsers()
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
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    ViewBag.Profile = profileName;
                    return View(await _userService.GetClientUsers(emailid));
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
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var model = new ClientUserVM();
                    model.Clients = new SelectList(await _clientService.GetClients(emailid, profileName), "id", "Name");
                    model.ClientRoles = new SelectList(await _clientService.GetClientRoles(), "id", "Name");
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
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();

                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "SupportAdmin")
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

                            if (await _userService.AddClientUser(clientUserVM))
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
                            model.Clients = new SelectList(await _clientService.GetClients(emailid, profileName), "id", "Name");
                            model.ClientRoles = new SelectList(await _clientService.GetClientRoles(), "id", "Name");
                            return View("ClientUserAdd", model);
                        }
                    }
                    return RedirectToAction("ClientUsers");
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

        [HttpGet]
        public async Task<IActionResult> DeleteClientUser(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        if (await _userService.DeleteClientUser(id))
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
                    ClientUserVM clientUserVM = await _userService.GetClientUser(id);

                    if (clientUserVM != null)
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                        profileName = profileName.Replace("\"", "");
                        clientUserVM.Clients = new SelectList(await _clientService.GetClients(emailid, profileName), "id", "Name");
                        clientUserVM.ClientRoles = new SelectList(await _clientService.GetClientRoles(), "id", "Name");
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
                        if (await _userService.UpdateClientUser(clientUserVM))
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
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                        profileName = profileName.Replace("\"", "");
                        clientUserVM.Clients = new SelectList(await _clientService.GetClients(emailid, profileName), "id", "Name");
                        clientUserVM.ClientRoles = new SelectList(await _clientService.GetClientRoles(), "id", "Name");
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

        [HttpGet]
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

        [HttpGet]
        public async Task<IActionResult> Interviewers(int? Page, string searchValue)
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
                    ViewBag.profile = profileName;
                    var model = await _userService.GetInterviewers();
                    if (string.IsNullOrEmpty(searchValue) && Page > 1)
                    {
                        searchValue = SearchInterviewers;
                    }

                    ViewBag.Searchvalue = searchValue;
                    if (!string.IsNullOrEmpty(searchValue))
                    {
                        searchValue = searchValue.ToUpper().Trim();
                        model = model.Where(x => (!string.IsNullOrEmpty(x.FirstName) ? x.FirstName.ToUpper().Contains(searchValue) : x.FirstName == "") || (!string.IsNullOrEmpty(x.PrimarySkill) ? x.PrimarySkill.ToUpper().Contains(searchValue) : x.PrimarySkill == "") || (!string.IsNullOrEmpty(x.EmailId) ? x.EmailId.ToUpper().Contains(searchValue) : x.EmailId == "")).ToList();

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
                    model.PrimarySkills = new SelectList(await _skillService.GetSkills(), "id", "Name");
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
                        ModelState.AddModelError(string.Empty, "Please upload a resume.");
                        var model = new InterviewerVM();
                        model.PrimarySkills = new SelectList(await _skillService.GetSkills(), "id", "Name");
                        var Heuristics = from Heuristicsscorelevel s in Enum.GetValues(typeof(Heuristicsscorelevel))
                                         select new { ID = (int)s, Name = s.GetDescription() };
                        model.Heuristicsscorelist = new SelectList(Heuristics, "ID", "Name");
                        return View("AddInterviewer", model);
                    }

                    if (interviewer.File != null)
                    {
                        var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(interviewer.File);
                        if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                                            || fileMimeType == "application/pdf" || fileMimeType == "application/x-java-archive" || fileMimeType == "application/vnd.ms-powerpoint")
                        {
                            await _azureStorageHelper.InsertBlob(interviewer.File.FileName, "resumes", interviewer.File);
                            var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                            var blob = containerRef.GetBlockBlobReference(interviewer.File.FileName);
                            uploadedFileURL = blob.Uri.ToString();
                        }
                    }
                    else
                        uploadedFileURL = interviewer.ResumeLink;

                    List<Skill> skills = await _skillService.GetSkills();

                    if (uploadedFileURL == null || uploadedFileURL == string.Empty)
                    {
                        ModelState.AddModelError(string.Empty, "Resume Upload is failed.");
                        var model = new InterviewerVM();
                        model.PrimarySkills = new SelectList(await _skillService.GetSkills(), "id", "Name");

                        return View("AddInterviewer", model);
                    }

                    else
                    {
                        interviewer.ResumeLink = uploadedFileURL;
                        List<string> selectedSkill = skills.Where(x => interviewer.SelectedSkillIds.Contains(x.id)).Select(x => x.Name).ToList();
                        interviewer.PrimarySkill = string.Join(",", selectedSkill);
                        if (!string.IsNullOrEmpty(interviewer.EmailId))
                        {
                            interviewer.EmailId = interviewer.EmailId.Trim().ToLower();
                        }
                        if (await _userService.AddInterviewer(interviewer))
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
                    InterviewerVM interviewerVM = await _userService.GetInterviewer(id);

                    if (interviewerVM != null)
                    {
                        interviewerVM.PrimarySkills = new SelectList(await _skillService.GetSkills(), "id", "Name");
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
                        var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(interviewer.File);
                        if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                                            || fileMimeType == "application/pdf" || fileMimeType == "application/x-java-archive" || fileMimeType == "application/vnd.ms-powerpoint")
                        {
                            await _azureStorageHelper.InsertBlob(interviewer.File.FileName, "resumes", interviewer.File);
                            var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                            var blob = containerRef.GetBlockBlobReference(interviewer.File.FileName);
                            uploadedFileURL = blob.Uri.ToString();
                        }

                        if (uploadedFileURL == null)
                        {
                            ModelState.AddModelError(string.Empty, "Resume Upload is failed.");
                            var model = new InterviewerVM();
                            model.PrimarySkills = new SelectList(await _skillService.GetSkills(), "id", "Name");
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
                        model.PrimarySkills = new SelectList(await _skillService.GetSkills(), "id", "Name");
                        var Heuristics = from Heuristicsscorelevel s in Enum.GetValues(typeof(Heuristicsscorelevel))
                                         select new { ID = (int)s, Name = s.GetDescription() };
                        model.Heuristicsscorelist = new SelectList(Heuristics, "ID", "Name");
                        return View("InterviewerUpdate", model);
                    }
                    List<Skill> skills = await _skillService.GetSkills();
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

                    if (!string.IsNullOrEmpty(interviewer.EmailId))
                    {
                        interviewer.EmailId = interviewer.EmailId.Trim().ToLower();
                    }

                    if (await _userService.UpdateInterviewer(interviewer))
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

        [HttpGet]
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
                        if (await _userService.DeleteInterviewer(id))
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
        public async Task<FileContentResult> InterviewwersExporttoExcel()
        {
            ExcelPackage Ep = new ExcelPackage();
            ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("All Interviewers");
            string fileName = "Risebird" + DateTime.Now.ToString("dd/MM/yyyy") + "_All_Interviewers" + ".xlsx";
            try
            {

                var interviewers = await _userService.GetInterviewers();

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
        [HttpGet]
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
                model.PrimarySkills = new SelectList(await _skillService.GetSkills(), "id", "Name");

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
                        ModelState.AddModelError(string.Empty, "Please upload a resume.");
                        qcinterviewer.PrimarySkills = new SelectList(await _skillService.GetSkills(), "id", "Name");

                        return View("AddQCInterviewer", qcinterviewer);
                    }

                    if (qcinterviewer.File != null)
                    {
                        var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(qcinterviewer.File);
                        if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                                            || fileMimeType == "application/pdf" || fileMimeType == "application/x-java-archive" || fileMimeType == "application/vnd.ms-powerpoint")
                        {
                            await _azureStorageHelper.InsertBlob(qcinterviewer.File.FileName, "resumes", qcinterviewer.File);
                            var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                            var blob = containerRef.GetBlockBlobReference(qcinterviewer.File.FileName);
                            uploadedFileURL = blob.Uri.ToString();

                        }

                    }
                    else if (qcinterviewer.ResumeLink != null)
                    {
                        uploadedFileURL = qcinterviewer.ResumeLink;
                    }

                    List<Skill> skills = await _skillService.GetSkills();

                    if (uploadedFileURL == null)
                    {
                        ModelState.AddModelError(string.Empty, "Resume Upload is failed.");
                        qcinterviewer.PrimarySkills = new SelectList(await _skillService.GetSkills(), "id", "Name");

                        return View("AddQCInterviewer", qcinterviewer);
                    }

                    else
                    {
                        qcinterviewer.ResumeLink = uploadedFileURL;
                        List<string> selectedSkill = skills.Where(x => qcinterviewer.SelectedSkillIds.Contains(x.id)).Select(x => x.Name).ToList();
                        qcinterviewer.PrimarySkill = string.Join(",", selectedSkill);

                        qcinterviewer.EmailId = qcinterviewer.EmailId.ToLower();

                        if (await _userService.AddQCInterviewer(qcinterviewer))
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
                    qcinterviewer.PrimarySkills = new SelectList(await _skillService.GetSkills(), "id", "Name");
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
                    QCInterviewerVM qcinterviewerVM = await _userService.GetQCInterviewer(id);

                    if (qcinterviewerVM != null)
                    {
                        qcinterviewerVM.PrimarySkills = new SelectList(await _skillService.GetSkills(), "id", "Name");
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
                            var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(qcInterviewer.File);
                            if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                                                || fileMimeType == "application/pdf" || fileMimeType == "application/x-java-archive" || fileMimeType == "application/vnd.ms-powerpoint")
                            {
                                await _azureStorageHelper.InsertBlob(qcInterviewer.File.FileName, "resumes", qcInterviewer.File);
                                var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                                var blob = containerRef.GetBlockBlobReference(qcInterviewer.File.FileName);
                                uploadedFileURL = blob.Uri.ToString();
                            }

                            if (uploadedFileURL == null)
                            {
                                ModelState.AddModelError(string.Empty, "Resume Upload is failed.");
                                var model = new InterviewerVM();
                                model.PrimarySkills = new SelectList(await _skillService.GetSkills(), "id", "Name");

                                return View("QCInterviewerUpdate", model);
                            }
                            else
                                qcInterviewer.ResumeLink = uploadedFileURL;
                        }
                        else if (qcInterviewer.PageResumeLink != null)
                        {
                            qcInterviewer.ResumeLink = qcInterviewer.PageResumeLink;
                        }

                        if (string.IsNullOrEmpty(qcInterviewer.PrimarySkill))
                        {
                            ModelState.AddModelError(string.Empty, "Please mention Skill");
                            var model = new InterviewerVM();
                            model.PrimarySkills = new SelectList(await _skillService.GetSkills(), "id", "Name");

                            return View("QCInterviewerUpdate", model);
                        }

                        if (!string.IsNullOrEmpty(qcInterviewer.EmailId))
                        {
                            qcInterviewer.EmailId = qcInterviewer.EmailId.ToLower();
                        }

                        if (await _userService.UpdateQCInterviewer(qcInterviewer))
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
                        model.PrimarySkills = new SelectList(await _skillService.GetSkills(), "id", "Name");

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

        [HttpGet]
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
                        if (await _userService.DeleteQCInterviewer(id))
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

        [HttpGet]
        public async Task<IActionResult> ResumeEvaluators(int? Page, string searchValue)
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

                    var model = await _userService.GetResumeEvaluators();
                    if (!string.IsNullOrEmpty(searchValue))
                    {
                        searchValue = searchValue.ToUpper().Trim();
                        model = model.Where(x => (!string.IsNullOrEmpty(x.FirstName) ? x.FirstName.ToUpper().Contains(searchValue) : x.FirstName == "") || (!string.IsNullOrEmpty(x.EmailId) ? x.EmailId.ToUpper().Contains(searchValue) : x.EmailId == "")).ToList();

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

                    if (await _userService.AddResumeEvaluator(resumeEvaluator))
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
                    var resumeEvaluator = await _userService.GetResumeEvaluator(id);
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

                    if (await _userService.EditResumeEvaluator(resumeEvaluator))
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
                        var result = await _userService.DeleteResumeEvaluator(id);
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

        [HttpGet]
        public async Task<IActionResult> Recruiters(int? Page, string searchValue)
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
                    var model = await _userService.GetRecruiters();
                    if (!string.IsNullOrEmpty(searchValue))
                    {
                        searchValue = searchValue.ToUpper().Trim();
                        model = model.Where(x => (!string.IsNullOrEmpty(x.FirstName) ? x.FirstName.ToUpper().Contains(searchValue) : x.FirstName == "") || (!string.IsNullOrEmpty(x.EmailId) ? x.EmailId.ToUpper().Contains(searchValue) : x.EmailId == "")).ToList();

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

                    if (await _userService.AddRecruiter(recruiter))
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

        [HttpGet]
        public async Task<IActionResult> EditRecruiter(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var recruiter = await _userService.GetRecruiter(id);
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

                    if (await _userService.EditRecruiter(recruiter))
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

        [HttpGet]
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
                        if (await _userService.DeleteRecruiter(id))
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
                if (!string.IsNullOrEmpty(searchValue))
                {
                    searchValue = searchValue.ToUpper().Trim();
                    model = model.Where(x => (!string.IsNullOrEmpty(x.FirstName) ? x.FirstName.ToUpper().Contains(searchValue) : x.FirstName == "") || (!string.IsNullOrEmpty(x.EmailId) ? x.EmailId.ToUpper().Contains(searchValue) : x.EmailId == "")).ToList();

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
                if (!string.IsNullOrEmpty(searchValue))
                {
                    searchValue = searchValue.ToUpper().Trim();
                    model = model.Where(x => (!string.IsNullOrEmpty(x.FirstName) ? x.FirstName.ToUpper().Contains(searchValue) : x.FirstName == "") || (!string.IsNullOrEmpty(x.EmailId) ? x.EmailId.ToUpper().Contains(searchValue) : x.EmailId == "")).ToList();

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
                if (!string.IsNullOrEmpty(searchValue))
                {
                    searchValue = searchValue.ToUpper().Trim();
                    model = model.Where(x => (!string.IsNullOrEmpty(x.FirstName) ? x.FirstName.ToUpper().Contains(searchValue) : x.FirstName == "") || (!string.IsNullOrEmpty(x.EmailId) ? x.EmailId.ToUpper().Contains(searchValue) : x.EmailId == "")).ToList();

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

        [HttpGet]
        public async Task<JsonResult> makecall(string sid, int? interviewid, string callRecipient, string callRecipientPhoneNumber = null, string IRFId = null, string recipientType = null, string CandidateEmail = null)//sid  actually means callSid here
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var currentUserEmailId = HttpContext.Session.GetString("email")?.Trim();
                currentUserEmailId = currentUserEmailId.Replace("\"", "");
                //var callbackUrl = "https://" + Request.Host.ToString() + "/User/CallRecordingback";
                var callbackUrl = "";
                var recordingSid = await _twiliohelper.callrecording(sid, callbackUrl);
                if (!string.IsNullOrEmpty(recipientType) && recipientType == "Interviewer")
                {
                    var resultset = _userService.SaveCallRecordingInterviewer(CandidateEmail, currentUserEmailId, sid, recordingSid);
                }
                else
                {
                    var resultset = _userService.SaveCallRecording((interviewid.HasValue ? interviewid.Value : 0), IRFId, callRecipient, currentUserEmailId, sid, recordingSid);
                }
                _ = await _userService.SaveCallRecordingData(interviewid, (callRecipient != null ? callRecipient : CandidateEmail), currentUserEmailId, sid, recordingSid, callRecipientPhoneNumber, IRFId);
                return Json(recordingSid);
            }
            return Json("Fail");
        }
        [HttpGet]
        public async Task<JsonResult> makecallInterviewer(string sid, string CandidateEmail)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                //var callbackUrl = "https://" + Request.Host.ToString() + "/User/CallRecordingback";
                var callbackUrl = "";
                var result = await _twiliohelper.callrecording(sid, callbackUrl);
                var resultset = _userService.SaveCallRecordingInterviewer(CandidateEmail, emailid, sid, result);
                return Json(result);
            }
            return Json("Fail");
        }

        [HttpPost]
        public async Task<JsonResult> FetchRecordings(string candidateEmailid, string irfid, string type)
        {
            try
            {
                //string accountSid = _configuration["TwilioSettings:ACCOUNT_SID"];
                var resultset = new List<CallRecordingDetailsVM>();
                if (type == "Interviewer")
                {
                    resultset = await _userService.FetchCallRecordingInterviewer(candidateEmailid);
                }
                else
                {
                    resultset = await _userService.FetchCallRecording(candidateEmailid, irfid);
                }
                if (resultset != null)
                {
                    resultset = resultset.OrderByDescending(x => x.CalledOn).ToList();
                }
                foreach (var item in resultset)
                {
                    if (!string.IsNullOrEmpty(item.RecordingUrl))
                        item.RecordingUrl = "https://api.twilio.com/2010-04-01/Accounts/ACcad9f6682d4a76df9af7cde78dd0867e/Recordings/" + item.RecordingUrl;
                }
                return Json(resultset);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(ex);
            }
        }

        [HttpGet]
        private async Task GetClientsSelectList(UserList model)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var Clients = await _clientService.GetClients(emailid, profileName);
                var _ClientList = new List<SelectListItem>();
                foreach (var item in Clients)
                {
                    bool selected = false;
                    if (model.ClientsArray != null && model.ClientsArray.Contains(item.id.ToString()))
                    {
                        selected = true;
                    }
                    var selectItem = new SelectListItem(item.Name, item.id.ToString(), selected);
                    _ClientList.Add(selectItem);
                }
                model.ClientList = _ClientList;
            }
        }

        #region InstaZoomAccounts
        [HttpGet]
        public async Task<IActionResult> InstaZoomAccounts()
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin")
                {
                    List<ZoomAccountDetailsVM> model = new List<ZoomAccountDetailsVM>();
                    var resultset = await _userService.FetchInstaZoomAccounts();
                    if (resultset != null)
                    {
                        model = resultset;
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

        [HttpPost]
        public async Task DeleteInstaZoomAccount(int Accountid)
        {
            await _userService.DeleteInstaZoomAccount(Accountid);
        }

        [HttpPost]
        public async Task AddInstaZoomAccount(string AccountEmail)
        {
            var resultset = await _userService.AddInstaZoomAccount(AccountEmail);
        }
        #endregion

        #region Freemium Client
        [HttpGet]
        public async Task<IActionResult> PremiumClientUsers()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null)
                {
                    if (TempData["SuccessErrorMessagePremiumClientUser"] != null)
                    {
                        ViewBag.Message = TempData["SuccessErrorMessagePremiumClientUser"].ToString();
                        TempData.Remove("SuccessErrorMessagePremiumClientUser");

                    }
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    return View(await _userService.GetClientPremiumUsers(emailid));
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
        public async Task<IActionResult> AddPremiumClientUser()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var model = new ClientUserVM();
                    model.Clients = new SelectList(await _clientService.GetClients(emailid, profileName), "id", "Name");
                    model.SelectedClientRole = 2;
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
                return RedirectToAction("PremiumClientUsers");
            }

        }
        [HttpPost]
        public async Task<IActionResult> AddPremiumClientUser(ClientUserVM clientUserVM)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();

                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "SupportAdmin")
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
                            if (await _userService.AddPremiumClientUser(clientUserVM))
                            {
                                TempData["SuccessErrorMessagePremiumClientUser"] = "Client User is added Successfully";
                                return RedirectToAction("PremiumClientUsers");
                            }
                            else
                            {
                                TempData["SuccessErrorMessagePremiumClientUser"] = "Client User addition is Failed";
                                return RedirectToAction("PremiumClientUsers");
                            }
                        }
                        else
                        {
                            var model = new ClientUserVM();
                            model.Clients = new SelectList(await _clientService.GetClients(emailid, profileName), "id", "Name");
                            model.SelectedClientRole = 2;
                            return View(model);
                        }
                    }
                    return RedirectToAction("PremiumClientUsers");
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessagePremiumClientUser"] = "Client User addition is Failed";
                return RedirectToAction("PremiumClientUsers");
            }

        }
        [HttpGet]
        public async Task<IActionResult> EditPremiumClientUser(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null)
                {
                    ClientUserVM clientUserVM = await _userService.GetClientPremiumUser(id);
                    if (clientUserVM != null)
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                        profileName = profileName.Replace("\"", "");
                        clientUserVM.Clients = new SelectList(await _clientService.GetClients(emailid, profileName), "id", "Name");
                        clientUserVM.SelectedClientRole = 2;
                        return View(clientUserVM);
                    }
                    else
                        return RedirectToAction("PremiumClientUsers");
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return RedirectToAction("PremiumClientUsers");
            }
        }
        [HttpPost]
        public async Task<IActionResult> EditPremiumClientUser(ClientUserVM clientUserVM)
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
                        if (await _userService.UpdateClientPremiumUser(clientUserVM))
                        {
                            TempData["SuccessErrorMessagePremiumClientUser"] = "Client User is updated Successfully";
                            return RedirectToAction("PremiumClientUsers");
                        }
                        else
                        {
                            TempData["SuccessErrorMessagePremiumClientUser"] = "Updating Client User Failed";
                            return RedirectToAction("PremiumClientUsers");
                        }
                    }
                    else
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                        profileName = profileName.Replace("\"", "");
                        clientUserVM.Clients = new SelectList(await _clientService.GetClients(emailid, profileName), "id", "Name");
                        clientUserVM.SelectedClientRole = 2;
                        return View(clientUserVM);
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
                TempData["SuccessErrorMessagePremiumClientUser"] = "Updating Client User is Failed"; ;
                return RedirectToAction("PremiumClientUsers");
            }
        }
        #endregion
    }
}


