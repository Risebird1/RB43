IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetRiskCategories'))
BEGIN
    DROP PROCEDURE dbo.usp_GetRiskCategories
END
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- =====================================================================
-- Procedure : usp_GetRiskCategories
-- Purpose   : Returns all RiskCategory rows joined with InterviewMeetings
--             filtered by interviewDate between @StartDate and @EndDate.
-- Usage     : EXEC usp_GetRiskCategories @StartDate = '2026-06-01', @EndDate = '2026-07-01'
-- =====================================================================
CREATE PROCEDURE [dbo].[usp_GetRiskCategories]
    @StartDate DATETIME,
    @EndDate   DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        rc.Id,
        rc.InterviewId,
        rc.Name,
        rc.Description,
        rc.Severity,
        rc.SeverityBreakdown,
        rc.CreatedOn,
        rc.ModifiedOn,
        im.candidateName,
        im.candidateEmail,
        im.Interviewer,
        im.interviewDate,
        im.IRFId,
        IRF.PrimarySkillInvite AS PrimarySkill
    FROM RiskCategory rc
    INNER JOIN InterviewMeetings im ON im.id   = rc.InterviewId
    LEFT  JOIN IRF               ON IRF.IRFId  = im.IRFId
    WHERE im.interviewDate >= @StartDate
      AND im.interviewDate <  @EndDate
    ORDER BY im.interviewDate DESC

END
GO
