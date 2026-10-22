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
