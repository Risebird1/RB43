namespace Risebird.Entities.Data
{
    public class AssessmentGroupInterviewerDetails
    {
        public int IRFId { get; set; }

        public string EmailId { get; set; }

        public string uniqueid { get; set; }
        public string InterviewerName { get; set; }

        public int Assessmentid { get; set; }

        //public int PerSuccessRate { get; set; }
        public string JDLink { get; set; }
        public string JDTitle { get; set; }
        public int AvailbilityHour { get; set; }
        public int FlatRate { get; set; }
        public string BasicSkill { get; set; }
        public string CountryTypeDesc { get; set; }
        public string CompanyTypeDesc { get; set; }
        public string PlanTypeDesc { get; set; }
        public int SelectedDurationMin { get; set; }
        public int RejectedDurationMin { get; set; }
        public string AGName { get; set; }
        public int AGAvailabilityCommunicationTimeElapsed { get; set; }
        public bool IsVideoRequired { get; set; }
        public string Phone { get; set; }

    }
}
