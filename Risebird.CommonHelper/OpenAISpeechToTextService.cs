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
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
//using Twilio.Http;

namespace Risebird.CommonHelper
{
    public class OpenAISpeechToTextService : IOpenAISpeechToTextService /// irrespective of the name, this service also contains the other function (text to speech)

    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;
        private readonly IOpenAIApiLogger _openAIApiLogger;

        private readonly string _openAiKey;

        public OpenAISpeechToTextService(IConfiguration config, IOpenAIApiLogger openAIApiLogger)//HttpClient http, 
        {
            //_http = http;
            _http = new HttpClient();
            _config = config;
            _openAIApiLogger = openAIApiLogger;
            _openAiKey = _config["OpenAI:ApiKey"];

            //_http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _openAiKey);
            //_http.DefaultRequestHeaders.Add("OpenAI-Beta", "assistants=v2");
        }

        public async Task<string> TranscribeAsync(Stream audioStream, string fileName, bool isHindi = false)
        {
            using var form = new MultipartFormDataContent();
            using var audioContent = new StreamContent(audioStream);
            audioContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");

            form.Add(audioContent, "file", fileName);
            form.Add(new StringContent("whisper-1"), "model");
            if(isHindi)
            {
                form.Add(new StringContent("hi"), "language");
            }

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/audio/transcriptions")
            {
                Content = form
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _openAiKey);

            var requestPayload = $"{{\"model\":\"whisper-1\",\"file\":\"{fileName}\",\"language\":\"{(isHindi ? "hi" : "en")}\"}}";
            
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var response = await _http.SendAsync(request);
            sw.Stop();
            
            var content = await response.Content.ReadAsStringAsync();

            // Log the API call
            await _openAIApiLogger.LogApiCallAsync(
                apiEndpoint: "https://api.openai.com/v1/audio/transcriptions",
                apiMethod: "POST",
                requestPayload: requestPayload,
                responsePayload: content,
                responseTimeMs: (int)sw.ElapsedMilliseconds,
                statusCode: (int)response.StatusCode,
                isSuccess: response.IsSuccessStatusCode,
                errorMessage: response.IsSuccessStatusCode ? null : content,
                model: "whisper-1",
                assistantId: null,
                threadId: null,
                runId: null,
                fileId: null,
                context: new OpenAIApiLogContext
                {
                    OperationType = "SpeechToText"
                });

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Speech-to-text failed: " + content);
            }

            var json = JsonDocument.Parse(content);
            return json.RootElement.GetProperty("text").GetString();
        }

        public async Task<byte[]> ConvertTextToSpeechAsync(string text)
        {
            var payload = new
            {
                model = "tts-1",
                input = text,
                voice = "alloy",
                response_format = "mp3"
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/audio/speech")
            {
                Content = JsonContent.Create(payload)
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _openAiKey);

            var requestPayload = System.Text.Json.JsonSerializer.Serialize(payload);
            
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var response = await _http.SendAsync(request);
            sw.Stop();

            var content = response.IsSuccessStatusCode ? "[Binary Audio Data]" : await response.Content.ReadAsStringAsync();
            
            // Log the API call
            await _openAIApiLogger.LogApiCallAsync(
                apiEndpoint: "https://api.openai.com/v1/audio/speech",
                apiMethod: "POST",
                requestPayload: requestPayload,
                responsePayload: content,
                responseTimeMs: (int)sw.ElapsedMilliseconds,
                statusCode: (int)response.StatusCode,
                isSuccess: response.IsSuccessStatusCode,
                errorMessage: response.IsSuccessStatusCode ? null : content,
                model: "tts-1",
                assistantId: null,
                threadId: null,
                runId: null,
                fileId: null,
                context: new OpenAIApiLogContext
                {
                    OperationType = "TextToSpeech"
                });

            if (!response.IsSuccessStatusCode)
            {
                var error = content;
                throw new Exception($"TTS failed: {error}");
            }

            return await response.Content.ReadAsByteArrayAsync();
        }
    }

}
