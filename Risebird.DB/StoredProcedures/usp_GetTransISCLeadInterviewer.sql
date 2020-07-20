/****** Object:  StoredProcedure [dbo].[usp_GetTransISCLeadInterviewer]    Script Date: 25-05-2020 11.03.38 AM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetTransISCLeadInterviewer'))
BEGIN
    DROP PROCEDURE dbo.usp_GetTransISCLeadInterviewer
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

create PROCEDURE [dbo].[usp_GetTransISCLeadInterviewer] 
@ISCId int
AS  
BEGIN  

	
select c.* from (SELECT  UserId, PrimarySkill = 
    STUFF((SELECT ', ' + SkillName
           FROM (select ipr.*, tis.SkillName from InterviewerProfile ipr join TransInterviewerSkill tis on tis.InterviewerProfileId = ipr.id ) b 
           WHERE b.Id = a.Id
           FOR XML PATH('')), 1, 2, '')
	FROM (select ipr.*, tis.SkillName from InterviewerProfile ipr join TransInterviewerSkill tis on tis.InterviewerProfileId = ipr.id ) a
	GROUP BY Id, UserId) c 
	join TransInterviewerSkillsLeadInterviewer tis on c.UserId = tis.UserId
	where tis.InterviewerSkillId = @ISCId
End  
GO


