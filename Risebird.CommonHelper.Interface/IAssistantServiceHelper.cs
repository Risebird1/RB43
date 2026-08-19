using Risebird.Entities.Data;
using Risebird.Entities.Data.OpenAIModels;
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
        /// Uses <paramref name="promptId"/> when provided; otherwise OpenAI:AIInterviewPromptMap.
        /// Returns null when UseResponsesApi is false or no prompt ID is available, so the caller
        /// can use the Assistants API.
        /// </summary>
        Task<RunResult> GenerateAIInterviewEvaluation(string techSkillsJson, string questionAnswerSetJson, string promptId = null);

        /// <summary>
        /// Adaptive AI-interviewer turn via Responses API using OpenAI:AIInterviewerPromptId.
        /// Returns the response id for multi-turn chaining (stored in InterviewSessions.ThreadId).
        /// </summary>
        Task<(RunResult Result, string ResponseId)> GenerateAIInterviewerMessage(string message, string previousResponseId = null);

        Task<RunResult> GenerateTypingAssessment(string inputsJson);

        Task<RunResult> GenerateIntervieweeResponseExtraction(string transcriptData);

        /// <summary>
        /// Calls the LinkedIn-verification assistant with a composed prospect context (name, email,
        /// phone, current role, and any candidate-supplied LinkedIn URL) and returns the raw RunResult
        /// carrying a JSON verdict (linkedin_match / confidence / linkedin_url / reason).
        /// </summary>
        Task<RunResult> GenerateLinkedInVerification(string composedMessage);
    }
}
