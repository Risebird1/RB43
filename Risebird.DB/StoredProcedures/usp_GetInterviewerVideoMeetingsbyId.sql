IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetInterviewerVideoMeetingsbyId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewerVideoMeetingsbyId
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
    
      
Create PROCEDURE [dbo].[usp_GetInterviewerVideoMeetingsbyId]    
@interviewId int,                    
@Email varchar(max)                    
AS                          
Begin                              
                        
Declare @id int                   
                  
Select @id= Id from UserList where EmailId=@Email                     
                  
select im.id as imid, im.ISCId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail,                    
im.confirmationStatus, im.zoomLink, im.ZoomId, im.interviewer,                 
im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, ca.NickName as candidateName,                                     
ulc.MobileNumber, ca.ResumeLink as resumeLink, ic.AadharFileLink, ca.CodedResumeLink as encodedresumeLink ,IRF.id as irfIntId,IRF.JDTiltle,ic.VideoLink,ic.InterviewerComment,ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus,ic.  
FeedbackAudioLink, cm.id as CandidateId,(Select Isonline from userlist where EmailId=im.candidateEmail) as IsOnline   
          
from UnapprovedInterviewerMeetings im                                    
inner join InterviewerSkillsChannel IRF on im.ISCId = IRF.InterviewerSkillId                                  
left join [dbo].[InterviewerComments] ic on im.id = ic.InterviewId                         
left join UserList ulc on im.candidateEmail=ulc.EmailId                    
join TransInterviewerListInterviewerSkills cm on ulc.id = cm.UserId                    
join InterviewerProfile ca on ca.id = cm.InterviewerProfileId and cm.ISCId = irf.id           
  
where im.id = @interviewId                                         
                             
End             
              
        
    
             
            
      
  
  
    