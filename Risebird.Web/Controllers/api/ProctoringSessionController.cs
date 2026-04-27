using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Repository.Interface;
using Risebird.Web.Services.Proctoring;

namespace Risebird.Web.Controllers.Api
{
    [ApiController]
    [AllowAnonymous]
    [Route("api/proctoring/session")]
    public sealed class ProctoringSessionController : ControllerBase
    {
        private readonly IProctoringTokenService _tokenService;
        private readonly IProctoringSessionEventRepository _proctoringSessionEvents;
        private readonly IProctoringSessionEligibilityService _proctoringEligibility;
        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ProctoringSessionController> _logger;

        public ProctoringSessionController(
            IProctoringTokenService tokenService,
            IProctoringSessionEventRepository proctoringSessionEvents,
            IProctoringSessionEligibilityService proctoringEligibility,
            IAzureStorageHelper azureStorageHelper,
            IConfiguration configuration,
            ILogger<ProctoringSessionController> logger)
        {
            _tokenService = tokenService;
            _proctoringSessionEvents = proctoringSessionEvents;
            _proctoringEligibility = proctoringEligibility;
            _azureStorageHelper = azureStorageHelper;
            _configuration = configuration;
            _logger = logger;
        }

        private string ProctoringScreenshotContainer =>
            _configuration["ProctoringSettings:ScreenshotContainerName"] ?? "proctoring-screenshots";

