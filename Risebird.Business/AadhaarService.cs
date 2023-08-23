using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class AadhaarService : IAadhaarService
    {
        private readonly IAadhaarRepository _aadhaarRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public AadhaarService(IAadhaarRepository aadhaarRepository, IApplicationErrorLogger applicationErrorLogger)
        {
            _aadhaarRepository = aadhaarRepository;
            _applicationErrorLogger = applicationErrorLogger;
        }
        public async Task<bool> SaveAadhaarDetails(AadhaarData model)
        {
            try
            {
                return await _aadhaarRepository.SaveAadhaarDetails(model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }
        public async Task<AadhaarData> FetchAadhaarDetails(string MCQId, string candidateEmail)
        {
            try
            {
                return await _aadhaarRepository.FetchAadhaarDetails(MCQId, candidateEmail);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<AadhaarDetails> FetchCandidateEmailidByUniqueId(string uniqueid)
        {
            try
            {
                return await _aadhaarRepository.FetchCandidateEmailidByUniqueId(uniqueid);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<string> FetchUniqueIdByEmailid(string emailid)
        {
            try
            {
                return await _aadhaarRepository.FetchUniqueIdByEmailid(emailid);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return "";
        }
        public async Task<AadhaarData> FetchInterviewAadhaarDetails(string candidateEmail)
        {
            try
            {
                return await _aadhaarRepository.FetchInterviewAadhaarDetails(candidateEmail);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<IEnumerable<AadhaarData>> GetAadharDetails()
        {
            try
            {
                return await _aadhaarRepository.GetAadharDetails();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<bool> SaveCandidateVerificationDetails(candidateDetailsresult model)
        {
            try
            {
                return await _aadhaarRepository.SaveCandidateVerificationDetails(model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }
        public async Task<AadhaarData> FetchInterviewCandidateDetails(string candidateEmail)
        {
            try
            {
                return await _aadhaarRepository.FetchInterviewCandidateDetails(candidateEmail);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<IEnumerable<EmploymentDetailsDM>> GetEmployementDetails()
        {
            try
            {
                return await _aadhaarRepository.GetEmployementDetails();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<int> CheckEmployementDetails(string mobilenumber)
        {
            try
            {
                return await _aadhaarRepository.CheckEmployementDetails(mobilenumber);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
            }
            return 0;
        }
        public async Task<EmploymentDetailsDM> GetEmployementDetailsByMobileNumber(string mobilenumber)
        {
            try
            {
                return await _aadhaarRepository.GetEmployementDetailsByMobileNumber(mobilenumber);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
            }
            return null;
        }
        public async Task SaveEmploymentDetails(SignzyEmployeeResponse model)
        {
            try
            {
                await _aadhaarRepository.SaveEmploymentDetails(model);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
            }
        }
    }
}
