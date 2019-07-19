using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Business
{
    public class IRFService :IIRFService
    {
        private IIRFRepository _irfRepository;
        private IApplicationErrorLogger _applicationErrorLogger;

        public IRFService(IIRFRepository irfRepository, IApplicationErrorLogger applicationErrorLogger)
        {
            _irfRepository = irfRepository;
            _applicationErrorLogger = applicationErrorLogger;
        }

        #region IRF Functions
        public bool AddIRFDetails(IRFVm irfDetails)
        {
            try
            {
                return _irfRepository.AddIRFDetails(irfDetails).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }


        #endregion
    }
}
