using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data.RbAPIDataModels;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System.Linq;
using System.Collections.Generic;
using Risebird.CommonHelper;
using Microsoft.Azure.Documents.SystemFunctions;
using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;

namespace Risebird.MicroService
{
    public class InterviewChannelCandidates
    {
        private readonly IHttpRequestHelper _httpRequestHelper;
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IClientService _clientService;
        private readonly ICandidateService _candidateService;
        private readonly IIRFRepository _irfRepository;

        public InterviewChannelCandidates(IHttpRequestHelper httpRequestHelper, IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger,
            IClientService clientService, ICandidateService candidateService, IIRFRepository irfRepository)
        {
            _httpRequestHelper = httpRequestHelper;
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _clientService = clientService;
            _candidateService = candidateService;
            _irfRepository = irfRepository;
        }
        [FunctionName("InterviewChannelCandidates")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "InterviewChannel/Candidates")] HttpRequest req,
            ILogger log)
        {
            try
            {
                log.LogInformation($"InterviewChannelCandidates : C# HTTP trigger function invoked at {DateTime.Now}");
                var tenantId = req.Headers["tenant-id"].ToString();
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
                        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                        log.LogInformation(requestBody);
                        var reqData = JsonConvert.DeserializeObject<CandidateLatestStatusRequest>(requestBody);
                        log.LogInformation("C# HTTP trigger function processed a request.");
                        var details = new Dictionary<string, string>();
                        if (reqData != null)
                        {
                            if (string.IsNullOrEmpty(reqData.skillChannelId) || string.IsNullOrEmpty(reqData.type) || string.IsNullOrEmpty(reqData.requestedBy))
                            {
                                if (string.IsNullOrEmpty(reqData.skillChannelId))
                                {
                                    details.Add("skillChannelId", "This field is required.");
                                }
                                if (string.IsNullOrEmpty(reqData.type))
                                {
                                    details.Add("type", "This field is required.");
                                }
                                if (string.IsNullOrEmpty(reqData.requestedBy))
                                {
                                    details.Add("requestedBy", "This field is required.");
                                }
                                var errorResponse = new ErrorResponse
                                {
                                    Error = "Invalid request parameters",
                                    Details = details
                                };
                                log.LogInformation($"CandidateLatestStatus : C# HTTP trigger function executed at {DateTime.Now}");
                                return new BadRequestObjectResult(errorResponse);
                            }

                            var model = new CandidateInfoResponse();
                            var irfid = _irfRepository.GetIRFIdByIRFName(reqData.skillChannelId.Trim());
                            if (irfid > 0)
                            {
                                //Update according to client user role based
                                var result = await _candidateService.GetIRFCandidateStatsForExcelDownLoad(irfid);
                                model.skillChannelId = reqData.skillChannelId;
                                model.requestedBy = reqData.requestedBy;
                                model.retrievedAt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ");
                                foreach (var item in result)
                                {
                                    if (item.InterviewStatusTwo != null && item.InterviewStatusTwo > 0)
                                    {
                                        item.InterviewStatusTwoDesc = ((InterviewStatus)Convert.ToInt32(item.InterviewStatusTwo)).GetDescription();
                                    }
                                    if (item.MeetingStatusTwo != null)
                                    {
                                        if (item.MeetingStatusTwo != 4)
                                        {
                                            if (!string.IsNullOrWhiteSpace(item.InterviewStatusTwoDesc))
                                            {
                                                item.CandidateLatestStatus = item.CandidateLatestStatus + " and " + item.InterviewStatusTwoDesc;
                                            }
                                        }
                                        else
                                        {
                                            if (item.MeetingStatusTwo > 0)
                                            {
                                                item.MeetingStatusTwoDesc = ((MeetingStatus)Convert.ToInt32(item.MeetingStatusTwo)).GetDescription();
                                            }
                                            if (!string.IsNullOrWhiteSpace(item.MeetingStatusTwoDesc))
                                            {
                                                item.CandidateLatestStatus = item.CandidateLatestStatus + " and " + item.MeetingStatusTwoDesc;
                                            }
                                        }
                                    }
                                    //Check for meeting status complete
                                    var executiveurl = string.Empty;
                                    var meetingstatus = !string.IsNullOrWhiteSpace(item.MeetingStatus) ? Convert.ToInt32(item.MeetingStatus) : 0;
                                    if (meetingstatus == 4 || meetingstatus == 6 || meetingstatus == 7 || meetingstatus == 8)
                                    {
                                        executiveurl = $"https://portal.risebird.io/Interview/ExecutiveReport?interviewId={item.id}&irfId={irfid}&isClient=true";
                                    }
                                    model.result.Add(new CandidateStatusResponse()
                                    {
                                        interviewDate = item.interviewDate?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                                        currentStatus = item.CandidateLatestStatus,
                                        candidateEmailId = item.candidateEmail,
                                        interviewer = item.InterviewerCodedName,
                                        reportUrl = executiveurl
                                    });
                                }
                                if (reqData.type == "Alldata")
                                {
                                    //Get skill channel all Candidate Info and return
                                    return new OkObjectResult(model);
                                }
                                else if (reqData.type == "DateRange")
                                {
                                    if (reqData.startDate != null && reqData.endDate != null)
                                    {
                                        //Get Skill channel candidate info with daterange candidate
                                        model.result = model.result.Where(x => !string.IsNullOrWhiteSpace(x.interviewDate) && Convert.ToDateTime(x.interviewDate) > reqData.startDate && Convert.ToDateTime(x.interviewDate) < reqData.endDate)?.ToList();
                                        return new OkObjectResult(model);
                                    }
                                    else
                                    {
                                        details.Add("DateFileds", "Date Fields are invalid");
                                        var errorResponse = new ErrorResponse
                                        {
                                            Error = "Invalid request parameters",
                                            Details = details
                                        };
                                        return new BadRequestObjectResult(errorResponse);
                                    }
                                }
                                else
                                {
                                    return new BadRequestObjectResult("Invalid type");
                                }
                            }
                            else
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
                        if (reqData == null)
                        {
                            details.Add("body", "Request Body is null.");
                        }
                        var errorResponserequest = new ErrorResponse
                        {
                            Error = "Invalid request parameters",
                            Details = details
                        };
                        return new BadRequestObjectResult(errorResponserequest);
                    }
                    return new UnauthorizedResult();
                }
                return new BadRequestResult();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                log.LogInformation($"CandidateLatestStatus : C# HTTP trigger function threw exception with message {ex.Message} at {DateTime.Now}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
