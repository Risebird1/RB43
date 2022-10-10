IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetCandidateTimelineByDateStatus'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCandidateTimelineByDateStatus
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_GetCandidateTimelineByDateStatus]       
@EmailId varchar(100),                          
@startDate datetime,                          
@endDate datetime,                  
@datatype int    
    
AS                            
Begin                            
declare @profileId int, @roleId int, @userId int=0;      
declare @Clientid int    
declare @clientRole int            
if(@datatype=1)                  
Begin                  
set @Clientid= (Select cu.ClientId from UserList ul join ClientUser cu on cu.UserId=ul.id where ul.EmailId=@Emailid)    
set @clientRole =(select top 1 cu.RoleId from UserList ul join ClientUser cu on cu.UserId = ul.id where ul.EmailId =@Emailid)            
  if(@clientRole = 4) -- staffing agency            
begin     
  ;with CTE (Emailid,IRFId,candidateName,JDTiltle,MobileNumber) as (Select distinct ul.EmailId,IRF.IRFId,(ISNULL(ul.FirstName,'')+' '+ISNULL(ul.LastName,'')) as candidateName,IRF.JDTiltle,ul.MobileNumber From Candidate c join CandidatesMaster cm on cm.id=c.CandidateMasterId                   
 join UserList ul on ul.id=cm.UserId join irf on irf.id=c.IRFId where irf.ClientId=@Clientid  and irf.EndDate>=GetDate() and IsNull(IsPaused,0)=0                       
 Union All Select distinct im.candidateEmail,irf.IRFId ,(ISNULL(ul.FirstName,'')+' '+ISNULL(ul.LastName,'')) as candidateName,IRF.JDTiltle,ul.MobileNumber from InterviewMeetings im join irf on irf.IRFId=im.IRFId join Userlist ul on ul.EmailId=im.candidateEmail where irf.ClientId=@Clientid and irf.EndDate>=GetDate() and IsNull(IsPaused,0)=0)                          
 Select ab.*,csm.StatusValue as ScheduleStatusValue,cs.Comments as ScheduleStatusComments from(  
 Select Cd.CandidateId,Cd.imid,Cd.IRFId,Cd.Emailid,Cd.IRFName,Cd.Name,Cd.JDTiltle as PrimarySkill,Cd.MobileNumber,Cd.resumeLink,Cd.ClientHrContact as HREmail,Max(dtCreatedOn) as interviewDate,CalendlyLink,ClientName,JobCountry,Cd.CandidateMasterId,Cd.ProfileSource from            
 (Select c.id as CandidateId,IRF.id as imid,IRF.id as IRFId,CTE.Emailid,CTE.IRFId as IRFName,candidateName as Name,CTE.JDTiltle,CTE.MobileNumber,c.resumeLink,c.ClientHRAttemptContact as ClientHrContact,cs.Date as dtCreatedOn,IRF.CalendlyLink,cl.Name as ClientName,IRF.JobCountry,c.CandidateMasterId,c.ProfileSource from CTE join    
 IRF on CTE.IRFID=IRF.IRFId left join Userlist ul on ul.EmailId=CTE.Emailid left join CandidatesMaster cm on cm.UserId=ul.id            
 left join Candidate c on c.IRFId=IRF.id and c.CandidateMasterId=cm.id                        
 left join CandidateStatus cs on cs.CandidateId=cm.id and cs.IRFId=c.IRFId and cs.Status=12                        
 join TransClientUser tcu on tcu.IRFId=IRF.id                      
 left join Userlist tcul on tcul.id=tcu.ClientUserId             
 join Client cl on cl.id=IRF.ClientId            
 Where tcul.EmailId=@Emailid and cs.Date is not Null and c.ProfileSource=@Emailid                     
 group by c.id,IRF.id,IRF.id,CTE.Emailid,CTE.IRFId,candidateName,CTE.JDTiltle,CTE.MobileNumber,c.resumeLink,c.ClientHRAttemptContact,cs.Date,IRF.CalendlyLink,cl.Name,IRF.JobCountry,c.CandidateMasterId,c.ProfileSource having Count(*)=1) as Cd              
  
 where cd.dtCreatedOn>=@startDate and cd.dtCreatedOn<@endDate    
 group by Cd.CandidateId,Cd.imid,Cd.IRFId,Cd.Emailid,Cd.IRFName,Cd.Name,Cd.JDTiltle,Cd.MobileNumber,Cd.resumeLink,Cd.ClientHrContact,CalendlyLink,ClientName,JobCountry,Cd.CandidateMasterId,Cd.ProfileSource)as ab   
 join CandidateStatus cs on ab.interviewDate=cs.Date and ab.IRFId=cs.IRFId and ab.CandidateMasterId=cs.CandidateId  
 join CandidateStatusOptionsMst csm on csm.id=cs.Status  
 order by ab.interviewDate desc  
  
