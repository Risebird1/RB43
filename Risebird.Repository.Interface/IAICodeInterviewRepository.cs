using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IAICodeInterviewRepository
    {
        Task<List<AICodeInterviewQCItemVM>> GetOpenQCItems(string expertEmail);

        Task<List<AICodeInterviewQCItemVM>> GetMyQCItems(string expertEmail, bool? isCompleted = null);

        /// <summary>
        /// Status: 1 = newly assigned, 2 = already held by this expert, 3 = held by another expert,
        /// 0 = session is not an auditable AI code interview.
        /// </summary>
        Task<(int AssignmentId, int Status)> AssignExpert(string sessionId, string expertEmail, string assignedBy);

        /// <summary>Upserts one report copy and replaces its per question rows. Returns the report id.</summary>
        Task<int> SaveReport(AICodeInterviewReportVM report, string user);

        Task<AICodeInterviewAuditReportVM> GetAuditReport(string sessionId);

        Task<(bool WasDiscrepancyFound, int MaxSeverity)> SaveDiscrepancies(
            string sessionId, int? assignmentId, int? expertReportId,
            List<AICodeInterviewReportDiscrepancyVM> discrepancies);
    }
}
