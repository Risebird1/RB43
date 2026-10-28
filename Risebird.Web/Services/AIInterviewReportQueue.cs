using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Risebird.Web.Services
{
    /// <summary>
    /// Generates an AI interview's report outside the request that ended it. Generation takes 20-30
    /// seconds, which the candidate spent staring at a frozen page after clicking Exit Interview. A
    /// fire-and-forget Task was tried before and was abandoned with the request; a hosted worker is
    /// not. A job lost to a restart is not lost for good: the Report page generates on demand.
    /// </summary>
    public sealed class AIInterviewReportQueue
    {
        public sealed record Job(Guid SessionId, string Prompt, int AIInterviewType);

        private readonly Channel<Job> _channel = Channel.CreateUnbounded<Job>(new UnboundedChannelOptions { SingleReader = false });

        public void Enqueue(Job job) => _channel.Writer.TryWrite(job);

        public ValueTask<Job> DequeueAsync(CancellationToken ct) => _channel.Reader.ReadAsync(ct);
    }

    public sealed class AIInterviewReportWorker : BackgroundService
    {
        private const int Workers = 2;
        private readonly AIInterviewReportQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;

        public AIInterviewReportWorker(AIInterviewReportQueue queue, IServiceScopeFactory scopeFactory)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var loops = new Task[Workers];
            for (var i = 0; i < Workers; i++)
                loops[i] = RunLoopAsync(stoppingToken);
            return Task.WhenAll(loops);
        }

        private async Task RunLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                AIInterviewReportQueue.Job job;
                try { job = await _queue.DequeueAsync(ct); }
                catch (OperationCanceledException) { return; }

                using (var scope = _scopeFactory.CreateScope())
                {
                    try
                    {
                        var api = scope.ServiceProvider.GetRequiredService<IInterviewApiService>();
                        await api.GenerateReport(job.SessionId, job.Prompt, job.AIInterviewType, isExit: true);
                    }
                    catch (Exception ex)
                    {
                        var logger = scope.ServiceProvider.GetService<IApplicationErrorLogger>();
                        if (logger != null)
                            await logger.LogMessage(LogLevelInfo.Exception,
                                new Exception($"Background report generation failed for AI interview session {job.SessionId}.", ex));
                    }
                }
            }
        }
    }
}
