

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetInterviewerEntitleDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewerEntitleDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetInterviewerEntitleDetails]
@id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT ia.id, ia.Overview, ia.Senioritylevel, ia.Difficultylevel, ia.TestedSkill, ag.AssessmentGroup
    FROM InterviewerAssessments ia
    JOIN AssessmentGroup ag ON ia.Assessmentgroupid = ag.id
    WHERE ia.Assessmentgroupid = @id AND ia.isApproved = 1
END
GO
