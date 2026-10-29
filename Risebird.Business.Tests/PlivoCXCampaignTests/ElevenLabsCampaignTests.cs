using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Risebird.Business.PlivoCX;
using Risebird.Entities.Data.ElevenLabsModels;
using Risebird.Entities.Data.PlivoCXModels;

namespace Risebird.Business.Tests.PlivoCXCampaignTests
{
    /// <summary>
    /// PlivoCX campaigns on ElevenLabs: the post-call webhook mapping, the agent definitions'
    /// contract with the dialler and the webhook, and the provider rules.
    /// </summary>
    public class ElevenLabsCampaignTests
    {
        private static JObject Data(string json) => JObject.Parse(json);

        private const string ConfirmedCall = @"{
          'conversation_id': 'conv_1',
          'transcript': [
            { 'role': 'agent', 'message': 'Hello, am I speaking with Rahul?' },
            { 'role': 'user',  'message': 'Yes.' },
            { 'role': 'agent', 'message': 'Would tomorrow work?' },
            { 'role': 'user',  'message': 'Tomorrow at 10 AM is fine.' },
            { 'role': 'agent', 'message': null }
          ],
          'metadata': { 'call_duration_secs': 74, 'termination_reason': 'end_call tool was called.' },
          'analysis': {
            'transcript_summary': 'Rahul booked tomorrow 10 AM.',
            'data_collection_results': {
              'candidate_confirmed':   { 'value': true },
              'interview_date':        { 'value': 'tomorrow' },
              'interview_time':        { 'value': '10:00 AM' },
              'candidate_disposition': { 'value': 'interested' },
              'confirmed_candidate_name': { 'value': null }
            }
          },
          'conversation_initiation_client_data': { 'dynamic_variables': {
            'log_id': '42', 'irf_id': '7', 'candidate_email': 'rahul@example.com', 'call_source': 'plivocx_campaign' } }
        }";

        [Fact]
        public void A_booked_call_maps_to_the_same_payload_a_Plivo_hangup_produces()
        {
            var p = ElevenLabsCampaignWebhook.Map("post_call_transcription", Data(ConfirmedCall));

            Assert.Equal("conv_1", p.ConversationId);
            Assert.Equal("42", p.LogId);
            Assert.Equal("7", p.IrfId);
            Assert.Equal("rahul@example.com", p.CandidateEmail);
            Assert.Equal("completed", p.CallStatus);
            Assert.Equal("74", p.Duration);
            Assert.Equal("true", p.CandidateConfirmed);
            Assert.True(PlivoCXOutcomeParser.IsConfirmed(p.CandidateConfirmed));
            Assert.Equal("tomorrow", p.InterviewDate);
            Assert.Equal("10:00 AM", p.InterviewTime);
            Assert.Equal("interested", p.CandidateDisposition);
            Assert.Null(p.ConfirmedCandidateName);
            Assert.Equal("Rahul booked tomorrow 10 AM.", p.Summary);

            // Transcript is stored as [{ role, message }] - the shape the call detail page renders; empty turns dropped.
            var turns = JArray.Parse(p.Transcript);
            Assert.Equal(4, turns.Count);
            Assert.Equal("user", (string)turns[1]["role"]);
        }

