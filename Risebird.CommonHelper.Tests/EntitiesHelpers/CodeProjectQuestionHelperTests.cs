using Risebird.Entities.ViewModel;

namespace Risebird.CommonHelper.Tests.EntitiesHelpers
{
    public class DmIvQuestionRubricHelperStackImageTests
    {
        private static StackImage Image(string code, bool active = true, string toolchain = "JDK 21, Maven") =>
            new() { Code = code, Toolchain = toolchain, IsActive = active };

        [Fact]
        public void Prompt_lists_only_active_images_with_their_toolchain()
        {
            var section = DmIvQuestionRubricHelper.BuildStackImagePromptSection(new[]
            {
                Image("rb-java-win"), Image("rb-node-win", active: false, toolchain: "Node 20")
            });

            Assert.StartsWith("Stack images (use these exact codes only):", section);
            Assert.Contains("- rb-java-win | JDK 21, Maven", section);
            Assert.DoesNotContain("rb-node-win", section);
            Assert.EndsWith(DmIvQuestionRubricHelper.StackImageInstruction, section);
        }

        [Fact]
        public void Prompt_still_has_header_and_instruction_when_empty()
        {
            var section = DmIvQuestionRubricHelper.BuildStackImagePromptSection(null);
            Assert.Contains("Stack images", section);
            Assert.DoesNotContain("- ", section);
        }

        [Fact]
        public void Valid_set_excludes_inactive_and_blank_codes()
        {
            var set = DmIvQuestionRubricHelper.BuildValidStackImageSet(new[]
            {
                Image(" rb-java-win "), Image("rb-node-win", active: false), Image(""), Image(null), null
            });

            Assert.Single(set);
            Assert.Contains("rb-java-win", set);
        }

        [Theory]
        [InlineData("rb-java-win", true)]
        [InlineData("RB-JAVA-WIN", true)]
        [InlineData("  rb-java-win ", true)]
        [InlineData("rb-kotlin-win", false)]   // an image nobody baked
        [InlineData("rb-java", false)]         // no partial match
        [InlineData("", false)]
        [InlineData(null, false)]
        public void Match_is_case_insensitive_exact(string returned, bool expected)
        {
            var valid = DmIvQuestionRubricHelper.BuildValidStackImageSet(new[] { Image("rb-java-win") });

            Assert.Equal(expected, DmIvQuestionRubricHelper.TryMatchStackImage(returned, valid, out var canonical));
            Assert.Equal(expected ? "rb-java-win" : null, canonical);
        }
    }

    public class CodeProjectQuestionHelperParseTests
    {
        private const string Contract = @"Here you go:
```json
{
  ""skill_or_competency"": ""Spring Boot"",
  ""secondary_skills"": [""REST API Design""],
  ""question_text"": ""Fix the stock service."",
  ""question_gist"": ""Fix a small bug."",
  ""expected_answer"": ""Validate in the service."",
  ""time_limit_minutes"": 15,
  ""language"": ""java"",
  ""framework"": ""spring-boot"",
  ""stack_image_code"": ""rb-java-win"",
  ""project_files"": [ { ""path"": ""src/Main.java"", ""content"": ""class Main {}"" } ],
  ""entry_point_file"": ""src/Main.java"",
  ""editable_files"": [""src/Main.java""],
  ""read_only_paths"": [""src/test/**""],
  ""build_command"": ""mvn -q -o compile"",
  ""test_command"": ""mvn -q -o test"",
  ""has_webserver"": true,
  ""ports"": [8080],
  ""requirements"": [ { ""id"": ""R1"", ""text"": ""Rejects negatives"", ""test_name"": ""r1_rejects"", ""weight"": 1.0 } ],
  ""estimated_minutes"": ""11"",
  ""hidden_tests"": [ { ""path"": ""src/test/HiddenTest.java"", ""content"": ""..."" } ],
  ""reference_solution"": [ { ""path"": ""src/Main.java"", ""content"": ""class Main { }"" } ],
  ""seeded_defects"": [ { ""id"": ""D1"", ""file"": ""src/Main.java"", ""fails"": [""R1""], ""explanation"": ""x"" } ]
}
```";

