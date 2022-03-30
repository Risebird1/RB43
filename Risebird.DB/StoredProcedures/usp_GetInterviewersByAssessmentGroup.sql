
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetInterviewersByAssessmentGroup'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewersByAssessmentGroup
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


Create PROCEDURE [dbo].[usp_GetInterviewersByAssessmentGroup]

@assessmentGroupId int


AS
Begin


;With CTE 
AS
(Select Id, UserId, AssessmentGroupId, ResumeLink, CodedResumeLink, [value] from InterviewerProfile cross Apply string_split(AssessmentGroupId,',')) 
SELECT distinct  UserId, PrimarySkill = 
    STUFF((SELECT ', ' + SkillName
           FROM (select CTE.*,sk.[Name] as SkillName from CTE join TransInterviewerSkill tis on tis.InterviewerProfileId = CTE.id left join Skill sk on sk.id = tis.SkillId) b 
           WHERE b.Id = a.Id
           FOR XML PATH('')), 1, 2, ''), EmailId, CodedResumeLink, ResumeLink
	FROM (select CTE.*, sk.[Name] as SkillName, ul.EmailId from CTE join TransInterviewerSkill tis on tis.InterviewerProfileId = CTE.id left join Skill sk on sk.id = tis.SkillId join UserList ul on ul.id = CTE.UserId where CTE.value = @assessmentGroupId) a

End


GO


