using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;

namespace Risebird.Web.Controllers
{
    public class ClonableSkillChannelTemplateController : Controller
    {
        private readonly IUserService _userService;
        private readonly IIRFService _irfService;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private ISkillService _skillService;
        private IConfiguration _configuration;
        private readonly INotificationHelper _notificationHelper;
        private readonly IClonableSkillChannelTemplateService _clonableSkillChannelTemplateService;
        

        public ClonableSkillChannelTemplateController(IApplicationErrorLogger applicationErrorLogger, IIRFService irfService, IUserService userService, ISkillService skillService, IConfiguration configuration, INotificationHelper notificationHelper, IClonableSkillChannelTemplateService clonableSkillChannelTemplateService)
        {
            _userService = userService;
            _applicationErrorLogger = applicationErrorLogger;
            _irfService = irfService;
            _skillService = skillService;
            _configuration = configuration;
            _notificationHelper = notificationHelper;
            _clonableSkillChannelTemplateService = clonableSkillChannelTemplateService;
        }


        public async Task<IActionResult> Create(int irfId, int assessmentGroupId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR")
                    {
                        ClonableSkillChannelVM model = new ClonableSkillChannelVM();
                        model = await _clonableSkillChannelTemplateService.GetTemplateDataFromSourceSkillChannel(irfId, assessmentGroupId);
                        model.UserId = _irfService.GetUserId(emailid);
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
                TempData["SuccessErrorCloneSkillChannnelMessage"] = "Failed to redirect to Create Clone Template.";
                return RedirectToAction("CloneSkillChannnel", "IRF");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create(ClonableSkillChannelVM model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR")
                    {

                        bool isTemplateInvalid = false;
                        if (!model.InterviewerCollection.Any(x => x.IsActive))
                        {
                            isTemplateInvalid = true;
                            ModelState.AddModelError("", "Please select atleast one Interviewer from the list.");
                        }
                        if (!model.ClonableTemplateQuestionsCollection.Any(x => x.IsSelected))
                        {
                            isTemplateInvalid = true;
                            ModelState.AddModelError("", "Please select atleast one Question from the list.");
                        }
                        if (model.Selectability.Any(x => string.IsNullOrEmpty(x.Value)))
                        {
                            isTemplateInvalid = true;
                            ModelState.AddModelError("", "Please fill all the columns in the Selectability Matrix.");
                        }
                        if (model.Joinability.Any(x => string.IsNullOrEmpty(x.Value)))
                        {
                            isTemplateInvalid = true;
                            ModelState.AddModelError("", "Please fill all the columns in the Joinability Matrix.");
                        }
                        if (model.SkillGeneral.Any(x => string.IsNullOrEmpty(x.Value)))
                        {
                            isTemplateInvalid = true;
                            ModelState.AddModelError("", "Please fill all the columns in the General Skills' Matrix.");
                        }
                        if (isTemplateInvalid)
                        {
                            _clonableSkillChannelTemplateService.PopulateDropDownsForTemplate(model);
                            return View(model);
                        }
                        if (await _clonableSkillChannelTemplateService.CreateClonableSkillChannel(model, emailId))
                        {
                            TempData["SuccessErrorCloneSkillChannnelMessage"] = $"Clonable Skill Channel Template '{model.TemplateName}' created Successfully!";
                            return RedirectToAction("CloneSkillChannnel", "IRF");
                        }
                        else
                        {
                            TempData["SuccessErrorCloneSkillChannnelMessage"] = "Failed! Clonable Skill Channel Template could not be created!";
                            return RedirectToAction("CloneSkillChannnel", "IRF");
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
                TempData["SuccessErrorCloneSkillChannnelMessage"] = "Failed to redirect to Create Clone Template.";
                return RedirectToAction("CloneSkillChannnel", "IRF");
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR")
                    {
                        ClonableSkillChannelVM model = new ClonableSkillChannelVM();
                        model = await _clonableSkillChannelTemplateService.GetClonableSkillChannel(id);
                        model.UserId = _irfService.GetUserId(emailid);
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
                TempData["SuccessErrorCloneSkillChannnelMessage"] = "Failed to redirect to Create Clone Template.";
                return RedirectToAction("CloneSkillChannnel", "IRF");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Edit(ClonableSkillChannelVM model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR")
                    {

                        bool isTemplateInvalid = false;
                        if (!model.InterviewerCollection.Any(x => x.IsActive))
                        {
                            isTemplateInvalid = true;
                            ModelState.AddModelError("", "Please select atleast one Interviewer from the list.");
                        }
                        if (!model.ClonableTemplateQuestionsCollection.Any(x => x.IsSelected))
                        {
                            isTemplateInvalid = true;
                            ModelState.AddModelError("", "Please select atleast one Question from the list.");
                        }

                        if(model.IsTemplateJoinSelectGenskillsRequired)
                        {
                            if (model.Selectability.Any(x => string.IsNullOrEmpty(x.Value)))
                            {
                                isTemplateInvalid = true;
                                ModelState.AddModelError("", "Please fill all the columns in the Selectability Matrix.");
                            }
                            if (model.Joinability.Any(x => string.IsNullOrEmpty(x.Value)))
                            {
                                isTemplateInvalid = true;
                                ModelState.AddModelError("", "Please fill all the columns in the Joinability Matrix.");
                            }
                            if (model.SkillGeneral.Any(x => string.IsNullOrEmpty(x.Value)))
                            {
                                isTemplateInvalid = true;
                                ModelState.AddModelError("", "Please fill all the columns in the General Skills' Matrix.");
                            }
                        }
                        
                        if (isTemplateInvalid)
                        {
                            _clonableSkillChannelTemplateService.PopulateDropDownsForTemplate(model, true);
                            return View(model);
                        }
                        if (await _clonableSkillChannelTemplateService.UpdateClonableSkillChannel(model, emailId))
                        {
                            TempData["SuccessErrorCloneTemplateMessage"] = $"Clonable Skill Channel Template '{model.TemplateName}' updated successfully!";
                            //return RedirectToAction("GetAllTemplates");
                            return RedirectToAction("Index", "Template");
                        }
                        else
                        {
                            TempData["SuccessErrorCloneTemplateMessage"] = $"Failed! Clonable Skill Channel '{model.TemplateName}' could not be updated!";
                            //return RedirectToAction("GetAllTemplates");
                            return RedirectToAction("Index", "Template");
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
                TempData["SuccessErrorCloneTemplateMessage"] = $"Failed! Clonable Skill Channel '{model.TemplateName}' could not be updated!";
                //return RedirectToAction("GetAllTemplates");
                return RedirectToAction("Index", "Template");
            }
        }

        [HttpPost]
        public JsonResult FetchTemplateTechSkillDetails(int id)
        {
            try
            {
                var transTechSkills = _clonableSkillChannelTemplateService.GetClonableTechSkillsbyId(id);
                return Json(transTechSkills);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [HttpPost]
        public JsonResult FetchTemplateInterviewersDetails(int id)
        {
            try
            {
                var interviewers = _clonableSkillChannelTemplateService.GetClonableIRFTransInterviewer(id);
                return Json(interviewers);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
    }
}
