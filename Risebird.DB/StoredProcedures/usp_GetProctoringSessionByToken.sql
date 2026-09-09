IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetProctoringSessionByToken'))
BEGIN
    DROP PROCEDURE dbo.usp_GetProctoringSessionByToken
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetProctoringSessionByToken]
    @SessionToken VARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        [SessionToken], [InterviewId], [CandidateEmail], [BrowserSessionId], [Trust], [State], [ClientState],
        [RiskScore], [CreatedAtUtc], [JoinedAtUtc], [StartedAtUtc], [LastHeartbeatAtUtc], [EndedAtUtc], [ExpiresAtUtc]
    FROM dbo.ProctoringSessions
    WHERE [SessionToken] = @SessionToken
END
GO
