using BitMiracle.LibTiff.Classic;
using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Chats.GetAllMessages;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Amazon.Runtime.Internal.Settings.SettingsCollection;
using Orientation = DinkToPdf.Orientation;

namespace Risebird.AzureTimerFunctions
{
    /// <summary>
    /// For QcInterviewAIevaluation rows where QC is not done and both Report AI and Transcript AI JSON exist,
    /// calls the merge assistant and stores <see cref="QcInterviewAIMergeAssistantResponse"/> in MergedAIQCDetails plus summary columns.
    /// </summary>
    public class QcInterviewAIMergeJob
    {
        private const string DefaultMergeAssistantId = "asst_VJ5EYhocpfuzhwjwZmMcfblr";
        private const string ContradictionAssistantId = "asst_9Nw7SKbhCsZ0Zp40kEa45rry";

        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IConfiguration _configuration;
        private readonly IAssistantServiceHelper _assistantServiceHelper;
        private readonly IMeetingService _meetingService;
        private readonly IOpenAIInterviewerAssistantHelper _ai;
        private readonly ILogger _log;
        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly ICompositeViewEngine _viewEngine;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly IConverter _converter;

        public QcInterviewAIMergeJob(
            ILoggerFactory loggerFactory,
            IApplicationErrorLogger applicationErrorLogger,
            IConfiguration configuration,
            IAssistantServiceHelper assistantServiceHelper,
            IMeetingService meetingService,
            IOpenAIInterviewerAssistantHelper ai,
            IAzureStorageHelper azureStorageHelper,
            IHttpContextAccessor contextAccessor,
            ICompositeViewEngine viewEngine,
            ITempDataProvider tempDataProvider,
            IConverter converter)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _configuration = configuration;
            _assistantServiceHelper = assistantServiceHelper;
            _meetingService = meetingService;
            _ai = ai;
            _log = loggerFactory.CreateLogger<QcInterviewAIMergeJob>();
            _azureStorageHelper = azureStorageHelper;
            _contextAccessor = contextAccessor;
            _viewEngine = viewEngine;
            _tempDataProvider = tempDataProvider;
            _converter = converter;
        }

