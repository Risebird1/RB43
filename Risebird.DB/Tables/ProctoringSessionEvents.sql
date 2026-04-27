-- Chrome extension proctoring API persistence (telemetry + session lifecycle).
-- Run against your Risebird database before deploying the updated API.

CREATE TABLE [dbo].[ProctoringSessionEvents] (
    [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [CreatedAtUtc] DATETIME2(7) NOT NULL CONSTRAINT DF_ProctoringSessionEvents_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
    [CreatedAtIst] DATETIME2(7) NULL,
    [InterviewId] INT NOT NULL,
    [CandidateEmail] NVARCHAR(256) NULL,
    -- join | start | stop | telemetry | screenshot | heartbeat
    [EventKind] NVARCHAR(64) NOT NULL,
    -- For telemetry rows: info | violation | null for lifecycle rows
    [Level] NVARCHAR(32) NULL,
    [Reason] NVARCHAR(MAX) NULL,
    [MetadataJson] NVARCHAR(MAX) NULL,
    -- For EventKind = screenshot: payload size only (image is not stored)
    [ScreenshotPayloadLength] INT NULL
);

GO

CREATE NONCLUSTERED INDEX IX_ProctoringSessionEvents_InterviewId_CreatedAtUtc
    ON [dbo].[ProctoringSessionEvents] ([InterviewId], [CreatedAtUtc] DESC);

GO
