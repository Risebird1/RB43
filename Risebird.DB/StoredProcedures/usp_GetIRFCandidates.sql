/****** Object:  StoredProcedure [dbo].[usp_GetIRFCandidates]    Script Date: 10-06-2020 9.24.16 PM ******/
-- Performance (2026-06-17): pre-aggregated comm counts + interview status; removed per-row scalar UDFs
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetIRFCandidates'))
BEGIN
    DROP PROCEDURE dbo.usp_GetIRFCandidates
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_GetIRFCandidates]    
@IRFId int,  
@emailId varchar(50) = null  
AS    
Begin      
declare @IRFName varchar(100), @roleId int  
set @IRFName = (select IRFId from IRF where id =@IRFId)  

declare @tblAttemptStatusCount table (IRFId int, CandidateId int, AttemptsCount int);      
 insert into @tblAttemptStatusCount      
 select IRFId, CandidateId, count(id) from CandidateStatus where IRFId = @IRFId group by IRFId, CandidateId       

-- Precompute per-email lookups once (set-based) instead of calling scalar UDFs per row.
-- Replaces dbo.fn_HasEmailDroppedOrBounced (was a full SendgridEventData scan per candidate).
declare @tblEmailDroppedBounced table (EmailId varchar(100) primary key);
insert into @tblEmailDroppedBounced (EmailId)
select distinct ul.EmailId
from Candidate ca
join CandidatesMaster cm on ca.CandidateMasterId = cm.id
join UserList ul on ul.id = cm.UserId
join SendgridEventData sed on sed.email = ul.EmailId
where ca.IRFId = @IRFId
and (sed.[event] like '%bounce%' or sed.[event] like '%drop%');

-- Replaces dbo.fn_candidateCurrentInterviewStatus for this IRF (@IRFName = irf.IRFId for every row).
declare @tblEmailInterviewStatus table (EmailId varchar(100) primary key, StatusDesc varchar(50));
insert into @tblEmailInterviewStatus (EmailId, StatusDesc)
select candidateEmail,
       case InterviewStatus
            when 1 then 'Proxy'
            when 2 then 'Strongly Selected'
            when 3 then 'Strongly Rejected'
            when 4 then 'Marginally Selected'
            when 5 then 'Marginally Rejected'
            else 'NA'
       end
from (
    select im.candidateEmail, ic.InterviewStatus,
           row_number() over (partition by im.candidateEmail order by im.interviewDate desc) as rn
    from InterviewMeetings im
    join InterviewComments ic on im.id = ic.InterviewId
    where im.confirmationStatus = 1 and im.irfid = @IRFName
) latest
where rn = 1;

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
select Cnd.*, ims.confirmationStatus, ims.interviewDate, ims.MeetingStatus, ims.InterviewStatus, ims.VideoLink, ims.imid,ims.SessionId from       
(select ca.id, ca.IRFId, ca.ResumeLink,ca.TotalInvites, ca.[Type], ca.[Location], ca.Skill, ca.WorkAuth, ca.CurrentLocation, ca.CurrentEmployer,      
ca.TotalExperience, ca.CurrentSalary, ca.ExpectedSalary, ca.Notice, ca.SelectabilityComments,ca.Heuristicsscore,      
ca.JoinabilityComments, ca.JoinabilityScore, ca.SelectabilityScore, ca.TechSkillComments, ca.SkillGeneralComments,ca.CostEstimate,      
ca.TechSkillScore, ca.SkillGeneralScore, ca.AddedByUserId, ca.VoiceItUserId, ca.InviteDate, ca.[Status], ul.EmailId, ca.Phone,ca.PreReportLink,ca.PreReportUsername,ca.PreReportPassword,   
ISNULL(eis.StatusDesc, 'NA') as CurrentInterviewStatus, CAST(CASE WHEN edb.EmailId IS NOT NULL THEN 1 ELSE 0 END AS bit) as HasEmailDroppedOrBounced,  
case   
  when ul.LastName is null then ul.FirstName  
  else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName))  
