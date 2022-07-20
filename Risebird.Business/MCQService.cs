using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;

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
    }
}
