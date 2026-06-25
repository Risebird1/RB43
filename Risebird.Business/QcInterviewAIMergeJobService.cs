using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class QcInterviewAIMergeJobService : IQcInterviewAIMergeJobService
    {
        private const string DefaultMergeAssistantId = "asst_VJ5EYhocpfuzhwjwZmMcfblr";

        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IConfiguration _configuration;
        private readonly IAssistantServiceHelper _assistantServiceHelper;
        private readonly IMeetingService _meetingService;
        private readonly IDmIvAutoQcRemovalService _autoQcRemovalService;
        private readonly ILogger<QcInterviewAIMergeJobService> _log;

        public QcInterviewAIMergeJobService(
            IApplicationErrorLogger applicationErrorLogger,
            IConfiguration configuration,
            IAssistantServiceHelper assistantServiceHelper,
            IMeetingService meetingService,
            IDmIvAutoQcRemovalService autoQcRemovalService,
            ILogger<QcInterviewAIMergeJobService> log)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _configuration = configuration;
            _assistantServiceHelper = assistantServiceHelper;
            _meetingService = meetingService;
            _autoQcRemovalService = autoQcRemovalService;
            _log = log;
        }

        /// <inheritdoc />
        public async Task<(int processed, int failed)> ProcessAllPendingAsync(CancellationToken cancellationToken = default)
        {
            var assistantId = _configuration["OpenAI:QcInterviewAIMergeAssistantId"];
            if (string.IsNullOrWhiteSpace(assistantId))
                assistantId = DefaultMergeAssistantId;

            var processed = 0;
            var failed = 0;

            try
            {
                var pending = await _meetingService.GetPendingQcInterviewAIMerge();
                if (pending == null || !pending.Any())
                    return (0, 0);

                foreach (var row in pending)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        var ok = await ProcessRowAsync(row, assistantId, cancellationToken);
                        if (ok)
                            processed++;
                        else
                            failed++;
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        _log.LogError(ex, "QcInterviewAIMergeJob: error for qcEvaluationId={Id} interviewId={InterviewId}", row.id, row.InterviewId);
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }

            return (processed, failed);
        }

        /// <inheritdoc />
        public async Task<(bool ok, string message)> ProcessInterviewAsync(int interviewId, CancellationToken cancellationToken = default)
        {
            if (interviewId <= 0)
                return (false, "Invalid interview id.");

            var assistantId = _configuration["OpenAI:QcInterviewAIMergeAssistantId"];
            if (string.IsNullOrWhiteSpace(assistantId))
                assistantId = DefaultMergeAssistantId;

            var row = await _meetingService.GetPendingQcInterviewAIMergeById(interviewId);
            if (row == null)
                return (false, "No pending QC AI merge for this interview (already merged, QC done, or Report/Transcript AI JSON not ready).");

            try
            {
                var ok = await ProcessRowAsync(row, assistantId, cancellationToken);
                return ok
                    ? (true, "QC Interview AI merge completed for this interview.")
                    : (false, "Merge assistant returned empty or invalid JSON; nothing was saved.");
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "QcInterviewAIMergeJob: error for qcEvaluationId={Id} interviewId={InterviewId}", row.id, row.InterviewId);
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return (false, "Merge failed; see application logs.");
            }
        }

        private async Task<bool> ProcessRowAsync(QcInterviewAIMergePendingRow row, string assistantId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var userMessage = BuildMergeUserMessage(row.ReportAIDetails, row.TranscritAiDetails, row.ContradictionJson, row.interviewDate, row.Joinedon, row.ActualDuration, row.RequiredDuration);
            var runResult = await _assistantServiceHelper.GenerateQcAnalysisInterviewAIMerge(userMessage, assistantId, useJsonResponseFormat: true);
            var rawText = runResult?.Data?.FirstOrDefault()?.Content?.FirstOrDefault()?.Text?.Value?.Replace("```json", "").Replace("```", "");
            if (string.IsNullOrWhiteSpace(rawText))
            {
                _log.LogWarning("QcInterviewAIMergeJob: empty assistant response for qcEvaluationId={Id} interviewId={InterviewId}", row.id, row.InterviewId);
                return false;
            }

            if (!QcInterviewAIMergeAssistantResponse.TryParseFromAssistantText(rawText, out var parsed) || parsed == null)
            {
                _log.LogWarning("QcInterviewAIMergeJob: could not parse JSON for qcEvaluationId={Id} interviewId={InterviewId}", row.id, row.InterviewId);
                return false;
            }

            var paymentComplianceCount = parsed.flags != null ? parsed.flags.Count() : 0;
            var combined = CombineFlagsByCategory(parsed);

            await _meetingService.SaveQcInterviewAIMergedDetails(
                row.id,
                rawText,
                totalPenalty: combined.total_penalty,
                interviewerSuspicious: combined.interviewer_suspicious,
                redoInterview: combined.RedoInterview);

            if (row.InterviewId > 0)
                await PersistPaymentSlaFromQcMergeAsync(row.InterviewId, combined, paymentComplianceCount);

            if (row.InterviewId > 0)
            {
                await _autoQcRemovalService.TryApplyAutoQcRemovalAsync(
                    row.InterviewId,
                    row.irfId,
                    combined.interviewer_suspicious,
                    combined.RedoInterview,
                    cancellationToken);
            }

            return true;
        }

        private static string BuildMergeUserMessage(string reportJson, string transcriptJson, string contradictionJson, DateTime interviewdate, DateTime? Joinedon, int ActualDuration, int RequiredDuration)
        {
            var message =
                "You are given JSON evaluations for the same interview: Report AI, Transcript AI, and optionally Contradiction AI. " +
                "Reconcile them and output one JSON object only (no markdown) with: " +
                "flags (array of { category, flag, penalty } only), " +
                "total_penalty (integer), interviewer_suspicious (string, e.g. Yes/No), RedoInterview (boolean).\n\n" +
                "Report AI JSON:\n" + (reportJson ?? "") + "\n\n" +
                "Transcript AI JSON:\n" + (transcriptJson ?? "");

            if (!string.IsNullOrWhiteSpace(contradictionJson))
                message += "\n\nContradiction AI JSON:\n" + contradictionJson;
            if (Joinedon.HasValue)
                message += "\n\nInterview Datetime: " + interviewdate + " and interviewer join the meeting at " + Joinedon.Value;
            if (ActualDuration > 0)
                message += "\n\nInterview Duration Required: " + RequiredDuration + " and Interview Duration Actual " + ActualDuration;
            return message;
        }

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

        private async Task PersistPaymentSlaFromQcMergeAsync(int interviewId, QcInterviewAIMergeAssistantResponse combined, int paymentComplianceCount)
        {
            const string slaType = "QC";
            const string createdBy = "AISystem";

            try
            {
                var existing = await _meetingService.FetchPaymentSlaDetailsById(interviewId, slaType);
                var model = existing ?? new PaymentSLAVM();

                model.Interviewid = interviewId;
                model.CreatedBy = createdBy;
                model.Isedit = existing != null;
                model.isQcDone = true;
                model.PaymentComplianceCount = paymentComplianceCount;
                if (existing == null)
                {
                    model.isapprovedchecked = false;
                    model.techqc = false;
                    model.Rating = 0;
                    model.VideoEnable = false;
                }

                PaymentSlaVmQcMergeMapper.ApplyFromMergeResponse(model, combined, applyOnlyIfEmpty: false);

                await _meetingService.AddorUpdatePaymentSLADetails(model);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "QcInterviewAIMergeJob: PersistPaymentSlaFromQcMergeAsync failed for interviewId={InterviewId}", interviewId);
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }
    }
}
