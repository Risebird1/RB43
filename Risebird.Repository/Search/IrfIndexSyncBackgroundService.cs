using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Risebird.Repository.Search
{
    /// <summary>
    /// Drains <see cref="IrfIndexSyncQueue"/> off the request path.
    /// <para>
    /// Each item runs in its own DI scope. <see cref="IIrfListingSearchRepository"/> is scoped,
    /// so reusing the originating request's scope would hit a disposed provider as soon as the
    /// response had been sent — the reason this is a hosted service and not a bare
    /// <c>Task.Run</c> in the caller.
    /// </para>
    /// <para>
    /// Items are processed one at a time. Each one can load and upload every IRF of a client,
    /// so serialising them keeps a burst of user edits from stacking up concurrent scans of
    /// <c>usp_GetIrfIndexBatch</c>.
    /// </para>
    /// </summary>
    public sealed class IrfIndexSyncBackgroundService : BackgroundService
    {
        private readonly IrfIndexSyncQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;

        public IrfIndexSyncBackgroundService(IrfIndexSyncQueue queue, IServiceScopeFactory scopeFactory)
        {
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await foreach (var request in _queue.ReadAllAsync(stoppingToken))
                {
                    await ProcessAsync(request, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown.
            }
        }

        private async Task ProcessAsync(Interface.Search.IrfIndexSyncRequest request, CancellationToken cancellationToken)
        {
            if (request == null || cancellationToken.IsCancellationRequested)
                return;

            using var scope = _scopeFactory.CreateScope();
            var logger = scope.ServiceProvider.GetService<IApplicationErrorLogger>();

            try
            {
                var repository = scope.ServiceProvider.GetRequiredService<IIrfListingSearchRepository>();
                if (!repository.IsEnabled)
                    return;

                await repository.UpsertIrfsByUserIdAsync(request.UserId, request.AdditionalIrfIntIds);
            }
            catch (Exception ex)
            {
                // A failed refresh leaves the index stale; it must never take the worker down,
                // or every later edit stops being indexed too.
                if (logger != null)
                {
                    await logger.LogMessage(
                        LogLevelInfo.Exception,
                        new InvalidOperationException(
                            $"IRF index sync failed for userId={request.UserId} ({request.Reason ?? "unspecified"}).", ex));
                }
            }
        }
    }
}
