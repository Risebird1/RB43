using Moq;
using Risebird.Business.Tests.Fixtures;

namespace Risebird.Business.Tests.UserTests
{
    /// <summary>
    /// The IRF index denormalizes UserList.EmailId into <c>risebirdHrEmail</c> and into the
    /// <c>allowedViewerEmails</c> ACL, so a user save that does not re-index leaves the IRF list
    /// showing a stale owner — or, worse, showing IRFs to someone who was just removed from them.
    /// </summary>
    public class UserServiceIrfSyncTests
    {
        [Fact]
        public async Task Adding_a_user_reindexes_the_IRFs_they_land_on()
        {
            var fixture = new UserServiceFixture();
            var sut = fixture.CreateSut();

            var result = await sut.AddUser(UserServiceFixture.NewUser());

            Assert.True(result);
            fixture.IrfListingSearch.Verify(
                x => x.UpsertIrfsByUserIdAsync(UserServiceFixture.UserId, It.IsAny<IEnumerable<int>>()),
                Times.Once);
        }

        [Fact]
        public async Task A_new_user_is_synced_under_the_id_the_insert_assigned_not_the_zero_it_arrived_with()
        {
            var fixture = new UserServiceFixture();
            var sut = fixture.CreateSut();
            var user = UserServiceFixture.NewUser();

            Assert.Equal(0, user.Id);
            await sut.AddUser(user);

            fixture.IrfListingSearch.Verify(
                x => x.UpsertIrfsByUserIdAsync(0, It.IsAny<IEnumerable<int>>()), Times.Never);
            fixture.IrfListingSearch.Verify(
                x => x.UpsertIrfsByUserIdAsync(UserServiceFixture.UserId, It.IsAny<IEnumerable<int>>()),
                Times.Once);
        }

        [Fact]
        public async Task Updating_a_user_reindexes_the_IRFs_they_are_on()
        {
            var fixture = new UserServiceFixture();
            var sut = fixture.CreateSut();

            var result = await sut.UpdateUser(UserServiceFixture.NewUser(UserServiceFixture.UserId));

            Assert.True(result);
            fixture.IrfListingSearch.Verify(
                x => x.UpsertIrfsByUserIdAsync(UserServiceFixture.UserId, It.IsAny<IEnumerable<int>>()),
                Times.Once);
        }

        [Fact]
        public async Task An_update_carries_the_pre_write_IRF_set_so_a_dropped_assignment_loses_its_stale_ACL_entry()
        {
            var fixture = new UserServiceFixture { CurrentIrfIntIds = new List<int> { 11, 22 } };
            var sut = fixture.CreateSut();

            await sut.UpdateUser(UserServiceFixture.NewUser(UserServiceFixture.UserId));

            // 11 and 22 are the IRFs the user was on *before* the save. The client assignment is
            // rewritten delete-then-insert, so if the edit dropped one of them the lookup after
            // the write would no longer return it — yet it still carries the user's email.
            Assert.NotNull(fixture.LastAdditionalIrfIntIds);
            Assert.Equal(new[] { 11, 22 }, fixture.LastAdditionalIrfIntIds);
        }

        [Fact]
        public async Task The_pre_write_snapshot_is_taken_before_the_row_is_written()
        {
            var fixture = new UserServiceFixture();
            var sut = fixture.CreateSut();

            await sut.UpdateUser(UserServiceFixture.NewUser(UserServiceFixture.UserId));

            var lookup = fixture.Calls.IndexOf("irf.GetIds");
            var write = fixture.Calls.IndexOf("repo.UpdateUser");

            Assert.True(lookup >= 0, "the IRF snapshot was never taken");
            Assert.True(write >= 0, "the user row was never written");
            Assert.True(lookup < write, $"snapshot must precede the write, got: {string.Join(" -> ", fixture.Calls)}");
        }

