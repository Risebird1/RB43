using System;
using Risebird.Business.WhatsAppCampaigns;
using Risebird.CommonHelper;
using Risebird.Entities.Data.PlivoCXModels;
using Risebird.Entities.Data.WhatsAppCampaignModels;

namespace Risebird.Business.Tests.WhatsAppCampaignTests
{
    /// <summary>
    /// The WhatsApp campaign candidate state after a send, the sending window and the policy bounds
    /// (hand-off: WhatsApp Campaign pages, send result rules).
    /// </summary>
    public class WhatsAppCampaignRulesTests
    {
        // 12:00 IST.
        private static readonly DateTime Now = new DateTime(2026, 10, 30, 6, 30, 0, DateTimeKind.Utc);
        private static readonly TimeZoneInfo Ist = Risebird.Business.PlivoCX.PlivoCXCampaignRules.ResolveTimeZone("Asia/Kolkata");

        [Fact]
        public void A_sent_message_counts_and_resets_the_failures()
        {
            var t = WhatsAppCampaignRules.AfterSend(WhatsAppHelper.SentMessage, 2, Now);

            Assert.Equal(WhatsAppCampaignCandidateState.Sent, t.State);
            Assert.True(t.Counted);
            Assert.Equal(0, t.FailedSends);
            Assert.Null(t.NextSendAt);
        }

        [Fact]
        public void No_number_ends_as_NoNumber_without_counting()
        {
            var t = WhatsAppCampaignRules.AfterSend(WhatsAppHelper.SkippedNoNumber, 0, Now);

            Assert.Equal(WhatsAppCampaignCandidateState.NoNumber, t.State);
            Assert.False(t.Counted);
        }

        [Theory]
        [InlineData(WhatsAppHelper.SkippedOptedOut, "Opted out of WhatsApp for this role")]
        [InlineData(WhatsAppHelper.SkippedLimitReached, "WhatsApp limit reached (3/3) for this IRF")]
        [InlineData("Skipped: the interview is no longer awaiting confirmation", "The interview is no longer awaiting confirmation")]
        public void Other_skips_end_as_NoResponse_with_the_reason(string result, string note)
        {
            var t = WhatsAppCampaignRules.AfterSend(result, 1, Now);

            Assert.Equal(WhatsAppCampaignCandidateState.NoResponse, t.State);
            Assert.False(t.Counted);
            Assert.Equal(note, t.OutcomeNote);
            Assert.Equal(1, t.FailedSends);
        }

        [Fact]
        public void A_failure_stays_Pending_and_retries_in_an_hour()
        {
            var t = WhatsAppCampaignRules.AfterSend(WhatsAppHelper.FailedMessage, 0, Now);

            Assert.Equal(WhatsAppCampaignCandidateState.Pending, t.State);
            Assert.False(t.Counted);
            Assert.Equal(1, t.FailedSends);
            Assert.Equal(Now.AddHours(1), t.NextSendAt);
        }

        [Fact]
        public void The_third_failure_in_a_row_is_Failed()
        {
            var t = WhatsAppCampaignRules.AfterSend(WhatsAppHelper.FailedMessage, 2, Now);

            Assert.Equal(WhatsAppCampaignCandidateState.Failed, t.State);
            Assert.Equal(3, t.FailedSends);
            Assert.Null(t.NextSendAt);
        }

        [Fact]
        public void Sends_inside_the_window_and_not_outside_it()
        {
            var open = new WhatsAppCampaign { WindowOpens = new TimeSpan(9, 0, 0), WindowCloses = new TimeSpan(20, 0, 0), TimeZone = "Asia/Kolkata" };
            var closed = new WhatsAppCampaign { WindowOpens = new TimeSpan(14, 0, 0), WindowCloses = new TimeSpan(20, 0, 0), TimeZone = "Asia/Kolkata" };

            Assert.Null(WhatsAppCampaignRules.WhyNotSending(open, Now, Ist));
            Assert.Contains("outside the sending window", WhatsAppCampaignRules.WhyNotSending(closed, Now, Ist));
        }

