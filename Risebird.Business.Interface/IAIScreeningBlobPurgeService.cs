using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    /// <summary>
    /// Deletes the job descriptions and voice recordings left behind by abandoned AI screening
    /// intakes. A draft expires out of the cache after a day; its uploads used to stay for ever.
    ///
    /// Switched off by default, and reporting-only when first switched on. It deletes files from a
    /// container that also holds real candidate resumes, so the decision of what is disposable
    /// lives in <see cref="Risebird.Entities.ViewModel.AIScreeningBlobRetention"/> where it can be
    /// tested without touching storage.
    /// </summary>
    public interface IAIScreeningBlobPurgeService
    {
        Task<AIScreeningBlobPurgeResult> PurgeAsync();
    }

    public class AIScreeningBlobPurgeResult
    {
        /// <summary>Set when the run did nothing, and why. Null on a run that proceeded.</summary>
        public string Skipped { get; set; }

        /// <summary>True when nothing was actually deleted and the run only reported.</summary>
        public bool DryRun { get; set; }

        public int Examined { get; set; }

        /// <summary>Deleted, or on a dry run, the count that would have been.</summary>
        public int Deleted { get; set; }

        public int Failed { get; set; }

        /// <summary>How many in-use job descriptions the run was holding clear of.</summary>
        public int ReferencedCount { get; set; }

        /// <summary>"container/blobName" for each blob chosen, so a dry run is actionable.</summary>
        public List<string> Candidates { get; } = new List<string>();
    }
}
