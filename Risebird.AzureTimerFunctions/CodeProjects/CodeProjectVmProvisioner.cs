using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Risebird.Business;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System.Security.Cryptography;
using System.Text;

namespace Risebird.AzureTimerFunctions.CodeProjects
{
    /// <summary>Settings under CodeProjects:Azure. The network, Bastion and gallery are pre-built (infra/code-projects).</summary>
    public sealed class CodeProjectAzureOptions
    {
        public string ResourceGroup { get; set; }
        public string Location { get; set; }
        /// <summary>Candidate subnet in the dedicated VNet (no peering to production). Its NSG carries the egress allowlist.</summary>
        public string SubnetId { get; set; }
        public string VmSize { get; set; } = "Standard_D2s_v5";
        /// <summary>Must match the gallery image definition: TrustedLaunch, or Standard.</summary>
        public string SecurityType { get; set; } = "TrustedLaunch";
        /// <summary>A specialized image carries its own accounts, so no osProfile may be sent.</summary>
        public bool ImageIsSpecialized { get; set; }
        public string BastionResourceGroup { get; set; }
        public string BastionName { get; set; }
        public string ComputeApiVersion { get; set; } = "2026-04-01";
        public string NetworkApiVersion { get; set; } = "2025-09-01";
        public int MaxLifetimeMinutes { get; set; } = 120;
        public string CollectContainer { get; set; } = "codeprojectcollect";
        public string WorkspacePath { get; set; } = @"C:\workspace";

        public static CodeProjectAzureOptions From(IConfiguration configuration)
        {
            var o = new CodeProjectAzureOptions();
            configuration.GetSection("CodeProjects:Azure").Bind(o);
            o.BastionResourceGroup = string.IsNullOrWhiteSpace(o.BastionResourceGroup) ? o.ResourceGroup : o.BastionResourceGroup;
            return o;
        }

        public string MissingSetting() =>
            string.IsNullOrWhiteSpace(ResourceGroup) ? "CodeProjects:Azure:ResourceGroup"
            : string.IsNullOrWhiteSpace(Location) ? "CodeProjects:Azure:Location"
            : string.IsNullOrWhiteSpace(SubnetId) ? "CodeProjects:Azure:SubnetId"
            : string.IsNullOrWhiteSpace(BastionName) ? "CodeProjects:Azure:BastionName"
            : null;
    }

    /// <summary>
    /// Builds, hands over, reads and tears down code project candidate machines.
    ///
    /// Each machine goes requested → nic → vm → setup → link → ready. Every step starts one Azure
    /// long-running operation and returns; the next call (the web app's status poll, or the
    /// 15-second advance timer) checks it and starts the next. Every PUT is idempotent, so a crash
    /// between "Azure accepted" and "we recorded it" only repeats the same PUT.
    /// </summary>
    public class CodeProjectVmProvisioner
    {
        public const string CandidateUserName = "candidate";

        private readonly AzureArmRestClient _arm;
        private readonly ICodeProjectVmRepository _repository;
        private readonly ICodeProjectBlobStore _blobStore;
        private readonly IEncryptionHelper _encryption;
        private readonly IApplicationErrorLogger _logger;
        private readonly CodeProjectAzureOptions _options;

        public CodeProjectVmProvisioner(AzureArmRestClient arm, ICodeProjectVmRepository repository, ICodeProjectBlobStore blobStore,
            IEncryptionHelper encryption, IApplicationErrorLogger logger, IConfiguration configuration)
        {
            _arm = arm;
            _repository = repository;
            _blobStore = blobStore;
            _encryption = encryption;
            _logger = logger;
            _options = CodeProjectAzureOptions.From(configuration);
        }

