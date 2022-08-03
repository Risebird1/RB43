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
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using X.PagedList;
namespace Risebird.Web.Controllers
{
    public class ProjectController : Controller
    {
        private IAzureStorageHelper _azureStorageHelper;
        private IIRFRepository _irfRepository;
        private IIRFService _irfService;
        private IApplicationErrorLogger _applicationErrorLogger;
        private IProjectService _projectService;
        private static List<MeetingVM> _modelDetails;
        private static string Searchvalue;
        public ProjectController(IApplicationErrorLogger applicationErrorLogger, IAzureStorageHelper azureStorageHelper, IProjectService projectService, IIRFService irfService, IIRFRepository irfRepository)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _azureStorageHelper = azureStorageHelper;
            _projectService = projectService;
            _irfService = irfService;
            _irfRepository = irfRepository;
        }

        [Authorize]
        [HttpGet]
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
                    var model = new ProjectViewModel();
                    var projectList = _projectService.GetProjects(emailid, profileName).ToList();
                    projectList.ForEach(x => x.ArchiveStatus = ((ArchiveStatus)Convert.ToInt32(x.IsDeleted)).GetDescription());

                    model.ProjectList = projectList;

                    if (TempData["SuccessErrorMessageProject"] != null)
                    {
                        model.Message = TempData["SuccessErrorMessageProject"].ToString();
                        TempData.Remove("SuccessErrorMessageProject");
                    }
                    if (HttpContext.Session.GetString("Projectfilters") != null)
                    {
                        var filter = JsonConvert.DeserializeObject<ProjectFilter>(HttpContext.Session.GetString("Projectfilters"));
                        if (filter.StartDate != null && filter.EndDate != null)
                        {
                            model.ProjectList = model.ProjectList.Where(t => t.CreatedOn.Date >= filter.StartDate && t.CreatedOn.Date <= filter.EndDate).ToList();
                        }

                        filter.Location = filter.Location == null ? filter.Location = model.ProjectList.Select(x => x.Location).ToArray() : filter.Location = filter.Location;
                        filter.ProjectID = filter.ProjectID == null ? filter.ProjectID = model.ProjectList.Select(x => x.Name).ToArray() : filter.ProjectID = filter.ProjectID;
                        filter.ArchiveStatus = filter.ArchiveStatus == null ? filter.ArchiveStatus = model.ProjectList.Select(x => x.ArchiveStatus).ToArray() : filter.ArchiveStatus;
                        filter.ClientName = filter.ClientName == null ? filter.ClientName = model.ProjectList.Select(x => x.EndClient).ToArray() : filter.ClientName = filter.ClientName;

                        //irfList = irfList.Where(x => filter.ClientRole.Intersect(x.ClientRole).Any());
                        model.ProjectList = model.ProjectList.Where(c => filter.Location.Contains(c.Location)
                        && filter.ProjectID.Contains(c.Name)
                        && (filter.ArchiveStatus.Count() > 0 ? filter.ArchiveStatus.Contains(c.ArchiveStatus) : c.ArchiveStatus == null)
                        && (filter.ClientName.Count() > 0 ? filter.ClientName.Contains(c.EndClient) : c.EndClient == null)).ToList();

                        if (model.ProjectList.Count() == 0 && filter.StartDate != null && filter.EndDate != null)
                        {
                            model.ProjectList = projectList;
                            model.ProjectList = model.ProjectList.Where(t => t.CreatedOn.Date >= filter.StartDate && t.CreatedOn.Date <= filter.EndDate).ToList();

                        }

                    }

                    if (profileName != null && profileName.Contains("Client"))
                    {
                        model.IsClientUser = true;
                    }
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

        [Authorize]
        [HttpPost]
        public IActionResult Index(ProjectFilter filter)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                if (filter != null)
                {
                    HttpContext.Session.SetString("Projectfilters", JsonConvert.SerializeObject(filter));
                }
                return RedirectToAction("Index");
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ResetProjectFilter()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("Projectfilters");
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

