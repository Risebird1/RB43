  IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetISCInterviewMeetings'))
BEGIN
    DROP PROCEDURE dbo.usp_GetISCInterviewMeetings
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_GetISCInterviewMeetings]  
  
@id int  
  
AS  
Begin  
declare    
@ISCId nvarchar(100) = (select InterviewerSkillId from [dbo].[InterviewerSkillsChannel] where id=@id)    
select irf.PerMintuteRate,im.id as imid, im.ISCId, im.interviewDate,  
im.interviewSlot, im.interviewDuration, im.candidateEmail, 
im.confirmationStatus, im.zoomLink, im.interviewer,
im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy,  
im.candidateName, ul.MobileNumber as MobileNumber,  ic.*, irf.*,   
ul.Isonline as IsOnline from unapprovedInterviewerMeetings im    
left join [dbo].[InterviewerComments] ic on im.id = ic.InterviewId
left join InterviewerSkillsChannel irf on IRF.InterviewerSkillId = im.ISCId    
left join userlist ul on im.candidateEmail = ul.EmailId 
where im.ISCId = @ISCId

ENd