end as [Name],  
IRF.EndDate, IRF.IRFId as IRFName,IRF.IsAIInterview,cm.id as CandidateMasterId ,Isnull(ca.EncodedResumeLink,'') as EncodedResumeLink, ca.AvailableFullTime, ca.AvailableFreeLancing,      
tasc.AttemptsCount, ca.ClientHRAttemptContact, ca.ProfileSource,ul.WhatsappNumber,ca.IsProctor, ca.CodedResumeLink, ca.IsResumeRedactAttempted,  
ca.IsResumeRedactFailed,  
--si.Uniqueid as SchedulerUniqueid,  
SUBSTRING( IRF.CalendlyLink , LEN(IRF.CalendlyLink) -  CHARINDEX('=',REVERSE(IRF.CalendlyLink)) + 2  , LEN(IRF.CalendlyLink)) as SchedulerUniqueid  
,ISNULL(wc.WhatsappCount, 0) as WhatsappCount, ISNULL(cc.CallsCount, 0) as CallsCount, ISNULL(mc.MailCount, 0) as MailCount,LastCandidateStatus.LatestComment,LastCandidateStatus.StatusValue,IRF.JDTiltle,ulcho.EmailId as ChannelOwnerEmail,  
cci.IsColdCandidate, ca.AIResumeScreeningVerdict, ca.AIResumeScreeningScore   
from Candidate ca join IRF on ca.IRFId=IRF.id      
join CandidatesMaster cm on ca.CandidateMasterId=cm.id        
join UserList ul on ul.id = cm.UserId  
join UserList ulcho on ulcho.id  = irf.RisebirdHr  
left join @tblAttemptStatusCount tasc on tasc.CandidateId = cm.id  
left join ColdCandidateInfos cci on ca.id=cci.FKCandidateId  
left join @tblEmailInterviewStatus eis on eis.EmailId = ul.EmailId  
left join @tblEmailDroppedBounced edb on edb.EmailId = ul.EmailId  
--left join ScheduleInterviewEvent si on (si.SkillChannel=IRF.IRFId and SUBSTRING( IRF.CalendlyLink , LEN(IRF.CalendlyLink) -  CHARINDEX('=',REVERSE(IRF.CalendlyLink)) + 2  , LEN(IRF.CalendlyLink)) = si.Uniqueid)  
OUTER APPLY dbo.fn_GetCandidateCommunicationCountForIRF(irf.id, ul.Emailid) as CountTable  
outer apply(select top 1 csom.StatusValue,cs.Comments as LatestComment From CandidateStatus cs join CandidateStatusOptionsMst csom on cs.[Status]=csom.id  where cs.IRFId=ca.IRFId and CandidateId = ca.CandidateMasterId order by cs.Date desc) as LastCandidateStatus  
where ca.IRFId = @IRFId and (ca.ClientHRAttemptContact = @emailId or ca.AddedByUserId =(select id from UserList where EmailId =@emailId))) Cnd        
left join (select im.id as imid,im.interviewDate, im.SessionId,im.confirmationStatus, ic.MeetingStatus, ic.InterviewStatus,      
ic.VideoLink, im.candidateEmail, im.IRFId from InterviewMeetings im join      
(select candidateEmail, max(interviewDate) as intereviewDate from InterviewMeetings where IRFId = @IRFName and Isnull(Isdeleted,0)=0 group by candidateEmail) Filtered      
on im.candidateEmail= Filtered.candidateEmail and im.interviewDate=Filtered.intereviewDate      
left join InterviewComments ic on ic.InterviewId = im.id) ims on ims.candidateEmail= Cnd.EmailId and Cnd.IRFName = ims.IRFId      
end  
else if(@roleId = 4) --staffing agency
begin  
select Cnd.*, ims.confirmationStatus, ims.interviewDate, ims.MeetingStatus, ims.InterviewStatus, ims.VideoLink, ims.imid,ims.SessionId from       
(select ca.id, ca.IRFId, ca.ResumeLink,ca.TotalInvites, ca.[Type], ca.[Location], ca.Skill, ca.WorkAuth, ca.CurrentLocation, ca.CurrentEmployer,      
ca.TotalExperience, ca.CurrentSalary, ca.ExpectedSalary, ca.Notice, ca.SelectabilityComments,ca.Heuristicsscore,      
ca.JoinabilityComments, ca.JoinabilityScore, ca.SelectabilityScore, ca.TechSkillComments, ca.SkillGeneralComments,ca.CostEstimate,      
ca.TechSkillScore, ca.SkillGeneralScore, ca.AddedByUserId, ca.VoiceItUserId, ca.InviteDate, ca.[Status], ul.EmailId, ca.Phone,ca.PreReportLink,ca.PreReportUsername,ca.PreReportPassword,   
ISNULL(eis.StatusDesc, 'NA') as CurrentInterviewStatus, CAST(CASE WHEN edb.EmailId IS NOT NULL THEN 1 ELSE 0 END AS bit) as HasEmailDroppedOrBounced,  
case   
  when ul.LastName is null then ul.FirstName  
  else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName))  
