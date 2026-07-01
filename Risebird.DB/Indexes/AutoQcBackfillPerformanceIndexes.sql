-- Performance indexes for trial-flag backfill + auto-QC backfill candidates.
-- Safe to run multiple times.

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_TrialFlags_TriggeringInterviewId'
      AND object_id = OBJECT_ID(N'dbo.TrialFlags'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_TrialFlags_TriggeringInterviewId]
        ON [dbo].[TrialFlags] ([TriggeringInterviewId]);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_QcInterviewAIevaluation_QCDone_QCDoneOn'
      AND object_id = OBJECT_ID(N'dbo.QcInterviewAIevaluation'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_QcInterviewAIevaluation_QCDone_QCDoneOn]
        ON [dbo].[QcInterviewAIevaluation] ([QCDone] ASC, [QCDoneOn] DESC)
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
        INCLUDE ([interviewer], [interviewDate], [confirmationStatus]);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_InterviewMeetings_IRFId_interviewer_date'
      AND object_id = OBJECT_ID(N'dbo.InterviewMeetings'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_InterviewMeetings_IRFId_interviewer_date]
        ON [dbo].[InterviewMeetings] ([IRFId] ASC, [interviewer] ASC, [interviewDate] ASC, [id] ASC)
        INCLUDE ([confirmationStatus]);
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
