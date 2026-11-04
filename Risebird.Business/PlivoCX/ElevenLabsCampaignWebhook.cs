using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Risebird.Entities.Data.PlivoCXModels;
using System;
using System.Linq;

namespace Risebird.Business.PlivoCX
{
    /// <summary>
    /// Turns an ElevenLabs post-call webhook for a campaign call into the PlivoCXHangupPayload a
    /// Plivo hangup event produces, so both providers go through one outcome pipeline (booking,
    /// disposition, campaign state). Pure - unit tested against sample webhook bodies.
    ///
    /// Events: post_call_transcription (transcript, analysis.transcript_summary,
    /// analysis.data_collection_results.{key}.value, metadata.call_duration_secs) and
    /// call_initiation_failure (data.failure_reason: busy | no-answer | ...). post_call_audio and
    /// anything else are not call outcomes and map to null.
    /// </summary>
    public static class ElevenLabsCampaignWebhook
    {
        /// <summary>True when the event's dynamic variables mark it as a PlivoCX campaign call.</summary>
        public static bool IsCampaignEvent(JObject data) =>
            string.Equals(DynamicVariable(data, ElevenLabsCampaignAgents.CallSourceVariable),
                          ElevenLabsCampaignAgents.CallSourceValue, StringComparison.OrdinalIgnoreCase);

        /// <summary>The outcome payload, or null for events that are not a call outcome.</summary>
        public static PlivoCXHangupPayload Map(string type, JObject data)
        {
            if (data == null) return null;
            type = (type ?? string.Empty).Trim().ToLowerInvariant();

            var payload = new PlivoCXHangupPayload
            {
                ConversationId = (string)data["conversation_id"],
                LogId          = DynamicVariable(data, "log_id"),
                IrfId          = DynamicVariable(data, "irf_id"),
                CandidateEmail = DynamicVariable(data, "candidate_email"),
                CandidateName  = DynamicVariable(data, "candidate_name"),
                PhoneNumber    = DynamicVariable(data, "phone_number"),
                JobTitle       = DynamicVariable(data, "job_title"),
                ClientName     = DynamicVariable(data, "client_name"),
                Provider       = "ElevenLabs"
            };

            if (type == "call_initiation_failure")
            {
                var reason = (string)data["failure_reason"] ?? "unknown";
                payload.HangupCause = reason;
                payload.CallStatus = reason.Trim().ToLowerInvariant() switch
                {
                    "busy"      => "busy",
                    "no-answer" => "no-answer",
                    "no_answer" => "no-answer",
                    _           => "failed"
                };
                payload.Summary = "Call did not connect: " + reason;
                return payload;
            }

            if (type != "post_call_transcription") return null;

            var transcript = data["transcript"] as JArray;
            var termination = (string)data.SelectToken("metadata.termination_reason");
            bool candidateSpoke = transcript != null && transcript.OfType<JObject>().Any(t =>
                string.Equals((string)t["role"], "user", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace((string)t["message"]));
            bool voicemail = termination != null && termination.IndexOf("voicemail", StringComparison.OrdinalIgnoreCase) >= 0;

            payload.CallStatus  = candidateSpoke && !voicemail ? "completed" : "no-answer";
            payload.HangupCause = termination;
            payload.Duration    = data.SelectToken("metadata.call_duration_secs")?.ToString();
            payload.Summary     = (string)data.SelectToken("analysis.transcript_summary");
            payload.Transcript  = transcript == null ? null : JsonConvert.SerializeObject(
                transcript.OfType<JObject>()
                    .Where(t => !string.IsNullOrWhiteSpace((string)t["message"]))
                    .Select(t => new { role = (string)t["role"], message = (string)t["message"] }));

            payload.InterviewDate          = Collected(data, "interview_date");
            payload.InterviewTime          = Collected(data, "interview_time");
            payload.CandidateConfirmed     = Collected(data, "candidate_confirmed");
            payload.CandidateDisposition   = Collected(data, "candidate_disposition");
            payload.ConfirmedCandidateName = Collected(data, "confirmed_candidate_name");
            payload.Unsubscribe            = Collected(data, "unsubscribe");
            payload.OptOutReason           = Collected(data, "opt_out_reason");
            return payload;
        }

        /// <summary>analysis.data_collection_results.{key}.value as text; booleans as true / false.</summary>
        public static string Collected(JObject data, string key)
        {
            var value = data?.SelectToken("analysis.data_collection_results")?[key]?["value"];
            if (value == null || value.Type == JTokenType.Null) return null;
            if (value.Type == JTokenType.Boolean) return value.Value<bool>() ? "true" : "false";
            var text = value.ToString().Trim();
            return text.Length == 0 ? null : text;
        }

        private static string DynamicVariable(JObject data, string key)
        {
            var value = data?.SelectToken("conversation_initiation_client_data.dynamic_variables")?[key];
            if (value == null || value.Type == JTokenType.Null) return null;
            var text = value.ToString().Trim();
            return text.Length == 0 ? null : text;
        }
    }
}
