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

        private readonly string _openAiKey;
        //private readonly string _assistantId;

        public OpenAIInterviewerAssistantHelper(IConfiguration config)//HttpClient http, 
        {
            //_http = http;
            _http = new HttpClient();
            _config = config;
            _openAiKey = _config["OpenAI:ApiKey"];
            //_assistantId = _config["OpenAI:AIInterviewerAssistantId"];

            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _openAiKey);
            _http.DefaultRequestHeaders.Add("OpenAI-Beta", "assistants=v2");
        }

        public async Task<string> CreateThreadAsync()
        {
            //var response = await _http.PostAsync("https://api.openai.com/v1/threads", null);
            var response = await _http.PostAsJsonAsync("https://api.openai.com/v1/threads", new { });


            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to create thread: {response.StatusCode} - {error}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(content);
            return json["id"]?.ToString();

            //var result = await response.Content.ReadFromJsonAsync<dynamic>();
            //return result.id;
        }

        public async Task AddMessageAsync(string threadId, string role, string content)
        {
            var url = $"https://api.openai.com/v1/threads/{threadId}/messages";
            var payload = new { role, content };
            await _http.PostAsJsonAsync(url, payload);
        }

        public async Task<string> CreateRunAsync(string threadId, string assistantId)
        {
            var url = $"https://api.openai.com/v1/threads/{threadId}/runs";
            //var payload = new { assistant_id = _assistantId };
            var payload = new { assistant_id = assistantId };
            var response = await _http.PostAsJsonAsync(url, payload);

            var content = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(content);
            return json["id"]?.ToString();

            //var result = await response.Content.ReadFromJsonAsync<dynamic>();
            //return result.id;
        }

        public async Task<string> WaitForRunAsync(string threadId, string runId)
        {
            string url = $"https://api.openai.com/v1/threads/{threadId}/runs/{runId}";
            string status;
            do
            {
                await Task.Delay(2000);
                var response = await _http.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(content);
                status = json["status"]?.ToString();

                //var result = await response.Content.ReadFromJsonAsync<dynamic>();
                //status = result.status;
            }
            while (status == "queued" || status == "in_progress");

            return status;
        }

        public async Task<List<string>> GetMessagesAsync(string threadId)
        {
            try
            {
                var url = $"https://api.openai.com/v1/threads/{threadId}/messages";
                var response = await _http.GetAsync(url);
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
            var response = await _http.GetAsync(url);
            var result = await response.Content.ReadFromJsonAsync<dynamic>();

            var messages = new List<TranscriptConv>();
            foreach (var msg in result.data)
            {
                var role = (string)msg.role;
                var content = (string)msg.content[0].text.value;

                messages.Add(new TranscriptConv
                {
                    SessionId = sessionId,
                    Role = role,
                    Message = content,
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
