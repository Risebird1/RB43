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
                                    var model = new List<CandidateLatestStatusResponse>();
                                    var irfid = _irfRepository.GetIRFIdByIRFName(reqData.skillChannelId.Trim());
                                    if (irfid > 0)
                                    {
                                        //Update according to client user role based
                                        var result = await _candidateService.GetIRFCandidateStatsForExcelDownLoad(irfid);
                                        foreach (var item in result)
                                        {
                                            if (item.InterviewStatusTwo != null && item.InterviewStatusTwo > 0)
                                            {
                                                item.InterviewStatusTwoDesc = ((InterviewStatus)Convert.ToInt32(item.InterviewStatusTwo)).GetDescription();
                                            }
                                            if (!string.IsNullOrWhiteSpace(item.InterviewStatusTwoDesc))
                                            {
                                                item.CandidateLatestStatus = item.CandidateLatestStatus + " and " + item.InterviewStatusTwoDesc;
                                            }
                                            //Check for meeting status complete
                                            var executiveurl = $"https://portal.risebird.io/Interview/ExecutiveReport?interviewId={item.id}&irfId={irfid}&isClient=true";
                                            model.Add(new CandidateLatestStatusResponse()
                                            {
                                                interviewDate = item.interviewDate,
                                                currentStatus = item.CandidateLatestStatus,
                                                candidateEmailId = item.candidateEmail,
                                                skillChannelId = item.IRFId,
                                                requestedBy = reqData.requestedBy,
                                                retrievedAt = DateTime.Now,
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
                                                model = model.Where(x => x.interviewDate > reqData.startDate && x.interviewDate < reqData.endDate)?.ToList();
                                                return new OkObjectResult(model);
                                            }
                                        }
                                        else
                                        {
                                            return new BadRequestObjectResult("Invalid type");
                                        }
                                    }
                                    return new BadRequestObjectResult("Invalid skillChannelId");
                                }
                            }
                        }
                        return new BadRequestObjectResult("Json Payload is invalid");
                        //return new BadRequestResult();
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
