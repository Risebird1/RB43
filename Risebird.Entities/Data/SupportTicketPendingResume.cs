using System;

namespace Risebird.Entities.Data
{
    /// <summary>
    /// Represents a resume awaiting HR review before ingestion.
    /// </summary>
    public class SupportTicketPendingResume
    {
        public int       Id                          { get; set; }
        public int       SupportTicketId             { get; set; }
        public string    ResumeUrl                   { get; set; }
        public string    CandidateName               { get; set; }
        public string    CandidateEmail              { get; set; }
        
        // Resolved IRF
        public int?      ResolvedIRFId               { get; set; }
        public string    ResolvedIRFStringId         { get; set; }
        public string    ResolvedJobTitle            { get; set; }
        public string    ResolvedHREmail             { get; set; }
        
        // Scheduling data
        public bool      HasSchedulingData           { get; set; }
        public DateTime? ScheduledInterviewDateTime  { get; set; }
        public string    ScheduleTimezone            { get; set; }
        
        // Status
        public string    Status                      { get; set; }
        public int?      IngestedResumeId            { get; set; }
        public DateTime? ProcessedAt                 { get; set; }
        public string    ProcessedBy                 { get; set; }
        public DateTime  CreatedAt                   { get; set; }
        
        // Additional info from joins
        public string    IRFJobTitle                 { get; set; }
        public string    ClientName                  { get; set; }
        public string    TenantId                    { get; set; }
    }
}
