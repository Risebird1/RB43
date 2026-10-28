using System.Formats.Tar;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using Risebird.Business;
using Xunit;

namespace Risebird.Business.Tests.CodeProjectTests
{
    public class CodeProjectRunnerHelperTests
    {
        private static byte[] Tgz(params (string Name, string Content)[] files)
        {
            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, CompressionLevel.Fastest, leaveOpen: true))
            using (var tar = new TarWriter(gzip, TarEntryFormat.Pax, leaveOpen: true))
            {
                foreach (var (name, content) in files)
                    tar.WriteEntry(new PaxTarEntry(TarEntryType.RegularFile, "./" + name) { DataStream = new MemoryStream(Encoding.UTF8.GetBytes(content)) });
            }
            return output.ToArray();
        }

        private const string Surefire = """
<?xml version="1.0" encoding="UTF-8"?>
<testsuite name="io.rb.StockHiddenTest" tests="3" failures="1">
  <testcase name="r1_creates()" classname="io.rb.StockHiddenTest" time="0.1"/>
  <testcase name="r2_rejectsNegative" classname="io.rb.StockHiddenTest" time="0.1">
    <failure message="expected: &lt;400&gt; but was: &lt;200&gt;" type="org.opentest4j.AssertionFailedError">stack...</failure>
  </testcase>
  <testcase name="r3_skipped" classname="io.rb.StockHiddenTest"><skipped/></testcase>
</testsuite>
""";

        private const string Trx = """
<?xml version="1.0" encoding="UTF-8"?>
<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
  <Results>
    <UnitTestResult testName="Hidden.StockTests.r1_creates" outcome="Passed" />
    <UnitTestResult testName="Hidden.StockTests.r2_rejects(value: -1)" outcome="Failed">
      <Output><ErrorInfo><Message>Assert.Equal() Failure
Expected: 400</Message></ErrorInfo></Output>
    </UnitTestResult>
  </Results>
</TestRun>
""";

        [Fact]
        public void Reads_exit_codes_timing_logs_and_junit_reports()
        {
            var result = CodeProjectRunnerHelper.ParseOutput(Tgz(
                ("build.exit", "0\n"), ("build.log", "BUILD SUCCESS"), ("test.exit", "1\n"), ("test.log", "Tests run: 3"),
                ("wall.ms", "12450"), ("reports/1-TEST-io.rb.StockHiddenTest.xml", Surefire)));

            Assert.True(result.Completed);
            Assert.Equal(0, result.BuildExitCode);
            Assert.Equal(1, result.TestExitCode);
            Assert.Equal(12.45, result.WallSeconds, 2);
            Assert.Equal("BUILD SUCCESS", result.BuildOutput);
            Assert.Equal(3, result.Tests.Count);

            var r1 = result.Tests.Single(t => t.Name == "io.rb.StockHiddenTest.r1_creates");   // parentheses stripped
            Assert.True(r1.Passed);
            var r2 = result.Tests.Single(t => t.Name.EndsWith("r2_rejectsNegative"));
            Assert.False(r2.Passed);
            Assert.Equal("expected: <400> but was: <200>", r2.Message);
            Assert.False(result.Tests.Single(t => t.Name.EndsWith("r3_skipped")).Passed);
        }

        [Fact]
        public void Reads_trx_reports()
        {
            var result = CodeProjectRunnerHelper.ParseOutput(Tgz(("build.exit", "0"), ("test.exit", "1"), ("reports/1-run.trx", Trx)));

            Assert.True(result.Tests.Single(t => t.Name == "Hidden.StockTests.r1_creates").Passed);
            var r2 = result.Tests.Single(t => t.Name == "Hidden.StockTests.r2_rejects");
            Assert.False(r2.Passed);
            Assert.Equal("Assert.Equal() Failure", r2.Message);
        }

        [Fact]
        public void Matches_requirement_test_names_through_the_existing_lookup()
        {
            var result = CodeProjectRunnerHelper.ParseOutput(Tgz(("build.exit", "0"), ("reports/1-a.xml", Surefire)));
            Assert.NotNull(Risebird.Entities.ViewModel.CodeProjectQuestionHelper.FindTest(result.Tests, "r1_creates"));
            Assert.NotNull(Risebird.Entities.ViewModel.CodeProjectQuestionHelper.FindTest(result.Tests, "r2_rejectsNegative"));
        }

        [Fact]
        public void A_test_reported_twice_passes_only_if_every_report_passes()
        {
            var failing = "<testsuite><testcase classname=\"C\" name=\"t\"><failure message=\"boom\"/></testcase></testsuite>";
            var passing = "<testsuite><testcase classname=\"C\" name=\"t\"/></testsuite>";
            var result = CodeProjectRunnerHelper.ParseOutput(Tgz(("build.exit", "0"), ("reports/1-a.xml", passing), ("reports/2-b.xml", failing)));
            Assert.False(result.Tests.Single().Passed);
        }

        [Fact]
        public void A_missing_build_exit_means_the_build_never_finished()
        {
            var result = CodeProjectRunnerHelper.ParseOutput(Tgz(("build.log", "killed")));
            Assert.True(result.Completed);
            Assert.Equal(-1, result.BuildExitCode);
            Assert.Null(result.TestExitCode);
        }

        [Fact]
        public void The_runner_error_file_means_not_completed()
        {
            var result = CodeProjectRunnerHelper.ParseOutput(Tgz(("error.txt", "Could not download the project.\n")));
            Assert.False(result.Completed);
            Assert.Equal("Could not download the project.", result.Error);
        }

        [Fact]
        public void Garbage_and_unreadable_reports_do_not_throw()
        {
            Assert.False(CodeProjectRunnerHelper.ParseOutput(null).Completed);
            Assert.False(CodeProjectRunnerHelper.ParseOutput(Encoding.UTF8.GetBytes("not a tarball")).Completed);
            var result = CodeProjectRunnerHelper.ParseOutput(Tgz(("build.exit", "0"), ("reports/1-bad.xml", "<not xml")));
            Assert.True(result.Completed);
            Assert.Empty(result.Tests);
        }
    }

    public class CodeProjectStackCatalogTests
    {
        public static IEnumerable<object[]> Stacks => CodeProjectStackCatalog.All.Select(s => new object[] { s.Code });

        [Theory]
        [MemberData(nameof(Stacks))]
        public void Stack_values_fit_their_columns_and_azure_name_rules(string code)
        {
            var s = CodeProjectStackCatalog.Find(code);
            Assert.True(s.Toolchain.Length <= 500, $"{code} toolchain is {s.Toolchain.Length} chars; StackImages.Toolchain is varchar(500).");
            Assert.Matches(new Regex("^[a-z][a-z0-9-]{0,30}[a-z0-9]$"), s.JobName);            // Container Apps job name
            Assert.Matches(new Regex("^[a-z0-9][a-z0-9._-]*$"), s.RunnerRepository);            // ACR repository
            Assert.True(CodeProjectStackCatalog.ImageTemplateName(s).Length <= 64);
            Assert.Contains("build_command:", s.Toolchain);
            Assert.Contains("test_command:", s.Toolchain);
        }

        [Theory]
        [MemberData(nameof(Stacks))]
        public void Runner_build_context_contains_dockerfile_script_and_seed(string code)
        {
            var s = CodeProjectStackCatalog.Find(code);
            var entries = new Dictionary<string, string>();
            using (var gzip = new GZipStream(new MemoryStream(CodeProjectStackCatalog.BuildRunnerContext(s)), CompressionMode.Decompress))
            using (var tar = new TarReader(gzip))
            {
                TarEntry e;
                while ((e = tar.GetNextEntry()) != null)
                    entries[e.Name] = new StreamReader(e.DataStream).ReadToEnd();
            }

            Assert.StartsWith($"FROM {s.RunnerBaseImage}\n", entries["Dockerfile"]);
            Assert.Contains("ENTRYPOINT [\"/usr/local/bin/rb-run\"]", entries["Dockerfile"]);
            Assert.StartsWith("#!/usr/bin/env bash", entries["rb-run.sh"]);
            Assert.DoesNotContain("\r", entries["rb-run.sh"]);   // CRLF would break the shebang in Linux
            foreach (var seed in s.SeedFiles.Keys)
                Assert.True(entries.ContainsKey("seed/" + seed), $"missing seed/{seed}");
        }

        [Fact]
        public void Runner_script_removes_the_sas_urls_before_running_project_code()
        {
            var script = CodeProjectStackCatalog.RunnerScript;
            var unset = script.IndexOf("unset ZIP_URL RESULT_URL", StringComparison.Ordinal);
            var build = script.IndexOf("BUILD_CMD:-true", StringComparison.Ordinal);
            Assert.True(unset > 0 && unset < build);
            Assert.Contains("runuser -u runner", script);
        }

        [Theory]
        [MemberData(nameof(Stacks))]
        public void Windows_seed_script_writes_every_seed_file_verbatim(string code)
        {
            var s = CodeProjectStackCatalog.Find(code);
            var lines = CodeProjectStackCatalog.WindowsSeedLines(s);
            foreach (var seed in s.SeedFiles)
            {
                var path = @"C:\rb-seed\" + seed.Key.Replace('/', '\\');
                var line = lines.Single(l => l.StartsWith($"[IO.File]::WriteAllBytes('{path}'"));
                var b64 = Regex.Match(line, @"FromBase64String\('([^']+)'\)").Groups[1].Value;
                Assert.Equal(seed.Value.Replace("\r\n", "\n"), Encoding.UTF8.GetString(Convert.FromBase64String(b64)));
            }
            Assert.All(CodeProjectStackCatalog.WindowsInstallLines(s).Concat(lines), l => Assert.True(l.All(c => c < 128), $"non-ASCII in: {l}"));
        }

        [Theory]
        [MemberData(nameof(Stacks))]
        public void Windows_script_blocks_are_not_glued_onto_one_line(string code)
        {
            // Raw string literals end without a newline; a missing separator once produced
            // "Invoke-Choco @('vscode')Invoke-Choco @('nodejs', ...)", which PowerShell rejects.
            var s = CodeProjectStackCatalog.Find(code);
            var install = CodeProjectStackCatalog.WindowsInstallLines(s);
            var seed = CodeProjectStackCatalog.WindowsSeedLines(s);
            Assert.Contains("Invoke-Choco @('vscode')", install);
            Assert.Contains("}", seed);
            Assert.Equal("Remove-Item 'C:\\rb-seed' -Recurse -Force -ErrorAction SilentlyContinue", seed.Last());
            var glued = new Regex(@"[)}']\s*(Invoke-Choco|Invoke-Native|New-Item|Remove-Item|Grant-Users|Push-Location|Pop-Location)\b");
            Assert.All(install.Concat(seed), l => Assert.False(glued.IsMatch(l), $"two commands on one line: {l}"));
        }

        [Fact]
        public void Node_projects_get_the_exact_pre_loaded_versions()
        {
            // "^5.4.10" made npm pick 5.4.21 offline, which is not on the machine (ENOTCACHED).
            const string generated = "{ \"name\": \"todo\", \"dependencies\": { \"express\": \"^4.21.1\" }, " +
                                     "\"devDependencies\": { \"vite\": \"^5.4.10\", \"vitest\": \"~2.1.4\", \"left-pad\": \"^1.3.0\" } }";
            var g = new Risebird.Entities.ViewModel.CodeProjectGeneration
            {
                StackImageCode = "rb-node-win",
                ProjectFiles = { new() { Path = "package.json", Content = generated } },
                ReferenceSolution = { new() { Path = "package.json", Content = generated } }
            };

            CodeProjectStackCatalog.ApplyStackFixups(g);

            foreach (var file in g.ProjectFiles.Concat(g.ReferenceSolution))
            {
                var json = System.Text.Json.JsonDocument.Parse(file.Content).RootElement;
                Assert.Equal("4.21.1", json.GetProperty("dependencies").GetProperty("express").GetString());
                Assert.Equal("5.4.10", json.GetProperty("devDependencies").GetProperty("vite").GetString());
                Assert.Equal("2.1.4", json.GetProperty("devDependencies").GetProperty("vitest").GetString());
                // Not on the machine: left for validation (and repair) to deal with.
                Assert.Equal("^1.3.0", json.GetProperty("devDependencies").GetProperty("left-pad").GetString());
            }
        }

        [Fact]
        public void Dotnet_test_projects_get_the_test_sdk_and_exact_versions()
        {
            // As generated: no Microsoft.NET.Test.Sdk (dotnet test would find no tests) and a floating xunit version.
            const string tests = "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>" +
                                 "<ItemGroup><PackageReference Include=\"xunit\" Version=\"2.*\" /><PackageReference Include=\"Microsoft.AspNetCore.Mvc.Testing\"><Version>8.0.*</Version></PackageReference></ItemGroup></Project>";
            var g = new Risebird.Entities.ViewModel.CodeProjectGeneration
            {
                StackImageCode = "rb-dotnet-win",
                BuildCommand = "dotnet build -nologo",
                ProjectFiles = { new() { Path = "Api.Tests/Api.Tests.csproj", Content = tests } }
            };

            CodeProjectStackCatalog.ApplyStackFixups(g);

            var refs = System.Xml.Linq.XDocument.Parse(g.ProjectFiles[0].Content).Descendants("PackageReference")
                .ToDictionary(r => (string)r.Attribute("Include"), r => (string)r.Attribute("Version"));
            Assert.Equal("2.9.2", refs["xunit"]);
            Assert.Equal("8.0.10", refs["Microsoft.AspNetCore.Mvc.Testing"]);
            Assert.Equal("17.11.1", refs["Microsoft.NET.Test.Sdk"]);
            Assert.Equal("2.8.2", refs["xunit.runner.visualstudio"]);
        }

        [Fact]
        public void Dotnet_workspaces_always_get_implicit_usings_and_no_audit()
        {
            var g = new Risebird.Entities.ViewModel.CodeProjectGeneration
            {
                BuildCommand = "dotnet build -nologo",
                // The project brought its own Directory.Build.props without either property.
                ProjectFiles = { new() { Path = "Directory.Build.props", Content = "<Project><PropertyGroup><LangVersion>latest</LangVersion></PropertyGroup></Project>" } }
            };

            Risebird.Entities.ViewModel.CodeProjectQuestionHelper.EnsureDotnetOffline(g);

            var props = System.Xml.Linq.XDocument.Parse(g.ProjectFiles.Single(f => f.Path == "Directory.Build.props").Content);
            Assert.Equal("enable", (string)props.Descendants("ImplicitUsings").Single());
            Assert.Equal("false", (string)props.Descendants("NuGetAudit").Single());
            Assert.Equal("latest", (string)props.Descendants("LangVersion").Single());
        }

        [Fact]
        public void Django_projects_tested_with_pytest_get_their_settings_module()
        {
            var g = new Risebird.Entities.ViewModel.CodeProjectGeneration
            {
                TestCommand = "python -m pytest --junitxml=test-results/junit.xml",
                ProjectFiles =
                {
                    // manage.py names the settings too, but pytest never runs it.
                    new() { Path = "manage.py", Content = "import os\nos.environ.setdefault('DJANGO_SETTINGS_MODULE', 'proj.settings')" },
                    new() { Path = "proj/settings.py", Content = "INSTALLED_APPS = []" },
                    new() { Path = "books/views.py", Content = "from rest_framework.decorators import api_view" }
                }
            };

            Assert.Equal("pytest.ini", Risebird.Entities.ViewModel.CodeProjectQuestionHelper.EnsureDjangoSettingsForPytest(g));
            Assert.Contains("DJANGO_SETTINGS_MODULE = proj.settings", g.ProjectFiles.Single(f => f.Path == "pytest.ini").Content);
            Assert.Contains("pytest.ini", g.ReadOnlyPaths);
            // Now configured: running it again changes nothing.
            Assert.Null(Risebird.Entities.ViewModel.CodeProjectQuestionHelper.EnsureDjangoSettingsForPytest(g));
        }

        [Fact]
        public void Non_django_python_projects_are_left_alone()
        {
            var g = new Risebird.Entities.ViewModel.CodeProjectGeneration
            {
                TestCommand = "python -m pytest",
                ProjectFiles = { new() { Path = "app/main.py", Content = "import sqlite3" } }
            };
            Assert.Null(Risebird.Entities.ViewModel.CodeProjectQuestionHelper.EnsureDjangoSettingsForPytest(g));
            Assert.Empty(g.ProjectFiles.Where(f => f.Path == "pytest.ini"));
        }

        [Fact]
        public void A_root_dotnet_project_beside_a_test_project_moves_into_its_own_folder()
        {
            var g = new Risebird.Entities.ViewModel.CodeProjectGeneration
            {
                BuildCommand = "dotnet build -nologo",
                EntryPointFile = "Program.cs",
                EditableFiles = { "Program.cs" },
                ProjectFiles =
                {
                    new() { Path = "Api.csproj", Content = "<Project Sdk=\"Microsoft.NET.Sdk.Web\" />" },
                    new() { Path = "Program.cs", Content = "" },
                    new() { Path = "Controllers/GreetController.cs", Content = "" },
                    new() { Path = "NuGet.Config", Content = "<configuration />" },
                    new() { Path = "Api.Tests/Api.Tests.csproj", Content = "<Project><ItemGroup><ProjectReference Include=\"..\\Api.csproj\" /></ItemGroup></Project>" }
                },
                ReferenceSolution = { new() { Path = "Program.cs", Content = "solved" } }
            };

            Assert.Equal("Api", Risebird.Entities.ViewModel.CodeProjectQuestionHelper.MoveRootDotnetProjectIntoFolder(g));

            var paths = g.ProjectFiles.Select(f => f.Path).ToList();
            Assert.Contains("Api/Api.csproj", paths);
            Assert.Contains("Api/Controllers/GreetController.cs", paths);
            Assert.Contains("NuGet.Config", paths);                  // shared root files stay
            Assert.Contains("Api.Tests/Api.Tests.csproj", paths);    // other projects stay
            Assert.Equal("Api/Program.cs", g.EntryPointFile);
            Assert.Equal(new[] { "Api/Program.cs" }, g.EditableFiles);
            Assert.Equal("Api/Program.cs", g.ReferenceSolution.Single().Path);
            Assert.Contains("Include=\"..\\Api\\Api.csproj\"", g.ProjectFiles.Single(f => f.Path == "Api.Tests/Api.Tests.csproj").Content);
        }

        [Fact]
        public void A_single_root_dotnet_project_is_left_where_it_is()
        {
            var g = new Risebird.Entities.ViewModel.CodeProjectGeneration
            {
                BuildCommand = "dotnet build -nologo",
                ProjectFiles = { new() { Path = "App.csproj", Content = "<Project />" }, new() { Path = "Program.cs", Content = "" } }
            };
            Assert.Null(Risebird.Entities.ViewModel.CodeProjectQuestionHelper.MoveRootDotnetProjectIntoFolder(g));
            Assert.Contains(g.ProjectFiles, f => f.Path == "App.csproj");
        }

        [Fact]
        public void Pinned_versions_come_from_the_seed_and_other_stacks_have_none()
        {
            var node = CodeProjectStackCatalog.PinnedPackageVersions(CodeProjectStackCatalog.Find("rb-node-win"));
            Assert.Equal("5.4.10", node["vite"]);
            Assert.Equal("4.21.1", node["express"]);
            Assert.Empty(CodeProjectStackCatalog.PinnedPackageVersions(CodeProjectStackCatalog.Find("rb-java-win")));
        }

        [Fact]
        public void Image_tags_and_template_names_are_stable_and_distinct()
        {
            foreach (var s in CodeProjectStackCatalog.All)
            {
                Assert.Equal(CodeProjectStackCatalog.RunnerImageTag(s), CodeProjectStackCatalog.RunnerImageTag(s));
                Assert.Equal(CodeProjectStackCatalog.ImageTemplateName(s), CodeProjectStackCatalog.ImageTemplateName(s));
            }
            Assert.Equal(CodeProjectStackCatalog.All.Count, CodeProjectStackCatalog.All.Select(CodeProjectStackCatalog.RunnerImageTag).Distinct().Count());
            Assert.Equal(CodeProjectStackCatalog.All.Count, CodeProjectStackCatalog.All.Select(s => s.JobName).Distinct().Count());
        }
    }
}
