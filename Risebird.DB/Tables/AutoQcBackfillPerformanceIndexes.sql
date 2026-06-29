-- Performance indexes for per-IRF auto-QC backfill preview/candidates.
-- Safe to run multiple times.

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_QcInterviewAIevaluation_QCDone_QCDoneOn'
      AND object_id = OBJECT_ID(N'dbo.QcInterviewAIevaluation'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_QcInterviewAIevaluation_QCDone_QCDoneOn]
        ON [dbo].[QcInterviewAIevaluation] ([QCDone] ASC, [QCDoneOn] ASC)
        INCLUDE ([InterviewId], [AIMergeInterviewerSuspicious], [AIMergeRedoInterview]);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_InterviewMeetings_IRFId_id'
      AND object_id = OBJECT_ID(N'dbo.InterviewMeetings'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_InterviewMeetings_IRFId_id]
        ON [dbo].[InterviewMeetings] ([IRFId] ASC, [id] ASC)
        INCLUDE ([interviewer]);
END
GO

IF OBJECT_ID(N'dbo.TransInterviewerAuditLog', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_TransInterviewerAuditLog_AutoQcFlag'
      AND object_id = OBJECT_ID(N'dbo.TransInterviewerAuditLog'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_TransInterviewerAuditLog_AutoQcFlag]
        ON [dbo].[TransInterviewerAuditLog] ([EventType] ASC, [TriggeringInterviewId] ASC)
        INCLUDE ([TransInterviewerId]);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_TransInterviewer_IRFId_UserId'
      AND object_id = OBJECT_ID(N'dbo.TransInterviewer'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_TransInterviewer_IRFId_UserId]
        ON [dbo].[TransInterviewer] ([IRFId] ASC, [UserId] ASC)
        INCLUDE ([InterviewerId], [AutoQcVerdict]);
END
GO
