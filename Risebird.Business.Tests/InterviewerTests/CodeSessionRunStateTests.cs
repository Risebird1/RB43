using Moq;
using Risebird.Business;
using Risebird.Business.Interface;
using Risebird.Business.Tests.Infrastructure;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;

namespace Risebird.Business.Tests.InterviewerTests
{
    /// <summary>
    /// Per-question code sessions record three outcomes, not two, and the difference between
    /// them is the whole point of the feature:
    ///
    ///   Executed     ran to completion, whatever the exit code
    ///   Failed       ran, and the code did not compile or threw — evidence about the candidate
    ///   NotExecuted  the runner could not give an answer — says nothing about them at all
    ///
    /// Failed publishes the error verbatim to a client. NotExecuted publishes an explicit line
    /// saying no run happened. Confusing the two puts a platform outage into a client report as
    /// the candidate's code failing, beside an empty output panel — so these tests pin the
    /// mapping rather than leaving it to be re-derived.
    /// </summary>
    public class CodeSessionRunStateTests
    {
        private const string Token = "a-session-token";

        private static (MeetingService Sut, Mock<IMeetingRespository> Repo, Mock<ICodeExecutionService> Runner) Create(
            bool sessionOpen = true)
        {
            var mocker = new AutoMocker();
            var repo = mocker.MockFor<IMeetingRespository>();
            var runner = mocker.MockFor<ICodeExecutionService>();

            runner.SetupGet(x => x.IsEnabled).Returns(true);

            // Running and submitting both check the session is still open before the code is
            // allowed anywhere near the runner.
            repo.Setup(x => x.GetCodeSessionByToken(Token))
                .ReturnsAsync(new CodeSessionCandidateVM
                {
                    IsValid = true,
                    SessionToken = Token,
                    SessionState = (int)(sessionOpen ? CodeSessionState.InProgress : CodeSessionState.Submitted)
                });

            repo.Setup(x => x.SaveCodeSession(It.IsAny<string>(), It.IsAny<CodeSessionVM>(), It.IsAny<bool>()))
                .ReturnsAsync(true);

            return (mocker.CreateService<MeetingService>(), repo, runner);
        }

        private static void RunnerReturns(Mock<ICodeExecutionService> runner, CodeExecutionResult result) =>
            runner.Setup(x => x.ExecuteAsync(It.IsAny<CodeExecutionRequest>())).ReturnsAsync(result);

        /// <summary>The CodeSessionVM handed to the repository for storage.</summary>
        private static CodeSessionVM Stored(Mock<IMeetingRespository> repo, bool submit)
        {
            CodeSessionVM captured = null;
            repo.Verify(x => x.SaveCodeSession(Token, It.Is<CodeSessionVM>(v => Capture(v, ref captured)), submit),
                Times.Once());
            return captured;
        }

        private static bool Capture(CodeSessionVM value, ref CodeSessionVM slot)
        {
            slot = value;
            return true;
        }

        [Fact]
        public async Task A_program_that_ran_to_completion_is_Executed()
        {
            var (sut, repo, runner) = Create();
            RunnerReturns(runner, new CodeExecutionResult
            {
                Success = true,
                Status = "Success",
                Stdout = "42\n",
                ExitCode = 0
            });

            await sut.RunCodeSession(Token, "print(42)", "python", 0);

            var stored = Stored(repo, submit: false);
            Assert.Equal((int)CodeRunState.Executed, stored.RunState);
            Assert.Equal("42\n", stored.Stdout);
        }

        [Fact]
        public async Task Code_that_did_not_compile_is_Failed_and_the_error_is_kept()
        {
            var (sut, repo, runner) = Create();
            RunnerReturns(runner, new CodeExecutionResult
            {
                Success = false,
                Status = "Compilation Error",
                CompileOutput = "main.c:3: expected ';'",
                ExitCode = 1
            });

            await sut.RunCodeSession(Token, "int main(){return 0}", "c", 0);

            var stored = Stored(repo, submit: false);

            // Evidence, not a system fault. It must not be swallowed into NotExecuted, and the
            // compiler's own words are what reaches the report.
            Assert.Equal((int)CodeRunState.Failed, stored.RunState);
            Assert.Equal("main.c:3: expected ';'", stored.Stderr);
        }

        [Fact]
        public async Task A_program_that_threw_is_Failed()
        {
            var (sut, repo, runner) = Create();
            RunnerReturns(runner, new CodeExecutionResult
            {
                Success = false,
                Status = "Runtime Error (NZEC)",
                Stderr = "ZeroDivisionError: division by zero",
                ExitCode = 1
            });

            await sut.RunCodeSession(Token, "print(1/0)", "python", 0);

            Assert.Equal((int)CodeRunState.Failed, Stored(repo, submit: false).RunState);
        }

        /// <summary>
        /// The regression this suite exists for. The execution service reports an unreachable,
        /// refusing, rate-limited or timed-out runner with Success = false and Status = "Error" —
        /// which is indistinguishable from a program that threw unless DidNotRun is honoured.
        /// Getting this wrong tells a client the candidate's code failed, next to a blank panel.
        /// </summary>
        [Fact]
        public async Task An_unavailable_runner_is_NotExecuted_not_Failed()
        {
            var (sut, repo, runner) = Create();
            RunnerReturns(runner, new CodeExecutionResult
            {
                Success = false,
                DidNotRun = true,
                Status = "Error",
                Message = "The code runner is temporarily unavailable."
            });

            await sut.RunCodeSession(Token, "print(42)", "python", 0);

            var stored = Stored(repo, submit: false);
            Assert.Equal((int)CodeRunState.NotExecuted, stored.RunState);
            Assert.NotEqual((int)CodeRunState.Failed, stored.RunState);
        }

