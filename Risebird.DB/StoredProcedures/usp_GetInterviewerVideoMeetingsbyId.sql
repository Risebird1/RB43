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
          
select im.id as imid, im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail, im.ClientHrContact,            
im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,            
im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, im.candidateName,            
im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.InterviewBiometryScore, im.JoiningBiometryScore,            
ulc.MobileNumber, ca.ResumeLink as resumeLink, ic.AadharFileLink, ca.CodedResumeLink as encodedresumeLink ,IRF.id as irfIntId,IRF.JDTiltle,im.InterviewBiometryScore, ic.VideoLink,ic.InterviewerComment,ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus,ic.FeedbackAudioLink, cm.id as CandidateId,                      
(Select Count(*) from RiseTubeVedioShortList where InterviewId=@interviewId )  as ShortListCount,(Select Count(*) from RiseTubeVedioShortList
where InterviewId=@interviewId and userId=@Id )  as IsShortlisted,(Select Isonline from userlist where EmailId=im.candidateEmail) as IsOnline ,im.ClientHrContact as ClientHrContact    
from InterviewMeetings im                            
inner join InterviewerSkillsChannel IRF on im.IRFId = IRF.InterviewerSkillId                          
left join [dbo].[InterviewerComments] ic on im.id = ic.InterviewId                 
left join UserList ulc on im.candidateEmail=ulc.EmailId            
join TransInterviewerListInterviewerSkills cm on ulc.id = cm.UserId            
join InterviewerProfile ca on ca.id = cm.InterviewerProfileId and cm.ISCId = irf.id   
where im.id = @interviewId                                 
                     
End     
      
  
  
    