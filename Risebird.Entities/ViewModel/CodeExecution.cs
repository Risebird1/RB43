using System.Collections.Generic;
using System.Linq;

namespace Risebird.Entities.ViewModel
{
    public class CodeExecutionRequest
    {
        public string LanguageId { get; set; }
        public string SourceCode { get; set; }
        public string Stdin { get; set; }
    }

    public class CodeExecutionResult
    {
        public bool Success { get; set; }
        public string Status { get; set; }
        public string Stdout { get; set; }
        public string Stderr { get; set; }
        public string CompileOutput { get; set; }
        public int? ExitCode { get; set; }
        public string Message { get; set; }

        /// <summary>Everything the candidate saw in the console, already merged in display order.</summary>
        public string CombinedOutput { get; set; }

        /// <summary>
        /// True when the program never ran at all — the runner was unreachable, refused the
        /// request, timed out, or is not configured in this environment.
        ///
        /// This is NOT the same thing as a run that failed. A compile error or a thrown
        /// exception says something true about the candidate; an outage says nothing about
        /// them whatsoever. Success = false cannot carry the distinction on its own, because
        /// a program that compiled and then threw is also Success = false. A code session
        /// stores the two as different run states, and a client report must never let them
        /// look alike.
        /// </summary>
        public bool DidNotRun { get; set; }

        /// <summary>
        /// The runtime version the backend actually used, which is not always the version that
        /// was asked for — a self-hosted instance upgrades its runtimes on its own schedule.
        /// Null when the backend does not report one.
        /// </summary>
        public string LanguageVersionUsed { get; set; }

        /// <summary>How long the run took, measured around the call to the backend.</summary>
        public int? DurationMs { get; set; }
    }

    /// <summary>
    /// One entry in the language dropdown of the coding IDE. Holds the identifiers each
    /// execution backend expects plus the CodeMirror mode and the starter template.
    /// </summary>
    public class CodeLanguageOption
    {
        public string Id { get; set; }
        public string Label { get; set; }
        public string CodeMirrorMode { get; set; }
        public string PistonLanguage { get; set; }
        public string PistonVersion { get; set; }
        public int Judge0Id { get; set; }
        public string FileName { get; set; }
        public string Template { get; set; }
    }

