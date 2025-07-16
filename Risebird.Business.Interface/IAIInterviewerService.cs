using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IAIInterviewerService
    {
        Task<bool> UpdateBotInterviewVideoDetails(string VideoLink, string question, string SessionId, string botSynthesizedVideoLink, string candidateResponse, int secondsLeft, int interviewId);
        Task<string> GetThreadId(Guid sessionId);
        Task<List<BotInterviewQuestionVideos>> GetBotInterviewVideoDetails(Guid sessionId);
        Task<bool> UpdateTimeLeft(Guid sessionId, int secondsLeft, int interviewId);
    }
}
