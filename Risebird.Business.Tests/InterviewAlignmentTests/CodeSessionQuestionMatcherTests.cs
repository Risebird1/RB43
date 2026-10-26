using Risebird.Entities.ViewModel;
using Xunit;

namespace Risebird.Business.Tests.InterviewAlignmentTests
{
    public class CodeSessionQuestionMatcherTests
    {
        private static InterviewCodeSession Session(int id, string snapshot, string current = null, string submitted = null) => new()
        {
            Id = id,
            QuestionId = 100 + id,
            QuestionText = snapshot,
            CurrentQuestionText = current ?? snapshot,
            SubmittedSource = submitted
        };

        [Fact]
        public void NoSessions_IsDisabled_SoReportsWithoutCodeAreUnchanged()
        {
            var matcher = new CodeSessionQuestionMatcher(new InterviewCodeSession[0], new[] { "Explain goroutines" });

            Assert.False(matcher.Enabled);
            Assert.Null(matcher.Find("Explain goroutines"));
            Assert.Empty(matcher.Unmatched);
            Assert.False(CodeSessionQuestionMatcher.Disabled.Enabled);
        }

        [Fact]
        public void DiscussedQuestion_FindsItsCode_IgnoringCaseAndWhitespace()
        {
            var pool = Session(1, "Implement a worker pool that drains\r\n cleanly on cancellation.", submitted: "package main");
            var matcher = new CodeSessionQuestionMatcher(new[] { pool }, new[] { "  implement a WORKER pool that drains cleanly on cancellation. " });

            Assert.True(matcher.Enabled);
            Assert.Same(pool, matcher.Find("Implement a worker pool that drains cleanly on cancellation."));
            Assert.Empty(matcher.Unmatched);
        }

        [Fact]
        public void QuestionEditedAfterTheSession_StillMatchesOnItsCurrentText()
        {
            var session = Session(1, snapshot: "Design a rate limiter", current: "Design a rate limiter for a shared API gateway");
            var matcher = new CodeSessionQuestionMatcher(new[] { session }, new[] { "Design a rate limiter for a shared API gateway" });

            Assert.Same(session, matcher.Find("Design a rate limiter for a shared API gateway"));
            Assert.Same(session, matcher.Find("Design a rate limiter"));
            Assert.Empty(matcher.Unmatched);
        }

        [Fact]
        public void QuestionWithoutCode_FindsNothing_AndUnmatchedSessionsAreStillListed()
        {
            var asked = Session(1, "Reverse a linked list");
            var notInReport = Session(2, "Find the longest palindrome");
            var matcher = new CodeSessionQuestionMatcher(new[] { asked, notInReport },
                new[] { "Reverse a linked list", "Tell me about your last project" });

            Assert.Null(matcher.Find("Tell me about your last project"));     // shows "No code is submitted yet"
            Assert.Same(asked, matcher.Find("Reverse a linked list"));
            var unmatched = Assert.Single(matcher.Unmatched);                   // never hidden from the client
            Assert.Same(notInReport, unmatched);
        }

        [Fact]
        public void BlankQuestions_NeverMatch()
        {
            var matcher = new CodeSessionQuestionMatcher(new[] { Session(1, "Q") }, new string[] { null, "", "   " });

            Assert.Null(matcher.Find(null));
            Assert.Null(matcher.Find("  "));
            Assert.Single(matcher.Unmatched);
        }
    }
}
