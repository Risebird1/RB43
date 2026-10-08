using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using Risebird.CommonHelper.Interface;
using Risebird.Repository.Interface;
using Xunit;

namespace Risebird.CommonHelper.Tests.CommonHelperLogic
{
    /// <summary>
    /// Interview invites share one calendar identity (UID interview-{id}@risebird.io) with a growing SEQUENCE, so a
    /// bounce resend updates the existing event and a cancel withdraws it (Bounce &amp; Dropped QA round 1, Q4).
    /// </summary>
    public class InterviewCalendarIcsTests
    {
        private static NotificationHelper Helper(Mock<ISendGridRepository> repo = null)
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
            {
                ["SendGridKeySettings:MailFrom"] = "notify@risebird.io",
            }).Build();
            return new NotificationHelper(new Mock<IAzureStorageHelper>().Object, config, new Mock<IApplicationErrorLogger>().Object,
                (repo ?? new Mock<ISendGridRepository>()).Object, new Mock<IHttpContextAccessor>().Object, new Mock<IValidationHelper>().Object);
        }

        [Fact]
        public void Invite_uses_the_interview_uid_and_given_sequence()
        {
            var ics = Helper().BuildInterviewInviteIcs(42, 3, new DateTime(2026, 10, 9, 5, 30, 0, DateTimeKind.Utc), 45,
                "https://zoom/1", "Invitation: A, B", new[] { "new@mail.com", "hr@acme.com" });

            Assert.Contains("METHOD:REQUEST", ics);
            Assert.Contains("UID:interview-42@risebird.io", ics);
            Assert.Contains("SEQUENCE:3", ics);
            Assert.Contains("DTSTART:20261009T053000Z", ics);
            Assert.Contains("DTEND:20261009T061500Z", ics);
            Assert.Contains("STATUS:CONFIRMED", ics);
            Assert.Contains("mailto:new@mail.com", ics);
            Assert.Contains("mailto:hr@acme.com", ics);
            // Commas in text must be escaped or calendars truncate the summary.
            Assert.Contains("SUMMARY:Invitation: A\\, B", ics);
        }

        [Fact]
        public void Invite_without_duration_lasts_an_hour()
        {
            var ics = Helper().BuildInterviewInviteIcs(1, 1, new DateTime(2026, 10, 9, 5, 0, 0, DateTimeKind.Utc), 0, null, "S", new[] { "a@b.com" });

            Assert.Contains("DTEND:20261009T060000Z", ics);
            Assert.DoesNotContain("LOCATION:", ics);
        }

        [Fact]
        public async Task Sequence_comes_from_the_repository()
        {
            var repo = new Mock<ISendGridRepository>();
            repo.Setup(r => r.NextInterviewInviteSequence(7)).ReturnsAsync(5);

            Assert.Equal(5, await Helper(repo).NextInterviewInviteSequence(7));
        }

        [Fact]
        public async Task Sequence_is_null_when_it_cannot_be_read()
        {
            var repo = new Mock<ISendGridRepository>();
            repo.Setup(r => r.NextInterviewInviteSequence(It.IsAny<int>())).ReturnsAsync((int?)null);

            Assert.Null(await Helper(repo).NextInterviewInviteSequence(7));
        }
    }
}