        [Fact]
        public async Task Deleting_a_user_reindexes_the_IRFs_they_were_on()
        {
            var fixture = new UserServiceFixture { CurrentIrfIntIds = new List<int> { 33 } };
            var sut = fixture.CreateSut();

            var result = await sut.DeleteUser(UserServiceFixture.UserId);

            Assert.True(result);
            // The row is gone after the delete, so the snapshot is the only record of which
            // IRFs still carry the user's email.
            Assert.Equal(new[] { 33 }, fixture.LastAdditionalIrfIntIds);
            fixture.IrfListingSearch.Verify(
                x => x.UpsertIrfsByUserIdAsync(UserServiceFixture.UserId, It.IsAny<IEnumerable<int>>()),
                Times.Once);
        }

        [Theory]
        [InlineData("add")]
        [InlineData("update")]
        [InlineData("delete")]
        public async Task A_save_that_failed_does_not_touch_the_index(string operation)
        {
            var fixture = new UserServiceFixture { RepositorySucceeds = false };
            var sut = fixture.CreateSut();

            var result = operation switch
            {
                "add" => await sut.AddUser(UserServiceFixture.NewUser()),
                "update" => await sut.UpdateUser(UserServiceFixture.NewUser(UserServiceFixture.UserId)),
                _ => await sut.DeleteUser(UserServiceFixture.UserId)
            };

            Assert.False(result);
            fixture.IrfListingSearch.Verify(
                x => x.UpsertIrfsByUserIdAsync(It.IsAny<int>(), It.IsAny<IEnumerable<int>>()),
                Times.Never);
        }

        [Fact]
        public async Task Nothing_is_indexed_while_the_IRF_listing_plugin_is_switched_off()
        {
            var fixture = new UserServiceFixture { IrfListingEnabled = false };
            var sut = fixture.CreateSut();

            await sut.UpdateUser(UserServiceFixture.NewUser(UserServiceFixture.UserId));

            fixture.IrfListingSearch.Verify(
                x => x.UpsertIrfsByUserIdAsync(It.IsAny<int>(), It.IsAny<IEnumerable<int>>()),
                Times.Never);
            fixture.IrfListingSearch.Verify(
                x => x.GetIrfIntIdsByUserIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task An_index_failure_is_logged_but_does_not_fail_the_save()
        {
            var fixture = new UserServiceFixture();
            var sut = fixture.CreateSut();
            fixture.IrfListingSearch
                .Setup(x => x.UpsertIrfsByUserIdAsync(It.IsAny<int>(), It.IsAny<IEnumerable<int>>()))
                .ThrowsAsync(new InvalidOperationException("azure search is down"));

            var result = await sut.UpdateUser(UserServiceFixture.NewUser(UserServiceFixture.UserId));

            // The row was saved; indexing is best-effort and must not roll the save back.
            Assert.True(result);
            fixture.Logger.Verify(
                x => x.LogMessage(It.IsAny<Risebird.Entities.Enum.LogLevelInfo>(), It.IsAny<Exception>(), It.IsAny<string>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task A_failing_snapshot_lookup_does_not_stop_the_save_or_the_reindex()
        {
            var fixture = new UserServiceFixture();
            var sut = fixture.CreateSut();
            fixture.IrfListingSearch
                .Setup(x => x.GetIrfIntIdsByUserIdAsync(It.IsAny<int>()))
                .ThrowsAsync(new InvalidOperationException("sql timeout"));

            var result = await sut.UpdateUser(UserServiceFixture.NewUser(UserServiceFixture.UserId));

            Assert.True(result);
            // Still re-index what the user is on now, even though the "before" set was lost.
            fixture.IrfListingSearch.Verify(
                x => x.UpsertIrfsByUserIdAsync(UserServiceFixture.UserId, It.IsAny<IEnumerable<int>>()),
                Times.Once);
        }

        [Fact]
        public async Task Updating_a_user_still_reindexes_the_interviewer_roster()
        {
            var fixture = new UserServiceFixture();
            var sut = fixture.CreateSut();

            await sut.UpdateUser(UserServiceFixture.NewUser(UserServiceFixture.UserId));

            // The IRF sync is additional to the interviewer sync, not a replacement for it.
            fixture.InterviewerListingSearch.Verify(
                x => x.UpsertInterviewerByUserIdAsync(UserServiceFixture.UserId), Times.Once);
        }
    }
}
