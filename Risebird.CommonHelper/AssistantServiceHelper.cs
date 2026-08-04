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
    /// <summary>
    /// One-shot assistant operations (screening, QC, rubric generation, resume/transcript analysis,
    /// contradiction report, payment-ticket + LinkedIn verification, etc.).
    ///
    /// Migrated from the deprecated Assistants API (threads / runs / run-steps / messages) to the
    /// stateless Responses API (POST /v1/responses). Each legacy "create thread -> add message ->
    /// create run -> poll -> list messages" sequence collapses to a single synchronous Responses call.
    ///
    /// The public contract (IAssistantServiceHelper) and the returned RunResult shape are unchanged so
    /// every caller keeps reading messages.Data[0].Content[0].Text.Value exactly as before. The legacy
    /// Assistant ids in configuration are reused: their stored model / instructions / tools are read
    /// once (and cached) and fed into the Responses calls, preserving behaviour.
    /// </summary>
    public class AssistantServiceHelper : IAssistantServiceHelper
    {
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IOpenAIApiLogger _openAIApiLogger;
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

        public AssistantServiceHelper(IApplicationErrorLogger applicationErrorLogger, IConfiguration configuration, IOpenAIApiLogger openAIApiLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _openAIApiLogger = openAIApiLogger;
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

        // ---------------------------------------------------------------------------------------
        //  Core Responses helpers
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// Resolves the assistant referenced by <paramref name="profileType"/> and returns its id +
        /// an operation-type label for logging. Mirrors the legacy CreateRun assistant selection.
        /// </summary>
        private (string assistantId, string operationType) MapAssistant(string profileType)
        {
            if (string.IsNullOrEmpty(profileType))
                return (rubricGeneratorAssistantId, "RubricGeneration");

            if (profileType == ProfileNames.Candidate)
                return (candidateScreenerAssistantId, "CandidateScreening");
            if (profileType == ProfileNames.Interviewer)
                return (interviewerScreenerAssistantId, "InterviewerScreening");
            if (profileType == ProfileNames.ResumeParserAIAssistant)
                return (resumeParserAssistantId, "ResumeParsing");
            if (profileType == ProfileNames.TranscriptAnalysisAIAssistant)
                return (aiTranscriptAnalysisAssistantId, "TranscriptAnalysis");
            if (profileType == ProfileNames.StringResumeAnalysisAIAssistant)
                return (stringResumeAnalysisAssistantIdV2, "ResumeAnalysis");
            if (profileType == ProfileNames.StringReportQCAIAssistant)
                return (stringReportQCAssistantIdStructuredOutput, "ReportQC");
            if (profileType == ProfileNames.TranscriptToNotesAssistantId)
                return (AITranscriptToNotesAssistantId, "TranscriptToNotes");
            if (profileType == ProfileNames.QcInterviewAIAssistantId)
                return (QcInterviewAIMergeAssistantId, "QcInterviewAIMerge");
            if (profileType == ProfileNames.ContradictionReportAIAssistantId)
                return (ContradictionReportAIAssistantId, "ContradictionReport");
            if (profileType == ProfileNames.PaymentTicketReviewAssistantId)
                return (PaymentTicketReviewAssistantId, "PaymentTicketReview");
            if (profileType == ProfileNames.LinkedInVerificationAssistantId)
                return (LinkedInVerificationAssistantId, "LinkedInVerification");

            // Default (Quality Controller and any unmapped profile).
            return (reportQCAssistantId, "QualityCheck");
        }

        /// <summary>
        /// Executes a single Responses call against the given assistant and returns its output text.
        /// </summary>
        private async Task<string> RunAssistantAsync(string assistantId, string operationType, string userInput, bool jsonObject, string fileId = null)
        {
            var profile = await OpenAIResponsesGateway.ResolveAssistantAsync(baseUrl, apiKey, assistantId, _openAIApiLogger);
            var input = OpenAIResponsesGateway.BuildUserInput(userInput, fileId);

            var result = await OpenAIResponsesGateway.CreateResponseAsync(
                baseUrl: baseUrl,
                apiKey: apiKey,
                model: profile.Model,
                instructions: profile.Instructions,
                input: input,
                jsonObjectFormat: jsonObject,
                tools: profile.Tools,
                logger: _openAIApiLogger,
                context: new OpenAIApiLogContext { OperationType = operationType },
                assistantId: assistantId);

            return result.OutputText;
        }

        /// <summary>
        /// Wraps assistant output text into the legacy RunResult shape so existing callers that read
        /// result.Data[0].Content[0].Text.Value continue to work unchanged.
        /// </summary>
        private static RunResult BuildRunResult(string outputText)
        {
            if (outputText == null)
            {
                // Preserve "no usable output" semantics.
                return null;
            }

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
                                Text = new Text { Value = outputText, Annotations = new List<object>() }
                            }
                        }
                    }
                }
            };
        }

        private static bool LooksLikeExtractionFailure(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            var lower = text.ToLower();
            return lower.Contains("persistent issue")
                || lower.Contains("error")
                || lower.Contains("issue with extracting")
                || lower.Contains("it seems");
        }

        // ---------------------------------------------------------------------------------------
        //  Public API (unchanged signatures)
        // ---------------------------------------------------------------------------------------

        public async Task<RunResult> ScreenAsync(ScreeningRequest requestModel)
        {
            try
            {
                string sourceUrl = requestModel.Type == ProfileNames.QualityController
                    ? requestModel.ReportUrl
                    : requestModel.ResumeLink;

                var fileId = await OpenAIResponsesGateway.UploadFileForInputAsync(
                    baseUrl, apiKey, sourceUrl, _openAIApiLogger,
                    new OpenAIApiLogContext { OperationType = "FileUpload" });

                if (string.IsNullOrEmpty(fileId))
                {
                    return null;
                }

                string queryContent = ComposeScreeningQuery(requestModel);
                var (assistantId, operationType) = MapAssistant(requestModel.Type);

                var outputText = await RunAssistantAsync(assistantId, operationType, queryContent, jsonObject: true, fileId: fileId);

                // If the model reported a file-extraction problem, retry once (mirrors legacy RecreateThread).
                if (LooksLikeExtractionFailure(outputText))
                {
                    var retryText = await RunAssistantAsync(assistantId, operationType, queryContent, jsonObject: true, fileId: fileId);
                    if (!LooksLikeExtractionFailure(retryText))
                    {
                        outputText = retryText;
                    }
                }

                return BuildRunResult(outputText);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        private static string ComposeScreeningQuery(ScreeningRequest requestModel)
        {
            if (requestModel.Type == ProfileNames.Candidate)
            {
                return $"Evaluate the resume and generate a response in json as per the instructions fed to you. Do not give me comparison with other Resumes. Stick to the current resume I am feeding you. The job title you need to screen the candidate and generate the fitment score against is {requestModel.JDTitle}.";
            }
            if (requestModel.Type == ProfileNames.Interviewer)
            {
                return $"Evaluate the resume and generate a response in json as per the instructions fed to you. Do not give me comparison with other Resumes. Stick to the current resume I am feeding you. The job title you need to screen the interviewer and generate the fitment score against is {requestModel.JDTitle}.";
            }
            if (requestModel.Type == ProfileNames.QualityController)
            {
                return "Generate an output as per the instructions fed to the assistant.";
            }
            // ResumeParserAIAssistant and any other file-based screening.
            return "Evaluate the resume and generate a response in json as per the instructions fed to you.";
        }

        public async Task<RunResult> GenerateTechRubric(string jobTitle)
        {
            try
            {
                var (assistantId, operationType) = MapAssistant(null); // rubric generator
                var query = $"Generate an output as per the instructions fed to the assistant. The job title you need to generate a tech rubric against is {jobTitle}.";
                var outputText = await RunAssistantAsync(assistantId, operationType, query, jsonObject: true);
                return BuildRunResult(outputText);
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

                var (assistantId, operationType) = MapAssistant(null); // rubric generator
                var outputText = await RunAssistantAsync(assistantId, operationType, revisionPrompt, jsonObject: true);
                return BuildRunResult(outputText);
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
                var (assistantId, operationType) = MapAssistant(ProfileNames.TranscriptAnalysisAIAssistant);
                var query = $"Generate an output as per the instructions fed to the assistant. The transcript data you need to generate an analysis against is {transcriptData}.";
                var outputText = await RunAssistantAsync(assistantId, operationType, query, jsonObject: true);
                return BuildRunResult(outputText);
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

                // Legacy behaviour selected the Qc-Interview-AI-Merge assistant via profile type
                // (the passed assistantId was not used for the run); preserved here.
                var (mappedAssistantId, operationType) = MapAssistant(ProfileNames.QcInterviewAIAssistantId);
                var outputText = await RunAssistantAsync(mappedAssistantId, operationType, userMessageContent, jsonObject: useJsonResponseFormat);
                return BuildRunResult(outputText);
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
                var (assistantId, operationType) = MapAssistant(ProfileNames.StringResumeAnalysisAIAssistant);
                var query = $"Generate an output as per the instructions fed to the assistant. The job title you need to screen the interviewer and generate the fitment score against is {jobTitle}. The Resume data you need to generate an analysis against is {resumeStringData}.";
                var outputText = await RunAssistantAsync(assistantId, operationType, query, jsonObject: true);
                return BuildRunResult(outputText);
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
                var (assistantId, operationType) = MapAssistant(ProfileNames.StringReportQCAIAssistant);
                var query = $"Generate an output as per the instructions fed to the assistant. The Report data you need to generate an analysis against is {reportStringData}.";
                var outputText = await RunAssistantAsync(assistantId, operationType, query, jsonObject: true);
                return BuildRunResult(outputText);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<RunResult> ParseResume(string resumeData)
        {
            try
            {
                var (assistantId, operationType) = MapAssistant(ProfileNames.ResumeParserAIAssistant);
                var query = "Evaluate the resume and generate a response in json as per the instructions fed to you.";
                // The composed prompt references the resume text supplied by the caller.
                if (!string.IsNullOrEmpty(resumeData))
                {
                    query = $"{query} The Resume data you need to evaluate is: {resumeData}";
                }
                var outputText = await RunAssistantAsync(assistantId, operationType, query, jsonObject: true);
                return BuildRunResult(outputText);
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
                var (assistantId, operationType) = MapAssistant(ProfileNames.TranscriptToNotesAssistantId);
                var query = $"Generate the Summary for {transcriptdata}";
                // TranscriptToNotes produces free-form prose, so JSON formatting is intentionally off.
                var outputText = await RunAssistantAsync(assistantId, operationType, query, jsonObject: false);
                return BuildRunResult(outputText);
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
                var (assistantId, operationType) = MapAssistant(ProfileNames.ContradictionReportAIAssistantId);
                var outputText = await RunAssistantAsync(assistantId, operationType, transcriptSummary, jsonObject: true);
                return BuildRunResult(outputText);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<RunResult> GeneratePaymentTicketReview(string composedMessage)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(composedMessage))
                    return null;

                var (assistantId, operationType) = MapAssistant(ProfileNames.PaymentTicketReviewAssistantId);
                var outputText = await RunAssistantAsync(assistantId, operationType, composedMessage, jsonObject: true);
                return BuildRunResult(outputText);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<RunResult> GenerateLinkedInVerification(string composedMessage)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(composedMessage))
                    return null;

                var (assistantId, operationType) = MapAssistant(ProfileNames.LinkedInVerificationAssistantId);
                var outputText = await RunAssistantAsync(assistantId, operationType, composedMessage, jsonObject: true);
                return BuildRunResult(outputText);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        // ---------------------------------------------------------------------------------------
        //  File / vector-store housekeeping (still valid endpoints; used to prune uploaded inputs).
        // ---------------------------------------------------------------------------------------

        private async Task<FilesList> ListFiles()
        {
            try
            {
                HttpClient client = new HttpClient();

                var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/files")
                {
                    Headers = { { "Authorization", $"Bearer {apiKey}" } }
                };

                var sw = System.Diagnostics.Stopwatch.StartNew();
                var response = await client.SendAsync(request);
                sw.Stop();

                var responseContent = await response.Content.ReadAsStringAsync();

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

                while (iteration < maxIterations)
                {
                    var filesList = await ListFiles();

                    if (filesList == null || filesList.data == null || filesList.data.Count() == 0)
                    {
                        return true;
                    }

                    foreach (var item in filesList.data)
                    {
                        try
                        {
                            HttpClient client = new HttpClient();

                            var request = new HttpRequestMessage(HttpMethod.Delete, $"{baseUrl}/files/{item.id}")
                            {
                                Headers = { { "Authorization", $"Bearer {apiKey}" } }
                            };

                            var sw = System.Diagnostics.Stopwatch.StartNew();
                            var response = await client.SendAsync(request);
                            sw.Stop();

                            var responseContent = await response.Content.ReadAsStringAsync();

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
                    Headers = { { "Authorization", $"Bearer {apiKey}" }, { "OpenAI-Beta", "assistants=v2" } }
                };

                var sw = System.Diagnostics.Stopwatch.StartNew();
                var response = await client.SendAsync(request);
                sw.Stop();

                var responseContent = await response.Content.ReadAsStringAsync();

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
                            Headers = { { "Authorization", $"Bearer {apiKey}" }, { "OpenAI-Beta", "assistants=v2" } }
                        };

                        var response = client.SendAsync(request).Result;
                        sw.Stop();

                        var responseContent = response.Content.ReadAsStringAsync().Result;

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
                    }
                    );
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