end as [Name],  
IRF.EndDate, IRF.IRFId as IRFName,IRF.IsAIInterview,cm.id as CandidateMasterId ,Isnull(ca.EncodedResumeLink,'') as EncodedResumeLink, ca.AvailableFullTime, ca.AvailableFreeLancing,      
tasc.AttemptsCount, ca.ClientHRAttemptContact, ca.ProfileSource,ul.WhatsappNumber,ca.IsProctor, ca.CodedResumeLink, ca.IsResumeRedactAttempted,  
ca.IsResumeRedactFailed,  
--si.Uniqueid as SchedulerUniqueid,  
SUBSTRING( IRF.CalendlyLink , LEN(IRF.CalendlyLink) -  CHARINDEX('=',REVERSE(IRF.CalendlyLink)) + 2  , LEN(IRF.CalendlyLink)) as SchedulerUniqueid  
,ISNULL(wc.WhatsappCount, 0) as WhatsappCount, ISNULL(cc.CallsCount, 0) as CallsCount, ISNULL(mc.MailCount, 0) as MailCount,LastCandidateStatus.LatestComment,LastCandidateStatus.StatusValue,IRF.JDTiltle,ulcho.EmailId as ChannelOwnerEmail,  
cci.IsColdCandidate, ca.AIResumeScreeningVerdict, ca.AIResumeScreeningScore    
from Candidate ca join IRF on ca.IRFId=IRF.id      
join CandidatesMaster cm on ca.CandidateMasterId=cm.id        
join UserList ul on ul.id = cm.UserId  
join UserList ulcho on ulcho.id  = irf.RisebirdHr  
left join @tblAttemptStatusCount tasc on tasc.CandidateId = cm.id  
left join ColdCandidateInfos cci on ca.id=cci.FKCandidateId  
left join @tblEmailInterviewStatus eis on eis.EmailId = ul.EmailId  
left join @tblEmailDroppedBounced edb on edb.EmailId = ul.EmailId  
--left join ScheduleInterviewEvent si on (si.SkillChannel=IRF.IRFId and SUBSTRING( IRF.CalendlyLink , LEN(IRF.CalendlyLink) -  CHARINDEX('=',REVERSE(IRF.CalendlyLink)) + 2  , LEN(IRF.CalendlyLink)) = si.Uniqueid)  
OUTER APPLY dbo.fn_GetCandidateCommunicationCountForIRF(irf.id, ul.Emailid) as CountTable  
outer apply(select top 1 csom.StatusValue,cs.Comments as LatestComment From CandidateStatus cs join CandidateStatusOptionsMst csom on cs.[Status]=csom.id  where cs.IRFId=ca.IRFId and CandidateId = ca.CandidateMasterId order by cs.Date desc) as LastCandidateStatus  
where ca.IRFId = @IRFId and (ca.ProfileSource = @emailId or ca.AddedByUserId =(select id from UserList where EmailId =@emailId))) Cnd        
left join (select im.id as imid,im.interviewDate,im.SessionId, im.confirmationStatus, ic.MeetingStatus, ic.InterviewStatus,      
ic.VideoLink, im.candidateEmail, im.IRFId from InterviewMeetings im join      
(select candidateEmail, max(interviewDate) as intereviewDate from InterviewMeetings where IRFId = @IRFName and Isnull(Isdeleted,0)=0 group by candidateEmail) Filtered      
on im.candidateEmail= Filtered.candidateEmail and im.interviewDate=Filtered.intereviewDate      
left join InterviewComments ic on ic.InterviewId = im.id) ims on ims.candidateEmail= Cnd.EmailId and Cnd.IRFName = ims.IRFId      
end  
  
