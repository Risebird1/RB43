  IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetProjectCandidates'))
BEGIN
    DROP PROCEDURE dbo.usp_GetProjectCandidates
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_GetProjectCandidates]    
@Projectid int,  
@emailId varchar(50) = null  
AS    
Begin  
declare @roleId int  
declare @IRFDetails As Table
(
    IRFId int,
	IRFName varchar(20)
)
Insert into @IRFDetails 
select distinct IRF.id as IRFId,IRF.IRFId as IRFName from Project pr join ProjectIRFMapping pim on pim.ProjectId=pr.id join IRF on IRF.id=pim.IRFId 
where pr.id =@Projectid  

declare @tblAttemptStatusCount table (IRFId int, CandidateId int, AttemptsCount int);      
 insert into @tblAttemptStatusCount      
 select IRFId, CandidateId, count(id) from CandidateStatus where IRFId in (Select IRFId from @IRFDetails) group by IRFId, CandidateId       
  
if(@emailId is not null)  
begin  
 declare @profile varchar(150)  
 set @profile = (select pr.[Name] from UserList ul join [Profile] pr on pr.id = ul.idProfile where ul.EmailId=@emailId)  
 if(@profile =  'Client')  
 begin  
  set @roleId = (select cu.RoleId from UserList ul join ClientUser cu on cu.UserId = ul.id where ul.EmailId = @emailId)  
 end  
end  
  
if(@roleId = 1) --client HR  
begin  
select Cnd.*, ims.confirmationStatus, ims.interviewDate, ims.MeetingStatus, ims.InterviewStatus, ims.VideoLink, ims.imid from       
(select ca.id, ca.IRFId, ca.ResumeLink,ca.TotalInvites, ca.[Type], ca.[Location], ca.Skill, ca.WorkAuth, ca.CurrentLocation, ca.CurrentEmployer,      
ca.TotalExperience, ca.CurrentSalary, ca.ExpectedSalary, ca.Notice, ca.SelectabilityComments,ca.Heuristicsscore,      
ca.JoinabilityComments, ca.JoinabilityScore, ca.SelectabilityScore, ca.TechSkillComments, ca.SkillGeneralComments,ca.CostEstimate,      
ca.TechSkillScore, ca.SkillGeneralScore, ca.AddedByUserId, ca.VoiceItUserId, ca.InviteDate, ca.[Status], ul.EmailId, ca.Phone,ca.PreReportLink,ca.PreReportUsername,ca.PreReportPassword,   
dbo.fn_candidateCurrentInterviewStatus(ul.EmailId, irf.IRFId) as CurrentInterviewStatus, dbo.fn_HasEmailDroppedOrBounced(ul.EmailId) as HasEmailDroppedOrBounced,  
case   
  when ul.LastName is null then ul.FirstName  
  else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName))  