end    
  else if(@clientRole = 1)        
begin      
 ;with CTE (Emailid,IRFId,candidateName,JDTiltle,MobileNumber) as (Select distinct ul.EmailId,IRF.IRFId,(ISNULL(ul.FirstName,'')+' '+ISNULL(ul.LastName,'')) as candidateName,IRF.JDTiltle,ul.MobileNumber From Candidate c join CandidatesMaster cm on cm.id=c
.CandidateMasterId                   
 join UserList ul on ul.id=cm.UserId join irf on irf.id=c.IRFId where irf.ClientId=@Clientid  and irf.EndDate>=GetDate() and IsNull(IsPaused,0)=0                       
 Union All Select distinct im.candidateEmail,irf.IRFId ,(ISNULL(ul.FirstName,'')+' '+ISNULL(ul.LastName,'')) as candidateName,IRF.JDTiltle,ul.MobileNumber from InterviewMeetings im join irf on irf.IRFId=im.IRFId join Userlist ul on ul.EmailId=im.candidateEmail where irf.ClientId=@Clientid and irf.EndDate>=GetDate() and IsNull(IsPaused,0)=0)                          
 Select ab.*,csm.StatusValue as ScheduleStatusValue,cs.Comments as ScheduleStatusComments from(  
 Select Cd.CandidateId,Cd.imid,Cd.IRFId,Cd.Emailid,Cd.IRFName,Cd.Name,Cd.JDTiltle as PrimarySkill,Cd.MobileNumber,Cd.resumeLink,Cd.ClientHrContact as HREmail,Max(dtCreatedOn) as interviewDate,CalendlyLink,ClientName,JobCountry,Cd.CandidateMasterId,Cd.ProfileSource from            
 (Select c.id as CandidateId,IRF.id as imid,IRF.id as IRFId,CTE.Emailid,CTE.IRFId as IRFName,candidateName as Name,CTE.JDTiltle,CTE.MobileNumber,c.resumeLink,c.ClientHRAttemptContact as ClientHrContact,cs.Date as dtCreatedOn,IRF.CalendlyLink,cl.Name as ClientName,IRF.JobCountry,c.CandidateMasterId,c.ProfileSource  from CTE join    
 IRF on CTE.IRFID=IRF.IRFId left join Userlist ul on ul.EmailId=CTE.Emailid left join CandidatesMaster cm on cm.UserId=ul.id            
 left join Candidate c on c.IRFId=IRF.id and c.CandidateMasterId=cm.id                   
 left join CandidateStatus cs on cs.CandidateId=cm.id and cs.IRFId=c.IRFId and cs.Status=12                        
 join TransClientUser tcu on tcu.IRFId=IRF.id                      
 left join Userlist tcul on tcul.id=tcu.ClientUserId             
 join Client cl on cl.id=IRF.ClientId            
 Where tcul.EmailId=@Emailid and cs.Date is not Null and c.ClientHRAttemptContact=@Emailid                     
 group by c.id,IRF.id,IRF.id,CTE.Emailid,CTE.IRFId,candidateName,CTE.JDTiltle,CTE.MobileNumber,c.resumeLink,c.ClientHRAttemptContact,cs.Date,IRF.CalendlyLink,cl.Name,IRF.JobCountry,c.CandidateMasterId,c.ProfileSource having Count(*)=1) as Cd              
  
 where cd.dtCreatedOn>=@startDate and cd.dtCreatedOn<@endDate    
 group by Cd.CandidateId,Cd.imid,Cd.IRFId,Cd.Emailid,Cd.IRFName,Cd.Name,Cd.JDTiltle,Cd.MobileNumber,Cd.resumeLink,Cd.ClientHrContact,CalendlyLink,ClientName,JobCountry,Cd.CandidateMasterId,Cd.ProfileSource)as ab   
 join CandidateStatus cs on ab.interviewDate=cs.Date and ab.IRFId=cs.IRFId and ab.CandidateMasterId=cs.CandidateId  
 join CandidateStatusOptionsMst csm on csm.id=cs.Status  
 order by ab.interviewDate desc  
  
