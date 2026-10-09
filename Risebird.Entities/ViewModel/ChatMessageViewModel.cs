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

        /// <summary>
        /// The transcript the candidate reviewed and submitted, after any correction they made.
        /// When this is present the server does not transcribe the audio again — the candidate has
        /// already seen what speech-to-text produced and this is what they stand behind.
        /// </summary>
        public string ConfirmedTranscript { get; set; }

        /// <summary>
        /// What speech-to-text returned before the candidate touched it. Posted back alongside
        /// <see cref="ConfirmedTranscript"/> so the machine's version is never lost; it is the
        /// tiebreaker on any dispute about what was said.
        /// </summary>
        public string RawTranscript { get; set; }

        /// <summary>
        /// How many times the candidate has recorded for the question currently on screen. The page
        /// posts it back and increments it whenever a failed attempt sends them around again, so a
        /// retry does not read as a fresh first answer.
        /// </summary>
        public int TranscriptTakeCount { get; set; }

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
        /// <summary>Code project question: the candidate works on a provisioned VM, not the in-page editor. Details in CodeProjectDetails.</summary>
        public bool IsCodeProjectQuestion { get; set; }
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

        /// <summary>
        /// The current question is the MCQ assessment section. The interview page renders it as a
        /// full-viewport overlay hosting the MCQ test in an iframe, instead of the usual
        /// record-an-answer UI.
        /// </summary>
        public bool IsMcqAssessmentQuestion { get; set; }

        /// <summary>
        /// Minutes allotted to the MCQ section, from the skill channel. Shown on the briefing
        /// panel; the authoritative timer runs on the MCQ side.
        /// </summary>
        public int McqTotalMinutes { get; set; }

        /// <summary>
        /// Section titles in ask order, shown on the pre-launch briefing panel. Derived from the
        /// channel's own configuration so the panel needs no call to the MCQ side to render - the
        /// iframe URL itself is fetched by AIInterview/PrepareMcqSection when Start is pressed.
        /// </summary>
        public List<string> McqSectionTitles { get; set; } = new List<string>();

        /// <summary>
        /// Set when the section could not be prepared (MCQ host down, generation failed). The
        /// page shows a short notice and lets the candidate move on rather than trapping them.
        /// </summary>
        public bool McqSectionUnavailable { get; set; }
    }

    public class ChatLine
    {
        public string Role { get; set; }
        public string Message { get; set; }
        public int sampleQuestionId { get; set; } // application only in case of preset interviews which require video
    }
}
