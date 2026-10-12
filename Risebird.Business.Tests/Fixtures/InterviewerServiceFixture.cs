using Moq;
using Risebird.Business;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.Search;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System.Collections.Generic;

namespace Risebird.Business.Tests.Fixtures
{
    public sealed class InterviewerServiceFixture
    {
        public Mock<IInterviewerRepository> Repository { get; } = new(MockBehavior.Loose);
        public Mock<IInterviewerListingSearchRepository> ListingSearch { get; } = new(MockBehavior.Loose);
        public Mock<IApplicationErrorLogger> Logger { get; } = new(MockBehavior.Loose);
        public Mock<IUserService> UserService { get; } = new(MockBehavior.Loose);
        public Mock<IMeetingRespository> MeetingRepository { get; } = new(MockBehavior.Loose);
        public Mock<INotificationHelper> NotificationHelper { get; } = new(MockBehavior.Loose);
        public Mock<IPasswordHelper> PasswordHelper { get; } = new(MockBehavior.Loose);
        public Mock<IIRFRepository> IrfRepository { get; } = new(MockBehavior.Loose);
        public Mock<IActivityTrackerRepository> ActivityTracker { get; } = new(MockBehavior.Loose);
        public Mock<IEncryptionHelper> EncryptionHelper { get; } = new(MockBehavior.Loose);
        public Mock<ICandidateService> CandidateService { get; } = new(MockBehavior.Loose);

        /// <summary>
        /// Outcome the Azure AD B2C provisioning step reports. Defaults to a freshly created account;
        /// set it to Failed to exercise the abort path.
        /// </summary>
        public B2CProvisioningOutcome B2CProvisioning { get; set; } = B2CProvisioningOutcome.Created;

        public InterviewerService CreateSut()
        {
            ListingSearch.SetupGet(x => x.IsEnabled).Returns(true);
            ListingSearch.Setup(x => x.UpsertInterviewerAsync(It.IsAny<int>())).ReturnsAsync(true);
            ListingSearch.Setup(x => x.UpsertInterviewerByUserIdAsync(It.IsAny<int>())).ReturnsAsync(true);
            ListingSearch.Setup(x => x.UpsertInterviewerByUniqueIdAsync(It.IsAny<string>())).ReturnsAsync(true);
            ListingSearch.Setup(x => x.UpsertInterviewerByEmailAsync(It.IsAny<string>())).ReturnsAsync(true);
            ListingSearch.Setup(x => x.RemoveInterviewerAsync(It.IsAny<int>())).ReturnsAsync(true);
            ListingSearch.Setup(x => x.SearchAllAsync(It.IsAny<string>()))
                .ReturnsAsync((IEnumerable<InterviewerProfileVM>)null);
            ListingSearch.Setup(x => x.SearchPageAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<InterviewFilter>()))
                .ReturnsAsync(new InterviewerSearchPageResult
                {
                    Items = new List<InterviewerProfileVM>(),
                    TotalCount = 0,
                    PageNumber = 1,
                    PageSize = 10
                });

