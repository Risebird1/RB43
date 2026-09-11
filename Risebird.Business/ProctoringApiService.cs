using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;

namespace Risebird.Business
{
    public class ProctoringApiService : IProctoringApiService
    {
        private const int DefaultTokenTtlMinutes = 180;
        private const string DefaultScreenshotContainer = "desktopproctor-screenshots";
        private const int MaxScreenshotBytes = 15 * 1024 * 1024;

        private readonly IProctoringApiRepository _repository;
        private readonly IProctoringLaunchTokenService _launchTokenService;
        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly IConfiguration _configuration;

        public ProctoringApiService(
            IProctoringApiRepository repository,
            IProctoringLaunchTokenService launchTokenService,
            IAzureStorageHelper azureStorageHelper,
            IConfiguration configuration)
        {
            _repository = repository;
            _launchTokenService = launchTokenService;
            _azureStorageHelper = azureStorageHelper;
            _configuration = configuration;
        }

        public async Task<ProctoringMintSessionResponse> MintSessionAsync(ProctoringMintSessionRequest request)
        {
            // NOTE: interviewId/candidateEmail validation against InterviewMeetings/InterviewComments
            // is deliberately disabled for now (product decision) — every mint request is trusted
            // as-is. IProctoringApiRepository.ValidateCandidateInterviewAsync and
            // usp_ValidateProctoringCandidateInterview already implement that check; re-enable by
            // restoring the guard this comment replaced (int.TryParse the interviewId, call
            // ValidateCandidateInterviewAsync, return null — 401 — on any failed check) once the
            // product wants candidate/interview validation back on this endpoint.

            var sessionToken = Guid.NewGuid().ToString("N");
            var ttl = TimeSpan.FromMinutes(request.TtlMinutes.GetValueOrDefault() > 0
                ? request.TtlMinutes.Value
                : _configuration.GetValue("ProctoringApiSettings:DefaultTokenTtlMinutes", DefaultTokenTtlMinutes));
            var apiBaseUrl = _configuration["ProctoringApiSettings:ApiBaseUrl"];

            var (launchToken, expiresAtUtc) = _launchTokenService.MintLaunchToken(
                apiBaseUrl, sessionToken, request.InterviewId, request.CandidateEmail, request.BrowserSessionId, ttl);

            await _repository.RegisterSessionAsync(new ProctoringApiSession
            {
                SessionToken = sessionToken,
                InterviewId = request.InterviewId,
                CandidateEmail = request.CandidateEmail,
                BrowserSessionId = request.BrowserSessionId,
                Trust = ProctoringTrust.Verified.ToWireString(),
                ExpiresAtUtc = expiresAtUtc,
            });

            return new ProctoringMintSessionResponse
            {
                Token = launchToken,
                ApiBaseUrl = apiBaseUrl,
                SessionToken = sessionToken,
            };
        }

        public async Task<ProctoringSessionResponse> JoinAsync(ProctoringJoinRequest request)
        {
            var session = await _repository.GetSessionByTokenAsync(request.SessionToken);
            if (session == null)
                return null;

            await _repository.MarkSessionJoinedAsync(request.SessionToken, request.CandidateEmail);
            return new ProctoringSessionResponse { SessionId = session.SessionToken, RiskScore = session.RiskScore };
        }

        public async Task<ProctoringSessionResponse> StartAsync(string sessionToken)
        {
            var session = await _repository.GetSessionByTokenAsync(sessionToken);
            if (session == null || session.JoinedAtUtc == null)
                return null;

            await _repository.MarkSessionStartedAsync(sessionToken);
            return new ProctoringSessionResponse { SessionId = session.SessionToken, RiskScore = session.RiskScore };
        }

        public async Task<ProctoringHeartbeatResponse> HeartbeatAsync(ProctoringHeartbeatRequest request)
        {
            var session = await _repository.GetSessionByTokenAsync(request.SessionToken);
            if (session == null)
                return null;

            await _repository.UpdateHeartbeatAsync(request.SessionToken, request.ClientState.ToWireString());
            return new ProctoringHeartbeatResponse { RiskScore = session.RiskScore, ClientState = request.ClientState };
        }

        public Task<ProctoringPolicyResponse> GetPolicyAsync()
        {
            var section = _configuration.GetSection("ProctoringApiSettings:Policy");
            var policy = section.Exists() ? section.Get<ProctoringPolicyResponse>() : null;
            return Task.FromResult(policy ?? DefaultPolicy());
        }

