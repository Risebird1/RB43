using Microsoft.Extensions.Configuration;
using RestSharp;
using Risebird.CommonHelper.Interface;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;


namespace Risebird.CommonHelper
{
    public class SynthesiaTextToVideoService : ISynthesiaTextToVideoService /// irrespective of the name, this service also contains the other function (text to speech)

    {
        //private readonly HttpClient _http;
        private readonly IConfiguration _config;

        private readonly string _apiKey;
        private readonly string _templateId;

        public SynthesiaTextToVideoService(IConfiguration config)
        {
            //_http = new HttpClient();
            _config = config;
            _apiKey = _config["Synthesia:ApiKey"];
            _templateId = _config["Synthesia:TemplateId"];
            //_http.BaseAddress = new Uri("https://api.synthesia.io/v2/");
            //_http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }

        public async Task<string> CreateVideoAsync(string scriptText, string templateIdParam)
        {
            
            var options = new RestClientOptions("https://api.synthesia.io/v2/videos/fromTemplate");
            var client = new RestClient(options);
            var request = new RestRequest("");
            request.AddHeader("accept", "application/json");
            request.AddHeader("Authorization", _apiKey);
            //request.AddJsonBody("{\"test\":false,\"templateData\":{\"scriptDynamic\":\"Hello, How are you doing?\"},\"visibility\":\"private\",\"templateId\":\"8fd3aaab-d7db-46e3-bf43-9969eb4a8e25\"}", false);

            var body = new
            {
                test = false,
                visibility = "private",
                templateId = templateIdParam, //_templateId,//"8fd3aaab-d7db-46e3-bf43-9969eb4a8e25",
                templateData = new
                {
                    scriptDynamic = CleanMessage(scriptText)
                }
            };

            request.AddJsonBody(body);

            var response = await client.PostAsync(request);
            using var doc = JsonDocument.Parse(response.Content);
            var id = doc.RootElement.GetProperty("id").GetString();
            return id;

        }

        public async Task<string> GetVideoUrlAsync(string videoId)
        {
            
            var options = new RestClientOptions($"https://api.synthesia.io/v2/videos/{videoId}");
            var client = new RestClient(options);
            var request = new RestRequest("");
            request.AddHeader("accept", "application/json");
            request.AddHeader("Authorization", _apiKey);
            var response = await client.GetAsync(request);

            var result = JsonDocument.Parse(response.Content);
            var videoUrl = result.RootElement.GetProperty("download").GetString();
            return videoUrl;
            
        }

        private string CleanMessage(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";

            return input
                .Replace("#", "")        // Remove Markdown headers
                .Replace("**", "")       // Remove bold formatting if present
                .Trim();                 // Trim whitespace
        }
    }

}