end    
else    
begin    
 ;with CTE (Emailid,IRFId,candidateName,JDTiltle,MobileNumber) as (Select distinct ul.EmailId,IRF.IRFId,(ISNULL(ul.FirstName,'')+' '+ISNULL(ul.LastName,'')) as candidateName,IRF.JDTiltle,ul.MobileNumber From Candidate c join CandidatesMaster cm on cm.id=c.CandidateMasterId                   
 join UserList ul on ul.id=cm.UserId join irf on irf.id=c.IRFId where irf.ClientId=@Clientid  and irf.EndDate>=GetDate() and IsNull(IsPaused,0)=0                       
 Union All Select distinct im.candidateEmail,irf.IRFId ,(ISNULL(ul.FirstName,'')+' '+ISNULL(ul.LastName,'')) as candidateName,IRF.JDTiltle,ul.MobileNumber from InterviewMeetings im join irf on irf.IRFId=im.IRFId join Userlist ul on ul.EmailId=im.candidateEmail where irf.ClientId=@Clientid and irf.EndDate>=GetDate() and IsNull(IsPaused,0)=0)                          
 Select ab.*,csm.StatusValue as ScheduleStatusValue,cs.Comments as ScheduleStatusComments from(  
 Select Cd.CandidateId,Cd.imid,Cd.IRFId,Cd.Emailid,Cd.IRFName,Cd.Name,Cd.JDTiltle as PrimarySkill,Cd.MobileNumber,Cd.resumeLink,Cd.ClientHrContact as HREmail,Max(dtCreatedOn) as interviewDate,CalendlyLink,ClientName,JobCountry,Cd.CandidateMasterId,Cd.ProfileSource from            
 (Select c.id as CandidateId,IRF.id as imid,IRF.id as IRFId,CTE.Emailid,CTE.IRFId as IRFName,candidateName as Name,CTE.JDTiltle,CTE.MobileNumber,c.resumeLink,c.ClientHRAttemptContact as ClientHrContact,cs.Date as dtCreatedOn,IRF.CalendlyLink,cl.Name as ClientName,IRF.JobCountry,c.CandidateMasterId,c.ProfileSource from CTE join    
 IRF on CTE.IRFID=IRF.IRFId left join Userlist ul on ul.EmailId=CTE.Emailid left join CandidatesMaster cm on cm.UserId=ul.id            
 left join Candidate c on c.IRFId=IRF.id and c.CandidateMasterId=cm.id                   
 left join CandidateStatus cs on cs.CandidateId=cm.id and cs.IRFId=c.IRFId and cs.Status=12                        
 join TransClientUser tcu on tcu.IRFId=IRF.id                      
 left join Userlist tcul on tcul.id=tcu.ClientUserId             
 join Client cl on cl.id=IRF.ClientId            
 Where tcul.EmailId=@Emailid and cs.Date is not Null                      
 group by c.id,IRF.id,IRF.id,CTE.Emailid,CTE.IRFId,candidateName,CTE.JDTiltle,CTE.MobileNumber,c.resumeLink,c.ClientHRAttemptContact,cs.Date,IRF.CalendlyLink,cl.Name,IRF.JobCountry,c.CandidateMasterId,c.ProfileSource having Count(*)=1) as Cd              
 
 where cd.dtCreatedOn>=@startDate and cd.dtCreatedOn<@endDate    
 group by Cd.CandidateId,Cd.imid,Cd.IRFId,Cd.Emailid,Cd.IRFName,Cd.Name,Cd.JDTiltle,Cd.MobileNumber,Cd.resumeLink,Cd.ClientHrContact,CalendlyLink,ClientName,JobCountry,Cd.CandidateMasterId,Cd.ProfileSource)as ab   
 join CandidateStatus cs on ab.interviewDate=cs.Date and ab.IRFId=cs.IRFId and ab.CandidateMasterId=cs.CandidateId  
 join CandidateStatusOptionsMst csm on csm.id=cs.Status  
 order by ab.interviewDate desc  
  
