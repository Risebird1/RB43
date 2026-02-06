-- =============================================
-- OpenAI API Logging System Deployment Script
-- =============================================
-- This script creates all necessary database objects for the OpenAI API logging system
-- Run this script on your database to deploy the logging infrastructure
-- =============================================

PRINT 'Starting OpenAI API Logging System deployment...'
GO

-- =============================================
-- Step 1: Create the OpenAIApiLogs table
-- =============================================
PRINT 'Creating OpenAIApiLogs table...'
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[OpenAIApiLogs]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[OpenAIApiLogs] (
        [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [LogDate] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
        [ApiEndpoint] NVARCHAR(500) NOT NULL,
        [ApiMethod] NVARCHAR(50) NOT NULL,
        [Model] NVARCHAR(100) NULL,
        [AssistantId] NVARCHAR(100) NULL,
        [ThreadId] NVARCHAR(100) NULL,
        [RunId] NVARCHAR(100) NULL,
        [FileId] NVARCHAR(100) NULL,
        [SessionId] UNIQUEIDENTIFIER NULL,
        [RequestPayload] NVARCHAR(MAX) NULL,
        [ResponsePayload] NVARCHAR(MAX) NULL,
        [PromptTokens] INT NULL DEFAULT 0,
        [CompletionTokens] INT NULL DEFAULT 0,
        [TotalTokens] INT NULL DEFAULT 0,
        [EstimatedCostUSD] DECIMAL(18, 6) NULL DEFAULT 0,
        [ResponseTimeMs] INT NULL,
        [StatusCode] INT NULL,
        [IsSuccess] BIT NOT NULL DEFAULT 1,
        [ErrorMessage] NVARCHAR(MAX) NULL,
        [UserId] INT NULL,
        [CandidateId] INT NULL,
        [InterviewerId] INT NULL,
        [IRFId] INT NULL,
        [MeetingId] INT NULL,
        [OperationType] NVARCHAR(100) NULL,
        [CreatedBy] NVARCHAR(100) NULL,
        [CreatedAt] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
        [ModifiedAt] DATETIME2(7) NULL
    );
    PRINT 'OpenAIApiLogs table created successfully.'
END
ELSE
BEGIN
    PRINT 'OpenAIApiLogs table already exists.'
END
GO

-- =============================================
-- Step 2: Create indexes
-- =============================================
PRINT 'Creating indexes...'
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OpenAIApiLogs_LogDate' AND object_id = OBJECT_ID('OpenAIApiLogs'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_OpenAIApiLogs_LogDate 
        ON [dbo].[OpenAIApiLogs] ([LogDate] DESC);
    PRINT 'Index IX_OpenAIApiLogs_LogDate created.'
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OpenAIApiLogs_SessionId' AND object_id = OBJECT_ID('OpenAIApiLogs'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_OpenAIApiLogs_SessionId 
        ON [dbo].[OpenAIApiLogs] ([SessionId]) 
        WHERE [SessionId] IS NOT NULL;
    PRINT 'Index IX_OpenAIApiLogs_SessionId created.'
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OpenAIApiLogs_OperationType' AND object_id = OBJECT_ID('OpenAIApiLogs'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_OpenAIApiLogs_OperationType 
        ON [dbo].[OpenAIApiLogs] ([OperationType]) 
        WHERE [OperationType] IS NOT NULL;
    PRINT 'Index IX_OpenAIApiLogs_OperationType created.'
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OpenAIApiLogs_Model' AND object_id = OBJECT_ID('OpenAIApiLogs'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_OpenAIApiLogs_Model 
        ON [dbo].[OpenAIApiLogs] ([Model]) 
        WHERE [Model] IS NOT NULL;
    PRINT 'Index IX_OpenAIApiLogs_Model created.'
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OpenAIApiLogs_IsSuccess' AND object_id = OBJECT_ID('OpenAIApiLogs'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_OpenAIApiLogs_IsSuccess 
        ON [dbo].[OpenAIApiLogs] ([IsSuccess], [LogDate] DESC);
    PRINT 'Index IX_OpenAIApiLogs_IsSuccess created.'
END
GO

-- =============================================
-- Step 3: Create stored procedures
-- =============================================
PRINT 'Creating stored procedures...'
GO

-- Stored Procedure: usp_GetOpenAICostSummaryByDateRange
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_GetOpenAICostSummaryByDateRange]') AND type in (N'P'))
    DROP PROCEDURE [dbo].[usp_GetOpenAICostSummaryByDateRange]
GO

CREATE PROCEDURE [dbo].[usp_GetOpenAICostSummaryByDateRange]
    @StartDate DATETIME2(7),
    @EndDate DATETIME2(7)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        CAST(LogDate AS DATE) AS [Date],
        OperationType,
        Model,
        COUNT(*) AS TotalCalls,
        SUM(CASE WHEN IsSuccess = 1 THEN 1 ELSE 0 END) AS SuccessfulCalls,
        SUM(CASE WHEN IsSuccess = 0 THEN 1 ELSE 0 END) AS FailedCalls,
        ISNULL(SUM(TotalTokens), 0) AS TotalTokens,
        ISNULL(SUM(PromptTokens), 0) AS TotalPromptTokens,
        ISNULL(SUM(CompletionTokens), 0) AS TotalCompletionTokens,
        ISNULL(SUM(EstimatedCostUSD), 0) AS TotalCostUSD,
        ISNULL(AVG(ResponseTimeMs), 0) AS AvgResponseTimeMs,
        ISNULL(MIN(EstimatedCostUSD), 0) AS MinCostUSD,
        ISNULL(MAX(EstimatedCostUSD), 0) AS MaxCostUSD
    FROM OpenAIApiLogs
    WHERE LogDate >= @StartDate AND LogDate <= @EndDate
    GROUP BY CAST(LogDate AS DATE), OperationType, Model
    ORDER BY [Date] DESC, TotalCostUSD DESC;
