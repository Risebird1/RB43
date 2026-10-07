using Risebird.Entities.Data;
using Risebird.Entities.Data.OpenAIModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    public interface IAssistantServiceHelper
    {
        //Task<CandidateScreeningResponse> ScreenCandidateAsync(ScreeningRequest request);
        Task<RunResult> ScreenAsync(ScreeningRequest request);
        Task<bool> DeleteVectors();
        Task<bool> DeleteFiles(int maxIterations = 10);
        Task<RunResult> GenerateTechRubric(string jobTitle);
        Task<RunResult> GenerateTechRubricRevision(string revisionPrompt);
        Task<RunResult> GenerateRubricQuestions(string rubricInput);
        Task<RunResult> GenerateTranscriptAnalysis(string transcriptData);
        Task<RunResult> GenerateQcAnalysisInterviewAIMerge(string userMessageContent, string assistantId, bool useJsonResponseFormat = true);
        Task<RunResult> GenerateCandidateScreening(string resumeStringData, string jobTitle, bool useResumeParserCopyPrompt = false);

        /// <summary>
        /// Advanced (rubric-based) resume screening: scores the resume against a skill channel's
        /// three-section rubric (OpenAI:AdvancedScreenerPromptId). Null when not configured or on failure.
        /// </summary>
        Task<RunResult> GenerateAdvancedResumeAnalysis(string resumeStringData, string screeningCriteriaJson);

        /// <summary>
        /// Generates a skill channel's advanced screening rubric from its JD, the hiring manager
        /// call and the recruiter's instructions (OpenAI:JdToScreeningPromptId). Null when not
        /// configured or on failure.
        /// </summary>
        Task<RunResult> GenerateJDToScreeningCriteria(string hiringManagerCallData, string jdStringData,
            string recruiterInstructions = null, string icpStringData = null);
        Task<RunResult> GenerateInterviewerScreening(string resumeStringData, string jobTitle);
        Task<RunResult> GenerateResumeAnalysis(string resumeStringData, string jobTitle);
        Task<RunResult> GenerateReportQCAnalysis(string reportStringData);
        Task<RunResult> ParseResume(string resumeData, bool usePublicUrlParser = false, bool useProfileSummary = false);
        Task<RunResult> TranscriptToNotes(string transcriptdata);
        Task<RunResult> GeneratecontradictionReport(string transcriptSummary);

        /// <summary>
        /// Calls the Payment-Ticket-Review assistant (asst_kjUOaWfVxQOymoAN1m29MrR8) with
        /// the composed ticket context and returns the raw RunResult for verdict parsing.
        /// </summary>
        Task<RunResult> GeneratePaymentTicketReview(string composedMessage);

        /// <summary>
        /// Preset AI-interview evaluation via Responses API.
        /// Uses <paramref name="promptId"/> when provided - the skill channel's Interview Evaluator Prompt ID,
        /// or its Primary Prompt ID when no evaluator prompt is set; otherwise OpenAI:AIInterviewPromptMap.
        /// Returns null when UseResponsesApi is false or no prompt ID is available, so the caller
        /// can use the Assistants API.
        /// </summary>
        Task<RunResult> GenerateAIInterviewEvaluation(string techSkillsJson, string questionAnswerSetJson, string promptId = null);

        /// <summary>
        /// Adaptive AI-interviewer turn via Responses API. Uses <paramref name="promptId"/> - the skill
        /// channel's Primary Prompt ID for interview turns, its Interview Evaluator Prompt ID for the
        /// closing report turn - falling back to OpenAI:AIInterviewerPromptId when neither is configured.
        /// Returns the response id for multi-turn chaining (stored in InterviewSessions.ThreadId).
        /// </summary>
        Task<(RunResult Result, string ResponseId)> GenerateAIInterviewerMessage(string message, string previousResponseId = null, string promptId = null);

        Task<RunResult> GenerateTypingAssessment(string inputsJson);
        Task<(RunResult Result, string ResponseId)> GenerateListeningComprehensionAssessment(string inputsJson);

        Task<RunResult> GenerateIntervieweeResponseExtraction(string transcriptData);

        /// <summary>
        /// Calls the LinkedIn-verification assistant with a composed prospect context (name, email,
        /// phone, current role, and any candidate-supplied LinkedIn URL) and returns the raw RunResult
        /// carrying a JSON verdict (linkedin_match / confidence / linkedin_url / reason).
        /// </summary>
        Task<RunResult> GenerateLinkedInVerification(string composedMessage);

        /// <summary>
        /// Selects the top N questions from an existing bank for a JD. Does not invent questions.
        /// </summary>
        Task<List<AISuggestedQuestion>> GenerateAIQuestionSuggestions(string jdContext, IList<AISuggestedQuestion> questionBank, int topCount);

        /// <summary>
        /// Generates an ideal answer for each supplied question in a single chat-completions call.
        /// </summary>
        Task<List<AISuggestedQuestion>> GenerateAIIdealAnswers(string jdContext, IList<AISuggestedQuestion> questions);
    }
}
