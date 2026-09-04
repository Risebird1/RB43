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

        /// <summary>
        /// Whether an MCQ skill channel is linked to this portal channel and marked as an AI
        /// Interview channel - i.e. whether the assessment section could actually be provisioned.
        ///
        /// Null means the question could not be answered (integration off, MCQ side unreachable),
        /// which is deliberately distinct from false: a warning that says "not linked" when the
        /// truth is "could not check" would send someone hunting for the wrong problem.
        /// </summary>
        Task<bool?> IsChannelLinkedAsync(string skillChannelId);

        /// <summary>
        /// Has the MCQ skill channel for this portal channel created if it does not exist yet, and
        /// returns the link either way. Idempotent on the MCQ side, so it is safe to call on every
        /// save of a channel that wants the assessment section.
        ///
        /// Null means the link could not be established - integration off, MCQ unreachable, or the
        /// configured service account does not resolve to an MCQ client.
        /// </summary>
        Task<McqChannelLinkResult> EnsureChannelLinkAsync(McqEnsureChannelLinkRequest request);

        /// <summary>
        /// The MCQ client user that auto-created channels are owned by
        /// (McqAssessment:ChannelOwnerEmail). Empty when unset, which switches auto-creation off.
        /// </summary>
        string ChannelOwnerEmail { get; }
    }
}
