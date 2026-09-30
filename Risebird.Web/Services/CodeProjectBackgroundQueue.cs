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
    /// Work that must not run inside a request: validating a generated project takes minutes
    /// (build + two test runs), and look-ahead provisioning starts a VM while the candidate is
    /// still on the previous question.
    /// </summary>
    public sealed class CodeProjectBackgroundQueue
    {
        public abstract record WorkItem;
        public sealed record Validate(int QuestionId) : WorkItem;
        public sealed record PrepareNextEnvironment(string SessionId, int InterviewId, int IrfId, int CurrentQuestionId) : WorkItem;
        public sealed record PrepareEnvironment(string SessionId, int QuestionId) : WorkItem;

        private readonly Channel<WorkItem> _channel = Channel.CreateUnbounded<WorkItem>(new UnboundedChannelOptions { SingleReader = false });

        public void Enqueue(WorkItem item) => _channel.Writer.TryWrite(item);

        public ValueTask<WorkItem> DequeueAsync(CancellationToken ct) => _channel.Reader.ReadAsync(ct);
    }

    public sealed class CodeProjectBackgroundWorker : BackgroundService
    {
        private const int Workers = 3;
        private readonly CodeProjectBackgroundQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;

        public CodeProjectBackgroundWorker(CodeProjectBackgroundQueue queue, IServiceScopeFactory scopeFactory)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await ResumeUnfinishedValidationsAsync(stoppingToken);
            var loops = new Task[Workers];
            for (var i = 0; i < Workers; i++)
                loops[i] = RunLoopAsync(stoppingToken);
            await Task.WhenAll(loops);
        }

        private async Task RunLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                CodeProjectBackgroundQueue.WorkItem item;
                try { item = await _queue.DequeueAsync(ct); }
                catch (OperationCanceledException) { return; }

                using (var scope = _scopeFactory.CreateScope())
                {
                    var service = scope.ServiceProvider.GetRequiredService<ICodeProjectQuestionService>();
                    try
                    {
                        switch (item)
                        {
                            case CodeProjectBackgroundQueue.Validate v:
                                await service.ValidateAsync(v.QuestionId, ct);
                                break;
                            case CodeProjectBackgroundQueue.PrepareNextEnvironment n:
                                await service.PrepareNextEnvironmentAsync(n.SessionId, n.InterviewId, n.IrfId, n.CurrentQuestionId, ct);
                                break;
                            case CodeProjectBackgroundQueue.PrepareEnvironment p:
                                await service.PrepareEnvironmentAsync(p.SessionId, p.QuestionId, ct);
                                break;
                        }
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        var logger = scope.ServiceProvider.GetService<IApplicationErrorLogger>();
                        if (logger != null) await logger.LogMessage(LogLevelInfo.Exception, ex);
                    }
                }
            }
        }

        /// <summary>Validation state lives in the database, so a restart mid-job simply re-runs it.</summary>
        private async Task ResumeUnfinishedValidationsAsync(CancellationToken ct)
        {
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var service = scope.ServiceProvider.GetRequiredService<ICodeProjectQuestionService>();
                    foreach (var id in await service.GetUnfinishedValidations())
                        _queue.Enqueue(new CodeProjectBackgroundQueue.Validate(id));
                }
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var logger = scope.ServiceProvider.GetService<IApplicationErrorLogger>();
                    if (logger != null) await logger.LogMessage(LogLevelInfo.Exception, ex);
                }
            }
        }
    }
}
