using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Data.OpenAIModels;
using Risebird.Entities.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.CommonHelper
{
    public class OpenAIResponsesHelper : IOpenAIResponsesHelper
    {
        private const int DefaultMaxOutputTokensCeiling = 32768;
        private const int MaxTokenBudgetEscalations = 2;

        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IOpenAIApiLogger _openAIApiLogger;
        private readonly string _apiKey;
        private readonly string _baseUrl;
        private readonly string _responsesModel;
        private readonly int _maxOutputTokensCeiling;

        public OpenAIResponsesHelper(
            IConfiguration configuration,
            IApplicationErrorLogger applicationErrorLogger,
            IOpenAIApiLogger openAIApiLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _openAIApiLogger = openAIApiLogger;
            _apiKey = _configuration["OpenAI:ApiKey"];
            _baseUrl = _configuration["OpenAI:BaseUrl"];
            _responsesModel = _configuration["OpenAI:ResponsesModel"];

            var configuredCeiling = _configuration.GetValue<int?>("OpenAI:ResponsesMaxOutputTokensCeiling");
            _maxOutputTokensCeiling = configuredCeiling.HasValue && configuredCeiling.Value > 0
                ? configuredCeiling.Value
                : DefaultMaxOutputTokensCeiling;
        }

        public Task<RunResult> CreateJsonResponseAsync(
            string input,
            string operationType,
            string promptId,
            bool useStoredPromptModel = false)
        {
            return CreateJsonResponseAsync((object)input, operationType, promptId, null, useStoredPromptModel);
        }

        public async Task<RunResult> CreateJsonResponseAsync(
            object input,
            string operationType,
            string promptId,
            int? maxOutputTokens = null,
            bool useStoredPromptModel = false)
        {
            var (result, _) = await CreateJsonResponseWithIdAsync(input, operationType, promptId, null, maxOutputTokens, useStoredPromptModel);
            return result;
        }

        public async Task<RunResult> CreateJsonResponseAsync(
            string input,
            string operationType,
            string promptId,
            string promptVersion,
            IDictionary<string, string> promptVariables,
            int? maxOutputTokens = null,
            bool useStoredPromptModel = false)
        {
            var (result, _) = await CreateJsonResponseWithIdAsync(
                (object)input, operationType, promptId, null, maxOutputTokens, useStoredPromptModel,
                promptVersion, promptVariables);
            return result;
        }

        public Task<(RunResult Result, string ResponseId)> CreateJsonResponseWithIdAsync(
            string input,
            string operationType,
            string promptId,
            string previousResponseId = null,
            int? maxOutputTokens = null,
            bool useStoredPromptModel = false)
        {
            return CreateJsonResponseWithIdAsync((object)input, operationType, promptId, previousResponseId, maxOutputTokens, useStoredPromptModel);
        }

        public async Task<(RunResult Result, string ResponseId)> CreateJsonResponseWithIdAsync(
            object input,
            string operationType,
            string promptId,
            string previousResponseId = null,
            int? maxOutputTokens = null,
            bool useStoredPromptModel = false,
            string promptVersion = null,
            IDictionary<string, string> promptVariables = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_apiKey))
                {
                    throw new InvalidOperationException("OpenAI:ApiKey is not configured.");
                }

                if (!useStoredPromptModel && string.IsNullOrWhiteSpace(_responsesModel))
                {
                    throw new InvalidOperationException("OpenAI:ResponsesModel is not configured.");
                }

                if (string.IsNullOrWhiteSpace(promptId))
                {
                    throw new InvalidOperationException(
                        $"OpenAI prompt ID is required for operation {operationType}.");
                }

                if (input == null || (input is string s && string.IsNullOrWhiteSpace(s)))
                {
                    throw new InvalidOperationException(
                        $"OpenAI Responses input is required for operation {operationType}.");
                }

                var tokenBudget = maxOutputTokens.HasValue && maxOutputTokens.Value > 0
                    ? Math.Min(maxOutputTokens.Value, _maxOutputTokensCeiling)
                    : (int?)null;
                var escalationsLeft = MaxTokenBudgetEscalations;

                while (true)
                {
                    var attempt = await SendJsonResponseAttemptAsync(
                        promptId, input, operationType, previousResponseId, tokenBudget, useStoredPromptModel,
                        promptVersion, promptVariables);

                    if (attempt.Succeeded)
                        return (ToRunResult(attempt.OutputText), attempt.ResponseId);

                    if (!attempt.TruncatedByTokenLimit || escalationsLeft-- <= 0)
                        return (null, attempt.ResponseId);

                    var nextBudget = NextTokenBudget(tokenBudget ?? attempt.TokensAllowedOrUsed);
                    if (!nextBudget.HasValue)
                    {
                        await _applicationErrorLogger.LogMessage(
                            LogLevelInfo.Exception,
                            new Exception($"OpenAI Responses API truncated output for operation {operationType} at the maximum allowed budget of {_maxOutputTokensCeiling} tokens. Raise OpenAI:ResponsesMaxOutputTokensCeiling, shorten the requested output, or reduce the prompt's reasoning effort."));
                        return (null, attempt.ResponseId);
                    }

                    await _applicationErrorLogger.LogMessage(
                        LogLevelInfo.Info,
                        $"OpenAI Responses API truncated output for operation {operationType}; retrying with max_output_tokens {nextBudget.Value}.");
                    tokenBudget = nextBudget;
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return (null, null);
            }
        }

        /// <summary>
        /// Performs a single Responses API call. A response truncated by the output token cap is reported
        /// back so the caller can retry with a larger budget; its partial text is never returned because
        /// truncated JSON cannot be deserialized.
        /// </summary>
        private async Task<JsonResponseAttempt> SendJsonResponseAttemptAsync(
            string promptId,
            object input,
            string operationType,
            string previousResponseId,
            int? maxOutputTokens,
            bool useStoredPromptModel,
            string promptVersion = null,
            IDictionary<string, string> promptVariables = null)
        {
            object requestBody = BuildJsonResponseRequestBody(promptId, input, previousResponseId, maxOutputTokens,
                useStoredPromptModel, promptVersion, promptVariables);

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(4);
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/responses")
            {
                Headers = { { "Authorization", $"Bearer {_apiKey}" } }
            };

            var jsonContent = JsonConvert.SerializeObject(requestBody);
            request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var response = await client.SendAsync(request);
            sw.Stop();

            var responseContent = await response.Content.ReadAsStringAsync();
            var modelName = ExtractModelFromResponse(responseContent) ?? _responsesModel;
            var responseId = ExtractResponseId(responseContent);

            await _openAIApiLogger.LogApiCallAsync(
                apiEndpoint: $"{_baseUrl}/responses",
                apiMethod: "POST",
                requestPayload: jsonContent,
                responsePayload: responseContent,
                responseTimeMs: (int)sw.ElapsedMilliseconds,
                statusCode: (int)response.StatusCode,
                isSuccess: response.IsSuccessStatusCode,
                errorMessage: response.IsSuccessStatusCode ? null : responseContent,
                model: modelName,
                assistantId: null,
                threadId: previousResponseId,
                runId: responseId,
                fileId: null,
                context: new OpenAIApiLogContext
                {
                    OperationType = operationType
                });

            if (!response.IsSuccessStatusCode)
            {
                await _applicationErrorLogger.LogMessage(
                    LogLevelInfo.Exception,
                    new Exception($"OpenAI Responses API failed ({(int)response.StatusCode}): {responseContent}"));
                return JsonResponseAttempt.Failed(null);
            }

            if (IsIncompleteResponse(responseContent, out var incompleteReason))
            {
                var truncatedByTokenLimit = string.Equals(incompleteReason, "max_output_tokens", StringComparison.OrdinalIgnoreCase);
                if (!truncatedByTokenLimit)
                {
                    await _applicationErrorLogger.LogMessage(
                        LogLevelInfo.Exception,
                        new Exception($"OpenAI Responses API returned incomplete output for operation {operationType}. Reason: {incompleteReason}"));
                    return JsonResponseAttempt.Failed(responseId);
                }

                return new JsonResponseAttempt
                {
                    ResponseId = responseId,
                    TruncatedByTokenLimit = true,
                    TokensAllowedOrUsed = ExtractMaxOutputTokens(responseContent) ?? ExtractOutputTokenUsage(responseContent)
                };
            }

            var outputText = ExtractOutputText(responseContent);
            if (string.IsNullOrWhiteSpace(outputText))
            {
                await _applicationErrorLogger.LogMessage(
                    LogLevelInfo.Exception,
                    new Exception($"OpenAI Responses API returned no output text for operation {operationType}."));
                return JsonResponseAttempt.Failed(responseId);
            }

            return new JsonResponseAttempt
            {
                Succeeded = true,
                OutputText = outputText,
                ResponseId = responseId
            };
        }

        /// <summary>
        /// Doubles the output budget up to the configured ceiling, returning null when no further headroom
        /// exists so the caller stops retrying instead of repeating an identical request.
        /// </summary>
        private int? NextTokenBudget(int? currentBudget)
        {
            var candidate = currentBudget.HasValue && currentBudget.Value > 0
                ? Math.Min(currentBudget.Value * 2, _maxOutputTokensCeiling)
                : _maxOutputTokensCeiling;

            return candidate > (currentBudget ?? 0) ? candidate : (int?)null;
        }

        private sealed class JsonResponseAttempt
        {
            public bool Succeeded { get; set; }
            public string OutputText { get; set; }
            public string ResponseId { get; set; }
            public bool TruncatedByTokenLimit { get; set; }
            public int? TokensAllowedOrUsed { get; set; }

            public static JsonResponseAttempt Failed(string responseId) =>
                new JsonResponseAttempt { ResponseId = responseId };
        }

        public async Task<string> CreateJsonWithInstructionsAsync(
            string instructions,
            string input,
            string operationType,
            int? maxOutputTokens = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_apiKey))
                    throw new InvalidOperationException("OpenAI:ApiKey is not configured.");
                if (string.IsNullOrWhiteSpace(_responsesModel))
                    throw new InvalidOperationException("OpenAI:ResponsesModel is not configured.");
                if (string.IsNullOrWhiteSpace(input))
                    throw new InvalidOperationException($"OpenAI Responses input is required for operation {operationType}.");

                // Inline instructions instead of a stored prompt: the caller owns the whole prompt in code.
                var body = new Dictionary<string, object>
                {
                    ["model"] = _responsesModel,
                    ["instructions"] = instructions,
                    ["input"] = input,
                    ["text"] = new { format = new { type = "json_object" } },
                    ["max_output_tokens"] = Math.Min(maxOutputTokens ?? _maxOutputTokensCeiling, _maxOutputTokensCeiling)
                };

                using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(6) };
                var jsonContent = JsonConvert.SerializeObject(body);
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/responses")
                {
                    Headers = { { "Authorization", $"Bearer {_apiKey}" } },
                    Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
                };

                var sw = System.Diagnostics.Stopwatch.StartNew();
                using var response = await client.SendAsync(request);
                sw.Stop();
                var responseContent = await response.Content.ReadAsStringAsync();

                await _openAIApiLogger.LogApiCallAsync(
                    apiEndpoint: $"{_baseUrl}/responses",
                    apiMethod: "POST",
                    requestPayload: jsonContent,
                    responsePayload: responseContent,
                    responseTimeMs: (int)sw.ElapsedMilliseconds,
                    statusCode: (int)response.StatusCode,
                    isSuccess: response.IsSuccessStatusCode,
                    errorMessage: response.IsSuccessStatusCode ? null : responseContent,
                    model: ExtractModelFromResponse(responseContent) ?? _responsesModel,
                    assistantId: null,
                    threadId: null,
                    runId: ExtractResponseId(responseContent),
                    fileId: null,
                    context: new OpenAIApiLogContext { OperationType = operationType });

                if (!response.IsSuccessStatusCode)
                {
                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception,
                        new Exception($"OpenAI Responses API failed ({(int)response.StatusCode}) for {operationType}: {responseContent}"));
                    return null;
                }
                if (IsIncompleteResponse(responseContent, out var incompleteReason))
                {
                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception,
                        new Exception($"OpenAI Responses API returned incomplete output for {operationType}. Reason: {incompleteReason}"));
                    return null;
                }
                return ExtractOutputText(responseContent);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public Task<RunResult> CreateTextResponseAsync(
            string input,
            string operationType,
            string promptId,
            bool useStoredPromptModel = false)
        {
            return CreateTextResponseAsync((object)input, operationType, promptId, useStoredPromptModel);
        }

        public async Task<RunResult> CreateTextResponseAsync(
            object input,
            string operationType,
            string promptId,
            bool useStoredPromptModel = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_apiKey))
                {
                    throw new InvalidOperationException("OpenAI:ApiKey is not configured.");
                }

                if (!useStoredPromptModel && string.IsNullOrWhiteSpace(_responsesModel))
                {
                    throw new InvalidOperationException("OpenAI:ResponsesModel is not configured.");
                }

                if (string.IsNullOrWhiteSpace(promptId))
                {
                    throw new InvalidOperationException(
                        $"OpenAI prompt ID is required for operation {operationType}.");
                }

                if (input == null || (input is string s && string.IsNullOrWhiteSpace(s)))
                {
                    throw new InvalidOperationException(
                        $"OpenAI Responses input is required for operation {operationType}.");
                }

                var requestBody = BuildBaseRequestBody(promptId, input, useStoredPromptModel);

                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromMinutes(4);
                var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/responses")
                {
                    Headers = { { "Authorization", $"Bearer {_apiKey}" } }
                };

                var jsonContent = JsonConvert.SerializeObject(requestBody);
                request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var sw = System.Diagnostics.Stopwatch.StartNew();
                var response = await client.SendAsync(request);
                sw.Stop();

                var responseContent = await response.Content.ReadAsStringAsync();
                var modelName = ExtractModelFromResponse(responseContent) ?? _responsesModel;
                var responseId = ExtractResponseId(responseContent);

                await _openAIApiLogger.LogApiCallAsync(
                    apiEndpoint: $"{_baseUrl}/responses",
                    apiMethod: "POST",
                    requestPayload: jsonContent,
                    responsePayload: responseContent,
                    responseTimeMs: (int)sw.ElapsedMilliseconds,
                    statusCode: (int)response.StatusCode,
                    isSuccess: response.IsSuccessStatusCode,
                    errorMessage: response.IsSuccessStatusCode ? null : responseContent,
                    model: modelName,
                    assistantId: null,
                    threadId: null,
                    runId: responseId,
                    fileId: null,
                    context: new OpenAIApiLogContext
                    {
                        OperationType = operationType
                    });

                if (!response.IsSuccessStatusCode)
                {
                    await _applicationErrorLogger.LogMessage(
                        LogLevelInfo.Exception,
                        new Exception($"OpenAI Responses API failed ({(int)response.StatusCode}): {responseContent}"));
                    return null;
                }

                var outputText = ExtractOutputText(responseContent);
                if (string.IsNullOrWhiteSpace(outputText))
                {
                    await _applicationErrorLogger.LogMessage(
                        LogLevelInfo.Exception,
                        new Exception($"OpenAI Responses API returned no output text for operation {operationType}."));
                    return null;
                }

                return ToRunResult(outputText);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        private object BuildJsonResponseRequestBody(string promptId, object input, string previousResponseId,
            int? maxOutputTokens, bool useStoredPromptModel, string promptVersion = null,
            IDictionary<string, string> promptVariables = null)
        {
            var body = BuildBaseRequestBody(promptId, input, useStoredPromptModel, promptVersion, promptVariables);
            body["text"] = new { format = new { type = "json_object" } };

            if (!string.IsNullOrWhiteSpace(previousResponseId))
                body["previous_response_id"] = previousResponseId;

            if (maxOutputTokens.HasValue)
                body["max_output_tokens"] = maxOutputTokens.Value;

            return body;
        }

        /// <summary>
        /// Omitting <c>model</c> makes the request run on the model saved with the stored prompt, which is
        /// required when that prompt carries model-specific settings such as reasoning; sending a
        /// conflicting model is rejected with an unsupported_value error.
        /// </summary>
        private Dictionary<string, object> BuildBaseRequestBody(string promptId, object input, bool useStoredPromptModel,
            string promptVersion = null, IDictionary<string, string> promptVariables = null)
        {
            var prompt = new Dictionary<string, object> { ["id"] = promptId };

            if (!string.IsNullOrWhiteSpace(promptVersion))
                prompt["version"] = promptVersion;

            // Stored-prompt variable values are passed as strings, which is what the Responses API accepts.
            if (promptVariables != null && promptVariables.Count > 0)
                prompt["variables"] = promptVariables;

            var body = new Dictionary<string, object>
            {
                ["prompt"] = prompt,
                ["input"] = input
            };

            if (!useStoredPromptModel && !string.IsNullOrWhiteSpace(_responsesModel))
                body["model"] = _responsesModel;

            return body;
        }

        private static bool IsIncompleteResponse(string responseContent, out string reason)
        {
            reason = null;
            try
            {
                var json = JObject.Parse(responseContent);
                if (!string.Equals(json["status"]?.ToString(), "incomplete", StringComparison.OrdinalIgnoreCase))
                    return false;

                reason = json["incomplete_details"]?["reason"]?.ToString() ?? "unknown";
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string ExtractOutputText(string responseContent)
        {
            var json = JObject.Parse(responseContent);
            var outputText = json["output_text"]?.ToString();
            if (!string.IsNullOrWhiteSpace(outputText))
            {
                return outputText;
            }

            var output = json["output"] as JArray;
            if (output == null || output.Count == 0)
                return null;

            var builder = new StringBuilder();
            foreach (var item in output)
            {
                if (!string.Equals(item?["type"]?.ToString(), "message", StringComparison.OrdinalIgnoreCase))
                    continue;

                var contentItems = item["content"] as JArray;
                if (contentItems == null)
                    continue;

                foreach (var content in contentItems)
                {
                    if (!string.Equals(content?["type"]?.ToString(), "output_text", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var text = content["text"]?.ToString();
                    if (!string.IsNullOrEmpty(text))
                        builder.Append(text);
                }
            }

            return builder.Length > 0 ? builder.ToString() : null;
        }

        public static string CleanJsonResponseText(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return raw;

            var cleaned = raw.Trim();
            if (cleaned.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
                cleaned = cleaned.Substring(7).TrimStart();
            else if (cleaned.StartsWith("```"))
                cleaned = cleaned.Substring(3).TrimStart();

            if (cleaned.EndsWith("```"))
                cleaned = cleaned.Substring(0, cleaned.Length - 3).TrimEnd();

            return cleaned.Trim();
        }

        public static bool TryValidateJsonObject(string json, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(json))
            {
                error = "Response is empty.";
                return false;
            }

            try
            {
                JsonConvert.DeserializeObject<JObject>(json);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static int? ExtractMaxOutputTokens(string responseContent)
        {
            try
            {
                return JObject.Parse(responseContent)["max_output_tokens"]?.Value<int?>();
            }
            catch
            {
                return null;
            }
        }

        private static int? ExtractOutputTokenUsage(string responseContent)
        {
            try
            {
                return JObject.Parse(responseContent)["usage"]?["output_tokens"]?.Value<int?>();
            }
            catch
            {
                return null;
            }
        }

        private static string ExtractModelFromResponse(string responseContent)
        {
            try
            {
                return JObject.Parse(responseContent)["model"]?.ToString();
            }
            catch
            {
                return null;
            }
        }

        private static string ExtractResponseId(string responseContent)
        {
            try
            {
                return JObject.Parse(responseContent)["id"]?.ToString();
            }
            catch
            {
                return null;
            }
        }

        private static RunResult ToRunResult(string outputText)
        {
            return new RunResult
            {
                Object = "list",
                Data = new List<Datum>
                {
                    new Datum
                    {
                        Role = "assistant",
                        Content = new List<Content>
                        {
                            new Content
                            {
                                Type = "text",
                                Text = new Text
                                {
                                    Value = outputText
                                }
                            }
                        }
                    }
                }
            };
        }
    }
}
