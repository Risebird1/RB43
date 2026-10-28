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

        // Off by default. A strict schema constrains every AI interview report on the platform, not
        // just screening ones, so it is switched on deliberately after one verified interview rather
        // than arriving with a deployment.
        private readonly bool _useStrictJsonSchema;

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly OpenAICostOptimization _costOptimization;

        public OpenAIResponsesHelper(
            IConfiguration configuration,
            IApplicationErrorLogger applicationErrorLogger,
            IOpenAIApiLogger openAIApiLogger, IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _openAIApiLogger = openAIApiLogger;
            _apiKey = _configuration["OpenAI:ApiKey"];
            _baseUrl = _configuration["OpenAI:BaseUrl"];
            _responsesModel = _configuration["OpenAI:ResponsesModel"];
            _costOptimization = new OpenAICostOptimization(_configuration);

            _useStrictJsonSchema = _configuration.GetValue("OpenAI:UseStrictJsonSchema", false);

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
            bool useStoredPromptModel = false,
            object responseSchema = null,
            string schemaName = null)
        {
            return CreateJsonResponseWithIdAsync((object)input, operationType, promptId, previousResponseId,
                maxOutputTokens, useStoredPromptModel, null, null, responseSchema, schemaName);
        }

        public async Task<(RunResult Result, string ResponseId)> CreateJsonResponseWithIdAsync(
            object input,
            string operationType,
            string promptId,
            string previousResponseId = null,
            int? maxOutputTokens = null,
            bool useStoredPromptModel = false,
            string promptVersion = null,
            IDictionary<string, string> promptVariables = null,
            object responseSchema = null,
            string schemaName = null)
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

                var plan = PlanFor(operationType, promptId, useStoredPromptModel);

                // A chained call continues a conversation held by OpenAI, so its answer depends on state
                // the request body does not show; it is never served from or stored in the cache.
                var cacheTtl = string.IsNullOrWhiteSpace(previousResponseId)
                    ? _costOptimization.CacheTtlMinutesFor(operationType)
                    : null;
                string cacheKey = null;
                if (cacheTtl.HasValue)
                {
                    cacheKey = OpenAIResponseCache.Key("json", operationType, JsonConvert.SerializeObject(
                        BuildJsonResponseRequestBody(promptId, input, null, tokenBudget, useStoredPromptModel,
                            promptVersion, promptVariables, responseSchema, schemaName, plan)));
                    var hit = await GetCachedAsync(cacheKey);
                    if (hit != null)
                    {
                        await LogCacheHitAsync(operationType, hit);
                        return (ToRunResult(hit.OutputText), hit.ResponseId);
                    }
                }

                var outcome = await RunWithFallbackAsync(plan, operationType, requireJson: true,
                    p => RunJsonWithBudgetEscalationAsync(p, promptId, input, operationType, previousResponseId,
                        tokenBudget, useStoredPromptModel, promptVersion, promptVariables, responseSchema, schemaName));

                if (outcome.OutputText == null)
                    return (null, outcome.ResponseId);

                await StoreInCacheAsync(cacheKey, cacheTtl, outcome);
                return (ToRunResult(outcome.OutputText), outcome.ResponseId);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return (null, null);
            }
        }

        private async Task<CallOutcome> RunJsonWithBudgetEscalationAsync(
            RequestPlan plan,
            string promptId,
            object input,
            string operationType,
            string previousResponseId,
            int? tokenBudget,
            bool useStoredPromptModel,
            string promptVersion,
            IDictionary<string, string> promptVariables,
            object responseSchema,
            string schemaName)
        {
            var escalationsLeft = MaxTokenBudgetEscalations;

            while (true)
            {
                var attempt = await SendJsonResponseAttemptAsync(
                    plan, promptId, input, operationType, previousResponseId, tokenBudget, useStoredPromptModel,
                    promptVersion, promptVariables, responseSchema, schemaName);

                if (attempt.Succeeded)
                    return new CallOutcome(attempt.OutputText, attempt.ResponseId, attempt.Model, attempt.CostUSD);

                if (!attempt.TruncatedByTokenLimit || escalationsLeft-- <= 0)
                    return CallOutcome.Failed(attempt.ResponseId);

                var nextBudget = NextTokenBudget(tokenBudget ?? attempt.TokensAllowedOrUsed);
                if (!nextBudget.HasValue)
                {
                    await _applicationErrorLogger.LogMessage(
                        LogLevelInfo.Exception,
                        new Exception($"OpenAI Responses API truncated output for operation {operationType} at the maximum allowed budget of {_maxOutputTokensCeiling} tokens. Raise OpenAI:ResponsesMaxOutputTokensCeiling, shorten the requested output, or reduce the prompt's reasoning effort."));
                    return CallOutcome.Failed(attempt.ResponseId);
                }

                await _applicationErrorLogger.LogMessage(
                    LogLevelInfo.Info,
                    $"OpenAI Responses API truncated output for operation {operationType}; retrying with max_output_tokens {nextBudget.Value}.");
                tokenBudget = nextBudget;
            }
        }

        /// <summary>
        /// What this request changes from the default: a cheaper model configured for the operation
        /// and/or a prompt cache key. Calls pinned to a stored prompt's model never get a model
        /// override, because sending a conflicting model is rejected by the API.
        /// </summary>
        private RequestPlan PlanFor(string operationType, string promptId, bool useStoredPromptModel)
        {
            var overrideModel = useStoredPromptModel ? null : _costOptimization.ModelOverrideFor(operationType);
            var modelOverridden = overrideModel != null
                && !string.Equals(overrideModel, _responsesModel, StringComparison.OrdinalIgnoreCase);

            return new RequestPlan
            {
                Model = modelOverridden ? overrideModel : _responsesModel,
                ModelOverridden = modelOverridden,
                PromptCacheKey = _costOptimization.UsePromptCacheKey
                    ? OpenAICostOptimization.PromptCacheKey(operationType, promptId ?? "inline")
                    : null
            };
        }

        /// <summary>
        /// Runs the optimised request and, if it fails, throws, or (for a JSON operation on an overridden
        /// model) returns something that is not a JSON object, repeats it exactly as it would have been
        /// sent with no optimisation configured. A cheaper model that cannot do the job therefore costs
        /// one extra call, never a missing report.
        /// </summary>
        private async Task<CallOutcome> RunWithFallbackAsync(
            RequestPlan plan, string operationType, bool requireJson, Func<RequestPlan, Task<CallOutcome>> run)
        {
            var baseline = RequestPlan.Baseline(_responsesModel);
            if (!plan.IsOptimized)
                return await run(baseline);

            CallOutcome outcome;
            try
            {
                outcome = await run(plan);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                outcome = CallOutcome.Failed(null);
            }

            var usable = outcome.OutputText != null
                && (!requireJson || !plan.ModelOverridden || IsJsonObject(outcome.OutputText));
            if (usable || !_costOptimization.FallbackToDefaultModelOnFailure)
                return outcome;

            await _applicationErrorLogger.LogMessage(
                LogLevelInfo.Info,
                $"OpenAI cost optimisation for operation {operationType} (model {(plan.ModelOverridden ? plan.Model : "default")}, prompt cache key {(plan.PromptCacheKey != null ? "on" : "off")}) did not return a usable answer; repeating the request with default settings.");
            return await run(baseline);
        }

        private static bool IsJsonObject(string outputText) =>
            TryValidateJsonObject(CleanJsonResponseText(outputText), out _);

        private async Task<OpenAIResponseCache.Entry> GetCachedAsync(string cacheKey)
        {
            Exception sharedStoreError = null;
            var hit = await OpenAIResponseCache.GetAsync(cacheKey, _costOptimization.CacheMemoryLimitMegabytes,
                _costOptimization.SharedStorePolicy, ex => sharedStoreError = ex);
            await LogSharedCacheErrorAsync(sharedStoreError);
            return hit;
        }

        private async Task StoreInCacheAsync(string cacheKey, int? cacheTtl, CallOutcome outcome)
        {
            if (cacheKey == null || !cacheTtl.HasValue || outcome.OutputText == null)
                return;

            Exception sharedStoreError = null;
            await OpenAIResponseCache.SetAsync(cacheKey, new OpenAIResponseCache.Entry
            {
                OutputText = outcome.OutputText,
                ResponseId = outcome.ResponseId,
                Model = outcome.Model,
                OriginalCostUSD = outcome.CostUSD
            }, cacheTtl.Value, _costOptimization.CacheMemoryLimitMegabytes,
                _costOptimization.SharedStorePolicy, ex => sharedStoreError = ex);
            await LogSharedCacheErrorAsync(sharedStoreError);
        }

        private Task LogSharedCacheErrorAsync(Exception ex) =>
            ex == null
                ? Task.CompletedTask
                : _applicationErrorLogger.LogMessage(LogLevelInfo.Warning,
                    $"Redis OpenAI response cache skipped for one minute ({ex.GetType().Name}: {ex.Message}); requests go to OpenAI meanwhile.");

        /// <summary>
        /// Records the reuse in OpenAIApiLogs at zero tokens so cache hits and the spend they avoided
        /// are visible per operation, alongside the calls that did reach OpenAI.
        /// </summary>
        private Task<long> LogCacheHitAsync(string operationType, OpenAIResponseCache.Entry hit)
        {
            return _openAIApiLogger.LogApiCallAsync(
                apiEndpoint: $"{_baseUrl}/responses",
                apiMethod: CacheHitApiMethod,
                requestPayload: JsonConvert.SerializeObject(new { cacheHit = true, avoidedCostUSD = hit.OriginalCostUSD, source = hit.Source }),
                responsePayload: null,
                responseTimeMs: 0,
                statusCode: 200,
                isSuccess: true,
                errorMessage: null,
                model: "cache:" + (hit.Model ?? _responsesModel ?? "unknown"),
                assistantId: null,
                threadId: null,
                runId: hit.ResponseId,
                fileId: null,
                context: new OpenAIApiLogContext { OperationType = operationType });
        }

        /// <summary>The ApiMethod value of an OpenAIApiLogs row that was answered from the response cache.</summary>
        public const string CacheHitApiMethod = "CACHE_HIT";

        private sealed class RequestPlan
        {
            public string Model { get; set; }
            public bool ModelOverridden { get; set; }
            public string PromptCacheKey { get; set; }
            public bool IsOptimized => ModelOverridden || PromptCacheKey != null;

            public static RequestPlan Baseline(string defaultModel) => new RequestPlan { Model = defaultModel };
        }

        private sealed class CallOutcome
        {
            public CallOutcome(string outputText, string responseId, string model, decimal costUSD)
            {
                OutputText = outputText;
                ResponseId = responseId;
                Model = model;
                CostUSD = costUSD;
            }

            public string OutputText { get; }
            public string ResponseId { get; }
            public string Model { get; }
            public decimal CostUSD { get; }

            public static CallOutcome Failed(string responseId) => new CallOutcome(null, responseId, null, 0);
        }

        /// <summary>Cost of a successful response, for the cache to report what a later hit avoided.</summary>
        private decimal EstimateCostUSD(string model, string responseContent)
        {
            var (promptTokens, completionTokens, _) = _openAIApiLogger.ExtractTokenUsage(responseContent);
            return _openAIApiLogger.CalculateCost(model, promptTokens, completionTokens);
        }

        /// <summary>
        /// Performs a single Responses API call. A response truncated by the output token cap is reported
        /// back so the caller can retry with a larger budget; its partial text is never returned because
        /// truncated JSON cannot be deserialized.
        /// </summary>
        private async Task<JsonResponseAttempt> SendJsonResponseAttemptAsync(
            RequestPlan plan,
            string promptId,
            object input,
            string operationType,
            string previousResponseId,
            int? maxOutputTokens,
            bool useStoredPromptModel,
            string promptVersion = null,
            IDictionary<string, string> promptVariables = null,
            object responseSchema = null,
            string schemaName = null)
        {
            object requestBody = BuildJsonResponseRequestBody(promptId, input, previousResponseId, maxOutputTokens,
                useStoredPromptModel, promptVersion, promptVariables, responseSchema, schemaName, plan);

            using var client = _httpClientFactory.CreateClient();
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
            var modelName = ExtractModelFromResponse(responseContent) ?? plan.Model ?? _responsesModel;
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
                ResponseId = responseId,
                Model = modelName,
                CostUSD = EstimateCostUSD(modelName, responseContent)
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
            public string Model { get; set; }
            public decimal CostUSD { get; set; }

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
                // json_object output is refused (400) unless the *input* mentions JSON; the instructions do not count.
                if (input.IndexOf("json", StringComparison.OrdinalIgnoreCase) < 0)
                    input += "\n\nRespond with a single JSON object.";

                var plan = PlanFor(operationType, null, useStoredPromptModel: false);
                var cacheTtl = _costOptimization.CacheTtlMinutesFor(operationType);
                string cacheKey = null;
                if (cacheTtl.HasValue)
                {
                    cacheKey = OpenAIResponseCache.Key("instructions", operationType,
                        JsonConvert.SerializeObject(BuildInstructionsRequestBody(plan, instructions, input, maxOutputTokens)));
                    var hit = await GetCachedAsync(cacheKey);
                    if (hit != null)
                    {
                        await LogCacheHitAsync(operationType, hit);
                        return hit.OutputText;
                    }
                }

                var outcome = await RunWithFallbackAsync(plan, operationType, requireJson: true,
                    p => SendInstructionsAttemptAsync(p, instructions, input, operationType, maxOutputTokens));

                await StoreInCacheAsync(cacheKey, cacheTtl, outcome);
                return outcome.OutputText;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        // Inline instructions instead of a stored prompt: the caller owns the whole prompt in code.
        private Dictionary<string, object> BuildInstructionsRequestBody(
            RequestPlan plan, string instructions, string input, int? maxOutputTokens)
        {
            var body = new Dictionary<string, object>
            {
                ["model"] = plan.Model ?? _responsesModel,
                ["instructions"] = instructions,
                ["input"] = input,
                ["text"] = new { format = new { type = "json_object" } },
                ["max_output_tokens"] = Math.Min(maxOutputTokens ?? _maxOutputTokensCeiling, _maxOutputTokensCeiling)
            };

            if (!string.IsNullOrWhiteSpace(plan.PromptCacheKey))
                body["prompt_cache_key"] = plan.PromptCacheKey;

            return body;
        }

        private async Task<CallOutcome> SendInstructionsAttemptAsync(
            RequestPlan plan, string instructions, string input, string operationType, int? maxOutputTokens)
        {
            var body = BuildInstructionsRequestBody(plan, instructions, input, maxOutputTokens);

            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(6);
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
            var modelName = ExtractModelFromResponse(responseContent) ?? plan.Model ?? _responsesModel;
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
                context: new OpenAIApiLogContext { OperationType = operationType });

            if (!response.IsSuccessStatusCode)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception,
                    new Exception($"OpenAI Responses API failed ({(int)response.StatusCode}) for {operationType}: {responseContent}"));
                return CallOutcome.Failed(responseId);
            }
            if (IsIncompleteResponse(responseContent, out var incompleteReason))
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception,
                    new Exception($"OpenAI Responses API returned incomplete output for {operationType}. Reason: {incompleteReason}"));
                return CallOutcome.Failed(responseId);
            }

            var outputText = ExtractOutputText(responseContent);
            return outputText == null
                ? CallOutcome.Failed(responseId)
                : new CallOutcome(outputText, responseId, modelName, EstimateCostUSD(modelName, responseContent));
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

                var plan = PlanFor(operationType, promptId, useStoredPromptModel);
                var cacheTtl = _costOptimization.CacheTtlMinutesFor(operationType);
                string cacheKey = null;
                if (cacheTtl.HasValue)
                {
                    cacheKey = OpenAIResponseCache.Key("text", operationType,
                        JsonConvert.SerializeObject(BuildBaseRequestBody(promptId, input, useStoredPromptModel, plan: plan)));
                    var hit = await GetCachedAsync(cacheKey);
                    if (hit != null)
                    {
                        await LogCacheHitAsync(operationType, hit);
                        return ToRunResult(hit.OutputText);
                    }
                }

                var outcome = await RunWithFallbackAsync(plan, operationType, requireJson: false,
                    p => SendTextAttemptAsync(p, promptId, input, operationType, useStoredPromptModel));

                if (outcome.OutputText == null)
                    return null;

                await StoreInCacheAsync(cacheKey, cacheTtl, outcome);
                return ToRunResult(outcome.OutputText);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        private async Task<CallOutcome> SendTextAttemptAsync(
            RequestPlan plan, string promptId, object input, string operationType, bool useStoredPromptModel)
        {
            var requestBody = BuildBaseRequestBody(promptId, input, useStoredPromptModel, plan: plan);

            using var client = _httpClientFactory.CreateClient();
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
            var modelName = ExtractModelFromResponse(responseContent) ?? plan.Model ?? _responsesModel;
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
                return CallOutcome.Failed(responseId);
            }

            var outputText = ExtractOutputText(responseContent);
            if (string.IsNullOrWhiteSpace(outputText))
            {
                await _applicationErrorLogger.LogMessage(
                    LogLevelInfo.Exception,
                    new Exception($"OpenAI Responses API returned no output text for operation {operationType}."));
                return CallOutcome.Failed(responseId);
            }

            return new CallOutcome(outputText, responseId, modelName, EstimateCostUSD(modelName, responseContent));
        }

        private object BuildJsonResponseRequestBody(string promptId, object input, string previousResponseId,
            int? maxOutputTokens, bool useStoredPromptModel, string promptVersion = null,
            IDictionary<string, string> promptVariables = null, object responseSchema = null,
            string schemaName = null, RequestPlan plan = null)
        {
            var body = BuildBaseRequestBody(promptId, input, useStoredPromptModel, promptVersion, promptVariables, plan);

            // json_object asks only for syntactically valid JSON, so a prompt that renames a field or
            // invents a verdict still returns 200 and the report silently renders empty. A schema
            // makes that shape impossible at generation time rather than unnoticed at read time.
            body["text"] = responseSchema != null && _useStrictJsonSchema
                ? (object)new
                {
                    format = new
                    {
                        type = "json_schema",
                        name = string.IsNullOrWhiteSpace(schemaName) ? "response" : schemaName,
                        strict = true,
                        schema = responseSchema
                    }
                }
                : new { format = new { type = "json_object" } };

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
            string promptVersion = null, IDictionary<string, string> promptVariables = null, RequestPlan plan = null)
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

            var model = plan?.Model ?? _responsesModel;
            if (!useStoredPromptModel && !string.IsNullOrWhiteSpace(model))
                body["model"] = model;

            if (!string.IsNullOrWhiteSpace(plan?.PromptCacheKey))
                body["prompt_cache_key"] = plan.PromptCacheKey;

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