else  
begin  
select Cnd.*, ims.confirmationStatus, ims.interviewDate, ims.MeetingStatus, ims.InterviewStatus, ims.VideoLink, ims.imid,ims.SessionId from       
(select ca.id, ca.IRFId, ca.ResumeLink,ca.TotalInvites, ca.[Type], ca.[Location], ca.Skill, ca.WorkAuth, ca.CurrentLocation, ca.CurrentEmployer,      
ca.TotalExperience, ca.CurrentSalary, ca.ExpectedSalary, ca.Notice, ca.SelectabilityComments,ca.Heuristicsscore,      
ca.JoinabilityComments, ca.JoinabilityScore, ca.SelectabilityScore, ca.TechSkillComments, ca.SkillGeneralComments,ca.CostEstimate,      
ca.TechSkillScore, ca.SkillGeneralScore, ca.AddedByUserId, ca.VoiceItUserId, ca.InviteDate, ca.[Status], ul.EmailId, ca.Phone,ca.PreReportLink,ca.PreReportUsername,ca.PreReportPassword,      
ISNULL(eis.StatusDesc, 'NA') as CurrentInterviewStatus, CAST(CASE WHEN edb.EmailId IS NOT NULL THEN 1 ELSE 0 END AS bit) as HasEmailDroppedOrBounced,  
case   
  when ul.LastName is null then ul.FirstName  
  else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName))  
end as [Name],  
IRF.EndDate, IRF.IRFId as IRFName,IRF.IsAIInterview,cm.id as CandidateMasterId ,Isnull(ca.EncodedResumeLink,'') as EncodedResumeLink, ca.AvailableFullTime, ca.AvailableFreeLancing,      
tasc.AttemptsCount, ca.ClientHRAttemptContact, ca.ProfileSource,ul.WhatsappNumber,ca.IsProctor, ca.CodedResumeLink, ca.IsResumeRedactAttempted,  
ca.IsResumeRedactFailed,  
--si.Uniqueid as SchedulerUniqueid,  
SUBSTRING( IRF.CalendlyLink , LEN(IRF.CalendlyLink) -  CHARINDEX('=',REVERSE(IRF.CalendlyLink)) + 2  , LEN(IRF.CalendlyLink)) as SchedulerUniqueid  
,ISNULL(wc.WhatsappCount, 0) as WhatsappCount, ISNULL(cc.CallsCount, 0) as CallsCount, ISNULL(mc.MailCount, 0) as MailCount,LastCandidateStatus.LatestComment,LastCandidateStatus.StatusValue, IRF.JDTiltle,
ulcho.EmailId as ChannelOwnerEmail  
,cci.IsColdCandidate, ca.AIResumeScreeningVerdict, ca.AIResumeScreeningScore  
from Candidate ca join IRF on ca.IRFId=IRF.id      
join CandidatesMaster cm on ca.CandidateMasterId=cm.id        
join UserList ul on ul.id = cm.UserId  
join UserList ulcho on ulcho.id  = irf.RisebirdHr  
left join @tblAttemptStatusCount tasc on tasc.CandidateId = cm.id   
left join ColdCandidateInfos cci on ca.id=cci.FKCandidateId  
left join @tblEmailInterviewStatus eis on eis.EmailId = ul.EmailId  
left join @tblEmailDroppedBounced edb on edb.EmailId = ul.EmailId  
--left join ScheduleInterviewEvent si on si.SkillChannel=IRF.IRFId  
OUTER APPLY dbo.fn_GetCandidateCommunicationCountForIRF(irf.id, ul.Emailid) as CountTable  
outer apply(select top 1 csom.StatusValue,cs.Comments as LatestComment From CandidateStatus cs join CandidateStatusOptionsMst csom on cs.[Status]=csom.id  where cs.IRFId=ca.IRFId and CandidateId = ca.CandidateMasterId order by cs.Date desc) as LastCandidateStatus  
where ca.IRFId = @IRFId) Cnd        
left join (select im.id as imid,im.interviewDate,im.SessionId, im.confirmationStatus, ic.MeetingStatus, ic.InterviewStatus,      
ic.VideoLink, im.candidateEmail, im.IRFId from InterviewMeetings im join      
(select candidateEmail, max(interviewDate) as intereviewDate from InterviewMeetings where IRFId = @IRFName and Isnull(Isdeleted,0)=0 group by candidateEmail) Filtered      
on im.candidateEmail= Filtered.candidateEmail and im.interviewDate=Filtered.intereviewDate      
left join InterviewComments ic on ic.InterviewId = im.id) ims on ims.candidateEmail= Cnd.EmailId and Cnd.IRFName = ims.IRFId     
  
  
end  
      
End  
GO


 

