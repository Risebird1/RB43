using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.Data;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class CandidateService : ICandidateService
    {
        private readonly ICandidateRepository _candidateRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        public CandidateService(ICandidateRepository candidateRepository, IApplicationErrorLogger applicationErrorLogger)
        {
            _candidateRepository = candidateRepository;
            _applicationErrorLogger = applicationErrorLogger;
        }

        public async Task<IEnumerable<CandidateTimelineData>> GetCandidateTimeline()
        {
            try
            {
                return await _candidateRepository.GetCandidateTimeline();
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
    }
}
