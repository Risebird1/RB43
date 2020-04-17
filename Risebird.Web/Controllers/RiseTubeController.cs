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

        public RiseTubeController(IMeetingService meetingService, IApplicationErrorLogger applicationErrorLogger, IAzureStorageHelper azureStorageHelper, ISkillService skillService, IIRFRepository iRFRepository, IIRFService iRFService, IUserRepository userRepository)
        {
            _meetingService = meetingService;
            _applicationErrorLogger = applicationErrorLogger;
            _azureStorageHelper = azureStorageHelper;
            _skillService = skillService;
            _iRFRepository = iRFRepository;
            _iRFService = iRFService;
            _userRepository = userRepository;
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
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                    {
                        if (profileName == "Client")
                        {
                            var emailid = HttpContext.Session.GetString("email")?.Trim();
                            emailid = emailid.Replace("\"", "");
                            ClientId = _userRepository.GetClientUserDetails(emailid).ClientId;
                            isClientUser = true;
                        }
                        if (TempData["SuccessErrorMessageInterview"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageInterview"].ToString();
                            TempData.Remove("SuccessErrorMessageInterview");
                        }
                        var modelList = _meetingService.GetRiseTubeMeetings();

                        if (isClientUser)
                        {
                            modelList = modelList.Where(x => x.ClientId == ClientId).ToList();
                        }
                        ViewBag.CandidateList = modelList.Select(x => x.candidateName).Distinct().ToList();
                        ViewBag.PrimarySkillList = modelList.Select(x => x.JDTiltle).Distinct().ToList();
                        ViewBag.ClientHrContactList = modelList.Select(x => x.ClientHrContact).Distinct().ToList();
                        ViewBag.preferredLocationList = modelList.Select(x => x.preferredLocation).Distinct().ToList();
                        ViewBag.ClientNameList = modelList.Select(x => x.ClientName).Distinct().ToList();
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

                            modelList = modelList.Where(c => filter.PrimarySkill.Contains(c.JDTiltle.Split("|")[0])
                            && (filter.ClientHR.Count() > 0 ? filter.ClientHR.Contains(c.ClientHrContact) : c.ClientHrContact == null)
                              && (filter.CandidateName.Count() > 0 ? filter.CandidateName.Contains(c.candidateName) : c.candidateName == null)
                            && (filter.ApprovalStatus.Count() > 0 ? filter.ApprovalStatus.Contains(c.NextLevelApprovalStat) : c.NextLevelApprovalStat == string.Empty)
                               && (filter.OfferStatus.Count() > 0 ? filter.OfferStatus.Contains(c.OfferStatusStatDesc) : c.OfferStatusStatDesc == string.Empty)
                                && (filter.JoiningStatus.Count() > 0 ? filter.JoiningStatus.Contains(c.JoiningStatusStatDesc) : c.JoiningStatusStatDesc == string.Empty)
                                 && (filter.ClientName.Count() > 0 ? filter.ClientName.Contains(c.ClientName) : c.ClientName == null)
                                  && (filter.CandidateLocation.Count() > 0 ? filter.CandidateLocation.Contains(c.preferredLocation) : c.preferredLocation == null)
                            && (filter.InterviewStatus.Count() > 0 ? filter.InterviewStatus.Contains(c.intervieweStatusStatDesc) : c.intervieweStatusStatDesc == string.Empty)).ToList();
                            ViewBag.CandidateList = modelList.Select(x => x.candidateName).Distinct().ToList();
                            ViewBag.PrimarySkillList = modelList.Select(x => x.JDTiltle).Distinct().ToList();
                            ViewBag.ClientHrContactList = modelList.Select(x => x.ClientHrContact).Distinct().ToList();
                            ViewBag.preferredLocationList = modelList.Select(x => x.preferredLocation).Distinct().ToList();
                            ViewBag.ClientNameList = modelList.Select(x => x.ClientName).Distinct().ToList();


                        }
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
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                    {
                        if (profileName == "Client")
                        {
                            var emailid = HttpContext.Session.GetString("email")?.Trim();
                            emailid = emailid.Replace("\"", "");
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
                        var modelList = _meetingService.GetRiseTubeMeetings();
                        if (isClientUser)
                        {
                            modelList = modelList.Where(x => x.ClientId == ClientId).ToList();
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

                        modelList = modelList.Where(c => filter.PrimarySkill.Contains(c.JDTiltle.Split("|")[0])
                        && (filter.ClientHR.Count() > 0 ? filter.ClientHR.Contains(c.ClientHrContact) : c.ClientHrContact == null)
                          && (filter.CandidateName.Count() > 0 ? filter.CandidateName.Contains(c.candidateName) : c.candidateName == null)
                        && (filter.ApprovalStatus.Count() > 0 ? filter.ApprovalStatus.Contains(c.NextLevelApprovalStat) : c.NextLevelApprovalStat == string.Empty)
                           && (filter.OfferStatus.Count() > 0 ? filter.OfferStatus.Contains(c.OfferStatusStatDesc) : c.OfferStatusStatDesc == string.Empty)
                            && (filter.JoiningStatus.Count() > 0 ? filter.JoiningStatus.Contains(c.JoiningStatusStatDesc) : c.JoiningStatusStatDesc == string.Empty)
                             && (filter.ClientName.Count() > 0 ? filter.ClientName.Contains(c.ClientName) : c.ClientName == null)
                              && (filter.CandidateLocation.Count() > 0 ? filter.CandidateLocation.Contains(c.preferredLocation) : c.preferredLocation == null)
                        && (filter.InterviewStatus.Count() > 0 ? filter.InterviewStatus.Contains(c.intervieweStatusStatDesc) : c.intervieweStatusStatDesc == string.Empty)).ToList();
                        ViewBag.CandidateList = modelList.Select(x => x.candidateName).Distinct().ToList();
                        ViewBag.PrimarySkillList = modelList.Select(x => x.JDTiltle).Distinct().ToList();
                        ViewBag.ClientHrContactList = modelList.Select(x => x.ClientHrContact).Distinct().ToList();
                        ViewBag.preferredLocationList = modelList.Select(x => x.preferredLocation).Distinct().ToList();
                        ViewBag.ClientNameList = modelList.Select(x => x.ClientName).Distinct().ToList();
                        ViewBag.NextLevelApprovalStatusList = modelList.Select(x => x.NextLevelApprovalStat).ToArray();
                        ViewBag.JoiningStatusList = modelList.Select(x => x.JoiningStatusStatDesc).ToArray();
                        ViewBag.OfferStatusList = modelList.Select(x => x.OfferStatusStatDesc).ToArray();

                        if (!String.IsNullOrEmpty(filter.SearchFillter))                        {                            modelList = modelList.Where(x => x.candidateName.ToUpper().Contains(filter.SearchFillter.ToUpper()) || x.JDTiltle.ToUpper().Contains(filter.SearchFillter.ToUpper())).ToList();                            ViewBag.searchvalue = filter.SearchFillter;                        }                        else                        {                            ViewBag.searchvalue = "";                        }

                        for (int i = 0; i < modelList.Count; i++)
                        {
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
        public async Task<IActionResult> VideoPage(int id)
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
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                    {
                        var Email = HttpContext.Session.GetString("email")?.Trim();
                        Email = Email.Replace("\"", "");
                        if (profileName == "Client")
                        {
                            //var emailid = HttpContext.Session.GetString("email")?.Trim();
                            //emailid = emailid.Replace("\"", "");
                            ClientId = _userRepository.GetClientUserDetails(Email).ClientId;
                            isClientUser = true;
                        }
                        if (TempData["SuccessErrorMessageInterview"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageInterview"].ToString();
                            TempData.Remove("SuccessErrorMessageInterview");
                        }
                        var modelList1 = _meetingService.GetRiseTubeMeetings();
                        if (isClientUser)
                        {
                            modelList1 = modelList1.Where(x => x.ClientId == ClientId).ToList();
                        }
                        MeetingVM model = _meetingService.GetMeetingbyid(id, Email);

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

                            modelList = modelList.Where(c =>
                           (filter.ClientHR.Count() > 0 ? filter.ClientHR.Contains(c.ClientHrContact) : c.ClientHrContact == null)
                          && (filter.CandidateName.Count() > 0 ? filter.CandidateName.Contains(c.candidateName) : c.candidateName == null)
                        && (filter.ApprovalStatus.Count() > 0 ? filter.ApprovalStatus.Contains(c.NextLevelApprovalStat) : c.NextLevelApprovalStat == string.Empty)
                           && (filter.OfferStatus.Count() > 0 ? filter.OfferStatus.Contains(c.OfferStatusStatDesc) : c.OfferStatusStatDesc == string.Empty)
                            && (filter.JoiningStatus.Count() > 0 ? filter.JoiningStatus.Contains(c.JoiningStatusStatDesc) : c.JoiningStatusStatDesc == string.Empty)
                             && (filter.ClientName.Count() > 0 ? filter.ClientName.Contains(c.ClientName) : c.ClientName == null)
                              && (filter.CandidateLocation.Count() > 0 ? filter.CandidateLocation.Contains(c.preferredLocation) : c.preferredLocation == null)).ToList();
                        }

                        MeetingVM NewModel = new MeetingVM();

                        if (HttpContext.Session.GetString("VideoPagefilters") != null)
                        {
                            NewModel.SubVideo_List = modelList.ToList();
                        }
                        else
                        {
                            NewModel.SubVideo_List = modelList1.ToList();
                        }

                        NewModel.candidateName = model.candidateName;
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

                        if (NewModel.IreportModel.QuestionsCollection == null)
                        {
                            ViewBag.QuestionsDiscussedCount = 0;
                        }
                        else
                        {
                            ViewBag.QuestionsDiscussedCount = NewModel.IreportModel.QuestionsCollection?.Count();
                        }
                        var comments= _meetingService.GetvedioComments(id);
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
                    //Start: Share button functionaly
                    MeetingVM model = _meetingService.GetMeetingbyid(id, "");                    var status = String.IsNullOrEmpty(model.IsShared) ? "" : model.IsShared;                    if (status == "Public")
                    {                        var IRListModel = _meetingService.GetInteviewReport(model.imid, model.irfIntId).Result;

                        MeetingVM NewModel = new MeetingVM();                        NewModel.SubVideo_List = new List<MeetingVM>();                        NewModel.candidateName = model.candidateName;                        NewModel.CandidateId = model.CandidateId;                        NewModel.interviewDate = model.interviewDate;                        NewModel.imid = model.imid;                        NewModel.IRFId = model.IRFId;                        NewModel.ClientName = model.ClientName;

                        NewModel.InterviewerComment = model.InterviewerComment;                        NewModel.InterviewStatus = model.InterviewStatus;                        NewModel.JDTiltle = model.JDTiltle;                        NewModel.JDLink = model.JDLink;                        NewModel.MeetingStatus = model.MeetingStatus;                        NewModel.confirmationStatus = model.confirmationStatus;                        NewModel.VideoLink = model.VideoLink;                        NewModel.candidateEmail = model.candidateEmail;                        NewModel.reportLink = model.reportLink;                        NewModel.resumeLink = model.resumeLink;                        NewModel.zoomLink = model.zoomLink;                        NewModel.IreportModel = IRListModel;                        NewModel.IsLike = model.IsLike;                        NewModel.TotalView = model.TotalView;
                        NewModel.IsShared = model.IsShared;



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
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                    {
                        var Email = HttpContext.Session.GetString("email")?.Trim();
                        Email = Email.Replace("\"", "");
                        if (profileName == "Client")
                        {

                            ClientId = _userRepository.GetClientUserDetails(Email).ClientId;
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



                        var modelList = _meetingService.GetRiseTubeMeetings();
                        if (isClientUser)
                        {
                            modelList = modelList.Where(x => x.ClientId == ClientId).ToList();
                        }
                        MeetingVM model = _meetingService.GetMeetingbyid(id, Email);
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

                        modelList = modelList.Where(c =>
                           (filter.ClientHR.Count() > 0 ? filter.ClientHR.Contains(c.ClientHrContact) : c.ClientHrContact == null)
                          && (filter.CandidateName.Count() > 0 ? filter.CandidateName.Contains(c.candidateName) : c.candidateName == null)
                        && (filter.ApprovalStatus.Count() > 0 ? filter.ApprovalStatus.Contains(c.NextLevelApprovalStat) : c.NextLevelApprovalStat == string.Empty)
                           && (filter.OfferStatus.Count() > 0 ? filter.OfferStatus.Contains(c.OfferStatusStatDesc) : c.OfferStatusStatDesc == string.Empty)
                            && (filter.JoiningStatus.Count() > 0 ? filter.JoiningStatus.Contains(c.JoiningStatusStatDesc) : c.JoiningStatusStatDesc == string.Empty)
                             && (filter.ClientName.Count() > 0 ? filter.ClientName.Contains(c.ClientName) : c.ClientName == null)
                              && (filter.CandidateLocation.Count() > 0 ? filter.CandidateLocation.Contains(c.preferredLocation) : c.preferredLocation == null)).ToList();
                        MeetingVM NewModel = new MeetingVM();


                        NewModel.SubVideo_List = modelList.ToList();
                        NewModel.candidateName = model.candidateName;
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
                        if (NewModel.IreportModel.QuestionsCollection == null)
                        {
                            ViewBag.QuestionsDiscussedCount = 0;
                        }
                        else
                        {
                            ViewBag.QuestionsDiscussedCount = NewModel.IreportModel.QuestionsCollection?.Count();
                        }

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
            //store data in database
            var email = HttpContext.Session.GetString("email");
            var modelList = _meetingService.VedioLike(value, vedioid, email);

            return Json("Success");
        }

        [HttpPost]
        public JsonResult VedioShare(string value, string vedioid)
        {
            //store data in database
            var modelList = _meetingService.VedioShare(value, vedioid);

            return Json("Success");
        }
        [HttpPost]
        public JsonResult VedioComment(string comment, string vedioid)
        {
            //store data in database
            var email = HttpContext.Session.GetString("email");
       
            var modelList = _meetingService.VedioComment(comment, vedioid,email);

            return Json("Success");
        }

    }
}