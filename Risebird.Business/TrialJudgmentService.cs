using Microsoft.Extensions.Logging;
using Risebird.Business.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class TrialJudgmentService : ITrialJudgmentService
    {
        private readonly ITrialJudgmentRepository _repository;
        private readonly IRiskCategoryService _riskCategoryService;
        private readonly ILogger<TrialJudgmentService> _log;

        public TrialJudgmentService(
            ITrialJudgmentRepository repository,
            IRiskCategoryService riskCategoryService,
            ILogger<TrialJudgmentService> log)
        {
            _repository = repository;
            _riskCategoryService = riskCategoryService;
            _log = log;
        }

        public async Task<TrialJudgmentResultVm> TryEvaluateAndFlagTrialAsync(
            int interviewId,
            int irfId,
            QcInterviewAIMergeAssistantResponse mergeResult,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (interviewId <= 0)
                return Skipped("Invalid interview id.");

            var ctx = await _repository.GetContextAsync(interviewId);
            if (ctx == null)
                return Skipped("No trial judgment context returned.");

            if (!ctx.IsFirstCompletedInterview)
                return Skipped("Not the first completed interview on this IRF.");

            if (ctx.AlreadyFlagged)
                return Skipped("Trial flag already exists for this interviewer–IRF pair.");

            if (ctx.ChannelId <= 0 || ctx.IrfId <= 0 || ctx.InterviewerId <= 0)
                return Skipped("Could not resolve TransInterviewer / IRF mapping (interviewer not on this IRF channel, or email/nickname does not match UserList).");

            if (!ctx.IsEligible)
                return Skipped("Interview is not eligible for trial flag.");

            if (irfId > 0 && ctx.IrfId != irfId)
            {
                _log.LogWarning(
                    "TrialJudgment: irfId mismatch interviewId={InterviewId} expected={Expected} actual={Actual}",
                    interviewId, irfId, ctx.IrfId);
            }

            await _riskCategoryService.AddUpdateRiskCategory(interviewId);
            var risk = await _riskCategoryService.GetRiskCategoryByInterviewId(interviewId);

            var judgment = EvaluateFactors(ctx, mergeResult);
            ApplyRiskVerdictAndSeverity(judgment, risk);
            var (inserted, trialFlagId) = await _repository.InsertTrialFlagAsync(judgment, ctx, interviewId);
            judgment.Inserted = inserted;
            judgment.TrialFlagId = trialFlagId > 0 ? trialFlagId : (int?)null;

            if (!inserted && string.IsNullOrWhiteSpace(judgment.SkipReason))
                judgment.SkipReason = "Trial flag insert did not create a row (may already exist).";

            if (inserted)
            {
                _log.LogInformation(
                    "TrialJudgment: inserted trial flag id={TrialFlagId} interviewId={InterviewId} irfId={IrfId} verdict={Verdict}",
                    trialFlagId, interviewId, ctx.IrfId, judgment.OverallVerdict);
            }

            return judgment;
        }

        public async Task<TrialJudgmentResultVm> TryEvaluateAndFlagTrialForCompletedMergeAsync(
            int interviewId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var stored = await _repository.GetStoredMergeResultAsync(interviewId);
            if (!stored.HasValue)
                return Skipped("QC merge not completed for this interview (QCDone=0 or no evaluation row).");

            return await TryEvaluateAndFlagTrialAsync(
                interviewId,
                stored.Value.IrfId,
                stored.Value.MergeResult,
                cancellationToken);
        }

        public async Task RecordBackfillRunAsync(string executedBy, int? interviewId, TrialFlagBackfillResultVm result)
        {
            try
            {
                var summary = result?.Messages?.FirstOrDefault();
                await _repository.RecordBackfillRunAsync(executedBy, interviewId,
                    result?.Inserted ?? 0, result?.Skipped ?? 0, result?.Failed ?? 0, summary);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "TrialJudgment backfill: could not record the run for {ExecutedBy}.", executedBy);
            }
        }

        public async Task<TrialFlagBackfillResultVm> BackfillMissingTrialFlagsAsync(
            int? interviewId = null,
            CancellationToken cancellationToken = default)
        {
            var messages = new List<string>();
            var inserted = 0;
            var skipped = 0;
            var failed = 0;

            IReadOnlyList<TrialFlagBackfillCandidateVm> candidates;
            try
            {
                if (interviewId.HasValue && interviewId.Value > 0)
                {
                    candidates = new[] { new TrialFlagBackfillCandidateVm { InterviewId = interviewId.Value, IrfId = 0 } };
                }
                else
                {
                    candidates = await _repository.GetBackfillCandidatesAsync();
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "TrialJudgment backfill: could not load candidates.");
                return new TrialFlagBackfillResultVm
                {
                    Failed = 1,
                    Messages = new[] { "Could not load backfill candidates: " + ex.Message }
                };
            }

            if (candidates.Count == 0)
            {
                messages.Add("No eligible pairs found. Requires: usp_GetTrialFlagBackfillCandidates deployed, TransInterviewer.IsActive=1, IRF in date window, >=1 completed interview in last "
                    + TrialFlagConstants.BackfillActivityWindowDays
                    + " days, no TrialFlags row for (InterviewerId, IrfId). Triggering interview = first completed on IRF; QCDone/merge still required at evaluate time.");
                return new TrialFlagBackfillResultVm { Messages = messages };
            }

            var batch = candidates.Take(TrialFlagConstants.BackfillCandidateFetchSize).ToList();
            messages.Add($"Trying up to {batch.Count} active interviewer–IRF pair(s) with recent completed interviews; stops after {TrialFlagConstants.BackfillBatchSize} insert(s). Click again to process more.");

            foreach (var candidate in batch)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (inserted >= TrialFlagConstants.BackfillBatchSize)
                    break;

                try
                {
                    var result = await TryEvaluateAndFlagTrialForCompletedMergeAsync(candidate.InterviewId, cancellationToken);
                    if (result.Inserted)
                    {
                        inserted++;
                        messages.Add($"Inserted trial flag id={result.TrialFlagId} for interview {candidate.InterviewId}.");
                    }
                    else if (!string.IsNullOrWhiteSpace(result.SkipReason))
                    {
                        skipped++;
                        messages.Add($"Skipped pair interview={candidate.InterviewId} irf={candidate.IrfId}: {result.SkipReason}");
                    }
                    else
                    {
                        skipped++;
                        messages.Add($"Skipped pair interview={candidate.InterviewId} irf={candidate.IrfId}: no insert and no skip reason.");
                    }
                }
                catch (Exception ex)
                {
                    failed++;
                    messages.Add($"Failed pair interview={candidate.InterviewId} irf={candidate.IrfId}: {ex.Message}");
                    _log.LogError(ex, "TrialJudgment backfill failed for interviewId={InterviewId} irfId={IrfId}", candidate.InterviewId, candidate.IrfId);
                }
            }

            return new TrialFlagBackfillResultVm
            {
                Inserted = inserted,
                Skipped = skipped,
                Failed = failed,
                Messages = BuildBackfillMessages(messages, inserted, skipped)
            };
        }

        private static IReadOnlyList<string> BuildBackfillMessages(List<string> messages, int inserted, int skipped)
        {
            if (inserted > 0 || skipped == 0)
                return messages;

            var skipReasons = messages
                .Where(m => m.StartsWith("Skipped pair ", StringComparison.OrdinalIgnoreCase)
                         || m.StartsWith("Skipped interview ", StringComparison.OrdinalIgnoreCase))
                .Select(m =>
                {
                    var idx = m.IndexOf(':');
                    return idx >= 0 ? m.Substring(idx + 1).Trim() : m;
                })
                .Where(r => r.Length > 0)
                .GroupBy(r => r)
                .OrderByDescending(g => g.Count())
                .Take(3)
                .Select(g => $"{g.Count()}× {g.Key}")
                .ToList();

            if (skipReasons.Count == 0)
                return messages;

            var summary = new List<string>(messages) { "Skip summary: " + string.Join("; ", skipReasons) };
            return summary;
        }

        internal static TrialJudgmentResultVm EvaluateFactors(
            TrialJudgmentContextVm ctx,
            QcInterviewAIMergeAssistantResponse mergeResult)
        {
            var transcriptPass = !ctx.TranscriptQualityScore.HasValue
                || ctx.TranscriptQualityScore.Value >= TrialFlagConstants.MinTranscriptScore;

            var reportPass = !ctx.ReportQualityScore.HasValue
                || ctx.ReportQualityScore.Value >= TrialFlagConstants.MinReportScore;

            var suspicious = DmIvAutoQcAuditConstants.IsAffirmativeSuspicious(mergeResult?.interviewer_suspicious);
            var redo = mergeResult?.RedoInterview == true;
            var decisionPass = !suspicious && !redo;

            var ratingPass = !ctx.CandidateRating.HasValue
                || ctx.CandidateRating.Value >= TrialFlagConstants.MinCandidateRating;

            var joinPass = !ctx.JoinTimeMinutes.HasValue
                || ctx.JoinTimeMinutes.Value <= TrialFlagConstants.MaxJoinTimeMinutes;

            var durationPass = true;
            if (ctx.RequiredDurationMinutes.HasValue && ctx.RequiredDurationMinutes.Value > 0
                && ctx.InterviewDurationMinutes.HasValue)
            {
                var required = ctx.RequiredDurationMinutes.Value;
                var actual = ctx.InterviewDurationMinutes.Value;
                durationPass = actual >= required * TrialFlagConstants.MinDurationRatio;
            }

            var allPass = transcriptPass && reportPass && decisionPass && ratingPass && joinPass && durationPass;
            var verdict = allPass ? TrialFlagConstants.VerdictGoodTrial : TrialFlagConstants.VerdictBadTrial;

            return new TrialJudgmentResultVm
            {
                OverallVerdict = verdict,
                TranscriptQualityPass = transcriptPass,
                ReportQualityPass = reportPass,
                DecisionQualityPass = decisionPass,
                CandidateRatingPass = ratingPass,
                JoinTimePass = joinPass,
                InterviewDurationPass = durationPass
            };
        }

        internal static void ApplyRiskVerdictAndSeverity(TrialJudgmentResultVm judgment, RiskCategory risk)
        {
            // Live risk only when Severity is non-null (husk / Description-only row is NOT risk).
            // Never null-coalesce missing severity to P0.
            var riskPresent = risk != null && !string.IsNullOrWhiteSpace(risk.Severity);
            var factorPass = string.Equals(
                judgment.OverallVerdict,
                TrialFlagConstants.VerdictGoodTrial,
                StringComparison.OrdinalIgnoreCase);

            if (riskPresent)
            {
                judgment.OverallVerdict = TrialFlagConstants.VerdictBadTrial;
                judgment.Severity = risk.Severity;
                return;
            }

            if (!factorPass)
            {
                judgment.OverallVerdict = TrialFlagConstants.VerdictBadTrial;
                judgment.Severity = TrialFlagConstants.SeverityP4;
                return;
            }

            judgment.OverallVerdict = TrialFlagConstants.VerdictGoodTrial;
            judgment.Severity = TrialFlagConstants.SeverityClean;
        }

        private static TrialJudgmentResultVm Skipped(string reason) =>
            new TrialJudgmentResultVm { SkipReason = reason };
    }
}
