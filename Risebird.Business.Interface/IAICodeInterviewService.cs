using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IAICodeInterviewService
    {
        Task<List<AICodeInterviewQCItemVM>> GetOpenQCItems(string expertEmail);

        Task<List<AICodeInterviewQCItemVM>> GetMyQCItems(string expertEmail, bool? isCompleted = null);

        /// <summary>
        /// Status: 1 = newly assigned, 2 = already held by this expert, 3 = held by another expert,
        /// 0 = session is not an auditable AI code interview.
        /// </summary>
        Task<(int AssignmentId, int Status)> AssignExpert(Guid sessionId, string expertEmail, string assignedBy);

        /// <summary>
        /// Loads the audit for a session. The AI copy of the report is materialised from the stored
        /// assistant evaluation the first time this is called, so the expert always has a baseline
        /// to diff against.
        /// </summary>
        Task<AICodeInterviewAuditReportVM> GetAuditReport(Guid sessionId);

        /// <summary>
        /// Stores the expert copy of the report (ReportSource forced to Expert), recomputes the
        /// discrepancies against the AI baseline and stamps the outcome onto the assignment.
        /// Resubmission replaces the previous expert copy.
        /// </summary>
        Task<AICodeInterviewAuditSubmitResultVM> SubmitExpertAudit(AICodeInterviewReportVM expertReport, string user);

        /// <summary>
        /// Field by field diff of the expert copy against the AI report. Exposed so the comparison
        /// rules can be exercised directly.
        /// </summary>
        List<AICodeInterviewReportDiscrepancyVM> ComputeDiscrepancies(
            AICodeInterviewReportVM aiReport, AICodeInterviewReportVM expertReport);
    }
}
