using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
            "- build_command and test_command run in the workspace root. For .NET put each project in its own folder (App/App.csproj, App.Tests/App.Tests.csproj) and a .sln at the root listing them; never a .csproj at the root next to other projects.",
            "- Node: in package.json use the EXACT versions listed for the stack (\"5.4.10\", never \"^5.4.10\" or \"~5.4.10\"); ranges resolve to versions that are not on the machine.",
            "- .NET minimal APIs tested with WebApplicationFactory<Program>: end Program.cs with `public partial class Program { }` (otherwise Program is internal and the tests do not compile).",
            "- project_files is a complete project that builds on its own with build_command: for .NET include every .csproj (and a test project referencing the code under test), " +
                "for Java the pom.xml, for Node the package.json, for Python any package __init__.py files.",
            "- SQL tasks: the only database available is SQLite through Python's built-in sqlite3 module (window functions supported) on the Python stack. " +
                "No other stack has a database or SQL driver, so do not attempt SQL on them.",
            "- Builds run OFFLINE. Use only the dependencies, versions and exact build_command/test_command listed for the chosen stack image.",
            "- test_command must write machine-readable results: Maven Surefire reports, pytest --junitxml=test-results/junit.xml, " +
                "vitest --reporter=junit --outputFile=test-results/junit.xml, or dotnet test --logger trx --results-directory TestResults.",
            "- Each requirement's test_name is the test method/function name exactly as the framework reports it (no class prefix, no parentheses)."
        });

        /// <summary>
        /// Inline instructions for suggesting an author brief from the chosen stack, rubric skill and
        /// difficulty (no stored OpenAI prompt). The brief is what an author would type: the generator
        /// turns it into the actual project, so it must be feasible on that stack's offline toolchain.
        /// </summary>
        public static string BriefSuggestionInstructions => string.Join("\n", new[]
        {
            "You suggest the brief for a timed code project interview question. An author would normally write it; you write it for them.",
            $"- 2 to 4 plain-English sentences, at most 600 characters, written as an instruction to the question generator (\"A small ... The candidate ...\").",
            $"- One focused defect to fix or one small feature to add, doable in {DefaultTimeLimitMinutes} minutes by a competent candidate at the given difficulty.",
            "- Name the concrete behaviour to implement or fix and what correct output looks like, so it can be checked by automated tests.",
            "- It must be buildable with ONLY the chosen stack's listed language, framework and dependencies, offline. Never require anything not listed (no external services, databases, cloud APIs or extra packages).",
            "- SQL tasks are only possible on the Python stack, using SQLite through the built-in sqlite3 module (window functions supported). On other stacks, do not suggest SQL work.",
            "- It must exercise the given rubric skill. If no skill is given, choose the single rubric skill that best fits the chosen stack.",
            "- Never include the solution, hints about the fix, or test code.",
            "- If a previous brief is given, suggest a clearly different scenario.",
            "Return one JSON object: {\"brief\": \"...\", \"skill\": \"<the rubric skill, copied exactly from the list>\"}."
        });

        public const int MaxSuggestedBriefLength = 800;

        // ------------------------------------------------------------------ automatic repair

        /// <summary>How many times a project that failed validation is sent back to the AI with its build/test output.</summary>
        public const int MaxRepairRounds = 2;

        /// <summary>
        /// Inline instructions for repairing a generated project. Only file contents may change: the
        /// question, requirements, test names and commands are stored with the question and stay fixed.
        /// </summary>
        public static string RepairInstructions => string.Join("\n\n", new[]
        {
            "You repair a generated code project for a timed technical interview. It failed automated validation in an offline build container. " +
            "You get every file, the failing step and the real build/test output. Return corrected files so that: the starter project builds; " +
            "the starter fails most hidden tests (the candidate's task is still to do); the starter plus the reference solution passes EVERY hidden test.",
            "Do NOT change the question, the requirements, their test names or weights, build_command, test_command, editable_files or read_only_paths. " +
            "Each requirement's test_name must be the exact name of a test method in hidden_tests. Fix the code, the project files and the tests instead.",
            ScopeRulesInstruction,
            "Return one JSON object: {\"project_files\": [{\"path\", \"content\"}], \"hidden_tests\": [{\"path\", \"content\"}], " +
            "\"reference_solution\": [{\"path\", \"content\"}], \"notes\": \"one sentence on what you fixed\"}. Each list is the COMPLETE new set for that tier, " +
            "with full file contents. The reference_solution lists only the files that differ from the starter (normally the editable files)."
        });

        /// <summary>
        /// The deterministic corrections every generated or repaired project gets (all idempotent): hidden
        /// tests out of the candidate's files, a root .sln, offline NuGet with implicit usings, a visible
        /// Program for WebApplicationFactory, and the stack's exact package versions.
        /// </summary>
        public static void ApplyAutomaticFixes(CodeProjectGeneration g, Action<CodeProjectGeneration> stackFixups = null)
        {
            StripHiddenTestsFromProject(g);
            EnsureDotnetSolution(g);
            EnsureDotnetOffline(g);
            EnsureProgramVisibleToTests(g);
            EnsureDjangoSettingsForPytest(g);
            stackFixups?.Invoke(g);
        }

        /// <summary>
        /// A .csproj at the workspace root compiles every .cs file beneath it, including the test
        /// project's, so the app fails with "Xunit could not be found"; and a root .sln next to it makes
        /// `dotnet build` ambiguous. When a root project sits beside projects in sub-folders, its files move
        /// into a folder of its own (MinimalApiDemo.csproj → MinimalApiDemo/MinimalApiDemo.csproj), and
        /// every reference to them (editable and read-only lists, entry point, reference solution,
        /// ProjectReference paths) follows. Run before the question is saved, since paths are stored.
        /// Returns the folder created, or null.
        /// </summary>
        public static string MoveRootDotnetProjectIntoFolder(CodeProjectGeneration g)
        {
            if (g == null || !(g.BuildCommand ?? "").TrimStart().StartsWith("dotnet", StringComparison.OrdinalIgnoreCase)) return null;
            var paths = g.ProjectFiles.Where(f => f?.Path != null).Select(f => NormalizePath(f.Path)).ToList();
            var roots = paths.Where(p => !p.Contains('/') && p.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)).ToList();
            var others = paths.Where(p => p.Contains('/') && p.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)).ToList();
            if (roots.Count != 1 || others.Count == 0) return null;

            var rootProject = roots[0];
            var name = Path.GetFileNameWithoutExtension(rootProject);
            var folder = paths.Any(p => p.StartsWith(name + "/", StringComparison.OrdinalIgnoreCase)) ? name + ".App" : name;
            var otherDirs = others.Select(p => p.Substring(0, p.LastIndexOf('/') + 1)).ToList();
            var shared = new[] { ".sln", ".slnx" };
            var sharedNames = new[] { "NuGet.Config", "Directory.Build.props", "Directory.Build.targets", "global.json", ".gitignore", ".editorconfig", "README.md" };
            bool Moves(string p) =>
                !otherDirs.Any(d => p.StartsWith(d, StringComparison.OrdinalIgnoreCase))
                && !(!p.Contains('/') && (shared.Any(s => p.EndsWith(s, StringComparison.OrdinalIgnoreCase)) || sharedNames.Contains(p, StringComparer.OrdinalIgnoreCase)));
            string Map(string p)
            {
                if (string.IsNullOrWhiteSpace(p)) return p;
                var n = NormalizePath(p);
                return Moves(n) ? folder + "/" + n : n;
            }

            // ProjectReference paths that pointed at the root project now point into its folder.
            static string Resolve(string fromDir, string include)
            {
                var parts = (fromDir + include.Replace('\\', '/')).Split('/').ToList();
                var stack = new List<string>();
                foreach (var part in parts)
                {
                    if (part == "..") { if (stack.Count > 0) stack.RemoveAt(stack.Count - 1); }
                    else if (part.Length > 0 && part != ".") stack.Add(part);
                }
                return string.Join("/", stack);
            }
            static string Relative(string fromDir, string to)
            {
                var up = fromDir.Count(c => c == '/');
                return string.Concat(Enumerable.Repeat("..\\", up)) + to.Replace('/', '\\');
            }

            foreach (var f in g.ProjectFiles.Concat(g.ReferenceSolution).Concat(g.HiddenTests))
            {
                if (f?.Path == null) continue;
                var oldPath = NormalizePath(f.Path);
                var newPath = Map(oldPath);
                if (oldPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) && f.Content != null)
                {
                    var oldDir = oldPath.Contains('/') ? oldPath.Substring(0, oldPath.LastIndexOf('/') + 1) : "";
                    var newDir = newPath.Contains('/') ? newPath.Substring(0, newPath.LastIndexOf('/') + 1) : "";
                    f.Content = Regex.Replace(f.Content, @"(<ProjectReference\s+Include="")([^""]+)("")", m =>
                    {
                        var target = Map(Resolve(oldDir, m.Groups[2].Value));
                        return m.Groups[1].Value + Relative(newDir, target) + m.Groups[3].Value;
                    });
                }
                f.Path = newPath;
            }
            g.EditableFiles = (g.EditableFiles ?? new List<string>()).Select(Map).ToList();
            g.ReadOnlyPaths = (g.ReadOnlyPaths ?? new List<string>()).Select(Map).ToList();
            g.EntryPointFile = Map(g.EntryPointFile);
            return folder;
        }

        /// <summary>
        /// Django (and DRF) tests run under pytest-django need DJANGO_SETTINGS_MODULE; without it every
        /// test module fails at collection with ImproperlyConfigured. When a Django project with a
        /// settings.py has no such setting, a root pytest.ini naming it is added (or the line is added to
        /// an existing pytest.ini). Returns the path changed, or null.
        /// </summary>
        public static string EnsureDjangoSettingsForPytest(CodeProjectGeneration g)
        {
            if (g == null || (g.TestCommand ?? "").IndexOf("pytest", StringComparison.OrdinalIgnoreCase) < 0) return null;
            var files = g.ProjectFiles.Where(f => f?.Path != null).ToList();
            var all = files.Concat(g.HiddenTests).Concat(g.ReferenceSolution).Where(f => f?.Content != null).ToList();
            if (!all.Any(f => Regex.IsMatch(f.Content, @"^\s*(from|import)\s+(django|rest_framework)\b", RegexOptions.Multiline))) return null;
            // Only where pytest itself picks the setting up counts: manage.py sets it too, but pytest never runs manage.py.
            var pytestConfig = new[] { "pytest.ini", "pyproject.toml", "setup.cfg", "tox.ini", "conftest.py" };
            if (all.Any(f => f.Path != null && pytestConfig.Contains(Path.GetFileName(NormalizePath(f.Path)), StringComparer.OrdinalIgnoreCase)
                    && (f.Content.Contains("DJANGO_SETTINGS_MODULE") || f.Content.Contains("settings.configure("))))
                return null;

            var settings = files.Select(f => NormalizePath(f.Path)).Where(p => p.EndsWith("settings.py", StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p.Count(c => c == '/')).ThenBy(p => p, StringComparer.Ordinal).FirstOrDefault();
            if (settings == null) return null;
            var module = settings.Substring(0, settings.Length - 3).Replace('/', '.');
            var line = $"DJANGO_SETTINGS_MODULE = {module}";

            var ini = files.FirstOrDefault(f => string.Equals(NormalizePath(f.Path), "pytest.ini", StringComparison.OrdinalIgnoreCase));
            if (ini != null)
            {
                var content = ini.Content ?? "";
                ini.Content = content.Contains("[pytest]")
                    ? Regex.Replace(content, @"\[pytest\][^\n]*\n?", m => m.Value.TrimEnd('\r', '\n') + "\n" + line + "\n", RegexOptions.None, TimeSpan.FromSeconds(1))
                    : "[pytest]\n" + line + "\n\n" + content;
            }
            else
            {
                g.ProjectFiles.Add(new CodeProjectFile { Path = "pytest.ini", Content = "[pytest]\n" + line + "\npython_files = test_*.py *_test.py tests.py\n" });
                g.ReadOnlyPaths ??= new List<string>();
                if (!g.ReadOnlyPaths.Contains("pytest.ini", StringComparer.OrdinalIgnoreCase)) g.ReadOnlyPaths.Add("pytest.ini");
            }
            return "pytest.ini";
        }

        /// <summary>Failures the project itself can be repaired from: compile errors, unsolvable or mismatched tests, a pre-solved starter.</summary>
        public static bool IsRepairable(CodeProjectValidationReport report, CodeProjectRunResult starterRun, CodeProjectRunResult referenceRun)
        {
            if (report?.FailureReason == null || starterRun == null || !starterRun.Completed) return false;
            if (referenceRun != null && !referenceRun.Completed) return false;   // infrastructure, not the project
            var failed = report.Steps.FirstOrDefault(s => s.State == "failed")?.Key;
            return failed == "build" || failed == "reference" || failed == "starter";
        }

        /// <summary>The repair request: fixed fields, every file of every tier, and what went wrong.</summary>
        public static string BuildRepairInput(CodeProjectGeneration g, IEnumerable<string> editableFiles, CodeProjectValidationReport report,
            CodeProjectRunResult starterRun, CodeProjectRunResult referenceRun)
        {
            var failed = report.Steps.FirstOrDefault(s => s.State == "failed");
            var run = failed?.Key == "build" || failed?.Key == "starter" ? starterRun : referenceRun ?? starterRun;
            var sb = new StringBuilder();
            sb.AppendLine("Repair this project. Answer in JSON.");
            sb.AppendLine();
            sb.AppendLine($"Failing step: {failed?.Title} — {report.FailureReason}");
            sb.AppendLine($"Run of: {(run == starterRun ? "starter project + hidden tests" : "starter + reference solution + hidden tests")}");
            sb.AppendLine($"Build exit code: {run?.BuildExitCode}; test exit code: {run?.TestExitCode?.ToString() ?? "not run"}");
            sb.AppendLine("Build output (end):"); sb.AppendLine(Truncate(run?.BuildOutput, 6000) ?? "(none)");
            sb.AppendLine("Test output (end):"); sb.AppendLine(Truncate(run?.TestOutput, 6000) ?? "(none)");
            if (run?.Tests?.Count > 0)
                sb.AppendLine("Tests reported by the framework: " + string.Join(", ", run.Tests.Select(t => $"{t.Name}={(t.Passed ? "pass" : "fail")}")));
            sb.AppendLine();
            sb.AppendLine("Fixed (do not change):");
            sb.AppendLine(JsonSerializer.Serialize(new
            {
                question_text = g.QuestionText,
                stack_image_code = g.StackImageCode,
                build_command = g.BuildCommand,
                test_command = g.TestCommand,
                editable_files = editableFiles,
                read_only_paths = g.ReadOnlyPaths,
                requirements = g.Requirements.Select(r => new { id = r.Id, text = r.Text, test_name = r.TestName, weight = r.Weight })
            }));
            sb.AppendLine();
            sb.AppendLine("Current files:");
            sb.AppendLine(JsonSerializer.Serialize(new
            {
                project_files = g.ProjectFiles.Select(f => new { path = f.Path, content = f.Content }),
                hidden_tests = g.HiddenTests.Select(f => new { path = f.Path, content = f.Content }),
                reference_solution = g.ReferenceSolution.Select(f => new { path = f.Path, content = f.Content })
            }));
            return sb.ToString();
        }

        /// <summary>
        /// Applies the AI's corrected files to a copy of the project, re-applies the automatic fixes and
        /// re-checks the static rules. Null with an error when the answer is unusable.
        /// </summary>
        public static CodeProjectGeneration ApplyRepair(CodeProjectGeneration original, string raw, int timeLimitMinutes, out string error, out string notes,
            Action<CodeProjectGeneration> stackFixups = null)
        {
            error = null; notes = null;
            if (string.IsNullOrWhiteSpace(raw)) { error = "The AI returned no repair."; return null; }
            JsonElement root;
            try
            {
                var start = raw.IndexOf('{');
                var end = raw.LastIndexOf('}');
                if (start < 0 || end <= start) { error = "The repair was not a JSON object."; return null; }
                root = JsonDocument.Parse(raw.Substring(start, end - start + 1)).RootElement.Clone();
            }
            catch (JsonException) { error = "The repair was not valid JSON."; return null; }

            static List<CodeProjectFile> Files(JsonElement r, string name) =>
                r.TryGetProperty(name, out var arr) && arr.ValueKind == JsonValueKind.Array
                    ? arr.EnumerateArray()
                        .Where(e => e.ValueKind == JsonValueKind.Object && e.TryGetProperty("path", out var p) && p.ValueKind == JsonValueKind.String)
                        .Select(e => new CodeProjectFile
                        {
                            Path = NormalizePath(e.GetProperty("path").GetString()),
                            Content = e.TryGetProperty("content", out var c) && c.ValueKind == JsonValueKind.String ? c.GetString() : ""
                        }).ToList()
                    : null;

            var project = Files(root, "project_files");
            var hidden = Files(root, "hidden_tests");
            var reference = Files(root, "reference_solution");
            if (project == null || project.Count == 0 || hidden == null || hidden.Count == 0 || reference == null || reference.Count == 0)
            {
                error = "The repair did not return all three file sets.";
                return null;
            }
            notes = root.TryGetProperty("notes", out var n) && n.ValueKind == JsonValueKind.String ? n.GetString() : null;

            // Everything except the files is kept from the original, so the stored question stays consistent.
            var g = new CodeProjectGeneration
            {
                SkillOrCompetency = original.SkillOrCompetency,
                SecondarySkills = original.SecondarySkills,
                QuestionText = original.QuestionText,
                QuestionGist = original.QuestionGist,
                ExpectedAnswer = original.ExpectedAnswer,
                TimeLimitMinutes = original.TimeLimitMinutes,
                Language = original.Language,
                Framework = original.Framework,
                StackImageCode = original.StackImageCode,
                EntryPointFile = original.EntryPointFile,
                EditableFiles = original.EditableFiles,
                ReadOnlyPaths = (original.ReadOnlyPaths ?? new List<string>()).ToList(),
                BuildCommand = original.BuildCommand,
                RunCommand = original.RunCommand,
                TestCommand = original.TestCommand,
                HasWebserver = original.HasWebserver,
                Ports = original.Ports,
                Requirements = original.Requirements,
                EstimatedMinutes = original.EstimatedMinutes,
                SeededDefects = original.SeededDefects,
                ProjectFiles = project,
                HiddenTests = hidden,
                ReferenceSolution = reference
            };
            ApplyAutomaticFixes(g, stackFixups);

            var problems = ValidateScope(g, timeLimitMinutes)
                // The automatic .NET files may push the count past the limit; they are not the AI's files.
                .Where(p => !p.StartsWith("Project has ", StringComparison.Ordinal) || !p.Contains(" files;"))
                .ToList();
            if (problems.Count > 0) { error = "The repaired project broke the rules: " + string.Join(" ", problems); return null; }
            return g;
        }

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
                Fail("build", "Build in container", $"{g.BuildCommand} → exit {starterRun.BuildExitCode}. The starter project must compile.{BuildErrorExcerpt(starterRun.BuildOutput)}", starterRun.WallSeconds);
                return Finish(report);
            }
            report.Steps.Add(Step("build", "Build in container", "ok", $"{g.BuildCommand} → exit 0", starterRun.WallSeconds));

            if (referenceRun == null || !referenceRun.Completed || referenceRun.BuildExitCode != 0)
            {
                Fail("reference", "Reference solution", referenceRun?.Error ?? $"Reference solution did not build (exit {referenceRun?.BuildExitCode}).{BuildErrorExcerpt(referenceRun?.BuildOutput)}", referenceRun?.WallSeconds);
                return Finish(report);
            }
            var missing = g.Requirements.Where(r => FindTest(referenceRun.Tests, r.TestName) == null).Select(r => r.TestName).ToList();
            var refPassed = g.Requirements.Count(r => FindTest(referenceRun.Tests, r.TestName)?.Passed == true);
            if (missing.Count > 0)
            {
                Fail("reference", "Reference solution", $"No hidden test found for: {string.Join(", ", missing)}. Tests reported: {(referenceRun.Tests.Count == 0 ? "none" : string.Join(", ", referenceRun.Tests.Take(8).Select(t => t.Name)))}.{BuildErrorExcerpt(referenceRun.TestOutput)}", referenceRun.WallSeconds);
                return Finish(report);
            }
            if (refPassed < total)
            {
                Fail("reference", "Reference solution", $"{refPassed} / {total} hidden tests passed — the question is not solvable as written.{BuildErrorExcerpt(referenceRun.TestOutput)}", referenceRun.WallSeconds);
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

        /// <summary>
        /// The lines of a build log that say what broke (compiler/MSBuild/npm/pip errors), so the author
        /// sees the cause rather than just an exit code. Empty when nothing recognisable is found.
        /// </summary>
        public static string BuildErrorExcerpt(string buildOutput)
        {
            if (string.IsNullOrWhiteSpace(buildOutput)) return "";
            var lines = buildOutput.Replace("\r", "").Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
            var errors = lines.Where(l => l.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0
                    || l.StartsWith("ERR!", StringComparison.Ordinal) || l.Contains("Exception") || l.StartsWith("E ", StringComparison.Ordinal))
                .Distinct().Take(4).ToList();
            var picked = errors.Count > 0 ? errors : lines.Skip(Math.Max(0, lines.Count - 3)).ToList();
            var text = string.Join(" ⏎ ", picked.Select(l => l.Length > 300 ? l.Substring(0, 300) + "…" : l));
            return " Output: " + text;
        }

        /// <summary>
        /// `dotnet build` / `dotnet test` in the workspace root need a solution or project file there. The
        /// generator often puts the app and test projects in sub-folders with no root solution, which fails
        /// at once (MSB1003) in the runner and on the candidate's machine. Adds a classic .sln listing every
        /// .csproj (readable by the .NET 8 SDK), marked read-only. Returns the added path, or null.
        /// </summary>
        public static string EnsureDotnetSolution(CodeProjectGeneration g)
        {
            if (g == null || !(g.BuildCommand ?? "").TrimStart().StartsWith("dotnet", StringComparison.OrdinalIgnoreCase)) return null;
            var paths = g.ProjectFiles.Where(f => !string.IsNullOrWhiteSpace(f?.Path)).Select(f => NormalizePath(f.Path)).ToList();
            if (paths.Any(p => !p.Contains('/') && (p.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)
                || p.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))))
                return null;
            var projects = paths.Where(p => p.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)).OrderBy(p => p, StringComparer.Ordinal).ToList();
            if (projects.Count == 0) return null;

            static string SlnGuid(string seed)
            {
                var hash = System.Security.Cryptography.MD5.HashData(Encoding.UTF8.GetBytes(seed));
                return new System.Guid(hash).ToString("B").ToUpperInvariant();
            }
            const string csharpProjectType = "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}";
            var sb = new StringBuilder();
            sb.Append("\r\nMicrosoft Visual Studio Solution File, Format Version 12.00\r\n# Visual Studio Version 17\r\n");
            sb.Append("VisualStudioVersion = 17.0.31903.59\r\nMinimumVisualStudioVersion = 10.0.40219.1\r\n");
            foreach (var p in projects)
            {
                var name = Path.GetFileNameWithoutExtension(p);
                sb.Append($"Project(\"{csharpProjectType}\") = \"{name}\", \"{p.Replace('/', '\\')}\", \"{SlnGuid(p)}\"\r\nEndProject\r\n");
            }
            sb.Append("Global\r\n\tGlobalSection(SolutionConfigurationPlatforms) = preSolution\r\n\t\tDebug|Any CPU = Debug|Any CPU\r\n\t\tRelease|Any CPU = Release|Any CPU\r\n\tEndGlobalSection\r\n");
            sb.Append("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution\r\n");
            foreach (var p in projects)
            {
                var id = SlnGuid(p);
                foreach (var cfg in new[] { "Debug", "Release" })
                    sb.Append($"\t\t{id}.{cfg}|Any CPU.ActiveCfg = {cfg}|Any CPU\r\n\t\t{id}.{cfg}|Any CPU.Build.0 = {cfg}|Any CPU\r\n");
            }
            sb.Append("\tEndGlobalSection\r\nEndGlobal\r\n");

            const string slnPath = "Workspace.sln";
            g.ProjectFiles.Add(new CodeProjectFile { Path = slnPath, Content = sb.ToString() });
            g.ReadOnlyPaths ??= new List<string>();
            if (!g.ReadOnlyPaths.Contains(slnPath, StringComparer.OrdinalIgnoreCase)) g.ReadOnlyPaths.Add(slnPath);
            return slnPath;
        }

        public const string DotnetOfflineNuGetConfig =
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<configuration>\r\n  <!-- Offline: only the packages pre-loaded on the machine. -->\r\n" +
            "  <packageSources>\r\n    <clear />\r\n  </packageSources>\r\n</configuration>\r\n";

        public const string DotnetOfflineBuildProps =
            "<Project>\r\n  <PropertyGroup>\r\n    <!-- The vulnerability audit downloads data from nuget.org; there is no internet here. -->\r\n" +
            "    <NuGetAudit>false</NuGetAudit>\r\n" +
            "    <!-- System.Linq, System.Net.Http, System.Threading.Tasks... in every project, as in the .NET templates. -->\r\n" +
            "    <ImplicitUsings>enable</ImplicitUsings>\r\n  </PropertyGroup>\r\n</Project>\r\n";

        /// <summary>Properties every .NET workspace needs here, enforced even when the project brings its own Directory.Build.props.</summary>
        private static readonly (string Name, string Value)[] RequiredBuildProperties = { ("NuGetAudit", "false"), ("ImplicitUsings", "enable") };

        /// <summary>
        /// The runner and the candidate's machine have no internet. Since .NET 8, every restore of a
        /// project with packages runs a NuGet vulnerability audit that contacts nuget.org, which hangs
        /// until the build times out. A root NuGet.Config with no sources and a Directory.Build.props
        /// turning the audit off keep restore to the pre-loaded packages. Existing files are left alone.
        /// Returns the paths added.
        /// </summary>
        public static List<string> EnsureDotnetOffline(CodeProjectGeneration g)
        {
            var added = new List<string>();
            if (g == null || !(g.BuildCommand ?? "").TrimStart().StartsWith("dotnet", StringComparison.OrdinalIgnoreCase)) return added;
            var paths = new HashSet<string>(g.ProjectFiles.Where(f => !string.IsNullOrWhiteSpace(f?.Path)).Select(f => NormalizePath(f.Path)), StringComparer.OrdinalIgnoreCase);
            g.ReadOnlyPaths ??= new List<string>();
            foreach (var (path, content) in new[] { ("NuGet.Config", DotnetOfflineNuGetConfig), ("Directory.Build.props", DotnetOfflineBuildProps) })
            {
                if (paths.Contains(path)) continue;
                g.ProjectFiles.Add(new CodeProjectFile { Path = path, Content = content });
                if (!g.ReadOnlyPaths.Contains(path, StringComparer.OrdinalIgnoreCase)) g.ReadOnlyPaths.Add(path);
                added.Add(path);
            }

            // A Directory.Build.props the project brought (or an older one of ours) still gets the required properties.
            var props = g.ProjectFiles.First(f => string.Equals(NormalizePath(f.Path), "Directory.Build.props", StringComparison.OrdinalIgnoreCase));
            try
            {
                var doc = System.Xml.Linq.XDocument.Parse(props.Content ?? "<Project />");
                var root = doc.Root;
                var missing = RequiredBuildProperties.Where(p => !root.Descendants(p.Name).Any()).ToList();
                if (missing.Count > 0)
                {
                    root.Add(new System.Xml.Linq.XElement("PropertyGroup", missing.Select(p => new System.Xml.Linq.XElement(p.Name, p.Value))));
                    props.Content = doc.ToString() + "\r\n";
                    if (!added.Contains("Directory.Build.props")) added.Add("Directory.Build.props");
                }
            }
            catch (System.Xml.XmlException)
            {
                props.Content = DotnetOfflineBuildProps;   // unreadable: replace with ours
            }
            return added;
        }

        /// <summary>
        /// .NET counterpart of <see cref="PinNodeDependencies"/>: every PackageReference the image pre-loads
        /// gets its exact version, and an xUnit test project missing the test SDK or runner (without them
        /// `dotnet test` finds no tests) gets them. Starter and reference .csproj files. Returns the changes.
        /// </summary>
        public static List<string> PinNuGetPackages(CodeProjectGeneration g, IReadOnlyDictionary<string, string> pinned)
        {
            var changed = new List<string>();
            if (g == null || pinned == null || pinned.Count == 0) return changed;
            foreach (var f in g.ProjectFiles.Concat(g.ReferenceSolution))
            {
                if (f?.Path == null || f.Content == null || !f.Path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)) continue;
                System.Xml.Linq.XDocument doc;
                try { doc = System.Xml.Linq.XDocument.Parse(f.Content); }
                catch (System.Xml.XmlException) { continue; }   // validation reports it
                var refs = doc.Root.Descendants().Where(e => e.Name.LocalName == "PackageReference").ToList();
                var touched = false;
                foreach (var r in refs)
                {
                    var id = (string)r.Attribute("Include");
                    if (id == null || !pinned.TryGetValue(id, out var exact)) continue;
                    var child = r.Elements().FirstOrDefault(e => e.Name.LocalName == "Version");
                    if ((string)r.Attribute("Version") == exact && child == null) continue;
                    child?.Remove();
                    r.SetAttributeValue("Version", exact);
                    touched = true;
                    changed.Add($"{NormalizePath(f.Path)}: {id} {exact}");
                }
                var ids = new HashSet<string>(refs.Select(r => (string)r.Attribute("Include")).Where(i => i != null), StringComparer.OrdinalIgnoreCase);
                if (ids.Contains("xunit"))
                {
                    var add = new[] { "Microsoft.NET.Test.Sdk", "xunit.runner.visualstudio" }.Where(p => !ids.Contains(p) && pinned.ContainsKey(p)).ToList();
                    if (add.Count > 0)
                    {
                        doc.Root.Add(new System.Xml.Linq.XElement("ItemGroup", add.Select(p =>
                            new System.Xml.Linq.XElement("PackageReference", new System.Xml.Linq.XAttribute("Include", p), new System.Xml.Linq.XAttribute("Version", pinned[p])))));
                        touched = true;
                        changed.AddRange(add.Select(p => $"{NormalizePath(f.Path)}: added {p}"));
                    }
                }
                if (touched) f.Content = doc.ToString() + "\r\n";
            }
            return changed;
        }

        /// <summary>
        /// Integration tests use WebApplicationFactory&lt;Program&gt;, but a minimal-API Program.cs (top-level
        /// statements) compiles Program as internal, so the test project fails with CS0122. Microsoft's
        /// documented fix is "public partial class Program { }" at the end of Program.cs; it is added to
        /// the starter and reference copies that do not declare the class themselves. Returns the paths changed.
        /// </summary>
        public static List<string> EnsureProgramVisibleToTests(CodeProjectGeneration g)
        {
            var changed = new List<string>();
            if (g == null || !(g.BuildCommand ?? "").TrimStart().StartsWith("dotnet", StringComparison.OrdinalIgnoreCase)) return changed;
            var all = g.ProjectFiles.Concat(g.HiddenTests).Concat(g.ReferenceSolution).Where(f => f?.Content != null);
            if (!all.Any(f => Regex.IsMatch(f.Content, @"WebApplicationFactory\s*<\s*Program\s*>"))) return changed;

            foreach (var f in g.ProjectFiles.Concat(g.ReferenceSolution))
            {
                if (f?.Path == null || f.Content == null) continue;
                if (!string.Equals(Path.GetFileName(NormalizePath(f.Path)), "Program.cs", StringComparison.OrdinalIgnoreCase)) continue;
                if (Regex.IsMatch(f.Content, @"\bclass\s+Program\b")) continue;   // declares the class itself
                f.Content = f.Content.TrimEnd() + "\n\n// Lets the integration tests' WebApplicationFactory<Program> see the app.\npublic partial class Program { }\n";
                changed.Add(NormalizePath(f.Path));
            }
            return changed;
        }

        /// <summary>
        /// Builds are offline. With a range such as "^5.4.10", npm picks the newest version it has metadata
        /// for (e.g. 5.4.21), which was never downloaded, and fails with ENOTCACHED. Every dependency the
        /// image pre-loads is rewritten to its exact pre-loaded version, in the starter and the reference
        /// solution. Packages the image does not have are left for validation to report. Returns the
        /// "file: package" entries changed.
        /// </summary>
        public static List<string> PinNodeDependencies(CodeProjectGeneration g, IReadOnlyDictionary<string, string> pinned)
        {
            var changed = new List<string>();
            if (g == null || pinned == null || pinned.Count == 0) return changed;
            foreach (var f in g.ProjectFiles.Concat(g.ReferenceSolution))
            {
                if (f?.Path == null || f.Content == null) continue;
                if (!string.Equals(Path.GetFileName(NormalizePath(f.Path)), "package.json", StringComparison.OrdinalIgnoreCase)) continue;
                System.Text.Json.Nodes.JsonObject root;
                try { root = System.Text.Json.Nodes.JsonNode.Parse(f.Content) as System.Text.Json.Nodes.JsonObject; }
                catch (JsonException) { continue; }   // validation will report the broken file
                if (root == null) continue;
                var touched = false;
                foreach (var section in new[] { "dependencies", "devDependencies" })
                {
                    if (root[section] is not System.Text.Json.Nodes.JsonObject deps) continue;
                    foreach (var name in deps.Select(p => p.Key).ToList())
                    {
                        if (!pinned.TryGetValue(name, out var exact)) continue;
                        if (deps[name]?.GetValueKind() == JsonValueKind.String && deps[name].GetValue<string>() == exact) continue;
                        deps[name] = exact;
                        touched = true;
                        changed.Add($"{NormalizePath(f.Path)}: {name}@{exact}");
                    }
                }
                if (touched) f.Content = root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n";
            }
            return changed;
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
