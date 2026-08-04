using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.CommonHelper
{
    /// <summary>
    /// Shared gateway for OpenAI's Responses API (POST /v1/responses) and the companion
    /// Conversations API. This replaces the deprecated Assistants API (threads / runs / messages),
    /// which OpenAI is sunsetting on 26 Aug 2026.
    ///
    /// The legacy Assistants stored their model + instructions + tools server-side. To preserve
    /// existing behaviour without rewriting ~15 prompts, this gateway reads each assistant's
    /// configuration once (GET /v1/assistants/{id}) and caches it, then feeds that model +
    /// instructions + mapped tools into the stateless Responses call. When the config lookup is not
    /// available (e.g. after the Assistants sunset) callers can supply overrides via
    /// OpenAI:ResponsesModel / OpenAI:ResponsesInstructions:{assistantId} without touching code.
    /// </summary>
    public static class OpenAIResponsesGateway
    {
        private static readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(180) };
        private static readonly ConcurrentDictionary<string, AssistantProfile> _profileCache =
            new ConcurrentDictionary<string, AssistantProfile>();

        /// <summary>Fallback model used only when an assistant's configured model cannot be read.</summary>
        public const string DefaultModel = "gpt-4o";

        public sealed class AssistantProfile
        {
            public string Model { get; set; }
            public string Instructions { get; set; }
            /// <summary>Responses-shaped tool definitions mapped from the assistant's tools (may be null).</summary>
            public JArray Tools { get; set; }
        }

        public sealed class ResponsesResult
        {
            public bool Success { get; set; }
            public string Id { get; set; }
            public string Model { get; set; }
            public string OutputText { get; set; }
            public string Raw { get; set; }
        }

        /// <summary>
        /// Resolves (and caches) an assistant's model / instructions / tools. Falls back to a default
        /// profile if the assistant cannot be retrieved so calls still succeed.
        /// </summary>
        public static async Task<AssistantProfile> ResolveAssistantAsync(
            string baseUrl, string apiKey, string assistantId, IOpenAIApiLogger logger = null, string modelOverride = null)
        {
            if (string.IsNullOrWhiteSpace(assistantId))
            {
                return new AssistantProfile { Model = modelOverride ?? DefaultModel };
            }

            if (_profileCache.TryGetValue(assistantId, out var cached))
            {
                return cached;
            }

            var profile = new AssistantProfile { Model = modelOverride ?? DefaultModel };
            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/assistants/{assistantId}"))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                    // Reading the assistant's stored config still requires the Assistants beta header
                    // until the Assistants API is fully sunset; results are cached so this is a rare call.
                    request.Headers.Add("OpenAI-Beta", "assistants=v2");

                    var response = await _http.SendAsync(request);
                    var content = await response.Content.ReadAsStringAsync();
                    if (response.IsSuccessStatusCode)
                    {
                        var json = JObject.Parse(content);
                        profile.Model = modelOverride ?? json["model"]?.ToString() ?? DefaultModel;
                        profile.Instructions = json["instructions"]?.ToString();
                        profile.Tools = MapAssistantTools(json);
                    }
                }
            }
            catch (Exception ex)
            {
                if (logger != null)
                {
                    try { await logger.LogApiCallAsync($"{baseUrl}/assistants/{assistantId}", "GET", null, ex.Message, 0, 0, false, ex.Message, context: new OpenAIApiLogContext { OperationType = "AssistantConfigResolve" }); }
                    catch { }
                }
            }

            _profileCache[assistantId] = profile;
            return profile;
        }

        /// <summary>
        /// Translates Assistants-API tool definitions into Responses-API tool definitions (best effort).
        /// Hosted tools (file_search / code_interpreter / web_search) and function tools are mapped;
        /// anything unrecognised is skipped so the request stays valid.
        /// </summary>
        private static JArray MapAssistantTools(JObject assistantJson)
        {
            var toolsIn = assistantJson["tools"] as JArray;
            if (toolsIn == null || toolsIn.Count == 0)
            {
                return null;
            }

            var mapped = new JArray();
            foreach (var tool in toolsIn)
            {
                var type = tool["type"]?.ToString();
                if (string.IsNullOrEmpty(type)) continue;

                if (type == "code_interpreter")
                {
                    mapped.Add(new JObject
                    {
                        ["type"] = "code_interpreter",
                        ["container"] = new JObject { ["type"] = "auto" }
                    });
                }
                else if (type == "file_search")
                {
                    var vectorStoreIds = assistantJson["tool_resources"]?["file_search"]?["vector_store_ids"] as JArray;
                    if (vectorStoreIds != null && vectorStoreIds.Count > 0)
                    {
                        mapped.Add(new JObject
                        {
                            ["type"] = "file_search",
                            ["vector_store_ids"] = vectorStoreIds
                        });
                    }
                }
                else if (type.StartsWith("web_search"))
                {
                    mapped.Add(new JObject { ["type"] = "web_search" });
                }
                else if (type == "function")
                {
                    // Chat/Assistants use an externally-tagged function ({ "function": { ... } }).
                    // Responses uses an internally-tagged function (name/parameters at the top level).
                    var fn = tool["function"] as JObject;
                    if (fn != null)
                    {
                        var responsesFn = new JObject { ["type"] = "function" };
                        if (fn["name"] != null) responsesFn["name"] = fn["name"];
                        if (fn["description"] != null) responsesFn["description"] = fn["description"];
                        if (fn["parameters"] != null) responsesFn["parameters"] = fn["parameters"];
                        if (fn["strict"] != null) responsesFn["strict"] = fn["strict"];
                        mapped.Add(responsesFn);
                    }
                }
            }

            return mapped.Count > 0 ? mapped : null;
        }

        /// <summary>
        /// Creates a response via POST /v1/responses. <paramref name="input"/> may be a plain string or
        /// an array of Responses input items (for multimodal / file inputs). Logs the call via the
        /// supplied logger (if any) and returns the concatenated assistant output text.
        /// </summary>
        public static async Task<ResponsesResult> CreateResponseAsync(
            string baseUrl,
            string apiKey,
            string model,
            string instructions,
            object input,
            bool jsonObjectFormat = false,
            JArray tools = null,
            string conversationId = null,
            string previousResponseId = null,
            IOpenAIApiLogger logger = null,
            OpenAIApiLogContext context = null,
            string assistantId = null)
        {
            var body = new Dictionary<string, object>
            {
                ["model"] = string.IsNullOrWhiteSpace(model) ? DefaultModel : model,
                ["store"] = true
            };

            if (!string.IsNullOrWhiteSpace(instructions)) body["instructions"] = instructions;
            if (input != null) body["input"] = input;
            if (jsonObjectFormat) body["text"] = new { format = new { type = "json_object" } };
            if (tools != null && tools.Count > 0) body["tools"] = tools;
            if (!string.IsNullOrWhiteSpace(conversationId)) body["conversation"] = conversationId;
            if (!string.IsNullOrWhiteSpace(previousResponseId)) body["previous_response_id"] = previousResponseId;

            var jsonContent = JsonConvert.SerializeObject(body);
            var result = new ResponsesResult();

            var sw = System.Diagnostics.Stopwatch.StartNew();
            HttpResponseMessage response = null;
            string content = null;
            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/responses"))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                    request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    response = await _http.SendAsync(request);
                    content = await response.Content.ReadAsStringAsync();
                }
            }
            finally
            {
                sw.Stop();
            }

            result.Raw = content;
            result.Success = response != null && response.IsSuccessStatusCode;

            string modelFromResponse = model;
            if (!string.IsNullOrEmpty(content))
            {
                try
                {
                    var json = JObject.Parse(content);
                    result.Id = json["id"]?.ToString();
                    modelFromResponse = json["model"]?.ToString() ?? model;
                    result.Model = modelFromResponse;
                    result.OutputText = ExtractOutputText(json);
                }
                catch { /* leave defaults */ }
            }

            if (logger != null)
            {
                try
                {
                    await logger.LogApiCallAsync(
                        apiEndpoint: $"{baseUrl}/responses",
                        apiMethod: "POST",
                        requestPayload: jsonContent,
                        responsePayload: content,
                        responseTimeMs: (int)sw.ElapsedMilliseconds,
                        statusCode: response != null ? (int)response.StatusCode : 0,
                        isSuccess: result.Success,
                        errorMessage: result.Success ? null : content,
                        model: modelFromResponse,
                        assistantId: assistantId,
                        threadId: conversationId,
                        runId: result.Id,
                        fileId: null,
                        context: context);
                }
                catch { }
            }

            return result;
        }

        /// <summary>
        /// Extracts the assistant text from a Responses payload by concatenating every
        /// output_text segment across all message output items.
        /// </summary>
        public static string ExtractOutputText(JObject responseJson)
        {
            if (responseJson == null) return null;

            // Convenience field returned by some responses.
            var direct = responseJson["output_text"]?.ToString();
            if (!string.IsNullOrEmpty(direct)) return direct;

            var output = responseJson["output"] as JArray;
            if (output == null) return null;

            var sb = new StringBuilder();
            foreach (var item in output)
            {
                if (item["type"]?.ToString() != "message") continue;
                var contentArr = item["content"] as JArray;
                if (contentArr == null) continue;
                foreach (var part in contentArr)
                {
                    if (part["type"]?.ToString() == "output_text")
                    {
                        sb.Append(part["text"]?.ToString());
                    }
                }
            }

            return sb.Length > 0 ? sb.ToString() : null;
        }

        /// <summary>Creates a persistent conversation and returns its id (conv_...).</summary>
        public static async Task<string> CreateConversationAsync(
            string baseUrl, string apiKey, IOpenAIApiLogger logger = null, OpenAIApiLogContext context = null)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            HttpResponseMessage response = null;
            string content = null;
            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/conversations"))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                    request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
                    response = await _http.SendAsync(request);
                    content = await response.Content.ReadAsStringAsync();
                }
            }
            finally
            {
                sw.Stop();
            }

            if (logger != null)
            {
                try
                {
                    await logger.LogApiCallAsync($"{baseUrl}/conversations", "POST", "{}", content,
                        (int)sw.ElapsedMilliseconds, response != null ? (int)response.StatusCode : 0,
                        response != null && response.IsSuccessStatusCode,
                        (response != null && response.IsSuccessStatusCode) ? null : content,
                        context: context);
                }
                catch { }
            }

            if (response == null || !response.IsSuccessStatusCode) return null;
            try { return JObject.Parse(content)["id"]?.ToString(); }
            catch { return null; }
        }

        /// <summary>Lists conversation items as (role, text) pairs in chronological order.</summary>
        public static async Task<List<(string Role, string Text)>> ListConversationItemsAsync(
            string baseUrl, string apiKey, string conversationId, IOpenAIApiLogger logger = null, OpenAIApiLogContext context = null)
        {
            var result = new List<(string, string)>();
            if (string.IsNullOrWhiteSpace(conversationId)) return result;

            var sw = System.Diagnostics.Stopwatch.StartNew();
            HttpResponseMessage response = null;
            string content = null;
            try
            {
                // order=asc so the transcript reads oldest -> newest.
                using (var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/conversations/{conversationId}/items?order=asc&limit=100"))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                    response = await _http.SendAsync(request);
                    content = await response.Content.ReadAsStringAsync();
                }
            }
            finally
            {
                sw.Stop();
            }

            if (logger != null)
            {
                try
                {
                    await logger.LogApiCallAsync($"{baseUrl}/conversations/{conversationId}/items", "GET", null, content,
                        (int)sw.ElapsedMilliseconds, response != null ? (int)response.StatusCode : 0,
                        response != null && response.IsSuccessStatusCode,
                        (response != null && response.IsSuccessStatusCode) ? null : content,
                        threadId: conversationId, context: context);
                }
                catch { }
            }

            if (response == null || !response.IsSuccessStatusCode) return result;

            try
            {
                var data = JObject.Parse(content)["data"] as JArray;
                if (data == null) return result;
                foreach (var item in data)
                {
                    if (item["type"]?.ToString() != "message") continue;
                    var role = item["role"]?.ToString();
                    var contentArr = item["content"] as JArray;
                    if (contentArr == null) continue;

                    var sb = new StringBuilder();
                    foreach (var part in contentArr)
                    {
                        var partType = part["type"]?.ToString();
                        // user turns arrive as input_text, assistant turns as output_text.
                        if (partType == "output_text" || partType == "input_text" || partType == "text")
                        {
                            sb.Append(part["text"]?.ToString());
                        }
                    }
                    result.Add((role, sb.ToString()));
                }
            }
            catch { }

            return result;
        }

        /// <summary>
        /// Downloads a file from <paramref name="url"/> and uploads it to OpenAI with purpose "user_data"
        /// so it can be referenced as an input_file in a Responses call. Returns the file id, or null.
        /// </summary>
        public static async Task<string> UploadFileForInputAsync(
            string baseUrl, string apiKey, string url, IOpenAIApiLogger logger = null, OpenAIApiLogContext context = null)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;

            try
            {
                byte[] data;
                using (var downloader = new HttpClient())
                {
                    data = await downloader.GetByteArrayAsync(url);
                }

                string fileName;
                try { fileName = url.Split('/').Last().Split('?')[0]; }
                catch { fileName = url.Split('/').Last(); }

                if (string.IsNullOrEmpty(fileName) || fileName.Split('.').Last().Equals("doc", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                using (var form = new MultipartFormDataContent())
                {
                    form.Add(new StringContent("user_data"), "purpose");
                    form.Add(new ByteArrayContent(data), "file", fileName);

                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    HttpResponseMessage response;
                    string responseContent;
                    using (var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/files") { Content = form })
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                        response = await _http.SendAsync(request);
                        responseContent = await response.Content.ReadAsStringAsync();
                    }
                    sw.Stop();

                    string fileId = null;
                    try { fileId = JObject.Parse(responseContent)["id"]?.ToString(); }
                    catch { }

                    if (logger != null)
                    {
                        try
                        {
                            await logger.LogApiCallAsync($"{baseUrl}/files", "POST",
                                $"{{\"purpose\":\"user_data\",\"file\":\"{fileName}\"}}", responseContent,
                                (int)sw.ElapsedMilliseconds, (int)response.StatusCode, response.IsSuccessStatusCode,
                                response.IsSuccessStatusCode ? null : responseContent, fileId: fileId, context: context);
                        }
                        catch { }
                    }

                    return response.IsSuccessStatusCode ? fileId : null;
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Builds a Responses "input" array carrying a user message with text plus an optional file input.
        /// </summary>
        public static object BuildUserInput(string text, string fileId = null)
        {
            if (string.IsNullOrEmpty(fileId))
            {
                return text;
            }

            var contentParts = new List<object>
            {
                new { type = "input_text", text = text ?? string.Empty },
                new { type = "input_file", file_id = fileId }
            };

            return new object[]
            {
                new { role = "user", content = contentParts }
            };
        }
    }
}