        private string Compute => "api-version=" + _options.ComputeApiVersion;
        private string Network => "api-version=" + _options.NetworkApiVersion;
        private static string VmPath(CodeProjectVm m) => $"resourceGroups/{m.ResourceGroupName}/providers/Microsoft.Compute/virtualMachines/{m.VmName}";
        private static string NicPath(CodeProjectVm m) => $"resourceGroups/{m.ResourceGroupName}/providers/Microsoft.Network/networkInterfaces/{m.NicName}";
        private string BastionPath => $"resourceGroups/{_options.BastionResourceGroup}/providers/Microsoft.Network/bastionHosts/{_options.BastionName}";
        private string VmResourceId(CodeProjectVm m) => $"/subscriptions/{_arm.SubscriptionId}/{VmPath(m)}";

        // ------------------------------------------------------------------ provision

        public async Task<CodeProjectProvisionResult> ProvisionAsync(CodeProjectProvisionRequest request, CancellationToken ct)
        {
            var missing = _options.MissingSetting();
            if (missing != null)
                return new CodeProjectProvisionResult { Accepted = false, Error = $"Candidate environments are not configured ({missing})." };
            if (string.IsNullOrWhiteSpace(request?.GalleryImageId) || string.IsNullOrWhiteSpace(request.ProjectZipUrl))
                return new CodeProjectProvisionResult { Accepted = false, Error = "The stack image or the project is missing." };
            if (!string.IsNullOrWhiteSpace(request.OsType) && !request.OsType.StartsWith("win", StringComparison.OrdinalIgnoreCase))
                return new CodeProjectProvisionResult { Accepted = false, Error = $"Only Windows stack images are supported (got {request.OsType})." };

            var handle = Guid.NewGuid().ToString("N");
            var names = CodeProjectVmHelper.Names(handle);
            var row = new CodeProjectVm
            {
                Handle = handle,
                SessionId = request.SessionId,
                QuestionId = request.QuestionId,
                ResourceGroupName = _options.ResourceGroup,
                Location = _options.Location,
                VmName = names.VmName,
                NicName = names.NicName,
                StackImageCode = request.StackImageCode,
                GalleryImageId = request.GalleryImageId,
                OsType = request.OsType ?? "Windows",
                ProjectZipUrl = request.ProjectZipUrl,
                ReadOnlyPathsJson = Newtonsoft.Json.JsonConvert.SerializeObject(request.ReadOnlyPaths ?? new List<string>()),
                CandidateUserName = CandidateUserName,
                // Both generated once and persisted, so a retried VM PUT is byte-identical.
                CandidatePasswordProtected = await _encryption.EncryptData(CodeProjectVmHelper.NewPassword(16)),
                AdminPasswordProtected = await _encryption.EncryptData(CodeProjectVmHelper.NewPassword(24))
            };

            var stored = await _repository.Add(row);   // an existing (session, question) returns its own handle
            await AdvanceAsync(stored, ct);
            return new CodeProjectProvisionResult { Accepted = true, Handle = stored };
        }

        // ------------------------------------------------------------------ status

        public async Task<CodeProjectEnvironmentStatus> GetStatusAsync(string handle, CancellationToken ct)
        {
            await AdvanceAsync(handle, ct);
            var m = await _repository.Get(handle);
            if (m == null) return new CodeProjectEnvironmentStatus { State = CodeProjectEnvironmentState.Failed, Message = "Unknown environment." };

            switch (m.Step)
            {
                case CodeProjectVmStep.Ready:
                    return new CodeProjectEnvironmentStatus
                    {
                        State = CodeProjectEnvironmentState.Ready,
                        ConnectUrl = m.ConnectUrl,
                        VMPropertyId = m.VMPropertyId,
                        Message = "Opening secure session…",
                        // Bastion shareable links carry no credentials; the candidate signs in with these.
                        Username = m.CandidateUserName,
                        Password = await _encryption.DecryptData(m.CandidatePasswordProtected)
                    };
                case CodeProjectVmStep.Failed:
                case CodeProjectVmStep.Released:
                    return new CodeProjectEnvironmentStatus { State = CodeProjectEnvironmentState.Failed, VMPropertyId = m.VMPropertyId, Message = m.Message };
                default:
                    return new CodeProjectEnvironmentStatus { State = CodeProjectEnvironmentState.Provisioning, VMPropertyId = m.VMPropertyId, Message = m.Message };
            }
        }

