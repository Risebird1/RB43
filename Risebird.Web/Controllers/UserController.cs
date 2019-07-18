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
        public UserController(IUserService userService, IClientService clientService, IApplicationErrorLogger applicationErrorLogger, IAzureStorageHelper azureStorageHelper, ISkillService skillService)
        {
            _userService = userService;
            _clientService = clientService;
            _applicationErrorLogger = applicationErrorLogger;
            _azureStorageHelper = azureStorageHelper;
            _skillService = skillService;
        }

        public IActionResult Index()
        {
            try
            {
                return View(_userService.GetRisebirdUsers());
            }
            catch (Exception)
            {
                return null;
            }
        }
        public IActionResult Interviewers()
        {
            try
            {
                ViewBag.Message = TempData["SuccessErrorMessage"];
                return View(_userService.GetInterviewers());
            }
            catch (Exception)
            {
                return null;
            }
        }
        public IActionResult ClientUsers()
        {
            try
            {
                ViewBag.Message = TempData["SuccessErrorMessage"];
                return View(_userService.GetClientUsers());
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
                var model = new ClientUserVM();
                model.Clients = new SelectList(_clientService.GetClients(), "id", "Name");


                model.ClientRoles = new SelectList(_clientService.GetClientRoles(), "id", "Name");
                return View("ClientUserAdd", model);
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
                if (ModelState.IsValid)
                {
                    if (_userService.AddClientUser(clientUserVM))
                    {
                        TempData["SuccessErrorMessage"] = "Client User is added Successfully";
                        return RedirectToAction("ClientUsers");
                    }
                    else
                    {
                        TempData["SuccessErrorMessage"] = "Client User addition is Failed";
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
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessage"] = "Client User addition is Failed";
                return RedirectToAction("ClientUsers");
            }

        }

        public async Task<IActionResult> DeleteClientUser(int id)
        {
            try
            {
                if (_userService.DeleteClientUser(id))
                {
                    TempData["SuccessErrorMessage"] = "Client User is deleted Successfully";
                    return Json(new { url = Url.Action("ClientUsers", "User") });
                }
                else
                {
                    TempData["SuccessErrorMessage"] = "Deletion of Client User Failed";
                    return Json(new { url = Url.Action("ClientUsers", "User") });
                }
            }

            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessage"] = "Deletion of Client User Failed";
                return RedirectToAction("ClientUsers");
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditClientUser(int id)
        {
            try
            {
                ClientUserVM clientUserVM = _userService.GetClientUser(id);

                clientUserVM.Clients = new SelectList(_clientService.GetClients(), "id", "Name");
                clientUserVM.ClientRoles = new SelectList(_clientService.GetClientRoles(), "id", "Name");


                if (clientUserVM != null)
                    return View("ClientUserUpdate", clientUserVM);
                else
                    return RedirectToAction("ClientUsers");

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
                if (ModelState.IsValid)
                {
                    if (_userService.UpdateClientUser(clientUserVM))
                    {
                        TempData["SuccessErrorMessage"] = "Client User is updated Successfully";
                        return RedirectToAction("ClientUsers");
                    }
                    else
                    {
                        TempData["SuccessErrorMessage"] = "Updating Client User is Failed";
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
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessage"] = "Updating Client User is Failed"; ;
                return RedirectToAction("ClientUsers");
            }

        }

        [HttpGet]
        public async Task<IActionResult> AddInterviewer()
        {
            try
            {
                var model = new InterviewerVM();
                model.PrimarySkills = new SelectList(_skillService.GetSkills(), "id", "Name");

                return View("AddInterviewer", model);

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
                if (ModelState.IsValid)
                {
                    if (interviewer.File == null)
                    {
                        ModelState.AddModelError(String.Empty, "Please upload a resume.");
                        var model = new InterviewerVM();
                        model.PrimarySkills = new SelectList(_skillService.GetSkills(), "id", "Name");

                        return View("AddInterviewer", model);
                    }

                    byte[] fileData = new byte[interviewer.File.Length];
                    string uploadedFileURL = _azureStorageHelper.InsertBlob(interviewer.File.FileName, "resumes", fileData);
                    List<Skill> skills = _skillService.GetSkills();

                    if (uploadedFileURL == null)
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
                        if (_userService.AddInterviewer(interviewer))
                        {
                            TempData["SuccessErrorMessage"] = "Interviewer is added Successfully";
                            return RedirectToAction("Interviewers");
                        }
                        else
                        {
                            TempData["SuccessErrorMessage"] = "Interviewer addition is Failed";
                            return RedirectToAction("Interviewers");
                        }
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

                    return View("AddInterviewer", model);
                }

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessage"] = "Interviewer addition is Failed";
                return RedirectToAction("Interviewers");
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditInterviewer(int id)
        {
            try
            {
                InterviewerVM interviewerVM = _userService.GetInterviewer(id);

                interviewerVM.PrimarySkills = new SelectList(_skillService.GetSkills(), "id", "Name");
                TempData["ResumeLink"] = interviewerVM.ResumeLink;

                if (interviewerVM != null)
                    return View("InterviewerUpdate", interviewerVM);
                else
                    return RedirectToAction("Interviewers");

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
                interviewer.ResumeLink = TempData["ResumeLink"].ToString();
                string uploadedFileURL = null;
                if (ModelState.IsValid)
                {
                    if (interviewer.File != null)
                    {
                        byte[] fileData = new byte[interviewer.File.Length];
                        uploadedFileURL = _azureStorageHelper.InsertBlob(interviewer.File.FileName, "resumes", fileData);


                        if (uploadedFileURL == null)
                        {
                            ModelState.AddModelError(String.Empty, "Resume Upload is failed.");
                            var model = new InterviewerVM();
                            model.PrimarySkills = new SelectList(_skillService.GetSkills(), "id", "Name");

                            return View("AddInterviewer", model);
                        }
                        else
                            interviewer.ResumeLink = uploadedFileURL;
                    }

                    if(String.IsNullOrEmpty(interviewer.PrimarySkill))
                    {
                        ModelState.AddModelError(String.Empty, "Please mention Skill");
                        var model = new InterviewerVM();
                        model.PrimarySkills = new SelectList(_skillService.GetSkills(), "id", "Name");

                        return View("AddInterviewer", model);
                    }

                    
                    if (_userService.UpdateInterviewer(interviewer))
                    {
                        TempData["SuccessErrorMessage"] = "Interviewer is Updated Successfully";
                        return RedirectToAction("Interviewers");
                    }
                    else
                    {
                        TempData["SuccessErrorMessage"] = "Interviewer update is Failed";
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

                    return View("InterviewerUpdate", model);
                }

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessage"] = "Interviewer addition is Failed";
                return RedirectToAction("Interviewers");
            }
        }

        public async Task<IActionResult> DeleteInterviewer(int id)
        {
            try
            {
                if (_userService.DeleteInterviewer(id))
                {
                    TempData["SuccessErrorMessage"] = "Interviewer is deleted Successfully";
                    return Json(new { url = Url.Action("Interviewers", "User") });
                }
                else
                {
                    TempData["SuccessErrorMessage"] = "Deletion of Intervieer Failed";
                    return Json(new { url = Url.Action("Interviewers", "User") });
                }
            }

            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessage"] = "Deletion of Interviewer Failed";
                return RedirectToAction("Interviewers");
            }
        }
    }
}