        [Fact]
        public async Task A_runner_that_is_switched_off_is_NotExecuted_and_is_never_called()
        {
            var (sut, repo, runner) = Create();
            runner.SetupGet(x => x.IsEnabled).Returns(false);

            await sut.RunCodeSession(Token, "print(42)", "python", 0);

            // Capture must not depend on execution: the attempt is still recorded in full.
            Assert.Equal((int)CodeRunState.NotExecuted, Stored(repo, submit: false).RunState);
            runner.Verify(x => x.ExecuteAsync(It.IsAny<CodeExecutionRequest>()), Times.Never());
        }

        [Fact]
        public async Task A_runner_that_throws_is_NotExecuted()
        {
            var (sut, repo, runner) = Create();
            runner.Setup(x => x.ExecuteAsync(It.IsAny<CodeExecutionRequest>()))
                .ThrowsAsync(new HttpRequestException("connection refused"));

            await sut.RunCodeSession(Token, "print(42)", "python", 0);

            Assert.Equal((int)CodeRunState.NotExecuted, Stored(repo, submit: false).RunState);
        }

        /// <summary>
        /// Stored output must match submitted code. A candidate can run, keep typing, then
        /// submit — so submit re-runs once and stores THAT result, rather than leaving the
        /// earlier run's output attached to code it never produced.
        /// </summary>
        [Fact]
        public async Task Submitting_re_runs_the_code_and_stores_the_fresh_result()
        {
            var (sut, repo, runner) = Create();
            RunnerReturns(runner, new CodeExecutionResult
            {
                Success = true,
                Status = "Success",
                Stdout = "final answer",
                LanguageVersionUsed = "3.10.0",
                DurationMs = 120
            });

            Assert.True(await sut.SubmitCodeSession(Token, "print('final answer')", "python", 2));

            runner.Verify(x => x.ExecuteAsync(It.IsAny<CodeExecutionRequest>()), Times.Once());

            var stored = Stored(repo, submit: true);
            Assert.Equal("final answer", stored.Stdout);
            Assert.Equal((int)CodeRunState.Executed, stored.RunState);

            // The version that actually ran and how long it took are only knowable now; a report
            // rendered later states them without re-running anything.
            Assert.Equal("3.10.0", stored.LanguageVersionUsed);
            Assert.Equal(120, stored.RunDurationMs);
            Assert.Equal(2, stored.PasteEvents);
        }

        /// <summary>
        /// The link expires when the interview ends, and so does the ability to run. Without
        /// this an expired token is an anonymous, unmetered code-execution endpoint.
        /// </summary>
        [Fact]
        public async Task A_frozen_session_neither_runs_nor_stores_anything()
        {
            var (sut, repo, runner) = Create(sessionOpen: false);
            RunnerReturns(runner, new CodeExecutionResult { Success = true, Stdout = "should never happen" });

            var result = await sut.RunCodeSession(Token, "print(42)", "python", 0);

            Assert.True(result.DidNotRun);
            runner.Verify(x => x.ExecuteAsync(It.IsAny<CodeExecutionRequest>()), Times.Never());
            repo.Verify(x => x.SaveCodeSession(It.IsAny<string>(), It.IsAny<CodeSessionVM>(), It.IsAny<bool>()),
                Times.Never());
        }

        [Fact]
        public async Task Submitting_a_frozen_session_is_refused()
        {
            var (sut, repo, runner) = Create(sessionOpen: false);

            Assert.False(await sut.SubmitCodeSession(Token, "print(42)", "python", 0));
            runner.Verify(x => x.ExecuteAsync(It.IsAny<CodeExecutionRequest>()), Times.Never());
        }

        /// <summary>
        /// Only submitted attempts reach a client. An attempt that was generated and never
        /// opened, or opened and never submitted, is kept but never published — publishing it
        /// would damage the candidate for something never offered as an answer.
        /// </summary>
        [Fact]
        public async Task Only_submitted_attempts_are_published_to_a_report()
        {
            var mocker = new AutoMocker();
            var repo = mocker.MockFor<IMeetingRespository>();
            repo.Setup(x => x.GetCodeSessionsForInterview(7)).ReturnsAsync(new List<CodeSessionVM>
            {
                new() { id = 1, SessionState = (int)CodeSessionState.Generated,  SkillName = "C#", AttemptNumber = 1 },
                new() { id = 2, SessionState = (int)CodeSessionState.Opened,     SkillName = "C#", AttemptNumber = 2 },
                new() { id = 3, SessionState = (int)CodeSessionState.InProgress, SkillName = "C#", AttemptNumber = 3 },
                new() { id = 4, SessionState = (int)CodeSessionState.Submitted,  SkillName = "C#", AttemptNumber = 4 },
                new() { id = 5, SessionState = (int)CodeSessionState.Expired,    SkillName = "C#", AttemptNumber = 5 },
                new() { id = 6, SessionState = (int)CodeSessionState.Submitted,  SkillName = "C#", AttemptNumber = 6 }
            });

            var published = await mocker.CreateService<MeetingService>().GetSubmittedCodeSessionsForReport(7);

            Assert.Equal(new[] { 4, 6 }, published.Select(s => s.id));

            // A progression should read as a progression: ordered, not an unexplained duplicate.
            Assert.Equal(new[] { 4, 6 }, published.Select(s => s.AttemptNumber));
        }
    }
}