end    
End                  
Else                  
BEGIN                  
if @EmailId is not null                            
 Begin                            
                            
  set @profileId = (select id from [Profile] where [Name]='Client')                            
  set @userId  = (select id from UserList where EmailId=@EmailId and idProfile=@profileId)                            
  set @roleId  = (select RoleId from ClientUser where UserId=@userId)                            
                          
  if(Isnull(@userId,0)=0)                        
  Begin                        
  set @profileId = (select id from [Profile] where [Name]='SeniorHR')                           
  set @userId  = (select id from UserList where EmailId=@EmailId and idProfile=@profileId)                         
                          
  if(Isnull(@userId,0)>0)                        
  begin                        
    Select Distinct ca.id as CandidateId, ca.CandidateMasterId, ul.EmailId,                          
    case                           
     when ul.LastName is null then ul.FirstName                          
     else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName))                          
    end as [Name],                          
    ca.ClientHRAttemptContact as HREmail, ca.ProfileSource,im.id as InterviewId,IRF.id as IRFId,    
 cs.Comments as ScheduleStatusComments,Max(cs.Date) as ScheduleStatusDate,csm.StatusValue as ScheduleStatusValue,    
    ic.InterviewStatus as CurrentInterviewStatus,                           
    ic.MeetingStatus as CurrentMeetingStatus, im.confirmationStatus, irf.IRFId as IRFName, ca.Phone, im.interviewDate,                        
    (CASE WHEN CHARINDEX('|', JDTiltle) > 0 THEN SUBSTRING(JDTiltle, 1, CHARINDEX('|', JDTiltle) -1) ELSE JDTiltle END) as PrimarySkill,ul.MobileNumber,IRF.id as imid,IRF.IRFId as IRFname,IRF.CalendlyLink,ic.InterviewerNickName,im.Interviewer    
