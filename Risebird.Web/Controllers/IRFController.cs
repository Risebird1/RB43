using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using Risebird.CommonHelper;

namespace Risebird.Web.Controllers
{
    public class IRFController : Controller
    {
        private IIRFRepository _irfRepository;
        private IIRFService _irfService;
        private IAzureStorageHelper _azureStorageHelper;
        private IMeetingService _meetingService;
        private IApplicationErrorLogger _applicationErrorLogger;
        public IRFController(IIRFRepository iRFRepository, IApplicationErrorLogger applicationErrorLogger, IIRFService irfService, IMeetingService meetingService, IAzureStorageHelper azureStorageHelper)
        {
            _irfRepository = iRFRepository;
            _applicationErrorLogger = applicationErrorLogger;
            _irfService = irfService;
            _meetingService = meetingService;
            _azureStorageHelper = azureStorageHelper;
        }
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                {
                    var irfList = _irfRepository.GetAllIRF();
                    if (profileName != null && profileName.Contains("Client"))
                    {
                        ViewBag.IsClientUser = true;
                        irfList.ToList().ForEach(c => c.IsClientUser = true);
                    }
                    else
                    {
                        ViewBag.IsClientUser = false;
                    }
                    if (TempData["SuccessErrorMessage"] != null)
                    {
                        ViewBag.Message = TempData["SuccessErrorMessage"].ToString();
                    }
                    //ViewBag.Message = "Failed";
                    return View(irfList);
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
        [HttpGet]
        public IActionResult Add()
        {
            if (HttpContext.Session.GetString("email") != null)
            {
                var model = new IRFVm();
                model.JoinabilityTypeList = new SelectList(_irfService.GetJoinabilityMatrix(), "id", "Name");
                model.SkillGeneralTypeList = new SelectList(_irfService.GetGeneralSkillMatrix(), "id", "Name");
                model.SelectabilityTypeList = new SelectList(_irfService.GetSelectabilityMatrix(), "id", "Name");

                ViewBag.Clients = new SelectList(_irfRepository.GetClients(), "id", "Name");
                ViewBag.Level = new SelectList(_irfRepository.GetLevels(), "id", "Name");
                ViewBag.LevelTech = new SelectList(_irfRepository.GetLevelTech(), "id", "Name");
                ViewBag.Skill = new SelectList(_irfRepository.GetSkills(), "id", "Name");
                ViewBag.ClientUser = new SelectList(_irfRepository.GetCientUsers(), "id", "EmailId");
                ViewBag.ClientRole = new SelectList(_irfRepository.GetClientRoles(), "id", "Name");
                ViewBag.Interviewers = new SelectList(_irfRepository.GetInterviewers(), "id", "EmailId");
                ViewBag.QualityController = _irfService.GetQualityController(); 
                ViewBag.RiseBirdHR = new SelectList(_irfService.GetRiseBirdHR(), "id", "EmailId");
                var jobLevels = from JobLevels s in Enum.GetValues(typeof(JobLevels))
                                select new { ID = (int)s, Name = s.GetDescription() };
                ViewBag.JobLevelList = new SelectList(jobLevels, "ID", "Name"); ;

                var companyTypes = from CompanyType s in Enum.GetValues(typeof(CompanyType))
                                   select new { ID = (int)s, Name = s.GetDescription() };
                model.CompanyTypeList = new SelectList(companyTypes, "ID", "Name");
                return View(model);
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }

        [HttpPost]
        public IActionResult AddIRF(IRFVm irfDetails)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR")
                    {
                        if (_irfService.AddIRFDetails(irfDetails).Result)
                        {
                            TempData["SuccessErrorMessage"] = "IRF is added Successfully";
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            TempData["SuccessErrorMessage"] = "Adding IRF has Failed";
                            return RedirectToAction("Index");
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
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessage"] = "Adding IRF has failed";
                return RedirectToAction("Index");
            }
        }

        public string GetInterviewerSkills(string id)
        {
            var name = _irfService.GetInterviewerSkills(Convert.ToInt16(id));
            return name;
        }

        public string GetQCSkills(string id)
        {
            return _irfService.GetQCSkills(Convert.ToInt16(id));
        }

        public string GetQuestionSkills(string id)
        {
            return _irfService.GetQuestionSkills(id);
        }

        public bool CheckUniqueness(string value)
        {
            return _irfService.CheckUniqueness(value);
        }

        public IActionResult SampleQuestions(string id)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {              
                TechSkillComments model = new TechSkillComments();
                var result = _irfService.GetTechSkillComments(Convert.ToInt16(id));
                //if (result.Count() > 0)
                model.SkillCollection = result;
                ViewBag.Skills = new SelectList(_irfService.GetTechSkills(Convert.ToInt16(id)), "id", "Name");
                model.IRFId = Convert.ToInt16(id);
                return View(model);               
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }

        [HttpPost]
        public IActionResult SampleQuestions(TechSkillComments model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if(profileName == "Admin" || profileName == "HR")
                    {
                        if (_irfService.AddSkillComments(model))
                        {
                            TempData["SuccessErrorMessage"] = "Questions saved Successfully";
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            TempData["SuccessErrorMessage"] = "Questions were not saved.";
                            return RedirectToAction("Index");
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
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessage"] = "Questions were not saved";
                return RedirectToAction("Index");
            }
        }

        public IActionResult DeleteIRF(string id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR")
                    {
                        if (_irfService.DeleteIRF(Convert.ToInt16(id)).Result)
                        {
                            TempData["SuccessErrorMessage"] = "IRF deleted Successfully";
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            TempData["SuccessErrorMessage"] = "IRF delete Failed";
                            return RedirectToAction("Index");
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
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessage"] = "IRF delete Failed";
                return RedirectToAction("Index");
            }

        }

        [HttpGet]
        public IActionResult SelectedInterviewMeetings(string id, string irfId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                    {
                        string email = string.Empty;
                        if (profileName == "Client")
                        {
                            email = HttpContext.Session.GetString("email");
                        }
                        var selectedMeeting = new List<MeetingVM>();
                        var meetings = _irfService.GetIRFInterviewMeetings(Convert.ToInt16(id), email);
                        meetings = meetings.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((ReceiveMeetingStatus)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();
                        ViewBag.IRF = irfId;
                        ViewBag.IRFintId = id;
                        //ViewBag.IsSelected = true;
                        foreach (var m in meetings)
                        {
                            if (m.intervieweStatus.HasValue && m.intervieweStatus.Value == 2)
                            {
                                m.intervieweStatusStatDesc = ((InterviewStatus)(m.intervieweStatus)).GetDescription();
                            }
                            else
                            {
                                continue;
                            }

                            if (m.L2InterviewStatus.HasValue)
                            {
                                m.L2InterviewStatDesc = ((L2InterviewStatus)(m.L2InterviewStatus)).GetDescription();
                            }
                            if (m.L2InterviewStatus.HasValue)
                            {
                                m.ClientInterviewStatDesc = ((ClientInterviewStatus)(m.ClientInterviewStatus)).GetDescription();
                            }
                            if (m.JoiningStatus.HasValue)
                            {
                                m.JoiningStatusStatDesc = ((JoiningStatus)(m.JoiningStatus)).GetDescription();
                            }
                            if (m.PreparationStatus.HasValue)
                            {
                                m.PreparationStatusDesc = ((PreparationStatus)(m.PreparationStatus)).GetDescription();
                            }
                            if (m.OfferStatus.HasValue)
                            {
                                m.OfferStatusStatDesc = ((PreparationStatus)(m.OfferStatus)).GetDescription();
                            }
                            if (m.MeetingStatus.HasValue)
                            {
                                m.MeetingStatusStatusDesc = ((MeetingStatus)(m.MeetingStatus)).GetDescription();
                            }
                            if (m.QCStatus.HasValue)
                            {
                                m.QCStatusStatusDesc = ((QCStatus)(m.QCStatus)).GetDescription();
                            }
                            selectedMeeting.Add(m);
                        }
                        return View(selectedMeeting);
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
                return RedirectToAction("Index");
            }

        }


        [HttpGet]
        public IActionResult GetInterviewMeetings(string id, string irfId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                    {
                        string email = string.Empty;
                        if (profileName == "Client")
                        {
                            email = HttpContext.Session.GetString("email");
                        }
                        var meetings = _irfService.GetIRFInterviewMeetings(Convert.ToInt16(id), email);
                        meetings = meetings.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((ReceiveMeetingStatus)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();
                        ViewBag.IRF = irfId;
                        ViewBag.IRFintId = id;
                        //ViewBag.IsSelected = false;
                        foreach (var m in meetings)
                        {
                            if (m.intervieweStatus.HasValue)
                            {
                                m.intervieweStatusStatDesc = ((InterviewStatus)(m.intervieweStatus)).GetDescription();
                            }
                            if (m.L2InterviewStatus.HasValue)
                            {
                                m.L2InterviewStatDesc = ((L2InterviewStatus)(m.L2InterviewStatus)).GetDescription();
                            }
                            if (m.L2InterviewStatus.HasValue)
                            {
                                m.ClientInterviewStatDesc = ((ClientInterviewStatus)(m.ClientInterviewStatus)).GetDescription();
                            }
                            if (m.JoiningStatus.HasValue)
                            {
                                m.JoiningStatusStatDesc = ((JoiningStatus)(m.JoiningStatus)).GetDescription();
                            }
                            if (m.PreparationStatus.HasValue)
                            {
                                m.PreparationStatusDesc = ((PreparationStatus)(m.PreparationStatus)).GetDescription();
                            }
                            if (m.OfferStatus.HasValue)
                            {
                                m.OfferStatusStatDesc = ((PreparationStatus)(m.OfferStatus)).GetDescription();
                            }
                            if (m.MeetingStatus.HasValue)
                            {
                                m.MeetingStatusStatusDesc = ((MeetingStatus)(m.MeetingStatus)).GetDescription();
                            }
                            if (m.QCStatus.HasValue)
                            {
                                m.QCStatusStatusDesc = ((QCStatus)(m.QCStatus)).GetDescription();
                            }

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
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }

        }

        [HttpGet]
        public IActionResult EditIRF(string id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null)
                {
                    string prof = HttpContext.Session.GetString("profileName");                   
                    if (prof != null && prof.Contains("Client"))
                    {
                        ViewBag.IsClientUser = true;                       
                    }
                    else
                    {
                        ViewBag.IsClientUser = false;
                    }
                    var model = _irfService.GetEditIRF(Convert.ToInt16(id)).Result;
                    ViewBag.Clients = new SelectList(_irfRepository.GetClients(), "id", "Name");
                    ViewBag.Level = new SelectList(_irfRepository.GetLevels(), "id", "Name");
                    ViewBag.LevelTech = new SelectList(_irfRepository.GetLevelTech(), "id", "Name");
                    ViewBag.Skill = new SelectList(_irfRepository.GetSkills(), "id", "Name");
                    ViewBag.ClientUser = new SelectList(_irfRepository.GetCientUsers(), "id", "EmailId");
                    ViewBag.ClientRole = new SelectList(_irfRepository.GetClientRoles(), "id", "Name");
                    ViewBag.Interviewers = new SelectList(_irfRepository.GetInterviewers(), "id", "EmailId");
                    ViewBag.QualityController = _irfService.GetQualityController();
                    ViewBag.RiseBirdHR = new SelectList(_irfService.GetRiseBirdHR(), "id", "EmailId");

                        var jobLevels = from JobLevels s in Enum.GetValues(typeof(JobLevels))
                                        select new { ID = (int)s, Name = s.GetDescription() };
                        ViewBag.JobLevelList = new SelectList(jobLevels, "ID", "Name");

                    model.JoinabilityTypeList = new SelectList(_irfService.GetJoinabilityMatrix(), "id", "Name");
                    model.SkillGeneralTypeList = new SelectList(_irfService.GetGeneralSkillMatrix(), "id", "Name");
                    model.SelectabilityTypeList = new SelectList(_irfService.GetSelectabilityMatrix(), "id", "Name");
                    
                    var companyTypes = from CompanyType s in Enum.GetValues(typeof(CompanyType))
                                    select new { ID = (int)s, Name = s.GetDescription() };
                    model.CompanyTypeList = new SelectList(companyTypes, "ID", "Name");

                    
                    return View(model);
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }

        }

        [HttpPost]
        public IActionResult EditIRF(IRFVm model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR")
                    {
                        if (_irfService.UpdateIRF(model))
                        {
                            TempData["SuccessErrorMessage"] = "IRF saved Successfully";
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            TempData["SuccessErrorMessage"] = "IRF edits were not saved.";
                            return RedirectToAction("Index");
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
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessage"] = "IRF edits were not saved";
                return RedirectToAction("Index");
            }
        }
            

        public IActionResult Stats(string id)
        {
            return View();

        }

        [HttpGet]
        public IActionResult AddReport(int interviewId, int irfId, bool IsSelected = false)
        {
            try
            {
                string prof = HttpContext.Session.GetString("profileName");

                var model = _meetingService.GetInteviewReport(interviewId, irfId).Result;
                if (prof != null && prof.Contains("Client"))
                {
                    model.IsClientUser = true;
                }
                ViewBag.IsSelected = IsSelected;
                if(model.QuestionsCollection==null)
                {
                    ViewBag.QuestionsDiscussedCount = 0;
                }
                else
                {
                    ViewBag.QuestionsDiscussedCount = model.QuestionsCollection?.Count();
                }
                GetStatusList(model, irfId);
                return View(model);

            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("MyInterview");
            }

        }

        [HttpPost]
        [DisableRequestSizeLimit]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReport(InterviewReport model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR")
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
                                model.AudioLink = uploadedaudioFileURL;
                            }
                        }
                        if (model.VideoFile != null)
                        {
                            // byte[] videofileData = new byte[model.VideoFile.Length];
                            // string uploadedvideoFileURL = _azureStorageHelper.InsertBlob(model.VideoFile.FileName, "videos", videofileData);
                            await _azureStorageHelper.InsertBlob(model.VideoFile.FileName, "videos", model.VideoFile);
                            var containerRef = _azureStorageHelper.GetCloudBlobContainer("videos");
                            var blob = containerRef.GetBlockBlobReference(model.VideoFile.FileName);
                            string uploadedvideoFileURL = blob.Uri.ToString();
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
                        if (_meetingService.AddInterviewReport(model).Result)
                        {
                            TempData["SuccessErrorMessage"] = "Report saved Successfully";
                            return RedirectToAction("AddReport", new { interviewId = model.InterviewId, irfId = model.IRFId });
                        }
                        else
                        {
                            TempData["SuccessErrorMessage"] = "Report was not saved.";
                            return RedirectToAction("Index");
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
                TempData["SuccessErrorMessage"] = "Report was not saved.";
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

            model.Level = new SelectList(_irfRepository.GetLevels(), "id", "Name");
            model.LevelTech = new SelectList(_irfRepository.GetLevelTech(), "id", "Name");
            var questionsLevel = from QuestionsLevel s in Enum.GetValues(typeof(QuestionsLevel))
                                 select new { ID = (int)s, Name = s.GetDescription() };
            model.QuestionsLevelList = new SelectList(questionsLevel, "ID", "Name");
            var confirmationTypes = from ConfirmationStatus s in Enum.GetValues(typeof(ConfirmationStatus))
                                    select new { ID = (int)s, Name = s.GetDescription() };
            model.ConfirmationTypeList = new SelectList(confirmationTypes, "ID", "Name"); ;
            model.QuestionsList = new SelectList(_meetingService.GetTechSkillComments(irfId), "Comment", "Comment");

        }
    }
}