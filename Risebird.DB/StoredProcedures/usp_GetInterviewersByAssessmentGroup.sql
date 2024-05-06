
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

if exists (select id from MasterAssessmentGroup where AssessmentGroupId = @assessmentGroupId)
Begin

	--get the list of all the assessment groups now
	DECLARE @tbl_AllAssessmentGroups TABLE (          
		id INT        
	)

	declare @magId int
	set @magId = (select top 1 id from MasterAssessmentGroup where AssessmentGroupId = @assessmentGroupId)

	insert into @tbl_AllAssessmentGroups
	select distinct AssessmentGroupId from SecondaryAssessmentGroups where MAGId =@magId
	union
	select top 1 AssessmentGroupId from MasterAssessmentGroup where id = @magId

	;With CTE AS
	(
		Select distinct ipr.Id, UserId, AssessmentGroupId, ResumeLink, CodedResumeLink, [value] from InterviewerProfile ipr cross Apply string_split(AssessmentGroupId,',') 
		join UserList ul on ul.id = ipr.UserId
		where value in (select distinct id from @tbl_AllAssessmentGroups) and ul.IsActive = 1
	)
	SELECT distinct  UserId, PrimarySkill = 
			STUFF((SELECT ', ' + SkillName
				   FROM (select CTE.*,sk.[Name] as SkillName from CTE join TransInterviewerSkill tis on tis.InterviewerProfileId = CTE.id left join Skill sk on sk.id = tis.SkillId) b 
				   WHERE b.Id = a.Id
				   FOR XML PATH('')), 1, 2, ''), EmailId, CodedResumeLink, ResumeLink
			FROM (select CTE.*, sk.[Name] as SkillName, ul.EmailId from CTE join TransInterviewerSkill tis on tis.InterviewerProfileId = CTE.id left join Skill sk on sk.id = tis.SkillId join UserList ul on ul.id = CTE.UserId) a
	


End
Else
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



End


GO


