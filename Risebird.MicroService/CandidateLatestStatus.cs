using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Data.RbAPIDataModels;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.MicroService
{
    public class CandidateLatestStatus
    {
        private readonly IHttpRequestHelper _httpRequestHelper;
        private readonly IConfiguration _configuration;
        private readonly ICandidateRepository _candidateRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public CandidateLatestStatus(IHttpRequestHelper httpRequestHelper, IConfiguration configuration, IInterviewerService interviewerService,
            IApplicationErrorLogger applicationErrorLogger, ICandidateRepository candidateRepository)
        {
            _httpRequestHelper = httpRequestHelper;
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _candidateRepository = candidateRepository;
            //employementapikey = _configuration["EmploymentVerfication:apikey"];
        }

        [FunctionName("CandidateLatestStatus")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                log.LogInformation($"CandidateLatestStatus : C# HTTP trigger function invoked at {DateTime.Now}");
                var tenantId = req.Headers["tenant-id"].ToString();
                
                //check if the tenant Id belongs to the client
                bool isAuthorizedTenant = true;
                if (isAuthorizedTenant)
                {
                    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                    log.LogInformation(requestBody);

                    if(!string.IsNullOrEmpty(requestBody))
                    {
                        CandidateLatestStatusRequest reqData = JsonConvert.DeserializeObject<CandidateLatestStatusRequest>(requestBody);

                        if (reqData == null || string.IsNullOrEmpty(reqData.skillChannelId) || string.IsNullOrEmpty(reqData.candidateEmailId) || string.IsNullOrEmpty(reqData.requestedBy))
                        {
                            log.LogInformation($"CandidateLatestStatus : C# HTTP trigger function executed at {DateTime.Now}");

                            return new BadRequestResult();
                        }

                        CandidateStatus candidateStatus = await _candidateRepository.GetCandidateLatestStatusForApi(reqData.skillChannelId, reqData.candidateEmailId);
                        
                        if (candidateStatus != null)
                        {
                            var response = new CandidateLatestStatusResponse
                            {
                                skillChannelId = reqData.skillChannelId,
                                candidateEmailId = reqData.requestedBy,
                                retrievedAt = DateTime.Now.ToString(),
                                currentStatusUpdatedAt = candidateStatus.Date.ToString(),

                            };
                            if (candidateStatus.Status == (int)CandidateStatusOptions.MeetingStatus || candidateStatus.Status == (int)CandidateStatusOptions.InterviewStatus)
                            {
                                response.currentStatus = candidateStatus.Comments;
                            }
                            else
                            {
                                response.currentStatus = candidateStatus.StatusValue;
                            }
                            if (candidateStatus.InterviewId.HasValue && candidateStatus.InterviewId.Value > 0)
                            {
                                response.interviewDate = candidateStatus.interviewDate.ToString();
                            }
                            else
                            {
                                response.interviewDate = "NA";
                            }
                            var result = JsonConvert.SerializeObject(response);
                            log.LogInformation($"CandidateLatestStatus : C# HTTP trigger function executed at {DateTime.Now}");
                            return new OkObjectResult(result);

                        }
                        else
                        {
                            log.LogInformation($"CandidateLatestStatus : C# HTTP trigger function executed at {DateTime.Now}");
                            return new NotFoundObjectResult(requestBody);
                        }

                    }
                    else
                    {
                        return new BadRequestResult();
                    }
                    
                    
                }

                log.LogInformation($"CandidateLatestStatus : C# HTTP trigger function executed at {DateTime.Now}");
                return new UnauthorizedResult();
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