from InterviewMeetings im join IRF on IRF.IRFId=im.IRFId    
join UserList ul on ul.EmailId=im.candidateEmail     
join CandidatesMaster cm on cm.UserId=ul.id    
join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id    
left join CandidateStatus cs on cs.CandidateId=cm.id and cs.IRFId=IRF.id and cs.InterviewId=im.id    
join CandidateStatusOptionsMst csm on csm.id=cs.Status    
join SeniorHr sh on sh.Clientid=IRF.ClientId                        
left join Userlist ulsh on ulsh.id=sh.Userid    
join InterviewComments ic on ic.Interviewid=im.id    
where ulsh.EmailId=@EmailId and im.interviewDate>=@startDate and im.interviewDate<=@endDate     
group by ca.id, ca.CandidateMasterId, ul.EmailId,                          
    case when ul.LastName is null then ul.FirstName else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName)) end,                          
    ca.ClientHRAttemptContact, ca.ProfileSource,im.id,IRF.id,cs.Comments,csm.StatusValue,ic.InterviewStatus,ic.MeetingStatus, im.confirmationStatus, irf.IRFId, ca.Phone,im.interviewDate,      
 (CASE WHEN CHARINDEX('|', JDTiltle) > 0 THEN SUBSTRING(JDTiltle, 1, CHARINDEX('|', JDTiltle) -1) ELSE JDTiltle END),ul.MobileNumber,IRF.id,IRF.IRFId,IRF.CalendlyLink,ic.InterviewerNickName,im.Interviewer    
    
  End                        
 End                            
                         
  else if(@roleId=1)                            
   Begin                            
     Select Distinct ca.id as CandidateId, ca.CandidateMasterId, ul.EmailId,                          
    case                           
     when ul.LastName is null then ul.FirstName                          
     else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName))                          
    end as [Name],                          
    ca.ClientHRAttemptContact as HREmail, ca.ProfileSource,     
 cs.Comments as ScheduleStatusComments,Max(cs.Date) as ScheduleStatusDate,csm.StatusValue as ScheduleStatusValue,    
    ic.InterviewStatus as CurrentInterviewStatus,im.id as InterviewId,IRF.id as IRFId,                           
    ic.MeetingStatus as CurrentMeetingStatus, im.confirmationStatus, irf.IRFId as IRFName, ca.Phone, im.interviewDate,                        
    (CASE WHEN CHARINDEX('|', JDTiltle) > 0 THEN SUBSTRING(JDTiltle, 1, CHARINDEX('|', JDTiltle) -1) ELSE JDTiltle END) as PrimarySkill,ul.MobileNumber,IRF.id as imid,IRF.IRFId as IRFname,IRF.CalendlyLink,ic.InterviewerNickName,im.Interviewer    
from InterviewMeetings im join IRF on IRF.IRFId=im.IRFId    
join UserList ul on ul.EmailId=im.candidateEmail     
join CandidatesMaster cm on cm.UserId=ul.id    
join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id    
left join CandidateStatus cs on cs.CandidateId=cm.id and cs.IRFId=IRF.id and cs.InterviewId=im.id    
join CandidateStatusOptionsMst csm on csm.id=cs.Status    
inner join TransClientUser tcu on IRF.id=tcu.IRFId                          
left join UserList ulc on tcu.ClientUserId = ulc.id     
join InterviewComments ic on ic.Interviewid=im.id    
where ulc.EmailId = @EmailId AND ulc.IsActive=1 and irf.IsDeleted=0 and ca.ClientHRAttemptContact=@EmailId    
and im.interviewDate>=@startDate and im.interviewDate<=@endDate     
group by ca.id, ca.CandidateMasterId, ul.EmailId,                          
    case when ul.LastName is null then ul.FirstName else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName)) end,                          
    ca.ClientHRAttemptContact, ca.ProfileSource,cs.Comments,csm.StatusValue,ic.InterviewStatus,im.id,IRF.id,ic.MeetingStatus, im.confirmationStatus, irf.IRFId, ca.Phone,im.interviewDate,      
 (CASE WHEN CHARINDEX('|', JDTiltle) > 0 THEN SUBSTRING(JDTiltle, 1, CHARINDEX('|', JDTiltle) -1) ELSE JDTiltle END),ul.MobileNumber,IRF.id,IRF.IRFId,IRF.CalendlyLink,ic.InterviewerNickName,im.Interviewer    
          
   End                           
  else if(@roleId=4)                          
  Begin                          
      Select Distinct ca.id as CandidateId, ca.CandidateMasterId, ul.EmailId,                          
    case                           
     when ul.LastName is null then ul.FirstName                          
     else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName))                          
    end as [Name],                          
    ca.ClientHRAttemptContact as HREmail, ca.ProfileSource,     
 cs.Comments as ScheduleStatusComments,Max(cs.Date) as ScheduleStatusDate,csm.StatusValue as ScheduleStatusValue,    
    ic.InterviewStatus as CurrentInterviewStatus,im.id as InterviewId,IRF.id as IRFId,                           
    ic.MeetingStatus as CurrentMeetingStatus, im.confirmationStatus, irf.IRFId as IRFName, ca.Phone, im.interviewDate,                        
    (CASE WHEN CHARINDEX('|', JDTiltle) > 0 THEN SUBSTRING(JDTiltle, 1, CHARINDEX('|', JDTiltle) -1) ELSE JDTiltle END) as PrimarySkill,ul.MobileNumber,IRF.id as imid,IRF.IRFId as IRFname,IRF.CalendlyLink,ic.InterviewerNickName,im.Interviewer    
