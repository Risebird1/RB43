using Moq;
using Risebird.Business.Tests.Fixtures;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;

namespace Risebird.Business.Tests.ProctoringTests
{
    public class ProctoringApiServiceMintSessionTests
    {
        private static ProctoringMintSessionRequest Request(int? ttlMinutes = null) => new()
        {
            InterviewId = "12345",
            CandidateEmail = "candidate@risebird.io",
            BrowserSessionId = "browser-1",
            TtlMinutes = ttlMinutes
        };

        [Fact]
        public async Task Returns_the_minted_token_and_the_api_base_url()
        {
            var fixture = new ProctoringApiServiceFixture();
            var sut = fixture.CreateSut();

            var response = await sut.MintSessionAsync(Request());

            Assert.NotNull(response);
            Assert.Equal(fixture.MintedToken, response.Token);
            Assert.Equal(ProctoringApiServiceFixture.DefaultApiBaseUrl, response.ApiBaseUrl);
            Assert.False(string.IsNullOrWhiteSpace(response.SessionToken));
        }

        [Fact]
        public async Task Generates_a_thirty_two_character_hex_session_token()
        {
            var sut = new ProctoringApiServiceFixture().CreateSut();

            var response = await sut.MintSessionAsync(Request());

            Assert.Equal(32, response.SessionToken.Length);
            Assert.All(response.SessionToken, c => Assert.True(Uri.IsHexDigit(c)));
        }

        [Fact]
        public async Task Generates_a_fresh_session_token_per_request()
        {
            var sut = new ProctoringApiServiceFixture().CreateSut();

            var first = await sut.MintSessionAsync(Request());
            var second = await sut.MintSessionAsync(Request());

            Assert.NotEqual(first.SessionToken, second.SessionToken);
        }

        [Fact]
        public async Task Defaults_the_ttl_to_one_hundred_eighty_minutes()
        {
            var fixture = new ProctoringApiServiceFixture();
            var sut = fixture.CreateSut();

            await sut.MintSessionAsync(Request());

            Assert.Equal(TimeSpan.FromMinutes(180), fixture.LastMintedTtl);
        }

        [Fact]
        public async Task Honours_the_configured_default_ttl()
        {
            var fixture = new ProctoringApiServiceFixture()
                .WithSetting("ProctoringApiSettings:DefaultTokenTtlMinutes", "45");
            var sut = fixture.CreateSut();

            await sut.MintSessionAsync(Request());

            Assert.Equal(TimeSpan.FromMinutes(45), fixture.LastMintedTtl);
        }

        [Fact]
        public async Task An_explicit_ttl_overrides_the_configured_default()
        {
            var fixture = new ProctoringApiServiceFixture()
                .WithSetting("ProctoringApiSettings:DefaultTokenTtlMinutes", "45");
            var sut = fixture.CreateSut();

            await sut.MintSessionAsync(Request(ttlMinutes: 10));

            Assert.Equal(TimeSpan.FromMinutes(10), fixture.LastMintedTtl);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-600)]
        public async Task A_non_positive_requested_ttl_falls_back_to_the_default(int ttlMinutes)
        {
            var fixture = new ProctoringApiServiceFixture();
            var sut = fixture.CreateSut();

            await sut.MintSessionAsync(Request(ttlMinutes));

            Assert.Equal(TimeSpan.FromMinutes(180), fixture.LastMintedTtl);
        }

        [Fact]
        public async Task Registers_the_session_before_returning_it_to_the_client()
        {
            var fixture = new ProctoringApiServiceFixture();
            var sut = fixture.CreateSut();

            var response = await sut.MintSessionAsync(Request());

            Assert.NotNull(fixture.RegisteredSession);
            Assert.Equal(response.SessionToken, fixture.RegisteredSession.SessionToken);
            Assert.Equal("12345", fixture.RegisteredSession.InterviewId);
            Assert.Equal("candidate@risebird.io", fixture.RegisteredSession.CandidateEmail);
            Assert.Equal("browser-1", fixture.RegisteredSession.BrowserSessionId);
            Assert.Equal(fixture.MintedExpiry, fixture.RegisteredSession.ExpiresAtUtc);
            fixture.Repository.Verify(x => x.RegisterSessionAsync(It.IsAny<ProctoringApiSession>()), Times.Once);
        }

