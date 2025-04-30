using Microsoft.AspNetCore.Mvc;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data.RbAPIDataModels;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;

namespace Risebird.Services.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CandidateStatusController : ControllerBase
    {
        private readonly IClientService _clientService;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IEncryptionHelper _encryptionHelper;
        private readonly ICandidateService _candidateService;
        private readonly IIRFRepository _irfRepository;
        private readonly ICandidateRepository _candidateRepository;

        public CandidateStatusController(IClientService clientService, IApplicationErrorLogger applicationErrorLogger, IEncryptionHelper encryptionHelper, ICandidateService candidateService, IIRFRepository irfRepository, ICandidateRepository candidateRepository)
        {
            _clientService = clientService;
            _applicationErrorLogger = applicationErrorLogger;
            _encryptionHelper = encryptionHelper;
            _candidateService = candidateService;
            _irfRepository = irfRepository;
            _candidateRepository = candidateRepository;
        }


        /// <summary>
        /// ProjectId or Skill Channel Id Candidate List
        /// Date Range or Complete Data
        /// Status
        /// </summary>
        /// <param name="reqData"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("CandidateList")]
        public async Task<IActionResult> CandidateList(CandidateLatestStatusRequest reqData)
        {
            try
            {
                var functionkey = Request.Headers["x-functions-key"].ToString();
                if (functionkey != "b8AiZsRKltzzEvBBip2Bm1gAOWyuK0rjxSehN3aDE0PzAzFuqRFi1A==")
                {
                    return new UnauthorizedResult();
                }
                var tenantId = Request.Headers["tenant-id"].ToString();
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
                                    return new BadRequestObjectResult(errorResponse);
                                }
                            }
                            var model = new CandidateInfoResponse();
                            var irfid = 0;
                            if (!string.IsNullOrEmpty(reqData.skillChannelId))
                            {
                                irfid = _irfRepository.GetIRFIdByIRFNametenantId(reqData.skillChannelId.Trim(), tenantId);
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
                                        Evaluatedscore = _skilldetail.WeightageActual.ToString() + "/" + _skilldetail.WeightageRequired.ToString(),
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
                                    CodeSnippet = item.CodeSnippet,
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
                                            Evaluatedscore = _skilldetail.WeightageActual.ToString() + "/" + _skilldetail.WeightageRequired.ToString(),
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
                                        CodeSnippet = item.CodeSnippetTwo,
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
                                    transcriptUrl = item.TranscriptLink,
                                    skills = _skills,
                                    createdOn = item.createdOn?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                                    createdBy = item.ClientHRAttemptContact,
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
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }



        /// <summary>
        /// Get Candidate Latest Status
        /// </summary>
        /// <param name="reqData"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("CandidateLatestStatus")]
        public async Task<IActionResult> CandidateLatestStatus(CandidateLatestStatusRequest reqData)
        {
            try
            {
                var functionkey = Request.Headers["x-functions-key"].ToString();
                var tenantId = Request.Headers["tenant-id"].ToString();
                if (functionkey != "ypiAr3NN3plzXtdPhwBxUPdzmK-QQmXjWFUWWAYS0bjAAzFubl6qrw==")
                {
                    return new UnauthorizedResult();
                }
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
                    if (reqData != null)
                    {
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
                                    var score = string.Empty;
                                    if (_skilldetail.WeightageRequired > 0)
                                    {
                                        score = (_skilldetail.WeightageActual * 100 / _skilldetail.WeightageRequired).ToString();
                                    }
                                    _skillset.Add(new SkillsSetsDetails
                                    {
                                        skillName = _skilldetail.skillName,
                                        EvaluatedAt = _skilldetail.EvaluatedAt,
                                        Evaluatedscore = _skilldetail.WeightageActual.ToString() + "/" + _skilldetail.WeightageRequired.ToString(),
                                        score = score
                                    });
                                }
                                if (result.CandidateLatestStatus.ToUpper() != "CANCELLED")
                                {
                                    result.Cancellationsource = "";
                                }
                                _skills.Add(new SkillDetails
                                {
                                    isPrimary = true,
                                    skillName = result.JDTiltle,
                                    status = result.CandidateLatestStatus,
                                    Cancellationsource = result.Cancellationsource,
                                    techscore = result.TechScoreone,
                                    interviewerComment = result.InterviewerComment,
                                    CodeSnippet = result.CodeSnippet,
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
                                        var score = string.Empty;
                                        if (_skilldetail.WeightageRequired > 0)
                                        {
                                            score = (_skilldetail.WeightageActual * 100 / _skilldetail.WeightageRequired).ToString();
                                        }
                                        _skillset.Add(new SkillsSetsDetails
                                        {
                                            skillName = _skilldetail.skillName,
                                            EvaluatedAt = _skilldetail.EvaluatedAt,
                                            Evaluatedscore = _skilldetail.WeightageActual.ToString() + "/" + _skilldetail.WeightageRequired.ToString(),
                                            score = score
                                        });
                                    }
                                    _skills.Add(new SkillDetails
                                    {
                                        isPrimary = false,
                                        skillName = result.JDTitleTwo,
                                        status = result.InterviewStatusTwoDesc,
                                        Cancellationsource = result.Cancellationsource,
                                        techscore = result.TechScoretwo,
                                        interviewerComment = result.InterviewerCommentTwo,
                                        CodeSnippet = result.CodeSnippetTwo,
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
                                    transcriptUrl = result.TranscriptLink,
                                    skills = _skills,
                                    createdOn = result.createdOn?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                                    createdBy = result.ClientHRAttemptContact,
                                    currentStatusUpdatedAt = result.currentStatusUpdatedAt?.ToString("yyyy-MM-ddTHH:mm:ssZ")
                                });
                            }
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
                            return new BadRequestObjectResult(errorResponse);
                        }
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
                        return new BadRequestObjectResult(errorResponse);
                    }
                }
                return new UnauthorizedResult();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        private async Task<string> GenerateExecutiveReportURl(string portalurl, int interviewid, int irfid)
        {
            var meetingid = await _encryptionHelper.EncryptIntegerValue(interviewid, "ExecutiveReport");
            var irf = await _encryptionHelper.EncryptIntegerValue(irfid, "ExecutiveReport");
            var reportlink = portalurl + "Interview/ExecutiveReport?meetingid=" + meetingid + "&irf=" + irf;
            return reportlink;
        }
    }
}
