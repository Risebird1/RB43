-- DesktopProctor API session lifecycle + cumulative risk score, one row per launched session.
-- SessionToken is the business key the desktop client and dashboard address the session by;
-- see ProctoringLogs for the child event log keyed off this table's surrogate id.

-- Pre-release schema rework: an earlier version of this script made SessionToken itself the
-- primary key, with no surrogate id. No real session data depends on that shape yet, so drop
-- and recreate clean rather than migrate it (see ProctoringLogs.sql for the same guard).
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'ProctoringSessions')
   AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.ProctoringSessions') AND name = N'id')
BEGIN
    DROP TABLE [dbo].[ProctoringSessions]
END
GO

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = N'ProctoringSessions')
BEGIN
CREATE TABLE [dbo].[ProctoringSessions](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[SessionToken] [varchar](64) NOT NULL,
	[InterviewId] [nvarchar](100) NOT NULL,
	[CandidateEmail] [nvarchar](256) NULL,
	[BrowserSessionId] [nvarchar](200) NULL,
	[Trust] [varchar](16) NOT NULL,
	[State] [varchar](16) NOT NULL CONSTRAINT [DF_ProctoringSessions_State] DEFAULT ('idle'),
	[ClientState] [varchar](16) NULL,
	[RiskScore] [int] NOT NULL CONSTRAINT [DF_ProctoringSessions_RiskScore] DEFAULT (0),
	[CreatedAtUtc] [datetime2](3) NOT NULL CONSTRAINT [DF_ProctoringSessions_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
	[JoinedAtUtc] [datetime2](3) NULL,
	[StartedAtUtc] [datetime2](3) NULL,
	[LastHeartbeatAtUtc] [datetime2](3) NULL,
	[EndedAtUtc] [datetime2](3) NULL,
	[ExpiresAtUtc] [datetime2](3) NOT NULL,
 CONSTRAINT [PK_ProctoringSessions] PRIMARY KEY CLUSTERED
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO

-- Every client call (join/start/heartbeat/events/end) looks the session up by SessionToken.
IF NOT EXISTS (
    SELECT * FROM sys.indexes
    WHERE name = 'UQ_ProctoringSessions_SessionToken'
    AND object_id = OBJECT_ID('ProctoringSessions')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UQ_ProctoringSessions_SessionToken
    ON ProctoringSessions (SessionToken);
END
GO

IF NOT EXISTS (
    SELECT * FROM sys.indexes
    WHERE name = 'IX_ProctoringSessions_CandidateEmail'
    AND object_id = OBJECT_ID('ProctoringSessions')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ProctoringSessions_CandidateEmail
    ON ProctoringSessions (CandidateEmail);
END
GO
