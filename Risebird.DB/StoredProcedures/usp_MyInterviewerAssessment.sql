/****** Object:  StoredProcedure [dbo].[usp_MyInterviewerAssessment]    Script Date: 27-05-2020 8.51.57 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_MyInterviewerAssessment'))
BEGIN
    DROP PROCEDURE dbo.usp_MyInterviewerAssessment
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_MyInterviewerAssessment]     
@emailID varchar(200)        
AS      
BEGIN      
 select im.*, Skill.[Name] as PrimarySkill, isc.JDTiltle,isc.id as iscIntId,ic.ApprovalComments, ic.IsApproved,  
  ic.InterviewStatus, ic.IsSelected, ic.MeetingStatus, ic.VideoLink from TransInterviewerSkillsLeadInterviewer tisli  
  join UserList ul on tisli.UserId = ul.id  
  join InterviewerSkillsChannel isc on isc.id = tisli.InterviewerSkillId  
  inner join InterviewMeetings im on  im.IRFId=isc.InterviewerSkillId  
  left join InterviewComments ic on ic.InterviewId = im.id  
  join Skill on isc.PrimarySkill=Skill.id  
  where ul.EmailId= @emailID and im.interviewer=@emailID and im.confirmationStatus=1 and IsEmergency=0         
      
END   
GO