            Logger.Setup(x => x.LogMessage(It.IsAny<LogLevelInfo>(), It.IsAny<Exception>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            Logger.Setup(x => x.LogMessage(It.IsAny<LogLevelInfo>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // AddInterviewerProfile now aborts when Azure AD B2C provisioning fails, so this has to be
            // stubbed - a loose mock would otherwise return null and send every test down the abort path.
            PasswordHelper.Setup(x => x.GenerateUserPassword(It.IsAny<UserDetails>(), It.IsAny<bool>()))
                .ReturnsAsync(() => new B2CProvisioningResult
                {
                    Outcome = B2CProvisioning,
                    Password = B2CProvisioning == B2CProvisioningOutcome.Created ? "Ab@123456" : string.Empty
                });

            ActivityTracker.Setup(x => x.AddActivity(It.IsAny<ActivityTrackerVM>()))
                .ReturnsAsync(true);

            Repository.Setup(x => x.AddOrEditInterviewerProfile(
                    It.IsAny<InterviewerProfileVM>(),
                    It.IsAny<string>(),
                    out It.Ref<string>.IsAny,
                    out It.Ref<int>.IsAny,
                    out It.Ref<string>.IsAny,
                    out It.Ref<bool>.IsAny,
                    out It.Ref<int>.IsAny))
                .Returns((InterviewerProfileVM _, string __, out string assessmentGroupList, out int userId,
                    out string uniqueid, out bool isInsertOperation, out int interviewerProfileId) =>
                {
                    assessmentGroupList = string.Empty;
                    userId = 9;
                    uniqueid = "uid-1";
                    isInsertOperation = true;
                    interviewerProfileId = 55;
                    return true;
                });

            Repository.Setup(x => x.DeleteInterviewerProfile(It.IsAny<int>())).ReturnsAsync(true);
            Repository.Setup(x => x.GetInterviewers()).ReturnsAsync(new List<InterviewerProfileVM>());
            Repository.Setup(x => x.UpdateInterviewerFlag(
                    It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<string>()))
                .ReturnsAsync(true);
            Repository.Setup(x => x.GetInterviewerFlagDetails(It.IsAny<string>()))
                .ReturnsAsync(new InterviewerProfileVM { id = 55, UserId = 9, EmailId = "iv@test.com" });
            Repository.Setup(x => x.UpdateInterviewerActiveStatus(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()))
                .ReturnsAsync(new UserList { EmailId = "iv@test.com", UserPassword = "pwd" });
            Repository.Setup(x => x.UpdateAllIntervieweravailbilityStatus(It.IsAny<int>(), It.IsAny<string>()))
                .Returns(true);
            Repository.Setup(x => x.UpdateInterviewerInterest(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(true);
            Repository.Setup(x => x.SaveInterviewerResume(It.IsAny<InterviewerProfileVM>())).ReturnsAsync(true);
            Repository.Setup(x => x.UpdateVideoConfirmationStatus(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(true);
            Repository.Setup(x => x.SaveInterviewerCodedResume(It.IsAny<InterviewerProfileVM>())).ReturnsAsync(true);
            Repository.Setup(x => x.SaveInterviewerResumesAndCodedResume(It.IsAny<InterviewerProfileVM>()))
                .ReturnsAsync(true);
            Repository.Setup(x => x.AddInterviewerMyProfile(It.IsAny<InterviewerProfileVM>())).ReturnsAsync(true);
            Repository.Setup(x => x.InterviewerRecommendation(It.IsAny<InterviewReport>())).ReturnsAsync(true);
            Repository.Setup(x => x.InactivateInterviewer(
                    It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>()))
                .ReturnsAsync(true);
            Repository.Setup(x => x.ReactivateInterviewer(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(true);
            Repository.Setup(x => x.UpdateInterviewerResume(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);
            Repository.Setup(x => x.UpdateInterviewerAvailabilityStatus(
                    It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(true);
            Repository.Setup(x => x.GetInterviewerProfileById(It.IsAny<int>()))
                .ReturnsAsync(new List<InterviewerProfileVM>
                {
                    new() { id = 55, UserId = 9, EmailId = "iv@test.com", FirstName = "A", LastName = "B" }
                });
            Repository.Setup(x => x.GetAccDetailsList(It.IsAny<int>()))
                .ReturnsAsync(new TransInterviewerAccDetails());
            Repository.Setup(x => x.GetAvailabilityList(It.IsAny<int>()))
                .ReturnsAsync(new List<TransInterviewerAvailability>());

            CandidateService.Setup(x => x.EnsureCandidatePseudonymsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<string>()))
                .ReturnsAsync(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

            NotificationHelper.Setup(x => x.SendWelcomeEmailToInterviewer(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("ok");
            NotificationHelper.Setup(x => x.SendAcknowledgementEmailToInterviewer(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("ok");

            return new InterviewerService(
                Repository.Object,
                Logger.Object,
                UserService.Object,
                MeetingRepository.Object,
                NotificationHelper.Object,
                PasswordHelper.Object,
                IrfRepository.Object,
                ActivityTracker.Object,
                EncryptionHelper.Object,
                CandidateService.Object,
                ListingSearch.Object);
        }
    }
}