                projectModel.ClientList = new SelectList(_projectService.GetClients(), "id", "Name");
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
                        TempData["SuccessErrorMessageProject"] = "Project has been added Successfully!";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["SuccessErrorMessageProject"] = "Failed to add Project!";
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
                TempData["SuccessErrorMessageProject"] = "Adding Project has failed";
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        [HttpGet]
        public IActionResult Edit(int id)
        {
            if (HttpContext.Session.GetString("email") != null)
            {

                var projectModel = _projectService.GetProjectEdit(id);
                projectModel.ClientList = new SelectList(_projectService.GetClients(), "id", "Name");
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
                    if (_projectService.UpdateProject(model))
                    {
                        TempData["SuccessErrorMessageProject"] = "Project has been edited Successfully!";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["SuccessErrorMessageProject"] = "Failed to edit Project!";
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
                TempData["SuccessErrorMessageProject"] = "Editing Project has failed";
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        [HttpGet]
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
                        if (_projectService.DeleteProject(Convert.ToInt32(id)).Result)
                        {
                            //TempData["SuccessErrorMessage"] = "Project deleted Successfully";
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            //TempData["SuccessErrorMessage"] = "Project delete Failed";
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

        [Authorize]
        [HttpGet]
        public IActionResult UnArchiveProject(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR")
                    {
                        if (_projectService.UnArchiveProject(id).Result)
                        {
                            TempData["SuccessErrorMessageProject"] = "Project has been restored Successfully!";
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            TempData["SuccessErrorMessageProject"] = "Failed to restore Project!";
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
                TempData["SuccessErrorMessage"] = "Project restore Failed";
                return RedirectToAction("Index");
            }

        }

        [HttpGet]
        public async Task<IActionResult> GetFilteredIRF(string clientId)
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
                        var irfList = _projectService.GetAllIRF();
                        if (Convert.ToInt32(clientId) != 0)
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

        [HttpGet]
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

                        if (projectCluster != null && projectCluster.Count() > 0)
                        {
                            if (Convert.ToInt32(clientId) != 0)
                            {
                                projectCluster = projectCluster.Where(x => x.ClientId == Convert.ToInt32(clientId)).ToList();

                            }
                            if (projectCluster != null)
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
        public async Task<IActionResult> ProjectInterviewMeetings(int id, int? Page, string searchValue)
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
                        var irfMeetings = await _irfService.GetIRFInterviewMeetings(irf.IRFId, profileName, emailid);
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




                    if (!String.IsNullOrEmpty(searchValue))
                    {
                        searchValue = searchValue.ToUpper();
                        meetings = meetings.Where(x => (!String.IsNullOrEmpty(x.candidateName) ? x.candidateName.ToUpper().Contains(searchValue) : x.candidateName == "") || (!String.IsNullOrEmpty(x.confirmationStatus) ? x.confirmationStatus.ToUpper().Contains(searchValue) : x.confirmationStatus == "") || (!String.IsNullOrEmpty(x.intervieweStatusStatDesc) ? x.intervieweStatusStatDesc.ToUpper().Contains(searchValue) : x.intervieweStatusStatDesc == "") || (!String.IsNullOrEmpty(x.MeetingStatusStatusDesc) ? x.MeetingStatusStatusDesc.ToUpper().Contains(searchValue) : x.MeetingStatusStatusDesc == "") || (!String.IsNullOrEmpty(x.ClientHrContact) ? x.ClientHrContact.ToUpper().Contains(searchValue) : x.ClientHrContact == "")).ToList();

                    }
                    ViewBag.TotalCount = meetings.Count();

                    ViewBag.PageStartCount = 10 * ((Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page)) - 1) + 1;
                    int a = meetings.Count() / 10;
                    if (meetings.Count() % 10 == 0)
                    {
                        ViewBag.PageEndCount = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
                    }
                    else
                    {
                        a = a + 1;
                        if (a == Page)
                        {
                            ViewBag.PageEndCount = meetings.Count();
                        }
                        else
                        {
                            ViewBag.PageEndCount = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));

                        }

                    }
                    if (meetings.Count() == 0)
                    {
                        ViewBag.PageStartCount = 0;
                        ViewBag.PageEndCount = 0;
                    }
                    _modelDetails = meetings;
                    return View(meetings.ToPagedList(Page ?? 1, 10));
                    // return View(meetings);

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
        [HttpGet]
        public async Task<IActionResult> SelectedInterviewMeetings(int id, int? Page, string searchValue)
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
                        var irfMeetings = await _irfService.GetIRFInterviewMeetings(irf.IRFId, profileName, emailid);
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
                    ViewBag.profileName = profileName.ToLower();






                    if (!String.IsNullOrEmpty(searchValue))
                    {
                        searchValue = searchValue.ToUpper();
                        selectedMeeting = selectedMeeting.Where(x => (!String.IsNullOrEmpty(x.candidateName) ? x.candidateName.ToUpper().Contains(searchValue) : x.candidateName == "") || (!String.IsNullOrEmpty(x.ClientHrContact) ? x.ClientHrContact.ToUpper().Contains(searchValue) : x.ClientHrContact == "") || (!String.IsNullOrEmpty(x.confirmationStatus) ? x.confirmationStatus.ToUpper().Contains(searchValue) : x.confirmationStatus == "") || (!String.IsNullOrEmpty(x.MeetingStatusStatusDesc) ? x.MeetingStatusStatusDesc.ToUpper().Contains(searchValue) : x.MeetingStatusStatusDesc == "") || (!String.IsNullOrEmpty(x.intervieweStatusStatDesc) ? x.intervieweStatusStatDesc.ToUpper().Contains(searchValue) : x.intervieweStatusStatDesc == "")).ToList();

                    }
                    ViewBag.TotalCount = selectedMeeting.Count;

                    ViewBag.PageStartCount = 10 * ((Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page)) - 1) + 1;
                    int a = selectedMeeting.Count / 10;
                    if (selectedMeeting.Count % 10 == 0)
                    {
                        ViewBag.PageEndCount = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
                    }
                    else
                    {
                        a = a + 1;
                        if (a == Page)
                        {
                            ViewBag.PageEndCount = selectedMeeting.Count;
                        }
                        else
                        {
                            ViewBag.PageEndCount = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));

                        }

                    }
                    if (selectedMeeting.Count == 0)
                    {
                        ViewBag.PageStartCount = 0;
                        ViewBag.PageEndCount = 0;
                    }
                    _modelDetails = selectedMeeting;
                    return View(selectedMeeting.ToPagedList(Page ?? 1, 10));

