using Risebird.Entities.Data;
using Risebird.Entities.Enum;
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
        Task<bool> UpdateBotInterviewVideoDetails(string VideoLink, string question, string SessionId, string botSynthesizedVideoLink, string candidateResponse, int secondsLeft, int interviewId, string BotAudioLink = null, int? deliveryHeadQuestionId = null, bool isTypingSpeedQuestion = false, bool isListeningComprehension = false, string candidateAnswerAudioLink = null);
        Task<string> GetThreadId(Guid sessionId);
        Task<List<BotInterviewQuestionVideos>> GetBotInterviewVideoDetails(Guid sessionId);
        Task<bool> UpdateTimeLeft(Guid sessionId, int secondsLeft, int interviewId);
        Task<MeetingVM> GetAIInterviewDetailsBySessionId(Guid sessionId);
        Task<IEnumerable<TranscriptConv>> GetAIInterviewTranscriptBySessionId(Guid sessionId);
        Task<bool> SaveReportOfAIInterviewByAssistant(Guid sessionId, string serializedReportOfAIInterviewByAssistant = null, bool? isExit = null, int? interviewStatus = null, string interviewerComment = null, int? meetingStatus = null);
        Task<string> GetReportOfAIInterviewByAssistant(Guid sessionId);
        Task<AIInterviewEvaluation> GetParsedAIInterviewEvaluationBySessionId(Guid sessionId);
        Task LogTranscript(Guid sessionId, string role, string message, int? deliveryHeadQuestionId = null);
        Task<AddUpdateMeetingVM> GetAIInterviewSpecificDetails(Guid sessionId);
        Task<TechSkillComments> GetRandomQuestionForSession(int interviewId, int irfId);
        Task<bool> UpdateCandidateAnswerById(int questionId, string updatedAnswer, string updatedBy);
        Task<bool> UpdateMeetingCompletedStatusForAIInterview(string sid);
        Task<VideoSentimentAnalysis> GenerateSentimentAnalysis(int videoId, string user, string videoUrl);
        Task<VideoSentimentAnalysis> GetSentimentAnalysisById(int videoId);
        Task<MeetingVM> GetAIInterviewSpecificDetailsByCandidateVideoId(int videoId);
        Task<bool> SaveTypingSpeedAssessment(TypingSpeedAssessment obj);
        Task<List<TypingSpeedAssessment>> GetTypingSpeedAssessmentBySessionId(string sessionId);
        Task<bool> SaveListeningComprehensionAssessment(ListeningComprehensionAssessment obj);
        Task<TypingSpeedSampleText> GetRandomTypingSpeedSampleText();
        Task LogAIInterviewException(string sessionId, string httpMethod, Exception ex, LogLevelInfo logLevel = LogLevelInfo.Exception);
    }
}
