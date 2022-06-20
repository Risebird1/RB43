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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Web.Controllers
{
    [Authorize]
    public class TemplateController : Controller
    {
        private readonly ITemplateService _templateService;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private IInterviewerService _interviewerService;
        private readonly IIRFService _irfService;
        private readonly IClientService _clientService;
        private readonly IClonableSkillChannelTemplateService _clonableSkillChannelTemplateService;

        public TemplateController(ITemplateService templateService, IApplicationErrorLogger applicationErrorLogger, IInterviewerService interviewerService, IIRFService irfService, IClonableSkillChannelTemplateService clonableSkillChannelTemplateService, IClientService clientService)
        {
            _templateService = templateService;
            _applicationErrorLogger = applicationErrorLogger;
            _interviewerService = interviewerService;
            _irfService = irfService;
            _clonableSkillChannelTemplateService = clonableSkillChannelTemplateService;
            _clientService = clientService;
        }

        //[Authorize]
        //[HttpGet]
        //public async Task<IActionResult> Index()
        //{
        //    try
        //    {
        //        if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
        //        {
        //            var profileName = HttpContext.Session.GetString("profileName")?.Trim();
        //            profileName = profileName.Replace("\"", "");
        //            var emailId = HttpContext.Session.GetString("email")?.Trim();
        //            emailId = emailId.Replace("\"", "");
        //            if (profileName == "Admin" || profileName == "HR")
        //            {
        //                var model = new TemplatesListVM();
        //                model.TemplateList = (await _templateService.GetAllTemplates(emailId/*, DateTime.Now.AddDays(-30), DateTime.Now*/)).ToList();
        //                model.Profile = profileName;

        //                if (HttpContext.Session.GetString("TemplateFilters") != null)
        //                {
        //                    var filter = JsonConvert.DeserializeObject<TemplateFilter>(HttpContext.Session.GetString("TemplateFilters"));

        //                    filter.AssessmentGroup = filter.AssessmentGroup == null ? model.TemplateList.Select(x => x.AssessmentGroupName).ToArray() : filter.AssessmentGroup;
        //                    model.TemplateList = model.TemplateList.Where(c => filter.AssessmentGroup.Contains(c.AssessmentGroupName)).ToList();
        //                }

        //                foreach (var item in model.TemplateList)
        //                {
        //                    if (item.IsDeleted)
        //                    {
        //                        item.ImageUrl = "/img/Cards_Png/01_Black-White.png";
        //                        item.Status = "Archived";
        //                    }
        //                    else
        //                    {
        //                        item.ImageUrl = "/img/Cards_Png/01_Green-White.png";
        //                        item.Status = "Active";
        //                    }
        //                }
        //                if (TempData["SuccessErrorMessageTemplate"] != null)
        //                {
        //                    model.Message = TempData["SuccessErrorMessageTemplate"].ToString();
        //                    TempData.Remove("SuccessErrorMessageTemplate");
        //                }
        //                return View(model);

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
        //        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //        return RedirectToAction("SignOut", "Session");
        //    }
        //}

        //[Authorize]
        //[HttpPost]
        //public async Task<IActionResult> Index(TemplateFilter filter)
        //{
        //    if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
        //    {
        //        if (filter != null)
        //        {
        //            HttpContext.Session.SetString("TemplateFilters", JsonConvert.SerializeObject(filter));

        //        }
        //        var profileName = HttpContext.Session.GetString("profileName")?.Trim();
        //        profileName = profileName.Replace("\"", "");
        //        var emailId = HttpContext.Session.GetString("email")?.Trim();
        //        emailId = emailId.Replace("\"", "");
        //        if (profileName == "Admin" || profileName == "HR")
        //        {

        //            var model = new TemplatesListVM();
        //            model.TemplateList = (await _templateService.GetAllTemplates(emailId)).ToList();
        //            model.Profile = profileName;

        //            filter.AssessmentGroup = filter.AssessmentGroup == null ?  model.TemplateList.Select(x => x.AssessmentGroupName).ToArray() : filter.AssessmentGroup;
        //            model.TemplateList = model.TemplateList.Where(c => filter.AssessmentGroup.Contains(c.AssessmentGroupName)).ToList();
        //            foreach (var item in model.TemplateList)
        //            {
        //                if (item.IsDeleted)
        //                {
        //                    item.ImageUrl = "/img/Cards_Png/01_Black-White.png";
        //                    item.Status = "Archived";
        //                }
        //                else
        //                {
        //                    item.ImageUrl = "/img/Cards_Png/01_Green-White.png";
        //                    item.Status = "Active";
        //                }
        //            }

        //            return View(model);

        //        }
        //        else
        //        {
        //            return RedirectToAction("SignOut", "Session");
        //        }
        //    }
        //    else
        //    {
        //        return RedirectToAction("SignOut", "Session");
        //    }
        //}

        [HttpGet]
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
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "Client")
                    {
                        ClonableTemplateListVM model = new ClonableTemplateListVM();
                        model.LoggedInUser = emailid;
                        if (profileName == "Client")
                        {
                            model.ClientId = await _clientService.GetClientByEmailId(emailid);
                            if (model.ClientId.HasValue)
                            {
                                model.ClientCompanyName = _clientService.GetClient(model.ClientId.Value).Name;
                            }
                            else
                            {
                                model.ClientId = null;
                            }
                        }
                        model.ClonableTemplates = await _clonableSkillChannelTemplateService.GetAllClonableSkillChannels();


                        if (HttpContext.Session.GetString("ClonableTemplatesFilters") != null)
                        {
                            var filter = JsonConvert.DeserializeObject<ClonableTemplatesFilter>(HttpContext.Session.GetString("ClonableTemplatesFilters"));

                            //filter.TemplateName = filter.TemplateName == null ? model.ClonableTemplates.Select(x => x.Name).ToArray() : filter.TemplateName;
                            //filter.AssessmentGroup = filter.AssessmentGroup ?? model.ClonableTemplates.Select(x => x.AssessmentGroupName).ToArray();
                            //filter.CompetencyLevel = filter.CompetencyLevel ?? model.ClonableTemplates.Select(x => x.CompetencyLevelDesc).ToArray();
                            //filter.CompanyType = filter.CompanyType ?? model.ClonableTemplates.Select(x => x.CompanyTypeDesc).ToArray();
                            //filter.CountryType = filter.CountryType ?? model.ClonableTemplates.Select(x => x.CountryTypeDesc).ToArray();
                            //filter.SeniorityLevel = filter.SeniorityLevel ?? model.ClonableTemplates.Select(x => x.SeniorityLevelDesc).ToArray();

                            
                            if(model.ClonableTemplates != null  && model.ClonableTemplates.Count()>0)
                            {
                                bool continueApplyingFilter = true;

                                //if (filter.AssessmentGroup != null )
                                //{
                                //    IEnumerable<ClonableSkillChannelVM> templatesFilteredByAssessmentGroup = null;

                                //    templatesFilteredByAssessmentGroup = model.ClonableTemplates.Where(c => filter.AssessmentGroup.Contains(c.AssessmentGroupName));
                                //    if(templatesFilteredByAssessmentGroup != null)
                                //    {
                                //        model.ClonableTemplates = templatesFilteredByAssessmentGroup;
                                //    }
                                //}


                                if (!string.IsNullOrEmpty(filter.CountryType))
                                {
                                    IEnumerable<ClonableSkillChannelVM> templatesFilteredByCountryType = null;

                                    templatesFilteredByCountryType = model.ClonableTemplates.Where(c => c.CountryType.HasValue && c.CountryType.Value == Convert.ToInt16(filter.CountryType));
                                    model.ClonableTemplates = templatesFilteredByCountryType;
                                }


                                if (!string.IsNullOrEmpty(filter.CompanyType))
                                {
                                    IEnumerable<ClonableSkillChannelVM> templatesFilteredByCompanyType = null;

                                    templatesFilteredByCompanyType = model.ClonableTemplates.Where(c => c.CompanyTypeId.HasValue && c.CompanyTypeId.Value == Convert.ToInt16(filter.CompanyType));
                                    if(templatesFilteredByCompanyType != null && templatesFilteredByCompanyType.Count()>0)
                                    {
                                        model.ClonableTemplates = templatesFilteredByCompanyType;
                                    }
                                    else
                                    {
                                        continueApplyingFilter = false;
                                    }
                                }


                                if (!string.IsNullOrEmpty(filter.SeniorityLevel) && continueApplyingFilter)
                                {
                                    IEnumerable<ClonableSkillChannelVM> templatesFilteredBySeniority = null;

                                    templatesFilteredBySeniority = model.ClonableTemplates.Where(c => c.SeniorityLevelId != null && c.SeniorityLevelId.Contains(filter.SeniorityLevel));
                                    if (templatesFilteredBySeniority != null && templatesFilteredBySeniority.Count() > 0)
                                    {
                                        model.ClonableTemplates = templatesFilteredBySeniority;
                                    }
                                    else
                                    {
                                        continueApplyingFilter = false;
                                    }
                                }


                                if (!string.IsNullOrEmpty(filter.DifficultyLevel) && continueApplyingFilter)
                                {
                                    IEnumerable<ClonableSkillChannelVM> templatesFilteredByDifficulty = null;

                                    templatesFilteredByDifficulty = model.ClonableTemplates.Where(c => c.DifficultyLevel != null && c.DifficultyLevel.Contains(filter.DifficultyLevel));
                                    if (templatesFilteredByDifficulty != null && templatesFilteredByDifficulty.Count() > 0)
                                    {
                                        model.ClonableTemplates = templatesFilteredByDifficulty;
                                    }
                                    else
                                    {
                                        continueApplyingFilter = false;
                                    }
                                }


                                if (!string.IsNullOrEmpty(filter.EmploymentType) && continueApplyingFilter)
                                {
                                    IEnumerable<ClonableSkillChannelVM> templatesFilteredByEmploymentType = null;

                                    templatesFilteredByEmploymentType = model.ClonableTemplates.Where(c => c.EmploymentType != null && c.EmploymentType.Contains(filter.EmploymentType));
                                    if (templatesFilteredByEmploymentType != null && templatesFilteredByEmploymentType.Count() > 0)
                                    {
                                        model.ClonableTemplates = templatesFilteredByEmploymentType;
                                    }
                                }

                            }

                            model.ClonableTemplates = model.ClonableTemplates?.ToList();
                        }

                        if (TempData["SuccessErrorCloneTemplateMessage"] != null)
                        {
                            model.Message = TempData["SuccessErrorCloneTemplateMessage"].ToString();
                            TempData.Remove("SuccessErrorCloneTemplateMessage");
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
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Index(ClonableTemplatesFilter filter)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR")
                    {
                        if (filter != null)
                        {
                            HttpContext.Session.SetString("ClonableTemplatesFilters", JsonConvert.SerializeObject(filter));
                        }
                        return RedirectToAction("Index");
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



        [HttpGet]
        public async Task<IActionResult> ResetTemplateFilter()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("TemplateFilters");
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

        [HttpPost]
        public async Task<IActionResult> ResetClonableTemplateFilter()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("ClonableTemplatesFilters");
                    return Json(true);
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

        [Authorize]
        [HttpGet]
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
                        SetTemplateParameters(model);
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

        [HttpGet]
        private void SetTemplateParameters(SkillChannelTemplateVM model)
        {
            model.AssessmentGroupList = new SelectList(_interviewerService.FetchAssessmentGroups(), "id", "AssessmentGroup"); ;
            model.Skills = new SelectList(_irfService.GetSkills(), "id", "Name");
            model.LevelTech = new SelectList(_irfService.GetLevelTech(), "id", "Name");
        }

        [HttpGet]
        private void SetEditTemplateParameters(EditSkillChannelTemplateVM model)
        {
            model.AssessmentGroupList = new SelectList(_interviewerService.FetchAssessmentGroups(), "id", "AssessmentGroup"); ;
            model.Skills = new SelectList(_irfService.GetSkills(), "id", "Name");
            model.LevelTech = new SelectList(_irfService.GetLevelTech(), "id", "Name");
            model.InterviewerList = new SelectList(_irfService.GetInterviewers(model.AssessmentGroupId?.ToString()), "UserId", "EmailId");

        }

        [Authorize]
        [HttpGet]
        public SelectList GetInterviewers(string assessmentgroupid)
        {

            var list = new List<SelectListItem>();
            var interviewers = _irfService.GetInterviewers(assessmentgroupid);

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
                            SetTemplateParameters(templateDetails);
                            return View(templateDetails);
                        }


                        if (ModelState.IsValid)
                        {
                            if (await _templateService.AddTemplate(templateDetails, emailId))
                            {
                                TempData["SuccessErrorMessageTemplate"] = "Template has been added Successfully!";
                                return RedirectToAction("Index");
                            }
                            else
                            {
                                TempData["SuccessErrorMessageTemplate"] = "Failed to add Template!";
                                return RedirectToAction("Index");
                            }
                        }
                        else
                        {
                            SetTemplateParameters(templateDetails);
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
                TempData["SuccessErrorMessageTemplate"] = "Failed to add Template";
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
        [HttpGet]
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


        [Authorize]
        [HttpGet]
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
                    if (profileName == "Admin" || profileName == "HR")
                    {
                        var model = new EditSkillChannelTemplateVM();
                        model = await _templateService.GetTemplateById(id);
                        SetEditTemplateParameters(model);
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

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditSkillChannelTemplateVM templateDetails)
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
                        templateDetails.InterviewerCollection = templateDetails.InterviewerCollection.Where(x => x.UserId != null).ToList();
                        var interviewerCollection = templateDetails.InterviewerCollection.Select(x => (x.UserId));
                        if (interviewerCollection.Count() != interviewerCollection.Distinct().Count())
                        {
                            //duplicates exist

                            isDuplicate = true;
                            ModelState.AddModelError("", "Please remove duplicate entries from Interviewers' list.");

                        }

                        templateDetails.TechSkillCollection = templateDetails.TechSkillCollection.Where(x => x.LevelId != null).ToList();
                        var techSkillCollection = templateDetails.TechSkillCollection.Select(x => (x.SkillId));
                        if (techSkillCollection.Count() != techSkillCollection.Distinct().Count())
                        {
                            //duplicates exist
                            isDuplicate = true;
                            ModelState.AddModelError("", "Please remove duplicate entries from Tech Skills' list.");
                        }

                        if (isDuplicate)
                        {
                            SetEditTemplateParameters(templateDetails);
                            return View(templateDetails);
                        }


                        if (ModelState.IsValid)
                        {
                            if (await _templateService.UpdateTemplate(templateDetails, emailId))
                            {
                                TempData["SuccessErrorMessageTemplate"] = "Template has been updated Successfully!";
                                return RedirectToAction("Index");
                            }
                            else
                            {
                                TempData["SuccessErrorMessageTemplate"] = "Failed to update Template!";
                                return RedirectToAction("Index");
                            }
                        }
                        else
                        {
                            SetEditTemplateParameters(templateDetails);
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
                TempData["SuccessErrorMessageTemplate"] = "Failed to update Template";
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> DeleteTemplate(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR")
                    {
                        var result = await _templateService.DeleteTemplate(id);
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

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> RestoreTemplate(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR")
                    {
                        var result = await _templateService.RestoreTemplate(id);
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
    }
}
