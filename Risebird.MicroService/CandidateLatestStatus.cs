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
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
//using Microsoft.Azure.Documents.SystemFunctions;

namespace Risebird.MicroService
{
    public class CandidateLatestStatus
    {
        private readonly IHttpRequestHelper _httpRequestHelper;
        private readonly IConfiguration _configuration;
        private readonly ICandidateRepository _candidateRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IClientService _clientService;
        private readonly IEncryptionHelper _encryptionHelper;
        private readonly ILogger log;

        public CandidateLatestStatus(ILoggerFactory loggerFactory, IHttpRequestHelper httpRequestHelper, IConfiguration configuration, IInterviewerService interviewerService,
            IApplicationErrorLogger applicationErrorLogger, ICandidateRepository candidateRepository, IClientService clientService, IEncryptionHelper encryptionHelper)
        {
            _httpRequestHelper = httpRequestHelper;
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _candidateRepository = candidateRepository;
            _clientService = clientService;
            _encryptionHelper = encryptionHelper;
            log = loggerFactory.CreateLogger<CandidateLatestStatus>();
        }

        [Function("CandidateLatestStatus")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestData req)
        {
            try
            {
                log.LogInformation($"CandidateLatestStatus : C# HTTP trigger function invoked at {DateTime.Now}");
                var tenantId = req.Headers.GetValues("tenant-id").FirstOrDefault();

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

                    if (!string.IsNullOrEmpty(requestBody))
                    {
                        CandidateLatestStatusRequest reqData = JsonConvert.DeserializeObject<CandidateLatestStatusRequest>(requestBody);

                        if (reqData == null || (string.IsNullOrEmpty(reqData.skillChannelId) && string.IsNullOrEmpty(reqData.jobId)) || string.IsNullOrEmpty(reqData.candidateEmailId) || string.IsNullOrEmpty(reqData.requestedBy))
                        {
                            var details = new Dictionary<string, string>();
                            if (reqData == null)
                            {
                                details.Add("body", "Request Body is null.");
                            }
                            if (string.IsNullOrEmpty(reqData.skillChannelId) && string.IsNullOrEmpty(reqData.jobId))
                            {
                                details.Add("skillChannelId/jobId", "Either of 'skillChannelId' or 'jobId' fields is required.");
                            }
                            if (string.IsNullOrEmpty(reqData.candidateEmailId))
                            {
                                details.Add("candidateEmailId", "This field is required.");
                            }
                            if (string.IsNullOrEmpty(reqData.requestedBy))
                            {
                                details.Add("requestedBy", "This field is required.");
                            }
                            var errorResponse = new ErrorResponse
                            {
                                Error = "Invalid request parameters.",
                                Details = details
                            };

                            log.LogInformation($"CandidateLatestStatus : C# HTTP trigger function executed at {DateTime.Now}");

                            return new BadRequestObjectResult(errorResponse);
                        }

                        var results = await _candidateRepository.GetCandidateLatestStatusForApi(reqData.skillChannelId, reqData.candidateEmailId, reqData.requestedBy, reqData.jobId);

                        var model = new CandidateInfoResponse();
                        model.requestedBy = reqData.requestedBy;
                        model.retrievedAt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ");
                        if (results != null)
                        {
                            foreach (var result in results)
                            {
                                if (result.InterviewStatusTwo != null && result.InterviewStatusTwo > 0)
                                {
                                    result.InterviewStatusTwoDesc = ((InterviewStatus)Convert.ToInt32(result.InterviewStatusTwo)).GetDescription();
                                }
                                if (result.MeetingStatusTwo > 0)
                                {
                                    result.MeetingStatusTwoDesc = ((MeetingStatus)Convert.ToInt32(result.MeetingStatusTwo)).GetDescription();
                                    if (string.IsNullOrEmpty(result.InterviewStatusTwoDesc))
                                    {
                                        result.InterviewStatusTwoDesc = result.MeetingStatusTwoDesc;
                                    }
                                }
                                //Check for meeting status complete
                                var executiveurl = string.Empty;
                                var meetingstatus = !string.IsNullOrWhiteSpace(result.MeetingStatus.ToString()) ? Convert.ToInt32(result.MeetingStatus) : 0;
                                if (meetingstatus == 4 || meetingstatus == 6 || meetingstatus == 7 || meetingstatus == 8)
                                {
                                    executiveurl = await GenerateExecutiveReportURl("https://portal.risebird.io/", Convert.ToInt32(result.id), result.irfintid);
                                }
                                //Create JD Based Info
                                List<SkillDetails> _skills = new List<SkillDetails>();
                                var _questions = new List<QuestionCommentsDetails>();
                                foreach (var _question in result.questions.Where(x => x.skillSet == 1).ToList())
                                {
                                    _questions.Add(new QuestionCommentsDetails
                                    {
                                        question = _question.question,
                                        skillName = _question.skillName,
                                        rating = ((QuestionsLevel)Convert.ToInt32(_question.rating)).GetDescription()
                                    });
                                }
                                var _skillset = new List<SkillsSetsDetails>();
                                foreach (var _skilldetail in result.skillset.Where(x => x.skillSet == 1).ToList())
                                {
                                    _skillset.Add(new SkillsSetsDetails
                                    {
                                        skillName = _skilldetail.skillName,
                                        EvaluatedAt = _skilldetail.EvaluatedAt,
                                        score = (_skilldetail.WeightageActual * 100 / _skilldetail.WeightageRequired).ToString()
                                    });
                                }
                                _skills.Add(new SkillDetails
                                {
                                    isPrimary = true,
                                    skillName = result.JDTiltle,
                                    status = result.CandidateLatestStatus,
                                    techscore = result.TechScoreone,
                                    interviewerComment = result.InterviewerComment,
                                    questions = _questions,
                                    skillset = _skillset
                                });
                                //In case of multiStack
                                if (result.numberOfTechStacks == 2)
                                {
                                    _questions = new List<QuestionCommentsDetails>();
                                    foreach (var _question in result.questions.Where(x => x.skillSet == 2).ToList())
                                    {
                                        _questions.Add(new QuestionCommentsDetails
                                        {
                                            question = _question.question,
                                            skillName = _question.skillName,
                                            rating = ((QuestionsLevel)Convert.ToInt32(_question.rating)).GetDescription()
                                        });
                                    }
                                    _skillset = new List<SkillsSetsDetails>();
                                    foreach (var _skilldetail in result.skillset.Where(x => x.skillSet == 2).ToList())
                                    {
                                        _skillset.Add(new SkillsSetsDetails
                                        {
                                            skillName = _skilldetail.skillName,
                                            EvaluatedAt = _skilldetail.EvaluatedAt,
                                            score = (_skilldetail.WeightageActual * 100 / _skilldetail.WeightageRequired).ToString()
                                        });
                                    }
                                    _skills.Add(new SkillDetails
                                    {
                                        isPrimary = false,
                                        skillName = result.JDTitleTwo,
                                        status = result.InterviewStatusTwoDesc,
                                        techscore = result.TechScoretwo,
                                        interviewerComment = result.InterviewerCommentTwo,
                                        questions = _questions,
                                        skillset = _skillset
                                    });
                                }
                                model.result.Add(new CandidateStatusResponse
                                {
                                    jobId = result.jobId,
                                    skillChannelId = result.IRFId,
                                    interviewDate = result.interviewDate?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                                    candidateEmailId = result.candidateEmail,
                                    interviewer = result.InterviewerCodedName,
                                    reportUrl = executiveurl,
                                    communicationScore = result.CommunicationalProficiency,
                                    videoUrl = result.VideoLink,
                                    skills = _skills,
                                    createdOn = result.createdOn?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                                    currentStatusUpdatedAt = result.currentStatusUpdatedAt?.ToString("yyyy-MM-ddTHH:mm:ssZ")
                                });
                            }
                            //if (result.InterviewStatusTwo != null && result.InterviewStatusTwo > 0)
                            //{
                            //    result.InterviewStatusTwoDesc = ((InterviewStatus)Convert.ToInt32(result.InterviewStatusTwo)).GetDescription();
                            //}
                            //if (result.MeetingStatusTwo > 0)
                            //{
                            //    result.MeetingStatusTwoDesc = ((MeetingStatus)Convert.ToInt32(result.MeetingStatusTwo)).GetDescription();
                            //    if (string.IsNullOrEmpty(result.InterviewStatusTwoDesc))
                            //    {
                            //        result.InterviewStatusTwoDesc = result.MeetingStatusTwoDesc;
                            //    }
                            //}
                            ////Check for meeting status complete
                            //var executiveurl = string.Empty;
                            //var meetingstatus = !string.IsNullOrWhiteSpace(result.MeetingStatus.ToString()) ? Convert.ToInt32(result.MeetingStatus) : 0;
                            //if (meetingstatus == 4 || meetingstatus == 6 || meetingstatus == 7 || meetingstatus == 8)
                            //{
                            //    executiveurl = $"https://portal.risebird.io/Interview/ExecutiveReport?interviewId={result.id}&irfId={result.irfintid}&isClient=true";
                            //}
                            ////Create JD Based Info
                            //List<SkillDetails> _skills = new List<SkillDetails>();
                            //var _questions = new List<QuestionCommentsDetails>();
                            //foreach (var _question in result.questions.Where(x => x.skillSet == 1).ToList())
                            //{
                            //    _questions.Add(new QuestionCommentsDetails
                            //    {
                            //        question = _question.question,
                            //        skillName = _question.skillName,
                            //        rating = ((QuestionsLevel)Convert.ToInt32(_question.rating)).GetDescription()
                            //    });
                            //}
                            //_skills.Add(new SkillDetails
                            //{
                            //    isPrimary = true,
                            //    skillName = result.JDTiltle,
                            //    status = result.CandidateLatestStatus,
                            //    techscore = result.TechScoreone,
                            //    interviewerComment = result.InterviewerComment,
                            //    questions = _questions
                            //});
                            ////In case of multiStack
                            //if (result.numberOfTechStacks == 2)
                            //{
                            //    _questions = new List<QuestionCommentsDetails>();
                            //    foreach (var _question in result.questions.Where(x => x.skillSet == 2).ToList())
                            //    {
                            //        _questions.Add(new QuestionCommentsDetails
                            //        {
                            //            question = _question.question,
                            //            skillName = _question.skillName,
                            //            rating = ((QuestionsLevel)Convert.ToInt32(_question.rating)).GetDescription()
                            //        });
                            //    }
                            //    _skills.Add(new SkillDetails
                            //    {
                            //        isPrimary = false,
                            //        skillName = result.JDTitleTwo,
                            //        status = result.InterviewStatusTwoDesc,
                            //        techscore = result.TechScoretwo,
                            //        interviewerComment = result.InterviewerCommentTwo,
                            //        questions = _questions
                            //    });
                            //}
                            //var model = new CandidateInfoResponse
                            //{
                            //    skillChannelId = result.IRFId,
                            //    interviewDate = result.interviewDate?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                            //    candidateEmailId = result.candidateEmail,
                            //    interviewer = result.InterviewerCodedName,
                            //    reportUrl = executiveurl,
                            //    communicationScore = result.CommunicationalProficiency,
                            //    videoUrl = result.VideoLink,
                            //    requestedBy = reqData.requestedBy,
                            //    retrievedAt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                            //    skills = _skills,
                            //    createdOn = result.createdOn?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                            //    currentStatusUpdatedAt = result.currentStatusUpdatedAt?.ToString("yyyy-MM-ddTHH:mm:ssZ")
                            //};
                            //if (result.CandidateLatestStatus == "FUTURESCHEDULE")
                            //{
                            //    //Schdeuled
                            //    model.currentStatus = "Scheduled";
                            //}
                            //else
                            //{
                            //    model.currentStatus = result.CandidateLatestStatus;
                            //}
                            //if (!string.IsNullOrWhiteSpace(reqData.status))
                            //{
                            //    if (result.InterviewStatus != null)
                            //    {
                            //        //completed
                            //    }
                            //    if (result.CandidateLatestStatus == "FUTURESCHEDULE")
                            //    {
                            //        //Schdeuled
                            //    }
                            //}
                            //else
                            //{
                            //    model.result.Add(new CandidateLatestStatusResponse()
                            //    {
                            //        skillChannelId = result.IRFId,
                            //        interviewDate = result.interviewDate?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                            //        candidateEmailId = result.candidateEmail,
                            //        interviewer = result.InterviewerCodedName,
                            //        reportUrl = executiveurl,
                            //        communicationScore = result.CommunicationalProficiency,
                            //        videoUrl = result.VideoLink,
                            //        skills = _skills
                            //    });
                            //}
                            return new OkObjectResult(model);
                        }
                        else
                        {

                            var details = new Dictionary<string, string>
                            {
                                { "Invalid request", "Invalid request" }
                            };
                            var errorResponse = new ErrorResponse
                            {
                                Error = "Invalid request.",
                                Details = details
                            };

                            log.LogInformation($"CandidateLatestStatus : C# HTTP trigger function executed at {DateTime.Now}");

                            return new BadRequestObjectResult(errorResponse);

                        }



                        //if (candidateStatus != null)
                        //{
                        //    var response = new CandidateLatestStatusResponse
                        //    {
                        //        skillChannelId = reqData.skillChannelId,
                        //        candidateEmailId = reqData.candidateEmailId,
                        //        retrievedAt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        //        currentStatusUpdatedAt = candidateStatus.Date.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        //        requestedBy = reqData.requestedBy,
                        //        interviewer = candidateStatus.interviewer
                        //    };
                        //    if (candidateStatus.Status == (int)CandidateStatusOptions.MeetingStatus || candidateStatus.Status == (int)CandidateStatusOptions.InterviewStatus)
                        //    {
                        //        response.currentStatus = candidateStatus.Comments;
                        //    }
                        //    else
                        //    {
                        //        response.currentStatus = candidateStatus.StatusValue;
                        //    }
                        //    if (candidateStatus.InterviewId.HasValue && candidateStatus.InterviewId.Value > 0)
                        //    {
                        //        response.interviewDate = candidateStatus.interviewDate.ToString();
                        //        var executiveurl = $"https://portal.risebird.io/Interview/ExecutiveReport?interviewId={candidateStatus.InterviewId.Value}&irfId={candidateStatus.IRFId}&isClient=true";
                        //        response.reportUrl = executiveurl;
                        //    }
                        //    else
                        //    {
                        //        response.interviewDate = string.Empty;
                        //        response.reportUrl = string.Empty;
                        //    }
                        //    var result = JsonConvert.SerializeObject(response);
                        //    log.LogInformation($"CandidateLatestStatus : C# HTTP trigger function executed at {DateTime.Now}");
                        //    return new OkObjectResult(result);

                        //}
                        //else
                        //{
                        //    log.LogInformation($"CandidateLatestStatus : C# HTTP trigger function executed at {DateTime.Now}");
                        //    return new NotFoundObjectResult(requestBody);
                        //}

                    }
                    else
                    {
                        var details = new Dictionary<string, string>
                        {
                            { "body", "Request Body is null." }
                        };
                        var errorResponse = new ErrorResponse
                        {
                            Error = "Invalid request.",
                            Details = details
                        };

                        log.LogInformation($"CandidateLatestStatus : C# HTTP trigger function executed at {DateTime.Now}");

                        return new BadRequestObjectResult(errorResponse);
                    }


                }

                log.LogInformation($"CandidateLatestStatus : C# HTTP trigger function executed at {DateTime.Now}");
                return new UnauthorizedResult();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                log.LogInformation($"CandidateLatestStatus : C# HTTP trigger function threw exception with message {ex.Message} at {DateTime.Now}");
                //var errorResponse = new ErrorResponseBasic
                //{
                //    Error = "Invalid request parameters",
                //    Details = "An unexpected error occurred. Please try again later."
                //};
                //return new InternalServerErrorResult(errorResponse);
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