        // ------------------------------------------------------------------ advance

        /// <summary>Runs at most one step. Safe to call from any number of places at once.</summary>
        public async Task AdvanceAsync(string handle, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(handle) || !await _repository.TryLease(handle)) return;
            var m = await _repository.Get(handle);
            if (m == null || CodeProjectVmStep.IsTerminal(m.Step)) return;

            try
            {
                switch (m.Step)
                {
                    case CodeProjectVmStep.Requested: await CreateNicAsync(m, ct); break;
                    case CodeProjectVmStep.Nic: await CreateVmWhenNicReadyAsync(m, ct); break;
                    case CodeProjectVmStep.Vm: await RunSetupWhenVmReadyAsync(m, ct); break;
                    case CodeProjectVmStep.Setup: await LinkWhenSetupDoneAsync(m, ct); break;
                    case CodeProjectVmStep.Link: await ReadLinkAsync(m, ct); break;
                }
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                // Leave the step as it is and drop the lease: the next call retries it. If even that
                // write fails, the lease simply expires.
                try { await _repository.Update(m.Handle, m.Step, m.Message, releaseLease: true); }
                catch (Exception inner) { await _logger.LogMessage(LogLevelInfo.Exception, inner); }
            }
        }

        private Task Fail(CodeProjectVm m, string reason) =>
            _repository.Update(m.Handle, CodeProjectVmStep.Failed, reason);

        private async Task CreateNicAsync(CodeProjectVm m, CancellationToken ct)
        {
            // No public IP: the only way in is Bastion. The subnet's NSG holds the egress allowlist.
            var r = await _arm.PutAsync($"{NicPath(m)}?{Network}", new
            {
                location = m.Location,
                tags = Tags(m),
                properties = new
                {
                    ipConfigurations = new[]
                    {
                        new { name = "ipconfig1", properties = new { subnet = new { id = _options.SubnetId }, privateIPAllocationMethod = "Dynamic" } }
                    }
                }
            }, ct);
            if (!r.IsSuccess) { await Fail(m, "Could not create the network interface: " + r.Error); return; }
            await _repository.Update(m.Handle, CodeProjectVmStep.Nic, "Allocating machine…");
        }

        private async Task CreateVmWhenNicReadyAsync(CodeProjectVm m, CancellationToken ct)
        {
            var nic = await _arm.GetAsync($"{NicPath(m)}?{Network}", ct);
            if (!nic.IsSuccess) { await Fail(m, "Network interface lookup failed: " + nic.Error); return; }
            if (nic.ProvisioningState == "Failed") { await Fail(m, "The network interface failed to provision."); return; }
            if (nic.ProvisioningState != "Succeeded") { await _repository.Update(m.Handle, m.Step, m.Message); return; }

            var properties = new JObject
            {
                ["hardwareProfile"] = new JObject { ["vmSize"] = _options.VmSize },
                ["storageProfile"] = new JObject
                {
                    // A gallery image definition id (no /versions/...) takes the latest version.
                    ["imageReference"] = new JObject { ["id"] = m.GalleryImageId },
                    ["osDisk"] = new JObject
                    {
                        ["createOption"] = "FromImage",
                        ["deleteOption"] = "Delete",
                        ["caching"] = "ReadWrite",
                        ["managedDisk"] = new JObject { ["storageAccountType"] = "StandardSSD_LRS" }
                    }
                },
                ["networkProfile"] = new JObject
                {
                    ["networkInterfaces"] = new JArray
                    {
                        new JObject { ["id"] = nic.Body["id"], ["properties"] = new JObject { ["primary"] = true, ["deleteOption"] = "Delete" } }
                    }
                },
                // Managed boot diagnostics (no storage account needed) for the portal's screenshot/serial log.
                ["diagnosticsProfile"] = new JObject { ["bootDiagnostics"] = new JObject { ["enabled"] = true } }
            };
            if (!_options.ImageIsSpecialized)
            {
                properties["osProfile"] = new JObject
                {
                    ["computerName"] = CodeProjectVmHelper.Names(m.Handle).ComputerName,
                    ["adminUsername"] = CodeProjectVmHelper.Names(m.Handle).AdminUser,
                    ["adminPassword"] = await _encryption.DecryptData(m.AdminPasswordProtected),
                    ["windowsConfiguration"] = new JObject
                    {
                        ["provisionVMAgent"] = true,
                        // No reboot or patch run may land in the middle of an interview.
                        ["enableAutomaticUpdates"] = false,
                        ["patchSettings"] = new JObject { ["patchMode"] = "Manual" }
                    }
                };
            }
            if (!string.Equals(_options.SecurityType, "Standard", StringComparison.OrdinalIgnoreCase))
            {
                properties["securityProfile"] = new JObject
                {
                    ["securityType"] = _options.SecurityType,
                    ["uefiSettings"] = new JObject { ["secureBoot"] = true, ["vTPM"] = true }
                };
            }

            // No "identity" block: candidate machines get no managed identity.
            var vm = await _arm.PutAsync($"{VmPath(m)}?{Compute}", new JObject
            {
                ["location"] = m.Location,
                ["tags"] = JObject.FromObject(Tags(m)),
                ["properties"] = properties
            }, ct);
            if (!vm.IsSuccess) { await Fail(m, "Could not create the machine: " + vm.Error); return; }

            var vmPropertyId = m.VMPropertyId ?? await _repository.AddVMProperty(m.VmName, m.ResourceGroupName, "Windows", m.Location, m.CandidateUserName);
            await _repository.Update(m.Handle, CodeProjectVmStep.Vm, "Starting machine…", vmPropertyId: vmPropertyId);
        }