from InterviewMeetings im join IRF on IRF.IRFId=im.IRFId    
join UserList ul on ul.EmailId=im.candidateEmail     
join CandidatesMaster cm on cm.UserId=ul.id    
join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id    
left join CandidateStatus cs on cs.CandidateId=cm.id and cs.IRFId=IRF.id and cs.InterviewId=im.id    
join CandidateStatusOptionsMst csm on csm.id=cs.Status    
inner join TransClientUser tcu on IRF.id=tcu.IRFId                          
left join UserList ulc on tcu.ClientUserId = ulc.id     
join InterviewComments ic on ic.Interviewid=im.id    
where ulc.EmailId = @EmailId AND ulc.IsActive=1 and irf.IsDeleted=0 and (ca.ProfileSource=@EmailId or im.staffingAgency= @EmailId)                           
and im.interviewDate>=@startDate and im.interviewDate<=@endDate     
group by ca.id, ca.CandidateMasterId, ul.EmailId,                          
    case when ul.LastName is null then ul.FirstName else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName)) end,                          
    ca.ClientHRAttemptContact, ca.ProfileSource,im.id,IRF.id,cs.Comments,csm.StatusValue,ic.InterviewStatus,ic.MeetingStatus, im.confirmationStatus, irf.IRFId, ca.Phone,im.interviewDate,      
 (CASE WHEN CHARINDEX('|', JDTiltle) > 0 THEN SUBSTRING(JDTiltle, 1, CHARINDEX('|', JDTiltle) -1) ELSE JDTiltle END),ul.MobileNumber,IRF.id,IRF.IRFId,IRF.CalendlyLink,ic.InterviewerNickName,im.Interviewer            
                          
  End                          
  else                            
   Begin                            
       Select Distinct ca.id as CandidateId, ca.CandidateMasterId, ul.EmailId,                          
    case                           
     when ul.LastName is null then ul.FirstName                          
     else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName))                          
    end as [Name],                          
    ca.ClientHRAttemptContact as HREmail, ca.ProfileSource,     
 cs.Comments as ScheduleStatusComments,Max(cs.Date) as ScheduleStatusDate,csm.StatusValue as ScheduleStatusValue,    
    ic.InterviewStatus as CurrentInterviewStatus,im.id as InterviewId,IRF.id as IRFId,                           
    ic.MeetingStatus as CurrentMeetingStatus, im.confirmationStatus, irf.IRFId as IRFName, ca.Phone, im.interviewDate,                        
    (CASE WHEN CHARINDEX('|', JDTiltle) > 0 THEN SUBSTRING(JDTiltle, 1, CHARINDEX('|', JDTiltle) -1) ELSE JDTiltle END) as PrimarySkill,ul.MobileNumber,IRF.id as imid,IRF.IRFId as IRFname,IRF.CalendlyLink,ic.InterviewerNickName,im.Interviewer from InterviewMeetings im 
  
