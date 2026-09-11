-- DesktopProctor periodic screenshot reference — one row per POST /api/proctoring/session/screenshot
-- call. Stores the Azure Blob Storage URL, not the image itself. Unlike ProctoringLogs, screenshots
-- are not durably queued and retried by the client (best-effort telemetry, sequence may have gaps),
-- but the insert is still idempotent on (ProctoringSessionId, Sequence) in case of an occasional
-- redelivery, same pattern as ProctoringLogs. FKs to ProctoringSessions.id, so run
-- Tables\ProctoringSessions.sql first.
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = N'Screenshots')
BEGIN
CREATE TABLE [dbo].[Screenshots](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[ProctoringSessionId] [int] NOT NULL,
	[Sequence] [int] NOT NULL,
	[BlobUrl] [nvarchar](1000) NOT NULL,
	[ContentType] [varchar](100) NULL,
	[CapturedAtUtc] [datetime2](3) NOT NULL,
	[CreatedAtUtc] [datetime2](3) NOT NULL CONSTRAINT [DF_Screenshots_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
 CONSTRAINT [PK_Screenshots] PRIMARY KEY CLUSTERED
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO

IF OBJECT_ID('dbo.ProctoringSessions') IS NOT NULL AND NOT EXISTS (
    SELECT * FROM sys.foreign_keys WHERE name = 'FK_Screenshots_ProctoringSessions'
)
BEGIN
    ALTER TABLE [dbo].[Screenshots]
    WITH CHECK ADD CONSTRAINT [FK_Screenshots_ProctoringSessions] FOREIGN KEY([ProctoringSessionId])
    REFERENCES [dbo].[ProctoringSessions] ([id])
    ON DELETE CASCADE;
END
GO

IF NOT EXISTS (
    SELECT * FROM sys.indexes
    WHERE name = 'UQ_Screenshots_SessionSequence'
    AND object_id = OBJECT_ID('Screenshots')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UQ_Screenshots_SessionSequence
    ON Screenshots (ProctoringSessionId, [Sequence]);
END
GO
