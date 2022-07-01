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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using X.PagedList;

namespace Risebird.Web.Controllers
{
    [Authorize]
    public class CommonPoolController : Controller
    {
        private readonly IMeetingService _meetingService;
        private readonly IUserService _userService;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly ISkillService _skillService;
        private readonly IIRFService _iRFService;
        private readonly IIRFRepository _iRFRepository;
        private readonly IUserRepository _userRepository;
        private readonly INotificationHelper _notificationHelper;
        public CommonPoolController(IMeetingService meetingService, IApplicationErrorLogger applicationErrorLogger, IAzureStorageHelper azureStorageHelper, ISkillService skillService, IIRFRepository iRFRepository, IIRFService iRFService, IUserRepository userRepository, INotificationHelper notificationHelper, IUserService userService)
        {
            _meetingService = meetingService;
            _applicationErrorLogger = applicationErrorLogger;
            _azureStorageHelper = azureStorageHelper;
            _skillService = skillService;
            _iRFRepository = iRFRepository;
            _iRFService = iRFService;
            _userRepository = userRepository;
            _notificationHelper = notificationHelper;
            _userService = userService;
        }

        /// <summary>
        ///Video view of selected and borderline candidates
        /// </summary>
        /// <param name="Page"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet]
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
                    if (profileName == "Client")
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        if (profileName == "Client")
                        {
                            var client = await _userRepository.GetClientUserDetails(emailid);
                            ClientId = client.ClientId;
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
                            modelList = await _meetingService.GetCPMeetings(ClientId, profileName, emailid);
                        }
                        modelList = (from m in modelList
                                     select (new MeetingVM
                                     {
                                         EmailId = m.EmailId,
                                         imid = m.imid,
                                         IRFId = m.IRFId,
                                         interviewDate = m.interviewDate,
                                         interviewSlot = m.interviewSlot,
                                         interviewDuration = m.interviewDuration,
                                         candidateEmail = m.candidateEmail,
                                         ClientHrContact = m.ClientHrContact,
                                         confirmationStatus = m.confirmationStatus,
                                         zoomLink = m.zoomLink,
                                         risebirdHR = m.risebirdHR,
                                         interviewer = m.interviewer,
                                         preferredLocation = m.preferredLocation,
                                         IsBiometricEnabled = m.IsBiometricEnabled,
                                         InterviewBiometryScore = m.InterviewBiometryScore,
                                         expectedPerHourRate = m.expectedPerHourRate,
                                         staffingAgency = m.staffingAgency,
                                         joiningTime = m.joiningTime,
                                         dtCreatedOn = m.dtCreatedOn,
                                         candidateName = m.candidateName,
                                         currentEmployerName = m.currentEmployerName,
                                         QCInterviewer = m.QCInterviewer,
                                         IsEmergency = m.IsEmergency,
                                         JoiningBiometryScore = m.JoiningBiometryScore,
                                         MobileNumber = m.MobileNumber,
                                         resumeLink = m.resumeLink,
                                         irfIntId = m.irfIntId,
                                         JDLink = m.JDLink,
                                         JDTiltle = m.JDTiltle,
                                         ClientId = m.ClientId,
                                         ClientName = m.ClientName,
                                         AadharFileLink = m.AadharFileLink,
                                         VideoLink = m.VideoLink,
                                         InterviewerComment = m.InterviewerComment,
                                         IsApproved = m.IsApproved,
                                         InterviewStatus = m.InterviewStatus,
                                         MeetingStatus = m.MeetingStatus,
                                         CandidateId = m.CandidateId,
                                         ThumbnailImage = m.ThumbnailImage,
                                         QCInterview = m.QCInterview,
                                         CandidateAvailable = m.CandidateAvailable,
                                         MeetingPatchCount = m.MeetingPatchCount,
                                         TechSkillScore = m.TechSkillScore,
                                         IsLock = m.IsLock,
                                         UnlockVideo = m.EmailId == emailid ? true : false
                                     })).ToList();


                        ViewBag.CandidateList = modelList.Select(x => x.candidateName).Distinct().ToList();
                        ViewBag.PrimarySkillList = modelList.Select(x => x.JDTiltle).Distinct().ToList();
                        ViewBag.ClientHrContactList = modelList.Select(x => x.ClientHrContact).Distinct().ToList();
                        ViewBag.preferredLocationList = modelList.Select(x => x.preferredLocation).Distinct().ToList();
                        ViewBag.ClientNameList = modelList.Select(x => x.ClientName).Distinct().ToList();
                        ViewBag.IsShortListed = modelList.Select(x => x.ShortListstatus).Distinct().ToList();
                        ViewBag.IsIntrested = modelList.Select(x => x.IsIntrested).Distinct().ToList();
                        ViewBag.CandidateAvailable = modelList.Select(x => x.CandidateAvailable).Distinct().ToList();

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
                        if (HttpContext.Session.GetString("CommonPoolfilters") != null)
                        {
                            var filter = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("CommonPoolfilters"));
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
                            filter.CandidateAvailable = filter.CandidateAvailable == null ? filter.CandidateAvailable = modelList.Select(x => x.CandidateAvailable).ToArray() : filter.CandidateAvailable = filter.CandidateAvailable;

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
                             && (filter.IsIntrested.Count() > 0 ? filter.IsIntrested.Contains(c.IsIntrested) : c.IsIntrested == string.Empty)
                             && (filter.CandidateAvailable.Count() > 0 ? filter.CandidateAvailable.Contains(c.CandidateAvailable) : c.CandidateAvailable == string.Empty)).ToList();

