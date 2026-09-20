using Moq;
using Risebird.Business.Tests.Fixtures;
using Risebird.Entities.Data;

namespace Risebird.Business.Tests.InterviewerTests
{
    /// <summary>
    /// The welcome email carries the portal walkthrough and the interview-instruction video as well
    /// as the sign-in credentials, so it has to go out even when there is no generated password to
    /// send - an interviewer provisioned against an Azure AD B2C account that already existed keeps
    /// their own credentials, but still needs the instructions.
    /// </summary>
    public class InterviewerServiceWelcomeEmailTests
    {
        private const int UserId = 77;

        private static UserList ActivatedInterviewer(string password) => new UserList
        {
            EmailId = "interviewer@example.com",
            UserPassword = password,
            HMVideoForTargetIRF = "https://video.example.com/briefing"
        };

        [Fact]
        public async Task The_generated_password_is_emailed_when_one_was_created()
        {
            var fixture = new InterviewerServiceFixture();
            fixture.Repository
                .Setup(x => x.UpdateInterviewerActiveStatus(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()))
                .ReturnsAsync(ActivatedInterviewer("Ab@123456"));
            var sut = fixture.CreateSut();

            await sut.UpdateInterviewerActiveStatus(1, UserId);

            fixture.NotificationHelper.Verify(
                x => x.SendWelcomeEmailToInterviewer("interviewer@example.com", "Ab@123456", "https://video.example.com/briefing"),
                Times.Once);
        }

        [Fact]
        public async Task The_welcome_email_still_goes_out_when_no_password_was_generated()
        {
            // Regression guard: suppressing the whole email here would also withhold the interview
            // instructions, which is worse than sending it without credentials.
            var fixture = new InterviewerServiceFixture();
            fixture.Repository
                .Setup(x => x.UpdateInterviewerActiveStatus(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()))
                .ReturnsAsync(ActivatedInterviewer(string.Empty));
            var sut = fixture.CreateSut();

            await sut.UpdateInterviewerActiveStatus(1, UserId);

            fixture.NotificationHelper.Verify(
                x => x.SendWelcomeEmailToInterviewer(
                    "interviewer@example.com",
                    It.IsAny<string>(),
                    "https://video.example.com/briefing"),
                Times.Once);
        }

        [Fact]
        public async Task A_blank_credential_is_never_emailed()
        {
            var fixture = new InterviewerServiceFixture();
            fixture.Repository
                .Setup(x => x.UpdateInterviewerActiveStatus(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()))
                .ReturnsAsync(ActivatedInterviewer(null));
            var sut = fixture.CreateSut();

            await sut.UpdateInterviewerActiveStatus(1, UserId);

            fixture.NotificationHelper.Verify(
                x => x.SendWelcomeEmailToInterviewer(
                    It.IsAny<string>(),
                    It.Is<string>(p => !string.IsNullOrWhiteSpace(p)),
                    It.IsAny<string>()),
                Times.Once);
        }
    }
}
