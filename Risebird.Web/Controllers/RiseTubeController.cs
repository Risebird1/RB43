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
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using X.PagedList;

namespace Risebird.Web.Controllers
{
    [Authorize]
    public class RiseTubeController : Controller
    {

        private IMeetingService _meetingService;
        private IApplicationErrorLogger _applicationErrorLogger;
        private IAzureStorageHelper _azureStorageHelper;
        private ISkillService _skillService;
        private IIRFService _iRFService;
        private IIRFRepository _iRFRepository;
        private IUserRepository _userRepository;
        private INotificationHelper _notificationHelper;

        public RiseTubeController(IMeetingService meetingService, IApplicationErrorLogger applicationErrorLogger, IAzureStorageHelper azureStorageHelper, ISkillService skillService, IIRFRepository iRFRepository, IIRFService iRFService, IUserRepository userRepository, INotificationHelper notificationHelper)
        {
            _meetingService = meetingService;
            _applicationErrorLogger = applicationErrorLogger;
            _azureStorageHelper = azureStorageHelper;
            _skillService = skillService;
            _iRFRepository = iRFRepository;
            _iRFService = iRFService;
            _userRepository = userRepository;
            _notificationHelper = notificationHelper;
        }


        /// <summary>
        /// Method to show all list of video if confirmation status is 1 or Selected and video link not equals to null on index page
        /// </summary>
        /// <param name="Page"></param>
        /// <returns></returns>
        [Authorize]
        public async Task<IActionResult> Index(int? Page, string searchvalue)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    bool isClientUser = false;
                    int ClientId = 0;
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "CuratedUser")
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        if (profileName == "Client")
                        {
                            ClientId = _userRepository.GetClientUserDetails(emailid).ClientId;
                            isClientUser = true;
                        }
                        if (TempData["SuccessErrorMessageInterview"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageInterview"].ToString();
                            TempData.Remove("SuccessErrorMessageInterview");
                        }
                        var modelList = new List<MeetingVM>();

                        if (isClientUser)
                        {
                            modelList = _meetingService.GetRiseTubeMeetings(ClientId, profileName, emailid).ToList();
                        }
                        else if (profileName == "CuratedUser")
                        {
                            modelList = _meetingService.GetRiseTubeMeetings(0, profileName, emailid).ToList().Where(x => x.CuratedLink).ToList();
                        }
                        else
                        {
                            modelList = _meetingService.GetRiseTubeMeetings(0, profileName, emailid).ToList();
                        }

                        ViewBag.CandidateList = modelList.Select(x => x.candidateName).Distinct().ToList();
                        ViewBag.PrimarySkillList = modelList.Select(x => x.JDTiltle).Distinct().ToList();
                        ViewBag.ClientHrContactList = modelList.Select(x => x.ClientHrContact).Distinct().ToList();
                        ViewBag.preferredLocationList = modelList.Select(x => x.preferredLocation).Distinct().ToList();
                        ViewBag.ClientNameList = modelList.Select(x => x.ClientName).Distinct().ToList();
                        ViewBag.IsShortListed = modelList.Select(x => x.ShortListstatus).Distinct().ToList();
                        ViewBag.IsIntrested = modelList.Select(x => x.IsIntrested).Distinct().ToList();
                        ViewBag.NextLevelApprovalStatusList =
                            from NextLevelApprovalStatus s in Enum.GetValues(typeof(NextLevelApprovalStatus))
                            select (s.GetDescription());
                        ViewBag.JoiningStatusList =
                            from JoiningStatus s in Enum.GetValues(typeof(JoiningStatus))
                            select (s.GetDescription());
                        ViewBag.OfferStatusList =
                           from OfferStatus s in Enum.GetValues(typeof(OfferStatus))
                           select (s.GetDescription());
                        modelList = modelList.Select(x =>
                        {
                            x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty;
                            return x;
                        }).ToList();
                        if (HttpContext.Session.GetString("RiseTubefilters") != null)
                        {
                            var filter = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("RiseTubefilters"));
                            if (filter.StartDate != null && filter.EndDate != null)
                            {
                                modelList = modelList.Where(t => t.interviewDate.Date >= filter.StartDate && t.interviewDate.Date <= filter.EndDate).OrderByDescending(x => x.interviewDate).ToList();
                            }
                            modelList = modelList.Select(x =>
                            {
                                x.NextLevelApprovalStat = x.IsApproved.HasValue ? ((NextLevelApprovalStatus)x.IsApproved).GetDescription() : string.Empty;
                                x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty;
                                return x;
                            }).ToList();

                            filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = modelList.Select(x => x.JDTiltle.Split("|")[0]).ToArray() : filter.PrimarySkill = filter.PrimarySkill;
                            filter.ClientHR = filter.ClientHR == null ? filter.ClientHR = modelList.Select(x => x.ClientHrContact).ToArray() : filter.ClientHR = filter.ClientHR;
                            filter.InterviewStatus = filter.InterviewStatus == null ? filter.InterviewStatus = modelList.Select(x => x.intervieweStatusStatDesc).ToArray() : filter.InterviewStatus = filter.InterviewStatus;
                            filter.JoiningStatus = filter.JoiningStatus == null ? filter.JoiningStatus = modelList.Select(x => x.JoiningStatusStatDesc).ToArray() : filter.JoiningStatus = filter.JoiningStatus;
                            filter.OfferStatus = filter.OfferStatus == null ? filter.OfferStatus = modelList.Select(x => x.OfferStatusStatDesc).ToArray() : filter.OfferStatus = filter.OfferStatus;
                            filter.CandidateName = filter.CandidateName == null ? filter.CandidateName = modelList.Select(x => x.candidateName).ToArray() : filter.CandidateName = filter.CandidateName;
                            filter.ClientName = filter.ClientName == null ? filter.ClientName = modelList.Select(x => x.ClientName).ToArray() : filter.ClientName = filter.ClientName;
                            filter.ApprovalStatus = filter.ApprovalStatus == null ? filter.ApprovalStatus = modelList.Select(x => x.NextLevelApprovalStat).ToArray() : filter.ApprovalStatus = filter.ApprovalStatus;
                            filter.CandidateLocation = filter.CandidateLocation == null ? filter.CandidateLocation = modelList.Select(x => x.preferredLocation).ToArray() : filter.CandidateLocation = filter.CandidateLocation;
                            filter.IsShortListed = filter.IsShortListed == null ? filter.IsShortListed = modelList.Select(x => x.ShortListstatus).ToArray() : filter.IsShortListed = filter.IsShortListed;
                            filter.IsIntrested = filter.IsIntrested == null ? filter.IsIntrested = modelList.Select(x => x.IsIntrested).ToArray() : filter.IsIntrested = filter.IsIntrested;

                            modelList = modelList.Where(c => filter.PrimarySkill.Contains(c.JDTiltle.Split("|")[0])
                            && (filter.ClientHR.Count() > 0 ? filter.ClientHR.Contains(c.ClientHrContact) : c.ClientHrContact == null)
                              && (filter.CandidateName.Count() > 0 ? filter.CandidateName.Contains(c.candidateName) : c.candidateName == null)
                            && (filter.ApprovalStatus.Count() > 0 ? filter.ApprovalStatus.Contains(c.NextLevelApprovalStat) : c.NextLevelApprovalStat == string.Empty)
                               && (filter.OfferStatus.Count() > 0 ? filter.OfferStatus.Contains(c.OfferStatusStatDesc) : c.OfferStatusStatDesc == string.Empty)
                                && (filter.JoiningStatus.Count() > 0 ? filter.JoiningStatus.Contains(c.JoiningStatusStatDesc) : c.JoiningStatusStatDesc == string.Empty)
                                 && (filter.ClientName.Count() > 0 ? filter.ClientName.Contains(c.ClientName) : c.ClientName == null)
                                  && (filter.CandidateLocation.Count() > 0 ? filter.CandidateLocation.Contains(c.preferredLocation) : c.preferredLocation == null)
                            && (filter.InterviewStatus.Count() > 0 ? filter.InterviewStatus.Contains(c.intervieweStatusStatDesc) : c.intervieweStatusStatDesc == string.Empty)
                             && (filter.IsShortListed.Count() > 0 ? filter.IsShortListed.Contains(c.ShortListstatus) : c.ShortListstatus == string.Empty)
                             && (filter.IsIntrested.Count() > 0 ? filter.IsIntrested.Contains(c.IsIntrested) : c.IsIntrested == string.Empty)).ToList();
                            ViewBag.CandidateList = modelList.Select(x => x.candidateName).Distinct().ToList();
                            ViewBag.PrimarySkillList = modelList.Select(x => x.JDTiltle).Distinct().ToList();
                            ViewBag.ClientHrContactList = modelList.Select(x => x.ClientHrContact).Distinct().ToList();
                            ViewBag.preferredLocationList = modelList.Select(x => x.preferredLocation).Distinct().ToList();
                            ViewBag.ClientNameList = modelList.Select(x => x.ClientName).Distinct().ToList();
                            ViewBag.IsShortListed = modelList.Select(x => x.ShortListstatus).Distinct().ToList();
                            ViewBag.IsIntrested = modelList.Select(x => x.IsIntrested).Distinct().ToList();

                        }
                        if (profileName == "CuratedUser")
                        {
                            foreach (var item in modelList)
                            {
                                if (item.IsIntrested != "Accept")
                                {
                                    var usernames = item.candidateName.Trim().Split(" ");
                                    var namemodel = String.Empty;
                                    foreach (var name in usernames)
                                    {
                                        namemodel = namemodel + name.ToCharArray()[0];
                                    }
                                    item.candidateName = namemodel;
                                }
                            }
                        }

