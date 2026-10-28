using Microsoft.Extensions.Configuration;
using RestSharp;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
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


        private readonly IHttpClientFactory _httpClientFactory;

        public SynthesiaTextToVideoService(IConfiguration config, IApplicationErrorLogger applicationErrorLogger, IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            //_http = _httpClientFactory.CreateClient();
            _config = config;
            _apiKey = _config["Synthesia:ApiKey"];
            _templateId = _config["Synthesia:TemplateId"];
            _applicationErrorLogger = applicationErrorLogger;
            //_http.BaseAddress = new Uri("https://api.synthesia.io/v2/");
            //_http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }

        public async Task<string> CreateVideoAsync(string scriptText, string templateIdParam)
        {
            var result = await CreateVideoWithDetailsAsync(scriptText, templateIdParam);
            return result?.VideoId;
        }

        public async Task<SynthesiaCreateResult> CreateVideoWithDetailsAsync(string scriptText, string templateIdParam)
        {
            var result = new SynthesiaCreateResult();

            try
            {
                var options = new RestClientOptions("https://api.synthesia.io/v2/videos/fromTemplate");
                var client = new RestClient(options);
                var request = new RestRequest("");
                request.Method = Method.Post;
                request.AddHeader("accept", "application/json");
                request.AddHeader("Authorization", _apiKey);

                var script = CleanMessage(scriptText);
                if (string.IsNullOrWhiteSpace(script))
                {
                    result.ErrorMessage = "There is nothing for the avatar to say once code punctuation is removed. Write a plain-language gist for this question.";
                    return result;
                }

                var body = new
                {
                    test = false,
                    visibility = "private",
                    templateId = templateIdParam,
                    templateData = new
                    {
                        scriptDynamic = script
                    }
                };

                request.AddJsonBody(body);

                // ExecuteAsync rather than PostAsync: PostAsync throws on a non-2xx reply, and the
                // exception it raises discards Synthesia's response body — the only place the actual
                // reason for a refusal (too long a script, bad template, quota) is stated.
                var response = await client.ExecuteAsync(request);

                result.HttpStatusCode = (int)response.StatusCode;
                result.RawResponse = response.Content;

                if (!response.IsSuccessful)
                {
                    result.ErrorMessage = DescribeSynthesiaFailure(response);
                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception,
                        "Synthesia create video failed: " + result.ErrorMessage + " | body: " + response.Content);
                    return result;
                }

                if (string.IsNullOrWhiteSpace(response.Content))
                {
                    result.ErrorMessage = "Synthesia accepted the request but returned an empty response.";
                    return result;
                }

                using var doc = JsonDocument.Parse(response.Content);
                if (doc.RootElement.TryGetProperty("id", out var idProperty)
                    && idProperty.ValueKind == JsonValueKind.String)
                {
                    result.VideoId = idProperty.GetString();
                }

                if (string.IsNullOrWhiteSpace(result.VideoId))
                {
                    result.ErrorMessage = "Synthesia's response carried no video id.";
                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception,
                        "Synthesia create video returned no id | body: " + response.Content);
                }

                return result;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        /// <summary>Pulls Synthesia's own wording out of an error body so the caller can repeat it.</summary>
        private string DescribeSynthesiaFailure(RestResponse response)
        {
            var status = (int)response.StatusCode;
            var detail = (string)null;

            if (!string.IsNullOrWhiteSpace(response.Content))
            {
                try
                {
                    using var doc = JsonDocument.Parse(response.Content);
                    if (doc.RootElement.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var name in new[] { "message", "detail", "error", "title" })
                        {
                            if (doc.RootElement.TryGetProperty(name, out var prop)
                                && prop.ValueKind == JsonValueKind.String
                                && !string.IsNullOrWhiteSpace(prop.GetString()))
                            {
                                detail = prop.GetString();
                                break;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // Not JSON — fall back to the raw text below.
                }

                if (string.IsNullOrWhiteSpace(detail))
                {
                    detail = response.Content.Length > 300
                        ? response.Content.Substring(0, 300) + "…"
                        : response.Content;
                }
            }

            if (string.IsNullOrWhiteSpace(detail))
            {
                detail = response.ErrorMessage;
            }

            return string.IsNullOrWhiteSpace(detail)
                ? $"Synthesia replied {status}."
                : $"Synthesia replied {status}: {detail}";
        }

        public async Task<string> GetVideoUrlAsync(string videoId)
        {
            var state = await GetVideoStatusAsync(videoId);
            return state?.DownloadUrl;
        }

        public async Task<SynthesiaVideoState> GetVideoStatusAsync(string videoId)
        {
            var state = new SynthesiaVideoState();
            if (string.IsNullOrWhiteSpace(videoId)) return state;

            try
            {
                var options = new RestClientOptions($"https://api.synthesia.io/v2/videos/{videoId}");
                var client = new RestClient(options);
                var request = new RestRequest("");
                request.AddHeader("accept", "application/json");
                request.AddHeader("Authorization", _apiKey);
                var response = await client.GetAsync(request);

                if (string.IsNullOrWhiteSpace(response?.Content)) return state;

                using var doc = JsonDocument.Parse(response.Content);

                // "download" only appears once the render has finished, and a render in progress is
                // the normal case while polling — so a missing property is read as "not ready yet"
                // rather than thrown, which would otherwise log an exception on every poll.
                if (doc.RootElement.TryGetProperty("status", out var status)
                    && status.ValueKind == JsonValueKind.String)
                {
                    state.Status = status.GetString();
                }

                if (doc.RootElement.TryGetProperty("download", out var download)
                    && download.ValueKind == JsonValueKind.String)
                {
                    state.DownloadUrl = download.GetString();
                }

                return state;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                // Status stays null: the caller cannot tell anything from this attempt and should retry.
                return new SynthesiaVideoState();
            }
        }

        /// <summary>Characters Synthesia refuses outright, reported as INVALID_SCRIPT_CHARACTERS.</summary>
        private static readonly char[] SynthesiaIllegalScriptCharacters = { '<', '>', '{', '}' };

        private string CleanMessage(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";

            var text = input
                .Replace("#", "")        // Remove Markdown headers
                .Replace("**", "")       // Remove bold formatting if present
                .Replace("`", "");       // Backticks around code spans are not spoken

            // A spaced angle bracket is prose ("capacity > 0") and has to survive as words: simply
            // dropping it would have the avatar say "capacity 0", which states the opposite of what
            // the question asks. An unspaced one is a generic ("LruCache<K,V>") and falls through to
            // the character pass below.
            text = Regex.Replace(text, @"\s>=\s", " greater than or equal to ");
            text = Regex.Replace(text, @"\s<=\s", " less than or equal to ");
            text = Regex.Replace(text, @"\s>\s", " greater than ");
            text = Regex.Replace(text, @"\s<\s", " less than ");

            // Synthesia rejects the whole request when the script contains < > { }, which question
            // text hits routinely through generics ("LruCache<K,V>") and code braces. They carry
            // nothing for a spoken script, so they become spaces rather than being deleted —
            // dropping them outright would run words together as "LruCacheK,V".
            foreach (var illegal in SynthesiaIllegalScriptCharacters)
            {
                text = text.Replace(illegal, ' ');
            }

            // Collapse the runs of spaces those substitutions leave behind, keeping line breaks
            // intact so the avatar's pacing is unchanged.
            text = Regex.Replace(text, @"[^\S\r\n]+", " ");

            return text.Trim();
        }
    }

}
