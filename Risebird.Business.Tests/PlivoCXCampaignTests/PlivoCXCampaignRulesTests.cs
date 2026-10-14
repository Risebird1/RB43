using System;
using Risebird.Business.PlivoCX;
using Risebird.Entities.Data.PlivoCXModels;

namespace Risebird.Business.Tests.PlivoCXCampaignTests
{
    /// <summary>
    /// The campaign candidate state machine (handoff §11f) and the calling-window rules the
    /// scheduler relies on.
    /// </summary>
    public class PlivoCXCampaignRulesTests
    {
        private static readonly DateTime Now = new DateTime(2026, 10, 7, 6, 30, 0, DateTimeKind.Utc);

        private static PlivoCXCandidateTransition Next(PlivoCXCallLog log, bool collectsSlot = true, bool confirmed = false, int attempts = 1, int total = 6) =>
            PlivoCXCampaignRules.NextState(collectsSlot, log, confirmed, attempts, total, 180, Now, "09 Oct 2026");

        [Fact]
        public void A_booked_slot_makes_the_candidate_Booked_with_the_slot()
        {
            var t = Next(new PlivoCXCallLog
            {
                CallStatus = "completed", CandidateDisposition = "interested",
                InterviewBooked = true, BookingApiSuccess = true,
                BookedInterviewDateTime = new DateTime(2026, 10, 9, 15, 0, 0)
            });

            Assert.Equal(PlivoCXCampaignCandidateState.Booked, t.State);
            Assert.Equal("09 Oct 2026, 3:00 PM", t.BookedSlot);
            Assert.Equal("interested", t.Disposition);
            Assert.Null(t.NextAttemptAt);
        }

        [Fact]
        public void Answered_without_a_booking_is_NotBooked()
        {
            var t = Next(new PlivoCXCallLog { CallStatus = "completed", CandidateDisposition = "uninterested" });

            Assert.Equal(PlivoCXCampaignCandidateState.NotBooked, t.State);
            Assert.Null(t.NextAttemptAt);
        }

        [Fact]
        public void No_answer_with_attempts_left_stays_Pending_and_retries_after_the_gap()
        {
            var t = Next(new PlivoCXCallLog { CallStatus = "no-answer" }, attempts: 2);

            Assert.Equal(PlivoCXCampaignCandidateState.Pending, t.State);
            Assert.Equal(Now.AddMinutes(180), t.NextAttemptAt);
            Assert.Equal("no-answer", t.Disposition);
        }

        [Fact]
        public void No_answer_on_the_last_attempt_is_Cold()
        {
            var t = Next(new PlivoCXCallLog { CallStatus = "no-answer" }, attempts: 6, total: 6);

            Assert.Equal(PlivoCXCampaignCandidateState.Cold, t.State);
            Assert.Null(t.NextAttemptAt);
        }

        [Fact]
        public void A_confirm_only_agent_counts_a_yes_as_success_without_any_booking()
        {
            var t = Next(new PlivoCXCallLog { CallStatus = "completed", CandidateDisposition = "cant_say" }, collectsSlot: false, confirmed: true);

            Assert.Equal(PlivoCXCampaignCandidateState.Booked, t.State);
            Assert.Equal("Confirmed · 09 Oct 2026", t.BookedSlot);
        }

        [Fact]
        public void A_confirm_only_agent_ignores_booking_flags()
        {
            // Confirm-only agents never book, so a stray booking flag must not count.
            var t = Next(new PlivoCXCallLog { CallStatus = "completed", CandidateDisposition = "uninterested", InterviewBooked = true }, collectsSlot: false);

            Assert.Equal(PlivoCXCampaignCandidateState.NotBooked, t.State);
        }

        [Fact]
        public void A_disposition_means_the_call_was_answered_even_without_a_completed_status()
        {
            var t = Next(new PlivoCXCallLog { CallStatus = "triggered", CandidateDisposition = "cant_say" });

            Assert.Equal(PlivoCXCampaignCandidateState.NotBooked, t.State);
        }

