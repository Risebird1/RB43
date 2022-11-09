using Risebird.Entities.ViewModel;
using System.Collections.Generic;

namespace Risebird.Business.Interface
{
    public interface IMCQService
    {
        string AddorUpdateMCQSet(MCQVMset _lstset);
        string AddMCQQuestions(List<MCQVM> _lstMCQ, string MCQId);
        IEnumerable<MCQVMset> FetchAllMCQSets(string clientname, string profileName, bool requesttype);
        IEnumerable<MCQVMset> FetchMCQSetById(string MCQPId);
        string AddMCQSetShareList(MCQVMShare _list);
        IEnumerable<MCQVMShare> GetShareMCQEmailListByMCQId(string MCQPId);
        string UpdateShareMCQEmailStatusByid(int id, int status);
        IEnumerable<CandidateDetailsForTest> GetMCQCandidateListByMCQId(string MCQPId);
        IEnumerable<MCQVM> GetMCQquestionsByMCQId(string MCQPId);
        string AddMCQCandidates(CandidateDetailsForTest _list);
        string AddMCQCandidateQuestions(List<MCQVM> _question);
        string DeleteMCQCandidate(string CSPId, string CPId);
        string DeleteMCQSet(string MCQPId);
        IEnumerable<CandidateDetailsForTestDuplicate> GetMCQCandidateDetailsByMCQId(string MCQPId, bool requesttype);
        IEnumerable<CandidateDetailsForTestDuplicate> GetMCQCandidateDetailsByMCQTitle(string MCQtitle);
        IEnumerable<MCQVM> GetMCQCandidateQuestions(string CandidateId);
        string UpdateVideoURLForCandidate(CandidateDetailsForTestDuplicate model);
        IEnumerable<CandidateDetailsForTestDuplicate> GetMCQCandidateDetails();
        bool SaveMCQReportDetails(CandidateDetailsForTestDuplicate _candidate);
        CandidateDetailsForTestDuplicate FetchMCQReportDetails(CandidateDetailsForTestDuplicate _candidate);
        MCQVMset FetchMCQSetsForApi(MCQApiVM model);
        IEnumerable<CandidateDetailsForTestDuplicate> GetMCQReportByMCQId(string MCQPId);
        IEnumerable<MCQVMShare> GetShareMCQEmailListByMCQIdPremium(string MCQPId);
        string UpdateShareMCQEmailStatusByidPremium(int id, int status);
        IEnumerable<CandidateDetailsForTest> GetMCQCandidateListByMCQIdPremium(string MCQPId);
        string AddMCQCandidatesPremium(CandidateDetailsForTest _list);
        string AddMCQCandidateQuestionsPremium(List<MCQVM> _question);
        string AddMCQSetShareListPremium(MCQVMShare _list);
        IEnumerable<MCQVMset> FetchAllMCQSetsPremium(string clientname, string profileName, bool requesttype);
        IEnumerable<CandidateDetailsForTestDuplicate> GetMCQCandidateDetailsByMCQIdPremium(string MCQPId);
        IEnumerable<CandidateDetailsForTestDuplicate> GetMCQReportByMCQIdPremium(string MCQPId);
        IEnumerable<MCQVM> GetMCQCandidateQuestionsPremium(string CandidateId);
        string UpdateVideoURLForCandidatePremium(CandidateDetailsForTestDuplicate model);
        int FetchTotalInviteAllowed(string clientemail);
        int FetchTotalInviteSent(string clientemail);
    }
}
