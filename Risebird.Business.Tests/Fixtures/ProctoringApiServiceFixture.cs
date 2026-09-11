using Microsoft.Extensions.Configuration;
using Moq;
using Risebird.Business;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;

namespace Risebird.Business.Tests.Fixtures
{
    public sealed class ProctoringApiServiceFixture
    {
        public const string DefaultApiBaseUrl = "https://proctor.risebird.io";

        public Mock<IProctoringApiRepository> Repository { get; } = new(MockBehavior.Loose);
        public Mock<IProctoringLaunchTokenService> LaunchTokenService { get; } = new(MockBehavior.Loose);

        /// <summary>Captured argument of the last <c>RegisterSessionAsync</c> call.</summary>
        public ProctoringApiSession RegisteredSession { get; private set; }

        /// <summary>Every <c>InsertLogIfNewAsync</c> payload, in call order.</summary>
        public List<ProctoringApiLog> InsertedLogs { get; } = new();

        public TimeSpan? LastMintedTtl { get; private set; }
        public string LastMintedApiBaseUrl { get; private set; }
        public string LastMintedSessionToken { get; private set; }
        public DateTime MintedExpiry { get; set; } = new(2026, 09, 09, 18, 00, 00, DateTimeKind.Utc);
        public string MintedToken { get; set; } = "v1.payload.signature";

        private readonly Dictionary<string, string> _settings = new();

        public ProctoringApiServiceFixture()
        {
            _settings["ProctoringApiSettings:ApiBaseUrl"] = DefaultApiBaseUrl;
        }

        public ProctoringApiServiceFixture WithSetting(string key, string value)
        {
            _settings[key] = value;
            return this;
        }

        public ProctoringApiServiceFixture WithoutApiBaseUrl()
        {
            _settings.Remove("ProctoringApiSettings:ApiBaseUrl");
            return this;
        }

        public ProctoringApiService CreateSut()
        {
            LaunchTokenService
                .Setup(x => x.MintLaunchToken(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .Returns((string apiBaseUrl, string sessionToken, string _, string __, string ___, TimeSpan ttl) =>
                {
                    LastMintedApiBaseUrl = apiBaseUrl;
                    LastMintedSessionToken = sessionToken;
                    LastMintedTtl = ttl;
                    return (MintedToken, MintedExpiry);
                });

            Repository
                .Setup(x => x.RegisterSessionAsync(It.IsAny<ProctoringApiSession>()))
                .Returns((ProctoringApiSession session) =>
                {
                    RegisteredSession = session;
                    return Task.FromResult(true);
                });

            Repository.Setup(x => x.MarkSessionJoinedAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);
            Repository.Setup(x => x.MarkSessionStartedAsync(It.IsAny<string>())).ReturnsAsync(true);
            Repository.Setup(x => x.MarkSessionEndedAsync(It.IsAny<string>())).ReturnsAsync(true);
            Repository.Setup(x => x.UpdateHeartbeatAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);
            Repository.Setup(x => x.GetLogsForCandidateAsync(It.IsAny<string>()))
                .ReturnsAsync((IReadOnlyList<ProctoringApiLog>)new List<ProctoringApiLog>());
            Repository.Setup(x => x.InsertLogIfNewAsync(It.IsAny<ProctoringApiLog>()))
                .Returns((ProctoringApiLog log) =>
                {
                    InsertedLogs.Add(log);
                    return Task.FromResult(true);
                });
            Repository.Setup(x => x.AddRiskScoreAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(0);

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(_settings)
                .Build();

            return new ProctoringApiService(Repository.Object, LaunchTokenService.Object, configuration);
        }

        /// <summary>Registers a session the repository will return for <paramref name="sessionToken"/>.</summary>
        public ProctoringApiSession GivenSession(
            string sessionToken = "known-session",
            int riskScore = 0,
            DateTime? joinedAtUtc = null,
            DateTime? startedAtUtc = null)
        {
            var session = new ProctoringApiSession
            {
                SessionToken = sessionToken,
                InterviewId = "12345",
                CandidateEmail = "candidate@risebird.io",
                BrowserSessionId = "browser-1",
                Trust = ProctoringTrust.Verified.ToWireString(),
                RiskScore = riskScore,
                JoinedAtUtc = joinedAtUtc,
                StartedAtUtc = startedAtUtc,
                CreatedAtUtc = new DateTime(2026, 09, 09, 12, 00, 00, DateTimeKind.Utc),
                ExpiresAtUtc = new DateTime(2026, 09, 09, 18, 00, 00, DateTimeKind.Utc)
            };

            Repository.Setup(x => x.GetSessionByTokenAsync(sessionToken)).ReturnsAsync(session);
            return session;
        }

        /// <summary>Makes every token lookup miss, as an unrecognised token would.</summary>
        public void GivenNoSession() =>
            Repository.Setup(x => x.GetSessionByTokenAsync(It.IsAny<string>()))
                .ReturnsAsync((ProctoringApiSession)null);
    }
}
