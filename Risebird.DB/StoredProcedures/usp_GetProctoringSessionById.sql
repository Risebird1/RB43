IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetProctoringSessionById'))
BEGIN
    DROP PROCEDURE dbo.usp_GetProctoringSessionById
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Single-session header for the dashboard's full-page Proctoring Session Report
-- (usp_GetProctoringSessions is the master list; this is its by-id counterpart).
CREATE PROCEDURE [dbo].[usp_GetProctoringSessionById]
    @id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        s.[id], s.[SessionToken], s.[InterviewId], s.[CandidateEmail], s.[BrowserSessionId],
        s.[Trust], s.[State], s.[ClientState], s.[RiskScore], s.[CreatedAtUtc], s.[JoinedAtUtc],
        s.[StartedAtUtc], s.[LastHeartbeatAtUtc], s.[EndedAtUtc], s.[ExpiresAtUtc],
        (SELECT COUNT(*) FROM dbo.ProctoringLogs l WHERE l.[ProctoringSessionId] = s.[id]) AS [LogCount]
    FROM dbo.ProctoringSessions s
    WHERE s.[id] = @id
END
GO