        [Fact]
        public void Does_not_send_before_StartsOn_or_after_EndsOn()
        {
            var early = new WhatsAppCampaign { StartsOn = new DateTime(2026, 10, 31) };
            var over = new WhatsAppCampaign { EndsOn = new DateTime(2026, 10, 29) };

            Assert.Contains("starts on", WhatsAppCampaignRules.WhyNotSending(early, Now, Ist));
            Assert.Contains("ended on", WhatsAppCampaignRules.WhyNotSending(over, Now, Ist));
            Assert.True(WhatsAppCampaignRules.HasEnded(over, Now, Ist));
        }

        [Theory]
        [InlineData(0, 24, 50, "09:00", "20:00", "Max sends")]
        [InlineData(4, 24, 50, "09:00", "20:00", "Max sends")]
        [InlineData(2, 3, 50, "09:00", "20:00", "Re-send after")]
        [InlineData(2, 200, 50, "09:00", "20:00", "Re-send after")]
        [InlineData(2, 24, 0, "09:00", "20:00", "Batch size")]
        [InlineData(2, 24, 201, "09:00", "20:00", "Batch size")]
        [InlineData(2, 24, 50, "20:00", "09:00", "open before it closes")]
        public void Policy_bounds_are_enforced(int maxSends, int resendHours, int batch, string opens, string closes, string error)
        {
            var c = new WhatsAppCampaign
            {
                MaxSends = maxSends, ResendAfterHours = resendHours, BatchSize = batch,
                WindowOpens = TimeSpan.Parse(opens), WindowCloses = TimeSpan.Parse(closes), TimeZone = "Asia/Kolkata"
            };

            Assert.Contains(error, WhatsAppCampaignRules.ValidatePolicy(c));
        }

        [Fact]
        public void The_default_policy_is_valid()
        {
            Assert.Null(WhatsAppCampaignRules.ValidatePolicy(new WhatsAppCampaign()));
        }

        [Fact]
        public void Eligibility_follows_number_opt_out_and_the_limit()
        {
            var ok = new WhatsAppCampaignEligibilityRow { WhatsAppNumber = "+919876543210", SendsUsed = 2 };
            var full = new WhatsAppCampaignEligibilityRow { WhatsAppNumber = "+919876543210", SendsUsed = 3 };
            var optedOut = new WhatsAppCampaignEligibilityRow { WhatsAppNumber = "+919876543210", OptedOut = true };

            Assert.Equal("no-number", WhatsAppCampaignRules.Eligibility(new WhatsAppCampaignEligibilityRow(), WhatsAppCampaignPurpose.Schedule, 3).Eligibility);
            Assert.Equal("eligible", WhatsAppCampaignRules.Eligibility(ok, WhatsAppCampaignPurpose.Schedule, 3).Eligibility);
            Assert.Equal("not-eligible", WhatsAppCampaignRules.Eligibility(full, WhatsAppCampaignPurpose.Schedule, 3).Eligibility);
            Assert.Equal("not-eligible", WhatsAppCampaignRules.Eligibility(optedOut, WhatsAppCampaignPurpose.Schedule, 3).Eligibility);
            // C3 has no send limit.
            Assert.Equal("eligible", WhatsAppCampaignRules.Eligibility(full, WhatsAppCampaignPurpose.Confirm, 0).Eligibility);
        }

        [Theory]
        [InlineData(PlivoCXCampaignSource.IRF, "Schedule Interview")]
        [InlineData(PlivoCXCampaignSource.NotScheduled, "Schedule Interview")]
        [InlineData(PlivoCXCampaignSource.Reschedule, "Reschedule Interview")]
        [InlineData(PlivoCXCampaignSource.Unconfirmed, "Interview Reminder")]
        public void The_source_picks_the_template(string source, string eventName)
        {
            Assert.Equal(eventName, WhatsAppCampaignPurpose.EventFor(WhatsAppCampaignPurpose.ForSource(source)));
        }
    }
}
