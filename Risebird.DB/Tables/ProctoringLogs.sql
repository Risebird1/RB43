-- DesktopProctor API violation/telemetry event log.
-- Each row is one item from a POST /api/proctoring/session/events batch. Idempotent insert on
-- (SessionToken, Sequence) is enforced by the unique constraint below plus usp_InsertProctoringLog's
-- existence check, since the desktop client's local durable queue can redeliver a batch after a
-- crash/restart. Run against your Risebird database before deploying ProctoringApiController.

CREATE TABLE [dbo].[ProctoringLogs] (
    [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [SessionToken] VARCHAR(64) NOT NULL,
    -- Monotonic, gap-free per session; the natural dedupe key together with SessionToken.
    [Sequence] INT NOT NULL,
    -- Machine slug, e.g. blocked-process, remote-session, watched-title, multiple-displays
    [Type] VARCHAR(64) NOT NULL,
    -- Human-readable label, e.g. "Remote access / screen sharing"
    [Category] NVARCHAR(200) NULL,
    -- info | low | medium | high
    [Level] VARCHAR(16) NOT NULL,
    -- low | medium | high
    [Confidence] VARCHAR(16) NOT NULL,
    [OpenedAtUtc] DATETIME2(3) NOT NULL,
    [ResolvedAtUtc] DATETIME2(3) NULL,
    -- Salted digest, sent whenever RawDetail isn't (i.e. almost always).
    [RedactedDetail] NVARCHAR(200) NULL,
    -- Only ever populated for the specific rules that send raw evidentiary text.
    [RawDetail] NVARCHAR(MAX) NULL,
    [CreatedAtUtc] DATETIME2(3) NOT NULL CONSTRAINT DF_ProctoringLogs_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT FK_ProctoringLogs_ProctoringSessions FOREIGN KEY ([SessionToken])
        REFERENCES [dbo].[ProctoringSessions] ([SessionToken]),
    CONSTRAINT UQ_ProctoringLogs_SessionToken_Sequence UNIQUE ([SessionToken], [Sequence])
);

GO

CREATE NONCLUSTERED INDEX IX_ProctoringLogs_SessionToken_OpenedAtUtc
    ON [dbo].[ProctoringLogs] ([SessionToken], [OpenedAtUtc]);

GO
