using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Risebird.Business;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System.Diagnostics;
using System.Net;
using System.Reflection;

namespace Risebird.AzureTimerFunctions.CodeProjects.Setup
{
    /// <summary>
    /// One-click setup of everything code project questions need in Azure, run from the functions app
    /// with its existing service principal (AzureVmCredentials:*):
    ///
    ///   database schema → resource providers → resource groups → managed identities → role assignments
    ///   → network (NSGs, NAT gateway, VNet) → Bastion (Standard, shareable links)
    ///   → Compute Gallery + image definitions → candidate VM images (Azure Image Builder)
    ///   → container registry → runner images (ACR Tasks) → Container Apps environment → runner jobs
    ///   → StackImages rows → settings for both apps → deployed functions app (URL + key) → runner smoke test
    ///
    /// Every step is re-entrant: it looks at what exists, creates only what is missing, and reports
    /// "waiting" while Azure provisions. A 30-second timer (and the Start call) advance the run; the
    /// state lives in CodeProjectInfraSetupRuns, so a restart resumes where it stopped.
    /// </summary>
    public class CodeProjectInfraSetupService
    {
        // ---------------------------------------------------------------- API versions
        private const string ResourcesApi = "api-version=2021-04-01";
        private const string NetworkApi = "api-version=2024-05-01";
        private const string IdentityApi = "api-version=2023-01-31";
        private const string GalleryApi = "api-version=2024-03-03";
        private const string ImageBuilderApi = "api-version=2024-02-01";
        private const string RegistryApi = "api-version=2023-07-01";
        private const string RegistryTasksApi = "api-version=2019-06-01-preview";
        private const string ContainerAppsApi = "api-version=2025-07-01";
        private const string AuthorizationApi = "api-version=2022-04-01";
        private const string WebApi = "api-version=2024-04-01";

        private static readonly string[] Providers =
        {
            "Microsoft.Compute", "Microsoft.Network", "Microsoft.ManagedIdentity", "Microsoft.VirtualMachineImages",
            "Microsoft.ContainerInstance", "Microsoft.ContainerRegistry", "Microsoft.App", "Microsoft.Storage", "Microsoft.KeyVault"
        };

        private const string BuildContainer = "codeprojectbuild";
        private const int MaxErrorsPerStep = 8;
        private static readonly TimeSpan TickBudget = TimeSpan.FromSeconds(150);

        private readonly AzureArmRestClient _arm;
        private readonly ICodeProjectSetupRepository _setup;
        private readonly ICodeProjectSettingsRepository _settingsRepository;
        private readonly ICodeProjectSettingsProvider _settings;
        private readonly ICodeProjectBlobStore _blobStore;
        private readonly IEncryptionHelper _encryption;
        private readonly IApplicationErrorLogger _logger;
        private readonly CodeProjectContainerRunner _runner;
        private readonly Options _o;

        public CodeProjectInfraSetupService(AzureArmRestClient arm, ICodeProjectSetupRepository setup, ICodeProjectSettingsRepository settingsRepository,
            ICodeProjectSettingsProvider settings, ICodeProjectBlobStore blobStore, IEncryptionHelper encryption, IApplicationErrorLogger logger,
            CodeProjectContainerRunner runner, IConfiguration configuration)
        {
            _arm = arm;
            _setup = setup;
            _settingsRepository = settingsRepository;
            _settings = settings;
            _blobStore = blobStore;
            _encryption = encryption;
            _logger = logger;
            _runner = runner;
            _o = Options.From(configuration);
        }

        /// <summary>Everything is optional (CodeProjects:Setup:*); the defaults are the documented layout.</summary>
        private sealed class Options
        {
            public string Location { get; set; }
            public string ResourceGroup { get; set; } = "rg-rb-codeprojects";
            public string ImagesResourceGroup { get; set; } = "rg-rb-codeprojects-images";
            public string ImageBuilderLocation { get; set; }
            public List<CodeProjectStack> Stacks { get; set; }
            public List<string> PackageMirrorPrefixes { get; set; } = new List<string>();
            public bool ApplyDatabaseMigrations { get; set; } = true;
            public string ImageBuildVmSize { get; set; } = "Standard_D4s_v5";
            public int MaxParallelImageBuilds { get; set; } = 2;
            public string VnetPrefix { get; set; } = "10.240.0.0/16";
            public string CandidateSubnetPrefix { get; set; } = "10.240.0.0/20";
            public string RunnerSubnetPrefix { get; set; } = "10.240.16.0/24";
            public string BastionSubnetPrefix { get; set; } = "10.240.255.0/26";

            public static Options From(IConfiguration c)
            {
                string S(string k) => string.IsNullOrWhiteSpace(c["CodeProjects:Setup:" + k]) ? null : c["CodeProjects:Setup:" + k].Trim();
                var o = new Options
                {
                    Location = (S("Location") ?? c["CodeProjects:Azure:Location"] ?? "centralindia").Replace(" ", "").ToLowerInvariant()
                };
                o.ResourceGroup = S("ResourceGroup") ?? o.ResourceGroup;
                o.ImagesResourceGroup = S("ImagesResourceGroup") ?? o.ImagesResourceGroup;
                o.ImageBuilderLocation = (S("ImageBuilderLocation") ?? o.Location).ToLowerInvariant();
                var codes = S("Stacks")?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                o.Stacks = codes == null
                    ? CodeProjectStackCatalog.All.ToList()
                    : codes.Select(CodeProjectStackCatalog.Find).Where(s => s != null).ToList();
                o.PackageMirrorPrefixes = S("PackageMirrorPrefixes")?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList() ?? o.PackageMirrorPrefixes;
                if (bool.TryParse(S("ApplyDatabaseMigrations"), out var apply)) o.ApplyDatabaseMigrations = apply;
                o.ImageBuildVmSize = S("ImageBuildVmSize") ?? o.ImageBuildVmSize;
                if (int.TryParse(S("MaxParallelImageBuilds"), out var parallel) && parallel > 0) o.MaxParallelImageBuilds = parallel;
                o.VnetPrefix = S("VnetPrefix") ?? o.VnetPrefix;
                o.CandidateSubnetPrefix = S("CandidateSubnetPrefix") ?? o.CandidateSubnetPrefix;
                o.RunnerSubnetPrefix = S("RunnerSubnetPrefix") ?? o.RunnerSubnetPrefix;
                o.BastionSubnetPrefix = S("BastionSubnetPrefix") ?? o.BastionSubnetPrefix;
                return o;
            }
        }

        // ---------------------------------------------------------------- names

        private string Rg => _o.ResourceGroup;
        private string ImgRg => _o.ImagesResourceGroup;
        private const string Prefix = "rb-cp";
        private string VnetPath => $"resourceGroups/{Rg}/providers/Microsoft.Network/virtualNetworks/{Prefix}-vnet";
        private string CandidateSubnetId => $"/subscriptions/{_arm.SubscriptionId}/{VnetPath}/subnets/candidates";
        private string RunnerSubnetId => $"/subscriptions/{_arm.SubscriptionId}/{VnetPath}/subnets/runners";
        private string BastionName => $"{Prefix}-bastion";
        private const string GalleryName = "rbcpgallery";
        private string GalleryPath => $"resourceGroups/{ImgRg}/providers/Microsoft.Compute/galleries/{GalleryName}";
        private string ImageDefinitionId(CodeProjectStack s) => $"/subscriptions/{_arm.SubscriptionId}/{GalleryPath}/images/{s.Code}";
        private string IdentityPath(string name) => $"resourceGroups/{ImgRg}/providers/Microsoft.ManagedIdentity/userAssignedIdentities/{name}";
        private const string BuilderIdentity = "rb-cp-image-builder";
        private const string RunnerIdentity = "rb-cp-runner";
        private string RegistryName => "rbcp" + CodeProjectStackCatalog.Hash(_arm.SubscriptionId ?? "").Substring(0, 10);
        private string RegistryPath => $"resourceGroups/{ImgRg}/providers/Microsoft.ContainerRegistry/registries/{RegistryName}";
        private string EnvironmentPath => $"resourceGroups/{Rg}/providers/Microsoft.App/managedEnvironments/{Prefix}-runners";
        private string JobPath(CodeProjectStack s) => $"resourceGroups/{Rg}/providers/Microsoft.App/jobs/{s.JobName}";