        private async Task RunSetupWhenVmReadyAsync(CodeProjectVm m, CancellationToken ct)
        {
            var vm = await _arm.GetAsync($"{VmPath(m)}?{Compute}", ct);
            if (!vm.IsSuccess) { await Fail(m, "Machine lookup failed: " + vm.Error); return; }
            if (vm.ProvisioningState == "Failed") { await Fail(m, "The machine failed to provision."); return; }
            if (vm.ProvisioningState != "Succeeded") { await _repository.Update(m.Handle, m.Step, m.Message); return; }
            if (string.IsNullOrEmpty(m.ProjectZipUrl)) { await Fail(m, "The project link was already consumed."); return; }

            var r = await _arm.PutAsync($"{VmPath(m)}/runCommands/rb-setup?{Compute}", new
            {
                location = m.Location,
                properties = new
                {
                    source = new { script = CodeProjectVmScripts.Setup },
                    parameters = new[]
                    {
                        new { name = "WorkspacePath", value = _options.WorkspacePath },
                        new { name = "CandidateUser", value = m.CandidateUserName }
                    },
                    protectedParameters = new[]
                    {
                        new { name = "CandidatePassword", value = await _encryption.DecryptData(m.CandidatePasswordProtected) },
                        new { name = "ProjectZipUrl", value = m.ProjectZipUrl }
                    },
                    asyncExecution = false,
                    treatFailureAsDeploymentFailure = true,
                    timeoutInSeconds = 600
                }
            }, ct);
            if (!r.IsSuccess) { await Fail(m, "Could not start machine setup: " + r.Error); return; }
            await _repository.Update(m.Handle, CodeProjectVmStep.Setup, "Restoring project files…");
        }

        private async Task LinkWhenSetupDoneAsync(CodeProjectVm m, CancellationToken ct)
        {
            var rc = await _arm.GetAsync($"{VmPath(m)}/runCommands/rb-setup?$expand=instanceView&{Compute}", ct);
            if (!rc.IsSuccess) { await Fail(m, "Setup status lookup failed: " + rc.Error); return; }
            var view = rc.Body?["properties"]?["instanceView"];
            var state = view?["executionState"]?.ToString();
            if (state == "Pending" || state == "Running" || state == null || state == "Unknown")
            {
                await _repository.Update(m.Handle, m.Step, "Restoring project files…");
                return;
            }
            if (state != "Succeeded" || (view["exitCode"]?.Value<int?>() ?? 0) != 0)
            {
                var error = view?["error"]?.ToString();
                var message = view?["executionMessage"]?.ToString();
                await Fail(m, $"Machine setup {state?.ToLowerInvariant()}: {Last(string.IsNullOrWhiteSpace(error) ? message : error, 600)}");
                return;
            }

            // The zip link has done its job; do not keep a live SAS around.
            await _repository.Update(m.Handle, CodeProjectVmStep.Link, "Opening secure session…", clearProjectZipUrl: true, releaseLease: false);
            await CreateLinkAsync(m, ct);
        }

