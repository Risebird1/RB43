using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
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

        private int MaxScreenChunkBytes =>
            ParseIntOrDefault(_configuration["ProctoringSettings:MaxScreenChunkBytes"], 15 * 1024 * 1024, 256 * 1024, 100 * 1024 * 1024);

        private int MinScreenChunkBytes =>
            ParseIntOrDefault(_configuration["ProctoringSettings:MinScreenChunkBytes"], 4096, 512, 65536);

        private int BlobReadSasMinutes =>
            ParseIntOrDefault(_configuration["ProctoringSettings:BlobReadSasMinutes"], 240, 5, 24 * 60);

        private int MaxScreenshotBytes =>
            ParseIntOrDefault(_configuration["ProctoringSettings:MaxScreenshotBytes"], 8 * 1024 * 1024, 128 * 1024, 30 * 1024 * 1024);

        private int UploadRetryCount =>
            ParseIntOrDefault(_configuration["ProctoringSettings:UploadRetryCount"], 3, 1, 6);

        private static string FileExtensionFromMime(string mimeType)
        {
            var mime = (mimeType ?? string.Empty).Trim().ToLowerInvariant();
            if (mime.Contains("webm")) return "webm";
            if (mime.Contains("mp4")) return "mp4";
            if (mime.Contains("jpeg") || mime.Contains("jpg")) return "jpg";
            if (mime.Contains("png")) return "png";
            return "bin";
        }

        private static string NormalizeMime(string mimeType)
        {
            var m = (mimeType ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(m)) return "";
            var semi = m.IndexOf(';');
            return semi > 0 ? m.Substring(0, semi).Trim() : m;
        }

        private static bool IsAllowedScreenChunkMime(string mimeType)
        {
            var m = NormalizeMime(mimeType);
            return m == "video/webm" || m == "video/mp4";
        }

        private static int ParseIntOrDefault(string value, int fallback, int min, int max)
        {
            if (!int.TryParse(value, out var parsed)) return fallback;
            if (parsed < min) return min;
            if (parsed > max) return max;
            return parsed;
        }

        private static bool TryDecodeDataUrl(string dataUrl, out byte[] bytes, out string mimeType, out string error)
        {
            bytes = Array.Empty<byte>();
            mimeType = "";
            error = "";
            if (string.IsNullOrWhiteSpace(dataUrl))
            {
                error = "Missing data.";
                return false;
            }

            var trimmed = dataUrl.Trim();
            string base64;
            if (trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                var comma = trimmed.IndexOf(',');
                if (comma <= 5 || comma >= trimmed.Length - 1)
                {
                    error = "Invalid data URL.";
                    return false;
                }
                var meta = trimmed.Substring(5, comma - 5);
                base64 = trimmed.Substring(comma + 1);
                var semi = meta.IndexOf(';');
                mimeType = (semi > 0 ? meta.Substring(0, semi) : meta)?.Trim() ?? "";
            }
            else
            {
                base64 = trimmed;
            }

            try
            {
                bytes = Convert.FromBase64String(base64);
                return true;
            }
            catch
            {
                error = "Invalid base64 payload.";
                return false;
            }
        }

        private async Task<string> EnsureDisplayableBlobUrlAsync(string containerName, string blobPath, string blobUrl)
        {
            if (string.IsNullOrWhiteSpace(blobUrl) || string.IsNullOrWhiteSpace(containerName) || string.IsNullOrWhiteSpace(blobPath))
                return blobUrl;

            if (blobUrl.Contains("?")) return blobUrl; // already SAS or query-authenticated

            try
            {
                var container = _azureStorageHelper.GetCloudBlobContainer(containerName);
                var perms = await container.GetPermissionsAsync();
                var publicAccess = perms?.PublicAccess ?? BlobContainerPublicAccessType.Off;
                if (publicAccess == BlobContainerPublicAccessType.Blob || publicAccess == BlobContainerPublicAccessType.Container)
                    return blobUrl;

                var blob = container.GetBlockBlobReference(blobPath);
                if (!await blob.ExistsAsync())
                    return blobUrl;

                var sas = blob.GetSharedAccessSignature(new SharedAccessBlobPolicy
                {
                    SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5).UtcDateTime,
                    SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(BlobReadSasMinutes).UtcDateTime,
                    Permissions = SharedAccessBlobPermissions.Read,
                });
                return blob.Uri + sas;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Proctoring blob access policy check failed for container {Container}", containerName);
                return blobUrl;
            }
        }

        private async Task<(string blobUrl, string uploadError)> UploadBlobWithRetryAsync(
            byte[] payloadBytes,
            string blobPath,
            string containerName,
            string contentType,
            string logContext,
            int interviewId)
        {
            if (payloadBytes == null || payloadBytes.Length == 0)
                return (null, "Empty upload payload.");

            string lastError = null;
            var attempts = UploadRetryCount;
            for (var attempt = 1; attempt <= attempts; attempt++)
            {
                try
                {
                    using var ms = new MemoryStream(payloadBytes, writable: false);
                    var blobUrl = await _azureStorageHelper.InsertBlobCloudCompare(
                        blobPath,
                        containerName,
                        null,
                        ms,
                        null,
                        contentType);
                    blobUrl = await EnsureDisplayableBlobUrlAsync(containerName, blobPath, blobUrl);
                    if (!string.IsNullOrWhiteSpace(blobUrl))
                        return (blobUrl, null);
                    lastError = "Blob URL was empty after upload.";
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    _logger.LogWarning(
                        ex,
                        "Proctoring {Context} blob upload attempt {Attempt}/{Attempts} failed for container {Container}, interview {InterviewId}",
                        logContext,
                        attempt,
                        attempts,
                        containerName,
                        interviewId);
                }

                if (attempt < attempts)
                    await Task.Delay(250 * attempt);
            }

            return (null, lastError ?? "Unknown blob upload failure.");
        }

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

        private async Task<bool> PersistMediaEventAsync(ProctoringTokenPayload payload, Func<Task<long>> insertRow)
        {
            if (!await _proctoringEligibility.ShouldPersistProctoringMediaAsync(payload))
            {
                _logger.LogDebug(
                    "Proctoring media metadata not persisted (session identity) for interview {InterviewId}",
                    payload.InterviewId);
                return false;
            }
            var id = await insertRow();
            if (id <= 0)
            {
                _logger.LogWarning(
                    "Proctoring media DB insert failed for interview {InterviewId}",
                    payload.InterviewId);
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
        [DisableRequestSizeLimit]
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
            if (imageBytes.Length == 0)
                return BadRequest("Screenshot payload is empty.");
            if (imageBytes.Length > MaxScreenshotBytes)
                return BadRequest($"Screenshot exceeds max allowed size ({MaxScreenshotBytes} bytes).");

            string blobUrl = null;
            string blobPath = null;
            string uploadError = null;
            blobPath = $"{payload.InterviewId}/{DateTime.UtcNow:yyyyMMdd}/{DateTime.UtcNow.Ticks}.jpg";
            var uploadResult = await UploadBlobWithRetryAsync(
                imageBytes,
                blobPath,
                ProctoringScreenshotContainer,
                "image/jpeg",
                "screenshot",
                payload.InterviewId);
            blobUrl = uploadResult.blobUrl;
            uploadError = uploadResult.uploadError;

            await PersistMediaEventAsync(payload, () => _proctoringSessionEvents.InsertAsync(new ProctoringSessionEvent
            {
                InterviewId = payload.InterviewId,
                CandidateEmail = payload.Email,
                EventKind = "screenshot",
                Level = string.IsNullOrWhiteSpace(uploadError) ? "info" : "violation",
                Reason = string.IsNullOrWhiteSpace(uploadError) ? "screenshot" : "screenshot_upload_failed",
                MetadataJson = JsonSerializer.Serialize(new
                {
                    imageBase64Length = len,
                    imageBytesLength = imageBytes.Length,
                    capturedAt = body?.CapturedAt,
                    captureSource = body?.CaptureSource,
                    captureContext = body?.CaptureContext,
                    blobUrl,
                    blobPath,
                    container = ProctoringScreenshotContainer,
                    uploadError,
                }),
                ScreenshotPayloadLength = len > 0 ? len : null,
            }));
            if (!string.IsNullOrWhiteSpace(uploadError))
                return StatusCode(500, new { ok = false, error = "Screenshot upload failed.", details = uploadError });
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

        [HttpPost("screen-chunk")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> ScreenChunk([FromBody] ProctoringScreenChunkDto body)
        {
            var payload = AuthorizeBearer();
            if (payload == null)
                return Unauthorized();

            if (body?.InterviewId != null && body.InterviewId != payload.InterviewId)
                return BadRequest("Interview mismatch.");

            if (body == null || string.IsNullOrWhiteSpace(body.Data))
                return BadRequest("Screen chunk payload is empty.");

            if (!TryDecodeDataUrl(body?.Data, out var chunkBytes, out var detectedMimeType, out var decodeError))
                return BadRequest(decodeError);

            var mimeType = string.IsNullOrWhiteSpace(body?.MimeType) ? detectedMimeType : body.MimeType.Trim();
            if (string.IsNullOrWhiteSpace(mimeType))
                mimeType = "video/webm";
            mimeType = NormalizeMime(mimeType);

            if (!IsAllowedScreenChunkMime(mimeType))
                return BadRequest("Unsupported MIME type. Allowed: video/webm, video/mp4.");

            if (chunkBytes.Length == 0)
                return BadRequest("Screen chunk is empty.");

            if (chunkBytes.Length < MinScreenChunkBytes)
                return BadRequest($"Screen chunk is too small ({chunkBytes.Length} bytes; minimum {MinScreenChunkBytes}). Likely blank capture.");

            if (chunkBytes.Length > MaxScreenChunkBytes)
                return BadRequest($"Screen chunk exceeds max allowed size ({MaxScreenChunkBytes} bytes).");

            var capturedAtUtc = body?.CapturedAt != null
                ? DateTimeOffset.FromUnixTimeMilliseconds(body.CapturedAt.Value).UtcDateTime
                : DateTime.UtcNow;

            string blobUrl = null;
            string blobPath = null;
            string uploadError = null;
            if (chunkBytes.Length > 0)
            {
                var ext = FileExtensionFromMime(mimeType);
                var seq = body?.ChunkSeq ?? 0;
                blobPath = $"{payload.InterviewId}/{capturedAtUtc:yyyyMMdd}/chunks/{capturedAtUtc:yyyyMMddHHmmssfff}-{seq}.{ext}";
                var uploadResult = await UploadBlobWithRetryAsync(
                    chunkBytes,
                    blobPath,
                    ProctoringScreenChunkContainer,
                    mimeType,
                    "screen_chunk",
                    payload.InterviewId);
                blobUrl = uploadResult.blobUrl;
                uploadError = uploadResult.uploadError;
            }

            await PersistMediaEventAsync(payload, () => _proctoringSessionEvents.InsertAsync(new ProctoringSessionEvent
            {
                InterviewId = payload.InterviewId,
                CandidateEmail = payload.Email,
                EventKind = "screen_chunk",
                Level = uploadError == null ? "info" : "violation",
                Reason = uploadError == null ? "screen_chunk" : "screen_chunk_upload_failed",
                MetadataJson = JsonSerializer.Serialize(new
                {
                    chunkSeq = body?.ChunkSeq,
                    mimeType,
                    capturedAt = body?.CapturedAt,
                    captureSource = body?.CaptureSource,
                    captureContext = body?.CaptureContext,
                    dataLength = body?.Data?.Length ?? 0,
                    bytesLength = chunkBytes.Length,
                    blobUrl,
                    blobPath,
                    container = ProctoringScreenChunkContainer,
                    uploadError,
                }),
                ScreenshotPayloadLength = null,
            }));

            if (!string.IsNullOrEmpty(uploadError))
                return StatusCode(500, new { ok = false, error = "Screen chunk upload failed.", details = uploadError });

            return Ok(new { ok = true, blobUrl });
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
        public long? CapturedAt { get; set; }
        public string? CaptureSource { get; set; }
        public object? CaptureContext { get; set; }
    }

    public sealed class ProctoringHeartbeatDto
    {
        public int? InterviewId { get; set; }
        public long? UtcTicks { get; set; }
        public string? Status { get; set; }
        public int? Violations { get; set; }
        public long? Timestamp { get; set; }
    }

    public sealed class ProctoringScreenChunkDto
    {
        public int? InterviewId { get; set; }
        public int? ChunkSeq { get; set; }
        public string? MimeType { get; set; }
        public long? CapturedAt { get; set; }
        public string? Data { get; set; }
        public string? CaptureSource { get; set; }
        public object? CaptureContext { get; set; }
    }
}
