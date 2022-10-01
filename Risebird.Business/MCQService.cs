using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

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
        public string AddorUpdateMCQSet(MCQVMset _lstset)
        {
            try
            {
                return _mCQRepository.AddorUpdateMCQSet(_lstset).Result;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public string AddMCQQuestions(List<MCQVM> _lstMCQ, string MCQId)
        {
            try
            {
                return _mCQRepository.AddMCQQuestions(_lstMCQ, MCQId).Result;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public IEnumerable<MCQVMset> FetchAllMCQSets(string clientname, string profileName)
        {
            try
            {
                return _mCQRepository.FetchAllMCQSets(clientname, profileName).Result;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public IEnumerable<MCQVMset> FetchMCQSetById(string MCQPId)
        {
            try
            {
                return _mCQRepository.FetchMCQSetById(MCQPId).Result;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public string AddMCQSetShareList(MCQVMShare _list)
        {
            try
            {
                return _mCQRepository.AddMCQSetShareList(_list).Result;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public IEnumerable<MCQVMShare> GetShareMCQEmailListByMCQId(string MCQPId)
        {
            try
            {
                return _mCQRepository.GetShareMCQEmailListByMCQId(MCQPId).Result;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public string UpdateShareMCQEmailStatusByid(int id, int status)
        {
            try
            {
                return _mCQRepository.UpdateShareMCQEmailStatusByid(id, status).Result;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public IEnumerable<CandidateDetailsForTest> GetMCQCandidateListByMCQId(string MCQPId)
        {
            try
            {
                return _mCQRepository.GetMCQCandidateListByMCQId(MCQPId).Result;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public IEnumerable<MCQVM> GetMCQquestionsByMCQId(string MCQPId)
        {
            try
            {
                return _mCQRepository.GetMCQquestionsByMCQId(MCQPId).Result;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public string AddMCQCandidates(CandidateDetailsForTest _list)
        {
            try
            {
                return _mCQRepository.AddMCQCandidates(_list).Result;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public string AddMCQCandidateQuestions(List<MCQVM> _question)
        {
            try
            {
                return _mCQRepository.AddMCQCandidateQuestions(_question).Result;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public string DeleteMCQCandidate(string CSPId, string CPId)
        {
            try
            {
                return _mCQRepository.DeleteMCQCandidate(CSPId, CPId).Result;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public string DeleteMCQSet(string MCQPId)
        {
            try
            {
                return _mCQRepository.DeleteMCQSet(MCQPId).Result;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public IEnumerable<CandidateDetailsForTestDuplicate> GetMCQCandidateDetailsByMCQId(string MCQPId)
        {
            try
            {
                return _mCQRepository.GetMCQCandidateDetailsByMCQId(MCQPId).Result;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public IEnumerable<CandidateDetailsForTestDuplicate> GetMCQCandidateDetailsByMCQTitle(string MCQtitle)
        {
            try
            {
                return _mCQRepository.GetMCQCandidateDetailsByMCQTitle(MCQtitle).Result;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public IEnumerable<MCQVM> GetMCQCandidateQuestions(string CandidateId)
        {
            try
            {
                return _mCQRepository.GetMCQCandidateQuestions(CandidateId).Result;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public string UpdateVideoURLForCandidate(CandidateDetailsForTestDuplicate model)
        {
            try
            {
                return _mCQRepository.UpdateVideoURLForCandidate(model).Result;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public IEnumerable<CandidateDetailsForTestDuplicate> GetMCQCandidateDetails()
        {
            try
            {
                return _mCQRepository.GetMCQCandidateDetails().Result;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public bool SaveMCQReportDetails(CandidateDetailsForTestDuplicate _candidate)
        {
            try
            {
                return _mCQRepository.SaveMCQReportDetails(_candidate).Result;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public CandidateDetailsForTestDuplicate FetchMCQReportDetails(CandidateDetailsForTestDuplicate _candidate)
        {
            try
            {
                var selectibity = _mCQRepository.FetchMCQReportDetailsByCMCQId(_candidate.Id).Result;
                if (selectibity == null || selectibity.Count() == 0)
                {
                    var selectibity2 = _mCQRepository.FetchMCQReportDetails(_candidate.SameID, _candidate.ApplyingCompany).Result;
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
                var joinability = _mCQRepository.FetchMCQjoinabilityDetailsByCMCQId(_candidate.Id).Result;
                if (joinability == null || joinability.Count() == 0)
                {
                    var joinability2 = _mCQRepository.FetchMCQjoinabilityDetails(_candidate.SameID, _candidate.ApplyingCompany).Result;
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
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return _candidate;
            }
        }
        public MCQVMset FetchMCQSetsForApi(MCQApiVM model)
        {
            try
            {
                var _mcqset = _mCQRepository.FetchMCQSetsForApi(model).Result.ToList();
                if (_mcqset.Count() > 1 && model.SeniorityLevel > 0)
                {
                    _mcqset = _mcqset.Where(x => !String.IsNullOrWhiteSpace(x.SeniorityLevel) ? x.SeniorityLevel.Contains(model.SeniorityLevel.ToString()) : x.SeniorityLevel == null).ToList();
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
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
    }
}
