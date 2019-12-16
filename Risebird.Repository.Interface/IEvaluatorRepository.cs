using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IEvaluatorRepository
    {
        Task<IEnumerable<CandidateResumeEvaluation>> GetOpenEvaluations(string emailID);
        string AssignYourselfSelectability(int id, string mailId);
        Task<IEnumerable<CandidateResumeEvaluation>> GetAssignedSelectabilityEvaluations(string emailID);
        bool UnAssignSelectability(int id);
        IEnumerable<TransSelectability> GetTransSelectability(int id);
        Candidate GetCandidate(int id);
        IEnumerable<SelectabilityEvaluatorReport> GetSelectabilityReport(int id);
        Task<bool> AddSelectabilityReport(EvaluatorReport reportDetails);
        Task<IEnumerable<CandidateHREvaluation>> GetEvaluationsHR(string emailID);
        string AssignYourselfJoinability(int id, string mailId);
        Task<IEnumerable<CandidateHREvaluation>> GetAssignedJoinabilityEvaluations(string emailID);
        bool UnAssignJoinability(int id);
        IEnumerable<JoinabilityHRReport> GetJoinabilityReport(int id);
        Task<bool> AddJoinabilityReport(HRReport reportDetails);
    }
}
