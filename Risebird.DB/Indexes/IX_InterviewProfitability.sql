-- =============================================
-- Indexes for usp_GetInterviewProfitability and usp_GetOpenAICostOptimizationReport.
-- Every one is filtered to the rows that carry the key, so they stay small: most OpenAIApiLogs
-- rows predate per-interview attribution and have no MeetingId or IRFId.
-- ONLINE = ON keeps the tables writable while the index builds (Azure SQL supports it on all tiers).
-- =============================================

-- Filtered indexes need these on (SSMS has them on; sqlcmd does not by default).
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('OpenAIApiLogs', 'U') IS NOT NULL
   AND NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OpenAIApiLogs_MeetingId' AND object_id = OBJECT_ID('OpenAIApiLogs'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_OpenAIApiLogs_MeetingId
    ON [dbo].[OpenAIApiLogs] ([MeetingId])
    INCLUDE ([EstimatedCostUSD])
    WHERE [MeetingId] IS NOT NULL
    WITH (ONLINE = ON);
    PRINT 'Index IX_OpenAIApiLogs_MeetingId created';
END
GO

IF OBJECT_ID('OpenAIApiLogs', 'U') IS NOT NULL
   AND NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OpenAIApiLogs_IRFId_LogDate' AND object_id = OBJECT_ID('OpenAIApiLogs'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_OpenAIApiLogs_IRFId_LogDate
    ON [dbo].[OpenAIApiLogs] ([IRFId], [LogDate])
    INCLUDE ([EstimatedCostUSD], [MeetingId], [SessionId])
    WHERE [IRFId] IS NOT NULL
    WITH (ONLINE = ON);
    PRINT 'Index IX_OpenAIApiLogs_IRFId_LogDate created';
END
GO

IF OBJECT_ID('OpenAIApiLogs', 'U') IS NOT NULL
   AND NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OpenAIApiLogs_OperationType_LogDate' AND object_id = OBJECT_ID('OpenAIApiLogs'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_OpenAIApiLogs_OperationType_LogDate
    ON [dbo].[OpenAIApiLogs] ([OperationType], [LogDate])
    INCLUDE ([Model], [EstimatedCostUSD], [PromptTokens], [CompletionTokens], [IsSuccess], [ApiMethod])
    WITH (ONLINE = ON);
    PRINT 'Index IX_OpenAIApiLogs_OperationType_LogDate created';
END
GO

IF OBJECT_ID('InterviewPoints', 'U') IS NOT NULL
   AND NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_InterviewPoints_Interviewid' AND object_id = OBJECT_ID('InterviewPoints'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_InterviewPoints_Interviewid
    ON [dbo].[InterviewPoints] ([Interviewid])
    INCLUDE ([userid], [Points], [Status], [PenaltyAmount], [QCPoints], [PaymentSLA], [VideoReward])
    WHERE [Interviewid] IS NOT NULL AND [Interviewid] <> 0
    WITH (ONLINE = ON);
    PRINT 'Index IX_InterviewPoints_Interviewid created';
END
GO

IF OBJECT_ID('CallRecordingData', 'U') IS NOT NULL
   AND NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CallRecordingData_InterviewId' AND object_id = OBJECT_ID('CallRecordingData'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_CallRecordingData_InterviewId
    ON [dbo].[CallRecordingData] ([InterviewId])
    INCLUDE ([Price], [PriceUnit])
    WHERE [InterviewId] IS NOT NULL
    WITH (ONLINE = ON);
    PRINT 'Index IX_CallRecordingData_InterviewId created';
END
GO

IF OBJECT_ID('PlivoCXCallLogs', 'U') IS NOT NULL
   AND NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PlivoCXCallLogs_IRFId_CreatedAt' AND object_id = OBJECT_ID('PlivoCXCallLogs'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_PlivoCXCallLogs_IRFId_CreatedAt
    ON [dbo].[PlivoCXCallLogs] ([IRFId], [CreatedAt])
    INCLUDE ([DurationSeconds])
    WITH (ONLINE = ON);
    PRINT 'Index IX_PlivoCXCallLogs_IRFId_CreatedAt created';
END
GO