        private async Task CreateLinkAsync(CodeProjectVm m, CancellationToken ct)
        {
            var r = await _arm.PostAsync($"{BastionPath}/createShareableLinks?{Network}", LinkBody(m), ct);
            if (!r.IsSuccess) { await Fail(m, "Could not create the secure connection link: " + r.Error); return; }
            var url = CodeProjectVmHelper.ExtractShareableLink(r.Body, VmResourceId(m));
            if (url != null) await _repository.Update(m.Handle, CodeProjectVmStep.Ready, "Ready", connectUrl: url);
            else await _repository.Update(m.Handle, CodeProjectVmStep.Link, "Opening secure session…");   // 202: read it next time
        }

        private async Task ReadLinkAsync(CodeProjectVm m, CancellationToken ct)
        {
            var r = await _arm.PostAsync($"{BastionPath}/getShareableLinks?{Network}", LinkBody(m), ct);
            var url = r.IsSuccess ? CodeProjectVmHelper.ExtractShareableLink(r.Body, VmResourceId(m)) : null;
            if (url != null) { await _repository.Update(m.Handle, CodeProjectVmStep.Ready, "Ready", connectUrl: url); return; }
            // Not created yet (or the create was lost): ask again. Creating is idempotent per VM.
            if ((DateTime.UtcNow - m.UpdatedAtUtc) > TimeSpan.FromSeconds(30)) await CreateLinkAsync(m, ct);
            else await _repository.Update(m.Handle, m.Step, m.Message);
        }

        private object LinkBody(CodeProjectVm m) => new { vms = new[] { new { vm = new { id = VmResourceId(m) } } } };

        // ------------------------------------------------------------------ collect

        /// <summary>
        /// Reads the candidate's last commit off the machine with a one-off Run Command whose output
        /// goes to a private append blob (Run Command's instance view keeps only a tail of the output).
        /// </summary>
        public async Task<CodeProjectWorkspaceSnapshot> CollectAsync(string handle, CancellationToken ct)
        {
            var m = await _repository.Get(handle);
            if (m == null || m.Step != CodeProjectVmStep.Ready)
                return new CodeProjectWorkspaceSnapshot { Success = false, Error = m == null ? "Unknown environment." : "The machine is not available (" + m.Step + ")." };

            var runName = "rb-collect-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var blobName = $"{m.Handle}/{runName}.txt";
            var outputUrl = await _blobStore.CreateAppendUrlAsync(_options.CollectContainer, blobName, TimeSpan.FromMinutes(30), ct);
            var put = await _arm.PutAsync($"{VmPath(m)}/runCommands/{runName}?{Compute}", new
            {
                location = m.Location,
                properties = new
                {
                    source = new { script = CodeProjectVmScripts.Collect },
                    parameters = new[] { new { name = "WorkspacePath", value = _options.WorkspacePath } },
                    outputBlobUri = outputUrl,
                    asyncExecution = false,
                    timeoutInSeconds = 180
                }
            }, ct);
            if (!put.IsSuccess) return new CodeProjectWorkspaceSnapshot { Success = false, Error = "Could not read the workspace: " + put.Error };

            // HTTP-triggered functions are cut off at 230 s; stay well inside that.
            var deadline = DateTime.UtcNow.AddSeconds(170);
            JToken view = null;
            while (DateTime.UtcNow < deadline)
            {
                await Task.Delay(TimeSpan.FromSeconds(4), ct);
                var get = await _arm.GetAsync($"{VmPath(m)}/runCommands/{runName}?$expand=instanceView&{Compute}", ct);
                view = get.Body?["properties"]?["instanceView"];
                var state = view?["executionState"]?.ToString();
                if (state == "Succeeded" || state == "Failed" || state == "TimedOut" || state == "Canceled") break;
            }

            try
            {
                var state = view?["executionState"]?.ToString();
                if (state != "Succeeded")
                    return new CodeProjectWorkspaceSnapshot { Success = false, Error = $"Reading the workspace {state?.ToLowerInvariant() ?? "timed out"}: {Last(view?["error"]?.ToString(), 400)}" };

                var output = Encoding.UTF8.GetString(await _blobStore.DownloadAsync(_options.CollectContainer, blobName, ct));
                return CodeProjectVmHelper.ParseSnapshot(output);
            }
            finally
            {
                // Tidy the one-off run command; the VM itself goes on release.
                try { await _arm.DeleteAsync($"{VmPath(m)}/runCommands/{runName}?{Compute}", ct); } catch { }
            }
        }

