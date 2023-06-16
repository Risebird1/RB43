
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetClonableIRFTransInterviewer'))
BEGIN
    DROP PROCEDURE dbo.usp_GetClonableIRFTransInterviewer
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


Create PROCEDURE [dbo].[usp_GetClonableIRFTransInterviewer]

@id int

AS
Begin

declare @rate int, @templateCreatedFromIrfId int

set @templateCreatedFromIrfId = (select CreatedFromIRFId from ClonableSkillChannel where id = @id)
if(@templateCreatedFromIrfId is not null and @templateCreatedFromIrfId > 0)
Begin
	set @rate = (select Rate from PlanTypeChargesMaster where CountryType = (select JobCountry from IRF where id = @templateCreatedFromIrfId) 
	and PlanTypeEnumValue = (select Heuristicsscore from IRF where id = @templateCreatedFromIrfId)
    and CompanyType = (select CompanyType from IRF where id = @templateCreatedFromIrfId))

End

select ti.Id, ti.ClonableIRFId, ti.InterviewerId, ti.UserId, @rate as PerSuccessRate, c.PrimarySkill, ul.EmailId, ipr.ResumeLink, ipr.CodedResumeLink, ipr.NickName, 1 as IsSelect from  (SELECT  UserId, PrimarySkill = 
    STUFF((SELECT ', ' + SkillName
           FROM (select ipr.*,sk.[Name] as SkillName from InterviewerProfile ipr left join TransInterviewerSkill tis on tis.InterviewerProfileId = ipr.id left join Skill sk on sk.id = tis.SkillId) b 
           WHERE b.Id = a.Id
           FOR XML PATH('')), 1, 2, '')
	FROM (select ipr.*, sk.[Name] as SkillName from InterviewerProfile ipr left join TransInterviewerSkill tis on tis.InterviewerProfileId = ipr.id left join Skill sk on sk.id = tis.SkillId) a
	GROUP BY Id, UserId) c 
join  TransClonableIRFInterviewer ti on ti.UserId=c.UserId
join InterviewerProfile ipr on ipr.UserId = ti.UserId
join UserList ul on ul.id = ti.UserId
where ClonableIRFId=@id and ul.IsActive = 1

End


GO

