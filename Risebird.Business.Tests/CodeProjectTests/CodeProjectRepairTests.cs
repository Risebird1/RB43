using Moq;
using Risebird.Business;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System.Text.Json;
using Xunit;

namespace Risebird.Business.Tests.CodeProjectTests
{
    /// <summary>
    /// Validation with automatic repair: the project's own failures go back to the AI with the real
    /// output, the repaired files replace the stored ones, and infrastructure failures are never "repaired".
    /// </summary>
    public class CodeProjectRepairTests
    {
        private const int QuestionId = 42;
        private static readonly JsonSerializerOptions Json = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        private static string Requirements() => JsonSerializer.Serialize(new[]
        {
            new CodeProjectRequirement { Id = "r1", Text = "adds", TestName = "Adds", Weight = 0.34m },
            new CodeProjectRequirement { Id = "r2", Text = "subtracts", TestName = "Subtracts", Weight = 0.33m },
            new CodeProjectRequirement { Id = "r3", Text = "multiplies", TestName = "Multiplies", Weight = 0.33m }
        }, Json);

        private static CodeProjectDetails Details() => new CodeProjectDetails
        {
            ValidationStatus = CodeProjectValidationStatus.Validating,
            Comment = "Implement the calculator.",
            StackImageCode = "rb-dotnet-win",
            BuildCommand = "dotnet build -nologo",
            TestCommand = "dotnet test -nologo --logger trx --results-directory TestResults",
            RequirementsJson = Requirements(),
            EstimatedMinutes = 8,
            CodingTimeLimitInMinutes = 15,
            EntryPointFile = "App/Calc.cs",
            ReadOnlyPathsJson = "[\"Tests/Tests.csproj\"]",
            GeneratedPayload = "{\"editable_files\":[\"App/Calc.cs\"]}"
        };

        private static List<CodeProjectFile> Starter(string calc) => new List<CodeProjectFile>
        {
            new() { Path = "App/App.csproj", Content = "<Project/>" },
            new() { Path = "App/Calc.cs", Content = calc },
            new() { Path = "Tests/Tests.csproj", Content = "<Project/>" }
        };

        private static List<CodeProjectTestResult> Tests(bool pass) =>
            new[] { "Adds", "Subtracts", "Multiplies" }.Select(n => new CodeProjectTestResult { Name = "CalcTests." + n, Passed = pass }).ToList();

        /// <summary>A fake build container: "BROKEN" does not compile, "SOLVED" passes every test, anything else fails them.</summary>
        private static CodeProjectRunResult Run(CodeProjectRunRequest r)
        {
            var calc = r.Files.First(f => f.Path == "App/Calc.cs").Content;
            if (calc.Contains("BROKEN"))
                return new CodeProjectRunResult { Completed = true, BuildExitCode = 1, BuildOutput = "App/Calc.cs(3,1): error CS1002: ; expected", WallSeconds = 3 };
            return new CodeProjectRunResult { Completed = true, BuildExitCode = 0, TestExitCode = 0, Tests = Tests(calc.Contains("SOLVED")), WallSeconds = 4 };
        }

        private sealed class Harness
        {
            public readonly Dictionary<string, byte[]> Blobs = new Dictionary<string, byte[]>();
            public readonly List<(string Status, CodeProjectValidationReport Report)> Updates = new List<(string, CodeProjectValidationReport)>();
            public readonly Mock<IOpenAIResponsesHelper> OpenAI = new Mock<IOpenAIResponsesHelper>();
            public Func<CodeProjectRunRequest, CodeProjectRunResult> Runner = Run;
            public CodeProjectQuestionService Service;