        [Fact]
        public void Parses_all_three_tiers_through_markdown_fences()
        {
            var g = CodeProjectQuestionHelper.Parse(Contract, out var error);

            Assert.Null(error);
            Assert.Equal("Spring Boot", g.SkillOrCompetency);
            Assert.Equal(new[] { "REST API Design" }, g.SecondarySkills);
            Assert.Equal("rb-java-win", g.StackImageCode);
            Assert.Single(g.ProjectFiles);
            Assert.True(g.HasWebserver);
            Assert.Equal(new[] { 8080 }, g.Ports);
            Assert.Equal(11, g.EstimatedMinutes);           // numeric strings are accepted
            Assert.Equal(1.0m, g.Requirements[0].Weight);
            Assert.Single(g.HiddenTests);
            Assert.Single(g.ReferenceSolution);
            Assert.Equal(new[] { "R1" }, g.SeededDefects[0].Fails);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("no json here")]
        [InlineData("{ not: valid json ")]
        public void Returns_null_with_an_error_for_unusable_responses(string raw)
        {
            Assert.Null(CodeProjectQuestionHelper.Parse(raw, out var error));
            Assert.False(string.IsNullOrEmpty(error));
        }

        [Fact]
        public void Defaults_the_time_limit_when_missing()
        {
            var g = CodeProjectQuestionHelper.Parse("{\"question_text\":\"x\"}", out _);
            Assert.Equal(CodeProjectQuestionHelper.DefaultTimeLimitMinutes, g.TimeLimitMinutes);
        }
    }

    public class CodeProjectQuestionHelperScopeTests
    {
        private static CodeProjectGeneration Valid() => new()
        {
            QuestionText = "Fix it",
            BuildCommand = "mvn -q -o compile",
            TestCommand = "mvn -q -o test",
            ProjectFiles = new() { new() { Path = "src/Service.java", Content = "" }, new() { Path = "src/test/SmokeTest.java", Content = "" } },
            EditableFiles = new() { "src/Service.java" },
            ReadOnlyPaths = new() { "src/test/**" },
            Requirements = new()
            {
                new() { Id = "R1", TestName = "r1", Weight = 0.34m },
                new() { Id = "R2", TestName = "r2", Weight = 0.33m },
                new() { Id = "R3", TestName = "r3", Weight = 0.33m }
            },
            EstimatedMinutes = 11,
            HiddenTests = new() { new() { Path = "src/test/HiddenTest.java", Content = "" } },
            ReferenceSolution = new() { new() { Path = "src/Service.java", Content = "" } }
        };

        [Fact]
        public void A_well_scoped_project_passes()
        {
            Assert.Empty(CodeProjectQuestionHelper.ValidateScope(Valid(), 15));
        }

        [Fact]
        public void Rejects_more_than_eight_files()
        {
            var g = Valid();
            for (var i = 0; i < 7; i++) g.ProjectFiles.Add(new() { Path = $"src/Extra{i}.java", Content = "" });
            Assert.Contains(CodeProjectQuestionHelper.ValidateScope(g, 15), e => e.Contains("limit is 8"));
        }

        [Fact]
        public void Rejects_an_edit_surface_outside_one_to_two_files()
        {
            var g = Valid();
            g.ProjectFiles.Add(new() { Path = "src/A.java", Content = "" });
            g.ProjectFiles.Add(new() { Path = "src/B.java", Content = "" });
            g.EditableFiles = new() { "src/Service.java", "src/A.java", "src/B.java" };
            Assert.Contains(CodeProjectQuestionHelper.ValidateScope(g, 15), e => e.Contains("Candidate edits 3 files"));
        }

        [Fact]
        public void Falls_back_to_the_entry_point_when_editable_files_are_missing()
        {
            var g = Valid();
            g.EditableFiles = new();
            g.EntryPointFile = "./src/Service.java";
            Assert.Equal(new[] { "src/Service.java" }, CodeProjectQuestionHelper.ResolveEditableFiles(g));
            Assert.Empty(CodeProjectQuestionHelper.ValidateScope(g, 15));
        }

        [Fact]
        public void Rejects_an_editable_file_under_a_read_only_path()
        {
            var g = Valid();
            g.EditableFiles = new() { "src/test/SmokeTest.java" };
            Assert.Contains(CodeProjectQuestionHelper.ValidateScope(g, 15), e => e.Contains("also marked read-only"));
        }

