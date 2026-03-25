using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.AzureTimerFunctions
{
    /// <summary>
    /// For QcInterviewAIevaluation rows where QC is not done and both Report AI and Transcript AI JSON exist,
    /// calls the merge assistant and stores <see cref="QcInterviewAIMergeAssistantResponse"/> in MergedAIQCDetails plus summary columns.
    /// </summary>
    public class QcInterviewAIMergeJob
    {
        private const string DefaultMergeAssistantId = "asst_VJ5EYhocpfuzhwjwZmMcfblr";

        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IConfiguration _configuration;
        private readonly IAssistantServiceHelper _assistantServiceHelper;
        private readonly IMeetingService _meetingService;
        private readonly ILogger _log;

        public QcInterviewAIMergeJob(
            ILoggerFactory loggerFactory,
            IApplicationErrorLogger applicationErrorLogger,
            IConfiguration configuration,
            IAssistantServiceHelper assistantServiceHelper,
            IMeetingService meetingService)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _configuration = configuration;
            _assistantServiceHelper = assistantServiceHelper;
            _meetingService = meetingService;
            _log = loggerFactory.CreateLogger<QcInterviewAIMergeJob>();
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
                        var userMessage = BuildMergeUserMessage(row.ReportAIDetails, row.TranscritAiDetails);
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

                        var combined = CombineFlagsByCategory(parsed);
                        var mergedJson = JsonConvert.SerializeObject(combined);

                        await _meetingService.SaveQcInterviewAIMergedDetails(
                            row.id,
                            mergedJson,
                            totalPenalty: combined.total_penalty,
                            interviewerSuspicious: combined.interviewer_suspicious,
                            redoInterview: combined.RedoInterview);

                        if (row.InterviewId > 0)
                            await PersistPaymentSlaFromQcMergeAsync(row.InterviewId, combined);
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

        private static string BuildMergeUserMessage(string reportJson, string transcriptJson)
        {
            return "You are given two JSON evaluations for the same interview: Report AI and Transcript AI. " +
                   "Reconcile them and output one JSON object only (no markdown) with: " +
                   "flags (array of { category, flag, penalty } only), " +
                   "total_penalty (integer), interviewer_suspicious (string, e.g. Yes/No), RedoInterview (boolean).\n\n" +
                   "Report AI JSON:\n" + (reportJson ?? "") + "\n\n" +
                   "Transcript AI JSON:\n" + (transcriptJson ?? "");
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
        private async Task PersistPaymentSlaFromQcMergeAsync(int interviewId, QcInterviewAIMergeAssistantResponse combined)
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
                if (existing == null)
                {
                    model.isapprovedchecked = false;
                    model.techqc = false;
                    model.PaymentComplianceCount = 0;
                    model.Rating = 0;
                    model.VideoEnable = false;
                }

                PaymentSlaVmQcMergeMapper.ApplyFromMergeResponse(model, combined, applyOnlyIfEmpty: false);

                var saved = await _meetingService.AddorUpdatePaymentSLADetails(model);
                if (!saved)
                    _log.LogWarning("QcInterviewAIMergeJob: AddorUpdatePaymentSLADetails returned false for interviewId={InterviewId}", interviewId);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "QcInterviewAIMergeJob: PersistPaymentSlaFromQcMergeAsync failed for interviewId={InterviewId}", interviewId);
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }
    }
}
