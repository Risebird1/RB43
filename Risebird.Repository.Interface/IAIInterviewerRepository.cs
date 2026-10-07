using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IAIInterviewerRepository
    {
        Task CreateSession(InterviewSession session);
        Task<string> GetThreadId(Guid sessionId);
        Task LogTranscript(Guid sessionId, string role, string message, int? deliveryHeadQuestionId = null);
        Task<bool> UpdateBotInterviewVideoDetails(string VideoLink, string question, string SessionId, string botSynthesizedVideoLink, string candidateResponse, int secondsLeft, int interviewId, string BotAudioLink = null, int? deliveryHeadQuestionId = null, bool isTypingSpeedQuestion = false, bool isListeningComprehension = false, string candidateAnswerAudioLink = null, bool allowMissingVideo = false, CandidateTranscriptReview transcriptReview = null);
        Task<List<BotInterviewQuestionVideos>> GetBotInterviewVideoDetails(Guid sessionId);
        Task<bool> UpdateTimeLeft(Guid sessionId, int secondsLeft, int interviewId);
        Task<MeetingVM> GetAIInterviewDetailsBySessionId(Guid sessionId);
        Task<IEnumerable<TranscriptConv>> GetAIInterviewTranscriptBySessionId(Guid sessionId);
        Task<bool> SaveReportOfAIInterviewByAssistant(Guid sessionId, string serializedReportOfAIInterviewByAssistant = null, bool? isExit = null, int? interviewStatus = null, string interviewerComment = null, int? meetingStatus = null);
        Task<string> GetReportOfAIInterviewByAssistant(Guid sessionId);


        /// <summary>The same stored evaluation, read by interview id rather than session id.</summary>

        Task<string> GetReportOfAIInterviewByInterviewId(int interviewId);
        Task<AddUpdateMeetingVM> GetAIInterviewSpecificDetails(Guid sessionId);
        Task<TechSkillComments> GetRandomQuestionForSession(int interviewId, int irfId);
        Task<bool> UpdateCandidateAnswerById(int questionId, string updatedAnswer, string updatedBy);
        Task<bool> UpdateMeetingCompletedStatusForAIInterview(string sessionId);
        Task<bool> SaveSentimentAnalysis(VideoSentimentAnalysis obj);
        Task<VideoSentimentAnalysis> GetSentimentAnalysisById(int videoId);
        Task<MeetingVM> GetAIInterviewSpecificDetailsByCandidateVideoId(int videoId);
        Task<bool> SaveTypingSpeedAssessment(TypingSpeedAssessment obj);
        Task<List<TypingSpeedAssessment>> GetTypingSpeedAssessmentBySessionId(string sessionId);
        Task<bool> SaveListeningComprehensionAssessment(ListeningComprehensionAssessment obj);
        Task<TechSkillComments> GetListeningComprehensionQuestionDetails(int questionId);
        Task<bool> SaveCodingAssessment(CodingAssessment obj);
        Task<TypingSpeedSampleText> GetRandomTypingSpeedSampleText();
        Task LogAIInterviewException(string sessionId, string httpMethod, Exception ex, LogLevelInfo logLevel = LogLevelInfo.Exception);

        /// <summary>
        /// Caches the MCQ section result fetched from screen.risebird.io. See
        /// Tables\AIInterviewMcqAssessment.sql for why the portal keeps a copy at all.
        /// </summary>
        Task<bool> SaveAIInterviewMcqAssessment(McqAssessmentCacheRow row);

        /// <summary>The cached MCQ section result, used when the live fetch is unavailable.</summary>
        Task<McqAssessmentCacheRow> GetAIInterviewMcqAssessment(string sessionId);
    }
}
