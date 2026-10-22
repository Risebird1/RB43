using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Data.OpenAIModels;
using Risebird.Entities.Enum;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
//using Twilio.Http;

namespace Risebird.CommonHelper
{
    public class OpenAIInterviewerAssistantHelper : IOpenAIInterviewerAssistantHelper
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;
        private readonly IOpenAIApiLogger _openAIApiLogger;

        private readonly string _openAiKey;
        //private readonly string _assistantId;

        private readonly IHttpClientFactory _httpClientFactory;

        public OpenAIInterviewerAssistantHelper(IConfiguration config, IOpenAIApiLogger openAIApiLogger, IHttpClientFactory httpClientFactory)//HttpClient http, 
        {
            _httpClientFactory = httpClientFactory;
            //_http = http;
            _http = _httpClientFactory.CreateClient();
            _config = config;
            _openAIApiLogger = openAIApiLogger;
            _openAiKey = _config["OpenAI:ApiKey"];
            //_assistantId = _config["OpenAI:AIInterviewerAssistantId"];

            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _openAiKey);
            _http.DefaultRequestHeaders.Add("OpenAI-Beta", "assistants=v2");
        }

        public async Task<string> CreateThreadAsync()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var response = await _http.PostAsJsonAsync("https://api.openai.com/v1/threads", new { });
            sw.Stop();

            var content = await response.Content.ReadAsStringAsync();
            
            // Log the API call
            await _openAIApiLogger.LogApiCallAsync(
                apiEndpoint: "https://api.openai.com/v1/threads",
                apiMethod: "POST",
                requestPayload: "{}",
                responsePayload: content,
                responseTimeMs: (int)sw.ElapsedMilliseconds,
                statusCode: (int)response.StatusCode,
                isSuccess: response.IsSuccessStatusCode,
                errorMessage: response.IsSuccessStatusCode ? null : content,
                model: null,
                assistantId: null,
                threadId: null,
                runId: null,
                fileId: null,
                context: new OpenAIApiLogContext
                {
                    OperationType = "AIInterview-ThreadCreation"
                });

            if (!response.IsSuccessStatusCode)
            {
                var error = content;
                throw new Exception($"Failed to create thread: {response.StatusCode} - {error}");
            }

            var json = JObject.Parse(content);
            return json["id"]?.ToString();

            //var result = await response.Content.ReadFromJsonAsync<dynamic>();
            //return result.id;
        }

        public async Task AddMessageAsync(string threadId, string role, string content)
        {
            var url = $"https://api.openai.com/v1/threads/{threadId}/messages";
            var payload = new { role, content };
            var requestJson = JsonConvert.SerializeObject(payload);
            
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var response = await _http.PostAsJsonAsync(url, payload);
            sw.Stop();
            
            var responseContent = await response.Content.ReadAsStringAsync();
            
            // Log the API call
            await _openAIApiLogger.LogApiCallAsync(
                apiEndpoint: url,
                apiMethod: "POST",
                requestPayload: requestJson,
                responsePayload: responseContent,
                responseTimeMs: (int)sw.ElapsedMilliseconds,
                statusCode: (int)response.StatusCode,
                isSuccess: response.IsSuccessStatusCode,
                errorMessage: response.IsSuccessStatusCode ? null : responseContent,
                model: null,
                assistantId: null,
                threadId: threadId,
                runId: null,
                fileId: null,
                context: new OpenAIApiLogContext
                {
                    OperationType = "AIInterview-AddMessage"
                });
        }

        public async Task<string> CreateRunAsync(string threadId, string assistantId)
        {
            var url = $"https://api.openai.com/v1/threads/{threadId}/runs";
            //var payload = new { assistant_id = _assistantId };
            // Add response_format to request structured JSON output (reduces output tokens)
            var payload = new { 
                assistant_id = assistantId,
                response_format = new { type = "json_object" }
            };
            var requestJson = JsonConvert.SerializeObject(payload);
            
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var response = await _http.PostAsJsonAsync(url, payload);
            sw.Stop();

            var content = await response.Content.ReadAsStringAsync();
            
            // Extract model and runId from response
            string modelName = null;
            string runIdFromResponse = null;
            try
            {
                var json = JObject.Parse(content);
                modelName = json["model"]?.ToString();
                runIdFromResponse = json["id"]?.ToString();
            }
            catch { /* Continue even if extraction fails */ }
            
            // Log the API call
            await _openAIApiLogger.LogApiCallAsync(
                apiEndpoint: url,
                apiMethod: "POST",
                requestPayload: requestJson,
                responsePayload: content,
                responseTimeMs: (int)sw.ElapsedMilliseconds,
                statusCode: (int)response.StatusCode,
                isSuccess: response.IsSuccessStatusCode,
                errorMessage: response.IsSuccessStatusCode ? null : content,
                model: modelName,
                assistantId: assistantId,
                threadId: threadId,
                runId: runIdFromResponse,
                fileId: null,
                context: new OpenAIApiLogContext
                {
                    OperationType = "AIInterview-CreateRun"
                });
            
            return runIdFromResponse;

            //var result = await response.Content.ReadFromJsonAsync<dynamic>();
            //return result.id;
        }

        public async Task<string> WaitForRunAsync(string threadId, string runId)
        {
            string url = $"https://api.openai.com/v1/threads/{threadId}/runs/{runId}";
            string status = "queued"; // Initialize to prevent unassigned variable error
            do
            {
                await Task.Delay(2000);
                
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var response = await _http.GetAsync(url);
                sw.Stop();
                
                var content = await response.Content.ReadAsStringAsync();
                
                // Extract model and assistant from response
                string modelName = null;
                string assistantIdFromResponse = null;
                try
                {
                    var json = JObject.Parse(content);
                    modelName = json["model"]?.ToString();
                    assistantIdFromResponse = json["assistant_id"]?.ToString();
                    status = json["status"]?.ToString() ?? "completed"; // Default to completed if null to exit loop
                }
                catch 
                { 
                    // If parsing fails, default to completed to exit loop
                    status = "completed";
                }
                
                // Log the API call
                await _openAIApiLogger.LogApiCallAsync(
                    apiEndpoint: url,
                    apiMethod: "GET",
                    requestPayload: null,
                    responsePayload: content,
                    responseTimeMs: (int)sw.ElapsedMilliseconds,
                    statusCode: (int)response.StatusCode,
                    isSuccess: response.IsSuccessStatusCode,
                    errorMessage: response.IsSuccessStatusCode ? null : content,
                    model: modelName,
                    assistantId: assistantIdFromResponse,
                    threadId: threadId,
                    runId: runId,
                    fileId: null,
                    context: new OpenAIApiLogContext
                    {
                        OperationType = "AIInterview-RunStatusCheck"
                    });

                //var result = await response.Content.ReadFromJsonAsync<dynamic>();
                //status = result.status;
            }
            while (status == "queued" || status == "in_progress");

            // After run completes, retrieve run steps to capture token usage
            if (status == "completed")
            {
                await RetrieveRunStepsForTokenUsage(runId, threadId);
            }

            return status;
        }
        
        private async Task RetrieveRunStepsForTokenUsage(string runId, string threadId)
        {
            try
            {
                var url = $"https://api.openai.com/v1/threads/{threadId}/runs/{runId}/steps";
                
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var response = await _http.GetAsync(url);
                sw.Stop();
                
                var responseContent = await response.Content.ReadAsStringAsync();
                
                // Extract model from the steps if available
                string modelName = null;
                string assistantIdFromResponse = null;
                try
                {
                    var json = JObject.Parse(responseContent);
                    var dataArray = json["data"] as JArray;
                    if (dataArray != null && dataArray.Count > 0)
                    {
                        var firstStep = dataArray[0] as JObject;
                        modelName = firstStep?["model"]?.ToString();
                        assistantIdFromResponse = firstStep?["assistant_id"]?.ToString();
                    }
                }
                catch { /* Continue even if extraction fails */ }
                
                // Log the API call - this will extract token usage from run steps
                await _openAIApiLogger.LogApiCallAsync(
                    apiEndpoint: url,
                    apiMethod: "GET",
                    requestPayload: null,
                    responsePayload: responseContent,
                    responseTimeMs: (int)sw.ElapsedMilliseconds,
                    statusCode: (int)response.StatusCode,
                    isSuccess: response.IsSuccessStatusCode,
                    errorMessage: response.IsSuccessStatusCode ? null : responseContent,
                    model: modelName,
                    assistantId: assistantIdFromResponse,
                    threadId: threadId,
                    runId: runId,
                    fileId: null,
                    context: new OpenAIApiLogContext
                    {
                        OperationType = "AIInterview-RunStepsRetrieval-TokenUsage"
                    });
            }
            catch (Exception ex)
            {
                // Silently fail - we don't want to break the main flow if run steps retrieval fails
                // The error will be logged by the OpenAIApiLogger
            }
        }

        public async Task<List<string>> GetMessagesAsync(string threadId)
        {
            try
            {
                var url = $"https://api.openai.com/v1/threads/{threadId}/messages";
                
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var response = await _http.GetAsync(url);
                sw.Stop();
                
                var content = await response.Content.ReadAsStringAsync();
                
                // Log the API call
                await _openAIApiLogger.LogApiCallAsync(
                    apiEndpoint: url,
                    apiMethod: "GET",
                    requestPayload: null,
                    responsePayload: content,
                    responseTimeMs: (int)sw.ElapsedMilliseconds,
                    statusCode: (int)response.StatusCode,
                    isSuccess: response.IsSuccessStatusCode,
                    errorMessage: response.IsSuccessStatusCode ? null : content,
                    model: null,
                    assistantId: null,
                    threadId: threadId,
                    runId: null,
                    fileId: null,
                    context: new OpenAIApiLogContext
                    {
                        OperationType = "AIInterview-GetMessages"
                    });
                
                //var result = await response.Content.ReadFromJsonAsync<dynamic>();
                var result = await response.Content.ReadFromJsonAsync<MessageListResponse>();
                var messages = new List<string>();
                //foreach (var msg in result.data)
                //{
                //    if (msg.role == "assistant")
                //        messages.Add((string)msg.content[0].text.value);
                //}
                foreach (var msg in result.data)
                {
                    if (msg.role == "assistant")
                    {
                        foreach (var contentItem in msg.content)
                        {
                            if (contentItem.type == "text")
                            {
                                messages.Add(contentItem.text.value);
                            }
                        }
                    }
                }
                return messages;
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        public async Task<List<TranscriptConv>> GetFullConversation(Guid sessionId, string threadId)
        {
            var url = $"https://api.openai.com/v1/threads/{threadId}/messages";
            
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var response = await _http.GetAsync(url);
            sw.Stop();
            
            var content = await response.Content.ReadAsStringAsync();
            
            // Log the API call
            await _openAIApiLogger.LogApiCallAsync(
                apiEndpoint: url,
                apiMethod: "GET",
                requestPayload: null,
                responsePayload: content,
                responseTimeMs: (int)sw.ElapsedMilliseconds,
                statusCode: (int)response.StatusCode,
                isSuccess: response.IsSuccessStatusCode,
                errorMessage: response.IsSuccessStatusCode ? null : content,
                model: null,
                assistantId: null,
                threadId: threadId,
                runId: null,
                fileId: null,
                context: new OpenAIApiLogContext
                {
                    SessionId = sessionId,
                    OperationType = "AIInterview-GetFullConversation"
                });
            
            var result = await response.Content.ReadFromJsonAsync<dynamic>();

            var messages = new List<TranscriptConv>();
            foreach (var msg in result.data)
            {
                var role = (string)msg.role;
                var messageContent = (string)msg.content[0].text.value;

                messages.Add(new TranscriptConv
                {
                    SessionId = sessionId,
                    Role = role,
                    Message = messageContent,
                    Timestamp = DateTime.UtcNow
                });
            }
            return messages.OrderBy(m => m.Timestamp).ToList();
        }
    }

    public class MessageListResponse
    {
        public List<OpenAIMessage> data { get; set; }
    }

    public class OpenAIMessage
    {
        public string id { get; set; }
        public string role { get; set; }
        public List<OpenAIContent> content { get; set; }
    }

    public class OpenAIContent
    {
        public string type { get; set; }
        public OpenAIText text { get; set; }
    }

    public class OpenAIText
    {
        public string value { get; set; }
    }
}
