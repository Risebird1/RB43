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

        // OpenAI Pricing (as of 2024 - update as needed)
        private readonly decimal GPT4_INPUT_COST_PER_1K = 0.03m;
        private readonly decimal GPT4_OUTPUT_COST_PER_1K = 0.06m;
        private readonly decimal GPT4_TURBO_INPUT_COST_PER_1K = 0.01m;
        private readonly decimal GPT4_TURBO_OUTPUT_COST_PER_1K = 0.03m;
        private readonly decimal GPT35_TURBO_INPUT_COST_PER_1K = 0.0015m;
        private readonly decimal GPT35_TURBO_OUTPUT_COST_PER_1K = 0.002m;
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
                var (promptTokens, completionTokens, totalTokens) = ExtractTokenUsage(responsePayload);
                var estimatedCost = CalculateCost(model, promptTokens, completionTokens);

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
            if (string.IsNullOrEmpty(model) || (promptTokens == 0 && completionTokens == 0))
                return 0;

            decimal inputCost = 0;
            decimal outputCost = 0;

            model = model.ToLower();

            if (model.Contains("gpt-4") && !model.Contains("turbo"))
            {
                inputCost = GPT4_INPUT_COST_PER_1K;
                outputCost = GPT4_OUTPUT_COST_PER_1K;
            }
            else if (model.Contains("gpt-4") && model.Contains("turbo"))
            {
                inputCost = GPT4_TURBO_INPUT_COST_PER_1K;
                outputCost = GPT4_TURBO_OUTPUT_COST_PER_1K;
            }
            else if (model.Contains("gpt-3.5-turbo"))
            {
                inputCost = GPT35_TURBO_INPUT_COST_PER_1K;
                outputCost = GPT35_TURBO_OUTPUT_COST_PER_1K;
            }
            else
            {
                // Default to GPT-3.5 pricing for unknown models
                inputCost = GPT35_TURBO_INPUT_COST_PER_1K;
                outputCost = GPT35_TURBO_OUTPUT_COST_PER_1K;
            }

            var totalCost = (promptTokens / 1000m * inputCost) + (completionTokens / 1000m * outputCost);
            return Math.Round(totalCost, 6);
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
