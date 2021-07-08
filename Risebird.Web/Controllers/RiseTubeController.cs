using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using X.PagedList;

namespace Risebird.Web.Controllers
{
    //[Authorize]
    public class RiseTubeController : Controller
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
        public RiseTubeController(IMeetingService meetingService, IApplicationErrorLogger applicationErrorLogger, IAzureStorageHelper azureStorageHelper, ISkillService skillService, IIRFRepository iRFRepository, IIRFService iRFService, IUserRepository userRepository, INotificationHelper notificationHelper, IUserService userService)
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
        /// Method to apply filters on index page
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="Page"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Index(int? Page, InterviewFilter filter, string searchvalue = null)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    int ClientId = 0;
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "CuratedUser")
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        if (profileName == "Client")
                        {
                            var client = await _userRepository.GetClientUserDetails(emailid);
                            ClientId = client.ClientId;
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
                        if (Page == null || Page == 0)
                        {
                            Page = 1;
                        }
                        int totalMeetings = 0;


                        filter.StartDate = filter.StartDate == null ? new DateTime(1753, 1, 1) : filter.StartDate;
                        filter.EndDate = filter.EndDate == null ? DateTime.MaxValue : filter.EndDate;

                        if (profileName == "CuratedUser")
                        {
                            modelList = await _meetingService.GetRiseTubeMeetingsPagedFiltered(profileName, emailid, Page.Value, filter.StartDate.Value, filter.EndDate.Value, searchvalue);
                            totalMeetings = await _meetingService.GetRisetubeIndexMeetingsFilteredCount(profileName, emailid, filter.StartDate.Value, filter.EndDate.Value, searchvalue);

                        }
                        else
                        {
                            modelList = await _meetingService.GetRiseTubeMeetingsPagedFiltered(profileName, emailid, Page.Value, filter.StartDate.Value, filter.EndDate.Value, searchvalue, ClientId);
                            totalMeetings = await _meetingService.GetRisetubeIndexMeetingsFilteredCount(profileName, emailid, filter.StartDate.Value, filter.EndDate.Value, searchvalue, ClientId);

                        }



                        if (profileName == "CuratedUser")
                        {
                            foreach (var item in modelList)
                            {
                                if (item.IsIntrested != "Interest Accepted")
                                {
                                    var usernames = item.candidateName.Trim().Split(" ");
                                    var namemodel = String.Empty;
                                    foreach (var name in usernames)
                                    {
                                        if (name != null && name.Length >= 2)
                                            namemodel = namemodel + name?.ToCharArray()[0] + name?.ToCharArray()[1];
                                        else if (name != null && name.Length == 1)
                                            namemodel = namemodel + name?.ToCharArray()[0];

                                    }
                                    item.candidateName = namemodel;
                                }
                            }
                        }
                        ViewBag.ProfileName = profileName;
                        if (!String.IsNullOrEmpty(searchvalue))
                        {
                            ViewBag.searchvalue = searchvalue;
                        }
                        else
                        {
                            ViewBag.searchvalue = "";
                        }


                        var remainder = totalMeetings % 12;
                        if (remainder == 0)
                        {
                            ViewBag.Pages = totalMeetings / 12;

                        }
                        else
                        {
                            ViewBag.Pages = (totalMeetings / 12) + 1;

                        }

                        ViewBag.Page = Page.Value;
                        ViewBag.MeetingsCount = totalMeetings;
                        ViewBag.Remainder = remainder;

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
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }

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
                    int ClientId = 0;
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");

                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "CuratedUser")
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        if (profileName == "Client")
                        {
                            var client = await _userRepository.GetClientUserDetails(emailid);
                            ClientId = client.ClientId;
                        }
                        if (TempData["SuccessErrorMessageInterview"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageInterview"].ToString();
                            TempData.Remove("SuccessErrorMessageInterview");
                        }

                        if (Page == null || Page == 0)
                        {
                            Page = 1;
                        }

                        var modelList = new List<MeetingVM>();
                        int totalMeetings = 0;

                        if (HttpContext.Session.GetString("RiseTubefilters") != null)
                        {
                            var filter = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("RiseTubefilters"));

                            filter.StartDate = filter.StartDate == null ? new DateTime(1753, 1, 1) : filter.StartDate;
                            filter.EndDate = filter.EndDate == null ? DateTime.MaxValue : filter.EndDate;

                            if (profileName == "CuratedUser")
                            {
                                modelList = await _meetingService.GetRiseTubeMeetingsPagedFiltered(profileName, emailid, Page.Value, filter.StartDate.Value, filter.EndDate.Value, searchvalue);
                                totalMeetings = await _meetingService.GetRisetubeIndexMeetingsFilteredCount(profileName, emailid, filter.StartDate.Value, filter.EndDate.Value, searchvalue);

                            }
                            else
                            {
                                modelList = await _meetingService.GetRiseTubeMeetingsPagedFiltered(profileName, emailid, Page.Value, filter.StartDate.Value, filter.EndDate.Value, searchvalue, ClientId);
                                totalMeetings = await _meetingService.GetRisetubeIndexMeetingsFilteredCount(profileName, emailid, filter.StartDate.Value, filter.EndDate.Value, searchvalue, ClientId);

                            }
                        }
                        else
                        {
                            if (profileName == "Client")
                            {
                                modelList = await _meetingService.GetRiseTubeMeetingsPagedFiltered(profileName, emailid, Page.Value, DateTime.Now.Date.AddDays(-30), DateTime.Now.Date, searchvalue, ClientId);
                                totalMeetings = await _meetingService.GetRisetubeIndexMeetingsFilteredCount(profileName, emailid, DateTime.Now.Date.AddDays(-30), DateTime.Now.Date, searchvalue, ClientId);

                            }
                            else if (profileName == "CuratedUser")
                            {

                                modelList = await _meetingService.GetRiseTubeMeetingsPaged(profileName, emailid, Page.Value, searchvalue);
                                totalMeetings = await _meetingService.GetRisetubeIndexMeetingsCount(profileName, emailid, searchvalue);

                                ViewBag.Profile = "CuratedUser";
                            }
                            else
                            {
                                modelList = await _meetingService.GetRiseTubeMeetingsPaged(profileName, emailid, Page.Value, searchvalue);
                                totalMeetings = await _meetingService.GetRisetubeIndexMeetingsCount(profileName, emailid, searchvalue);

                            }
                        }


                        if (profileName == "CuratedUser")
                        {
                            foreach (var item in modelList)
                            {
                                if (item.IsIntrested != "Interest Accepted")
                                {
                                    var usernames = item.candidateName.Trim().Split(" ");
                                    var namemodel = String.Empty;
                                    foreach (var name in usernames)
                                    {
                                        if (name != null && name.Length >= 2)
                                            namemodel = namemodel + name?.ToCharArray()[0] + name?.ToCharArray()[1];
                                        else if (name != null && name.Length == 1)
                                            namemodel = namemodel + name?.ToCharArray()[0];

                                    }
                                    item.candidateName = namemodel;
                                }
                            }
                        }

                        ViewBag.ProfileName = profileName;
                        if (!String.IsNullOrEmpty(searchvalue))
                        {
                            ViewBag.searchvalue = searchvalue;
                        }
                        else
                        {
                            ViewBag.searchvalue = "";
                        }


                        var remainder = totalMeetings % 12;
                        if (remainder == 0)
                        {
                            ViewBag.Pages = totalMeetings / 12;

                        }
                        else
                        {
                            ViewBag.Pages = (totalMeetings / 12) + 1;

                        }

                        ViewBag.Page = Page.Value;
                        ViewBag.MeetingsCount = totalMeetings;
                        ViewBag.Remainder = remainder;


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
        [AllowAnonymous]
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

                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "CuratedUser" || profileName == "ExternalUser")
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
                        MeetingVM model = new MeetingVM();
                        if (isClientUser)
                        {
                            model = await _meetingService.GetMeetingbyid(id, emailId);
                            if (model == null)
                            {
                                model = new MeetingVM();
                                ViewBag.ResultStatus = "VideoNotPresent";
                                return View(model);

                            }
                            //modelList1 = await _meetingService.GetRiseTubeMeetingsByJD(ClientId, emailId, model.JDTiltle.Split("|")[0], DateTime.Now.AddDays(-14), DateTime.Now);
                            modelList1 = await _meetingService.GetRiseTubeMeetingsByIRFId(ClientId, emailId, model.IRFId, DateTime.Now.AddDays(-29), DateTime.Now);

                        }
                        else if (profileName == "CuratedUser")
                        {

                            if (String.IsNullOrEmpty(irfid))
                            {
                                model = await _meetingService.GetMeetingbyid(id, emailId);
                                //modelList1 = await _meetingService.GetRiseTubeMeetingsByJD(ClientId, emailId, model.JDTiltle.Split("|")[0], DateTime.Now.AddDays(-14), DateTime.Now, profileName);
                                modelList1 = await _meetingService.GetRiseTubeMeetingsByIRFId(ClientId, emailId, model.IRFId, DateTime.Now.AddDays(-29), DateTime.Now, profileName);

                            }


                            else if (!String.IsNullOrEmpty(irfid))
                            {
                                modelList1 = await _meetingService.GetRiseTubeMeetingsForCuratedUserByIRF(emailId, actionstatus.ToUpper(), Convert.ToInt32(irfid));
                                id = modelList1.Max(x => x.imid);

                                ViewBag.actionstatus = "SkillChannel";
                                model = await _meetingService.GetMeetingbyid(id, emailId);

                            }
                            if (model == null)
                            {
                                model = new MeetingVM();
                                ViewBag.ResultStatus = "VideoNotPresent";
                                return View(model);

                            }

                        }
                        else if (profileName == "ExternalUser")
                        {
                            modelList1 = await _meetingService.GetRiseTubeMeetings(0, profileName, emailId);
                            if (id == 0)
                            {
                                id = modelList1.OrderByDescending(x => x.SharedOn).Select(x => x.imid).FirstOrDefault();
                            }
                            model = await _meetingService.GetMeetingbyid(id, emailId);
                            if (model == null)
                            {
                                model = new MeetingVM();
                                ViewBag.ResultStatus = "VideoNotPresent";
                                return View(model);

                            }
                        }
                        else
                        {
                            model = await _meetingService.GetMeetingbyid(id, emailId);
                            if (model == null)
                            {
                                model = new MeetingVM();
                                ViewBag.ResultStatus = "VideoNotPresent";
                                return View(model);

                            }
                            //modelList1 = await _meetingService.GetRiseTubeMeetingsByJD(0, emailId, model.JDTiltle.Split("|")[0], DateTime.Now.AddDays(-14), DateTime.Now);
                            modelList1 = await _meetingService.GetRiseTubeMeetingsByIRFId(0, emailId, model.IRFId, DateTime.Now.AddDays(-29), DateTime.Now);
                        }

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


                        if (profileName == "ExternalUser")
                        {
                            modelList1 = modelList1.OrderByDescending(x => x.interviewDate).ToList();
                        }
                        else if (profileName == "CuratedUser")
                        {
                            if (!String.IsNullOrEmpty(irfid))
                            {
                                modelList1 = modelList1.Where(c => c.JDTiltle.Contains(model.JDTiltle.Split("|")[0])).OrderByDescending(x => x.interviewDate).ToList();
                            }
                        }

                        modelList1 = modelList1.Select(x =>
                        {
                            x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty;
                            return x;
                        }).ToList();
                        var IRListModel = _meetingService.GetInteviewReport(model.imid, model.irfIntId).Result;
                        var modelList = modelList1;
                        if (HttpContext.Session.GetString("RiseTubefilters") != null)
                        {
                            var filter1 = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("RiseTubefilters"));
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
                        if (profileName == "CuratedUser")
                        {
                            foreach (var itemmodel in modelList)
                            {
                                if (itemmodel.IsIntrested != "Interest Accepted")
                                {
                                    var usernames = itemmodel.candidateName.Trim().Split(" ");
                                    var namemodel = String.Empty;
                                    foreach (var name in usernames)
                                    {
                                        if (name != null && name.Length >= 2)
                                            namemodel = namemodel + name?.ToCharArray()[0] + name?.ToCharArray()[1];
                                        else if (name != null && name.Length == 1)
                                            namemodel = namemodel + name?.ToCharArray()[0];

                                    }
                                    itemmodel.candidateName = namemodel;
                                }
                            }

                        }

                        MeetingVM NewModel = new MeetingVM();

                        if (HttpContext.Session.GetString("VideoPagefilters") != null || HttpContext.Session.GetString("RiseTubefilters") != null)
                        {
                            NewModel.SubVideo_List = modelList.ToList().Where(x => x.imid != id).ToList();
                        }
                        else
                        {
                            NewModel.SubVideo_List = modelList1.ToList().Where(x => x.imid != id).ToList();
                        }

                        if (profileName == "CuratedUser")
                        {
                            if (model.IsIntrested != "Interest Accepted")
                            {
                                var usernames = model.candidateName.Trim().Split(" ");
                                var namemodel = String.Empty;
                                foreach (var name in usernames)
                                {
                                    if (name != null && name.Length >= 2)
                                        namemodel = namemodel + name?.ToCharArray()[0] + name?.ToCharArray()[1];
                                    else if (name != null && name.Length == 1)
                                        namemodel = namemodel + name?.ToCharArray()[0];
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
                                if (model.IsIntrested != "Interest Accepted")
                                {
                                    var usernames = IRListModel.CandidateName.Trim().Split(" ");
                                    var namemodel = String.Empty;
                                    foreach (var name in usernames)
                                    {
                                        if (name != null && name.Length >= 2)
                                            namemodel = namemodel + name?.ToCharArray()[0] + name?.ToCharArray()[1];
                                        else if (name != null && name.Length == 1)
                                            namemodel = namemodel + name?.ToCharArray()[0];
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
                        if (profileName == "CuratedUser")
                        {
                            if (!String.IsNullOrEmpty(IRListModel.encodedresumeLink))
                            {
                                NewModel.resumeLink = IRListModel.encodedresumeLink;
                                IRListModel.resumeLink = IRListModel.encodedresumeLink;
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
                        NewModel.ClientHrContact = model.ClientHrContact;
                        // to get videocount details
                        var videodetailscount = await _meetingService.GetTotalViewCount(id);
                        NewModel.VideoCountDetail = videodetailscount.ToList();
                        NewModel.FeedbackAudioLink = model.FeedbackAudioLink;
                        NewModel.IsBiometricEnabled = model.IsBiometricEnabled;
                        NewModel.MeetingPatch = model.MeetingPatch;
                        if (model.MeetingPatch.Count() > 0)
                        {
                            NewModel.VideoLink = model.MeetingPatch.FirstOrDefault().VideoLink;
                        }
                        if (NewModel.IreportModel.QuestionsCollection.Where(x => x.VideoLink != null && x.VideoLink != "").Count() > 0)
                        {
                            NewModel.IreportModel.QuestionsCollection = NewModel.IreportModel.QuestionsCollection.Where(x => x.VideoLink != null && x.VideoLink != "").ToList();
                        }
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
                        NewModel.irfIntId = model.irfIntId;
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
                                    namemodel = namemodel + name?.ToCharArray()[0];
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
                                namemodel1 = namemodel1 + name?.ToCharArray()[0];
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
                            IRListModel.resumeLink = IRListModel.encodedresumeLink;
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
                        NewModel.ClientHrContact = model.ClientHrContact;
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
                        ViewBag.VideoStatus = "Public";
                        //ViewBag.VideoStatus = "Private";
                        var menus = _userService.GetMenuBasedOnUserEmailld("ExternalUser").GroupBy(p => p.functionName).Select(g => g.First()).ToList();
                        if (menus != null)
                        {
                            menus = menus.Where(x => x.functionName != "Support" && x.functionName != "Common Pool").ToList();
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
                                //not do anything
                            }
                            values = values + ";" + id;
                            HttpContext.Session.SetString("UserLogedInVedioId", values);
                        }
                    }
                    //End:Save View counts of vedio
                    bool isClientUser = false;
                    int ClientId = 0;
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");

                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "CuratedUser" || profileName == "ExternalUser")
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
                        MeetingVM model = await _meetingService.GetMeetingbyid(id, emailId);
                        string irfId = model.IRFId;
                        if (isClientUser)
                        {
                            //modelList = await _meetingService.GetRiseTubeMeetingsByJD(ClientId, emailId, model.JDTiltle.Split("|")[0], filter.StartDate.Value, filter.EndDate.Value);
                            modelList = await _meetingService.GetRiseTubeMeetingsByIRFId(ClientId, emailId, irfId, filter.StartDate.Value, filter.EndDate.Value);

                        }
                        else if (profileName == "CuratedUser")
                        {
                            //modelList = await _meetingService.GetRiseTubeMeetingsByJD(ClientId, emailId, model.JDTiltle.Split("|")[0], filter.StartDate.Value, filter.EndDate.Value, profileName);
                            modelList = await _meetingService.GetRiseTubeMeetingsByIRFId(ClientId, emailId, irfId, filter.StartDate.Value, filter.EndDate.Value, profileName);
                        }
                        else if (profileName == "ExternalUser")
                        {
                            modelList = await _meetingService.GetRiseTubeMeetings(0, profileName, emailId);

                        }
                        else
                        {
                            //modelList = await _meetingService.GetRiseTubeMeetingsByJD(0, emailId, model.JDTiltle.Split("|")[0], filter.StartDate.Value, filter.EndDate.Value);
                            modelList = await _meetingService.GetRiseTubeMeetingsByIRFId(0, emailId, irfId, filter.StartDate.Value, filter.EndDate.Value);
                        }

                        if (profileName == "ExternalUser")
                        {
                            modelList = modelList.Where(c => c.JDTiltle.Contains(model.JDTiltle.Split("|")[0])).OrderByDescending(x => x.interviewDate).ToList();

                        }
                        var IRListModel = _meetingService.GetInteviewReport(model.imid, model.irfIntId).Result;
                        if (HttpContext.Session.GetString("RiseTubefilters") != null)
                        {
                            var filter1 = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("RiseTubefilters"));
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
                        MeetingVM NewModel = new MeetingVM();
                        if (profileName == "CuratedUser")
                        {
                            foreach (var itemmodel in modelList)
                            {
                                if (itemmodel.IsIntrested != "Interest Accepted")
                                {
                                    var usernames = itemmodel.candidateName.Trim().Split(" ");
                                    var namemodel = String.Empty;
                                    foreach (var name in usernames)
                                    {
                                        if (name != null && name.Length >= 2)
                                            namemodel = namemodel + name?.ToCharArray()[0] + name?.ToCharArray()[1];
                                        else if (name != null && name.Length == 1)
                                            namemodel = namemodel + name?.ToCharArray()[0];
                                    }
                                    itemmodel.candidateName = namemodel;
                                }
                            }
                        }

                        NewModel.SubVideo_List = modelList;// modelList.ToList();
                        if (profileName == "CuratedUser")
                        {
                            if (model.IsIntrested != "Interest Accepted")
                            {
                                var usernames = model.candidateName.Trim().Split(" ");
                                var namemodel = String.Empty;
                                foreach (var name in usernames)
                                {
                                    if (name != null && name.Length >= 2)
                                        namemodel = namemodel + name?.ToCharArray()[0] + name?.ToCharArray()[1];
                                    else if (name != null && name.Length == 1)
                                        namemodel = namemodel + name?.ToCharArray()[0];
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
                            if (model.IsIntrested != "Interest Accepted")
                            {
                                var usernames = IRListModel.CandidateName.Trim().Split(" ");
                                var namemodel = String.Empty;
                                foreach (var name in usernames)
                                {
                                    if (name != null && name.Length >= 2)
                                        namemodel = namemodel + name?.ToCharArray()[0] + name?.ToCharArray()[1];
                                    else if (name != null && name.Length == 1)
                                        namemodel = namemodel + name?.ToCharArray()[0];

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
                        if (profileName == "CuratedUser")
                        {
                            if (!String.IsNullOrEmpty(IRListModel.encodedresumeLink))
                            {
                                NewModel.resumeLink = IRListModel.encodedresumeLink;
                                IRListModel.resumeLink = IRListModel.encodedresumeLink;
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
                        NewModel.ClientHrContact = model.ClientHrContact;
                        var videodetailscount = await _meetingService.GetTotalViewCount(id);
                        NewModel.VideoCountDetail = videodetailscount.ToList();
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
        public async Task<IActionResult> ResetVideoPageFilter(int imid)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("VideoPagefilters");
                    return RedirectToAction("VideoPage", new { id = imid });
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
            var modelList = _meetingService.VedioShare(value, videoid);
            string Name = string.Empty;
            if (name != null && name.Length >= 2)
                Name = Name + name?.ToCharArray()[0] + name?.ToCharArray()[1];
            else if (name != null && name.Length == 1)
                Name += name?.ToCharArray()[0];
            //send a email
            var email = HttpContext.Session.GetString("email");
            email = email.Replace("\"", "");
            var resultset = _meetingService.AddVedioShare(email, videoid, Emailids);
            string currenturl = Request.Host.ToString() + "/RiseTube/VideoPage/" + videoid;
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
            var modelList = _meetingService.VedioShare(value, videoid);
            return Json("Success");
        }
        [HttpPost]
        public JsonResult VideoComment(string comment, string videoid)
        {
            var email = HttpContext.Session.GetString("email");
            var modelList = _meetingService.VedioComment(comment, videoid, email);
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
                var result1 = _notificationHelper.CuretatedNotification(_mail, "Candidate", bulider.ToString());

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
                var result = _notificationHelper.CuretatedNotification(_mail, "Candidate", bulider.ToString());
            }
            return Json("Success");
        }
        [HttpPost]
        public JsonResult VideoShared(string Emailids, string videoid, string Name, string skills)
        {
            var email = HttpContext.Session.GetString("email");
            email = email.Replace("\"", "");
            var modelList = _meetingService.VedioShared(Emailids, videoid, email);
            string currenturl = Request.Host.ToString() + Request.Path.ToString() + "/" + videoid;
            var resultset = _meetingService.AddVedioShare(email, videoid, Emailids);
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
                var result = _notificationHelper.SharedVideoNofiticationExternal(_mail, Name, currenturl);
            }
            return Json("");
        }
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
                var result = _notificationHelper.SharedVideoNofiticationExternal(_mail, Name, currenturl, comment, resumeLink);
            }
            return Json("");
        }
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

        public async Task<IActionResult> VideoPageCurated(int curatedGroupId, string actionstatus, int id = 0)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {

                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    ViewBag.actionstatus = "Risetube";
                    ViewBag.ResultStatus = "VideoPresent";
                    profileName = profileName.Replace("\"", "");
                    ViewBag.ProfileName = profileName;
                    ViewBag.CuratedGroupId = curatedGroupId;
                    if (profileName == "CuratedUser" || profileName == "Client")
                    {
                        var isClient = false;
                        if (profileName == "Client")
                        {
                            isClient = true;
                        }
                        var emailId = HttpContext.Session.GetString("email")?.Trim();
                        emailId = emailId.Replace("\"", "");

                        if (TempData["SuccessErrorMessageInterview"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageInterview"].ToString();
                            TempData.Remove("SuccessErrorMessageInterview");
                        }

                        var modelList1 = new List<MeetingVM>();

                        var irfList = _meetingService.GetCuratedGroupIRFs(curatedGroupId).ToArray();
                        var dataResult = await _meetingService.GetRiseTubeMeetings(0, profileName, emailId, isClient);
                        modelList1 = dataResult.ToList().OrderByDescending(y => y.Score).ToList();

                        modelList1 = modelList1.Where(x => irfList.Contains(x.irfIntId)).ToList();
                        ViewBag.actionstatus = "SkillChannel";
                        if (actionstatus.ToUpper() == "ALL")
                        {
                            if (id == 0)
                            {
                                id = modelList1.OrderByDescending(x => x.imid).Select(x => x.imid).FirstOrDefault();
                            }
                        }
                        else if (actionstatus.ToUpper() == "SHORTLIST")
                        {
                            modelList1 = modelList1.Where(x => x.ShortListstatus == "Shortlisted").ToList();
                            if (id == 0)
                            {
                                id = modelList1.OrderByDescending(x => x.imid).Select(x => x.imid).FirstOrDefault();

                            }
                        }
                        else if (actionstatus.ToUpper() == "INTRESTED")
                        {
                            modelList1 = modelList1.Where(x => x.IsIntrested == "Interest Sent").ToList();
                            if (id == 0)
                            {
                                id = modelList1.OrderByDescending(x => x.imid).Select(x => x.imid).FirstOrDefault();
                            }
                        }
                        else if (actionstatus.ToUpper() == "ACCEPT")
                        {
                            modelList1 = modelList1.Where(x => x.IsIntrested == "Interest Accepted").ToList();
                            if (id == 0)
                            {
                                id = modelList1.OrderByDescending(x => x.imid).Select(x => x.imid).FirstOrDefault();

                            }
                        }

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
                        MeetingVM model = await _meetingService.GetMeetingbyid(id, emailId);
                        if (model == null)
                        {
                            model = new MeetingVM();
                            ViewBag.ResultStatus = "VideoNotPresent";
                            return View("VideoPage", model);

                        }

                        modelList1 = modelList1.Where(c => c.JDTiltle.Contains(model.JDTiltle.Split("|")[0])).OrderByDescending(x => x.interviewDate).ToList();

                        modelList1 = modelList1.Select(x =>
                        {
                            x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty;
                            return x;
                        }).ToList();
                        var IRListModel = _meetingService.GetInteviewReport(model.imid, model.irfIntId).Result;
                        var modelList = modelList1;
                        if (HttpContext.Session.GetString("RiseTubefilters") != null)
                        {
                            var filter1 = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("RiseTubefilters"));
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
                        && (filter.CandidateAvailable.Count() > 0 ? filter.CandidateAvailable.Contains(c.CandidateAvailable) : c.CandidateAvailable == string.Empty)).ToList();
                        }

                        foreach (var itemmodel in modelList)
                        {
                            if (itemmodel.IsIntrested != "Interest Accepted")
                            {
                                var usernames = itemmodel.candidateName.Trim().Split(" ");
                                var namemodel = String.Empty;
                                foreach (var name in usernames)
                                {
                                    if (name != null && name.Length >= 2)
                                        namemodel = namemodel + name?.ToCharArray()[0] + name?.ToCharArray()[1];
                                    else if (name != null && name.Length == 1)
                                        namemodel += name?.ToCharArray()[0];

                                }
                                itemmodel.candidateName = namemodel;
                            }
                        }

                        MeetingVM NewModel = new MeetingVM();

                        if (HttpContext.Session.GetString("VideoPagefilters") != null || HttpContext.Session.GetString("RiseTubefilters") != null)
                        {
                            NewModel.SubVideo_List = modelList.ToList().Where(x => x.imid != id).ToList();
                        }
                        else
                        {
                            NewModel.SubVideo_List = modelList1.ToList().Where(x => x.imid != id).ToList();
                        }
                        if (model.IsIntrested != "Interest Accepted")
                        {
                            var usernames = model.candidateName.Trim().Split(" ");
                            var namemodel = String.Empty;
                            foreach (var name in usernames)
                            {
                                if (name != null && name.Length >= 2)
                                    namemodel = namemodel + name?.ToCharArray()[0] + name?.ToCharArray()[1];
                                else if (name != null && name.Length == 1)
                                    namemodel = namemodel + name?.ToCharArray()[0];
                            }
                            NewModel.candidateName = namemodel;
                        }
                        else
                        {
                            NewModel.candidateName = model.candidateName;
                        }

                        if (IRListModel != null)
                        {
                            if (model.IsIntrested != "Interest Accepted")
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
                        if (profileName == "CuratedUser")
                        {
                            if (!String.IsNullOrEmpty(IRListModel.encodedresumeLink))
                            {
                                NewModel.resumeLink = IRListModel.encodedresumeLink;
                                IRListModel.resumeLink = IRListModel.encodedresumeLink;
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
                        NewModel.IsBiometricEnabled = model.IsBiometricEnabled;
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
                        ViewBag.VideoStatus = "Private";

                        if (profileName == "Client")
                        {
                            NewModel.IsClientCurated = true;
                        }
                        else
                        {
                            NewModel.IsClientCurated = false;
                        }
                        NewModel.ActionStatus = actionstatus;
                        NewModel.CuratedGroupId = curatedGroupId;
                        return View("VideoPage", NewModel);
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
        [AllowAnonymous]
        public async Task<IActionResult> Sharecreatedview(MeetingVM _newobject)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                {
                    if (_newobject.IreportModel.VideoFile != null)
                    {
                        await _azureStorageHelper.InsertBlob(_newobject.IreportModel.VideoFile.FileName, "videos", _newobject.IreportModel.VideoFile);
                        var containerRef = _azureStorageHelper.GetCloudBlobContainer("videos");
                        var blob = containerRef.GetBlockBlobReference(_newobject.IreportModel.VideoFile.FileName);
                        string uploadedvideoFileURL = blob.Uri.ToString();
                        _newobject.IreportModel.VideoLink = uploadedvideoFileURL;
                    }
                    var result = await _meetingService.AddSharedVideoDetails(_newobject.IreportModel);
                    var Emailids = _newobject.IreportModel.SharedEmailAddress;
                    //Send A mail with video url
                    var email = HttpContext.Session.GetString("email");
                    email = email.Replace("\"", "");
                    string currenturl = "https://" + Request.Host + Request.Path;
                    currenturl = currenturl.Replace("Sharecreatedview", "SharedVideoPage") + "/" + result;
                    if (Emailids != "" && Emailids != ";")
                    {
                        Mail _mail = new Mail
                        {
                            TO = Emailids,
                            CC = email,
                            Subject = "An interview of Candidate: " + _newobject.IreportModel.CandidateName + " video is shared with you"
                        };
                        var resultset = _notificationHelper.SharedVideoNofiticationExternal(_mail, _newobject.IreportModel.CandidateName, currenturl, "", "");
                    }
                    //return View("VideoPage");
                    return RedirectToAction("VideoPage", new { id = _newobject.IreportModel.InterviewId });
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
        [AllowAnonymous]
        public async Task<IActionResult> SharedVideoPage(int id, string irfid, string actionstatus)
        {
            try
            {
                //Start: Share button functionaly
                ViewBag.ResultStatus = "VideoPresent";
                //Get Video Details From SharedInterviewComments
                var interviewid = _meetingService.SharedGetInterviewId(id).Result;

                MeetingVM model = await _meetingService.GetMeetingbyid(interviewid, "");
                var status = String.IsNullOrEmpty(model.IsShared) ? "" : model.IsShared;

                //update with help of new tables
                var IRListModel = _meetingService.GetSharedInteviewReport(model.imid, model.irfIntId, id).Result;
                if (IRListModel == null)
                {
                    IRListModel = new InterviewReport();
                }
                else
                {
                    IRListModel.CandidateName = IRListModel.CandidateName.Trim();
                }
                MeetingVM NewModel = new MeetingVM
                {
                    SubVideo_List = new List<MeetingVM>()
                };

                NewModel.candidateName = IRListModel.CandidateName.Trim();

                ViewBag.ProfileName = "";
                NewModel.CandidateId = model.CandidateId;
                NewModel.interviewDate = model.interviewDate;
                NewModel.imid = model.imid;
                NewModel.IRFId = model.IRFId;
                NewModel.ClientName = model.ClientName;

                NewModel.InterviewerComment = IRListModel.InterviewerComment;
                NewModel.InterviewStatus = model.InterviewStatus;
                NewModel.JDTiltle = model.JDTiltle;
                NewModel.JDLink = model.JDLink;
                NewModel.MeetingStatus = model.MeetingStatus;
                NewModel.confirmationStatus = model.confirmationStatus;
                NewModel.VideoLink = model.VideoLink;
                NewModel.candidateEmail = IRListModel.CandidateEmail;
                NewModel.reportLink = model.reportLink;
                NewModel.resumeLink = model.resumeLink;
                if (!String.IsNullOrEmpty(IRListModel.encodedresumeLink))
                {
                    NewModel.resumeLink = IRListModel.encodedresumeLink;
                    IRListModel.resumeLink = IRListModel.encodedresumeLink;
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
                NewModel.FeedbackAudioLink = model.FeedbackAudioLink;
                NewModel.LogoUrl = IRListModel.LogoUrl;
                NewModel.ClientHrContact = model.ClientHrContact;
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
                ViewBag.VideoStatus = "Public";
                //ViewBag.VideoStatus = "Private";
                var menus = _userService.GetMenuBasedOnUserEmailld("ExternalUser").GroupBy(p => p.functionName).Select(g => g.First()).ToList();
                if (menus != null)
                {
                    menus = menus.Where(x => x.functionName != "Support" && x.functionName != "Common Pool").ToList();
                    HttpContext.Session.SetString("userMenus", JsonConvert.SerializeObject(menus));
                }
                if (IRListModel != null)
                {
                    GetStatusList(IRListModel, model.irfIntId);
                }
                //desible Like and dilisk button and share button and comment opetion
                return View("VideoPage", NewModel);
                //END: Share button functionaly
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<IActionResult> VideoDetailView(int? Page)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                {
                    //To get data from db for video displayes
                    var model = _meetingService.GetallVideoSumaryDetails();
                    foreach (var item in model)
                    {
                        item.VideoUrl = "https://" + Request.Host + "/RiseTube/" + item.VideoUrl + "/" + item.Videoid.ToString();
                    }
                    return View(model.ToPagedList(Page ?? 1, 12));
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
        public JsonResult CalculateTimeSpend(string videoid, string Emailid, string CurrentUrl, string timeStart, string timeend, string CurrentTime)
        {
            //Start:SaveTime Spend on vedio page
            if (String.IsNullOrEmpty(Emailid))
            {
                if (HttpContext.Session.GetString("email") != null)
                {
                    Emailid = HttpContext.Session.GetString("email")?.Trim();
                    Emailid = Emailid.Replace("\"", "");
                }
            }
            else
            {
                Emailid = Emailid.Replace("\"", "");
            }
            var TimeSpend = Convert.ToDateTime(timeend) - Convert.ToDateTime(timeStart);
            var time = TimeSpan.FromSeconds(Convert.ToDouble(CurrentTime));
            CurrentTime = time.ToString("hh") + ":" + time.ToString("mm") + ":" + time.ToString("ss");
            if (CurrentUrl.Contains("SharedVideoPage"))
            {
                videoid = CurrentUrl.Split("/")[CurrentUrl.Split("/").Count() - 1].ToString();
                _meetingService.AddTimeSpend(Convert.ToInt32(videoid), Emailid, "Shared", TimeSpend, CurrentTime);

            }
            else
            {
                _meetingService.AddTimeSpend(Convert.ToInt32(videoid), Emailid, "Public", TimeSpend, CurrentTime);
            }
            //End:SaveTime Spend on vedio page
            return Json("Succecss");
        }
        public JsonResult VideoDetailsJsonView(int interviewid)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "CuratedUser")
                {
                    //To get data from db for video displayes
                    var model = _meetingService.GetVideoSumaryDetails(interviewid);
                    foreach (var item in model)
                    {
                        item.VideoUrl = "https://" + Request.Host + "/RiseTube/" + item.VideoUrl + "/" + item.Videoid.ToString();
                        item.interviewid = interviewid;
                        if (profileName == "CuratedUser")
                        {
                            item.Candidate = item.Candidate?.ToCharArray()[0] + "XXXXXX" + item.Candidate.Split("@")[1];
                        }
                    }
                    return Json(model);
                }
                else if (profileName == "Candidate")
                {
                    var email = HttpContext.Session.GetString("email");
                    email = email.Replace("\"", "");
                    //To get data from db for video displayes
                    //only candidate video share data//InterviewShareDetails
                    var model = _meetingService.GetVideoCandidateSumaryDetails(interviewid, email);

                    return Json(model);
                }
                else
                {
                    return Json("No Data");
                }
            }
            else
            {
                return Json("No Data");
            }
        }
        [HttpPost]
        public JsonResult VideoShareDetails(string value, string videoid, string name, string Emailids, string currentUrl)
        {
            //var modelList = _meetingService.VedioShare(value, vedioid);
            string Name = string.Empty;
            if (name != null && name.Length >= 2)
                Name = Name + name?.ToCharArray()[0] + name?.ToCharArray()[1];
            else if (name != null && name.Length == 1)
                Name += name?.ToCharArray()[0];
            //send a email
            var email = HttpContext.Session.GetString("email");
            email = email.Replace("\"", "");
            Mail _mail = new Mail
            {
                TO = Emailids,
                CC = email,
                Subject = "Risebird: Interview video is shared with you"
            };
            var result = _notificationHelper.SharedVideoNofiticationExternal(_mail, Name, currentUrl);
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
        public JsonResult VideoShareIntrest(string currentUrl, string Emailid, string CandidateEmail, string clienthr)
        {
            if (HttpContext.Session.GetString("email") != null)
            {
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                string videoid = currentUrl.Split("/")[currentUrl.Split("/").Count() - 1].ToString();
                string urlType = "";
                if (currentUrl.Contains("SharedVideoPage"))
                {
                    urlType = "Shared";
                }
                else
                {
                    urlType = "RiseTube";
                }
                //save into db
                var model = _meetingService.AddVideoIntrestDetails(videoid, urlType, emailid);
            }
            else
            {
                string videoid = currentUrl.Split("/")[currentUrl.Split("/").Count() - 1].ToString();
                string urlType = "";
                if (currentUrl.Contains("SharedVideoPage"))
                {
                    urlType = "Shared";
                }
                else
                {
                    urlType = "RiseTube";
                }
                //save into db
                var model = _meetingService.AddVideoIntrestDetails(videoid, urlType, Emailid);
            }
            //Send Email
            if (currentUrl.Contains("CandidateVideoPage"))
            {
                Mail _mail = new Mail
                {
                    TO = CandidateEmail,
                    CC = "Talent@risebird.io",
                    Subject = $"Risebird:{Emailid} shown interest in your Risebird interview video"
                };
                var result = _notificationHelper.ShowIntrestInterview(_mail, Emailid, currentUrl);
            }
            else
            {
                Mail _mail = new Mail
                {
                    TO = clienthr,//Client HR
                    CC = "Talent@risebird.io",
                    Subject = $"Risebird:{Emailid} shown interest in {CandidateEmail} candidate interview video"
                };
                var result = _notificationHelper.ShowIntrestInterview(_mail, Emailid, currentUrl);
            }
            return Json("Success");
        }
        public IActionResult VideoIntrestDetails()
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin")
                {
                    var model = _meetingService.GetVideoIntrestDetails();
                    foreach (var item in model)
                    {
                        item.VideoUrl = "https://" + Request.Host + "/RiseTube/" + item.VideoUrl + "/" + item.Videoid.ToString();
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

        public async Task<SentimentPercentage> GetSentimentAnalysis(int imid)
        {
            try
            {
                var data = await _meetingService.GetSentimentAnalysis(imid);

                return data;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }


        [AllowAnonymous]
        public async Task<IActionResult> SharedIndex(int? Page, string searchvalue)
        {
            try
            {
                var profileName = "CuratedUser";

                {

                    var modelList = new List<MeetingVM>();

                    if (Page == null || Page == 0)
                    {
                        Page = 1;
                    }
                    modelList = await _meetingService.GetSharedRiseTubeMeetingsPaged(Page.Value, searchvalue);

                    foreach (var item in modelList)
                    {

                        var usernames = item.candidateName.Trim().Split(" ");
                        var namemodel = String.Empty;
                        foreach (var name in usernames)
                        {
                            if (name != null && name.Length >= 2)
                                namemodel = namemodel + name?.ToCharArray()[0] + name?.ToCharArray()[1];
                            else if (name != null && name.Length == 1)
                                namemodel = namemodel + name?.ToCharArray()[0];

                        }
                        item.candidateName = namemodel;

                    }


                    ViewBag.ProfileName = profileName;
                    if (!String.IsNullOrEmpty(searchvalue))
                    {
                        ViewBag.searchvalue = searchvalue;
                    }
                    else
                    {
                        ViewBag.searchvalue = "";
                    }

                    var menus = _userService.GetMenuBasedOnUserEmailld("ExternalUser").GroupBy(p => p.functionName).Select(g => g.First()).ToList();
                    if (menus != null)
                    {
                        menus = menus.Where(x => x.functionName != "Support" && x.functionName != "Common Pool").ToList();
                        HttpContext.Session.SetString("userMenus", JsonConvert.SerializeObject(menus));
                    }

                    var totalMeetings = await _meetingService.GetSharedIndexMeetingsCount(searchvalue);

                    var remainder = totalMeetings % 12;
                    if (remainder == 0)
                    {
                        ViewBag.Pages = totalMeetings / 12;

                    }
                    else
                    {
                        ViewBag.Pages = (totalMeetings / 12) + 1;

                    }

                    ViewBag.Page = Page.Value;
                    ViewBag.MeetingsCount = totalMeetings;
                    ViewBag.Remainder = remainder;
                    return View(modelList);
                }

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }

        }

        [Authorize]
        public async Task<IActionResult> RBOnDemandTalent(int? Page, string searchvalue)
        {
            try
            {

                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    if (profileName != null)
                    {
                        profileName = profileName.Replace("\"", "");
                        if (profileName == "Client")
                        {
                            var modelList = new List<MeetingVM>();

                            if (Page == null || Page == 0)
                            {
                                Page = 1;
                            }

                            int totalMeetings = 0;
                            var heuristicLevels =
                                (from Heuristicsscorelevel s in Enum.GetValues(typeof(Heuristicsscorelevel))
                                 select (s.GetDescription())).ToArray();

                            ViewBag.HeuristicLevels = heuristicLevels;

                            var primarySkills = await _iRFService.GetPrimarySkillFromIRF();
                            ViewBag.PrimarySkillList = primarySkills;
                            if (HttpContext.Session.GetString("TalentOnDemandFilter") != null)
                            {
                                var filter =
                                    JsonConvert.DeserializeObject<TalentOnDemandFilter>(
                                        HttpContext.Session.GetString("TalentOnDemandFilter"));

                                filter.PrimarySkill = filter.PrimarySkill == null
                                    ? primarySkills.ToArray()
                                    : filter.PrimarySkill;
                                filter.HeuristicType =
                                    filter.HeuristicType == null ? heuristicLevels : filter.HeuristicType;

                                modelList = await _meetingService.GetSharedRiseTubeMeetingsForClientPagedFiltered(
                                    Page.Value, string.Join(",", filter.PrimarySkill),
                                    string.Join(",", filter.HeuristicType), searchvalue);

                                totalMeetings = await _meetingService.GetSharedIndexMeetingsCountforClientFiltered(
                                    string.Join(",", filter.PrimarySkill), string.Join(",", filter.HeuristicType),
                                    searchvalue);
                            }
                            else
                            {
                                modelList = await _meetingService.GetSharedRiseTubeMeetingsPaged(Page.Value,
                                    searchvalue, profileName);
                                totalMeetings =
                                    await _meetingService.GetSharedIndexMeetingsCount(searchvalue, profileName);
                            }

                            foreach (var item in modelList)
                            {
                                var usernames = item.candidateName.Trim().Split(" ");
                                var namemodel = String.Empty;
                                foreach (var name in usernames)
                                {
                                    if (name != null && name.Length >= 2)
                                        namemodel = namemodel + name?.ToCharArray()[0] + name?.ToCharArray()[1];
                                    else if (name != null && name.Length == 1)
                                        namemodel = namemodel + name?.ToCharArray()[0];
                                }

                                item.candidateName = namemodel;
                            }


                            ViewBag.ProfileName = profileName;
                            if (!String.IsNullOrEmpty(searchvalue))
                            {
                                ViewBag.searchvalue = searchvalue;
                            }
                            else
                            {
                                ViewBag.searchvalue = "";
                            }

                            var remainder = totalMeetings % 12;
                            if (remainder == 0)
                            {
                                ViewBag.Pages = totalMeetings / 12;
                            }
                            else
                            {
                                ViewBag.Pages = (totalMeetings / 12) + 1;
                            }

                            ViewBag.Page = Page.Value;
                            ViewBag.MeetingsCount = totalMeetings;
                            ViewBag.Remainder = remainder;
                            return View(modelList);
                        }
                        else
                        {
                            return RedirectToAction("SignOut", "Session");
                        }
                    }
                }


                return RedirectToAction("SignOut", "Session");



            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }

        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> RBOnDemandTalent(int? Page, TalentOnDemandFilter filter, string searchvalue = null)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {

                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Client")
                    {


                        if (filter != null)
                        {
                            HttpContext.Session.SetString("TalentOnDemandFilter", JsonConvert.SerializeObject(filter));
                        }
                        var modelList = new List<MeetingVM>();
                        if (Page == null || Page == 0)
                        {
                            Page = 1;
                        }
                        int totalMeetings = 0;
                        var heuristicLevels = (from Heuristicsscorelevel s in Enum.GetValues(typeof(Heuristicsscorelevel))
                                               select (s.GetDescription())).ToArray();

                        ViewBag.HeuristicLevels = heuristicLevels;

                        var primarySkills = await _iRFService.GetPrimarySkillFromIRF();
                        ViewBag.PrimarySkillList = primarySkills;



                        filter.PrimarySkill = filter.PrimarySkill == null ? primarySkills.ToArray() : filter.PrimarySkill;
                        filter.HeuristicType = filter.HeuristicType == null ? heuristicLevels : filter.HeuristicType;

                        modelList = await _meetingService.GetSharedRiseTubeMeetingsForClientPagedFiltered(Page.Value, string.Join(",", filter.PrimarySkill), string.Join(",", filter.HeuristicType), searchvalue);

                        totalMeetings = await _meetingService.GetSharedIndexMeetingsCountforClientFiltered(string.Join(",", filter.PrimarySkill), string.Join(",", filter.HeuristicType), searchvalue);
                        foreach (var item in modelList)
                        {

                            var usernames = item.candidateName.Trim().Split(" ");
                            var namemodel = String.Empty;
                            foreach (var name in usernames)
                            {
                                if (name != null && name.Length >= 2)
                                    namemodel = namemodel + name?.ToCharArray()[0] + name?.ToCharArray()[1];
                                else if (name != null && name.Length == 1)
                                    namemodel = namemodel + name?.ToCharArray()[0];

                            }
                            item.candidateName = namemodel;

                        }


                        ViewBag.ProfileName = profileName;
                        if (!String.IsNullOrEmpty(searchvalue))
                        {
                            ViewBag.searchvalue = searchvalue;
                        }
                        else
                        {
                            ViewBag.searchvalue = "";
                        }


                        var remainder = totalMeetings % 12;
                        if (remainder == 0)
                        {
                            ViewBag.Pages = totalMeetings / 12;

                        }
                        else
                        {
                            ViewBag.Pages = (totalMeetings / 12) + 1;

                        }

                        ViewBag.Page = Page.Value;
                        ViewBag.MeetingsCount = totalMeetings;
                        ViewBag.Remainder = remainder;
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
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }

        }

        [Authorize]
        public async Task<IActionResult> ResetRisetubeOnDemandFilter()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("TalentOnDemandFilter");
                    return RedirectToAction("RBOnDemandTalent");
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

        [AllowAnonymous]
        public async Task<IActionResult> ResetSharedRisetubeFilter()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("RiseTubefilters");
                    return RedirectToAction("SharedIndex");
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
        [AllowAnonymous]
        public async Task<IActionResult> VideoPageDetails(int id, string irfid, string actionstatus)
        {
            try
            {
                //Start: Share button functionaly
                ViewBag.ResultStatus = "VideoPresent";
                MeetingVM model = await _meetingService.GetMeetingbyid(id, "");
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
                            namemodel = namemodel + name?.ToCharArray()[0];
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
                        namemodel1 = namemodel1 + name?.ToCharArray()[0];
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
                    IRListModel.resumeLink = IRListModel.encodedresumeLink;
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
                NewModel.ClientHrContact = model.ClientHrContact;
                NewModel.FeedbackAudioLink = model.FeedbackAudioLink;
                var videodetailscount = await _meetingService.GetTotalViewCount(id);
                NewModel.MeetingPatch = model.MeetingPatch;
                if (model.MeetingPatch.Count() > 0)
                {
                    NewModel.VideoLink = model.MeetingPatch.FirstOrDefault().VideoLink;
                }
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
                ViewBag.VideoStatus = "Public";
                //ViewBag.VideoStatus = "Private";

                var menus = _userService.GetMenuBasedOnUserEmailld("ExternalUser").GroupBy(p => p.functionName).Select(g => g.First()).ToList();
                if (menus != null)
                {
                    menus = menus.Where(x => x.functionName != "Support" && x.functionName != "Common Pool").ToList();
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
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }

        }

        public async Task<IActionResult> VideoPageCandidateJd(int ClientJdId, string actionstatus, int id = 0)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    ViewBag.actionstatus = "Risetube";
                    ViewBag.ResultStatus = "VideoPresent";
                    profileName = profileName.Replace("\"", "");
                    ViewBag.ProfileName = profileName;
                    ViewBag.CuratedGroupId = ClientJdId;
                    if (profileName == "Admin" || profileName == "Client")
                    {
                        var isClient = false;
                        if (profileName == "Client")
                        {
                            isClient = true;
                        }
                        var emailId = HttpContext.Session.GetString("email")?.Trim();
                        emailId = emailId.Replace("\"", "");

                        if (TempData["SuccessErrorMessageInterview"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageInterview"].ToString();
                            TempData.Remove("SuccessErrorMessageInterview");
                        }

                        var modelList1 = new List<MeetingVM>();

                        var dataResult = _meetingService.GetClientJdIRFs(ClientJdId, emailId);
                        modelList1 = dataResult.ToList().OrderBy(y => y.Priority).ToList();
                        ViewBag.pagestatus = "ALL";
                        if (id == 0)
                        {
                            if (actionstatus.ToUpper() == "ALL")
                            {
                                id = modelList1.OrderBy(x => x.Priority).Select(x => x.imid).FirstOrDefault();
                                ViewBag.pagestatus = "ALL";
                            }
                            else if (actionstatus.ToUpper() == "SHORTLIST")
                            {
                                modelList1 = modelList1.Where(x => x.ShortListstatus == "Shortlisted").ToList();
                                id = modelList1.OrderBy(x => x.Priority).Select(x => x.imid).FirstOrDefault();
                                ViewBag.pagestatus = "SHORTLIST";
                            }
                            else if (actionstatus.ToUpper() == "INTRESTED")
                            {
                                modelList1 = modelList1.Where(x => x.IsIntrested == "Interest Sent").ToList();
                                id = modelList1.OrderBy(x => x.Priority).Select(x => x.imid).FirstOrDefault();
                                ViewBag.pagestatus = "INTRESTED";
                            }
                            else if (actionstatus.ToUpper() == "ACCEPT")
                            {
                                modelList1 = modelList1.Where(x => x.IsIntrested == "Interest Accepted").ToList();
                                id = modelList1.OrderBy(x => x.Priority).Select(x => x.imid).FirstOrDefault();
                                ViewBag.pagestatus = "ACCEPT";
                            }
                        }
                        //modelList1 = modelList1.Where(x => irfList.Contains(x.IRFId)).ToList();
                        ViewBag.actionstatus = "SkillChannelDetails";
                        //fetch data on based on irfid and Emailid 
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
                        MeetingVM model = await _meetingService.GetMeetingbyid(id, emailId);
                        if (model == null)
                        {
                            model = new MeetingVM();
                            ViewBag.ResultStatus = "VideoNotPresent";
                            return View(model);

                        }

                        // modelList1 = modelList1.Where(c => c.JDTiltle.Contains(model.JDTiltle.Split("|")[0])).OrderByDescending(x => x.interviewDate).ToList();

                        modelList1 = modelList1.Select(x =>
                        {
                            x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty;
                            return x;
                        }).ToList();
                        var IRListModel = _meetingService.GetInteviewReport(model.imid, model.irfIntId).Result;
                        var modelList = modelList1;
                        if (HttpContext.Session.GetString("RiseTubefilters") != null)
                        {
                            var filter1 = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("RiseTubefilters"));
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
                        && (filter.CandidateAvailable.Count() > 0 ? filter.CandidateAvailable.Contains(c.CandidateAvailable) : c.CandidateAvailable == string.Empty)).ToList();
                        }

                        foreach (var itemmodel in modelList)
                        {
                            if (itemmodel.IsIntrested != "Interest Accepted" || itemmodel.Pending == true)
                            {
                                var usernames = itemmodel.candidateName.Trim().Split(" ");
                                var namemodel = String.Empty;
                                foreach (var name in usernames)
                                {
                                    if (name != null && name.Length >= 2)
                                        namemodel = namemodel + name?.ToCharArray()[0] + name?.ToCharArray()[1];
                                    else if (name != null && name.Length == 1)
                                        namemodel += name?.ToCharArray()[0];

                                }
                                itemmodel.candidateName = namemodel;
                            }
                        }

                        MeetingVM NewModel = new MeetingVM();

                        if (HttpContext.Session.GetString("VideoPagefilters") != null || HttpContext.Session.GetString("RiseTubefilters") != null)
                        {
                            NewModel.SubVideo_List = modelList.ToList().Where(x => x.imid != id).ToList();
                        }
                        else
                        {
                            NewModel.SubVideo_List = modelList1.ToList().Where(x => x.imid != id).ToList();
                        }
                        NewModel.candidateName = model.candidateName;
                        if (model.IsIntrested != "Interest Accepted" || model.Pending == true)
                        {
                            var usernames1 = model.candidateName.Trim().Split(" ");
                            var namemodel1 = String.Empty;
                            foreach (var name in usernames1)
                            {
                                if (name != null && name.Length >= 2)
                                    namemodel1 = namemodel1 + name?.ToCharArray()[0] + name?.ToCharArray()[1];
                                else if (name != null && name.Length == 1)
                                    namemodel1 = namemodel1 + name?.ToCharArray()[0];
                            }
                            NewModel.candidateName = namemodel1;
                        }


                        if (IRListModel != null)
                        {
                            if (model.IsIntrested != "Interest Accepted" || model.Pending == true)
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
                        if (model.IsIntrested != "Interest Accepted" || model.Pending == true)
                        {
                            if (!String.IsNullOrEmpty(IRListModel.encodedresumeLink))
                            {
                                NewModel.resumeLink = IRListModel.encodedresumeLink;
                                IRListModel.resumeLink = IRListModel.encodedresumeLink;
                            }
                        }
                        NewModel.encodedresumeLink = model.encodedresumeLink;
                        NewModel.zoomLink = model.zoomLink;
                        NewModel.IreportModel = IRListModel ?? new InterviewReport();
                        NewModel.TotalExperience = IRListModel.TotalExperience;
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
                        NewModel.IsBiometricEnabled = model.IsBiometricEnabled;
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
                        ViewBag.VideoStatus = "Private";

                        if (profileName == "Client")
                        {
                            NewModel.IsClientCurated = true;
                        }
                        else
                        {
                            NewModel.IsClientCurated = false;
                        }
                        NewModel.ActionStatus = actionstatus;
                        NewModel.CuratedGroupId = ClientJdId;
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
        [HttpPost]
        public JsonResult CandidateSuitableJd(string videoid, string Comment, int Clientjdid)
        {
            //store data in database
            int id = Convert.ToInt32(videoid);
            var email = HttpContext.Session.GetString("email");
            var Status = _meetingService.CandidateSuitableJd(id, email, Comment, Clientjdid);
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
                var result = _notificationHelper.CuretatedNotification(_mail, "Candidate", bulider.ToString());
            }
            return Json("Success");
        }
        public JsonResult VideoDetailsJsonViewJd(int interviewid)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                {
                    //To get data from db for video displayes
                    var model = _meetingService.GetVideoSumaryDetails(interviewid);
                    foreach (var item in model)
                    {
                        item.VideoUrl = "https://" + Request.Host + "/RiseTube/" + item.VideoUrl + "/" + item.Videoid.ToString();
                        item.interviewid = interviewid;
                        if (item.Candidate.Split("@").Length > 1)
                        {
                            item.Candidate = item.Candidate?.ToCharArray()[0] + "XXXXXX@" + item.Candidate.Split("@")[item.Candidate.Split("@").Length - 1];
                        }
                        else
                        {
                            item.Candidate = item.Candidate?.ToCharArray()[0] + "XXXXXX";
                        }
                    }
                    return Json(model);
                }
                else
                {
                    return Json("No Data");
                }
            }
            else
            {
                return Json("No Data");
            }
        }

        public async Task<ActionResult> GetMeetingAudioText(int imid)
        {
            try
            {
                var model = new MeetingTranscriptModel();
                int count = 0;
                model.ConvertedText = string.Empty;
                var tableEntries = _azureStorageHelper.GetAllEntities<SpeechToTextServiceModel>("SpeechToTextTbl");
                var requiredEntry = new SpeechToTextServiceModel();
                if (tableEntries != null && tableEntries.Count() != 0)
                {
                    requiredEntry = tableEntries.Where(x => x.PartitionKey == imid.ToString()).FirstOrDefault();
                }
                if (requiredEntry != null)
                {
                    string convertedText = string.Empty;
                    if (!string.IsNullOrEmpty(requiredEntry.KeyWords))
                    {
                        var keywords = requiredEntry.KeyWords.Split(",");
                        foreach (var keyword in keywords)
                        {
                            var word = keyword.ToLower().Trim();
                            convertedText = requiredEntry.ConvertedText.ToLower();
                            if (convertedText.Contains(word))
                            {
                                foreach (Match m in Regex.Matches(convertedText, word))
                                {
                                    count = count + 1;
                                }

                                //word = "<mark>" + word + "</mark>";
                                word = " <span style=\"background-color:Red;\">" + word + "</span>";
                                word = " <span style=\"color:#fff;\">" + word + "</span>";
                                word = " <span style=\"padding:0px 5px 0px 5px;\">" + word + "</span>";
                                requiredEntry.ConvertedText = convertedText.Replace(keyword, word);
                            }

                        }
                    }

                    model.ConvertedText = requiredEntry.ConvertedText;
                    model.Total = count;
                }
                return Json(model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        [HttpPost]
        public async Task<JsonResult> FetchRaitingDetails(int imid)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                var model = await _meetingService.FetchRatingDetails(imid, emailid);
                if (model == null)
                {
                    model = new InterviewerRating();
                }
                return Json(model);
            }
            else
            {
                var model = new InterviewerRating();
                return Json(model);
            }
        }
        [HttpPost]
        public async Task<JsonResult> SaveRaitingDetails(int imid, int rating, string comment)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                var model = new InterviewerRating();
                model.StarRating = rating;
                model.Comment = comment;
                model.Emailid = emailid;
                model.Interviewid = imid;
                var result = await _meetingService.SaveRatingDetails(model);
                //Send Mail and WhatsApp
                return Json(result);
            }
            else
            {
                return Json("Fail to save the rating.");
            }
        }
    }
}