using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Data.OpenAIModels;
using Risebird.Entities.Enum;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Net.Http;
using System.Text;
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
        private readonly string resumeParserAssistantId;
        private readonly string rubricGeneratorAssistantId;
        private readonly string aiTranscriptAnalysisAssistantId;
        private readonly string stringResumeAnalysisAssistantId;
        private readonly string stringReportQCAssistantId;
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
            resumeParserAssistantId = _configuration["OpenAI:ResumeParserAssistantId"];
            rubricGeneratorAssistantId = _configuration["OpenAI:RubricGeneratorAssistantId"];
            aiTranscriptAnalysisAssistantId = _configuration["OpenAI:TranscriptAnalysisAssistantId"];
            stringResumeAnalysisAssistantId = _configuration["OpenAI:StringResumeAnalysisAssistantId"];
            stringReportQCAssistantId = _configuration["OpenAI:StringReportQCAssistantId"];

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
                else if (requestModel.Type == ProfileNames.ResumeParserAIAssistant)
                {
                    fileId = await UploadFileAsync(requestModel.ResumeLink);
                }
                else
                {
                    fileId = await UploadFileAsync(requestModel.ResumeLink);
                }
                //if (!string.IsNullOrEmpty(fileId))
                //{
                //    await AddFileToVectorStoreAsync(fileId, requestModel.Type);
                //}
                //else
                //{
                //    return null;
                //}
                string threadId = string.Empty;
                if (!string.IsNullOrEmpty(fileId))
                {
                    //await Task.Delay(15000);//60000// try 45000
                    await Task.Delay(30000);//60000// try 45000
                    threadId = await CreateThreadAsync(requestModel, fileId);
                }
                else
                {
                    return null;
                }
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
                    if (messages != null && messages.Data.First().Content.FirstOrDefault().Text.Value != null && (messages.Data.First().Content.FirstOrDefault().Text.Value.ToLower().Contains("persistent issue") || messages.Data.First().Content.FirstOrDefault().Text.Value.ToLower().Contains("error") || messages.Data.First().Content.FirstOrDefault().Text.Value.ToLower().Contains("issue with extracting") || messages.Data.First().Content.FirstOrDefault().Text.Value.ToLower().Contains("it seems")) && !string.IsNullOrEmpty(fileId))
                    {
                        return await RecreateThread(requestModel, fileId, 0);
                    }
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

        private async Task<RunResult> RecreateThread(ScreeningRequest requestModel, string fileId, int? counter)
        {
            if(counter == 0)
            {
                await Task.Delay(15000);
                string threadId = string.Empty;
                if (!string.IsNullOrEmpty(fileId))
                {
                    //await Task.Delay(60000);
                    threadId = await CreateThreadAsync(requestModel, fileId);
                }
                else
                {
                    return null;
                }
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
                    if (messages != null && (messages.Data.First().Content.FirstOrDefault().Text.Value.ToLower().Contains("persistent issue") || messages.Data.First().Content.FirstOrDefault().Text.Value.ToLower().Contains("persistent issue") || messages.Data.First().Content.FirstOrDefault().Text.Value.ToLower().Contains("error") || messages.Data.First().Content.FirstOrDefault().Text.Value.ToLower().Contains("issue with extracting") || messages.Data.First().Content.FirstOrDefault().Text.Value.ToLower().Contains("it seems")) && !string.IsNullOrEmpty(fileId))
                    {
                        counter = counter + 1;
                        messages = await RecreateThread(requestModel, fileId, counter);
                    }
                    return messages;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
            
        }
        private async Task<string> CreateRun(string threadId, string profileType = null)
        {
            try
            {
                HttpClient client = new HttpClient();
                string assistantId = string.Empty;
                if(!string.IsNullOrEmpty(profileType))
                {
                    if (profileType == ProfileNames.Candidate)
                    {
                        assistantId = candidateScreenerAssistantId;
                    }
                    else if (profileType == ProfileNames.Interviewer)
                    {
                        assistantId = interviewerScreenerAssistantId;
                    }
                    else if (profileType == ProfileNames.ResumeParserAIAssistant)
                    {
                        assistantId = resumeParserAssistantId;
                    }
                    else if (profileType == ProfileNames.TranscriptAnalysisAIAssistant)
                    {
                        assistantId = aiTranscriptAnalysisAssistantId;
                    }
                    else if (profileType == ProfileNames.StringResumeAnalysisAIAssistant)
                    {
                        assistantId = stringResumeAnalysisAssistantId;
                    }
                    else if (profileType == ProfileNames.StringReportQCAIAssistant)
                    {
                        assistantId = stringReportQCAssistantId;
                    }
                    else
                    {
                        assistantId = reportQCAssistantId;
                    }

                }
                else
                {
                    assistantId = rubricGeneratorAssistantId;
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
                    Headers = { { "Authorization", $"Bearer {apiKey}" }, { "OpenAI-Beta", "assistants=v2" } }//{ "Content-Type", "application/json" },
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
                    string fcName;
                    try
                    {
                        fcName = url.Split("/").Last().Split("?")[0];
                    }
                    catch (Exception)
                    {

                        fcName = url.Split("/").Last();
                    }
                    if (!string.IsNullOrEmpty(fcName) && fcName.Split(".").Last() != "doc")
                    {
                        var content = new MultipartFormDataContent
                        {
                            { new StringContent("assistants"), "purpose" },
                            { fileContent, "file", fcName }
                        };

                        HttpClient client = new HttpClient();
                        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                        var response = await client.PostAsync($"{baseUrl}/files", content);
                        response.EnsureSuccessStatusCode();

                        var responseContent = await response.Content.ReadAsStringAsync();

                        var fileResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseContent);
                        return fileResponse["id"].ToString();
                    }
                    else
                    {
                        return null;
                    }

                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }


        }

        //private async Task<bool> AddFileToVectorStoreAsync(string fileId, string profileType)
        //{
        //    try
        //    {
        //        HttpClient client = new HttpClient();
        //        var requestBody = new
        //        {
        //            file_id = fileId
        //        };
        //        string vectorStoreId;
        //        if(profileType == ProfileNames.Candidate)
        //        {
        //            vectorStoreId = candidateResumesVectorStoreId;
        //        }
        //        else if(profileType == ProfileNames.Interviewer)
        //        {
        //            vectorStoreId = interviewerResumesVectorStoreId;
        //        }
        //        else
        //        {
        //            vectorStoreId = reportQCVectorStoreId;
        //        }
        //        var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/vector_stores/{vectorStoreId}/files");

        //        // Add headers to the request
        //        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        //        request.Headers.Add("OpenAI-Beta", "assistants=v2");

        //        var jsonContent = JsonConvert.SerializeObject(requestBody);
        //        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        //        //content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        //        request.Content = content;

        //        var response = await client.SendAsync(request);
        //        response.EnsureSuccessStatusCode();

        //        var responseContent = await response.Content.ReadAsStringAsync();
        //        return true;

        //    }
        //    catch (Exception ex)
        //    {
        //        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //        return false;
        //    }

        //}
        private async Task<string> CreateThreadAsync(ScreeningRequest requestModel = null, string fileId = null, string jobTitleForRubrics = null, string transcriptData = null, string resumeStringData = null, string reportStringData = null)
        {
            try
            {

                var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/threads")
                {
                    Headers = { { "Authorization", $"Bearer {apiKey}" }, { "OpenAI-Beta", "assistants=v2" } }//{ "Content-Type", "application/json" }, 
                };

                string queryContent = string.Empty;

                if(requestModel != null)
                {
                    if (requestModel.Type == ProfileNames.Candidate)
                    {
                        queryContent = $"Evaluate the resume and generate a response in json as per the instructions fed to you. Do not give me comparison with other Resumes. Stick to the current resume I am feeding you. The job title you need to screen the candidate and generate the fitment score against is {requestModel.JDTitle}.";
                    }
                    else if (requestModel.Type == ProfileNames.Interviewer)
                    {
                        queryContent = $"Evaluate the resume and generate a response in json as per the instructions fed to you. Do not give me comparison with other Resumes. Stick to the current resume I am feeding you. The job title you need to screen the interviewer and generate the fitment score against is {requestModel.JDTitle}.";
                    }
                    else if (requestModel.Type == ProfileNames.QualityController)
                    {
                        queryContent = $"Generate an output as per the instructions fed to the assistant.";

                    }
                    else if (requestModel.Type == ProfileNames.ResumeParserAIAssistant)
                    {
                        queryContent = $"Evaluate the resume and generate a response in json as per the instructions fed to you.";
                    }

                }
                else if(!string.IsNullOrEmpty(jobTitleForRubrics))//case for generating tech skill rubric
                {
                    queryContent = $"Generate an output as per the instructions fed to the assistant. The job title you need to generate a tech rubric against is {jobTitleForRubrics}.";
                }
                else if(!string.IsNullOrEmpty(transcriptData))
                {
                    queryContent = $"Generate an output as per the instructions fed to the assistant. The transcript data you need to generate an analysis against is {transcriptData}.";

                }
                else if (!string.IsNullOrEmpty(resumeStringData))
                {
                    queryContent = $"Generate an output as per the instructions fed to the assistant. The Resume data you need to generate an analysis against is {resumeStringData}.";

                }
                else if (!string.IsNullOrEmpty(reportStringData))
                {
                    queryContent = $"Generate an output as per the instructions fed to the assistant. The Report data you need to generate an analysis against is {reportStringData}.";

                }
                var threadRequest = new Thread();
                if (requestModel != null && !string.IsNullOrEmpty(fileId))
                {
                    threadRequest = new Thread
                    {
                        messages = new List<Message>
                    {
                        new Message {
                            role = "assistant", content = queryContent,
                            attachments = new List<object>
                            {
                                new
                                {
                                    file_id = fileId, // Optional file ID
                                    tools = new List<object>
                                    {
                                        new
                                        {
                                            type = "code_interpreter" // Required tool type
                                        },
                                        new
                                        {
                                            type = "file_search" // Required tool type
                                        }
                                    }
                                }//,
                                // Additional attachment objects can be added here
                            }
                        }
                    }//,


                        //tool_resources = new ToolResources()
                        //{
                        //    file_search = new FileSearch
                        //    {
                        //        vector_stores = new List<VectorStore>() { new VectorStore { file_ids = new List<string> { fileId } } }

                        //    }
                        //}
                    };
                }
                else
                {
                    threadRequest = new Thread
                    {
                        messages = new List<Message>
                        {
                            new Message {
                                role = "assistant", content = queryContent

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


        private async Task<FilesList> ListFiles()
        {
            try
            {
                HttpClient client = new HttpClient();

                var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/files")
                {
                    Headers = { { "Authorization", $"Bearer {apiKey}" } }//{ "Content-Type", "application/json" },, { "OpenAI-Beta", "assistants=v2" } 
                };

                var response = await client.SendAsync(request);

                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();

                FilesList files = JsonConvert.DeserializeObject<FilesList>(responseContent);
                return files;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<bool> DeleteFiles()
        {
            try
            {
                var filesList = await ListFiles();
                if (filesList != null && filesList.data != null && filesList.data.Count() > 0)
                {

                    //Parallel.ForEach(filesList?.data, item =>
                    //{
                    //    HttpClient client = new HttpClient();

                    //    var request = new HttpRequestMessage(HttpMethod.Delete, $"{baseUrl}/files/{item.id}")
                    //    {
                    //        Headers = { { "Authorization", $"Bearer {apiKey}" }, { "OpenAI-Beta", "assistants=v2" } }//{ "Content-Type", "application/json" }, , { "Content-Type", "application/json" } 
                    //    };

                    //    var response = client.SendAsync(request).Result;

                    //    response.EnsureSuccessStatusCode();
                    //    var responseContent = response.Content.ReadAsStringAsync().Result;

                    //    //FilesList files = JsonConvert.DeserializeObject<FilesList>(responseContent);
                    //    //return files;
                    //}
                    //);
                    foreach (var item in filesList?.data)
                    {
                        try
                        {
                            HttpClient client = new HttpClient();

                            //var request = new HttpRequestMessage(HttpMethod.Delete, $"{baseUrl}/files/{item.id}")
                            //{
                            //    Headers = { { "Authorization", $"Bearer {apiKey}" } }//{ "Content-Type", "application/json" },, { "OpenAI-Beta", "assistants=v2" } 
                            //};

                            var request = new HttpRequestMessage(HttpMethod.Delete, $"{baseUrl}/files/{item.id}")
                            {
                                Headers = { { "Authorization", $"Bearer {apiKey}" }, { "OpenAI-Beta", "assistants=v2" } }//{ "Content-Type", "application/json" }, , { "Content-Type", "application/json" } 
                            };

                            var response = await client.SendAsync(request);

                            response.EnsureSuccessStatusCode();
                            var responseContent = await response.Content.ReadAsStringAsync();

                            //FilesList files = JsonConvert.DeserializeObject<FilesList>(responseContent);
                            //return files;
                        }
                        catch (Exception ex)
                        {
                            await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                        }

                    }
                    await DeleteFiles();
                }

                return true;

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        private async Task<FilesList> ListVectors()
        {
            try
            {
                HttpClient client = new HttpClient();

                var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/vector_stores")
                {
                    Headers = { { "Authorization", $"Bearer {apiKey}" }, { "OpenAI-Beta", "assistants=v2" } }//{ "Content-Type", "application/json" },, { "OpenAI-Beta", "assistants=v2" } 
                };

                var response = await client.SendAsync(request);

                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();

                FilesList vectors = JsonConvert.DeserializeObject<FilesList>(responseContent);//FilesList: used the same type for vector list
                return vectors;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<bool> DeleteVectors()
        {
            try
            {
                var vectorsList = await ListVectors();
                if (vectorsList != null && vectorsList.data != null && vectorsList.data.Count() > 0)
                {
                    Parallel.ForEach(vectorsList?.data, item =>
                    {
                        HttpClient client = new HttpClient();

                        var request = new HttpRequestMessage(HttpMethod.Delete, $"{baseUrl}/vector_stores/{item.id}")
                        {
                            Headers = { { "Authorization", $"Bearer {apiKey}" }, { "OpenAI-Beta", "assistants=v2" } }//{ "Content-Type", "application/json" }, , { "Content-Type", "application/json" } 
                        };

                        var response = client.SendAsync(request).Result;

                        response.EnsureSuccessStatusCode();
                        var responseContent = response.Content.ReadAsStringAsync().Result;

                        //FilesList files = JsonConvert.DeserializeObject<FilesList>(responseContent);
                        //return files;
                    }
                    );
                    //foreach (var item in vectorsList?.data)
                    //{
                    //    HttpClient client = new HttpClient();

                    //    var request = new HttpRequestMessage(HttpMethod.Delete, $"{baseUrl}/vector_stores/{item.id}")
                    //    {
                    //        Headers = { { "Authorization", $"Bearer {apiKey}" }, { "OpenAI-Beta", "assistants=v2" } }//{ "Content-Type", "application/json" }, , { "Content-Type", "application/json" } 
                    //    };

                    //    var response = await client.SendAsync(request);

                    //    response.EnsureSuccessStatusCode();
                    //    var responseContent = await response.Content.ReadAsStringAsync();

                    //    //FilesList files = JsonConvert.DeserializeObject<FilesList>(responseContent);
                    //    //return files;
                    //}
                    await DeleteVectors();
                }

                return true;

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }


        public async Task<RunResult> GenerateTechRubric(string jobTitle)
        {
            try
            {
                
                string threadId = string.Empty;
                threadId = await CreateThreadAsync(jobTitleForRubrics: jobTitle);
                //if (!string.IsNullOrEmpty(fileId))
                //{
                //    await Task.Delay(15000);//60000
                    
                //}
                //else
                //{
                //    return null;
                //}
                string runId;
                //now create a run
                if (!string.IsNullOrEmpty(threadId))
                {
                    runId = await CreateRun(threadId);
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
                        await Task.Delay(4000);
                        runStatus = await RetrieveRun(runId, threadId);
                        if (runStatus == "failed")
                            break;
                    }
                    var messages = await ListRunMessages(threadId);
                    //if (messages != null && messages.Data.First().Content.FirstOrDefault().Text.Value != null && (messages.Data.First().Content.FirstOrDefault().Text.Value.ToLower().Contains("persistent issue") || messages.Data.First().Content.FirstOrDefault().Text.Value.ToLower().Contains("error") || messages.Data.First().Content.FirstOrDefault().Text.Value.ToLower().Contains("issue with extracting") || messages.Data.First().Content.FirstOrDefault().Text.Value.ToLower().Contains("it seems")) && !string.IsNullOrEmpty(fileId))
                    //{
                    //    return await RecreateThread(requestModel, fileId);
                    //}
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

        public async Task<RunResult> GenerateTranscriptAnalysis(string transcriptData)
        {
            try
            {

                string threadId = string.Empty;
                threadId = await CreateThreadAsync(transcriptData: transcriptData);
                //if (!string.IsNullOrEmpty(fileId))
                //{
                //    await Task.Delay(15000);//60000

                //}
                //else
                //{
                //    return null;
                //}
                string runId;
                //now create a run
                if (!string.IsNullOrEmpty(threadId))
                {
                    runId = await CreateRun(threadId, ProfileNames.TranscriptAnalysisAIAssistant);
                }
                else
                {
                    return null;
                }
                await Task.Delay(10000);
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

        public async Task<RunResult> GenerateResumeAnalysis(string resumeStringData)
        {
            try
            {
                string threadId = string.Empty;
                threadId = await CreateThreadAsync(resumeStringData: resumeStringData);             
                string runId;
                //now create a run
                if (!string.IsNullOrEmpty(threadId))
                {
                    runId = await CreateRun(threadId, ProfileNames.StringResumeAnalysisAIAssistant);
                }
                else
                {
                    return null;
                }
                await Task.Delay(15000);
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

        public async Task<RunResult> GenerateReportQCAnalysis(string reportStringData)
        {
            try
            {
                string threadId = string.Empty;
                threadId = await CreateThreadAsync(reportStringData: reportStringData);
                string runId;
                //now create a run
                if (!string.IsNullOrEmpty(threadId))
                {
                    runId = await CreateRun(threadId, ProfileNames.StringReportQCAIAssistant);
                }
                else
                {
                    return null;
                }
                await Task.Delay(15000);
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

        public async Task<RunResult> GenerateTranscriptAnalysis(string transcriptData)
        {
            try
            {

                string threadId = string.Empty;
                threadId = await CreateThreadAsync(transcriptData: transcriptData);
                //if (!string.IsNullOrEmpty(fileId))
                //{
                //    await Task.Delay(15000);//60000

                //}
                //else
                //{
                //    return null;
                //}
                string runId;
                //now create a run
                if (!string.IsNullOrEmpty(threadId))
                {
                    runId = await CreateRun(threadId, ProfileNames.TranscriptAnalysisAIAssistant);
                }
                else
                {
                    return null;
                }
                await Task.Delay(10000);
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

        public async Task<RunResult> GenerateTranscriptAnalysis(string transcriptData)
        {
            try
            {

                string threadId = string.Empty;
                threadId = await CreateThreadAsync(transcriptData: transcriptData);
                //if (!string.IsNullOrEmpty(fileId))
                //{
                //    await Task.Delay(15000);//60000

                //}
                //else
                //{
                //    return null;
                //}
                string runId;
                //now create a run
                if (!string.IsNullOrEmpty(threadId))
                {
                    runId = await CreateRun(threadId, ProfileNames.TranscriptAnalysisAIAssistant);
                }
                else
                {
                    return null;
                }
                await Task.Delay(10000);
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
    }

    public class VectorStore
    {
        public List<string> file_ids { get; set; }

    }
    public class Message
    {
        public string role { get; set; }
        public string content { get; set; }
        //public List<AttachmentObj> attachments { get; set; }
        public List<object> attachments { get; set; }
    }

    public class AttachmentObj
    {
        public string file_id { get; set; }
        public List<VectorStore> vector_stores { get; set; }
    }

    public class Thread
    {
        //public string assistant_id { get; set; }
        public List<Message> messages { get; set; }
        //public ToolResources tool_resources { get; set; }
    }
    public class ToolResources
    {
        public FileSearch file_search { get; set; }
    }
    public class FileSearch
    {
        public List<string> vector_store_ids { get; set; }
        public List<VectorStore> vector_stores { get; set; }
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

    public class FilesList
    {
        public List<FileObj> data { get; set; }

    }

    public class FileObj
    {
        public string id { get; set; }
    }

}
