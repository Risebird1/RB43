using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Risebird.CommonHelper
{
    public class OpenAIApiLogger : IOpenAIApiLogger
    {
        private readonly IOpenAIApiLogRepository _logRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        // OpenAI Pricing (as of January 2026)
        // GPT-4o models (most common)
        private readonly decimal GPT4O_INPUT_COST_PER_1K = 0.0025m;      // $2.50 per 1M tokens
        private readonly decimal GPT4O_OUTPUT_COST_PER_1K = 0.0100m;     // $10.00 per 1M tokens
        private readonly decimal GPT4O_CACHED_COST_PER_1K = 0.00125m;    // $1.25 per 1M tokens
        
        // GPT-4o-mini models
        private readonly decimal GPT4O_MINI_INPUT_COST_PER_1K = 0.00015m;   // $0.15 per 1M tokens
        private readonly decimal GPT4O_MINI_OUTPUT_COST_PER_1K = 0.00060m;  // $0.60 per 1M tokens
        private readonly decimal GPT4O_MINI_CACHED_COST_PER_1K = 0.000075m; // $0.075 per 1M tokens

        // GPT-4.1 models (OpenAI:ResponsesModel and most stored prompts). Every one of these names
        // contains "gpt-4", so they must be matched before the legacy GPT-4 tier, which billed them
        // at fifteen to seventy-five times their real price.
        private readonly decimal GPT41_INPUT_COST_PER_1K = 0.0020m;          // $2.00 per 1M tokens
        private readonly decimal GPT41_OUTPUT_COST_PER_1K = 0.0080m;         // $8.00 per 1M tokens
        private readonly decimal GPT41_CACHED_COST_PER_1K = 0.0005m;         // $0.50 per 1M tokens
        private readonly decimal GPT41_MINI_INPUT_COST_PER_1K = 0.0004m;     // $0.40 per 1M tokens
        private readonly decimal GPT41_MINI_OUTPUT_COST_PER_1K = 0.0016m;    // $1.60 per 1M tokens
        private readonly decimal GPT41_MINI_CACHED_COST_PER_1K = 0.0001m;    // $0.10 per 1M tokens
        private readonly decimal GPT41_NANO_INPUT_COST_PER_1K = 0.0001m;     // $0.10 per 1M tokens
        private readonly decimal GPT41_NANO_OUTPUT_COST_PER_1K = 0.0004m;    // $0.40 per 1M tokens
        private readonly decimal GPT41_NANO_CACHED_COST_PER_1K = 0.000025m;  // $0.025 per 1M tokens

        // GPT-5 models, priced so a model-routing trial (OpenAI:CostOptimization:ModelOverrides) is
        // measured at its real cost rather than at the gpt-4o fallback. Verify against
        // https://openai.com/api/pricing before relying on them.
        private readonly decimal GPT5_INPUT_COST_PER_1K = 0.00125m;          // $1.25 per 1M tokens
        private readonly decimal GPT5_OUTPUT_COST_PER_1K = 0.0100m;          // $10.00 per 1M tokens
        private readonly decimal GPT5_CACHED_COST_PER_1K = 0.000125m;        // $0.125 per 1M tokens
        private readonly decimal GPT5_MINI_INPUT_COST_PER_1K = 0.00025m;     // $0.25 per 1M tokens
        private readonly decimal GPT5_MINI_OUTPUT_COST_PER_1K = 0.0020m;     // $2.00 per 1M tokens
        private readonly decimal GPT5_MINI_CACHED_COST_PER_1K = 0.000025m;   // $0.025 per 1M tokens
        private readonly decimal GPT5_NANO_INPUT_COST_PER_1K = 0.00005m;     // $0.05 per 1M tokens
        private readonly decimal GPT5_NANO_OUTPUT_COST_PER_1K = 0.0004m;     // $0.40 per 1M tokens
        private readonly decimal GPT5_NANO_CACHED_COST_PER_1K = 0.000005m;   // $0.005 per 1M tokens

        // Legacy GPT-4 models
        private readonly decimal GPT4_INPUT_COST_PER_1K = 0.03m;
        private readonly decimal GPT4_OUTPUT_COST_PER_1K = 0.06m;
        private readonly decimal GPT4_TURBO_INPUT_COST_PER_1K = 0.01m;
        private readonly decimal GPT4_TURBO_OUTPUT_COST_PER_1K = 0.03m;
        
        // GPT-3.5 models
        private readonly decimal GPT35_TURBO_INPUT_COST_PER_1K = 0.0015m;
        private readonly decimal GPT35_TURBO_OUTPUT_COST_PER_1K = 0.002m;
        
        // Audio models
        private readonly decimal WHISPER_COST_PER_MINUTE = 0.006m;
        private readonly decimal TTS_COST_PER_1K_CHARS = 0.015m;
        private readonly decimal TTS_HD_COST_PER_1K_CHARS = 0.030m;

        public OpenAIApiLogger(IOpenAIApiLogRepository logRepository, IApplicationErrorLogger applicationErrorLogger)
        {
            _logRepository = logRepository;
            _applicationErrorLogger = applicationErrorLogger;
        }

        public async Task<long> LogApiCallAsync(
            string apiEndpoint,
            string apiMethod,
            string requestPayload,
            string responsePayload,
            int responseTimeMs,
            int statusCode,
            bool isSuccess,
            string errorMessage = null,
            string model = null,
            string assistantId = null,
            string threadId = null,
            string runId = null,
            string fileId = null,
            OpenAIApiLogContext context = null)
        {
            try
            {
                context = OpenAIApiLogScope.Apply(context);

                var (promptTokens, completionTokens, totalTokens) = ExtractTokenUsage(responsePayload);
                var cachedPromptTokens = ExtractCachedInputTokens(responsePayload);
                var estimatedCost = CalculateCost(model, promptTokens, completionTokens, cachedPromptTokens);

                // For Whisper (speech-to-text), estimate cost differently
                if (apiEndpoint?.Contains("/audio/transcriptions") == true)
                {
                    estimatedCost = EstimateWhisperCost(requestPayload);
                    model = model ?? "whisper-1";
                }

                // For TTS (text-to-speech), estimate cost based on character count
                if (apiEndpoint?.Contains("/audio/speech") == true)
                {
                    estimatedCost = EstimateTTSCost(requestPayload);
                    model = model ?? "tts-1";
                }

                // Realtime (voice) sessions bill audio and text tokens at their own rates; the payload
                // is one whole session summed by OpenAIRealtimeUsage.
                if (apiEndpoint?.Contains("/realtime") == true)
                {
                    estimatedCost = OpenAIRealtimeUsage.FromPayload(responsePayload).CostUSD(model);
                }

                var log = new OpenAIApiLog
                {
                    LogDate = DateTime.UtcNow,
                    ApiEndpoint = TruncateString(apiEndpoint, 500),
                    ApiMethod = TruncateString(apiMethod, 50),
                    Model = TruncateString(model, 100),
                    AssistantId = TruncateString(assistantId, 100),
                    ThreadId = TruncateString(threadId, 100),
                    RunId = TruncateString(runId, 100),
                    FileId = TruncateString(fileId, 100),
                    SessionId = context?.SessionId,
                    RequestPayload = requestPayload,
                    ResponsePayload = responsePayload,
                    PromptTokens = promptTokens,
                    CompletionTokens = completionTokens,
                    TotalTokens = totalTokens,
                    EstimatedCostUSD = estimatedCost,
                    ResponseTimeMs = responseTimeMs,
                    StatusCode = statusCode,
                    IsSuccess = isSuccess,
                    ErrorMessage = errorMessage,
                    UserId = context?.UserId,
                    CandidateId = context?.CandidateId,
                    InterviewerId = context?.InterviewerId,
                    IRFId = context?.IRFId,
                    MeetingId = context?.MeetingId,
                    OperationType = TruncateString(context?.OperationType, 100),
                    CreatedBy = TruncateString(context?.CreatedBy, 100),
                    CreatedAt = DateTime.UtcNow
                };

                return await _logRepository.LogApiCallAsync(log);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }

        public decimal CalculateCost(string model, int promptTokens, int completionTokens)
        {
            return CalculateCost(model, promptTokens, completionTokens, 0);
        }

        /// <summary>
        /// Prices a call whose prompt was partly served from OpenAI's prompt cache. The provider counts
        /// cached tokens inside the prompt total, so only the remainder is billed at the full input rate.
        /// Models without a published cached rate bill cached tokens at the normal input rate.
        /// </summary>
        public decimal CalculateCost(string model, int promptTokens, int completionTokens, int cachedPromptTokens)
        {
            if (string.IsNullOrEmpty(model) || (promptTokens == 0 && completionTokens == 0))
                return 0;

            decimal inputCost = 0;
            decimal outputCost = 0;
            decimal? cachedCost = null;

            model = model.ToLower();

            // Check for gpt-4o models first (most common)
            if (model.Contains("gpt-4o-mini"))
            {
                inputCost = GPT4O_MINI_INPUT_COST_PER_1K;
                outputCost = GPT4O_MINI_OUTPUT_COST_PER_1K;
                cachedCost = GPT4O_MINI_CACHED_COST_PER_1K;
            }
            else if (model.Contains("gpt-4o"))
            {
                inputCost = GPT4O_INPUT_COST_PER_1K;
                outputCost = GPT4O_OUTPUT_COST_PER_1K;
                cachedCost = GPT4O_CACHED_COST_PER_1K;
            }
            // GPT-4.1 tiers, smallest first because "gpt-4.1-mini" also contains "gpt-4.1"
            else if (model.Contains("gpt-4.1-nano"))
            {
                inputCost = GPT41_NANO_INPUT_COST_PER_1K;
                outputCost = GPT41_NANO_OUTPUT_COST_PER_1K;
                cachedCost = GPT41_NANO_CACHED_COST_PER_1K;
            }
            else if (model.Contains("gpt-4.1-mini"))
            {
                inputCost = GPT41_MINI_INPUT_COST_PER_1K;
                outputCost = GPT41_MINI_OUTPUT_COST_PER_1K;
                cachedCost = GPT41_MINI_CACHED_COST_PER_1K;
            }
            else if (model.Contains("gpt-4.1"))
            {
                inputCost = GPT41_INPUT_COST_PER_1K;
                outputCost = GPT41_OUTPUT_COST_PER_1K;
                cachedCost = GPT41_CACHED_COST_PER_1K;
            }
            else if (model.Contains("gpt-5-nano"))
            {
                inputCost = GPT5_NANO_INPUT_COST_PER_1K;
                outputCost = GPT5_NANO_OUTPUT_COST_PER_1K;
                cachedCost = GPT5_NANO_CACHED_COST_PER_1K;
            }
            else if (model.Contains("gpt-5-mini"))
            {
                inputCost = GPT5_MINI_INPUT_COST_PER_1K;
                outputCost = GPT5_MINI_OUTPUT_COST_PER_1K;
                cachedCost = GPT5_MINI_CACHED_COST_PER_1K;
            }
            else if (model.Contains("gpt-5"))
            {
                inputCost = GPT5_INPUT_COST_PER_1K;
                outputCost = GPT5_OUTPUT_COST_PER_1K;
                cachedCost = GPT5_CACHED_COST_PER_1K;
            }
            // Legacy GPT-4 models
            else if (model.Contains("gpt-4-turbo") || model.Contains("gpt-4-1106") || model.Contains("gpt-4-0125"))
            {
                inputCost = GPT4_TURBO_INPUT_COST_PER_1K;
                outputCost = GPT4_TURBO_OUTPUT_COST_PER_1K;
            }
            else if (model.Contains("gpt-4"))
            {
                inputCost = GPT4_INPUT_COST_PER_1K;
                outputCost = GPT4_OUTPUT_COST_PER_1K;
            }
            else if (model.Contains("gpt-3.5-turbo"))
            {
                inputCost = GPT35_TURBO_INPUT_COST_PER_1K;
                outputCost = GPT35_TURBO_OUTPUT_COST_PER_1K;
            }
            else
            {
                // Default to gpt-4o pricing for unknown models (most likely case)
                inputCost = GPT4O_INPUT_COST_PER_1K;
                outputCost = GPT4O_OUTPUT_COST_PER_1K;
            }

            var cached = Math.Clamp(cachedPromptTokens, 0, Math.Max(promptTokens, 0));
            var uncachedPromptTokens = promptTokens - cached;

            var totalCost = (uncachedPromptTokens / 1000m * inputCost)
                + (cached / 1000m * (cachedCost ?? inputCost))
                + (completionTokens / 1000m * outputCost);
            return Math.Round(totalCost, 6);
        }

        /// <summary>
        /// Reads the cached share of the prompt from either API shape: the Responses API reports it under
        /// <c>usage.input_tokens_details</c>, Chat Completions under <c>usage.prompt_tokens_details</c>.
        /// </summary>
        public int ExtractCachedInputTokens(string responsePayload)
        {
            if (string.IsNullOrEmpty(responsePayload))
                return 0;

            try
            {
                var usage = JObject.Parse(responsePayload)["usage"];
                if (usage == null || usage.Type != JTokenType.Object)
                    return 0;

                return usage["input_tokens_details"]?["cached_tokens"]?.Value<int?>()
                    ?? usage["prompt_tokens_details"]?["cached_tokens"]?.Value<int?>()
                    ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        public (int promptTokens, int completionTokens, int totalTokens) ExtractTokenUsage(string responsePayload)
        {
            if (string.IsNullOrEmpty(responsePayload))
                return (0, 0, 0);

            try
            {
                var json = JObject.Parse(responsePayload);

                // Check for usage object (common in completion/chat APIs)
                var usage = json["usage"];
                if (usage != null)
                {
                    var promptTokens = usage["prompt_tokens"]?.Value<int>() ?? 0;
                    var completionTokens = usage["completion_tokens"]?.Value<int>() ?? 0;
                    var totalTokens = usage["total_tokens"]?.Value<int>() ?? (promptTokens + completionTokens);
                    
                    if (promptTokens > 0 || completionTokens > 0)
                        return (promptTokens, completionTokens, totalTokens);

                    // The Responses API (OpenAIResponsesHelper, every stored-prompt call) names the same
                    // counts input_tokens / output_tokens. Without this branch those calls log zero
                    // tokens and zero cost.
                    var inputTokens = usage["input_tokens"]?.Value<int>() ?? 0;
                    var outputTokens = usage["output_tokens"]?.Value<int>() ?? 0;
                    if (inputTokens > 0 || outputTokens > 0)
                        return (inputTokens, outputTokens, usage["total_tokens"]?.Value<int>() ?? (inputTokens + outputTokens));
                }

                // Check for camelCase version (some APIs return this)
                var usageCamel = json["Usage"];
                if (usageCamel != null)
                {
                    var promptTokens = usageCamel["promptTokens"]?.Value<int>() ?? usageCamel["PromptTokens"]?.Value<int>() ?? 0;
                    var completionTokens = usageCamel["completionTokens"]?.Value<int>() ?? usageCamel["CompletionTokens"]?.Value<int>() ?? 0;
                    var totalTokens = usageCamel["totalTokens"]?.Value<int>() ?? usageCamel["TotalTokens"]?.Value<int>() ?? (promptTokens + completionTokens);
                    
                    if (promptTokens > 0 || completionTokens > 0)
                        return (promptTokens, completionTokens, totalTokens);
                }

                // For assistant runs, check for different structure
                var promptTokensField = json["prompt_tokens"];
                var completionTokensField = json["completion_tokens"];
                if (promptTokensField != null || completionTokensField != null)
                {
                    var promptTokens = promptTokensField?.Value<int>() ?? 0;
                    var completionTokens = completionTokensField?.Value<int>() ?? 0;
                    return (promptTokens, completionTokens, promptTokens + completionTokens);
                }
                
                // Check if response has "object": "thread.run" with status "completed"
                // In Assistants API v2, usage might be at different levels
                var objectType = json["object"]?.Value<string>();
                var status = json["status"]?.Value<string>();
                
                if (objectType == "thread.run" && status == "completed")
                {
                    // Try to find usage anywhere in the response
                    var allTokens = json.SelectTokens("$..usage").FirstOrDefault();
                    if (allTokens != null && allTokens is JObject usageObj)
                    {
                        var promptTokens = usageObj["prompt_tokens"]?.Value<int>() ?? 0;
                        var completionTokens = usageObj["completion_tokens"]?.Value<int>() ?? 0;
                        var totalTokens = usageObj["total_tokens"]?.Value<int>() ?? (promptTokens + completionTokens);
                        
                        if (promptTokens > 0 || completionTokens > 0)
                            return (promptTokens, completionTokens, totalTokens);
                    }
                }
                
                // Check for run steps format (list.thread.run.step)
                if (objectType == "list" && json["data"] != null)
                {
                    var dataArray = json["data"] as JArray;
                    if (dataArray != null && dataArray.Count > 0)
                    {
                        int totalPromptTokens = 0;
                        int totalCompletionTokens = 0;
                        int totalAllTokens = 0;
                        
                        // Sum up usage from all steps
                        foreach (var step in dataArray)
                        {
                            var stepUsage = step["usage"];
                            if (stepUsage != null)
                            {
                                totalPromptTokens += stepUsage["prompt_tokens"]?.Value<int>() ?? 0;
                                totalCompletionTokens += stepUsage["completion_tokens"]?.Value<int>() ?? 0;
                                totalAllTokens += stepUsage["total_tokens"]?.Value<int>() ?? 0;
                            }
                        }
                        
                        if (totalPromptTokens > 0 || totalCompletionTokens > 0)
                        {
                            // If total_tokens wasn't provided, calculate it
                            if (totalAllTokens == 0)
                                totalAllTokens = totalPromptTokens + totalCompletionTokens;
                                
                            return (totalPromptTokens, totalCompletionTokens, totalAllTokens);
                        }
                    }
                }
            }
            catch
            {
                // If parsing fails, return zeros
            }

            return (0, 0, 0);
        }

        public async Task<HttpResponseMessage> LoggedApiCallAsync(
            Func<Task<HttpResponseMessage>> apiCall,
            string apiEndpoint,
            string apiMethod,
            string requestPayload,
            string model = null,
            string assistantId = null,
            string threadId = null,
            string runId = null,
            string fileId = null,
            OpenAIApiLogContext context = null)
        {
            var stopwatch = Stopwatch.StartNew();
            HttpResponseMessage response = null;
            string responsePayload = null;
            bool isSuccess = false;
            string errorMessage = null;
            int statusCode = 0;

            try
            {
                response = await apiCall();
                stopwatch.Stop();

                statusCode = (int)response.StatusCode;
                isSuccess = response.IsSuccessStatusCode;
                responsePayload = await response.Content.ReadAsStringAsync();

                if (!isSuccess)
                {
                    errorMessage = $"HTTP {statusCode}: {responsePayload}";
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                isSuccess = false;
                errorMessage = ex.Message;
                statusCode = 500;
            }
            finally
            {
                await LogApiCallAsync(
                    apiEndpoint,
                    apiMethod,
                    requestPayload,
                    responsePayload,
                    (int)stopwatch.ElapsedMilliseconds,
                    statusCode,
                    isSuccess,
                    errorMessage,
                    model,
                    assistantId,
                    threadId,
                    runId,
                    fileId,
                    context);
            }

            return response;
        }

        private decimal EstimateWhisperCost(string requestPayload)
        {
            // Whisper charges per minute of audio
            // This is an estimation - actual duration would need to be extracted from audio file
            // Default to 1 minute if we can't determine
            return WHISPER_COST_PER_MINUTE;
        }

        private decimal EstimateTTSCost(string requestPayload)
        {
            try
            {
                var json = JObject.Parse(requestPayload);
                var text = json["input"]?.Value<string>();
                var model = json["model"]?.Value<string>()?.ToLower();

                if (!string.IsNullOrEmpty(text))
                {
                    var charCount = text.Length;
                    var costPerChar = model?.Contains("hd") == true ? TTS_HD_COST_PER_1K_CHARS : TTS_COST_PER_1K_CHARS;
                    return Math.Round(charCount / 1000m * costPerChar, 6);
                }
            }
            catch
            {
                // If parsing fails, return default
            }

            return TTS_COST_PER_1K_CHARS; // Default to 1K characters
        }

        private string TruncateString(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}
