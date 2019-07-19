using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Risebird.Business.Interface;
using Risebird.Entities.Data;
using Risebird.Repository.Interface;
using Risebird.Entities.ViewModel;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;

namespace Risebird.Business
{
    public class JoinabilityMatrixService : IJoinabilityMatrixService
    {
        private IJoinabilityMatrixRepository _joinmatrixRepository;
        private IApplicationErrorLogger _applicationErrorLogger;

        public JoinabilityMatrixService(IJoinabilityMatrixRepository joinmatrixRepository, IApplicationErrorLogger applicationErrorLogger)
        {
            _joinmatrixRepository = joinmatrixRepository;
            _applicationErrorLogger = applicationErrorLogger;
        }
        public List<JoinabilityMatrix> GetJoinabilityMatrix()
        {
            try
            {
                return _joinmatrixRepository.GetJoinabilityMatrix().ToList();
            }
            catch (Exception e)
            {
                return null;
            }
        }
        public bool AddJoinabilityMatrix(JoinabilityMatrix maxtrix)
        {
            try
            {
                return _joinmatrixRepository.AddJoinabilityMatrix(maxtrix).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public JoinabilityMatrix GetJoinabilityMatrixById(int id)
        {
            try
            {
                if (_joinmatrixRepository.GetJoinabilityMatrixById(id) == null)
                    return null;
                else
                    return _joinmatrixRepository.GetJoinabilityMatrixById(id).Result;

            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public bool UpdateJoinabilityMatrix(JoinabilityMatrix joinableToUpdate)
        {
            try
            {
                return _joinmatrixRepository.UpdateJoinabilityMatrix(joinableToUpdate).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public bool DeleteJoinabilityMatrix(int id)
        {
            try
            {
                _joinmatrixRepository.DeleteJoinabilityMatrix(id);
                return true;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
    }
}
