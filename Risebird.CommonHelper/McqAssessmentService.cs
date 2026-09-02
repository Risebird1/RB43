using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Risebird.CommonHelper
{
    /// <summary>
    /// Talks to the MCQ assessment API on screen.risebird.io.
    ///
    /// Auth is the service-account token already shared between the two systems for
    /// AIInterviewStatusWebhookNotifier, so there is one secret to rotate rather than two.
    ///
    /// Every method swallows transport failures and returns null. That is deliberate: this sits
    /// in the path of a candidate's live interview and of a client reading a report, and neither
    /// should break because the MCQ host is slow or restarting. Callers decide what a null means -
    /// the interview skips the section, the report falls back to its cached copy.
    /// </summary>
    public class McqAssessmentService : IMcqAssessmentService
    {
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _logger;
        private readonly HttpClient _http;

        public McqAssessmentService(IConfiguration configuration, IApplicationErrorLogger logger)
        {
            _configuration = configuration;
            _logger = logger;
            _http = new HttpClient();
        }

        private string BaseUrl => _configuration["McqAssessment:BaseUrl"]?.TrimEnd('/');

        private string ServiceAccountToken =>
            _configuration["McqAssessment:ServiceAccountToken"]
            ?? _configuration["AIInterviewWebhook:ServiceAccountToken"];

        public bool IsConfigured => !string.IsNullOrWhiteSpace(BaseUrl);

        public async Task<McqSectionProvisionResult> ProvisionSectionAsync(string sessionId, McqSectionProvisionRequest request)
        {
            if (!IsConfigured || string.IsNullOrWhiteSpace(sessionId) || request == null)
                return null;

            // Question generation for a whole sectioned paper is several model calls, so the
            // first provision of a session is slow by nature. Anything past this and the
            // candidate is better served by skipping the section than by staring at a spinner.
            var body = await SendAsync(
                HttpMethod.Post,
                $"{BaseUrl}/api/v1/ai-sessions/{Uri.EscapeDataString(sessionId)}/mcq",
                request,
                TimeSpan.FromSeconds(180));

            if (body == null) return null;

            try
            {
                return JsonConvert.DeserializeObject<McqSectionProvisionResult>(body);
            }
            catch (Exception ex)
            {
                await LogAsync(ex, $"Could not read the MCQ provision response for session {sessionId}.");
                return null;
            }
        }

        public async Task<McqAssessmentReport> FetchReportAsync(string sessionId)
        {
            if (!IsConfigured || string.IsNullOrWhiteSpace(sessionId))
                return null;

            var body = await SendAsync(
                HttpMethod.Get,
                $"{BaseUrl}/api/v1/ai-sessions/{Uri.EscapeDataString(sessionId)}/mcq/report",
                null,
                TimeSpan.FromSeconds(30));

            if (body == null) return null;

            try
            {
                return JsonConvert.DeserializeObject<McqAssessmentReport>(body);
            }
            catch (Exception ex)
            {
                await LogAsync(ex, $"Could not read the MCQ report response for session {sessionId}.");
                return null;
            }
        }

        /// <summary>
        /// Returns the response body for 2xx, and null for anything else - including 404 (this
        /// session has no MCQ section) and 202 (the candidate has not finished yet), both of
        /// which are ordinary states rather than faults and so are not logged as errors.
        /// </summary>
        private async Task<string> SendAsync(HttpMethod method, string url, object payload, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);

            try
            {
                using var request = new HttpRequestMessage(method, url);

                var token = ServiceAccountToken;
                if (!string.IsNullOrWhiteSpace(token))
                    request.Headers.TryAddWithoutValidation("service-account-token", token);

                if (payload != null)
                {
                    request.Content = new StringContent(
                        JsonConvert.SerializeObject(payload,
                            new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }),
                        Encoding.UTF8,
                        "application/json");
                }

                using var response = await _http.SendAsync(request, cts.Token);
                var body = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    return body;

                // 202 carries a usable in-progress payload, but the caller asked for a finished
                // result; treating it as "nothing yet" keeps that distinction at one place.
                if (response.StatusCode == HttpStatusCode.Accepted || response.StatusCode == HttpStatusCode.NotFound)
                    return null;

                await LogAsync(
                    new Exception($"MCQ assessment API returned {(int)response.StatusCode} for {method} {url}. Body: {Truncate(body, 1000)}"),
                    null);
                return null;
            }
            catch (OperationCanceledException)
            {
                await LogAsync(new Exception($"MCQ assessment API timed out after {timeout.TotalSeconds:0}s for {method} {url}."), null);
                return null;
            }
            catch (Exception ex)
            {
                await LogAsync(ex, $"MCQ assessment API call failed for {method} {url}.");
                return null;
            }
        }

        private async Task LogAsync(Exception ex, string context)
        {
            try
            {
                if (_logger == null) return;
                await _logger.LogMessage(LogLevelInfo.Exception,
                    string.IsNullOrWhiteSpace(context) ? ex : new Exception(context, ex));
            }
            catch
            {
                // Logging must never be the thing that breaks an interview.
            }
        }

        private static string Truncate(string value, int max) =>
            string.IsNullOrEmpty(value) || value.Length <= max ? value : value.Substring(0, max) + "…";
    }
}
