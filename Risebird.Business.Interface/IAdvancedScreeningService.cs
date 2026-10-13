using Risebird.Entities.ViewModel.AdvancedScreening;
using System.Threading;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    /// <summary>
    /// Advanced Resume Screening at IRF level: the rubric (generate, edit, approve, version), the
    /// recruiter instructions, the screening queue, and scoring one candidate end to end.
    /// </summary>
    public interface IAdvancedScreeningService
    {
        /// <summary>The IRF's rubric row and inputs, with RubricJson parsed. Null when the IRF does not exist.</summary>
        Task<IRFScreeningRubricVM> GetRubricAsync(int irfId);

        /// <summary>
        /// Extracts the JD (when no JD text is stored yet), calls the rubric generator, validates
        /// (27 keys, total 300 - repairing the weights once by scaling), and saves it as Generated.
        /// </summary>
        Task<RubricOperationResult> GenerateRubricAsync(int irfId, string user);

        /// <summary>Saves an edited rubric; with <paramref name="approve"/> it must have all 27 keys and total 300.</summary>
        Task<RubricOperationResult> SaveRubricAsync(int irfId, ScreeningRubric rubric, bool approve, string user, string jdText = null);

        Task<SaveInstructionsResult> SaveInstructionsAsync(int irfId, string instructions, bool applyToRubric, bool applyToScoring, string user);

        Task<AdvancedScreeningCounts> GetCountsAsync(int irfId);

        /// <summary>Queues candidates for the job; returns how many were queued (-1 on failure).</summary>
        Task<int> QueueAsync(int irfId, ScreenAllScope scope, int? candidateId, string user);

        Task<CandidateAdvancedScreeningVM> GetCandidateResultAsync(int candidateId);

        /// <summary>
        /// Scores one candidate: resume text, verified facts, the scorer (validated, retried once),
        /// the computed totals - and saves the result. An unsupported or empty resume ends Skipped.
        /// </summary>
        Task<ScreenCandidateOutcome> ScreenCandidateAsync(int candidateId, string triggeredBy);

        /// <summary>Claims up to <paramref name="batchSize"/> rows and screens them, at most 4 at a time. Returns how many were claimed.</summary>
        Task<int> ScreenPendingBatchAsync(int batchSize = 20, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Company registry lookups for the CompanyGenuineness check (e.g. MCA company master data via
    /// a paid provider). The provider is undecided; until it is chosen the registered
    /// implementation is a no-op and the scorer is told the companies are "not verified".
    /// </summary>
    public interface ICompanyRegistryClient
    {
        bool IsConfigured { get; }
        string ProviderName { get; }

        /// <summary>Looks a company up; null when not configured or the lookup failed. Found = false when it does not exist.</summary>
        Task<CompanyVerificationRecord> LookupAsync(string companyName);
    }
}
