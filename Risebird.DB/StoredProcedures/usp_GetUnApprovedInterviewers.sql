/****** Object:  StoredProcedure [dbo].[usp_GetUnApprovedInterviewers]    Script Date: 25-05-2020 10.59.45 AM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetUnApprovedInterviewers'))
BEGIN
    DROP PROCEDURE dbo.usp_GetUnApprovedInterviewers
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_GetUnApprovedInterviewers]  
@ISCId int
AS  
BEGIN  
	declare @PrimarySkill varchar(max)

	set @PrimarySkill = (select Skill.[Name] from InterviewerSkillsChannel isc join Skill on isc.PrimarySkill = Skill.id where isc.id=@ISCId)
	
	select * from (select d.*,tilis.ISCId, ul.FirstName, ul.EmailId from  (select * from (SELECT Id as InterviewerProfileId, UserId,CurrentRole, PreferredDomain, ResumeLink, SkillName = 
    STUFF((SELECT ', ' + SkillName
           FROM (select ipr.*, tis.SkillName from InterviewerProfile ipr join TransInterviewerSkill tis on tis.InterviewerProfileId = ipr.id where ipr.IsApproved=0) b 
           WHERE b.Id = a.Id
           FOR XML PATH('')), 1, 2, '')
	FROM (select ipr.*, tis.SkillName from InterviewerProfile ipr join TransInterviewerSkill tis on tis.InterviewerProfileId = ipr.id where ipr.IsApproved=0) a
	GROUP BY Id, UserId,CurrentRole, PreferredDomain, ResumeLink) c 
	where CHARINDEX(@PrimarySkill,c.SkillName)>0 ) d
	join UserList ul on d.UserId = ul.id
	left join (select * from TransInterviewerListInterviewerSkills where ISCId=@ISCId) tilis 
	on tilis.InterviewerProfileId = d.InterviewerProfileId) e
	where e.ISCId is null

		
End  
GO


