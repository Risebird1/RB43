using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using Risebird.CommonHelper;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;

namespace Risebird.Web.Controllers
{
    [Authorize]
    public class InterviewerSkillsController : Controller
    {
        private IInterviewerSkillsRepository _InterviewerSkillsRepository;
        private IInterviewerSkillsService _IInterviewerSkillsService;
        private IEvaluatorService _evaluatorService;
        private IAzureStorageHelper _azureStorageHelper;
        private IMeetingService _meetingService;
        private IApplicationErrorLogger _applicationErrorLogger;
        public InterviewerSkillsController(IInterviewerSkillsRepository InterviewerSkillsRepository, IApplicationErrorLogger applicationErrorLogger, IInterviewerSkillsService InterviewerSkillsService, IMeetingService meetingService, IAzureStorageHelper azureStorageHelper, IEvaluatorService evaluatorService)
        {
            _InterviewerSkillsRepository = InterviewerSkillsRepository;
            _IInterviewerSkillsService = InterviewerSkillsService;
            _applicationErrorLogger = applicationErrorLogger;
            _meetingService = meetingService;
            _azureStorageHelper = azureStorageHelper;
            _evaluatorService = evaluatorService;
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
                if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "Recruiter")
                {
                    var model = new InterviewerSkillsViewModel();
                    var InterviewerSkillsList = _IInterviewerSkillsService.GetAllInterviewerSkills(emailid, profileName);
                    InterviewerSkillsList.ToList().ForEach(x => x.ArchiveStatus = ((ArchiveStatus)Convert.ToInt16(x.IsDeleted)).GetDescription());
                    model.InterviewerSkillsModelList = InterviewerSkillsList.Where(x => x.EndDate.Date > DateTime.Now).OrderByDescending(y => y.EndDate);
                    foreach (var item in model.InterviewerSkillsModelList)
                    {
                        if (item.IsPaused)
                        {
                            item.ImageUrl = "/img/Cards_Png/01_Grey-White.png";
                            item.IRFStatus = "Paused";
                        }
                        else
                        {
                            item.ImageUrl = "/img/Cards_Png/01_Green-White.png";
                            item.IRFStatus = "Active";
                        }
                    }
                    if (profileName != null && profileName.Contains("Client"))
                    {
                        model.IsClientUser = true;
                    }

                    if (TempData["SuccessErrorMessage"] != null)
                    {
                        model.Message = TempData["SuccessErrorMessage"].ToString();
                        TempData.Remove("SuccessErrorMessage");
                    }
                    if (HttpContext.Session.GetString("IRFfilters") != null)
                    {
                        var filter = JsonConvert.DeserializeObject<InterviewerSkillsFilter>(HttpContext.Session.GetString("IRFfilters"));
                        if (filter.StartDate != null && filter.EndDate != null)
                        {
                            model.InterviewerSkillsModelList = InterviewerSkillsList.Where(t => t.StartDate >= filter.StartDate && t.EndDate <= filter.EndDate).ToList();
                        }
                        else
                        {
                            model.InterviewerSkillsModelList = InterviewerSkillsList.Where(x => x.EndDate >= DateTime.Now);
                        }
                        filter.ISID = filter.ISID == null ? filter.ISID = model.InterviewerSkillsModelList.Select(x => x.InterviewerSkillId).ToArray() : filter.ISID = filter.ISID;
                        filter.ArchiveStatus = filter.ArchiveStatus == null ? filter.ArchiveStatus = model.InterviewerSkillsModelList.Select(x => x.ArchiveStatus).ToArray() : filter.ArchiveStatus;
                        filter.ClientName = filter.ClientName == null ? filter.ClientName = model.InterviewerSkillsModelList.Select(x => x.ClientName).ToArray() : filter.ClientName = filter.ClientName;


                        model.InterviewerSkillsModelList = model.InterviewerSkillsModelList.Where(c => filter.ISID.Contains(c.InterviewerSkillId)
                        && (filter.ArchiveStatus.Count() > 0 ? filter.ArchiveStatus.Contains(c.ArchiveStatus) : c.ArchiveStatus == null)
                        && (filter.ClientName.Count() > 0 ? filter.ClientName.Contains(c.ClientName) : c.ClientName == null)).ToList();

                        if (model.InterviewerSkillsModelList.Count() == 0 && filter.StartDate != null && filter.EndDate != null)
                        {
                            model.InterviewerSkillsModelList = InterviewerSkillsList.Where(t => t.StartDate.Date >= filter.StartDate && t.EndDate.Date <= filter.EndDate).ToList();
                        }
                        foreach (var item in model.InterviewerSkillsModelList)
                        {
                            if (item.IsPaused)
                            {
                                item.ImageUrl = "/img/Cards_Png/01_Grey-White.png";
                                item.IRFStatus = "Paused";
                            }
                            else if (item.EndDate >= DateTime.Now)
                            {
                                item.ImageUrl = "/img/Cards_Png/01_Green-White.png";
                                item.IRFStatus = "Active";
                            }
                            else
                            {
                                item.ImageUrl = "/img/Cards_Png/01_Black-White.png";
                                item.IRFStatus = "Archived";
                            }
                        }
                        model.InterviewerSkillsModelList = model.InterviewerSkillsModelList.OrderByDescending(x => x.EndDate).ToList();
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

        [Authorize]
        [HttpPost]
        public IActionResult Index(InterviewerSkillsFilter filter)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                if (filter != null)
                {
                    HttpContext.Session.SetString("IRFfilters", JsonConvert.SerializeObject(filter));
                }
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "Recruiter")
                {

                    var model = new InterviewerSkillsViewModel();
                    var InterviewerSkillsList = _IInterviewerSkillsService.GetAllInterviewerSkills(emailid, profileName);
                    InterviewerSkillsList.ToList().ForEach(x => x.ArchiveStatus = ((ArchiveStatus)Convert.ToInt16(x.IsDeleted)).GetDescription());
                    if (filter.StartDate != null && filter.EndDate != null)
                    {
                        InterviewerSkillsList = InterviewerSkillsList.Where(t => t.StartDate.Date >= filter.StartDate && t.EndDate.Date <= filter.EndDate).ToList();
                    }
                    else
                    {
                        InterviewerSkillsList = InterviewerSkillsList.Where(x => x.EndDate.Date > DateTime.Now);
                    }
                    if (profileName != null && profileName.Contains("Client"))
                    {
                        model.IsClientUser = true;
                    }

                    filter.ISID = filter.ISID == null ? filter.ISID = InterviewerSkillsList.Select(x => x.InterviewerSkillId).ToArray() : filter.ISID = filter.ISID;
                    filter.ArchiveStatus = filter.ArchiveStatus == null ? filter.ArchiveStatus = InterviewerSkillsList.Select(x => x.ArchiveStatus).ToArray() : filter.ArchiveStatus;
                    filter.ClientName = filter.ClientName == null ? filter.ClientName = InterviewerSkillsList.Select(x => x.ClientName).ToArray() : filter.ClientName = filter.ClientName;


                    InterviewerSkillsList = InterviewerSkillsList.Where(c => filter.ISID.Contains(c.InterviewerSkillId)
                    && (filter.ArchiveStatus.Count() > 0 ? filter.ArchiveStatus.Contains(c.ArchiveStatus) : c.ArchiveStatus == null)
                    && (filter.ClientName.Count() > 0 ? filter.ClientName.Contains(c.ClientName) : c.ClientName == null)).ToList();

                    if (InterviewerSkillsList.Count() == 0 && filter.StartDate != null && filter.EndDate != null)
                    {
                        InterviewerSkillsList = _IInterviewerSkillsService.GetAllInterviewerSkills(emailid, profileName).Where(x => x.StartDate.Date >= filter.StartDate && x.EndDate.Date <= filter.EndDate);
                        InterviewerSkillsList.ToList().ForEach(x => x.ArchiveStatus = ((ArchiveStatus)Convert.ToInt16(x.IsDeleted)).GetDescription());

                    }
                    foreach (var item in InterviewerSkillsList)
                    {
                        if (item.IsPaused)
                        {
                            item.ImageUrl = "/img/Cards_Png/01_Grey-White.png";
                            item.IRFStatus = "Paused";
                        }
                        else if (item.EndDate >= DateTime.Now)
                        {
                            item.ImageUrl = "/img/Cards_Png/01_Green-White.png";
                            item.IRFStatus = "Active";
                        }
                        else
                        {
                            item.ImageUrl = "/img/Cards_Png/01_Black-White.png";
                            item.IRFStatus = "Archived";
                        }
                    }
                    model.InterviewerSkillsModelList = InterviewerSkillsList.OrderByDescending(x => x.EndDate).ToList();
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
    }
}