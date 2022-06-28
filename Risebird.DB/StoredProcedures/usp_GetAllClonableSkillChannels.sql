
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAllClonableSkillChannels'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAllClonableSkillChannels
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetAllClonableSkillChannels]  

AS  
Begin

	select csc.id as ClonableSkillChannelId, csc.Name, csc.CreatedFromIRFId, csc.MeetingPlatform, ir.CompanyType as CompanyTypeId,-- ir.CompetencyLevelId,
	ir.SeniorityLevelId, ir.CountryType, asg.AssessmentGroup as AssessmentGroupName, asg.id as AssessmentGroupId,
	ir.DifficultyLevel, ir.EmploymentType, ir.PerHourRate, agc.CategoryName
	from ClonableSkillChannel csc 
	join IRFRecommendations ir on ir.CloneIRFId = csc.id
	Join AssessmentGroup asg on asg.id = ir.AssessmentGroupId
	left join AssessmentGroupCategory agc on agc.id = asg.Categoryid
	where ir.[Status] = 1

End
GO


