using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Risebird.Web.Services
{
    /// <summary>
    /// Signals that Advanced Resume Screening work was queued (Screen All, Re-screen). Coalescing:
    /// any number of signals while the worker is busy result in one more drain.
    /// </summary>
    public sealed class AdvancedScreeningSignal
    {
        private readonly Channel<bool> _channel = Channel.CreateBounded<bool>(
            new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropWrite, SingleReader = true });

        public void Kick() => _channel.Writer.TryWrite(true);

        public ValueTask<bool> WaitAsync(CancellationToken ct) => _channel.Reader.ReadAsync(ct);
    }

    /// <summary>
    /// Drains the screening queue as soon as a recruiter queues work, so results start appearing
    /// in seconds instead of at the next AdvancedResumeScreeningJob tick (every 5 minutes).
    ///
    /// Safe alongside the functions-host job: rows are claimed atomically by
    /// usp_GetCandidatesForAdvancedScreening, so the two never screen the same candidate. Turn it
    /// off with AdvancedScreening:ProcessInWebHost = false to leave all processing to the job.
    /// </summary>
    public sealed class AdvancedScreeningWorker : BackgroundService
    {
        private const int BatchSize = 20;
        private readonly AdvancedScreeningSignal _signal;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly bool _enabled;

        public AdvancedScreeningWorker(AdvancedScreeningSignal signal, IServiceScopeFactory scopeFactory, IConfiguration configuration)
        {
            _signal = signal;
            _scopeFactory = scopeFactory;
            _enabled = configuration.GetValue("AdvancedScreening:ProcessInWebHost", true);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_enabled)
            {
                return;
            }
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _signal.WaitAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                using (var scope = _scopeFactory.CreateScope())
                {
                    var service = scope.ServiceProvider.GetRequiredService<IAdvancedScreeningService>();
                    try
                    {
                        while (!stoppingToken.IsCancellationRequested
                               && await service.ScreenPendingBatchAsync(BatchSize, stoppingToken) > 0)
                        {
                        }
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        var logger = scope.ServiceProvider.GetService<IApplicationErrorLogger>();
                        if (logger != null)
                        {
                            await logger.LogMessage(LogLevelInfo.Exception, ex);
                        }
                    }
                }
            }
        }
    }
}
