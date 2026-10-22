using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// Parses and scope-checks a generated code project question. Fifteen minutes is a constraint on
    /// what the generator may produce, not a display value: it bounds file count, edit surface,
    /// build time and scope. The same limits go into the prompt and are re-checked here and again by
    /// the validation job, so an over-scoped project never publishes.
    /// </summary>
    public static class CodeProjectQuestionHelper
    {
        public const int DefaultTimeLimitMinutes = 15;
        public const int MaxProjectFiles = 8;
        public const int MinEditableFiles = 1;
        public const int MaxEditableFiles = 2;
        public const int MinRequirements = 3;
        public const int MaxRequirements = 5;
        public const int MaxEstimatedMinutes = 12;
        public const int MaxBuildAndTestSeconds = 20;

        public static readonly string ScopeRulesInstruction = string.Join("\n", new[]
        {
            $"Hard limits for a {DefaultTimeLimitMinutes}-minute code project (a question that breaks any of these is rejected):",
            $"- At most {MaxProjectFiles} files in project_files.",
            $"- The candidate edits {MinEditableFiles} to {MaxEditableFiles} files; list them in editable_files.",
            $"- Build plus test must finish in under {MaxBuildAndTestSeconds} seconds. Use offline flags; dependencies are pre-seeded in the stack image.",
            $"- {MinRequirements} to {MaxRequirements} requirements, each checked by exactly one hidden test named in test_name. Weights sum to 1.",
            $"- estimated_minutes is your honest estimate of candidate effort and must be at most {MaxEstimatedMinutes}.",
            "- Shape: one focused defect to fix, or one small feature to add. Never \"explore this codebase\".",
            "- The starter project must build, and the hidden tests must mostly FAIL against it. The reference solution must make every hidden test pass.",
            "- hidden_tests, reference_solution and seeded_defects are server-side only. Do not copy their content into project_files.",
            "- Every hidden test file has its own path that does NOT appear in project_files (for example under tests/hidden/). Visible tests, if any, use different file names.",
            "- project_files is a complete project that builds on its own with build_command: for .NET include every .csproj (and a test project referencing the code under test), " +
                "for Java the pom.xml, for Node the package.json, for Python any package __init__.py files.",
            "- SQL tasks: the only database available is SQLite through Python's built-in sqlite3 module (window functions supported) on the Python stack. " +
                "No other stack has a database or SQL driver, so do not attempt SQL on them.",
            "- Builds run OFFLINE. Use only the dependencies, versions and exact build_command/test_command listed for the chosen stack image.",
            "- test_command must write machine-readable results: Maven Surefire reports, pytest --junitxml=test-results/junit.xml, " +
                "vitest --reporter=junit --outputFile=test-results/junit.xml, or dotnet test --logger trx --results-directory TestResults.",
            "- Each requirement's test_name is the test method/function name exactly as the framework reports it (no class prefix, no parentheses)."
        });

        /// <summary>System instructions for the Responses API call; the request input carries the brief, rubric and allowlists.</summary>
        public static string GeneratorInstructions => string.Join("\n\n", new[]
        {
            "You generate small, runnable coding projects for timed technical interviews. The candidate works on a real machine with an IDE and a terminal.",
            ScopeRulesInstruction,
            DmIvQuestionRubricHelper.QuestionGistInstruction,
            ResponseContractInstruction,
            "Respond with the JSON object only."
        });

        public const string ResponseContractInstruction =
            "Return one JSON object with these fields: skill_or_competency, secondary_skills, question_text, question_gist, expected_answer, " +
            "time_limit_minutes, language, framework, stack_image_code, project_files [{path, content}], entry_point_file, editable_files, " +
            "read_only_paths, build_command, run_command, test_command, has_webserver, ports, requirements [{id, text, test_name, weight}], " +
            "estimated_minutes, hidden_tests [{path, content}], reference_solution [{path, content}], seeded_defects [{id, file, fails, explanation}].";

        /// <summary>Strips markdown fences and surrounding prose, then parses the contract. Returns null with an error on bad JSON.</summary>
        public static CodeProjectGeneration Parse(string raw, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(raw))
            {
                error = "AI returned an empty response.";
                return null;
            }

            var cleaned = raw.Trim();
            var start = cleaned.IndexOf('{');
            var end = cleaned.LastIndexOf('}');
            if (start < 0 || end <= start)
            {
                error = "AI did not return a JSON object.";
                return null;
            }
            cleaned = cleaned.Substring(start, end + 1 - start);

            try
            {
                using var doc = JsonDocument.Parse(cleaned);
                var r = doc.RootElement;
                return new CodeProjectGeneration
                {
                    SkillOrCompetency = Str(r, "skill_or_competency"),
                    SecondarySkills = StrList(r, "secondary_skills"),
                    QuestionText = Str(r, "question_text"),
                    QuestionGist = Str(r, "question_gist"),
                    ExpectedAnswer = Str(r, "expected_answer"),
                    TimeLimitMinutes = Int(r, "time_limit_minutes") ?? DefaultTimeLimitMinutes,
                    Language = Str(r, "language"),
                    Framework = Str(r, "framework"),
                    StackImageCode = Str(r, "stack_image_code"),
                    ProjectFiles = Files(r, "project_files"),
                    EntryPointFile = Str(r, "entry_point_file"),
                    EditableFiles = StrList(r, "editable_files"),
                    ReadOnlyPaths = StrList(r, "read_only_paths"),
                    BuildCommand = Str(r, "build_command"),
                    RunCommand = Str(r, "run_command"),
                    TestCommand = Str(r, "test_command"),
                    HasWebserver = r.TryGetProperty("has_webserver", out var hw) && hw.ValueKind == JsonValueKind.True,
                    Ports = IntList(r, "ports"),
                    Requirements = Requirements(r),
                    EstimatedMinutes = Int(r, "estimated_minutes") ?? 0,
                    HiddenTests = Files(r, "hidden_tests"),
                    ReferenceSolution = Files(r, "reference_solution"),
                    SeededDefects = Defects(r)
                };
            }
            catch (JsonException ex)
            {
                error = "AI returned invalid JSON: " + ex.Message;
                return null;
            }
        }

        /// <summary>Files the candidate is expected to edit. Falls back to the entry point when the model omits the list.</summary>
        public static List<string> ResolveEditableFiles(CodeProjectGeneration g)
        {
            var list = (g?.EditableFiles ?? new List<string>()).Where(p => !string.IsNullOrWhiteSpace(p)).Select(NormalizePath).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (list.Count == 0 && !string.IsNullOrWhiteSpace(g?.EntryPointFile))
                list.Add(NormalizePath(g.EntryPointFile));
            return list;
        }

        /// <summary>
        /// The model sometimes lists a hidden test in project_files too, which would hand the candidate
        /// the answer key. The hidden copy is overlaid at grading time anyway, so the candidate-facing
        /// copy is simply removed (from editable and read-only lists as well). Returns the removed paths.
        /// </summary>
        public static List<string> StripHiddenTestsFromProject(CodeProjectGeneration g)
        {
            if (g == null) return new List<string>();
            var hidden = new HashSet<string>(g.HiddenTests.Where(t => !string.IsNullOrWhiteSpace(t?.Path)).Select(t => NormalizePath(t.Path)),
                StringComparer.OrdinalIgnoreCase);
            var removed = g.ProjectFiles.Where(f => !string.IsNullOrWhiteSpace(f?.Path) && hidden.Contains(NormalizePath(f.Path)))
                .Select(f => NormalizePath(f.Path)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (removed.Count == 0) return removed;
            g.ProjectFiles = g.ProjectFiles.Where(f => f == null || string.IsNullOrWhiteSpace(f.Path) || !hidden.Contains(NormalizePath(f.Path))).ToList();
            g.EditableFiles = (g.EditableFiles ?? new List<string>()).Where(p => p == null || !hidden.Contains(NormalizePath(p))).ToList();
            g.ReadOnlyPaths = (g.ReadOnlyPaths ?? new List<string>()).Where(p => p == null || !hidden.Contains(NormalizePath(p))).ToList();
            return removed;
        }

        /// <summary>
        /// Static checks that need no build: the §06 limits plus the structural rules that keep tier 3
        /// apart from the candidate project. Returns every violation so the author sees them all at once.
        /// </summary>
        public static List<string> ValidateScope(CodeProjectGeneration g, int timeLimitMinutes)
        {
            var errors = new List<string>();
            if (g == null)
            {
                errors.Add("No project was generated.");
                return errors;
            }

            if (string.IsNullOrWhiteSpace(g.QuestionText)) errors.Add("question_text is missing.");
            if (string.IsNullOrWhiteSpace(g.BuildCommand)) errors.Add("build_command is missing.");
            if (string.IsNullOrWhiteSpace(g.TestCommand)) errors.Add("test_command is missing.");

            var files = g.ProjectFiles.Where(f => !string.IsNullOrWhiteSpace(f?.Path)).ToList();
            var paths = new HashSet<string>(files.Select(f => NormalizePath(f.Path)), StringComparer.OrdinalIgnoreCase);
            if (files.Count == 0) errors.Add("project_files is empty.");
            if (files.Count > MaxProjectFiles) errors.Add($"Project has {files.Count} files; the limit is {MaxProjectFiles}.");
            if (paths.Count != files.Count) errors.Add("project_files contains duplicate paths.");

            var editable = ResolveEditableFiles(g);
            if (editable.Count < MinEditableFiles || editable.Count > MaxEditableFiles)
                errors.Add($"Candidate edits {editable.Count} files; it must be {MinEditableFiles}–{MaxEditableFiles}.");
            foreach (var e in editable)
            {
                if (!paths.Contains(e)) errors.Add($"Editable file '{e}' is not in project_files.");
                if (IsReadOnly(e, g.ReadOnlyPaths)) errors.Add($"Editable file '{e}' is also marked read-only.");
            }

            var reqCount = g.Requirements.Count;
            if (reqCount < MinRequirements || reqCount > MaxRequirements)
                errors.Add($"Project has {reqCount} requirements; it must be {MinRequirements}–{MaxRequirements}.");
            if (g.Requirements.Any(r => string.IsNullOrWhiteSpace(r.TestName)))
                errors.Add("Every requirement needs a test_name.");
            if (reqCount > 0 && Math.Abs(g.Requirements.Sum(r => r.Weight) - 1m) > 0.05m)
                errors.Add("Requirement weights must sum to 1.");

            if (g.EstimatedMinutes <= 0)
                errors.Add("estimated_minutes is missing.");
            else if (g.EstimatedMinutes > MaxEstimatedMinutes || g.EstimatedMinutes > timeLimitMinutes)
                errors.Add($"Estimated effort is {g.EstimatedMinutes} minutes; it must be at most {Math.Min(MaxEstimatedMinutes, timeLimitMinutes)}.");

            if (g.HiddenTests.Count == 0) errors.Add("hidden_tests is empty — nothing to grade against.");
            if (g.ReferenceSolution.Count == 0) errors.Add("reference_solution is empty — solvability cannot be proved.");

            // Tier 3 must never ride along in the candidate zip.
            foreach (var t in g.HiddenTests.Where(t => !string.IsNullOrWhiteSpace(t?.Path)))
            {
                if (paths.Contains(NormalizePath(t.Path)))
                    errors.Add($"Hidden test '{t.Path}' also appears in project_files.");
            }

            return errors;
        }

        /// <summary>
        /// Tier 1/2 only, without file contents — safe for GeneratedPayload, which the admin panel
        /// reads with SELECT *. Hidden tests, reference solution and seeded defects are dropped.
        /// </summary>
        public static string BuildSanitizedPayload(CodeProjectGeneration g) => JsonSerializer.Serialize(new
        {
            skill_or_competency = g.SkillOrCompetency,
            secondary_skills = g.SecondarySkills,
            question_text = g.QuestionText,
            question_gist = g.QuestionGist,
            time_limit_minutes = g.TimeLimitMinutes,
            language = g.Language,
            framework = g.Framework,
            stack_image_code = g.StackImageCode,
            project_files = g.ProjectFiles.Select(f => NormalizePath(f.Path)).ToList(),
            entry_point_file = g.EntryPointFile,
            editable_files = ResolveEditableFiles(g),
            read_only_paths = g.ReadOnlyPaths,
            build_command = g.BuildCommand,
            run_command = g.RunCommand,
            test_command = g.TestCommand,
            has_webserver = g.HasWebserver,
            ports = g.Ports,
            requirements = g.Requirements.Select(r => new { id = r.Id, text = r.Text, weight = r.Weight }).ToList(),
            estimated_minutes = g.EstimatedMinutes
        });

        /// <summary>
        /// Finds a requirement's hidden test among runner results. Runners report names such as
        /// "StockItemTests.r1_creates" or "StockItemTests#r1_creates", so the class prefix is optional.
        /// </summary>
        public static CodeProjectTestResult FindTest(IEnumerable<CodeProjectTestResult> tests, string testName)
        {
            if (string.IsNullOrWhiteSpace(testName) || tests == null) return null;
            var name = testName.Trim();
            return tests.FirstOrDefault(t => t?.Name != null && (
                string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase)
                || t.Name.EndsWith("." + name, StringComparison.OrdinalIgnoreCase)
                || t.Name.EndsWith("#" + name, StringComparison.OrdinalIgnoreCase)
                || t.Name.EndsWith("::" + name, StringComparison.OrdinalIgnoreCase)));
        }

        /// <summary>Starter + reference solution overlaid + hidden tests.</summary>
        public static List<CodeProjectFile> BuildReferenceWorkspace(IEnumerable<CodeProjectFile> starter,
            IEnumerable<CodeProjectFile> referenceSolution, IEnumerable<CodeProjectFile> hiddenTests) =>
            Overlay(Overlay(starter, referenceSolution), hiddenTests);

        /// <summary>
        /// What gets graded: the candidate's workspace, except that read-only paths are reset to the
        /// starter version (a candidate cannot pass by editing the visible tests), and hidden tests are added last.
        /// </summary>
        public static List<CodeProjectFile> BuildGradingWorkspace(IEnumerable<CodeProjectFile> starter,
            IEnumerable<CodeProjectFile> candidate, IEnumerable<string> readOnlyGlobs, IEnumerable<CodeProjectFile> hiddenTests)
        {
            var globs = (readOnlyGlobs ?? Enumerable.Empty<string>()).ToList();
            var starterList = (starter ?? Enumerable.Empty<CodeProjectFile>()).ToList();
            var editable = (candidate ?? Enumerable.Empty<CodeProjectFile>())
                .Where(f => !string.IsNullOrWhiteSpace(f?.Path) && !IsReadOnly(f.Path, globs));
            var readOnlyStarter = starterList.Where(f => IsReadOnly(f.Path, globs));
            return Overlay(Overlay(Overlay(starterList, editable), readOnlyStarter), hiddenTests);
        }

        private static List<CodeProjectFile> Overlay(IEnumerable<CodeProjectFile> baseFiles, IEnumerable<CodeProjectFile> overlay)
        {
            var map = new Dictionary<string, CodeProjectFile>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in (baseFiles ?? Enumerable.Empty<CodeProjectFile>()).Concat(overlay ?? Enumerable.Empty<CodeProjectFile>()))
            {
                if (string.IsNullOrWhiteSpace(f?.Path)) continue;
                var p = NormalizePath(f.Path);
                map[p] = new CodeProjectFile { Path = p, Content = f.Content ?? "" };
            }
            return map.Values.ToList();
        }

        /// <summary>
        /// The validation verdict. Reference passing everything proves the task is solvable; starter
        /// mostly failing proves it is not already solved — a pre-solved question scores every candidate full marks.
        /// </summary>
        public static CodeProjectValidationReport EvaluateValidation(CodeProjectGeneration g, int timeLimitMinutes,
            CodeProjectRunResult starterRun, CodeProjectRunResult referenceRun)
        {
            var report = new CodeProjectValidationReport();
            var total = g.Requirements.Count;

            report.Steps.Add(Step("generate", "Generate project", "ok",
                $"{g.ProjectFiles.Count} files · {g.Framework ?? g.Language} · {g.StackImageCode}"));

            bool Fail(string key, string title, string detail, double? secs = null)
            {
                report.Steps.Add(Step(key, title, "failed", detail, secs));
                report.FailureReason ??= detail;
                return false;
            }

            if (starterRun == null || !starterRun.Completed)
            {
                Fail("build", "Build in container", starterRun?.Error ?? "The runner did not complete.");
                return Finish(report);
            }
            if (starterRun.BuildExitCode != 0)
            {
                Fail("build", "Build in container", $"{g.BuildCommand} → exit {starterRun.BuildExitCode}. The starter project must compile.", starterRun.WallSeconds);
                return Finish(report);
            }
            report.Steps.Add(Step("build", "Build in container", "ok", $"{g.BuildCommand} → exit 0", starterRun.WallSeconds));

            if (referenceRun == null || !referenceRun.Completed || referenceRun.BuildExitCode != 0)
            {
                Fail("reference", "Reference solution", referenceRun?.Error ?? $"Reference solution did not build (exit {referenceRun?.BuildExitCode}).", referenceRun?.WallSeconds);
                return Finish(report);
            }
            var missing = g.Requirements.Where(r => FindTest(referenceRun.Tests, r.TestName) == null).Select(r => r.TestName).ToList();
            var refPassed = g.Requirements.Count(r => FindTest(referenceRun.Tests, r.TestName)?.Passed == true);
            if (missing.Count > 0)
            {
                Fail("reference", "Reference solution", $"No hidden test found for: {string.Join(", ", missing)}.", referenceRun.WallSeconds);
                return Finish(report);
            }
            if (refPassed < total)
            {
                Fail("reference", "Reference solution", $"{refPassed} / {total} hidden tests passed — the question is not solvable as written.", referenceRun.WallSeconds);
                return Finish(report);
            }
            report.Steps.Add(Step("reference", "Reference solution", "ok", $"{refPassed} / {total} hidden tests passed — question is solvable", referenceRun.WallSeconds));

            var starterPassed = g.Requirements.Count(r => FindTest(starterRun.Tests, r.TestName)?.Passed == true);
            if (starterPassed * 2 >= total)
            {
                Fail("starter", "Starter state", $"{starterPassed} / {total} passed before any change — the question ships pre-solved.");
                return Finish(report);
            }
            report.Steps.Add(Step("starter", "Starter state", "ok", $"{starterPassed} / {total} passed — not pre-solved"));

            var wall = referenceRun.WallSeconds;
            if (wall >= MaxBuildAndTestSeconds)
            {
                Fail("budget", "Time budget", $"Build + test took {wall:0.0}s; it must be under {MaxBuildAndTestSeconds}s.", wall);
                return Finish(report);
            }
            if (g.EstimatedMinutes > timeLimitMinutes || g.EstimatedMinutes > MaxEstimatedMinutes)
            {
                Fail("budget", "Time budget", $"Estimated effort {g.EstimatedMinutes} min exceeds the {Math.Min(timeLimitMinutes, MaxEstimatedMinutes)}-minute cap.");
                return Finish(report);
            }
            report.Steps.Add(Step("budget", "Time budget", "ok", $"build + test {wall:0.0}s · est. {g.EstimatedMinutes} of {timeLimitMinutes} min", wall));
            return report;
        }

        /// <summary>Marks every step after the first failure as not reached, so the pipeline renders fully.</summary>
        private static CodeProjectValidationReport Finish(CodeProjectValidationReport report)
        {
            foreach (var key in PipelineKeys.Skip(report.Steps.Count))
                report.Steps.Add(Step(key.Key, key.Title, "pending", null));
            return report;
        }

        public static readonly IReadOnlyList<(string Key, string Title)> PipelineKeys = new[]
        {
            ("generate", "Generate project"),
            ("build", "Build in container"),
            ("reference", "Reference solution"),
            ("starter", "Starter state"),
            ("budget", "Time budget"),
            ("publish", "Publish")
        };

        public static CodeProjectValidationReport InitialReport(string runningKey = "build")
        {
            var report = new CodeProjectValidationReport();
            foreach (var (key, title) in PipelineKeys)
                report.Steps.Add(Step(key, title, key == "generate" ? "ok" : key == runningKey ? "run" : "pending", null));
            return report;
        }

        public static CodeProjectValidationStep Step(string key, string title, string state, string detail, double? seconds = null) =>
            new CodeProjectValidationStep { Key = key, Title = title, State = state, Detail = detail, Seconds = seconds };

        /// <summary>Weighted requirement score from a grading run. A build failure scores zero on every requirement.</summary>
        public static CodeProjectGradeResult Grade(IEnumerable<CodeProjectRequirement> requirements, CodeProjectRunResult run)
        {
            var reqs = (requirements ?? Enumerable.Empty<CodeProjectRequirement>()).ToList();
            var built = run != null && run.Completed && run.BuildExitCode == 0;
            var result = new CodeProjectGradeResult
            {
                BuildSucceeded = built,
                BuildOutput = Truncate(run?.BuildOutput, 4000),
                Tests = run?.Tests ?? new List<CodeProjectTestResult>(),
                Error = run == null || !run.Completed ? run?.Error ?? "Grading did not run." : null
            };
            foreach (var r in reqs)
            {
                var t = built ? FindTest(run.Tests, r.TestName) : null;
                result.Requirements.Add(new CodeProjectRequirementResult
                {
                    Id = r.Id,
                    Text = r.Text,
                    TestName = r.TestName,
                    Weight = r.Weight,
                    Passed = t?.Passed == true,
                    Message = !built ? "Project did not build." : t == null ? "Test did not run." : t.Message
                });
            }
            var totalWeight = reqs.Sum(r => r.Weight);
            var passedWeight = result.Requirements.Where(r => r.Passed).Sum(r => r.Weight);
            result.CorrectnessScore = totalWeight > 0
                ? Math.Round(passedWeight / totalWeight * 100m, 1)
                : reqs.Count == 0 ? 0 : Math.Round(100m * result.Requirements.Count(r => r.Passed) / reqs.Count, 1);
            return result;
        }

        private static string Truncate(string s, int max) => s == null || s.Length <= max ? s : s.Substring(s.Length - max);

        public static bool IsReadOnly(string path, IEnumerable<string> readOnlyGlobs) =>
            (readOnlyGlobs ?? Enumerable.Empty<string>()).Any(g => GlobMatch(NormalizePath(path), g));

        /// <summary>Minimal glob: '**' spans directories, '*' stays within one segment, '?' is one character.</summary>
        public static bool GlobMatch(string path, string glob)
        {
            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(glob)) return false;
            var pattern = "^" + Regex.Escape(NormalizePath(glob))
                .Replace(@"\*\*/", "(?:.*/)?")
                .Replace(@"\*\*", ".*")
                .Replace(@"\*", "[^/]*")
                .Replace(@"\?", "[^/]") + "$";
            return Regex.IsMatch(NormalizePath(path), pattern, RegexOptions.IgnoreCase);
        }

        public static string NormalizePath(string path) =>
            (path ?? string.Empty).Trim().Replace('\\', '/').TrimStart('.', '/');

        private static string Str(JsonElement r, string name) =>
            r.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()?.Trim() : null;

        private static int? Int(JsonElement r, string name)
        {
            if (!r.TryGetProperty(name, out var v)) return null;
            if (v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var n)) return n;
            if (v.ValueKind == JsonValueKind.String && int.TryParse(v.GetString(), out var s)) return s;
            return null;
        }

        private static List<string> StrList(JsonElement r, string name) =>
            r.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Array
                ? v.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.String).Select(e => e.GetString()?.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList()
                : new List<string>();

        private static List<int> IntList(JsonElement r, string name) =>
            r.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Array
                ? v.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.Number && e.TryGetInt32(out _)).Select(e => e.GetInt32()).ToList()
                : new List<int>();

        private static List<CodeProjectFile> Files(JsonElement r, string name) =>
            r.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Array
                ? v.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.Object)
                    .Select(e => new CodeProjectFile { Path = Str(e, "path"), Content = e.TryGetProperty("content", out var c) && c.ValueKind == JsonValueKind.String ? c.GetString() : "" })
                    .Where(f => !string.IsNullOrWhiteSpace(f.Path)).ToList()
                : new List<CodeProjectFile>();

        private static List<CodeProjectRequirement> Requirements(JsonElement r) =>
            r.TryGetProperty("requirements", out var v) && v.ValueKind == JsonValueKind.Array
                ? v.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.Object).Select(e => new CodeProjectRequirement
                {
                    Id = Str(e, "id"),
                    Text = Str(e, "text"),
                    TestName = Str(e, "test_name"),
                    Weight = e.TryGetProperty("weight", out var w) && w.ValueKind == JsonValueKind.Number ? w.GetDecimal() : 0m
                }).ToList()
                : new List<CodeProjectRequirement>();

        private static List<CodeProjectSeededDefect> Defects(JsonElement r) =>
            r.TryGetProperty("seeded_defects", out var v) && v.ValueKind == JsonValueKind.Array
                ? v.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.Object).Select(e => new CodeProjectSeededDefect
                {
                    Id = Str(e, "id"),
                    File = Str(e, "file"),
                    Fails = StrList(e, "fails"),
                    Explanation = Str(e, "explanation")
                }).ToList()
                : new List<CodeProjectSeededDefect>();
    }
}