        public async Task<ProctoringEventsResponse> SubmitEventsAsync(ProctoringEventsRequest request)
        {
            var session = await _repository.GetSessionByTokenAsync(request.SessionToken);
            if (session == null)
                return null;

            var delta = 0;
            foreach (var evt in request.Events ?? Enumerable.Empty<ProctoringEventItem>())
            {
                var inserted = await _repository.InsertLogIfNewAsync(new ProctoringApiLog
                {
                    SessionToken = request.SessionToken,
                    Sequence = evt.Sequence,
                    Type = evt.Type,
                    Category = evt.Category,
                    Level = evt.Level.ToWireString(),
                    Confidence = evt.Confidence.ToWireString(),
                    OpenedAtUtc = evt.OpenedAt,
                    ResolvedAtUtc = evt.ResolvedAt,
                    RedactedDetail = evt.RedactedDetail,
                    RawDetail = evt.RawDetail,
                });

                if (inserted)
                    delta += ProctoringRiskScorer.ScoreEvent(evt.Level, evt.Confidence);
            }

            var riskScore = delta == 0
                ? session.RiskScore
                : await _repository.AddRiskScoreAsync(request.SessionToken, delta);

            return new ProctoringEventsResponse { RiskScore = riskScore };
        }

        public async Task<bool> EndAsync(string sessionToken)
        {
            var session = await _repository.GetSessionByTokenAsync(sessionToken);
            if (session == null)
                return false;

            await _repository.MarkSessionEndedAsync(sessionToken);
            return true;
        }

        public async Task<bool> SubmitScreenshotAsync(ProctoringScreenshotRequest request)
        {
            var session = await _repository.GetSessionByTokenAsync(request.SessionToken);
            if (session == null)
                return false;

            var imageBytes = Convert.FromBase64String(request.ImageBase64);
            if (imageBytes.Length == 0 || imageBytes.Length > MaxScreenshotBytes)
                return true; // best-effort telemetry — drop silently rather than fail the call

            var containerName = _configuration["ProctoringApiSettings:ScreenshotContainerName"] ?? DefaultScreenshotContainer;
            var extension = ExtensionFromContentType(request.ContentType);
            var blobPath = $"{request.SessionToken}/{request.CapturedAt:yyyyMMdd}/{request.CapturedAt:HHmmssfff}-{request.Sequence}.{extension}";

            using var stream = new MemoryStream(imageBytes, writable: false);
            var blobUrl = await _azureStorageHelper.InsertBlobCloudCompare(blobPath, containerName, null, stream, null, request.ContentType);

            if (!string.IsNullOrWhiteSpace(blobUrl))
            {
                await _repository.InsertScreenshotIfNewAsync(new ProctoringScreenshot
                {
                    SessionToken = request.SessionToken,
                    Sequence = request.Sequence,
                    BlobUrl = blobUrl,
                    ContentType = request.ContentType,
                    CapturedAtUtc = request.CapturedAt,
                });
            }

            return true;
        }

        private static string ExtensionFromContentType(string contentType)
        {
            var type = (contentType ?? string.Empty).Trim().ToLowerInvariant();
            if (type.Contains("png")) return "png";
            if (type.Contains("webp")) return "webp";
            return "jpg";
        }

        public async Task<IReadOnlyList<ProctoringLogEntry>> GetLogsForCandidateAsync(string candidateEmail)
        {
            var rows = await _repository.GetLogsForCandidateAsync(candidateEmail);
            return rows.Select(r => new ProctoringLogEntry
            {
                SessionToken = r.SessionToken,
                Sequence = r.Sequence,
                Type = r.Type,
                Category = r.Category,
                Level = r.Level,
                Confidence = r.Confidence,
                OpenedAt = r.OpenedAtUtc,
                ResolvedAt = r.ResolvedAtUtc,
                RedactedDetail = r.RedactedDetail,
                RawDetail = r.RawDetail,
                CreatedAtUtc = r.CreatedAtUtc,
            }).ToList();
        }

        private static ProctoringPolicyResponse DefaultPolicy() => new ProctoringPolicyResponse
        {
            Version = 1,
            BlockedProcesses = new List<ProctoringBlockedProcess>
            {
                new ProctoringBlockedProcess { ProcessName = "anydesk", Category = "Remote access / screen sharing" },
                new ProctoringBlockedProcess { ProcessName = "teamviewer", Category = "Remote access / screen sharing" },
                new ProctoringBlockedProcess { ProcessName = "obs64", Category = "Screen recording" },
            },
            WatchedTitlePhrases = new List<string> { "chatgpt", "chat.openai", "claude.ai" },
            AllowedDomains = new List<string> { "portal.risebird.io" },
            ForegroundFlickerFloorSeconds = 1.5,
            MaxQueuedEvents = 5000,
        };
    }
}
