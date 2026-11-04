namespace Risebird.Entities.Helpers
{
    /// <summary>
    /// Kill switch for phone calls (Plivo CX / Twilio IVR / campaign agents) that ask an interviewer
    /// whether they are available to take interviews. Those calls are turned off: interviewers were
    /// the most likely to complain and ask us for proof of consent, and the availability email does
    /// the same job. The calling code is kept in place behind this flag; set it to true to bring the
    /// calls back.
    /// </summary>
    public static class InterviewerAvailabilityCalls
    {
        // static readonly rather than const, so the guarded code still compiles without unreachable-code warnings.
        public static readonly bool Enabled = true;

        public const string DisabledMessage = "Availability calls to interviewers are disabled. Interviewer availability is checked by email.";
    }
}
