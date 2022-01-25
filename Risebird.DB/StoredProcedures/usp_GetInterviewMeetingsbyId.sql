IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetInterviewMeetingsbyId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewMeetingsbyId
END


SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

  
    
Create PROCEDURE [dbo].[usp_GetInterviewMeetingsbyId]                  
   @interviewId int,            
   @Email varchar(max)            
AS                  
Begin                      
                
Declare @id int           
          
Select @id= Id from UserList where EmailId=@Email             
          
select im.id as imid, im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, ulc.EmailId as candidateEmail, im.ClientHrContact,            
im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,            
im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, 
case 
		when ulc.LastName is null then ulc.FirstName
		else RTRIM(LTRIM(ulc.FirstName)) + SPACE(1) + RTRIM(LTRIM(ulc.LastName))
end as candidateName,            
im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.InterviewBiometryScore, im.JoiningBiometryScore,            
ulc.MobileNumber, ca.ResumeLink as resumeLink, ic.AadharFileLink, ca.EncodedResumeLink as encodedresumeLink ,IRF.id as irfIntId,IRF.JDLink as JDLink,IRF.JDTiltle,IRF.ClientName,IRF.IsBiometricEnabled,im.InterviewBiometryScore, ic.VideoLink,ic.InterviewerComment,ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus,ic.FeedbackAudioLink, cm.id as CandidateId,                      
(Select distinct isLike from RisetubeVideoLike where userId=@Id and InterviewId=@interviewId) as IsLike,ic.TotalView as TotalView,ic.IsShared as IsShared,(Select count(*) from RisetubeVideoLike where IsLike='Liked' and InterviewId=@interviewId) as TotalVedioLike,(Select count(*) from RisetubeVideoLike where IsLike='Disliked' and InterviewId=@interviewId) as TotalVedioDisLike,(Select Count(*) from RiseTubeVedioShortList where InterviewId=@interviewId )  as ShortListCount,(Select Count(*) from RiseTubeVedioShortList                  
where InterviewId=@interviewId and userId=@Id )  as IsShortlisted,(Select top 1 status from SuitableCandidate where Userid=@id and InterviewId= @interviewId order by InterstedOn desc) as IsIntrested,              
(Select count(*) from tblsharedvideo where userid=@id and VideoId=@interviewId) as shared,(Select Isonline from userlist where EmailId=im.candidateEmail) as IsOnline ,im.ClientHrContact as ClientHrContact,   
IRF.IsJoinabilitySelectabilityRequired,
(CASE WHEN ic.CandidateAvailable = 1 THEN 'Available' ELSE 'NotAvailable' END) as CandidateAvailable    
from InterviewMeetings im                            
inner join IRF on im.IRFId = IRF.IRFId                          
left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId                 
left join UserList ulc on im.candidateEmail=ulc.EmailId            
join CandidatesMaster cm on ulc.id = cm.UserId            
join Candidate ca on cm.id = ca.CandidateMasterId and irf.id= ca.IRFId            
where im.id = @interviewId                                 
                     
End     
      
  
  
    
Go