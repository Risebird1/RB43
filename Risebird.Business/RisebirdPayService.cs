using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
        public string DeductWalletPoints(int ClientId, string emailid, int points, string comment)
        {
            try
            {
                var result = _risebirdPayRepository.DeductWalletPoints(ClientId, emailid, points, comment).Result;
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
