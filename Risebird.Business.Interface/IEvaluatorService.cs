using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IEvaluatorService
    {
        List<CandidateResumeEvaluation> GetOpenEvaluations(string emailId);
        string AssignSelectabilityYourself(int id, string mailId);
        List<CandidateResumeEvaluation> GetAssignedSelectabilityEvaluations(string emailID);
        bool UnAssignSelectability(int id);
        Task<EvaluatorReport> GetEvaluatorReport(int candidateId, int irfId);
        Task<bool> AddSelectabilityReport(EvaluatorReport reportModel);
        List<CandidateHREvaluation> GetEvaluationsHR(string emailId);
        string AssignJoinabilityYourself(int id, string mailId);
        List<CandidateHREvaluation> GetAssignedJoinabilityEvaluations(string emailID);
        bool UnAssignJoinability(int id);
        Task<HRReport> GetHRJoinabilityReport(int candidateId, int irfId);
        Task<bool> AddJoinabilityReport(HRReport reportModel);

    }
}