        [Theory]
        [InlineData(2)]
        [InlineData(6)]
        public void Rejects_requirement_counts_outside_three_to_five(int count)
        {
            var g = Valid();
            g.Requirements = Enumerable.Range(1, count).Select(i => new CodeProjectRequirement { Id = "R" + i, TestName = "r" + i, Weight = 1m / count }).ToList();
            Assert.Contains(CodeProjectQuestionHelper.ValidateScope(g, 15), e => e.Contains("requirements; it must be 3–5"));
        }

        [Fact]
        public void Rejects_weights_that_do_not_sum_to_one()
        {
            var g = Valid();
            g.Requirements[0].Weight = 0.9m;
            Assert.Contains(CodeProjectQuestionHelper.ValidateScope(g, 15), e => e.Contains("sum to 1"));
        }

        [Theory]
        [InlineData(13, 15)]   // over the 12-minute cap
        [InlineData(11, 10)]   // over the slot itself
        public void Blocks_over_scoped_projects(int estimated, int limit)
        {
            var g = Valid();
            g.EstimatedMinutes = estimated;
            Assert.Contains(CodeProjectQuestionHelper.ValidateScope(g, limit), e => e.Contains("Estimated effort"));
        }

        [Fact]
        public void Rejects_a_hidden_test_shipped_in_the_candidate_project()
        {
            var g = Valid();
            g.HiddenTests.Add(new() { Path = "src/test/SmokeTest.java", Content = "" });
            Assert.Contains(CodeProjectQuestionHelper.ValidateScope(g, 15), e => e.Contains("also appears in project_files"));
        }

        [Fact]
        public void Strips_a_hidden_test_from_the_candidate_project_so_it_passes_scope()
        {
            var g = Valid();
            g.HiddenTests.Add(new() { Path = "src/test/SmokeTest.java", Content = "secret" });
            g.ReadOnlyPaths.Add("src\\test\\SmokeTest.java");

            var removed = CodeProjectQuestionHelper.StripHiddenTestsFromProject(g);

            Assert.Equal(new[] { "src/test/SmokeTest.java" }, removed);
            Assert.DoesNotContain(g.ProjectFiles, f => f.Path == "src/test/SmokeTest.java");
            Assert.DoesNotContain(g.ReadOnlyPaths, p => p.Contains("SmokeTest"));
            Assert.Contains(g.HiddenTests, t => t.Path == "src/test/SmokeTest.java");
            Assert.DoesNotContain(CodeProjectQuestionHelper.ValidateScope(g, 15), e => e.Contains("also appears in project_files"));
        }

        [Fact]
        public void Requires_hidden_tests_and_a_reference_solution()
        {
            var g = Valid();
            g.HiddenTests.Clear();
            g.ReferenceSolution.Clear();
            var errors = CodeProjectQuestionHelper.ValidateScope(g, 15);
            Assert.Contains(errors, e => e.Contains("hidden_tests is empty"));
            Assert.Contains(errors, e => e.Contains("reference_solution is empty"));
        }

        [Fact]
        public void Sanitized_payload_never_contains_tier_three_or_file_contents()
        {
            var g = Valid();
            g.HiddenTests[0].Content = "SECRET_HIDDEN_TEST";
            g.ReferenceSolution[0].Content = "SECRET_REFERENCE";
            g.ProjectFiles[0].Content = "STARTER_CONTENT";
            var payload = CodeProjectQuestionHelper.BuildSanitizedPayload(g);

            Assert.DoesNotContain("SECRET_HIDDEN_TEST", payload);
            Assert.DoesNotContain("SECRET_REFERENCE", payload);
            Assert.DoesNotContain("STARTER_CONTENT", payload);
            Assert.DoesNotContain("hidden_tests", payload);
            Assert.Contains("src/Service.java", payload);
        }
    }

    public class CodeProjectQuestionHelperGlobTests
    {
        [Theory]
        [InlineData("src/test/Foo.java", "src/test/**", true)]
        [InlineData("src/test/deep/Foo.java", "src/test/**", true)]
        [InlineData("src/main/Foo.java", "src/test/**", false)]
        [InlineData("src\\test\\Foo.java", "src/test/**", true)]
        [InlineData("pom.xml", "*.xml", true)]
        [InlineData("config/app.xml", "*.xml", false)]
        [InlineData("config/app.xml", "**/*.xml", true)]
        [InlineData("app.xml", "**/*.xml", true)]
        [InlineData("a/b.java", "a/?.java", true)]
        public void Matches_globs(string path, string glob, bool expected) =>
            Assert.Equal(expected, CodeProjectQuestionHelper.GlobMatch(path, glob));
    }

