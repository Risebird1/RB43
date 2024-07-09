using Azure.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
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
        private readonly string interviewerScreenerAssistantId;
        private readonly string interviewerResumesVectorStoreId;
        private readonly string reportQCAssistantId;
        private readonly string reportQCVectorStoreId;
        //private readonly List<string> candidateResumesVectorStoreId;
        public AssistantServiceHelper(IApplicationErrorLogger applicationErrorLogger, IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            apiKey = _configuration["OpenAI:ApiKey"];
            baseUrl = _configuration["OpenAI:BaseUrl"];
            candidateScreenerAssistantId = _configuration["OpenAI:CandidateScreenerAssistantId"];
            candidateResumesVectorStoreId = _configuration["OpenAI:CandidateResumesVectorStoreId"];
            interviewerScreenerAssistantId = _configuration["OpenAI:InterviewerScreenerAssistantId"];
            interviewerResumesVectorStoreId = _configuration["OpenAI:InterviewerResumesVectorStoreId"];
            reportQCAssistantId = _configuration["OpenAI:InterviewerReportQCAssistantId"];
            reportQCVectorStoreId = _configuration["OpenAI:ReportQCVectorStoreId"];
        }

        public async Task<RunResult> ScreenAsync(ScreeningRequest requestModel)
        {
            try
            {
                string fileId;
                if (requestModel.Type == ProfileNames.QualityController)
                {
                    fileId = await UploadFileAsync(requestModel.ReportUrl);

                }
                else
                {
                    fileId = await UploadFileAsync(requestModel.ResumeLink);
                }
                if (!string.IsNullOrEmpty(fileId))
                {
                    await AddFileToVectorStoreAsync(fileId, requestModel.Type);
                }
                else
                {
                    return null;
                }

                var threadId = await CreateThreadAsync(requestModel);
                string runId;
                //now create a run
                if (!string.IsNullOrEmpty(threadId))
                {
                    runId = await CreateRun(threadId, requestModel.Type);
                }
                else
                {
                    return null;
                }

                if (!string.IsNullOrEmpty(runId))
                {
                    string runStatus = string.Empty;
                    //retrieve a run
                    while (runStatus != "completed")
                    {
                        await Task.Delay(5000);
                        runStatus = await RetrieveRun(runId, threadId);
                        if (runStatus == "failed")
                            break;
                    }
                    var messages = await ListRunMessages(threadId);
                    return messages;
                }
                else
                {
                    return null;
                }
                
                
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        private async Task<string> CreateRun(string threadId, string profileType)
        {
            try
            {
                HttpClient client = new HttpClient();
                string assistantId = string.Empty;
                if(profileType == ProfileNames.Candidate)
                {
                    assistantId = candidateScreenerAssistantId;
                }
                else if(profileType == ProfileNames.Interviewer)
                {
                    assistantId = interviewerScreenerAssistantId;
                }
                else
                {
                    assistantId = reportQCAssistantId;
                }
                var requestBody = new
                {
                    assistant_id = assistantId//(profileType == ProfileNames.Candidate ? candidateScreenerAssistantId : interviewerScreenerAssistantId)
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
        private async Task<string> UploadFileAsync(string url)
        {

            try
            {
                using (HttpClient resumeClient = new HttpClient())
                {
                    var data = await resumeClient.GetByteArrayAsync(url);
                    var fileContent = new ByteArrayContent(data);
                    var content = new MultipartFormDataContent
                    {
                        { new StringContent("assistants"), "purpose" },
                        { fileContent, "file", url.Split("/").Last().Split("?")[0] }
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

        private async Task<bool> AddFileToVectorStoreAsync(string fileId, string profileType)
        {
            try
            {
                HttpClient client = new HttpClient();
                var requestBody = new
                {
                    file_id = fileId
                };
                string vectorStoreId;
                if(profileType == ProfileNames.Candidate)
                {
                    vectorStoreId = candidateResumesVectorStoreId;
                }
                else if(profileType == ProfileNames.Interviewer)
                {
                    vectorStoreId = interviewerResumesVectorStoreId;
                }
                else
                {
                    vectorStoreId = reportQCVectorStoreId;
                }
                var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/vector_stores/{vectorStoreId}/files");

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
        private async Task<string> CreateThreadAsync(ScreeningRequest requestModel)
        {
            try
            {
               
                var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/threads")
                {
                    Headers = { { "Authorization", $"Bearer {apiKey}" }, { "OpenAI-Beta", "assistants=v2" } }//{ "Content-Type", "application/json" }, 
                };

                string queryContent = string.Empty;
                if(requestModel.Type == ProfileNames.Candidate)
                {
                    queryContent = $"Evaluate the resume and generate a response in json as per the instructions fed to you. Do not give me comparison with other Resumes. Stick to the current resume I am feeding you. The job title you need to screen the candidate and generate the fitment score against is {requestModel.JDTitle}.";
                }
                else if(requestModel.Type == ProfileNames.Interviewer)
                {
                    queryContent = $"Evaluate the resume and generate a response in json as per the instructions fed to you. Do not give me comparison with other Resumes. Stick to the current resume I am feeding you. The job title you need to screen the interviewer and generate the fitment score against is {requestModel.JDTitle}.";
                }
                else if(requestModel.Type == ProfileNames.QualityController)
                {
                    queryContent = $"Generate an output as per the instructions fed to the assistant.";

                }
                var threadRequest = new Thread();
                if (requestModel.Type == ProfileNames.Candidate || requestModel.Type == ProfileNames.Interviewer)
                {
                    threadRequest = new Thread
                    {
                        messages = new List<Message>
                        {
                            new Message { role = "assistant", content = queryContent}
                        },
                        tool_resources = new ToolResources()
                        {
                            file_search = new FileSearch
                            {
                                vector_store_ids = new List<string>() { (requestModel.Type == ProfileNames.Candidate ? candidateResumesVectorStoreId : interviewerResumesVectorStoreId) }
                            }
                        }
                    };
                }
                else if(requestModel.Type == ProfileNames.QualityController)
                {
                    threadRequest = new Thread
                    {
                        messages = new List<Message>
                        {
                            new Message { role = "assistant", content = queryContent}
                        },
                        tool_resources = new ToolResources()
                        {
                            file_search = new FileSearch
                            {
                                vector_store_ids = new List<string>() { (reportQCVectorStoreId) }
                            }
                        }
                    };
                }

                string jsonContent = JsonConvert.SerializeObject(threadRequest);
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
