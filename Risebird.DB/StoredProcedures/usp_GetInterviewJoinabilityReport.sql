IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetInterviewJoinabilityReport'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewJoinabilityReport
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetInterviewJoinabilityReport]
    @InterviewId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- JoinabilityReport is stored by CandidateId + IRFId (no InterviewId column).
    -- Resolve both from the interview meeting, same as evaluator fitment reports.
    SELECT
        jr.id,
        im.id              AS InterviewId,
        jr.RequiredLevelId,
        jr.Type,
        jr.Type            AS Name,
        jr.ActualLevelId,
        jr.WeightageRequired,
        jr.WeightageActual,
        jr.Value,
        jr.CandidateId,
        jr.IRFId,
        req.[Name]         AS RequiredLevel,
        act.[Name]         AS ActualLevel
    FROM       dbo.InterviewMeetings im
    INNER JOIN dbo.UserList           ul  ON ul.EmailId = im.candidateEmail
    INNER JOIN dbo.CandidatesMaster   cm  ON cm.UserId = ul.id
    INNER JOIN dbo.IRF              irf ON irf.IRFId = im.IRFId
    INNER JOIN dbo.JoinabilityReport  jr  ON jr.CandidateId = cm.id AND jr.IRFId = irf.id
    INNER JOIN dbo.[Level]            req ON jr.RequiredLevelId = req.id
    LEFT JOIN  dbo.[Level]            act ON jr.ActualLevelId = act.id
    WHERE im.id = @InterviewId;
END
GO
