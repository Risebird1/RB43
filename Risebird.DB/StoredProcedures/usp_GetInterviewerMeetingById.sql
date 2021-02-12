  
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetInterviewerMeetingById'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewerMeetingById
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

   

	   
        
      
CREATE PROCEDURE [dbo].[usp_GetInterviewerMeetingById]              
              
@interviewId int              
              
AS              
Begin              
              
SELECT im.id as imid              
  ,im.ISCId ,      
   InterviewerComments.VideoLink      
  ,interviewDate              
  ,interviewSlot              
  ,interviewDuration              
  ,interviewDuration              
  ,candidateEmail              
  ,ca.ResumeLink as resumeLink    
  ,ca.CodedResumeLink as encodedresumeLink              
  ,confirmationStatus              
  ,zoomLink              
  ,im.ZoomId        
  ,interviewer              
  ,ca.NickName as candidateName              
  ,ul.MobileNumber              
  ,InterviewerComments.IsApproved              
  ,InterviewerComments.IsSelected              
  ,IRF.id as irfIntId              
FROM [dbo].[UnapprovedInterviewerMeetings] im              
inner join InterviewerSkillsChannel as IRF on im .ISCId = IRF.InterviewerSkillId              
left join InterviewerComments on im.id=InterviewerComments.InterviewId           
join Userlist ul on ul.EmailId=im.candidateEmail          
left join InterviewerProfile ca on ca.UserId =ul.id            
WHERE              
      im.id = @interviewId              
End 
go