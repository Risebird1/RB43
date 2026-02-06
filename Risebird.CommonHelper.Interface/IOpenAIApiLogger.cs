using Risebird.Entities.Data;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    public interface IOpenAIApiLogger
    {
        /// <summary>
        /// Logs an OpenAI API call with automatic token and cost calculation
        /// </summary>
        Task<long> LogApiCallAsync(
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
            OpenAIApiLogContext context = null);

        /// <summary>
        /// Calculates estimated cost based on model and token usage
        /// </summary>
        decimal CalculateCost(string model, int promptTokens, int completionTokens);

        /// <summary>
        /// Extracts token usage from OpenAI response
        /// </summary>
        (int promptTokens, int completionTokens, int totalTokens) ExtractTokenUsage(string responsePayload);

        /// <summary>
        /// Wraps an HTTP call to OpenAI API with automatic logging
        /// </summary>
        Task<HttpResponseMessage> LoggedApiCallAsync(
            Func<Task<HttpResponseMessage>> apiCall,
            string apiEndpoint,
            string apiMethod,
            string requestPayload,
            string model = null,
            string assistantId = null,
            string threadId = null,
            string runId = null,
            string fileId = null,
            OpenAIApiLogContext context = null);
    }
}
