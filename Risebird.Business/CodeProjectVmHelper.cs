using Newtonsoft.Json.Linq;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;

namespace Risebird.Business
{
    /// <summary>Pure helpers for the code project VM provisioner (Risebird.AzureTimerFunctions), kept here so they are testable.</summary>
    public static class CodeProjectVmHelper
    {
        public const string SnapshotMarker = "RBJSON:";

        /// <summary>
        /// Parses the collect script's output: one line "RBJSON:{...}" carrying the baseline and head
        /// commits, the commit log, the diff and a base64 zip of the tree at HEAD. Anything else the
        /// script or Windows printed is ignored.
        /// </summary>
        public static CodeProjectWorkspaceSnapshot ParseSnapshot(string output)
        {
            var line = (output ?? "").Replace("\r", "").Split('\n').LastOrDefault(l => l.StartsWith(SnapshotMarker, StringComparison.Ordinal));
            if (line == null) return new CodeProjectWorkspaceSnapshot { Success = false, Error = "The workspace reader returned no result." };

            JObject json;
            try { json = JObject.Parse(line.Substring(SnapshotMarker.Length)); }
            catch (Newtonsoft.Json.JsonException) { return new CodeProjectWorkspaceSnapshot { Success = false, Error = "The workspace reader returned malformed output." }; }

            // ConvertTo-Json writes a one-element array as an array, but be tolerant of a bare object.
            var commitTokens = json["commits"] is JArray arr ? arr : json["commits"] is JObject one ? new JArray(one) : new JArray();
            var commits = commitTokens.Select(c => new CodeProjectCommit
            {
                Sha = c["sha"]?.ToString(),
                Message = c["message"]?.ToString(),
                AtUtc = ReadUtc(c["atUtc"]),
                FilesChanged = c["filesChanged"]?.Value<int?>(),
                Insertions = c["insertions"]?.Value<int?>(),
                Deletions = c["deletions"]?.Value<int?>()
            }).ToList();

            List<CodeProjectFile> files;
            try
            {
                var zip = json["zipBase64"]?.ToString();
                files = string.IsNullOrEmpty(zip) ? new List<CodeProjectFile>() : CodeProjectQuestionService.Unzip(Convert.FromBase64String(zip));
            }
            catch (Exception ex) when (ex is FormatException || ex is System.IO.InvalidDataException)
            {
                return new CodeProjectWorkspaceSnapshot { Success = false, Error = "The workspace archive could not be read." };
            }

            return new CodeProjectWorkspaceSnapshot
            {
                Success = true,
                BaselineSha = json["baselineSha"]?.ToString(),
                HeadSha = json["headSha"]?.ToString(),
                Diff = json["diff"]?.ToString(),
                Commits = commits,
                Files = files
            };
        }

        /// <summary>
        /// JObject.Parse turns ISO date strings into Date tokens, whose ToString() is culture-formatted,
        /// so read the token by type rather than round-tripping through a string.
        /// </summary>
        private static DateTime? ReadUtc(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null) return null;
            if (token.Type == JTokenType.Date)
            {
                var value = token.Value<DateTime>();
                return value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
            }
            return DateTime.TryParse(token.ToString(), CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed) ? parsed : (DateTime?)null;
        }

        /// <summary>
        /// Meets Windows complexity (upper, lower, digit, symbol). Symbols are limited to ones
        /// PowerShell never treats specially at the start of an argument (no '-', '#', '@', '$',
        /// quotes or backtick), since the password reaches the VM as a Run Command parameter.
        /// </summary>
        public static string NewPassword(int length)
        {
            if (length < 12) throw new ArgumentOutOfRangeException(nameof(length), "Use at least 12 characters.");
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ", lower = "abcdefghijkmnopqrstuvwxyz", digits = "23456789", symbols = "_+=";
            var all = upper + lower + digits + symbols;
            var chars = new List<char>
            {
                upper[RandomNumberGenerator.GetInt32(upper.Length)],
                lower[RandomNumberGenerator.GetInt32(lower.Length)],
                digits[RandomNumberGenerator.GetInt32(digits.Length)],
                symbols[RandomNumberGenerator.GetInt32(symbols.Length)]
            };
            while (chars.Count < length) chars.Add(all[RandomNumberGenerator.GetInt32(all.Length)]);
            // Fisher-Yates with a CSPRNG so the guaranteed characters are not always first.
            for (var i = chars.Count - 1; i > 0; i--)
            {
                var j = RandomNumberGenerator.GetInt32(i + 1);
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }
            return new string(chars.ToArray());
        }

        /// <summary>Names derived from the handle. Windows computer names are at most 15 characters.</summary>
        public static (string VmName, string NicName, string ComputerName, string AdminUser) Names(string handle) =>
            ("rbcp-" + handle.Substring(0, 12), "rbcp-" + handle.Substring(0, 12) + "-nic", "rbcp" + handle.Substring(0, 11), "rbadmin" + handle.Substring(0, 6));

        // ------------------------------------------------------------------ session monitoring

