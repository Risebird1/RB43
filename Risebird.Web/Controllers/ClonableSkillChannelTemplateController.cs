using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Linq;
using System.Threading.Tasks;

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
                        if (profileName == "Admin" && model.ClonableTemplateQuestionsCollection != null && model.ClonableTemplateQuestionsCollection.Count() > 500)
                        {
                            model.ClonableTemplateQuestionsCollection = model.ClonableTemplateQuestionsCollection.Take(500).ToList();

                        }
                        if (profileName == "Admin" && model.InterviewerCollection != null && model.InterviewerCollection.Count() > 100)
                        {
                            model.InterviewerCollection = model.InterviewerCollection.Take(100).ToList();
                        }
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
                        //if (model.ClonableTemplateQuestionsCollection !=null  && model.ClonableTemplateQuestionsCollection.Count() !=0 && !model.ClonableTemplateQuestionsCollection.Any(x => x.IsSelected))
                        //{
                        //    isTemplateInvalid = true;
                        //    ModelState.AddModelError("", "Please select atleast one Question from the list.");
                        //}
                        //if (model.Selectability.Any(x => string.IsNullOrEmpty(x.Value)))
                        //{
                        //    isTemplateInvalid = true;
                        //    ModelState.AddModelError("", "Please fill all the columns in the Selectability Matrix.");
                        //}
                        //if (model.Joinability.Any(x => string.IsNullOrEmpty(x.Value)))
                        //{
                        //    isTemplateInvalid = true;
                        //    ModelState.AddModelError("", "Please fill all the columns in the Joinability Matrix.");
                        //}
                        //if (model.SkillGeneral.Any(x => string.IsNullOrEmpty(x.Value)))
                        //{
                        //    isTemplateInvalid = true;
                        //    ModelState.AddModelError("", "Please fill all the columns in the General Skills' Matrix.");
                        //}
                        if (isTemplateInvalid)
                        {
                            _clonableSkillChannelTemplateService.PopulateDropDownsForTemplate(model);
                            return View(model);
                        }
                        if (await _clonableSkillChannelTemplateService.CreateClonableSkillChannel(model, emailId))
                        {
                            TempData["SuccessErrorCloneSkillChannnelMessage"] = $"Clonable Skill Channel Template '{model.TemplateName}' created successfully!";
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
                        if (profileName == "Admin" && model.ClonableTemplateQuestionsCollection != null && model.ClonableTemplateQuestionsCollection.Count() > 500)
                        {
                            model.ClonableTemplateQuestionsCollection = model.ClonableTemplateQuestionsCollection.Take(500).ToList();

                        }
                        if (profileName == "Admin" && model.InterviewerCollection != null && model.InterviewerCollection.Count() > 100)
                        {
                            model.InterviewerCollection = model.InterviewerCollection.Take(100).ToList();
                        }

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
                        //if (!model.ClonableTemplateQuestionsCollection.Any(x => x.IsSelected))
                        //{
                        //    isTemplateInvalid = true;
                        //    ModelState.AddModelError("", "Please select atleast one Question from the list.");
                        //}

                        //if(model.IsTemplateJoinSelectGenskillsRequired)
                        //{
                        //    if (model.Selectability.Any(x => string.IsNullOrEmpty(x.Value)))
                        //    {
                        //        isTemplateInvalid = true;
                        //        ModelState.AddModelError("", "Please fill all the columns in the Selectability Matrix.");
                        //    }
                        //    if (model.Joinability.Any(x => string.IsNullOrEmpty(x.Value)))
                        //    {
                        //        isTemplateInvalid = true;
                        //        ModelState.AddModelError("", "Please fill all the columns in the Joinability Matrix.");
                        //    }
                        //    if (model.SkillGeneral.Any(x => string.IsNullOrEmpty(x.Value)))
                        //    {
                        //        isTemplateInvalid = true;
                        //        ModelState.AddModelError("", "Please fill all the columns in the General Skills' Matrix.");
                        //    }
                        //}

                        if (isTemplateInvalid)
                        {
                            _clonableSkillChannelTemplateService.PopulateDropDownsForTemplate(model, true);
                            return View(model);
                        }
                        if (await _clonableSkillChannelTemplateService.UpdateClonableSkillChannel(model, emailId))
                        {
                            TempData["SuccessErrorCloneTemplateMessage"] = $"Clonable Skill Channel Template '{model.TemplateName}' updated successfully!";
                            return RedirectToAction("Index", "Template");
                        }
                        else
                        {
                            TempData["SuccessErrorCloneTemplateMessage"] = $"Failed! Clonable Skill Channel '{model.TemplateName}' could not be updated!";
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
        public IActionResult FetchTemplateTechSkillDetails(int id)
        {
            try
            {
                var transTechSkills = _clonableSkillChannelTemplateService.GetClonableTechSkillsbyId(id);
                return PartialView(transTechSkills);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("Sorry! Failed to retrive Data.");
            }
        }

        [HttpPost]
        public IActionResult FetchTemplateInterviewersDetails(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var interviewers = _clonableSkillChannelTemplateService.GetClonableIRFTransInterviewer(id);
                    ViewBag.Profile = profileName;
                    return PartialView(interviewers);
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }

            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("Sorry! Failed to retrive Data.");
            }
        }
    }
}
