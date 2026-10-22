using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data.PlivoWhatsAppModels;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.CommonHelper
{
    /// <summary>
    /// Triggers outbound WhatsApp AI conversations via the Plivo CX trigger endpoint.
    ///
    /// This mirrors PlivoCXService exactly — but for WhatsApp flows instead of voice flows.
    /// In cx.plivo.com you create separate WhatsApp agent flows; each has its own trigger URL.
    /// The conversation is handled entirely by Plivo CX (AI node, variable extraction, etc.).
    /// When the conversation ends, Plivo CX POSTs extracted variables to our ConversationEndCallback.
    ///
    /// Configuration keys (appsettings.json → "PlivoWhatsApp" section):
    ///   ApiAuthId                  — Plivo CX Auth ID  (Settings → Authentication in cx.plivo.com)
    ///   ApiAuthToken               — Plivo CX Auth Token
    ///   ScheduleTriggerEndpointUrl — Full trigger URL for the WhatsApp SCHEDULE agent flow
    ///   RescheduleTriggerEndpointUrl — Full trigger URL for the WhatsApp RESCHEDULE agent flow
    /// </summary>
    public class PlivoWhatsAppService : IPlivoWhatsAppService
    {
        private readonly HttpClient _httpClient;
        private readonly string     _scheduleTriggerUrl;
        private readonly string     _rescheduleTriggerUrl;

        private readonly IHttpClientFactory _httpClientFactory;

        public PlivoWhatsAppService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            var authId    = configuration["PlivoWhatsApp:ApiAuthId"]    ?? string.Empty;
            var authToken = configuration["PlivoWhatsApp:ApiAuthToken"]  ?? string.Empty;
            _scheduleTriggerUrl   = configuration["PlivoWhatsApp:ScheduleTriggerEndpointUrl"]   ?? string.Empty;
            _rescheduleTriggerUrl = configuration["PlivoWhatsApp:RescheduleTriggerEndpointUrl"] ?? string.Empty;

            _httpClient = _httpClientFactory.CreateClient();
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Basic",
                    Convert.ToBase64String(Encoding.ASCII.GetBytes($"{authId}:{authToken}")));
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public Task<PlivoWhatsAppSendResponse> SendScheduleMessageAsync(PlivoWhatsAppTriggerContext context)
            => PostTriggerAsync(_scheduleTriggerUrl, context, "PlivoWhatsApp:ScheduleTriggerEndpointUrl");

        public Task<PlivoWhatsAppSendResponse> SendRescheduleMessageAsync(PlivoWhatsAppTriggerContext context)
            => PostTriggerAsync(_rescheduleTriggerUrl, context, "PlivoWhatsApp:RescheduleTriggerEndpointUrl");

        public async Task<PlivoWhatsAppSendResponse> TriggerByUrlAsync(string triggerUrl, object payload)
        {
            if (string.IsNullOrEmpty(triggerUrl))
                throw new InvalidOperationException(
                    "The WhatsApp assistant URL is not configured for this skill channel. " +
                    "Copy the trigger URL from cx.plivo.com → your WhatsApp Phone Screener flow → " +
                    "API Request trigger node → API URL, and set it on the skill channel's edit page.");

            var json    = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(triggerUrl, content);
            var body     = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return new PlivoWhatsAppSendResponse
                {
                    Error           = $"HTTP {(int)response.StatusCode}: {body}",
                    RawResponseBody = body,
                    HttpStatusCode  = (int)response.StatusCode
                };

            var result = JsonConvert.DeserializeObject<PlivoWhatsAppSendResponse>(body)
                         ?? new PlivoWhatsAppSendResponse { Message = body };
            result.RawResponseBody = body;
            result.HttpStatusCode  = (int)response.StatusCode;
            return result;
        }

        /// <summary>
        /// Not used in the Plivo CX approach — the AI agent handles the full conversation.
        /// Kept for interface compatibility; throws if called.
        /// </summary>
        public Task<PlivoWhatsAppSendResponse> SendSessionMessageAsync(string toPhone, string text)
            => throw new NotSupportedException(
                "Session messages are not needed when using Plivo CX for WhatsApp. " +
                "The AI agent handles the full conversation.");

        private async Task<PlivoWhatsAppSendResponse> PostTriggerAsync(
            string triggerUrl, PlivoWhatsAppTriggerContext context, string configKeyName)
        {
            if (string.IsNullOrEmpty(triggerUrl))
                throw new InvalidOperationException(
                    $"{configKeyName} is not configured. " +
                    "Copy the trigger URL from cx.plivo.com → AI Studio → your WhatsApp flow → Trigger node → API URL " +
                    "and set it in appsettings.json.");

            // Payload mirrors PlivoCXTriggerRequest — same variables the AI node will use
            var payload = new
            {
                candidate_phone           = context.CandidatePhone,  // used by "Send WhatsApp to" in the flow
                candidate_name            = context.CandidateName,
                candidate_email           = context.CandidateEmail,
                irf_id                    = context.IrfId.ToString(),
                job_title                 = context.JobTitle,
                client_name               = context.ClientName,
                log_id                    = context.LogId,
                call_purpose              = context.MessagePurpose,
                original_interview_date   = DateTime.Now.AddDays(-3),
                today_date                = context.TodayDate,
                tomorrow_date             = context.TomorrowDateLabel,
                day_after_tomorrow_date   = context.DayAfterDateLabel
            };

            var json     = JsonConvert.SerializeObject(payload);
            var content  = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(triggerUrl, content);
            var body     = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return new PlivoWhatsAppSendResponse
                {
                    Error           = $"HTTP {(int)response.StatusCode}: {body}",
                    RawResponseBody = body,
                    HttpStatusCode  = (int)response.StatusCode
                };

            var result = JsonConvert.DeserializeObject<PlivoWhatsAppSendResponse>(body)
                         ?? new PlivoWhatsAppSendResponse { Message = body };
            result.RawResponseBody = body;
            result.HttpStatusCode  = (int)response.StatusCode;
            return result;
        }
    }
}
