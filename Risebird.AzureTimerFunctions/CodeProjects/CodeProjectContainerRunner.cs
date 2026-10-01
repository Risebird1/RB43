using Risebird.Business;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;

namespace Risebird.AzureTimerFunctions.CodeProjects
{
    /// <summary>
    /// Build + test in a throwaway container: one Azure Container Apps job execution per run, on the
    /// stack's runner image, in a VNet whose NSG allows only Azure platform endpoints and Storage.
    /// The project (which includes hidden tests) goes in through a short-lived read SAS and the
    /// results come back through a write SAS; both blobs are deleted afterwards.
    /// </summary>
    public class CodeProjectContainerRunner
    {
        public const string RunContainer = "codeprojectruns";
        private const string ContainerAppsApi = "api-version=2025-07-01";
        /// <summary>HTTP-triggered functions are cut off at 230 s in Azure.</summary>
        private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(210);

        private readonly AzureArmRestClient _arm;
        private readonly ICodeProjectBlobStore _blobStore;
        private readonly ICodeProjectSettingsProvider _settings;
        private readonly IApplicationErrorLogger _logger;

        public CodeProjectContainerRunner(AzureArmRestClient arm, ICodeProjectBlobStore blobStore, ICodeProjectSettingsProvider settings, IApplicationErrorLogger logger)
        {
            _arm = arm;
            _blobStore = blobStore;
            _settings = settings;
            _logger = logger;
        }

        public async Task<CodeProjectRunResult> RunAsync(CodeProjectRunRequest request, CancellationToken ct)
        {
            var stack = request?.StackImageCode;
            var resourceGroup = _settings.Get(CodeProjectSettingNames.RunnerResourceGroup);
            var job = _settings.Get(CodeProjectSettingNames.RunnerJobPrefix + stack);
            var image = _settings.Get(CodeProjectSettingNames.RunnerImagePrefix + stack);
            if (string.IsNullOrEmpty(resourceGroup) || string.IsNullOrEmpty(job) || string.IsNullOrEmpty(image))
                return new CodeProjectRunResult { Completed = false, Error = $"No runner is set up for stack '{stack}'. Run the code project setup." };

            var started = DateTime.UtcNow;
            var id = Guid.NewGuid().ToString("N");
            var inputBlob = $"{id}/input.zip";
            var outputBlob = $"{id}/output.tgz";
            try
            {
                await _blobStore.UploadAsync(RunContainer, inputBlob, CodeProjectQuestionService.Zip(request.Files), "application/zip", ct);
                var zipUrl = _blobStore.CreateReadUrl(RunContainer, inputBlob, TimeSpan.FromMinutes(20));
                var resultUrl = await _blobStore.CreateAppendUrlAsync(RunContainer, outputBlob, TimeSpan.FromMinutes(20), ct);

                var start = await _arm.PostAsync($"resourceGroups/{resourceGroup}/providers/Microsoft.App/jobs/{job}/start?{ContainerAppsApi}", new
                {
                    containers = new[]
                    {
                        new
                        {
                            name = "runner",
                            image,
                            env = new[]
                            {
                                new { name = "ZIP_URL", value = zipUrl },
                                new { name = "RESULT_URL", value = resultUrl },
                                new { name = "BUILD_CMD", value = request.BuildCommand ?? "true" },
                                new { name = "TEST_CMD", value = request.TestCommand ?? "true" },
                                new { name = "RB_TIMEOUT", value = Math.Clamp(request.TimeoutSeconds, 10, 150).ToString() }
                            },
                            resources = new { cpu = 1.0, memory = "2Gi" }
                        }
                    }
                }, ct);
                if (!start.IsSuccess)
                    return new CodeProjectRunResult { Completed = false, Error = "Could not start the runner: " + start.Error };

                var execution = start.Body?["name"]?.ToString();
                // 202: the execution name arrives on the Location URL.
                while (string.IsNullOrEmpty(execution) && !string.IsNullOrEmpty(start.Location) && DateTime.UtcNow - started < Deadline)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), ct);
                    var op = await _arm.GetAsync(start.Location, ct);
                    execution = op.Body?["name"]?.ToString();
                }
                if (string.IsNullOrEmpty(execution))
                    return new CodeProjectRunResult { Completed = false, Error = "The runner did not report an execution." };

                string status = null;
                while (DateTime.UtcNow - started < Deadline)
                {
                    await Task.Delay(TimeSpan.FromSeconds(3), ct);
                    var get = await _arm.GetAsync($"resourceGroups/{resourceGroup}/providers/Microsoft.App/jobs/{job}/executions/{execution}?{ContainerAppsApi}", ct);
                    status = get.Body?["properties"]?["status"]?.ToString();
                    if (status == "Succeeded" || status == "Failed" || status == "Stopped" || status == "Degraded") break;
                }

                if (!await _blobStore.ExistsAsync(RunContainer, outputBlob, ct))
                    return new CodeProjectRunResult
                    {
                        Completed = false,
                        Error = status == null || status == "Running" || status == "Processing"
                            ? "The runner did not finish in time."
                            : $"The runner execution {status?.ToLowerInvariant()} without producing results."
                    };

                return CodeProjectRunnerHelper.ParseOutput(await _blobStore.DownloadAsync(RunContainer, outputBlob, ct));
            }
            catch (Exception ex) when (!(ex is OperationCanceledException && ct.IsCancellationRequested))
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return new CodeProjectRunResult { Completed = false, Error = "Runner failure: " + ex.Message };
            }
            finally
            {
                // The input holds hidden tests and the reference solution; never leave it behind.
                try { await _blobStore.DeleteAsync(RunContainer, inputBlob, CancellationToken.None); } catch { }
                try { await _blobStore.DeleteAsync(RunContainer, outputBlob, CancellationToken.None); } catch { }
            }
        }
    }
}
