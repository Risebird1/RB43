using Moq;
using Risebird.Business;
using Risebird.Business.Tests.Fixtures;
using Risebird.Business.Tests.Infrastructure;
using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;

namespace Risebird.Business.Tests.InterviewerTests
{
    public class InterviewerServiceMethodCoverageTests
    {
        [Fact]
        public void Declared_public_methods_are_discoverable()
        {
            var methods = ServiceMethodInvoker.GetPublicInstanceMethods(typeof(InterviewerService)).ToList();
            Assert.NotEmpty(methods);
            Assert.Contains(methods, m => m.Name == nameof(InterviewerService.AddInterviewerProfile));
            Assert.Contains(methods, m => m.Name == nameof(InterviewerService.EditInterviewerProfile));
            Assert.Contains(methods, m => m.Name == nameof(InterviewerService.GetInterviewers));
        }

        [Theory]
        [MemberData(nameof(AllPublicMethods))]
        public async Task Every_public_method_can_be_invoked_without_unhandled_exception(string methodKey)
        {
            var fixture = new InterviewerServiceFixture();
            var sut = fixture.CreateSut();
            var method = ServiceMethodInvoker.GetPublicInstanceMethods(typeof(InterviewerService))
                .Single(m => ServiceMethodInvoker.GetMethodKey(m) == methodKey);

            var exception = await Record.ExceptionAsync(() => ServiceMethodInvoker.InvokeAsync(sut, method));
            Assert.Null(exception);
        }

        public static IEnumerable<object[]> AllPublicMethods() =>
            ServiceMethodInvoker.GetPublicInstanceMethods(typeof(InterviewerService))
                .Select(m => new object[] { ServiceMethodInvoker.GetMethodKey(m) });
    }

    public class InterviewerServiceIndexSyncTests
    {
        [Fact]
        public void AddInterviewerProfile_upserts_search_index()
        {
            var fixture = new InterviewerServiceFixture();
            var sut = fixture.CreateSut();
            var model = MinimalProfile();

            var result = sut.AddInterviewerProfile(model, "admin@test.com", out _, out var userId, out _);

            Assert.True(result);
            Assert.Equal(9, userId);
            fixture.ListingSearch.Verify(x => x.UpsertInterviewerAsync(55), Times.Once);
        }

        [Fact]
        public void EditInterviewerProfile_upserts_search_index()
        {
            var fixture = new InterviewerServiceFixture();
            var sut = fixture.CreateSut();
            var model = MinimalProfile();
            model.AssessmentGroupArray = new[] { "1" };
            model.AssessmentGroupArrayOld = new[] { "1" };

            var result = sut.EditInterviewerProfile(model, "admin@test.com", out _, out _);

            Assert.True(result);
            fixture.ListingSearch.Verify(x => x.UpsertInterviewerAsync(55), Times.Once);
        }

        [Fact]
        public async Task DeleteInterviewerProfile_upserts_by_user_id()
        {
            var fixture = new InterviewerServiceFixture();
            var sut = fixture.CreateSut();

            var result = await sut.DeleteInterviewerProfile(9);

            Assert.True(result);
            fixture.ListingSearch.Verify(x => x.UpsertInterviewerByUserIdAsync(9), Times.Once);
        }

        [Fact]
        public async Task UpdateInterviewerFlag_upserts_search_index()
        {
            var fixture = new InterviewerServiceFixture();
            var sut = fixture.CreateSut();

            var result = await sut.UpdateInterviewerFlag("iv@test.com", true, true, true, false);

            Assert.True(result);
            fixture.ListingSearch.Verify(x => x.UpsertInterviewerAsync(55), Times.Once);
        }

