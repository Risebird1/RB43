using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class AIInterviewerService : IAIInterviewerService
    {
        private readonly IAIInterviewerRepository _aiInterviewerRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        public AIInterviewerService(IAIInterviewerRepository aiInterviewerRepository, IApplicationErrorLogger applicationErrorLogger)
        {
            _aiInterviewerRepository = aiInterviewerRepository;
            _applicationErrorLogger = applicationErrorLogger;
        }
        public async Task<bool> UpdateBotInterviewVideoDetails(string VideoLink, string question, string SessionId)
        {
            try
            {
                return await _aiInterviewerRepository.UpdateBotInterviewVideoDetails(VideoLink, question, SessionId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
    }
}
