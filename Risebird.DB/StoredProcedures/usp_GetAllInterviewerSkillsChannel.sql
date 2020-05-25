/****** Object:  StoredProcedure [dbo].[usp_GetAllInterviewerSkillsChannel]    Script Date: 25-05-2020 10.53.35 AM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAllInterviewerSkillsChannel'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAllInterviewerSkillsChannel
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_GetAllInterviewerSkillsChannel]  
@EmailId varchar(200),   
@Profile varchar(100)

AS  
Begin 

IF (@Profile = 'HR')  
	BEGIN
	
		select isc.*, Skill.[Name] as SkillName, ul.EmailId as HREmailId from InterviewerSkillsChannel isc left join UserList ul on ul.id = isc.RisebirdHr 
		left join Skill on Skill.id = isc.PrimarySkill
		where ul.EmailId = @EmailId
		
	End  

Else
	begin

		select isc.*, Skill.[Name] as SkillName, ul.EmailId as HREmailId from InterviewerSkillsChannel isc left join UserList ul on ul.id = isc.RisebirdHr
		left join Skill on Skill.id = isc.PrimarySkill

	End
End

GO


