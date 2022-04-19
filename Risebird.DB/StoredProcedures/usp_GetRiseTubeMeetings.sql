/****** Object:  StoredProcedure [dbo].[usp_GetRiseTubeMeetings]    Script Date: 4/12/2020 5:28:28 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetRiseTubeMeetings'))
BEGIN
    DROP PROCEDURE dbo.usp_GetRiseTubeMeetings
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetRiseTubeMeetings]    
     @clientid int,
	  @Emailid varchar(100)  
AS    
Begin            
if @clientid = 0        
 BEGIN          
select distinct im.id as imid, im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, ul.EmailId as candidateEmail, im.ClientHrContact,        
im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,IRF.IsBiometricEnabled,  im.InterviewBiometryScore,        
im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, 
case 
	when ul.LastName is null then ul.FirstName
	else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName))
end as candidateName,        
im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.InterviewBiometryScore, im.JoiningBiometryScore,         
ca.Phone as MobileNumber, ca.ResumeLink as resumeLink,         
IRF.id as irfIntId,IRF.JDLink as JDLink,IRF.JDTiltle,IRF.ClientId, IRF.ClientName,ic.AadharFileLink,ic.VideoLink,ic.InterviewerComment,ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, cm.id as CandidateId,ic.ThumbnailImage as ThumbnailImage,cl.CuratedLink as CuratedLink, QCF.InterviewId as QCInterview,(CASE WHEN ic.CandidateAvailable = 1 THEN 'Available' ELSE 'NotAvailable' END) as CandidateAvailable,    
patchvideoCount.MeetingCount as MeetingPatchCount,  
Techscore.TechSkillScore as TechSkillScore, ic.ThumbnailTimeSeconds     
  
from InterviewMeetings im            
inner join IRF on im.IRFId = IRF.IRFId          
left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId        
left join UserList ul on ul.EmailId = im.candidateEmail        
join CandidatesMaster cm on ul.id = cm.UserId        
join Candidate ca on ca.CandidateMasterId = cm.id and ca.IRFId = irf.id        
left join QCFeedbackReport QCF on im.id=QCF.InterviewId        
inner join client cl on IRF.ClientID=cl.id        
inner join TechSkillReport tsr  on tsr.InterviewId= im.id  
outer apply (select count(*) as MeetingCount from VideoQuestionDetails where Interviewid=ic.InterviewId and NULLIF(VideoLink, '') IS NOT NULL)patchvideoCount  
outer apply (select sum(WeightageActual) as TechSkillScore from TechSkillReport where Interviewid=ic.InterviewId )Techscore  
  
where (ic.InterviewStatus =2 or ic.InterviewStatus=4) and ic.VideoLink is not null  
group by im.id,im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, ul.EmailId, im.ClientHrContact,        
im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,IRF.IsBiometricEnabled,  im.InterviewBiometryScore,        
im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, ul.FirstName, ul.LastName,        
im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.InterviewBiometryScore, im.JoiningBiometryScore,         
ca.Phone, ca.ResumeLink,IRF.id,IRF.JDLink,IRF.JDTiltle,IRF.ClientId, IRF.ClientName,ic.AadharFileLink,ic.VideoLink,ic.InterviewerComment,  
ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, cm.id,ic.ThumbnailImage ,cl.CuratedLink , QCF.InterviewId ,  
ic.CandidateAvailable,patchvideoCount.MeetingCount,Techscore.TechSkillScore, ic.ThumbnailTimeSeconds    
  
END        
else        
 BEGIN          
select distinct im.id as imid, im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, ulc.EmailId as candidateEmail, im.ClientHrContact,        
im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,IRF.IsBiometricEnabled,im.InterviewBiometryScore,        
im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy,
case 
	when ulc.LastName is null then ulc.FirstName
	else RTRIM(LTRIM(ulc.FirstName)) + SPACE(1) + RTRIM(LTRIM(ulc.LastName))
end as candidateName,     
im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.InterviewBiometryScore, im.JoiningBiometryScore,         
ca.Phone as MobileNumber, ca.ResumeLink as resumeLink,         
IRF.id as irfIntId,IRF.JDLink as JDLink,IRF.JDTiltle,IRF.ClientId, IRF.ClientName,ic.AadharFileLink,ic.VideoLink,ic.InterviewerComment,ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, cm.id as CandidateId,ic.ThumbnailImage as ThumbnailImage,QCF.InterviewId as QCInterview,(CASE WHEN ic.CandidateAvailable = 1 THEN 'Available' ELSE 'NotAvailable' END) as CandidateAvailable,      
patchvideoCount.MeetingCount as MeetingPatchCount,  
Techscore.TechSkillScore as TechSkillScore, ic.ThumbnailTimeSeconds     
from InterviewMeetings im          
inner join [dbo].[InterviewComments] ic on im.id = ic.InterviewId        
inner join UserList ulc on ulc.EmailId = im.candidateEmail        
inner join CandidatesMaster cm on ulc.id = cm.UserId        
inner join QCFeedbackReport QCF on im.id=QCF.InterviewId        
inner join IRF on im.IRFId = IRF.IRFId            
inner join [dbo].[TransClientUser] tc on tc.IRFId = IRF.id        
inner join dbo.UserList ul on ul.id = tc.ClientUserId        
inner join Candidate ca on ca.CandidateMasterId = cm.id and ca.IRFId = irf.id     
inner join TechSkillReport tsr  on tsr.InterviewId= im.id  
outer apply (select count(*) as MeetingCount from VideoQuestionDetails where Interviewid=ic.InterviewId and NULLIF(VideoLink, '') IS NOT NULL)patchvideoCount  
outer apply (select sum(WeightageActual) as TechSkillScore from TechSkillReport where Interviewid=ic.InterviewId )Techscore  
where (ic.InterviewStatus =2 or ic.InterviewStatus=4) and ic.VideoLink is not null and ul.EmailId=@Emailid  and (ic.MoveToCP is null or ic.MoveToCP =0)         
group by im.id,im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, ulc.EmailId, im.ClientHrContact,        
im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,IRF.IsBiometricEnabled,  im.InterviewBiometryScore,        
im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, ulc.FirstName, ulc.LastName,        
im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.InterviewBiometryScore, im.JoiningBiometryScore,         
ca.Phone, ca.ResumeLink,IRF.id,IRF.JDLink,IRF.JDTiltle,IRF.ClientId, IRF.ClientName,ic.AadharFileLink,  
ic.VideoLink,ic.InterviewerComment,ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus,  
cm.id,ic.ThumbnailImage  , QCF.InterviewId ,ic.CandidateAvailable,patchvideoCount.MeetingCount,Techscore.TechSkillScore, ic.ThumbnailTimeSeconds    
  
END        
           
End   
GO
