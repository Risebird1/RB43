/****** Object:  StoredProcedure [dbo].[usp_GetLeadInterviewersSkills]    Script Date: 25-05-2020 11.00.04 AM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetLeadInterviewersSkills'))
BEGIN
    DROP PROCEDURE dbo.usp_GetLeadInterviewersSkills
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_GetLeadInterviewersSkills]  
AS  
BEGIN  

	select c.*, ul.EmailId from (SELECT  UserId, PrimarySkill = 
    STUFF((SELECT ', ' + SkillName
           FROM (select ipr.*, tis.SkillName from InterviewerProfile ipr join TransInterviewerSkill tis on tis.InterviewerProfileId = ipr.id where ipr.IsLeadInterviewer=1) b 
           WHERE b.Id = a.Id
           FOR XML PATH('')), 1, 2, '')
	FROM (select ipr.*, tis.SkillName from InterviewerProfile ipr join TransInterviewerSkill tis on tis.InterviewerProfileId = ipr.id where ipr.IsLeadInterviewer=1) a
	GROUP BY Id, UserId) c join UserList ul on ul.id = c.UserId
		
End  
GO


