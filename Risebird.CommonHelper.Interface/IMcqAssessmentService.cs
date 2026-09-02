using Risebird.Entities.ViewModel;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    /// <summary>
    /// The portal's client for the MCQ assessment section that lives on screen.risebird.io.
    ///
    /// Everything about the test - its questions, the candidate's answers, the answer key - stays
    /// in the MCQ database. Two calls cross the boundary: one to have a session's section
    /// prepared, one to read its result.
    /// </summary>
    public interface IMcqAssessmentService
    {
        /// <summary>True when the MCQ base URL is configured, i.e. the integration is switched on.</summary>
        bool IsConfigured { get; }

        /// <summary>
        /// Prepares (or re-fetches) the MCQ section for one interview session and returns the
        /// iframe URL. Idempotent on the MCQ side, so calling it on every render of the section
        /// is safe and returns the same paper. Null when the MCQ side could not provision.
        /// </summary>
        Task<McqSectionProvisionResult> ProvisionSectionAsync(string sessionId, McqSectionProvisionRequest request);

        /// <summary>
        /// The live section result. Null when the session has no MCQ section or the MCQ side is
        /// unreachable - callers fall back to the portal's cached copy.
        /// </summary>
        Task<McqAssessmentReport> FetchReportAsync(string sessionId);
    }
}
