using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IMCQRepository
    {
        Task<string> AddorUpdateMCQSet(MCQVMset _lstset);
        Task<string> AddMCQQuestions(List<MCQVM> _lstMCQ, string MCQId);
        Task<IEnumerable<MCQVMset>> FetchAllMCQSets(string clientname, string profileName);
        Task<IEnumerable<MCQVMset>> FetchMCQSetById(string MCQPId);
        Task<string> AddMCQSetShareList(MCQVMShare _list);
        Task<IEnumerable<MCQVMShare>> GetShareMCQEmailListByMCQId(string MCQPId);
        Task<string> UpdateShareMCQEmailStatusByid(int id, int status);
        Task<IEnumerable<CandidateDetailsForTest>> GetMCQCandidateListByMCQId(string MCQPId);
        Task<IEnumerable<MCQVM>> GetMCQquestionsByMCQId(string MCQPId);
        Task<string> AddMCQCandidates(CandidateDetailsForTest _list);
        Task<string> AddMCQCandidateQuestions(List<MCQVM> _question);
        Task<string> DeleteMCQCandidate(string CSPId, string CPId);
        Task<string> DeleteMCQSet(string MCQPId);
        Task<IEnumerable<CandidateDetailsForTestDuplicate>> GetMCQCandidateDetailsByMCQId(string MCQPId);
        Task<IEnumerable<CandidateDetailsForTestDuplicate>> GetMCQCandidateDetailsByMCQTitle(string MCQtitle);
        Task<IEnumerable<MCQVM>> GetMCQCandidateQuestions(string CandidateId);
        Task<string> UpdateVideoURLForCandidate(CandidateDetailsForTestDuplicate model);
        Task<IEnumerable<CandidateDetailsForTestDuplicate>> GetMCQCandidateDetails();
        Task<bool> SaveMCQReportDetails(CandidateDetailsForTestDuplicate _candidate);
        Task<IEnumerable<SelectabilityEvaluatorReport>> FetchMCQReportDetailsByCMCQId(string CMCQId);
        Task<IEnumerable<SelectabilityEvaluatorReport>> FetchMCQReportDetails(string MCQId, string ApplyingCompany);
        Task<IEnumerable<JoinabilityHRReport>> FetchMCQjoinabilityDetailsByCMCQId(string CMCQId);
        Task<IEnumerable<JoinabilityHRReport>> FetchMCQjoinabilityDetails(string MCQId, string ApplyingCompany);
        Task<IEnumerable<MCQVMset>> FetchMCQSetsForApi(MCQApiVM model);
    }
}
