using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// Puts the candidate's code under the question it answers in a client report's "Questions Discussed".
    ///
    /// The report stores each discussed question as text only (InterviewerQuestionsDiscussed.Question, picked from the
    /// interviewer's own TechSkillComments list), and a code session belongs to one of those TechSkillComments questions.
    /// So a discussed question is matched to a session by its text - against both the session's snapshot of the question
    /// and the question as it reads now - ignoring case and whitespace. Sessions that no discussed question matches are
    /// reported as Unmatched, so the report can still show them and no candidate code is ever hidden.
    ///
    /// Enabled is false when the interview has no code sessions: the report then renders exactly as before, with no
    /// "No code is submitted yet" under questions of interviews that never had a coding question.
    /// </summary>
    public sealed class CodeSessionQuestionMatcher
    {
        private readonly Dictionary<string, InterviewCodeSession> _byQuestion = new Dictionary<string, InterviewCodeSession>(StringComparer.Ordinal);
        private readonly List<InterviewCodeSession> _sessions;
        private readonly HashSet<int> _matchedSessionIds = new HashSet<int>();

        public static readonly CodeSessionQuestionMatcher Disabled = new CodeSessionQuestionMatcher(null, null);

        public CodeSessionQuestionMatcher(IEnumerable<InterviewCodeSession> sessions, IEnumerable<string> discussedQuestions)
        {
            _sessions = (sessions ?? Enumerable.Empty<InterviewCodeSession>()).Where(s => s != null).ToList();

            foreach (var session in _sessions)
            {
                foreach (var text in new[] { session.QuestionText, session.CurrentQuestionText })
                {
                    var key = Normalize(text);
                    if (key.Length > 0 && !_byQuestion.ContainsKey(key))
                    {
                        _byQuestion[key] = session;
                    }
                }
            }

            // Matched up front over every discussed question (both skill sets), so Unmatched is known before any of the
            // report is rendered and does not depend on which section is drawn first.
            foreach (var question in discussedQuestions ?? Enumerable.Empty<string>())
            {
                var session = Find(question);
                if (session != null)
                {
                    _matchedSessionIds.Add(session.Id);
                }
            }
        }

        /// <summary>True when the interview has at least one code session.</summary>
        public bool Enabled => _sessions.Count > 0;

        /// <summary>The code session for a discussed question, or null when it has none.</summary>
        public InterviewCodeSession Find(string discussedQuestion)
        {
            var key = Normalize(discussedQuestion);
            return key.Length > 0 && _byQuestion.TryGetValue(key, out var session) ? session : null;
        }

        /// <summary>Code sessions that no discussed question matched, in the order they were created.</summary>
        public IList<InterviewCodeSession> Unmatched =>
            _sessions.Where(s => !_matchedSessionIds.Contains(s.Id)).ToList();

        internal static string Normalize(string text) =>
            string.IsNullOrWhiteSpace(text) ? string.Empty : Regex.Replace(text, @"\s+", " ").Trim().ToLowerInvariant();
    }

    /// <summary>Model of _QuestionCandidateCode.cshtml: one question's code block in a report.</summary>
    public sealed class QuestionCandidateCodeVM
    {
        /// <summary>The session for this question, or null ("No code is submitted yet").</summary>
        public InterviewCodeSession Session { get; set; }

        /// <summary>"li" inside a question list, "tr" inside a question table (ColumnSpan columns wide), "div" otherwise.</summary>
        public string Wrapper { get; set; } = "div";

        public int ColumnSpan { get; set; } = 1;

        /// <summary>Set for a code session no discussed question matched: the question is shown with it.</summary>
        public bool ShowQuestion { get; set; }
    }
}