    public static class CodeLanguageCatalog
    {
        public static readonly IReadOnlyList<CodeLanguageOption> Languages = new List<CodeLanguageOption>
        {
            new CodeLanguageOption
            {
                Id = "python",
                Label = "Python 3",
                CodeMirrorMode = "python",
                PistonLanguage = "python",
                PistonVersion = "3.10.0",
                Judge0Id = 71,
                FileName = "main.py",
                Template = "def solve():\n    # Write your solution here\n    pass\n\n\nif __name__ == \"__main__\":\n    solve()\n"
            },
            new CodeLanguageOption
            {
                Id = "javascript",
                Label = "JavaScript (Node)",
                CodeMirrorMode = "javascript",
                PistonLanguage = "javascript",
                PistonVersion = "18.15.0",
                Judge0Id = 63,
                FileName = "main.js",
                Template = "function solve() {\n    // Write your solution here\n}\n\nsolve();\n"
            },
            new CodeLanguageOption
            {
                Id = "typescript",
                Label = "TypeScript",
                CodeMirrorMode = "javascript",
                PistonLanguage = "typescript",
                PistonVersion = "5.0.3",
                Judge0Id = 74,
                FileName = "main.ts",
                Template = "function solve(): void {\n    // Write your solution here\n}\n\nsolve();\n"
            },
            new CodeLanguageOption
            {
                Id = "java",
                Label = "Java",
                CodeMirrorMode = "text/x-java",
                PistonLanguage = "java",
                PistonVersion = "15.0.2",
                Judge0Id = 62,
                FileName = "Main.java",
                Template = "public class Main {\n    public static void main(String[] args) {\n        // Write your solution here\n    }\n}\n"
            },
            new CodeLanguageOption
            {
                Id = "csharp",
                Label = "C#",
                CodeMirrorMode = "text/x-csharp",
                PistonLanguage = "csharp",
                PistonVersion = "6.12.0",
                Judge0Id = 51,
                FileName = "Main.cs",
                Template = "using System;\n\npublic class Program\n{\n    public static void Main()\n    {\n        // Write your solution here\n    }\n}\n"
            },
            new CodeLanguageOption
            {
                Id = "cpp",
                Label = "C++",
                CodeMirrorMode = "text/x-c++src",
                PistonLanguage = "c++",
                PistonVersion = "10.2.0",
                Judge0Id = 54,
                FileName = "main.cpp",
                Template = "#include <bits/stdc++.h>\nusing namespace std;\n\nint main() {\n    // Write your solution here\n    return 0;\n}\n"
            },
            new CodeLanguageOption
            {
                Id = "c",
                Label = "C",
                CodeMirrorMode = "text/x-csrc",
                PistonLanguage = "c",
                PistonVersion = "10.2.0",
                Judge0Id = 50,
                FileName = "main.c",
                Template = "#include <stdio.h>\n\nint main(void) {\n    /* Write your solution here */\n    return 0;\n}\n"
            },
            new CodeLanguageOption
            {
                Id = "go",
                Label = "Go",
                CodeMirrorMode = "text/x-go",
                PistonLanguage = "go",
                PistonVersion = "1.16.2",
                Judge0Id = 60,
                FileName = "main.go",
                Template = "package main\n\nimport \"fmt\"\n\nfunc main() {\n    // Write your solution here\n    fmt.Println()\n}\n"
            },
            new CodeLanguageOption
            {
                Id = "ruby",
                Label = "Ruby",
                CodeMirrorMode = "text/x-ruby",
                PistonLanguage = "ruby",
                PistonVersion = "3.0.1",
                Judge0Id = 72,
                FileName = "main.rb",
                Template = "# Write your solution here\n"
            },
            new CodeLanguageOption
            {
                Id = "php",
                Label = "PHP",
                CodeMirrorMode = "application/x-httpd-php",
                PistonLanguage = "php",
                PistonVersion = "8.2.3",
                Judge0Id = 68,
                FileName = "main.php",
                Template = "<?php\n// Write your solution here\n"
            },
            new CodeLanguageOption
            {
                Id = "rust",
                Label = "Rust",
                CodeMirrorMode = "text/x-rustsrc",
                PistonLanguage = "rust",
                PistonVersion = "1.68.2",
                Judge0Id = 73,
                FileName = "main.rs",
                Template = "fn main() {\n    // Write your solution here\n}\n"
            },
            new CodeLanguageOption
            {
                Id = "kotlin",
                Label = "Kotlin",
                CodeMirrorMode = "text/x-kotlin",
                PistonLanguage = "kotlin",
                PistonVersion = "1.8.20",
                Judge0Id = 78,
                FileName = "Main.kt",
                Template = "fun main() {\n    // Write your solution here\n}\n"
            },
            new CodeLanguageOption
            {
                Id = "swift",
                Label = "Swift",
                CodeMirrorMode = "text/x-swift",
                PistonLanguage = "swift",
                PistonVersion = "5.3.3",
                Judge0Id = 83,
                FileName = "main.swift",
                Template = "// Write your solution here\n"
            },
            new CodeLanguageOption
            {
                Id = "sql",
                Label = "SQL (SQLite)",
                CodeMirrorMode = "sql",
                PistonLanguage = "sqlite3",
                PistonVersion = "3.36.0",
                Judge0Id = 82,
                FileName = "main.sql",
                Template = "-- Write your query here\n"
            }
        };

        public static CodeLanguageOption Find(string languageId)
        {
            if (string.IsNullOrWhiteSpace(languageId))
            {
                return null;
            }

            return Languages.FirstOrDefault(x => string.Equals(x.Id, languageId.Trim(), System.StringComparison.OrdinalIgnoreCase));
        }
    }
}
