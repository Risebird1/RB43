using System.Linq;
using Newtonsoft.Json.Linq;
using Risebird.Business.PlivoCX;
using Xunit;

namespace Risebird.Business.Tests.CallPreferenceTests
{
    /// <summary>
    /// Call opt-out through the existing AI call pipeline: the ElevenLabs campaign agents ask for and report
    /// `unsubscribe` / `opt_out_reason`, and the post-call mapping hands them to PlivoCXCallOutcomeProcessor.
    /// </summary>
    public class AgentCallOptOutTests
    {
        private static JObject Data(string json) => JObject.Parse(json);

        [Fact]
        public void Unsubscribe_and_reason_are_read_from_data_collection()
        {
            var p = ElevenLabsCampaignWebhook.Map("post_call_transcription", Data(@"{
              'conversation_id': 'conv_9',
              'transcript': [ { 'role': 'user', 'message': 'Please do not call me again' } ],
              'conversation_initiation_client_data': { 'dynamic_variables': { 'log_id': '42', 'irf_id': '7' } },
              'analysis': { 'data_collection_results': {
                  'unsubscribe': { 'value': true },
                  'opt_out_reason': { 'value': 'please do not call me again' } } } }"));

            Assert.True(PlivoCXOutcomeParser.IsConfirmed(p.Unsubscribe));
            Assert.Equal("please do not call me again", p.OptOutReason);
            Assert.Equal("ElevenLabs", p.Provider);
            Assert.Equal("42", p.LogId);
        }

        [Fact]
        public void No_unsubscribe_item_is_not_an_opt_out()
        {
            var p = ElevenLabsCampaignWebhook.Map("post_call_transcription", Data(@"{
              'conversation_id': 'c', 'transcript': [ { 'role': 'user', 'message': 'call me later' } ],
              'analysis': { 'data_collection_results': { 'unsubscribe': { 'value': false } } } }"));

            Assert.False(PlivoCXOutcomeParser.IsConfirmed(p.Unsubscribe));
        }

        [Fact]
        public void Every_campaign_agent_asks_for_and_reports_the_opt_out()
        {
            foreach (var agent in ElevenLabsCampaignAgents.Definitions)
            {
                Assert.True(agent.DataCollection.ContainsKey("unsubscribe"), agent.Name);
                Assert.Equal("boolean", agent.DataCollection["unsubscribe"].Type);
                Assert.True(agent.DataCollection.ContainsKey("opt_out_reason"), agent.Name);
                Assert.Contains("# Opt-out handling", agent.Prompt);
                Assert.Contains("NOT an opt-out", agent.Prompt);
            }
        }
    }
}
