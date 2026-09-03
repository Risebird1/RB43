using Microsoft.Extensions.Configuration;
using RestSharp;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;


namespace Risebird.CommonHelper
{
    public class SynthesiaTextToVideoService : ISynthesiaTextToVideoService /// irrespective of the name, this service also contains the other function (text to speech)

    {
        //private readonly HttpClient _http;
        private readonly IConfiguration _config;

        private readonly string _apiKey;
        private readonly string _templateId;
        private readonly IApplicationErrorLogger _applicationErrorLogger;


        public SynthesiaTextToVideoService(IConfiguration config, IApplicationErrorLogger applicationErrorLogger)
        {
            //_http = new HttpClient();
            _config = config;
            _apiKey = _config["Synthesia:ApiKey"];
            _templateId = _config["Synthesia:TemplateId"];
            _applicationErrorLogger = applicationErrorLogger;
            //_http.BaseAddress = new Uri("https://api.synthesia.io/v2/");
            //_http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }

        public async Task<string> CreateVideoAsync(string scriptText, string templateIdParam)
        {

            try
            {
                // continue to parse response.Content for id...
                var options = new RestClientOptions("https://api.synthesia.io/v2/videos/fromTemplate");
                var client = new RestClient(options);
                var request = new RestRequest("");
                request.AddHeader("accept", "application/json");
                request.AddHeader("Authorization", _apiKey);
                //request.AddJsonBody("{\"test\":false,\"templateData\":{\"scriptDynamic\":\"Hello, How are you doing?\"},\"visibility\":\"private\",\"templateId\":\"8fd3aaab-d7db-46e3-bf43-9969eb4a8e25\"}", false);

                var body = new
                {
                    test = false,
                    visibility = "private",
                    templateId = templateIdParam, //_templateId,//"8fd3aaab-d7db-46e3-bf43-9969eb4a8e25",
                    templateData = new
                    {
                        scriptDynamic = CleanMessage(scriptText)
                    }
                };

                request.AddJsonBody(body);

                var response = await client.PostAsync(request);

                if (string.IsNullOrWhiteSpace(response?.Content))
                {
                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception,
                        new Exception($"Synthesia returned an empty response when creating a video. HTTP {(int?)response?.StatusCode}."));
                    return null;
                }

                using var doc = JsonDocument.Parse(response.Content);

                // A rejected request (bad key, bad template, quota) comes back as a JSON error
                // object with no "id". Reporting the API's own message beats the caller's generic
                // "Synthesia did not return a video id", which says nothing about what to fix.
                if (!doc.RootElement.TryGetProperty("id", out var idElement))
                {
                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception,
                        new Exception($"Synthesia rejected the create-video request. HTTP {(int?)response.StatusCode}. Body: {Truncate(response.Content, 1000)}"));
                    return null;
                }

                return idElement.GetString();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }

        }

        /// <summary>
        /// The video's download URL, or null while Synthesia is still rendering it.
        ///
        /// Synthesia omits "download" entirely until status becomes "complete", so reading that
        /// property unconditionally threw a KeyNotFoundException on every poll of an in-progress
        /// video - which the caller swallowed, logging an exception every few seconds and never
        /// distinguishing "not ready yet" from "this video failed". Null now means "still
        /// rendering, poll again"; an exception means the render actually failed.
        /// </summary>
        public async Task<string> GetVideoUrlAsync(string videoId)
        {
            if (string.IsNullOrWhiteSpace(videoId)) return null;

            var options = new RestClientOptions($"https://api.synthesia.io/v2/videos/{videoId}");
            var client = new RestClient(options);
            var request = new RestRequest("");
            request.AddHeader("accept", "application/json");
            request.AddHeader("Authorization", _apiKey);
            var response = await client.GetAsync(request);

            if (string.IsNullOrWhiteSpace(response?.Content))
                return null;

            using var result = JsonDocument.Parse(response.Content);
            var root = result.RootElement;

            var status = root.TryGetProperty("status", out var statusElement)
                ? statusElement.GetString()
                : null;

            // Synthesia reports a render that will never finish; polling on would just burn the
            // caller's whole window before failing with a misleading "still processing".
            if (string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "rejected", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Synthesia could not render video {videoId} (status: {status}). Body: {Truncate(response.Content, 500)}");
            }

            if (root.TryGetProperty("download", out var downloadElement)
                && downloadElement.ValueKind == JsonValueKind.String)
            {
                var url = downloadElement.GetString();
                if (!string.IsNullOrWhiteSpace(url)) return url;
            }

            // Still rendering - "in_progress", or complete but the URL has not been published yet.
            return null;
        }

        private static string Truncate(string value, int max) =>
            string.IsNullOrEmpty(value) || value.Length <= max ? value : value.Substring(0, max) + "…";

        private string CleanMessage(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";

            return input
                .Replace("#", "")        // Remove Markdown headers
                .Replace("**", "")       // Remove bold formatting if present
                .Trim();                 // Trim whitespace
        }
    }

}
