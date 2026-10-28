using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class CodeProjectQuestionService : ICodeProjectQuestionService
    {
        private static readonly JsonSerializerOptions Json = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        /// <summary>Grace for the final commit and push after the clock runs out.</summary>
        private static readonly TimeSpan SubmitGrace = TimeSpan.FromMinutes(2);

        private readonly ICodeProjectQuestionRepository _repository;
        private readonly ICodeProjectBlobStore _blobStore;
        private readonly ICodeProjectRunner _runner;
        private readonly ICodeProjectEnvironmentProvisioner _provisioner;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public CodeProjectQuestionService(ICodeProjectQuestionRepository repository, ICodeProjectBlobStore blobStore,
            ICodeProjectRunner runner, ICodeProjectEnvironmentProvisioner provisioner, IApplicationErrorLogger applicationErrorLogger,
            IOpenAIResponsesHelper openAI = null)
        {
            _repository = repository;
            _blobStore = blobStore;
            _runner = runner;
            _provisioner = provisioner;
            _applicationErrorLogger = applicationErrorLogger;
            _openAI = openAI;
        }

        /// <summary>Repairs projects that fail validation; without it a failed validation is final.</summary>
        private readonly IOpenAIResponsesHelper _openAI;

        private static string ProjectBlobName(int questionId) => $"q{questionId}/project.zip";
        private static string SecretsBlobName(int questionId) => $"q{questionId}/secrets.json";

        /// <summary>Tier 3, stored apart from the candidate zip.</summary>
        private sealed class ProjectSecrets
        {
            public List<CodeProjectFile> HiddenTests { get; set; } = new List<CodeProjectFile>();
            public List<CodeProjectFile> ReferenceSolution { get; set; } = new List<CodeProjectFile>();
            public List<CodeProjectSeededDefect> SeededDefects { get; set; } = new List<CodeProjectSeededDefect>();
        }

        public Task<List<StackImage>> GetActiveStackImages() => _repository.GetActiveStackImages();

        public Task<List<int>> GetUnfinishedValidations() => _repository.GetUnfinishedValidations();

        public Task<CodeProjectDetails> GetDetails(int questionId) => _repository.GetCodeProjectDetails(questionId);

        public Task<List<CodeProjectStatusRow>> GetStatusesByIRF(int irfId) => _repository.GetStatusesByIRF(irfId);

        public async Task<(int QuestionId, string Error)> CreateQuestionAsync(int irfId, string skillName, CodeProjectGeneration g,
            int timeLimitMinutes, string createdBy)
        {
            var questionId = await _repository.AddCodeProjectQuestion(irfId, skillName, g, timeLimitMinutes,
                CodeProjectQuestionHelper.BuildSanitizedPayload(g), createdBy);
            if (questionId <= 0)
                return (0, "Could not save the generated question.");

            try
            {
                var projectUri = await _blobStore.UploadAsync(_blobStore.ProjectContainer, ProjectBlobName(questionId),
                    Zip(g.ProjectFiles), "application/zip");
                var secrets = new ProjectSecrets { HiddenTests = g.HiddenTests, ReferenceSolution = g.ReferenceSolution, SeededDefects = g.SeededDefects };
                var secretsUri = await _blobStore.UploadAsync(_blobStore.SecretsContainer, SecretsBlobName(questionId),
                    JsonSerializer.SerializeToUtf8Bytes(secrets, Json), "application/json");

                await _repository.UpdateValidation(questionId, CodeProjectValidationStatus.Validating,
                    JsonSerializer.Serialize(CodeProjectQuestionHelper.InitialReport(), Json), projectUri, secretsUri);
                return (questionId, null);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                await SaveFailure(questionId, CodeProjectQuestionHelper.InitialReport(runningKey: null), "Could not store the generated project.");
                return (questionId, "Could not store the generated project.");
            }
        }

        public async Task ValidateAsync(int questionId, CancellationToken ct = default)
        {
            var details = await _repository.GetCodeProjectDetails(questionId);
            if (details == null
                || (details.ValidationStatus != CodeProjectValidationStatus.Validating && details.ValidationStatus != CodeProjectValidationStatus.Pending))
                return;

            var progress = CodeProjectQuestionHelper.InitialReport();
            try
            {
                if (!_runner.IsConfigured)
                {
                    await SaveFailure(questionId, progress, "The container runner is not configured, so the project cannot be proved to build. It will not be published.");
                    return;
                }

                var (starter, secrets) = await LoadProjectAsync(questionId, ct);
                var g = ToGeneration(details, starter, secrets);
                g.EditableFiles = EditableFilesFromPayload(details.GeneratedPayload);
                var timeLimit = details.CodingTimeLimitInMinutes ?? CodeProjectQuestionHelper.DefaultTimeLimitMinutes;

                // Questions generated before a fix existed (or re-validated later) get it too; saved when anything changed.
                var before = Fingerprint(g);
                CodeProjectQuestionHelper.ApplyAutomaticFixes(g, CodeProjectStackCatalog.ApplyStackFixups);
                if (Fingerprint(g) != before) await StoreProjectAsync(questionId, g, ct);

                // Build and test; when the project itself is at fault (compile errors, unsolvable or
                // mismatched tests, a pre-solved starter), the AI gets the real output and repairs the
                // files, up to MaxRepairRounds times, before the question is reported as failed.
                CodeProjectValidationReport report;
                var repairs = new List<string>();
                for (var round = 0; ; round++)
                {
                    var starterRun = await _runner.RunAsync(RunRequest(details, CodeProjectQuestionHelper.BuildReferenceWorkspace(g.ProjectFiles, null, g.HiddenTests)), ct);
                    if (starterRun.Completed && starterRun.BuildExitCode == 0)
                    {
                        // Surface progress while the second run is under way.
                        SetStep(progress, "build", "ok", $"{details.BuildCommand} → exit 0", starterRun.WallSeconds);
                        SetStep(progress, "reference", "run", null);
                        await _repository.UpdateValidation(questionId, CodeProjectValidationStatus.Validating, JsonSerializer.Serialize(progress, Json));
                    }

                    CodeProjectRunResult referenceRun = null;
                    if (starterRun.Completed && starterRun.BuildExitCode == 0)
                        referenceRun = await _runner.RunAsync(RunRequest(details,
                            CodeProjectQuestionHelper.BuildReferenceWorkspace(g.ProjectFiles, g.ReferenceSolution, g.HiddenTests)), ct);

                    report = CodeProjectQuestionHelper.EvaluateValidation(g, timeLimit, starterRun, referenceRun);
                    if (report.FailureReason == null) break;

                    if (round >= CodeProjectQuestionHelper.MaxRepairRounds || _openAI == null
                        || !CodeProjectQuestionHelper.IsRepairable(report, starterRun, referenceRun))
                    {
                        if (repairs.Count > 0) report.FailureReason += $" (after {repairs.Count} automatic repair{(repairs.Count == 1 ? "" : "s")})";
                        await _repository.UpdateValidation(questionId, CodeProjectValidationStatus.Failed, JsonSerializer.Serialize(report, Json));
                        return;
                    }

                    // Show the repair on the failing step while the AI works on it.
                    var failedKey = report.Steps.First(s => s.State == "failed").Key;
                    var shown = CodeProjectQuestionHelper.InitialReport(runningKey: null);
                    foreach (var s in report.Steps.TakeWhile(s => s.State == "ok")) SetStep(shown, s.Key, s.State, s.Detail, s.Seconds);
                    SetStep(shown, failedKey, "run", $"Automatic repair {round + 1} of {CodeProjectQuestionHelper.MaxRepairRounds}: {report.FailureReason}");
                    await _repository.UpdateValidation(questionId, CodeProjectValidationStatus.Validating, JsonSerializer.Serialize(shown, Json));

                    var raw = await _openAI.CreateJsonWithInstructionsAsync(CodeProjectQuestionHelper.RepairInstructions,
                        CodeProjectQuestionHelper.BuildRepairInput(g, CodeProjectQuestionHelper.ResolveEditableFiles(g), report, starterRun, referenceRun),
                        "RepairCodeProjectQuestion");
                    var repaired = CodeProjectQuestionHelper.ApplyRepair(g, raw, timeLimit, out var repairError, out var notes, CodeProjectStackCatalog.ApplyStackFixups);
                    // A "repair" that returns the same files would only fail the same way again.
                    if (repaired != null && Fingerprint(repaired) == Fingerprint(g))
                    {
                        repaired = null;
                        repairError = "it returned the files unchanged.";
                    }
                    if (repaired == null)
                    {
                        report.FailureReason += $" Automatic repair {round + 1} did not help: {repairError}";
                        await _repository.UpdateValidation(questionId, CodeProjectValidationStatus.Failed, JsonSerializer.Serialize(report, Json));
                        return;
                    }

                    // The repaired files replace the stored ones; question, requirements and commands are unchanged.
                    g = repaired;
                    await StoreProjectAsync(questionId, g, ct);
                    repairs.Add(string.IsNullOrWhiteSpace(notes) ? report.FailureReason : notes.Trim());
                    progress = CodeProjectQuestionHelper.InitialReport();
                    SetStep(progress, "build", "run", $"Re-validating after automatic repair {round + 1}…");
                    await _repository.UpdateValidation(questionId, CodeProjectValidationStatus.Validating, JsonSerializer.Serialize(progress, Json));
                }

                report.Steps.Add(CodeProjectQuestionHelper.Step("publish", "Publish",
                    "ok", $"project.zip → blob · question {questionId}" +
                    (repairs.Count > 0 ? $" · passed after {repairs.Count} automatic repair{(repairs.Count == 1 ? "" : "s")}: {string.Join(" / ", repairs)}" : "")));
                // The CHECK constraint refuses 'passed' without a ProjectBlobUri, so a question that
                // lost its zip cannot be published by accident.
                var published = await _repository.UpdateValidation(questionId, CodeProjectValidationStatus.Passed, JsonSerializer.Serialize(report, Json));
                if (!published)
                    await SaveFailure(questionId, report, "Could not publish the validated project.");
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw; // left 'validating'; the worker resumes it on next start
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                await SaveFailure(questionId, progress, "Validation crashed: " + ex.Message);
            }
        }

        public async Task PrepareEnvironmentAsync(string sessionId, int questionId, CancellationToken ct = default)
        {
            var details = await _repository.GetCodeProjectDetails(questionId);
            if (details == null || details.ValidationStatus != CodeProjectValidationStatus.Passed) return;
            if (!await _repository.ClaimEnvironment(sessionId, questionId, details.StackImageCode)) return;

            if (!_provisioner.IsConfigured)
            {
                await _repository.UpdateEnvironment(sessionId, questionId, CodeProjectEnvironmentState.Failed,
                    message: "Candidate environments are not configured yet.");
                return;
            }

            try
            {
                var image = (await _repository.GetActiveStackImages())
                    .FirstOrDefault(i => string.Equals(i.Code, details.StackImageCode, StringComparison.OrdinalIgnoreCase));
                if (image == null)
                {
                    await _repository.UpdateEnvironment(sessionId, questionId, CodeProjectEnvironmentState.Failed,
                        message: $"Stack image {details.StackImageCode} is no longer active.");
                    return;
                }

                var result = await _provisioner.ProvisionAsync(new CodeProjectProvisionRequest
                {
                    SessionId = sessionId,
                    QuestionId = questionId,
                    StackImageCode = image.Code,
                    GalleryImageId = image.GalleryImageId,
                    OsType = image.OsType,
                    // Candidate zip only. The secrets blob is never linked to the machine.
                    ProjectZipUrl = _blobStore.CreateReadUrl(_blobStore.ProjectContainer, ProjectBlobName(questionId), TimeSpan.FromHours(3)),
                    ReadOnlyPaths = ParseList(details.ReadOnlyPathsJson)
                }, ct);

                await _repository.UpdateEnvironment(sessionId, questionId,
                    result.Accepted ? CodeProjectEnvironmentState.Provisioning : CodeProjectEnvironmentState.Failed,
                    environmentHandle: result.Handle,
                    message: result.Accepted ? "Allocating machine…" : result.Error);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                await _repository.UpdateEnvironment(sessionId, questionId, CodeProjectEnvironmentState.Failed, message: "Could not start the environment.");
            }
        }

        public async Task PrepareNextEnvironmentAsync(string sessionId, int interviewId, int irfId, int currentQuestionId, CancellationToken ct = default)
        {
            var next = await _repository.PeekNextCodeProjectQuestion(interviewId, irfId, currentQuestionId);
            if (next.HasValue)
                await PrepareEnvironmentAsync(sessionId, next.Value.QuestionId, ct);
        }

        public async Task<CodeProjectEnvironment> RefreshEnvironmentAsync(string sessionId, int questionId, CancellationToken ct = default)
        {
            var env = await _repository.GetEnvironment(sessionId, questionId);
            if (env == null)
            {
                // Look-ahead did not run (first question, or a resumed session): start now.
                await PrepareEnvironmentAsync(sessionId, questionId, ct);
                env = await _repository.GetEnvironment(sessionId, questionId);
            }
            if (env == null || env.State != CodeProjectEnvironmentState.Provisioning || string.IsNullOrEmpty(env.EnvironmentHandle))
                return env;

            var status = await _provisioner.GetStatusAsync(env.EnvironmentHandle, ct);
            var state = status?.State == CodeProjectEnvironmentState.Ready ? CodeProjectEnvironmentState.Ready
                : status?.State == CodeProjectEnvironmentState.Failed ? CodeProjectEnvironmentState.Failed
                : CodeProjectEnvironmentState.Provisioning;
            await _repository.UpdateEnvironment(sessionId, questionId, state, connectUrl: status?.ConnectUrl,
                vmPropertyId: status?.VMPropertyId, message: status?.Message);
            return await _repository.GetEnvironment(sessionId, questionId);
        }

        public async Task<CodeProjectEnvironmentStatus> GetConnectionAsync(CodeProjectEnvironment env, CancellationToken ct = default)
        {
            if (env?.State != CodeProjectEnvironmentState.HandedOver || string.IsNullOrEmpty(env.EnvironmentHandle)) return null;
            var status = await _provisioner.GetStatusAsync(env.EnvironmentHandle, ct);
            return status?.State == CodeProjectEnvironmentState.Ready ? status : null;
        }

        public async Task<CodeProjectEnvironment> HandOverAsync(string sessionId, int questionId, CancellationToken ct = default)
        {
            var env = await RefreshEnvironmentAsync(sessionId, questionId, ct);
            if (env?.State == CodeProjectEnvironmentState.Ready)
            {
                await _repository.UpdateEnvironment(sessionId, questionId, CodeProjectEnvironmentState.HandedOver, message: "Connected");
                env = await _repository.GetEnvironment(sessionId, questionId);
            }
            if (env?.State == CodeProjectEnvironmentState.HandedOver && env.HandedOverAtUtc.HasValue && !string.IsNullOrEmpty(env.EnvironmentHandle))
            {
                // The provisioner locks the machine at this time even if the browser never submits.
                // Only the first deadline sticks, so a relaunch cannot extend the slot.
                var details = await _repository.GetCodeProjectDetails(questionId);
                var timeLimit = details?.CodingTimeLimitInMinutes ?? CodeProjectQuestionHelper.DefaultTimeLimitMinutes;
                try
                {
                    if (!await _provisioner.HandoverAsync(env.EnvironmentHandle, env.HandedOverAtUtc.Value.AddMinutes(timeLimit) + SubmitGrace, ct))
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Warning,
                            new Exception($"Code project deadline not recorded for session {sessionId}, question {questionId}; the janitor's lifetime cap still applies."));
                }
                catch (Exception ex) { await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex); }
            }
            return env;
        }

        public async Task<(CodeProjectAssessment Assessment, CodeProjectGradeResult Grade)> SubmitAsync(string sessionId, int questionId,
            string createdBy, CancellationToken ct = default)
        {
            var existing = await _repository.GetCodeProjectAssessment(sessionId, questionId);
            if (!string.IsNullOrEmpty(existing?.ResultsJson))
                return (existing, Deserialize<CodeProjectGradeResult>(existing.ResultsJson));

            var details = await _repository.GetCodeProjectDetails(questionId);
            var env = await _repository.GetEnvironment(sessionId, questionId);
            if (details == null)
                return (null, new CodeProjectGradeResult { Error = "Question not found." });

            var now = DateTime.UtcNow;
            var timeLimit = details.CodingTimeLimitInMinutes ?? CodeProjectQuestionHelper.DefaultTimeLimitMinutes;
            var started = env?.HandedOverAtUtc;
            var assessment = new CodeProjectAssessment
            {
                SessionId = sessionId,
                QuestionId = questionId,
                QuestionText = details.Comment,
                CodeLanguage = details.Language,
                TimeLimitInMinutes = timeLimit,
                TimeStarted = started,
                TimeCompleted = now,
                TimeTakenInSeconds = started.HasValue ? (decimal)(now - started.Value).TotalSeconds : (decimal?)null,
                IsTimeExpired = started.HasValue && now > started.Value.AddMinutes(timeLimit),
                VMPropertyId = env?.VMPropertyId,
                CreatedBy = createdBy
            };

            CodeProjectGradeResult grade;
            try
            {
                if (env == null || string.IsNullOrEmpty(env.EnvironmentHandle) || env.HandedOverAtUtc == null)
                {
                    grade = new CodeProjectGradeResult { Error = "The environment was never handed over, so there is no work to grade." };
                    // A machine that failed or never became ready is our fault, not the candidate's. One
                    // that was ready but never opened is the candidate's choice and is scored as such.
                    if (env != null && (env.State == CodeProjectEnvironmentState.Failed || env.State == CodeProjectEnvironmentState.Provisioning))
                        Exclude(grade, env.State == CodeProjectEnvironmentState.Failed
                            ? "The candidate machine could not be started" + (string.IsNullOrEmpty(env.Message) ? "." : ": " + env.Message)
                            : "The candidate machine was not ready in time.");
                }
                else
                {
                    var snapshot = await _provisioner.CollectAsync(env.EnvironmentHandle, ct);
                    if (snapshot == null || !snapshot.Success)
                    {
                        grade = new CodeProjectGradeResult { Error = snapshot?.Error ?? "Could not read the candidate's workspace." };
                        Exclude(grade, "The candidate's work could not be read from the machine.");
                    }
                    else
                    {
                        var (starter, secrets) = await LoadProjectAsync(questionId, ct);
                        var workspace = CodeProjectQuestionHelper.BuildGradingWorkspace(starter, snapshot.Files,
                            ParseList(details.ReadOnlyPathsJson), secrets.HiddenTests);
                        var run = await _runner.RunAsync(RunRequest(details, workspace), ct);
                        // The machine is released right after this, so a grade can never be redone. A runner
                        // that did not complete (typically a cold image pull timing out) gets one more go;
                        // the second execution usually lands on a node that already has the image.
                        if (!run.Completed)
                        {
                            await _applicationErrorLogger.LogMessage(LogLevelInfo.Warning,
                                new Exception($"Code project grading for session {sessionId}, question {questionId} retried: {run.Error}"));
                            run = await _runner.RunAsync(RunRequest(details, workspace), ct);
                        }
                        grade = CodeProjectQuestionHelper.Grade(Deserialize<List<CodeProjectRequirement>>(details.RequirementsJson), run);
                        if (!run.Completed) Exclude(grade, "The grading runner was unavailable: " + (run.Error ?? "no result."));
                        grade.Commits = snapshot.Commits ?? new List<CodeProjectCommit>();
                        grade.BaselineSha = snapshot.BaselineSha;
                        grade.HeadSha = snapshot.HeadSha;
                        assessment.SubmittedCode = snapshot.Diff;
                    }
                    grade.IntegrityFlags = snapshot?.IntegrityFlags ?? new List<CodeProjectIntegrityFlag>();
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                grade = new CodeProjectGradeResult { Error = "Grading failed: " + ex.Message };
                Exclude(grade, "Grading failed on our side.");
            }

            if (grade.Requirements.Count == 0)
            {
                // Record every requirement as not met, so the report and the rollup still see the question.
                grade.Requirements = Deserialize<List<CodeProjectRequirement>>(details.RequirementsJson)
                    .Select(r => new CodeProjectRequirementResult { Id = r.Id, Text = r.Text, TestName = r.TestName, Weight = r.Weight, Passed = false, Message = grade.Error })
                    .ToList();
            }
            assessment.RequirementsTotal = grade.Requirements.Count;
            assessment.RequirementsPassed = grade.Requirements.Count(r => r.Passed);
            assessment.ResultsJson = JsonSerializer.Serialize(grade, Json);
            assessment.id = await _repository.SaveCodeProjectAssessment(assessment);

            if (env != null)
            {
                await _repository.UpdateEnvironment(sessionId, questionId, CodeProjectEnvironmentState.Submitted, message: "Submitted");
                try { await _provisioner.ReleaseAsync(env.EnvironmentHandle, ct); }
                catch (Exception ex) { await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex); }
            }
            return (assessment, grade);
        }

        public Task<List<CodeProjectAssessment>> GetAssessmentsForSession(string sessionId) =>
            _repository.GetCodeProjectAssessmentsBySessionId(sessionId);

        public string BuildTranscriptAnswer(CodeProjectDetails details, CodeProjectAssessment assessment, CodeProjectGradeResult grade)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Code Project Submission");
            if (grade?.Excluded == true)
            {
                // Read by the AI evaluator: a platform failure must not count as a zero.
                sb.AppendLine("NOT SCORED — environment unavailable. Exclude this question from the candidate's score; it is no reflection on the candidate.");
                sb.AppendLine("Reason : " + grade.ExclusionReason);
            }
            sb.AppendLine($"Requirements passed : {assessment?.RequirementsPassed ?? 0} / {assessment?.RequirementsTotal ?? 0} (correctness {grade?.CorrectnessScore ?? 0:0.#}/100)");
            foreach (var r in grade?.Requirements ?? new List<CodeProjectRequirementResult>())
                sb.AppendLine($"- [{(r.Passed ? "PASS" : "FAIL")}] {r.Id}: {r.Text}{(r.Passed || string.IsNullOrEmpty(r.Message) ? "" : " — " + r.Message)}");
            if (!string.IsNullOrEmpty(grade?.Error))
                sb.AppendLine("Note : " + grade.Error);
            if (grade?.Commits?.Count > 0)
                sb.AppendLine($"Commits : {grade.Commits.Count}");
            if (grade?.IntegrityFlags?.Count > 0)
            {
                sb.AppendLine("Session notes for a human reviewer (not proof of misconduct; do not adjust the score for these) :");
                foreach (var f in grade.IntegrityFlags)
                    sb.AppendLine($"- {f.Code}: {f.Detail}");
            }
            if (!string.IsNullOrWhiteSpace(assessment?.SubmittedCode))
            {
                var diff = assessment.SubmittedCode.Length > 12000 ? assessment.SubmittedCode.Substring(0, 12000) + "\n… (diff truncated)" : assessment.SubmittedCode;
                sb.AppendLine($"Written Code ({details?.Language}) — diff against baseline :");
                sb.AppendLine(diff);
            }
            return sb.ToString().TrimEnd();
        }

        /// <summary>Marks a grade as not attempted because our infrastructure failed, not the candidate.</summary>
        private static void Exclude(CodeProjectGradeResult grade, string reason)
        {
            grade.Excluded = true;
            grade.ExclusionReason = reason;
        }

        private async Task SaveFailure(int questionId, CodeProjectValidationReport report, string reason)
        {
            report.FailureReason ??= reason;
            var failedAt = report.Steps.FirstOrDefault(s => s.State == "run") ?? report.Steps.FirstOrDefault(s => s.State == "pending");
            if (failedAt != null)
            {
                failedAt.State = "failed";
                failedAt.Detail = reason;
            }
            foreach (var s in report.Steps.Where(s => s.State == "run")) s.State = "pending";
            await _repository.UpdateValidation(questionId, CodeProjectValidationStatus.Failed, JsonSerializer.Serialize(report, Json));
        }

        private static void SetStep(CodeProjectValidationReport report, string key, string state, string detail, double? seconds = null)
        {
            var step = report.Steps.FirstOrDefault(s => s.Key == key);
            if (step == null) return;
            step.State = state;
            step.Detail = detail;
            step.Seconds = seconds;
        }

        /// <summary>Order-independent identity of a project's three file tiers.</summary>
        private static string Fingerprint(CodeProjectGeneration g)
        {
            string Tier(IEnumerable<CodeProjectFile> files) => string.Join("\u0001", (files ?? Enumerable.Empty<CodeProjectFile>())
                .Where(f => f?.Path != null)
                .OrderBy(f => CodeProjectQuestionHelper.NormalizePath(f.Path), StringComparer.OrdinalIgnoreCase)
                .Select(f => CodeProjectQuestionHelper.NormalizePath(f.Path) + "\u0002" + (f.Content ?? "").Replace("\r\n", "\n").Trim()));
            return Tier(g.ProjectFiles) + "\u0003" + Tier(g.HiddenTests) + "\u0003" + Tier(g.ReferenceSolution);
        }

        /// <summary>Overwrites the stored candidate zip and the server-only secrets with the given project.</summary>
        private async Task StoreProjectAsync(int questionId, CodeProjectGeneration g, CancellationToken ct)
        {
            await _blobStore.UploadAsync(_blobStore.ProjectContainer, ProjectBlobName(questionId), Zip(g.ProjectFiles), "application/zip", ct);
            var secrets = new ProjectSecrets { HiddenTests = g.HiddenTests, ReferenceSolution = g.ReferenceSolution, SeededDefects = g.SeededDefects };
            await _blobStore.UploadAsync(_blobStore.SecretsContainer, SecretsBlobName(questionId),
                JsonSerializer.SerializeToUtf8Bytes(secrets, Json), "application/json", ct);
        }

        /// <summary>editable_files from the sanitized payload (the details row does not store them separately).</summary>
        private static List<string> EditableFilesFromPayload(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload)) return new List<string>();
            try
            {
                using var doc = JsonDocument.Parse(payload);
                return doc.RootElement.TryGetProperty("editable_files", out var e) && e.ValueKind == JsonValueKind.Array
                    ? e.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()).ToList()
                    : new List<string>();
            }
            catch (JsonException) { return new List<string>(); }
        }

        private async Task<(List<CodeProjectFile> Starter, ProjectSecrets Secrets)> LoadProjectAsync(int questionId, CancellationToken ct)
        {
            var zip = await _blobStore.DownloadAsync(_blobStore.ProjectContainer, ProjectBlobName(questionId), ct);
            var secretsBytes = await _blobStore.DownloadAsync(_blobStore.SecretsContainer, SecretsBlobName(questionId), ct);
            return (Unzip(zip), JsonSerializer.Deserialize<ProjectSecrets>(secretsBytes, Json) ?? new ProjectSecrets());
        }

        private static CodeProjectRunRequest RunRequest(CodeProjectDetails d, List<CodeProjectFile> files) => new CodeProjectRunRequest
        {
            StackImageCode = d.StackImageCode,
            Files = files,
            BuildCommand = d.BuildCommand,
            TestCommand = d.TestCommand,
            TimeoutSeconds = 120
        };

        private static CodeProjectGeneration ToGeneration(CodeProjectDetails d, List<CodeProjectFile> starter, ProjectSecrets s) => new CodeProjectGeneration
        {
            QuestionText = d.Comment,
            Language = d.Language,
            Framework = d.Framework,
            StackImageCode = d.StackImageCode,
            ProjectFiles = starter,
            EntryPointFile = d.EntryPointFile,
            ReadOnlyPaths = ParseList(d.ReadOnlyPathsJson),
            BuildCommand = d.BuildCommand,
            RunCommand = d.RunCommand,
            TestCommand = d.TestCommand,
            HasWebserver = d.HasWebserver ?? false,
            Requirements = Deserialize<List<CodeProjectRequirement>>(d.RequirementsJson),
            EstimatedMinutes = d.EstimatedMinutes ?? 0,
            TimeLimitMinutes = d.CodingTimeLimitInMinutes ?? CodeProjectQuestionHelper.DefaultTimeLimitMinutes,
            HiddenTests = s.HiddenTests,
            ReferenceSolution = s.ReferenceSolution,
            SeededDefects = s.SeededDefects
        };

        private static List<string> ParseList(string json) => Deserialize<List<string>>(json);

        private static T Deserialize<T>(string json) where T : new()
        {
            if (string.IsNullOrWhiteSpace(json)) return new T();
            try { return JsonSerializer.Deserialize<T>(json, Json) ?? new T(); }
            catch (JsonException) { return new T(); }
        }

        public static byte[] Zip(IEnumerable<CodeProjectFile> files)
        {
            using (var ms = new MemoryStream())
            {
                using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
                {
                    foreach (var f in files ?? Enumerable.Empty<CodeProjectFile>())
                    {
                        if (string.IsNullOrWhiteSpace(f?.Path)) continue;
                        var entry = archive.CreateEntry(CodeProjectQuestionHelper.NormalizePath(f.Path), CompressionLevel.Optimal);
                        using (var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false)))
                            writer.Write(f.Content ?? "");
                    }
                }
                return ms.ToArray();
            }
        }

        public static List<CodeProjectFile> Unzip(byte[] zip)
        {
            var files = new List<CodeProjectFile>();
            if (zip == null || zip.Length == 0) return files;
            using (var archive = new ZipArchive(new MemoryStream(zip), ZipArchiveMode.Read))
            {
                foreach (var entry in archive.Entries.Where(e => !string.IsNullOrEmpty(e.Name)))
                {
                    using (var reader = new StreamReader(entry.Open(), Encoding.UTF8))
                        files.Add(new CodeProjectFile { Path = CodeProjectQuestionHelper.NormalizePath(entry.FullName), Content = reader.ReadToEnd() });
                }
            }
            return files;
        }
    }
}