        [Fact]
        public async Task UpdateInterviewerActiveStatus_upserts_before_welcome_email()
        {
            var fixture = new InterviewerServiceFixture();
            var sut = fixture.CreateSut();

            var result = await sut.UpdateInterviewerActiveStatus(1, 9);

            Assert.NotNull(result);
            fixture.ListingSearch.Verify(x => x.UpsertInterviewerByUserIdAsync(9), Times.Once);
            fixture.NotificationHelper.Verify(x => x.SendWelcomeEmailToInterviewer(
                "iv@test.com", "pwd", It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void UpdateAllIntervieweravailbilityStatus_upserts_by_unique_id()
        {
            var fixture = new InterviewerServiceFixture();
            var sut = fixture.CreateSut();

            var result = sut.UpdateAllIntervieweravailbilityStatus(1, "uid-99");

            Assert.True(result);
            fixture.ListingSearch.Verify(x => x.UpsertInterviewerByUniqueIdAsync("uid-99"), Times.Once);
        }

        [Fact]
        public async Task UpdateInterviewerInterest_upserts_by_unique_id()
        {
            var fixture = new InterviewerServiceFixture();
            var sut = fixture.CreateSut();

            var result = await sut.UpdateInterviewerInterest(1, "uid-88");

            Assert.True(result);
            fixture.ListingSearch.Verify(x => x.UpsertInterviewerByUniqueIdAsync("uid-88"), Times.Once);
        }

        [Fact]
        public async Task SaveInterviewerResume_upserts_by_email()
        {
            var fixture = new InterviewerServiceFixture();
            var sut = fixture.CreateSut();
            var model = new InterviewerProfileVM { EmailId = "iv@test.com", ResumeLink = "https://r" };

            var result = await sut.SaveInterviewerResume(model);

            Assert.True(result);
            fixture.ListingSearch.Verify(x => x.UpsertInterviewerByEmailAsync("iv@test.com"), Times.Once);
        }

        [Fact]
        public async Task UpdateVideoConfirmationStatus_upserts_by_email()
        {
            var fixture = new InterviewerServiceFixture();
            var sut = fixture.CreateSut();

            var result = await sut.UpdateVideoConfirmationStatus(1, "iv@test.com");

            Assert.True(result);
            fixture.ListingSearch.Verify(x => x.UpsertInterviewerByEmailAsync("iv@test.com"), Times.Once);
        }

        [Fact]
        public async Task AddInterviewerMyProfile_upserts_search_index()
        {
            var fixture = new InterviewerServiceFixture();
            var sut = fixture.CreateSut();
            var model = MinimalProfile();
            model.id = 55;
            model.InterviewerAvailabilityDetails = new List<TransInterviewerAvailability>
            {
                new() { Weekday = "Mon" }
            };

            var result = await sut.AddInterviewerMyProfile(model);

            Assert.True(result);
            fixture.ListingSearch.Verify(x => x.UpsertInterviewerAsync(55), Times.Once);
        }

        [Fact]
        public async Task InterviewerRecommendation_upserts_by_user_id()
        {
            var fixture = new InterviewerServiceFixture();
            var sut = fixture.CreateSut();
            var report = new InterviewReport { Userid = 9 };

            var result = await sut.InterviewerRecommendation(report);

            Assert.True(result);
            fixture.ListingSearch.Verify(x => x.UpsertInterviewerByUserIdAsync(9), Times.Once);
        }

        [Fact]
        public async Task InactivateInterviewer_upserts_by_user_id()
        {
            var fixture = new InterviewerServiceFixture();
            var sut = fixture.CreateSut();

            var result = await sut.InactivateInterviewer(9, "admin@test.com", "reason", true);

            Assert.True(result);
            fixture.ListingSearch.Verify(x => x.UpsertInterviewerByUserIdAsync(9), Times.Once);
        }

        [Fact]
        public async Task ReactivateInterviewer_upserts_by_user_id()
        {
            var fixture = new InterviewerServiceFixture();
            var sut = fixture.CreateSut();

            var result = await sut.ReactivateInterviewer(9, "admin@test.com");

            Assert.True(result);
            fixture.ListingSearch.Verify(x => x.UpsertInterviewerByUserIdAsync(9), Times.Once);
        }

        [Fact]
        public async Task UpdateInterviewerResume_upserts_by_user_id()
        {
            var fixture = new InterviewerServiceFixture();
            var sut = fixture.CreateSut();

            var result = await sut.UpdateInterviewerResume(9, "https://resume", "admin@test.com");

            Assert.True(result);
            fixture.ListingSearch.Verify(x => x.UpsertInterviewerByUserIdAsync(9), Times.Once);
        }

        [Fact]
        public async Task UpdateInterviewerAvailabilityStatus_upserts_by_user_id()
        {
            var fixture = new InterviewerServiceFixture();
            var sut = fixture.CreateSut();

            var result = await sut.UpdateInterviewerAvailabilityStatus(9, true, 3, "admin@test.com");

            Assert.True(result);
            fixture.ListingSearch.Verify(x => x.UpsertInterviewerByUserIdAsync(9), Times.Once);
        }

        [Fact]
        public void GetInterviewers_uses_search_when_enabled()
        {
            var fixture = new InterviewerServiceFixture();
            var sut = fixture.CreateSut();
            fixture.ListingSearch.Setup(x => x.SearchAllAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<InterviewerProfileVM>
                {
                    new() { id = 1, EmailId = "a@test.com" }
                });

            var result = sut.GetInterviewers();

            Assert.Single(result);
            fixture.ListingSearch.Verify(x => x.SearchAllAsync(null), Times.Once);
            fixture.Repository.Verify(x => x.GetInterviewers(), Times.Never);
        }

        [Fact]
        public void AddInterviewerProfile_skips_upsert_when_search_disabled()
        {
            var fixture = new InterviewerServiceFixture();
            var sut = fixture.CreateSut();
            fixture.ListingSearch.SetupGet(x => x.IsEnabled).Returns(false);

            var result = sut.AddInterviewerProfile(MinimalProfile(), "admin@test.com", out _, out _, out _);

            Assert.True(result);
            fixture.ListingSearch.Verify(x => x.UpsertInterviewerAsync(It.IsAny<int>()), Times.Never);
        }

        private static InterviewerProfileVM MinimalProfile() =>
            new()
            {
                EmailId = "iv@test.com",
                FirstName = "Ada",
                LastName = "Lovelace",
                IndiaServiceRate = 100,
                AssessmentGroupArray = Array.Empty<string>(),
                SuppressAcknowledgementEmail = true
            };
    }
}
