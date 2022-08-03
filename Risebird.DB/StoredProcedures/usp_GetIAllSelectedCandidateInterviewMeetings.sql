IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetIAllSelectedCandidateInterviewMeetings'))
BEGIN
    DROP PROCEDURE dbo.usp_GetIAllSelectedCandidateInterviewMeetings
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
        
          
Create PROCEDURE [dbo].[usp_GetIAllSelectedCandidateInterviewMeetings]              
@startDate datetime,          
@endDate datetime,
@email varchar(150)
AS              
Begin              
Declare @Role varchar(150)
Select @Role=pr.Name From UserList ul join Profile pr on pr.id=ul.idprofile where Emailid=@email
if(@Role='SeniorHR' or @Role='SupportAdmin')
BEGIN
select distinct Meetings.*,  ca.ResumeLink as resumeLink, ca.Phone as MobileNumber from (select distinct im.id imid,im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail,            
im.ClientHrContact, im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, IRF.JobCountry,           
im.PreferredLocation, im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.candidateName, im.currentEmployerName,             
im.QCInterviewer, im.IsEmergency, im.InterviewBiometryScore, im.JoiningBiometryScore,            
IRF.id as irfIntId,IRF.JDLink as JDLink,IRF.JDTiltle,IRF.ClientName,IRF.IsBiometricEnabled,ic.VideoLink,ic.InterviewerComment,      
ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, ic.AadharFileLink, cm.id as CandidateId           
,ulc.Isonline as IsOnline,ulh.Isonline as IsHrOnline,uli.Isonline  as IsInterviewerOnline, im.HRCoordinatorEmailId, im.HRComment, cl.[Name] as Client, qcfr.QCStatus,ic.isLock,ic.ClientUserid as Emailid,ic.MoveToCP, (CASE WHEN ic.CandidateAvailable = 1 THEN 'Available' ELSE 'NotAvailable' END) as CandidateAvailable     
from InterviewMeetings im            
inner join IRF on im.IRFId = IRF.IRFId    
join SeniorHr sh on sh.Clientid=irf.ClientId
left join UserList ulsh on ulsh.id=sh.Userid
left join UserList ulc on im.candidateEmail= ulc.EmailId          
left join UserList ulh on im.ClientHrContact= ulh.EmailId          
left join UserList uli on im.interviewer= uli.EmailId          
left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId            
join CandidatesMaster cm on ulc.id = cm.UserId      
left join QCFeedbackReport qcfr on qcfr.InterviewId=im.id      
left join Client cl on cl.id = irf.ClientId
where ulsh.Emailid= @email) Meetings            
join Candidate ca on Meetings.CandidateId = ca.CandidateMasterId and Meetings.irfIntId= ca.IRFId      
where (Meetings.InterviewStatus=2 or Meetings.InterviewStatus=4) and interviewDate >= @startDate AND interviewDate < @endDate         
END
ELSE
BEGIN
select distinct Meetings.*,  ca.ResumeLink as resumeLink, ca.Phone as MobileNumber from (select distinct im.id imid,im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail,            
im.ClientHrContact, im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, IRF.JobCountry,           
im.PreferredLocation, im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.candidateName, im.currentEmployerName,             
im.QCInterviewer, im.IsEmergency, im.InterviewBiometryScore, im.JoiningBiometryScore,            
IRF.id as irfIntId,IRF.JDLink as JDLink,IRF.JDTiltle,IRF.ClientName,IRF.IsBiometricEnabled,ic.VideoLink,ic.InterviewerComment,      
ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, ic.AadharFileLink, cm.id as CandidateId           
,ulc.Isonline as IsOnline,ulh.Isonline as IsHrOnline,uli.Isonline  as IsInterviewerOnline, im.HRCoordinatorEmailId, im.HRComment, cl.[Name] as Client, qcfr.QCStatus,ic.isLock,ic.ClientUserid as Emailid,ic.MoveToCP, (CASE WHEN ic.CandidateAvailable = 1 THEN 'Available' ELSE 'NotAvailable' END) as CandidateAvailable     
from InterviewMeetings im            
inner join IRF on im.IRFId = IRF.IRFId            
left join UserList ulc on im.candidateEmail= ulc.EmailId          
left join UserList ulh on im.ClientHrContact= ulh.EmailId          
left join UserList uli on im.interviewer= uli.EmailId          
left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId            
join CandidatesMaster cm on ulc.id = cm.UserId      
left join QCFeedbackReport qcfr on qcfr.InterviewId=im.id      
left join Client cl on cl.id = irf.ClientId) Meetings            
join Candidate ca on Meetings.CandidateId = ca.CandidateMasterId and Meetings.irfIntId= ca.IRFId      
where (Meetings.InterviewStatus=2 or Meetings.InterviewStatus=4) and interviewDate >= @startDate AND interviewDate < @endDate          
END         
             
End               
          
                 
        
       
	   Go