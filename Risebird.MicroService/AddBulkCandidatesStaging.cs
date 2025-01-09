using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Data.RbAPIDataModels;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Risebird.MicroService
{
    public class AddBulkCandidatesStaging
    {
        private readonly IHttpRequestHelper _httpRequestHelper;
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IClientService _clientService;
        private readonly ICandidateService _candidateService;
        private readonly IIRFRepository _irfRepository;
        private readonly IIRFService _irfService;
        private readonly ILogger log;

        public AddBulkCandidatesStaging(ILoggerFactory loggerFactory, IHttpRequestHelper httpRequestHelper, IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger,
            IClientService clientService, ICandidateService candidateService, IIRFRepository irfRepository, IIRFService irfService)
        {
            _httpRequestHelper = httpRequestHelper;
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _clientService = clientService;
            _candidateService = candidateService;
            _irfRepository = irfRepository;
            _irfService = irfService;
            log = loggerFactory.CreateLogger<AddBulkCandidatesStaging>();
        }
        [Function("AddBulkCandidatesStaging")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "Staging/Candidates/Add")] HttpRequestData req)
        {
            HttpResponseData response = req.CreateResponse();
            try
            {
                log.LogInformation($"AddBulkCandidates : C# HTTP trigger function invoked at {DateTime.Now}");
                var tenantId = req.Headers.GetValues("tenant-id").FirstOrDefault();
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

                                log.LogInformation($"AddBulkCandidates : C# HTTP trigger function executed at {DateTime.Now}");

                                response.StatusCode = HttpStatusCode.BadRequest;
                                await response.WriteAsJsonAsync(errorResponse);
                                return response;
                            }
                            //find the name of the skill channel based on the request parameters;
                            //var irf = await _irfRepository.GetIRFBasedOnTuringsJobId((reqData.jobCountry.ToLower().Contains("onsite")? 2 :1), reqData.jobId, reqData.planType);
                            var result = await _irfService.AddMultipleCandidatesViaApi(reqData.CandidateList, reqData.requestedBy, true, reqData.jobId, isStaging: true);
                            if (result != null)
                            {
                                result.performedAt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ");
                                response.StatusCode = HttpStatusCode.OK;
                                await response.WriteAsJsonAsync(result);
                                return response;
                            }
                            else
                            {
                                response.StatusCode = HttpStatusCode.InternalServerError;
                                return response;
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
                        response.StatusCode = HttpStatusCode.BadRequest;
                        await response.WriteAsJsonAsync(errorResponserequest);
                        return response;
                    }
                    response.StatusCode = HttpStatusCode.Unauthorized;
                    return response;
                }
                response.StatusCode = HttpStatusCode.BadRequest;
                return response;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                log.LogInformation($"AddBulkCandidates : C# HTTP trigger function threw exception with message {ex.Message} at {DateTime.Now}");
                response.StatusCode = HttpStatusCode.InternalServerError;
                return response;
            }
        }
    }
}