end as [Name],  
IRF.EndDate, IRF.IRFId as IRFName,cm.id as CandidateMasterId ,Isnull(ca.EncodedResumeLink,'') as EncodedResumeLink, ca.AvailableFullTime, ca.AvailableFreeLancing,      
tasc.AttemptsCount, ca.ClientHRAttemptContact, ca.ProfileSource,ul.WhatsappNumber,ca.IsProctor, ca.CodedResumeLink, ca.IsResumeRedactAttempted,  
ca.IsResumeRedactFailed,  
SUBSTRING( IRF.CalendlyLink , LEN(IRF.CalendlyLink) -  CHARINDEX('=',REVERSE(IRF.CalendlyLink)) + 2  , LEN(IRF.CalendlyLink)) as SchedulerUniqueid  
,CountTable.WhatsappCount, CountTable.CallsCount, CountTable.MailCount,LastCandidateStatus.LatestComment,LastCandidateStatus.StatusValue,IRF.JDTiltle,ulcho.EmailId as ChannelOwnerEmail,  
cci.IsColdCandidate  
from Candidate ca join IRF on ca.IRFId=IRF.id      
join CandidatesMaster cm on ca.CandidateMasterId=cm.id        
join UserList ul on ul.id = cm.UserId  
join UserList ulcho on ulcho.id  = irf.RisebirdHr  
left join @tblAttemptStatusCount tasc on tasc.CandidateId = cm.id  
left join ColdCandidateInfos cci on ca.id=cci.FKCandidateId  
OUTER APPLY dbo.fn_GetCandidateCommunicationCountForIRF(irf.id, ul.Emailid) as CountTable  
outer apply(select top 1 csom.StatusValue,cs.Comments as LatestComment From CandidateStatus cs join CandidateStatusOptionsMst csom on cs.[Status]=csom.id  where cs.IRFId=ca.IRFId and CandidateId = ca.CandidateMasterId order by cs.Date desc) as LastCandidateStatus  
where ca.IRFId in (Select IRFId from @IRFDetails) and ca.ClientHRAttemptContact = @emailId) Cnd        
left join (select im.id as imid,im.interviewDate, im.confirmationStatus, ic.MeetingStatus, ic.InterviewStatus,      
ic.VideoLink, im.candidateEmail, im.IRFId from InterviewMeetings im join      
(select candidateEmail, max(interviewDate) as intereviewDate from InterviewMeetings where IRFId in (Select IRFName from @IRFDetails) group by candidateEmail) Filtered      
on im.candidateEmail= Filtered.candidateEmail and im.interviewDate=Filtered.intereviewDate      
left join InterviewComments ic on ic.InterviewId = im.id) ims on ims.candidateEmail= Cnd.EmailId and Cnd.IRFName = ims.IRFId      
end  
else if(@roleId = 4) --staffing agency
begin  
select Cnd.*, ims.confirmationStatus, ims.interviewDate, ims.MeetingStatus, ims.InterviewStatus, ims.VideoLink, ims.imid from       
(select ca.id, ca.IRFId, ca.ResumeLink,ca.TotalInvites, ca.[Type], ca.[Location], ca.Skill, ca.WorkAuth, ca.CurrentLocation, ca.CurrentEmployer,      
ca.TotalExperience, ca.CurrentSalary, ca.ExpectedSalary, ca.Notice, ca.SelectabilityComments,ca.Heuristicsscore,      
ca.JoinabilityComments, ca.JoinabilityScore, ca.SelectabilityScore, ca.TechSkillComments, ca.SkillGeneralComments,ca.CostEstimate,      
ca.TechSkillScore, ca.SkillGeneralScore, ca.AddedByUserId, ca.VoiceItUserId, ca.InviteDate, ca.[Status], ul.EmailId, ca.Phone,ca.PreReportLink,ca.PreReportUsername,ca.PreReportPassword,   
dbo.fn_candidateCurrentInterviewStatus(ul.EmailId, irf.IRFId) as CurrentInterviewStatus, dbo.fn_HasEmailDroppedOrBounced(ul.EmailId) as HasEmailDroppedOrBounced,  
case   
  when ul.LastName is null then ul.FirstName  
  else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName))  
end as [Name],  
IRF.EndDate, IRF.IRFId as IRFName,cm.id as CandidateMasterId ,Isnull(ca.EncodedResumeLink,'') as EncodedResumeLink, ca.AvailableFullTime, ca.AvailableFreeLancing,      
tasc.AttemptsCount, ca.ClientHRAttemptContact, ca.ProfileSource,ul.WhatsappNumber,ca.IsProctor, ca.CodedResumeLink, ca.IsResumeRedactAttempted,  
ca.IsResumeRedactFailed,  
SUBSTRING( IRF.CalendlyLink , LEN(IRF.CalendlyLink) -  CHARINDEX('=',REVERSE(IRF.CalendlyLink)) + 2  , LEN(IRF.CalendlyLink)) as SchedulerUniqueid  
,CountTable.WhatsappCount, CountTable.CallsCount, CountTable.MailCount,LastCandidateStatus.LatestComment,LastCandidateStatus.StatusValue,IRF.JDTiltle,ulcho.EmailId as ChannelOwnerEmail,  
cci.IsColdCandidate  
from Candidate ca join IRF on ca.IRFId=IRF.id      
join CandidatesMaster cm on ca.CandidateMasterId=cm.id        
join UserList ul on ul.id = cm.UserId  
join UserList ulcho on ulcho.id  = irf.RisebirdHr  
left join @tblAttemptStatusCount tasc on tasc.CandidateId = cm.id  
left join ColdCandidateInfos cci on ca.id=cci.FKCandidateId  
OUTER APPLY dbo.fn_GetCandidateCommunicationCountForIRF(irf.id, ul.Emailid) as CountTable  
outer apply(select top 1 csom.StatusValue,cs.Comments as LatestComment From CandidateStatus cs join CandidateStatusOptionsMst csom on cs.[Status]=csom.id  where cs.IRFId=ca.IRFId and CandidateId = ca.CandidateMasterId order by cs.Date desc) as LastCandidateStatus  
where ca.IRFId in (Select IRFId from @IRFDetails) and ca.ProfileSource = @emailId) Cnd        
left join (select im.id as imid,im.interviewDate, im.confirmationStatus, ic.MeetingStatus, ic.InterviewStatus,      
ic.VideoLink, im.candidateEmail, im.IRFId from InterviewMeetings im join      
(select candidateEmail, max(interviewDate) as intereviewDate from InterviewMeetings where IRFId in (Select IRFName from @IRFDetails) group by candidateEmail) Filtered      
on im.candidateEmail= Filtered.candidateEmail and im.interviewDate=Filtered.intereviewDate      
left join InterviewComments ic on ic.InterviewId = im.id) ims on ims.candidateEmail= Cnd.EmailId and Cnd.IRFName = ims.IRFId      
end  
else  
begin  
select Cnd.*, ims.confirmationStatus, ims.interviewDate, ims.MeetingStatus, ims.InterviewStatus, ims.VideoLink, ims.imid from       
(select ca.id, ca.IRFId, ca.ResumeLink,ca.TotalInvites, ca.[Type], ca.[Location], ca.Skill, ca.WorkAuth, ca.CurrentLocation, ca.CurrentEmployer,      
ca.TotalExperience, ca.CurrentSalary, ca.ExpectedSalary, ca.Notice, ca.SelectabilityComments,ca.Heuristicsscore,      
ca.JoinabilityComments, ca.JoinabilityScore, ca.SelectabilityScore, ca.TechSkillComments, ca.SkillGeneralComments,ca.CostEstimate,      
ca.TechSkillScore, ca.SkillGeneralScore, ca.AddedByUserId, ca.VoiceItUserId, ca.InviteDate, ca.[Status], ul.EmailId, ca.Phone,ca.PreReportLink,ca.PreReportUsername,ca.PreReportPassword,      
dbo.fn_candidateCurrentInterviewStatus(ul.EmailId, irf.IRFId) as CurrentInterviewStatus, dbo.fn_HasEmailDroppedOrBounced(ul.EmailId) as HasEmailDroppedOrBounced,  
case   
  when ul.LastName is null then ul.FirstName  
  else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName))  