        // ---------------------------------------------------------------- steps

        private sealed record StepDef(string Key, string Title, string[] DependsOn, Func<CodeProjectSetupState, CancellationToken, Task<Outcome>> Run);

        private sealed record Outcome(string Status, string Detail)
        {
            public static Outcome Done(string d) => new Outcome(CodeProjectSetupStepStatus.Done, d);
            public static Outcome Wait(string d) => new Outcome(CodeProjectSetupStepStatus.Running, d);
            public static Outcome Fail(string d) => new Outcome(CodeProjectSetupStepStatus.Failed, d);
            public static Outcome Block(string d) => new Outcome(CodeProjectSetupStepStatus.Blocked, d);
            public static Outcome Skip(string d) => new Outcome(CodeProjectSetupStepStatus.Skipped, d);
        }

        private IReadOnlyList<StepDef> Steps() => new[]
        {
            new StepDef("database", "Database schema", Array.Empty<string>(), DatabaseAsync),
            new StepDef("preflight", "Azure access and storage", Array.Empty<string>(), PreflightAsync),
            new StepDef("providers", "Resource providers", new[] { "preflight" }, ProvidersAsync),
            new StepDef("groups", "Resource groups", new[] { "providers" }, GroupsAsync),
            new StepDef("identities", "Managed identities", new[] { "groups" }, IdentitiesAsync),
            new StepDef("access", "Role assignments", new[] { "identities" }, AccessAsync),
            new StepDef("network", "Network: VNet, NSGs, NAT gateway", new[] { "groups" }, NetworkAsync),
            new StepDef("bastion", "Azure Bastion (Standard, shareable links)", new[] { "network" }, BastionAsync),
            new StepDef("gallery", "Compute Gallery and image definitions", new[] { "groups" }, GalleryAsync),
            new StepDef("images", "Candidate VM images (Azure Image Builder)", new[] { "gallery", "access" }, ImagesAsync),
            new StepDef("registry", "Container registry", new[] { "groups", "access" }, RegistryAsync),
            new StepDef("runnerImages", "Runner container images (ACR Tasks)", new[] { "registry", "preflight" }, RunnerImagesAsync),
            new StepDef("runnerEnvironment", "Container Apps environment", new[] { "network" }, RunnerEnvironmentAsync),
            new StepDef("runnerJobs", "Runner jobs", new[] { "runnerEnvironment", "runnerImages", "registry" }, RunnerJobsAsync),
            new StepDef("stackImages", "StackImages rows", new[] { "images", "database" }, StackImagesAsync),
            new StepDef("settings", "Settings for the web and functions apps", new[] { "bastion", "runnerJobs", "database" }, SettingsAsync),
            new StepDef("functionsApp", "Deployed functions app URL and key", new[] { "settings" }, FunctionsAppAsync),
            new StepDef("smokeTest", "Runner smoke test", new[] { "settings" }, SmokeTestAsync)
        };

        // ---------------------------------------------------------------- run control

        /// <summary>Starts a run, or resumes the unfinished one (failed and blocked steps are retried).</summary>
        public async Task<object> StartAsync(string startedBy, CancellationToken ct)
        {
            // The run table itself comes from this script, so it is applied before anything else.
            await _setup.ApplySqlScript(ReadMigration("AddCodeProjectInfraSetup.sql"));

            var id = await _setup.StartOrResume(startedBy, JsonConvert.SerializeObject(NewState()));
            var run = await _setup.GetLatest();
            var state = Deserialize(run?.StateJson);
            foreach (var step in state.Steps.Where(s => s.Status == CodeProjectSetupStepStatus.Failed || s.Status == CodeProjectSetupStepStatus.Blocked))
            {
                step.Status = CodeProjectSetupStepStatus.Pending;
                step.Attempts = 0;
                step.Detail = "Retrying…";
            }
            Log(state, $"Run {id} started or resumed by {startedBy ?? "unknown"}.");
            await _setup.Save(id, CodeProjectSetupStatus.Running, JsonConvert.SerializeObject(state));

            await TickAsync(ct);
            return await GetStatusAsync();
        }

        public async Task<object> GetStatusAsync()
        {
            var run = await _setup.GetLatest();
            if (run == null) return new { status = "not-started", steps = Array.Empty<object>() };
            var state = Deserialize(run.StateJson);
            return new
            {
                id = run.Id,
                status = run.Status,
                startedBy = run.StartedBy,
                startedAtUtc = run.StartedAtUtc,
                updatedAtUtc = run.UpdatedAtUtc,
                completedAtUtc = run.CompletedAtUtc,
                steps = state.Steps.Select(s => new { s.Key, s.Title, s.Status, s.Detail, s.UpdatedAtUtc }),
                log = state.Log.TakeLast(40)
            };
        }

        /// <summary>Advances every step whose dependencies are met, once. Called by the timer and by Start.</summary>
        public async Task TickAsync(CancellationToken ct)
        {
            var run = await _setup.GetLatest();
            if (run == null || run.Status != CodeProjectSetupStatus.Running) return;
            if (!await _setup.TryLease(run.Id, leaseSeconds: 420)) return;

            var state = Deserialize(run.StateJson);
            var defs = Steps();
            EnsureSteps(state, defs);
            var clock = Stopwatch.StartNew();

            try
            {
                foreach (var def in defs)
                {
                    if (clock.Elapsed > TickBudget) break;
                    var step = state.Steps.First(s => s.Key == def.Key);
                    if (IsFinal(step.Status)) continue;
                    if (!def.DependsOn.All(d => IsSatisfied(state, d))) continue;

                    Outcome outcome;
                    try
                    {
                        step.StartedAtUtc ??= DateTime.UtcNow;
                        outcome = await def.Run(state, ct);
                    }
                    catch (Exception ex) when (!(ex is OperationCanceledException && ct.IsCancellationRequested))
                    {
                        await _logger.LogMessage(LogLevelInfo.Exception, ex);
                        step.Attempts++;
                        outcome = step.Attempts >= MaxErrorsPerStep
                            ? Outcome.Fail(ex.Message)
                            : Outcome.Wait($"Error, retrying ({step.Attempts}/{MaxErrorsPerStep}): {ex.Message}");
                    }

                    if (outcome.Status != step.Status || outcome.Detail != step.Detail)
                    {
                        if (outcome.Status != step.Status) Log(state, $"{def.Title}: {outcome.Status}. {outcome.Detail}");
                        step.Status = outcome.Status;
                        step.Detail = outcome.Detail;
                        step.UpdatedAtUtc = DateTime.UtcNow;
                    }
                    await _setup.Save(run.Id, CodeProjectSetupStatus.Running, JsonConvert.SerializeObject(state), releaseLease: false);
                }
            }
            finally
            {
                await _setup.Save(run.Id, OverallStatus(state, defs), JsonConvert.SerializeObject(state));
            }
        }

