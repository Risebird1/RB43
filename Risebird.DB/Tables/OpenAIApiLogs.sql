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
    [OperationType] NVARCHAR(100) NULL, -- e.g., 'ResumeScreening', 'ReportQC', 'TranscriptAnalysis', 'RubricGeneration', 'AIInterview', 'SpeechToText', 'TextToSpeech'
    [CreatedBy] NVARCHAR(100) NULL,
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    [ModifiedAt] DATETIME2(7) NULL
);

GO

-- Create indexes for common queries
CREATE NONCLUSTERED INDEX IX_OpenAIApiLogs_LogDate 
    ON [dbo].[OpenAIApiLogs] ([LogDate] DESC);

CREATE NONCLUSTERED INDEX IX_OpenAIApiLogs_SessionId 
    ON [dbo].[OpenAIApiLogs] ([SessionId]) 
    WHERE [SessionId] IS NOT NULL;

CREATE NONCLUSTERED INDEX IX_OpenAIApiLogs_OperationType 
    ON [dbo].[OpenAIApiLogs] ([OperationType]) 
    WHERE [OperationType] IS NOT NULL;

CREATE NONCLUSTERED INDEX IX_OpenAIApiLogs_Model 
    ON [dbo].[OpenAIApiLogs] ([Model]) 
    WHERE [Model] IS NOT NULL;

CREATE NONCLUSTERED INDEX IX_OpenAIApiLogs_IsSuccess 
    ON [dbo].[OpenAIApiLogs] ([IsSuccess], [LogDate] DESC);

GO
