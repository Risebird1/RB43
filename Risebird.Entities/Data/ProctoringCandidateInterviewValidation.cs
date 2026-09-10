namespace Risebird.Entities.Data
{
    /// <summary>
    /// Result of validating a DesktopProctor mint request's (interviewId, candidateEmail) pair
    /// against InterviewMeetings/InterviewComments — the credential pair itself, since an
    /// unauthenticated candidate has nothing else to present.
    /// </summary>
    public class ProctoringCandidateInterviewValidation
    {
        public bool InterviewExists { get; set; }
        public bool CandidateEmailMatches { get; set; }
        /// <summary>True unless the interview's MeetingStatus is a terminal one (completed/no-show/dropped/...).</summary>
        public bool IsJoinable { get; set; }
    }
}