end as [Name],  
IRF.EndDate, IRF.IRFId as IRFName,cm.id as CandidateMasterId ,Isnull(ca.EncodedResumeLink,'') as EncodedResumeLink, ca.AvailableFullTime, ca.AvailableFreeLancing,      
tasc.AttemptsCount, ca.ClientHRAttemptContact, ca.ProfileSource,ul.WhatsappNumber,ca.IsProctor, ca.CodedResumeLink, ca.IsResumeRedactAttempted,  
ca.IsResumeRedactFailed,  
SUBSTRING( IRF.CalendlyLink , LEN(IRF.CalendlyLink) -  CHARINDEX('=',REVERSE(IRF.CalendlyLink)) + 2  , LEN(IRF.CalendlyLink)) as SchedulerUniqueid  
,CountTable.WhatsappCount, CountTable.CallsCount, CountTable.MailCount,LastCandidateStatus.LatestComment,LastCandidateStatus.StatusValue, IRF.JDTiltle,ulcho.EmailId as ChannelOwnerEmail  
,cci.IsColdCandidate  
from Candidate ca join IRF on ca.IRFId=IRF.id      
join CandidatesMaster cm on ca.CandidateMasterId=cm.id        
join UserList ul on ul.id = cm.UserId  
join UserList ulcho on ulcho.id  = irf.RisebirdHr  
left join @tblAttemptStatusCount tasc on tasc.CandidateId = cm.id   
left join ColdCandidateInfos cci on ca.id=cci.FKCandidateId  
OUTER APPLY dbo.fn_GetCandidateCommunicationCountForIRF(irf.id, ul.Emailid) as CountTable  
outer apply(select top 1 csom.StatusValue,cs.Comments as LatestComment From CandidateStatus cs join CandidateStatusOptionsMst csom on cs.[Status]=csom.id  where cs.IRFId=ca.IRFId and CandidateId = ca.CandidateMasterId order by cs.Date desc) as LastCandidateStatus  
where ca.IRFId in (Select IRFId from @IRFDetails)) Cnd        
left join (select im.id as imid,im.interviewDate, im.confirmationStatus, ic.MeetingStatus, ic.InterviewStatus,      
ic.VideoLink, im.candidateEmail, im.IRFId from InterviewMeetings im join      
(select candidateEmail, max(interviewDate) as intereviewDate from InterviewMeetings where IRFId in (Select IRFName from @IRFDetails) group by candidateEmail) Filtered      
on im.candidateEmail= Filtered.candidateEmail and im.interviewDate=Filtered.intereviewDate      
left join InterviewComments ic on ic.InterviewId = im.id) ims on ims.candidateEmail= Cnd.EmailId and Cnd.IRFName = ims.IRFId     
end  
End
Go