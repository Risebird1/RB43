using System;

namespace Risebird.Entities.Data.PlivoCXModels
{
    /// <summary>
    /// A single named step captured during Plivo CX webhook processing.
    /// Maps to the PlivoCXWebhookCheckpoints table.
    ///
    /// Checkpoint names used by HangupCallback:
    ///   hangup_received          — raw HTTP request arrived
    ///   hangup_body_read         — raw body successfully read
    ///   hangup_json_parsed       — body parsed as JSON
    ///   hangup_form_parsed       — body parsed as form-urlencoded (JSON failed)
    ///   hangup_parse_failed      — both JSON and form parsing produced no identifiers
    ///   hangup_log_found         — matching PlivoCXCallLog row found in DB
    ///   hangup_log_not_found     — no matching call log (dropped)
    ///   hangup_booking_skipped   — candidate did not confirm or date/time missing
    ///   hangup_booking_started   — booking API call about to be made
    ///   hangup_booking_done      — booking API responded
    ///   hangup_complete          — call log updated, processing finished
    /// </summary>
    public class PlivoCXWebhookCheckpoint
    {
        public int      Id               { get; set; }
        public DateTime Timestamp        { get; set; }

        /// <summary>Named step — use the constants on <see cref="CheckpointNames"/>.</summary>
        public string   CheckpointName   { get; set; }

        public string   Message          { get; set; }
        public string   ConversationId   { get; set; }
        public int?     CallLogId        { get; set; }

        /// <summary>Any extra JSON context (parsed field values, error detail, etc.).</summary>
        public string   AdditionalData   { get; set; }
    }

    public static class CheckpointNames
    {
        public const string HangupReceived       = "hangup_received";
        public const string HangupBodyRead       = "hangup_body_read";
        public const string HangupJsonParsed     = "hangup_json_parsed";
        public const string HangupFormParsed     = "hangup_form_parsed";
        public const string HangupParseFailed    = "hangup_parse_failed";
        public const string HangupLogFound       = "hangup_log_found";
        public const string HangupLogNotFound    = "hangup_log_not_found";
        public const string HangupBookingSkipped = "hangup_booking_skipped";
        public const string HangupBookingStarted = "hangup_booking_started";
        public const string HangupBookingDone    = "hangup_booking_done";
        public const string HangupComplete       = "hangup_complete";
    }
}
