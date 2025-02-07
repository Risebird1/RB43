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
using System.Threading;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Risebird.Services.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SkillChannelController : ControllerBase
    {
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IClientService _clientService;
        private readonly IIRFService _irfService;
        private readonly IIRFRepository _irfRepository;
        private readonly IInterviewSchedulerService _interviewSchedulerservice;
        private readonly INotificationHelper _notificationHelper;
        private readonly HttpClient _httpClient;
        private readonly CancellationToken _cancellationToken;
        public SkillChannelController(IApplicationErrorLogger applicationErrorLogger, 
            IClientService clientService, IIRFService irfService, 
            IIRFRepository irfRepository, IInterviewSchedulerService interviewSchedulerservice,
            INotificationHelper notificationHelper,
            HttpClientHelper factory)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _clientService = clientService;
            _irfService = irfService;
            _irfRepository = irfRepository;
            _interviewSchedulerservice = interviewSchedulerservice;
            _notificationHelper = notificationHelper;
            _httpClient = factory.GetHttpClient();
        }
        [HttpPost]
        [Route("CreateAndSchedule")]
        public async Task<IActionResult> CreateAndSchedule(AddCandidatesRequest reqData)
        {
            try
            {
                var functionkey = Request.Headers["x-functions-key"].ToString();
                var tenantId = Request.Headers["tenant-id"].ToString();//6143FFF2-4172-488F-8D37-C1C85C34D65E  for UST US
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
                        //check if a skill channel is already created against the given request Id; get two outputs from here one boolean isExists and the other the assessmentGroupId/MagId mapped against the JobTitle which shall be required if a skill channel doesnt exist. This will eliminate the need to make another db call to just get the magId for the job title.

                        var details = new Dictionary<string, string>();
                        if (reqData != null)
                        {
                            if ((string.IsNullOrEmpty(reqData.requestId) && string.IsNullOrEmpty(reqData.jobId)) || string.IsNullOrEmpty(reqData.requestedBy) || string.IsNullOrEmpty(reqData.jobTitle))
                            {
                                var detailsError = new Dictionary<string, string>();

                                if (string.IsNullOrEmpty(reqData.requestId) && string.IsNullOrEmpty(reqData.jobId))
                                {
                                    detailsError.Add("requestId/jobId", "Either of 'requestId' or 'jobId' fields is required.");
                                }
                                if (string.IsNullOrEmpty(reqData.requestedBy))
                                {
                                    detailsError.Add("requestedBy", "This field is required.");
                                }
                                if (string.IsNullOrEmpty(reqData.jobTitle))
                                {
                                    detailsError.Add("jobTitle", "This field is required.");
                                }
                                var errorResponse = new ErrorResponse
                                {
                                    Error = "Invalid request parameters.",
                                    Details = detailsError
                                };
                                return new BadRequestObjectResult(errorResponse);
                            }

                            var isExistsObj = await _irfRepository.CheckExistenceofSkillChannelAgainstRequestId(reqData.requestId ?? reqData.jobId, reqData.jobTitle);
                            int irfId = isExistsObj.irfId;
                            int assessmentGroupId = isExistsObj.assessmentGroupId;
                            string assessmentGroupName = isExistsObj.AssessmentGroupName;
                            //if skill channel doesnt exist, check if there is a mag mapped to the jdtitle sent in the request
                            var irfDetails = new IRF();
                            if (irfId == 0)//skill channel doesnt exist
                            {
                                if (assessmentGroupId == 0)
                                {
                                    //shoot a mail that we do not have an assessment group mapped to the job title and therefore skill channel cant be created
                                    await _notificationHelper.SendAsssessmentGroupNotMappedToJobTitleMail(reqData.requestedBy, string.Empty, reqData.jobTitle);
                                }
                                else
                                {
                                    //if mag exists, check for template
                                    if(isExistsObj.TemplateId > 0)
                                    {
                                        //if template exists...go ahead and create the skill channel...
                                        var irfModel = new IRFVm();
                                        irfModel.AssessmentGroupId = assessmentGroupId.ToString();
                                        irfModel.ClientId = clientDetails != null ? clientDetails.id : 67; //ust's client id : 29 ust, 67 ust us, 152 ust pe?
                                        irfModel.StartDate = DateTime.Now;
                                        irfModel.EndDate = DateTime.Now.AddMonths(3);
                                        irfModel.MeetingPlatform = MeetingPlatforms.Zoom;
                                        irfModel.JDTiltle = reqData.jobTitle;
                                        irfModel.ClientName = clientDetails?.Name;
                                        irfModel.CreatedFromTemplateId = isExistsObj.TemplateId;
                                        irfModel.TemplateName = isExistsObj.TemplateName;
                                        //var clientDetails = await _clientService.GetClient(irfModel.ClientId);
                                        string _schedulinglink = await AutoGenerateSchedulingLink(irfModel.StartDate, irfModel.IRFId, irfModel.JDTiltle + "_Scheduling", "CreateInterviewLinkV1", irfModel.MeetingPlatform);

                                        irfModel.CalendlyLink = _schedulinglink;
                                        irfModel.IRFId = GenerateSkillChannelName(assessmentGroupName, clientDetails?.Name);
                                        irfModel.BillingOwner = 0;
                                        irfModel.CompanyType = clientDetails?.ClientType > 0 ? clientDetails.ClientType.ToString() : ((int)CompanyType.Service).ToString();
                                        irfModel.Heuristicsscore = (clientDetails != null && clientDetails.PlanType > 0) ? clientDetails.PlanType : (int)Heuristicsscorelevel.Standard;
                                        irfModel.JobCountry = clientDetails?.CountryType > 0 ? clientDetails.CountryType.ToString() : ((int)CountryTypeEnum.Offshore).ToString();
                                        var irfCreationResult = await _irfService.CreateSkillChannelUsingTemplateByInterviewLinkPopUpV1(irfModel, "api", null);

                                        if (irfCreationResult.Item1)
                                        {
                                            irfDetails = await _irfService.GetSkillChannelDetailsByName(irfModel?.IRFId);

                                            FireAndForgetRequestToSendAvailabilityMails(irfModel?.AssessmentGroupId, irfDetails.id, irfCreationResult.Item2, irfDetails.PrimarySkillInvite, irfModel?.IRFId, reqData.jobTitle);
                                            return RedirectToAction("Schedule", "Candidates", new { candidateList = reqData.CandidateList, irfId = irfDetails.id });

                                        }
                                        else
                                        {
                                            return StatusCode(StatusCodes.Status500InternalServerError);
                                        }
                                    }
                                    else
                                    {
                                        //if template doesnt exist, call the openAI method to create a tech rubric and create the skill channel
                                        return RedirectToAction("Schedule", "Candidates", new { candidateList = reqData.CandidateList, irfId = irfDetails.id });

                                    }



                                }
                            }
                            //var candidateListToBeProcessed = new List<CandidateQuickAddModel>();

                            //foreach (var item in reqData.CandidateList)
                            //{
                            //    var obj = new CandidateQuickAddModel
                            //    {
                            //        Name = item.Name,
                            //        ResumeLink = item.ResumeLink,
                            //        EmailId = item.EmailId,
                            //        Phone = item.Phone,
                            //        countryCode = item.countryCode,
                            //        IRFId = irfDetails.id,
                            //    };
                            //    candidateListToBeProcessed.Add(obj);
                            //}
                            ////find the name of the skill channel based on the request parameters;
                            ////var irf = await _irfRepository.GetIRFBasedOnTuringsJobId((reqData.jobCountry.ToLower().Contains("onsite")? 2 :1), reqData.jobId, reqData.planType);
                            //var result = await _irfService.AddMultipleCandidatesViaApi(candidateListToBeProcessed, reqData.requestedBy, true, irfDetails.IRFId);
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

        private async Task FireAndForgetRequestToSendAvailabilityMails(string assessmentGroupId, int irfId, int flatRate, string primarySkillInvite, string irfName, string jdTitle)
        {
            var dto = new AvailabilityCheckDTO { id = irfId, AssessmentGroupId = assessmentGroupId, FlatRate = flatRate, PrimarySkillInvite = primarySkillInvite, IRFId = irfName, JDTitle = jdTitle };
            var path = $"https://risebirdmicroservicededicated.azurewebsites.net/api/CheckIRFAvailability";
            //var path = $"http://localhost:7071/api/CheckIRFAvailability";
            var functionKey = "ok-GN-EaOPcWFO2ft8_m_eIaLSxMgT_0a19YdeFmBSPCAzFuuzQdQg==";
            //functionKey = string.Empty;
            await _httpClient.SendRequestAsync(HttpMethod.Post, path, dto, functionKey, "", _cancellationToken);
        }
        private async Task<string> AutoGenerateSchedulingLink(DateTime eventStartdate, string SkillChannel, string EventName, string source, string meetingPlatform = null)
        {
            var model_Second = new List<daytimes_second>();
            var sun = new List<perdaytime>();
            var mon = new List<perdaytime>();
            sun.Add(new perdaytime { Start = "8", End = "23" });
            mon.Add(new perdaytime { Start = "10", End = "23" });
            //mon.Add(new perdaytime { Start = "18", End = "23" });
            model_Second.Add(new daytimes_second { Daytype = "sunday", dayCheck = true, dayvalue = sun });
            model_Second.Add(new daytimes_second { Daytype = "monday", dayCheck = true, dayvalue = mon });
            model_Second.Add(new daytimes_second { Daytype = "tuesday", dayCheck = true, dayvalue = mon });
            model_Second.Add(new daytimes_second { Daytype = "wednesday", dayCheck = true, dayvalue = mon });
            model_Second.Add(new daytimes_second { Daytype = "thursday", dayCheck = true, dayvalue = mon });
            model_Second.Add(new daytimes_second { Daytype = "friday", dayCheck = true, dayvalue = mon });
            model_Second.Add(new daytimes_second { Daytype = "saturday", dayCheck = true, dayvalue = sun });
            var model_report = new daytimes_report();
            model_report.setvalue = model_Second;
            model_report.StartDate = eventStartdate.AddHours(12);
            model_report.EndDate = eventStartdate.AddDays(60);
            model_report.EventName = EventName;
            model_report.SkillChannel = SkillChannel;
            model_report.duration = 60;
            model_report.Concurrency = 10;
            model_report.BufferTime = 18;
            model_report.TimeSlotDays = 6;
            model_report.MeetingPlatform = meetingPlatform;
            model_report.sourcevalue = source;
            var eventid = await _interviewSchedulerservice.AutoCreateEvent(model_report);
            var model = await _interviewSchedulerservice.FetchEventById(eventid);
            if (model != null)
            {
                var eventurl = "https://" + Request.Host.ToString() + "/InterviewScheduler/ScheduleEvent?id=" + model.Uniqueid;
                return eventurl;
            }
            return "";
        }

        private string GenerateSkillChannelName(string assessmentGroupName, string clientCompanyName)
        {
            bool flag = false;
            string skillChannelName = string.Empty;
            while (!flag)
            {
                if (!string.IsNullOrEmpty(clientCompanyName))
                {
                    skillChannelName = clientCompanyName.Substring(0, 3);
                    skillChannelName = skillChannelName + assessmentGroupName.Replace(" ", "").Substring(0, 2) + GenerateRandomstring();
                }
                //flag = CheckUniqueness(skillChannelName, false);
                flag = _irfService.CheckUniqueness(skillChannelName, false);
            }
            skillChannelName = skillChannelName.Replace(".", "a").Replace(" ", "-").Replace("+", "s").Replace("&", "o");
            return skillChannelName;
        }
        private string GenerateRandomstring()
        {
            //4char date or month and 1char secondUnique
            DateTime _dt = DateTime.Now;
            var month = _dt.Month.ToString();
            if (month.Length == 1)
            {
                month = "0" + month;
            }
            var _date = _dt.Day.ToString();
            if (_date.Length == 1)
            {
                _date = "0" + _date;
            }
            var sec = digSum(_dt.Second);
            return _date + month + sec.ToString();
        }

        private int digSum(int n)
        {
            int sum = 0;
            while (n > 0 || sum > 9)
            {
                if (n == 0)
                {
                    n = sum;
                    sum = 0;
                }
                sum += n % 10;
                n /= 10;
            }
            return sum;
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