        [Fact]
        public async Task Registers_the_session_as_verified_because_mint_validation_is_currently_disabled()
        {
            // Pinned deliberately: candidate/interview validation is switched off, so every
            // minted session is trusted. If validation is re-enabled this must change.
            var fixture = new ProctoringApiServiceFixture();
            var sut = fixture.CreateSut();

            await sut.MintSessionAsync(Request());

            Assert.Equal(ProctoringTrust.Verified.ToWireString(), fixture.RegisteredSession.Trust);
        }

        [Fact]
        public async Task Never_calls_the_candidate_interview_validation_stored_procedure()
        {
            var fixture = new ProctoringApiServiceFixture();
            var sut = fixture.CreateSut();

            await sut.MintSessionAsync(Request());

            fixture.Repository.Verify(
                x => x.ValidateCandidateInterviewAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Passes_the_generated_session_token_and_base_url_into_the_signer()
        {
            var fixture = new ProctoringApiServiceFixture();
            var sut = fixture.CreateSut();

            var response = await sut.MintSessionAsync(Request());

            Assert.Equal(response.SessionToken, fixture.LastMintedSessionToken);
            Assert.Equal(ProctoringApiServiceFixture.DefaultApiBaseUrl, fixture.LastMintedApiBaseUrl);
        }

        [Fact]
        public async Task Tolerates_a_missing_api_base_url_setting()
        {
            var fixture = new ProctoringApiServiceFixture().WithoutApiBaseUrl();
            var sut = fixture.CreateSut();

            var response = await sut.MintSessionAsync(Request());

            Assert.Null(response.ApiBaseUrl);
            Assert.Null(fixture.LastMintedApiBaseUrl);
        }

        [Fact]
        public async Task Accepts_a_request_with_blank_identifiers_because_validation_is_disabled()
        {
            var fixture = new ProctoringApiServiceFixture();
            var sut = fixture.CreateSut();

            var response = await sut.MintSessionAsync(new ProctoringMintSessionRequest());

            Assert.NotNull(response);
            Assert.Null(fixture.RegisteredSession.InterviewId);
            Assert.Null(fixture.RegisteredSession.CandidateEmail);
        }

        [Fact]
        public async Task A_null_request_throws_rather_than_minting_an_empty_session()
        {
            var sut = new ProctoringApiServiceFixture().CreateSut();

            await Assert.ThrowsAnyAsync<Exception>(() => sut.MintSessionAsync(null));
        }
    }

    public class ProctoringApiServiceJoinTests
    {
        [Fact]
        public async Task Returns_null_for_an_unrecognised_session_token()
        {
            var fixture = new ProctoringApiServiceFixture();
            fixture.GivenNoSession();
            var sut = fixture.CreateSut();

            var response = await sut.JoinAsync(new ProctoringJoinRequest { SessionToken = "nope" });

            Assert.Null(response);
            fixture.Repository.Verify(
                x => x.MarkSessionJoinedAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Marks_the_session_joined_and_echoes_the_current_risk_score()
        {
            var fixture = new ProctoringApiServiceFixture();
            fixture.GivenSession(riskScore: 15);
            var sut = fixture.CreateSut();

            var response = await sut.JoinAsync(new ProctoringJoinRequest
            {
                SessionToken = "known-session",
                CandidateEmail = "candidate@risebird.io"
            });

            Assert.NotNull(response);
            Assert.Equal("known-session", response.SessionId);
            Assert.Equal(15, response.RiskScore);
            fixture.Repository.Verify(
                x => x.MarkSessionJoinedAsync("known-session", "candidate@risebird.io"), Times.Once);
        }

        [Fact]
        public async Task Joining_twice_is_allowed_and_stays_idempotent_from_the_services_point_of_view()
        {
            var fixture = new ProctoringApiServiceFixture();
            fixture.GivenSession(joinedAtUtc: new DateTime(2026, 09, 09, 12, 05, 00, DateTimeKind.Utc));
            var sut = fixture.CreateSut();

            var first = await sut.JoinAsync(new ProctoringJoinRequest { SessionToken = "known-session" });
            var second = await sut.JoinAsync(new ProctoringJoinRequest { SessionToken = "known-session" });

            Assert.NotNull(first);
            Assert.NotNull(second);
            fixture.Repository.Verify(
                x => x.MarkSessionJoinedAsync("known-session", It.IsAny<string>()), Times.Exactly(2));
        }
    }

    public class ProctoringApiServiceStartTests
    {
        [Fact]
        public async Task Returns_null_for_an_unrecognised_session_token()
        {
            var fixture = new ProctoringApiServiceFixture();
            fixture.GivenNoSession();
            var sut = fixture.CreateSut();

            Assert.Null(await sut.StartAsync("nope"));
            fixture.Repository.Verify(x => x.MarkSessionStartedAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Refuses_to_start_a_session_that_was_never_joined()
        {
            var fixture = new ProctoringApiServiceFixture();
            fixture.GivenSession(joinedAtUtc: null);
            var sut = fixture.CreateSut();

            Assert.Null(await sut.StartAsync("known-session"));
            fixture.Repository.Verify(x => x.MarkSessionStartedAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Starts_a_joined_session_and_echoes_the_risk_score()
        {
            var fixture = new ProctoringApiServiceFixture();
            fixture.GivenSession(riskScore: 30, joinedAtUtc: new DateTime(2026, 09, 09, 12, 05, 00, DateTimeKind.Utc));
            var sut = fixture.CreateSut();

            var response = await sut.StartAsync("known-session");

            Assert.NotNull(response);
            Assert.Equal("known-session", response.SessionId);
            Assert.Equal(30, response.RiskScore);
            fixture.Repository.Verify(x => x.MarkSessionStartedAsync("known-session"), Times.Once);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task A_blank_token_is_looked_up_and_rejected(string sessionToken)
        {
            var fixture = new ProctoringApiServiceFixture();
            fixture.GivenNoSession();
            var sut = fixture.CreateSut();

            Assert.Null(await sut.StartAsync(sessionToken));
        }
    }

    public class ProctoringApiServiceHeartbeatTests
    {
        [Fact]
        public async Task Returns_null_for_an_unrecognised_session_token()
        {
            var fixture = new ProctoringApiServiceFixture();
            fixture.GivenNoSession();
            var sut = fixture.CreateSut();

            var response = await sut.HeartbeatAsync(new ProctoringHeartbeatRequest { SessionToken = "nope" });

            Assert.Null(response);
            fixture.Repository.Verify(
                x => x.UpdateHeartbeatAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Theory]
        [InlineData(ProctoringSessionState.Idle, "idle")]
        [InlineData(ProctoringSessionState.Starting, "starting")]
        [InlineData(ProctoringSessionState.Running, "running")]
        [InlineData(ProctoringSessionState.Degraded, "degraded")]
        [InlineData(ProctoringSessionState.Faulted, "faulted")]
        [InlineData(ProctoringSessionState.Stopped, "stopped")]
        public async Task Persists_the_client_state_as_its_camel_case_wire_string(
            ProctoringSessionState clientState, string expectedWire)
        {
            var fixture = new ProctoringApiServiceFixture();
            fixture.GivenSession(riskScore: 7);
            var sut = fixture.CreateSut();

            var response = await sut.HeartbeatAsync(new ProctoringHeartbeatRequest
            {
                SessionToken = "known-session",
                ClientState = clientState
            });

            Assert.NotNull(response);
            Assert.Equal(7, response.RiskScore);
            Assert.Equal(clientState, response.ClientState);
            fixture.Repository.Verify(
                x => x.UpdateHeartbeatAsync("known-session", expectedWire), Times.Once);
        }

        [Fact]
        public async Task Returns_the_risk_score_as_it_was_before_the_heartbeat()
        {
            // A heartbeat never changes the score — only events do.
            var fixture = new ProctoringApiServiceFixture();
            fixture.GivenSession(riskScore: 55);
            var sut = fixture.CreateSut();

            var response = await sut.HeartbeatAsync(new ProctoringHeartbeatRequest
            {
                SessionToken = "known-session",
                ClientState = ProctoringSessionState.Running
            });

            Assert.Equal(55, response.RiskScore);
            fixture.Repository.Verify(
                x => x.AddRiskScoreAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task An_undefined_client_state_throws_rather_than_writing_a_bogus_value()
        {
            var fixture = new ProctoringApiServiceFixture();
            fixture.GivenSession();
            var sut = fixture.CreateSut();

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                sut.HeartbeatAsync(new ProctoringHeartbeatRequest
                {
                    SessionToken = "known-session",
                    ClientState = (ProctoringSessionState)99
                }));
        }
    }

    public class ProctoringApiServicePolicyTests
    {
        [Fact]
        public async Task Returns_the_built_in_policy_when_none_is_configured()
        {
            var sut = new ProctoringApiServiceFixture().CreateSut();

            var policy = await sut.GetPolicyAsync();

            Assert.NotNull(policy);
            Assert.Equal(1, policy.Version);
            Assert.Equal(3, policy.BlockedProcesses.Count);
            Assert.Equal(1.5, policy.ForegroundFlickerFloorSeconds);
            Assert.Equal(5000, policy.MaxQueuedEvents);
        }

        [Fact]
        public async Task Built_in_policy_blocks_the_usual_remote_access_and_recording_tools()
        {
            var sut = new ProctoringApiServiceFixture().CreateSut();

            var policy = await sut.GetPolicyAsync();

            var names = policy.BlockedProcesses.Select(p => p.ProcessName).ToList();
            Assert.Contains("anydesk", names);
            Assert.Contains("teamviewer", names);
            Assert.Contains("obs64", names);
            Assert.All(policy.BlockedProcesses, p => Assert.False(string.IsNullOrWhiteSpace(p.Category)));
        }

        [Fact]
        public async Task Built_in_policy_watches_the_usual_assistant_window_titles()
        {
            var sut = new ProctoringApiServiceFixture().CreateSut();

            var policy = await sut.GetPolicyAsync();

            Assert.Contains("chatgpt", policy.WatchedTitlePhrases);
            Assert.Contains("chat.openai", policy.WatchedTitlePhrases);
            Assert.Contains("claude.ai", policy.WatchedTitlePhrases);
        }

        [Fact]
        public async Task Configuration_replaces_the_built_in_policy_entirely()
        {
            var fixture = new ProctoringApiServiceFixture()
                .WithSetting("ProctoringApiSettings:Policy:Version", "7")
                .WithSetting("ProctoringApiSettings:Policy:ForegroundFlickerFloorSeconds", "2.5")
                .WithSetting("ProctoringApiSettings:Policy:MaxQueuedEvents", "100")
                .WithSetting("ProctoringApiSettings:Policy:BlockedProcesses:0:ProcessName", "vnc")
                .WithSetting("ProctoringApiSettings:Policy:BlockedProcesses:0:Category", "Remote access")
                .WithSetting("ProctoringApiSettings:Policy:WatchedTitlePhrases:0", "gemini");
            var sut = fixture.CreateSut();

            var policy = await sut.GetPolicyAsync();

            Assert.Equal(7, policy.Version);
            Assert.Equal(2.5, policy.ForegroundFlickerFloorSeconds);
            Assert.Equal(100, policy.MaxQueuedEvents);
            Assert.Single(policy.BlockedProcesses);
            Assert.Equal("vnc", policy.BlockedProcesses[0].ProcessName);
            Assert.Equal(new[] { "gemini" }, policy.WatchedTitlePhrases);
        }

        [Fact]
        public async Task Policy_is_stable_across_calls()
        {
            var sut = new ProctoringApiServiceFixture().CreateSut();

            var first = await sut.GetPolicyAsync();
            var second = await sut.GetPolicyAsync();

            Assert.Equal(first.Version, second.Version);
            Assert.Equal(first.BlockedProcesses.Count, second.BlockedProcesses.Count);
        }
    }

    public class ProctoringApiServiceSubmitEventsTests
    {
        private static ProctoringEventItem Event(
            int sequence,
            ProctoringSeverity level = ProctoringSeverity.High,
            ProctoringConfidence confidence = ProctoringConfidence.High) => new()
            {
                Sequence = sequence,
                Type = "process",
                Category = "Remote access",
                Level = level,
                Confidence = confidence,
                OpenedAt = new DateTime(2026, 09, 09, 12, 10, 00, DateTimeKind.Utc),
                RedactedDetail = "anydesk running",
                RawDetail = "anydesk.exe pid 1234"
            };

        [Fact]
        public async Task Returns_null_for_an_unrecognised_session_token()
        {
            var fixture = new ProctoringApiServiceFixture();
            fixture.GivenNoSession();
            var sut = fixture.CreateSut();

            var response = await sut.SubmitEventsAsync(new ProctoringEventsRequest { SessionToken = "nope" });

            Assert.Null(response);
            fixture.Repository.Verify(x => x.InsertLogIfNewAsync(It.IsAny<ProctoringApiLog>()), Times.Never);
        }

        [Fact]
        public async Task An_empty_batch_returns_the_stored_score_without_touching_it()
        {
            var fixture = new ProctoringApiServiceFixture();
            fixture.GivenSession(riskScore: 42);
            var sut = fixture.CreateSut();

            var response = await sut.SubmitEventsAsync(new ProctoringEventsRequest
            {
                SessionToken = "known-session",
                Events = new List<ProctoringEventItem>()
            });

            Assert.Equal(42, response.RiskScore);
            fixture.Repository.Verify(
                x => x.AddRiskScoreAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task A_null_event_list_is_treated_as_an_empty_batch()
        {
            var fixture = new ProctoringApiServiceFixture();
            fixture.GivenSession(riskScore: 42);
            var sut = fixture.CreateSut();

            var response = await sut.SubmitEventsAsync(new ProctoringEventsRequest
            {
                SessionToken = "known-session",
                Events = null
            });

            Assert.Equal(42, response.RiskScore);
            fixture.Repository.Verify(x => x.InsertLogIfNewAsync(It.IsAny<ProctoringApiLog>()), Times.Never);
        }

        [Fact]
        public async Task Persists_every_event_field_including_the_wire_encoded_enums()
        {
            var fixture = new ProctoringApiServiceFixture();
            fixture.GivenSession();
            var sut = fixture.CreateSut();

            await sut.SubmitEventsAsync(new ProctoringEventsRequest
            {
                SessionToken = "known-session",
                Events = new List<ProctoringEventItem>
                {
                    Event(1, ProctoringSeverity.Medium, ProctoringConfidence.Low)
                }
            });

            var log = Assert.Single(fixture.InsertedLogs);
            Assert.Equal("known-session", log.SessionToken);
            Assert.Equal(1, log.Sequence);
            Assert.Equal("process", log.Type);
            Assert.Equal("Remote access", log.Category);
            Assert.Equal("medium", log.Level);
            Assert.Equal("low", log.Confidence);
            Assert.Equal(new DateTime(2026, 09, 09, 12, 10, 00, DateTimeKind.Utc), log.OpenedAtUtc);
            Assert.Null(log.ResolvedAtUtc);
            Assert.Equal("anydesk running", log.RedactedDetail);
            Assert.Equal("anydesk.exe pid 1234", log.RawDetail);
        }

        [Fact]
        public async Task Adds_the_summed_risk_delta_for_newly_inserted_events()
        {
            var fixture = new ProctoringApiServiceFixture();
            fixture.GivenSession(riskScore: 10);
            var sut = fixture.CreateSut();
            fixture.Repository.Setup(x => x.AddRiskScoreAsync("known-session", It.IsAny<int>()))
                .ReturnsAsync(70);

            var response = await sut.SubmitEventsAsync(new ProctoringEventsRequest
            {
                SessionToken = "known-session",
                Events = new List<ProctoringEventItem>
                {
                    Event(1, ProctoringSeverity.High, ProctoringConfidence.High),   // 30
                    Event(2, ProctoringSeverity.Medium, ProctoringConfidence.High)  // 15
                }
            });

            Assert.Equal(70, response.RiskScore);
            fixture.Repository.Verify(x => x.AddRiskScoreAsync("known-session", 45), Times.Once);
        }

        [Fact]
        public async Task A_redelivered_batch_scores_nothing_and_leaves_the_score_alone()
        {
            var fixture = new ProctoringApiServiceFixture();
            fixture.GivenSession(riskScore: 25);
            var sut = fixture.CreateSut();
            fixture.Repository.Setup(x => x.InsertLogIfNewAsync(It.IsAny<ProctoringApiLog>()))
                .ReturnsAsync(false);

            var response = await sut.SubmitEventsAsync(new ProctoringEventsRequest
            {
                SessionToken = "known-session",
                Events = new List<ProctoringEventItem> { Event(1), Event(2) }
            });

            Assert.Equal(25, response.RiskScore);
            fixture.Repository.Verify(
                x => x.AddRiskScoreAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task A_partially_redelivered_batch_only_scores_the_new_events()
        {
            var fixture = new ProctoringApiServiceFixture();
            fixture.GivenSession(riskScore: 0);
            var sut = fixture.CreateSut();
            fixture.Repository.Setup(x => x.InsertLogIfNewAsync(It.Is<ProctoringApiLog>(l => l.Sequence == 1)))
                .ReturnsAsync(false);
            fixture.Repository.Setup(x => x.InsertLogIfNewAsync(It.Is<ProctoringApiLog>(l => l.Sequence == 2)))
                .ReturnsAsync(true);
            fixture.Repository.Setup(x => x.AddRiskScoreAsync("known-session", It.IsAny<int>()))
                .ReturnsAsync(30);

            var response = await sut.SubmitEventsAsync(new ProctoringEventsRequest
            {
                SessionToken = "known-session",
                Events = new List<ProctoringEventItem> { Event(1), Event(2) }
            });

            Assert.Equal(30, response.RiskScore);
            fixture.Repository.Verify(x => x.AddRiskScoreAsync("known-session", 30), Times.Once);
        }

        [Fact]
        public async Task Info_level_events_are_stored_but_score_nothing()
        {
            var fixture = new ProctoringApiServiceFixture();
            fixture.GivenSession(riskScore: 5);
            var sut = fixture.CreateSut();

            var response = await sut.SubmitEventsAsync(new ProctoringEventsRequest
            {
                SessionToken = "known-session",
                Events = new List<ProctoringEventItem>
                {
                    Event(1, ProctoringSeverity.Info, ProctoringConfidence.High)
                }
            });

            Assert.Single(fixture.InsertedLogs);
            Assert.Equal(5, response.RiskScore);
            fixture.Repository.Verify(
                x => x.AddRiskScoreAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task Events_are_inserted_in_submission_order()
        {
            var fixture = new ProctoringApiServiceFixture();
            fixture.GivenSession();
            var sut = fixture.CreateSut();

            await sut.SubmitEventsAsync(new ProctoringEventsRequest
            {
                SessionToken = "known-session",
                Events = new List<ProctoringEventItem> { Event(3), Event(1), Event(2) }
            });

            Assert.Equal(new[] { 3, 1, 2 }, fixture.InsertedLogs.Select(l => l.Sequence));
        }

        [Fact]
        public async Task Carries_a_resolved_timestamp_through_when_the_client_supplies_one()
        {
            var fixture = new ProctoringApiServiceFixture();
            fixture.GivenSession();
            var sut = fixture.CreateSut();
            var resolvedAt = new DateTime(2026, 09, 09, 12, 12, 00, DateTimeKind.Utc);

            var evt = Event(1);
            evt.ResolvedAt = resolvedAt;

            await sut.SubmitEventsAsync(new ProctoringEventsRequest
            {
                SessionToken = "known-session",
                Events = new List<ProctoringEventItem> { evt }
            });

            Assert.Equal(resolvedAt, Assert.Single(fixture.InsertedLogs).ResolvedAtUtc);
        }

        [Fact]
        public async Task An_undefined_severity_throws_rather_than_storing_a_bogus_level()
        {
            var fixture = new ProctoringApiServiceFixture();
            fixture.GivenSession();
            var sut = fixture.CreateSut();

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                sut.SubmitEventsAsync(new ProctoringEventsRequest
                {
                    SessionToken = "known-session",
                    Events = new List<ProctoringEventItem> { Event(1, (ProctoringSeverity)99) }
                }));
        }
    }

    public class ProctoringApiServiceEndTests
    {
        [Fact]
        public async Task Returns_false_for_an_unrecognised_session_token()
        {
            var fixture = new ProctoringApiServiceFixture();
            fixture.GivenNoSession();
            var sut = fixture.CreateSut();

            Assert.False(await sut.EndAsync("nope"));
            fixture.Repository.Verify(x => x.MarkSessionEndedAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Marks_a_known_session_ended()
        {
            var fixture = new ProctoringApiServiceFixture();
            fixture.GivenSession();
            var sut = fixture.CreateSut();

            Assert.True(await sut.EndAsync("known-session"));
            fixture.Repository.Verify(x => x.MarkSessionEndedAsync("known-session"), Times.Once);
        }

        [Fact]
        public async Task Ending_a_session_that_was_never_joined_still_succeeds()
        {
            var fixture = new ProctoringApiServiceFixture();
            fixture.GivenSession(joinedAtUtc: null);
            var sut = fixture.CreateSut();

            Assert.True(await sut.EndAsync("known-session"));
        }

        [Fact]
        public async Task Ending_twice_is_permitted()
        {
            var fixture = new ProctoringApiServiceFixture();
            fixture.GivenSession();
            var sut = fixture.CreateSut();

            Assert.True(await sut.EndAsync("known-session"));
            Assert.True(await sut.EndAsync("known-session"));
            fixture.Repository.Verify(x => x.MarkSessionEndedAsync("known-session"), Times.Exactly(2));
        }
    }

    public class ProctoringApiServiceGetLogsTests
    {
        [Fact]
        public async Task Returns_an_empty_list_when_the_candidate_has_no_logs()
        {
            var sut = new ProctoringApiServiceFixture().CreateSut();

            var logs = await sut.GetLogsForCandidateAsync("candidate@risebird.io");

            Assert.NotNull(logs);
            Assert.Empty(logs);
        }

        [Fact]
        public async Task Maps_every_stored_column_onto_the_view_model()
        {
            var fixture = new ProctoringApiServiceFixture();
            var sut = fixture.CreateSut();
            fixture.Repository.Setup(x => x.GetLogsForCandidateAsync("candidate@risebird.io"))
                .ReturnsAsync((IReadOnlyList<ProctoringApiLog>)new List<ProctoringApiLog>
                {
                    new()
                    {
                        Id = 1,
                        SessionToken = "s1",
                        Sequence = 4,
                        Type = "process",
                        Category = "Remote access",
                        Level = "high",
                        Confidence = "medium",
                        OpenedAtUtc = new DateTime(2026, 09, 09, 12, 10, 00, DateTimeKind.Utc),
                        ResolvedAtUtc = new DateTime(2026, 09, 09, 12, 11, 00, DateTimeKind.Utc),
                        RedactedDetail = "redacted",
                        RawDetail = "raw",
                        CreatedAtUtc = new DateTime(2026, 09, 09, 12, 10, 05, DateTimeKind.Utc)
                    }
                });

            var entry = Assert.Single(await sut.GetLogsForCandidateAsync("candidate@risebird.io"));

            Assert.Equal("s1", entry.SessionToken);
            Assert.Equal(4, entry.Sequence);
            Assert.Equal("process", entry.Type);
            Assert.Equal("Remote access", entry.Category);
            Assert.Equal("high", entry.Level);
            Assert.Equal("medium", entry.Confidence);
            Assert.Equal(new DateTime(2026, 09, 09, 12, 10, 00, DateTimeKind.Utc), entry.OpenedAt);
            Assert.Equal(new DateTime(2026, 09, 09, 12, 11, 00, DateTimeKind.Utc), entry.ResolvedAt);
            Assert.Equal("redacted", entry.RedactedDetail);
            Assert.Equal("raw", entry.RawDetail);
            Assert.Equal(new DateTime(2026, 09, 09, 12, 10, 05, DateTimeKind.Utc), entry.CreatedAtUtc);
        }

        [Fact]
        public async Task Preserves_repository_ordering()
        {
            var fixture = new ProctoringApiServiceFixture();
            var sut = fixture.CreateSut();
            fixture.Repository.Setup(x => x.GetLogsForCandidateAsync(It.IsAny<string>()))
                .ReturnsAsync((IReadOnlyList<ProctoringApiLog>)new List<ProctoringApiLog>
                {
                    new() { Sequence = 3 }, new() { Sequence = 1 }, new() { Sequence = 2 }
                });

            var logs = await sut.GetLogsForCandidateAsync("candidate@risebird.io");

            Assert.Equal(new[] { 3, 1, 2 }, logs.Select(l => l.Sequence));
        }

        [Fact]
        public async Task A_null_repository_result_throws_rather_than_returning_an_empty_list()
        {
            // Pinned: GetLogsForCandidateAsync does not null-guard the repository result.
            var fixture = new ProctoringApiServiceFixture();
            var sut = fixture.CreateSut();
            fixture.Repository.Setup(x => x.GetLogsForCandidateAsync(It.IsAny<string>()))
                .ReturnsAsync((IReadOnlyList<ProctoringApiLog>)null);

            await Assert.ThrowsAnyAsync<Exception>(() => sut.GetLogsForCandidateAsync("x@y.z"));
        }
    }
}