END
GO
PRINT 'Stored procedure usp_GetOpenAICostSummaryByDateRange created.'
GO

-- Stored Procedure: usp_GetOpenAICostByCandidate
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_GetOpenAICostByCandidate]') AND type in (N'P'))
    DROP PROCEDURE [dbo].[usp_GetOpenAICostByCandidate]
GO

CREATE PROCEDURE [dbo].[usp_GetOpenAICostByCandidate]
    @StartDate DATETIME2(7) = NULL,
    @EndDate DATETIME2(7) = NULL,
    @Top INT = 100
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@Top)
        CandidateId,
        COUNT(*) AS TotalCalls,
        SUM(CASE WHEN IsSuccess = 1 THEN 1 ELSE 0 END) AS SuccessfulCalls,
        ISNULL(SUM(TotalTokens), 0) AS TotalTokens,
        ISNULL(SUM(EstimatedCostUSD), 0) AS TotalCostUSD,
        MIN(LogDate) AS FirstCallDate,
        MAX(LogDate) AS LastCallDate
    FROM OpenAIApiLogs
    WHERE CandidateId IS NOT NULL
        AND (@StartDate IS NULL OR LogDate >= @StartDate)
        AND (@EndDate IS NULL OR LogDate <= @EndDate)
    GROUP BY CandidateId
    ORDER BY TotalCostUSD DESC;
END
GO
PRINT 'Stored procedure usp_GetOpenAICostByCandidate created.'
GO

-- Stored Procedure: usp_GetOpenAICostByInterviewer
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_GetOpenAICostByInterviewer]') AND type in (N'P'))
    DROP PROCEDURE [dbo].[usp_GetOpenAICostByInterviewer]
GO

CREATE PROCEDURE [dbo].[usp_GetOpenAICostByInterviewer]
    @StartDate DATETIME2(7) = NULL,
    @EndDate DATETIME2(7) = NULL,
    @Top INT = 100
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@Top)
        InterviewerId,
        COUNT(*) AS TotalCalls,
        SUM(CASE WHEN IsSuccess = 1 THEN 1 ELSE 0 END) AS SuccessfulCalls,
        ISNULL(SUM(TotalTokens), 0) AS TotalTokens,
        ISNULL(SUM(EstimatedCostUSD), 0) AS TotalCostUSD,
        MIN(LogDate) AS FirstCallDate,
        MAX(LogDate) AS LastCallDate
    FROM OpenAIApiLogs
    WHERE InterviewerId IS NOT NULL
        AND (@StartDate IS NULL OR LogDate >= @StartDate)
        AND (@EndDate IS NULL OR LogDate <= @EndDate)
    GROUP BY InterviewerId
    ORDER BY TotalCostUSD DESC;
END
GO
PRINT 'Stored procedure usp_GetOpenAICostByInterviewer created.'
GO

-- Stored Procedure: usp_GetOpenAICostByIRF
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_GetOpenAICostByIRF]') AND type in (N'P'))
    DROP PROCEDURE [dbo].[usp_GetOpenAICostByIRF]
GO

CREATE PROCEDURE [dbo].[usp_GetOpenAICostByIRF]
    @StartDate DATETIME2(7) = NULL,
    @EndDate DATETIME2(7) = NULL,
    @Top INT = 100
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@Top)
        IRFId,
        COUNT(*) AS TotalCalls,
        SUM(CASE WHEN IsSuccess = 1 THEN 1 ELSE 0 END) AS SuccessfulCalls,
        ISNULL(SUM(TotalTokens), 0) AS TotalTokens,
        ISNULL(SUM(EstimatedCostUSD), 0) AS TotalCostUSD,
        MIN(LogDate) AS FirstCallDate,
        MAX(LogDate) AS LastCallDate
    FROM OpenAIApiLogs
    WHERE IRFId IS NOT NULL
        AND (@StartDate IS NULL OR LogDate >= @StartDate)
        AND (@EndDate IS NULL OR LogDate <= @EndDate)
    GROUP BY IRFId
    ORDER BY TotalCostUSD DESC;
END
GO
PRINT 'Stored procedure usp_GetOpenAICostByIRF created.'
GO

-- =============================================
-- Deployment Complete
-- =============================================
PRINT ''
PRINT '========================================='
PRINT 'OpenAI API Logging System deployment completed successfully!'
PRINT '========================================='
PRINT ''
PRINT 'Created objects:'
PRINT '  - Table: OpenAIApiLogs'
PRINT '  - 5 Indexes'
PRINT '  - 4 Stored Procedures'
PRINT ''
PRINT 'You can now start using the logging system.'
PRINT ''
GO
