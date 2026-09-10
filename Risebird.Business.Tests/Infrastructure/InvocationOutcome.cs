namespace Risebird.Business.Tests.Infrastructure
{
    /// <summary>What happened when the smoke suite drove one method with auto-mocked inputs.</summary>
    internal sealed class InvocationOutcome
    {
        public required string Key { get; init; }

        /// <summary>The exception that escaped the method, or null when it completed.</summary>
        public Exception Exception { get; init; }

        /// <summary>True when the method did not return within the invocation budget.</summary>
        public bool TimedOut { get; init; }

        public bool Completed => Exception == null && !TimedOut;

        public string Describe() =>
            TimedOut ? $"{Key} — TIMED OUT"
                     : Exception == null ? $"{Key} — ok"
                                         : $"{Key} — {Exception.GetType().Name}: {Firstline(Exception.Message)}";

        private static string Firstline(string message)
        {
            if (string.IsNullOrEmpty(message)) return "(no message)";
            var idx = message.IndexOfAny(new[] { '\r', '\n' });
            return idx < 0 ? message : message.Substring(0, idx);
        }
    }
}
