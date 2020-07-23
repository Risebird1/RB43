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
    public class GeneralSkillMatrixService : IGeneralSkillMatrixService
    {
        private readonly IGeneralSkillMatrixRepository _GeneralRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public GeneralSkillMatrixService(IGeneralSkillMatrixRepository GeneralRepository, IApplicationErrorLogger applicationErrorLogger)
        {
            _GeneralRepository = GeneralRepository;
            _applicationErrorLogger = applicationErrorLogger;
        }
        public List<GeneralSkillMatrix> GetGeneralSkillMatrix()
        {
            try
            {
                return _GeneralRepository.GetGeneralSkillMatrix().ToList();
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public bool AddGeneralSkillMatrix(GeneralSkillMatrix maxtrix)
        {
            try
            {
                return _GeneralRepository.AddGeneralSkillMatrix(maxtrix).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public GeneralSkillMatrix GetGeneralSkillMatrixByiId(int id)
        {
            try
            {
                if (_GeneralRepository.GetGeneralSkillMatrixByiId(id) == null)
                    return null;
                else
                    return _GeneralRepository.GetGeneralSkillMatrixByiId(id).Result;

            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public bool UpdateGeneralSkillMatrix(GeneralSkillMatrix generalToUpdate)
        {
            try
            {
                return _GeneralRepository.UpdateGeneralSkillMatrix(generalToUpdate).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public bool DeleteGeneralSkillMatrix(int id)
        {
            try
            {
                _GeneralRepository.DeleteGeneralSkillMatrix(id);
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