                            ViewBag.CandidateList = modelList.Select(x => x.candidateName).Distinct().ToList();
                            ViewBag.PrimarySkillList = modelList.Select(x => x.JDTiltle).Distinct().ToList();
                            ViewBag.ClientHrContactList = modelList.Select(x => x.ClientHrContact).Distinct().ToList();
                            ViewBag.preferredLocationList = modelList.Select(x => x.preferredLocation).Distinct().ToList();
                            ViewBag.ClientNameList = modelList.Select(x => x.ClientName).Distinct().ToList();
                            ViewBag.IsShortListed = modelList.Select(x => x.ShortListstatus).Distinct().ToList();
                            ViewBag.IsIntrested = modelList.Select(x => x.IsIntrested).Distinct().ToList();
                            ViewBag.CandidateAvailable = modelList.Select(x => x.CandidateAvailable).Distinct().ToList();

                        }
                        else
                        {
                            modelList = modelList.Where(x => x.CandidateAvailable == "Available").ToList();
                        }

                        ViewBag.ProfileName = profileName;
                        if (!String.IsNullOrEmpty(searchvalue))
                        {
                            modelList = modelList.Where(x => x.candidateName.ToUpper().Contains(searchvalue.ToUpper()) || x.JDTiltle.ToUpper().Contains(searchvalue.ToUpper())).ToList();
                            ViewBag.searchvalue = searchvalue;
                        }
                        else
                        {
                            ViewBag.searchvalue = "";
                        }


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
                    if (filter != null)
                    {
                        HttpContext.Session.SetString("CommonPoolfilters", JsonConvert.SerializeObject(filter));
                    }
                    return RedirectToAction("Index", new { Page = Page });
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
        [HttpGet]
        public async Task<IActionResult> ResetCommonPoolFilter()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("CommonPoolfilters");
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
        [AllowAnonymous]
        [HttpGet]
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
                    ViewBag.ResultStatus = "VideoPresent";
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Client" || profileName == "ExternalUser")
                    {
                        var emailId = HttpContext.Session.GetString("email")?.Trim();
                        emailId = emailId.Replace("\"", "");
                        if (profileName == "Client")
                        {
                            var client = await _userRepository.GetClientUserDetails(emailId);
                            ClientId = client.ClientId;
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
                            modelList1 = await _meetingService.GetCPMeetings(ClientId, profileName, emailId);
                        }
                        else if (profileName == "ExternalUser")
                        {
                            modelList1 = await _meetingService.GetRiseTubeMeetings(0, profileName, emailId);
                            if (id == 0)
                            {
                                id = modelList1.OrderByDescending(x => x.SharedOn).Select(x => x.imid).FirstOrDefault();
                            }
                        }

                        //Start:Save View counts of vedio
                        if (HttpContext.Session.GetString("UserLogedIn") != null)
                        {
                            string values = string.Empty;
                            var valuestatus = HttpContext.Session.GetString("UserLogedIn")?.Trim();
                            if (valuestatus == "Yes")
                            {
                                var emailaddress = HttpContext.Session.GetString("email")?.Trim();
                                emailaddress = emailaddress.Replace("\"", "");
                                await _meetingService.AddTotalViewCount(id, emailaddress, "Public");
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
                                        var emailaddress = HttpContext.Session.GetString("email")?.Trim();
                                        emailaddress = emailaddress.Replace("\"", "");
                                        await _meetingService.AddTotalViewCount(id, emailaddress, "Public");
                                        //save into database to increase view count save into main table from where data is picked
                                    }
                                }
                                //not do anything
                            }
                            values = values + ";" + id;
                            HttpContext.Session.SetString("UserLogedInVedioId", values);
                        }
                        //End:Save View counts of vedio

                        MeetingVM model = await _meetingService.GetMeetingbyid(id, emailId);
                        if (model == null)
                        {
                            model = new MeetingVM();
                            ViewBag.ResultStatus = "VideoNotPresent";
                            return View(model);

                        }
                        if (profileName == "ExternalUser")
                        {
                            modelList1 = modelList1.OrderByDescending(x => x.interviewDate).ToList();
                        }
                        else
                        {
                            modelList1 = modelList1.Where(c => c.JDTiltle.Contains(model.JDTiltle.Split("|")[0])).OrderByDescending(x => x.interviewDate).ToList();
                        }
                        modelList1 = modelList1.Select(x =>
                        {
                            x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty;
                            return x;
                        }).ToList();
                        var IRListModel = _meetingService.GetInteviewReport(model.imid, model.irfIntId).Result;
                        var modelList = modelList1;
                        if (HttpContext.Session.GetString("CommonPoolfilters") != null)
                        {
                            var filter1 = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("CommonPoolfilters"));
                            filter1.IsShortListed = filter1.IsShortListed == null ? filter1.IsShortListed = modelList.Select(x => x.ShortListstatus).ToArray() : filter1.IsShortListed = filter1.IsShortListed;
                            filter1.IsIntrested = filter1.IsIntrested == null ? filter1.IsIntrested = modelList.Select(x => x.IsIntrested).ToArray() : filter1.IsIntrested = filter1.IsIntrested;
                            modelList = modelList.Where(c => (filter1.IsShortListed.Count() > 0 ? filter1.IsShortListed.Contains(c.ShortListstatus) : c.ShortListstatus == string.Empty)
                                       && (filter1.IsIntrested.Count() > 0 ? filter1.IsIntrested.Contains(c.IsIntrested) : c.IsIntrested == string.Empty)).ToList();
                        }
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
                            filter.CandidateAvailable = filter.CandidateAvailable == null ? filter.CandidateAvailable = modelList.Select(x => x.CandidateAvailable).ToArray() : filter.CandidateAvailable = filter.CandidateAvailable;

                            modelList = modelList.Where(c =>
                           (filter.ClientHR.Count() > 0 ? filter.ClientHR.Contains(c.ClientHrContact) : c.ClientHrContact == null)
                          && (filter.CandidateName.Count() > 0 ? filter.CandidateName.Contains(c.candidateName) : c.candidateName == null)
                        && (filter.ApprovalStatus.Count() > 0 ? filter.ApprovalStatus.Contains(c.NextLevelApprovalStat) : c.NextLevelApprovalStat == string.Empty)
                           && (filter.OfferStatus.Count() > 0 ? filter.OfferStatus.Contains(c.OfferStatusStatDesc) : c.OfferStatusStatDesc == string.Empty)
                            && (filter.JoiningStatus.Count() > 0 ? filter.JoiningStatus.Contains(c.JoiningStatusStatDesc) : c.JoiningStatusStatDesc == string.Empty)
                             && (filter.ClientName.Count() > 0 ? filter.ClientName.Contains(c.ClientName) : c.ClientName == null)
                              && (filter.CandidateLocation.Count() > 0 ? filter.CandidateLocation.Contains(c.preferredLocation) : c.preferredLocation == null)
                                && (filter.IsShortListed.Count() > 0 ? filter.IsShortListed.Contains(c.ShortListstatus) : c.ShortListstatus == string.Empty)
                        && (filter.IsIntrested.Count() > 0 ? filter.IsIntrested.Contains(c.IsIntrested) : c.IsIntrested == string.Empty)
                        && (filter.CandidateAvailable.Count() > 0 ? filter.CandidateAvailable.Contains(c.CandidateAvailable) : c.CandidateAvailable == string.Empty)).ToList();

                        }
                        else
                        {
                            modelList = modelList.Where(x => x.CandidateAvailable == "Available").ToList();
                        }


                        MeetingVM NewModel = new MeetingVM();

                        if (HttpContext.Session.GetString("VideoPagefilters") != null || HttpContext.Session.GetString("CommonPoolfilters") != null)
                        {
                            NewModel.SubVideo_List = modelList.ToList().Where(x => x.imid != id).ToList();
                        }
                        else
                        {
                            NewModel.SubVideo_List = modelList1.ToList().Where(x => x.imid != id).ToList();
                        }


                        NewModel.candidateName = model.candidateName;



                        ViewBag.ProfileName = profileName;


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
                        if (profileName == "CuratedUser")
                        {
                            if (!String.IsNullOrEmpty(IRListModel.encodedresumeLink))
                            {
                                NewModel.resumeLink = IRListModel.encodedresumeLink;
                            }
                        }
                        NewModel.zoomLink = model.zoomLink;
                        NewModel.IreportModel = IRListModel ?? new InterviewReport();
                        NewModel.IsLike = model.IsLike;
                        NewModel.TotalView = model.TotalView;
                        NewModel.IsShared = model.IsShared;
                        NewModel.TotalVedioLike = model.TotalVedioLike;
                        NewModel.TotalVedioDisLike = model.TotalVedioDisLike;
                        NewModel.ShortListCount = model.ShortListCount;
                        NewModel.IsShortlisted = model.IsShortlisted;
                        NewModel.ThumbnailImage = model.ThumbnailImage;
                        NewModel.IsIntrested = model.IsIntrested;
                        NewModel.IsOnline = model.IsOnline;
                        NewModel.candidateEmail = model.candidateEmail;
                        NewModel.MeetingPatch = model.MeetingPatch;
                        if (model.MeetingPatch.Count() > 0)
                        {
                            NewModel.VideoLink = model.MeetingPatch.FirstOrDefault().VideoLink;
                        }
                        if (NewModel.IreportModel.QuestionsCollection.Where(x => x.VideoLink != null && x.VideoLink != "").Count() > 0)
                        {
                            NewModel.IreportModel.QuestionsCollection = NewModel.IreportModel.QuestionsCollection.Where(x => x.VideoLink != null && x.VideoLink != "").ToList();
                        }
                        // to get videocount details
                        var videodetailscount = await _meetingService.GetTotalViewCount(id);
                        NewModel.VideoCountDetail = videodetailscount.ToList();
                        NewModel.FeedbackAudioLink = model.FeedbackAudioLink;
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
                        NewModel.IsJoinabilitySelectabilityRequired = model.IsJoinabilitySelectabilityRequired;
                        if (IRListModel != null)
                        {
                            GetStatusList(IRListModel, model.irfIntId);
                        }
                        ViewBag.VideoStatus = "Private";

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
                    ViewBag.ResultStatus = "VideoPresent";
                    MeetingVM model = await _meetingService.GetMeetingbyid(id, "");
                    var status = String.IsNullOrEmpty(model.IsShared) ? "" : model.IsShared;
                    if (status == "Public")
                    {
                        if (model.CandidateAvailable != "Available")
                        {
                            model = new MeetingVM();
                            ViewBag.ResultStatus = "VideoNotPresent";
                        }

                        var IRListModel = _meetingService.GetInteviewReport(model.imid, model.irfIntId).Result;
                        if (IRListModel == null)
                        {
                            IRListModel = new InterviewReport();
                        }
                        else
                        {
                            var usernames = IRListModel.CandidateName.Trim().Split(" ");
                            var namemodel = String.Empty;
                            foreach (var name in usernames)
                            {
                                if (name != null && name.Length >= 2)
                                    namemodel = namemodel + name?.ToCharArray()[0] + name?.ToCharArray()[1];
                                else if (name != null && name.Length == 1)
                                    namemodel += name?.ToCharArray()[0];
                            }
                            IRListModel.CandidateName = namemodel;
                        }
                        MeetingVM NewModel = new MeetingVM
                        {
                            SubVideo_List = new List<MeetingVM>()
                        };
                        //NewModel.candidateName = model.candidateName;

                        var usernames1 = model.candidateName.Trim().Split(" ");
                        var namemodel1 = String.Empty;
                        foreach (var name in usernames1)
                        {
                            if (name != null && name.Length >= 2)
                                namemodel1 = namemodel1 + name?.ToCharArray()[0] + name?.ToCharArray()[1];
                            else if (name != null && name.Length == 1)
                                namemodel1 += name?.ToCharArray()[0];
                        }
                        NewModel.candidateName = namemodel1;

                        ViewBag.ProfileName = "";
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
                        if (!String.IsNullOrEmpty(IRListModel.encodedresumeLink))
                        {
                            NewModel.resumeLink = IRListModel.encodedresumeLink;
                        }
                        NewModel.zoomLink = model.zoomLink;
                        NewModel.IreportModel = IRListModel ?? new InterviewReport();
                        NewModel.IsLike = model.IsLike;
                        NewModel.TotalView = model.TotalView;
                        NewModel.IsShared = model.IsShared;
                        NewModel.TotalVedioLike = model.TotalVedioLike;
                        NewModel.TotalVedioDisLike = model.TotalVedioDisLike;
                        NewModel.ShortListCount = model.ShortListCount;
                        NewModel.IsShortlisted = model.IsShortlisted;
                        NewModel.ThumbnailImage = model.ThumbnailImage;
                        NewModel.IsIntrested = model.IsIntrested;
                        NewModel.IsOnline = model.IsOnline;
                        NewModel.candidateEmail = model.candidateEmail;
                        NewModel.FeedbackAudioLink = model.FeedbackAudioLink;
                        NewModel.MeetingPatch = model.MeetingPatch;
                        if (model.MeetingPatch.Count() > 0)
                        {
                            NewModel.VideoLink = model.MeetingPatch.FirstOrDefault().VideoLink;
                        }
                        if (NewModel.IreportModel.QuestionsCollection.Where(x => x.VideoLink != null && x.VideoLink != "").Count() > 0)
                        {
                            NewModel.IreportModel.QuestionsCollection = NewModel.IreportModel.QuestionsCollection.Where(x => x.VideoLink != null && x.VideoLink != "").ToList();
                        }
                        var videodetailscount = await _meetingService.GetTotalViewCount(id);
                        NewModel.VideoCountDetail = videodetailscount.ToList();
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
                        NewModel.IsJoinabilitySelectabilityRequired = model.IsJoinabilitySelectabilityRequired;
                        ViewBag.VideoStatus = "Public";
                        //ViewBag.VideoStatus = "Private";
                        var menus = _userService.GetMenuBasedOnUserEmailld("ExternalUser").GroupBy(p => p.functionName).Select(g => g.First()).ToList();
                        if (menus != null)
                        {
                            menus = menus.Where(x => x.functionName != "Support" && x.functionName != "RiseTube").ToList();
                            HttpContext.Session.SetString("userMenus", JsonConvert.SerializeObject(menus));
                        }
                        if (IRListModel != null)
                        {
                            GetStatusList(IRListModel, model.irfIntId);
                        }
                        //desible Like and dilisk button and share button and comment opetion
                        return View(NewModel);
                        //END: Share button functionaly
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }

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
                    ViewBag.ResultStatus = "VideoPresent";
                    //Start:Save View counts of vedio
                    if (HttpContext.Session.GetString("UserLogedIn") != null)
                    {
                        string values = String.Empty;
                        var valuestatus = HttpContext.Session.GetString("UserLogedIn")?.Trim();
                        if (valuestatus == "Yes")
                        {
                            var emailaddress = HttpContext.Session.GetString("email")?.Trim();
                            emailaddress = emailaddress.Replace("\"", "");
                            await _meetingService.AddTotalViewCount(id, emailaddress, "Public");
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
                                    var emailaddress = HttpContext.Session.GetString("email")?.Trim();
                                    emailaddress = emailaddress.Replace("\"", "");
                                    await _meetingService.AddTotalViewCount(id, emailaddress, "Public");
                                    //save into database to increase view count save into main table from where data is picked
                                }
                            }
                            //not do anything
                        }
                        values = values + ";" + id;
                        HttpContext.Session.SetString("UserLogedInVedioId", values);
                    }
                    //End:Save View counts of vedio
                    bool isClientUser = false;
                    int ClientId = 0;
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Client" || profileName == "ExternalUser")
                    {
                        var emailId = HttpContext.Session.GetString("email")?.Trim();
                        emailId = emailId.Replace("\"", "");
                        if (profileName == "Client")
                        {
                            var client = await _userRepository.GetClientUserDetails(emailId);
                            ClientId = client.ClientId;
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
                            modelList = await _meetingService.GetCPMeetings(ClientId, profileName, emailId);
                        }

                        MeetingVM model = await _meetingService.GetMeetingbyid(id, emailId);
                        //MeetingVM model = modelList.Where(a => a.imid == id).FirstOrDefault();
                        modelList = modelList.Where(c => c.JDTiltle.Contains(model.JDTiltle.Split("|")[0])).OrderByDescending(x => x.interviewDate).ToList();
                        var IRListModel = _meetingService.GetInteviewReport(model.imid, model.irfIntId).Result;
                        if (HttpContext.Session.GetString("CommonPoolfilters") != null)
                        {
                            var filter1 = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("CommonPoolfilters"));
                            filter1.IsShortListed = filter1.IsShortListed == null ? filter1.IsShortListed = modelList.Select(x => x.ShortListstatus).ToArray() : filter1.IsShortListed = filter1.IsShortListed;
                            filter1.IsIntrested = filter1.IsIntrested == null ? filter1.IsIntrested = modelList.Select(x => x.IsIntrested).ToArray() : filter1.IsIntrested = filter1.IsIntrested;
                            modelList = modelList.Where(c => (filter1.IsShortListed.Count() > 0 ? filter1.IsShortListed.Contains(c.ShortListstatus) : c.ShortListstatus == string.Empty)
                                       && (filter1.IsIntrested.Count() > 0 ? filter1.IsIntrested.Contains(c.IsIntrested) : c.IsIntrested == string.Empty)).ToList();
                        }
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
                        filter.CandidateAvailable = filter.CandidateAvailable == null ? filter.CandidateAvailable = modelList.Select(x => x.CandidateAvailable).ToArray() : filter.CandidateAvailable = filter.CandidateAvailable;

                        modelList = modelList.Where(c =>
                           (filter.ClientHR.Count() > 0 ? filter.ClientHR.Contains(c.ClientHrContact) : c.ClientHrContact == null)
                          && (filter.CandidateName.Count() > 0 ? filter.CandidateName.Contains(c.candidateName) : c.candidateName == null)
                        && (filter.ApprovalStatus.Count() > 0 ? filter.ApprovalStatus.Contains(c.NextLevelApprovalStat) : c.NextLevelApprovalStat == string.Empty)
                           && (filter.OfferStatus.Count() > 0 ? filter.OfferStatus.Contains(c.OfferStatusStatDesc) : c.OfferStatusStatDesc == string.Empty)
                            && (filter.JoiningStatus.Count() > 0 ? filter.JoiningStatus.Contains(c.JoiningStatusStatDesc) : c.JoiningStatusStatDesc == string.Empty)
                             && (filter.ClientName.Count() > 0 ? filter.ClientName.Contains(c.ClientName) : c.ClientName == null)
                              && (filter.CandidateLocation.Count() > 0 ? filter.CandidateLocation.Contains(c.preferredLocation) : c.preferredLocation == null)
                                && (filter.IsShortListed.Count() > 0 ? filter.IsShortListed.Contains(c.ShortListstatus) : c.ShortListstatus == string.Empty)
                        && (filter.IsIntrested.Count() > 0 ? filter.IsIntrested.Contains(c.IsIntrested) : c.IsIntrested == string.Empty)
                        && (filter.CandidateAvailable.Count() > 0 ? filter.CandidateAvailable.Contains(c.CandidateAvailable) : c.CandidateAvailable == string.Empty)).ToList();

                        MeetingVM NewModel = new MeetingVM
                        {
                            SubVideo_List = modelList.ToList(),

                            candidateName = model.candidateName
                        };

                        ViewBag.ProfileName = profileName;

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
                        if (profileName == "CuratedUser")
                        {
                            if (!String.IsNullOrEmpty(IRListModel.encodedresumeLink))
                            {
                                NewModel.resumeLink = IRListModel.encodedresumeLink;
                            }
                        }
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
                        NewModel.IsOnline = model.IsOnline;
                        NewModel.candidateEmail = model.candidateEmail;
                        NewModel.MeetingPatch = model.MeetingPatch;
                        if (model.MeetingPatch.Count() > 0)
                        {
                            NewModel.VideoLink = model.MeetingPatch.FirstOrDefault().VideoLink;
                        }
                        if (NewModel.IreportModel.QuestionsCollection.Where(x => x.VideoLink != null && x.VideoLink != "").Count() > 0)
                        {
                            NewModel.IreportModel.QuestionsCollection = NewModel.IreportModel.QuestionsCollection.Where(x => x.VideoLink != null && x.VideoLink != "").ToList();
                        }
                        var videodetailscount = await _meetingService.GetTotalViewCount(id);
                        NewModel.VideoCountDetail = videodetailscount.ToList();
                        NewModel.FeedbackAudioLink = model.FeedbackAudioLink;
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
                        ViewBag.VideoStatus = "Private";


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
        [HttpGet]
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
        [HttpGet]
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

            model.QuestionsList = new SelectList(_meetingService.GetTechSkillComments(irfId).Result, "Comment", "Comment");
        }

        [HttpPost]
        public JsonResult VideoLike(string value, string videoid)
        {
            var email = HttpContext.Session.GetString("email");
            var modelList = _meetingService.VedioLike(value, videoid, email);
            return Json(modelList);
        }

        [HttpPost]
        public JsonResult VideoShare(string value, string videoid, string name, string Emailids)
        {
            _meetingService.VedioShare(value, videoid);
            string Name = string.Empty;
            if (name != null && name.Length >= 2)
                Name = Name + name?.ToCharArray()[0] + name?.ToCharArray()[1];
            else if (name != null && name.Length == 1)
                Name += name?.ToCharArray()[0];
            //send a email
            var email = HttpContext.Session.GetString("email");
            email = email.Replace("\"", "");
            string currenturl = Request.Host.ToString() + "/CommonPool/VideoPage?id=" + videoid;
            Mail _mail = new Mail
            {
                TO = Emailids,
                CC = email,
                Subject = "Risebird: Interview video is shared with you"
            };
            var result = _notificationHelper.SharedVideoNofiticationExternal(_mail, Name, currenturl);
            if (result.Contains("Mail sent successfully"))
            {
                return Json("Success");
            }
            else
            {
                return Json("Fail");
            }

        }

        [HttpPost]
        public JsonResult publicVideoShare(string value, string videoid)
        {
            _meetingService.VedioShare(value, videoid);
            return Json("Success");
        }

        [HttpPost]
        public JsonResult VideoComment(string comment, string videoid)
        {
            var email = HttpContext.Session.GetString("email");
            _meetingService.VedioComment(comment, videoid, email);
            return Json("Success");
        }

        [HttpPost]
        public JsonResult UpdateVideoComment(string videoid)
        {
            //store data in database
            int id = Convert.ToInt32(videoid);
            var comments = _meetingService.GetvedioComments(id);
            return Json(comments);
        }

        [HttpPost]
        public JsonResult CandidateShortList(string videoid)
        {
            var email = HttpContext.Session.GetString("email");
            var Shortlistcount = _meetingService.GetvedioShortList(videoid, email);
            int result = 0;
            if (Shortlistcount != null)
            {
                result = Shortlistcount.totalcount;
                Mail _mail = new Mail
                {
                    TO = Shortlistcount.Email,
                    Subject = "Your profile has been shortlisted"
                };
                email = email.Replace("\"", "");
                email = email.Split("@")[1].Split(".")[0];
                StringBuilder bulider = new StringBuilder();

                bulider.Append("Client: " + email.ToUpper() + " has shortlisted your profile");
                _notificationHelper.CuretatedNotification(_mail, "Candidate", bulider.ToString());

            }
            return Json(result);
        }

        [HttpPost]
        public JsonResult CandidateSuitable(string videoid, string Comment)
        {
            //store data in database
            int id = Convert.ToInt32(videoid);
            var email = HttpContext.Session.GetString("email");
            var Status = _meetingService.CandidateSuitable(id, email, Comment);
            if (Status != null)
            {
                Mail _mail = new Mail
                {
                    TO = Status,
                    Subject = "Client has shown interest in your profile"
                };
                email = email.Replace("\"", "");
                email = email.Split("@")[1].Split(".")[0];
                StringBuilder bulider = new StringBuilder();
                bulider.Append("Client: " + email.ToUpper() + " has showed interest in your profile");
                _notificationHelper.CuretatedNotification(_mail, "Candidate", bulider.ToString());
            }
            return Json("Success");
        }

        [HttpPost]
        public JsonResult VedioShared(string Emailids, string videoid, string Name, string skills)
        {
            var email = HttpContext.Session.GetString("email");
            email = email.Replace("\"", "");
            var modelList = _meetingService.VedioShared(Emailids, videoid, email);
            string currenturl = Request.Host.ToString() + Request.Path.ToString() + "?id=" + videoid;

            if (!String.IsNullOrEmpty(modelList))
            {
                Mail _mail1 = new Mail
                {
                    TO = Emailids,
                    CC = email,
                    Subject = "Risebird: Candidate Name: " + Name + " Interview video is shared with you"
                };
                _ = _notificationHelper.SharedVideoNofitication(_mail1, Name, skills);
            }
            var modelemail = modelList.Split(";");
            foreach (var item in modelemail)
            {
                Emailids = Emailids.Replace(item, "");
                Emailids = Emailids.Replace(";;", ";");
            }
            if (Emailids != "" && Emailids != ";")
            {
                Mail _mail = new Mail
                {
                    TO = Emailids,
                    CC = email,
                    Subject = "Risebird: Candidate Name: " + Name + " Interview video is shared with you"
                };
                _notificationHelper.SharedVideoNofiticationExternal(_mail, Name, currenturl);
            }
            return Json("");
        }

        [HttpPost]
        public JsonResult ShareVideoLinkToMail(string Emailids, string videoid, string Name, string comment, string skills, string resumeLink)
        {
            var email = HttpContext.Session.GetString("email");
            email = email.Replace("\"", "");

            string currenturl = videoid;
            if (resumeLink == null)
            {
                resumeLink = string.Empty;
            }

            if (Emailids != "" && Emailids != ";")
            {
                Mail _mail = new Mail
                {
                    TO = Emailids,
                    CC = email,
                    Subject = "An interview of Candidate: " + Name + " video is shared with you"
                };
                _notificationHelper.SharedVideoNofiticationExternal(_mail, Name, currenturl, comment, resumeLink);
            }
            return Json("");
        }

        [HttpGet]
        public JsonResult VideoShareEmail(string videoid, string Emailid, string CurrentUrl)
        {
            //Start:Save View counts of vedio
            Emailid = Emailid.Replace("\"", "");
            if (CurrentUrl.Contains("SharedVideoPage"))
            {
                videoid = CurrentUrl.Split("/")[CurrentUrl.Split("/").Count() - 1].ToString();
                _meetingService.AddTotalViewCount(Convert.ToInt32(videoid), Emailid, "Shared");
            }
            else
            {
                _meetingService.AddTotalViewCount(Convert.ToInt32(videoid), Emailid, "Public");
            }
            //End:Save View counts of vedio
            return Json("Succecss");
        }

        [HttpGet]
        public JsonResult LockorUnlockVideo(string imid, int islock, string emailid = null)
        {
            if (HttpContext.Session.GetString("email") != null)
            {
                if (String.IsNullOrEmpty(emailid))
                {
                    emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                }
                var result = _meetingService.LockorUnlockVideo(imid, islock, emailid);
                return Json(result);
            }
            else
            {
                return Json("Fail");
            }
        }

        [Authorize]
        [HttpGet]
        public IActionResult SelectedCandidate()
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var email = HttpContext.Session.GetString("email")?.Trim();
                email = email.Replace("\"", "");
                if (profileName == "Admin" || profileName == "SeniorHR")
                {
                    List<MeetingVM> modelList = new List<MeetingVM>();
                    if (HttpContext.Session.GetString("CommonPoolSelectedCandidatefilters") != null)
                    {
                        var filter = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("CommonPoolSelectedCandidatefilters"));
                        if (filter.StartDate != null && filter.EndDate != null)
                        {
                            modelList = _meetingService.GetAllSelectedCandidateInterviewList(filter.StartDate.Value, filter.EndDate.Value.AddDays(1), email);
                        }
                        else
                        {
                            modelList = _meetingService.GetAllSelectedCandidateInterviewList(DateTime.Now.Date.AddMonths(-1), DateTime.Now.Date.AddDays(1), email);
                        }
                        modelList = modelList.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty; x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty; x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); x.QCStatusStatusDesc = x.QCStatus.HasValue ? ((QCStatus)x.QCStatus).GetDescription() : string.Empty; return x; }).ToList();

                        foreach (var item in modelList)
                        {
                            item.NextLevelApprovalStat = item.IsApproved.HasValue ? ((NextLevelApprovalStatus)item.IsApproved).GetDescription() : string.Empty;

                            if (item.confirmationStatus.Trim() != ReceiveMeetingStatus.Cancelled.GetDescription())
                            {
                                if (item.interviewDate < DateTime.Now && item.MeetingStatusStatusDesc == Risebird.Entities.Enum.MeetingStatus.Scheduled.GetDescription())
                                {
                                    item.MeetingStatusStatusDesc = "Status Pending";
                                }
                            }
                            else
                            {
                                item.MeetingStatusStatusDesc = string.Empty;
                            }
                            if ((item.InterviewStatus == 4 || item.InterviewStatus == 2) && item.QCStatus == null)
                            {
                                item.intervieweStatusStatDesc = "QC Pending(" + item.intervieweStatusStatDesc + ")";
                            }
                            else if ((item.InterviewStatus == 4 || item.InterviewStatus == 2) && item.QCStatus == 2)
                            {
                                item.intervieweStatusStatDesc = "QC Failed(" + item.intervieweStatusStatDesc + ")";
                            }
                        }
                        filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = modelList.Select(x => x.JDTiltle.Split("|")[0]).ToArray() : filter.PrimarySkill = filter.PrimarySkill;
                        filter.RFID = filter.RFID == null ? filter.RFID = modelList.Select(x => x.IRFId).ToArray() : filter.RFID = filter.RFID;
                        filter.ClientHR = filter.ClientHR == null ? filter.ClientHR = modelList.Select(x => x.ClientHrContact).ToArray() : filter.ClientHR = filter.ClientHR;
                        filter.ConfirmationStatus = filter.ConfirmationStatus == null ? filter.ConfirmationStatus = modelList.Select(x => x.confirmationStatus).ToArray() : filter.ConfirmationStatus = filter.ConfirmationStatus;
                        filter.MeetingStatus = filter.MeetingStatus == null ? filter.MeetingStatus = modelList.Select(x => x.MeetingStatusStatusDesc).ToArray() : filter.MeetingStatus = filter.MeetingStatus;
                        filter.InterviewStatus = filter.InterviewStatus == null ? filter.InterviewStatus = modelList.Select(x => x.intervieweStatusStatDesc).ToArray() : filter.InterviewStatus = filter.InterviewStatus;
                        filter.Interviewer = filter.Interviewer == null ? filter.Interviewer = modelList.Select(x => x.interviewer).ToArray() : filter.Interviewer = filter.Interviewer;
                        filter.ClientName = filter.ClientName == null ? filter.ClientName = modelList.Select(x => x.ClientName).ToArray() : filter.ClientName = filter.ClientName;
                        filter.Client = filter.Client == null ? filter.Client = modelList.Select(x => x.Client).ToArray() : filter.Client = filter.Client;

                        modelList = modelList.Where(c => filter.PrimarySkill != null && filter.PrimarySkill.Contains(c.JDTiltle.Split("|")[0])
                        && filter.RFID.Contains(c.IRFId)
                        && ((filter.ClientHR != null && filter.ClientHR.Count() > 0) ? filter.ClientHR.Contains(c.ClientHrContact) : c.ClientHrContact == null)
                        && ((filter.ConfirmationStatus != null && filter.ConfirmationStatus.Count() > 0) ? filter.ConfirmationStatus.Contains(c.confirmationStatus) : c.confirmationStatus == null)
                        && ((filter.MeetingStatus != null && filter.MeetingStatus.Count() > 0) ? filter.MeetingStatus.Contains(c.MeetingStatusStatusDesc) : c.MeetingStatusStatusDesc == string.Empty)
                        && ((filter.InterviewStatus != null && filter.InterviewStatus.Count() > 0) ? filter.InterviewStatus.Contains(c.intervieweStatusStatDesc) : c.intervieweStatusStatDesc == string.Empty)
                        && ((filter.Interviewer != null && filter.Interviewer.Count() > 0) ? filter.Interviewer.Contains(c.interviewer) : c.interviewer == null)
                        && ((filter.ClientName != null && filter.ClientName.Count() > 0) ? filter.ClientName.Contains(c.ClientName) : c.ClientName == null)
                        && ((filter.Client != null && filter.Client.Count() > 0) ? filter.Client.Contains(c.Client) : c.Client == null)).ToList();

                    }
                    else
                    {
                        modelList = _meetingService.GetAllSelectedCandidateInterviewList(DateTime.Now.Date.AddMonths(-1), DateTime.Now.Date.AddDays(1), email);
                        modelList = modelList.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty; x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty; x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); x.QCStatusStatusDesc = x.QCStatus.HasValue ? ((QCStatus)x.QCStatus).GetDescription() : string.Empty; return x; }).ToList();
                        foreach (var item in modelList)
                        {
                            item.NextLevelApprovalStat = item.IsApproved.HasValue ? ((NextLevelApprovalStatus)item.IsApproved).GetDescription() : string.Empty;

                            if (item.confirmationStatus.Trim() != ReceiveMeetingStatus.Cancelled.GetDescription())
                            {
                                if (item.interviewDate < DateTime.Now && item.MeetingStatusStatusDesc == Risebird.Entities.Enum.MeetingStatus.Scheduled.GetDescription())
                                {
                                    item.MeetingStatusStatusDesc = "Status Pending";
                                }
                            }
                            else
                            {
                                item.MeetingStatusStatusDesc = string.Empty;
                            }
                            if ((item.InterviewStatus == 4 || item.InterviewStatus == 2) && item.QCStatus == null)
                            {
                                item.intervieweStatusStatDesc = "QC Pending(" + item.intervieweStatusStatDesc + ")";
                            }
                            else if ((item.InterviewStatus == 4 || item.InterviewStatus == 2) && item.QCStatus == 2)
                            {
                                item.intervieweStatusStatDesc = "QC Failed(" + item.intervieweStatusStatDesc + ")";
                            }
                        }
                    }
                    return View(modelList);
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
        public IActionResult SelectedCandidate(InterviewFilter filter)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var email = HttpContext.Session.GetString("email")?.Trim();
                email = email.Replace("\"", "");
                if (profileName == "Admin" || profileName == "SeniorHR")
                {
                    if (filter != null)
                    {
                        HttpContext.Session.SetString("CommonPoolSelectedCandidatefilters", JsonConvert.SerializeObject(filter));
                    }
                    List<MeetingVM> modelList = new List<MeetingVM>();

                    if (filter.StartDate != null && filter.EndDate != null)
                    {
                        modelList = _meetingService.GetAllSelectedCandidateInterviewList(filter.StartDate.Value, filter.EndDate.Value.AddDays(1), email);
                    }
                    else
                    {
                        modelList = _meetingService.GetAllSelectedCandidateInterviewList(DateTime.Now.Date.AddMonths(-1), DateTime.Now.Date.AddDays(1), email);
                    }
                    modelList = modelList.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty; x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty; x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); x.QCStatusStatusDesc = x.QCStatus.HasValue ? ((QCStatus)x.QCStatus).GetDescription() : string.Empty; return x; }).ToList();
                    foreach (var item in modelList)
                    {
                        item.NextLevelApprovalStat = item.IsApproved.HasValue ? ((NextLevelApprovalStatus)item.IsApproved).GetDescription() : string.Empty;

                        if (item.confirmationStatus.Trim() != ReceiveMeetingStatus.Cancelled.GetDescription())
                        {
                            if (item.interviewDate < DateTime.Now && item.MeetingStatusStatusDesc == Risebird.Entities.Enum.MeetingStatus.Scheduled.GetDescription())
                            {
                                item.MeetingStatusStatusDesc = "Status Pending";
                            }
                        }
                        else
                        {
                            item.MeetingStatusStatusDesc = string.Empty;
                        }
                        if ((item.InterviewStatus == 4 || item.InterviewStatus == 2) && item.QCStatus == null)
                        {
                            item.intervieweStatusStatDesc = "QC Pending(" + item.intervieweStatusStatDesc + ")";
                        }
                        else if ((item.InterviewStatus == 4 || item.InterviewStatus == 2) && item.QCStatus == 2)
                        {
                            item.intervieweStatusStatDesc = "QC Failed(" + item.intervieweStatusStatDesc + ")";
                        }
                    }
                    filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = modelList.Select(x => x.JDTiltle.Split("|")[0]).ToArray() : filter.PrimarySkill = filter.PrimarySkill;
                    filter.RFID = filter.RFID == null ? filter.RFID = modelList.Select(x => x.IRFId).ToArray() : filter.RFID = filter.RFID;
                    filter.ClientHR = filter.ClientHR == null ? filter.ClientHR = modelList.Select(x => x.ClientHrContact).ToArray() : filter.ClientHR = filter.ClientHR;
                    filter.ConfirmationStatus = filter.ConfirmationStatus == null ? filter.ConfirmationStatus = modelList.Select(x => x.confirmationStatus).ToArray() : filter.ConfirmationStatus = filter.ConfirmationStatus;
                    filter.MeetingStatus = filter.MeetingStatus == null ? filter.MeetingStatus = modelList.Select(x => x.MeetingStatusStatusDesc).ToArray() : filter.MeetingStatus = filter.MeetingStatus;
                    filter.InterviewStatus = filter.InterviewStatus == null ? filter.InterviewStatus = modelList.Select(x => x.intervieweStatusStatDesc).ToArray() : filter.InterviewStatus = filter.InterviewStatus;
                    filter.Interviewer = filter.Interviewer == null ? filter.Interviewer = modelList.Select(x => x.interviewer).ToArray() : filter.Interviewer = filter.Interviewer;
                    filter.ClientName = filter.ClientName == null ? filter.ClientName = modelList.Select(x => x.ClientName).ToArray() : filter.ClientName = filter.ClientName;
                    filter.Client = filter.Client == null ? filter.Client = modelList.Select(x => x.Client).ToArray() : filter.Client = filter.Client;

                    modelList = modelList.Where(c => filter.PrimarySkill != null && filter.PrimarySkill.Contains(c.JDTiltle.Split("|")[0])
                    && filter.RFID.Contains(c.IRFId)
                    && ((filter.ClientHR != null && filter.ClientHR.Count() > 0) ? filter.ClientHR.Contains(c.ClientHrContact) : c.ClientHrContact == null)
                    && ((filter.ConfirmationStatus != null && filter.ConfirmationStatus.Count() > 0) ? filter.ConfirmationStatus.Contains(c.confirmationStatus) : c.confirmationStatus == null)
                    && ((filter.MeetingStatus != null && filter.MeetingStatus.Count() > 0) ? filter.MeetingStatus.Contains(c.MeetingStatusStatusDesc) : c.MeetingStatusStatusDesc == string.Empty)
                    && ((filter.InterviewStatus != null && filter.InterviewStatus.Count() > 0) ? filter.InterviewStatus.Contains(c.intervieweStatusStatDesc) : c.intervieweStatusStatDesc == string.Empty)
                    && ((filter.Interviewer != null && filter.Interviewer.Count() > 0) ? filter.Interviewer.Contains(c.interviewer) : c.interviewer == null)
                    && ((filter.ClientName != null && filter.ClientName.Count() > 0) ? filter.ClientName.Contains(c.ClientName) : c.ClientName == null)
                    && ((filter.Client != null && filter.Client.Count() > 0) ? filter.Client.Contains(c.Client) : c.Client == null)).ToList();


                    return View(modelList);
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
        public async Task<IActionResult> ResetSelectedCandidateFilter()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("CommonPoolSelectedCandidatefilters");
                    return RedirectToAction("SelectedCandidate");
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SelectedCandidate");
            }

        }

        [HttpGet]
        public JsonResult FetchClientUsers(string ClientName)
        {
            var result = _meetingService.FetchClientUsers(ClientName);
            return Json(result);
        }

        [HttpGet]
        public JsonResult SetCandidateAvailability(string imid, int isavailable)
        {
            var result = _meetingService.SetCandidateAvailability(imid, isavailable);
            return Json(result);
        }

        [HttpGet]
        public JsonResult MoveToCP(string imid, int movetocp)
        {
            var result = _meetingService.MoveToCP(imid, movetocp);
            return Json(result);
        }
    }
}
