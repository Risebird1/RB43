using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Risebird.Web.Services.Proctoring
{
    public sealed class ProctoringScreenshotAnalysisResult
    {
        public bool Ok { get; set; }
        public string Error { get; set; }
        public int ScreenshotsAnalyzed { get; set; }
        public List<string> ScreenshotPaths { get; set; } = new List<string>();
        /// <summary>Valid JSON text returned by the model (candidate eye gaze, suspicious apps, etc.).</summary>
        public string AnalysisJson { get; set; }
    }

    public interface IProctoringScreenshotAnalysisService
    {
        /// <summary>
        /// Picks a sample of the interview's proctoring screenshots from blob storage, sends them to the
        /// configured OpenAI prompt and returns the model's JSON analysis (eye gaze, suspicious apps).
        /// </summary>
        Task<ProctoringScreenshotAnalysisResult> AnalyzeAsync(int interviewId, string candidateEmail = null, CancellationToken cancellationToken = default);
    }
}
