-- Migration: Indexes to support usp_GetEligibleCandidatesForPlivoCXCalls
-- Call joins prefer CandidateId; fall back to (RecipientEmail, IRFId). Meeting blocks are date-scoped.

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_PlivoCXCallLogs_CandidateId_CreatedAt'
      AND object_id = OBJECT_ID(N'dbo.PlivoCXCallLogs')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_PlivoCXCallLogs_CandidateId_CreatedAt
    ON dbo.PlivoCXCallLogs (CandidateId, CreatedAt)
    INCLUDE (CallStatus, CallPurpose, BookingApiSuccess, BookedInterviewDateTime, RecipientEmail, IRFId)
    WHERE CandidateId IS NOT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_PlivoCXCallLogs_Email_IRFId_CreatedAt'
      AND object_id = OBJECT_ID(N'dbo.PlivoCXCallLogs')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_PlivoCXCallLogs_Email_IRFId_CreatedAt
    ON dbo.PlivoCXCallLogs (RecipientEmail, IRFId, CreatedAt)
    INCLUDE (CallStatus, CallPurpose, BookingApiSuccess, BookedInterviewDateTime, CandidateId);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_InterviewMeetings_CandidateEmail_IRFId_InterviewDate'
      AND object_id = OBJECT_ID(N'dbo.InterviewMeetings')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_InterviewMeetings_CandidateEmail_IRFId_InterviewDate
    ON dbo.InterviewMeetings (candidateEmail, IRFId, interviewDate)
    INCLUDE (confirmationStatus, IsDeleted, RedoInterview, id);
END
GO

-- Keep legacy name if already deployed; otherwise create date-inclusive index above.
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_InterviewMeetings_CandidateEmail_IRFId'
      AND object_id = OBJECT_ID(N'dbo.InterviewMeetings')
)
AND NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_InterviewMeetings_CandidateEmail_IRFId_InterviewDate'
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

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_IRF_EndDate_ActiveFlags'
      AND object_id = OBJECT_ID(N'dbo.IRF')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_IRF_EndDate_ActiveFlags
    ON dbo.IRF (EndDate, IsDeleted, IsPaused)
    INCLUDE (IRFId, ClientId, JDTiltle);
END
GO
