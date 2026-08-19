using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Risebird.Entities.ViewModel;

namespace Risebird.Web.Helpers
{
    /// <summary>
    /// Confirm preconditions, gist code-syntax guard, and normalised draft comparison for SME review.
    /// </summary>
    public static class SmeQuestionReviewHelper
    {
        public static string NormalizeEmail(string email)
        {
            return (email ?? string.Empty).Trim().Replace("\"", "");
        }

        public static string NormalizeText(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            return Regex.Replace(value.Trim(), @"\s+", " ");
        }

        public static bool TextEquals(string a, string b)
        {
            return string.Equals(NormalizeText(a), NormalizeText(b), StringComparison.Ordinal);
        }

        public static bool GistContainsCodeSyntax(string gist)
        {
            if (string.IsNullOrWhiteSpace(gist)) return false;
            var patterns = new[]
            {
                @"=>",
                @"->",
                @"::",
                @"\bdef\s",
                @"\bfunction\s",
                @"\bclass\s",
                @"\bimport\s",
                @"\bSELECT\s",
                @";\s*$",
                @"\{[^}]*\}",
                @"\w\s*\([^)]*\)\s*[:{]",
                "```"
            };
            foreach (var p in patterns)
            {
                if (Regex.IsMatch(gist, p, RegexOptions.Multiline | RegexOptions.IgnoreCase))
                    return true;
            }
            return false;
        }

        public static int SpokenSeconds(string gist)
        {
            if (string.IsNullOrWhiteSpace(gist)) return 0;
            var words = Regex.Split(gist.Trim(), @"\s+").Length;
            return (int)Math.Round(words / 2.5);
        }

        public static AIQuestionDraftSnapshot SnapshotFromQuestion(SkillComment q)
        {
            if (q == null) return new AIQuestionDraftSnapshot();
            return new AIQuestionDraftSnapshot
            {
                title = q.Comment,
                gist = q.QuestionGist,
                expectedOutput = q.ExpectedOutput,
                explanation = q.AnswerExplanation,
                safeImplementation = q.SafeImplementation,
                semantic = q.SemanticMatchCriteria,
                redFlags = q.MisconceptionRedFlags,
                max = q.MaxScore,
                time = q.CodingTimeLimitInMinutes,
                code = null
            };
        }

