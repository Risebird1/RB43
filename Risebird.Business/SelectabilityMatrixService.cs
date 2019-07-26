using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Risebird.Business
{
    public class SelectabilityMatrixService : ISelectabilityMatrixService
    {
        private ISelectabilityMatrixRepository _selectmatrixRepository;
        private IApplicationErrorLogger _applicationErrorLogger;

        public SelectabilityMatrixService(ISelectabilityMatrixRepository selectmatrixRepository, IApplicationErrorLogger applicationErrorLogger)
        {
            _selectmatrixRepository = selectmatrixRepository;
            _applicationErrorLogger = applicationErrorLogger;
        }

        public List<SelectabilityMatrix> GetSelectabilityMatrix()
        {
            try
            {
                return _selectmatrixRepository.GetSelectabilityMatrix().ToList();
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public bool AddSelectabilityMatrix(SelectabilityMatrix maxtrix)
        {
            try
            {
                return _selectmatrixRepository.AddSelectabilityMatrix(maxtrix).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public SelectabilityMatrix GetSelectabilityMatrixById(int id)
        {
            try
            {
                if (_selectmatrixRepository.GetSelectabilityMatrixById(id) == null)
                    return null;
                else
                    return _selectmatrixRepository.GetSelectabilityMatrixById(id).Result;

            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public bool UpdateSelectabilityMatrix(SelectabilityMatrix selectableToUpdate)
        {
            try
            {
                return _selectmatrixRepository.UpdateSelectabilityMatrix(selectableToUpdate).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public bool DeleteSelectabilityMatrix(int id)
        {
            try
            {
                _selectmatrixRepository.DeleteSelectabilityMatrix(id);
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
