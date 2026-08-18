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
using System.Text;
using System.Threading.Tasks;
//using Twilio.Http;

namespace Risebird.CommonHelper
{
    public class AssistantServiceHelper : IAssistantServiceHelper
    {
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IOpenAIApiLogger _openAIApiLogger;
        private readonly IOpenAIResponsesHelper _openAIResponsesHelper;
        private readonly IPdfReaderHelper _pdfReaderHelper;
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
        private readonly string stringReportQCAssistantIdStructuredOutput;//gives output in a structured json format as opposed to the other one which takes in a string but gives back a string making it difficult and often an error prone task to extract the verdict.
        private readonly string stringResumeAnalysisAssistantIdV2;
        private readonly string AITranscriptToNotesAssistantId;
        private readonly string QcInterviewAIMergeAssistantId;
        private readonly string ContradictionReportAIAssistantId;
        private readonly string PaymentTicketReviewAssistantId;
        private readonly string LinkedInVerificationAssistantId;

        //private readonly List<string> candidateResumesVectorStoreId;
        public AssistantServiceHelper(
            IApplicationErrorLogger applicationErrorLogger,
            IConfiguration configuration,
            IOpenAIApiLogger openAIApiLogger,
            IOpenAIResponsesHelper openAIResponsesHelper,
            IPdfReaderHelper pdfReaderHelper)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _openAIApiLogger = openAIApiLogger;
            _openAIResponsesHelper = openAIResponsesHelper;
            _pdfReaderHelper = pdfReaderHelper;
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
            stringReportQCAssistantIdStructuredOutput = _configuration["OpenAI:StringReportQCAssistantIdStructuredOutPut"];
            stringResumeAnalysisAssistantIdV2 = _configuration["OpenAI:InterviewerScreenerAssistantIdV2"];
            AITranscriptToNotesAssistantId = _configuration["OpenAI:AITranscriptToNotesAssistantId"];
            QcInterviewAIMergeAssistantId = _configuration["OpenAI:QcInterviewAIMergeAssistantId"];
            ContradictionReportAIAssistantId = _configuration["OpenAI:ContradictionReportAIAssistantId"];
            PaymentTicketReviewAssistantId = _configuration["OpenAI:PaymentTicketReviewAssistantId"];
            LinkedInVerificationAssistantId = _configuration["OpenAI:LinkedInVerificationAssistantId"];
        }

