using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using Risebird.Web.Controllers.AzureMediaservice;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        private IMeetingService _meetingService;
        private IIRFRepository _iRFRepository;
        private IConfiguration _configuration;


        public InterviewerController(IInterviewerService interviewerService, IClientService clientService, IApplicationErrorLogger applicationErrorLogger, IAzureStorageHelper azureStorageHelper,
                                ISkillService skillService, IProfileService profileService, IInterviewerRepository interviewerRepository, IMeetingService meetingService, IIRFRepository iRFRepository, IConfiguration configuration)

        {
            _interviewerService = interviewerService;
            _clientService = clientService;
            _applicationErrorLogger = applicationErrorLogger;
            _azureStorageHelper = azureStorageHelper;
            _skillService = skillService;
            _profileService = profileService;
            _interviewerRepository = interviewerRepository;
            _meetingService = meetingService;
            _iRFRepository = iRFRepository;
            _configuration = configuration;

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
                        TempData["MessageInterviewer"] = "Edit Interviewer Successful";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["MessageInterviewer"] = " Edit Interviewer is Failed";
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

            ViewBag.Years = new SelectList(Enumerable.Range(DateTime.Today.Year - 50, 51).Select(x => new SelectListItem()
            { Text = x.ToString(), Value = x.ToString() }), "Value", "Text");

            //=========Skills============

            model.SkillTypeList = new SelectList(_skillService.GetSkills(), "id", "Name");

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

        public async Task<IActionResult> DeleteInterviewerProfile(int UserId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {

                    if (_interviewerService.DeleteInterviewerProfile(UserId).Result)
                    {
                        TempData["InterviewerEmployerMessage"] = "Successfully Deleted";
                        //return RedirectToAction("Index");
                        return Json(new { url = Url.Action("Index", "Interviewer") });
                    }
                    else
                    {
                        TempData["InterviewerEmployerMessage"] = "Failed to Delete";
                        return Json(new { url = Url.Action("Index", "Interviewer") });
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
                TempData["InterviewerEmployerMessage"] = "Failed to Delete";
                return Json(new { url = Url.Action("Index", "Interviewer") });
            }
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
        public async Task<IActionResult> IndexEmployer(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    List<InterviewerProfessional> EmployerList = _interviewerRepository.GetEmployerListById(id).Result;
                    GetAllEmployerSelectList();

                    if (TempData["InterviewerEmployerMessage"] != null)
                    {
                        ViewBag.Message = TempData["InterviewerEmployerMessage"].ToString();
                        TempData.Remove("InterviewerEmployerMessage");
                    }
                    ViewBag.IProfileId = id;

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
        public async Task<IActionResult> AddEmployerDetails(int IProfileId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    InterviewerProfessional model = new InterviewerProfessional();
                    model.InterviewerProfileId = IProfileId;
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
                return RedirectToAction("IndexEmployer", new { id = IProfileId });
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

                    if (_interviewerService.AddEmployerProfile(interviewerProfessional).Result)
                    {
                        TempData["InterviewerEmployerMessage"] = "Adding Interviewer Employer Details is Successful";
                        return RedirectToAction("IndexEmployer", new { id = interviewerProfessional.InterviewerProfileId });
                    }
                    else
                    {
                        TempData["InterviewerEmployerMessage"] = "Adding Interviewer Employer Details is failed";
                        return RedirectToAction("IndexEmployer", new { id = interviewerProfessional.InterviewerProfileId });
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
                return RedirectToAction("IndexEmployer", new { id = interviewerProfessional.InterviewerProfileId });
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
        public async Task<IActionResult> EditEmployerDetails(int id, int IProfileId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    InterviewerProfessional ModelProfessional = _interviewerService.GetEmployerById(id);
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
                return RedirectToAction("IndexEmployer", new { id = IProfileId });
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> EditEmployerDetails(InterviewerProfessional interviewerProfessional)
        {
            try
            {
                //int UserId = Convert.ToInt32(TempData["UserId"]);

                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (_interviewerService.EditEmployerProfile(interviewerProfessional).Result)
                    {
                        TempData["InterviewerEmployerMessage"] = "Editing Interviewer Employer Details is Successful";
                        return RedirectToAction("IndexEmployer", new { id = interviewerProfessional.InterviewerProfileId });
                    }
                    else
                    {
                        TempData["InterviewerEmployerMessage"] = "Editing Interviewer Employer Details is failed";
                        return RedirectToAction("IndexEmployer", new { id = interviewerProfessional.InterviewerProfileId });
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
                TempData["InterviewerEmployerMessage"] = "Adding Interviewer Employer Details is failed";
                return RedirectToAction("IndexEmployer", new { id = interviewerProfessional.InterviewerProfileId });
            }
        }


        public async Task<IActionResult> DeleteEmployerDetails(int id, int IProfileId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {

                    if (_interviewerService.DeleteEmployerProfile(id).Result)
                    {
                        //TempData["InterviewerEmployerMessage"] = "Successfully Deleted";
                        return Json(new { url = Url.Action("IndexEmployer", "Interviewer", new { id = IProfileId }) });
                        //return RedirectToAction("IndexEmployer", new { id = IProfileId });
                    }
                    else
                    {
                        //TempData["InterviewerEmployerMessage"] = "Failed to Delete";
                        return Json(new { url = Url.Action("IndexEmployer", "Interviewer", new { id = IProfileId }) });
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
                //TempData["InterviewerEmployerMessage"] = "Failed to Delete";
                return Json(new { url = Url.Action("IndexEmployer", "Interviewer", new { id = IProfileId }) });
            }
        }



        #endregion

        #region Interviewer My Profile

        [HttpGet]
        public async Task<IActionResult> MyProfile()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Interviewer")
                    {
                        InterviewerProfileVM model = _interviewerService.GetInterviewerProfileByEmailId(emailid, profileName);
                        if (model == null)
                        {
                            TempData["MessageInterviewer"] = "No Information available in the System";
                            return RedirectToAction("Index");
                        }
                        // Get all the select Lists
                        GetAllSelectList(model);

                        return View("MyProfile", model);
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
                TempData["MessageInterviewer"] = "Failed";
                return RedirectToAction("Index");
            }
        }
        [HttpPost]
        public async Task<IActionResult> MyProfile(InterviewerProfileVM interviewerProfile)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    string uploadedFileURL = string.Empty;

                    if (interviewerProfile.ResumeFile != null)
                    {
                        await _azureStorageHelper.InsertBlob(interviewerProfile.ResumeFile.FileName, "resumes", interviewerProfile.ResumeFile);
                        var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                        var blob = containerRef.GetBlockBlobReference(interviewerProfile.ResumeFile.FileName);
                        uploadedFileURL = blob.Uri.ToString();
                        interviewerProfile.ResumeLink = uploadedFileURL;

                    }

                    if (_interviewerService.AddInterviewerMyProfile(interviewerProfile).Result)
                    {
                        TempData["MessageInterviewer"] = "My Profile is added Successfully";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["MessageInterviewer"] = "My Profile addition is Failed";
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

        #region unapproved interviewer

        [Authorize]
        [HttpGet]
        public IActionResult OpenInterviewerDiscussion()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Interviewer")
                    {
                        var invitesList = _interviewerService.GetInterviewerInvites(emailid)?.ToList();
                        return View(invitesList);
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
                return RedirectToAction("MyProfile");
            }

        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MyScheduledDiscussions()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Interviewer")
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        var myInterview = _interviewerService.GetUnapprovedInterviewerScheduledInterviews(emailid);

                        myInterview = myInterview.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); x.JobLevelDesc = ((JobLevels)Convert.ToInt32(x.JobLevel)).GetDescription(); return x; }).ToList();
                        foreach (var item in myInterview)
                        {
                            if (!string.IsNullOrEmpty(item.resumeLink))
                            {
                                try
                                {
                                    item.resumeLink = new Uri(item.resumeLink, UriKind.Absolute).ToString();

                                }
                                catch (Exception)
                                {
                                    item.resumeLink = "";
                                }
                            }
                            else
                            {
                                item.resumeLink = "";
                            }

                            if (item.InterviewStatus.HasValue)
                            {
                                item.InterviewStatusDesc = ((InterviewStatus)(item.InterviewStatus)).GetDescription();
                            }
                            if (item.MeetingStatus.HasValue)
                            {
                                item.MeetingStatusDesc = ((MeetingStatus)(item.MeetingStatus)).GetDescription();

                            }
                        }



                        return View(myInterview);
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
                return null;
            }
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> CancelInterview(int id, DateTime interviewDate, string intervieweeEmail)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var email = HttpContext.Session.GetString("email");
                    email = email.Replace("\"", "");
                    if (profileName == "Interviewer")
                    {
                        if (_meetingService.CancelInterview(id, interviewDate.ToString("dd MMM yyyy"), intervieweeEmail))
                        {
                            return Json(new { url = Url.Action("MyScheduledDiscussions", "Interviewer") });
                        }
                        else
                        {
                            return Json(new { url = Url.Action("MyScheduledDiscussions", "Interviewer") });
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
                return RedirectToAction("MyScheduledDiscussions");
            }
        }
        #endregion

        #region lead interviewer
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> OpenInterviewerAssessment()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Interviewer")
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        var meetings = _interviewerService.GetOpenInterviewerAssessment(emailid);
                        meetings = meetings.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();


                        if (TempData["SuccessErrorMessageLeadInterviewer"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageLeadInterviewer"].ToString();
                            TempData.Remove("SuccessErrorMessageLeadInterviewer");
                        }
                        return View(meetings);
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
                return null;
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MyInterviewerAssessment()
        {
            //add report
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Interviewer")
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        var myInterview = _interviewerService.GetAssignedInterviewerAssessment(emailid);
                        myInterview = myInterview.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();
                        foreach (var item in myInterview)
                        {
                            if (!string.IsNullOrEmpty(item.resumeLink))
                            {
                                try
                                {
                                    item.resumeLink = new Uri(item.resumeLink, UriKind.Absolute).ToString();

                                }
                                catch (Exception)
                                {
                                    item.resumeLink = "";
                                }
                            }
                            else
                            {
                                item.resumeLink = "";
                            }

                            if (item.InterviewStatus.HasValue)
                            {
                                item.InterviewStatusDesc = ((InterviewStatus)(item.InterviewStatus)).GetDescription();
                            }
                            if (item.MeetingStatus.HasValue)
                            {
                                item.MeetingStatusDesc = ((MeetingStatus)(item.MeetingStatus)).GetDescription();

                            }
                        }

                        if (TempData["SuccessErrorMessageLeadInterviewer"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageLeadInterviewer"].ToString();
                            TempData.Remove("SuccessErrorMessageLeadInterviewer");
                        }
                        return View(myInterview);
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
                return null;
            }
        }

        [Authorize]
        //[HttpPost]
        public async Task<IActionResult> AssignYourself(int id, DateTime InterviewDate)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Interviewer")
                    {
                        var result = _interviewerService.AssignYourself(id, emailid.ToLower(), InterviewDate);
                        TempData["SuccessErrorMessageLeadInterviewer"] = result;
                        ViewBag.Message = result;
                        return RedirectToAction("OpenInterviewerAssessment");
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
                return RedirectToAction("OpenInterviewerAssessment");
            }
        }
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> UnassignYourself(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var email = HttpContext.Session.GetString("email");
                    email = email.Replace("\"", "");
                    if (profileName == "Interviewer")

                    {
                        if (_interviewerService.UnassignYourself(id, email.ToLower()))
                        {
                            //TempData["SuccessErrorMessageLeadInterviewer"] = "Unassigned Successfully";
                            return Json(new { url = Url.Action("MyInterviewerAssessment", "Interviewer") });
                        }
                        else
                        {
                            //TempData["SuccessErrorMessageLeadInterviewer"] = "Unassignment has Failed";
                            return Json(new { url = Url.Action("MyInterviewerAssessment", "Interviewer") });
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
                TempData["SuccessErrorMessageLeadInterviewer"] = "Unassignment is Failed";
                return RedirectToAction("MyInterviewerAssessment");
            }
        }
        #endregion
        public IActionResult AddReport(int interviewId, int irfId, bool isClient = false, bool isClientSelected = false, bool isQC = false)
        {
            try
            {
                string userMenus = HttpContext.Session.GetString("userMenus");
                if (userMenus == null)
                {
                    return RedirectToAction("Index", "Home", new { url = "AddReport/Interviewer/" + interviewId + "/" + irfId + "/" + isClient + "/" + isClientSelected });
                }
                string profileName = HttpContext.Session.GetString("profileName");

                if (profileName != null)
                {
                    profileName = profileName.Replace("\"", "");
                }
                // var model = _meetingService.GetInteviewReport(interviewId, irfId).Result;
                var model = _interviewerService.GetInterviewerinteviewReport(interviewId, irfId).Result;
                if (model == null)
                {
                    model = new InterviewReport();
                    if (profileName == "Admin" || profileName == "HR")
                    {
                        return RedirectToAction("Index");
                    }
                    else if (profileName != null && (profileName == "Interviewer" || profileName == "Quality Controller"))
                    {
                        model.IsInterviewer = true;
                        if (isQC)
                        {
                            return RedirectToAction("MyQualityControlledInterviews");
                        }
                        else
                        {
                            return RedirectToAction("MyInterviewerAssessment");
                        }
                    }
                }
                //if (model.QuestionsCollection == null)
                //{
                //    ViewBag.QuestionsDiscussedCount = 0;
                //}
                //else
                //{
                //    ViewBag.QuestionsDiscussedCount = model.QuestionsCollection?.Count();
                //}
                ViewBag.IsClient = isClient;
                ViewBag.IsClientSelected = isClientSelected;
                model.IsQC = isQC;
                GetStatusList(model, irfId);
                if (isClient || isClientSelected)
                {
                    return PartialView("ClientPartialReport", model);
                }
                else
                {
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                if (isQC)
                {
                    return RedirectToAction("MyQualityControlledInterviews");

                }
                else
                {
                    return RedirectToAction("MyInterviewerAssessment");
                }
            }
        }
        [Authorize]
        [HttpPost]
        [DisableRequestSizeLimit]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReport(InterviewReport model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null &&
                    HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    profileName = "Interviewer";
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Interviewer" ||
                        profileName == "Quality Controller")
                    {
                        if (model.AudioFile != null)
                        {
                            await _azureStorageHelper.InsertBlob(model.AudioFile.FileName, "audios", model.AudioFile);
                            var containerRef = _azureStorageHelper.GetCloudBlobContainer("audios");
                            var blob = containerRef.GetBlockBlobReference(model.AudioFile.FileName);
                            string uploadedaudioFileURL = blob.Uri.ToString();
                            if (uploadedaudioFileURL == null)
                            {
                                ModelState.AddModelError(String.Empty, "Audio Upload is failed.");
                                GetStatusList(model);
                                return View("AddReport", model);
                            }
                            else
                            {
                                model.ThumbnailImage = uploadedaudioFileURL;
                            }
                        }
                        string uploadedvideoFileURL = string.Empty;

                        if (model.VideoFile != null)
                        {
                            AzureCall _call = new AzureCall(_configuration);
                            var resulttype = _call.CallApi(model.VideoFile, model.VideoFile.FileName, model.VideoFile.Length);
                            resulttype = resulttype.Replace("\"", "");
                            uploadedvideoFileURL = resulttype;

                            if (uploadedvideoFileURL == null)
                            {
                                ModelState.AddModelError(String.Empty, "Video Upload is failed.");
                                GetStatusList(model);
                                return View("AddReport", model);
                            }
                            else
                            {
                                model.VideoLink = uploadedvideoFileURL;
                            }
                        }
                        else if (model.PageVideoLink != null)
                        {
                            model.VideoLink = model.PageVideoLink;
                        }

                        if (_interviewerService.AddInterviewerReport(model).Result)
                        {
                            TempData["SuccessErrorMessageInterview"] = "Report saved Successfully";
                            if (profileName == "Admin" || profileName == "HR")
                            {
                                return RedirectToAction("Index");
                            }
                            else if (model.IsQC)
                            {
                                return RedirectToAction("MyQualityControlledInterviews");

                            }
                            else
                            {
                                return RedirectToAction("MyInterviewerAssessment");

                            }

                        }
                        else
                        {
                            TempData["SuccessErrorMessageInterview"] = "Report was not saved.";
                            if (profileName == "Admin" || profileName == "HR")
                            {
                                return RedirectToAction("Index");
                            }
                            else if (model.IsQC)
                            {
                                return RedirectToAction("MyQualityControlledInterviews");

                            }
                            else
                            {
                                return RedirectToAction("MyInterviewerAssessment");

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
                TempData["SuccessErrorMessageInterview"] = "Report was not saved.";
                if (model.IsQC)
                {
                    return RedirectToAction("MyQualityControlledInterviews");

                }
                return RedirectToAction("Index");
            }
        }
        private void GetStatusList(InterviewReport model, int irfId = 0)
        {
            var interviewStatuses = from InterviewStatus s in Enum.GetValues(typeof(InterviewStatus))
                                    select new { ID = (int)s, Name = s.GetDescription() };
            model.InterviewStatusList = new SelectList(interviewStatuses, "ID", "Name");

            var meetingStatuses = from MeetingStatus s in Enum.GetValues(typeof(MeetingStatus))
                                  select new { ID = (int)s, Name = s.GetDescription() };

            model.MeetingStatusList = new SelectList(meetingStatuses, "ID", "Name");

            model.Level = new SelectList(_iRFRepository.GetLevels(), "id", "Name");
            model.LevelTech = new SelectList(_iRFRepository.GetLevelTech(), "id", "Name");
            //var questionsLevel = from QuestionsLevel s in Enum.GetValues(typeof(QuestionsLevel))
            //                     select new { ID = (int)s, Name = s.GetDescription() };
            //model.QuestionsLevelList = new SelectList(questionsLevel, "ID", "Name");

            //model.QuestionsList = new SelectList(_interviewerService.GetTechSkillComments(irfId), "Comment", "Comment");

            model.SkillQualityList = new SelectList(_iRFRepository.GetSkillQuality(), "Id", "Name");
            model.CertificationQualityList = new SelectList(_iRFRepository.GetCertificationQuality(), "Id", "Name");
            model.EducationQualityList = new SelectList(_iRFRepository.GetEducationQuality(), "Id", "Name");
            model.EmployersQualityList = new SelectList(_iRFRepository.GetEmployersQuality(), "Id", "Name");
            model.CareerProgressionList = new SelectList(_iRFRepository.GetCareerProgression(), "Id", "Name");
            model.ProjectsQualityList = new SelectList(_iRFRepository.GetProjectsQuality(), "Id", "Name");
            model.OnlinePresenceList = new SelectList(_iRFRepository.GetOnlinePresence(), "Id", "Name");
            model.BehavioralScoreList = new SelectList(_iRFRepository.GetBehavioralScore(), "Id", "Name");
            model.InterviwerCommunicationList = new SelectList(_iRFRepository.GetInterviwerCommunication(), "Id", "Name");
            model.InterviewexperienceList = new SelectList(_iRFRepository.GetInterviewexperience(), "Id", "Name");

            model.CompanyStageList = new SelectList(_iRFRepository.GetCompanyStage(), "Id", "Name");
            model.SkillCategoryList = new SelectList(_iRFRepository.GetSkillCategory(), "Id", "Name");
            model.YearsofexperienceList = new SelectList(_iRFRepository.GetInterviewerExprienceYear(), "Id", "Name");
            model.SenioritylevelList = new SelectList(_iRFRepository.GetSenioritylevel(), "Id", "Name");
            model.SkillNatureList = new SelectList(_iRFRepository.GetSkillNature(), "Id", "Name");


        }

    }

}