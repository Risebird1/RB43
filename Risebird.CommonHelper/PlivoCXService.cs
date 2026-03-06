using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data.PlivoCXModels;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.CommonHelper
{
    /// <summary>
    /// Triggers outbound AI voice calls via the Plivo CX "API Request" trigger endpoint.
    ///
    /// How Plivo CX works (vs. Plivo Programmable Voice):
    ///   - You build a flow once in the Plivo CX dashboard (cx.plivo.com)
    ///   - The flow contains: API Trigger → Initiate Call node → AI Conversation node (with your instructions) → HTTP Request node (for booking)
    ///   - We POST candidate variables to the trigger URL to start each call
    ///   - Plivo CX handles STT + LLM + TTS internally — no WebSocket bridge needed
    ///   - On call end, Plivo CX POSTs extracted variables to our Hangup URL
    ///
    /// Configuration keys (appsettings.json → "PlivoCX" section):
    ///   TriggerEndpointUrl  — full URL copied from Plivo CX flow trigger settings
    ///   ApiAuthId           — Plivo CX API Auth ID (Settings → Authentication in cx.plivo.com)
    ///   ApiAuthToken        — Plivo CX API Auth Token
    /// </summary>
    public class PlivoCXService : IPlivoCXService
    {
        private readonly HttpClient _httpClient;
        private readonly string     _triggerEndpointUrl;

        public PlivoCXService(IConfiguration configuration)
        {
            var authId    = configuration["PlivoCX:ApiAuthId"]    ?? string.Empty;
            var authToken = configuration["PlivoCX:ApiAuthToken"]  ?? string.Empty;
            _triggerEndpointUrl = configuration["PlivoCX:TriggerEndpointUrl"] ?? string.Empty;

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Basic",
                    Convert.ToBase64String(Encoding.ASCII.GetBytes($"{authId}:{authToken}")));
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        /// <summary>
        /// POSTs candidate context variables to the Plivo CX API trigger endpoint.
        /// The Plivo CX flow then:
        ///   1. Initiates an outbound call to candidate's phone number
        ///   2. When answered, runs the AI Conversation node with injected variables
        ///   3. On end, POSTs extracted variables to our Hangup URL
        /// </summary>
        public async Task<PlivoCXTriggerResponse> TriggerOutboundCallAsync(PlivoCXTriggerRequest request)
        {
            if (string.IsNullOrEmpty(_triggerEndpointUrl))
                throw new InvalidOperationException(
                    "PlivoCX:TriggerEndpointUrl is not configured. " +
                    "Copy the trigger URL from your Plivo CX flow (AI Studio → Select flow → Trigger node → API URL) " +
                    "and set it in appsettings.json.");

            var json    = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_triggerEndpointUrl, content);
            var body     = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return new PlivoCXTriggerResponse
                {
                    Error           = $"HTTP {(int)response.StatusCode}: {body}",
                    Message         = "Plivo CX trigger failed",
                    RawResponseBody = body,
                    HttpStatusCode  = (int)response.StatusCode
                };

            var result = JsonConvert.DeserializeObject<PlivoCXTriggerResponse>(body)
                         ?? new PlivoCXTriggerResponse { Message = body };
            result.RawResponseBody = body;
            result.HttpStatusCode  = (int)response.StatusCode;
            return result;
        }
    }
}
