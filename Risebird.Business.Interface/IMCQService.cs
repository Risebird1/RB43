using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IMCQService
    {
        Task<string> AddorUpdateMCQSet(MCQVMset _lstset);
        Task<string> AddMCQQuestions(List<MCQVM> _lstMCQ, string MCQId);
        Task<IEnumerable<MCQVMset>> FetchAllMCQSets(string clientname, string profileName, bool requesttype);
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
        Task<IEnumerable<CandidateDetailsForTestDuplicate>> GetMCQCandidateDetailsByMCQId(string MCQPId, bool requesttype);
        Task<IEnumerable<CandidateDetailsForTestDuplicate>> GetMCQCandidateDetailsByMCQTitle(string MCQtitle);
        Task<IEnumerable<MCQVM>> GetMCQCandidateQuestions(string CandidateId);
        Task<string> UpdateVideoURLForCandidate(CandidateDetailsForTestDuplicate model);
        Task<IEnumerable<CandidateDetailsForTestDuplicate>> GetMCQCandidateDetails();
        Task<bool> SaveMCQReportDetails(CandidateDetailsForTestDuplicate _candidate);
        Task<CandidateDetailsForTestDuplicate> FetchMCQReportDetails(CandidateDetailsForTestDuplicate _candidate);
        Task<MCQVMset> FetchMCQSetsForApi(MCQApiVM model);
        Task<IEnumerable<CandidateDetailsForTestDuplicate>> GetMCQReportByMCQId(string MCQPId);
        Task<IEnumerable<MCQVMShare>> GetShareMCQEmailListByMCQIdPremium(string MCQPId);
        Task<string> UpdateShareMCQEmailStatusByidPremium(int id, int status);
        Task<IEnumerable<CandidateDetailsForTest>> GetMCQCandidateListByMCQIdPremium(string MCQPId);
        Task<string> AddMCQCandidatesPremium(CandidateDetailsForTest _list);
        Task<string> AddMCQCandidateQuestionsPremium(List<MCQVM> _question);
        Task<string> AddMCQSetShareListPremium(MCQVMShare _list);
        Task<IEnumerable<MCQVMset>> FetchAllMCQSetsPremium(string clientname, string profileName, bool requesttype);
        Task<IEnumerable<CandidateDetailsForTestDuplicate>> GetMCQCandidateDetailsByMCQIdPremium(string MCQPId);
        Task<IEnumerable<CandidateDetailsForTestDuplicate>> GetMCQReportByMCQIdPremium(string MCQPId);
        Task<IEnumerable<MCQVM>> GetMCQCandidateQuestionsPremium(string CandidateId);
        Task<string> UpdateVideoURLForCandidatePremium(CandidateDetailsForTestDuplicate model);
        Task<int> FetchTotalInviteAllowed(string clientemail);
        Task<int> FetchTotalInviteSent(string clientemail);
    }
}
