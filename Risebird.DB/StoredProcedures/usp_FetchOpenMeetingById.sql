IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchOpenMeetingById'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchOpenMeetingById
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

Create Procedure usp_FetchOpenMeetingById    
@meetingid varchar(200)    
AS    
BEGIN    
select Distinct im.id, im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail, im.ClientHrContact,        
im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,        
im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, im.candidateName,        
im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.InterviewBiometryScore, im.JoiningBiometryScore,        
ca.Phone as MobileNumber, ca.ResumeLink as resumeLink,         
irf.JDLink from InterviewMeetings im          
inner join IRF irf on im.IRFId=irf.IRFId           
inner join [dbo].[TransInterviewer] tirf on IRF.id =tirf.IRFId          
left join [dbo].[InterviewerProfile] inte on tirf.InterviewerId = inte.id      
left join [dbo].[UserList] ul on inte.UserId = ul.id       
left join UserList ulc on ulc.EmailId = im.candidateEmail        
join CandidatesMaster cm on cm.UserId = ulc.id        
join Candidate ca on ca.CandidateMasterId = cm.id and ca.IRFId=irf.id        
where im.ZoomMeetingId=@meetingid and confirmationStatus = 1 and im.IsNotificationSentToEI=1 and ISNULL(im.IsRescheduleNotificationSent,0)!=1 and ISNULL(im.EIAssigned,0)!=1    
END    
  
  
  
  
  




