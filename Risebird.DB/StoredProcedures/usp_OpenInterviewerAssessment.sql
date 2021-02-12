/****** Object:  StoredProcedure [dbo].[usp_OpenInterviewerAssessment]    Script Date: 27-05-2020 8.52.10 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_OpenInterviewerAssessment'))
BEGIN
    DROP PROCEDURE dbo.usp_OpenInterviewerAssessment
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

    
CREATE PROCEDURE [dbo].[usp_OpenInterviewerAssessment]     
   @emailID varchar(200)         
AS    
BEGIN    
  select im.*, Skill.[Name] as PrimarySkill from TransInterviewerSkillsLeadInterviewer tisli    
  join UserList ul on tisli.UserId = ul.id    
  join InterviewerSkillsChannel isc on isc.id = tisli.InterviewerSkillId    
  inner join UnapprovedInterviewerMeetings im on  im.ISCId=isc.InterviewerSkillId    
  join Skill on isc.PrimarySkill=Skill.id    
  where ul.EmailId= @emailID and im.interviewer is null and im.confirmationStatus=1   
END 
GO


