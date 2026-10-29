using System;
using System.Threading;

namespace Risebird.Entities.Data
{
    /// <summary>
    /// Ambient attribution for OpenAI cost logging. The AI helpers (AssistantServiceHelper,
    /// OpenAIResponsesHelper, ...) take only prompt text, so they cannot know which interview a call
    /// belongs to; the business code that does know opens a scope around the work instead:
    /// <code>
    /// using (OpenAIApiLogScope.Begin(meetingId: interviewId))
    /// {
    ///     await _assistantServiceHelper.GenerateTranscriptAnalysis(transcript);
    /// }
    /// </code>
    /// <c>OpenAIApiLogger</c> fills any id the call site left empty from the innermost open
    /// scope, so every OpenAIApiLogs row written inside it carries the interview, which is what
    /// the Interview Profitability grid attributes AI cost by. Scopes flow across awaits and nest;
    /// disposing one restores the outer scope. Opening a scope never changes what is sent to OpenAI.
    /// </summary>
    public static class OpenAIApiLogScope
    {
        private static readonly AsyncLocal<OpenAIApiLogContext> Current = new AsyncLocal<OpenAIApiLogContext>();

        /// <summary>The attribution of the innermost open scope, or null outside any scope.</summary>
        public static OpenAIApiLogContext Ambient => Current.Value;

        public static IDisposable Begin(
            int? meetingId = null,
            int? irfId = null,
            int? candidateId = null,
            int? interviewerId = null,
            Guid? sessionId = null)
        {
            var outer = Current.Value;
            Current.Value = new OpenAIApiLogContext
            {
                // Zero is how much of this codebase spells "no id"; logging it would attribute the
                // call to a meeting that does not exist.
                MeetingId = Positive(meetingId) ?? outer?.MeetingId,
                IRFId = Positive(irfId) ?? outer?.IRFId,
                CandidateId = Positive(candidateId) ?? outer?.CandidateId,
                InterviewerId = Positive(interviewerId) ?? outer?.InterviewerId,
                SessionId = (sessionId.HasValue && sessionId.Value != Guid.Empty ? sessionId : null) ?? outer?.SessionId
            };
            return new Restore(outer);
        }

        /// <summary>
        /// Returns the context to log: the call site's own values where it set them, the ambient
        /// scope's values where it did not. The caller's object is never mutated.
        /// </summary>
        public static OpenAIApiLogContext Apply(OpenAIApiLogContext explicitContext)
        {
            var ambient = Current.Value;
            if (ambient == null)
                return explicitContext;

            return new OpenAIApiLogContext
            {
                SessionId = explicitContext?.SessionId ?? ambient.SessionId,
                UserId = explicitContext?.UserId ?? ambient.UserId,
                CandidateId = explicitContext?.CandidateId ?? ambient.CandidateId,
                InterviewerId = explicitContext?.InterviewerId ?? ambient.InterviewerId,
                IRFId = explicitContext?.IRFId ?? ambient.IRFId,
                MeetingId = explicitContext?.MeetingId ?? ambient.MeetingId,
                OperationType = explicitContext?.OperationType ?? ambient.OperationType,
                CreatedBy = explicitContext?.CreatedBy ?? ambient.CreatedBy
            };
        }

        private static int? Positive(int? value) => value.HasValue && value.Value > 0 ? value : null;

        private sealed class Restore : IDisposable
        {
            private readonly OpenAIApiLogContext _outer;
            private bool _disposed;

            public Restore(OpenAIApiLogContext outer) => _outer = outer;

            public void Dispose()
            {
                if (_disposed)
                    return;
                _disposed = true;
                Current.Value = _outer;
            }
        }
    }
}
