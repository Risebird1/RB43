IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'TransInterviewerAuditLog')
BEGIN
    CREATE TABLE [dbo].[TransInterviewerAuditLog] (
        [Id]                     INT            IDENTITY(1,1) NOT NULL,
        [TransInterviewerId]     INT            NOT NULL,
        [EventType]              VARCHAR(50)    NOT NULL,
        [FromValue]              NVARCHAR(500)  NULL,
        [ToValue]                NVARCHAR(500)  NULL,
        [ReasonCategory]         NVARCHAR(100)  NULL,
        [TriggeringInterviewId]  INT            NULL,
        [ChangedBy]              VARCHAR(150)   NOT NULL,
        [ChangedAt]              DATETIME       NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_TransInterviewerAuditLog] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_TransInterviewerAuditLog_TransInterviewer]
            FOREIGN KEY ([TransInterviewerId]) REFERENCES [dbo].[TransInterviewer] ([id])
    )

    CREATE NONCLUSTERED INDEX [IX_TransInterviewerAuditLog_TransInterviewerId]
        ON [dbo].[TransInterviewerAuditLog] ([TransInterviewerId] ASC, [ChangedAt] DESC, [Id] DESC)
        INCLUDE ([EventType], [ReasonCategory], [TriggeringInterviewId])
END
GO

IF NOT EXISTS (
    SELECT * FROM sys.columns
    WHERE Name = N'AutoQcVerdict' AND Object_ID = Object_ID(N'TransInterviewer'))
BEGIN
    ALTER TABLE [dbo].[TransInterviewer] ADD [AutoQcVerdict] VARCHAR(20) NULL
END
GO

IF NOT EXISTS (
    SELECT * FROM sys.columns
    WHERE Name = N'AutoQcTriggeringInterviewId' AND Object_ID = Object_ID(N'TransInterviewer'))
BEGIN
    ALTER TABLE [dbo].[TransInterviewer] ADD [AutoQcTriggeringInterviewId] INT NULL
END
GO

IF NOT EXISTS (
    SELECT * FROM sys.columns
    WHERE Name = N'AutoQcRemovedAt' AND Object_ID = Object_ID(N'TransInterviewer'))
BEGIN
    ALTER TABLE [dbo].[TransInterviewer] ADD [AutoQcRemovedAt] DATETIME NULL
END
GO