            public Harness(string starterCalc)
            {
                Blobs["projects/q42/project.zip"] = CodeProjectQuestionService.Zip(Starter(starterCalc));
                Blobs["secrets/q42/secrets.json"] = JsonSerializer.SerializeToUtf8Bytes(new
                {
                    hiddenTests = new[] { new { path = "Tests/Hidden/CalcTests.cs", content = "tests" } },
                    referenceSolution = new[] { new { path = "App/Calc.cs", content = "SOLVED" } }
                }, Json);

                var repo = new Mock<ICodeProjectQuestionRepository>();
                repo.Setup(x => x.GetCodeProjectDetails(QuestionId)).ReturnsAsync(Details());
                repo.Setup(x => x.UpdateValidation(QuestionId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                    .Callback<int, string, string, string, string>((_, status, json, _, _) => Updates.Add((status, JsonSerializer.Deserialize<CodeProjectValidationReport>(json, Json))))
                    .ReturnsAsync(true);

                var blobs = new Mock<ICodeProjectBlobStore>();
                blobs.SetupGet(x => x.ProjectContainer).Returns("projects");
                blobs.SetupGet(x => x.SecretsContainer).Returns("secrets");
                blobs.Setup(x => x.DownloadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((string c, string n, CancellationToken _) => Blobs[c + "/" + n]);
                blobs.Setup(x => x.UploadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Callback<string, string, byte[], string, CancellationToken>((c, n, b, _, _) => Blobs[c + "/" + n] = b)
                    .ReturnsAsync("https://blob/x");

                var runner = new Mock<ICodeProjectRunner>();
                runner.SetupGet(x => x.IsConfigured).Returns(true);
                runner.Setup(x => x.RunAsync(It.IsAny<CodeProjectRunRequest>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((CodeProjectRunRequest r, CancellationToken _) => Runner(r));

                Service = new CodeProjectQuestionService(repo.Object, blobs.Object, runner.Object,
                    new Mock<ICodeProjectEnvironmentProvisioner>().Object, new Mock<IApplicationErrorLogger>().Object, OpenAI.Object);
            }

            public void RepairReturns(params string[] starterCalcs)
            {
                var seq = OpenAI.SetupSequence(x => x.CreateJsonWithInstructionsAsync(It.IsAny<string>(), It.IsAny<string>(), "RepairCodeProjectQuestion", It.IsAny<int?>()));
                foreach (var calc in starterCalcs) seq = seq.ReturnsAsync(Answer(calc));
            }

            private static string Answer(string starterCalc) => JsonSerializer.Serialize(new
            {
                project_files = Starter(starterCalc).Select(f => new { path = f.Path, content = f.Content }),
                hidden_tests = new[] { new { path = "Tests/Hidden/CalcTests.cs", content = "tests" } },
                reference_solution = new[] { new { path = "App/Calc.cs", content = "SOLVED" } },
                notes = "Added the missing semicolon."
            });
        }

        [Fact]
        public async Task A_compile_error_is_repaired_and_the_question_publishes()
        {
            var h = new Harness("BROKEN");
            h.RepairReturns("class Calc { /* starter */ }");

            await h.Service.ValidateAsync(QuestionId);

            var last = h.Updates.Last();
            Assert.Equal(CodeProjectValidationStatus.Passed, last.Status);
            Assert.Contains("passed after 1 automatic repair", last.Report.Steps.Single(s => s.Key == "publish").Detail);
            // The repaired starter replaced the stored one, so candidates get the code that was proved to build.
            var stored = CodeProjectQuestionService.Unzip(h.Blobs["projects/q42/project.zip"]);
            Assert.Equal("class Calc { /* starter */ }", stored.Single(f => f.Path == "App/Calc.cs").Content);
            // The AI was shown the compiler's own message.
            h.OpenAI.Verify(x => x.CreateJsonWithInstructionsAsync(It.IsAny<string>(),
                It.Is<string>(input => input.Contains("error CS1002")), "RepairCodeProjectQuestion", It.IsAny<int?>()), Times.Once);
            // While repairing, the modal showed it on the failing step.
            Assert.Contains(h.Updates, u => u.Report.Steps.Any(s => s.Key == "build" && s.State == "run" && (s.Detail ?? "").StartsWith("Automatic repair 1 of 2")));
        }

        [Fact]
        public async Task Gives_up_after_the_maximum_number_of_repairs()
        {
            var h = new Harness("BROKEN");
            h.RepairReturns("BROKEN again", "BROKEN once more");

            await h.Service.ValidateAsync(QuestionId);

            var last = h.Updates.Last();
            Assert.Equal(CodeProjectValidationStatus.Failed, last.Status);
            Assert.Contains($"after {CodeProjectQuestionHelper.MaxRepairRounds} automatic repairs", last.Report.FailureReason);
            h.OpenAI.Verify(x => x.CreateJsonWithInstructionsAsync(It.IsAny<string>(), It.IsAny<string>(), "RepairCodeProjectQuestion", It.IsAny<int?>()),
                Times.Exactly(CodeProjectQuestionHelper.MaxRepairRounds));
        }

        [Fact]
        public async Task A_repair_that_changes_nothing_stops_without_another_build()
        {
            var h = new Harness("BROKEN");
            h.RepairReturns("BROKEN");   // the same files it was given

            await h.Service.ValidateAsync(QuestionId);

            var last = h.Updates.Last();
            Assert.Equal(CodeProjectValidationStatus.Failed, last.Status);
            Assert.Contains("returned the files unchanged", last.Report.FailureReason);
            h.OpenAI.Verify(x => x.CreateJsonWithInstructionsAsync(It.IsAny<string>(), It.IsAny<string>(), "RepairCodeProjectQuestion", It.IsAny<int?>()), Times.Once);
        }

        [Fact]
        public async Task An_infrastructure_failure_is_not_sent_for_repair()
        {
            var h = new Harness("class Calc { }");
            h.Runner = _ => new CodeProjectRunResult { Completed = false, Error = "The runner did not complete." };

            await h.Service.ValidateAsync(QuestionId);

            Assert.Equal(CodeProjectValidationStatus.Failed, h.Updates.Last().Status);
            h.OpenAI.Verify(x => x.CreateJsonWithInstructionsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>()), Times.Never);
        }

        [Fact]
        public async Task A_project_that_passes_first_time_never_calls_the_AI()
        {
            var h = new Harness("class Calc { }");

            await h.Service.ValidateAsync(QuestionId);

            var last = h.Updates.Last();
            Assert.Equal(CodeProjectValidationStatus.Passed, last.Status);
            Assert.DoesNotContain("automatic repair", last.Report.Steps.Single(s => s.Key == "publish").Detail);
            h.OpenAI.Verify(x => x.CreateJsonWithInstructionsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>()), Times.Never);
        }

        [Fact]
        public void A_repair_that_drops_a_tier_is_rejected()
        {
            var g = new CodeProjectGeneration { QuestionText = "q", BuildCommand = "dotnet build", TestCommand = "dotnet test" };
            var repaired = CodeProjectQuestionHelper.ApplyRepair(g, "{\"project_files\":[{\"path\":\"a.cs\",\"content\":\"x\"}]}", 15, out var error, out _);
            Assert.Null(repaired);
            Assert.Contains("all three file sets", error);
        }
    }
}
