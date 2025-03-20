using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Azure;
using Newtonsoft.Json;
using RestSharp;
using Risebird.Business;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Data.RbAPIDataModels;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System.Reflection;
using static SkiaSharp.HarfBuzz.SKShaper;
using static System.Reflection.Metadata.BlobBuilder;

namespace Risebird.Services.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CandidatesController : ControllerBase
    {
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IClientService _clientService;
        private readonly IIRFService _irfService;
        private readonly IIRFRepository _irfRepository;
        private readonly IInterviewSchedulerService _interviewSchedulerservice;
        private readonly IZoomHelper _zoomHelper;
        private readonly IConfiguration _configuration;
        private readonly IRisebirdMeetingsRepository _risebirdMeetings;
        private readonly INotificationHelper _notificationHelper;
        private readonly ICandidateService _candidateService;

        public CandidatesController(IApplicationErrorLogger applicationErrorLogger, IClientService clientService, IIRFService irfService, IIRFRepository irfRepository, IInterviewSchedulerService interviewSchedulerservice, IZoomHelper zoomHelper, IConfiguration configuration, IRisebirdMeetingsRepository risebirdMeetings, INotificationHelper notificationHelper, ICandidateService candidateService)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _clientService = clientService;
            _irfService = irfService;
            _irfRepository = irfRepository;
            _interviewSchedulerservice = interviewSchedulerservice;
            _zoomHelper = zoomHelper;
            _configuration = configuration;
            _risebirdMeetings = risebirdMeetings;
            _notificationHelper = notificationHelper;
            _candidateService = candidateService;
        }
        [HttpPost]
        [Route("Add")]
        public async Task<IActionResult> Add(AddCandidatesRequest reqData)
        {
            try
            {
                var functionkey = Request.Headers["x-functions-key"].ToString();
                var tenantId = Request.Headers["tenant-id"].ToString();
                if (functionkey != "sdDIDvwHbtfmq2RtPTNhm5s9bKItCQE4K0FxSnoYuLr-AzFuLCbDTw==")
                {
                    return new UnauthorizedResult();
                }
                if (!string.IsNullOrEmpty(tenantId))
                {
                    //check if the tenant Id belongs to the client
                    bool isAuthorizedTenant = false;
                    var clientDetails = await _clientService.GetClientByTalentId(tenantId);
                    if (clientDetails != null)
                    {
                        if (clientDetails.id > 0)
                        {
                            isAuthorizedTenant = true;
                        }
                    }
                    if (isAuthorizedTenant)
                    {
                        var details = new Dictionary<string, string>();
                        if (reqData != null)
                        {
                            if (string.IsNullOrEmpty(reqData.jobId) || string.IsNullOrEmpty(reqData.requestedBy))
                            {
                                var detailsError = new Dictionary<string, string>();

                                if (string.IsNullOrEmpty(reqData.jobId))
                                {
                                    detailsError.Add("jobId", "This field is required.");
                                }
                                if (string.IsNullOrEmpty(reqData.requestedBy))
                                {
                                    detailsError.Add("requestedBy", "This field is required.");
                                }
                                var errorResponse = new ErrorResponse
                                {
                                    Error = "Invalid request parameters.",
                                    Details = detailsError
                                };
                                return new BadRequestObjectResult(errorResponse);
                            }
                            var candidateListToBeProcessed = new List<CandidateQuickAddModel>();

                            foreach (var item in reqData.CandidateList)
                            {
                                var obj = new CandidateQuickAddModel
                                {
                                    Name = item.Name,
                                    ResumeLink = item.ResumeLink,
                                    EmailId = item.EmailId,
                                    Phone = item.Phone,
                                    countryCode = item.countryCode
                                };
                                candidateListToBeProcessed.Add(obj);
                            }
                            //find the name of the skill channel based on the request parameters;
                            //var irf = await _irfRepository.GetIRFBasedOnTuringsJobId((reqData.jobCountry.ToLower().Contains("onsite")? 2 :1), reqData.jobId, reqData.planType);
                            var result = await _irfService.AddMultipleCandidatesViaApi(candidateListToBeProcessed, reqData.requestedBy, true, reqData.jobId);
                            if (result != null)
                            {
                                result.performedAt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ");
                                return new OkObjectResult(result);
                            }
                            else
                            {
                                return StatusCode(StatusCodes.Status500InternalServerError);
                            }
                        }
                        else
                        {
                            details.Add("body", "Request Body is null.");
                        }
                        var errorResponserequest = new ErrorResponse
                        {
                            Error = "Invalid request.",
                            Details = details
                        };
                        return new BadRequestObjectResult(errorResponserequest);
                    }
                    return new UnauthorizedResult();
                }
                return new UnauthorizedResult();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        #region Schedule Interview Meeting
        [HttpPost]
        [Route("Schedule")]
        public async Task<IActionResult> Schedule(ScheduleCandidatesRequest reqData)
        {
            try
            {
                var functionkey = Request.Headers["x-functions-key"].ToString();
                var tenantId = Request.Headers["tenant-id"].ToString();
                if (functionkey != "SJJvpXEzy-1fOQN2mygBaTT6gXctL406VhpMwn0BqhKUAzFuBDDP5Q==")
                {
                    return new UnauthorizedResult();
                }
                if (!string.IsNullOrEmpty(tenantId))
                {
                    //check if the tenant Id belongs to the client
                    bool isAuthorizedTenant = false;
                    var clientDetails = await _clientService.GetClientByTalentId(tenantId);
                    if (clientDetails != null)
                    {
                        if (clientDetails.id > 0)
                        {
                            isAuthorizedTenant = true;
                        }
                    }
                    if (isAuthorizedTenant)
                    {
                        var details = new Dictionary<string, string>();
                        if (reqData != null)
                        {
                            if (string.IsNullOrEmpty(reqData.skillChannelId) || string.IsNullOrEmpty(reqData.requestedBy))
                            {
                                var detailsError = new Dictionary<string, string>();

                                if (string.IsNullOrEmpty(reqData.skillChannelId))
                                {
                                    detailsError.Add("skillChannelId", "This field is required.");
                                }
                                if (string.IsNullOrEmpty(reqData.requestedBy))
                                {
                                    detailsError.Add("requestedBy", "This field is required.");
                                }
                                var errorResponse = new ErrorResponse
                                {
                                    Error = "Invalid request parameters.",
                                    Details = detailsError
                                };
                                return new BadRequestObjectResult(errorResponse);
                            }
                            var irfid = 0;
                            if (!string.IsNullOrEmpty(reqData.skillChannelId))
                            {
                                irfid = _irfRepository.GetIRFIdByIRFNametenantId(reqData.skillChannelId.Trim(), tenantId);
                                if (irfid == 0)
                                {
                                    details.Add("skillChannelId", "skillChannelId is invalid");
                                    var errorResponse = new ErrorResponse
                                    {
                                        Error = "Invalid request parameters",
                                        Details = details
                                    };
                                    return new BadRequestObjectResult(errorResponse);
                                }
                            }
                            //Check Skill Channel is Active or not
                            var jdDetails = await _interviewSchedulerservice.FetchJDTitle(reqData.skillChannelId);
                            var offset = -330;
                            var TimeZoneoffset = -330;
                            var TimeZonesValue = "(UTC+05:30) Chennai, Kolkata, Mumbai, New Delhi";
                            string interviewOnDevice = string.Empty;
                            string Uniqueid = jdDetails?.CalendlyLink;
                            interviewOnDevice = "Laptop or machine on which i can code Quality Headphone Working webcam";

                            var _schedulemeeting = new ScheduleCandidateResponse();
                            _schedulemeeting.performedAt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ");
                            _schedulemeeting.SkillChannelId = reqData.skillChannelId;
                            foreach (var item in reqData.CandidateList)
                            {
                                var _ScheduleCandidateStatus = new ScheduleCandidateStatus();
                                _ScheduleCandidateStatus.CandidateEmail = item.EmailId;
                                _ScheduleCandidateStatus.Interviewdate = item.Interviewdate;
                                try
                                {
                                    var _interviewdetails = await _interviewSchedulerservice.CheckInterviewScheduleavailability(item.EmailId, reqData.skillChannelId.Trim());
                                    if (_interviewdetails != null)
                                    {
                                        if (_interviewdetails.Any(x => x.interviewtype.Contains("OtherProfile")))
                                        {
                                            _ScheduleCandidateStatus.onboardingStatus = "This Email Id can not be added. Try with another emailId or contact the Admin.";
                                        }
                                        if (_interviewdetails.Any(x => x.interviewtype.Contains("PastComplete")))
                                        {
                                            _ScheduleCandidateStatus.onboardingStatus = "An interview has already been completed for you on" + _interviewdetails.Where(x => x.interviewtype.Contains("PastComplete")).Select(x => x.interviewDate).FirstOrDefault() + ". Please contact the admin for further information.";
                                        }
                                        if (_interviewdetails.Any(x => x.interviewtype.Contains("FutureScheduled")))
                                        {
                                            _ScheduleCandidateStatus.onboardingStatus = "An interview has already been scheduled for you on " + _interviewdetails.Where(x => x.interviewtype.Contains("FutureScheduled")).Select(x => x.interviewDate).FirstOrDefault() + ". Please contact the admin for further information.";
                                        }
                                        if (_interviewdetails.Any(x => x.interviewtype.Contains("ColdCandidate")))
                                        {
                                            _ScheduleCandidateStatus.onboardingStatus = "Your Emailid is marked cold. Please contact the admin for further information.";
                                        }
                                    }
                                    var interviewdatecheck = await CheckInterviewDateMeetingSchedule(item.Interviewdate.Day, item.Interviewdate.Month, item.Interviewdate.Year, Uniqueid, item.Interviewdate);
                                    if (interviewdatecheck)
                                    {
                                        var _checkdetails = await _interviewSchedulerservice.CheckInterviewConcurrency(item.Interviewdate, reqData.skillChannelId, Uniqueid);
                                        if (_checkdetails)
                                        {
                                            var indianTime = item.Interviewdate;
                                            var interviewdatetime = item.Interviewdate;
                                            var zoomLink = new InstaZoomMeetingResponse();
                                            zoomLink = await CreateZoomMeeting(item.Name, jdDetails.JDTiltle, indianTime, jdDetails.InterviewDuration);
                                            if (zoomLink == null)
                                            {
                                                _ScheduleCandidateStatus.onboardingStatus = "Apologies, the selected slot for the meeting is currently unavailable. Please open this Scheduling link again and choose another time slot to schedule the interview, Please contact Client HR if any issues.";
                                            }
                                            string textreminder = string.Empty;
                                            textreminder = item.Phone;
                                            int interviewId = 0;
                                            var result = _risebirdMeetings.AddOrUpdateMeeting(indianTime, "60", item.Name, item.EmailId, item.ResumeLink, zoomLink.join_url, reqData.skillChannelId, zoomLink.id.ToString(), interviewOnDevice, textreminder, reqData.requestedBy, reqData.requestedBy, TimeZoneoffset, TimeZonesValue, out interviewId, zoomLink.accountemail);
                                            if (result)
                                            {
                                                _ = await _interviewSchedulerservice.UpdateSchedulingLinkDetails(Uniqueid);
                                                await _interviewSchedulerservice.CheckFutureCancelInterview(reqData.skillChannelId, indianTime, item.EmailId);
                                                string timezonevalue = string.Empty;
                                                string hh = string.Empty;
                                                var valuecheck = (-offset / 60);
                                                if (valuecheck < 10 && valuecheck > 0)
                                                {
                                                    hh = "0" + valuecheck;
                                                }
                                                if (valuecheck > -10 && valuecheck < 0)
                                                {
                                                    hh = "-0" + valuecheck.ToString().Replace("-", "");
                                                }
                                                var offsetvalue = (hh + ":" + ((offset % 60) < 0 ? (-offset % 60) : (offset % 60))).ToString();
                                                var timeZones = TimeZoneInfo.GetSystemTimeZones();
                                                foreach (TimeZoneInfo timeZone in timeZones)
                                                {
                                                    if (timeZone.DisplayName.Contains(TimeZonesValue))
                                                    {
                                                        if (timeZone.DisplayName.Contains(offsetvalue))
                                                        {
                                                            if (timeZone.IsDaylightSavingTime(interviewdatetime))
                                                            {
                                                                timezonevalue = timeZone.DaylightName;
                                                            }
                                                            else
                                                            {
                                                                timezonevalue = timeZone.StandardName;
                                                            }
                                                        }
                                                    }
                                                }
                                                string accountEmail = "hire@risebird.io";
                                                _ScheduleCandidateStatus.meetingLink = zoomLink.join_url;
                                                _ScheduleCandidateStatus.onboardingStatus = "successful";
                                                //Send Mail Notification
                                                if (item.isCandidateportal)
                                                {
                                                    var candidateUniqueId = await _candidateService.GetCandidateUniqueId(item.EmailId);
                                                    item._IRFDetails = await _interviewSchedulerservice.fetchIRFClientDetails(reqData.skillChannelId);
                                                    var portallink = "https://portal.risebird.io/Candidate/InterviewDetails?id=" + candidateUniqueId + "&IRFId=" + item._IRFDetails.inIRFid;
                                                    Mail _mail = new Mail
                                                    {
                                                        TO = item.EmailId,
                                                        Subject = item.ClientName + " Interview - " + item.Name + " | " + jdDetails.JDTiltle + " | " + interviewdatetime + "(" + timezonevalue + ")"
                                                    };
                                                    _ = await _notificationHelper.SendInterviewInvite(_mail, item.EmailId, portallink, reqData.requestedBy, reqData.requestedBy, accountEmail, interviewdatetime, item.Name, timezonevalue, item.ClientName, TimeZoneoffset, interviewId != 0 ? interviewId : new int?(), jdDetails.InterviewDuration, jdDetails.JDLink?.Replace(" ", "%20"), item._IRFDetails?.LogoLink, item._IRFDetails?.CompanyLink);
                                                    Mail _mail1 = new Mail
                                                    {
                                                        TO = reqData.requestedBy,
                                                        Subject = item.ClientName + " Interview - " + item.Name + " | " + jdDetails.JDTiltle + " | " + interviewdatetime + "(" + timezonevalue + ")"
                                                    };
                                                    var resultmail = await _notificationHelper.SendInterviewInvite(_mail1, item.EmailId, zoomLink.join_url, reqData.requestedBy, reqData.requestedBy, accountEmail, interviewdatetime, item.Name, timezonevalue, item.ClientName, TimeZoneoffset, interviewId != 0 ? interviewId : new int?(), jdDetails.InterviewDuration, jdDetails.JDLink?.Replace(" ", "%20"), item._IRFDetails?.LogoLink, item._IRFDetails?.CompanyLink);
                                                }
                                                else
                                                {
                                                    Mail _mail = new Mail
                                                    {
                                                        TO = item.EmailId,
                                                        CC = reqData.requestedBy,//zoomLink.accountemail,
                                                        Subject = item.ClientName + " Interview - " + item.Name + " | " + jdDetails.JDTiltle + " | " + interviewdatetime + "(" + timezonevalue + ")"
                                                    };
                                                    _ = await _notificationHelper.SendInterviewInvite(_mail, item.EmailId, zoomLink.join_url, reqData.requestedBy, reqData.requestedBy, accountEmail, interviewdatetime, item.Name, timezonevalue, item.ClientName, TimeZoneoffset, interviewId != 0 ? interviewId : new int?(), jdDetails.InterviewDuration, jdDetails.JDLink?.Replace(" ", "%20"), item._IRFDetails?.LogoLink, item._IRFDetails?.CompanyLink);
                                                }
                                            }
                                            else
                                            {
                                                _ScheduleCandidateStatus.onboardingStatus = "Please try again to schedule the event.";
                                            }
                                        }
                                        else
                                        {
                                            _ScheduleCandidateStatus.onboardingStatus = "Apologies, The selected slot for the meeting is currently unavailable. Please open this Scheduling link again and choose another time slot to schedule the interview, Please contact Client HR if any issues.rrently unavailable. Kindly choose another slot.";
                                        }
                                    }
                                    else
                                    {
                                        _ScheduleCandidateStatus.onboardingStatus = "Apologies, The selected slot for the meeting is currently unavailable. Please open this Scheduling link again and choose another time slot to schedule the interview, Please contact Client HR if any issues.rrently unavailable. Kindly choose another slot.";
                                    }
                                    _schedulemeeting.list.Add(_ScheduleCandidateStatus);
                                }
                                catch (Exception)
                                {
                                    _ScheduleCandidateStatus.onboardingStatus = "Please try again to schedule the event.";
                                    _schedulemeeting.list.Add(_ScheduleCandidateStatus);
                                }
                            }
                            return new OkObjectResult(_schedulemeeting);
                        }
                        else
                        {
                            details.Add("body", "Request Body is null.");
                        }
                        var errorResponserequest = new ErrorResponse
                        {
                            Error = "Invalid request.",
                            Details = details
                        };
                        return new BadRequestObjectResult(errorResponserequest);
                    }
                    return new UnauthorizedResult();
                }
                return new UnauthorizedResult();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
        #region Create Zoom Meeting
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<InstaZoomMeetingResponse> CreateZoomMeeting(string CandidateName, string Role, DateTime indianTime, int interviewduration)
        {
            InstaZoomMeetingVM _zoommetingobject = new InstaZoomMeetingVM();
            _zoommetingobject.topic = CandidateName + "| " + Role + "| Video Interview";
            //TimeZoneInfo INDIAN_ZONE = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            //DateTime indianTime = TimeZoneInfo.ConvertTimeFromUtc(interviewDate, INDIAN_ZONE);
            _zoommetingobject.start_time = indianTime.ToString("yyyy-MM-ddTHH:mm:ss");
            //var accountEmail = await _interviewSchedulerservice.CheckInstaMeetingAccount(indianTime, indianTime.AddMinutes(60));
            var accountEmail = await _interviewSchedulerservice.GetAvailableZoomAccount(indianTime.AddMinutes(-30), indianTime.AddMinutes(30));
            if (string.IsNullOrEmpty(accountEmail))
            {
                return null;
            }
            var zoomlink = CreateZoomMeetings(_zoommetingobject, accountEmail, indianTime, interviewduration);
            zoomlink.accountemail = accountEmail;
            var succeresult = await _interviewSchedulerservice.UpdateInstaZoomDetails(indianTime, indianTime.AddMinutes(60), accountEmail);
            return zoomlink;
        }
        [ApiExplorerSettings(IgnoreApi = true)]
        private InstaZoomMeetingResponse CreateZoomMeetings(InstaZoomMeetingVM _zoommetingobject, string accountEmail, DateTime indianTime, int interviewduration)
        {
            _zoommetingobject.type = 2;
            //_zoommetingobject.duration = 60;
            _zoommetingobject.duration = interviewduration;
            _zoommetingobject.timezone = "Asia/Calcutta";
            _zoommetingobject.settings.host_video = true;
            _zoommetingobject.settings.participant_video = true;
            _zoommetingobject.settings.join_before_host = true;
            _zoommetingobject.settings.jbh_time = 5;
            _zoommetingobject.settings.auto_recording = "cloud";

            //string token = ZoomToken();

            //get oauth token
            string token = _zoomHelper.GenerateAccessToken().Result;

            var client = new RestClient(string.Concat(_configuration["ZoomAppDetails:endPoint"], $"users/" + accountEmail + "/meetings"));

            var request = new RestRequest(string.Concat(_configuration["ZoomAppDetails:endPoint"], $"users/" + accountEmail + "/meetings"), Method.Post);
            request.AddHeader("content-type", "application/json");
            request.AddHeader("authorization", "Bearer " + token);
            var body = JsonConvert.SerializeObject(_zoommetingobject);
            request.AddParameter("application/json", body, ParameterType.RequestBody);
            RestResponse response = client.Execute(request);
            var content = response.Content;
            var convertedResponse = JsonConvert.DeserializeObject<InstaZoomMeetingResponse>(content);
            convertedResponse.InterviewDate = indianTime;
            convertedResponse.accountemail = accountEmail;
            return convertedResponse;
        }
        #endregion
        [ApiExplorerSettings(IgnoreApi = true)]
        private async Task<bool> CheckInterviewDateMeetingSchedule(int day, int month, int year, string id, DateTime interviewdate)
        {
            DateTime value = new DateTime(year, month, day);
            var result = await _interviewSchedulerservice.FetchSlots(value, id);
            string dayofweek = value.DayOfWeek.ToString().ToLower();
            var result0 = result.Where(x => x.Daytype == dayofweek).ToList();
            string dayofweek1 = value.AddDays(1).DayOfWeek.ToString().ToLower();
            var result1 = result.Where(x => x.Daytype == dayofweek1).ToList();
            string dayofweek2 = value.AddDays(-1).DayOfWeek.ToString().ToLower();
            var result2 = result.Where(x => x.Daytype == dayofweek2).ToList();
            var model = new List<string>();

            if (result != null)
            {
                int duration = result.FirstOrDefault().duration;
                foreach (var item in result0)
                {
                    var slotcount = Convert.ToDouble(item.End) - Convert.ToDouble(item.Start);
                    int valueset = (int)(slotcount * 60 / duration);
                    int valueincrement = (int)(slotcount * 60 % duration);
                    if (valueincrement > 0)
                    {
                        valueset++;
                    }
                    if (dayofweek != DateTime.Now.DayOfWeek.ToString().ToLower())
                    {
                        if (result.FirstOrDefault().BufferTime > 0)
                        {
                            int buffertimevalue = result.FirstOrDefault().BufferTime;
                            var comdate = DateTime.Now.AddHours(buffertimevalue);
                            for (int i = 0; i < valueset; i++)
                            {
                                var slots = Convert.ToDouble(item.Start) * 60 + (i * duration);
                                var newslot = (int)(slots / 60);
                                var newmin = Convert.ToInt32(slots % 60);
                                DateTime value1 = new DateTime(year, month, day, newslot, newmin, 0);
                                if (result.FirstOrDefault().StartDate <= value1)
                                {
                                    if (comdate < value1)
                                    {
                                        model.Add(((int)(slots / 60)).ToString("00").Replace("-", "") + ":" + (slots % 60).ToString("00").Replace("-", ""));
                                    }
                                }
                            }
                        }
                        else
                        {
                            for (int i = 0; i < valueset; i++)
                            {
                                //var slots = Convert.ToInt32(item.Start) + i;
                                var slots = Convert.ToDouble(item.Start) * 60 + (i * duration);
                                var newslot = (int)(slots / 60);
                                var newmin = Convert.ToInt32(slots % 60);
                                DateTime value1 = new DateTime(year, month, day, newslot, newmin, 0);
                                if (result.FirstOrDefault().StartDate <= value1)
                                    model.Add(((int)(slots / 60)).ToString("00").Replace("-", "") + ":" + (slots % 60).ToString("00").Replace("-", ""));
                            }
                        }
                    }
                    else
                    {
                        if (result.FirstOrDefault().BufferTime > 0)
                        {
                            int buffertimevalue = result.FirstOrDefault().BufferTime;
                            var comdate = DateTime.Now.AddHours(buffertimevalue);
                            if (comdate.Date == DateTime.Now.Date)
                            {
                                int offsetbuffer = (int)DateTime.Now.AddHours(buffertimevalue).TimeOfDay.TotalMinutes;
                                for (int i = 0; i < valueset; i++)
                                {
                                    var slots = Convert.ToDouble(item.Start) * 60 + (i * duration);
                                    var newslot = (int)(slots / 60);
                                    var newmin = Convert.ToInt32(slots % 60);
                                    DateTime value1 = new DateTime(year, month, day, newslot, newmin, 0);
                                    if (result.FirstOrDefault().StartDate <= value1)
                                    {
                                        if (slots > offsetbuffer)
                                        {
                                            model.Add(((int)(slots / 60)).ToString("00").Replace("-", "") + ":" + (slots % 60).ToString("00").Replace("-", ""));
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            var comdate = DateTime.Now;
                            if (comdate.Date == DateTime.Now.Date)
                            {
                                int offsetbuffer = (int)DateTime.Now.TimeOfDay.TotalMinutes;
                                for (int i = 0; i < valueset; i++)
                                {
                                    var slots = Convert.ToDouble(item.Start) * 60 + (i * duration);
                                    var newslot = (int)(slots / 60);
                                    var newmin = Convert.ToInt32(slots % 60);
                                    DateTime value1 = new DateTime(year, month, day, newslot, newmin, 0);
                                    if (result.FirstOrDefault().StartDate <= value1)
                                    {
                                        if (slots > offsetbuffer)
                                        {
                                            model.Add(((int)(slots / 60)).ToString("00").Replace("-", "") + ":" + (slots % 60).ToString("00").Replace("-", ""));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            List<DateTime> slotswithtime = new List<DateTime>();
            foreach (var item in model)
            {
                var _time = item.Split(":");
                DateTime datevalue = new DateTime(Convert.ToInt32(year), Convert.ToInt32(month), Convert.ToInt32(day), Convert.ToInt32(_time[0]), Convert.ToInt32(_time[1]), 0);
                slotswithtime.Add(datevalue);
            }
            var interviewdatevalidate = slotswithtime.Contains(interviewdate);
            return interviewdatevalidate;
        }
        #endregion
    }
}
