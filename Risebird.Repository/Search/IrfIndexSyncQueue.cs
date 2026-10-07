using Risebird.Repository.Interface.Search;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;

namespace Risebird.Repository.Search
{
    /// <summary>
    /// Bounded in-memory queue behind <see cref="IIrfIndexSyncQueue"/>, drained by
    /// <see cref="IrfIndexSyncBackgroundService"/>. Registered as a singleton.
    /// </summary>
    public sealed class IrfIndexSyncQueue : IIrfIndexSyncQueue
    {
        /// <summary>
        /// Deliberately small. Each entry can fan out to every IRF of a client, so a large
        /// backlog means the index is already far behind and a full crawl is the better repair.
        /// </summary>
        public const int Capacity = 512;

        private readonly Channel<IrfIndexSyncRequest> _channel =
            Channel.CreateBounded<IrfIndexSyncRequest>(new BoundedChannelOptions(Capacity)
            {
                // Never block the request thread: that is the whole point of the queue.
                FullMode = BoundedChannelFullMode.DropWrite,
                SingleReader = true,
                SingleWriter = false
            });

        /// <inheritdoc />
        public bool Enqueue(IrfIndexSyncRequest request)
        {
            if (request == null || request.UserId <= 0)
                return false;

            return _channel.Writer.TryWrite(request);
        }

        /// <summary>Consumed by the background worker; completes when the host shuts down.</summary>
        public IAsyncEnumerable<IrfIndexSyncRequest> ReadAllAsync(CancellationToken cancellationToken) =>
            _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
