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
using static Risebird.Entities.Data.RchilliResumeMapFields;
using Risebird.Business;

namespace Risebird.MicroService
{
    public class InterviewChannelCandidates
    {
        private readonly IHttpRequestHelper _httpRequestHelper;
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IClientService _clientService;

        public InterviewChannelCandidates(IHttpRequestHelper httpRequestHelper, IConfiguration configuration, IInterviewerService interviewerService,
            IApplicationErrorLogger applicationErrorLogger, IClientService clientService)
        {
            _httpRequestHelper = httpRequestHelper;
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _clientService = clientService;
        }
        [FunctionName("InterviewChannel/Candidates")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
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
                        if (reqData != null)
                        {
                            if (!string.IsNullOrEmpty(reqData.skillChannelId))
                            {
                                if (!string.IsNullOrEmpty(reqData.type))
                                {
                                    //candidateData = (await _candidateService.GetCandidateTimelinebyDateStatus(DateTime.Now.AddDays(-200), DateTime.Now.AddDays(15), dataType, emailid)).ToList();
                                    if (reqData.type == "Alldata")
                                    {
                                        //Get skill channel all Candidate Info and return
                                        return new OkObjectResult("");
                                    }
                                    else if (reqData.type == "DateRange")
                                    {
                                        if (reqData.startDate != null && reqData.endDate != null)
                                        {
                                            //Get Skill channel candidate info with daterange candidate
                                            return new OkObjectResult("");
                                        }
                                    }
                                }
                            }
                        }
                        return new BadRequestResult();
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
