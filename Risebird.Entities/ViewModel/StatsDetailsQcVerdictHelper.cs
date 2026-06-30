using Risebird.Entities.Data;
using System;

namespace Risebird.Entities.ViewModel
{
    /// <summary>Display labels for QCMergedAIDetails verdict on Completed Interviews rows.</summary>
    public static class StatsDetailsQcVerdictHelper
    {
        public const string DisplaySuspicious = "Suspicious";
        public const string DisplayRedo = "Redo";
        public const string DisplayPending = "Pending";

        /// <summary>
        /// Maps latest <see cref="QcInterviewAIevaluation"/> row to a table cell label.
        /// Returns null when the cell should show blank (—).
        /// </summary>
        public static string ResolveDisplay(QcInterviewAIevaluation evaluation)
        {
            if (evaluation == null)
                return null;

            if (evaluation.QCDone != true)
                return DisplayPending;

            var suspicious = evaluation.AIMergeInterviewerSuspicious;
            var redo = evaluation.AIMergeRedoInterview;

            if (!string.IsNullOrWhiteSpace(evaluation.MergedAIQCDetails))
            {
                var merged = QcInterviewAIMergeAssistantResponse.FromMergedDetailsJson(evaluation.MergedAIQCDetails);
                if (merged != null)
                {
                    if (!DmIvAutoQcAuditConstants.IsAffirmativeSuspicious(suspicious)
                        && DmIvAutoQcAuditConstants.IsAffirmativeSuspicious(merged.interviewer_suspicious))
                    {
                        suspicious = merged.interviewer_suspicious;
                    }
                    else if (string.IsNullOrWhiteSpace(suspicious))
                    {
                        suspicious = merged.interviewer_suspicious;
                    }

                    if (redo != true && merged.RedoInterview == true)
                        redo = merged.RedoInterview;
                }
            }

            var normalized = DmIvAutoQcAuditConstants.ResolveVerdict(suspicious, redo);
            if (string.Equals(normalized, DmIvAutoQcAuditConstants.VerdictSuspicious, StringComparison.OrdinalIgnoreCase))
                return DisplaySuspicious;
            if (string.Equals(normalized, DmIvAutoQcAuditConstants.VerdictRedo, StringComparison.OrdinalIgnoreCase))
                return DisplayRedo;

            return null;
        }

        public static bool IsLinkable(string display) =>
            display == DisplaySuspicious || display == DisplayRedo;
    }
}
