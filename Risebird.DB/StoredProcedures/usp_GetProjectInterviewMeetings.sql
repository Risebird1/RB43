IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetProjectInterviewMeetings'))
BEGIN
    DROP PROCEDURE dbo.usp_GetProjectInterviewMeetings
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO    
CREATE PROCEDURE [dbo].[usp_GetProjectInterviewMeetings]        
@id int,        
@emailId varchar(100) = null        
AS        
Begin   
declare @Roleid int 
Select @Roleid=cu.RoleId from ClientUser cu join UserList ul on ul.id=cu.Userid where ul.Emailid= @emailId

declare @temp as Table(  
IRFId varchar(15)  
)          
Insert into @temp select IRF.IRFId From Project pr join ProjectIRFMapping pim on pr.id=pim.ProjectId join IRF on IRF.id=pim.IRFId where pr.id=@id  
if(@Roleid=1)
begin
select irf.PerMintuteRate, irf.TotalBudget,QCF.InterviewId as QCInterview,im.id as imid, im.IRFId, im.interviewDate,          
 im.interviewSlot, im.interviewDuration, ul.EmailId as candidateEmail, ca.ClientHRAttemptContact as ClientHrContact,        
 im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,          
 im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy,          
 case when ul.LastName is null then ul.FirstName        
  else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName))        
 end as candidateName,         
 im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.InterviewBiometryScore,          
 im.JoiningBiometryScore, ca.Phone as MobileNumber, ca.ResumeLink as resumeLink, ic.*, irf.*, qcf.*,          
 ul.Isonline as IsOnline, ulh.Isonline as IsHrOnline, irf.IsQCRequired,im.RedoInterview        
 from @temp te join InterviewMeetings im on te.IRFId=im.IRFId            
 left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId left join irf on IRF.IRFId = im.IRFId            
 left join QCFeedbackReport QCF on im.id=QCF.InterviewId          
 left join userlist ul on im.candidateEmail = ul.EmailId          
 join CandidatesMaster cm on cm.UserId = ul.id          
 join Candidate ca on cm.id = ca.CandidateMasterId and irf.id= ca.IRFId          
 left join userlist ulh on im.ClientHrContact = ulh.EmailId 
 where ca.ClientHRAttemptContact=@emailId
end
else if(@Roleid=4)
begin
select irf.PerMintuteRate, irf.TotalBudget,QCF.InterviewId as QCInterview,im.id as imid, im.IRFId, im.interviewDate,          
 im.interviewSlot, im.interviewDuration, ul.EmailId as candidateEmail, ca.ClientHRAttemptContact as ClientHrContact,        
 im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,          
 im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy,          
 case when ul.LastName is null then ul.FirstName        
  else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName))        
 end as candidateName,         
 im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.InterviewBiometryScore,          
 im.JoiningBiometryScore, ca.Phone as MobileNumber, ca.ResumeLink as resumeLink, ic.*, irf.*, qcf.*,          
 ul.Isonline as IsOnline, ulh.Isonline as IsHrOnline, irf.IsQCRequired,im.RedoInterview        
 from @temp te join InterviewMeetings im on te.IRFId=im.IRFId            
 left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId left join irf on IRF.IRFId = im.IRFId            
 left join QCFeedbackReport QCF on im.id=QCF.InterviewId          
 left join userlist ul on im.candidateEmail = ul.EmailId          
 join CandidatesMaster cm on cm.UserId = ul.id          
 join Candidate ca on cm.id = ca.CandidateMasterId and irf.id= ca.IRFId          
 left join userlist ulh on im.ClientHrContact = ulh.EmailId 
 where ca.ProfileSource=@emailId
end
else if(@Roleid=9)
begin

 select irf.PerMintuteRate, irf.TotalBudget,QCF.InterviewId as QCInterview,im.id as imid, im.IRFId, im.interviewDate,          
 im.interviewSlot, im.interviewDuration, ul.EmailId as candidateEmail, ca.ClientHRAttemptContact as ClientHrContact,        
 im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,          
 im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy,          
 case when ul.LastName is null then ul.FirstName        
  else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName))        
 end as candidateName,         
 im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.InterviewBiometryScore,          
 im.JoiningBiometryScore, ca.Phone as MobileNumber, ca.ResumeLink as resumeLink, ic.*, irf.*, qcf.*,          
 ul.Isonline as IsOnline, ulh.Isonline as IsHrOnline, irf.IsQCRequired,im.RedoInterview        
 from @temp te join InterviewMeetings im on te.IRFId=im.IRFId            
 left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId left join irf on IRF.IRFId = im.IRFId            
 left join QCFeedbackReport QCF on im.id=QCF.InterviewId          
 left join userlist ul on im.candidateEmail = ul.EmailId          
 join CandidatesMaster cm on cm.UserId = ul.id          
 join Candidate ca on cm.id = ca.CandidateMasterId and irf.id= ca.IRFId          
 left join userlist ulh on im.ClientHrContact = ulh.EmailId          
 end
 else
 begin
 select irf.PerMintuteRate, irf.TotalBudget,QCF.InterviewId as QCInterview,im.id as imid, im.IRFId, im.interviewDate,          
 im.interviewSlot, im.interviewDuration, ul.EmailId as candidateEmail, ca.ClientHRAttemptContact as ClientHrContact,        
 im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,          
 im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy,          
 case when ul.LastName is null then ul.FirstName        
  else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName))        
 end as candidateName,         
 im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.InterviewBiometryScore,          
 im.JoiningBiometryScore, ca.Phone as MobileNumber, ca.ResumeLink as resumeLink, ic.*, irf.*, qcf.*,          
 ul.Isonline as IsOnline, ulh.Isonline as IsHrOnline, irf.IsQCRequired,im.RedoInterview        
 from @temp te join InterviewMeetings im on te.IRFId=im.IRFId            
 left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId 
 left join irf on IRF.IRFId = im.IRFId            
 join TransClientUser tc on tc.IRFId=IRF.id join UserList uli on uli.id=tc.ClientUserId
 left join QCFeedbackReport QCF on im.id=QCF.InterviewId          
 left join userlist ul on im.candidateEmail = ul.EmailId          
 join CandidatesMaster cm on cm.UserId = ul.id          
 join Candidate ca on cm.id = ca.CandidateMasterId and irf.id= ca.IRFId          
 left join userlist ulh on im.ClientHrContact = ulh.EmailId  
 where uli.EmailId=@emailId
 end
End          
GO  