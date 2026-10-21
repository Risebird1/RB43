using Newtonsoft.Json.Linq;
using Risebird.Business;
using Risebird.Entities.ViewModel;
using Xunit;
using static Risebird.Business.CodeProjectVmHelper;

namespace Risebird.Business.Tests.CodeProjectTests
{
    public class CodeProjectSessionReviewTests
    {
        private static readonly DateTime Now = new DateTime(2026, 10, 15, 10, 0, 0, DateTimeKind.Utc);
        private const string Vm = "/subscriptions/s/resourceGroups/rg/providers/Microsoft.Compute/virtualMachines/rbcp-abc";

        private static BastionSession S(string id, string user = "candidate", int minutesAgo = 5) =>
            new BastionSession { SessionId = id, UserName = user, TargetResourceId = Vm, StartTimeUtc = Now.AddMinutes(-minutesAgo) };

        private static SessionReview Review(IList<BastionSession> sessions, DateTime? deadline = null, DateTime? lockedAt = null,
            IEnumerable<string> seen = null, IEnumerable<CodeProjectIntegrityFlag> flags = null) =>
            ReviewSessions("candidate", deadline ?? Now.AddMinutes(10), lockedAt, seen, flags, sessions, Now);

        [Fact]
        public void One_candidate_session_before_the_deadline_is_left_alone()
        {
            var r = Review(new[] { S("a") });
            Assert.Empty(r.Disconnect);
            Assert.Empty(r.NewFlags);
            Assert.False(r.LockNow);
            Assert.Contains("a", r.Seen);
        }

        [Fact]
        public void Domain_prefixed_candidate_user_counts_as_the_candidate()
        {
            var r = Review(new[] { S("a", @"RBCPABC\candidate") });
            Assert.Empty(r.Disconnect);
        }

        [Fact]
        public void A_second_session_keeps_the_newest_and_flags_once()
        {
            var r = Review(new[] { S("old", minutesAgo: 9), S("new", minutesAgo: 1) });
            Assert.Equal(new[] { "old" }, r.Disconnect);
            var flag = Assert.Single(r.NewFlags);
            Assert.Equal("concurrent-sessions", flag.Code);

            var again = Review(new[] { S("old", minutesAgo: 9), S("new", minutesAgo: 1) }, flags: r.NewFlags);
            Assert.Equal(new[] { "old" }, again.Disconnect);
            Assert.Empty(again.NewFlags);
        }

        [Fact]
        public void Another_account_is_disconnected_and_flagged()
        {
            var r = Review(new[] { S("a"), S("b", "rbadminabc123") });
            Assert.Equal(new[] { "b" }, r.Disconnect);
            Assert.Equal("unexpected-user", Assert.Single(r.NewFlags).Code);
        }

        [Fact]
        public void Passing_the_deadline_locks_and_disconnects_without_a_flag()
        {
            var r = Review(new[] { S("a") }, deadline: Now.AddSeconds(-1));
            Assert.True(r.LockNow);
            Assert.Equal(new[] { "a" }, r.Disconnect);
            Assert.Empty(r.NewFlags);
        }

        [Fact]
        public void A_session_after_the_lock_is_disconnected_and_flagged()
        {
            var r = Review(new[] { S("late") }, deadline: Now.AddMinutes(-5), lockedAt: Now.AddMinutes(-4));
            Assert.False(r.LockNow);
            Assert.Equal(new[] { "late" }, r.Disconnect);
            Assert.Equal("session-after-lock", Assert.Single(r.NewFlags).Code);
        }

        [Fact]
        public void No_deadline_never_locks()
        {
            var r = ReviewSessions("candidate", null, null, null, null, new[] { S("a") }, Now);
            Assert.False(r.LockNow);
            Assert.Empty(r.Disconnect);
        }

        [Fact]
        public void Many_distinct_sessions_over_the_slot_are_flagged_once()
        {
            var seen = Enumerable.Range(1, MaxDistinctSessions).Select(i => "s" + i);
            var r = Review(new[] { S("s-new") }, seen: seen);
            Assert.Equal("many-reconnects", Assert.Single(r.NewFlags).Code);
            Assert.Empty(r.Disconnect);

            var again = Review(new[] { S("s-newer") }, seen: r.Seen, flags: r.NewFlags);
            Assert.Empty(again.NewFlags);
        }

        [Fact]
        public void ParseSessions_keeps_only_this_machine()
        {
            var body = JObject.Parse(@"{ ""value"": [
                { ""sessionId"": ""a"", ""userName"": ""candidate"", ""targetResourceId"": """ + Vm.ToUpperInvariant() + @""", ""startTime"": ""2026-10-15T09:55:00Z"" },
                { ""sessionId"": ""b"", ""userName"": ""candidate"", ""targetResourceId"": ""/subscriptions/s/other"" }
            ] }");
            var sessions = ParseSessions(body, Vm);
            var s = Assert.Single(sessions);
            Assert.Equal("a", s.SessionId);
            Assert.Equal(new DateTime(2026, 10, 15, 9, 55, 0, DateTimeKind.Utc), s.StartTimeUtc);
        }

        [Fact]
        public void ParseSessions_tolerates_an_empty_body()
        {
            Assert.Empty(ParseSessions(null, Vm));
            Assert.Empty(ParseSessions(new JObject(), Vm));
        }
    }
}
