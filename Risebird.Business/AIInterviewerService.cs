using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
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
        public async Task<bool> UpdateBotInterviewVideoDetails(string VideoLink, string question, string SessionId, string botSynthesizedVideoLink, string candidateResponse, int secondsLeft, int interviewId, string BotAudioLink = null)
        {
            try
            {
                return await _aiInterviewerRepository.UpdateBotInterviewVideoDetails(VideoLink, question, SessionId,botSynthesizedVideoLink, candidateResponse, secondsLeft, interviewId, BotAudioLink);
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

        public async Task<bool> UpdateTimeLeft(Guid sessionId, int secondsLeft, int interviewId)
        {
            try
            {
                return await _aiInterviewerRepository.UpdateTimeLeft(sessionId, secondsLeft, interviewId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public async Task<MeetingVM> GetAIInterviewDetailsBySessionId(Guid sessionId)
        {
            try
            {
                return await _aiInterviewerRepository.GetAIInterviewDetailsBySessionId(sessionId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<IEnumerable<TranscriptConv>> GetAIInterviewTranscriptBySessionId(Guid sessionId)
        {
            try
            {
                return await _aiInterviewerRepository.GetAIInterviewTranscriptBySessionId(sessionId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<bool> SaveReportOfAIInterviewByAssistant(Guid sessionId, string serializedReportOfAIInterviewByAssistant)
        {
            try
            {
                return await _aiInterviewerRepository.SaveReportOfAIInterviewByAssistant(sessionId, serializedReportOfAIInterviewByAssistant);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public async Task<string> GetReportOfAIInterviewByAssistant(Guid sessionId)
        {
            try
            {
                return await _aiInterviewerRepository.GetReportOfAIInterviewByAssistant(sessionId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task LogTranscript(Guid sessionId, string role, string message)
        {
            try
            {
                await _aiInterviewerRepository.LogTranscript(sessionId, role, message);
                return;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return;
            }
        }
    }
}