    public class CodeProjectQuestionHelperValidationTests
    {
        private static CodeProjectGeneration Gen(int reqs = 4, int estimated = 10) => new()
        {
            BuildCommand = "mvn -q -o compile",
            StackImageCode = "rb-java-win",
            Framework = "spring-boot",
            ProjectFiles = new() { new() { Path = "src/A.java" } },
            Requirements = Enumerable.Range(1, reqs).Select(i => new CodeProjectRequirement { Id = "R" + i, TestName = "r" + i, Weight = 1m / reqs }).ToList(),
            EstimatedMinutes = estimated
        };

        private static CodeProjectRunResult Run(int passing, int total = 4, int exit = 0, double wall = 9.5) => new()
        {
            Completed = true,
            BuildExitCode = exit,
            WallSeconds = wall,
            Tests = Enumerable.Range(1, total).Select(i => new CodeProjectTestResult { Name = "HiddenTest.r" + i, Passed = i <= passing }).ToList()
        };

        [Fact]
        public void Passes_when_solvable_not_presolved_and_within_budget()
        {
            var report = CodeProjectQuestionHelper.EvaluateValidation(Gen(), 15, Run(1), Run(4));

            Assert.Null(report.FailureReason);
            Assert.All(report.Steps, s => Assert.Equal("ok", s.State));
            Assert.Equal(new[] { "generate", "build", "reference", "starter", "budget" }, report.Steps.Select(s => s.Key));
        }

        [Fact]
        public void Fails_when_the_starter_does_not_build_and_marks_later_steps_not_reached()
        {
            var report = CodeProjectQuestionHelper.EvaluateValidation(Gen(), 15, Run(0, exit: 1), null);

            Assert.Contains("exit 1", report.FailureReason);
            Assert.Equal("failed", report.Steps.Single(s => s.Key == "build").State);
            Assert.All(report.Steps.SkipWhile(s => s.Key != "reference"), s => Assert.Equal("pending", s.State));
            Assert.Equal(CodeProjectQuestionHelper.PipelineKeys.Count, report.Steps.Count);
        }

        [Fact]
        public void Fails_when_the_reference_solution_does_not_pass_everything()
        {
            var report = CodeProjectQuestionHelper.EvaluateValidation(Gen(), 15, Run(1), Run(3));
            Assert.Contains("not solvable", report.FailureReason);
        }

        [Theory]
        [InlineData(2)]   // half passing counts as pre-solved
        [InlineData(4)]
        public void Fails_when_the_starter_is_already_mostly_solved(int starterPassing)
        {
            var report = CodeProjectQuestionHelper.EvaluateValidation(Gen(), 15, Run(starterPassing), Run(4));
            Assert.Contains("pre-solved", report.FailureReason);
        }

        [Fact]
        public void Fails_when_a_requirement_has_no_hidden_test()
        {
            var g = Gen();
            g.Requirements[3].TestName = "missing_test";
            var report = CodeProjectQuestionHelper.EvaluateValidation(g, 15, Run(1), Run(4));
            Assert.Contains("missing_test", report.FailureReason);
        }

        [Fact]
        public void Fails_when_build_and_test_take_twenty_seconds_or_more()
        {
            var report = CodeProjectQuestionHelper.EvaluateValidation(Gen(), 15, Run(1), Run(4, wall: 20.0));
            Assert.Contains("under 20s", report.FailureReason);
        }

        [Fact]
        public void Fails_when_the_runner_did_not_complete()
        {
            var report = CodeProjectQuestionHelper.EvaluateValidation(Gen(), 15,
                new CodeProjectRunResult { Completed = false, Error = "runner down" }, null);
            Assert.Equal("runner down", report.FailureReason);
        }
    }

    public class CodeProjectQuestionHelperGradingTests
    {
        private static readonly List<CodeProjectFile> Starter = new()
        {
            new() { Path = "src/Service.java", Content = "starter" },
            new() { Path = "src/test/SmokeTest.java", Content = "visible test" }
        };

