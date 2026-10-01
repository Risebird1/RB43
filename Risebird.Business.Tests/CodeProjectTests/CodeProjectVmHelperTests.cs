using Newtonsoft.Json.Linq;
using Risebird.Business;
using Risebird.Entities.ViewModel;
using Xunit;

namespace Risebird.Business.Tests.CodeProjectTests
{
    public class CodeProjectVmHelperSnapshotTests
    {
        private static string Zip(params (string Path, string Content)[] files) =>
            Convert.ToBase64String(CodeProjectQuestionService.Zip(files.Select(f => new CodeProjectFile { Path = f.Path, Content = f.Content })));

        private static string Output(object result) =>
            "Windows noise line\r\nRB-something else\r\n" + CodeProjectVmHelper.SnapshotMarker + JObject.FromObject(result).ToString(Newtonsoft.Json.Formatting.None) + "\r\n";

        [Fact]
        public void Parses_commits_diff_and_the_tree_at_head()
        {
            var snapshot = CodeProjectVmHelper.ParseSnapshot(Output(new
            {
                baselineSha = "a3f81c2aaaa",
                headSha = "4f2a9c1bbbb",
                diff = "diff --git a/src/A.java b/src/A.java\n+added",
                commits = new[]
                {
                    new { sha = "111", message = "create + validation", atUtc = "2026-09-29T10:23:00.0000000Z", filesChanged = 2, insertions = 40, deletions = 1 },
                    new { sha = "4f2a9c1bbbb", message = "update endpoint", atUtc = "2026-09-29T10:31:00.0000000Z", filesChanged = 1, insertions = 6, deletions = 0 }
                },
                zipBase64 = Zip(("src/A.java", "class A {}"), ("pom.xml", "<project/>"))
            }));

            Assert.True(snapshot.Success);
            Assert.Equal("a3f81c2aaaa", snapshot.BaselineSha);
            Assert.Equal("4f2a9c1bbbb", snapshot.HeadSha);
            Assert.StartsWith("diff --git", snapshot.Diff);
            Assert.Equal(2, snapshot.Commits.Count);
            Assert.Equal(new DateTime(2026, 9, 29, 10, 23, 0, DateTimeKind.Utc), snapshot.Commits[0].AtUtc);
            Assert.Equal(DateTimeKind.Utc, snapshot.Commits[0].AtUtc.Value.Kind);
            Assert.Equal(40, snapshot.Commits[0].Insertions);
            Assert.Equal("class A {}", snapshot.Files.Single(f => f.Path == "src/A.java").Content);
        }

        [Fact]
        public void Accepts_a_single_commit_written_as_an_object()
        {
            var snapshot = CodeProjectVmHelper.ParseSnapshot(Output(new
            {
                baselineSha = "a", headSha = "b", diff = "",
                commits = new { sha = "b", message = "only one", atUtc = "2026-09-29T10:00:00Z" },
                zipBase64 = Zip(("x.txt", "1"))
            }));
            Assert.True(snapshot.Success);
            Assert.Single(snapshot.Commits);
        }

        [Fact]
        public void No_commits_after_baseline_is_a_valid_empty_submission()
        {
            var snapshot = CodeProjectVmHelper.ParseSnapshot(Output(new { baselineSha = "a", headSha = "a", diff = "", commits = Array.Empty<object>(), zipBase64 = Zip(("x.txt", "1")) }));
            Assert.True(snapshot.Success);
            Assert.Empty(snapshot.Commits);
            Assert.Single(snapshot.Files);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("RB-SETUP-OK abc\nno json here")]
        public void Fails_when_there_is_no_result_line(string output)
        {
            var snapshot = CodeProjectVmHelper.ParseSnapshot(output);
            Assert.False(snapshot.Success);
            Assert.Contains("no result", snapshot.Error);
        }

        [Theory]
        [InlineData("RBJSON:{not json")]
        [InlineData("RBJSON:{\"zipBase64\":\"%%%not-base64%%%\"}")]
        [InlineData("RBJSON:{\"zipBase64\":\"aGVsbG8=\"}")]   // valid base64, not a zip
        public void Fails_cleanly_on_malformed_output(string output)
        {
            var snapshot = CodeProjectVmHelper.ParseSnapshot(output);
            Assert.False(snapshot.Success);
            Assert.False(string.IsNullOrEmpty(snapshot.Error));
        }
    }

    public class CodeProjectVmHelperMiscTests
    {
        [Fact]
        public void Passwords_meet_windows_complexity_and_are_powershell_safe()
        {
            for (var i = 0; i < 200; i++)
            {
                var p = CodeProjectVmHelper.NewPassword(16);
                Assert.Equal(16, p.Length);
                Assert.Contains(p, char.IsUpper);
                Assert.Contains(p, char.IsLower);
                Assert.Contains(p, char.IsDigit);
                Assert.Contains(p, c => "_+=".Contains(c));
                Assert.DoesNotContain(p, c => "-#@$`'\"".Contains(c));
            }
        }

        [Fact]
        public void Passwords_are_not_repeated()
        {
            var set = Enumerable.Range(0, 500).Select(_ => CodeProjectVmHelper.NewPassword(16)).ToHashSet();
            Assert.Equal(500, set.Count);
        }

        [Fact]
        public void Rejects_short_passwords() =>
            Assert.Throws<ArgumentOutOfRangeException>(() => CodeProjectVmHelper.NewPassword(8));

        [Fact]
        public void Derived_names_fit_azure_and_windows_limits()
        {
            var handle = Guid.NewGuid().ToString("N");
            var n = CodeProjectVmHelper.Names(handle);
            Assert.True(n.ComputerName.Length <= 15);   // Windows NetBIOS computer name
            Assert.True(n.VmName.Length <= 64);
            Assert.StartsWith(n.VmName, n.NicName);
            Assert.DoesNotContain("admin", n.AdminUser.Replace("rbadmin", ""));   // "admin" alone is a reserved name
        }

        [Fact]
        public void Finds_the_shareable_link_for_this_vm_only()
        {
            var body = JObject.Parse(@"{ ""value"": [
                { ""bsl"": ""https://bst/x/other"", ""vm"": { ""id"": ""/subscriptions/s/resourceGroups/rg/providers/Microsoft.Compute/virtualMachines/other"" } },
                { ""bsl"": ""https://bst/x/mine"",  ""vm"": { ""id"": ""/subscriptions/s/resourceGroups/RG/providers/Microsoft.Compute/virtualMachines/rbcp-1"" } } ] }");

            Assert.Equal("https://bst/x/mine", CodeProjectVmHelper.ExtractShareableLink(body,
                "/subscriptions/s/resourceGroups/rg/providers/Microsoft.Compute/virtualMachines/rbcp-1"));
            Assert.Null(CodeProjectVmHelper.ExtractShareableLink(body, "/subscriptions/s/resourceGroups/rg/providers/Microsoft.Compute/virtualMachines/missing"));
            Assert.Null(CodeProjectVmHelper.ExtractShareableLink(null, "x"));
        }
    }
}
