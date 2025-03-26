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
        Task<bool> DeleteFiles();
        Task<RunResult> GenerateTechRubric(string jobTitle);
        Task<RunResult> GenerateTranscriptAnalysis(string transcriptData);
        Task<RunResult> GenerateResumeAnalysis(string resumeStringData);
        Task<RunResult> GenerateReportQCAnalysis(string reportStringData);
    }
}
