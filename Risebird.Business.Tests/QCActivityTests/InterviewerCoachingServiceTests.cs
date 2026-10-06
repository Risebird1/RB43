using Moq;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;

namespace Risebird.Business.Tests.QCActivityTests
{
    public class InterviewerCoachingServiceTests
    {
        private static readonly DateTime NowUtc = new DateTime(2026, 10, 1, 6, 0, 0, DateTimeKind.Utc);
        private const string Interviewer = "ayesha@example.com";
        private const string Coach = "coach@risebird.io";

        private readonly Mock<IInterviewerCoachingRepository> _repository = new();
        private readonly Mock<INotificationHelper> _notification = new();
        private readonly Mock<IApplicationErrorLogger> _logger = new();

        private InterviewerCoachingService CreateService() =>
            new InterviewerCoachingService(_repository.Object, _notification.Object, _logger.Object, () => NowUtc);

        private void GivenExposure(DateTime? lastEmailUtc = null)
        {
            _repository.Setup(r => r.GetExposureAsync(Interviewer, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new InterviewerExposureVM
                {
                    Profile = new ExposureProfileVM { InterviewerEmail = Interviewer, InterviewerName = "Ayesha Khan", IsActive = true, LastSummaryEmailAtUtc = lastEmailUtc },
                    Risks = new[] { new ExposureRiskRowVM { InterviewId = 1, IrfName = "IRF1", SeverityBreakdown = "InterviewerLateJoin:P2" } }
                });
        }

        private static CoachingEmailRequestVM EmailRequest() => new CoachingEmailRequestVM
        {
            InterviewerEmail = Interviewer,
            Themes = new List<string> { InterviewerCoachingBriefBuilder.ThemeJoinOnTime },
            Note = "Thanks for your time."
        };

        [Fact]
        public async Task Unknown_interviewer_returns_an_error_brief()
        {
            _repository.Setup(r => r.GetExposureAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new InterviewerExposureVM());

            var brief = await CreateService().GetBriefAsync("nobody@example.com", null, null);

            Assert.NotNull(brief.Error);
            Assert.Equal("2026-09-25", brief.FromIst);
        }

        [Fact]
        public async Task Send_reserves_the_slot_then_sends_with_reply_to_the_coach()
        {
            GivenExposure();
            _repository.Setup(r => r.SaveSessionAsync(Interviewer, Coach, "Email", "EmailOnly", "JoinOnTime", It.IsAny<string>(),
                    It.IsAny<string>(), 1, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), false))
                .ReturnsAsync(new CoachingSaveResultVM { Result = 42 });
            _notification.Setup(n => n.SendInterviewerCoachingEmail(Interviewer, "Ayesha Khan", Coach, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("Mail sent successfully.");

            var result = await CreateService().SendEmailAsync(EmailRequest(), Coach);

            Assert.True(result.Ok);
            Assert.Equal(42, result.SessionId);
            _repository.Verify(r => r.CancelEmailSessionAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task A_failed_send_releases_the_cooldown_slot()
        {
            GivenExposure();
            _repository.Setup(r => r.SaveSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<bool>()))
                .ReturnsAsync(new CoachingSaveResultVM { Result = 42 });
            _notification.Setup(n => n.SendInterviewerCoachingEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("Failed to send mail.");

            var result = await CreateService().SendEmailAsync(EmailRequest(), Coach);

            Assert.False(result.Ok);
            _repository.Verify(r => r.CancelEmailSessionAsync(42), Times.Once);
        }

        [Fact]
        public async Task An_exception_while_sending_releases_the_cooldown_slot()
        {
            GivenExposure();
            _repository.Setup(r => r.SaveSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<bool>()))
                .ReturnsAsync(new CoachingSaveResultVM { Result = 42 });
            _notification.Setup(n => n.SendInterviewerCoachingEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new TimeoutException("SendGrid timed out"));

            var result = await CreateService().SendEmailAsync(EmailRequest(), Coach);

            Assert.False(result.Ok);
            _repository.Verify(r => r.CancelEmailSessionAsync(42), Times.Once);
        }

        [Fact]
        public async Task Inactive_interviewer_preview_explains_why_it_cannot_be_sent()
        {
            _repository.Setup(r => r.GetExposureAsync(Interviewer, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new InterviewerExposureVM
                {
                    Profile = new ExposureProfileVM { InterviewerEmail = Interviewer, InterviewerName = "Ayesha Khan", IsActive = false },
                    Risks = new[] { new ExposureRiskRowVM { InterviewId = 1, SeverityBreakdown = "InterviewerLateJoin:P2" } }
                });

            var preview = await CreateService().PreviewEmailAsync(EmailRequest());

            Assert.False(preview.CanSend);
            Assert.Contains("inactive", preview.Message);
        }

        [Fact]
        public async Task Send_inside_the_cooldown_does_not_save_or_send()
        {
            GivenExposure(lastEmailUtc: NowUtc.AddHours(-1));

            var result = await CreateService().SendEmailAsync(EmailRequest(), Coach);

            Assert.False(result.Ok);
            _repository.Verify(r => r.SaveSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<bool>()), Times.Never);
            _notification.Verify(n => n.SendInterviewerCoachingEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task A_race_lost_to_another_coach_reports_the_cooldown()
        {
            GivenExposure();
            _repository.Setup(r => r.SaveSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<bool>()))
                .ReturnsAsync(new CoachingSaveResultVM { Result = -1 });

            var result = await CreateService().SendEmailAsync(EmailRequest(), Coach);

            Assert.False(result.Ok);
            Assert.Contains("12 hours", result.Message);
            _notification.Verify(n => n.SendInterviewerCoachingEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Preview_with_no_area_selected_cannot_be_sent()
        {
            GivenExposure();

            var preview = await CreateService().PreviewEmailAsync(new CoachingEmailRequestVM { InterviewerEmail = Interviewer });

            Assert.False(preview.CanSend);
            Assert.Equal(0, preview.ThemeCount);
        }

        [Theory]
        [InlineData("Call", "AgreedToChange", "too short")]
        [InlineData("Email", "AgreedToChange", "Will join five minutes early from now on")]
        [InlineData("Carrier pigeon", "AgreedToChange", "Will join five minutes early from now on")]
        [InlineData("Call", "Maybe", "Will join five minutes early from now on")]
        public async Task Log_rejects_bad_input_without_saving(string channel, string outcome, string agreed)
        {
            var result = await CreateService().LogSessionAsync(new LogCoachingSessionRequestVM
            {
                InterviewerEmail = Interviewer, Channel = channel, Outcome = outcome, Agreed = agreed
            }, Coach);

            Assert.False(result.Ok);
            _repository.Verify(r => r.SaveSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task Log_a_call_links_the_recent_call_and_keeps_only_known_themes()
        {
            _repository.Setup(r => r.SaveSessionAsync(Interviewer, Coach, "Call", "AgreedToChange", "JoinOnTime,ChannelStanding",
                    "Will join five minutes early", null, null, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), true))
                .ReturnsAsync(new CoachingSaveResultVM { Result = 9, LinkedCallSid = "abc-123" });

            var result = await CreateService().LogSessionAsync(new LogCoachingSessionRequestVM
            {
                InterviewerEmail = Interviewer, Channel = "call", Outcome = "agreedtochange", Agreed = "  Will join five minutes early ",
                Themes = new List<string> { "joinontime", "NotATheme", "ChannelStanding", "JoinOnTime" }
            }, Coach);

            Assert.True(result.Ok);
            Assert.Equal("abc-123", result.LinkedCallSid);
        }

        [Fact]
        public async Task Expired_session_cannot_send_or_log()
        {
            Assert.False((await CreateService().SendEmailAsync(EmailRequest(), null)).Ok);
            Assert.False((await CreateService().LogSessionAsync(new LogCoachingSessionRequestVM(), " ")).Ok);
        }
    }
}
