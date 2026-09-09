using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;

namespace Risebird.Services.Controllers
{
    /// <summary>
    /// Backend for the Risebird Desktop Proctor client, per the DesktopProctor API contract:
    /// join/start/heartbeat/policy/events/end are called by the desktop app itself and are
    /// identified purely by sessionToken (no bearer token). Status codes are load-bearing for
    /// the client's retry state machine — 401/403/410 are fatal (client stops, no retry),
    /// everything else non-2xx is transient (client retries with backoff) — so every action here
    /// returns 401 for "session token not recognized" and otherwise lets unexpected exceptions
    /// fall through to the catch-all 500 rather than masking them as a fatal status.
    /// mint/logs are operational endpoints for the scheduling dashboard, not part of the desktop
    /// client contract, and are gated by a shared secret instead.
    /// </summary>
    [Route("api/proctoring/session")]
    [ApiController]
    public class ProctoringApiController : ControllerBase
    {
        private readonly IProctoringApiService _proctoringApiService;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IConfiguration _configuration;

        public ProctoringApiController(
            IProctoringApiService proctoringApiService,
            IApplicationErrorLogger applicationErrorLogger,
            IConfiguration configuration)
        {
            _proctoringApiService = proctoringApiService;
            _applicationErrorLogger = applicationErrorLogger;
            _configuration = configuration;
        }

        [HttpPost]
        [Route("join")]
        public async Task<IActionResult> Join([FromBody] ProctoringJoinRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.SessionToken))
                    return BadRequest();

                var result = await _proctoringApiService.JoinAsync(request);
                if (result == null)
                    return Unauthorized();

                return Ok(result);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost]
        [Route("start")]
        public async Task<IActionResult> Start([FromBody] ProctoringSessionActionRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.SessionToken))
                    return BadRequest();

                var result = await _proctoringApiService.StartAsync(request.SessionToken);
                if (result == null)
                    return Unauthorized();

                return Ok(result);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost]
        [Route("heartbeat")]
        public async Task<IActionResult> Heartbeat([FromBody] ProctoringHeartbeatRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.SessionToken))
                    return BadRequest();

                var result = await _proctoringApiService.HeartbeatAsync(request);
                if (result == null)
                    return Unauthorized();

                return Ok(result);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet]
        [Route("policy")]
        public async Task<IActionResult> Policy([FromQuery] string sessionToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sessionToken))
                    return BadRequest();

                return Ok(await _proctoringApiService.GetPolicyAsync());
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost]
        [Route("events")]
        public async Task<IActionResult> Events([FromBody] ProctoringEventsRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.SessionToken))
                    return BadRequest();

                var result = await _proctoringApiService.SubmitEventsAsync(request);
                if (result == null)
                    return Unauthorized();

                return Ok(result);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost]
        [Route("end")]
        public async Task<IActionResult> End([FromBody] ProctoringSessionActionRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.SessionToken))
                    return BadRequest();

                var ended = await _proctoringApiService.EndAsync(request.SessionToken);
                if (!ended)
                    return Unauthorized();

                return Ok();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Mints a signed launch token and registers the session (not part of the desktop client
        /// contract — called by the scheduling dashboard that hands the app its risebird-proctor://
        /// join link). Gated by a shared secret since it can mint sessions carrying a candidate email.
        /// </summary>
        [HttpPost]
        [Route("mint")]
        public async Task<IActionResult> Mint([FromBody] ProctoringMintSessionRequest request)
        {
            try
            {
                if (!IsAuthorizedMintCaller())
                    return Unauthorized();

                if (request == null || string.IsNullOrWhiteSpace(request.InterviewId) || string.IsNullOrWhiteSpace(request.CandidateEmail))
                    return BadRequest();

                return Ok(await _proctoringApiService.MintSessionAsync(request));
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>Operational lookup backing usp_GetProctoringLogsForCandidate — not part of the desktop client contract.</summary>
        [HttpGet]
        [Route("logs")]
        public async Task<IActionResult> Logs([FromQuery] string candidateEmail)
        {
            try
            {
                if (!IsAuthorizedMintCaller())
                    return Unauthorized();

                if (string.IsNullOrWhiteSpace(candidateEmail))
                    return BadRequest();

                return Ok(await _proctoringApiService.GetLogsForCandidateAsync(candidateEmail));
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        private bool IsAuthorizedMintCaller()
        {
            var configuredKey = _configuration["ProctoringApiSettings:MintKey"];
            if (string.IsNullOrWhiteSpace(configuredKey))
                return false;

            var providedKey = Request.Headers["x-proctoring-mint-key"].ToString();
            return string.Equals(providedKey, configuredKey, StringComparison.Ordinal);
        }
    }
}
