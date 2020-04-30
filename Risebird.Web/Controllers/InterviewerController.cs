using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
using Microsoft.AspNetCore.Authorization;

namespace Risebird.Web.Controllers
{
    public class InterviewerController : Controller
    {
        private IInterviewerService _interviewerService;
        private IClientService _clientService;
        private IApplicationErrorLogger _applicationErrorLogger;
        private IAzureStorageHelper _azureStorageHelper;
        private ISkillService _skillService;
        private IProfileService _profileService;
        private IInterviewerRepository _interviewerRepository;
        public InterviewerController(IInterviewerService interviewerService, IClientService clientService, IApplicationErrorLogger applicationErrorLogger, IAzureStorageHelper azureStorageHelper,
                                ISkillService skillService, IProfileService profileService, IInterviewerRepository interviewerRepository)

        {
            _interviewerService = interviewerService;
            _clientService = clientService;
            _applicationErrorLogger = applicationErrorLogger;
            _azureStorageHelper = azureStorageHelper;
            _skillService = skillService;
            _profileService = profileService;
            _interviewerRepository = interviewerRepository;
        }



        #region Interviewer Profile functions

        public IActionResult Index()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (TempData["MessageInterviewer"] != null)
                    {
                        ViewBag.Message = TempData["MessageInterviewer"].ToString();
                        TempData.Remove("MessageInterviewer");

                    }

                    return View(_interviewerService.GetInterviewers());
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
                return RedirectToAction("Index");
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
                        ModelState.AddModelError(string.Empty, "Please add a Resume File");
                        InterviewerProfileVM model = new InterviewerProfileVM();
                        // Get all the select Lists
                        GetAllSelectList(model);
                        return View("AddInterviewerProfile", model);
                    }