        public static AIQuestionDraftSnapshot ParseSnapshot(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload)) return null;
            try
            {
                var jo = Newtonsoft.Json.Linq.JObject.Parse(payload);
                return new AIQuestionDraftSnapshot
                {
                    title = (string)(jo["title"] ?? jo["Comment"]),
                    gist = (string)(jo["gist"] ?? jo["QuestionGist"]),
                    expectedOutput = (string)(jo["out"] ?? jo["expectedOutput"] ?? jo["ExpectedOutput"]),
                    explanation = (string)(jo["exp"] ?? jo["explanation"] ?? jo["AnswerExplanation"]),
                    safeImplementation = (string)(jo["safe"] ?? jo["safeImplementation"] ?? jo["SafeImplementation"]),
                    semantic = (string)(jo["sem"] ?? jo["semantic"] ?? jo["SemanticMatchCriteria"]),
                    redFlags = (string)(jo["red"] ?? jo["redFlags"] ?? jo["MisconceptionRedFlags"]),
                    max = (int?)(jo["max"] ?? jo["MaxScore"]),
                    time = (int?)(jo["time"] ?? jo["CodingTimeLimitInMinutes"]),
                    code = (string)jo["code"]
                };
            }
            catch
            {
                return null;
            }
        }

        public static string SerializeSnapshot(AIQuestionDraftSnapshot snap)
        {
            snap = snap ?? new AIQuestionDraftSnapshot();
            return JsonConvert.SerializeObject(new
            {
                snap.title,
                snap.gist,
                @out = snap.expectedOutput,
                exp = snap.explanation,
                safe = snap.safeImplementation,
                sem = snap.semantic,
                red = snap.redFlags,
                snap.max,
                snap.time,
                snap.code
            });
        }

        public static string ConcatExpectedAnswer(SkillComment q)
        {
            if (q == null) return null;
            var parts = new[]
            {
                q.ExpectedOutput,
                q.AnswerExplanation,
                q.SafeImplementation,
                q.SemanticMatchCriteria,
                q.MisconceptionRedFlags
            };
            var filled = new List<string>();
            foreach (var p in parts)
            {
                if (!string.IsNullOrWhiteSpace(p)) filled.Add(p.Trim());
            }
            if (filled.Count == 0) return q.ExpectedAnswer;
            return string.Join("\n\n", filled);
        }

        public static List<string> ConfirmBlockers(SkillComment q, bool requireAiVideo = true)
        {
            var blockers = new List<string>();
            if (q == null)
            {
                blockers.Add("Question not found.");
                return blockers;
            }

            if (string.IsNullOrWhiteSpace(q.ExpectedOutput))
                blockers.Add("Expected answer — expected output is empty");
            if (string.IsNullOrWhiteSpace(q.AnswerExplanation))
                blockers.Add("Expected answer — explanation is empty");
            if (string.IsNullOrWhiteSpace(q.SafeImplementation))
                blockers.Add("Expected answer — safe implementation is empty");
            if (string.IsNullOrWhiteSpace(q.SemanticMatchCriteria))
                blockers.Add("Expected answer — semantic matching criteria is empty");
            if (string.IsNullOrWhiteSpace(q.MisconceptionRedFlags))
                blockers.Add("Expected answer — misconception red flags is empty");
            if (q.MaxScore <= 0)
                blockers.Add("Max score is not set (the report renders every score against it)");
            if (!q.CodingTimeLimitInMinutes.HasValue || q.CodingTimeLimitInMinutes.Value <= 0)
                blockers.Add("Time in minutes is not set");
            if (string.IsNullOrWhiteSpace(q.QuestionGist))
                blockers.Add("Avatar gist is empty");
            else if (GistContainsCodeSyntax(q.QuestionGist))
                blockers.Add("Avatar gist contains code syntax — the avatar reads it aloud");
            if (requireAiVideo)
            {
                if (q.AIVideoStatus == 3)
                    blockers.Add("AI video is stale — regenerate it before confirming");
                else if (q.AIVideoStatus == 1)
                    blockers.Add("AI video is still generating");
                else if (q.AIVideoStatus == 4 || (q.AIVideoStatus == 0 && string.IsNullOrWhiteSpace(q.AIQuestionVideoLink)))
                    blockers.Add("AI video has not been generated yet");
            }
            return blockers;
        }

        public static byte FieldSeverity(string fieldName, string aiValue, string smeValue)
        {
            if (TextEquals(aiValue, smeValue)) return 0;
            var major = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "ExpectedOutput", "AnswerExplanation", "SafeImplementation",
                "SemanticMatchCriteria", "MisconceptionRedFlags", "MaxScore", "RubricItemId", "out", "exp", "safe", "sem", "red", "max"
            };
            if (string.Equals(fieldName, "Comment", StringComparison.OrdinalIgnoreCase)
                || string.Equals(fieldName, "title", StringComparison.OrdinalIgnoreCase))
            {
                var ai = NormalizeText(aiValue);
                var sme = NormalizeText(smeValue);
                if (ai.Length == 0) return 1;
                var changed = 1.0 - (double)LongestCommonPrefix(ai, sme) / Math.Max(ai.Length, 1);
                if (changed > 0.6) return 3;
            }
            return major.Contains(fieldName) ? (byte)2 : (byte)1;
        }

        public static List<QuestionReviewLogRow> BuildReviewLogs(SkillComment live, string changedBy)
        {
            var logs = new List<QuestionReviewLogRow>();
            if (live == null) return logs;
            var snap = ParseSnapshot(live.AIDraftPayload);
            if (snap == null) return logs;

            void add(string field, string ai, string sme)
            {
                if (TextEquals(ai, sme)) return;
                logs.Add(new QuestionReviewLogRow
                {
                    QuestionBankId = live.id,
                    FieldName = field,
                    AIValue = ai,
                    SMEValue = sme,
                    Severity = FieldSeverity(field, ai, sme),
                    ChangedBy = changedBy
                });
            }

            add("Comment", snap.title, live.Comment);
            add("QuestionGist", snap.gist, live.QuestionGist);
            add("ExpectedOutput", snap.expectedOutput, live.ExpectedOutput);
            add("AnswerExplanation", snap.explanation, live.AnswerExplanation);
            add("SafeImplementation", snap.safeImplementation, live.SafeImplementation);
            add("SemanticMatchCriteria", snap.semantic, live.SemanticMatchCriteria);
            add("MisconceptionRedFlags", snap.redFlags, live.MisconceptionRedFlags);
            add("MaxScore", snap.max?.ToString(), live.MaxScore.ToString());
            add("TimeLimitMins", snap.time?.ToString(), live.CodingTimeLimitInMinutes?.ToString());
            return logs;
        }

        private static int LongestCommonPrefix(string a, string b)
        {
            var n = Math.Min(a.Length, b.Length);
            var i = 0;
            while (i < n && a[i] == b[i]) i++;
            return i;
        }

        public static void ApplySave(SkillComment target, SMEQuestionSaveRequest req)
        {
            if (target == null || req == null) return;
            var prevComment = target.Comment;
            var prevGist = target.QuestionGist;

            target.Comment = req.Comment;
            target.QuestionGist = req.QuestionGist;
            target.ExpectedOutput = req.ExpectedOutput;
            target.AnswerExplanation = req.AnswerExplanation;
            target.SafeImplementation = req.SafeImplementation;
            target.SemanticMatchCriteria = req.SemanticMatchCriteria;
            target.MisconceptionRedFlags = req.MisconceptionRedFlags;
            target.MaxScore = req.MaxScore ?? 0;
            target.CodingTimeLimitInMinutes = req.TimeLimitMins;
            target.IsCodingQuestion = req.IsCodingQuestion;
            target.IsFlaggedForSentimentAnalysis = req.FlagForSentiment;
            target.QuestionSlot = req.QuestionSlot;
            target.QuestionVariant = req.QuestionVariant;
            if (!string.IsNullOrWhiteSpace(req.SkillName))
                target.SkillName = req.SkillName;
            target.ExpectedAnswer = ConcatExpectedAnswer(target);

            if (!TextEquals(prevComment, target.Comment) || !TextEquals(prevGist, target.QuestionGist))
            {
                if (target.AIVideoStatus == 2)
                    target.AIVideoStatus = 3;
            }
        }
    }
}
