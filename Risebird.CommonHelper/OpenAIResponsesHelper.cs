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
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IOpenAIApiLogger _openAIApiLogger;
        private readonly string _apiKey;
        private readonly string _baseUrl;
        private readonly string _responsesModel;

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
        }

        public Task<RunResult> CreateJsonResponseAsync(
            string input,
            string operationType,
            string promptId)
        {
            return CreateJsonResponseAsync((object)input, operationType, promptId);
        }

        public async Task<RunResult> CreateJsonResponseAsync(
            object input,
            string operationType,
            string promptId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_apiKey))
                {
                    throw new InvalidOperationException("OpenAI:ApiKey is not configured.");
                }

                if (string.IsNullOrWhiteSpace(_responsesModel))
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

                var requestBody = new
                {
                    prompt = new { id = promptId },
                    model = _responsesModel,
                    input,
                    text = new { format = new { type = "json_object" } }
                };

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
                    runId: ExtractResponseId(responseContent),
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

        private static string ExtractOutputText(string responseContent)
        {
            var json = JObject.Parse(responseContent);
            var outputText = json["output_text"]?.ToString();
            if (!string.IsNullOrWhiteSpace(outputText))
            {
                return outputText;
            }

            var message = json["output"]?
                .FirstOrDefault(item => item?["type"]?.ToString() == "message");

            return message?["content"]?
                .FirstOrDefault(item => item?["type"]?.ToString() == "output_text")?["text"]?
                .ToString();
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
