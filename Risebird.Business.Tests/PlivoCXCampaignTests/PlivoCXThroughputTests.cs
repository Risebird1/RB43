using System.Collections.Generic;
using System.Linq;
using Risebird.Business.PlivoCX;

namespace Risebird.Business.Tests.PlivoCXCampaignTests
{
    /// <summary>
    /// How the dispatcher shares the account's campaign lines, the throughput the policy page
    /// promises, and which hangups are Plivo capacity rejections rather than "no answer".
    /// </summary>
    public class PlivoCXThroughputTests
    {
        private static Dictionary<int, int> Plan(int globalFree, params (int Id, int Concurrent, int InFlight)[] campaigns) =>
            PlivoCXCampaignRules.PlanSlots(campaigns.Select(c => (c.Id, c.Concurrent, c.InFlight)).ToList(), globalFree);

        [Fact]
        public void One_campaign_gets_its_free_lines_up_to_the_account_budget()
        {
            Assert.Equal(15, Plan(40, (1, 20, 5))[1]);   // its own room: 20 - 5
            Assert.Equal(8,  Plan(8,  (1, 20, 5))[1]);   // the account has only 8 free
        }

        [Fact]
        public void Free_lines_are_shared_in_turn_so_no_campaign_starves_another()
        {
            var plan = Plan(10, (1, 20, 0), (2, 20, 0));
            Assert.Equal(5, plan[1]);
            Assert.Equal(5, plan[2]);
        }

        [Fact]
        public void A_campaign_at_its_own_limit_leaves_its_share_to_the_others()
        {
            var plan = Plan(10, (1, 3, 0), (2, 20, 0));
            Assert.Equal(3, plan[1]);
            Assert.Equal(7, plan[2]);
        }

        [Fact]
        public void Nothing_is_planned_when_the_account_or_the_campaigns_are_full()
        {
            Assert.Empty(Plan(0, (1, 20, 0)));
            Assert.Empty(Plan(10, (1, 5, 5)));
            Assert.Empty(Plan(10));
        }

        [Fact]
        public void Two_hundred_calls_in_half_an_hour_need_about_eleven_lines_and_twenty_gives_headroom()
        {
            Assert.Equal(11, PlivoCXCampaignRules.ConcurrentCallsFor(200, 30));
            Assert.True(PlivoCXCampaignRules.EstimateCallsPer30Minutes(11) >= 200);
            Assert.True(PlivoCXCampaignRules.EstimateCallsPer30Minutes(20) >= 300);
            Assert.True(PlivoCXCampaignRules.EstimateCallsPer30Minutes(5) < 100);   // the old default
        }

        [Theory]
        [InlineData("5030", true)]
        [InlineData("Concurrency Limit Breached", true)]
        [InlineData("hangup cause 5190", true)]
        [InlineData("cps_limit_reached", true)]
        [InlineData("5180", true)]
        [InlineData("NO_ANSWER", false)]
        [InlineData("USER_BUSY", false)]
        [InlineData("NORMAL_CLEARING", false)]
        [InlineData(null, false)]
        public void Only_Plivo_capacity_hangups_count_as_capacity_rejections(string cause, bool expected)
        {
            Assert.Equal(expected, PlivoCXOutcomeParser.IsCapacityRejection(cause));
        }
    }
}
