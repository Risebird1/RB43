  IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetRiseTubeMeetingsForJd'))
BEGIN
    DROP PROCEDURE dbo.usp_GetRiseTubeMeetingsForJd
END


SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetRiseTubeMeetingsForJd]      
   @Clientjdid int    
AS      
Begin            
select distinct im.id as imid, im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail, im.ClientHrContact,        
im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,IRF.IsBiometricEnabled,  im.InterviewBiometryScore,        
im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, im.candidateName,        
im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.InterviewBiometryScore, im.JoiningBiometryScore,         
ca.Phone as MobileNumber, ca.EncodedResumeLink as resumeLink,         
IRF.id as irfIntId,IRF.JDLink as JDLink,IRF.JDTiltle,IRF.ClientId, IRF.ClientName,ic.AadharFileLink,ic.VideoLink,ic.InterviewerComment,ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, cm.id as CandidateId,ic.ThumbnailImage as ThumbnailImage,cl.CuratedLink as CuratedLink, QCF.InterviewId as QCInterview,(CASE WHEN ic.CandidateAvailable = 1 THEN 'Available' ELSE 'NotAvailable' END) as CandidateAvailable,    
patchvideoCount.MeetingCount as MeetingPatchCount,  
Techscore.TechSkillScore as TechSkillScore    
  
from InterviewMeetings im            
inner join IRF on im.IRFId = IRF.IRFId          
left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId        
left join UserList ul on ul.EmailId = im.candidateEmail        
join CandidatesMaster cm on ul.id = cm.UserId        
join Candidate ca on ca.CandidateMasterId = cm.id and ca.IRFId = irf.id        
left join QCFeedbackReport QCF on im.id=QCF.InterviewId        
inner join client cl on IRF.ClientID=cl.id        
inner join TechSkillReport tsr  on tsr.InterviewId= im.id  
inner join CandidateJDScore cj on cj.IRFId=im.IRFId and cj.CandidateEmailId=im.candidateEmail
outer apply (select count(*) as MeetingCount from VideoQuestionDetails where Interviewid=ic.InterviewId and NULLIF(VideoLink, '') IS NOT NULL)patchvideoCount  
outer apply (select sum(WeightageActual) as TechSkillScore from TechSkillReport where Interviewid=ic.InterviewId )Techscore  
  
where (ic.InterviewStatus =2 or ic.InterviewStatus=4) and ic.VideoLink is not null  and cj.ClientJdid=@Clientjdid
group by im.id,im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail, im.ClientHrContact,        
im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,IRF.IsBiometricEnabled,  im.InterviewBiometryScore,        
im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, im.candidateName,        
im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.InterviewBiometryScore, im.JoiningBiometryScore,         
ca.Phone, ca.EncodedResumeLink,IRF.id,IRF.JDLink,IRF.JDTiltle,IRF.ClientId, IRF.ClientName,ic.AadharFileLink,ic.VideoLink,ic.InterviewerComment,ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, cm.id,ic.ThumbnailImage ,cl.CuratedLink , QCF.InterviewId ,ic.CandidateAvailable,patchvideoCount.MeetingCount,Techscore.TechSkillScore   
  
           
End   