using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
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
        public async Task<bool> UpdateBotInterviewVideoDetails(string VideoLink, string question, string SessionId, string botSynthesizedVideoLink, string candidateResponse)
        {
            try
            {
                return await _aiInterviewerRepository.UpdateBotInterviewVideoDetails(VideoLink, question, SessionId,botSynthesizedVideoLink, candidateResponse);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<string> GetThreadId(Guid sessionId)
        {
            try
            {
                return await _aiInterviewerRepository.GetThreadId(sessionId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<List<BotInterviewQuestionVideos>> GetBotInterviewVideoDetails(Guid sessionId)
        {
            try
            {
                return await _aiInterviewerRepository.GetBotInterviewVideoDetails(sessionId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
    }
}