        // ------------------------------------------------------------------ release

        /// <summary>Removes the Bastion link and deletes the VM; its NIC and OS disk go with it (deleteOption).</summary>
        public async Task ReleaseAsync(string handle, CancellationToken ct)
        {
            var m = await _repository.Get(handle);
            if (m == null || m.Step == CodeProjectVmStep.Released) return;

            if (!string.IsNullOrEmpty(m.ConnectUrl) && !string.IsNullOrWhiteSpace(_options.BastionName))
            {
                var unlink = await _arm.PostAsync($"{BastionPath}/deleteShareableLinks?{Network}", LinkBody(m), ct);
                if (!unlink.IsSuccess && !unlink.IsNotFound)
                    await _logger.LogMessage(LogLevelInfo.Warning, new Exception($"Bastion link delete for {m.VmName}: {unlink.Error}"));
            }

            var del = await _arm.DeleteAsync($"{VmPath(m)}?forceDeletion=true&{Compute}", ct);
            if (!del.IsSuccess && !del.IsNotFound)
            {
                // Keep the row so the janitor tries again rather than losing track of a running VM.
                await _repository.Update(m.Handle, m.Step == CodeProjectVmStep.Failed ? m.Step : CodeProjectVmStep.Failed, "Release pending: " + del.Error);
                return;
            }
            if (del.IsNotFound)
            {
                // The VM never existed (failed early); its NIC may, and is not covered by deleteOption.
                var nic = await _arm.DeleteAsync($"{NicPath(m)}?{Network}", ct);
                if (!nic.IsSuccess && !nic.IsNotFound)
                {
                    await _repository.Update(m.Handle, CodeProjectVmStep.Failed, "Release pending: " + nic.Error);
                    return;
                }
            }
            await _repository.Update(m.Handle, CodeProjectVmStep.Released, "Released");
        }

        // ------------------------------------------------------------------ timers

        public async Task AdvanceAllAsync(CancellationToken ct)
        {
            foreach (var handle in await _repository.GetInProgress())
                await AdvanceAsync(handle, ct);
        }

        public async Task ReleaseExpiredAsync(CancellationToken ct)
        {
            foreach (var handle in await _repository.GetExpired(_options.MaxLifetimeMinutes, failedGraceMinutes: 10))
            {
                try { await ReleaseAsync(handle, ct); }
                catch (Exception ex) { await _logger.LogMessage(LogLevelInfo.Exception, ex); }
            }
        }

        // ------------------------------------------------------------------ helpers

        private static Dictionary<string, string> Tags(CodeProjectVm m) => new Dictionary<string, string>
        {
            ["purpose"] = "rb-code-project",
            ["rb-handle"] = m.Handle,
            ["rb-question"] = m.QuestionId.ToString(),
            ["rb-stack"] = m.StackImageCode ?? ""
        };

        private static string Last(string s, int max) => string.IsNullOrEmpty(s) || s.Length <= max ? s : s.Substring(s.Length - max);

    }
}
