using Moq;
using Risebird.Business;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;

namespace Risebird.Business.Tests.ThinServiceTests
{
    /// <summary>
    /// The thin business services are pass-throughs with a try/catch that logs and returns a
    /// documented fallback. These suites pin both halves of that contract for each service:
    /// the happy path delegates, and the failure path swallows, logs, and returns the fallback.
    /// </summary>
    internal static class LoggerMock
    {
        public static Mock<IApplicationErrorLogger> Create()
        {
            var logger = new Mock<IApplicationErrorLogger>(MockBehavior.Loose);
            logger.Setup(x => x.LogMessage(It.IsAny<LogLevelInfo>(), It.IsAny<Exception>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            logger.Setup(x => x.LogMessage(It.IsAny<LogLevelInfo>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            return logger;
        }

        public static void VerifyExceptionLogged(this Mock<IApplicationErrorLogger> logger, Times times) =>
            logger.Verify(
                x => x.LogMessage(LogLevelInfo.Exception, It.IsAny<Exception>(), It.IsAny<string>()), times);
    }

    public class SettingsServiceTests
    {
        private static (SettingsService Sut, Mock<ISettingsRepository> Repo, Mock<IApplicationErrorLogger> Log) Create()
        {
            var repo = new Mock<ISettingsRepository>(MockBehavior.Loose);
            var log = LoggerMock.Create();
            return (new SettingsService(repo.Object, log.Object), repo, log);
        }

        [Fact]
        public async Task Get_all_settings_returns_the_repository_row()
        {
            var (sut, repo, log) = Create();
            var settings = new Settings { InterviewerUnassignedMinutes = 30 };
            repo.Setup(x => x.GetAllSettings()).ReturnsAsync(settings);

            Assert.Same(settings, await sut.GetAllSettings());
            log.VerifyExceptionLogged(Times.Never());
        }

        [Fact]
        public async Task Get_all_settings_returns_null_and_logs_on_failure()
        {
            var (sut, repo, log) = Create();
            repo.Setup(x => x.GetAllSettings()).ThrowsAsync(new InvalidOperationException("sql down"));

            Assert.Null(await sut.GetAllSettings());
            log.VerifyExceptionLogged(Times.Once());
        }

        [Fact]
        public async Task Get_all_settings_passes_a_null_row_through_unchanged()
        {
            var (sut, repo, _) = Create();
            repo.Setup(x => x.GetAllSettings()).ReturnsAsync((Settings)null);

            Assert.Null(await sut.GetAllSettings());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Update_settings_returns_the_repository_result(bool repositoryResult)
        {
            var (sut, repo, _) = Create();
            var settings = new Settings();
            repo.Setup(x => x.UpdateSettings(settings)).ReturnsAsync(repositoryResult);

            Assert.Equal(repositoryResult, await sut.UpdateSettings(settings));
        }

        [Fact]
        public async Task Update_settings_returns_false_and_logs_on_failure()
        {
            var (sut, repo, log) = Create();
            repo.Setup(x => x.UpdateSettings(It.IsAny<Settings>()))
                .ThrowsAsync(new InvalidOperationException("sql down"));

            Assert.False(await sut.UpdateSettings(new Settings()));
            log.VerifyExceptionLogged(Times.Once());
        }

        [Fact]
        public async Task Update_settings_forwards_a_null_model_without_guarding_it()
        {
            var (sut, repo, _) = Create();
            repo.Setup(x => x.UpdateSettings(null)).ReturnsAsync(true);

            Assert.True(await sut.UpdateSettings(null));
            repo.Verify(x => x.UpdateSettings(null), Times.Once);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(15)]
        [InlineData(int.MaxValue)]
        public async Task Unassigned_minutes_returns_the_repository_value(int minutes)
        {
            var (sut, repo, _) = Create();
            repo.Setup(x => x.GetInterviewUnassignedMinutes()).ReturnsAsync(minutes);

            Assert.Equal(minutes, await sut.GetInterviewUnassignedMinutes());
        }

        [Fact]
        public async Task Unassigned_minutes_returns_zero_and_logs_on_failure()
        {
            var (sut, repo, log) = Create();
            repo.Setup(x => x.GetInterviewUnassignedMinutes())
                .ThrowsAsync(new InvalidOperationException("sql down"));

            Assert.Equal(0, await sut.GetInterviewUnassignedMinutes());
            log.VerifyExceptionLogged(Times.Once());
        }
    }

    public class RiskCategoryServiceTests
    {
        private static (RiskCategoryService Sut, Mock<IRiskCategoryRepository> Repo, Mock<IApplicationErrorLogger> Log) Create()
        {
            var repo = new Mock<IRiskCategoryRepository>(MockBehavior.Loose);
            var log = LoggerMock.Create();
            return (new RiskCategoryService(repo.Object, log.Object), repo, log);
        }

        [Fact]
        public async Task Get_risk_categories_materialises_the_repository_sequence()
        {
            var (sut, repo, _) = Create();
            repo.Setup(x => x.GetRiskCategories(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new[] { new RiskCategory { Id = 1 }, new RiskCategory { Id = 2 } });

            var result = await sut.GetRiskCategories(new DateTime(2026, 01, 01), new DateTime(2026, 12, 31));

            Assert.Equal(2, result.Count);
            Assert.IsType<List<RiskCategory>>(result);
        }

        [Fact]
        public async Task Get_risk_categories_forwards_the_date_range_verbatim()
        {
            var (sut, repo, _) = Create();
            var start = new DateTime(2026, 04, 01);
            var end = new DateTime(2026, 06, 30);
            repo.Setup(x => x.GetRiskCategories(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(Array.Empty<RiskCategory>());

            await sut.GetRiskCategories(start, end);

            repo.Verify(x => x.GetRiskCategories(start, end), Times.Once);
        }

        [Fact]
        public async Task Get_risk_categories_returns_null_when_the_repository_returns_null()
        {
            var (sut, repo, _) = Create();
            repo.Setup(x => x.GetRiskCategories(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync((IEnumerable<RiskCategory>)null);

            Assert.Null(await sut.GetRiskCategories(DateTime.Today, DateTime.Today));
        }

        [Fact]
        public async Task Get_risk_categories_returns_null_and_logs_on_failure()
        {
            var (sut, repo, log) = Create();
            repo.Setup(x => x.GetRiskCategories(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ThrowsAsync(new InvalidOperationException("sql down"));

            Assert.Null(await sut.GetRiskCategories(DateTime.Today, DateTime.Today));
            log.VerifyExceptionLogged(Times.Once());
        }

        [Fact]
        public async Task Get_risk_category_by_interview_id_passes_through()
        {
            var (sut, repo, _) = Create();
            var row = new RiskCategory { InterviewId = 7 };
            repo.Setup(x => x.GetRiskCategoryByInterviewId(7)).ReturnsAsync(row);

            Assert.Same(row, await sut.GetRiskCategoryByInterviewId(7));
        }

        [Fact]
        public async Task Get_risk_category_by_interview_id_returns_null_and_logs_on_failure()
        {
            var (sut, repo, log) = Create();
            repo.Setup(x => x.GetRiskCategoryByInterviewId(It.IsAny<int>()))
                .ThrowsAsync(new InvalidOperationException("sql down"));

            Assert.Null(await sut.GetRiskCategoryByInterviewId(7));
            log.VerifyExceptionLogged(Times.Once());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Add_update_risk_category_returns_the_repository_result(bool repositoryResult)
        {
            var (sut, repo, _) = Create();
            repo.Setup(x => x.AddUpdateRiskCategory(7)).ReturnsAsync(repositoryResult);

            Assert.Equal(repositoryResult, await sut.AddUpdateRiskCategory(7));
        }

        [Fact]
        public async Task Add_update_risk_category_returns_false_and_logs_on_failure()
        {
            var (sut, repo, log) = Create();
            repo.Setup(x => x.AddUpdateRiskCategory(It.IsAny<int>()))
                .ThrowsAsync(new InvalidOperationException("sql down"));

            Assert.False(await sut.AddUpdateRiskCategory(7));
            log.VerifyExceptionLogged(Times.Once());
        }

        [Fact]
        public async Task Dry_run_passes_through_without_writing()
        {
            var (sut, repo, _) = Create();
            var row = new RiskCategory { InterviewId = 7, Severity = "P2" };
            repo.Setup(x => x.EvaluateRiskCategoryDryRun(7)).ReturnsAsync(row);

            Assert.Same(row, await sut.EvaluateRiskCategoryDryRun(7));
            repo.Verify(x => x.AddUpdateRiskCategory(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task Dry_run_returns_null_and_logs_on_failure()
        {
            var (sut, repo, log) = Create();
            repo.Setup(x => x.EvaluateRiskCategoryDryRun(It.IsAny<int>()))
                .ThrowsAsync(new InvalidOperationException("sql down"));

            Assert.Null(await sut.EvaluateRiskCategoryDryRun(7));
            log.VerifyExceptionLogged(Times.Once());
        }
    }

    public class ActivityTrackerServiceTests
    {
        private static (ActivityTrackerService Sut, Mock<IActivityTrackerRepository> Repo, Mock<IApplicationErrorLogger> Log) Create()
        {
            var repo = new Mock<IActivityTrackerRepository>(MockBehavior.Loose);
            var log = LoggerMock.Create();
            return (new ActivityTrackerService(repo.Object, log.Object), repo, log);
        }

        [Fact]
        public async Task Get_all_activities_forwards_the_date_range_and_returns_the_rows()
        {
            var (sut, repo, _) = Create();
            var start = new DateTime(2026, 01, 01);
            var end = new DateTime(2026, 01, 31);
            var rows = new[] { new ActivityTrackerVM { id = 1 } };
            repo.Setup(x => x.GetAllActivities(start, end)).ReturnsAsync(rows);

            Assert.Same(rows, await sut.GetAllActivities(start, end));
        }

        [Fact]
        public async Task Get_all_activities_returns_null_and_logs_on_failure()
        {
            var (sut, repo, log) = Create();
            repo.Setup(x => x.GetAllActivities(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ThrowsAsync(new InvalidOperationException("sql down"));

            Assert.Null(await sut.GetAllActivities(DateTime.Today, DateTime.Today));
            log.VerifyExceptionLogged(Times.Once());
        }

        [Fact]
        public async Task Get_all_activities_accepts_an_inverted_range_without_guarding_it()
        {
            var (sut, repo, _) = Create();
            repo.Setup(x => x.GetAllActivities(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(Array.Empty<ActivityTrackerVM>());

            await sut.GetAllActivities(new DateTime(2026, 12, 31), new DateTime(2026, 01, 01));

            repo.Verify(
                x => x.GetAllActivities(new DateTime(2026, 12, 31), new DateTime(2026, 01, 01)), Times.Once);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Save_capture_click_details_returns_the_repository_result(bool repositoryResult)
        {
            var (sut, repo, _) = Create();
            var model = new CapturePageDM { PerformedBy = "ops@risebird.io" };
            repo.Setup(x => x.SaveCaptureClickDetails(model)).ReturnsAsync(repositoryResult);

            Assert.Equal(repositoryResult, await sut.SaveCaptureClickDetails(model));
        }

        [Fact]
        public async Task Save_capture_click_details_returns_false_and_logs_on_failure()
        {
            var (sut, repo, log) = Create();
            repo.Setup(x => x.SaveCaptureClickDetails(It.IsAny<CapturePageDM>()))
                .ThrowsAsync(new InvalidOperationException("sql down"));

            Assert.False(await sut.SaveCaptureClickDetails(new CapturePageDM()));
            log.VerifyExceptionLogged(Times.Once());
        }

        [Fact]
        public async Task Save_interviewer_page_click_returns_false_and_logs_on_failure()
        {
            var (sut, repo, log) = Create();
            repo.Setup(x => x.SaveCaptureInterviewerRelatedPageClick(It.IsAny<CapturePageDM>()))
                .ThrowsAsync(new InvalidOperationException("sql down"));

            Assert.False(await sut.SaveCaptureInterviewerRelatedPageClick(new CapturePageDM()));
            log.VerifyExceptionLogged(Times.Once());
        }

        [Fact]
        public async Task Debug_logger_forwards_every_argument()
        {
            var (sut, repo, _) = Create();
            repo.Setup(x => x.DebugLogger("msg", "type", "ops@risebird.io")).ReturnsAsync(true);

            Assert.True(await sut.DebugLogger("msg", "type", "ops@risebird.io"));
        }

        [Fact]
        public async Task Debug_logger_returns_false_and_logs_on_failure()
        {
            var (sut, repo, log) = Create();
            repo.Setup(x => x.DebugLogger(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("sql down"));

            Assert.False(await sut.DebugLogger("msg", "type", "ops@risebird.io"));
            log.VerifyExceptionLogged(Times.Once());
        }

        [Fact]
        public async Task Debug_logger_tolerates_null_arguments()
        {
            var (sut, repo, _) = Create();
            repo.Setup(x => x.DebugLogger(null, null, null)).ReturnsAsync(true);

            Assert.True(await sut.DebugLogger(null, null, null));
        }
    }

    public class EmailLookupServiceTests
    {
        private static (EmailLookupService Sut, Mock<IEmailLookupRepository> Repo, Mock<IApplicationErrorLogger> Log) Create()
        {
            var repo = new Mock<IEmailLookupRepository>(MockBehavior.Loose);
            var log = LoggerMock.Create();
            return (new EmailLookupService(repo.Object, log.Object), repo, log);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t")]
        public async Task A_blank_email_short_circuits_to_not_found(string email)
        {
            var (sut, repo, log) = Create();

            var result = await sut.GetEmailLookupData(email);

            Assert.NotNull(result);
            Assert.False(result.IsFound);
            repo.Verify(x => x.GetEmailLookupData(It.IsAny<string>()), Times.Never);
            log.VerifyExceptionLogged(Times.Never());
        }

        [Theory]
        [InlineData("ADA@RISEBIRD.IO", "ada@risebird.io")]
        [InlineData("  Ada@Risebird.io  ", "ada@risebird.io")]
        [InlineData("ada@risebird.io", "ada@risebird.io")]
        public async Task The_email_is_trimmed_and_lower_cased_before_the_lookup(string input, string expected)
        {
            var (sut, repo, _) = Create();
            repo.Setup(x => x.GetEmailLookupData(It.IsAny<string>()))
                .ReturnsAsync(new EmailLookupResultVM { IsFound = true });

            await sut.GetEmailLookupData(input);

            repo.Verify(x => x.GetEmailLookupData(expected), Times.Once);
        }

        [Fact]
        public async Task A_found_result_is_passed_through()
        {
            var (sut, repo, _) = Create();
            var found = new EmailLookupResultVM { IsFound = true, UserId = 9 };
            repo.Setup(x => x.GetEmailLookupData("ada@risebird.io")).ReturnsAsync(found);

            Assert.Same(found, await sut.GetEmailLookupData("ada@risebird.io"));
        }

        [Fact]
        public async Task A_failure_returns_a_not_found_result_that_echoes_the_search_email()
        {
            var (sut, repo, log) = Create();
            repo.Setup(x => x.GetEmailLookupData(It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("sql down"));

            var result = await sut.GetEmailLookupData("  Ada@Risebird.io  ");

            Assert.False(result.IsFound);
            Assert.Equal("  Ada@Risebird.io  ", result.SearchEmail);
            log.VerifyExceptionLogged(Times.Once());
        }

        [Fact]
        public async Task A_null_repository_result_is_passed_through()
        {
            var (sut, repo, _) = Create();
            repo.Setup(x => x.GetEmailLookupData(It.IsAny<string>()))
                .ReturnsAsync((EmailLookupResultVM)null);

            // Pinned: a null repository result IS passed through, so callers must null-check.
            Assert.Null(await sut.GetEmailLookupData("ada@risebird.io"));
        }
    }

    public class GeneralSkillMatrixServiceTests
    {
        private static (GeneralSkillMatrixService Sut, Mock<IGeneralSkillMatrixRepository> Repo, Mock<IApplicationErrorLogger> Log) Create()
        {
            var repo = new Mock<IGeneralSkillMatrixRepository>(MockBehavior.Loose);
            var log = LoggerMock.Create();
            return (new GeneralSkillMatrixService(repo.Object, log.Object), repo, log);
        }

        [Fact]
        public void Get_materialises_the_repository_sequence()
        {
            var (sut, repo, _) = Create();
            repo.Setup(x => x.GetGeneralSkillMatrix())
                .Returns(new[] { new GeneralSkillMatrix { id = 1 }, new GeneralSkillMatrix { id = 2 } });

            var result = sut.GetGeneralSkillMatrix();

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void Get_returns_null_and_logs_when_the_repository_returns_null()
        {
            var (sut, repo, log) = Create();
            repo.Setup(x => x.GetGeneralSkillMatrix()).Returns((IEnumerable<GeneralSkillMatrix>)null);

            Assert.Null(sut.GetGeneralSkillMatrix());
            log.VerifyExceptionLogged(Times.Once());
        }

        [Fact]
        public void Get_returns_null_and_logs_on_failure()
        {
            var (sut, repo, log) = Create();
            repo.Setup(x => x.GetGeneralSkillMatrix()).Throws(new InvalidOperationException("sql down"));

            Assert.Null(sut.GetGeneralSkillMatrix());
            log.VerifyExceptionLogged(Times.Once());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Add_returns_the_repository_result(bool repositoryResult)
        {
            var (sut, repo, _) = Create();
            var model = new GeneralSkillMatrix { Name = "Communication" };
            repo.Setup(x => x.AddGeneralSkillMatrix(model)).ReturnsAsync(repositoryResult);

            Assert.Equal(repositoryResult, sut.AddGeneralSkillMatrix(model));
        }

        [Fact]
        public void Add_returns_false_and_logs_on_failure()
        {
            var (sut, repo, log) = Create();
            repo.Setup(x => x.AddGeneralSkillMatrix(It.IsAny<GeneralSkillMatrix>()))
                .ThrowsAsync(new InvalidOperationException("sql down"));

            Assert.False(sut.AddGeneralSkillMatrix(new GeneralSkillMatrix()));
            log.VerifyExceptionLogged(Times.Once());
        }

        [Fact]
        public void Get_by_id_returns_the_row()
        {
            var (sut, repo, _) = Create();
            var row = new GeneralSkillMatrix { id = 5 };
            repo.Setup(x => x.GetGeneralSkillMatrixByiId(5)).ReturnsAsync(row);

            Assert.Same(row, sut.GetGeneralSkillMatrixByiId(5));
        }

        [Fact]
        public void Get_by_id_calls_the_repository_twice_because_of_the_null_check()
        {
            // Pinned as a known inefficiency: the null-guard evaluates the Task, then the
            // result is fetched again. Two round trips per lookup.
            var (sut, repo, _) = Create();
            repo.Setup(x => x.GetGeneralSkillMatrixByiId(5)).ReturnsAsync(new GeneralSkillMatrix { id = 5 });

            sut.GetGeneralSkillMatrixByiId(5);

            repo.Verify(x => x.GetGeneralSkillMatrixByiId(5), Times.Exactly(2));
        }

        [Fact]
        public void Get_by_id_returns_null_and_logs_on_failure()
        {
            var (sut, repo, log) = Create();
            repo.Setup(x => x.GetGeneralSkillMatrixByiId(It.IsAny<int>()))
                .ThrowsAsync(new InvalidOperationException("sql down"));

            Assert.Null(sut.GetGeneralSkillMatrixByiId(5));
            log.VerifyExceptionLogged(Times.Once());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Update_returns_the_repository_result(bool repositoryResult)
        {
            var (sut, repo, _) = Create();
            var model = new GeneralSkillMatrix { id = 5 };
            repo.Setup(x => x.UpdateGeneralSkillMatrix(model)).ReturnsAsync(repositoryResult);

            Assert.Equal(repositoryResult, sut.UpdateGeneralSkillMatrix(model));
        }

        [Fact]
        public void Update_returns_false_and_logs_on_failure()
        {
            var (sut, repo, log) = Create();
            repo.Setup(x => x.UpdateGeneralSkillMatrix(It.IsAny<GeneralSkillMatrix>()))
                .ThrowsAsync(new InvalidOperationException("sql down"));

            Assert.False(sut.UpdateGeneralSkillMatrix(new GeneralSkillMatrix()));
            log.VerifyExceptionLogged(Times.Once());
        }

        [Fact]
        public void Delete_reports_success_without_awaiting_the_repository_result()
        {
            // Pinned: the delete is fire-and-forget, so a rejected delete still reports true.
            var (sut, repo, _) = Create();
            repo.Setup(x => x.DeleteGeneralSkillMatrix(5)).ReturnsAsync(false);

            Assert.True(sut.DeleteGeneralSkillMatrix(5));
            repo.Verify(x => x.DeleteGeneralSkillMatrix(5), Times.Once);
        }

        [Fact]
        public void Delete_returns_false_and_logs_when_the_call_throws_synchronously()
        {
            var (sut, repo, log) = Create();
            repo.Setup(x => x.DeleteGeneralSkillMatrix(It.IsAny<int>()))
                .Throws(new InvalidOperationException("sql down"));

            Assert.False(sut.DeleteGeneralSkillMatrix(5));
            log.VerifyExceptionLogged(Times.Once());
        }
    }

    public class JoinabilityMatrixServiceTests
    {
        private static (JoinabilityMatrixService Sut, Mock<IJoinabilityMatrixRepository> Repo, Mock<IApplicationErrorLogger> Log) Create()
        {
            var repo = new Mock<IJoinabilityMatrixRepository>(MockBehavior.Loose);
            var log = LoggerMock.Create();
            return (new JoinabilityMatrixService(repo.Object, log.Object), repo, log);
        }

        [Fact]
        public void Get_materialises_the_repository_sequence()
        {
            var (sut, repo, _) = Create();
            repo.Setup(x => x.GetJoinabilityMatrix()).Returns(new[] { new JoinabilityMatrix { id = 1 } });

            Assert.Single(sut.GetJoinabilityMatrix());
        }

        [Fact]
        public void Get_returns_null_and_logs_on_failure()
        {
            var (sut, repo, log) = Create();
            repo.Setup(x => x.GetJoinabilityMatrix()).Throws(new InvalidOperationException("sql down"));

            Assert.Null(sut.GetJoinabilityMatrix());
            log.VerifyExceptionLogged(Times.Once());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Add_returns_the_repository_result(bool repositoryResult)
        {
            var (sut, repo, _) = Create();
            var model = new JoinabilityMatrix();
            repo.Setup(x => x.AddJoinabilityMatrix(model)).ReturnsAsync(repositoryResult);

            Assert.Equal(repositoryResult, sut.AddJoinabilityMatrix(model));
        }

        [Fact]
        public void Add_returns_false_and_logs_on_failure()
        {
            var (sut, repo, log) = Create();
            repo.Setup(x => x.AddJoinabilityMatrix(It.IsAny<JoinabilityMatrix>()))
                .ThrowsAsync(new InvalidOperationException("sql down"));

            Assert.False(sut.AddJoinabilityMatrix(new JoinabilityMatrix()));
            log.VerifyExceptionLogged(Times.Once());
        }

        [Fact]
        public void Get_by_id_returns_the_row()
        {
            var (sut, repo, _) = Create();
            var row = new JoinabilityMatrix { id = 5 };
            repo.Setup(x => x.GetJoinabilityMatrixById(5)).ReturnsAsync(row);

            Assert.Same(row, sut.GetJoinabilityMatrixById(5));
        }

        [Fact]
        public void Update_returns_false_and_logs_on_failure()
        {
            var (sut, repo, log) = Create();
            repo.Setup(x => x.UpdateJoinabilityMatrix(It.IsAny<JoinabilityMatrix>()))
                .ThrowsAsync(new InvalidOperationException("sql down"));

            Assert.False(sut.UpdateJoinabilityMatrix(new JoinabilityMatrix()));
            log.VerifyExceptionLogged(Times.Once());
        }

        [Fact]
        public void Delete_reports_success_without_awaiting_the_repository_result()
        {
            var (sut, repo, _) = Create();
            repo.Setup(x => x.DeleteJoinabilityMatrix(5)).ReturnsAsync(false);

            Assert.True(sut.DeleteJoinabilityMatrix(5));
            repo.Verify(x => x.DeleteJoinabilityMatrix(5), Times.Once);
        }

        [Fact]
        public void Delete_returns_false_and_logs_when_the_call_throws_synchronously()
        {
            var (sut, repo, log) = Create();
            repo.Setup(x => x.DeleteJoinabilityMatrix(It.IsAny<int>()))
                .Throws(new InvalidOperationException("sql down"));

            Assert.False(sut.DeleteJoinabilityMatrix(5));
            log.VerifyExceptionLogged(Times.Once());
        }
    }

    public class SelectabilityMatrixServiceTests
    {
        private static (SelectabilityMatrixService Sut, Mock<ISelectabilityMatrixRepository> Repo, Mock<IApplicationErrorLogger> Log) Create()
        {
            var repo = new Mock<ISelectabilityMatrixRepository>(MockBehavior.Loose);
            var log = LoggerMock.Create();
            return (new SelectabilityMatrixService(repo.Object, log.Object), repo, log);
        }

        [Fact]
        public void Get_materialises_the_repository_sequence()
        {
            var (sut, repo, _) = Create();
            repo.Setup(x => x.GetSelectabilityMatrix()).Returns(new[] { new SelectabilityMatrix { id = 1 } });

            Assert.Single(sut.GetSelectabilityMatrix());
        }

        [Fact]
        public void Get_returns_null_and_logs_on_failure()
        {
            var (sut, repo, log) = Create();
            repo.Setup(x => x.GetSelectabilityMatrix()).Throws(new InvalidOperationException("sql down"));

            Assert.Null(sut.GetSelectabilityMatrix());
            log.VerifyExceptionLogged(Times.Once());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Add_returns_the_repository_result(bool repositoryResult)
        {
            var (sut, repo, _) = Create();
            var model = new SelectabilityMatrix();
            repo.Setup(x => x.AddSelectabilityMatrix(model)).ReturnsAsync(repositoryResult);

            Assert.Equal(repositoryResult, sut.AddSelectabilityMatrix(model));
        }

        [Fact]
        public void Get_by_id_returns_the_row()
        {
            var (sut, repo, _) = Create();
            var row = new SelectabilityMatrix { id = 5 };
            repo.Setup(x => x.GetSelectabilityMatrixById(5)).ReturnsAsync(row);

            Assert.Same(row, sut.GetSelectabilityMatrixById(5));
        }

        [Fact]
        public void Update_returns_false_and_logs_on_failure()
        {
            var (sut, repo, log) = Create();
            repo.Setup(x => x.UpdateSelectabilityMatrix(It.IsAny<SelectabilityMatrix>()))
                .ThrowsAsync(new InvalidOperationException("sql down"));

            Assert.False(sut.UpdateSelectabilityMatrix(new SelectabilityMatrix()));
            log.VerifyExceptionLogged(Times.Once());
        }

        [Fact]
        public void Delete_reports_success_without_awaiting_the_repository_result()
        {
            var (sut, repo, _) = Create();
            repo.Setup(x => x.DeleteSelectabilityMatrix(5)).ReturnsAsync(false);

            Assert.True(sut.DeleteSelectabilityMatrix(5));
            repo.Verify(x => x.DeleteSelectabilityMatrix(5), Times.Once);
        }

        [Fact]
        public void Delete_returns_false_and_logs_when_the_call_throws_synchronously()
        {
            var (sut, repo, log) = Create();
            repo.Setup(x => x.DeleteSelectabilityMatrix(It.IsAny<int>()))
                .Throws(new InvalidOperationException("sql down"));

            Assert.False(sut.DeleteSelectabilityMatrix(5));
            log.VerifyExceptionLogged(Times.Once());
        }
    }
}
