namespace Risebird.Entities.Data
{
    public static class ProfileNames
    {
        public static readonly string Admin = "Admin";
        public static readonly string SupportAdmin = "SupportAdmin";
        public static readonly string Interviewer = "Interviewer";
        public static readonly string Client = "Client";
        public static readonly string SeniorHR = "SeniorHR";
        public static readonly string HR = "HR";
        public static readonly string QualityController = "Quality Controller";
        public static readonly string Finance = "Finance";
        public static readonly string FinanceSupport = "Finance Support";
        public static readonly string InterviewerManagement = "InterviewerManagement";
        public static readonly string SeniorInterviewerManagement = "SeniorInterviewerManagement";
        public static readonly string Recruiter = "Recruiter";
        public static readonly string Candidate = "Candidate";
        public static readonly string ResumeParserAIAssistant = "ResumeParserAIAssistant";  // this profile is created solely for the purpose of Resume Parsing using OpenAI's assistant and does not map to any Risebird-defined profiles. Added here to avoid duplicacy of creating another enum that lists Profiles.    
        public static readonly string ResumeParserPublicUrlAIAssistant = "ResumeParserPublicUrlAIAssistant";
        public static readonly string ResumeProfileSummaryAIAssistant = "ResumeProfileSummaryAIAssistant";
        public static readonly string TranscriptAnalysisAIAssistant = "TranscriptAnalysisAIAssistant";  // this profile is created solely for the purpose of Resume Parsing using OpenAI's assistant and does not map to any Risebird-defined profiles. Added here to avoid duplicacy of creating another enum that lists Profiles.    
        public static readonly string StringResumeAnalysisAIAssistant = "StringResumeAnalysisAIAssistant";  // this profile is created solely for the purpose of Resume Parsing using OpenAI's assistant and does not map to any Risebird-defined profiles. Added here to avoid duplicacy of creating another enum that lists Profiles.    
        public static readonly string StringReportQCAIAssistant = "StringReportQCAIAssistant";  // this profile is created solely for the purpose of Resume Parsing using OpenAI's assistant and does not map to any Risebird-defined profiles. Added here to avoid duplicacy of creating another enum that lists Profiles.    
        public static readonly string TranscriptToNotesAssistantId = "TranscriptToNotesAssistantId";
        public static readonly string QcInterviewAIAssistantId = "QcInterviewAIMerge";
        public static readonly string ContradictionReportAIAssistantId = "ContradictionReport";
        public static readonly string PaymentTicketReviewAssistantId = "PaymentTicketReview";
        public static readonly string LinkedInVerificationAssistantId = "LinkedInVerification";
    }
}
