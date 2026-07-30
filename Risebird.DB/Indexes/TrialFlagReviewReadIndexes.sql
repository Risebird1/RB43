-- ============================================================================
-- Trial Flag Review — InterviewId probe indexes (lazy two-tier read model)
-- Safe / idempotent. Part 0 of trial-read-architecture.
-- ============================================================================

IF OBJECT_ID(N'dbo.PaymentSLADetails', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_PaymentSLADetails_InterviewId'
      AND object_id = OBJECT_ID(N'dbo.PaymentSLADetails'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_PaymentSLADetails_InterviewId]
        ON [dbo].[PaymentSLADetails] ([Interviewid] ASC)
        INCLUDE ([id], [PaymentDeduction], [Recommendation], [RecommendationComments]);
END
GO

IF OBJECT_ID(N'dbo.QcInterviewAIevaluation', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_QcInterviewAIevaluation_InterviewId'
      AND object_id = OBJECT_ID(N'dbo.QcInterviewAIevaluation'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_QcInterviewAIevaluation_InterviewId]
        ON [dbo].[QcInterviewAIevaluation] ([InterviewId] ASC, [id] DESC)
        INCLUDE ([qcmergeverdict], [qcmergescore], [AIMergeRedoInterview], [AIMergeInterviewerSuspicious]);
END
GO

IF OBJECT_ID(N'dbo.CandidateFeedbackDetails', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_CandidateFeedbackDetails_InterviewId'
      AND object_id = OBJECT_ID(N'dbo.CandidateFeedbackDetails'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_CandidateFeedbackDetails_InterviewId]
        ON [dbo].[CandidateFeedbackDetails] ([InterviewId] ASC, [Id] DESC)
        INCLUDE ([Rating]);
END
GO

IF OBJECT_ID(N'dbo.RedoInterviews', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_RedoInterviews_InterviewId'
      AND object_id = OBJECT_ID(N'dbo.RedoInterviews'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_RedoInterviews_InterviewId]
        ON [dbo].[RedoInterviews] ([interviewid] ASC);
END
GO

IF OBJECT_ID(N'dbo.InterviewMeetingsJoinTime', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_InterviewMeetingsJoinTime_InterviewId'
      AND object_id = OBJECT_ID(N'dbo.InterviewMeetingsJoinTime'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_InterviewMeetingsJoinTime_InterviewId]
        ON [dbo].[InterviewMeetingsJoinTime] ([Interviewid] ASC);
END
GO

IF OBJECT_ID(N'dbo.RiskCategory', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_RiskCategory_InterviewId'
      AND object_id = OBJECT_ID(N'dbo.RiskCategory'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_RiskCategory_InterviewId]
        ON [dbo].[RiskCategory] ([InterviewId] ASC)
        INCLUDE ([Severity], [Name], [Description], [SeverityBreakdown]);
END
GO

IF OBJECT_ID(N'dbo.InterviewComments', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_InterviewComments_InterviewId'
      AND object_id = OBJECT_ID(N'dbo.InterviewComments'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_InterviewComments_InterviewId]
        ON [dbo].[InterviewComments] ([InterviewId] ASC)
        INCLUDE ([AITranscriptAnalysisScore], [AIReportQCAnalysisScore]);
END
GO
