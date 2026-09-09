IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetProctoringLogsForCandidate'))
BEGIN
    DROP PROCEDURE dbo.usp_GetProctoringLogsForCandidate
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetProctoringLogsForCandidate]
    @CandidateEmail NVARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        l.[SessionToken], l.[Sequence], l.[Type], l.[Category], l.[Level], l.[Confidence],
        l.[OpenedAtUtc], l.[ResolvedAtUtc], l.[RedactedDetail], l.[RawDetail], l.[CreatedAtUtc]
    FROM dbo.ProctoringLogs l
    INNER JOIN dbo.ProctoringSessions s ON s.[SessionToken] = l.[SessionToken]
    WHERE s.[CandidateEmail] = @CandidateEmail
    ORDER BY l.[OpenedAtUtc] ASC, l.[Sequence] ASC
END
GO
