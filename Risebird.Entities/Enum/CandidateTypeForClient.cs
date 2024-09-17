using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.Enum
{
    public enum CandidateTypeForClient
    {
        [Description("Scheduled")]
        Scheduled = 1,

        [Display(Name = "Not Scheduled")]
        [Description("Not Scheduled")]
        NotScheduled = 2,

        [Display(Name = "Cold Candidates")]
        [Description("Cold Candidates")]
        ColdCandidates = 3,

        [Display(Name = "Not Processed Resumes")]
        [Description("Not Processed Resumes")]
        NotProcessedResumes = 4,

        [Display(Name = "Potential Cold Candidate ")]
        [Description("Potential Cold Candidate ")]
        PotentialColdCandidate = 5,

        [Display(Name = "Reschedule Meeting")]
        [Description("Reschedule Meeting")]
        RescheduleMeeting = 6,
        
        [Display(Name = "Future Scheduled")]
        [Description("Future Scheduled")]
        FutureScheduled = 7,

        [Display(Name = "Completed Meeting")]
        [Description("Completed Meeting")]
        CompletedMeeting = 8
    }
}