                    //   return View(selectedMeeting);

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
        [HttpGet]
        public IActionResult Stats(string id, string name)
        {
            //TempData["id"] = id;
            //TempData["irfId"] = irfId;
            //TempData.Keep();
            ViewBag.ProjectId = id;
            ViewBag.ProjectName = name;
            return View();

        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> NewChart(string id)
        {
            try
            {
                //string id = TempData.Peek("id").ToString();
                //string irfId = TempData.Peek("irfId").ToString();
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                    {
                        var irfList = _projectService.GetProjectCluster(Convert.ToInt32(id));
                        var meetings = new List<MeetingVM>();
                        foreach (var irf in irfList)
                        {
                            var irfMeetings = await _irfService.GetIRFInterviewMeetings(irf.IRFId, profileName, emailid);
                            if (irfMeetings != null)
                            {
                                foreach (var item in irfMeetings)
                                {
                                    item.irfIntId = irf.IRFId;
                                    meetings.Add(item);
                                }
                            }

                        }
                        var noShowCount = meetings.Where(x => x.MeetingStatus.HasValue && x.MeetingStatus.Value == 1).Count();
                        var ScheduledCount = meetings.Where(x => !x.MeetingStatus.HasValue && x.interviewDate >= DateTime.Now && x.confirmationStatus == "1").Count();
                        var DroppedCount = meetings.Where(x => x.MeetingStatus.HasValue && x.MeetingStatus.Value == 3).Count();
                        var CompletedCount = meetings.Where(x => x.MeetingStatus.HasValue && x.MeetingStatus.Value == 4).Count();


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
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }



        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> NewChartSelected(string id)
        {
            try
            {

                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                    {
                        //var selectedMeeting = new List<MeetingVM>();
                        var irfList = _projectService.GetProjectCluster(Convert.ToInt32(id));
                        var meetings = new List<MeetingVM>();
                        foreach (var irf in irfList)
                        {
                            var irfMeetings = await _irfService.GetIRFInterviewMeetings(irf.IRFId, profileName, emailid);
                            if (irfMeetings != null)
                            {
                                foreach (var item in irfMeetings)
                                {
                                    item.irfIntId = irf.IRFId;
                                    meetings.Add(item);
                                }
                            }

                        }
                        var ProxyCount = meetings.Where(x => x.InterviewStatus.HasValue && x.InterviewStatus.Value == 1).Count();
                        var SelectedCount = meetings.Where(x => x.InterviewStatus.HasValue && x.InterviewStatus.Value == 2).Count();
                        var RejectedCount = meetings.Where(x => x.InterviewStatus.HasValue && x.InterviewStatus.Value == 3).Count();
                        var BorderLineCount = meetings.Where(x => x.InterviewStatus.HasValue && x.InterviewStatus.Value == 4).Count();

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
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> NewChartClientMeetingData(string id)
        {
            try
            {
                //string id = TempData.Peek("id").ToString();
                //string irfId = TempData.Peek("irfId").ToString();
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                    {
                        //var selectedMeeting = new List<MeetingVM>();
                        var irfList = _projectService.GetProjectCluster(Convert.ToInt32(id));
                        var meetings = new List<MeetingVM>();
                        foreach (var irf in irfList)
                        {
                            var irfMeetings = await _irfService.GetIRFInterviewMeetings(irf.IRFId, profileName, emailid);
                            if (irfMeetings != null)
                            {
                                foreach (var item in irfMeetings)
                                {
                                    item.irfIntId = irf.IRFId;
                                    meetings.Add(item);
                                }
                            }

                        }
                        var clientList = new List<ClientData>();
                        var chartData = new List<ClientChartData>();

                        foreach (var irf in irfList)
                        {
                            var clientUserList = _irfRepository.GetTransClientUser(irf.IRFId);
                            foreach (var item in clientUserList)
                            {
                                if (item.ClientRoleId == 1 || item.ClientRoleId == 4)
                                {
                                    var clientObj = new ClientData();
                                    clientObj.EmailId = item.EmailId;
                                    clientObj.IRFId = irf.IRFId;
                                    clientObj.IRFName = irf.IRFName;
                                    clientList.Add(clientObj);

                                }

                            }
                        }
                        //var clientUserList = _irfRepository.GetTransClientUser(Convert.ToInt32(id));

                        foreach (var item in clientList)
                        {
                            var obj = new ClientChartData();
                            obj.Name = item.EmailId + "(" + item.IRFName + ")";
                            obj.NoShowCount = meetings.Where(x => x.MeetingStatus.HasValue && x.MeetingStatus.Value == 1 && (x.ClientHrContact == item.EmailId || x.staffingAgency == item.EmailId) && x.irfIntId == item.IRFId).Count();
                            obj.ScheduledCount = meetings.Where(x => !x.MeetingStatus.HasValue && x.interviewDate >= DateTime.Now && x.confirmationStatus == "1" && (x.ClientHrContact == item.EmailId || x.staffingAgency == item.EmailId) && x.irfIntId == item.IRFId).Count();
                            obj.CompletedCount = meetings.Where(x => x.MeetingStatus.HasValue && x.MeetingStatus.Value == 4 && (x.ClientHrContact == item.EmailId || x.staffingAgency == item.EmailId) && x.irfIntId == item.IRFId).Count();
                            obj.DroppedCount = meetings.Where(x => x.MeetingStatus.HasValue && x.MeetingStatus.Value == 3 && (x.ClientHrContact == item.EmailId || x.staffingAgency == item.EmailId) && x.irfIntId == item.IRFId).Count();
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



                        //ViewBag.IRF = irfId;
                        //ViewBag.IRFintId = id;

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
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> NewChartClientFeedbackData(string id)
        {
            try
            {
                //string id = TempData.Peek("id").ToString();
                //string irfId = TempData.Peek("irfId").ToString();
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                    {
                        //var selectedMeeting = new List<MeetingVM>();
                        var irfList = _projectService.GetProjectCluster(Convert.ToInt32(id));
                        var meetings = new List<MeetingVM>();
                        foreach (var irf in irfList)
                        {
                            var irfMeetings = await _irfService.GetIRFInterviewMeetings(irf.IRFId, profileName, emailid);
                            if (irfMeetings != null)
                            {
                                foreach (var item in irfMeetings)
                                {
                                    item.irfIntId = irf.IRFId;
                                    meetings.Add(item);
                                }
                            }

                        }
                        var clientList = new List<ClientData>();
                        var chartData = new List<ClientChartFeedbackData>();
                        foreach (var irf in irfList)
                        {
                            var clientUserList = _irfRepository.GetTransClientUser(irf.IRFId);
                            foreach (var item in clientUserList)
                            {
                                if (item.ClientRoleId == 1 || item.ClientRoleId == 4)
                                {
                                    var clientObj = new ClientData();
                                    clientObj.EmailId = item.EmailId;
                                    clientObj.IRFId = irf.IRFId;
                                    clientObj.IRFName = irf.IRFName;
                                    clientList.Add(clientObj);

                                }

                            }
                        }
                        foreach (var item in clientList)
                        {
                            var obj = new ClientChartFeedbackData();
                            obj.Name = item.EmailId + "(" + item.IRFName + ")";
                            obj.ProxyCount = meetings.Where(x => x.InterviewStatus.HasValue && x.InterviewStatus.Value == 1 && (x.ClientHrContact == item.EmailId || x.staffingAgency == item.EmailId) && x.irfIntId == item.IRFId).Count();
                            obj.SelectedCount = meetings.Where(x => x.InterviewStatus.HasValue && x.InterviewStatus.Value == 2 && (x.ClientHrContact == item.EmailId || x.staffingAgency == item.EmailId) && x.irfIntId == item.IRFId).Count(); ;
                            obj.RejectedCount = meetings.Where(x => x.InterviewStatus.HasValue && x.InterviewStatus.Value == 3 && (x.ClientHrContact == item.EmailId || x.staffingAgency == item.EmailId) && x.irfIntId == item.IRFId).Count();
                            obj.BorderLineCount = meetings.Where(x => x.InterviewStatus.HasValue && x.InterviewStatus.Value == 4 && (x.ClientHrContact == item.EmailId || x.staffingAgency == item.EmailId) && x.irfIntId == item.IRFId).Count();
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



                        //ViewBag.IRF = irfId;
                        //ViewBag.IRFintId = id;

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
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        [HttpGet]
        public IActionResult FunnelChart(string id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                    {

                        var chartData = _projectService.GetFunnelData(Convert.ToInt32(id));
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

        [HttpPost]
        public JsonResult ProjectInterviewMeetingsJson(string searchValue)
        {
            try
            {
                int Page = 1;
                var meetings = _modelDetails;
                Searchvalue = searchValue;
                if (!String.IsNullOrEmpty(searchValue))
                {
                    searchValue = searchValue.ToUpper();
                    meetings = meetings.Where(x => (!String.IsNullOrEmpty(x.candidateName) ? x.candidateName.ToUpper().Contains(searchValue) : x.candidateName == "") || (!String.IsNullOrEmpty(x.confirmationStatus) ? x.confirmationStatus.ToUpper().Contains(searchValue) : x.confirmationStatus == "") || (!String.IsNullOrEmpty(x.intervieweStatusStatDesc) ? x.intervieweStatusStatDesc.ToUpper().Contains(searchValue) : x.intervieweStatusStatDesc == "") || (!String.IsNullOrEmpty(x.MeetingStatusStatusDesc) ? x.MeetingStatusStatusDesc.ToUpper().Contains(searchValue) : x.MeetingStatusStatusDesc == "") || (!String.IsNullOrEmpty(x.ClientHrContact) ? x.ClientHrContact.ToUpper().Contains(searchValue) : x.ClientHrContact == "")).ToList();

                }
                int totalcount = meetings.Count;
                int Pagestart = 0;
                int Pageend = 0;

                Pagestart = 10 * ((Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page)) - 1) + 1;
                int a = meetings.Count / 10;
                if (meetings.Count % 10 == 0)
                {
                    Pageend = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
                }
                else
                {
                    a = a + 1;
                    if (a == Page)
                    {
                        Pageend = meetings.Count;
                    }
                    else
                    {
                        Pageend = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
                    }
                }
                if (meetings.Count == 0)
                {
                    Pagestart = 0;
                    Pageend = 0;
                }
                return Json(new { result = meetings.Take(10), startPage = Pagestart, endPage = Pageend, totalPage = a, totalentry = meetings.Count });
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("failed");
            }
        }

        [HttpPost]
        public JsonResult SelectedInterviewMeetingsJson(string searchValue)
        {
            int Page = 1;
            var selectedMeeting = _modelDetails;
            if (!String.IsNullOrEmpty(searchValue))
            {
                searchValue = searchValue.ToUpper();
                selectedMeeting = selectedMeeting.Where(x => (!String.IsNullOrEmpty(x.candidateName) ? x.candidateName.ToUpper().Contains(searchValue) : x.candidateName == "") || (!String.IsNullOrEmpty(x.ClientHrContact) ? x.ClientHrContact.ToUpper().Contains(searchValue) : x.ClientHrContact == "") || (!String.IsNullOrEmpty(x.confirmationStatus) ? x.confirmationStatus.ToUpper().Contains(searchValue) : x.confirmationStatus == "") || (!String.IsNullOrEmpty(x.MeetingStatusStatusDesc) ? x.MeetingStatusStatusDesc.ToUpper().Contains(searchValue) : x.MeetingStatusStatusDesc == "") || (!String.IsNullOrEmpty(x.intervieweStatusStatDesc) ? x.intervieweStatusStatDesc.ToUpper().Contains(searchValue) : x.intervieweStatusStatDesc == "") || (!String.IsNullOrEmpty(x.confirmationStatus) ? x.confirmationStatus.ToUpper().Contains(searchValue) : x.confirmationStatus == "") || (!String.IsNullOrEmpty(x.MeetingStatusStatusDesc) ? x.MeetingStatusStatusDesc.ToUpper().Contains(searchValue) : x.MeetingStatusStatusDesc == "") || (!String.IsNullOrEmpty(x.intervieweStatusStatDesc) ? x.intervieweStatusStatDesc.ToUpper().Contains(searchValue) : x.intervieweStatusStatDesc == "")).ToList();

            }
            int totalcount = selectedMeeting.Count;
            int Pagestart = 0;
            int Pageend = 0;
            ViewBag.TotalCount = selectedMeeting.Count;

            Pagestart = 10 * ((Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page)) - 1) + 1;
            int a = selectedMeeting.Count / 10;
            if (selectedMeeting.Count % 10 == 0)
            {
                Pageend = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
            }
            else
            {
                a = a + 1;
                if (a == Page)
                {
                    Pageend = selectedMeeting.Count;
                }
                else
                {
                    Pageend = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));

                }

            }
            if (selectedMeeting.Count == 0)
            {
                Pagestart = 0;
                Pageend = 0;
            }

            return Json(new { result = selectedMeeting.Take(10), startPage = Pagestart, endPage = Pageend, totalPage = a, totalentry = selectedMeeting.Count });
        }



    }
}