        [Fact]
        public void A_call_where_the_candidate_never_spoke_is_no_answer()
        {
            var p = ElevenLabsCampaignWebhook.Map("post_call_transcription", Data(@"{
              'conversation_id': 'c', 'transcript': [ { 'role': 'agent', 'message': 'Hello?' } ],
              'metadata': { 'call_duration_secs': 20 } }"));
            Assert.Equal("no-answer", p.CallStatus);
        }

        [Fact]
        public void A_call_that_reached_voicemail_is_no_answer_even_with_a_greeting_heard()
        {
            var p = ElevenLabsCampaignWebhook.Map("post_call_transcription", Data(@"{
              'conversation_id': 'c',
              'transcript': [ { 'role': 'user', 'message': 'The person you are calling is not available.' } ],
              'metadata': { 'termination_reason': 'Voicemail detected' } }"));
            Assert.Equal("no-answer", p.CallStatus);
        }

        [Theory]
        [InlineData("busy", "busy")]
        [InlineData("no-answer", "no-answer")]
        [InlineData("unknown", "failed")]
        public void An_initiation_failure_maps_to_an_unanswered_or_failed_call(string reason, string status)
        {
            var p = ElevenLabsCampaignWebhook.Map("call_initiation_failure", Data(@"{
              'conversation_id': 'c', 'failure_reason': '" + reason + @"',
              'conversation_initiation_client_data': { 'dynamic_variables': { 'log_id': '9' } } }"));
            Assert.Equal(status, p.CallStatus);
            Assert.Equal(reason, p.HangupCause);
            Assert.Equal("9", p.LogId);
        }

        [Fact]
        public void Audio_and_unknown_events_are_not_call_outcomes()
        {
            Assert.Null(ElevenLabsCampaignWebhook.Map("post_call_audio", Data("{ 'conversation_id': 'c' }")));
            Assert.Null(ElevenLabsCampaignWebhook.Map("something_else", Data("{ 'conversation_id': 'c' }")));
            Assert.Null(ElevenLabsCampaignWebhook.Map("post_call_transcription", null));
        }

        [Fact]
        public void Only_events_marked_as_campaign_calls_are_handed_to_campaigns()
        {
            Assert.True(ElevenLabsCampaignWebhook.IsCampaignEvent(Data(ConfirmedCall)));
            Assert.False(ElevenLabsCampaignWebhook.IsCampaignEvent(Data(
                "{ 'conversation_initiation_client_data': { 'dynamic_variables': { 'log_id': '42' } } }")));
            Assert.False(ElevenLabsCampaignWebhook.IsCampaignEvent(Data("{ }")));
        }

        // ── Agent definitions ────────────────────────────────────────────────────

        [Fact]
        public void There_is_an_ElevenLabs_agent_for_each_candidate_agent_on_Plivo()
        {
            var defs = ElevenLabsCampaignAgents.Definitions;
            Assert.Equal(new[] { "schedule", "reschedule", "confirm" }, defs.Select(d => d.Purpose).ToArray());
            Assert.True(defs.Single(d => d.Purpose == "schedule").CollectsSlot);
            Assert.True(defs.Single(d => d.Purpose == "reschedule").CollectsSlot);
            Assert.False(defs.Single(d => d.Purpose == "confirm").CollectsSlot);
        }

        [Fact]
        public void Every_prompt_variable_is_one_the_dialler_sends()
        {
            // ElevenLabs rejects a call whose prompt uses a dynamic variable the call does not send.
            // The dialler sends PlivoCXTriggerRequest's JSON fields plus call_source.
            var sent = JObject.FromObject(new PlivoCXTriggerRequest()).Properties().Select(p => p.Name)
                .Append(ElevenLabsCampaignAgents.CallSourceVariable).ToHashSet();
            var placeholders = ElevenLabsCampaignAgents.PlaceholderValues().Keys.ToHashSet();

            foreach (var d in ElevenLabsCampaignAgents.Definitions)
            {
                var used = Regex.Matches(d.Prompt + " " + d.FirstMessage, @"\{\{\s*([a-z_]+)\s*\}\}")
                    .Select(m => m.Groups[1].Value).Distinct();
                foreach (var v in used)
                {
                    Assert.True(sent.Contains(v), $"{d.Purpose} uses {{{{{v}}}}}, which calls do not send");
                    Assert.True(placeholders.Contains(v), $"{d.Purpose} uses {{{{{v}}}}} with no dashboard placeholder");
                }
            }
        }

        [Fact]
        public void The_agents_extract_what_the_webhook_reads_and_can_hang_up()
        {
            var options = new ElevenLabsCampaignAgentOptions();
            foreach (var d in ElevenLabsCampaignAgents.Definitions)
            {
                var request = ElevenLabsCampaignAgents.BuildRequest(d, options);
                var keys = request.PlatformSettings.DataCollection.Keys;

                Assert.Contains("candidate_confirmed", keys);
                Assert.Contains("candidate_disposition", keys);
                Assert.Equal("boolean", request.PlatformSettings.DataCollection["candidate_confirmed"].Type);
                if (d.CollectsSlot)
                {
                    Assert.Contains("interview_date", keys);
                    Assert.Contains("interview_time", keys);
                }

                Assert.Equal("end_call", request.ConversationConfig.Agent.Prompt.BuiltInTools.EndCall?.Name);
                Assert.Contains("end_call", d.Prompt);
                Assert.False(string.IsNullOrWhiteSpace(request.ConversationConfig.Tts.VoiceId));
                Assert.Equal("Asia/Kolkata", request.ConversationConfig.Agent.Prompt.Timezone);
            }
        }

        // ── Providers ─────────────────────────────────────────────────────────────

        [Theory]
        [InlineData(null, "Plivo")]
        [InlineData("", "Plivo")]
        [InlineData("plivo", "Plivo")]
        [InlineData("elevenlabs", "ElevenLabs")]
        [InlineData("VAPI", "Vapi")]
        [InlineData("twilio", null)]
        public void Providers_normalise_to_one_spelling(string raw, string expected)
        {
            Assert.Equal(expected, PlivoCXCampaignProvider.Normalize(raw));
        }

        [Fact]
        public void Plivo_and_ElevenLabs_are_enabled_and_Vapi_is_not_yet()
        {
            Assert.True(PlivoCXCampaignProvider.IsEnabled(PlivoCXCampaignProvider.Plivo));
            Assert.True(PlivoCXCampaignProvider.IsEnabled(PlivoCXCampaignProvider.ElevenLabs));
            Assert.False(PlivoCXCampaignProvider.IsEnabled(PlivoCXCampaignProvider.Vapi));
        }

        [Theory]
        [InlineData("HTTP 429: {\"detail\":\"Too many concurrent requests\"}", true)]
        [InlineData("HTTP 400: invalid agent", false)]
        public void An_ElevenLabs_429_is_a_capacity_rejection(string error, bool expected)
        {
            Assert.Equal(expected, PlivoCXOutcomeParser.IsCapacityRejection(error));
        }
    }
}