                    if (_interviewerService.AddInterviewerProfile(interviewerProfile).Result)
                    {
                        TempData["MessageInterviewer"] = "Interviewer Profile is added Successfully";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["MessageInterviewer"] = "Interviewer Profile addition is Failed";
                        return RedirectToAction("Index");
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
                TempData["MessageInterviewer"] = "Interviewer Profile addition is Failed";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditInterviewerProfile(int userId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    InterviewerProfileVM model = _interviewerService.GetInterviewerProfileById(userId);
                    // Get all the select Lists
                    GetAllSelectList(model);

                    return View("EditInterviewerProfile", model);
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

                    if (_interviewerService.EditInterviewerProfile(interviewerProfile).Result)
                    {
                        TempData["MessageInterviewer"] = "Interviewer is added Successfully";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["MessageInterviewer"] = "Interviewer addition is Failed while adding to the System";
                        return RedirectToAction("Index");
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
                TempData["MessageInterviewer"] = "Interviewer Profile Edit is Failed";
                return RedirectToAction("Index");
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
            model.LevelList = new SelectList(_interviewerService.GetLevelTech(), "id", "Name");
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
            IEnumerable<string> NickNamesList = _interviewerService.GetNickNames(UserId);
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
            IEnumerable<string> EmailIdList = _interviewerService.GetEmailIds(UserId);
            if (EmailIdList.Contains(EmailId))
            {
                return Json(false, new Newtonsoft.Json.JsonSerializerSettings());
            }
            else
                return Json(true, new Newtonsoft.Json.JsonSerializerSettings());
        }

        #endregion

        #region Interviewer Employer functions

        [HttpGet]
        public async Task<IActionResult> AddInterviewerProfile_EmployerDetails(int UserId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    List<InterviewerProfessional> EmployerList = _interviewerRepository.GetEmployerListById(UserId).Result;
                    if (TempData["InterviewerEmployerMessage"] != null)
                    {
                        ViewBag.Message = TempData["InterviewerEmployerMessage"].ToString();
                        TempData.Remove("InterviewerEmployerMessage");
                    }
                    ViewBag.UserId = UserId;
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
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> AddEmployerDetails(int ProfileId, int UserId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    InterviewerProfessional model = new InterviewerProfessional();
                    model.InterviewerProfileId = ProfileId;
                    ViewBag.UserId = UserId;
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
                return RedirectToAction("AddInterviewerProfile_EmployerDetails", new { UserId });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEmployerDetails(InterviewerProfessional interviewerProfessional)
        {

            try
            {
                int UserId = Convert.ToInt32(TempData["UserId"]);
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {

                    if (_interviewerService.AddEmployerProfile(interviewerProfessional).Result)
                    {
                        TempData["InterviewerEmployerMessage"] = "Adding Interviewer Employer Details is Successfully";
                        return RedirectToAction("AddInterviewerProfile_EmployerDetails", new { UserId });
                    }
                    else
                    {
                        TempData["InterviewerEmployerMessage"] = "Adding Interviewer Employer Details is failed";
                        return RedirectToAction("AddInterviewerProfile_EmployerDetails", new { UserId });
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
        private void GetAllEmployerSelectList()
        {
            ViewBag.ExperienceRange = new SelectList(_interviewerService.GetExperienceRange(), "id", "Description");
            ViewBag.JobFunction = new SelectList(_interviewerService.GetJobFunction(), "id", "Description");
            ViewBag.Industry = new SelectList(_interviewerService.GetIndustry(), "id", "Description");

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
        public async Task<IActionResult> EditEmployerDetails(int Id, int UserId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    InterviewerProfessional ModelProfessional = _interviewerService.GetEmployerById(Id);
                    // Get all the select Lists
                    GetAllEmployerSelectList();
                    ViewBag.UserId = UserId;
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
                return RedirectToAction("AddInterviewerProfile_EmployerDetails", new { UserId });
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> EditEmployerDetails(InterviewerProfessional interviewerProfessional)
        {
            try
            {
                int UserId = Convert.ToInt32(TempData["UserId"]);

                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (_interviewerService.EditEmployerProfile(interviewerProfessional).Result)
                    {
                        TempData["InterviewerEmployerMessage"] = "Editing Interviewer Employer Details is failed";
                        return RedirectToAction("AddInterviewerProfile_EmployerDetails", new { UserId });
                    }
                    else
                    {
                        TempData["InterviewerEmployerMessage"] = "Editing Interviewer Employer Details is failed";
                        return RedirectToAction("AddInterviewerProfile_EmployerDetails", new { UserId });
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

        

        [HttpDelete]
        public async Task<IActionResult> DeleteEmployerDetails(int Id, int UserId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (_interviewerService.DeleteEmployerProfile(Id).Result)
                    {
                        TempData["InterviewerEmployerMessage"] = "Successfully Deleted";
                        return RedirectToAction("AddInterviewerProfile_EmployerDetails", new { UserId });
                    }
                    else
                    {
                        TempData["InterviewerEmployerMessage"] = "Failed to Delete";
                        return RedirectToAction("AddInterviewerProfile_EmployerDetails", new { UserId });
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



        #endregion

        #region Interviewer My Profile

        [HttpGet]
        public async Task<IActionResult> AddInterviewerMyProfileIndex(int UserId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {

                    //InterviewerProfileVM model = new InterviewerProfileVM();
                    InterviewerProfileVM model = _interviewerService.GetInterviewerMyProfileById(UserId);
                    // Get all the select Lists
                    GetAllSelectList(model);

                    return View("AddInterviewerMyProfileIndex", model);
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
        public async Task<IActionResult> AddInterviewerMyProfile(InterviewerProfileVM interviewerProfile)
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
                    }

                    if (_interviewerService.AddInterviewerMyProfile(interviewerProfile).Result)
                    {
                        TempData["MessageInterviewer"] = "Interviewer My Profile is added Successfully";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["MessageInterviewer"] = "Interviewer My Profile addition is Failed while adding to the System";
                        return RedirectToAction("Index");
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
                TempData["SuccessErrorMessageInterviewer"] = "Interviewer My Profile addition is Failed";
                return RedirectToAction("Index");
            }
        }

        #endregion
    }

}