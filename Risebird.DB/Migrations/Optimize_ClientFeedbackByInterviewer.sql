-- Supports usp_FetchClientFeedbackByInterviewer (client feedback by interviewer email)

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_ClientCommentsForReport_InterviewId'
      AND object_id = OBJECT_ID(N'dbo.ClientCommentsForReport')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ClientCommentsForReport_InterviewId
    ON dbo.ClientCommentsForReport (InterviewId)
    INCLUDE (Comment, Rating, CreatedAt);
END
GO

-- Covering seek for interviewer lookups (candidateName used in client feedback list)
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_InterviewMeetings_Interviewer_Feedback'
      AND object_id = OBJECT_ID(N'dbo.InterviewMeetings')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_InterviewMeetings_Interviewer_Feedback
    ON dbo.InterviewMeetings (interviewer)
    INCLUDE (id, interviewDate, candidateName);
END
GO