                        ViewBag.ProfileName = profileName;
                        if (!String.IsNullOrEmpty(searchvalue))                        {                            modelList = modelList.Where(x => x.candidateName.ToUpper().Contains(searchvalue.ToUpper()) || x.JDTiltle.ToUpper().Contains(searchvalue.ToUpper())).ToList();                            ViewBag.searchvalue = searchvalue;                        }                        else                        {                            ViewBag.searchvalue = "";                        }


                        for (int i = 0; i < modelList.Count; i++)
                        {
                            modelList[i].interviewVediosequence = modelList.Count;
                            var totalpage = modelList.Count / 12;
                            Page = Page == null ? 1 : Page;
                            int? initail = 12 * (Page - 1) + 1;
                            int? endvalue = 0;
                            if (totalpage >= Page)
                            {

                                endvalue = 12 * Page;
                            }
                            else
                            {
                                endvalue = modelList.Count;
                            }
                            modelList[i].PageNumber = initail.ToString() + " - " + endvalue.ToString();

                        }
                        return View(modelList.ToPagedList(Page ?? 1, 12));
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

        /// <summary>
        /// Method to apply filters on index page
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="Page"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Index(InterviewFilter filter, int? Page)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    bool isClientUser = false;
                    int ClientId = 0;
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "CuratedUser")
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        if (profileName == "Client")
                        {
                            ClientId = _userRepository.GetClientUserDetails(emailid).ClientId;
                            isClientUser = true;
                        }
                        if (TempData["SuccessErrorMessageInterview"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageInterview"].ToString();
                            TempData.Remove("SuccessErrorMessageInterview");
                        }
                        if (filter != null)
                        {
                            HttpContext.Session.SetString("RiseTubefilters", JsonConvert.SerializeObject(filter));
                        }
                        var modelList = new List<MeetingVM>();

                        if (isClientUser)
                        {
                            modelList = _meetingService.GetRiseTubeMeetings(ClientId, profileName, emailid).ToList();
                        }
                        else if (profileName == "CuratedUser")
                        {
                            modelList = _meetingService.GetRiseTubeMeetings(0, profileName, emailid).ToList().Where(x => x.CuratedLink).ToList();
                        }
                        else
                        {
                            modelList = _meetingService.GetRiseTubeMeetings(0, profileName, emailid).ToList();
                        }

                        if (filter.StartDate != null && filter.EndDate != null)
                        {
                            modelList = modelList.Where(t => t.interviewDate.Date >= filter.StartDate && t.interviewDate.Date <= filter.EndDate).ToList();
                        }


                        modelList = modelList.Select(x =>
                        {
                            x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty;
                            return x;
                        }).ToList();

                        filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = modelList.Select(x => x.JDTiltle.Split("|")[0]).ToArray() : filter.PrimarySkill = filter.PrimarySkill;
                        filter.ClientHR = filter.ClientHR == null ? filter.ClientHR = modelList.Select(x => x.ClientHrContact).ToArray() : filter.ClientHR = filter.ClientHR;
                        filter.InterviewStatus = filter.InterviewStatus == null ? filter.InterviewStatus = modelList.Select(x => x.intervieweStatusStatDesc).ToArray() : filter.InterviewStatus = filter.InterviewStatus;
                        filter.JoiningStatus = filter.JoiningStatus == null ? filter.JoiningStatus = modelList.Select(x => x.JoiningStatusStatDesc).ToArray() : filter.JoiningStatus = filter.JoiningStatus;
                        filter.OfferStatus = filter.OfferStatus == null ? filter.OfferStatus = modelList.Select(x => x.OfferStatusStatDesc).ToArray() : filter.OfferStatus = filter.OfferStatus;
                        filter.CandidateName = filter.CandidateName == null ? filter.CandidateName = modelList.Select(x => x.candidateName).ToArray() : filter.CandidateName = filter.CandidateName;
                        filter.ClientName = filter.ClientName == null ? filter.ClientName = modelList.Select(x => x.ClientName).ToArray() : filter.ClientName = filter.ClientName;
                        filter.ApprovalStatus = filter.ApprovalStatus == null ? filter.ApprovalStatus = modelList.Select(x => x.NextLevelApprovalStat).ToArray() : filter.ApprovalStatus = filter.ApprovalStatus;
                        filter.CandidateLocation = filter.CandidateLocation == null ? filter.CandidateLocation = modelList.Select(x => x.preferredLocation).ToArray() : filter.CandidateLocation = filter.CandidateLocation;
                        filter.IsShortListed = filter.IsShortListed == null ? filter.IsShortListed = modelList.Select(x => x.ShortListstatus).ToArray() : filter.IsShortListed = filter.IsShortListed;
                        filter.IsIntrested = filter.IsIntrested == null ? filter.IsIntrested = modelList.Select(x => x.IsIntrested).ToArray() : filter.IsIntrested = filter.IsIntrested;

                        modelList = modelList.Where(c => filter.PrimarySkill.Contains(c.JDTiltle.Split("|")[0])
                        && (filter.ClientHR.Count() > 0 ? filter.ClientHR.Contains(c.ClientHrContact) : c.ClientHrContact == null)
                          && (filter.CandidateName.Count() > 0 ? filter.CandidateName.Contains(c.candidateName) : c.candidateName == null)
                        && (filter.ApprovalStatus.Count() > 0 ? filter.ApprovalStatus.Contains(c.NextLevelApprovalStat) : c.NextLevelApprovalStat == string.Empty)
                           && (filter.OfferStatus.Count() > 0 ? filter.OfferStatus.Contains(c.OfferStatusStatDesc) : c.OfferStatusStatDesc == string.Empty)
                            && (filter.JoiningStatus.Count() > 0 ? filter.JoiningStatus.Contains(c.JoiningStatusStatDesc) : c.JoiningStatusStatDesc == string.Empty)
                             && (filter.ClientName.Count() > 0 ? filter.ClientName.Contains(c.ClientName) : c.ClientName == null)
                              && (filter.CandidateLocation.Count() > 0 ? filter.CandidateLocation.Contains(c.preferredLocation) : c.preferredLocation == null)
                        && (filter.InterviewStatus.Count() > 0 ? filter.InterviewStatus.Contains(c.intervieweStatusStatDesc) : c.intervieweStatusStatDesc == string.Empty)
                        && (filter.IsShortListed.Count() > 0 ? filter.IsShortListed.Contains(c.ShortListstatus) : c.ShortListstatus == string.Empty)
                        && (filter.IsIntrested.Count() > 0 ? filter.IsIntrested.Contains(c.IsIntrested) : c.IsIntrested == string.Empty)).ToList();
                        ViewBag.CandidateList = modelList.Select(x => x.candidateName).Distinct().ToList();
                        ViewBag.PrimarySkillList = modelList.Select(x => x.JDTiltle).Distinct().ToList();
                        ViewBag.ClientHrContactList = modelList.Select(x => x.ClientHrContact).Distinct().ToList();
                        ViewBag.preferredLocationList = modelList.Select(x => x.preferredLocation).Distinct().ToList();
                        ViewBag.ClientNameList = modelList.Select(x => x.ClientName).Distinct().ToList();
                        ViewBag.NextLevelApprovalStatusList = modelList.Select(x => x.NextLevelApprovalStat).ToArray();
                        ViewBag.JoiningStatusList = modelList.Select(x => x.JoiningStatusStatDesc).ToArray();
                        ViewBag.OfferStatusList = modelList.Select(x => x.OfferStatusStatDesc).ToArray();
                        ViewBag.IsShortListed = modelList.Select(x => x.ShortListstatus).Distinct().ToList();
                        ViewBag.IsIntrested = modelList.Select(x => x.IsIntrested).Distinct().ToList();

                        if (profileName == "CuratedUser")
                        {
                            foreach (var item in modelList)
                            {
                                if (item.IsIntrested != "Accept")
                                {
                                    var usernames = item.candidateName.Trim().Split(" ");
                                    var namemodel = String.Empty;
                                    foreach (var name in usernames)
                                    {
                                        namemodel = namemodel + name.ToCharArray()[0];
                                    }
                                    item.candidateName = namemodel;
                                }
                            }
                        }
                        ViewBag.ProfileName = profileName;
                        if (!String.IsNullOrEmpty(filter.SearchFillter))                        {                            modelList = modelList.Where(x => x.candidateName.ToUpper().Contains(filter.SearchFillter.ToUpper()) || x.JDTiltle.ToUpper().Contains(filter.SearchFillter.ToUpper())).ToList();                            ViewBag.searchvalue = filter.SearchFillter;                        }                        else                        {                            ViewBag.searchvalue = "";                        }

                        for (int i = 0; i < modelList.Count; i++)
                        {
                            Page = Page == null ? 1 : Page;
                            modelList[i].interviewVediosequence = modelList.Count;
                            var totalpage = modelList.Count / 12;
                            int? initail = 12 * (Page - 1) + 1;
                            int? endvalue = 0;
                            if (totalpage >= Page)
                            {

                                endvalue = 12 * Page;
                            }
                            else
                            {
                                endvalue = modelList.Count;
                            }
                            modelList[i].PageNumber = initail.ToString() + " - " + endvalue.ToString();

                        }
                        return View(modelList.ToPagedList(Page ?? 1, 12));
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

        /// <summary>
        /// Method to reset applied filters on index page
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> ResetRisetubeFilter()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("RiseTubefilters");
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

        /// <summary>
        /// Function to Redirect mainframe page risetube on interview id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<IActionResult> VideoPage(int id, string irfid, string actionstatus)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    bool isClientUser = false;
                    int ClientId = 0;
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    ViewBag.actionstatus = "Risetube";
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "CuratedUser")
                    {
                        var emailId = HttpContext.Session.GetString("email")?.Trim();
                        emailId = emailId.Replace("\"", "");
                        if (profileName == "Client")
                        {
                            ClientId = _userRepository.GetClientUserDetails(emailId).ClientId;
                            isClientUser = true;
                        }
                        if (TempData["SuccessErrorMessageInterview"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageInterview"].ToString();
                            TempData.Remove("SuccessErrorMessageInterview");
                        }

                        var modelList1 = new List<MeetingVM>();

                        if (isClientUser)
                        {
                            modelList1 = _meetingService.GetRiseTubeMeetings(ClientId, profileName, emailId).ToList();
                        }
                        else if (profileName == "CuratedUser")
                        {
                            modelList1 = _meetingService.GetRiseTubeMeetings(0, profileName, emailId).ToList().Where(x => x.CuratedLink).ToList();
                            if (!String.IsNullOrEmpty(irfid))
                            {
                                int irf = Convert.ToInt32(irfid);
                                modelList1 = modelList1.Where(x => x.irfIntId == irf).ToList();
                                ViewBag.actionstatus = "SkillChannel";
                                if (actionstatus.ToUpper() == "ALL")
                                {
                                    id = modelList1.OrderByDescending(x => x.imid).Select(x => x.imid).FirstOrDefault();
                                }
                                else if (actionstatus.ToUpper() == "SHORTLIST")
                                {
                                    modelList1 = modelList1.Where(x => x.ShortListstatus == "ShortListed").ToList();
                                    id = modelList1.OrderByDescending(x => x.imid).Select(x => x.imid).FirstOrDefault();
                                }
                                else if (actionstatus.ToUpper() == "INTRESTED")
                                {
                                    modelList1 = modelList1.Where(x => x.IsIntrested == "Intrested").ToList();
                                    id = modelList1.OrderByDescending(x => x.imid).Select(x => x.imid).FirstOrDefault();
                                }
                                else if (actionstatus.ToUpper() == "ACCEPT")
                                {
                                    modelList1 = modelList1.Where(x => x.IsIntrested == "Accept").ToList();
                                    id = modelList1.OrderByDescending(x => x.imid).Select(x => x.imid).FirstOrDefault();
                                }
                            }

                        }
                        else
                        {
                            modelList1 = _meetingService.GetRiseTubeMeetings(0, profileName, emailId).ToList();
                        }
                        //Start:Save View counts of vedio
                        if (HttpContext.Session.GetString("UserLogedIn") != null)
                        {
                            string values = String.Empty;
                            var valuestatus = HttpContext.Session.GetString("UserLogedIn")?.Trim();
                            if (valuestatus == "Yes")
                            {
                                _meetingService.AddTotalViewCount(id);
                                //save into database to increase view count save into main table from where data is picked
                                HttpContext.Session.SetString("UserLogedIn", "No");
                            }
                            else
                            {
                                if (HttpContext.Session.GetString("UserLogedInVedioId") != null)
                                {
                                    values = HttpContext.Session.GetString("UserLogedInVedioId")?.Trim();
                                    var _lst = values.Split(";");
                                    var flag = true;
                                    for (int i = 0; i < _lst.Count(); i++)
                                    {
                                        if (_lst[i] == id.ToString())
                                        {
                                            flag = false;
                                        }
                                    }
                                    if (flag == true)
                                    {
                                        _meetingService.AddTotalViewCount(id);
                                        //save into database to increase view count save into main table from where data is picked
                                    }
                                }
                                //not do anything
                            }
                            values = values + ";" + id;
                            HttpContext.Session.SetString("UserLogedInVedioId", values);
                        }
                        //End:Save View counts of vedio

                        MeetingVM model = _meetingService.GetMeetingbyid(id, emailId);
                        if (model == null)
                        {
                            model = new MeetingVM();
                        }

                        modelList1 = modelList1.Where(c => c.JDTiltle.Contains(model.JDTiltle.Split("|")[0])).OrderByDescending(x => x.interviewDate).ToList();
                        modelList1 = modelList1.Select(x =>
                        {
                            x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty;
                            return x;
                        }).ToList();
                        var IRListModel = _meetingService.GetInteviewReport(model.imid, model.irfIntId).Result;
                        var modelList = modelList1;

                        if (HttpContext.Session.GetString("VideoPagefilters") != null)
                        {

                            var filter = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("VideoPagefilters"));
                            if (filter.StartDate != null && filter.EndDate != null)
                            {
                                modelList = _meetingService.GetMeetingList();
                                modelList = modelList.Where(t => t.JDTiltle.Contains(model.JDTiltle.Split("|")[0]) && t.interviewDate.Date >= filter.StartDate && t.interviewDate.Date <= filter.EndDate && t.confirmationStatus == "1" && (t.InterviewStatus == 2 || t.InterviewStatus == 4) && t.VideoLink != null).OrderByDescending(x => x.interviewDate).ToList();
                            }
                            modelList = modelList.Select(x =>
                            {
                                x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty;
                                return x;
                            }).ToList();
                            modelList.ForEach(x => x.NextLevelApprovalStat = x.IsApproved.HasValue ? ((NextLevelApprovalStatus)x.IsApproved).GetDescription() : string.Empty);

                            filter.ClientHR = filter.ClientHR == null ? filter.ClientHR = modelList.Select(x => x.ClientHrContact).ToArray() : filter.ClientHR = filter.ClientHR;
                            filter.InterviewStatus = filter.InterviewStatus == null ? filter.InterviewStatus = modelList.Select(x => x.intervieweStatusStatDesc).ToArray() : filter.InterviewStatus = filter.InterviewStatus;
                            filter.JoiningStatus = filter.JoiningStatus == null ? filter.JoiningStatus = modelList.Select(x => x.JoiningStatusStatDesc).ToArray() : filter.JoiningStatus = filter.JoiningStatus;
                            filter.OfferStatus = filter.OfferStatus == null ? filter.OfferStatus = modelList.Select(x => x.OfferStatusStatDesc).ToArray() : filter.OfferStatus = filter.OfferStatus;
                            filter.CandidateName = filter.CandidateName == null ? filter.CandidateName = modelList.Select(x => x.candidateName).ToArray() : filter.CandidateName = filter.CandidateName;
                            filter.ClientName = filter.ClientName == null ? filter.ClientName = modelList.Select(x => x.ClientName).ToArray() : filter.ClientName = filter.ClientName;
                            filter.ApprovalStatus = filter.ApprovalStatus == null ? filter.ApprovalStatus = modelList.Select(x => x.NextLevelApprovalStat).ToArray() : filter.ApprovalStatus = filter.ApprovalStatus;
                            filter.CandidateLocation = filter.CandidateLocation == null ? filter.CandidateLocation = modelList.Select(x => x.preferredLocation).ToArray() : filter.CandidateLocation = filter.CandidateLocation;
                            filter.IsShortListed = filter.IsShortListed == null ? filter.IsShortListed = modelList.Select(x => x.ShortListstatus).ToArray() : filter.IsShortListed = filter.IsShortListed;
                            filter.IsIntrested = filter.IsIntrested == null ? filter.IsIntrested = modelList.Select(x => x.IsIntrested).ToArray() : filter.IsIntrested = filter.IsIntrested;

                            modelList = modelList.Where(c =>
                           (filter.ClientHR.Count() > 0 ? filter.ClientHR.Contains(c.ClientHrContact) : c.ClientHrContact == null)
                          && (filter.CandidateName.Count() > 0 ? filter.CandidateName.Contains(c.candidateName) : c.candidateName == null)
                        && (filter.ApprovalStatus.Count() > 0 ? filter.ApprovalStatus.Contains(c.NextLevelApprovalStat) : c.NextLevelApprovalStat == string.Empty)
                           && (filter.OfferStatus.Count() > 0 ? filter.OfferStatus.Contains(c.OfferStatusStatDesc) : c.OfferStatusStatDesc == string.Empty)
                            && (filter.JoiningStatus.Count() > 0 ? filter.JoiningStatus.Contains(c.JoiningStatusStatDesc) : c.JoiningStatusStatDesc == string.Empty)
                             && (filter.ClientName.Count() > 0 ? filter.ClientName.Contains(c.ClientName) : c.ClientName == null)
                              && (filter.CandidateLocation.Count() > 0 ? filter.CandidateLocation.Contains(c.preferredLocation) : c.preferredLocation == null)
                                && (filter.IsShortListed.Count() > 0 ? filter.IsShortListed.Contains(c.ShortListstatus) : c.ShortListstatus == string.Empty)
                        && (filter.IsIntrested.Count() > 0 ? filter.IsIntrested.Contains(c.IsIntrested) : c.IsIntrested == string.Empty)).ToList();
                        }
                        if (profileName == "CuratedUser")
                        {
                            foreach (var itemmodel in modelList)
                            {
                                if (itemmodel.IsIntrested != "Accept")
                                {
                                    var usernames = itemmodel.candidateName.Trim().Split(" ");
                                    var namemodel = String.Empty;
                                    foreach (var name in usernames)
                                    {
                                        namemodel = namemodel + name.ToCharArray()[0];
                                    }
                                    itemmodel.candidateName = namemodel;
                                }
                            }
                        }

                        MeetingVM NewModel = new MeetingVM();

                        if (HttpContext.Session.GetString("VideoPagefilters") != null)
                        {
                            NewModel.SubVideo_List = modelList.ToList().Where(x => x.imid != id).ToList();
                        }
                        else
                        {
                            NewModel.SubVideo_List = modelList1.ToList().Where(x => x.imid != id).ToList();
                        }

                        if (profileName == "CuratedUser")
                        {
                            if (model.IsIntrested != "Accept" && model.IsIntrested != null)
                            {
                                var usernames = model.candidateName.Trim().Split(" ");
                                var namemodel = String.Empty;
                                foreach (var name in usernames)
                                {
                                    namemodel = namemodel + name.ToCharArray()[0];
                                }
                                NewModel.candidateName = namemodel;
                            }
                            else
                            {
                                NewModel.candidateName = model.candidateName;
                            }
                        }
                        else
                        {
                            NewModel.candidateName = model.candidateName;
                        }
                        ViewBag.ProfileName = profileName;
                        if (profileName == "CuratedUser")
                        {
                            if (IRListModel != null)
                            {
                                if (model.IsIntrested != "Accept")
                                {
                                    var usernames = IRListModel.CandidateName.Trim().Split(" ");
                                    var namemodel = String.Empty;
                                    foreach (var name in usernames)
                                    {
                                        namemodel = namemodel + name.ToCharArray()[0];
                                    }
                                    IRListModel.CandidateName = namemodel;
                                }
                            }
                        }
                        NewModel.CandidateId = model.CandidateId;
                        NewModel.interviewDate = model.interviewDate;
                        NewModel.imid = model.imid;
                        NewModel.IRFId = model.IRFId;
                        NewModel.ClientName = model.ClientName;
                        NewModel.InterviewerComment = model.InterviewerComment;
                        NewModel.InterviewStatus = model.InterviewStatus;
                        NewModel.JDTiltle = model.JDTiltle;
                        NewModel.JDLink = model.JDLink;
                        NewModel.MeetingStatus = model.MeetingStatus;
                        NewModel.confirmationStatus = model.confirmationStatus;
                        NewModel.VideoLink = model.VideoLink;
                        NewModel.candidateEmail = model.candidateEmail;
                        NewModel.reportLink = model.reportLink;
                        NewModel.resumeLink = model.resumeLink;
                        NewModel.zoomLink = model.zoomLink;
                        NewModel.IreportModel = IRListModel == null ? new InterviewReport() : IRListModel;
                        NewModel.IsLike = model.IsLike;
                        NewModel.TotalView = model.TotalView;
                        NewModel.IsShared = model.IsShared;
                        NewModel.TotalVedioLike = model.TotalVedioLike;
                        NewModel.TotalVedioDisLike = model.TotalVedioDisLike;
                        NewModel.ShortListCount = model.ShortListCount;
                        NewModel.IsShortlisted = model.IsShortlisted;
                        NewModel.ThumbnailImage = model.ThumbnailImage;
                        NewModel.IsIntrested = model.IsIntrested;
                        if (NewModel.IreportModel.QuestionsCollection == null)
                        {
                            ViewBag.QuestionsDiscussedCount = 0;
                        }
                        else
                        {
                            ViewBag.QuestionsDiscussedCount = NewModel.IreportModel.QuestionsCollection?.Count();
                        }
                        var comments = _meetingService.GetvedioComments(id);
                        NewModel.Comments = comments;
                        bool isQC = false;
                        NewModel.IreportModel.IsQC = isQC;
                        if (IRListModel != null)
                        {
                            GetStatusList(IRListModel, model.irfIntId);
                        }
                        ViewBag.VedioStatus = "Private";

                        return View(NewModel);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    //Start: Share button functionaly
                    MeetingVM model = _meetingService.GetMeetingbyid(id, "");                    var status = String.IsNullOrEmpty(model.IsShared) ? "" : model.IsShared;                    if (status == "Public")
                    {                        var IRListModel = _meetingService.GetInteviewReport(model.imid, model.irfIntId).Result;

                        MeetingVM NewModel = new MeetingVM();                        NewModel.SubVideo_List = new List<MeetingVM>();                        NewModel.candidateName = model.candidateName;

                        ViewBag.ProfileName = "";                        NewModel.CandidateId = model.CandidateId;                        NewModel.interviewDate = model.interviewDate;                        NewModel.imid = model.imid;                        NewModel.IRFId = model.IRFId;                        NewModel.ClientName = model.ClientName;

                        NewModel.InterviewerComment = model.InterviewerComment;                        NewModel.InterviewStatus = model.InterviewStatus;                        NewModel.JDTiltle = model.JDTiltle;                        NewModel.JDLink = model.JDLink;                        NewModel.MeetingStatus = model.MeetingStatus;                        NewModel.confirmationStatus = model.confirmationStatus;                        NewModel.VideoLink = model.VideoLink;                        NewModel.candidateEmail = model.candidateEmail;                        NewModel.reportLink = model.reportLink;                        NewModel.resumeLink = model.resumeLink;                        NewModel.zoomLink = model.zoomLink;                        NewModel.IreportModel = IRListModel;                        NewModel.IsLike = model.IsLike;                        NewModel.TotalView = model.TotalView;
                        NewModel.IsShared = model.IsShared;
                        NewModel.TotalVedioLike = model.TotalVedioLike;
                        NewModel.TotalVedioDisLike = model.TotalVedioDisLike;
                        NewModel.ShortListCount = model.ShortListCount;
                        NewModel.IsShortlisted = model.IsShortlisted;
                        NewModel.ThumbnailImage = model.ThumbnailImage;
                        NewModel.IsIntrested = model.IsIntrested;

                        if (NewModel.IreportModel.QuestionsCollection == null)                        {                            ViewBag.QuestionsDiscussedCount = 0;                        }                        else                        {                            ViewBag.QuestionsDiscussedCount = NewModel.IreportModel.QuestionsCollection?.Count();                        }                        bool isQC = false;                        NewModel.IreportModel.IsQC = isQC;
                        ViewBag.VedioStatus = "Public";
                        //desible Like and dilisk button and share button and comment opetion
                        return View(NewModel);
                        //END: Share button functionaly
                    }                    else                    {                        return RedirectToAction("SignOut", "Session");                    }

                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }

        }

        /// <summary>
        /// Method to apply filters on mainframe VideoPage
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> VideoPage(InterviewFilter filter, int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    //Start:Save View counts of vedio
                    if (HttpContext.Session.GetString("UserLogedIn") != null)                    {                        string values = String.Empty;                        var valuestatus = HttpContext.Session.GetString("UserLogedIn")?.Trim();                        if (valuestatus == "Yes")                        {
                            _meetingService.AddTotalViewCount(id);
                            //save into database to increase view count save into main table from where data is picked
                            HttpContext.Session.SetString("UserLogedIn", "No");                        }                        else                        {                            if (HttpContext.Session.GetString("UserLogedInVedioId") != null)                            {                                values = HttpContext.Session.GetString("UserLogedInVedioId")?.Trim();                                var _lst = values.Split(";");
                                var flag = true;                                for (int i = 0; i < _lst.Count(); i++)                                {                                    if (_lst[i] == id.ToString())                                    {                                        flag = false;                                    }                                }                                if (flag == true)                                {
                                    _meetingService.AddTotalViewCount(id);
                                    //save into database to increase view count save into main table from where data is picked
                                }                            }
                            //not do anything
                        }                        values = values + ";" + id;                        HttpContext.Session.SetString("UserLogedInVedioId", values);                    }
                    //End:Save View counts of vedio
                    bool isClientUser = false;
                    int ClientId = 0;
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "CuratedUser")
                    {
                        var emailId = HttpContext.Session.GetString("email")?.Trim();
                        emailId = emailId.Replace("\"", "");
                        if (profileName == "Client")
                        {
                            ClientId = _userRepository.GetClientUserDetails(emailId).ClientId;
                            isClientUser = true;
                        }
                        if (TempData["SuccessErrorMessageInterview"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageInterview"].ToString();
                            TempData.Remove("SuccessErrorMessageInterview");
                        }
                        if (filter != null)
                        {
                            HttpContext.Session.SetString("VideoPagefilters", JsonConvert.SerializeObject(filter));
                        }

                        var modelList = new List<MeetingVM>();

                        if (isClientUser)
                        {
                            modelList = _meetingService.GetRiseTubeMeetings(ClientId, profileName, emailId).ToList();
                        }
                        else if (profileName == "CuratedUser")
                        {
                            modelList = _meetingService.GetRiseTubeMeetings(0, profileName, emailId).ToList().Where(x => x.CuratedLink).ToList();
                        }
                        else
                        {
                            modelList = _meetingService.GetRiseTubeMeetings(0, profileName, emailId).ToList();
                        }
                        MeetingVM model = _meetingService.GetMeetingbyid(id, emailId);
                        //MeetingVM model = modelList.Where(a => a.imid == id).FirstOrDefault();
                        modelList = modelList.Where(c => c.JDTiltle.Contains(model.JDTiltle.Split("|")[0])).OrderByDescending(x => x.interviewDate).ToList();
                        var IRListModel = _meetingService.GetInteviewReport(model.imid, model.irfIntId).Result;

                        if (filter.StartDate != null && filter.EndDate != null)
                        {
                            modelList = modelList.Where(t => t.JDTiltle.Contains(model.JDTiltle.Split("|")[0]) && t.interviewDate.Date >= filter.StartDate && t.interviewDate.Date <= filter.EndDate).ToList();
                        }
                        modelList = modelList.Select(x =>
                        {
                            x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty;
                            return x;
                        }).ToList();


                        filter.ClientHR = filter.ClientHR == null ? filter.ClientHR = modelList.Select(x => x.ClientHrContact).ToArray() : filter.ClientHR = filter.ClientHR;
                        filter.InterviewStatus = filter.InterviewStatus == null ? filter.InterviewStatus = modelList.Select(x => x.intervieweStatusStatDesc).ToArray() : filter.InterviewStatus = filter.InterviewStatus;
                        filter.JoiningStatus = filter.JoiningStatus == null ? filter.JoiningStatus = modelList.Select(x => x.JoiningStatusStatDesc).ToArray() : filter.JoiningStatus = filter.JoiningStatus;
                        filter.OfferStatus = filter.OfferStatus == null ? filter.OfferStatus = modelList.Select(x => x.OfferStatusStatDesc).ToArray() : filter.OfferStatus = filter.OfferStatus;
                        filter.CandidateName = filter.CandidateName == null ? filter.CandidateName = modelList.Select(x => x.candidateName).ToArray() : filter.CandidateName = filter.CandidateName;
                        filter.ClientName = filter.ClientName == null ? filter.ClientName = modelList.Select(x => x.ClientName).ToArray() : filter.ClientName = filter.ClientName;
                        filter.ApprovalStatus = filter.ApprovalStatus == null ? filter.ApprovalStatus = modelList.Select(x => x.NextLevelApprovalStat).ToArray() : filter.ApprovalStatus = filter.ApprovalStatus;
                        filter.CandidateLocation = filter.CandidateLocation == null ? filter.CandidateLocation = modelList.Select(x => x.preferredLocation).ToArray() : filter.CandidateLocation = filter.CandidateLocation;
                        filter.IsShortListed = filter.IsShortListed == null ? filter.IsShortListed = modelList.Select(x => x.ShortListstatus).ToArray() : filter.IsShortListed = filter.IsShortListed;
                        filter.IsIntrested = filter.IsIntrested == null ? filter.IsIntrested = modelList.Select(x => x.IsIntrested).ToArray() : filter.IsIntrested = filter.IsIntrested;

                        modelList = modelList.Where(c =>
                           (filter.ClientHR.Count() > 0 ? filter.ClientHR.Contains(c.ClientHrContact) : c.ClientHrContact == null)
                          && (filter.CandidateName.Count() > 0 ? filter.CandidateName.Contains(c.candidateName) : c.candidateName == null)
                        && (filter.ApprovalStatus.Count() > 0 ? filter.ApprovalStatus.Contains(c.NextLevelApprovalStat) : c.NextLevelApprovalStat == string.Empty)
                           && (filter.OfferStatus.Count() > 0 ? filter.OfferStatus.Contains(c.OfferStatusStatDesc) : c.OfferStatusStatDesc == string.Empty)
                            && (filter.JoiningStatus.Count() > 0 ? filter.JoiningStatus.Contains(c.JoiningStatusStatDesc) : c.JoiningStatusStatDesc == string.Empty)
                             && (filter.ClientName.Count() > 0 ? filter.ClientName.Contains(c.ClientName) : c.ClientName == null)
                              && (filter.CandidateLocation.Count() > 0 ? filter.CandidateLocation.Contains(c.preferredLocation) : c.preferredLocation == null)
                                && (filter.IsShortListed.Count() > 0 ? filter.IsShortListed.Contains(c.ShortListstatus) : c.ShortListstatus == string.Empty)
                        && (filter.IsIntrested.Count() > 0 ? filter.IsIntrested.Contains(c.IsIntrested) : c.IsIntrested == string.Empty)).ToList();
                        MeetingVM NewModel = new MeetingVM();
                        if (profileName == "CuratedUser")
                        {
                            foreach (var itemmodel in modelList)
                            {
                                if (itemmodel.IsIntrested != "Accept")
                                {
                                    var usernames = itemmodel.candidateName.Trim().Split(" ");
                                    var namemodel = String.Empty;
                                    foreach (var name in usernames)
                                    {
                                        namemodel = namemodel + name.ToCharArray()[0];
                                    }
                                    itemmodel.candidateName = namemodel;
                                }
                            }
                        }

                        NewModel.SubVideo_List = modelList.ToList();
                        if (profileName == "CuratedUser")
                        {
                            if (model.IsIntrested != "Accept")
                            {
                                var usernames = model.candidateName.Trim().Split(" ");
                                var namemodel = String.Empty;
                                foreach (var name in usernames)
                                {
                                    namemodel = namemodel + name.ToCharArray()[0];
                                }
                                NewModel.candidateName = namemodel;
                            }
                        }
                        else
                        {
                            NewModel.candidateName = model.candidateName;
                        }
                        ViewBag.ProfileName = profileName;
                        if (profileName == "CuratedUser")
                        {
                            if (model.IsIntrested != "Accept")
                            {
                                var usernames = IRListModel.CandidateName.Trim().Split(" ");
                                var namemodel = String.Empty;
                                foreach (var name in usernames)
                                {
                                    namemodel = namemodel + name.ToCharArray()[0];
                                }
                                IRListModel.CandidateName = namemodel;
                            }
                        }
                        NewModel.CandidateId = model.CandidateId;
                        NewModel.interviewDate = model.interviewDate;
                        NewModel.imid = model.imid;
                        NewModel.IRFId = model.IRFId;
                        NewModel.ClientName = model.ClientName;
                        NewModel.InterviewerComment = model.InterviewerComment;
                        NewModel.InterviewStatus = model.InterviewStatus;
                        NewModel.JDTiltle = model.JDTiltle;
                        NewModel.JDLink = model.JDLink;
                        NewModel.MeetingStatus = model.MeetingStatus;
                        NewModel.confirmationStatus = model.confirmationStatus;
                        NewModel.VideoLink = model.VideoLink;
                        NewModel.candidateEmail = model.candidateEmail;
                        NewModel.reportLink = model.reportLink;
                        NewModel.resumeLink = model.resumeLink;
                        NewModel.zoomLink = model.zoomLink;
                        NewModel.IreportModel = IRListModel;
                        NewModel.IsLike = model.IsLike;
                        NewModel.TotalView = model.TotalView;
                        NewModel.IsShared = model.IsShared;
                        NewModel.TotalVedioLike = model.TotalVedioLike;
                        NewModel.TotalVedioDisLike = model.TotalVedioDisLike;
                        NewModel.ShortListCount = model.ShortListCount;
                        NewModel.IsShortlisted = model.IsShortlisted;
                        NewModel.ThumbnailImage = model.ThumbnailImage;
                        NewModel.IsIntrested = model.IsIntrested;

                        if (NewModel.IreportModel.QuestionsCollection == null)
                        {
                            ViewBag.QuestionsDiscussedCount = 0;
                        }
                        else
                        {
                            ViewBag.QuestionsDiscussedCount = NewModel.IreportModel.QuestionsCollection?.Count();
                        }
                        var comments = _meetingService.GetvedioComments(id);
                        NewModel.Comments = comments;
                        bool isQC = false;
                        NewModel.IreportModel.IsQC = isQC;
                        GetStatusList(IRListModel, model.irfIntId);
                        ViewBag.VedioStatus = "Private";


                        return View(NewModel);
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

        /// <summary>
        /// Method to reset applied filters on mainframe VideoPage
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> ResetVideoPageFilter()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("VideoPagefilters");
                    return Json("Success");
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


        /// <summary>
        /// Method to show all report data on mainframe VideoPage
        /// </summary>
        /// <param name="model"></param>
        /// <param name="irfId"></param>
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
            var questionsLevel = from QuestionsLevel s in Enum.GetValues(typeof(QuestionsLevel))
                                 select new { ID = (int)s, Name = s.GetDescription() };
            model.QuestionsLevelList = new SelectList(questionsLevel, "ID", "Name");

            model.QuestionsList = new SelectList(_meetingService.GetTechSkillComments(irfId), "Comment", "Comment");
        }
        [HttpPost]
        public JsonResult VedioLike(string value, string vedioid)
        {
            var email = HttpContext.Session.GetString("email");
            var modelList = _meetingService.VedioLike(value, vedioid, email);
            return Json(modelList);
        }

        [HttpPost]
        public JsonResult VedioShare(string value, string vedioid)
        {
            var modelList = _meetingService.VedioShare(value, vedioid);
            return Json("Success");
        }
        [HttpPost]
        public JsonResult VedioComment(string comment, string vedioid)
        {
            var email = HttpContext.Session.GetString("email");
            var modelList = _meetingService.VedioComment(comment, vedioid, email);
            return Json("Success");
        }
        [HttpPost]
        public JsonResult UpdateVedioComment(string vedioid)
        {
            //store data in database
            int id = Convert.ToInt32(vedioid);
            var comments = _meetingService.GetvedioComments(id);
            return Json(comments);
        }
        public JsonResult CandidateShortList(string vedioid)
        {
            var email = HttpContext.Session.GetString("email");
            var Shortlistcount = _meetingService.GetvedioShortList(vedioid, email);
            int result = 0;
            if (Shortlistcount != null)
            {
                result = Shortlistcount.totalcount;
                Mail _mail = new Mail();
                _mail.TO = Shortlistcount.Email;
                email = email.Replace("\"", "");
                email = email.Split("@")[1].Split(".")[0];
                StringBuilder bulider = new StringBuilder();
                bulider.Append("<p>Hi Dear,</p><br/>");
                bulider.Append("<p>Client Name: " + email.ToUpper());
                bulider.Append("is Shortlisted your profile.</p>");
                bulider.Append("<p>For more details, Please visit RiseBird portal.</p>");
                bulider.Append("<br/><p>Thanks,</p>RiseBird");
                _mail.Body = bulider.ToString();
                var result1 = _notificationHelper.NotifyCandidate(_mail);

            }
            return Json(result);
        }
        [HttpPost]
        public JsonResult CandidateSuitable(string vedioid, string Comment)
        {
            //store data in database
            int id = Convert.ToInt32(vedioid);
            var email = HttpContext.Session.GetString("email");
            var Status = _meetingService.CandidateSuitable(id, email, Comment);
            if (Status != null)
            {
                Mail _mail = new Mail();
                _mail.TO = Status;
                email = email.Replace("\"", "");
                email = email.Split("@")[1].Split(".")[0];
                StringBuilder bulider = new StringBuilder();
                bulider.Append("<p>Hi Dear,</p><br/>");
                bulider.Append("<p>Client Name:</p>" + email.ToUpper());
                bulider.Append("<br/><p>Shows interest in your profile.</p>");
                bulider.Append("<p>For more details, Please visit RiseBird portal.</p>");
                bulider.Append("<br/><p>Thanks,</p>RiseBird");
                _mail.Body = bulider.ToString();
                var result = _notificationHelper.NotifyCandidate(_mail);
            }
            return Json("Success");
        }
    }
}