        [Fact]
        public void Grading_workspace_resets_read_only_files_and_adds_hidden_tests()
        {
            var candidate = new List<CodeProjectFile>
            {
                new() { Path = "src/Service.java", Content = "candidate" },
                new() { Path = "src/test/SmokeTest.java", Content = "tampered" },   // must be ignored
                new() { Path = "src/Helper.java", Content = "new file" }
            };
            var hidden = new List<CodeProjectFile> { new() { Path = "src/test/HiddenTest.java", Content = "hidden" } };

            var ws = CodeProjectQuestionHelper.BuildGradingWorkspace(Starter, candidate, new[] { "src/test/**" }, hidden)
                .ToDictionary(f => f.Path, f => f.Content);

            Assert.Equal("candidate", ws["src/Service.java"]);
            Assert.Equal("visible test", ws["src/test/SmokeTest.java"]);
            Assert.Equal("new file", ws["src/Helper.java"]);
            Assert.Equal("hidden", ws["src/test/HiddenTest.java"]);
        }

        [Fact]
        public void Reference_workspace_overlays_the_solution_on_the_starter()
        {
            var ws = CodeProjectQuestionHelper.BuildReferenceWorkspace(Starter,
                new List<CodeProjectFile> { new() { Path = "./src/Service.java", Content = "solved" } }, null)
                .ToDictionary(f => f.Path, f => f.Content);
            Assert.Equal("solved", ws["src/Service.java"]);
            Assert.Equal(2, ws.Count);
        }

        [Fact]
        public void Scores_by_requirement_weight()
        {
            var reqs = new List<CodeProjectRequirement>
            {
                new() { Id = "R1", TestName = "r1", Weight = 0.5m },
                new() { Id = "R2", TestName = "r2", Weight = 0.3m },
                new() { Id = "R3", TestName = "r3", Weight = 0.2m }
            };
            var run = new CodeProjectRunResult
            {
                Completed = true,
                BuildExitCode = 0,
                Tests = new()
                {
                    new() { Name = "Suite#r1", Passed = true },
                    new() { Name = "Suite.r2", Passed = false, Message = "expected 400, was 200" },
                    new() { Name = "r3", Passed = true }
                }
            };

            var grade = CodeProjectQuestionHelper.Grade(reqs, run);

            Assert.Equal(70.0m, grade.CorrectnessScore);
            Assert.Equal("expected 400, was 200", grade.Requirements.Single(r => r.Id == "R2").Message);
            Assert.True(grade.BuildSucceeded);
        }

        [Fact]
        public void A_build_failure_scores_zero_on_every_requirement()
        {
            var reqs = new List<CodeProjectRequirement> { new() { Id = "R1", TestName = "r1", Weight = 1m } };
            var grade = CodeProjectQuestionHelper.Grade(reqs, new CodeProjectRunResult
            {
                Completed = true,
                BuildExitCode = 1,
                Tests = new() { new() { Name = "r1", Passed = true } }
            });

            Assert.Equal(0m, grade.CorrectnessScore);
            Assert.All(grade.Requirements, r => Assert.False(r.Passed));
            Assert.False(grade.BuildSucceeded);
        }

        [Fact]
        public void Test_lookup_does_not_match_a_different_test_by_suffix()
        {
            var tests = new[] { new CodeProjectTestResult { Name = "Suite.not_r1" } };
            Assert.Null(CodeProjectQuestionHelper.FindTest(tests, "r1"));
        }
    }

    public class CodeProjectDotnetSolutionTests
    {
        private static CodeProjectGeneration TwoProjects() => new CodeProjectGeneration
        {
            BuildCommand = "dotnet build -nologo",
            ProjectFiles = new List<CodeProjectFile>
            {
                new() { Path = "UppercaseApi/UppercaseApi.csproj", Content = "<Project/>" },
                new() { Path = "UppercaseApi/Program.cs", Content = "" },
                new() { Path = "UppercaseApi.Tests/UppercaseApi.Tests.csproj", Content = "<Project/>" }
            }
        };

