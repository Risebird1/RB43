-- Migration: Indexes to support usp_GetEligibleCandidatesForPlivoCXCalls
-- Scoped aggregations join on (RecipientEmail/candidateEmail, IRFId) and filter by CreatedAt

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_PlivoCXCallLogs_Email_IRFId_CreatedAt'
      AND object_id = OBJECT_ID(N'dbo.PlivoCXCallLogs')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_PlivoCXCallLogs_Email_IRFId_CreatedAt
    ON dbo.PlivoCXCallLogs (RecipientEmail, IRFId, CreatedAt)
    INCLUDE (CallStatus, CallPurpose, BookingApiSuccess, BookedInterviewDateTime);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_InterviewMeetings_CandidateEmail_IRFId'
      AND object_id = OBJECT_ID(N'dbo.InterviewMeetings')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_InterviewMeetings_CandidateEmail_IRFId
    ON dbo.InterviewMeetings (candidateEmail, IRFId)
    INCLUDE (confirmationStatus, interviewDate, IsDeleted, RedoInterview, id);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_InterviewComments_InterviewId'
      AND object_id = OBJECT_ID(N'dbo.InterviewComments')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_InterviewComments_InterviewId
    ON dbo.InterviewComments (InterviewId)
    INCLUDE (MeetingStatus);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Candidate_Status_Disposition'
      AND object_id = OBJECT_ID(N'dbo.Candidate')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Candidate_Status_Disposition
    ON dbo.Candidate (Status, PlivoCXDisposition)
    INCLUDE (IRFId, CandidateMasterId, Phone, VapiCallAttempts);
END
GO
