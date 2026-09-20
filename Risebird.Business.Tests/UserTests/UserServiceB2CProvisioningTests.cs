using Moq;
using Risebird.Business.Tests.Fixtures;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;

namespace Risebird.Business.Tests.UserTests
{
    /// <summary>
    /// A user has to exist in Azure AD B2C before they can sign in, and separately in UserList before
    /// the portal will show them a menu. If B2C creation fails but the UserList row is written anyway,
    /// the user looks fully provisioned to an administrator and is told "account not found" at the
    /// sign-in page, with nothing logged anywhere.
    /// <para>
    /// "Already exists" is deliberately not a failure: re-adding somebody who is already in the
    /// directory is a normal case, and must not block provisioning.
    /// </para>
    /// </summary>
    public class UserServiceB2CProvisioningTests
    {
        [Fact]
        public async Task A_user_is_not_written_to_the_database_when_B2C_creation_fails()
        {
            var fixture = new UserServiceFixture { B2CProvisioning = B2CProvisioningOutcome.Failed };
            var sut = fixture.CreateSut();

            var result = await sut.AddUser(UserServiceFixture.NewUser());

            Assert.False(result);
            fixture.Repository.Verify(x => x.AddUser(It.IsAny<UserList>()), Times.Never);
        }

        [Fact]
        public async Task A_failed_B2C_creation_is_logged_so_it_can_be_diagnosed()
        {
            var fixture = new UserServiceFixture { B2CProvisioning = B2CProvisioningOutcome.Failed };
            var sut = fixture.CreateSut();

            await sut.AddUser(UserServiceFixture.NewUser());

            fixture.Logger.Verify(
                x => x.LogMessage(LogLevelInfo.Error, It.IsAny<string>(), It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task An_account_that_already_exists_in_B2C_does_not_block_provisioning()
        {
            // This is the regression guard: treating a 409 as a failure would stop anyone who is
            // already in the directory from being added to the portal.
            var fixture = new UserServiceFixture { B2CProvisioning = B2CProvisioningOutcome.AlreadyExists };
            var sut = fixture.CreateSut();

            var result = await sut.AddUser(UserServiceFixture.NewUser());

            Assert.True(result);
            fixture.Repository.Verify(x => x.AddUser(It.IsAny<UserList>()), Times.Once);
        }

        [Fact]
        public async Task No_password_is_stored_for_an_account_that_already_existed()
        {
            // The generated password was never applied to the existing account, so storing it would
            // put a credential in the database that does not work - and it gets emailed to the user.
            var fixture = new UserServiceFixture { B2CProvisioning = B2CProvisioningOutcome.AlreadyExists };
            var sut = fixture.CreateSut();
            UserList captured = null;
            fixture.Repository
                .Setup(x => x.AddUser(It.IsAny<UserList>()))
                .Callback((UserList u) => { captured = u; u.Id = UserServiceFixture.UserId; })
                .ReturnsAsync(true);

            await sut.AddUser(UserServiceFixture.NewUser());

            Assert.NotNull(captured);
            Assert.True(string.IsNullOrEmpty(captured.UserPassword));
        }

        [Fact]
        public async Task A_newly_created_account_stores_the_generated_password()
        {
            var fixture = new UserServiceFixture { B2CProvisioning = B2CProvisioningOutcome.Created };
            var sut = fixture.CreateSut();
            UserList captured = null;
            fixture.Repository
                .Setup(x => x.AddUser(It.IsAny<UserList>()))
                .Callback((UserList u) => { captured = u; u.Id = UserServiceFixture.UserId; })
                .ReturnsAsync(true);

            await sut.AddUser(UserServiceFixture.NewUser());

            Assert.NotNull(captured);
            Assert.False(string.IsNullOrEmpty(captured.UserPassword));
        }
    }
}
