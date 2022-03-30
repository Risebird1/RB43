
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetQuestionsForAssessmentGroup'))
BEGIN
    DROP PROCEDURE dbo.usp_GetQuestionsForAssessmentGroup
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


Create PROCEDURE [dbo].[usp_GetQuestionsForAssessmentGroup]

@assessmentGroupId int


AS
Begin

Select IRF.IRFId, sk.id as SkillId, sk.Name as SkillName, ts.Comment as Question From TechSkillComments ts 
join IRF on IRF.id=ts.IRFId join Skill sk on sk.id=ts.Skill 
where AssessmentGroupId = @assessmentGroupId order by IRF.IRFId
End


GO


