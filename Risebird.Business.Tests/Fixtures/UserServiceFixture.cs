using Moq;
using Risebird.Business;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;

namespace Risebird.Business.Tests.Fixtures
{
    /// <summary>
    /// Builds a <see cref="UserService"/> over loose mocks, and records the order in which the
    /// save and the index lookups happen — the pre-write snapshot only works if it is taken
    /// before the repository write, so ordering is the property under test, not just call count.
    /// </summary>
    public sealed class UserServiceFixture
    {
        public const int UserId = 77;

        public Mock<IUserRepository> Repository { get; } = new(MockBehavior.Loose);
        public Mock<IApplicationErrorLogger> Logger { get; } = new(MockBehavior.Loose);
        public Mock<IPasswordHelper> PasswordHelper { get; } = new(MockBehavior.Loose);
        public Mock<IInterviewerListingSearchRepository> InterviewerListingSearch { get; } = new(MockBehavior.Loose);
        public Mock<ICallCatalogueListingSearchRepository> CallCatalogueListingSearch { get; } = new(MockBehavior.Loose);
        public Mock<IIrfListingSearchRepository> IrfListingSearch { get; } = new(MockBehavior.Loose);

        /// <summary>Every collaborator call that matters, in the order it happened.</summary>
        public List<string> Calls { get; } = new();

        /// <summary>IRFs the user is on when <c>GetIrfIntIdsByUserIdAsync</c> is asked.</summary>
        public List<int> CurrentIrfIntIds { get; set; } = new() { 11, 22 };

        /// <summary>Whether the IRF listing plugin is switched on in configuration.</summary>
        public bool IrfListingEnabled { get; set; } = true;

        /// <summary>Result the user repository returns for Add / Update / Delete.</summary>
        public bool RepositorySucceeds { get; set; } = true;

        /// <summary>Captured <c>additionalIrfIntIds</c> from the last upsert call.</summary>
        public IEnumerable<int> LastAdditionalIrfIntIds { get; private set; }

        public UserService CreateSut()
        {
            Logger.Setup(x => x.LogMessage(It.IsAny<LogLevelInfo>(), It.IsAny<Exception>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            Logger.Setup(x => x.LogMessage(It.IsAny<LogLevelInfo>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            PasswordHelper.Setup(x => x.GenerateUserPassword(It.IsAny<UserDetails>(), It.IsAny<bool>()))
                .ReturnsAsync("Ab@123456");

            // AddUser is what assigns the identity, so the sync that follows depends on it.
            Repository.Setup(x => x.AddUser(It.IsAny<UserList>()))
                .Callback((UserList u) => { Calls.Add("repo.AddUser"); u.Id = UserId; })
                .ReturnsAsync(() => RepositorySucceeds);

            Repository.Setup(x => x.UpdateUser(It.IsAny<UserList>()))
                .Callback(() => Calls.Add("repo.UpdateUser"))
                .ReturnsAsync(() => RepositorySucceeds);

            Repository.Setup(x => x.DeleteUser(It.IsAny<int>()))
                .Callback(() => Calls.Add("repo.DeleteUser"))
                .ReturnsAsync(() => RepositorySucceeds);

            InterviewerListingSearch.SetupGet(x => x.IsEnabled).Returns(true);
            InterviewerListingSearch.Setup(x => x.UpsertInterviewerByUserIdAsync(It.IsAny<int>()))
                .ReturnsAsync(true);
            InterviewerListingSearch.Setup(x => x.RemoveInterviewerAsync(It.IsAny<int>()))
                .ReturnsAsync(true);
            InterviewerListingSearch.Setup(x => x.GetIndexDocumentsFromDatabaseAsync(It.IsAny<int?>(), It.IsAny<int?>()))
                .ReturnsAsync(new List<Risebird.Entities.Search.InterviewerIndexDocument>());

            CallCatalogueListingSearch.SetupGet(x => x.IsEnabled).Returns(false);

            IrfListingSearch.SetupGet(x => x.IsEnabled).Returns(() => IrfListingEnabled);
            IrfListingSearch.Setup(x => x.GetIrfIntIdsByUserIdAsync(It.IsAny<int>()))
                .Callback(() => Calls.Add("irf.GetIds"))
                .ReturnsAsync(() => CurrentIrfIntIds);
            IrfListingSearch.Setup(x => x.UpsertIrfsByUserIdAsync(It.IsAny<int>(), It.IsAny<IEnumerable<int>>()))
                .Callback((int _, IEnumerable<int> additional) =>
                {
                    Calls.Add("irf.Upsert");
                    LastAdditionalIrfIntIds = additional;
                })
                .ReturnsAsync(1);

            return new UserService(
                Repository.Object,
                Logger.Object,
                PasswordHelper.Object,
                InterviewerListingSearch.Object,
                CallCatalogueListingSearch.Object,
                IrfListingSearch.Object);
        }

        public static UserList NewUser(int id = 0) => new()
        {
            Id = id,
            FirstName = "Ada",
            LastName = "Lovelace",
            EmailId = "ada@test.com",
            ClientsArray = new[] { "5" }
        };
    }
}
