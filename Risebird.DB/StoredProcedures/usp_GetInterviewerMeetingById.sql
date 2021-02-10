  
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
              
SELECT [InterviewMeetings].id as imid              
     ,[InterviewMeetings].IRFId ,      
     InterviewerComments.VideoLink      
  ,interviewDate              
  ,interviewSlot              
  ,interviewDuration              
  ,interviewDuration              
  ,candidateEmail              
  ,ClientHrContact              
  ,ca.ResumeLink as resumeLink    
  ,ca.CodedResumeLink as encodedresumeLink              
  ,confirmationStatus              
  ,zoomLink              
  ,[InterviewMeetings].risebirdHR              
  ,interviewer              
  ,PreferredLocation              
  ,ExpectedPerHourRate              
  ,staffingAgency              
  ,ca.NickName as candidateName              
  ,joiningTime              
  ,currentEmployerName              
  ,ul.MobileNumber              
  ,JoiningBiometryScore              
  ,InterviewBiometryScore              
  ,InterviewerComments.IsApproved              
  ,InterviewerComments.IsSelected              
  ,IRF.id as irfIntId              
FROM [dbo].[InterviewMeetings]              
inner join InterviewerSkillsChannel as IRF on [InterviewMeetings].IRFId = IRF.InterviewerSkillId              
left join InterviewerComments on InterviewMeetings.id=InterviewerComments.InterviewId           
join Userlist ul on ul.EmailId=InterviewMeetings.candidateEmail          
left join InterviewerProfile ca on ca.UserId =ul.id            
WHERE              
      [InterviewMeetings].id = @interviewId              
End 
go