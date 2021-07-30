using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;

namespace Risebird.Web.Controllers
{
    public class TemplateController : Controller
    {
        private readonly ITemplateService _templateService;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private IInterviewerService _interviewerService;
        private readonly IIRFService _irfService;


        public TemplateController(ITemplateService templateService, IApplicationErrorLogger applicationErrorLogger, IInterviewerService interviewerService, IIRFService irfService)
        {
            _templateService = templateService;
            _applicationErrorLogger = applicationErrorLogger;
            _interviewerService = interviewerService;
            _irfService = irfService;

        }
        public async Task<IActionResult> Index()
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
                        var model = new List<SkillChannelTemplateVM>();
                        model = (await _templateService.GetAllTemplates()).ToList();

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
                return RedirectToAction("SignOut", "Session");
            }
        }

        public async Task<IActionResult> Create()
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
                        var model = new SkillChannelTemplateVM();
                        SetCreateTemplateParameters(model);
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

        private void SetCreateTemplateParameters(SkillChannelTemplateVM model)
        {
            model.AssessmentGroupList = new SelectList(_interviewerService.FetchAssessmentGroups(), "id", "AssessmentGroup"); ;
            model.Skill = new SelectList(_irfService.GetSkills(), "id", "Name");
            model.LevelTech = new SelectList(_irfService.GetLevelTech(), "id", "Name");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SkillChannelTemplateVM templateDetails)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR")
                    {
                        //check duplicates for interviewer, emergency interviewer and quality controllers
                        bool isDuplicate = false;
                        var interviewerCollection = templateDetails.InterviewerCollection.Select(x => (x.UserId));
                        if (interviewerCollection.Count() != interviewerCollection.Distinct().Count())
                        {
                            //duplicates exist

                            isDuplicate = true;
                            ModelState.AddModelError("", "Please remove duplicate entries from Interviewers' list.");

                        }

                        var techSkillCollection = templateDetails.TechSkillCollection.Select(x => (x.SkillId));
                        if (techSkillCollection.Count() != techSkillCollection.Distinct().Count())
                        {
                            //duplicates exist
                            isDuplicate = true;
                            ModelState.AddModelError("", "Please remove duplicate entries from Tech Skills' list.");
                        }

                        if (isDuplicate)
                        {
                            SetCreateTemplateParameters(templateDetails);
                            return View(templateDetails);
                        }


                        if (ModelState.IsValid)
                        {
                            if (await _templateService.AddTemplate(templateDetails, emailId))
                            {
                                TempData["SuccessErrorMessageTemplate"] = "Template added Successfully";
                                return RedirectToAction("Index");
                            }
                            else
                            {
                                TempData["SuccessErrorMessage"] = "Failed to add Template";
                                return RedirectToAction("Index");
                            }
                        }
                        else
                        {
                            SetCreateTemplateParameters(templateDetails);
                            return View(templateDetails);
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
                TempData["SuccessErrorMessage"] = "Failed to add Template";
                return RedirectToAction("Index");
            }
        }

        //public async Task<IActionResult> GetInterviewersByAssessmentGroup(string assessmentGroupId)
        //{
        //    try
        //    {
        //        var model = await _irfService.GetInterviewersByAssessmentGroup(assessmentGroupId);
        //        return PartialView("TemplateInterviewersPartial", model);
        //    }
        //    catch (Exception ex)
        //    {
        //        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //        return null;
        //    }
        //}

        [Authorize]
        public async Task<string> GetInterviewerSkillsByUserId(int userId)
        {
            var name = await _irfService.GetInterviewerSkillsByUserId(userId);
            return name;
        }

        [AcceptVerbs("Get", "Post")]
        [AllowAnonymous]
        public async Task<IActionResult> IsTemplateNameInUse(string templateName)
        {
            bool inUse = await _templateService.IsTemplateNameInUse(templateName.Replace("\r", "").Replace("\n", ""));

            if (!inUse)
            {
                return Json(true);
            }
            else
            {
                return Json($"Template Name '{templateName}' is already in use.");
            }
        }
    }
}
