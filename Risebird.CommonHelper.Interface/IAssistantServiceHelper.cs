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
        Task<RunResult> GenerateTranscriptAnalysis(string transcriptData);
        Task<RunResult> GenerateQcAnalysisInterviewAIMerge(string userMessageContent, string assistantId, bool useJsonResponseFormat = true);
        Task<RunResult> GenerateResumeAnalysis(string resumeStringData, string jobTitle);
        Task<RunResult> GenerateReportQCAnalysis(string reportStringData);
        Task<RunResult> ParseResume(string resumeData);
        Task<RunResult> TranscriptToNotes(string transcriptdata);
    }
}
