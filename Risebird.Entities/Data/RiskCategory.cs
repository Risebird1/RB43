using System;

namespace Risebird.Entities.Data
{
    public class RiskCategory
    {
        public int Id { get; set; }
        public int InterviewId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? ModifiedOn { get; set; }

        // Interview info joined from InterviewMeetings (read-only, populated by usp_GetRiskCategoryByInterviewId)
        public string CandidateName { get; set; }
        public string CandidateEmail { get; set; }
        public string Interviewer { get; set; }
        public DateTime? InterviewDate { get; set; }
        public string PrimarySkill { get; set; }
    }
}
