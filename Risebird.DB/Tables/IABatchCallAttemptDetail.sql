-- Per-call detail for a single batch-call attempt. One row per interviewer dialed during an attempt
-- (BatchCallLogId -> IABatchCallLog.Id). Lets us see exactly who was called, when, and the outcome
-- for every attempt against a campaign.
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'IABatchCallAttemptDetail')
BEGIN
    CREATE TABLE [dbo].[IABatchCallAttemptDetail](
        [Id]                   INT IDENTITY(1,1) NOT NULL,
        [BatchCallLogId]       INT NOT NULL,
        [InterviewerProfileId] INT NULL,
        [InterviewerName]      NVARCHAR(200) NULL,
        [InterviewerEmail]     NVARCHAR(256) NULL,
        [Phone]                VARCHAR(20) NULL,
        [Outcome]              VARCHAR(20) NOT NULL,   -- Succeeded | Failed | Skipped
        [Error]                NVARCHAR(1000) NULL,
        [ConversationId]       VARCHAR(200) NULL,
        [CalledAt]             DATETIME NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_IABatchCallAttemptDetail] PRIMARY KEY CLUSTERED ([Id] ASC)
    ) ON [PRIMARY];

    CREATE NONCLUSTERED INDEX [IX_IABatchCallAttemptDetail_BatchCallLogId]
        ON [dbo].[IABatchCallAttemptDetail] ([BatchCallLogId] ASC);
END
GO