        private static string OverallStatus(CodeProjectSetupState state, IReadOnlyList<StepDef> defs)
        {
            if (state.Steps.All(s => s.Status == CodeProjectSetupStepStatus.Done || s.Status == CodeProjectSetupStepStatus.Skipped))
                return CodeProjectSetupStatus.Completed;

            // Still running while any unfinished step can make progress.
            var canProgress = defs.Any(d =>
            {
                var s = state.Steps.First(x => x.Key == d.Key);
                return !IsFinal(s.Status) && d.DependsOn.All(dep => !IsDead(state, dep, defs));
            });
            if (canProgress) return CodeProjectSetupStatus.Running;
            return state.Steps.Any(s => s.Status == CodeProjectSetupStepStatus.Blocked) ? CodeProjectSetupStatus.Blocked : CodeProjectSetupStatus.Failed;
        }

        private static bool IsDead(CodeProjectSetupState state, string key, IReadOnlyList<StepDef> defs)
        {
            var s = state.Steps.First(x => x.Key == key);
            if (s.Status == CodeProjectSetupStepStatus.Failed || s.Status == CodeProjectSetupStepStatus.Blocked) return true;
            var def = defs.First(d => d.Key == key);
            return !IsFinal(s.Status) && def.DependsOn.Any(dep => IsDead(state, dep, defs));
        }

        private static bool IsFinal(string status) =>
            status == CodeProjectSetupStepStatus.Done || status == CodeProjectSetupStepStatus.Skipped
            || status == CodeProjectSetupStepStatus.Failed || status == CodeProjectSetupStepStatus.Blocked;

        private static bool IsSatisfied(CodeProjectSetupState state, string key)
        {
            var s = state.Steps.FirstOrDefault(x => x.Key == key);
            return s != null && (s.Status == CodeProjectSetupStepStatus.Done || s.Status == CodeProjectSetupStepStatus.Skipped);
        }

        private CodeProjectSetupState NewState()
        {
            var state = new CodeProjectSetupState();
            EnsureSteps(state, Steps());
            return state;
        }

        private static void EnsureSteps(CodeProjectSetupState state, IReadOnlyList<StepDef> defs)
        {
            foreach (var d in defs)
                if (state.Steps.All(s => s.Key != d.Key))
                    state.Steps.Add(new CodeProjectSetupStep { Key = d.Key, Title = d.Title });
            state.Steps = defs.Select(d => state.Steps.First(s => s.Key == d.Key)).ToList();
        }

        private static CodeProjectSetupState Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new CodeProjectSetupState();
            try { return JsonConvert.DeserializeObject<CodeProjectSetupState>(json) ?? new CodeProjectSetupState(); }
            catch (JsonException) { return new CodeProjectSetupState(); }
        }

        private static void Log(CodeProjectSetupState state, string line)
        {
            state.Log.Add($"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}Z {line}");
            if (state.Log.Count > 200) state.Log.RemoveRange(0, state.Log.Count - 200);
        }

        private static string Get(CodeProjectSetupState s, string key) => s.Data.TryGetValue(key, out var v) ? v : null;

        // ---------------------------------------------------------------- ARM helpers

        /// <summary>
        /// Creates the resource if it does not exist and reports whether it is ready. Existing resources
        /// are left as they are, so re-running setup never fights a change someone made in the portal.
        /// </summary>
        private async Task<(bool Ready, string Error, ArmResponse Resource)> EnsureAsync(string path, Func<object> body, CancellationToken ct)
        {
            var get = await _arm.GetAsync(path, ct);
            if (get.IsSuccess)
            {
                var state = get.ProvisioningState;
                if (state == null || state == "Succeeded") return (true, null, get);
                if (state == "Failed" || state == "Canceled")
                {
                    var retry = await _arm.PutAsync(path, body(), ct);
                    return (false, retry.IsSuccess ? null : retry.Error, retry);
                }
                return (false, null, get);   // Creating / Updating
            }
            if (!get.IsNotFound) return (false, get.Error, get);

            var put = await _arm.PutAsync(path, body(), ct);
            if (!put.IsSuccess) return (false, put.Error, put);
            var putState = put.ProvisioningState;
            return (putState == null || putState == "Succeeded", null, put);
        }

        private static bool IsForbidden(string error) =>
            error != null && (error.StartsWith("AuthorizationFailed") || error.StartsWith("LinkedAuthorizationFailed") || error.StartsWith("403"));

        private string PermissionMessage(string what) =>
            $"The service principal (object id {_arm.CallerObjectId()}) is not allowed to {what}. Give it Contributor on the subscription " +
            "(portal: Subscriptions → your subscription → Access control (IAM) → Add role assignment), then click Resume.";

