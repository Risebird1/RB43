IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'AutoQcBackfillRun')
BEGIN
    CREATE TABLE [dbo].[AutoQcBackfillRun] (
        [Id]                       INT              IDENTITY(1,1) NOT NULL,
        [BatchId]                  UNIQUEIDENTIFIER NOT NULL,
        [ExecutedAtUtc]            DATETIME         NOT NULL DEFAULT GETUTCDATE(),
        [RecordsRemoved]           INT              NOT NULL DEFAULT 0,
        [RecordsSkipped]           INT              NOT NULL DEFAULT 0,
        [TriggeringInterviewCount] INT              NOT NULL DEFAULT 0,
        [ExecutedBy]               VARCHAR(150)     NOT NULL,
        CONSTRAINT [PK_AutoQcBackfillRun] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [UQ_AutoQcBackfillRun_BatchId] UNIQUE ([BatchId])
    )
END
GO

IF NOT EXISTS (
    SELECT * FROM sys.columns
    WHERE Name = N'AuditTag' AND Object_ID = Object_ID(N'TransInterviewerAuditLog'))
BEGIN
    ALTER TABLE [dbo].[TransInterviewerAuditLog] ADD [AuditTag] VARCHAR(50) NULL
END
GO

IF NOT EXISTS (
    SELECT * FROM sys.columns
    WHERE Name = N'AuditSource' AND Object_ID = Object_ID(N'TransInterviewerAuditLog'))
BEGIN
    ALTER TABLE [dbo].[TransInterviewerAuditLog] ADD [AuditSource] VARCHAR(50) NULL
END
GO

IF NOT EXISTS (
    SELECT * FROM sys.columns
    WHERE Name = N'BackfillBatchId' AND Object_ID = Object_ID(N'TransInterviewerAuditLog'))
BEGIN
    ALTER TABLE [dbo].[TransInterviewerAuditLog] ADD [BackfillBatchId] UNIQUEIDENTIFIER NULL
END
GO

IF NOT EXISTS (
    SELECT * FROM sys.columns
    WHERE Name = N'ExtraMetadata' AND Object_ID = Object_ID(N'TransInterviewerAuditLog'))
BEGIN
    ALTER TABLE [dbo].[TransInterviewerAuditLog] ADD [ExtraMetadata] NVARCHAR(MAX) NULL
END
GO

IF NOT EXISTS (
    SELECT * FROM sys.indexes
    WHERE name = N'IX_TransInterviewerAuditLog_AutoQcRemoval'
      AND object_id = OBJECT_ID(N'dbo.TransInterviewerAuditLog'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_TransInterviewerAuditLog_AutoQcRemoval]
        ON [dbo].[TransInterviewerAuditLog] ([EventType], [AuditSource], [ChangedAt] DESC)
        INCLUDE ([TransInterviewerId], [ToValue], [TriggeringInterviewId], [BackfillBatchId])
END
GO
