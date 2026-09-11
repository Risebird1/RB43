IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetProctoringLogsBySessionId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetProctoringLogsBySessionId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Logs for one specific session (the dashboard's "Check Report" drill-down). Distinct from
-- usp_GetProctoringLogsForCandidate, which spans all of a candidate's sessions by email.
CREATE PROCEDURE [dbo].[usp_GetProctoringLogsBySessionId]
    @ProctoringSessionId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        [id], [ProctoringSessionId], [Sequence], [Type], [Category], [Level], [Confidence],
        [OpenedAtUtc], [ResolvedAtUtc], [RedactedDetail], [RawDetail], [CreatedAtUtc]
    FROM dbo.ProctoringLogs
    WHERE [ProctoringSessionId] = @ProctoringSessionId
    ORDER BY [Sequence] ASC
END
GO
