using System;

namespace Risebird.Entities.Data.PlivoWhatsAppModels
{
    /// <summary>
    /// A single named step captured during Plivo CX WhatsApp webhook processing.
    /// Maps to the PlivoWhatsAppCheckpoints table.
    ///
    /// Checkpoint names used by ConversationEndCallback:
    ///   wa_callback_received       — raw HTTP request arrived
    ///   wa_callback_body_read      — raw body successfully read
    ///   wa_callback_json_parsed    — body parsed as nested JSON envelope
    ///   wa_callback_form_parsed    — body parsed as form-urlencoded (JSON failed)
    ///   wa_callback_parse_failed   — both JSON and form parsing produced no identifiers
    ///   wa_log_found               — matching PlivoWhatsAppLog row found in DB
    ///   wa_log_not_found           — no matching log (webhook dropped)
    ///   wa_disposition_extracted   — disposition value captured from payload
    ///   wa_booking_skipped         — candidate did not confirm or date/time missing
    ///   wa_booking_started         — booking API call about to be made
    ///   wa_booking_done            — booking API responded
    ///   wa_booking_exception       — exception thrown during booking
    ///   wa_callback_complete       — log updated, processing finished
    /// </summary>
    public class PlivoWhatsAppCheckpoint
    {
        public int      Id               { get; set; }
        public DateTime Timestamp        { get; set; }

        /// <summary>Named step — use the constants on <see cref="WaCheckpointNames"/>.</summary>
        public string   CheckpointName   { get; set; }

        public string   Message          { get; set; }
        public string   ConversationId   { get; set; }

        /// <summary>FK to PlivoWhatsAppLogs.Id</summary>
        public int?     LogId            { get; set; }

        /// <summary>Any extra JSON context (parsed field values, error detail, etc.).</summary>
        public string   AdditionalData   { get; set; }
    }

    public static class WaCheckpointNames
    {
        public const string CallbackReceived      = "wa_callback_received";
        public const string CallbackBodyRead      = "wa_callback_body_read";
        public const string CallbackJsonParsed    = "wa_callback_json_parsed";
        public const string CallbackFormParsed    = "wa_callback_form_parsed";
        public const string CallbackParseFailed   = "wa_callback_parse_failed";
        public const string LogFound              = "wa_log_found";
        public const string LogNotFound           = "wa_log_not_found";
        public const string DispositionExtracted  = "wa_disposition_extracted";
        public const string BookingSkipped        = "wa_booking_skipped";
        public const string BookingStarted        = "wa_booking_started";
        public const string BookingDone           = "wa_booking_done";
        public const string BookingException      = "wa_booking_exception";
        public const string CallbackComplete      = "wa_callback_complete";
    }
}