        [Function("QcInterviewAIMergeJob")]
        public async Task Run([TimerTrigger("0 */15 * * * *")] TimerInfo myTimer)
        {
            _log.LogInformation("QcInterviewAIMergeJob started at {Time}", DateTime.Now);

            var assistantId = _configuration["OpenAI:QcInterviewAIMergeAssistantId"];
            if (string.IsNullOrWhiteSpace(assistantId))
                assistantId = DefaultMergeAssistantId;

            try
            {
                var pending = await _meetingService.GetPendingQcInterviewAIMerge();
                if (pending == null || !pending.Any())
                {
                    _log.LogInformation("QcInterviewAIMergeJob: no pending rows.");
                    return;
                }

                foreach (QcInterviewAIMergePendingRow row in pending)
                {
                    try
                    {
                        string contradictionJson = null;

                        // Only call the Contradiction AI assistant if TranscriptSummary exists for this interview
                        try
                        {
                            var transcriptSummary = row.TranscriptSummary;
                            if (string.IsNullOrWhiteSpace(row.ContradictionJson))
                            {
                                if (!string.IsNullOrWhiteSpace(transcriptSummary))
                                {
                                    var model = await _meetingService.GetInteviewReport(row.InterviewId, row.irfId);
                                    await ReportSkillSetandSeperate(model);
                                    if (!string.IsNullOrEmpty(model.AudioTranscriptFileLink))
                                    {
                                        model.AudioTranscriptFileLink = null;
                                    }
                                    // Render the partial view to a string
                                    var htmlContent = await RenderToStringAsync("AIClientReport", model);
                                    //Generate PDF from HTML content
                                    var pdfContent = GeneratePdf(htmlContent);
                                    string fileName = "AIClientReport_" + model.InterviewId.ToString() + "_" + DateTime.Now.ToString() + ".pdf";
                                    // Upload PDF to Azure Blob Storage
                                    string uploadedFileURL = string.Empty;
                                    using (MemoryStream stream = new MemoryStream(pdfContent))
                                    {
                                        await _azureStorageHelper.InsertBlobCloudCompare(fileName, "aiqc", blobFile: stream, isOpenAiQc: true);
                                        var containerRef = _azureStorageHelper.GetCloudBlobContainer("aiqc");
                                        var blob = containerRef.GetBlockBlobReference(fileName);
                                        uploadedFileURL = blob.Uri.ToString();
                                    }
                                    var reportStringContent = await ReadPdfFromUrlAsync(uploadedFileURL);
                                    var contradictionReportMessage = BuildContradictionReportMessage(transcriptSummary, reportStringContent);
                                    var runcontradictionResult = await _assistantServiceHelper.GeneratecontradictionReport(contradictionReportMessage);
                                    contradictionJson = runcontradictionResult?.Data?.FirstOrDefault()?.Content?.FirstOrDefault()?.Text?.Value?.Replace("```json", "").Replace("```", "");
                                }
                            }
                            else
                            {
                                contradictionJson = row.ContradictionJson;
                            }
                        }
                        catch (Exception contradictionEx)
                        {
                            _log.LogError(contradictionEx, "QcInterviewAIMergeJob: error while calling Contradiction AI assistant for qcEvaluationId={Id} interviewId={InterviewId}", row.id, row.InterviewId);
                            await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, contradictionEx);
                        }

                        var userMessage = BuildMergeUserMessage(row.ReportAIDetails, row.TranscritAiDetails, contradictionJson, row.interviewDate, row.Joinedon);
                        var runResult = await _assistantServiceHelper.GenerateQcAnalysisInterviewAIMerge(userMessage, assistantId, useJsonResponseFormat: true);
                        var rawText = runResult?.Data?.FirstOrDefault()?.Content?.FirstOrDefault()?.Text?.Value?.Replace("```json", "").Replace("```", "");
                        if (string.IsNullOrWhiteSpace(rawText))
                        {
                            _log.LogWarning("QcInterviewAIMergeJob: empty assistant response for qcEvaluationId={Id} interviewId={InterviewId}", row.id, row.InterviewId);
                            continue;
                        }

                        if (!QcInterviewAIMergeAssistantResponse.TryParseFromAssistantText(rawText, out var parsed) || parsed == null)
                        {
                            _log.LogWarning("QcInterviewAIMergeJob: could not parse JSON for qcEvaluationId={Id} interviewId={InterviewId}", row.id, row.InterviewId);
                            continue;
                        }
                        var PaymentComplanceCount = parsed.flags != null ? parsed.flags.Count() : 0;
                        var combined = CombineFlagsByCategory(parsed);
                        var mergedJson = JsonConvert.SerializeObject(combined);

                        await _meetingService.SaveQcInterviewAIMergedDetails(
                            row.id,
                            rawText,
                            totalPenalty: combined.total_penalty,
                            interviewerSuspicious: combined.interviewer_suspicious,
                            redoInterview: combined.RedoInterview,
                            contradictionJson: contradictionJson);

                        if (row.InterviewId > 0)
                            await PersistPaymentSlaFromQcMergeAsync(row.InterviewId, combined, PaymentComplanceCount);
                    }
                    catch (Exception ex)
                    {
                        _log.LogError(ex, "QcInterviewAIMergeJob: error for qcEvaluationId={Id} interviewId={InterviewId}", row.id, row.InterviewId);
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                    }
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }

            _log.LogInformation("QcInterviewAIMergeJob finished at {Time}", DateTime.Now);
        }
        public async Task ReportSkillSetandSeperate(InterviewReport model)
        {
            model.QuestionsTwoCollection = model.QuestionsCollection?.Where(x => x.SkillSet == 2).ToList();
            model.QuestionsCollection = model.QuestionsCollection?.Where(x => x.SkillSet != 2).ToList();
            model.TechSkillTwoReportCollection = model.TechSkillReportCollection?.Where(x => x.SkillSet == 2).ToList();
            model.TechSkillReportCollection = model.TechSkillReportCollection?.Where(x => x.SkillSet != 2).ToList();
        }
        public async Task<string> RenderToStringAsync(string viewName, object model)
        {
            try
            {
                var httpContext = _contextAccessor.HttpContext;
                var actionContext = new ActionContext(httpContext, httpContext.GetRouteData(), new ActionDescriptor());

                using (var sw = new StringWriter())
                {
                    var viewResult = _viewEngine.FindView(actionContext, viewName, false);

                    if (viewResult.View == null)
                    {
                        throw new ArgumentNullException($"The view '{viewName}' was not found.");
                    }

                    var viewDictionary = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                    {
                        Model = model
                    };

                    var tempData = new TempDataDictionary(actionContext.HttpContext, _tempDataProvider);

                    var viewContext = new ViewContext(
                        actionContext,
                        viewResult.View,
                        viewDictionary,
                        tempData,
                        sw,
                        new HtmlHelperOptions()
                    );
                    await viewResult.View.RenderAsync(viewContext);
                    return sw.ToString();
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public byte[] GeneratePdf(string htmlContent)
        {
            try
            {
                var globalSettings = new GlobalSettings
                {
                    ColorMode = DinkToPdf.ColorMode.Color,
                    Orientation = Orientation.Portrait,
                    PaperSize = DinkToPdf.PaperKind.A4,
                };

                var objectSettings = new DinkToPdf.ObjectSettings
                {
                    PagesCount = true,
                    HtmlContent = htmlContent,
                    WebSettings = { DefaultEncoding = "utf-8" },
                    HeaderSettings = { FontName = "Arial", FontSize = 9, Right = "Page [page] of [toPage]", Line = true },
                    FooterSettings = { FontName = "Arial", FontSize = 9, Line = true, Center = "Report Footer" }
                };

                var pdf = new HtmlToPdfDocument()
                {
                    GlobalSettings = globalSettings,
                    Objects = { objectSettings }
                };

                return _converter.Convert(pdf);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<string> ReadPdfFromUrlAsync(string pdfUrl)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    byte[] pdfBytes = await client.GetByteArrayAsync(pdfUrl);

                    using (MemoryStream stream = new MemoryStream(pdfBytes))
                    {
                        using (var pdfDocument = UglyToad.PdfPig.PdfDocument.Open(stream))
                        {
                            string text = "";
                            foreach (var page in pdfDocument.GetPages())
                            {
                                text += page.Text + "\n";
                            }
                            return text;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //var resultDe = _activityTrackerService.DebugLogger($"AI Resume Screener function error : {ex.Message}, {ex.StackTrace}", "aiScreener", string.Empty).Result;

                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);

                return null;
            }
        }
        private static string BuildContradictionReportMessage(string transcriptSummary, string reportStringContent)
        {
            var message =
                "You are given JSON evaluations for the same interview:AI Report ,Human Report. " +
                "Reconcile them and output one JSON object only (no markdown) with: " +
                "AI Report JSON:\n" + (transcriptSummary ?? "") + "\n\n" +
                "Human Report JSON:\n" + (reportStringContent ?? "");
            return message;
        }

        private static string BuildMergeUserMessage(string reportJson, string transcriptJson, string contradictionJson, DateTime interviewdate, DateTime? Joinedon)
        {
            var message =
                "You are given JSON evaluations for the same interview: Report AI, Transcript AI, and optionally Contradiction AI. " +
                "Reconcile them and output one JSON object only (no markdown) with: " +
                "flags (array of { category, flag, penalty } only), " +
                "total_penalty (integer), interviewer_suspicious (string, e.g. Yes/No), RedoInterview (boolean).\n\n" +
                "Report AI JSON:\n" + (reportJson ?? "") + "\n\n" +
                "Transcript AI JSON:\n" + (transcriptJson ?? "");

            if (!string.IsNullOrWhiteSpace(contradictionJson))
            {
                message += "\n\nContradiction AI JSON:\n" + contradictionJson;
            }
            if (Joinedon.HasValue)
            {
                message += "\n\nInterview Datetime: " + interviewdate + " and interviewer join the meeting at " + Joinedon.Value;
            }
            return message;
        }

        /// <summary>Merge duplicate categories: combine flags text and sum penalties; recompute total_penalty from flags.</summary>
        private static QcInterviewAIMergeAssistantResponse CombineFlagsByCategory(QcInterviewAIMergeAssistantResponse response)
        {
            if (response == null)
                return null;

            if (response.flags == null || response.flags.Count == 0)
            {
                return new QcInterviewAIMergeAssistantResponse
                {
                    flags = response.flags ?? new List<QcInterviewAIMergeFlagItem>(),
                    total_penalty = response.total_penalty,
                    interviewer_suspicious = response.interviewer_suspicious,
                    RedoInterview = response.RedoInterview
                };
            }

            var mergedFlags = response.flags
                .GroupBy(f => (f.category ?? "").Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var list = g.ToList();
                    if (list.Count == 1)
                        return list[0];

                    var categoryDisplay = list.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.category))?.category?.Trim() ?? g.Key;
                    return new QcInterviewAIMergeFlagItem
                    {
                        category = string.IsNullOrEmpty(categoryDisplay) ? null : categoryDisplay,
                        flag = string.Join(", ", list.SelectMany(x => SplitFlagTokens(x.flag)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase)),
                        penalty = list.Sum(x => x.penalty ?? 0)
                    };
                })
                .ToList();

            var sumPenalties = mergedFlags.Sum(f => f.penalty ?? 0);
            var total = sumPenalties > 0 ? sumPenalties : response.total_penalty;

            return new QcInterviewAIMergeAssistantResponse
            {
                flags = mergedFlags,
                total_penalty = total,
                interviewer_suspicious = response.interviewer_suspicious,
                RedoInterview = response.RedoInterview
            };
        }

        private static IEnumerable<string> SplitFlagTokens(string flag)
        {
            if (string.IsNullOrWhiteSpace(flag))
                yield break;
            foreach (var part in flag.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var t = part.Trim();
                if (t.Length > 0)
                    yield return t;
            }
        }

        /// <summary>
        /// Builds <see cref="PaymentSLAVM"/> from AI QC merge (same fields as Interview/GetPaymentSlaDetails) and saves via <see cref="IMeetingService.AddorUpdatePaymentSLADetails"/>.
        /// </summary>
        private async Task PersistPaymentSlaFromQcMergeAsync(int interviewId, QcInterviewAIMergeAssistantResponse combined, int PaymentComplanceCount)
        {
            var slaType = "QC";
            var createdBy = "AISystem";

            try
            {
                var existing = await _meetingService.FetchPaymentSlaDetailsById(interviewId, slaType);
                var model = existing ?? new PaymentSLAVM();

                model.Interviewid = interviewId;
                model.CreatedBy = createdBy;
                model.Isedit = existing != null;
                model.isQcDone = true;
                model.PaymentComplianceCount = PaymentComplanceCount;
                if (existing == null)
                {
                    model.isapprovedchecked = false;
                    model.techqc = false;
                    model.Rating = 0;
                    model.VideoEnable = false;
                }

                PaymentSlaVmQcMergeMapper.ApplyFromMergeResponse(model, combined, applyOnlyIfEmpty: false);

                var saved = await _meetingService.AddorUpdatePaymentSLADetails(model);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "QcInterviewAIMergeJob: PersistPaymentSlaFromQcMergeAsync failed for interviewId={InterviewId}", interviewId);
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }
    }
}
