


IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetInterviewerEntitleById'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewerEntitleById
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetInterviewerEntitleById]
@assessmentid INT
AS
BEGIN
    SET NOCOUNT ON;
SELECT ag.id, AssessmentGroup, PrimarySkills, ag.CompanyType, ag.IsRelevant, ag.ProfileType, ag.CompetencyLevel, ag.SeniorityLevel, ag.Country, ag.PerHourRate,
        (SELECT COUNT(*) FROM InterviewerProfile CROSS APPLY STRING_SPLIT(AssessmentGroupId, ',') WHERE value = ag.id) AS Assessmentcount,
        ag.Isinstaavailable, agc.CategoryName, COUNT(IRF.id) AS IrfCount, ag.EmploymentType, ag.AssessmentVerificationType, ag.CodeLanguage, ag.MCQSetsValue, ag.IsMasterAG, ag.USTJobTitle
    FROM AssessmentGroup ag
    LEFT JOIN AssessmentGroupCategory agc ON agc.id = ag.Categoryid
    LEFT JOIN IRF ON IRF.AssessmentGroupId = ag.id
    WHERE ag.id = @assessmentid
    GROUP BY ag.id, AssessmentGroup, PrimarySkills, ag.Isinstaavailable, agc.CategoryName, ag.CompanyType, ag.IsRelevant, ag.ProfileType, ag.CompetencyLevel, ag.Country, ag.PerHourRate, ag.SeniorityLevel, ag.EmploymentType, ag.AssessmentVerificationType, ag.CodeLanguage, ag.MCQSetsValue, ag.IsMasterAG, ag.USTJobTitle

END
GO
