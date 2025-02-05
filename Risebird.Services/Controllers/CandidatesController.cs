using Microsoft.AspNetCore.Mvc;
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

        public CandidatesController(IApplicationErrorLogger applicationErrorLogger, IClientService clientService, IIRFService irfService, IIRFRepository irfRepository, IInterviewSchedulerService interviewSchedulerservice)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _clientService = clientService;
            _irfService = irfService;
            _irfRepository = irfRepository;
            _interviewSchedulerservice = interviewSchedulerservice;
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
                            //find the name of the skill channel based on the request parameters;
                            //var irf = await _irfRepository.GetIRFBasedOnTuringsJobId((reqData.jobCountry.ToLower().Contains("onsite")? 2 :1), reqData.jobId, reqData.planType);
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
                                irfid = _irfRepository.GetIRFIdByIRFName(reqData.skillChannelId.Trim());
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
                            ////Check Requester have skillchannel access
                            ////Check SkillChannel is active or not
                            ////Schedule Interview with details and checked the details
                            foreach (var item in reqData.CandidateList)
                            {
                                var _interviewdetails = await _interviewSchedulerservice.CheckInterviewScheduleavailability(item.Email, reqData.skillChannelId.Trim());
                                if (_interviewdetails != null)
                                {
                                    if (_interviewdetails.Any(x => x.interviewtype.Contains("OtherProfile")))
                                    {
                                        ModelState.AddModelError(string.Empty, "This Email Id can not be added. Try with another emailId or contact the Admin.");
                                    }
                                    if (_interviewdetails.Any(x => x.interviewtype.Contains("PastComplete")))
                                    {
                                        ModelState.AddModelError(string.Empty, "An interview has already been completed for you on" + _interviewdetails.Where(x => x.interviewtype.Contains("PastComplete")).Select(x => x.interviewDate).FirstOrDefault() + ". Please contact the admin for further information.");
                                    }
                                    if (_interviewdetails.Any(x => x.interviewtype.Contains("FutureScheduled")))
                                    {
                                        ModelState.AddModelError(string.Empty, "An interview has already been scheduled for you on " + _interviewdetails.Where(x => x.interviewtype.Contains("FutureScheduled")).Select(x => x.interviewDate).FirstOrDefault() + ". Please contact the admin for further information.");
                                    }
                                    if (_interviewdetails.Any(x => x.interviewtype.Contains("ColdCandidate")))
                                    {
                                        ModelState.AddModelError(string.Empty, "Your Emailid is marked cold. Please contact the admin for further information.");
                                    }
                                }
                                if (string.IsNullOrEmpty(item.Resume))
                                {
                                    ModelState.AddModelError(string.Empty, "Resume Link is required.");
                                }
                                var _checkdetails = await _interviewSchedulerservice.CheckInterviewConcurrency(item.scheduleDate, reqData.skillChannelId, item.Uniqueid);
                                if (_checkdetails)
                                {
                                    //var zoomLink = new InstaZoomMeetingResponse();
                                    //zoomLink = await CreateZoomMeeting(model.Name, jdDetails.JDTiltle, indianTime, jdDetails.InterviewDuration);
                                    //if (zoomLink == null)
                                    //{
                                    //    ModelState.AddModelError(string.Empty, "Apologies, The selected slot for the meeting is currently unavailable. Please open this Scheduling link again and choose another time slot to schedule the interview, Please contact Client HR if any issues.");
                                    //}
                                    //string textreminder = string.Empty;
                                    //textreminder = model.MobileNumber;
                                    //int interviewId = 0;
                                    //var result = _risebirdMeetings.AddOrUpdateMeeting(indianTime, "60", model.Name, model.Email, model.Resume, zoomLink.join_url, model.InterviewChannelId, zoomLink.id.ToString(), interviewOnDevice, textreminder, model.StaffingAgiencies, model.HRCoordinator, model.TimeZoneoffset, model.TimeZonesValue, out interviewId, zoomLink.accountemail);
                                    //if (result)
                                    //{
                                    //    _ = await _interviewSchedulerservice.UpdateSchedulingLinkDetails(model.Uniqueid);
                                    //    await _interviewSchedulerservice.CheckFutureCancelInterview(model.InterviewChannelId, indianTime, model.Email);
                                    //    string timezonevalue = string.Empty;
                                    //    string hh = string.Empty;
                                    //    var valuecheck = (-model.offset / 60);
                                    //    if (valuecheck < 10 && valuecheck > 0)
                                    //    {
                                    //        hh = "0" + valuecheck;
                                    //    }
                                    //    if (valuecheck > -10 && valuecheck < 0)
                                    //    {
                                    //        hh = "-0" + valuecheck.ToString().Replace("-", "");
                                    //    }
                                    //    var offsetvalue = (hh + ":" + ((model.offset % 60) < 0 ? (-model.offset % 60) : (model.offset % 60))).ToString();
                                    //    var timeZones = TimeZoneInfo.GetSystemTimeZones();
                                    //    foreach (TimeZoneInfo timeZone in timeZones)
                                    //    {
                                    //        if (timeZone.DisplayName.Contains(model.TimeZonesValue))
                                    //        {
                                    //            if (timeZone.DisplayName.Contains(offsetvalue))
                                    //            {
                                    //                if (timeZone.IsDaylightSavingTime(interviewdatetime))
                                    //                {
                                    //                    timezonevalue = timeZone.DaylightName;
                                    //                }
                                    //                else
                                    //                {
                                    //                    timezonevalue = timeZone.StandardName;
                                    //                }
                                    //            }
                                    //        }
                                    //    }
                                    //    string accountEmail = "hire@risebird.io";
                                    //    //Send Mail Notification
                                    //    if (model.isCandidateportal)
                                    //    {
                                    //        var candidateUniqueId = await _candidateService.GetCandidateUniqueId(model.Email);
                                    //        model._IRFDetails = await _interviewSchedulerservice.fetchIRFClientDetails(model.InterviewChannelId);
                                    //        var portallink = "https://portal.risebird.io/Candidate/InterviewDetails?id=" + candidateUniqueId + "&IRFId=" + model._IRFDetails.inIRFid;
                                    //        Mail _mail = new Mail
                                    //        {
                                    //            TO = model.Email,
                                    //            Subject = model.ClientName + " Interview - " + model.Name + " | " + jdDetails.JDTiltle + " | " + interviewdatetime + "(" + timezonevalue + ")"
                                    //        };
                                    //        _ = await _notificationHelper.SendInterviewInvite(_mail, model.Email, portallink, model.StaffingAgiencies, model.HRCoordinator, accountEmail, interviewdatetime, model.Name, timezonevalue, model.ClientName, model.TimeZoneoffset, interviewId != 0 ? interviewId : new int?(), jdDetails.InterviewDuration, jdDetails.JDLink?.Replace(" ", "%20"), model._IRFDetails?.LogoLink, model._IRFDetails?.CompanyLink);
                                    //        Mail _mail1 = new Mail
                                    //        {
                                    //            TO = model.Guests + ";" + model.HRCoordinator,
                                    //            Subject = model.ClientName + " Interview - " + model.Name + " | " + jdDetails.JDTiltle + " | " + interviewdatetime + "(" + timezonevalue + ")"
                                    //        };
                                    //        _ = await _notificationHelper.SendInterviewInvite(_mail1, model.Email, zoomLink.join_url, model.StaffingAgiencies, model.HRCoordinator, accountEmail, interviewdatetime, model.Name, timezonevalue, model.ClientName, model.TimeZoneoffset, interviewId != 0 ? interviewId : new int?(), jdDetails.InterviewDuration, jdDetails.JDLink?.Replace(" ", "%20"), model._IRFDetails?.LogoLink, model._IRFDetails?.CompanyLink);
                                    //    }
                                    //    else
                                    //    {
                                    //        Mail _mail = new Mail
                                    //        {
                                    //            TO = model.Email,
                                    //            CC = model.Guests + ";" + model.HRCoordinator,//zoomLink.accountemail,
                                    //            Subject = model.ClientName + " Interview - " + model.Name + " | " + jdDetails.JDTiltle + " | " + interviewdatetime + "(" + timezonevalue + ")"
                                    //        };
                                    //        if (string.IsNullOrEmpty(model.MeetingPlatform) || model.MeetingPlatform == MeetingPlatforms.Zoom)
                                    //        {
                                    //            _ = await _notificationHelper.SendInterviewInvite(_mail, model.Email, zoomLink.join_url, model.StaffingAgiencies, model.HRCoordinator, accountEmail, interviewdatetime, model.Name, timezonevalue, model.ClientName, model.TimeZoneoffset, interviewId != 0 ? interviewId : new int?(), jdDetails.InterviewDuration, jdDetails.JDLink?.Replace(" ", "%20"), model._IRFDetails?.LogoLink, model._IRFDetails?.CompanyLink);
                                    //        }
                                    //        else if (model.MeetingPlatform == MeetingPlatforms.RisebirdMeet)
                                    //        {
                                    //            _ = await _notificationHelper.SendInterviewInvite(_mail, model.Email, rbMeetingLinkResponse.meetingAttendee, model.StaffingAgiencies, model.HRCoordinator, accountEmail, interviewdatetime, model.Name, timezonevalue, model.ClientName, model.TimeZoneoffset, interviewId != 0 ? interviewId : new int?(), jdDetails.InterviewDuration, jdDetails.JDLink?.Replace(" ", "%20"), model._IRFDetails?.LogoLink, model._IRFDetails?.CompanyLink);
                                    //        }
                                    //    }
                                    //}
                                    //else
                                    //{
                                    //    ModelState.AddModelError(string.Empty, "Please try again to schedule the event.");
                                    //}
                                }
                                else
                                {
                                    ModelState.AddModelError(string.Empty, "Apologies, The selected slot for the meeting is currently unavailable. Please open this Scheduling link again and choose another time slot to schedule the interview, Please contact Client HR if any issues.rrently unavailable. Kindly choose another slot.");
                                }
                            }
                            ////find the name of the skill channel based on the request parameters;
                            ////var irf = await _irfRepository.GetIRFBasedOnTuringsJobId((reqData.jobCountry.ToLower().Contains("onsite")? 2 :1), reqData.jobId, reqData.planType);
                            //var result = await _irfService.AddMultipleCandidatesViaApi(reqData.CandidateList, reqData.requestedBy, true, reqData.jobId);
                            //if (result != null)
                            //{
                            //    result.performedAt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ");
                            //    return new OkObjectResult(result);
                            //}
                            //else
                            //{
                            //    return StatusCode(StatusCodes.Status500InternalServerError);
                            //}
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
    }
}
