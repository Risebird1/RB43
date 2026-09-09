-- DesktopProctor API violation/telemetry event log — one row per item from a
-- POST /api/proctoring/session/events batch (see ProctoringSessions for the parent session row).
-- Idempotent on (ProctoringSessionId, Sequence): the desktop client's local durable queue can
-- redeliver a batch after a crash/restart, so usp_InsertProctoringLog checks existence first.
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = N'ProctoringLogs')
BEGIN
CREATE TABLE [dbo].[ProctoringLogs](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[ProctoringSessionId] [int] NOT NULL,
	-- Monotonic, gap-free per session; the natural dedupe key together with ProctoringSessionId.
	[Sequence] [int] NOT NULL,
	-- Machine slug, e.g. blocked-process, remote-session, watched-title, multiple-displays
	[Type] [varchar](64) NOT NULL,
	-- Human-readable label, e.g. "Remote access / screen sharing"
	[Category] [nvarchar](200) NULL,
	-- info | low | medium | high
	[Level] [varchar](16) NOT NULL,
	-- low | medium | high
	[Confidence] [varchar](16) NOT NULL,
	[OpenedAtUtc] [datetime2](3) NOT NULL,
	[ResolvedAtUtc] [datetime2](3) NULL,
	-- Salted digest, sent whenever RawDetail isn't (i.e. almost always).
	[RedactedDetail] [nvarchar](200) NULL,
	-- Only ever populated for the specific rules that send raw evidentiary text.
	[RawDetail] [nvarchar](max) NULL,
	[CreatedAtUtc] [datetime2](3) NOT NULL CONSTRAINT [DF_ProctoringLogs_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
 CONSTRAINT [PK_ProctoringLogs] PRIMARY KEY CLUSTERED
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO

IF NOT EXISTS (
    SELECT * FROM sys.foreign_keys WHERE name = 'FK_ProctoringLogs_ProctoringSessions'
)
BEGIN
    ALTER TABLE [dbo].[ProctoringLogs]
    WITH CHECK ADD CONSTRAINT [FK_ProctoringLogs_ProctoringSessions] FOREIGN KEY([ProctoringSessionId])
    REFERENCES [dbo].[ProctoringSessions] ([id])
    ON DELETE CASCADE;
END
GO

IF NOT EXISTS (
    SELECT * FROM sys.indexes
    WHERE name = 'UQ_ProctoringLogs_SessionSequence'
    AND object_id = OBJECT_ID('ProctoringLogs')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UQ_ProctoringLogs_SessionSequence
    ON ProctoringLogs (ProctoringSessionId, [Sequence]);
END
GO
