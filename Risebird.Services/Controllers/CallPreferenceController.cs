using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Risebird.Business.CallPreference;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data.RbAPIDataModels;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;

namespace Risebird.Services.Controllers
{
    /// <summary>
    /// Call opt-out webhooks and pre-dial checks. The interviewer calling agent posts to the Interviewer routes
    /// (platform level: churn, out of every MAG); the candidate agent posts to the Candidate routes (IRF level:
    /// cold in that skill channel only). The two never touch each other, even when a phone matches both.
    /// Auth: header x-functions-key or ?key= (Plivo callbacks can't set headers), compared in constant time with
    /// CallPreference:WebhookKey. An empty configured key rejects every request.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class CallPreferenceController : ControllerBase
    {
        private readonly ICallPreferenceRepository _callPreferenceRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IConfiguration _configuration;
        private readonly INotificationHelper _notificationHelper;
        private readonly ICandidateService _candidateService;

        public CallPreferenceController(ICallPreferenceRepository callPreferenceRepository, IApplicationErrorLogger applicationErrorLogger, IConfiguration configuration,
            INotificationHelper notificationHelper, ICandidateService candidateService)
        {
            _callPreferenceRepository = callPreferenceRepository;
            _applicationErrorLogger = applicationErrorLogger;
            _configuration = configuration;
            _notificationHelper = notificationHelper;
            _candidateService = candidateService;
        }

        [HttpPost]
        [Route("Interviewer/OptOut")]
        public async Task<IActionResult> InterviewerOptOut([FromQuery] string? source = null)
        {
            if (!IsAuthorized()) return new UnauthorizedResult();
            try
            {
                var body = await ReadBody();
                var payload = CallOptOutPayload.Parse(body);
                if (payload == null)
                    return BadRequest(new { error = "Invalid request", details = "Body must be a JSON object." });
                if (!payload.IsOptOut)
                    return Ok(new { processed = false, message = "No unsubscribe signal in payload." });

                var phone = payload.Phone;
                var email = payload.Email;
                var userId = payload.UserId;
                if (userId == null && string.IsNullOrEmpty(phone) && string.IsNullOrEmpty(email))
                    return BadRequest(new { error = "Invalid request", details = "userId, phone or email is required." });

                var detected = string.IsNullOrWhiteSpace(source) ? payload.DetectSource() : source.Trim();
                var result = await _callPreferenceRepository.RecordInterviewerCallOptOut(new InterviewerCallOptOutRequest
                {
                    UserId = userId,
                    Phone = phone,
                    EmailId = email,
                    Source = detected,
                    // A Risebird caller posting from the portal / dialer is a human-call opt-out.
                    OptOutSource = string.Equals(detected, "Portal", StringComparison.OrdinalIgnoreCase) ? "HumanCall" : "AICall",
                    ProviderCallId = payload.CallId,
                    Reason = payload.Reason,
                    RawPayload = body,
                });
                return Ok(new { processed = true, optedOut = result.OptedOut, irfId = result.IRFId });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                // 500 so the provider retries.
                return StatusCode(500, new { error = "Failed", details = "Could not record the opt-out." });
            }
        }

        [HttpPost]
        [Route("Candidate/OptOut")]
        public async Task<IActionResult> CandidateOptOut([FromQuery] string? source = null)
        {
            if (!IsAuthorized()) return new UnauthorizedResult();
            try
            {
                var body = await ReadBody();
                var payload = CallOptOutPayload.Parse(body);
                if (payload == null)
                    return BadRequest(new { error = "Invalid request", details = "Body must be a JSON object." });
                if (!payload.IsOptOut)
                    return Ok(new { processed = false, message = "No unsubscribe signal in payload." });

                var candidateId = payload.CandidateId;
                var irfId = payload.IRFId;
                var phone = payload.Phone;
                var email = payload.Email;
                if (candidateId == null && irfId == null && string.IsNullOrEmpty(phone) && string.IsNullOrEmpty(email))
                    return BadRequest(new { error = "Invalid request", details = "candidateId, irfId, phone or email is required." });

                var detected = string.IsNullOrWhiteSpace(source) ? payload.DetectSource() : source.Trim();
                var result = await _callPreferenceRepository.RecordCandidateCallOptOut(new CandidateCallOptOutRequest
                {
                    CandidateId = candidateId,
                    IRFId = irfId,
                    Phone = phone,
                    EmailId = email,
                    Source = detected,
                    OptOutSource = string.Equals(detected, "Portal", StringComparison.OrdinalIgnoreCase) ? "HumanCall" : "AICall",
                    ProviderCallId = payload.CallId,
                    Reason = payload.Reason,
                    RawPayload = body,
                });

                await SendCancellationMails(result.CancelledInterviews);

                if (candidateId == null && irfId == null)
                    return Ok(new
                    {
                        processed = true,
                        optedOut = result.OptedOut,
                        irfId = result.IRFId,
                        warning = "candidateId or irfId is required to apply a candidate opt-out; it was only logged. Pass candidate_id and irf_id as dynamic variables."
                    });
                return Ok(new { processed = true, optedOut = result.OptedOut, irfId = result.IRFId });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return StatusCode(500, new { error = "Failed", details = "Could not record the opt-out." });
            }
        }

        [HttpGet]
        [Route("Interviewer/CanCall")]
        public async Task<IActionResult> InterviewerCanCall([FromQuery] string? phone)
        {
            if (!IsAuthorized()) return new UnauthorizedResult();
            if (string.IsNullOrWhiteSpace(phone))
                return BadRequest(new { error = "Invalid request", details = "phone is required." });
            try
            {
                var result = await _callPreferenceRepository.CanCallContact("Interviewer", phone);
                return Ok(new { canCall = result.CanCall, reason = result.Reason });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return StatusCode(500, new { error = "Failed", details = "Could not check the number." });
            }
        }

        [HttpGet]
        [Route("Candidate/CanCall")]
        public async Task<IActionResult> CandidateCanCall([FromQuery] string? phone, [FromQuery] int? irfId)
        {
            if (!IsAuthorized()) return new UnauthorizedResult();
            if (string.IsNullOrWhiteSpace(phone) || irfId == null || irfId <= 0)
                return BadRequest(new { error = "Invalid request", details = "phone and irfId are required." });
            try
            {
                var result = await _callPreferenceRepository.CanCallContact("Candidate", phone, irfId);
                return Ok(new { canCall = result.CanCall, reason = result.Reason });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return StatusCode(500, new { error = "Failed", details = "Could not check the number." });
            }
        }

        private Task SendCancellationMails(List<CancelledInterview> interviews) =>
            CallOptOutCancellationMails.SendAsync(interviews, _notificationHelper, _candidateService, _applicationErrorLogger);

        private bool IsAuthorized()
        {
            var expected = _configuration["CallPreference:WebhookKey"];
            if (string.IsNullOrEmpty(expected)) return false;

            var supplied = Request.Headers["x-functions-key"].ToString();
            if (string.IsNullOrEmpty(supplied)) supplied = Request.Query["key"].ToString();
            if (string.IsNullOrEmpty(supplied)) return false;

            return CryptographicOperations.FixedTimeEquals(
                SHA256.HashData(Encoding.UTF8.GetBytes(supplied)),
                SHA256.HashData(Encoding.UTF8.GetBytes(expected)));
        }

        // Parsed whole: an ElevenLabs post-call payload with a long transcript can exceed the 100,000 characters
        // the repository keeps in CallPreferences.RawPayload.
        private async Task<string> ReadBody()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            return await reader.ReadToEndAsync();
        }
    }
}
