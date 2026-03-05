using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data.PlivoModels;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.CommonHelper
{
    /// <summary>
    /// Plivo REST API client for outbound voice calls.
    /// Authentication: HTTP Basic Auth with AuthId:AuthToken.
    /// Base URL: https://api.plivo.com/v1/Account/{auth_id}/
    /// </summary>
    public class PlivoService : IPlivoService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly string _authId;
        private readonly string _authToken;
        private readonly string _fromNumber;
        private readonly ICandidateCallTracker _candidateCallTracker;

        public PlivoService(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            ICandidateCallTracker candidateCallTracker)
        {
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient();
            _authId = _configuration["Plivo:AuthId"];
            _authToken = _configuration["Plivo:AuthToken"];
            _fromNumber = _configuration["Plivo:FromNumber"];
            _candidateCallTracker = candidateCallTracker;

            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_authId}:{_authToken}"));
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", credentials);
        }

        /// <inheritdoc/>
        public async Task<PlivoOutboundCallResponse> InitiateCallAsync(
            PlivoOutboundCallRequest request,
            int? candidateId = null)
        {
            if (string.IsNullOrEmpty(request.From))
                request.From = _fromNumber;

            var json = JsonConvert.SerializeObject(request, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var url = $"https://api.plivo.com/v1/Account/{_authId}/Call/";

            var response = await _httpClient.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Plivo call initiation failed ({(int)response.StatusCode}): {responseContent}");

            if (candidateId.HasValue)
                await _candidateCallTracker.IncrementVapiCallAttemptsAsync(candidateId.Value);

            return JsonConvert.DeserializeObject<PlivoOutboundCallResponse>(responseContent);
        }

        /// <inheritdoc/>
        public async Task<PlivoCallDetails> GetCallDetailsAsync(string callUUID)
        {
            var url = $"https://api.plivo.com/v1/Account/{_authId}/Call/{callUUID}/";
            var response = await _httpClient.GetAsync(url);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Plivo get call details failed ({(int)response.StatusCode}): {responseContent}");

            return JsonConvert.DeserializeObject<PlivoCallDetails>(responseContent);
        }

        /// <inheritdoc/>
        public async Task<bool> HangupCallAsync(string callUUID)
        {
            var url = $"https://api.plivo.com/v1/Account/{_authId}/Call/{callUUID}/";
            var response = await _httpClient.DeleteAsync(url);
            return response.IsSuccessStatusCode;
        }

        /// <inheritdoc/>
        public string BuildAnswerXml(string webSocketUrl, string statusCallbackUrl)
        {
            // PlivoXML: bidirectional mulaw stream — the AI speaks first via OpenAI Realtime
            return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Response>
    <Stream
        bidirectional=""true""
        keepCallAlive=""true""
        contentType=""audio/x-mulaw;rate=8000""
        statusCallbackUrl=""{statusCallbackUrl}""
        statusCallbackMethod=""POST"">
        {webSocketUrl}
    </Stream>
</Response>";
        }
    }
}
