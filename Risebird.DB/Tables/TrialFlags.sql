IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'TrialFlags')
BEGIN
    CREATE TABLE [dbo].[TrialFlags] (
        [Id]                         INT              IDENTITY(1,1) NOT NULL,
        [InterviewerId]              INT              NOT NULL,
        [IrfId]                      INT              NOT NULL,
        [ChannelId]                  INT              NOT NULL,
        [TriggeringInterviewId]      INT              NOT NULL,
        [TriggeringInterviewDate]  DATETIME         NOT NULL,
        [TranscriptQualityPass]      BIT              NOT NULL,
        [ReportQualityPass]          BIT              NOT NULL,
        [DecisionQualityPass]        BIT              NOT NULL,
        [CandidateRatingPass]        BIT              NOT NULL,
        [JoinTimePass]               BIT              NOT NULL,
        [InterviewDurationPass]      BIT              NOT NULL,
        [TranscriptQualityScore]     DECIMAL(5,2)     NULL,
        [CandidateRating]            DECIMAL(5,2)     NULL,
        [JoinTimeMinutes]            DECIMAL(8,2)     NULL,
        [InterviewDurationMinutes]   DECIMAL(8,2)     NULL,
        [OverallVerdict]             VARCHAR(30)      NOT NULL,
        [ReviewStatus]               VARCHAR(20)      NOT NULL CONSTRAINT [DF_TrialFlags_ReviewStatus] DEFAULT ('pending'),
        [FlaggedAtUtc]               DATETIME         NOT NULL CONSTRAINT [DF_TrialFlags_FlaggedAtUtc] DEFAULT (GETUTCDATE()),
        [ReviewedAtUtc]              DATETIME         NULL,
        [ReviewedBy]                 VARCHAR(150)     NULL,
        [ReviewerComment]            NVARCHAR(500)    NULL,
        CONSTRAINT [PK_TrialFlags] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [UQ_TrialFlags_InterviewerIrf] UNIQUE ([InterviewerId], [IrfId])
    )
END
GO

IF OBJECT_ID(N'dbo.TrialFlags', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT * FROM sys.columns
    WHERE Name = N'Severity'
      AND Object_ID = OBJECT_ID(N'dbo.TrialFlags'))
BEGIN
    ALTER TABLE dbo.TrialFlags ADD Severity VARCHAR(4) NULL
END
GO

IF NOT EXISTS (
    SELECT * FROM sys.indexes
    WHERE name = N'IX_TrialFlags_ReviewStatus_FlaggedAt'
      AND object_id = OBJECT_ID(N'dbo.TrialFlags'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_TrialFlags_ReviewStatus_FlaggedAt]
        ON [dbo].[TrialFlags] ([ReviewStatus], [FlaggedAtUtc] DESC)
        INCLUDE ([IrfId], [ChannelId], [OverallVerdict], [TriggeringInterviewId])
END
GO

IF NOT EXISTS (
    SELECT * FROM sys.indexes
    WHERE name = N'IX_TrialFlags_TriggeringInterviewId'
      AND object_id = OBJECT_ID(N'dbo.TrialFlags'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_TrialFlags_TriggeringInterviewId]
        ON [dbo].[TrialFlags] ([TriggeringInterviewId])
END
GO
