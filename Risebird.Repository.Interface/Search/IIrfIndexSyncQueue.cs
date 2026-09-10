using System.Collections.Generic;

namespace Risebird.Repository.Interface.Search
{
    /// <summary>
    /// One user edit whose IRF documents need refreshing.
    /// </summary>
    public sealed class IrfIndexSyncRequest
    {
        /// <summary>The user whose name, email or client assignment changed.</summary>
        public int UserId { get; set; }

        /// <summary>
        /// IRFs the user was on *before* the write. Client assignments are rewritten
        /// delete-then-insert, so an IRF the user was just dropped from will not come back from
        /// the post-write lookup even though it still carries their email in its ACL.
        /// </summary>
        public IReadOnlyList<int> AdditionalIrfIntIds { get; set; }

        /// <summary>Which operation queued this, for the log line when it fails.</summary>
        public string Reason { get; set; }
    }

    /// <summary>
    /// Hands IRF re-index work to a background worker so a user save does not wait on
    /// <c>usp_GetIrfIndexBatch</c> and an Azure Search round trip.
    /// <para>
    /// The queue is in-memory: queued work is lost if the process stops before it drains.
    /// That is acceptable because the index is a denormalized cache — the
    /// <c>IrfIndexSearch/FullCrawl</c> function rebuilds it — but it does mean this is a
    /// best-effort refresh, not a guarantee.
    /// </para>
    /// </summary>
    public interface IIrfIndexSyncQueue
    {
        /// <summary>
        /// Queues a refresh without blocking. Returns <see langword="false"/> when the request
        /// was dropped (queue full, or nothing valid to do), so the caller can log it.
        /// </summary>
        bool Enqueue(IrfIndexSyncRequest request);
    }
}
