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
        //IEnumerable<SelectabilityEvaluatorReport> GetSelectabilityReport(int id);
        Task<bool> AddSelectabilityReport(EvaluatorReport reportDetails);
        Task<IEnumerable<CandidateHREvaluation>> GetEvaluationsHR(string emailID);
        string AssignYourselfJoinability(int id, string mailId);
        Task<IEnumerable<CandidateHREvaluation>> GetAssignedJoinabilityEvaluations(string emailID);
        bool UnAssignJoinability(int id);
        IEnumerable<JoinabilityHRReport> GetJoinabilityReport(int id, int irfId);
        Task<bool> AddJoinabilityReport(HRReport reportDetails);
        string GetSelectabilityComments(int irfId, string emailId);
        string GetJoinabilityComments(int irfId, string emailId);
        IEnumerable<SelectabilityEvaluatorReport> GetSelectabilityReport(int id, int irfId);
        IEnumerable<TechSkillEvaluatorReport> GetTechReport(int candidateId, int irfId);
        string GetTechComments(int irfId, string emailId);
        string GetGeneralSkillComments(int irfId, string emailId);
        IEnumerable<LevelTech> GetLevelTech();
        Task<bool> AddTechReport(IList<TechSkillEvaluatorReport> reportDetails, int candidateId, int irfId);
        IEnumerable<SkillGeneralEvaluatorReport> GetSkillGeneralReport(int candidateId, int irfId);
        Task<bool> AddSkillGeneralReport(IList<SkillGeneralEvaluatorReport> reportDetails, int candidateId, int irfId);
        Task<IEnumerable<CandidateRecruiter>> GetRecruiterCandidates(int userId);
        int GetUserId(string emailId, string profile);
        CandidateAudio GetCandidate(int id, int irfId);
        Task<bool> AddCandidateAudio(CandidateAudio obj);
        IEnumerable<CandidateAudio> GetCandidateAudios(int candidateId, int irfId);
    }
}