        public async Task<RunResult> ScreenAsync(ScreeningRequest requestModel)
        {
            try
            {
                if (requestModel.Type == ProfileNames.Candidate || requestModel.Type == ProfileNames.Interviewer)
                {
                    var useResponsesApi = _configuration.GetValue("OpenAI:UseResponsesApi", true);
                    if (useResponsesApi)
                    {
                        if (string.IsNullOrEmpty(requestModel.ResumeLink))
                        {
                            return null;
                        }

                        var resumeStringContent = await _pdfReaderHelper.ReadPdfFromUrlAsync(requestModel.ResumeLink);
                        if (string.IsNullOrEmpty(resumeStringContent))
                        {
                            return null;
                        }

                        if (requestModel.Type == ProfileNames.Candidate)
                        {
                            return await GenerateCandidateScreening(resumeStringContent, requestModel.JDTitle);
                        }

                        return await GenerateInterviewerScreening(resumeStringContent, requestModel.JDTitle);
                    }
                }

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
                    int pollInterval = 10000;
                    int maxPollInterval = 30000;
                    int maxAttempts = 12;
                    int attempts = 0;

                    while (runStatus != "completed")
                    {
                        await Task.Delay(pollInterval);
                        runStatus = await RetrieveRun(runId, threadId);
                        attempts++;
                        if (runStatus == "failed" || runStatus == "cancelled" || runStatus == "expired"
                            || runStatus == null || attempts >= maxAttempts)
                            break;
                        pollInterval = Math.Min(pollInterval + 5000, maxPollInterval);
                    }

                    // After run completes, retrieve run steps to capture token usage
                    if (runStatus == "completed")
                    {
                        await RetrieveRunStepsForTokenUsage(runId, threadId);
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
            if (counter == 0)
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
                    int pollInterval = 10000;
                    int maxPollInterval = 30000;
                    int maxAttempts = 12;
                    int attempts = 0;

                    while (runStatus != "completed")
                    {
                        await Task.Delay(pollInterval);
                        runStatus = await RetrieveRun(runId, threadId);
                        attempts++;
                        if (runStatus == "failed" || runStatus == "cancelled" || runStatus == "expired"
                            || runStatus == null || attempts >= maxAttempts)
                            break;
                        pollInterval = Math.Min(pollInterval + 5000, maxPollInterval);
                    }

                    // After run completes, retrieve run steps to capture token usage
                    if (runStatus == "completed")
                    {
                        await RetrieveRunStepsForTokenUsage(runId, threadId);
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
                string operationType = "Unknown";

                if (!string.IsNullOrEmpty(profileType))
                {
                    if (profileType == ProfileNames.Candidate)
                    {
                        assistantId = candidateScreenerAssistantId;
                        operationType = "CandidateScreening";
                    }
                    else if (profileType == ProfileNames.Interviewer)
                    {
                        assistantId = interviewerScreenerAssistantId;
                        operationType = "InterviewerScreening";
                    }
                    else if (profileType == ProfileNames.ResumeParserAIAssistant)
                    {
                        assistantId = resumeParserAssistantId;
                        operationType = "ResumeParsing";
                    }
                    else if (profileType == ProfileNames.ResumeParserPublicUrlAIAssistant)
                    {
                        assistantId = _configuration["OpenAI:ResumeParserPublicUrlAssistantId"];
                        operationType = "ResumeParsingPublicUrl";
                    }
                    else if (profileType == ProfileNames.ResumeProfileSummaryAIAssistant)
                    {
                        assistantId = _configuration["OpenAI:ResumeProfileSummaryAssistantId"];
                        operationType = "ResumeProfileSummary";
                    }
                    else if (profileType == ProfileNames.TranscriptAnalysisAIAssistant)
                    {
                        assistantId = aiTranscriptAnalysisAssistantId;
                        operationType = "TranscriptAnalysis";
                    }
                    else if (profileType == ProfileNames.StringResumeAnalysisAIAssistant)
                    {
                        //assistantId = stringResumeAnalysisAssistantId;
                        assistantId = stringResumeAnalysisAssistantIdV2;
                        operationType = "ResumeAnalysis";
                    }
                    else if (profileType == ProfileNames.StringReportQCAIAssistant)
                    {
                        assistantId = stringReportQCAssistantIdStructuredOutput;//stringReportQCAssistantId;
                        operationType = "ReportQC";
                    }
                    else if (profileType == ProfileNames.TranscriptToNotesAssistantId)
                    {
                        assistantId = AITranscriptToNotesAssistantId;//stringReportQCAssistantId;
                        operationType = "TranscriptToNotes";
                    }
                    else if (profileType == ProfileNames.QcInterviewAIAssistantId)
                    {
                        assistantId = QcInterviewAIMergeAssistantId;
                        operationType = "QcInterviewAIMerge";
                    }
                    else if (profileType == ProfileNames.ContradictionReportAIAssistantId)
                    {
                        assistantId = ContradictionReportAIAssistantId;
                        operationType = "ContradictionReport";
                    }
                    else if (profileType == ProfileNames.PaymentTicketReviewAssistantId)
                    {
                        assistantId = PaymentTicketReviewAssistantId;
                        operationType = "PaymentTicketReview";
                    }
                    else if (profileType == ProfileNames.LinkedInVerificationAssistantId)
                    {
                        assistantId = LinkedInVerificationAssistantId;
                        operationType = "LinkedInVerification";
                    }
                    else
                    {
                        assistantId = reportQCAssistantId;
                        operationType = "QualityCheck";
                    }

                }
                else
                {
                    assistantId = rubricGeneratorAssistantId;
                    operationType = "RubricGeneration";
                }



                // response_format: json_object enforces valid JSON output and reduces output tokens
                // by preventing the model from wrapping JSON in markdown fences.
                // Only applies to assistants whose system prompts mention "JSON" (OpenAI requirement).
                // Excluded for TranscriptToNotes which produces free-form prose, not structured JSON.
                bool useJsonResponseFormat = profileType != ProfileNames.TranscriptToNotesAssistantId;

                object requestBody = useJsonResponseFormat
                    ? (object)new { assistant_id = assistantId, response_format = new { type = "json_object" } }
                    : (object)new { assistant_id = assistantId };

                var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/threads/{threadId}/runs")
                {
                    Headers = { { "Authorization", $"Bearer {apiKey}" }, { "OpenAI-Beta", "assistants=v2" } }//{ "Content-Type", "application/json" },
                };

                var jsonContent = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                request.Content = content;

                var sw = System.Diagnostics.Stopwatch.StartNew();
                var response = await client.SendAsync(request);
                sw.Stop();

                var responseContent = await response.Content.ReadAsStringAsync();

                // Extract model from response
                string modelName = null;
                string runIdFromResponse = null;
                try
                {
                    var runResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseContent);
                    if (runResponse != null)
                    {
                        modelName = runResponse.ContainsKey("model") ? runResponse["model"]?.ToString() : null;
                        runIdFromResponse = runResponse.ContainsKey("id") ? runResponse["id"]?.ToString() : null;
                    }
                }
                catch { /* Continue even if extraction fails */ }

                // Log the API call
                await _openAIApiLogger.LogApiCallAsync(
                    apiEndpoint: $"{baseUrl}/threads/{threadId}/runs",
                    apiMethod: "POST",
                    requestPayload: jsonContent,
                    responsePayload: responseContent,
                    responseTimeMs: (int)sw.ElapsedMilliseconds,
                    statusCode: (int)response.StatusCode,
                    isSuccess: response.IsSuccessStatusCode,
                    errorMessage: response.IsSuccessStatusCode ? null : responseContent,
                    model: modelName,
                    assistantId: assistantId,
                    threadId: threadId,
                    runId: runIdFromResponse,
                    fileId: null,
                    context: new OpenAIApiLogContext
                    {
                        OperationType = operationType
                    });

                response.EnsureSuccessStatusCode();
                return runIdFromResponse;
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

                var sw = System.Diagnostics.Stopwatch.StartNew();
                var response = await client.SendAsync(request);
                sw.Stop();

                var responseContent = await response.Content.ReadAsStringAsync();

                // Extract model and assistant from response
                string modelName = null;
                string assistantIdFromResponse = null;
                string statusFromResponse = null;
                try
                {
                    var runResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseContent);
                    if (runResponse != null)
                    {
                        modelName = runResponse.ContainsKey("model") ? runResponse["model"]?.ToString() : null;
                        assistantIdFromResponse = runResponse.ContainsKey("assistant_id") ? runResponse["assistant_id"]?.ToString() : null;
                        statusFromResponse = runResponse.ContainsKey("status") ? runResponse["status"]?.ToString() : null;
                        var failedAt = runResponse["failed_at"];
                        if (failedAt != null)
                        {
                            statusFromResponse = "failed";
                        }
                    }
                }
                catch { /* Continue even if extraction fails */ }

                // Log the API call
                await _openAIApiLogger.LogApiCallAsync(
                    apiEndpoint: $"{baseUrl}/threads/{threadId}/runs/{runId}",
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
                        OperationType = "RunRetrieval"
                    });

                response.EnsureSuccessStatusCode();
                return statusFromResponse;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }

        }

        private async Task RetrieveRunStepsForTokenUsage(string runId, string threadId)
        {
            try
            {
                HttpClient client = new HttpClient();

                var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/threads/{threadId}/runs/{runId}/steps")
                {
                    Headers = { { "Authorization", $"Bearer {apiKey}" }, { "OpenAI-Beta", "assistants=v2" } }
                };

                var sw = System.Diagnostics.Stopwatch.StartNew();
                var response = await client.SendAsync(request);
                sw.Stop();

                var responseContent = await response.Content.ReadAsStringAsync();

                // Extract model from the parent run context if available
                string modelName = null;
                string assistantIdFromResponse = null;
                try
                {
                    var json = JObject.Parse(responseContent);
                    var dataArray = json["data"] as JArray;
                    if (dataArray != null && dataArray.Count > 0)
                    {
                        var firstStep = dataArray[0] as JObject;
                        // The model might be in step details
                        modelName = firstStep?["model"]?.ToString();
                        assistantIdFromResponse = firstStep?["assistant_id"]?.ToString();
                    }
                }
                catch { /* Continue even if extraction fails */ }

                // Log the API call - this will extract token usage from run steps
                await _openAIApiLogger.LogApiCallAsync(
                    apiEndpoint: $"{baseUrl}/threads/{threadId}/runs/{runId}/steps",
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
                        OperationType = "RunStepsRetrieval-TokenUsage"
                    });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
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

                var sw = System.Diagnostics.Stopwatch.StartNew();
                var response = await client.SendAsync(request);
                sw.Stop();

                var responseContent = await response.Content.ReadAsStringAsync();

                // Log the API call
                await _openAIApiLogger.LogApiCallAsync(
                    apiEndpoint: $"{baseUrl}/threads/{threadId}/messages",
                    apiMethod: "GET",
                    requestPayload: null,
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
                        OperationType = "MessageRetrieval"
                    });

                response.EnsureSuccessStatusCode();

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

                        var sw = System.Diagnostics.Stopwatch.StartNew();
                        var response = await client.PostAsync($"{baseUrl}/files", content);
                        sw.Stop();

                        var responseContent = await response.Content.ReadAsStringAsync();

                        // Log the API call
                        await _openAIApiLogger.LogApiCallAsync(
                            apiEndpoint: $"{baseUrl}/files",
                            apiMethod: "POST",
                            requestPayload: $"{{\"purpose\":\"assistants\",\"file\":\"{fcName}\"}}",
                            responsePayload: responseContent,
                            responseTimeMs: (int)sw.ElapsedMilliseconds,
                            statusCode: (int)response.StatusCode,
                            isSuccess: response.IsSuccessStatusCode,
                            errorMessage: response.IsSuccessStatusCode ? null : responseContent,
                            model: null,
                            assistantId: null,
                            threadId: null,
                            runId: null,
                            fileId: null,
                            context: new OpenAIApiLogContext
                            {
                                OperationType = "FileUpload"
                            });

                        response.EnsureSuccessStatusCode();

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
        private async Task<string> CreateThreadAsync(ScreeningRequest requestModel = null, string fileId = null, string jobTitleForRubrics = null, string transcriptData = null, string resumeStringData = null, string reportStringData = null, string jdTitle = null, string resumeStringDataForParsing = null, string transcripttonotes = null, string plainAssistantMessage = null, string contradictiontranscriptSummary = null)
        {
            try
            {
                HttpClient client = new HttpClient();

                var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/threads")
                {
                    Headers = { { "Authorization", $"Bearer {apiKey}" }, { "OpenAI-Beta", "assistants=v2" } }//{ "Content-Type", "application/json" }, 
                };

                string queryContent = string.Empty;

                if (requestModel != null)
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
                else if (!string.IsNullOrWhiteSpace(plainAssistantMessage))
                {
                    queryContent = plainAssistantMessage;
                }
                else if (!string.IsNullOrWhiteSpace(contradictiontranscriptSummary))
                {
                    queryContent = contradictiontranscriptSummary;
                }
                else if (!string.IsNullOrEmpty(jobTitleForRubrics))//case for generating tech skill rubric
                {
                    queryContent = $"Generate an output as per the instructions fed to the assistant. The job title you need to generate a tech rubric against is {jobTitleForRubrics}.";
                }
                else if (!string.IsNullOrEmpty(transcriptData))
                {
                    queryContent = $"Generate an output as per the instructions fed to the assistant. The transcript data you need to generate an analysis against is {transcriptData}.";

                }
                else if (!string.IsNullOrEmpty(resumeStringData))
                {
                    queryContent = $"Generate an output as per the instructions fed to the assistant. The job title you need to screen the interviewer and generate the fitment score against is {jdTitle}. The Resume data you need to generate an analysis against is {resumeStringData}.";

                }
                else if (!string.IsNullOrEmpty(reportStringData))
                {
                    queryContent = $"Generate an output as per the instructions fed to the assistant. The Report data you need to generate an analysis against is {reportStringData}.";

                }
                else if (!string.IsNullOrEmpty(resumeStringDataForParsing))
                {
                    queryContent = $"Evaluate the resume and generate a response in json as per the instructions fed to you.";

                }
                else if (!string.IsNullOrEmpty(transcripttonotes))
                {
                    queryContent = $"Generate the Summary for {transcripttonotes}";

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

                var sw = System.Diagnostics.Stopwatch.StartNew();
                var response = await client.SendAsync(request);
                sw.Stop();

                var responseContent = await response.Content.ReadAsStringAsync();

                // Log the API call
                await _openAIApiLogger.LogApiCallAsync(
                    apiEndpoint: $"{baseUrl}/threads",
                    apiMethod: "POST",
                    requestPayload: jsonContent,
                    responsePayload: responseContent,
                    responseTimeMs: (int)sw.ElapsedMilliseconds,
                    statusCode: (int)response.StatusCode,
                    isSuccess: response.IsSuccessStatusCode,
                    errorMessage: response.IsSuccessStatusCode ? null : responseContent,
                    model: null,
                    assistantId: null,
                    threadId: null,
                    runId: null,
                    fileId: null,
                    context: new OpenAIApiLogContext
                    {
                        OperationType = "ThreadCreation"
                    });

                response.EnsureSuccessStatusCode();

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

                var sw = System.Diagnostics.Stopwatch.StartNew();
                var response = await client.SendAsync(request);
                sw.Stop();

                var responseContent = await response.Content.ReadAsStringAsync();

                // Log the API call
                await _openAIApiLogger.LogApiCallAsync(
                    apiEndpoint: $"{baseUrl}/files",
                    apiMethod: "GET",
                    requestPayload: null,
                    responsePayload: responseContent,
                    responseTimeMs: (int)sw.ElapsedMilliseconds,
                    statusCode: (int)response.StatusCode,
                    isSuccess: response.IsSuccessStatusCode,
                    errorMessage: response.IsSuccessStatusCode ? null : responseContent,
                    model: null,
                    assistantId: null,
                    threadId: null,
                    runId: null,
                    fileId: null,
                    context: new OpenAIApiLogContext
                    {
                        OperationType = "FileList"
                    });

                response.EnsureSuccessStatusCode();

                FilesList files = JsonConvert.DeserializeObject<FilesList>(responseContent);
                return files;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<bool> DeleteFiles(int maxIterations = 10)
        {
            try
            {
                int iteration = 0;
                int totalDeleted = 0;

                // Loop with circuit breaker to prevent infinite recursion
                while (iteration < maxIterations)
                {
                    var filesList = await ListFiles();

                    // No more files to delete
                    if (filesList == null || filesList.data == null || filesList.data.Count() == 0)
                    {
                        return true;
                    }

                    // Delete all files in current batch
                    foreach (var item in filesList.data)
                    {
                        try
                        {
                            HttpClient client = new HttpClient();

                            var request = new HttpRequestMessage(HttpMethod.Delete, $"{baseUrl}/files/{item.id}")
                            {
                                Headers = { { "Authorization", $"Bearer {apiKey}" }, { "OpenAI-Beta", "assistants=v2" } }
                            };

                            var sw = System.Diagnostics.Stopwatch.StartNew();
                            var response = await client.SendAsync(request);
                            sw.Stop();

                            var responseContent = await response.Content.ReadAsStringAsync();

                            // Log the API call
                            await _openAIApiLogger.LogApiCallAsync(
                                apiEndpoint: $"{baseUrl}/files/{item.id}",
                                apiMethod: "DELETE",
                                requestPayload: null,
                                responsePayload: responseContent,
                                responseTimeMs: (int)sw.ElapsedMilliseconds,
                                statusCode: (int)response.StatusCode,
                                isSuccess: response.IsSuccessStatusCode,
                                errorMessage: response.IsSuccessStatusCode ? null : responseContent,
                                model: null,
                                assistantId: null,
                                threadId: null,
                                runId: null,
                                fileId: item.id,
                                context: new OpenAIApiLogContext
                                {
                                    OperationType = "FileDelete"
                                });

                            response.EnsureSuccessStatusCode();

                            totalDeleted++;
                        }
                        catch (Exception ex)
                        {
                            await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                        }
                    }

                    iteration++;
                }

                // Log if we hit the max iterations (potential issue)
                if (iteration >= maxIterations)
                {
                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Warning,
                        new Exception($"DeleteFiles: Hit max iterations ({maxIterations}). Deleted {totalDeleted} files. More files may still exist."));
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

                var sw = System.Diagnostics.Stopwatch.StartNew();
                var response = await client.SendAsync(request);
                sw.Stop();

                var responseContent = await response.Content.ReadAsStringAsync();

                // Log the API call
                await _openAIApiLogger.LogApiCallAsync(
                    apiEndpoint: $"{baseUrl}/vector_stores",
                    apiMethod: "GET",
                    requestPayload: null,
                    responsePayload: responseContent,
                    responseTimeMs: (int)sw.ElapsedMilliseconds,
                    statusCode: (int)response.StatusCode,
                    isSuccess: response.IsSuccessStatusCode,
                    errorMessage: response.IsSuccessStatusCode ? null : responseContent,
                    model: null,
                    assistantId: null,
                    threadId: null,
                    runId: null,
                    fileId: null,
                    context: new OpenAIApiLogContext
                    {
                        OperationType = "VectorStoreList"
                    });

                response.EnsureSuccessStatusCode();

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
                        var sw = System.Diagnostics.Stopwatch.StartNew();
                        HttpClient client = new HttpClient();

                        var request = new HttpRequestMessage(HttpMethod.Delete, $"{baseUrl}/vector_stores/{item.id}")
                        {
                            Headers = { { "Authorization", $"Bearer {apiKey}" }, { "OpenAI-Beta", "assistants=v2" } }//{ "Content-Type", "application/json" }, , { "Content-Type", "application/json" } 
                        };

                        var response = client.SendAsync(request).Result;
                        sw.Stop();

                        var responseContent = response.Content.ReadAsStringAsync().Result;

                        // Log the API call (using GetAwaiter().GetResult() for sync context)
                        _openAIApiLogger.LogApiCallAsync(
                            apiEndpoint: $"{baseUrl}/vector_stores/{item.id}",
                            apiMethod: "DELETE",
                            requestPayload: null,
                            responsePayload: responseContent,
                            responseTimeMs: (int)sw.ElapsedMilliseconds,
                            statusCode: (int)response.StatusCode,
                            isSuccess: response.IsSuccessStatusCode,
                            errorMessage: response.IsSuccessStatusCode ? null : responseContent,
                            model: null,
                            assistantId: null,
                            threadId: null,
                            runId: null,
                            fileId: null,
                            context: new OpenAIApiLogContext
                            {
                                OperationType = "VectorStoreDelete"
                            }).GetAwaiter().GetResult();

                        response.EnsureSuccessStatusCode();

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


        //public async Task<RunResult> GenerateTechRubric(string jobTitle)
        //{
        //    try
        //    {
        //        var useResponsesApi = _configuration.GetValue("OpenAI:UseResponsesApi", true);
        //        if (useResponsesApi)
        //        {
        //            var input = $"Generate an output as per the instructions fed to the assistant. The job title you need to generate a tech rubric against is {jobTitle}.";
        //            return await _openAIResponsesHelper.CreateJsonResponseAsync(
        //                input,
        //                operationType: "RubricGeneration",
        //                promptId: _configuration["OpenAI:RubricGeneratorPromptId"]);
        //        }

        //        string threadId = string.Empty;
        //        threadId = await CreateThreadAsync(jobTitleForRubrics: jobTitle);
        //        //if (!string.IsNullOrEmpty(fileId))
        //        //{
        //        //    await Task.Delay(15000);//60000

        //        //}
        //        //else
        //        //{
        //        //    return null;
        //        //}
        //        string runId;
        //        //now create a run
        //        if (!string.IsNullOrEmpty(threadId))
        //        {
        //            runId = await CreateRun(threadId);
        //        }
        //        else
        //        {
        //            return null;
        //        }

        //        if (!string.IsNullOrEmpty(runId))
        //        {
        //            string runStatus = string.Empty;
        //            int pollInterval = 10000;
        //            int maxPollInterval = 30000;
        //            int maxAttempts = 12;
        //            int attempts = 0;

        //            while (runStatus != "completed")
        //            {
        //                await Task.Delay(pollInterval);
        //                runStatus = await RetrieveRun(runId, threadId);
        //                attempts++;
        //                if (runStatus == "failed" || runStatus == "cancelled" || runStatus == "expired"
        //                    || runStatus == null || attempts >= maxAttempts)
        //                    break;
        //                pollInterval = Math.Min(pollInterval + 5000, maxPollInterval);
        //            }

        //            // After run completes, retrieve run steps to capture token usage
        //            if (runStatus == "completed")
        //            {
        //                await RetrieveRunStepsForTokenUsage(runId, threadId);
        //            }

        //            var messages = await ListRunMessages(threadId);
        //            //if (messages != null && messages.Data.First().Content.FirstOrDefault().Text.Value != null && (messages.Data.First().Content.FirstOrDefault().Text.Value.ToLower().Contains("persistent issue") || messages.Data.First().Content.FirstOrDefault().Text.Value.ToLower().Contains("error") || messages.Data.First().Content.FirstOrDefault().Text.Value.ToLower().Contains("issue with extracting") || messages.Data.First().Content.FirstOrDefault().Text.Value.ToLower().Contains("it seems")) && !string.IsNullOrEmpty(fileId))
        //            //{
        //            //    return await RecreateThread(requestModel, fileId);
        //            //}
        //            return messages;
        //        }
        //        else
        //        {
        //            return null;
        //        }


        //    }
        //    catch (Exception ex)
        //    {
        //        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //        return null;
        //    }
        //}
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
                    int pollInterval = 10000;
                    int maxPollInterval = 30000;
                    int maxAttempts = 12;
                    int attempts = 0;

                    while (runStatus != "completed")
                    {
                        await Task.Delay(pollInterval);
                        runStatus = await RetrieveRun(runId, threadId);
                        attempts++;
                        if (runStatus == "failed" || runStatus == "cancelled" || runStatus == "expired"
                            || runStatus == null || attempts >= maxAttempts)
                            break;
                        pollInterval = Math.Min(pollInterval + 5000, maxPollInterval);
                    }

                    // After run completes, retrieve run steps to capture token usage
                    if (runStatus == "completed")
                    {
                        await RetrieveRunStepsForTokenUsage(runId, threadId);
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

        public async Task<RunResult> GenerateTechRubricRevision(string revisionPrompt)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(revisionPrompt))
                    return null;

                var useResponsesApi = _configuration.GetValue("OpenAI:UseResponsesApi", true);
                if (useResponsesApi)
                {
                    return await _openAIResponsesHelper.CreateJsonResponseAsync(
                        revisionPrompt,
                        operationType: "RubricRevision",
                        promptId: _configuration["OpenAI:RubricGeneratorPromptId"]);
                }

                string threadId = await CreateThreadAsync(plainAssistantMessage: revisionPrompt);
                if (string.IsNullOrEmpty(threadId))
                    return null;

                string runId = await CreateRun(threadId);
                if (string.IsNullOrEmpty(runId))
                    return null;

                string runStatus = string.Empty;
                int pollInterval = 10000;
                int maxPollInterval = 30000;
                int maxAttempts = 12;
                int attempts = 0;

                while (runStatus != "completed")
                {
                    await Task.Delay(pollInterval);
                    runStatus = await RetrieveRun(runId, threadId);
                    attempts++;
                    if (runStatus == "failed" || runStatus == "cancelled" || runStatus == "expired"
                        || runStatus == null || attempts >= maxAttempts)
                        break;
                    pollInterval = Math.Min(pollInterval + 5000, maxPollInterval);
                }

                if (runStatus == "completed")
                    await RetrieveRunStepsForTokenUsage(runId, threadId);

                return await ListRunMessages(threadId);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<RunResult> GenerateRubricQuestions(string rubricInput)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(rubricInput))
                    return null;

                var useResponsesApi = _configuration.GetValue("OpenAI:UseResponsesApi", true);
                if (useResponsesApi)
                {
                    return await _openAIResponsesHelper.CreateJsonResponseAsync(
                        rubricInput,
                        operationType: "RubricQuestionGeneration",
                        promptId: _configuration["OpenAI:RubricQuestionPromptId"]);
                }

                return null;
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
                var useResponsesApi = _configuration.GetValue("OpenAI:UseResponsesApi", true);
                if (useResponsesApi)
                {
                    var input = $"Generate an output in JSON format as per the instructions fed to the assistant. The transcript data you need to generate an analysis against is {transcriptData}.";
                    return await _openAIResponsesHelper.CreateJsonResponseAsync(
                        input,
                        operationType: "TranscriptAnalysis",
                        promptId: _configuration["OpenAI:TranscriptAnalysisPromptId"]);
                }

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
                    int pollInterval = 10000;
                    int maxPollInterval = 30000;
                    int maxAttempts = 12;
                    int attempts = 0;

                    while (runStatus != "completed")
                    {
                        await Task.Delay(pollInterval);
                        runStatus = await RetrieveRun(runId, threadId);
                        attempts++;
                        if (runStatus == "failed" || runStatus == "cancelled" || runStatus == "expired"
                            || runStatus == null || attempts >= maxAttempts)
                            break;
                        pollInterval = Math.Min(pollInterval + 5000, maxPollInterval);
                    }

                    // After run completes, retrieve run steps to capture token usage
                    if (runStatus == "completed")
                    {
                        await RetrieveRunStepsForTokenUsage(runId, threadId);
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
        public async Task<RunResult> GenerateQcAnalysisInterviewAIMerge(string userMessageContent, string assistantId, bool useJsonResponseFormat = true)
        {
            try
            {
                if (string.IsNullOrEmpty(userMessageContent))
                    return null;

                var useResponsesApi = _configuration.GetValue("OpenAI:UseResponsesApi", true);
                if (useResponsesApi)
                {
                    var input = $"Generate an output in JSON format as per the instructions fed to the assistant. {userMessageContent}";
                    return await _openAIResponsesHelper.CreateJsonResponseAsync(
                        input,
                        operationType: "QcInterviewAIMerge",
                        promptId: _configuration["OpenAI:QcInterviewAIMergePromptId"]);
                }

                if (string.IsNullOrEmpty(assistantId))
                    return null;

                string threadId = await CreateThreadAsync(plainAssistantMessage: userMessageContent);
                if (string.IsNullOrEmpty(threadId))
                    return null;

                string runId = await CreateRun(threadId, ProfileNames.QcInterviewAIAssistantId);
                await Task.Delay(10000);
                if (string.IsNullOrEmpty(runId))
                    return null;

                string runStatus = string.Empty;
                int pollInterval = 10000;
                int maxPollInterval = 30000;
                int maxAttempts = 12;
                int attempts = 0;
                while (runStatus != "completed")
                {
                    await Task.Delay(pollInterval);
                    runStatus = await RetrieveRun(runId, threadId);
                    attempts++;
                    if (runStatus == "failed" || runStatus == "cancelled" || runStatus == "expired"
                        || runStatus == null || attempts >= maxAttempts)
                        break;
                    pollInterval = Math.Min(pollInterval + 5000, maxPollInterval);
                }

                if (runStatus == "completed")
                    await RetrieveRunStepsForTokenUsage(runId, threadId);

                var messages = await ListRunMessages(threadId);
                return messages;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<RunResult> GeneratecontradictionReport(string transcriptSummary)
        {
            try
            {
                var useResponsesApi = _configuration.GetValue("OpenAI:UseResponsesApi", true);
                if (useResponsesApi)
                {
                    var input = $"Generate an output in JSON format as per the instructions fed to the assistant. {transcriptSummary}";
                    return await _openAIResponsesHelper.CreateJsonResponseAsync(
                        input,
                        operationType: "ContradictionReport",
                        promptId: _configuration["OpenAI:ContradictionReportAIPromptId"]);
                }

                string threadId = await CreateThreadAsync(contradictiontranscriptSummary: transcriptSummary);
                if (string.IsNullOrEmpty(threadId))
                    return null;

                string runId = await CreateRun(threadId, ProfileNames.ContradictionReportAIAssistantId);
                await Task.Delay(10000);
                if (string.IsNullOrEmpty(runId))
                    return null;

                string runStatus = string.Empty;
                int pollInterval = 10000;
                int maxPollInterval = 30000;
                int maxAttempts = 12;
                int attempts = 0;
                while (runStatus != "completed")
                {
                    await Task.Delay(pollInterval);
                    runStatus = await RetrieveRun(runId, threadId);
                    attempts++;
                    if (runStatus == "failed" || runStatus == "cancelled" || runStatus == "expired"
                        || runStatus == null || attempts >= maxAttempts)
                        break;
                    pollInterval = Math.Min(pollInterval + 5000, maxPollInterval);
                }

                if (runStatus == "completed")
                    await RetrieveRunStepsForTokenUsage(runId, threadId);

                var messages = await ListRunMessages(threadId);
                return messages;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<RunResult> ParseResume(string resumeData, bool usePublicUrlParser = false, bool useProfileSummary = false)
        {
            try
            {
                var useResponsesApi = _configuration.GetValue("OpenAI:UseResponsesApi", true);
                if (useResponsesApi)
                {
                    string input;
                    string operationType;
                    string promptConfigKey;
                    if (useProfileSummary)
                    {
                        input = $"Evaluate the resume and generate a response in JSON format as per the instructions fed to you. The resume data you need to generate a profile summary against is {resumeData}.";
                        operationType = "ResumeProfileSummary";
                        promptConfigKey = "OpenAI:ResumeProfileSummaryPromptId";
                    }
                    else if (usePublicUrlParser)
                    {
                        input = $"Evaluate the resume and generate a response in JSON format as per the instructions fed to you. The resume public URL you need to parse is {resumeData}.";
                        operationType = "ResumeParsingPublicUrl";
                        promptConfigKey = "OpenAI:ResumeParserPublicUrlPromptId";
                    }
                    else
                    {
                        input = $"Evaluate the resume and generate a response in JSON format as per the instructions fed to you. The resume data you need to parse is {resumeData}.";
                        operationType = "ResumeParsing";
                        promptConfigKey = "OpenAI:ResumeParserPromptId";
                    }

                    return await _openAIResponsesHelper.CreateJsonResponseAsync(
                        input,
                        operationType: operationType,
                        promptId: _configuration[promptConfigKey]);
                }

                string threadId = string.Empty;
                threadId = await CreateThreadAsync(resumeStringDataForParsing: resumeData);

                string runId;
                //now create a run
                if (!string.IsNullOrEmpty(threadId))
                {
                    string profileType = ProfileNames.ResumeParserAIAssistant;
                    if (useProfileSummary)
                    {
                        profileType = ProfileNames.ResumeProfileSummaryAIAssistant;
                    }
                    else if (usePublicUrlParser)
                    {
                        profileType = ProfileNames.ResumeParserPublicUrlAIAssistant;
                    }

                    runId = await CreateRun(threadId, profileType);
                }
                else
                {
                    return null;
                }
                await Task.Delay(10000);
                if (!string.IsNullOrEmpty(runId))
                {
                    string runStatus = string.Empty;
                    int pollInterval = 10000;
                    int maxPollInterval = 30000;
                    int maxAttempts = 12;
                    int attempts = 0;

                    while (runStatus != "completed")
                    {
                        await Task.Delay(pollInterval);
                        runStatus = await RetrieveRun(runId, threadId);
                        attempts++;
                        if (runStatus == "failed" || runStatus == "cancelled" || runStatus == "expired"
                            || runStatus == null || attempts >= maxAttempts)
                            break;
                        pollInterval = Math.Min(pollInterval + 5000, maxPollInterval);
                    }

                    // After run completes, retrieve run steps to capture token usage
                    if (runStatus == "completed")
                    {
                        await RetrieveRunStepsForTokenUsage(runId, threadId);
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

        public async Task<RunResult> GenerateCandidateScreening(string resumeStringData, string jobTitle, bool useResumeParserCopyPrompt = false)
        {
            try
            {
                var useResponsesApi = _configuration.GetValue("OpenAI:UseResponsesApi", true);
                if (useResponsesApi)
                {
                    var input = useResumeParserCopyPrompt
                        ? $"Generate an output in JSON format as per the instructions fed to the assistant. The job title is {jobTitle} and the resume string is {resumeStringData}."
                        : $"Evaluate the resume and generate a response in JSON format as per the instructions fed to you. Do not give me comparison with other Resumes. Stick to the current resume I am feeding you. The job title you need to screen the candidate and generate the fitment score against is {jobTitle}. The resume data you need to generate an analysis against is {resumeStringData}.";
                    return await _openAIResponsesHelper.CreateJsonResponseAsync(
                        input,
                        operationType: useResumeParserCopyPrompt ? "CandidateScreeningV2" : "CandidateScreening",
                        promptId: _configuration[useResumeParserCopyPrompt
                            ? "OpenAI:ResumeParserCopyPromptId"
                            : "OpenAI:CandidateScreenerPromptId"]);
                }

                return null;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<RunResult> GenerateInterviewerScreening(string resumeStringData, string jobTitle)
        {
            try
            {
                var useResponsesApi = _configuration.GetValue("OpenAI:UseResponsesApi", true);
                if (useResponsesApi)
                {
                    var input = $"Evaluate the resume and generate a response in JSON format as per the instructions fed to you. Do not give me comparison with other Resumes. Stick to the current resume I am feeding you. The job title you need to screen the interviewer and generate the fitment score against is {jobTitle}. The resume data you need to generate an analysis against is {resumeStringData}.";
                    return await _openAIResponsesHelper.CreateJsonResponseAsync(
                        input,
                        operationType: "InterviewerScreening",
                        promptId: _configuration["OpenAI:InterviewerScreenerPromptId"]);
                }

                return null;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<RunResult> GenerateResumeAnalysis(string resumeStringData, string jobTitle)
        {
            try
            {
                var useResponsesApi = _configuration.GetValue("OpenAI:UseResponsesApi", true);
                if (useResponsesApi)
                {
                    var input = $"Generate an output in JSON format as per the instructions fed to the assistant. The job title you need to screen the interviewer and generate the fitment score against is {jobTitle}. The Resume data you need to generate an analysis against is {resumeStringData}.";
                    return await _openAIResponsesHelper.CreateJsonResponseAsync(
                        input,
                        operationType: "ResumeAnalysis",
                        promptId: _configuration["OpenAI:StringResumeAnalysisPromptId"]);
                }

                string threadId = string.Empty;
                threadId = await CreateThreadAsync(resumeStringData: resumeStringData, jdTitle: jobTitle);
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
                await Task.Delay(5000);
                if (!string.IsNullOrEmpty(runId))
                {
                    string runStatus = string.Empty;
                    int pollInterval = 10000;
                    int maxPollInterval = 30000;
                    int maxAttempts = 12;
                    int attempts = 0;

                    while (runStatus != "completed")
                    {
                        await Task.Delay(pollInterval);
                        runStatus = await RetrieveRun(runId, threadId);
                        attempts++;
                        if (runStatus == "failed" || runStatus == "cancelled" || runStatus == "expired"
                            || runStatus == null || attempts >= maxAttempts)
                            break;
                        pollInterval = Math.Min(pollInterval + 5000, maxPollInterval);
                    }

                    // After run completes, retrieve run steps to capture token usage
                    if (runStatus == "completed")
                    {
                        await RetrieveRunStepsForTokenUsage(runId, threadId);
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
                var useResponsesApi = _configuration.GetValue("OpenAI:UseResponsesApi", true);
                if (useResponsesApi)
                {
                    var input = $"Generate an output in JSON format as per the instructions fed to the assistant. The Report data you need to generate an analysis against is {reportStringData}.";
                    return await _openAIResponsesHelper.CreateJsonResponseAsync(
                        input,
                        operationType: "ReportQC",
                        promptId: _configuration["OpenAI:ReportQCPromptId"]);
                }

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
                    int pollInterval = 10000;
                    int maxPollInterval = 30000;
                    int maxAttempts = 12;
                    int attempts = 0;

                    while (runStatus != "completed")
                    {
                        await Task.Delay(pollInterval);
                        runStatus = await RetrieveRun(runId, threadId);
                        attempts++;
                        if (runStatus == "failed" || runStatus == "cancelled" || runStatus == "expired"
                            || runStatus == null || attempts >= maxAttempts)
                            break;
                        pollInterval = Math.Min(pollInterval + 5000, maxPollInterval);
                    }

                    // After run completes, retrieve run steps to capture token usage
                    if (runStatus == "completed")
                    {
                        await RetrieveRunStepsForTokenUsage(runId, threadId);
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
        public async Task<RunResult> TranscriptToNotes(string transcriptdata)
        {
            try
            {
                var useResponsesApi = _configuration.GetValue("OpenAI:UseResponsesApi", true);
                if (useResponsesApi)
                {
                    var input = $"Generate the Summary for {transcriptdata}";
                    return await _openAIResponsesHelper.CreateTextResponseAsync(
                        input,
                        operationType: "TranscriptToNotes",
                        promptId: _configuration["OpenAI:TranscriptToNotesPromptId"]);
                }

                string threadId = string.Empty;
                threadId = await CreateThreadAsync(transcripttonotes: transcriptdata);

                string runId;
                //now create a run
                if (!string.IsNullOrEmpty(threadId))
                {
                    runId = await CreateRun(threadId, ProfileNames.TranscriptToNotesAssistantId);
                }
                else
                {
                    return null;
                }
                await Task.Delay(10000);
                if (!string.IsNullOrEmpty(runId))
                {
                    string runStatus = string.Empty;
                    int pollInterval = 10000;
                    int maxPollInterval = 30000;
                    int maxAttempts = 12;
                    int attempts = 0;

                    while (runStatus != "completed")
                    {
                        await Task.Delay(pollInterval);
                        runStatus = await RetrieveRun(runId, threadId);
                        attempts++;
                        if (runStatus == "failed" || runStatus == "cancelled" || runStatus == "expired"
                            || runStatus == null || attempts >= maxAttempts)
                            break;
                        pollInterval = Math.Min(pollInterval + 5000, maxPollInterval);
                    }

                    // After run completes, retrieve run steps to capture token usage
                    if (runStatus == "completed")
                    {
                        await RetrieveRunStepsForTokenUsage(runId, threadId);
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

        /// <summary>
        /// Calls assistant asst_kjUOaWfVxQOymoAN1m29MrR8 (Payment Ticket Review) directly
        /// with a plain-text composed message and returns the assistant reply.
        /// </summary>
        public async Task<RunResult> GeneratePaymentTicketReview(string composedMessage)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(composedMessage))
                    return null;

                var useResponsesApi = _configuration.GetValue("OpenAI:UseResponsesApi", true);
                if (useResponsesApi)
                {
                    var input = $"Generate an output in JSON format as per the instructions fed to the assistant. {composedMessage}";
                    return await _openAIResponsesHelper.CreateJsonResponseAsync(
                        input,
                        operationType: "PaymentTicketReview",
                        promptId: _configuration["OpenAI:PaymentTicketReviewPromptId"]);
                }

                string threadId = await CreateThreadAsync(plainAssistantMessage: composedMessage);

                string runId;
                if (!string.IsNullOrEmpty(threadId))
                {
                    runId = await CreateRun(threadId, ProfileNames.PaymentTicketReviewAssistantId);
                }
                else
                {
                    return null;
                }
                await Task.Delay(10000);
                if (string.IsNullOrEmpty(runId))
                    return null;

                string runStatus = string.Empty;
                int pollInterval = 10000;
                int maxPollInterval = 30000;
                int maxAttempts = 12;
                int attempts = 0;

                while (runStatus != "completed")
                {
                    await Task.Delay(pollInterval);
                    runStatus = await RetrieveRun(runId, threadId);
                    attempts++;
                    if (runStatus == "failed" || runStatus == "cancelled" || runStatus == "expired"
                        || runStatus == null || attempts >= maxAttempts)
                        break;
                    pollInterval = Math.Min(pollInterval + 5000, maxPollInterval);
                }
                if (runStatus == "completed")
                    await RetrieveRunStepsForTokenUsage(runId, threadId);

                return await ListRunMessages(threadId);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        /// <summary>
        /// Preset AI-interview evaluation. When OpenAI:UseResponsesApi is true, this uses
        /// OpenAI:AIInterviewPromptMap (pmpt_…) on the Responses API. Otherwise returns null
        /// so the caller can use the Assistants API.
        /// </summary>
        public async Task<RunResult> GenerateAIInterviewEvaluation(string techSkillsJson, string questionAnswerSetJson, string assistantId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(questionAnswerSetJson))
                    return null;

                var useResponsesApi = _configuration.GetValue("OpenAI:UseResponsesApi", true);
                var promptId = _configuration["OpenAI:AIInterviewPromptMap"];
                if (!useResponsesApi || string.IsNullOrWhiteSpace(promptId))
                    return null;

                var input = $"Generate an output in JSON format as per the instructions fed to the assistant. The techrubrics in json are as follows:{techSkillsJson}. The mapped_rubric_parameters, question, candidate's answer and ideal answer set object in json are as follows: {questionAnswerSetJson}";
                return await _openAIResponsesHelper.CreateJsonResponseAsync(
                    input,
                    operationType: "AIInterviewEvaluation",
                    promptId: promptId);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        /// <summary>
        /// Adaptive AI-interviewer conversational turn. When OpenAI:UseResponsesApi is true, uses
        /// OpenAI:AIInterviewerPromptId on the Responses API with optional previous_response_id chaining.
        /// </summary>
        public async Task<(RunResult Result, string ResponseId)> GenerateAIInterviewerMessage(string message, string previousResponseId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(message))
                    return (null, null);

                var useResponsesApi = _configuration.GetValue("OpenAI:UseResponsesApi", true);
                var promptId = _configuration["OpenAI:AIInterviewerPromptId"];
                if (!useResponsesApi || string.IsNullOrWhiteSpace(promptId))
                    return (null, null);

                return await _openAIResponsesHelper.CreateJsonResponseWithIdAsync(
                    message,
                    operationType: "AIInterviewer",
                    promptId: promptId,
                    previousResponseId: previousResponseId);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return (null, null);
            }
        }

        public async Task<RunResult> GenerateTypingAssessment(string inputsJson)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(inputsJson))
                    return null;

                var useResponsesApi = _configuration.GetValue("OpenAI:UseResponsesApi", true);
                if (useResponsesApi)
                {
                    return await _openAIResponsesHelper.CreateJsonResponseAsync(
                        inputsJson,
                        operationType: "TypingAssessment",
                        promptId: _configuration["OpenAI:TypingAssessmentPromptId"]);
                }

                return null;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<RunResult> GenerateIntervieweeResponseExtraction(string transcriptData)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(transcriptData))
                    return null;

                var useResponsesApi = _configuration.GetValue("OpenAI:UseResponsesApi", true);
                if (useResponsesApi)
                {
                    return await _openAIResponsesHelper.CreateJsonResponseAsync(
                        transcriptData,
                        operationType: "IntervieweeResponseExtraction",
                        promptId: _configuration["OpenAI:IntervieweeResponseExtractionPromptId"]);
                }

                return null;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        /// <summary>
        /// Calls the LinkedIn-verification assistant (OpenAI:LinkedInVerificationAssistantId) with a
        /// composed message describing the prospect (name, email, phone, current role, and any candidate-
        /// supplied LinkedIn URL) and returns the raw RunResult. The assistant judges whether the URL is a
        /// genuine profile for this person — or, when no URL is given, tries to identify one — and returns
        /// a JSON verdict (linkedin_match / confidence / linkedin_url / reason).
        /// </summary>
        public async Task<RunResult> GenerateLinkedInVerification(string composedMessage)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(composedMessage))
                    return null;

                var useResponsesApi = _configuration.GetValue("OpenAI:UseResponsesApi", true);
                if (useResponsesApi)
                {
                    return await _openAIResponsesHelper.CreateJsonResponseAsync(
                        composedMessage,
                        operationType: "LinkedInVerification",
                        promptId: _configuration["OpenAI:LinkedInVerificationPromptId"]);
                }

                string threadId = await CreateThreadAsync(plainAssistantMessage: composedMessage);
                if (string.IsNullOrEmpty(threadId))
                    return null;

                string runId = await CreateRun(threadId, ProfileNames.LinkedInVerificationAssistantId);
                await Task.Delay(10000);
                if (string.IsNullOrEmpty(runId))
                    return null;

                string runStatus = string.Empty;
                int pollInterval = 10000;
                int maxPollInterval = 30000;
                int maxAttempts = 12;
                int attempts = 0;

                while (runStatus != "completed")
                {
                    await Task.Delay(pollInterval);
                    runStatus = await RetrieveRun(runId, threadId);
                    attempts++;
                    if (runStatus == "failed" || runStatus == "cancelled" || runStatus == "expired"
                        || runStatus == null || attempts >= maxAttempts)
                        break;
                    pollInterval = Math.Min(pollInterval + 5000, maxPollInterval);
                }

                if (runStatus == "completed")
                    await RetrieveRunStepsForTokenUsage(runId, threadId);

                return await ListRunMessages(threadId);
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
