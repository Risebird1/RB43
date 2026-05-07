using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private string ProctoringScreenChunkContainer =>
            _configuration["ProctoringSettings:ScreenChunkContainerName"] ?? "proctoring-screen-chunks";

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

        private ProctoringTokenPayload? AuthorizeBearerOrBodyToken(string? sessionTokenFromBody)
        {
            return AuthorizeBearer() ?? _tokenService.ValidateToken(sessionTokenFromBody ?? string.Empty);
        }

        private static bool IsInterviewMismatch(int interviewIdFromToken, int? interviewIdFromBody, string? sessionIdFromBody)
        {
            if (interviewIdFromBody.HasValue && interviewIdFromBody.Value != interviewIdFromToken)
                return true;
            if (!string.IsNullOrWhiteSpace(sessionIdFromBody) && int.TryParse(sessionIdFromBody, out var parsedSessionId) && parsedSessionId != interviewIdFromToken)
                return true;
            return false;
        }

        private object BuildPolicyPayload()
        {
            int ParseIntWithFallback(string? value, int fallback)
            {
                return int.TryParse(value, out var parsed) && parsed > 0 ? parsed : fallback;
            }

            string[] ParseCsv(string? value)
            {
                if (string.IsNullOrWhiteSpace(value))
                    return Array.Empty<string>();
                return value
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }

            static string[] MergePolicyHosts(params IEnumerable<string>?[] sources)
            {
                var list = new List<string>();
                foreach (var src in sources)
                {
                    if (src == null) continue;
                    foreach (var x in src)
                    {
                        if (!string.IsNullOrWhiteSpace(x))
                            list.Add(x.Trim());
                    }
                }
                return list.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            }

            var screenshotIntervalSeconds = ParseIntWithFallback(_configuration["ProctoringSettings:Policy:ScreenshotIntervalSeconds"], 15);
            var heartbeatIntervalSeconds = ParseIntWithFallback(_configuration["ProctoringSettings:Policy:HeartbeatIntervalSeconds"], 10);
            var chunkIntervalMs = ParseIntWithFallback(_configuration["ProctoringSettings:Policy:ChunkIntervalMs"], 5000);
            var restrictedDomains = MergePolicyHosts(
                _configuration.GetSection("ProctoringSettings:Policy:RestrictedDomains").Get<string[]>(),
                ParseCsv(_configuration["ProctoringSettings:Policy:RestrictedDomains"]),
                _configuration.GetSection("Proctoring:RestrictedDomains").Get<string[]>());
            var allowedHosts = MergePolicyHosts(
                _configuration.GetSection("ProctoringSettings:Policy:AllowedHosts").Get<string[]>(),
                ParseCsv(_configuration["ProctoringSettings:Policy:AllowedHosts"]),
                _configuration.GetSection("Proctoring:AllowedHosts").Get<string[]>());

            return new
            {
                screenshotIntervalSeconds,
                heartbeatIntervalSeconds,
                chunkIntervalMs,
                restrictedDomains,
                allowedHosts
            };
        }

        private static bool TryExtractBase64AndMime(string? value, string defaultMimeType, out string mimeType, out string normalizedBase64)
        {
            mimeType = defaultMimeType;
            normalizedBase64 = string.Empty;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            var trimmed = value.Trim();
            if (!trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                normalizedBase64 = trimmed;
                return true;
            }

            var commaIndex = trimmed.IndexOf(',');
            if (commaIndex <= 0 || commaIndex >= trimmed.Length - 1)
                return false;

            var header = trimmed.Substring(5, commaIndex - 5);
            var payload = trimmed.Substring(commaIndex + 1);
            var semicolonIndex = header.IndexOf(';');
            if (semicolonIndex > 0)
                mimeType = header.Substring(0, semicolonIndex);
            else if (!string.IsNullOrWhiteSpace(header))
                mimeType = header;

            normalizedBase64 = payload;
            return true;
        }

        private static string GuessExtensionFromMime(string mimeType)
        {
            if (string.IsNullOrWhiteSpace(mimeType))
                return "bin";
            if (mimeType.Contains("jpeg", StringComparison.OrdinalIgnoreCase) || mimeType.Contains("jpg", StringComparison.OrdinalIgnoreCase))
                return "jpg";
            if (mimeType.Contains("png", StringComparison.OrdinalIgnoreCase))
                return "png";
            if (mimeType.Contains("webm", StringComparison.OrdinalIgnoreCase))
                return "webm";
            if (mimeType.Contains("mp4", StringComparison.OrdinalIgnoreCase))
                return "mp4";
            if (mimeType.Contains("gif", StringComparison.OrdinalIgnoreCase))
                return "gif";
            return "bin";
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
            var payload = AuthorizeBearerOrBodyToken(body?.SessionToken);
            if (payload == null)
                return Unauthorized();
            if (IsInterviewMismatch(payload.InterviewId, body?.InterviewId, body?.SessionId))
                return BadRequest("Interview mismatch.");
            _logger.LogInformation("Proctoring join: interview {InterviewId}, email {Email}", payload.InterviewId, payload.Email);
            var persisted = await PersistIfEligibleAsync(payload, () => _proctoringSessionEvents.InsertAsync(new ProctoringSessionEvent
            {
                InterviewId = payload.InterviewId,
                CandidateEmail = string.IsNullOrWhiteSpace(payload.Email) ? body?.Email : payload.Email,
                EventKind = "join",
                Level = "info",
                Reason = "join",
                MetadataJson = JsonSerializer.Serialize(new
                {
                    body?.Name,
                    body?.Email,
                    body?.SessionId,
                    body?.CandidateId,
                    body?.ExtensionVersion,
                    body?.TabId,
                    body?.Timestamp
                }),
            }));
            if (!persisted)
                _logger.LogDebug("Proctoring join not persisted (ineligible window or DB error) for interview {InterviewId}", payload.InterviewId);
            return Ok(new
            {
                ok = true,
                interviewId = payload.InterviewId,
                sessionId = body?.SessionId ?? payload.InterviewId.ToString(),
                policy = BuildPolicyPayload()
            });
        }

        [HttpPost("start")]
        public async Task<IActionResult> Start([FromBody] ProctoringSessionActionDto body)
        {
            var payload = AuthorizeBearer();
            if (payload == null)
                return Unauthorized();
            if (IsInterviewMismatch(payload.InterviewId, body?.InterviewId, body?.SessionId))
                return BadRequest();
            _logger.LogInformation("Proctoring start: interview {InterviewId}", payload.InterviewId);
            await PersistIfEligibleAsync(payload, () => _proctoringSessionEvents.InsertAsync(new ProctoringSessionEvent
            {
                InterviewId = payload.InterviewId,
                CandidateEmail = payload.Email,
                EventKind = "start",
                Level = "info",
                Reason = "start",
                MetadataJson = JsonSerializer.Serialize(new
                {
                    body?.SessionId,
                    body?.CandidateId,
                    body?.Timestamp
                }),
            }));
            return Ok(new { ok = true, policy = BuildPolicyPayload() });
        }

        [HttpPost("event")]
        public async Task<IActionResult> Event([FromBody] ProctoringEventDto body)
        {
            var payload = AuthorizeBearer();
            if (payload == null)
                return Unauthorized();
            if (IsInterviewMismatch(payload.InterviewId, body?.InterviewId, body?.SessionId))
                return BadRequest("Interview mismatch.");

            var normalizedEventType = string.IsNullOrWhiteSpace(body?.EventType) ? "event" : body.EventType;
            var level = !string.IsNullOrWhiteSpace(body?.Level)
                ? body.Level
                : string.Equals(normalizedEventType, "violation", StringComparison.OrdinalIgnoreCase)
                    ? "violation"
                    : "info";
            var reason = !string.IsNullOrWhiteSpace(body?.Reason)
                ? body.Reason
                : (!string.IsNullOrWhiteSpace(body?.Message) ? body.Message : normalizedEventType);

            var normalizedMetadata = new Dictionary<string, object?>
            {
                ["eventType"] = normalizedEventType,
                ["message"] = body?.Message,
                ["timestamp"] = body?.Timestamp
            };

            if (body?.Metadata != null)
                normalizedMetadata["payload"] = body.Metadata;

            var metaJson = JsonSerializer.Serialize(normalizedMetadata);
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
                MetadataJson = metaJson,
            }));
            return Ok(new { ok = true });
        }

        [HttpPost("screenshot")]
        public async Task<IActionResult> Screenshot([FromBody] ProctoringScreenshotDto body)
        {
            var payload = AuthorizeBearer();
            if (payload == null)
                return Unauthorized();
            if (IsInterviewMismatch(payload.InterviewId, body?.InterviewId, body?.SessionId))
                return BadRequest("Interview mismatch.");

            var incomingImage = !string.IsNullOrWhiteSpace(body?.Data) ? body.Data : body?.ImageBase64;
            var len = incomingImage?.Length ?? 0;
            _logger.LogDebug("Proctoring screenshot: interview {InterviewId}, bytes ~{Len}", payload.InterviewId, len);
            byte[] imageBytes = Array.Empty<byte>();
            string mimeType = "image/jpeg";
            if (!string.IsNullOrEmpty(incomingImage))
            {
                try
                {
                    if (!TryExtractBase64AndMime(incomingImage, "image/jpeg", out mimeType, out var normalizedBase64))
                        return BadRequest("Invalid screenshot payload");
                    imageBytes = Convert.FromBase64String(normalizedBase64);
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
                var extension = GuessExtensionFromMime(mimeType);
                blobPath = $"{payload.InterviewId}/{DateTime.UtcNow:yyyyMMdd}/{DateTime.UtcNow.Ticks}.{extension}";
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
                        mimeType);
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
                    mimeType,
                    body?.CapturedAt,
                    body?.TabId,
                    body?.CandidateId,
                    body?.EvidenceTrigger,
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
            if (IsInterviewMismatch(payload.InterviewId, body?.InterviewId, body?.SessionId))
                return BadRequest("Interview mismatch.");
            _logger.LogTrace("Proctoring heartbeat: interview {InterviewId}", payload.InterviewId);
            await PersistIfEligibleAsync(payload, () => _proctoringSessionEvents.InsertAsync(new ProctoringSessionEvent
            {
                InterviewId = payload.InterviewId,
                CandidateEmail = payload.Email,
                EventKind = "heartbeat",
                Level = "info",
                Reason = "heartbeat",
                MetadataJson = JsonSerializer.Serialize(new
                {
                    utcTicks = body?.UtcTicks,
                    body?.Timestamp,
                    body?.Status,
                    body?.ViolationCount,
                    body?.ScreenshotUploads,
                    body?.LastScreenshotAt
                }),
            }));
            return Ok(new { ok = true });
        }

        [HttpPost("screen-chunk")]
        public async Task<IActionResult> ScreenChunk([FromBody] ProctoringScreenChunkDto body)
        {
            var payload = AuthorizeBearer();
            if (payload == null)
                return Unauthorized();
            if (IsInterviewMismatch(payload.InterviewId, body?.InterviewId, body?.SessionId))
                return BadRequest("Interview mismatch.");

            var len = body?.Data?.Length ?? 0;
            var chunkBytes = Array.Empty<byte>();
            var mimeType = string.IsNullOrWhiteSpace(body?.MimeType) ? "video/webm" : body.MimeType;

            if (!string.IsNullOrWhiteSpace(body?.Data))
            {
                try
                {
                    if (!TryExtractBase64AndMime(body.Data, mimeType, out mimeType, out var normalizedBase64))
                        return BadRequest("Invalid screen chunk payload");
                    chunkBytes = Convert.FromBase64String(normalizedBase64);
                }
                catch
                {
                    return BadRequest("Invalid screen chunk base64");
                }
            }

            string blobUrl = null;
            string blobPath = null;
            if (chunkBytes.Length > 0)
            {
                var extension = GuessExtensionFromMime(mimeType);
                blobPath = $"{payload.InterviewId}/{DateTime.UtcNow:yyyyMMdd}/{DateTime.UtcNow.Ticks}-{body?.ChunkSeq ?? 0}.{extension}";
                try
                {
                    using var ms = new MemoryStream(chunkBytes, writable: false);
                    blobUrl = await _azureStorageHelper.InsertBlobCloudCompare(
                        blobPath,
                        ProctoringScreenChunkContainer,
                        null,
                        ms,
                        null,
                        mimeType);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Proctoring screen chunk blob upload failed for interview {InterviewId}", payload.InterviewId);
                }
            }

            await PersistIfEligibleAsync(payload, () => _proctoringSessionEvents.InsertAsync(new ProctoringSessionEvent
            {
                InterviewId = payload.InterviewId,
                CandidateEmail = payload.Email,
                EventKind = "screen-chunk",
                Level = "info",
                Reason = $"chunk-{body?.ChunkSeq ?? 0}",
                MetadataJson = JsonSerializer.Serialize(new
                {
                    body?.ChunkSeq,
                    body?.CapturedAt,
                    mimeType,
                    body?.EvidenceTrigger,
                    payloadLength = len,
                    bytesLength = chunkBytes.Length,
                    blobUrl,
                    blobPath,
                    container = ProctoringScreenChunkContainer
                }),
                ScreenshotPayloadLength = len > 0 ? len : null
            }));

            return Ok(new { ok = true, blobUrl });
        }

        [HttpPost("stop")]
        public async Task<IActionResult> Stop([FromBody] ProctoringSessionActionDto body)
        {
            var payload = AuthorizeBearer();
            if (payload == null)
                return Unauthorized();
            if (IsInterviewMismatch(payload.InterviewId, body?.InterviewId, body?.SessionId))
                return BadRequest();
            _logger.LogInformation("Proctoring stop: interview {InterviewId}", payload.InterviewId);
            await PersistIfEligibleAsync(payload, () => _proctoringSessionEvents.InsertAsync(new ProctoringSessionEvent
            {
                InterviewId = payload.InterviewId,
                CandidateEmail = payload.Email,
                EventKind = "stop",
                Level = "info",
                Reason = "stop",
                MetadataJson = JsonSerializer.Serialize(new
                {
                    body?.SessionId,
                    body?.CandidateId,
                    body?.Reason,
                    body?.Timestamp,
                    body?.ViolationCount
                }),
            }));
            return Ok(new { ok = true });
        }
    }

    public sealed class ProctoringJoinDto
    {
        public int? InterviewId { get; set; }
        public string? SessionToken { get; set; }
        public string? SessionId { get; set; }
        public string? CandidateId { get; set; }
        public string? ExtensionVersion { get; set; }
        public int? TabId { get; set; }
        public string? Timestamp { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
    }

    public sealed class ProctoringSessionActionDto
    {
        public int? InterviewId { get; set; }
        public string? SessionId { get; set; }
        public string? CandidateId { get; set; }
        public string? Reason { get; set; }
        public string? Timestamp { get; set; }
        public int? ViolationCount { get; set; }
    }

    public sealed class ProctoringEventDto
    {
        public int? InterviewId { get; set; }
        public string? SessionId { get; set; }
        public string? CandidateId { get; set; }
        public string? Level { get; set; }
        public string? Reason { get; set; }
        public string? EventType { get; set; }
        public string? Message { get; set; }
        public string? Timestamp { get; set; }
        public object? Metadata { get; set; }
    }

    public sealed class ProctoringScreenshotDto
    {
        public int? InterviewId { get; set; }
        public string? SessionId { get; set; }
        public string? CandidateId { get; set; }
        public int? TabId { get; set; }
        public string? CapturedAt { get; set; }
        public string? Data { get; set; }
        public string? ImageBase64 { get; set; }
        /// <summary>Optional violation / incident code when capture was triggered by evidence pipeline (e.g. EXAM_TAB_FOCUS_LOST).</summary>
        public string? EvidenceTrigger { get; set; }
    }

    public sealed class ProctoringHeartbeatDto
    {
        public int? InterviewId { get; set; }
        public string? SessionId { get; set; }
        public string? CandidateId { get; set; }
        public string? Status { get; set; }
        public int? ViolationCount { get; set; }
        public int? ScreenshotUploads { get; set; }
        public string? LastScreenshotAt { get; set; }
        public string? Timestamp { get; set; }
        public long? UtcTicks { get; set; }
    }

    public sealed class ProctoringScreenChunkDto
    {
        public int? InterviewId { get; set; }
        public string? SessionId { get; set; }
        public string? CandidateId { get; set; }
        public int? ChunkSeq { get; set; }
        public string? MimeType { get; set; }
        public string? CapturedAt { get; set; }
        public string? Data { get; set; }
        /// <summary>Set when chunk was forced via MediaRecorder.requestData() after an incident (matches EvidenceTrigger on screenshot).</summary>
        public string? EvidenceTrigger { get; set; }
    }
}
