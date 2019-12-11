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
        Candidate GetIRFCandidate(int id);
    }
}