        private static string ReadMigration(string fileName)
        {
            var assembly = typeof(CodeProjectInfraSetupService).Assembly;
            var name = assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith(fileName, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"Embedded migration {fileName} is missing from the build.");
            using var stream = assembly.GetManifestResourceStream(name);
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        // ---------------------------------------------------------------- 1. database

        private async Task<Outcome> DatabaseAsync(CodeProjectSetupState s, CancellationToken ct)
        {
            if (!_o.ApplyDatabaseMigrations)
                return Outcome.Skip("CodeProjects:Setup:ApplyDatabaseMigrations is false; apply the three AddCodeProject*.sql migrations yourself.");
            foreach (var file in new[] { "AddCodeProjectQuestionSupport.sql", "AddCodeProjectVmProvisioning.sql", "AddCodeProjectInfraSetup.sql" })
                await _setup.ApplySqlScript(ReadMigration(file));
            return Outcome.Done($"Migrations applied to {_setup.DescribeDatabase()}.");
        }

        // ---------------------------------------------------------------- 2. preflight

        private async Task<Outcome> PreflightAsync(CodeProjectSetupState s, CancellationToken ct)
        {
            var sub = await _arm.GetAsync($"https://management.azure.com/subscriptions/{_arm.SubscriptionId}?api-version=2022-12-01", ct);
            if (!sub.IsSuccess)
                return sub.StatusCode == HttpStatusCode.Unauthorized
                    ? Outcome.Block("Could not sign in to Azure with AzureVmCredentials (tenant, client id, secret). Check the secret has not expired.")
                    : Outcome.Block("The service principal cannot read the subscription: " + sub.Error);
            if (sub.Body?["state"]?.ToString() != "Enabled")
                return Outcome.Block($"Subscription state is {sub.Body?["state"]}.");

            var oid = _arm.CallerObjectId();
            s.Data["callerObjectId"] = oid;
            s.Data["subscriptionName"] = sub.Body?["displayName"]?.ToString();

            try
            {
                // Proves the storage connection string works and can create private containers.
                await _blobStore.CreateAppendUrlAsync(BuildContainer, "preflight.txt", TimeSpan.FromMinutes(1), ct);
            }
            catch (Exception ex)
            {
                return Outcome.Block("Blob storage is not usable (CodeProjects:StorageConnectionString or SASURLSettings:ConnectionString): " + ex.Message);
            }

            return Outcome.Done($"Subscription {s.Data["subscriptionName"]} ({_arm.SubscriptionId}), caller {oid}, region {_o.Location}, database {_setup.DescribeDatabase()}, stacks {string.Join(", ", _o.Stacks.Select(x => x.Code))}.");
        }

        // ---------------------------------------------------------------- 3. providers

        private async Task<Outcome> ProvidersAsync(CodeProjectSetupState s, CancellationToken ct)
        {
            var pending = new List<string>();
            foreach (var ns in Providers)
            {
                var get = await _arm.GetAsync($"providers/{ns}?{ResourcesApi}", ct);
                var state = get.Body?["registrationState"]?.ToString();
                if (state == "Registered") continue;
                pending.Add(ns);
                if (state != "Registering")
                {
                    var reg = await _arm.PostAsync($"providers/{ns}/register?{ResourcesApi}", null, ct);
                    if (!reg.IsSuccess)
                        return IsForbidden(reg.Error) ? Outcome.Block(PermissionMessage($"register the {ns} resource provider")) : Outcome.Fail($"{ns}: {reg.Error}");
                }
            }
            return pending.Count == 0 ? Outcome.Done("All required providers are registered.") : Outcome.Wait("Registering " + string.Join(", ", pending));
        }

        // ---------------------------------------------------------------- 4. resource groups

        private async Task<Outcome> GroupsAsync(CodeProjectSetupState s, CancellationToken ct)
        {
            foreach (var rg in new[] { Rg, ImgRg })
            {
                var r = await _arm.PutAsync($"resourcegroups/{rg}?{ResourcesApi}", new { location = _o.Location, tags = new { purpose = "rb-code-projects" } }, ct);
                if (!r.IsSuccess)
                    return IsForbidden(r.Error) ? Outcome.Block(PermissionMessage("create resource groups")) : Outcome.Fail($"{rg}: {r.Error}");
            }
            return Outcome.Done($"{Rg} (network, candidate machines, runners) and {ImgRg} (gallery, registry, identities) in {_o.Location}.");
        }

        // ---------------------------------------------------------------- 5. identities

        private async Task<Outcome> IdentitiesAsync(CodeProjectSetupState s, CancellationToken ct)
        {
            foreach (var name in new[] { BuilderIdentity, RunnerIdentity })
            {
                var (ready, error, resource) = await EnsureAsync($"{IdentityPath(name)}?{IdentityApi}", () => new { location = _o.Location }, ct);
                if (error != null) return Outcome.Fail($"{name}: {error}");
                if (!ready) return Outcome.Wait($"Creating {name}…");
                var principal = resource.Body?["properties"]?["principalId"]?.ToString();
                if (string.IsNullOrEmpty(principal)) return Outcome.Wait($"Waiting for {name}'s principal…");
                s.Data[$"identity.{name}.id"] = resource.Body?["id"]?.ToString();
                s.Data[$"identity.{name}.principalId"] = principal;
            }
            return Outcome.Done($"{BuilderIdentity} (Image Builder) and {RunnerIdentity} (runner jobs pull images).");
        }

        // ---------------------------------------------------------------- 6. role assignments

        private async Task<Outcome> AccessAsync(CodeProjectSetupState s, CancellationToken ct)
        {
            var perms = await _arm.GetAsync($"resourceGroups/{Rg}/providers/Microsoft.Authorization/permissions?{AuthorizationApi}", ct);
            var entries = (perms.Body?["value"] as JArray) ?? new JArray();
            bool Has(string action) => entries.Any(p =>
            {
                var actions = (p["actions"] as JArray)?.Select(a => a.ToString()).ToList() ?? new List<string>();
                var notActions = (p["notActions"] as JArray)?.Select(a => a.ToString()).ToList() ?? new List<string>();
                var allowed = actions.Any(a => a == "*" || string.Equals(a, action, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(a, "Microsoft.Authorization/*", StringComparison.OrdinalIgnoreCase));
                var denied = notActions.Any(n => string.Equals(n, action, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(n, "Microsoft.Authorization/*/Write", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(n, "Microsoft.Authorization/*", StringComparison.OrdinalIgnoreCase));
                return allowed && !denied;
            });
            var canManage = entries.Any(p => (p["actions"] as JArray)?.Any(a => a.ToString() == "*") == true);
            var canAssign = Has("Microsoft.Authorization/roleAssignments/write");
            s.Data["canAssignRoles"] = canAssign.ToString();

            if (!canAssign)
                return Outcome.Block(
                    $"The service principal (object id {_arm.CallerObjectId()}) can create resources but not grant roles. Image Builder needs Contributor on {ImgRg} " +
                    $"and the runner jobs need AcrPull on the registry. Give the service principal 'User Access Administrator' (or Owner) on subscription " +
                    $"{s.Data.GetValueOrDefault("subscriptionName")} — portal: Subscriptions → Access control (IAM) → Add role assignment — then click Resume.");

            var assignments = new List<(string Scope, string Principal, string Role, string Label)>
            {
                ($"resourceGroups/{ImgRg}", Get(s, $"identity.{BuilderIdentity}.principalId"), AzureArmRestClient.RoleContributor, "Image Builder → Contributor on images group")
            };
            if (!canManage)
            {
                var me = _arm.CallerObjectId();
                assignments.Add(($"resourceGroups/{Rg}", me, AzureArmRestClient.RoleVirtualMachineContributor, "provisioner → Virtual Machine Contributor"));
                assignments.Add(($"resourceGroups/{Rg}", me, AzureArmRestClient.RoleNetworkContributor, "provisioner → Network Contributor"));
                assignments.Add(($"resourceGroups/{ImgRg}", me, AzureArmRestClient.RoleReader, "provisioner → Reader on gallery"));
            }

            foreach (var a in assignments)
            {
                var r = await _arm.AssignRoleAsync(a.Scope, a.Principal, a.Role, ct);
                if (r.IsSuccess) continue;
                if (r.ErrorCode == "PrincipalNotFound") return Outcome.Wait("Waiting for new identities to replicate in Microsoft Entra ID…");
                return IsForbidden(r.Error) ? Outcome.Block(PermissionMessage("assign roles")) : Outcome.Fail($"{a.Label}: {r.Error}");
            }
            return Outcome.Done(string.Join("; ", assignments.Select(a => a.Label)) + ".");
        }

        // ---------------------------------------------------------------- 7. network

        private object Rule(string name, int priority, string direction, string access, string protocol, string source, string destination, string ports) => new
        {
            name,
            properties = new
            {
                priority,
                direction,
                access,
                protocol,
                sourceAddressPrefix = source,
                sourcePortRange = "*",
                destinationAddressPrefix = destination,
                destinationPortRange = ports
            }
        };

        private object CandidateNsg()
        {
            var rules = new List<object>
            {
                Rule("Allow-RDP-From-Bastion", 100, "Inbound", "Allow", "Tcp", _o.BastionSubnetPrefix, "*", "3389"),
                Rule("Deny-All-Other-Inbound", 4000, "Inbound", "Deny", "*", "*", "*", "*"),
                // Project zip download and Run Command output blobs.
                Rule("Allow-Storage-Out", 100, "Outbound", "Allow", "Tcp", "*", "Storage", "443"),
                // Windows activation (azkms.core.windows.net / kms.core.windows.net).
                Rule("Allow-KMS-1", 110, "Outbound", "Allow", "Tcp", "*", "20.118.99.224", "1688"),
                Rule("Allow-KMS-2", 111, "Outbound", "Allow", "Tcp", "*", "40.83.235.53", "1688"),
                Rule("Allow-KMS-3", 112, "Outbound", "Allow", "Tcp", "*", "23.102.135.246", "1688"),
                Rule("Deny-Internet-Out", 4000, "Outbound", "Deny", "*", "*", "Internet", "*"),
                // Candidates cannot reach one another or the runners.
                Rule("Deny-VNet-Out", 4001, "Outbound", "Deny", "*", "*", "VirtualNetwork", "*")
            };
            var priority = 200;
            foreach (var prefix in _o.PackageMirrorPrefixes)
                rules.Add(Rule($"Allow-Mirror-{priority}", priority++, "Outbound", "Allow", "Tcp", "*", prefix, "*"));
            return new { location = _o.Location, properties = new { securityRules = rules } };
        }

        /// <summary>
        /// Outbound rules Azure Container Apps (workload profiles) requires, plus Storage for the run
        /// blobs. No Internet: code under test cannot send hidden tests anywhere.
        /// </summary>
        private object RunnerNsg() => new
        {
            location = _o.Location,
            properties = new
            {
                securityRules = new object[]
                {
                    Rule("Allow-LB-Probes", 100, "Inbound", "Allow", "Tcp", "AzureLoadBalancer", "*", "30000-32767"),
                    Rule("Allow-Subnet-In", 110, "Inbound", "Allow", "*", _o.RunnerSubnetPrefix, _o.RunnerSubnetPrefix, "*"),
                    Rule("Deny-All-Other-Inbound", 4000, "Inbound", "Deny", "*", "*", "*", "*"),
                    Rule("Allow-MCR", 100, "Outbound", "Allow", "Tcp", "*", "MicrosoftContainerRegistry", "443"),
                    Rule("Allow-AFD-FirstParty", 110, "Outbound", "Allow", "Tcp", "*", "AzureFrontDoor.FirstParty", "443"),
                    Rule("Allow-EntraID", 120, "Outbound", "Allow", "Tcp", "*", "AzureActiveDirectory", "443"),
                    Rule("Allow-ACR", 130, "Outbound", "Allow", "Tcp", "*", "AzureContainerRegistry", "443"),
                    Rule("Allow-Storage", 140, "Outbound", "Allow", "Tcp", "*", "Storage", "443"),
                    Rule("Allow-Subnet-Out", 150, "Outbound", "Allow", "*", _o.RunnerSubnetPrefix, _o.RunnerSubnetPrefix, "*"),
                    Rule("Allow-AzureDNS", 160, "Outbound", "Allow", "*", "*", "168.63.129.16", "53"),
                    Rule("Deny-Internet-Out", 4000, "Outbound", "Deny", "*", "*", "Internet", "*"),
                    Rule("Deny-VNet-Out", 4001, "Outbound", "Deny", "*", "*", "VirtualNetwork", "*")
                }
            }
        };

        private async Task<Outcome> NetworkAsync(CodeProjectSetupState s, CancellationToken ct)
        {
            var net = $"resourceGroups/{Rg}/providers/Microsoft.Network";
            var candidateNsg = $"{net}/networkSecurityGroups/{Prefix}-candidate-nsg";
            var runnerNsg = $"{net}/networkSecurityGroups/{Prefix}-runner-nsg";
            var natIp = $"{net}/publicIPAddresses/{Prefix}-nat-pip";
            var nat = $"{net}/natGateways/{Prefix}-nat";
            string Id(string path) => $"/subscriptions/{_arm.SubscriptionId}/{path}";

            var prerequisites = new (string Path, Func<object> Body, string Label)[]
            {
                (candidateNsg, CandidateNsg, "candidate NSG"),
                (runnerNsg, RunnerNsg, "runner NSG"),
                (natIp, () => new { location = _o.Location, sku = new { name = "Standard" }, properties = new { publicIPAllocationMethod = "Static" } }, "NAT public IP")
            };
            foreach (var (path, body, label) in prerequisites)
            {
                var (ready, error, _) = await EnsureAsync($"{path}?{NetworkApi}", body, ct);
                if (error != null) return Outcome.Fail($"{label}: {error}");
                if (!ready) return Outcome.Wait($"Creating {label}…");
            }

            var natResult = await EnsureAsync($"{nat}?{NetworkApi}", () => new
            {
                location = _o.Location,
                sku = new { name = "Standard" },
                properties = new { publicIpAddresses = new[] { new { id = Id(natIp) } } }
            }, ct);
            if (natResult.Error != null) return Outcome.Fail("NAT gateway: " + natResult.Error);
            if (!natResult.Ready) return Outcome.Wait("Creating NAT gateway…");

            // Dedicated VNet, never peered. Egress only through the NAT gateway, as each NSG allows.
            var vnet = await EnsureAsync($"{VnetPath}?{NetworkApi}", () => new
            {
                location = _o.Location,
                tags = new { purpose = "rb-code-projects" },
                properties = new
                {
                    addressSpace = new { addressPrefixes = new[] { _o.VnetPrefix } },
                    subnets = new object[]
                    {
                        new
                        {
                            name = "candidates",
                            properties = new
                            {
                                addressPrefix = _o.CandidateSubnetPrefix,
                                networkSecurityGroup = new { id = Id(candidateNsg) },
                                natGateway = new { id = Id(nat) },
                                defaultOutboundAccess = false
                            }
                        },
                        new
                        {
                            name = "runners",
                            properties = new
                            {
                                addressPrefix = _o.RunnerSubnetPrefix,
                                networkSecurityGroup = new { id = Id(runnerNsg) },
                                natGateway = new { id = Id(nat) },
                                defaultOutboundAccess = false,
                                delegations = new[] { new { name = "containerapps", properties = new { serviceName = "Microsoft.App/environments" } } }
                            }
                        },
                        new { name = "AzureBastionSubnet", properties = new { addressPrefix = _o.BastionSubnetPrefix } }
                    }
                }
            }, ct);
            if (vnet.Error != null) return Outcome.Fail("VNet: " + vnet.Error);
            if (!vnet.Ready) return Outcome.Wait("Creating VNet…");

            var peerings = vnet.Resource.Body?["properties"]?["virtualNetworkPeerings"] as JArray;
            if (peerings != null && peerings.Count > 0)
                Log(s, $"Warning: {Prefix}-vnet has {peerings.Count} peering(s). Candidate machines must not reach other networks — remove them.");

            return Outcome.Done($"{Prefix}-vnet {_o.VnetPrefix}: candidates {_o.CandidateSubnetPrefix}, runners {_o.RunnerSubnetPrefix}, Bastion {_o.BastionSubnetPrefix}. Egress: Storage" +
                (_o.PackageMirrorPrefixes.Count > 0 ? " + " + string.Join(", ", _o.PackageMirrorPrefixes) : "") + " only.");
        }

        // ---------------------------------------------------------------- 8. bastion

        private async Task<Outcome> BastionAsync(CodeProjectSetupState s, CancellationToken ct)
        {
            var net = $"resourceGroups/{Rg}/providers/Microsoft.Network";
            var ipPath = $"{net}/publicIPAddresses/{Prefix}-bastion-pip";
            var ip = await EnsureAsync($"{ipPath}?{NetworkApi}", () => new { location = _o.Location, sku = new { name = "Standard" }, properties = new { publicIPAllocationMethod = "Static" } }, ct);
            if (ip.Error != null) return Outcome.Fail("Bastion public IP: " + ip.Error);
            if (!ip.Ready) return Outcome.Wait("Creating Bastion public IP…");

            var bastion = await EnsureAsync($"{net}/bastionHosts/{BastionName}?{NetworkApi}", () => new
            {
                location = _o.Location,
                sku = new { name = "Standard" },   // shareable links need Standard or higher
                properties = new
                {
                    enableShareableLink = true,
                    ipConfigurations = new[]
                    {
                        new
                        {
                            name = "ipconfig1",
                            properties = new
                            {
                                subnet = new { id = $"/subscriptions/{_arm.SubscriptionId}/{VnetPath}/subnets/AzureBastionSubnet" },
                                publicIPAddress = new { id = $"/subscriptions/{_arm.SubscriptionId}/{ipPath}" }
                            }
                        }
                    }
                }
            }, ct);
            if (bastion.Error != null) return Outcome.Fail("Bastion: " + bastion.Error);
            if (!bastion.Ready) return Outcome.Wait("Creating Bastion (about 10 minutes)…");

            if (bastion.Resource.Body?["properties"]?["enableShareableLink"]?.Value<bool>() != true)
                return Outcome.Fail($"{BastionName} exists without shareable links. Enable 'Shareable Link' under its Configuration, then Resume.");
            return Outcome.Done($"{BastionName} (Standard, shareable links on).");
        }

        // ---------------------------------------------------------------- 9. gallery

        private async Task<Outcome> GalleryAsync(CodeProjectSetupState s, CancellationToken ct)
        {
            var gallery = await EnsureAsync($"{GalleryPath}?{GalleryApi}", () => new
            {
                location = _o.Location,
                properties = new { description = "Risebird code project candidate machine images" }
            }, ct);
            if (gallery.Error != null) return Outcome.Fail("Gallery: " + gallery.Error);
            if (!gallery.Ready) return Outcome.Wait("Creating gallery…");

            foreach (var stack in _o.Stacks)
            {
                var def = await EnsureAsync($"{GalleryPath}/images/{stack.Code}?{GalleryApi}", () => new
                {
                    location = _o.Location,
                    properties = new
                    {
                        osType = "Windows",
                        osState = "Generalized",
                        hyperVGeneration = "V2",
                        description = stack.DisplayName,
                        identifier = new { publisher = "Risebird", offer = "codeprojects", sku = stack.Code },
                        // Lets the provisioner create Trusted Launch VMs (CodeProjects:Azure:SecurityType) from it.
                        features = new[] { new { name = "SecurityType", value = "TrustedLaunchSupported" } }
                    }
                }, ct);
                if (def.Error != null) return Outcome.Fail($"Image definition {stack.Code}: {def.Error}");
                if (!def.Ready) return Outcome.Wait($"Creating image definition {stack.Code}…");
            }
            return Outcome.Done($"{GalleryName} with {string.Join(", ", _o.Stacks.Select(x => x.Code))}.");
        }

        // ---------------------------------------------------------------- 10. images

        private object ImageTemplate(CodeProjectStack stack, string identityId) => new JObject
        {
            ["location"] = _o.ImageBuilderLocation,
            ["tags"] = new JObject { ["purpose"] = "rb-code-projects", ["stack"] = stack.Code },
            ["identity"] = new JObject
            {
                ["type"] = "UserAssigned",
                ["userAssignedIdentities"] = new JObject { [identityId] = new JObject() }
            },
            ["properties"] = new JObject
            {
                ["buildTimeoutInMinutes"] = 240,
                ["source"] = new JObject
                {
                    ["type"] = "PlatformImage",
                    ["publisher"] = CodeProjectStackCatalog.WindowsSourcePublisher,
                    ["offer"] = CodeProjectStackCatalog.WindowsSourceOffer,
                    ["sku"] = CodeProjectStackCatalog.WindowsSourceSku,
                    ["version"] = "latest"
                },
                ["customize"] = new JArray
                {
                    new JObject
                    {
                        ["type"] = "PowerShell",
                        ["name"] = "install-toolchain",
                        ["inline"] = new JArray(CodeProjectStackCatalog.WindowsInstallLines(stack)),
                        ["runElevated"] = true,
                        ["runAsSystem"] = true
                    },
                    new JObject { ["type"] = "WindowsRestart", ["restartTimeout"] = "15m" },
                    new JObject
                    {
                        ["type"] = "PowerShell",
                        ["name"] = "seed-dependencies",
                        ["inline"] = new JArray(CodeProjectStackCatalog.WindowsSeedLines(stack)),
                        ["runElevated"] = true,
                        ["runAsSystem"] = true
                    }
                },
                ["distribute"] = new JArray
                {
                    new JObject
                    {
                        ["type"] = "SharedImage",
                        ["galleryImageId"] = ImageDefinitionId(stack),
                        ["runOutputName"] = stack.Code,
                        ["targetRegions"] = new JArray
                        {
                            new JObject { ["name"] = _o.Location, ["replicaCount"] = 1, ["storageAccountType"] = "Standard_LRS" }
                        }
                    }
                },
                ["vmProfile"] = new JObject { ["vmSize"] = _o.ImageBuildVmSize, ["osDiskSizeGB"] = 128 },
                ["errorHandling"] = new JObject { ["onCustomizerError"] = "abort", ["onValidationError"] = "cleanup" }
            }
        };

        private async Task<Outcome> ImagesAsync(CodeProjectSetupState s, CancellationToken ct)
        {
            var identityId = Get(s, $"identity.{BuilderIdentity}.id");
            var lines = new List<string>();
            var done = 0;
            var building = _o.Stacks.Count(st => Get(s, $"image.{st.Code}.state") == "building");

            foreach (var stack in _o.Stacks)
            {
                var name = CodeProjectStackCatalog.ImageTemplateName(stack);
                var path = $"resourceGroups/{ImgRg}/providers/Microsoft.VirtualMachineImages/imageTemplates/{name}";
                var template = await EnsureAsync($"{path}?{ImageBuilderApi}", () => ImageTemplate(stack, identityId), ct);
                if (template.Error != null)
                {
                    var provisioningError = template.Resource?.Body?["properties"]?["provisioningError"]?["message"]?.ToString();
                    return Outcome.Fail($"{stack.Code} template: {provisioningError ?? template.Error}");
                }
                if (!template.Ready) { lines.Add($"{stack.Code}: validating template"); continue; }

                var last = template.Resource.Body?["properties"]?["lastRunStatus"];
                var runState = last?["runState"]?.ToString();
                var subState = last?["runSubState"]?.ToString();
                switch (runState)
                {
                    case "Succeeded":
                    case "PartiallySucceeded":
                        s.Data[$"image.{stack.Code}.state"] = "done";
                        done++;
                        lines.Add($"{stack.Code}: built");
                        break;
                    case "Running":
                        s.Data[$"image.{stack.Code}.state"] = "building";
                        lines.Add($"{stack.Code}: building ({subState ?? "starting"})");
                        break;
                    case "Failed":
                    case "Canceled":
                        var retries = int.TryParse(Get(s, $"image.{stack.Code}.retries"), out var r) ? r : 0;
                        if (retries >= 1)
                            return Outcome.Fail($"{stack.Code} image build {runState.ToLowerInvariant()}: {last?["message"]}. " +
                                $"The customization log is in the IT_{ImgRg}_{name}* resource group's storage account (packerlogs).");
                        s.Data[$"image.{stack.Code}.retries"] = (retries + 1).ToString();
                        await StartImageBuildAsync(s, stack, path, ct);
                        lines.Add($"{stack.Code}: retrying build");
                        break;
                    default:
                        // Never run. Starting builds a few at a time keeps within regional vCPU quota.
                        var requested = Get(s, $"image.{stack.Code}.state") == "building";
                        if (!requested && building < _o.MaxParallelImageBuilds)
                        {
                            await StartImageBuildAsync(s, stack, path, ct);
                            building++;
                            lines.Add($"{stack.Code}: build started");
                        }
                        else lines.Add($"{stack.Code}: {(requested ? "starting" : "queued")}");
                        break;
                }
            }

            var summary = string.Join(" · ", lines);
            return done == _o.Stacks.Count ? Outcome.Done(summary) : Outcome.Wait(summary + " (each build takes 60–120 minutes)");
        }

        private async Task StartImageBuildAsync(CodeProjectSetupState s, CodeProjectStack stack, string templatePath, CancellationToken ct)
        {
            var run = await _arm.PostAsync($"{templatePath}/run?{ImageBuilderApi}", null, ct);
            if (!run.IsSuccess && run.ErrorCode != "Conflict")
                throw new InvalidOperationException($"{stack.Code}: could not start the image build: {run.Error}");
            s.Data[$"image.{stack.Code}.state"] = "building";
        }

        // ---------------------------------------------------------------- 11. registry

        private async Task<Outcome> RegistryAsync(CodeProjectSetupState s, CancellationToken ct)
        {
            var acr = await EnsureAsync($"{RegistryPath}?{RegistryApi}", () => new
            {
                location = _o.Location,
                sku = new { name = "Basic" },
                properties = new { adminUserEnabled = false }
            }, ct);
            if (acr.Error != null) return Outcome.Fail($"{RegistryName}: {acr.Error}");
            if (!acr.Ready) return Outcome.Wait($"Creating registry {RegistryName}…");
            s.Data["registry.loginServer"] = acr.Resource.Body?["properties"]?["loginServer"]?.ToString();

            var pull = await _arm.AssignRoleAsync(RegistryPath, Get(s, $"identity.{RunnerIdentity}.principalId"), AzureArmRestClient.RoleAcrPull, ct);
            if (!pull.IsSuccess)
            {
                if (pull.ErrorCode == "PrincipalNotFound") return Outcome.Wait("Waiting for the runner identity to replicate…");
                return IsForbidden(pull.Error) ? Outcome.Block(PermissionMessage("assign AcrPull on the registry")) : Outcome.Fail("AcrPull: " + pull.Error);
            }
            return Outcome.Done($"{s.Data["registry.loginServer"]} (Basic); {RunnerIdentity} has AcrPull.");
        }

        // ---------------------------------------------------------------- 12. runner images

        private async Task<Outcome> RunnerImagesAsync(CodeProjectSetupState s, CancellationToken ct)
        {
            var login = Get(s, "registry.loginServer");
            var lines = new List<string>();
            var done = 0;
            foreach (var stack in _o.Stacks)
            {
                var tag = CodeProjectStackCatalog.RunnerImageTag(stack);
                var image = $"{login}/{stack.RunnerRepository}:{tag}";
                if (Get(s, $"runnerImage.{stack.Code}") == image) { done++; lines.Add($"{stack.Code}: built"); continue; }

                var runId = Get(s, $"acrRun.{stack.Code}.{tag}");
                if (runId == null)
                {
                    var op = Get(s, $"acrOp.{stack.Code}.{tag}");
                    if (op != null)
                    {
                        var opResult = await _arm.GetAsync(op, ct);
                        runId = opResult.Body?["properties"]?["runId"]?.ToString();
                        if (runId == null) { lines.Add($"{stack.Code}: queuing"); continue; }
                        s.Data[$"acrRun.{stack.Code}.{tag}"] = runId;
                    }
                    else
                    {
                        runId = await ScheduleRunnerBuildAsync(s, stack, tag, ct);
                        lines.Add($"{stack.Code}: build scheduled");
                        continue;
                    }
                }

                var run = await _arm.GetAsync($"{RegistryPath}/runs/{runId}?{RegistryTasksApi}", ct);
                var status = run.Body?["properties"]?["status"]?.ToString();
                switch (status)
                {
                    case "Succeeded":
                        s.Data[$"runnerImage.{stack.Code}"] = image;
                        done++;
                        lines.Add($"{stack.Code}: built");
                        break;
                    case "Failed":
                    case "Error":
                    case "Timeout":
                    case "Canceled":
                        var key = $"acrRetries.{stack.Code}.{tag}";
                        var retries = int.TryParse(Get(s, key), out var r) ? r : 0;
                        if (retries >= 1)
                            return Outcome.Fail($"{stack.Code} runner image build {status.ToLowerInvariant()} (ACR run {runId}). " +
                                $"See the log in the portal: {RegistryName} → Services → Tasks → Runs → {runId}.");
                        s.Data[key] = (retries + 1).ToString();
                        s.Data.Remove($"acrRun.{stack.Code}.{tag}");
                        s.Data.Remove($"acrOp.{stack.Code}.{tag}");
                        lines.Add($"{stack.Code}: retrying");
                        break;
                    default:
                        lines.Add($"{stack.Code}: {status?.ToLowerInvariant() ?? "queued"}");
                        break;
                }
            }
            var summary = string.Join(" · ", lines);
            return done == _o.Stacks.Count ? Outcome.Done(summary) : Outcome.Wait(summary);
        }

        private async Task<string> ScheduleRunnerBuildAsync(CodeProjectSetupState s, CodeProjectStack stack, string tag, CancellationToken ct)
        {
            // ACR Tasks accepts a remote tarball as the build context; ours sits in private storage behind a short SAS.
            var blob = $"{RegistryName}/{stack.Code}-{tag}.tar.gz";
            await _blobStore.UploadAsync(BuildContainer, blob, CodeProjectStackCatalog.BuildRunnerContext(stack), "application/gzip", ct);
            var source = _blobStore.CreateReadUrl(BuildContainer, blob, TimeSpan.FromHours(4));

            var run = await _arm.PostAsync($"{RegistryPath}/scheduleRun?{RegistryTasksApi}", new
            {
                type = "DockerBuildRequest",
                imageNames = new[] { $"{stack.RunnerRepository}:{tag}" },
                isPushEnabled = true,
                noCache = false,
                dockerFilePath = "Dockerfile",
                sourceLocation = source,
                platform = new { os = "Linux", architecture = "amd64" },
                timeout = 3600
            }, ct);
            if (!run.IsSuccess)
            {
                if (run.ErrorCode == "TasksOperationsNotAllowed")
                    throw new InvalidOperationException("ACR Tasks are not available on this subscription (common on free-trial subscriptions). Use a pay-as-you-go subscription or build the runner images another way.");
                throw new InvalidOperationException($"{stack.Code}: could not schedule the runner image build: {run.Error}");
            }

            var runId = run.Body?["properties"]?["runId"]?.ToString();
            if (runId != null) s.Data[$"acrRun.{stack.Code}.{tag}"] = runId;
            else if (!string.IsNullOrEmpty(run.Location ?? run.AsyncOperation)) s.Data[$"acrOp.{stack.Code}.{tag}"] = run.Location ?? run.AsyncOperation;
            return runId;
        }

        // ---------------------------------------------------------------- 13. container apps environment

        private async Task<Outcome> RunnerEnvironmentAsync(CodeProjectSetupState s, CancellationToken ct)
        {
            var env = await EnsureAsync($"{EnvironmentPath}?{ContainerAppsApi}", () => new
            {
                location = _o.Location,
                tags = new { purpose = "rb-code-projects" },
                properties = new
                {
                    // Internal: jobs have no ingress; all traffic stays inside the runner subnet's NSG.
                    vnetConfiguration = new { infrastructureSubnetId = RunnerSubnetId, @internal = true },
                    workloadProfiles = new[] { new { name = "Consumption", workloadProfileType = "Consumption" } }
                }
            }, ct);
            if (env.Error != null) return Outcome.Fail("Container Apps environment: " + env.Error);
            if (!env.Ready) return Outcome.Wait("Creating the Container Apps environment (5–10 minutes)…");
            s.Data["runner.environmentId"] = env.Resource.Body?["id"]?.ToString();
            return Outcome.Done($"{Prefix}-runners (internal, runner subnet).");
        }

        // ---------------------------------------------------------------- 14. runner jobs

        private async Task<Outcome> RunnerJobsAsync(CodeProjectSetupState s, CancellationToken ct)
        {
            var identityId = Get(s, $"identity.{RunnerIdentity}.id");
            var login = Get(s, "registry.loginServer");
            var pending = new List<string>();
            foreach (var stack in _o.Stacks)
            {
                var image = Get(s, $"runnerImage.{stack.Code}");
                var path = $"{JobPath(stack)}?{ContainerAppsApi}";
                object Body() => new JObject
                {
                    ["location"] = _o.Location,
                    ["identity"] = new JObject { ["type"] = "UserAssigned", ["userAssignedIdentities"] = new JObject { [identityId] = new JObject() } },
                    ["properties"] = new JObject
                    {
                        ["environmentId"] = Get(s, "runner.environmentId"),
                        ["workloadProfileName"] = "Consumption",
                        ["configuration"] = new JObject
                        {
                            ["triggerType"] = "Manual",
                            ["replicaTimeout"] = 300,
                            ["replicaRetryLimit"] = 0,
                            ["manualTriggerConfig"] = new JObject { ["parallelism"] = 1, ["replicaCompletionCount"] = 1 },
                            ["registries"] = new JArray { new JObject { ["server"] = login, ["identity"] = identityId } }
                        },
                        ["template"] = new JObject
                        {
                            ["containers"] = new JArray
                            {
                                new JObject
                                {
                                    ["name"] = "runner",
                                    ["image"] = image,
                                    ["resources"] = new JObject { ["cpu"] = 1.0, ["memory"] = "2Gi" }
                                }
                            }
                        }
                    }
                };

                var (ready, error, resource) = await EnsureAsync(path, Body, ct);
                if (error != null) return Outcome.Fail($"{stack.JobName}: {error}");
                if (!ready) { pending.Add(stack.JobName); continue; }

                // A new runner image (changed seed or script) is rolled onto the existing job.
                var current = resource.Body?["properties"]?["template"]?["containers"]?[0]?["image"]?.ToString();
                if (!string.Equals(current, image, StringComparison.OrdinalIgnoreCase))
                {
                    var update = await _arm.PutAsync(path, Body(), ct);
                    if (!update.IsSuccess) return Outcome.Fail($"{stack.JobName}: {update.Error}");
                    pending.Add(stack.JobName);
                }
            }
            return pending.Count == 0
                ? Outcome.Done(string.Join(", ", _o.Stacks.Select(x => x.JobName)))
                : Outcome.Wait("Creating " + string.Join(", ", pending) + "…");
        }

        // ---------------------------------------------------------------- 15. StackImages

        private async Task<Outcome> StackImagesAsync(CodeProjectSetupState s, CancellationToken ct)
        {
            foreach (var stack in _o.Stacks)
                await _setup.UpsertStackImage(stack.Code, stack.DisplayName, stack.Toolchain, ImageDefinitionId(stack), "Windows");
            return Outcome.Done($"{_o.Stacks.Count} stack image(s) registered and active.");
        }

        // ---------------------------------------------------------------- 16. settings

        private async Task<Outcome> SettingsAsync(CodeProjectSetupState s, CancellationToken ct)
        {
            var values = new Dictionary<string, string>
            {
                [CodeProjectSettingNames.AzureResourceGroup] = Rg,
                [CodeProjectSettingNames.AzureLocation] = _o.Location,
                [CodeProjectSettingNames.AzureSubnetId] = CandidateSubnetId,
                [CodeProjectSettingNames.AzureBastionName] = BastionName,
                [CodeProjectSettingNames.AzureBastionResourceGroup] = Rg,
                [CodeProjectSettingNames.RunnerResourceGroup] = Rg
            };
            foreach (var stack in _o.Stacks)
            {
                values[CodeProjectSettingNames.RunnerJobPrefix + stack.Code] = stack.JobName;
                values[CodeProjectSettingNames.RunnerImagePrefix + stack.Code] = Get(s, $"runnerImage.{stack.Code}");
            }
            foreach (var pair in values)
                await _settingsRepository.Set(pair.Key, pair.Value);
            _settings.Invalidate();
            return Outcome.Done($"{values.Count} settings stored in CodeProjectSettings (configuration still overrides them).");
        }

        // ---------------------------------------------------------------- 17. functions app

        /// <summary>
        /// Finds the deployed functions app that hosts these functions, so the deployed web app can reach
        /// it. Until one is deployed the web app falls back to the local functions host in Development.
        /// </summary>
        private async Task<Outcome> FunctionsAppAsync(CodeProjectSetupState s, CancellationToken ct)
        {
            var sites = new List<JToken>();
            string next = $"providers/Microsoft.Web/sites?{WebApi}";
            for (var page = 0; next != null && page < 10; page++)
            {
                var list = await _arm.GetAsync(next, ct);
                if (!list.IsSuccess) return Outcome.Skip("Could not list App Service sites: " + list.Error);
                sites.AddRange((list.Body?["value"] as JArray) ?? new JArray());
                next = list.Body?["nextLink"]?.ToString();
            }

            foreach (var site in sites.Where(x => (x["kind"]?.ToString() ?? "").Contains("functionapp", StringComparison.OrdinalIgnoreCase)))
            {
                var id = site["id"]?.ToString();
                var functions = await _arm.GetAsync($"https://management.azure.com{id}/functions?{WebApi}", ct);
                var hasProvisioner = (functions.Body?["value"] as JArray)?
                    .Any(f => (f["name"]?.ToString() ?? "").EndsWith("/CodeProjectProvision", StringComparison.OrdinalIgnoreCase)) == true;
                if (!hasProvisioner) continue;

                var host = site["properties"]?["defaultHostName"]?.ToString();
                var key = await _arm.PutAsync($"https://management.azure.com{id}/host/default/functionKeys/codeprojects-web?{WebApi}",
                    new { properties = new { name = "codeprojects-web" } }, ct);
                var value = key.Body?["properties"]?["value"]?.ToString();
                if (string.IsNullOrEmpty(value))
                {
                    var keys = await _arm.PostAsync($"https://management.azure.com{id}/host/default/listkeys?{WebApi}", null, ct);
                    value = keys.Body?["functionKeys"]?["codeprojects-web"]?.ToString() ?? keys.Body?["functionKeys"]?["default"]?.ToString();
                }
                if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(value))
                    return Outcome.Skip($"Found {site["name"]} but could not read its host name or a function key.");

                await _settingsRepository.Set(CodeProjectSettingNames.FunctionsBaseUrl, $"https://{host}/api");
                await _settingsRepository.Set(CodeProjectSettingNames.FunctionsKey, await _encryption.EncryptData(value), isSecret: true);
                _settings.Invalidate();
                return Outcome.Done($"Web app will call https://{host}/api (key 'codeprojects-web').");
            }

            return Outcome.Skip("No deployed functions app has the code project functions yet. Locally the web app uses " +
                $"{CodeProjectSettingNames.LocalFunctionsBaseUrl}. After deploying Risebird.AzureTimerFunctions, run setup again to wire the deployed apps.");
        }

        // ---------------------------------------------------------------- 18. smoke test

        /// <summary>
        /// One tiny run per stack through the real path: blob in, job execution in the VNet, report out.
        /// One stack per tick, because each run takes up to a few minutes on a cold start.
        /// </summary>
        private async Task<Outcome> SmokeTestAsync(CodeProjectSetupState s, CancellationToken ct)
        {
            var remaining = _o.Stacks.Where(x => Get(s, $"smoke.{x.Code}") != CodeProjectStackCatalog.RunnerImageTag(x)).ToList();
            if (remaining.Count == 0) return Outcome.Done("Every runner job built and reported a test.");

            var stack = remaining[0];
            var result = await _runner.RunAsync(new CodeProjectRunRequest
            {
                StackImageCode = stack.Code,
                Files = new List<CodeProjectFile> { new CodeProjectFile { Path = "README.md", Content = "smoke" } },
                BuildCommand = "true",
                TestCommand = "mkdir -p test-results && printf '%s' '<testsuite name=\"smoke\"><testcase classname=\"Smoke\" name=\"rb_smoke\"/></testsuite>' > test-results/junit.xml",
                TimeoutSeconds = 60
            }, ct);

            var passed = result.Completed && result.BuildExitCode == 0 && result.Tests.Any(t => t.Name.EndsWith("rb_smoke") && t.Passed);
            if (!passed)
            {
                var attempts = int.TryParse(Get(s, $"smokeAttempts.{stack.Code}"), out var a) ? a + 1 : 1;
                s.Data[$"smokeAttempts.{stack.Code}"] = attempts.ToString();
                var why = result.Error ?? $"build exit {result.BuildExitCode}, {result.Tests.Count} test(s) reported";
                // The first execution on a fresh environment often times out while the image is pulled.
                return attempts >= 3 ? Outcome.Fail($"{stack.JobName}: {why}") : Outcome.Wait($"{stack.JobName}: {why} — retrying");
            }

            s.Data[$"smoke.{stack.Code}"] = CodeProjectStackCatalog.RunnerImageTag(stack);
            return remaining.Count == 1
                ? Outcome.Done("Every runner job built and reported a test.")
                : Outcome.Wait($"{stack.JobName} passed; {remaining.Count - 1} to go.");
        }
    }
}
