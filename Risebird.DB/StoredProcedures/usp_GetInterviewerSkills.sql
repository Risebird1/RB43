/****** Object:  StoredProcedure [dbo].[usp_GetInterviewerSkills]    Script Date: 25-05-2020 11.03.21 AM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetInterviewerSkills'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewerSkills
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

create PROCEDURE [dbo].[usp_GetInterviewerSkills] 
@UserId int
AS  
BEGIN  

	select c.*, ul.EmailId from (SELECT  UserId, PrimarySkill = 
    STUFF((SELECT ', ' + SkillName
           FROM (select ipr.*, tis.SkillName from InterviewerProfile ipr join TransInterviewerSkill tis on tis.InterviewerProfileId = ipr.id ) b 
           WHERE b.Id = a.Id
           FOR XML PATH('')), 1, 2, '')
	FROM (select ipr.*, tis.SkillName from InterviewerProfile ipr join TransInterviewerSkill tis on tis.InterviewerProfileId = ipr.id ) a
	GROUP BY Id, UserId) c join UserList ul on ul.id = c.UserId
	where UserId=@UserId	
End  
GO


