IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetInterviewSelectabilityReport'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewSelectabilityReport
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetInterviewSelectabilityReport]
    @InterviewId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- SelectabilityReport is stored by CandidateId + IRFId (no InterviewId column).
    SELECT
        sr.id,
        im.id              AS InterviewId,
        sr.RequiredLevelId,
        sr.Type,
        sr.Type            AS Name,
        sr.ActualLevelId,
        sr.WeightageRequired,
        sr.WeightageActual,
        sr.Value,
        sr.CandidateId,
        sr.IRFId,
        req.[Name]         AS RequiredLevel,
        act.[Name]         AS ActualLevel
    FROM       dbo.InterviewMeetings im
    INNER JOIN dbo.UserList             ul  ON ul.EmailId = im.candidateEmail
    INNER JOIN dbo.CandidatesMaster     cm  ON cm.UserId = ul.id
    INNER JOIN dbo.IRF                  irf ON irf.IRFId = im.IRFId
    INNER JOIN dbo.SelectabilityReport  sr  ON sr.CandidateId = cm.id AND sr.IRFId = irf.id
    INNER JOIN dbo.[Level]              req ON sr.RequiredLevelId = req.id
    LEFT JOIN  dbo.[Level]              act ON sr.ActualLevelId = act.id
    WHERE im.id = @InterviewId;
END
GO
