using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;

namespace Risebird.Web.Controllers
{
    public class ProjectController : Controller
    {
        private IAzureStorageHelper _azureStorageHelper;
        private IIRFService _irfService;
        private IApplicationErrorLogger _applicationErrorLogger;
        private IProjectService _projectService;

        public ProjectController(IApplicationErrorLogger applicationErrorLogger, IAzureStorageHelper azureStorageHelper, IProjectService projectService, IIRFService irfService)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _azureStorageHelper = azureStorageHelper;
            _projectService = projectService;
            _irfService = irfService;
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
                    var projectList = _projectService.GetAllProjects();
                    
                    if (profileName != null && profileName.Contains("Client"))
                    {
                        ViewBag.IsClientUser = true;
                        projectList.ToList().ForEach(c => c.IsClientUser = true);
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
                    return View(projectList);
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
                
                var projectModel = new Project();
                var irfList = _projectService.GetAllIRF();
                var objList = new List<ProjectIRFMapping>();
                foreach (var item in irfList)
                {
                    
                    var obj = new ProjectIRFMapping();
                    obj.IRFId = item.id;
                    obj.IRFName = item.IRFId;
                    obj.IsAdded = false;
                    //obj.id = 0;
                    obj.PrimarySkills = item.JDTiltle.Split("|")[0];
                    //obj.ProjectId = 0;
                    obj.Client = item.ClientCompName;
                    objList.Add(obj);
                }
                projectModel.ProjectIRFMappings = (IList<ProjectIRFMapping>)(objList);

                ViewBag.Clients = new SelectList(_projectService.GetClients(), "id", "Name");
                return View(projectModel);
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Add(Project model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    //var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    //profileName = profileName.Replace("\"", "");
                    if (_projectService.AddProject(model).Result)
                    {
                        TempData["SuccessErrorMessageProject"] = "Project added Successfully";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["SuccessErrorMessageProject"] = "Adding Project has Failed";
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
                TempData["SuccessErrorMessage"] = "Adding Project has failed";
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        [HttpGet]
        public IActionResult Edit(int id)
        {
            if (HttpContext.Session.GetString("email") != null)
            {

                var projectModel = _projectService.GetProject(id);
                ViewBag.Clients = new SelectList(_projectService.GetClients(), "id", "Name");
                ViewBag.id = id;
                return View(projectModel);
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Edit(Project model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    //var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    //profileName = profileName.Replace("\"", "");
                    if (_projectService.UpdateProject(model))
                    {
                        TempData["SuccessErrorMessageProject"] = "Project edited Successfully";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["SuccessErrorMessageProject"] = "Editing Project has Failed";
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
                TempData["SuccessErrorMessage"] = "Editing Project has failed";
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        public IActionResult DeleteProject(string id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR")
                    {
                        if (_projectService.DeleteProject(Convert.ToInt16(id)).Result)
                        {
                            TempData["SuccessErrorMessage"] = "Project deleted Successfully";
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            TempData["SuccessErrorMessage"] = "Project delete Failed";
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
                TempData["SuccessErrorMessage"] = "Project delete Failed";
                return RedirectToAction("Index");
            }

        }

        public async Task<IActionResult> GetFilteredIRF(string clientId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName=="Client")
                    {
                        var projectModel = new Project();
                        var irfList = _projectService.GetAllIRF();
                        if(Convert.ToInt32(clientId) != 0)
                        {
                            irfList = irfList.Where(x => x.ClientId == Convert.ToInt32(clientId)).ToList();
                        }
                        //if(irfList.Count()==0)
                        //{
                        //    return new EmptyResult();
                        //}

                        var objList = new List<ProjectIRFMapping>();
                        foreach (var item in irfList)
                        {

                            var obj = new ProjectIRFMapping();
                            obj.IRFId = item.id;
                            obj.IRFName = item.IRFId;
                            obj.IsAdded = false;
                            //obj.id = 0;
                            obj.PrimarySkills = item.JDTiltle.Split("|")[0];
                            //obj.ProjectId = 0;
                            obj.Client = item.ClientCompName;
                            objList.Add(obj);
                        }
                        projectModel.ProjectIRFMappings = (IList<ProjectIRFMapping>)(objList);
                        if(projectModel.ProjectIRFMappings.Count()==0)
                        {
                            return new EmptyResult();
                        }

                        return PartialView("GetFilteredIRF", projectModel);
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
        public async Task<IActionResult> GetEditFilteredIRF(string clientId, string projectId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                    {
                        var projectModel = new Project();
                        var projectCluster = _projectService.GetProjectCluster(Convert.ToInt32(projectId));
                        var allIRFs = _projectService.GetAllIRF();

                        if (projectCluster!=null && projectCluster.Count()>0)
                        {
                            if(Convert.ToInt32(clientId)!=0)
                            {
                                projectCluster = projectCluster.Where(x => x.ClientId == Convert.ToInt32(clientId)).ToList();

                            }
                            if (projectCluster!=null)
                            {
                                foreach (var item in projectCluster)
                                {
                                    item.PrimarySkills = allIRFs.Where(x => x.id == item.IRFId).FirstOrDefault().JDTiltle.Split("|")[0];
                                    item.Client = allIRFs.Where(x => x.id == item.IRFId).FirstOrDefault().ClientCompName;
                                    allIRFs = allIRFs.Where(x => x.id != item.IRFId);
                                }
                            }
                            
                        }
                        if (Convert.ToInt32(clientId) != 0)
                        {
                            allIRFs = allIRFs.Where(x => x.ClientId == Convert.ToInt32(clientId)).ToList();

                        }
                        var objList = new List<ProjectIRFMapping>();

                        if (projectCluster != null && projectCluster.Count() != 0)
                        {
                            foreach (var item in projectCluster)
                            {
                                var obj = new ProjectIRFMapping();
                                obj.IRFId = item.IRFId;
                                obj.IRFName = item.IRFName;
                                obj.IsAdded = item.IsAdded;
                                //obj.id = 0;
                                obj.PrimarySkills = item.PrimarySkills;
                                //obj.ProjectId = 0;
                                obj.Client = item.Client;
                                objList.Add(obj);
                            }

                        }
                        foreach (var item in allIRFs)
                        {

                            var obj = new ProjectIRFMapping();
                            obj.IRFId = item.id;
                            obj.IRFName = item.IRFId;
                            obj.IsAdded = false;
                            //obj.id = 0;
                            obj.PrimarySkills = item.JDTiltle.Split("|")[0];
                            //obj.ProjectId = 0;
                            obj.Client = item.ClientCompName;
                            objList.Add(obj);
                        }
                        projectModel.ProjectIRFMappings = (IList<ProjectIRFMapping>)(objList);
                        if (projectModel.ProjectIRFMappings.Count() == 0)
                        {
                            return new EmptyResult();
                        }

                        return PartialView("GetFilteredIRF", projectModel);
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
        public IActionResult GetProjectInterviewMeetings(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    var irfList = _projectService.GetProjectCluster(id);
                    var meetings = new List<MeetingVM>();
                    foreach (var irf in irfList)
                    {
                        var irfMeetings = _irfService.GetIRFInterviewMeetings(irf.IRFId, profileName, emailid);
                        if(irfMeetings !=null)
                        {
                            foreach (var item in irfMeetings)
                            {
                                item.irfIntId = irf.IRFId;
                                meetings.Add(item);
                            }
                        }
                       
                    }
                    
                    //meetings = meetings.Where(x => x.interviewDate.Date == DateTime.Now.Date).ToList();
                    meetings = meetings.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((ReceiveMeetingStatus)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();
                    //ViewBag.IRF = irfId;
                    //ViewBag.IRFintId = id;
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
                    ViewBag.ProjectName = irfList.FirstOrDefault().ProjectName;

                    return View(meetings);

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
        public IActionResult GetSelectedInterviewMeetings(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    var irfList = _projectService.GetProjectCluster(id);
                    var selectedMeeting = new List<MeetingVM>();
                    var meetings = new List<MeetingVM>();
                    foreach (var irf in irfList)
                    {
                        var irfMeetings = _irfService.GetIRFInterviewMeetings(irf.IRFId, profileName, emailid);
                        if (irfMeetings != null)
                        {
                            foreach (var item in irfMeetings)
                            {
                                item.irfIntId = irf.IRFId;
                                meetings.Add(item);
                            }
                        }

                    }

                    //meetings = meetings.Where(x => x.interviewDate.Date == DateTime.Now.Date).ToList();
                    meetings = meetings.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((ReceiveMeetingStatus)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();
                    //ViewBag.IRF = irfId;
                    //ViewBag.IRFintId = id;
                    //ViewBag.IsSelected = false;
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
                    ViewBag.ProjectName = irfList.FirstOrDefault().ProjectName;
                    ViewBag.ProfileName = profileName.ToLower();


                    return View(selectedMeeting);

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


    }
}