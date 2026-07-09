using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class RiskCategoryService : IRiskCategoryService
    {
        private readonly IRiskCategoryRepository _riskCategoryRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public RiskCategoryService(IRiskCategoryRepository riskCategoryRepository, IApplicationErrorLogger applicationErrorLogger)
        {
            _riskCategoryRepository = riskCategoryRepository;
            _applicationErrorLogger = applicationErrorLogger;
        }

        public async Task<List<RiskCategory>> GetRiskCategories(DateTime startDate, DateTime endDate)
        {
            try
            {
                var data = await _riskCategoryRepository.GetRiskCategories(startDate, endDate);
                return data?.ToList();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<RiskCategory> GetRiskCategoryByInterviewId(int interviewId)
        {
            try
            {
                return await _riskCategoryRepository.GetRiskCategoryByInterviewId(interviewId);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<bool> AddUpdateRiskCategory(int interviewId)
        {
            try
            {
                return await _riskCategoryRepository.AddUpdateRiskCategory(interviewId);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
    }
}
