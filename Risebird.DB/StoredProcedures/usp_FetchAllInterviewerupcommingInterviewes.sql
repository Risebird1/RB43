  IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchAllInterviewerupcommingInterviewes'))
BEGIN
    DROP PROCEDURE [dbo].[usp_FetchAllInterviewerupcommingInterviewes]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
    
Create Procedure usp_FetchAllInterviewerupcommingInterviewes
@timeNow datetime 
AS  
BEGIN  
      
;With CTE as (            
select im.id, im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail,      
ISNULL(ca.ClientHRAttemptContact, im.ClientHrContact) as ClientHrContact,        
im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,        
im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, im.candidateName,        
im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.InterviewBiometryScore, im.JoiningBiometryScore,        
ca.Phone as MobileNumber, ca.ResumeLink as resumeLink,irf.JDTiltle,irf.id as irfIntId,       
irf.JDLink,ul.EmailId from InterviewMeetings im          
inner join IRF irf on im.IRFId=irf.IRFId           
inner join [dbo].[TransInterviewer] tirf on IRF.id =tirf.IRFId          
left join [dbo].[Interviewer] inte on tirf.InterviewerId = inte.id      
left join [dbo].[UserList] ul on inte.UserId = ul.id       
left join UserList ulc on ulc.EmailId = im.candidateEmail        
join CandidatesMaster cm on cm.UserId = ulc.id        
join Candidate ca on ca.CandidateMasterId = cm.id and ca.IRFId=irf.id        
where interviewer is null and confirmationStatus = 1 and IsEmergency=0 and im.interviewDate>=@timeNow and tirf.IsActive=1      
union       
select im.id, im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail,       
ISNULL(ca.ClientHRAttemptContact, im.ClientHrContact) as ClientHrContact,          
im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,        
im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, im.candidateName,        
im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.InterviewBiometryScore, im.JoiningBiometryScore,        
ca.Phone as MobileNumber, ca.ResumeLink as resumeLink,irf.JDTiltle,irf.id as irfIntId,        
irf.JDLink,ul.EmailId from InterviewMeetings im          
inner join IRF irf on im.IRFId=irf.IRFId           
inner join [dbo].[TransInterviewer] tirf on IRF.id =tirf.IRFId          
left join [dbo].[InterviewerProfile] inte on tirf.InterviewerId = inte.id      
left join [dbo].[UserList] ul on inte.UserId = ul.id       
left join UserList ulc on ulc.EmailId = im.candidateEmail        
join CandidatesMaster cm on cm.UserId = ulc.id        
join Candidate ca on ca.CandidateMasterId = cm.id and ca.IRFId=irf.id        
where interviewer is null and confirmationStatus = 1 and IsEmergency=0 and im.interviewDate>=@timeNow and tirf.IsActive=1)   
Select CTE.EmailId,COUNT(CTE.id) as Total,ip.uniqueid from CTE left join InterviewerPublicURLs ip on ip.Emailid=CTE.EmailId group by CTE.EmailId,ip.uniqueid  
END     
      
      
  
  GO  
    

  