        private ProctoringTokenPayload? AuthorizeBearer()
        {
            if (!Request.Headers.TryGetValue("Authorization", out var auth))
                return null;
            var value = auth.ToString();
            const string prefix = "Bearer ";
            if (string.IsNullOrEmpty(value) || !value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return null;
            var token = value.Substring(prefix.Length).Trim();
            return _tokenService.ValidateToken(token);
        }

        private async Task<bool> PersistIfEligibleAsync(ProctoringTokenPayload payload, Func<Task<long>> insertRow)
        {
            if (!await _proctoringEligibility.ShouldPersistProctoringEventsAsync(payload))
                return false;
            var id = await insertRow();
            if (id <= 0)
            {
                _logger.LogWarning("Proctoring DB insert failed or returned no id for interview {InterviewId}", payload.InterviewId);
                return false;
            }
            return true;
        }

        [HttpPost("join")]
        public async Task<IActionResult> Join([FromBody] ProctoringJoinDto body)
        {
            var payload = _tokenService.ValidateToken(body?.SessionToken ?? string.Empty);
            if (payload == null)
                return Unauthorized();
            if (body?.InterviewId != null && body.InterviewId != payload.InterviewId)
                return BadRequest("Interview mismatch.");
            _logger.LogInformation("Proctoring join: interview {InterviewId}, email {Email}", payload.InterviewId, payload.Email);
            var persisted = await PersistIfEligibleAsync(payload, () => _proctoringSessionEvents.InsertAsync(new ProctoringSessionEvent
            {
                InterviewId = payload.InterviewId,
                CandidateEmail = string.IsNullOrWhiteSpace(payload.Email) ? body?.Email : payload.Email,
                EventKind = "join",
                Level = "info",
                Reason = "join",
                MetadataJson = JsonSerializer.Serialize(new { body?.Name, body?.Email }),
            }));
            if (!persisted)
                _logger.LogDebug("Proctoring join not persisted (ineligible window or DB error) for interview {InterviewId}", payload.InterviewId);
            return Ok(new { ok = true, interviewId = payload.InterviewId });
        }

        [HttpPost("start")]
        public async Task<IActionResult> Start([FromBody] ProctoringSessionActionDto body)
        {
            var payload = AuthorizeBearer();
            if (payload == null)
                return Unauthorized();
            if (body?.InterviewId != null && body.InterviewId != payload.InterviewId)
                return BadRequest();
            _logger.LogInformation("Proctoring start: interview {InterviewId}", payload.InterviewId);
            await PersistIfEligibleAsync(payload, () => _proctoringSessionEvents.InsertAsync(new ProctoringSessionEvent
            {
                InterviewId = payload.InterviewId,
                CandidateEmail = payload.Email,
                EventKind = "start",
                Level = "info",
                Reason = "start",
                MetadataJson = null,
            }));
            return Ok(new { ok = true });
        }

        [HttpPost("event")]
        public async Task<IActionResult> Event([FromBody] ProctoringEventDto body)
        {
            var payload = AuthorizeBearer();
            if (payload == null)
                return Unauthorized();
            var level = body?.Level ?? "info";
            var reason = body?.Reason ?? "";
            var metaJson = body?.Metadata != null ? JsonSerializer.Serialize(body.Metadata) : "";
            if (string.Equals(level, "violation", StringComparison.OrdinalIgnoreCase))
                _logger.LogWarning("Proctoring violation: interview {InterviewId} — {Reason} — {Metadata}", payload.InterviewId, reason, metaJson);
            else
                _logger.LogInformation("Proctoring event: interview {InterviewId} — {Reason} — {Metadata}", payload.InterviewId, reason, metaJson);
            await PersistIfEligibleAsync(payload, () => _proctoringSessionEvents.InsertAsync(new ProctoringSessionEvent
            {
                InterviewId = payload.InterviewId,
                CandidateEmail = payload.Email,
                EventKind = "telemetry",
                Level = level,
                Reason = reason,
                MetadataJson = string.IsNullOrEmpty(metaJson) ? null : metaJson,
            }));
            return Ok(new { ok = true });
        }

        [HttpPost("screenshot")]
        public async Task<IActionResult> Screenshot([FromBody] ProctoringScreenshotDto body)
        {
            var payload = AuthorizeBearer();
            if (payload == null)
                return Unauthorized();
            var len = body?.ImageBase64?.Length ?? 0;
            _logger.LogDebug("Proctoring screenshot: interview {InterviewId}, bytes ~{Len}", payload.InterviewId, len);
            byte[] imageBytes = Array.Empty<byte>();
            if (!string.IsNullOrEmpty(body?.ImageBase64))
            {
                try
                {
                    imageBytes = Convert.FromBase64String(body.ImageBase64);
                }
                catch
                {
                    return BadRequest("Invalid base64");
                }
            }

            string blobUrl = null;
            string blobPath = null;
            if (imageBytes.Length > 0)
            {
                blobPath = $"{payload.InterviewId}/{DateTime.UtcNow:yyyyMMdd}/{DateTime.UtcNow.Ticks}.jpg";
                try
                {
                    using var ms = new MemoryStream(imageBytes, writable: false);
                    // InsertBlobCloudCompare uses typo parameter name blobFIleStream (Stream) — pass positionally.
                    blobUrl = await _azureStorageHelper.InsertBlobCloudCompare(
                        blobPath,
                        ProctoringScreenshotContainer,
                        null,
                        ms,
                        null,
                        "image/jpeg");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Proctoring screenshot blob upload failed for interview {InterviewId}", payload.InterviewId);
                }
            }

            await PersistIfEligibleAsync(payload, () => _proctoringSessionEvents.InsertAsync(new ProctoringSessionEvent
            {
                InterviewId = payload.InterviewId,
                CandidateEmail = payload.Email,
                EventKind = "screenshot",
                Level = "info",
                Reason = "screenshot",
                MetadataJson = JsonSerializer.Serialize(new
                {
                    imageBase64Length = len,
                    imageBytesLength = imageBytes.Length,
                    blobUrl,
                    blobPath,
                    container = ProctoringScreenshotContainer,
                }),
                ScreenshotPayloadLength = len > 0 ? len : null,
            }));
            return Ok(new { ok = true, blobUrl });
        }

        [HttpPost("heartbeat")]
        public async Task<IActionResult> Heartbeat([FromBody] ProctoringHeartbeatDto body)
        {
            var payload = AuthorizeBearer();
            if (payload == null)
                return Unauthorized();
            _logger.LogTrace("Proctoring heartbeat: interview {InterviewId}", payload.InterviewId);
            await PersistIfEligibleAsync(payload, () => _proctoringSessionEvents.InsertAsync(new ProctoringSessionEvent
            {
                InterviewId = payload.InterviewId,
                CandidateEmail = payload.Email,
                EventKind = "heartbeat",
                Level = "info",
                Reason = "heartbeat",
                MetadataJson = JsonSerializer.Serialize(new { utcTicks = body?.UtcTicks }),
            }));
            return Ok(new { ok = true });
        }

        [HttpPost("stop")]
        public async Task<IActionResult> Stop([FromBody] ProctoringSessionActionDto body)
        {
            var payload = AuthorizeBearer();
            if (payload == null)
                return Unauthorized();
            if (body?.InterviewId != null && body.InterviewId != payload.InterviewId)
                return BadRequest();
            _logger.LogInformation("Proctoring stop: interview {InterviewId}", payload.InterviewId);
            await PersistIfEligibleAsync(payload, () => _proctoringSessionEvents.InsertAsync(new ProctoringSessionEvent
            {
                InterviewId = payload.InterviewId,
                CandidateEmail = payload.Email,
                EventKind = "stop",
                Level = "info",
                Reason = "stop",
                MetadataJson = null,
            }));
            return Ok(new { ok = true });
        }
    }

    public sealed class ProctoringJoinDto
    {
        public int? InterviewId { get; set; }
        public string? SessionToken { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
    }

    public sealed class ProctoringSessionActionDto
    {
        public int? InterviewId { get; set; }
    }

    public sealed class ProctoringEventDto
    {
        public int? InterviewId { get; set; }
        public string? Level { get; set; }
        public string? Reason { get; set; }
        public object? Metadata { get; set; }
    }

    public sealed class ProctoringScreenshotDto
    {
        public int? InterviewId { get; set; }
        public string? ImageBase64 { get; set; }
    }

    public sealed class ProctoringHeartbeatDto
    {
        public int? InterviewId { get; set; }
        public long? UtcTicks { get; set; }
    }
}
