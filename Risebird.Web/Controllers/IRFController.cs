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
using System.Data;
using Microsoft.AspNetCore.Authorization;
using OfficeOpenXml;

namespace Risebird.Web.Controllers
{
    [Authorize]
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

        [Authorize]
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                {
                    var irfList = _irfRepository.GetAllIRF(emailid, profileName);
                    foreach (var irf in irfList)
                    {
                        var meetings= _irfService.GetIRFInterviewMeetings(irf.id, profileName, emailid).ToList();
                        var selectedMeeting = new List<MeetingVM>();
                        irf.TotalMeetings = (meetings != null ? meetings.Count() : 0);
                        if(meetings !=null)
                        {
                            selectedMeeting = meetings.Where(x => x.InterviewStatus.HasValue && (x.InterviewStatus.Value == 2 || x.InterviewStatus.Value == 4)).ToList();//new List<MeetingVM>();
                            irf.ScheduleMeetings = meetings.Where(x => !x.MeetingStatus.HasValue && x.interviewDate >= DateTime.Now && x.confirmationStatus == "1").Count();
                        }
                      
                        irf.SelectedMeetings = selectedMeeting != null ? selectedMeeting.Count() : 0;
                    }
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
                        TempData.Remove("SuccessErrorMessage");
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

        [Authorize]
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

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddIRF(IRFVm irfDetails)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR")
                    {
                        string uploadedFileURL = string.Empty;
                        if (irfDetails.JDFile == null && (irfDetails.JDLink == null))
                        {
                            TempData["SuccessErrorMessage"] = "Adding IRF has Failed. JDLink not found.";
                            return RedirectToAction("Index");
                        }
                        if (irfDetails.JDFile != null)
                        {
                            await _azureStorageHelper.InsertBlob(irfDetails.JDFile.FileName, "resumes", irfDetails.JDFile);
                            var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                            var blob = containerRef.GetBlockBlobReference(irfDetails.JDFile.FileName);
                            uploadedFileURL = blob.Uri.ToString();
                            irfDetails.JDLink = uploadedFileURL;
                        }
                        
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

        [Authorize]
        public string GetInterviewerSkills(string id)
        {
            var name = _irfService.GetInterviewerSkills(Convert.ToInt16(id));
            return name;
        }

        [Authorize]
        public SelectList GetInterviewers(string skills)
        {

            var list = new List<SelectListItem>();
            var interviewers = _irfService.GetInterviewers(skills);
            
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
        public string GetQCSkills(string id)
        {
            return _irfService.GetQCSkills(Convert.ToInt16(id));
        }

        //GetClientUserRole
        [Authorize]
        public string GetClientUserRole(string id)
        {
            return _irfService.GetClientUserRole(Convert.ToInt16(id));
        }

        [Authorize]
        public string GetQuestionSkills(string id)
        {
            return _irfService.GetQuestionSkills(id);
        }

        [Authorize]
        public bool CheckUniqueness(string value)
        {
            return _irfService.CheckUniqueness(value);
        }

        [Authorize]
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

        [Authorize]
        [HttpPost]
        public IActionResult SampleQuestions(TechSkillComments model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR")
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

        [Authorize]
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

        [Authorize]
        [HttpGet]
        public IActionResult SelectedInterviewMeetings(string id, string irfId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                    {
                        var selectedMeeting = new List<MeetingVM>();
                        var meetings = _irfService.GetIRFInterviewMeetings(Convert.ToInt16(id), profileName, emailid);
                        meetings = meetings.Where(x => x.interviewDate.Date == DateTime.Now.Date).ToList();
                        meetings = meetings.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((ReceiveMeetingStatus)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();
                        ViewBag.IRF = irfId;
                        ViewBag.IRFintId = id;
                        //ViewBag.IsSelected = true;
                        foreach (var m in meetings)
                        {
                            if (m.InterviewStatus.HasValue && (m.InterviewStatus.Value == 2 || m.InterviewStatus.Value == 4))
                            {
                                m.intervieweStatusStatDesc = ((InterviewStatus)(m.InterviewStatus)).GetDescription();
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
                            if (m.IsApproved.HasValue)
                            {
                                m.NextLevelApprovalStat = ((NextLevelApprovalStatus)(m.IsApproved)).GetDescription();
                            }
                            if (m.IsSelected.HasValue)
                            {
                                m.FinalSelectionStat = ((FinalSelectionStatus)(m.IsSelected)).GetDescription();
                            }
                            selectedMeeting.Add(m);
                        }
                        var list = new SelectList(new[]
{
    new { ID = "Today", Name = "Today"},
    new { ID = "Tomorrow", Name = "Tomorrow"  },
    new { ID = "Yesterday", Name = "Yesterday"  },
    new { ID = "Next Seven Days", Name = "Next Seven Days" },
    new { ID = "All", Name = "All"},
},
"ID", "Name", "All");

                        ViewData["list"] = list;
                        ViewBag.ProfileName = profileName.ToLower();

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
        public IActionResult GetFilteredSelectedMeetings(string filterValue, string id, string irfId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                    {
                        var selectedMeeting = new List<MeetingVM>();
                        var meetings = _irfService.GetIRFInterviewMeetings(Convert.ToInt16(id), profileName, emailid);

                        if (filterValue == "Today")
                        {
                            meetings = meetings.Where(x => x.interviewDate.Date == DateTime.Now.Date).ToList();
                            if (meetings.Count() == 0)
                            {
                                return new EmptyResult();
                            }

                        }
                        else if (filterValue == "Tomorrow")
                        {
                            meetings = meetings.Where(x => x.interviewDate.Date == DateTime.Now.AddDays(1).Date).ToList();
                            if (meetings.Count() == 0)
                            {
                                return new EmptyResult();
                            }
                        }
                        else if (filterValue == "Yesterday")
                        {
                            meetings = meetings.Where(x => x.interviewDate.Date == DateTime.Now.AddDays(-1).Date).ToList();
                            if (meetings.Count() == 0)
                            {
                                return new EmptyResult();
                            }
                        }
                        else if (filterValue == "Next Seven Days")
                        {
                            meetings = meetings.Where(x => x.interviewDate.Date <= DateTime.Now.AddDays(7).Date && x.interviewDate.Date >= DateTime.Now.Date).ToList();
                            if (meetings.Count() == 0)
                            {
                                return new EmptyResult();
                            }
                        }




                        meetings = meetings.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((ReceiveMeetingStatus)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();
                        ViewBag.IRF = irfId;
                        ViewBag.IRFintId = id;
                        //ViewBag.IsSelected = true;
                        foreach (var m in meetings)
                        {
                            if (m.InterviewStatus.HasValue && (m.InterviewStatus.Value == 2 || m.InterviewStatus.Value == 4))
                            {
                                m.intervieweStatusStatDesc = ((InterviewStatus)(m.InterviewStatus)).GetDescription();
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
                            if (m.IsApproved.HasValue)
                            {
                                m.NextLevelApprovalStat = ((NextLevelApprovalStatus)(m.IsApproved)).GetDescription();
                            }
                            if (m.IsSelected.HasValue)
                            {
                                m.FinalSelectionStat = ((FinalSelectionStatus)(m.IsSelected)).GetDescription();
                            }
                            selectedMeeting.Add(m);
                        }
                        ViewBag.ProfileName = profileName.ToLower();

                        return PartialView("GetFilteredSelectedMeetings", selectedMeeting);
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


        [Authorize]
        [HttpGet]
        public IActionResult GetInterviewMeetings(string id, string irfId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                    {
                        var meetings = _irfService.GetIRFInterviewMeetings(Convert.ToInt16(id), profileName, emailid);
                        meetings = meetings.Where(x => x.interviewDate.Date == DateTime.Now.Date).ToList();
                        meetings = meetings.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((ReceiveMeetingStatus)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();
                        ViewBag.IRF = irfId;
                        ViewBag.IRFintId = id;
                        //ViewBag.IsSelected = false;
                        foreach (var m in meetings)
                        {
                            if (m.InterviewStatus.HasValue)
                            {
                                m.intervieweStatusStatDesc = ((InterviewStatus)(m.InterviewStatus)).GetDescription();
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
                        var list = new SelectList(new[]
{
    new { ID = "Today", Name = "Today"},
    new { ID = "Tomorrow", Name = "Tomorrow"  },
    new { ID = "Yesterday", Name = "Yesterday"  },
    new { ID = "Next Seven Days", Name = "Next Seven Days" },
    new { ID = "All", Name = "All"},
},
"ID", "Name", "All");

                        ViewData["list"] = list;

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

        public IActionResult GetFilteredInterviewMeetings(string filterValue, string id, string irfId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                    {
                        var meetings = _irfService.GetIRFInterviewMeetings(Convert.ToInt16(id), profileName, emailid);

                        if (filterValue == "Today")
                        {
                            meetings = meetings.Where(x => x.interviewDate.Date == DateTime.Now.Date).ToList();
                            if (meetings.Count() == 0)
                            {
                                return new EmptyResult();
                            }

                        }
                        else if (filterValue == "Tomorrow")
                        {
                            meetings = meetings.Where(x => x.interviewDate.Date == DateTime.Now.AddDays(1).Date).ToList();
                            if (meetings.Count() == 0)
                            {
                                return new EmptyResult();
                            }
                        }
                        else if (filterValue == "Yesterday")
                        {
                            meetings = meetings.Where(x => x.interviewDate.Date == DateTime.Now.AddDays(-1).Date).ToList();
                            if (meetings.Count() == 0)
                            {
                                return new EmptyResult();
                            }
                        }
                        else if (filterValue == "Next Seven Days")
                        {
                            meetings = meetings.Where(x => x.interviewDate.Date <= DateTime.Now.AddDays(7).Date && x.interviewDate.Date >= DateTime.Now.Date).ToList();
                            if (meetings.Count() == 0)
                            {
                                return new EmptyResult();
                            }
                        }
                        
                        meetings = meetings.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((ReceiveMeetingStatus)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();
                        ViewBag.IRF = irfId;
                        ViewBag.IRFintId = id;
                        
                        foreach (var m in meetings)
                        {
                            if (m.InterviewStatus.HasValue)
                            {
                                m.intervieweStatusStatDesc = ((InterviewStatus)(m.InterviewStatus)).GetDescription();
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

                        return PartialView("GetFilteredInterviewMeetings", meetings);
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

        [Authorize]
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
                    string skills = string.Empty;
                    foreach (var item in model.SkillCollection)
                    {
                        skills = skills + "," + item.SkillId;
                    }
                    ViewBag.Clients = new SelectList(_irfRepository.GetClients(), "id", "Name");
                    ViewBag.Level = new SelectList(_irfRepository.GetLevels(), "id", "Name");
                    ViewBag.LevelTech = new SelectList(_irfRepository.GetLevelTech(), "id", "Name");
                    ViewBag.Skill = new SelectList(_irfRepository.GetSkills(), "id", "Name");
                    ViewBag.ClientUser = new SelectList(_irfService.GetClientUser(model.ClientId), "id", "EmailId");
                    ViewBag.ClientRole = new SelectList(_irfRepository.GetClientRoles(), "id", "Name");
                    ViewBag.Interviewers = new SelectList(_irfService.GetInterviewers(skills), "UserId", "EmailId");
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
                    TempData["JDLink"] = model.JDLink;


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

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> EditIRF(IRFVm model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR")
                    {
                        if ((model.JDFile != null))
                        {

                            await _azureStorageHelper.InsertBlob(model.JDFile.FileName, "resumes", model.JDFile);
                            var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                            var blob = containerRef.GetBlockBlobReference(model.JDFile.FileName);
                            string uploadedFileURL = blob.Uri.ToString();
                            if (uploadedFileURL == null)
                            {
                                TempData["SuccessErrorMessage"] = "IRF edits were not saved.";
                                return RedirectToAction("Index");

                            }
                        
                            model.JDLink = uploadedFileURL;
                        }
                        else if (model.JDLink == null || model.JDLink=="")
                        {
                            model.JDLink = TempData["JDLink"] != null ? TempData["JDLink"].ToString() : null;
                        }

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

        [Authorize]
        public IActionResult Stats(string id, string irfId)
        {
            TempData["id"] = id;
            TempData["irfId"] = irfId;
            TempData.Keep();
            ViewBag.IRF = irfId;
            return View();

        }

        [HttpGet]
        public IActionResult AddReport(int interviewId, int irfId, bool IsSelected = false)
        {
            try
            {
                string userMenus = HttpContext.Session.GetString("userMenus");

                if (userMenus == null)
                {
                    return RedirectToAction("Index", "Home", new { url = "AddReport/IRF/" + interviewId + "/" + irfId });
                }

                string prof = HttpContext.Session.GetString("profileName");

                if (prof != null)
                {
                    prof = prof.Replace("\"", "");
                }

                var model = _meetingService.GetInteviewReport(interviewId, irfId).Result;

                if (model == null)
                {
                   return RedirectToAction("Index");
                }
            
                ViewBag.IsSelected = IsSelected;
                if (model.QuestionsCollection == null)
                {
                    ViewBag.QuestionsDiscussedCount = 0;
                }
                else 
                {
                    ViewBag.QuestionsDiscussedCount = model.QuestionsCollection?.Count();
                }
                ViewBag.ProfileName = prof.ToLower();
                
                GetStatusList(model, irfId);
                
                return PartialView("AddReportPartial",model);

            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("MyInterview");
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
                        if (model.VideoFile == null && model.PageVideoLink == null && model.VideoLink == null)
                        {
                            ModelState.AddModelError(String.Empty, "Video file/link not found.");
                            GetStatusList(model);
                            return View("AddReport", model);
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
                        else if(model.PageVideoLink !=null)
                        {
                            model.VideoLink = model.PageVideoLink;
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

        [Authorize]
        [HttpGet]
        public IActionResult NewChart()
        {
            try
            {
                string id = TempData.Peek("id").ToString();
                string irfId = TempData.Peek("irfId").ToString();
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                    {
                        var meetings = _irfService.GetIRFInterviewMeetings(Convert.ToInt16(id), profileName, emailid);
                        var noShowCount = meetings.Where(x => x.MeetingStatus.HasValue && x.MeetingStatus.Value == 1).Count();
                        var ScheduledCount = meetings.Where(x => !x.MeetingStatus.HasValue && x.interviewDate >= DateTime.Now && x.confirmationStatus =="1").Count();
                        var DroppedCount = meetings.Where(x => x.MeetingStatus.HasValue && x.MeetingStatus.Value == 3).Count();
                        var CompletedCount = meetings.Where(x => x.MeetingStatus.HasValue && x.MeetingStatus.Value == 4).Count();
                        ViewBag.IRF = irfId;
                        ViewBag.IRFintId = id;

                        List<object> iData = new List<object>();

                        DataTable dt = new DataTable();
                        dt.Columns.Add("Meeting Status", System.Type.GetType("System.String"));
                        dt.Columns.Add("Count", System.Type.GetType("System.Int32"));

                        DataRow dr = dt.NewRow();
                        dr["Meeting Status"] = "No Show";
                        dr["Count"] = noShowCount;
                        dt.Rows.Add(dr);

                        dr = dt.NewRow();
                        dr["Meeting Status"] = "Scheduled";
                        dr["Count"] = ScheduledCount;
                        dt.Rows.Add(dr);

                        dr = dt.NewRow();
                        dr["Meeting Status"] = "Dropped";
                        dr["Count"] = DroppedCount;
                        dt.Rows.Add(dr);

                        dr = dt.NewRow();
                        dr["Meeting Status"] = "Completed";
                        dr["Count"] = CompletedCount;
                        dt.Rows.Add(dr);
                        //Looping and extracting each DataColumn to List<Object>  
                        foreach (DataColumn dc in dt.Columns)
                        {
                            List<object> x = new List<object>();
                            x = (from DataRow drr in dt.Rows select drr[dc.ColumnName]).ToList();
                            iData.Add(x);
                        }
                        //Source data returned as JSON  
                        return Json(iData);
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
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }



        }

        [Authorize]
        [HttpGet]
        public IActionResult NewChartSelected()
        {
            try
            {
                string id = TempData.Peek("id").ToString();
                string irfId = TempData.Peek("irfId").ToString();
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                    {
                        var selectedMeeting = new List<MeetingVM>();
                        var meetings = _irfService.GetIRFInterviewMeetings(Convert.ToInt16(id), profileName, emailid);

                        var ProxyCount = meetings.Where(x => x.InterviewStatus.HasValue && x.InterviewStatus.Value == 1).Count();
                        var SelectedCount = meetings.Where(x => x.InterviewStatus.HasValue && x.InterviewStatus.Value == 2).Count();
                        var RejectedCount = meetings.Where(x => x.InterviewStatus.HasValue && x.InterviewStatus.Value == 3).Count();
                        var BorderLineCount = meetings.Where(x => x.InterviewStatus.HasValue && x.InterviewStatus.Value == 4).Count();



                        ViewBag.IRF = irfId;
                        ViewBag.IRFintId = id;
                        List<object> iData = new List<object>();
                        //Creating sample data  
                        DataTable dt = new DataTable();
                        dt.Columns.Add("Interview Status", System.Type.GetType("System.String"));
                        dt.Columns.Add("Count", System.Type.GetType("System.Int32"));

                        DataRow dr = dt.NewRow();
                        dr["Interview Status"] = "Proxy";
                        dr["Count"] = ProxyCount;
                        dt.Rows.Add(dr);

                        dr = dt.NewRow();
                        dr["Interview Status"] = "Selected";
                        dr["Count"] = SelectedCount;
                        dt.Rows.Add(dr);

                        dr = dt.NewRow();
                        dr["Interview Status"] = "Rejected";
                        dr["Count"] = RejectedCount;
                        dt.Rows.Add(dr);

                        dr = dt.NewRow();
                        dr["Interview Status"] = "BorderLine";
                        dr["Count"] = BorderLineCount;
                        dt.Rows.Add(dr);
                        //Looping and extracting each DataColumn to List<Object>  
                        foreach (DataColumn dc in dt.Columns)
                        {
                            List<object> x = new List<object>();
                            x = (from DataRow drr in dt.Rows select drr[dc.ColumnName]).ToList();
                            iData.Add(x);
                        }
                        //Source data returned as JSON  
                        return Json(iData);
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
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }
        }

        public SelectList GetClientUser(int clientId)
        {
            var list = new List<SelectListItem>();
            var clientUsers = _irfService.GetClientUser(clientId);
            if (clientUsers!=null && clientUsers.Count() > 0)
            {
                return new SelectList(clientUsers, "id", "EmailId");
            }
            else
                list.Add(new SelectListItem { Text = "No Client User", Value = "0" });
                //clientUsers.ToList().Add(new Users { id = 0, FirstName = "No Client User" });
                return new SelectList(list, "Value", "Text");
        }


        //NewChartClientsData
        [Authorize]
        [HttpGet]
        public IActionResult NewChartClientMeetingData()
        {
            try
            {
                string id = TempData.Peek("id").ToString();
                string irfId = TempData.Peek("irfId").ToString();
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                    {
                        var selectedMeeting = new List<MeetingVM>();
                        var meetings = _irfService.GetIRFInterviewMeetings(Convert.ToInt16(id), profileName, emailid);
                        var clientList = new List<string>();
                        var chartData = new List<ClientChartData>();
                        var clientUserList = _irfRepository.GetTransClientUser(Convert.ToInt16(id));
                        foreach (var item in clientUserList)
                        {
                            if(item.ClientRoleId ==1 || item.ClientRoleId ==4)
                            {
                                clientList.Add(item.EmailId);

                            }

                        }
                        foreach (var item in clientList)
                        {
                            var obj = new ClientChartData();
                            obj.Name = item;
                            obj.NoShowCount = meetings.Where(x => x.MeetingStatus.HasValue && x.MeetingStatus.Value == 1 && (x.ClientHrContact == item  || x.staffingAgency == item)).Count();
                            obj.ScheduledCount = meetings.Where(x => !x.MeetingStatus.HasValue && x.interviewDate >= DateTime.Now && x.confirmationStatus == "1" && (x.ClientHrContact == item || x.staffingAgency == item)).Count(); 
                            obj.CompletedCount = meetings.Where(x => x.MeetingStatus.HasValue && x.MeetingStatus.Value == 4 && (x.ClientHrContact == item || x.staffingAgency == item)).Count();
                            obj.DroppedCount = meetings.Where(x => x.MeetingStatus.HasValue && x.MeetingStatus.Value == 3 && (x.ClientHrContact == item || x.staffingAgency == item)).Count();
                            chartData.Add(obj);
                            //check for this if condition
                            //if (meetings.Where(x=> x.ClientHrContact == item).Count()>0)
                            //{
                                
                            //}
                            
                        }
                        //var ProxyCount = meetings.Where(x => x.InterviewStatus.HasValue && x.InterviewStatus.Value == 1).Count();
                        //var SelectedCount = meetings.Where(x => x.InterviewStatus.HasValue && x.InterviewStatus.Value == 2).Count();
                        //var RejectedCount = meetings.Where(x => x.InterviewStatus.HasValue && x.InterviewStatus.Value == 3).Count();
                        //var BorderLineCount = meetings.Where(x => x.InterviewStatus.HasValue && x.InterviewStatus.Value == 4).Count();



                        ViewBag.IRF = irfId;
                        ViewBag.IRFintId = id;

                        return Json(chartData);
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
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        [HttpGet]
        public IActionResult NewChartClientFeedbackData()
        {
            try
            {
                string id = TempData.Peek("id").ToString();
                string irfId = TempData.Peek("irfId").ToString();
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                    {
                        var selectedMeeting = new List<MeetingVM>();
                        var meetings = _irfService.GetIRFInterviewMeetings(Convert.ToInt16(id), profileName, emailid);
                        var clientList = new List<string>();
                        var chartData = new List<ClientChartFeedbackData>();
                        var clientUserList = _irfRepository.GetTransClientUser(Convert.ToInt16(id));
                        foreach (var item in clientUserList)
                        {
                            if (item.ClientRoleId == 1 || item.ClientRoleId == 4)
                            {
                                clientList.Add(item.EmailId);

                            }

                        }
                        foreach (var item in clientList)
                        {
                            var obj = new ClientChartFeedbackData();
                            obj.Name = item;
                            obj.ProxyCount = meetings.Where(x => x.InterviewStatus.HasValue && x.InterviewStatus.Value == 1 && (x.ClientHrContact == item || x.staffingAgency == item)).Count();
                            obj.SelectedCount = meetings.Where(x => x.InterviewStatus.HasValue && x.InterviewStatus.Value == 2 && (x.ClientHrContact == item || x.staffingAgency == item)).Count(); ;
                            obj.RejectedCount = meetings.Where(x => x.InterviewStatus.HasValue && x.InterviewStatus.Value == 3 && (x.ClientHrContact == item || x.staffingAgency == item)).Count();
                            obj.BorderLineCount = meetings.Where(x => x.InterviewStatus.HasValue && x.InterviewStatus.Value == 4 && (x.ClientHrContact == item || x.staffingAgency == item)).Count();
                            chartData.Add(obj);
                            //check for this if condition
                            //if (meetings.Where(x=> x.ClientHrContact == item).Count()>0)
                            //{

                            //}
                        }
                        //var ProxyCount = meetings.Where(x => x.InterviewStatus.HasValue && x.InterviewStatus.Value == 1).Count();
                        //var SelectedCount = meetings.Where(x => x.InterviewStatus.HasValue && x.InterviewStatus.Value == 2).Count();
                        //var RejectedCount = meetings.Where(x => x.InterviewStatus.HasValue && x.InterviewStatus.Value == 3).Count();
                        //var BorderLineCount = meetings.Where(x => x.InterviewStatus.HasValue && x.InterviewStatus.Value == 4).Count();



                        ViewBag.IRF = irfId;
                        ViewBag.IRFintId = id;

                        return Json(chartData);
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
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        [HttpGet]
        public IActionResult GetCandidateList(string id, string irfName)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                    {
                        ViewBag.IRFName = irfName;
                        ViewBag.IRFId = id;
                        var candidateList = _irfService.GetIRFCandidates(Convert.ToInt32(id));                       
                        return View(candidateList);

                        //return View();
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

        [Authorize]
        [HttpGet]
        public IActionResult AddCandidate(string irfId, string irfName)
        {
            if (HttpContext.Session.GetString("email") != null)
            {

                var model = new Candidate();
                model.IRFId = Convert.ToInt32(irfId);
                model.IRFName = irfName;
                return View(model);
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AddCandidate(Candidate model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    //var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    //profileName = profileName.Replace("\"", "");
                    if (_irfService.AddCandidate(model).Result)
                    {
                        TempData["SuccessErrorMessageCandidate"] = "Candidate added Successfully";
                        return RedirectToAction("GetCandidateList", new { id = model.IRFId, irfName = model.IRFName});
                    }
                    else
                    {
                        TempData["SuccessErrorMessageCandidate"] = "Adding candidate failed.";
                        return RedirectToAction("GetCandidateList", new { id = model.IRFId, irfName = model.IRFName });
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
                TempData["SuccessErrorMessage"] = "Adding candidate failed";
                return RedirectToAction("Index");
            }
        }

        public ActionResult ImportCandidates(IFormCollection formCollection, string irfId, string IRFName)
        {

            try
            {
                if (Request != null)
                {
                    var file = formCollection.Files[0];
                    var IRFId = Convert.ToInt32((formCollection["IRFId"].ToString()));
                    if ((file != null) && (file.Length > 0) && !string.IsNullOrEmpty(file.FileName))
                    {

                        string fileName = file.FileName;
                        string fileContentType = file.ContentType;
                        byte[] fileBytes = new byte[file.Length];
                        var data = file.OpenReadStream();
                        //var data = file.InputStream.Read(fileBytes, 0, Convert.ToInt32(file.ContentLength));
                        var candidateList = new List<Candidate>();
                        using (var package = new ExcelPackage(file.OpenReadStream()))
                        {
                            var currentSheet = package.Workbook.Worksheets;
                            var workSheet = currentSheet.First();
                            var noOfCol = workSheet.Dimension.End.Column;
                            var noOfRow = workSheet.Dimension.End.Row;
                            for (int rowIterator = 2; rowIterator <= noOfRow; rowIterator++)
                            {
                                var candidate = new Candidate();
                                candidate.Name = workSheet.Cells[rowIterator, 1].Value.ToString();
                                candidate.EmailId = workSheet.Cells[rowIterator, 2].Value.ToString();
                                candidate.ResumeLink = workSheet.Cells[rowIterator, 3].Value.ToString();
                                candidate.IRFId = IRFId;
                                candidateList.Add(candidate);
                            }

                        }

                        if (_irfService.AddMultipleCandidates(candidateList).Result)
                        {
                            TempData["SuccessErrorMessageCandidate"] = "Candidates added Successfully!";
                            return RedirectToAction("GetCandidateList", new { id = IRFId, irfName = IRFName });
                        }
                        else
                        {
                            TempData["SuccessErrorMessageCandidate"] = "Importing candidates failed!";
                            return RedirectToAction("GetCandidateList", new { id = IRFId, irfName = IRFName });
                        }

                    }

                    else
                    {
                        TempData["SuccessErrorMessageCandidate"] = "Importing candidates failed!";
                        return RedirectToAction("GetCandidateList", new { id = IRFId, irfName = IRFName });
                    }

                }


            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessage"] = "Importing candidates failed!";
                return RedirectToAction("Index");
            }
            //return View("Index");
            return null;
        }

        [Authorize]
        public IActionResult DeleteCandidate(string id, string irfId, string IRFName)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (_irfService.DeleteCandidate(Convert.ToInt16(id)).Result)
                    {
                        TempData["SuccessErrorMessage"] = "Candidate deleted Successfully";
                        return RedirectToAction("GetCandidateList", new { id = Convert.ToInt32(irfId), irfName = IRFName });
                    }
                    else
                    {
                        TempData["SuccessErrorMessage"] = "Candidate delete Failed";
                        return RedirectToAction("GetCandidateList", new { id = Convert.ToInt32(irfId), irfName = IRFName });
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
                TempData["SuccessErrorMessage"] = "Candidate delete Failed";
                return RedirectToAction("Index");
            }

        }
    }
}