using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Risebird.CommonHelper;
using Risebird.Repository.Interface;

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
        private IUserRepository _userRepository;
        public UserController(IUserService userService, IClientService clientService, IApplicationErrorLogger applicationErrorLogger, IAzureStorageHelper azureStorageHelper,
                                ISkillService skillService, IProfileService profileService, IUserRepository userRepository)

        {
            _userService = userService;
            _clientService = clientService;
            _applicationErrorLogger = applicationErrorLogger;
            _azureStorageHelper = azureStorageHelper;
            _skillService = skillService;
            _profileService = profileService;
            _userRepository = userRepository;
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

        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (_userService.DeleteUser(id))
                    {
                        TempData["SuccessErrorMessageUser"] = "User is deleted Successfully";
                        return Json(new { url = Url.Action("Index", "User") });
                    }
                    else
                    {
                        TempData["SuccessErrorMessageUser"] = "Deletion of User Failed";
                        return Json(new { url = Url.Action("Index", "User") });
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
                TempData["SuccessErrorMessageUser"] = "Deletion of User Failed";
                return RedirectToAction("Index");
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
                        clientUserVM.EmailId = clientUserVM.EmailId.ToLower();
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
                    if (_userService.DeleteClientUser(id))
                    {
                        TempData["SuccessErrorMessageClientUser"] = "Client User is deleted Successfully";
                        return Json(new { url = Url.Action("ClientUsers", "User") });
                    }
                    else
                    {
                        TempData["SuccessErrorMessageClientUser"] = "Deletion of Client User Failed";
                        return Json(new { url = Url.Action("ClientUsers", "User") });
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
                        clientUserVM.EmailId = clientUserVM.EmailId.ToLower();
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

        #endregion

        #region Interviewer Functions

        public IActionResult Interviewers()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (TempData["SuccessErrorMessageInterviewer"] != null)
                    {
                        ViewBag.Message = TempData["SuccessErrorMessageInterviewer"].ToString();
                        TempData.Remove("SuccessErrorMessageInterviewer");

                    }

                    return View(_userService.GetInterviewers());
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
                        interviewer.EmailId = interviewer.EmailId.ToLower();
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

                    //    return View("AddInterviewer", model);
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

                            return View("InterviewerUpdate", model);
                        }
                        else
                            interviewer.ResumeLink = uploadedFileURL;
                    }
                    else
                    {
                        interviewer.ResumeLink = TempData["ResumeLink"].ToString();
                    }


                    if (string.IsNullOrEmpty(interviewer.PrimarySkill))
                    {
                        ModelState.AddModelError(string.Empty, "Please mention Skill");
                        var model = new InterviewerVM();
                        model.PrimarySkills = new SelectList(_skillService.GetSkills(), "id", "Name");

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

            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessageInterviewer"] = "Deletion of Interviewer Failed";
                return RedirectToAction("Interviewers");
            }
        }

        #endregion

        #region Interviewer Profile Functions

        [HttpGet]
        public async Task<IActionResult> AddInterviewerProfile()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
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
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Interviewers");
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
                    string uploadedFileURL = string.Empty;

                    if (interviewerProfile.ResumeFile != null || interviewerProfile.CodedResumeFile != null)
                    {
                        if (interviewerProfile.ResumeFile != null)
                        {
                            await _azureStorageHelper.InsertBlob(interviewerProfile.ResumeFile.FileName, "resumes", interviewerProfile.ResumeFile);
                            var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                            var blob = containerRef.GetBlockBlobReference(interviewerProfile.ResumeFile.FileName);
                            uploadedFileURL = blob.Uri.ToString();
                            interviewerProfile.ResumeLink = uploadedFileURL;

                        }
                        else if (interviewerProfile.CodedResumeFile != null)
                        {
                            await _azureStorageHelper.InsertBlob(interviewerProfile.CodedResumeFile.FileName, "resumes", interviewerProfile.CodedResumeFile);
                            var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                            var blob = containerRef.GetBlockBlobReference(interviewerProfile.CodedResumeFile.FileName);
                            uploadedFileURL = blob.Uri.ToString();
                            interviewerProfile.CodedResumeLink = uploadedFileURL;
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("ResumeLink", "Please add a Resume File");
                        InterviewerProfileVM model = new InterviewerProfileVM();
                        // Get all the select Lists
                        GetAllSelectList(model);
                        return View("AddInterviewerProfile", model);
                    }

                    if (_userService.AddInterviewerProfile(interviewerProfile).Result)
                    {
                        TempData["SuccessErrorMessageInterviewer"] = "Interviewer is added Successfully";
                        return RedirectToAction("Interviewers");
                    }
                    else
                    {
                        TempData["SuccessErrorMessageInterviewer"] = "Interviewer addition is Failed while adding to the System";
                        return RedirectToAction("Interviewers");
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
                TempData["SuccessErrorMessageInterviewer"] = "Interviewer Profile addition is Failed";
                return RedirectToAction("Interviewers");
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditInterviewerProfile(int userId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    InterviewerProfileVM model = _userService.GetInterviewerProfileById(userId);
                    // Get all the select Lists
                    GetAllSelectList(model);

                    return View("AddInterviewerProfile", model);
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditInterviewerProfile(InterviewerProfileVM interviewerProfile)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    string uploadedFileURL = string.Empty;

                    if (interviewerProfile.ResumeFile != null || interviewerProfile.CodedResumeFile != null)
                    {
                        if (interviewerProfile.ResumeFile != null)
                        {
                            await _azureStorageHelper.InsertBlob(interviewerProfile.ResumeFile.FileName, "resumes", interviewerProfile.ResumeFile);
                            var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                            var blob = containerRef.GetBlockBlobReference(interviewerProfile.ResumeFile.FileName);
                            uploadedFileURL = blob.Uri.ToString();
                            interviewerProfile.ResumeLink = uploadedFileURL;

                        }
                        else if (interviewerProfile.CodedResumeFile != null)
                        {
                            await _azureStorageHelper.InsertBlob(interviewerProfile.CodedResumeFile.FileName, "resumes", interviewerProfile.CodedResumeFile);
                            var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                            var blob = containerRef.GetBlockBlobReference(interviewerProfile.CodedResumeFile.FileName);
                            uploadedFileURL = blob.Uri.ToString();
                            interviewerProfile.CodedResumeLink = uploadedFileURL;
                        }
                    }

                    if (_userService.EditInterviewerProfile(interviewerProfile).Result)
                    {
                        TempData["SuccessErrorMessageInterviewer"] = "Interviewer is added Successfully";
                        return RedirectToAction("Interviewers");
                    }
                    else
                    {
                        TempData["SuccessErrorMessageInterviewer"] = "Interviewer addition is Failed while adding to the System";
                        return RedirectToAction("Interviewers");
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
                TempData["SuccessErrorMessageInterviewer"] = "Interviewer Profile addition is Failed";
                return RedirectToAction("Interviewers");
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

            //=========Skills============

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
            model.SkillTypeList = new SelectList(_skillService.GetSkills(), "id", "Name");
            model.LevelList = new SelectList(_userService.GetLevelTech(), "id", "Name");
            var companyTypes = from CompanyType s in Enum.GetValues(typeof(CompanyType))
                               select new { ID = (int)s, Name = s.GetDescription() };
            model.CompanyTypeList = new SelectList(companyTypes, "ID", "Name");


            var dayTimeSlot = from DayTimeSlot s in Enum.GetValues(typeof(DayTimeSlot))
                              select new { ID = (int)s, Name = s.GetDescription() };
            model.DayTimeSlotList = new SelectList(dayTimeSlot, "ID", "Name");
            var daySlot = from DaySlot s in Enum.GetValues(typeof(DaySlot))
                          select new { ID = (int)s, Name = s.GetDescription() };
            model.DaySlotList = new SelectList(daySlot, "ID", "Name");
        }
        public JsonResult IsNickNameExists(string NickName, int UserId)
        {
            //check if the NickName exists
            IEnumerable<string> NickNamesList = _userService.GetNickNames(UserId);
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
            IEnumerable<string> EmailIdList = _userService.GetEmailIds(UserId);
            if (EmailIdList.Contains(EmailId))
            {
                return Json(false, new Newtonsoft.Json.JsonSerializerSettings());
            }
            else
                return Json(true, new Newtonsoft.Json.JsonSerializerSettings());
        }

        [HttpGet]
        public async Task<IActionResult> AddEmployerDetails()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    InterviewerProfessional model = new InterviewerProfessional();
                    //TransInterviewerProfessional model = _userService.GetInterviewerProfileById(Id);

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
                return RedirectToAction("Interviewers");
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

                    if (_userService.AddEmployerProfile(interviewerProfessional).Result)
                    {
                        TempData["SuccessErrorMessageInterviewer"] = "Interviewer Employer Details is added Successfully";
                        return RedirectToAction("Interviewers");
                    }
                    else
                    {
                        TempData["SuccessErrorMessageInterviewer"] = "Interviewer addition is Failed while adding to the System";
                        return RedirectToAction("Interviewers");
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
                return RedirectToAction("Interviewers");
            }
        }
        private void GetAllEmployerSelectList()
        {
            ViewBag.ExperienceRange = new SelectList(_userService.GetExperienceRange(), "id", "Description");
            ViewBag.JobFunction = new SelectList(_userService.GetJobFunction(), "id", "Description");
            ViewBag.Industry = new SelectList(_userService.GetIndustry(), "id", "Description");

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
        public async Task<IActionResult> EditEmployerDetails(int Id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    InterviewerProfessional ModelProfessional = _userService.GetEmployerById(Id);
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
                TempData["SuccessErrorMessageInterviewer"] = "Interviewer Profile addition is Failed";
                return RedirectToAction("Interviewers");
            }
        }
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> EditEmployerDetails(InterviewerProfessional interviewerProfessional)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (_userService.EditEmployerProfile(interviewerProfessional).Result)
                    {
                        TempData["SuccessErrorMessageInterviewer"] = "Interviewer Employer Details is added Successfully";
                        return RedirectToAction("AddEmployerDetails");
                    }
                    else
                    {
                        TempData["SuccessErrorMessageInterviewer"] = "Interviewer addition is Failed while adding to the System";
                        return RedirectToAction("AddEmployerDetails");
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
                TempData["SuccessErrorMessageInterviewer"] = "Interviewer Profile addition is Failed";
                return RedirectToAction("Interviewers");
            }
        }

        [HttpGet]
        public async Task<IActionResult> AddInterviewerProfile_EmployerDetails(int UserId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    List<InterviewerProfessional> EmployerList = _userRepository.GetEmployerListById(UserId).Result;

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
                TempData["SuccessErrorMessageInterviewer"] = "Interviewer Profile addition is Failed";
                return RedirectToAction("Interviewers");
            }
        }
        public async Task<IActionResult> DeleteEmployerDetails(int Id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (_userService.DeleteEmployerProfile(Id).Result)

                    {
                        TempData["SuccessErrorMessageInterviewer"] = "Interviewer Employer Details is added Successfully";
                        return RedirectToAction("AddEmployerDetails");
                    }
                    else
                    {
                        TempData["SuccessErrorMessageInterviewer"] = "Interviewer addition is Failed while adding to the System";
                        return RedirectToAction("AddEmployerDetails");
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
                TempData["SuccessErrorMessageInterviewer"] = "Interviewer Profile addition is Failed";
                return RedirectToAction("Interviewers");
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

            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessageInterviewer"] = "Deletion of Interviewer Failed";
                return RedirectToAction("Interviewers");
            }
        }

        #endregion

        #region Resume Evaluator functions

        public IActionResult ResumeEvaluators()
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

                    return View(_userService.GetResumeEvaluators());
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

        public async Task<IActionResult> DeleteResumeEvaluator(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (_userService.DeleteResumeEvaluator(id))
                    {
                        TempData["SuccessErrorMessageEvaluate"] = "Resume Evaluator deleted successfully.";
                        return Json(new { url = Url.Action("ResumeEvaluators", "User") });
                    }
                    else
                    {
                        TempData["SuccessErrorMessageEvaluate"] = "Sorry! Could not delete Resume Evaluator!";
                        return Json(new { url = Url.Action("ResumeEvaluators", "User") });
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
                TempData["SuccessErrorMessageEvaluate"] = "Sorry! Could not delete Resume Evaluator!";
                return RedirectToAction("ResumeEvaluators");
            }
        }

        #endregion


        #region Recruiter functions

        public IActionResult Recruiters()
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

                    return View(_userService.GetRecruiters());
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

            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                //TempData["SuccessErrorMessageEvaluate"] = "Sorry! Could not delete Resume Evaluator!";
                return RedirectToAction("Recruiters");
            }
        }

        #endregion


    }
}