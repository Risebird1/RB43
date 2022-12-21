using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class MCQService : IMCQService
    {
        private readonly IMCQRepository _mCQRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        public MCQService(IMCQRepository mCQRepository, IApplicationErrorLogger applicationErrorLogger)
        {
            _mCQRepository = mCQRepository;
            _applicationErrorLogger = applicationErrorLogger;
        }
        public async Task<string> AddorUpdateMCQSet(MCQVMset _lstset)
        {
            try
            {
                return _mCQRepository.AddorUpdateMCQSet(_lstset).Result;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<string> AddMCQQuestions(List<MCQVM> _lstMCQ, string MCQId)
        {
            try
            {
                return await _mCQRepository.AddMCQQuestions(_lstMCQ, MCQId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<IEnumerable<MCQVMset>> FetchAllMCQSets(string clientname, string profileName, bool requesttype)
        {
            try
            {
                return await _mCQRepository.FetchAllMCQSets(clientname, profileName, requesttype);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<IEnumerable<MCQVMset>> FetchMCQSetById(string MCQPId)
        {
            try
            {
                return await _mCQRepository.FetchMCQSetById(MCQPId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<string> AddMCQSetShareList(MCQVMShare _list)
        {
            try
            {
                return await _mCQRepository.AddMCQSetShareList(_list);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<IEnumerable<MCQVMShare>> GetShareMCQEmailListByMCQId(string MCQPId)
        {
            try
            {
                return await _mCQRepository.GetShareMCQEmailListByMCQId(MCQPId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<string> UpdateShareMCQEmailStatusByid(int id, int status)
        {
            try
            {
                return await _mCQRepository.UpdateShareMCQEmailStatusByid(id, status);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<IEnumerable<CandidateDetailsForTest>> GetMCQCandidateListByMCQId(string MCQPId)
        {
            try
            {
                return await _mCQRepository.GetMCQCandidateListByMCQId(MCQPId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<IEnumerable<MCQVM>> GetMCQquestionsByMCQId(string MCQPId)
        {
            try
            {
                return await _mCQRepository.GetMCQquestionsByMCQId(MCQPId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<string> AddMCQCandidates(CandidateDetailsForTest _list)
        {
            try
            {
                return await _mCQRepository.AddMCQCandidates(_list);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<string> AddMCQCandidateQuestions(List<MCQVM> _question)
        {
            try
            {
                return await _mCQRepository.AddMCQCandidateQuestions(_question);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<string> DeleteMCQCandidate(string CSPId, string CPId)
        {
            try
            {
                return await _mCQRepository.DeleteMCQCandidate(CSPId, CPId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<string> DeleteMCQSet(string MCQPId)
        {
            try
            {
                return await _mCQRepository.DeleteMCQSet(MCQPId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<IEnumerable<CandidateDetailsForTestDuplicate>> GetMCQCandidateDetailsByMCQId(string MCQPId, bool requesttype)
        {
            try
            {
                return await _mCQRepository.GetMCQCandidateDetailsByMCQId(MCQPId, requesttype);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<IEnumerable<CandidateDetailsForTestDuplicate>> GetMCQCandidateDetailsByMCQTitle(string MCQtitle)
        {
            try
            {
                return await _mCQRepository.GetMCQCandidateDetailsByMCQTitle(MCQtitle);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<IEnumerable<MCQVM>> GetMCQCandidateQuestions(string CandidateId)
        {
            try
            {
                return await _mCQRepository.GetMCQCandidateQuestions(CandidateId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<string> UpdateVideoURLForCandidate(CandidateDetailsForTestDuplicate model)
        {
            try
            {
                return await _mCQRepository.UpdateVideoURLForCandidate(model);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<IEnumerable<CandidateDetailsForTestDuplicate>> GetMCQCandidateDetails()
        {
            try
            {
                return await _mCQRepository.GetMCQCandidateDetails();
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<bool> SaveMCQReportDetails(CandidateDetailsForTestDuplicate _candidate)
        {
            try
            {
                return await _mCQRepository.SaveMCQReportDetails(_candidate);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public async Task<CandidateDetailsForTestDuplicate> FetchMCQReportDetails(CandidateDetailsForTestDuplicate _candidate)
        {
            try
            {
                var selectibity = _mCQRepository.FetchMCQReportDetailsByCMCQId(_candidate.Id).Result;
                if (selectibity == null || selectibity.Count() == 0)
                {
                    var selectibity2 = await _mCQRepository.FetchMCQReportDetails(_candidate.SameID, _candidate.ApplyingCompany);
                    var Selectvalue = selectibity2.Select(x => x.CMCQId).FirstOrDefault();
                    selectibity2 = selectibity2.Where(x => x.CMCQId == Selectvalue).ToList();
                    _candidate.SelectabilityEvaluatorReportCollection = (List<SelectabilityEvaluatorReport>)selectibity2;
                    foreach (var item in _candidate.SelectabilityEvaluatorReportCollection)
                    {
                        item.LevelId = "0";
                        item.Value = "";
                    }
                }
                else
                {
                    _candidate.SelectabilityEvaluatorReportCollection = (List<SelectabilityEvaluatorReport>)selectibity;
                }
                var joinability = await _mCQRepository.FetchMCQjoinabilityDetailsByCMCQId(_candidate.Id);
                if (joinability == null || joinability.Count() == 0)
                {
                    var joinability2 = await _mCQRepository.FetchMCQjoinabilityDetails(_candidate.SameID, _candidate.ApplyingCompany);
                    var joinvalue = joinability2.Select(x => x.CMCQId).FirstOrDefault();
                    joinability2 = joinability2.Where(x => x.CMCQId == joinvalue).ToList();
                    _candidate.JoinabilityHRReportCollection = (List<JoinabilityHRReport>)joinability2;
                    foreach (var item in _candidate.JoinabilityHRReportCollection)
                    {
                        item.LevelId = "0";
                        item.Value = "";
                    }
                }
                else
                {
                    _candidate.JoinabilityHRReportCollection = (List<JoinabilityHRReport>)joinability;
                }
                return _candidate;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return _candidate;
            }
        }
        public async Task<MCQVMset> FetchMCQSetsForApi(MCQApiVM model)
        {
            try
            {
                var _mcqsetdata = await _mCQRepository.FetchMCQSetsForApi(model);
                var _mcqset = _mcqsetdata.ToList();
                if (_mcqset.Count() > 1 && model.SeniorityLevel > 0)
                {
                    _mcqset = _mcqset.Where(x => !string.IsNullOrWhiteSpace(x.SeniorityLevel) ? x.SeniorityLevel.Contains(model.SeniorityLevel.ToString()) : x.SeniorityLevel == null).ToList();
                    var random = new Random();
                    int index = random.Next(_mcqset.Count());
                    return _mcqset[index];
                }
                return new MCQVMset
                {
                    ErrorMessage = "No Matching Result Found"
                };
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<IEnumerable<CandidateDetailsForTestDuplicate>> GetMCQReportByMCQId(string MCQPId)
        {
            try
            {
                return await _mCQRepository.GetMCQReportByMCQId(MCQPId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<IEnumerable<MCQVMShare>> GetShareMCQEmailListByMCQIdPremium(string MCQPId)
        {
            try
            {
                return await _mCQRepository.GetShareMCQEmailListByMCQIdPremium(MCQPId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<string> UpdateShareMCQEmailStatusByidPremium(int id, int status)
        {
            try
            {
                return await _mCQRepository.UpdateShareMCQEmailStatusByidPremium(id, status);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<IEnumerable<CandidateDetailsForTest>> GetMCQCandidateListByMCQIdPremium(string MCQPId)
        {
            try
            {
                return await _mCQRepository.GetMCQCandidateListByMCQIdPremium(MCQPId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<string> AddMCQCandidatesPremium(CandidateDetailsForTest _list)
        {
            try
            {
                return await _mCQRepository.AddMCQCandidatesPremium(_list);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<string> AddMCQCandidateQuestionsPremium(List<MCQVM> _question)
        {
            try
            {
                return await _mCQRepository.AddMCQCandidateQuestionsPremium(_question);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<string> AddMCQSetShareListPremium(MCQVMShare _list)
        {
            try
            {
                return await _mCQRepository.AddMCQSetShareListPremium(_list);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<IEnumerable<MCQVMset>> FetchAllMCQSetsPremium(string clientname, string profileName, bool requesttype)
        {
            try
            {
                return await _mCQRepository.FetchAllMCQSetsPremium(clientname, profileName, requesttype);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<IEnumerable<CandidateDetailsForTestDuplicate>> GetMCQCandidateDetailsByMCQIdPremium(string MCQPId)
        {
            try
            {
                return await _mCQRepository.GetMCQCandidateDetailsByMCQIdPremium(MCQPId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<IEnumerable<CandidateDetailsForTestDuplicate>> GetMCQReportByMCQIdPremium(string MCQPId)
        {
            try
            {
                return await _mCQRepository.GetMCQReportByMCQIdPremium(MCQPId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<IEnumerable<MCQVM>> GetMCQCandidateQuestionsPremium(string CandidateId)
        {
            try
            {
                return await _mCQRepository.GetMCQCandidateQuestionsPremium(CandidateId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<string> UpdateVideoURLForCandidatePremium(CandidateDetailsForTestDuplicate model)
        {
            try
            {
                return await _mCQRepository.UpdateVideoURLForCandidatePremium(model);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<int> FetchTotalInviteAllowed(string clientemail)
        {
            try
            {
                return await _mCQRepository.FetchTotalInviteAllowed(clientemail);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return 0;
            }
        }
        public async Task<int> FetchTotalInviteSent(string clientemail)
        {
            try
            {
                return await _mCQRepository.FetchTotalInviteSent(clientemail);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return 0;
            }
        }
    }
}
