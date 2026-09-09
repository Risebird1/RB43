-- DesktopProctor API session lifecycle + cumulative risk score.
-- Run against your Risebird database before deploying ProctoringApiController.

CREATE TABLE [dbo].[ProctoringSessions] (
    [SessionToken] VARCHAR(64) NOT NULL PRIMARY KEY,
    [InterviewId] NVARCHAR(100) NOT NULL,
    [CandidateEmail] NVARCHAR(256) NULL,
    [BrowserSessionId] NVARCHAR(200) NULL,
    -- Unverified | Verified (raw C# enum name, not camelCase)
    [Trust] VARCHAR(16) NOT NULL,
    -- Server-tracked lifecycle set by join/start/end: idle | starting | running | stopped
    [State] VARCHAR(16) NOT NULL CONSTRAINT DF_ProctoringSessions_State DEFAULT ('idle'),
    -- Last clientState reported by a heartbeat: idle | starting | running | degraded | faulted | stopped
    [ClientState] VARCHAR(16) NULL,
    [RiskScore] INT NOT NULL CONSTRAINT DF_ProctoringSessions_RiskScore DEFAULT (0),
    [CreatedAtUtc] DATETIME2(3) NOT NULL CONSTRAINT DF_ProctoringSessions_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
    [JoinedAtUtc] DATETIME2(3) NULL,
    [StartedAtUtc] DATETIME2(3) NULL,
    [LastHeartbeatAtUtc] DATETIME2(3) NULL,
    [EndedAtUtc] DATETIME2(3) NULL,
    [ExpiresAtUtc] DATETIME2(3) NOT NULL,
    CONSTRAINT CK_ProctoringSessions_RiskScore CHECK ([RiskScore] BETWEEN 0 AND 100)
);

GO

CREATE NONCLUSTERED INDEX IX_ProctoringSessions_CandidateEmail
    ON [dbo].[ProctoringSessions] ([CandidateEmail]);

GO