        public sealed class BastionSession
        {
            public string SessionId { get; set; }
            public string UserName { get; set; }
            public string TargetResourceId { get; set; }
            public DateTime? StartTimeUtc { get; set; }
        }

        public sealed class SessionReview
        {
            public List<string> Disconnect { get; } = new List<string>();
            public List<CodeProjectIntegrityFlag> NewFlags { get; } = new List<CodeProjectIntegrityFlag>();
            public HashSet<string> Seen { get; set; }
            /// <summary>The deadline has just passed: lock the account now.</summary>
            public bool LockNow { get; set; }
        }

        /// <summary>A candidate who reconnects more often than this in one slot is worth a reviewer's look.</summary>
        public const int MaxDistinctSessions = 6;

        /// <summary>
        /// Decides what to do about the Bastion sessions on one candidate machine. Nothing here blocks or
        /// fails a candidate: it disconnects sessions that should not exist and records flags for a human.
        ///   • after the deadline (or once locked): every session is disconnected;
        ///   • a session signed in as anyone but the candidate account is disconnected and flagged;
        ///   • more than one candidate session at once: the newest stays, the others are disconnected and flagged;
        ///   • an unusual number of distinct sessions over the slot is flagged once.
        /// </summary>
        public static SessionReview ReviewSessions(string candidateUser, DateTime? deadlineUtc, DateTime? lockedAtUtc,
            IEnumerable<string> seenBefore, IEnumerable<CodeProjectIntegrityFlag> flagsBefore, IList<BastionSession> sessions, DateTime nowUtc)
        {
            var existing = (flagsBefore ?? Enumerable.Empty<CodeProjectIntegrityFlag>()).ToList();
            var review = new SessionReview
            {
                Seen = new HashSet<string>(seenBefore ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase),
                LockNow = deadlineUtc.HasValue && nowUtc > deadlineUtc.Value && lockedAtUtc == null
            };
            sessions ??= new List<BastionSession>();
            foreach (var s in sessions.Where(x => !string.IsNullOrEmpty(x.SessionId))) review.Seen.Add(s.SessionId);

            void Flag(string code, string detail)
            {
                if (existing.Concat(review.NewFlags).Any(f => f.Code == code && f.Detail == detail)) return;
                review.NewFlags.Add(new CodeProjectIntegrityFlag { AtUtc = nowUtc, Code = code, Detail = detail });
            }

            if (lockedAtUtc != null || review.LockNow)
            {
                foreach (var s in sessions)
                {
                    review.Disconnect.Add(s.SessionId);
                    // Disconnecting at the deadline is expected; a session that appears after the lock is not.
                    if (lockedAtUtc != null) Flag("session-after-lock", $"Session {s.SessionId} opened after the slot ended.");
                }
            }
            else
            {
                static string Bare(string user) => user == null ? "" : user.Contains('\\') ? user.Substring(user.LastIndexOf('\\') + 1) : user;
                var candidate = new List<BastionSession>();
                foreach (var s in sessions)
                {
                    if (string.Equals(Bare(s.UserName), candidateUser, StringComparison.OrdinalIgnoreCase)) { candidate.Add(s); continue; }
                    review.Disconnect.Add(s.SessionId);
                    Flag("unexpected-user", $"Session {s.SessionId} signed in as '{s.UserName}', not the candidate account.");
                }
                if (candidate.Count > 1)
                {
                    var keep = candidate.OrderByDescending(s => s.StartTimeUtc ?? DateTime.MinValue).First();
                    foreach (var s in candidate.Where(s => s != keep))
                    {
                        review.Disconnect.Add(s.SessionId);
                        Flag("concurrent-sessions", $"Session {s.SessionId} ran alongside {keep.SessionId}; the older one was disconnected.");
                    }
                }
            }

            if (review.Seen.Count > MaxDistinctSessions)
                Flag("many-reconnects", $"More than {MaxDistinctSessions} separate sessions were opened during the slot.");
            return review;
        }

        /// <summary>Parses a getActiveSessions response, keeping only sessions on the given VM.</summary>
        public static List<BastionSession> ParseSessions(JObject body, string vmResourceId) =>
            ((body?["value"] as JArray) ?? new JArray())
                .Where(v => string.Equals(v["targetResourceId"]?.ToString(), vmResourceId, StringComparison.OrdinalIgnoreCase))
                .Select(v => new BastionSession
                {
                    SessionId = v["sessionId"]?.ToString(),
                    UserName = v["userName"]?.ToString(),
                    TargetResourceId = v["targetResourceId"]?.ToString(),
                    StartTimeUtc = ReadUtc(v["startTime"])
                })
                .ToList();

        /// <summary>The Bastion shareable link for this VM from a create/get response.</summary>
        public static string ExtractShareableLink(JObject body, string vmResourceId) =>
            (body?["value"] as JArray)?
                .FirstOrDefault(v => string.Equals(v["vm"]?["id"]?.ToString(), vmResourceId, StringComparison.OrdinalIgnoreCase))?["bsl"]?.ToString();
    }
}
