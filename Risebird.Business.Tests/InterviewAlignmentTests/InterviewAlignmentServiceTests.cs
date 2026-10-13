using Microsoft.Extensions.Configuration;
using Moq;
using Risebird.Business;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using Xunit;

namespace Risebird.Business.Tests.InterviewAlignmentTests
{
    public sealed class InterviewAlignmentFixture
    {
        public Mock<IInterviewAlignmentRepository> Repository { get; } = new(MockBehavior.Loose);
        public Mock<ICandidateService> CandidateService { get; } = new(MockBehavior.Loose);
        public Mock<IIRFService> IrfService { get; } = new(MockBehavior.Loose);
        public Mock<ICodeExecutionService> CodeExecution { get; } = new(MockBehavior.Loose);
        public Mock<IApplicationErrorLogger> Logger { get; } = new(MockBehavior.Loose);
        public Dictionary<string, string> Settings { get; } = new();

        public InterviewAlignmentService CreateSut()
        {
            Logger.Setup(x => x.LogMessage(It.IsAny<LogLevelInfo>(), It.IsAny<Exception>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(Settings).Build();
            return new InterviewAlignmentService(Repository.Object, CandidateService.Object, IrfService.Object,
                CodeExecution.Object, configuration, Logger.Object);
        }

        /// <summary>A session whose interview is in progress right now.</summary>
        public static InterviewCodeSession LiveSession(Guid token) => new()
        {
            Id = 1,
            InterviewId = 48210,
            QuestionId = 7,
            QuestionText = "Implement a worker pool that drains cleanly on context cancellation.",
            Token = token,
            CodingTimeLimitInMinutes = 25,
            CreatedOn = DateTime.Now.AddMinutes(-30),
            InterviewDate = DateTime.Now.AddMinutes(-10),
            InterviewDurationMinutes = 75
        };
    }

    public class InterviewAlignmentServiceTests
    {
        [Fact]
        public void IsSessionLive_ExpiresWithTheInterview()
        {
            var sut = new InterviewAlignmentFixture().CreateSut();
            var interviewDate = new DateTime(2026, 10, 22, 15, 30, 0);
            var session = new InterviewCodeSession { InterviewDate = interviewDate, InterviewDurationMinutes = 60, CreatedOn = interviewDate.AddDays(-2) };

            Assert.True(sut.IsSessionLive(session, interviewDate.AddDays(-1)));          // minted before the call
            Assert.True(sut.IsSessionLive(session, interviewDate.AddMinutes(60 + 180)));  // planned end + grace
            Assert.False(sut.IsSessionLive(session, interviewDate.AddMinutes(60 + 181)));
        }

        [Fact]
        public void IsSessionLive_WithoutAPlanDuration_FallsBackToNinetyMinutes()
        {
            var sut = new InterviewAlignmentFixture().CreateSut();
            var interviewDate = new DateTime(2026, 10, 22, 15, 30, 0);
            var session = new InterviewCodeSession { InterviewDate = interviewDate, InterviewDurationMinutes = null };

            Assert.True(sut.IsSessionLive(session, interviewDate.AddMinutes(90 + 180)));
            Assert.False(sut.IsSessionLive(session, interviewDate.AddMinutes(90 + 181)));
        }

        [Fact]
        public void IsSessionLive_UndatedInterview_LivesADayFromCreation()
        {
            var sut = new InterviewAlignmentFixture().CreateSut();
            var created = new DateTime(2026, 10, 12, 9, 0, 0);
            var session = new InterviewCodeSession { InterviewDate = null, CreatedOn = created };

            Assert.True(sut.IsSessionLive(session, created.AddHours(24)));
            Assert.False(sut.IsSessionLive(session, created.AddHours(24).AddMinutes(1)));
            Assert.False(sut.IsSessionLive(null, created));
        }

        [Fact]
        public async Task RunAsync_RunnerDisabled_DoesNotRunOrPersist()
        {
            var fixture = new InterviewAlignmentFixture();
            fixture.CodeExecution.SetupGet(x => x.IsEnabled).Returns(false);
            var sut = fixture.CreateSut();

            var (accepted, message, result) = await sut.RunAsync(new CodeSessionRunRequestVM { Token = Guid.NewGuid(), Language = "go", Code = "package main" });

            Assert.False(accepted);
            Assert.Contains("not available", message);
            Assert.Null(result);
            fixture.CodeExecution.Verify(x => x.ExecuteAsync(It.IsAny<CodeExecutionRequest>()), Times.Never);
            fixture.Repository.Verify(x => x.SaveCodeSessionRunAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
        }

        [Fact]
        public async Task RunAsync_UnknownToken_IsRefused()
        {
            var fixture = new InterviewAlignmentFixture();
            fixture.CodeExecution.SetupGet(x => x.IsEnabled).Returns(true);
            fixture.Repository.Setup(x => x.GetCodeSessionByTokenAsync(It.IsAny<Guid>())).ReturnsAsync((InterviewCodeSession)null);
            var sut = fixture.CreateSut();

            var (accepted, _, _) = await sut.RunAsync(new CodeSessionRunRequestVM { Token = Guid.NewGuid(), Language = "python", Code = "print(1)" });

            Assert.False(accepted);
            fixture.CodeExecution.Verify(x => x.ExecuteAsync(It.IsAny<CodeExecutionRequest>()), Times.Never);
        }

        [Fact]
        public async Task RunAsync_ExpiredSession_IsRefused()
        {
            var fixture = new InterviewAlignmentFixture();
            var token = Guid.NewGuid();
            var session = InterviewAlignmentFixture.LiveSession(token);
            session.InterviewDate = DateTime.Now.AddDays(-2);
            fixture.CodeExecution.SetupGet(x => x.IsEnabled).Returns(true);
            fixture.Repository.Setup(x => x.GetCodeSessionByTokenAsync(token)).ReturnsAsync(session);
            var sut = fixture.CreateSut();

            var (accepted, _, _) = await sut.RunAsync(new CodeSessionRunRequestVM { Token = token, Language = "python", Code = "print(1)" });

            Assert.False(accepted);
            fixture.CodeExecution.Verify(x => x.ExecuteAsync(It.IsAny<CodeExecutionRequest>()), Times.Never);
        }

        [Fact]
        public async Task RunAsync_UnknownLanguage_IsRefused()
        {
            var fixture = new InterviewAlignmentFixture();
            fixture.CodeExecution.SetupGet(x => x.IsEnabled).Returns(true);
            var sut = fixture.CreateSut();

            var (accepted, _, _) = await sut.RunAsync(new CodeSessionRunRequestVM { Token = Guid.NewGuid(), Language = "cobol", Code = "DISPLAY 'HI'." });

            Assert.False(accepted);
            fixture.CodeExecution.Verify(x => x.ExecuteAsync(It.IsAny<CodeExecutionRequest>()), Times.Never);
        }

        [Fact]
        public async Task RunAsync_LiveSession_RunsAndPersistsWhatCameBack()
        {
            var fixture = new InterviewAlignmentFixture();
            var token = Guid.NewGuid();
            // The runner caps output (CodeExecution:MaxOutputCharacters); whatever it returns is stored, not dropped.
            var cappedOutput = new string('x', 20000) + "\n... output truncated ...";
            fixture.CodeExecution.SetupGet(x => x.IsEnabled).Returns(true);
            fixture.CodeExecution.Setup(x => x.ExecuteAsync(It.IsAny<CodeExecutionRequest>()))
                .ReturnsAsync(new CodeExecutionResult { Success = true, Status = "Accepted", CombinedOutput = cappedOutput });
            fixture.Repository.Setup(x => x.GetCodeSessionByTokenAsync(token)).ReturnsAsync(InterviewAlignmentFixture.LiveSession(token));
            var sut = fixture.CreateSut();

            var (accepted, _, result) = await sut.RunAsync(new CodeSessionRunRequestVM { Token = token, Language = "GO", Code = "package main", Stdin = "8" });

            Assert.True(accepted);
            Assert.Equal("Accepted", result.Status);
            fixture.CodeExecution.Verify(x => x.ExecuteAsync(It.Is<CodeExecutionRequest>(r =>
                r.LanguageId == "go" && r.SourceCode == "package main" && r.Stdin == "8")), Times.Once);
            fixture.Repository.Verify(x => x.SaveCodeSessionRunAsync(token, "go", "package main", cappedOutput, "Accepted", It.IsAny<DateTime>()), Times.Once);
        }

        [Fact]
        public async Task SubmitAsync_LiveSession_SavesTheCode()
        {
            var fixture = new InterviewAlignmentFixture();
            var token = Guid.NewGuid();
            fixture.Repository.Setup(x => x.GetCodeSessionByTokenAsync(token)).ReturnsAsync(InterviewAlignmentFixture.LiveSession(token));
            fixture.Repository.Setup(x => x.SubmitCodeSessionAsync(token, "python", "print(1)", It.IsAny<DateTime>())).ReturnsAsync(true);
            var sut = fixture.CreateSut();

            var (accepted, _, submittedOn) = await sut.SubmitAsync(new CodeSessionSubmitRequestVM { Token = token, Language = "python", Code = "print(1)" });

            Assert.True(accepted);
            Assert.NotNull(submittedOn);
        }

        [Fact]
        public async Task SubmitAsync_DoesNotNeedTheRunner()
        {
            var fixture = new InterviewAlignmentFixture();
            var token = Guid.NewGuid();
            fixture.CodeExecution.SetupGet(x => x.IsEnabled).Returns(false);
            fixture.Repository.Setup(x => x.GetCodeSessionByTokenAsync(token)).ReturnsAsync(InterviewAlignmentFixture.LiveSession(token));
            fixture.Repository.Setup(x => x.SubmitCodeSessionAsync(token, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>())).ReturnsAsync(true);
            var sut = fixture.CreateSut();

            var (accepted, _, _) = await sut.SubmitAsync(new CodeSessionSubmitRequestVM { Token = token, Language = "python", Code = "print(1)" });

            Assert.True(accepted);
        }

        [Fact]
        public async Task SubmitAsync_ExpiredSession_IsRefused()
        {
            var fixture = new InterviewAlignmentFixture();
            var token = Guid.NewGuid();
            var session = InterviewAlignmentFixture.LiveSession(token);
            session.InterviewDate = DateTime.Now.AddDays(-3);
            fixture.Repository.Setup(x => x.GetCodeSessionByTokenAsync(token)).ReturnsAsync(session);
            var sut = fixture.CreateSut();

            var (accepted, _, _) = await sut.SubmitAsync(new CodeSessionSubmitRequestVM { Token = token, Language = "python", Code = "print(1)" });

            Assert.False(accepted);
            fixture.Repository.Verify(x => x.SubmitCodeSessionAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
        }

        [Fact]
        public async Task SaveJoinGateOverride_StoresOnlyKnownRequirements()
        {
            var fixture = new InterviewAlignmentFixture();
            fixture.Repository.Setup(x => x.SaveJoinGateOverrideAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
                .ReturnsAsync(true);
            var sut = fixture.CreateSut();

            await sut.SaveJoinGateOverrideAsync(48210, " rash@example.com ", new[] { "Rubric", "issues", "<script>", "rubric", "" });

            fixture.Repository.Verify(x => x.SaveJoinGateOverrideAsync(48210, "rash@example.com", "rubric,issues", It.IsAny<DateTime>()), Times.Once);
        }

        [Fact]
        public async Task AcknowledgeTrialOnboarding_UsesTheConfiguredPolicyVersion()
        {
            var fixture = new InterviewAlignmentFixture();
            fixture.Settings["InterviewAlignment:TrialPolicyVersion"] = "2026-11";
            fixture.Repository.Setup(x => x.AcknowledgeTrialOnboardingAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>())).ReturnsAsync(true);
            var sut = fixture.CreateSut();

            Assert.True(await sut.AcknowledgeTrialOnboardingAsync("rash@example.com"));
            Assert.False(await sut.AcknowledgeTrialOnboardingAsync(" "));
            fixture.Repository.Verify(x => x.AcknowledgeTrialOnboardingAsync("rash@example.com", "2026-11", It.IsAny<DateTime>()), Times.Once);
        }

        [Fact]
        public async Task BuildAlignment_WhenContextIsUnavailable_ReturnsNullSoThePageFallsBack()
        {
            var fixture = new InterviewAlignmentFixture();
            fixture.Repository.Setup(x => x.GetAlignmentContextAsync(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync((InterviewAlignmentContext)null);
            var sut = fixture.CreateSut();

            var model = await sut.BuildAlignmentAsync(48210, "rash@example.com", new Meeting(), 15, 5000);

            Assert.Null(model);
        }

        [Fact]
        public async Task BuildAlignment_WhenSomethingThrows_ReturnsNullSoThePageFallsBack()
        {
            var fixture = new InterviewAlignmentFixture();
            fixture.Repository.Setup(x => x.GetAlignmentContextAsync(It.IsAny<int>(), It.IsAny<string>())).ThrowsAsync(new InvalidOperationException());
            var sut = fixture.CreateSut();

            var model = await sut.BuildAlignmentAsync(48210, "rash@example.com", new Meeting(), 15, 5000);

            Assert.Null(model);
        }

        [Theory]
        [InlineData(true, false, true)]   // trial, never acknowledged -> modal opens
        [InlineData(true, true, false)]   // trial, already acknowledged -> banner only
        [InlineData(false, false, false)] // not a trial (or trials off for the channel)
        public async Task BuildAlignment_ShowsOnboardingOnlyForAnUnacknowledgedTrial(bool isTrial, bool acknowledged, bool expectModal)
        {
            var fixture = new InterviewAlignmentFixture();
            fixture.Repository.Setup(x => x.GetAlignmentContextAsync(48210, "rash@example.com")).ReturnsAsync(new InterviewAlignmentContext
            {
                IRFIntId = 10482,
                IRFCode = "IRF10482",
                IsTrialInterview = isTrial,
                AITrialEnabled = isTrial,
                TrialOnboardingAcknowledged = acknowledged
            });
            var sut = fixture.CreateSut();

            var model = await sut.BuildAlignmentAsync(48210, "rash@example.com", new Meeting(), 15, 5000);

            Assert.Equal(isTrial, model.IsTrialInterview);
            Assert.Equal(expectModal, model.ShowTrialOnboarding);
        }

        [Fact]
        public async Task BuildAlignment_ShowsTheCodedIdentity_NeverTheRealName()
        {
            var fixture = new InterviewAlignmentFixture();
            fixture.Repository.Setup(x => x.GetAlignmentContextAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(new InterviewAlignmentContext { IRFIntId = 10482, RoleTitle = " Senior Backend Engineer ", DurationMinutes = 75 });
            fixture.CandidateService.Setup(x => x.EnsureCandidatePseudonymsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<string>()))
                .ReturnsAsync(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["ada.lovelace@example.com"] = "AdLo1234" });
            var sut = fixture.CreateSut();

            var model = await sut.BuildAlignmentAsync(48210, "rash@example.com",
                new Meeting { candidateEmail = "ada.lovelace@example.com", candidateName = "Ada Lovelace" }, 15, 5000);

            Assert.Equal("AdLo1234", model.CandidateCodedName);
            Assert.Equal("Senior Backend Engineer", model.RoleTitle);
            Assert.Equal(75, model.DurationMinutes);
            fixture.CandidateService.Verify(x => x.EnsureCandidatePseudonymsAsync(
                It.Is<IEnumerable<string>>(e => e.Single() == "ada.lovelace@example.com"), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task BuildAlignment_WhenTheMeetingHasNoCandidate_UsesTheInterviewsCandidateEmailForTheCodedName()
        {
            var fixture = new InterviewAlignmentFixture();
            fixture.Repository.Setup(x => x.GetAlignmentContextAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(new InterviewAlignmentContext { IRFIntId = 10482, CandidateEmail = " mary.curie@example.com " });
            fixture.CandidateService.Setup(x => x.EnsureCandidatePseudonymsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<string>()))
                .ReturnsAsync(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["mary.curie@example.com"] = "MaCu5106" });
            var sut = fixture.CreateSut();

            // usp_FetchAudioComment returned nothing, so the page's Meeting is empty.
            var model = await sut.BuildAlignmentAsync(48210, "rash@example.com", new Meeting(), 15, 5000);

            Assert.Equal("MaCu5106", model.CandidateCodedName);
        }

        [Fact]
        public async Task CandidatePage_ExpiredToken_IsMarkedExpiredAndDoesNotStartTheClock()
        {
            var fixture = new InterviewAlignmentFixture();
            var token = Guid.NewGuid();
            var session = InterviewAlignmentFixture.LiveSession(token);
            session.InterviewDate = DateTime.Now.AddDays(-2);
            fixture.Repository.Setup(x => x.GetCodeSessionByTokenAsync(token)).ReturnsAsync(session);
            var sut = fixture.CreateSut();

            var page = await sut.GetCandidatePageAsync(token);

            Assert.True(page.IsExpired);
            fixture.Repository.Verify(x => x.MarkCodeSessionOpenedAsync(It.IsAny<Guid>(), It.IsAny<DateTime>()), Times.Never);
        }

        [Fact]
        public async Task CandidatePage_FirstOpen_StartsTheClock()
        {
            var fixture = new InterviewAlignmentFixture();
            var token = Guid.NewGuid();
            var opened = DateTime.Now;
            fixture.Repository.Setup(x => x.GetCodeSessionByTokenAsync(token)).ReturnsAsync(InterviewAlignmentFixture.LiveSession(token));
            fixture.Repository.Setup(x => x.MarkCodeSessionOpenedAsync(token, It.IsAny<DateTime>())).ReturnsAsync(opened);
            fixture.CodeExecution.SetupGet(x => x.IsEnabled).Returns(true);
            var sut = fixture.CreateSut();

            var page = await sut.GetCandidatePageAsync(token);

            Assert.False(page.IsExpired);
            Assert.Equal(opened, page.OpenedOn);
            Assert.True(page.RunEnabled);
            Assert.NotEmpty(page.Languages);
        }
    }
}