        [Theory]
        [InlineData("09:00", "20:00", "08:59", false)]
        [InlineData("09:00", "20:00", "09:00", true)]
        [InlineData("09:00", "20:00", "19:59", true)]
        [InlineData("09:00", "20:00", "20:00", false)]
        [InlineData("20:00", "02:00", "23:30", true)]
        [InlineData("20:00", "02:00", "01:00", true)]
        [InlineData("20:00", "02:00", "12:00", false)]
        [InlineData("00:00", "00:00", "12:00", true)]
        public void The_window_includes_its_open_and_excludes_its_close(string opens, string closes, string at, bool inside)
        {
            Assert.Equal(inside, PlivoCXCampaignRules.IsWithinWindow(TimeSpan.Parse(at), TimeSpan.Parse(opens), TimeSpan.Parse(closes)));
        }

        [Fact]
        public void Time_zones_resolve_from_IANA_or_Windows_ids_and_fall_back_to_IST()
        {
            var ist = new DateTime(2026, 10, 7, 12, 0, 0);
            foreach (var id in new[] { "Asia/Kolkata", "India Standard Time", "Not/AZone", null })
            {
                var tz = PlivoCXCampaignRules.ResolveTimeZone(id);
                Assert.Equal(TimeSpan.FromMinutes(330), tz.GetUtcOffset(ist));
            }
            Assert.True(PlivoCXCampaignRules.IsKnownTimeZone("Asia/Kolkata"));
            Assert.False(PlivoCXCampaignRules.IsKnownTimeZone("Not/AZone"));
        }

        [Fact]
        public void Attempts_per_day_count_from_local_midnight()
        {
            var tz = PlivoCXCampaignRules.ResolveTimeZone("Asia/Kolkata");
            // 06:30 UTC is 12:00 IST on 7 Oct; IST midnight that day is 18:30 UTC on 6 Oct.
            Assert.Equal(new DateTime(2026, 10, 6, 18, 30, 0), PlivoCXCampaignRules.LocalDayStartUtc(Now, tz));
        }

        [Fact]
        public void A_running_campaign_waits_for_its_window_and_start_date()
        {
            var tz = PlivoCXCampaignRules.ResolveTimeZone("Asia/Kolkata");
            var c = new PlivoCXCampaign { WindowOpens = new TimeSpan(9, 0, 0), WindowCloses = new TimeSpan(20, 0, 0) };

            Assert.Null(PlivoCXCampaignRules.WhyNotDialling(c, Now, tz));                    // 12:00 IST
            Assert.NotNull(PlivoCXCampaignRules.WhyNotDialling(c, Now.AddHours(10), tz));    // 22:00 IST
            c.StartsOn = new DateTime(2026, 10, 8);
            Assert.NotNull(PlivoCXCampaignRules.WhyNotDialling(c, Now, tz));
        }

        [Fact]
        public void A_campaign_ends_the_day_after_EndsOn_in_its_own_time_zone()
        {
            var tz = PlivoCXCampaignRules.ResolveTimeZone("Asia/Kolkata");
            var c = new PlivoCXCampaign { EndsOn = new DateTime(2026, 10, 7) };

            Assert.False(PlivoCXCampaignRules.HasEnded(c, Now, tz));
            // 18:30 UTC on 7 Oct is midnight IST on 8 Oct.
            Assert.True(PlivoCXCampaignRules.HasEnded(c, new DateTime(2026, 10, 7, 18, 30, 0, DateTimeKind.Utc), tz));
        }

        [Fact]
        public void Policy_validation_rejects_out_of_range_values()
        {
            var ok = new PlivoCXCampaign();
            Assert.Null(PlivoCXCampaignRules.ValidatePolicy(ok));

            Assert.NotNull(PlivoCXCampaignRules.ValidatePolicy(new PlivoCXCampaign { ConcurrentCalls = 0 }));
            Assert.NotNull(PlivoCXCampaignRules.ValidatePolicy(new PlivoCXCampaign { TimeZone = "Mars/Olympus" }));
            Assert.NotNull(PlivoCXCampaignRules.ValidatePolicy(new PlivoCXCampaign { StartsOn = new DateTime(2026, 10, 9), EndsOn = new DateTime(2026, 10, 8) }));
        }
    }
}
