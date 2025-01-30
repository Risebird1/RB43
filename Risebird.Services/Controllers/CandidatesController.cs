using Microsoft.AspNetCore.Mvc;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Data.RbAPIDataModels;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;

namespace Risebird.Services.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CandidatesController : ControllerBase
    {
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IClientService _clientService;
        private readonly IIRFService _irfService;

        public CandidatesController(IApplicationErrorLogger applicationErrorLogger, IClientService clientService, IIRFService irfService)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _clientService = clientService;
            _irfService = irfService;
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
    }
}
