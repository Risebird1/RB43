using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Risebird.CommonHelper;

public class InterviewApiService : IInterviewApiService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    public InterviewApiService(IConfiguration config)//HttpClient http,
    {
        //_http = http;
        _http = new HttpClient();
        _config = config;
        _http.BaseAddress = new Uri(_config["ApiLayer:ApiBaseUrl"]);
    }

    public async Task<string> SendInterviewMessage(Guid sessionId, string message)
    {
        var payload = new
        {
            SessionId = sessionId,
            Message = message
        };

        //var apiurl = "https://api.portal.risebird.io/api/CheckIRFAvailability/InterviewerAvailabilityCheck";
        //var apiurl = "https://" + "localhost:50323" + "/api/CheckIRFAvailability/InterviewerAvailabilityCheck";

        var response = await _http.PostAsJsonAsync("api/interview/ask", payload);
        if (!response.IsSuccessStatusCode)
            return "Error sending message.";

        var content = await response.Content.ReadAsStringAsync();
        var parsed = JsonConvert.DeserializeObject<dynamic>(content);
        return parsed?.reply ?? "";
    }
    //public async Task<string> StartConversation(Guid sessionId)
    //{
    //    var response = await _http.PostAsJsonAsync("api/interview/start", sessionId);
    //    if (!response.IsSuccessStatusCode)
    //    {
    //        return "[Error starting conversation]";
    //    }

    //    var content = await response.Content.ReadAsStringAsync();
    //    var parsed = JsonConvert.DeserializeObject<dynamic>(content);
    //    return parsed?.reply ?? "[No reply]";
    //}
    public async Task<InterviewStartResult> StartConversation(Guid sessionId, string prompt, string assistantId)
    {
        var payload = new InterviewMessageVM
        {
            SessionId = sessionId,
            Message = prompt,
            AssistantId = assistantId
        };

        var response = await _http.PostAsJsonAsync("api/OpenAIApi/start", payload);

        //var response = await _http.PostAsJsonAsync("api/OpenAIApi/start", prompt);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to start conversation: {response.StatusCode}");
        }

        var result = await response.Content.ReadFromJsonAsync<InterviewStartResult>();
        return result;
    }

    public async Task<List<string>> SendReply(Guid sessionId, string message, string assistantId)
    {
        var payload = new InterviewMessageVM
        {
            SessionId = sessionId,
            Message = message,
            AssistantId = assistantId
        };

        var response = await _http.PostAsJsonAsync("api/OpenAIApi/ask", payload);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Interview API error: {response.StatusCode}");
        }

        var result = await response.Content.ReadFromJsonAsync<AskResponse>();
        return result?.Replies ?? new List<string>();
    }

    public async Task<InterviewReportResult> GenerateReport(Guid sessionId, string prompt, int? aiInterviewType = null)
    {
        var payload = new AIReportVM
        {
            SessionId = sessionId,
            Message = prompt,
            AIInterviewType = aiInterviewType
        };

        var response = await _http.PostAsJsonAsync("api/OpenAIApi/report", payload);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to generate report: {response.StatusCode}");
        }

        var result = await response.Content.ReadFromJsonAsync<InterviewReportResult>();
        return result;
    }
}
