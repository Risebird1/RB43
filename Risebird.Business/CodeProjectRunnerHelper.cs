using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Risebird.Business
{
    /// <summary>Reads the runner container's output archive (see CodeProjectStackCatalog.RunnerScript).</summary>
    public static class CodeProjectRunnerHelper
    {
        public static CodeProjectRunResult ParseOutput(byte[] tgz)
        {
            if (tgz == null || tgz.Length == 0)
                return new CodeProjectRunResult { Completed = false, Error = "The runner produced no output." };

            var files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using var gzip = new GZipStream(new MemoryStream(tgz), CompressionMode.Decompress);
                using var tar = new TarReader(gzip);
                TarEntry entry;
                while ((entry = tar.GetNextEntry()) != null)
                {
                    if (entry.EntryType != TarEntryType.RegularFile && entry.EntryType != TarEntryType.V7RegularFile) continue;
                    if (entry.DataStream == null) continue;
                    using var ms = new MemoryStream();
                    entry.DataStream.CopyTo(ms);
                    files[entry.Name.TrimStart('.', '/')] = ms.ToArray();
                }
            }
            catch (Exception ex) when (ex is InvalidDataException || ex is FormatException || ex is EndOfStreamException)
            {
                return new CodeProjectRunResult { Completed = false, Error = "The runner output could not be read." };
            }

            string Text(string name) => files.TryGetValue(name, out var b) ? Encoding.UTF8.GetString(b).Trim() : null;
            int? Int(string name) => int.TryParse(Text(name), out var v) ? v : (int?)null;

            var error = Text("error.txt");
            if (!string.IsNullOrEmpty(error))
                return new CodeProjectRunResult { Completed = false, Error = error };

            var tests = new List<CodeProjectTestResult>();
            foreach (var report in files.Where(f => f.Key.StartsWith("reports/", StringComparison.OrdinalIgnoreCase)))
            {
                try
                {
                    var xml = XDocument.Load(new MemoryStream(report.Value));
                    tests.AddRange(report.Key.EndsWith(".trx", StringComparison.OrdinalIgnoreCase) ? ParseTrx(xml) : ParseJUnit(xml));
                }
                catch (XmlException)
                {
                    // An unreadable report counts as no results rather than failing the whole run.
                }
            }

            return new CodeProjectRunResult
            {
                Completed = true,
                BuildExitCode = Int("build.exit") ?? -1,
                BuildOutput = Text("build.log"),
                TestExitCode = Int("test.exit"),
                TestOutput = Text("test.log"),
                WallSeconds = (Int("wall.ms") ?? 0) / 1000.0,
                // A test reported twice (e.g. retried) counts as passed only if every report says so.
                Tests = tests
                    .GroupBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.FirstOrDefault(t => !t.Passed) ?? g.First())
                    .ToList()
            };
        }

        /// <summary>JUnit XML (Surefire, pytest, vitest, jest-junit): testcase with failure/error/skipped children.</summary>
        public static IEnumerable<CodeProjectTestResult> ParseJUnit(XDocument xml)
        {
            foreach (var tc in xml.Descendants().Where(e => e.Name.LocalName == "testcase"))
            {
                var name = CleanName(tc.Attribute("name")?.Value);
                if (string.IsNullOrEmpty(name)) continue;
                var classname = tc.Attribute("classname")?.Value?.Trim();
                var failure = tc.Elements().FirstOrDefault(e => e.Name.LocalName == "failure" || e.Name.LocalName == "error");
                var skipped = tc.Elements().Any(e => e.Name.LocalName == "skipped");
                yield return new CodeProjectTestResult
                {
                    Name = string.IsNullOrEmpty(classname) ? name : classname + "." + name,
                    Passed = failure == null && !skipped,
                    Message = failure != null ? Shorten(failure.Attribute("message")?.Value ?? failure.Value) : skipped ? "skipped" : null
                };
            }
        }

        /// <summary>Visual Studio TRX (dotnet test --logger trx).</summary>
        public static IEnumerable<CodeProjectTestResult> ParseTrx(XDocument xml)
        {
            foreach (var r in xml.Descendants().Where(e => e.Name.LocalName == "UnitTestResult"))
            {
                var name = CleanName(r.Attribute("testName")?.Value);
                if (string.IsNullOrEmpty(name)) continue;
                var outcome = r.Attribute("outcome")?.Value;
                var message = r.Descendants().FirstOrDefault(e => e.Name.LocalName == "Message")?.Value;
                yield return new CodeProjectTestResult
                {
                    Name = name,
                    Passed = string.Equals(outcome, "Passed", StringComparison.OrdinalIgnoreCase),
                    Message = string.Equals(outcome, "Passed", StringComparison.OrdinalIgnoreCase) ? null : Shorten(message ?? outcome)
                };
            }
        }

        /// <summary>"r1_rejects()" and "r1_rejects(int)[1]" become "r1_rejects".</summary>
        private static string CleanName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            var n = name.Trim();
            var paren = n.IndexOf('(');
            if (paren > 0) n = n.Substring(0, paren);
            var bracket = n.IndexOf('[');
            if (bracket > 0) n = n.Substring(0, bracket);
            return n.Trim();
        }

        private static string Shorten(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            var firstLine = s.Trim().Split('\n')[0].Trim();
            return firstLine.Length > 300 ? firstLine.Substring(0, 300) + "…" : firstLine;
        }
    }
}
