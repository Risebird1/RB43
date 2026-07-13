using Microsoft.Extensions.Configuration;
using Moq;
using Risebird.Business;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using IrfEntity = Risebird.Entities.ViewModel.IRF;

namespace Risebird.Business.Tests.Fixtures
{
    public sealed class IRFServiceFixture
    {
        public Mock<IIRFRepository> Repository { get; } = new(MockBehavior.Loose);
        public Mock<IIrfListingSearchRepository> ListingSearch { get; } = new(MockBehavior.Loose);
        public Mock<INotificationHelper> NotificationHelper { get; } = new(MockBehavior.Loose);
        public Mock<IApplicationErrorLogger> Logger { get; } = new(MockBehavior.Loose);
        public Mock<IClientService> ClientService { get; } = new(MockBehavior.Loose);
        public Mock<IConfiguration> Configuration { get; } = new(MockBehavior.Loose);
        public Mock<IActivityTrackerRepository> ActivityTracker { get; } = new(MockBehavior.Loose);
        public Mock<IWhatsAppHelper> WhatsAppHelper { get; } = new(MockBehavior.Loose);
        public Mock<ITwilioHelper> TwilioHelper { get; } = new(MockBehavior.Loose);
        public Mock<ICalendlyHelper> CalendlyHelper { get; } = new(MockBehavior.Loose);
        public Mock<ICandidateRepository> CandidateRepository { get; } = new(MockBehavior.Loose);
        public Mock<IInterviewSchedulerRepository> InterviewSchedulerRepository { get; } = new(MockBehavior.Loose);
        public Mock<IClonableSkillChannelTemplateRepository> ClonableTemplateRepository { get; } = new(MockBehavior.Loose);
        public Mock<IUserRepository> UserRepository { get; } = new(MockBehavior.Loose);

        public IRFService CreateSut()
        {
            ListingSearch.SetupGet(x => x.IsEnabled).Returns(true);
            ListingSearch.Setup(x => x.UpsertIrfAsync(It.IsAny<int>())).ReturnsAsync(true);
            ListingSearch.Setup(x => x.RemoveIrfAsync(It.IsAny<int>())).ReturnsAsync(true);
            ListingSearch.Setup(x => x.SearchAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<IRFFilter>()))
                .ReturnsAsync(new List<IrfEntity>());

            Logger.Setup(x => x.LogMessage(It.IsAny<LogLevelInfo>(), It.IsAny<Exception>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            Logger.Setup(x => x.LogMessage(It.IsAny<LogLevelInfo>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            ActivityTracker.Setup(x => x.AddActivity(It.IsAny<ActivityTrackerVM>()))
                .ReturnsAsync(true);

            Repository.Setup(x => x.GetSkillChannelDetailsByName(It.IsAny<string>()))
                .ReturnsAsync(new IrfEntity { id = 42, IRFId = "SC-1" });

            Repository.Setup(x => x.AddIRFDetails(It.IsAny<IRFVm>(), out It.Ref<int>.IsAny))
                .Returns((IRFVm _, out int irfId) =>
                {
                    irfId = 101;
                    return true;
                });

            Repository.Setup(x => x.UpdateIRFDetails(
                    It.IsAny<IRFVm>(),
                    It.IsAny<List<InterviewerSuccessRateChangeData>>(),
                    It.IsAny<List<RemovedInterviewersFromSkillsChannelData>>(),
                    It.IsAny<List<AddedNewInterviewersInSkillsChannelData>>(),
                    It.IsAny<List<RemovedTechSkillsFromIRF>>()))
                .ReturnsAsync(Tuple.Create(true, false));

            Repository.Setup(x => x.CreateSkillChannelFromTemplate(It.IsAny<IRFVm>(), It.IsAny<string>()))
                .ReturnsAsync(Tuple.Create(true, 10));
            Repository.Setup(x => x.CreateSkillChannelFromTemplateV1(It.IsAny<IRFVm>(), It.IsAny<string>()))
                .ReturnsAsync(Tuple.Create(true, 10));
            Repository.Setup(x => x.EditSkillChannelCreatedFromTemplate(It.IsAny<IRFVm>()))
                .ReturnsAsync(true);

            Repository.Setup(x => x.ArchiveIRF(It.IsAny<int>())).ReturnsAsync(true);
            Repository.Setup(x => x.UnArchiveIRF(It.IsAny<int>())).ReturnsAsync(true);
            Repository.Setup(x => x.PlayIRF(It.IsAny<string>())).Returns(true);
            Repository.Setup(x => x.UpdateMeetingStatus(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<MeetingVM>());
            Repository.Setup(x => x.UpdateSkillChannelEmbeddableUrl(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);
            Repository.Setup(x => x.UpdateNewPriority(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(true);
            Repository.Setup(x => x.UpdateIRFInterviewerBillingCycle(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(true);
            Repository.Setup(x => x.GetUsers()).Returns(new List<Users>());
            Repository.Setup(x => x.GetEligibleChannelOwners()).Returns(new List<Users>
            {
                new() { id = 2, EmailId = "hr@test.com" }
            });
            Repository.Setup(x => x.GetUserId(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(2);
            Repository.Setup(x => x.GetAllIRFs(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<IRFFilter>()))
                .ReturnsAsync(new List<IrfEntity>());

            ClientService.Setup(x => x.GetClient(It.IsAny<int>(), It.IsAny<bool>())).ReturnsAsync(new Client { Name = "Client" });
            ClientService.Setup(x => x.GetClientName(It.IsAny<int>())).ReturnsAsync("Client");
            UserRepository.Setup(x => x.GetUser(It.IsAny<int>()))
                .ReturnsAsync(new UserList { EmailId = "hr@test.com" });
            NotificationHelper.Setup(x => x.SendAssignedNotificationToSME(
                    It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("ok");
            NotificationHelper.Setup(x => x.SendIRFCreatedFromTemplateNotificationToChannelOwner(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("ok");

            return new IRFService(
                Repository.Object,
                Logger.Object,
                NotificationHelper.Object,
                ClientService.Object,
                Configuration.Object,
                ActivityTracker.Object,
                WhatsAppHelper.Object,
                TwilioHelper.Object,
                CalendlyHelper.Object,
                CandidateRepository.Object,
                InterviewSchedulerRepository.Object,
                ClonableTemplateRepository.Object,
                UserRepository.Object,
                ListingSearch.Object);
        }
    }
}
