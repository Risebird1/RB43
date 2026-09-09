using System;
using System.Collections.Generic;
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

        private readonly IProctoringApiRepository _repository;
        private readonly IProctoringLaunchTokenService _launchTokenService;
        private readonly IConfiguration _configuration;

        public ProctoringApiService(
            IProctoringApiRepository repository,
            IProctoringLaunchTokenService launchTokenService,
            IConfiguration configuration)
        {
            _repository = repository;
            _launchTokenService = launchTokenService;
            _configuration = configuration;
        }

        public async Task<ProctoringMintSessionResponse> MintSessionAsync(ProctoringMintSessionRequest request)
        {
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
                Trust = request.Trust.ToWireString(),
                ExpiresAtUtc = expiresAtUtc,
            });

            return new ProctoringMintSessionResponse
            {
                LaunchToken = launchToken,
                SessionToken = sessionToken,
                ExpiresAtUtc = expiresAtUtc,
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
            ForegroundFlickerFloorSeconds = 1.5,
            MaxQueuedEvents = 5000,
        };
    }
}
