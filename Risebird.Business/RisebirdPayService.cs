using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;

namespace Risebird.Business
{

    public class RisebirdPayService : IRisebirdPayService
    {
        private IRisebirdPayRepository _risebirdPayRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        public RisebirdPayService(IRisebirdPayRepository risebirdPayRepository, IApplicationErrorLogger applicationErrorLogger)
        {
            _risebirdPayRepository = risebirdPayRepository;
            _applicationErrorLogger = applicationErrorLogger;
        }
        public IEnumerable<Wallet> FetchClientDetails()
        {
            try
            {
                var result = _risebirdPayRepository.FetchClientDetails();
                return result;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public string UpdateClient(int PerSuccessRate, int ClientId)
        {
            try
            {
                var result = _risebirdPayRepository.UpdateClient(PerSuccessRate, ClientId);
                return result;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "failed";
            }
        }
        public string DeductWalletPoints(int ClientId, string emailid, int points, string comment)
        {
            try
            {
                var result = _risebirdPayRepository.DeductWalletPoints(ClientId, emailid, points, comment);
                return result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "failed";
            }
        }
    }
}
