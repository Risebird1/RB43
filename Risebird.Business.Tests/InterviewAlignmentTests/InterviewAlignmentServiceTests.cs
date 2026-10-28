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
            InterviewDurationMinutes = 75,
            IsInterviewerAlignmentRequired = true
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
        public async Task SubmitAsync_AfterSubmission_IsRefusedAndTheSubmittedCodeIsNotReplaced()
        {
            var fixture = new InterviewAlignmentFixture();
            var token = Guid.NewGuid();
            var session = InterviewAlignmentFixture.LiveSession(token);
            session.SubmittedOn = new DateTime(2026, 10, 23, 14, 38, 0);
            session.SubmittedSource = "print('candidate')";
            fixture.Repository.Setup(x => x.GetCodeSessionByTokenAsync(token)).ReturnsAsync(session);
            var sut = fixture.CreateSut();

            // e.g. an interviewer who opened the candidate's link and edited the code
            var (accepted, message, submittedOn) = await sut.SubmitAsync(new CodeSessionSubmitRequestVM { Token = token, Language = "python", Code = "print('edited')" });

            Assert.False(accepted);
            Assert.Contains("can no longer be changed", message);
            Assert.Equal(session.SubmittedOn, submittedOn);
            fixture.Repository.Verify(x => x.SubmitCodeSessionAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
        }

        [Fact]
        public async Task RunAsync_AfterSubmission_IsRefusedAndNothingIsSaved()
        {
            var fixture = new InterviewAlignmentFixture();
            var token = Guid.NewGuid();
            var session = InterviewAlignmentFixture.LiveSession(token);
            session.SubmittedOn = DateTime.Now.AddMinutes(-5);
            fixture.CodeExecution.SetupGet(x => x.IsEnabled).Returns(true);
            fixture.Repository.Setup(x => x.GetCodeSessionByTokenAsync(token)).ReturnsAsync(session);
            var sut = fixture.CreateSut();

            var (accepted, message, _) = await sut.RunAsync(new CodeSessionRunRequestVM { Token = token, Language = "python", Code = "print(1)" });

            Assert.False(accepted);
            Assert.Contains("can no longer be changed", message);
            fixture.CodeExecution.Verify(x => x.ExecuteAsync(It.IsAny<CodeExecutionRequest>()), Times.Never);
            fixture.Repository.Verify(x => x.SaveCodeSessionRunAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
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

            await sut.SaveJoinGateOverrideAsync(48210, " rash@example.com ",
                new[] { "checklist_3", "Rubric", "issues", "<script>", "rubric", "", "CHECKLIST_9", "checklist_99" });

            // Only the gate's own keys, de-duplicated, in the gate's order.
            fixture.Repository.Verify(x => x.SaveJoinGateOverrideAsync(48210, "rash@example.com", "rubric,checklist_9,checklist_3", It.IsAny<DateTime>()), Times.Once);
        }

        [Fact]
        public async Task IsAlignmentRequired_ReadsTheInterviewsOwnChannel()
        {
            var fixture = new InterviewAlignmentFixture();
            fixture.Repository.Setup(x => x.IsAlignmentRequiredAsync(48210)).ReturnsAsync(true);
            fixture.Repository.Setup(x => x.IsAlignmentRequiredAsync(48211)).ReturnsAsync(false);
            var sut = fixture.CreateSut();

            Assert.True(await sut.IsAlignmentRequiredAsync(48210));
            Assert.False(await sut.IsAlignmentRequiredAsync(48211));
            Assert.False(await sut.IsAlignmentRequiredAsync(0));
            fixture.Repository.Verify(x => x.IsAlignmentRequiredAsync(0), Times.Never);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task SetAlignmentRequired_WritesOnlyTheSwitch(bool isRequired)
        {
            var fixture = new InterviewAlignmentFixture();
            fixture.Repository.Setup(x => x.SetAlignmentRequiredAsync(10482, isRequired)).ReturnsAsync(true);
            var sut = fixture.CreateSut();

            Assert.True(await sut.SetAlignmentRequiredAsync(10482, isRequired));

            fixture.Repository.Verify(x => x.SetAlignmentRequiredAsync(10482, isRequired), Times.Once);
            // Toggling the switch must not touch the channel's interviewers or anything else on the alignment side.
            fixture.IrfService.VerifyNoOtherCalls();
            fixture.Repository.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task SetAlignmentRequired_WhenTheDatabaseCallFails_ReportsNullNotFalse()
        {
            var fixture = new InterviewAlignmentFixture();
            fixture.Repository.Setup(x => x.SetAlignmentRequiredAsync(10482, false)).ReturnsAsync((bool?)null);
            var sut = fixture.CreateSut();

            // Null lets the endpoint say the database is not ready, rather than "channel not found".
            Assert.Null(await sut.SetAlignmentRequiredAsync(10482, false));
        }

        [Fact]
        public async Task SetAlignmentRequired_WithoutAChannel_DoesNothing()
        {
            var fixture = new InterviewAlignmentFixture();
            var sut = fixture.CreateSut();

            Assert.False(await sut.SetAlignmentRequiredAsync(0, true));
            fixture.Repository.Verify(x => x.SetAlignmentRequiredAsync(It.IsAny<int>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task BuildAlignment_WhenTheChannelSwitchIsOff_ReturnsNullSoTodaysPageRenders()
        {
            var fixture = new InterviewAlignmentFixture();
            fixture.Repository.Setup(x => x.GetAlignmentContextAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(new InterviewAlignmentContext { IsInterviewerAlignmentRequired = false, IRFIntId = 10482 });
            var sut = fixture.CreateSut();

            var model = await sut.BuildAlignmentAsync(48210, "rash@example.com", new Meeting { candidateEmail = "ada.lovelace@example.com" }, 15, 5000);

            Assert.Null(model);
            // Nothing else is queried for a channel that is switched off.
            fixture.CandidateService.Verify(x => x.EnsureCandidatePseudonymsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<string>()), Times.Never);
            fixture.Repository.Verify(x => x.GetInterviewerCodeQuestionsAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task CodeSession_WhenTheChannelIsSwitchedOff_AnswersNothing()
        {
            var fixture = new InterviewAlignmentFixture();
            var token = Guid.NewGuid();
            var session = InterviewAlignmentFixture.LiveSession(token);
            session.IsInterviewerAlignmentRequired = false;
            fixture.CodeExecution.SetupGet(x => x.IsEnabled).Returns(true);
            fixture.Repository.Setup(x => x.GetCodeSessionByTokenAsync(token)).ReturnsAsync(session);
            var sut = fixture.CreateSut();

            var page = await sut.GetCandidatePageAsync(token);
            var (ran, _, _) = await sut.RunAsync(new CodeSessionRunRequestVM { Token = token, Language = "python", Code = "print(1)" });
            var (submitted, _, _) = await sut.SubmitAsync(new CodeSessionSubmitRequestVM { Token = token, Language = "python", Code = "print(1)" });

            Assert.True(page.IsExpired);
            Assert.False(ran);
            Assert.False(submitted);
            fixture.Repository.Verify(x => x.MarkCodeSessionOpenedAsync(It.IsAny<Guid>(), It.IsAny<DateTime>()), Times.Never);
            fixture.CodeExecution.Verify(x => x.ExecuteAsync(It.IsAny<CodeExecutionRequest>()), Times.Never);
            fixture.Repository.Verify(x => x.SubmitCodeSessionAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
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
                IsInterviewerAlignmentRequired = true,
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
                .ReturnsAsync(new InterviewAlignmentContext { IsInterviewerAlignmentRequired = true, IRFIntId = 10482, RoleTitle = " Senior Backend Engineer ", DurationMinutes = 75 });
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
        public async Task BuildAlignment_WhatWentWrongBefore_UsesCodedNamesAndCleansTheReviewText()
        {
            var fixture = new InterviewAlignmentFixture();
            fixture.Repository.Setup(x => x.GetAlignmentContextAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(new InterviewAlignmentContext { IsInterviewerAlignmentRequired = true, IRFIntId = 10482 });
            fixture.Repository.Setup(x => x.GetFlaggedInterviewsAsync(48210, "rash@example.com")).ReturnsAsync(new List<AlignmentFlaggedInterview>
            {
                new() { InterviewId = 44118, InterviewDate = new DateTime(2026, 9, 2), CandidateEmail = " ada.lovelace@example.com ",
                        SignalCount = 2, RiskCategory = "Medium", IsRedo = true, IsLowCandidateRating = true, CandidateRating = 2,
                        AIReasoning = "**Verdict:** candidate ada.lovelace@example.com was not probed\n\non the elimination skill." }
            });
            fixture.CandidateService.Setup(x => x.EnsureCandidatePseudonymsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<string>()))
                .ReturnsAsync(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["ada.lovelace@example.com"] = "AdLo1234" });
            var sut = fixture.CreateSut();

            var model = await sut.BuildAlignmentAsync(48210, "rash@example.com", new Meeting(), 15, 5000);

            Assert.True(model.FlaggedInterviewsLoaded);
            var row = Assert.Single(model.FlaggedInterviews);
            Assert.Equal("AdLo1234", row.CandidateCodedName);
            Assert.Null(row.CandidateEmail);                                      // never reaches the page
            Assert.Equal("Verdict: candidate [email] was not probed on the elimination skill.", row.AIReasoning);
            Assert.Equal(new[] { "The interview was redone", "Low candidate rating (2/5)" }, row.Issues);
        }

        [Fact]
        public async Task BuildAlignment_WhenWhatWentWrongBeforeFails_ThePageStillBuilds()
        {
            var fixture = new InterviewAlignmentFixture();
            fixture.Repository.Setup(x => x.GetAlignmentContextAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(new InterviewAlignmentContext { IsInterviewerAlignmentRequired = true, IRFIntId = 10482 });
            fixture.Repository.Setup(x => x.GetFlaggedInterviewsAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync((IList<AlignmentFlaggedInterview>)null);     // e.g. the procedure is not deployed yet
            var sut = fixture.CreateSut();

            var model = await sut.BuildAlignmentAsync(48210, "rash@example.com", new Meeting(), 15, 5000);

            Assert.NotNull(model);
            Assert.False(model.FlaggedInterviewsLoaded);
            Assert.Empty(model.FlaggedInterviews);
        }

        [Fact]
        public async Task BuildAlignment_WithoutACodedName_LogsWhyByInterviewIdOnly()
        {
            var fixture = new InterviewAlignmentFixture();
            fixture.Repository.Setup(x => x.GetAlignmentContextAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(new InterviewAlignmentContext { IsInterviewerAlignmentRequired = true, IRFIntId = 10482, CandidateEmail = null });
            fixture.Logger.Setup(x => x.LogMessage(It.IsAny<LogLevelInfo>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            var sut = fixture.CreateSut();

            var model = await sut.BuildAlignmentAsync(48210, "rash@example.com", new Meeting(), 15, 5000);

            Assert.Null(model.CandidateCodedName);
            fixture.Logger.Verify(x => x.LogMessage(LogLevelInfo.Warning,
                It.Is<string>(m => m.Contains("48210") && m.Contains("no candidate email")), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task BuildAlignment_WhenTheMeetingHasNoCandidate_UsesTheInterviewsCandidateEmailForTheCodedName()
        {
            var fixture = new InterviewAlignmentFixture();
            fixture.Repository.Setup(x => x.GetAlignmentContextAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(new InterviewAlignmentContext { IsInterviewerAlignmentRequired = true, IRFIntId = 10482, CandidateEmail = " mary.curie@example.com " });
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
