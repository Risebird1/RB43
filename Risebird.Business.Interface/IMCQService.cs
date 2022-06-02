using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Business.Interface
{
    public interface IMCQService
    {
        string AddorUpdateMCQSet(MCQVMset _lstset);
        string AddMCQQuestions(List<MCQVM> _lstMCQ, string MCQId);
        IEnumerable<MCQVMset> FetchAllMCQSets();
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
        IEnumerable<CandidateDetailsForTestDuplicate> GetMCQCandidateDetailsByMCQId(string MCQPId);
        IEnumerable<CandidateDetailsForTestDuplicate> GetMCQCandidateDetailsByMCQTitle(string MCQtitle);
        IEnumerable<MCQVM> GetMCQCandidateQuestions(string CandidateId);
    }
}
