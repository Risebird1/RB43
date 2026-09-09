using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.ViewModel
{
    public class BotInterviewQuestionVideos
    {
        public int id { get; set; }
        public string SessionId { get; set; }
        public string VideoLink { get; set; }
        public string question { get; set; }
        public DateTime CreatedAt { get; set; }
        public string BotSynthesizedVideoLink { get; set; }
        public string CandidateAnswer { get; set; }
        public string BotAudioLink { get; set; }
        public string CandidateAnswerAudioLink { get; set; }
        public int DeliveryHeadQuestionId { get; set; }
        public bool IsFlaggedForSentimentAnalysis { get; set; }
        /// <summary>
        /// Current AI question video from SkillChannelSampleQuestions (may be newer than BotSynthesizedVideoLink).
        /// </summary>
        public string AIQuestionVideoLink { get; set; }

    }

    public class AIInterviewReportWrapperClass
    {
        public List<BotInterviewQuestionVideos> videoQuestionDetails { get; set; }
        public ChatMessageViewModel chatMessageViewModel { get; set; }
        public AIInterviewEvaluation AIInterviewEvaluation { get; set; }
        public VideoSentimentAnalysis sentimentViewModel { get; set; }

        public int TotalQuestions { get; set; }
        public string CandidateName { get; set; }
        public string CandidateEmail { get; set; }
        public string resumeLink { get; set; }
        public int MatchScorePercentage { get; set; }
        public DateTime InterviewDate { get; set; }
        public int InterviewId { get; set; }
        public string SessionId { get; set; } //Guid
        // Kept alongside ResumeEvaluation deliberately: these two are read from their own
        // columns, so they survive a screening record whose JSON will not parse — which is
        // what the summary strip falls back to when ResumeEvaluation is null.
        public string AIResumeScreeningVerdict { get; set; }
        public int AIResumeScreeningScore { get; set; }

        /// <summary>
        /// The resume screener's full result, parsed from Candidate.AIScreeningResultJsonV2.
        /// Null when the candidate was never screened, or when the stored JSON could not be
        /// parsed — the report renders no Resume Evaluation section in either case.
        /// </summary>
        public CandidateResumeScreeningVM ResumeEvaluation { get; set; }
        public List<TypingSpeedAssessment> TypingSpeedAssessments { get; set; }

        /// <summary>
        /// Raw report content returned by the AI assistant. For Adaptive interviews the
        /// assistant produces a ready-to-render HTML report (not the structured JSON that
        /// <see cref="AIInterviewEvaluation"/> is parsed from), so the view falls back to
        /// rendering this HTML when <see cref="AIInterviewEvaluation"/> could not be parsed.
        /// </summary>
        public string RawAIReportHtml { get; set; }

        /// <summary>
        /// The MCQ assessment section result, fetched live from screen.risebird.io or read from
        /// the portal's cache. Null when the skill channel has no MCQ section, which is what the
        /// report keys the whole MCQ block off.
        /// </summary>
        public McqAssessmentReport McqAssessment { get; set; }
    }

}