join IRF on IRF.IRFId=im.IRFId    
join UserList ul on ul.EmailId=im.candidateEmail     
join CandidatesMaster cm on cm.UserId=ul.id    
join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id    
left join CandidateStatus cs on cs.CandidateId=cm.id and cs.IRFId=IRF.id and cs.InterviewId=im.id    
join CandidateStatusOptionsMst csm on csm.id=cs.Status    
inner join TransClientUser tcu on IRF.id=tcu.IRFId                          
left join UserList ulc on tcu.ClientUserId = ulc.id     
join InterviewComments ic on ic.Interviewid=im.id    
where ulc.EmailId = @EmailId AND ulc.IsActive=1 and irf.IsDeleted=0    
and im.interviewDate>=@startDate and im.interviewDate<=@endDate     
group by ca.id, ca.CandidateMasterId, ul.EmailId,                          
    case when ul.LastName is null then ul.FirstName else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName)) end,                          
    ca.ClientHRAttemptContact, ca.ProfileSource,cs.Comments,csm.StatusValue,ic.InterviewStatus,ic.MeetingStatus,im.id,IRF.id, im.confirmationStatus, irf.IRFId, ca.Phone,im.interviewDate,     
    (CASE WHEN CHARINDEX('|', JDTiltle) > 0 THEN SUBSTRING(JDTiltle, 1, CHARINDEX('|', JDTiltle) -1) ELSE JDTiltle END),ul.MobileNumber,IRF.id,IRF.IRFId,IRF.CalendlyLink,ic.InterviewerNickName,im.Interviewer    
             
   End                            
 End                            
Else                            
 Begin    
  Select Distinct ca.id as CandidateId, ca.CandidateMasterId, ul.EmailId,                          
    case                           
     when ul.LastName is null then ul.FirstName                          
     else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName))                          
    end as [Name],                          
    ca.ClientHRAttemptContact as HREmail, ca.ProfileSource,im.id as InterviewId,IRF.id as IRFId,     
 cs.Comments as ScheduleStatusComments,Max(cs.Date) as ScheduleStatusDate,csm.StatusValue as ScheduleStatusValue,    
    ic.InterviewStatus as CurrentInterviewStatus,                           
    ic.MeetingStatus as CurrentMeetingStatus, im.confirmationStatus, irf.IRFId as IRFName, ca.Phone, im.interviewDate,                        
    (CASE WHEN CHARINDEX('|', JDTiltle) > 0 THEN SUBSTRING(JDTiltle, 1, CHARINDEX('|', JDTiltle) -1) ELSE JDTiltle END) as PrimarySkill,ul.MobileNumber,IRF.id as imid,IRF.IRFId as IRFname,IRF.CalendlyLink,ic.InterviewerNickName,im.Interviewer    
from InterviewMeetings im join IRF on IRF.IRFId=im.IRFId    
join UserList ul on ul.EmailId=im.candidateEmail     
join CandidatesMaster cm on cm.UserId=ul.id    
join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id    
left join CandidateStatus cs on cs.CandidateId=cm.id and cs.IRFId=IRF.id and cs.InterviewId=im.id    
join CandidateStatusOptionsMst csm on csm.id=cs.Status    
inner join TransClientUser tcu on IRF.id=tcu.IRFId                          
left join UserList ulc on tcu.ClientUserId = ulc.id     
join InterviewComments ic on ic.Interviewid=im.id    
where im.interviewDate>=@startDate and im.interviewDate<=@endDate     
group by ca.id, ca.CandidateMasterId, ul.EmailId,                          
    case when ul.LastName is null then ul.FirstName else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName)) end,                          
    ca.ClientHRAttemptContact, ca.ProfileSource,im.id,IRF.id,cs.Comments,csm.StatusValue,ic.InterviewStatus,ic.MeetingStatus, im.confirmationStatus, irf.IRFId, ca.Phone,im.interviewDate,      
 (CASE WHEN CHARINDEX('|', JDTiltle) > 0 THEN SUBSTRING(JDTiltle, 1, CHARINDEX('|', JDTiltle) -1) ELSE JDTiltle END),ul.MobileNumber,IRF.id,IRF.IRFId,IRF.CalendlyLink,ic.InterviewerNickName,im.Interviewer    
    
 End                   
END                  
                             
End



Go