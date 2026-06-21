-- Migration: Indexes to support usp_FetchCandidateFeedbackByInterviewer
-- Drive from filtered InterviewMeetings, then seek feedback and IRF rows

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_InterviewMeetings_Interviewer_ConfirmationStatus'
      AND object_id = OBJECT_ID(N'dbo.InterviewMeetings')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_InterviewMeetings_Interviewer_ConfirmationStatus
    ON dbo.InterviewMeetings (interviewer, confirmationStatus)
    INCLUDE (id, IRFId, candidateEmail, interviewDate);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_CandidateFeedbackDetails_InterviewId'
      AND object_id = OBJECT_ID(N'dbo.CandidateFeedbackDetails')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_CandidateFeedbackDetails_InterviewId
    ON dbo.CandidateFeedbackDetails (InterviewId);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_IRF_IRFId'
      AND object_id = OBJECT_ID(N'dbo.IRF')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_IRF_IRFId
    ON dbo.IRF (IRFId)
    INCLUDE (JDTiltle);
END
GO
