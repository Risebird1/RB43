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
                if (functionkey != "hyHapmsGp_YTuibiaxcEc3UZCG3F6rciouJjTGShzZrwAzFuNGdqCw==")
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
                            var SkillChannelid = isExistsObj.IRFName;
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
                                    
                                    var detailsError = new Dictionary<string, string>();

                                    if (string.IsNullOrEmpty(reqData.requestId) && string.IsNullOrEmpty(reqData.jobId))
                                    {
                                        detailsError.Add("Unavailable", "A mapping for the requested Job title is unavailable.");
                                    }
                                    var errorResponse = new ErrorResponse
                                    {
                                        Error = "Unavailable",
                                        Details = detailsError
                                    };
                                    return new BadRequestObjectResult(errorResponse);
                                }
                                else
                                {
                                    //if mag exists, check for template
                                    if (isExistsObj.TemplateId > 0)
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
                                        irfModel.JDLink = reqData.jdLink ?? string.Empty;
                                        //var clientDetails = await _clientService.GetClient(irfModel.ClientId);
                                        string _schedulinglink = await AutoGenerateSchedulingLink(irfModel.StartDate, irfModel.IRFId, irfModel.JDTiltle + "_Scheduling", "CreateInterviewLinkV1", irfModel.MeetingPlatform);

                                        irfModel.CalendlyLink = _schedulinglink;
                                        irfModel.IRFId = GenerateSkillChannelName(assessmentGroupName, clientDetails?.Name);
                                        irfModel.BillingOwner = 0;
                                        irfModel.CompanyType = clientDetails?.ClientType > 0 ? clientDetails.ClientType.ToString() : ((int)CompanyType.Service).ToString();
                                        irfModel.Heuristicsscore = (clientDetails != null && clientDetails.PlanType > 0) ? clientDetails.PlanType : (int)Heuristicsscorelevel.Standard;
                                        irfModel.JobCountry = clientDetails?.CountryType > 0 ? clientDetails.CountryType.ToString() : ((int)CountryTypeEnum.Offshore).ToString();
                                        var irfCreationResult = await _irfService.CreateSkillChannelUsingTemplateByInterviewLinkPopUpV1(irfModel, "api", null);
                                        SkillChannelid = irfModel?.IRFId;
                                        if (irfCreationResult.Item1)
                                        {
                                            irfDetails = await _irfService.GetSkillChannelDetailsByName(irfModel?.IRFId);

                                            FireAndForgetRequestToSendAvailabilityMails(irfModel?.AssessmentGroupId, irfDetails.id, irfCreationResult.Item2, irfDetails.PrimarySkillInvite, irfModel?.IRFId, reqData.jobTitle);
                                        }
                                        else
                                        {
                                            return StatusCode(StatusCodes.Status500InternalServerError);
                                        }
                                    }
                                    else
                                    {
                                        //if template doesnt exist, call the openAI method to create a tech rubric and create the skill channel


                                        //temporary
                                        var detailsError = new Dictionary<string, string>();

                                        if (string.IsNullOrEmpty(reqData.requestId) && string.IsNullOrEmpty(reqData.jobId))
                                        {
                                            detailsError.Add("Template", "Template for the Assessment Group mapped to the Job Title not found!");
                                        }
                                        var errorResponse = new ErrorResponse
                                        {
                                            Error = "Missing template",
                                            Details = detailsError
                                        };
                                        return new BadRequestObjectResult(errorResponse);

                                    }
                                }
                            }
                            //schedule the candidate meetings
                            //return RedirectToAction("Schedule", "Candidates", new { candidateList = reqData.CandidateList, irfId = irfDetails.id });
                            //Api Call
                            var _ScheduleCandidatesRequest = new ScheduleCandidatesRequest();
                            _ScheduleCandidatesRequest.skillChannelId = SkillChannelid;
                            _ScheduleCandidatesRequest.requestedBy = reqData.requestedBy;
                            _ScheduleCandidatesRequest.RequestId = reqData.requestId ?? reqData.jobId;
                            _ScheduleCandidatesRequest.CandidateList = new List<ScheduleCandidateList>();
                            foreach (var item in reqData.CandidateList)
                            {
                                _ScheduleCandidatesRequest.CandidateList.Add(new ScheduleCandidateList
                                {
                                    EmailId = item.EmailId,
                                    Name = item.Name,
                                    ResumeLink = item.ResumeLink,
                                    Phone = item.Phone,
                                    Interviewdate = item.InterviewDate
                                });
                            }

                            var apiurl = "https://" + Request.Host.ToString() + "/api/Candidates/Schedule";
                            var client = new RestClient(apiurl);
                            var request = new RestRequest(apiurl, Method.Post);

                            request.AddHeader("content-type", "application/json");
                            request.AddHeader("x-functions-key", "SJJvpXEzy-1fOQN2mygBaTT6gXctL406VhpMwn0BqhKUAzFuBDDP5Q==");
                            request.AddHeader("tenant-id", tenantId);

                            var body = JsonConvert.SerializeObject(_ScheduleCandidatesRequest);
                            request.AddParameter("application/json", body, ParameterType.RequestBody);
                            RestResponse response = client.Execute(request);
                            var content = response.Content;
                            var convertedResponse = JsonConvert.DeserializeObject<ScheduleCandidateResponse>(content);
                            return new OkObjectResult(response.Content);
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
    }
}