        [Fact]
        public void Adds_a_root_solution_listing_every_project_when_none_exists()
        {
            var g = TwoProjects();
            Assert.Equal("Workspace.sln", CodeProjectQuestionHelper.EnsureDotnetSolution(g));
            var sln = g.ProjectFiles.Single(f => f.Path == "Workspace.sln").Content;
            Assert.Contains(@"""UppercaseApi"", ""UppercaseApi\UppercaseApi.csproj""", sln);
            Assert.Contains(@"""UppercaseApi.Tests"", ""UppercaseApi.Tests\UppercaseApi.Tests.csproj""", sln);
            Assert.Contains("Workspace.sln", g.ReadOnlyPaths);
            // Deterministic: regenerating the same project gives the same file.
            var again = TwoProjects();
            CodeProjectQuestionHelper.EnsureDotnetSolution(again);
            Assert.Equal(sln, again.ProjectFiles.Single(f => f.Path == "Workspace.sln").Content);
        }

        [Theory]
        [InlineData("App.sln")]
        [InlineData("App.csproj")]
        public void Leaves_projects_that_already_build_from_the_root(string rootFile)
        {
            var g = TwoProjects();
            g.ProjectFiles.Add(new() { Path = rootFile, Content = "" });
            Assert.Null(CodeProjectQuestionHelper.EnsureDotnetSolution(g));
        }

        [Fact]
        public void Ignores_other_stacks()
        {
            var g = TwoProjects();
            g.BuildCommand = "mvn -o -q package";
            Assert.Null(CodeProjectQuestionHelper.EnsureDotnetSolution(g));
        }

        [Fact]
        public void Dotnet_workspaces_restore_offline_only()
        {
            var g = TwoProjects();
            var added = CodeProjectQuestionHelper.EnsureDotnetOffline(g);
            Assert.Equal(new[] { "NuGet.Config", "Directory.Build.props" }, added);
            Assert.Contains("<clear />", g.ProjectFiles.Single(f => f.Path == "NuGet.Config").Content);
            Assert.Contains("<NuGetAudit>false</NuGetAudit>", g.ProjectFiles.Single(f => f.Path == "Directory.Build.props").Content);
            Assert.Contains("NuGet.Config", g.ReadOnlyPaths);
            // Running it again, or on a project that already has its own files, adds nothing.
            Assert.Empty(CodeProjectQuestionHelper.EnsureDotnetOffline(g));
            var other = TwoProjects();
            other.BuildCommand = "npm install --offline";
            Assert.Empty(CodeProjectQuestionHelper.EnsureDotnetOffline(other));
        }

        [Fact]
        public void Makes_a_minimal_api_Program_visible_to_WebApplicationFactory_tests()
        {
            var g = TwoProjects();
            g.ProjectFiles.Single(f => f.Path == "UppercaseApi/Program.cs").Content = "var app = WebApplication.Create(args);\napp.Run();";
            g.ReferenceSolution.Add(new() { Path = "UppercaseApi/Program.cs", Content = "var app = WebApplication.Create(args);\napp.Run();" });
            g.HiddenTests.Add(new() { Path = "UppercaseApi.Tests/Hidden/T.cs", Content = "class T : IClassFixture<WebApplicationFactory<Program>> { }" });

            var changed = CodeProjectQuestionHelper.EnsureProgramVisibleToTests(g);

            Assert.Equal(2, changed.Count);
            Assert.EndsWith("public partial class Program { }\n", g.ProjectFiles.Single(f => f.Path == "UppercaseApi/Program.cs").Content);
            Assert.EndsWith("public partial class Program { }\n", g.ReferenceSolution.Single().Content);
            Assert.Empty(CodeProjectQuestionHelper.EnsureProgramVisibleToTests(g));   // already declared now
        }

        [Fact]
        public void Leaves_Program_alone_when_no_test_uses_WebApplicationFactory()
        {
            var g = TwoProjects();
            g.ProjectFiles.Single(f => f.Path == "UppercaseApi/Program.cs").Content = "var app = WebApplication.Create(args);";
            Assert.Empty(CodeProjectQuestionHelper.EnsureProgramVisibleToTests(g));
        }

        [Fact]
        public void Build_failures_show_the_error_lines()
        {
            var excerpt = CodeProjectQuestionHelper.BuildErrorExcerpt("Welcome\nMSBUILD : error MSB1003: Specify a project or solution file.\n");
            Assert.Contains("MSB1003", excerpt);
            Assert.Equal("", CodeProjectQuestionHelper.BuildErrorExcerpt(null));
        }
    }
}
