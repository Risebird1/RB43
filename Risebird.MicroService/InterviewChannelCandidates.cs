using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data.RbAPIDataModels;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
        private readonly IEncryptionHelper _encryptionHelper;
        private readonly ILogger log;
        public InterviewChannelCandidates(ILoggerFactory loggerFactory, IHttpRequestHelper httpRequestHelper, IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger,
            IClientService clientService, ICandidateService candidateService, IIRFRepository irfRepository, IEncryptionHelper encryptionHelper)
        {
            _httpRequestHelper = httpRequestHelper;
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _clientService = clientService;
            _candidateService = candidateService;
            _irfRepository = irfRepository;
            _encryptionHelper = encryptionHelper;
            log = loggerFactory.CreateLogger<InterviewChannelCandidates>();
        }
        [Function("InterviewChannelCandidates")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "InterviewChannel/Candidates")] HttpRequestData req)
        {
            try
            {
                log.LogInformation($"InterviewChannelCandidates : C# HTTP trigger function invoked at {DateTime.Now}");
                log.LogInformation($"InterviewChannelCandidates :{req}");
                log.LogInformation($"InterviewChannelCandidates :{req.Headers}");
                log.LogInformation($"InterviewChannelCandidates :{req.Body}");
                var tenantId = req.Headers.GetValues("tenant-id").ToString();
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
                        log.LogInformation($"InterviewChannelCandidates : TenantId is verified at {DateTime.Now}");
                        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                        log.LogInformation(requestBody);
                        var reqData = JsonConvert.DeserializeObject<CandidateLatestStatusRequest>(requestBody);
                        log.LogInformation("C# HTTP trigger function processed a request.");
                        var details = new Dictionary<string, string>();
                        if (reqData != null)
                        {
                            if (string.IsNullOrEmpty(reqData.type) || string.IsNullOrEmpty(reqData.requestedBy))
                            {
                                if (string.IsNullOrEmpty(reqData.type))
                                {
                                    details.Add("type", "This field is required.");
                                }
                                if (string.IsNullOrEmpty(reqData.requestedBy))
                                {
                                    details.Add("requestedBy", "This field is required.");
                                }
                                if (!string.IsNullOrEmpty(reqData.type))
                                {
                                    if (reqData.type.ToLower() == "alldata")
                                    {
                                        if (string.IsNullOrEmpty(reqData.skillChannelId) && string.IsNullOrEmpty(reqData.jobId))
                                        {
                                            details.Add("skillChannelId or jobId", "This field is required.");
                                        }
                                    }
                                    else if (reqData.type.ToLower() == "daterange")
                                    {
                                        if (reqData.startDate == null || reqData.endDate == null)
                                        {
                                            details.Add("DateFields", "Date Fields are invalid");
                                        }
                                    }
                                    else
                                    {
                                        details.Add("type", "type is invalid");
                                    }
                                }
                                var errorResponse = new ErrorResponse
                                {
                                    Error = "Invalid request parameters",
                                    Details = details
                                };
                                log.LogInformation($"CandidateLatestStatus : C# HTTP trigger function executed at {DateTime.Now}");
                                return new BadRequestObjectResult(errorResponse);
                            }
                            if (!string.IsNullOrEmpty(reqData.type))
                            {
                                if (reqData.type.ToLower() == "alldata")
                                {
                                    if (string.IsNullOrEmpty(reqData.skillChannelId) && string.IsNullOrEmpty(reqData.jobId))
                                    {
                                        details.Add("skillChannelId or jobId", "This field is required.");
                                    }
                                }
                                else if (reqData.type.ToLower() == "daterange")
                                {
                                    if (reqData.startDate == null || reqData.endDate == null)
                                    {
                                        details.Add("DateFields", "Date Fields are invalid");
                                    }
                                }
                                else
                                {
                                    details.Add("type", "type is invalid");
                                }
                                if (details.Count > 0)
                                {
                                    var errorResponse = new ErrorResponse
                                    {
                                        Error = "Invalid request parameters",
                                        Details = details
                                    };
                                    log.LogInformation($"CandidateLatestStatus : C# HTTP trigger function executed at {DateTime.Now}");
                                    return new BadRequestObjectResult(errorResponse);
                                }
                            }
                            var model = new CandidateInfoResponse();
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
                            //Update according to client user role based
                            var result = await _candidateService.FetchInterviewChannelCandidateInformation(irfid, clientDetails.id, reqData.requestedBy, reqData.startDate, reqData.endDate, reqData.jobId);
                            if (result == null)
                            {
                                result = new List<InterviewChannelDetails>();
                            }
                            model.requestedBy = reqData.requestedBy;
                            model.retrievedAt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ");
                            foreach (var item in result)
                            {
                                if (item.InterviewStatusTwo != null && item.InterviewStatusTwo > 0)
                                {
                                    item.InterviewStatusTwoDesc = ((InterviewStatus)Convert.ToInt32(item.InterviewStatusTwo)).GetDescription();
                                }
                                if (item.MeetingStatusTwo > 0)
                                {
                                    item.MeetingStatusTwoDesc = ((MeetingStatus)Convert.ToInt32(item.MeetingStatusTwo)).GetDescription();
                                    if (string.IsNullOrEmpty(item.InterviewStatusTwoDesc))
                                    {
                                        item.InterviewStatusTwoDesc = item.MeetingStatusTwoDesc;
                                    }
                                }
                                //Check for meeting status complete
                                var executiveurl = string.Empty;
                                var meetingstatus = !string.IsNullOrWhiteSpace(item.MeetingStatus.ToString()) ? Convert.ToInt32(item.MeetingStatus) : 0;
                                if (meetingstatus == 4 || meetingstatus == 6 || meetingstatus == 7 || meetingstatus == 8)
                                {
                                    executiveurl = await GenerateExecutiveReportURl("https://portal.risebird.io/", Convert.ToInt32(item.id), item.irfintid);
                                }
                                //Create JD Based Info
                                List<SkillDetails> _skills = new List<SkillDetails>();
                                var _questions = new List<QuestionCommentsDetails>();
                                foreach (var _question in item.questions.Where(x => x.skillSet == 1).ToList())
                                {
                                    _questions.Add(new QuestionCommentsDetails
                                    {
                                        question = _question.question,
                                        skillName = _question.skillName,
                                        rating = ((QuestionsLevel)Convert.ToInt32(_question.rating)).GetDescription()
                                    });
                                }
                                var _skillset = new List<SkillsSetsDetails>();
                                foreach (var _skilldetail in item.skillset.Where(x => x.skillSet == 1).ToList())
                                {
                                    var score = string.Empty;
                                    if (_skilldetail.WeightageRequired > 0)
                                    {
                                        score = (_skilldetail.WeightageActual * 100 / _skilldetail.WeightageRequired).ToString();
                                    }
                                    _skillset.Add(new SkillsSetsDetails
                                    {
                                        skillName = _skilldetail.skillName,
                                        EvaluatedAt = _skilldetail.EvaluatedAt,
                                        score = score
                                    });
                                }
                                _skills.Add(new SkillDetails
                                {
                                    isPrimary = true,
                                    skillName = item.JDTiltle,
                                    status = item.CandidateLatestStatus,
                                    techscore = item.TechScoreone,
                                    interviewerComment = item.InterviewerComment,
                                    questions = _questions,
                                    skillset = _skillset
                                });
                                //In case of multiStack
                                if (item.numberOfTechStacks == 2)
                                {
                                    _questions = new List<QuestionCommentsDetails>();
                                    foreach (var _question in item.questions.Where(x => x.skillSet == 2).ToList())
                                    {
                                        _questions.Add(new QuestionCommentsDetails
                                        {
                                            question = _question.question,
                                            skillName = _question.skillName,
                                            rating = ((QuestionsLevel)Convert.ToInt32(_question.rating)).GetDescription()
                                        });
                                    }
                                    _skillset = new List<SkillsSetsDetails>();
                                    foreach (var _skilldetail in item.skillset.Where(x => x.skillSet == 2).ToList())
                                    {
                                        var score = string.Empty;
                                        if (_skilldetail.WeightageRequired > 0)
                                        {
                                            score = (_skilldetail.WeightageActual * 100 / _skilldetail.WeightageRequired).ToString();
                                        }
                                        _skillset.Add(new SkillsSetsDetails
                                        {
                                            skillName = _skilldetail.skillName,
                                            EvaluatedAt = _skilldetail.EvaluatedAt,
                                            score = score
                                        });
                                    }
                                    _skills.Add(new SkillDetails
                                    {
                                        isPrimary = false,
                                        skillName = item.JDTitleTwo,
                                        status = item.InterviewStatusTwoDesc,
                                        techscore = item.TechScoretwo,
                                        interviewerComment = item.InterviewerCommentTwo,
                                        questions = _questions,
                                        skillset = _skillset
                                    });
                                }
                                model.result.Add(new CandidateStatusResponse()
                                {
                                    jobId = item.jobId,
                                    skillChannelId = item.IRFId,
                                    interviewDate = item.interviewDate?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                                    candidateEmailId = item.candidateEmail,
                                    interviewer = item.InterviewerCodedName,
                                    reportUrl = executiveurl,
                                    communicationScore = item.CommunicationalProficiency,
                                    videoUrl = item.VideoLink,
                                    skills = _skills,
                                    createdOn = item.createdOn?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                                    currentStatusUpdatedAt = item.currentStatusUpdatedAt?.ToString("yyyy-MM-ddTHH:mm:ssZ")
                                });
                            }
                            if (!string.IsNullOrWhiteSpace(reqData.status))
                            {
                                if (reqData.status.ToLower() == "completed")
                                {
                                    var _select = model.result.Where(x => x.skills.Any(y => !string.IsNullOrWhiteSpace(y.status) ? y.status.ToLower().Contains("select") : false)).ToList();
                                    var _reject = model.result.Where(x => x.skills.Any(y => !string.IsNullOrWhiteSpace(y.status) ? y.status.ToLower().Contains("reject") : false)).ToList();
                                    var _proxy = model.result.Where(x => x.skills.Any(y => !string.IsNullOrWhiteSpace(y.status) ? y.status.ToLower().Contains("proxy") : false)).ToList();
                                    model.result = _select.Union(_reject).Union(_proxy).ToList();
                                }
                                else if (reqData.status.ToLower() == "scheduled")
                                {
                                    model.result = model.result.Where(x => x.skills.Any(y => !string.IsNullOrWhiteSpace(y.status) ? y.status.ToLower().Contains("scheduled") : false)).ToList();
                                }
                                else if (reqData.status.ToLower() == "pending")
                                {
                                    model.result = model.result.Where(x => x.skills.Any(y => !string.IsNullOrWhiteSpace(y.status) ? y.status.ToLower().Contains("pending") : false)).ToList();
                                }
                            }
                            if (reqData.type.ToLower() == "alldata" || reqData.type.ToLower() == "daterange")
                            {
                                //Get skill channel all Candidate Info and return
                                return new OkObjectResult(model);
                            }
                            else
                            {
                                details.Add("type", "type is invalid.");
                            }
                        }
                        else
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
        public async Task<string> GenerateExecutiveReportURl(string portalurl, int interviewid, int irfid)
        {
            var meetingid = await _encryptionHelper.EncryptIntegerValue(interviewid, "ExecutiveReport");
            var irf = await _encryptionHelper.EncryptIntegerValue(irfid, "ExecutiveReport");
            var reportlink = portalurl + "Interview/ExecutiveReport?meetingid=" + meetingid + "&irf=" + irf;
            return reportlink;
        }
    }
}
