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
using Microsoft.IdentityModel.Tokens;
using System.Web.Http;
using System.Reflection;

namespace Risebird.MicroService
{
    public class AddBulkCandidates
    {
        private readonly IHttpRequestHelper _httpRequestHelper;
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IClientService _clientService;
        private readonly ICandidateService _candidateService;
        private readonly IIRFRepository _irfRepository;
        private readonly IIRFService _irfService;

        public AddBulkCandidates(IHttpRequestHelper httpRequestHelper, IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger,
            IClientService clientService, ICandidateService candidateService, IIRFRepository irfRepository, IIRFService irfService)
        {
            _httpRequestHelper = httpRequestHelper;
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _clientService = clientService;
            _candidateService = candidateService;
            _irfRepository = irfRepository;
            _irfService = irfService;
        }
        [FunctionName("AddBulkCandidates")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "Candidates/Add")] HttpRequest req,
            ILogger log)
        {
            try
            {
                log.LogInformation($"AddBulkCandidates : C# HTTP trigger function invoked at {DateTime.Now}");
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
                        var reqData = JsonConvert.DeserializeObject<AddCandidatesRequest>(requestBody);
                        log.LogInformation("C# HTTP trigger function processed a request.");
                        var details = new Dictionary<string, string>();
                        if (reqData != null)
                        {
                            if (string.IsNullOrEmpty(reqData.jobCountry) || string.IsNullOrEmpty(reqData.jobId) || string.IsNullOrEmpty(reqData.requestedBy))
                            {
                                var detailsError = new Dictionary<string, string>();
                                
                                if (string.IsNullOrEmpty(reqData.jobCountry))
                                {
                                    detailsError.Add("jobCountry", "This field is required.");
                                }
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

                                log.LogInformation($"AddBulkCandidates : C# HTTP trigger function executed at {DateTime.Now}");

                                return new BadRequestObjectResult(errorResponse);
                            }
                            //find the name of the skill channel based on the request parameters;
                            var irf = await _irfRepository.GetIRFBasedOnTuringsJobId((reqData.jobCountry.ToLower().Contains("onsite")? 2 :1), reqData.jobId, reqData.planType);
                            var result = await _irfService.AddMultipleCandidatesViaApi(reqData.CandidateList, reqData.requestedBy, true, irf.IRFId);
                            if(result != null)
                            {
                                result.performedAt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ");
                                return new OkObjectResult(result);
                            }
                            else
                            {
                                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
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
                return new BadRequestResult();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                log.LogInformation($"AddBulkCandidates : C# HTTP trigger function threw exception with message {ex.Message} at {DateTime.Now}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
