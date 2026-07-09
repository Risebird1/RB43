IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetRiskCategoryByInterviewId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetRiskCategoryByInterviewId
END
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- =====================================================================
-- Procedure : usp_GetRiskCategoryByInterviewId
-- Purpose   : Returns the RiskCategory row for the given InterviewId,
--             joined with basic interview info from InterviewMeetings.
-- Usage     : EXEC usp_GetRiskCategoryByInterviewId @InterviewId = 1234
-- =====================================================================
CREATE PROCEDURE [dbo].[usp_GetRiskCategoryByInterviewId]
    @InterviewId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        rc.Id,
        rc.InterviewId,
        rc.Name,
        rc.Description,
        rc.CreatedOn,
        rc.ModifiedOn,
        im.candidateName,
        im.candidateEmail,
        im.Interviewer,
        im.interviewDate,
        im.PrimarySkill
    FROM RiskCategory rc
    LEFT JOIN InterviewMeetings im ON im.id = rc.InterviewId
    WHERE rc.InterviewId = @InterviewId

END
GO
