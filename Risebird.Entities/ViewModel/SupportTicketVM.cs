using Newtonsoft.Json;
using System;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// View model used by the Support Tickets dashboard.
    /// Combines SupportTickets row data with the resolved Client.Name.
    /// </summary>
    public class SupportTicketVM
    {
        public int       Id           { get; set; }
        public string    SenderEmail  { get; set; }
        public string    Subject      { get; set; }
        public string    MailContent  { get; set; }
        public DateTime? ReceivedAt   { get; set; }
        public string    Intent       { get; set; }
        public int?      UserId       { get; set; }
        public int?      ClientId     { get; set; }
        public string    Status       { get; set; }
        public DateTime  CreatedAt    { get; set; }
        public string    ClientName   { get; set; }
    }

    /// <summary>
    /// The JSON payload that Relay.app POSTs to our webhook endpoint.
    /// Property names match the snake_case keys Relay sends.
    /// </summary>
    public class RelayEmailWebhookPayload
    {
        [JsonProperty("sender_email")]
        public string SenderEmail  { get; set; }

        [JsonProperty("subject")]
        public string Subject      { get; set; }

        [JsonProperty("mail_content")]
        public string MailContent  { get; set; }

        [JsonProperty("received_at")]
        public string ReceivedAt   { get; set; }

        [JsonProperty("intent")]
        public string Intent       { get; set; }
    }
}
