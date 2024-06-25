using Azure.Core;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Data.OpenAIModels;
using Risebird.Entities.Enum;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
//using Twilio.Http;

namespace Risebird.CommonHelper
{
    public class AssistantServiceHelper : IAssistantServiceHelper
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly string apiKey;
        private readonly string baseUrl;
        private readonly string candidateScreenerAssistantId;
        private readonly string candidateResumesVectorStoreId;
        //private readonly List<string> candidateResumesVectorStoreId;
        public AssistantServiceHelper(IApplicationErrorLogger applicationErrorLogger, IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            apiKey = _configuration["OpenAI:ApiKey"];
            baseUrl = _configuration["OpenAI:BaseUrl"];
            candidateScreenerAssistantId = _configuration["OpenAI:CandidateScreenerAssistantId"];
            candidateResumesVectorStoreId = _configuration["OpenAI:CandidateResumesVectorStoreId"];//"vs_ryqG2ou94myQaS3TOatPEZwY";
        }

        public async Task<RunResult> ScreenCandidateAsync(CandidateScreeningRequest requestModel)
        {
            try
            {
                var fileId = await UploadFileAsync(requestModel.ResumeLink);
                await AddFileToVectorStoreAsync(fileId);
                var threadId = await CreateThreadAsync(requestModel);

                //now create a run
                var runId = await CreateRun(threadId);

                string runStatus = string.Empty;
                //retrieve a run
                while(runStatus != "completed")
                {
                    await Task.Delay(5000);
                    runStatus = await RetrieveRun(runId, threadId);
                    if (runStatus == "failed")
                        break;
                }
                var messages = await ListRunMessages(threadId);
                return messages;
                //if (runStatus == "completed")
                //{
                //    //retrive messages list
                //    var messages = await ListRunMessages(threadId);
                //    return messages;
                //    //return new CandidateScreeningResponse { Status = true, EvaluationResult = messages?.Data.Where };
                //}
                //else if(runStatus == "failed")
                //{
                //    return null;
                //}

                //return null;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        private async Task<string> CreateRun(string threadId)
        {
            try
            {
                HttpClient client = new HttpClient();
                var requestBody = new
                {
                    assistant_id = candidateScreenerAssistantId
                    //response_format = new { type = "json_object" }
                };

                var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/threads/{threadId}/runs")
                {
                    Headers = { { "Authorization", $"Bearer {apiKey}" }, { "OpenAI-Beta", "assistants=v2" } }//{ "Content-Type", "application/json" },
                };

                var jsonContent = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                request.Content = content;

                var response = await client.SendAsync(request);

                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();
                var runResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseContent);
                return runResponse["id"].ToString();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }

        }

        private async Task<string> RetrieveRun(string runId, string threadId)
        {
            try
            {
                HttpClient client = new HttpClient();

                var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/threads/{threadId}/runs/{runId}")
                {
                    Headers = { { "Authorization", $"Bearer {apiKey}" }, { "OpenAI-Beta", "assistants=v2" } }//{ "Content-Type", "application/json" },
                };

                var response = await client.SendAsync(request);
                //HttpClient client = new HttpClient();
                //client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                //client.DefaultRequestHeaders.Add("Content-Type", "application/json");
                //client.DefaultRequestHeaders.Add("OpenAI-Beta", "assistants=v2");
                //var response = await client.PostAsync($"{baseUrl}/threads/{threadId}/runs", content);
                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();
                var runResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseContent);
                var failedAt = runResponse["failed_at"];
                if (failedAt != null)
                {
                    return "failed";
                }
                return runResponse["status"].ToString();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }

        }
        private async Task<RunResult> ListRunMessages(string threadId)
        {
            try
            {
                HttpClient client = new HttpClient();

                var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/threads/{threadId}/messages")
                {
                    Headers = { { "Authorization", $"Bearer {apiKey}" }, { "OpenAI-Beta", "assistants=v2" }}//{ "Content-Type", "application/json" },
                };

                var response = await client.SendAsync(request);

                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();

                RunResult runResultMessages = JsonConvert.DeserializeObject<RunResult>(responseContent);
                return runResultMessages;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        private async Task<string> UploadFileAsync(string resumeLink)
        {

            try
            {
                using (HttpClient resumeClient = new HttpClient())
                {
                    var data = await resumeClient.GetByteArrayAsync(resumeLink);
                    var fileContent = new ByteArrayContent(data);
                    var content = new MultipartFormDataContent
                    {
                        { new StringContent("assistants"), "purpose" },
                        { fileContent, "file", resumeLink.Split("/").Last().Split("?")[0] }
                    };

                    HttpClient client = new HttpClient();
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                    var response = await client.PostAsync($"{baseUrl}/files", content);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    var fileResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseContent);
                    return fileResponse["id"].ToString();
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }

            
        }

        private async Task<bool> AddFileToVectorStoreAsync(string fileId)
        {
            try
            {
                HttpClient client = new HttpClient();
                var requestBody = new
                {
                    file_id = fileId
                };

                var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/vector_stores/{candidateResumesVectorStoreId}/files");

                // Add headers to the request
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                request.Headers.Add("OpenAI-Beta", "assistants=v2");

                var jsonContent = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                //content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                request.Content = content;

                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                return true;

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
            
        }
        private async Task<string> CreateThreadAsync(CandidateScreeningRequest requestModel)
        {
            try
            {
               
                var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/threads")
                {
                    Headers = { { "Authorization", $"Bearer {apiKey}" }, { "OpenAI-Beta", "assistants=v2" } }//{ "Content-Type", "application/json" }, 
                };


                var threadRequest = new Thread
                {
                    messages = new List<Message>
                    {
                        new Message { role = "assistant", content = $"Evaluate the resume and generate a response in json as per the instructions fed to you. Do not give me comparison with other Resumes. Stick to the current resume I am feeding you. The job title you need to screen the candidate and generate the fitment score against is {requestModel.JDTitle}."}//string.Join(",",requestModel.Skills)//{requestModel.Skills}//Python(must have) and DSA
                    },
                    tool_resources = new ToolResources()
                    {
                        file_search = new FileSearch
                        {
                            vector_store_ids = new List<string>() { candidateResumesVectorStoreId }
                        }
                    }
                };
                var jsonContent = JsonConvert.SerializeObject(threadRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                request.Content = content;

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                dynamic thread = JsonConvert.DeserializeObject(responseContent);

                //return JsonConvert.DeserializeObject<CandidateScreeningResponse>(responseContent);
                return thread.id;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }


    }
    public class Message
    {
        public string role { get; set; }
        public string content { get; set; }
    }

    public class Thread
    {
        //public string assistant_id { get; set; }
        public List<Message> messages { get; set; }
        public ToolResources tool_resources { get; set; }
    }
    public class ToolResources
    {
        public FileSearch file_search { get; set; }
    }
    public class FileSearch
    {
        public List<string> vector_store_ids { get; set; }
    }
    public class MetaData
    {
        public string resume { get; set; }
        public string[] skills { get; set; }
    }

    public class AssistantRequest
    {
        public string assistant_id { get; set; }
        public Thread thread { get; set; }
        //public MetaData metadata { get; set; }
    }

}
