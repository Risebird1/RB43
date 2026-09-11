IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetProctoringSessions'))
BEGIN
    DROP PROCEDURE dbo.usp_GetProctoringSessions
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Master list for the dashboard's Proctoring Report page: every ProctoringSessions row,
-- newest first, with a LogCount so the page can tell rows with no ProctoringLogs apart
-- from rows worth drilling into via "Check Report".
CREATE PROCEDURE [dbo].[usp_GetProctoringSessions]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        s.[id], s.[SessionToken], s.[InterviewId], s.[CandidateEmail], s.[BrowserSessionId],
        s.[Trust], s.[State], s.[ClientState], s.[RiskScore], s.[CreatedAtUtc], s.[JoinedAtUtc],
        s.[StartedAtUtc], s.[LastHeartbeatAtUtc], s.[EndedAtUtc], s.[ExpiresAtUtc],
        (SELECT COUNT(*) FROM dbo.ProctoringLogs l WHERE l.[ProctoringSessionId] = s.[id]) AS [LogCount]
    FROM dbo.ProctoringSessions s
    ORDER BY s.[CreatedAtUtc] DESC
END
GO
