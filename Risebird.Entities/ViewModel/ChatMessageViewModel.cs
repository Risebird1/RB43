using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    public class ChatMessageViewModel
    {
        public Guid SessionId { get; set; }
        public string Message { get; set; }
        //public string BotReply { get; set; }
        public List<ChatLine> ChatLog { get; set; } = new();
        public IFormFile AudioFile { get; set; }
        public string AIInterviewerAudioLink { get; set; }//points to a url that is the audio recieved from text to speech api, for the ai interviewer
        public IFormFile VideoFile { get; set; }
        public string SynthesizedVideoUrl { get; set; } //synthesised by synthesia, this will be the video of the AI Interviewer speaking
        public int TimeLeftInSeconds { get; set; }
        public int InterviewId { get; set; }
        public string candidateName { get; set; }
        public bool IsAdaptive { get; set; }
        public string CurrentQuestion { get; set; }
        public string AssistantId { get; set; }
        public int AIInterviewType { get; set; }
        public int CurrentQuestionId { get; set; }
        public int IRFId { get; set; }
        public bool showChatBox { get; set; }
        public bool isMultiLingual { get; set; }
        public bool IsTypingSpeedQuestion { get; set; }
        public string TypingSampleText { get; set; }
        public string TypingSampleTextFromDB { get; set; } // Sample text fetched from database to display to candidate
        public string TypedText { get; set; }
        public string TypingStartTime { get; set; }
        public string TypingEndTime { get; set; }
        public decimal TypingTimeTaken { get; set; }
        public int TypingTotalChars { get; set; }
        public int TypingCorrectChars { get; set; }
        public decimal TypingAccuracy { get; set; }
        public decimal TypingWPM { get; set; }
        public decimal TypingPassingWPM { get; set; } = 30.00M;
        public decimal TypingPassingAccuracy { get; set; } = 97.00M;
        public bool? TypingIsPassed { get; set; }
        public decimal TypingTotalMinutes { get; set; }
        public string TypingOpenAIAnalysisJson { get; set; }
        public bool IsListeningComprehensionQuestion { get; set; }
        public string ListeningAudioUrl { get; set; }
        public string ListeningAudioTranscription { get; set; }
        public string ListeningSummary { get; set; }
        public string ListeningStartTime { get; set; }
        public string ListeningEndTime { get; set; }
        public decimal ListeningTimeTaken { get; set; }
        public int ListeningAudioPlayCount { get; set; }
        public bool ListeningIsTimeExpired { get; set; }
        public bool IsCodingQuestion { get; set; }
        public int? CodingTimeLimitInMinutes { get; set; }
        public string CodingLanguage { get; set; }
        public string CodingSubmittedCode { get; set; }
        public string CodingStartTime { get; set; }
        public string CodingEndTime { get; set; }
        public decimal CodingTimeTaken { get; set; }
        public bool CodingIsTimeExpired { get; set; }
        public string CodingStdInput { get; set; }
        public string CodingLastRunOutput { get; set; }
        public string CodingLastRunStatus { get; set; }
        public int CodingRunCount { get; set; }
    }

    public class ChatLine
    {
        public string Role { get; set; }
        public string Message { get; set; }
        public int sampleQuestionId { get; set; } // application only in case of preset interviews which require video
    }
}
