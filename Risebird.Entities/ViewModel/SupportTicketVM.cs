using Newtonsoft.Json;
using System;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// View model for the Support Tickets dashboard.
    /// Maps 1:1 to SupportTickets table columns + joined ClientName.
    /// TotalCount is populated by COUNT(*) OVER() in usp_GetSupportTickets.
    /// </summary>
    public class SupportTicketVM
    {
        public int      Id                          { get; set; }
        public string   SenderEmail                 { get; set; }
        public string   Subject                     { get; set; }
        public string   MailContent                 { get; set; }
        public DateTime? ReceivedAt                 { get; set; }
        public string   Intent                      { get; set; }
        public string   PrimarySpecialization       { get; set; }
        public string   MailBodyIntent              { get; set; }
        public bool     AttachmentJobDescription    { get; set; }
        public bool     AttachmentPdf               { get; set; }
        public bool     AttachmentResumes           { get; set; }
        public string   DriveFolderLink             { get; set; }
        public string   SummaryMail                 { get; set; }
        public string   SummaryCandidates           { get; set; }
        public string   SummaryInterviewScores      { get; set; }
        public int?     UserId                      { get; set; }
        public int?     ClientId                    { get; set; }
        public string   AssignedTo                  { get; set; }
        public string   Status                      { get; set; }
        public DateTime CreatedAt                   { get; set; }
        public string   ClientName                  { get; set; }
        public string   AssignedTo                  { get; set; }
        public int      TotalCount                  { get; set; }
    }

    /// <summary>
    /// JSON payload that Relay.app POSTs to the webhook endpoint.
    /// All property names match the snake_case keys defined in the agent prompt.
    /// </summary>
    public class RelayEmailWebhookPayload
    {
        [JsonProperty("sender_email")]
        public string SenderEmail                   { get; set; }

        [JsonProperty("subject")]
        public string Subject                       { get; set; }

        [JsonProperty("mail_content")]
        public string MailContent                   { get; set; }

        [JsonProperty("received_at")]
        public string ReceivedAt                    { get; set; }

        [JsonProperty("webhook_intent")]
        public string WebhookIntent                 { get; set; }

        [JsonProperty("primary_specialization")]
        public string PrimarySpecialization         { get; set; }

        [JsonProperty("mail_body_intent")]
        public string MailBodyIntent                { get; set; }

        [JsonProperty("attachment_job_description")]
        public bool AttachmentJobDescription        { get; set; }

        [JsonProperty("attachment_pdf")]
        public bool AttachmentPdf                   { get; set; }

        [JsonProperty("attachment_resumes")]
        public bool AttachmentResumes               { get; set; }

        [JsonProperty("drive_folder_link")]
        public string DriveFolderLink               { get; set; }

        [JsonProperty("summary_mail")]
        public string SummaryMail                   { get; set; }

        [JsonProperty("summary_candidates")]
        public string SummaryCandidates             { get; set; }

        [JsonProperty("summary_interview_scores")]
        public string SummaryInterviewScores        { get; set; }
    }
}
