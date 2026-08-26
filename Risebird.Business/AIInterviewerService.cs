using Amazon.Rekognition.Model;
using Newtonsoft.Json;
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
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class AIInterviewerService : IAIInterviewerService
    {
        private readonly IAIInterviewerRepository _aiInterviewerRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IAWSRekognitionService _aWSRekognitionService;
        public AIInterviewerService(IAIInterviewerRepository aiInterviewerRepository,
            IApplicationErrorLogger applicationErrorLogger,
            IAWSRekognitionService aWSRekognitionService)
        {
            _aiInterviewerRepository = aiInterviewerRepository;
            _applicationErrorLogger = applicationErrorLogger;
            _aWSRekognitionService = aWSRekognitionService;
        }
        public async Task<bool> UpdateBotInterviewVideoDetails(string VideoLink, string question, string SessionId, string botSynthesizedVideoLink, string candidateResponse, int secondsLeft, int interviewId, string BotAudioLink = null, int? deliveryHeadQuestionId = null, bool isTypingSpeedQuestion = false, bool isListeningComprehension = false, string candidateAnswerAudioLink = null)
        {
            try
            {
                return await _aiInterviewerRepository.UpdateBotInterviewVideoDetails(VideoLink, question, SessionId,botSynthesizedVideoLink, candidateResponse, secondsLeft, interviewId, BotAudioLink, deliveryHeadQuestionId, isTypingSpeedQuestion, isListeningComprehension, candidateAnswerAudioLink);
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
        public async Task<bool> SaveReportOfAIInterviewByAssistant(Guid sessionId, string serializedReportOfAIInterviewByAssistant = null, bool? isExit = null, int? interviewStatus = null, string interviewerComment = null, int? meetingStatus = null)
        {
            try
            {
                return await _aiInterviewerRepository.SaveReportOfAIInterviewByAssistant(sessionId, serializedReportOfAIInterviewByAssistant, isExit, interviewStatus, interviewerComment, meetingStatus);
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

        public async Task<AIInterviewEvaluation> GetParsedAIInterviewEvaluationBySessionId(Guid sessionId)
        {
            try
            {
                var aiReportJson = await _aiInterviewerRepository.GetReportOfAIInterviewByAssistant(sessionId);
                if (string.IsNullOrWhiteSpace(aiReportJson))
                {
                    return null;
                }

                InterviewReportResult aiReport = null;
                try
                {
                    aiReport = JsonConvert.DeserializeObject<InterviewReportResult>(aiReportJson);
                }
                catch
                {
                    aiReport = null;
                }

                var evaluation = TryParseEvaluation(aiReport?.Replies?.FirstOrDefault());
                if (evaluation != null)
                {
                    var videoDetails = await _aiInterviewerRepository.GetBotInterviewVideoDetails(sessionId);
                    EnrichQuestionRatingsWithActualQuestions(evaluation, videoDetails);
                }
                return evaluation;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        private void EnrichQuestionRatingsWithActualQuestions(AIInterviewEvaluation evaluation, List<BotInterviewQuestionVideos> videoDetails)
        {
            if (evaluation?.QuestionRatings == null || videoDetails == null || !videoDetails.Any())
                return;

            for (int i = 0; i < evaluation.QuestionRatings.Count; i++)
            {
                var qr = evaluation.QuestionRatings[i];
                if (string.IsNullOrWhiteSpace(qr.Question) || Regex.IsMatch(qr.Question, @"^Q\d+$"))
                {
                    int index;
                    var match = Regex.Match(qr.Question ?? "", @"\d+");
                    if (match.Success)
                        index = int.Parse(match.Value) - 1;
                    else
                        index = i;

                    if (index >= 0 && index < videoDetails.Count && !string.IsNullOrWhiteSpace(videoDetails[index].question))
                    {
                        qr.Question = videoDetails[index].question;
                    }
                }
            }
        }

        private AIInterviewEvaluation TryParseEvaluation(string raw)
            => AIInterviewEvaluation.TryParse(raw);

        public async Task LogTranscript(Guid sessionId, string role, string message, int? deliveryHeadQuestionId = null)
        {
            try
            {
                await _aiInterviewerRepository.LogTranscript(sessionId, role, message, deliveryHeadQuestionId);
                return;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return;
            }
        }
        public async Task<AddUpdateMeetingVM> GetAIInterviewSpecificDetails(Guid sessionId)
        {
            try
            {
                return await _aiInterviewerRepository.GetAIInterviewSpecificDetails(sessionId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<TechSkillComments> GetRandomQuestionForSession(int interviewId, int irfId)
        {
            try
            {
                return await _aiInterviewerRepository.GetRandomQuestionForSession(interviewId, irfId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<bool> UpdateCandidateAnswerById(int questionId, string updatedAnswer, string updatedBy)
        {
            try
            {
                return await _aiInterviewerRepository.UpdateCandidateAnswerById(questionId, updatedAnswer, updatedBy);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<bool> UpdateMeetingCompletedStatusForAIInterview(string sid)
        {
            try
            {
                return await _aiInterviewerRepository.UpdateMeetingCompletedStatusForAIInterview(sid);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        // Amazon Rekognition Video only decodes H.264 inside an MPEG-4, MOV or AVI
        // container. Browser recordings default to WebM/VP9, which Rekognition accepts
        // as a job and then fails asynchronously with "Unsupported codec/format", so it
        // is worth refusing up front rather than paying for an upload to find out.
        private static readonly HashSet<string> RekognitionSupportedVideoExtensions =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".mp4", ".m4v", ".mov", ".qt", ".avi" };

        private static bool IsRekognitionSupportedFormat(string videoUrl, out string extension)
        {
            extension = string.Empty;
            if (string.IsNullOrWhiteSpace(videoUrl))
            {
                return false;
            }

            var path = Uri.TryCreate(videoUrl, UriKind.Absolute, out var uri) ? uri.LocalPath : videoUrl;
            extension = System.IO.Path.GetExtension(path);

            return RekognitionSupportedVideoExtensions.Contains(extension);
        }

        public async Task<SentimentAnalysisResultVM> GenerateSentimentAnalysis(int videoId, string user, string videoUrl)
        {
            try
            {
                if (!IsRekognitionSupportedFormat(videoUrl, out var extension))
                {
                    var describedFormat = string.IsNullOrEmpty(extension)
                        ? "an unrecognised format"
                        : extension.TrimStart('.').ToUpperInvariant();
                    var refusal = $"This answer was recorded as {describedFormat}, which Amazon Rekognition cannot analyse. " +
                                  "Sentiment analysis needs H.264 video in an MP4 or MOV container.";

                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Warning,
                        $"Skipped sentiment analysis for video {videoId}: {refusal} Video URL: {videoUrl}", user);

                    return new SentimentAnalysisResultVM { Success = false, Message = refusal };
                }

                var key = await _aWSRekognitionService.UploadVideoAsync(videoUrl);
                var sentimentAnalysis = await _aWSRekognitionService.AnalyzeVideoAsync(key);
                if(sentimentAnalysis != null)
                {
                    int averageFactor = sentimentAnalysis.Faces.Count();
                    float? fear = 0;
                    float? confused = 0;
                    float? sad = 0;
                    float? calm = 0;
                    float? surprised = 0;
                    float? disgusted = 0;
                    float? angry = 0;
                    float? happy = 0;
                    foreach (var face in sentimentAnalysis.Faces)
                    {
                        fear = fear + face.Emotions.Where(x => x.Type == AWSEmotionTypes.FEAR).FirstOrDefault().Confidence;
                        confused = confused + face.Emotions.Where(x => x.Type == AWSEmotionTypes.CONFUSED).FirstOrDefault().Confidence;
                        sad = sad + face.Emotions.Where(x => x.Type == AWSEmotionTypes.SAD).FirstOrDefault().Confidence;
                        calm = calm + face.Emotions.Where(x => x.Type == AWSEmotionTypes.CALM).FirstOrDefault().Confidence;
                        surprised = surprised + face.Emotions.Where(x => x.Type == AWSEmotionTypes.SURPRISED).FirstOrDefault().Confidence;
                        disgusted = disgusted + face.Emotions.Where(x => x.Type == AWSEmotionTypes.DISGUSTED).FirstOrDefault().Confidence;
                        angry = angry + face.Emotions.Where(x => x.Type == AWSEmotionTypes.ANGRY).FirstOrDefault().Confidence;
                        happy = happy + face.Emotions.Where(x => x.Type == AWSEmotionTypes.HAPPY).FirstOrDefault().Confidence;
                    }
                    float? avgFear = fear/averageFactor;
                    float? avgConfused = confused/averageFactor;
                    float? avgSad = sad/averageFactor;
                    float? avgCalm = calm/averageFactor;
                    float? avgSurprised = surprised/averageFactor;
                    float? avgDisgusted = disgusted/averageFactor;
                    float? avgAngry = angry/averageFactor;
                    float? avgHappy = happy/averageFactor;
                    var sentimentObj = new VideoSentimentAnalysis();
                    sentimentObj.FEAR = avgFear;
                    sentimentObj.CONFUSED = avgConfused;
                    sentimentObj.SAD = avgSad;
                    sentimentObj.CALM = avgCalm;
                    sentimentObj.SURPRISED = avgSurprised;
                    sentimentObj.DISGUSTED = avgDisgusted;
                    sentimentObj.ANGRY = avgAngry;
                    sentimentObj.HAPPY = avgHappy;

                    sentimentObj.CreatedBy = user;
                    sentimentObj.biqvId = videoId;
                    await _aiInterviewerRepository.SaveSentimentAnalysis(sentimentObj);
                    return new SentimentAnalysisResultVM { Success = true, Sentiment = sentimentObj };
                }

                return new SentimentAnalysisResultVM
                {
                    Success = false,
                    Message = "Amazon Rekognition returned no face data for this video."
                };
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return new SentimentAnalysisResultVM
                {
                    Success = false,
                    Message = "Sentiment analysis failed unexpectedly. Please check the application logs."
                };
            }
        }

        public async Task<VideoSentimentAnalysis> GetSentimentAnalysisById(int videoId)
        {
            try
            {
                return await _aiInterviewerRepository.GetSentimentAnalysisById(videoId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<MeetingVM> GetAIInterviewSpecificDetailsByCandidateVideoId(int videoId)
        {
            try
            {
                return await _aiInterviewerRepository.GetAIInterviewSpecificDetailsByCandidateVideoId(videoId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<bool> SaveTypingSpeedAssessment(TypingSpeedAssessment obj)
        {
            try
            {
                return await _aiInterviewerRepository.SaveTypingSpeedAssessment(obj);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<List<TypingSpeedAssessment>> GetTypingSpeedAssessmentBySessionId(string sessionId)
        {
            try
            {
                return await _aiInterviewerRepository.GetTypingSpeedAssessmentBySessionId(sessionId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return new List<TypingSpeedAssessment>();
            }
        }

        public async Task<bool> SaveListeningComprehensionAssessment(ListeningComprehensionAssessment obj)
        {
            try
            {
                return await _aiInterviewerRepository.SaveListeningComprehensionAssessment(obj);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<bool> SaveCodingAssessment(CodingAssessment obj)
        {
            try
            {
                return await _aiInterviewerRepository.SaveCodingAssessment(obj);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<TypingSpeedSampleText> GetRandomTypingSpeedSampleText()
        {
            try
            {
                return await _aiInterviewerRepository.GetRandomTypingSpeedSampleText();
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task LogAIInterviewException(string sessionId, string httpMethod, Exception ex, LogLevelInfo logLevel = LogLevelInfo.Exception)
        {
            await _aiInterviewerRepository.LogAIInterviewException(sessionId, httpMethod, ex, logLevel);
        }
    }
}
