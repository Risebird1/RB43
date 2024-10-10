IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAllCandidateSearchbyDateStatus'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAllCandidateSearchbyDateStatus
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_GetAllCandidateSearchbyDateStatus
@EmailId varchar(100),                                
@startDate datetime,                                
@endDate datetime,
@datatype int
AS
BEGIN
declare @Clientid int,@roleId int,@userId int,@profileId int
set @Clientid= (Select cu.ClientId from UserList ul join ClientUser cu on cu.UserId=ul.id where ul.EmailId=@Emailid)  
set @profileId = (select id from [Profile] where [Name]='Client')      
set @userId  = (select id from UserList where EmailId=@EmailId and idProfile=@profileId) 
set @roleId  = (select RoleId from ClientUser where UserId=@userId)      
--Processed Candidates
if(@datatype=2)
begin
if(@roleId=1)
begin
;with CTE as(Select Distinct ca.id as CandidateId, ca.CandidateMasterId, ul.EmailId as CandidateEmailId,                                
case when ul.LastName is null then ul.FirstName else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName)) end as CandidateName,                                
ca.ClientHRAttemptContact as HREmail, ca.ProfileSource,IRF.id as IRFId,                                 
irf.IRFId as IRFname, MAX(im.interviewDate) interviewDate,                              
Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end as PrimarySkill,      
ul.MobileNumber as Phone,IRF.CalendlyLink,ISNULL(cci.IsColdCandidate,0) as IsColdCandidate
from InterviewMeetings im join IRF on IRF.IRFId=im.IRFId          
join UserList ul on ul.EmailId=im.candidateEmail           
join CandidatesMaster cm on cm.UserId=ul.id          
join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id          
left join ColdCandidateInfos cci on ca.id=cci.FKCandidateId 
inner join TransClientUser tcu on IRF.id=tcu.IRFId                                
left join UserList ulc on tcu.ClientUserId = ulc.id           
where ulc.EmailId = @EmailId and irf.IsDeleted=0 and ISNULL(im.IsDeleted,0)=0  and irf.Clientid=@Clientid and ca.ClientHRAttemptContact=@EmailId                           
and im.interviewDate>=@startDate and im.interviewDate<=@endDate
group by ca.id, ca.CandidateMasterId, ul.EmailId,                                
    case when ul.LastName is null then ul.FirstName else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName)) end,                                
    ca.ClientHRAttemptContact, ca.ProfileSource,IRF.id,  irf.IRFId,
 Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end,ul.MobileNumber,      
 IRF.id,IRF.IRFId,IRF.CalendlyLink,cci.IsColdCandidate),
 CTE2 as (Select ca.id as CandidateId,IRF.id as imid,IRF.id as IRFId,ul.Emailid,IRF.IRFId as IRFname,(ISNULL(ul.FirstName,'')+' '+ISNULL(ul.LastName,'')) as Name,Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end as JDTiltle,ul.MobileNumber,
ca.ClientHRAttemptContact as ClientHrContact,cs.Date as dtCreatedOn,IRF.CalendlyLink,ca.CandidateMasterId,ca.ProfileSource,SUBSTRING( IRF.CalendlyLink , LEN(IRF.CalendlyLink) -  CHARINDEX('=',REVERSE(IRF.CalendlyLink)) + 2  , LEN(IRF.CalendlyLink)) as SchedulerUniqueid,ISNULL(cci.IsColdCandidate,0) as IsColdCandidate    
From IRF join 
Candidate ca on ca.IRFId=IRF.id
join CandidatesMaster cm on cm.id=ca.CandidateMasterId
join UserList ul on ul.id=cm.UserId
left join ColdCandidateInfos cci on ca.id=cci.FKCandidateId    
left join InterviewMeetings im on im.candidateEmail=ul.EmailId and im.IRFId=IRF.IRFId
left join CandidateStatus cs on cs.CandidateId=cm.id and cs.IRFId=ca.IRFId and cs.Status=12  
inner join TransClientUser tcu on IRF.id=tcu.IRFId                                
left join UserList ulc on tcu.ClientUserId = ulc.id 
where ulc.EmailId = @EmailId and ca.ClientHRAttemptContact=@EmailId and cs.Date>=@startDate and cs.Date<@endDate and IRF.ClientId=@Clientid and im.id is null)
Select CTE.CandidateId,CTE.CandidateMasterId,CTE.IRFId,CTE.CandidateEmailId,CTE.CandidateName,CTE.IRFname,CTE.PrimarySkill,
CTE.Phone,CTE.HREmail,CTE.interviewDate,CTE.CalendlyLink,CTE.ProfileSource,im.RedoInterview,dbo.fn_HasEmailDroppedOrBounced(CTE.CandidateEmailId) as HasEmailDroppedOrBounced,
ic.InterviewStatus as CurrentInterviewStatus,im.id as InterviewId,CTE.IsColdCandidate,
ic.MeetingStatus as CurrentMeetingStatus, im.confirmationStatus,SUBSTRING(CTE.CalendlyLink , LEN(CTE.CalendlyLink) -  CHARINDEX('=',REVERSE(CTE.CalendlyLink)) + 2  , LEN(CTE.CalendlyLink)) as SchedulerUniqueid,Case when CTE.IsColdCandidate=1 then LastCandidateStatus.StatusValue else '' end as StatusValue
From CTE 
 join InterviewMeetings im on im.interviewDate=CTE.interviewDate and im.candidateEmail=CTE.CandidateEmailId and im.IRFId=CTE.IRFname
 join InterviewComments ic on ic.Interviewid=im.id  
 outer apply(select top 1 csom.StatusValue,cs.Comments as LatestComment From CandidateStatus cs join CandidateStatusOptionsMst csom on cs.[Status]=csom.id  where cs.IRFId=CTE.IRFId and CandidateId = CTE.CandidateMasterId and CTE.IsColdCandidate=1 order by cs.Date desc) as LastCandidateStatus    
union
Select CTE2.CandidateId,CTE2.CandidateMasterId,CTE2.IRFId,CTE2.EmailId as CandidateEmailId,CTE2.Name as CandidateName,CTE2.IRFname,CTE2.JDTiltle as PrimarySkill,
CTE2.MobileNumber as Phone,CTE2.ClientHrContact as HREmail,CTE2.dtCreatedOn as interviewDate,CTE2.CalendlyLink,
CTE2.ProfileSource,0 as RedoInterview,dbo.fn_HasEmailDroppedOrBounced(CTE2.EmailId) as HasEmailDroppedOrBounced,
null as CurrentInterviewStatus,0 as InterviewId,CTE2.IsColdCandidate, null as CurrentMeetingStatus,0 as confirmationStatus,CTE2.SchedulerUniqueid,ct.StatusValue
From CTE2 
outer Apply(Select top 1 cs.Date,csm.StatusValue From  CandidateStatus cs join CandidateStatusOptionsMst csm on csm.id=cs.Status
where CTE2.IRFId=cs.IRFId and CTE2.CandidateMasterId=cs.CandidateId order by cs.Date desc) ct    

end
else if(@roleId=4)
begin
Select 'ca.ProfileSource=@EmailId'
;with CTE as(Select Distinct ca.id as CandidateId, ca.CandidateMasterId, ul.EmailId as CandidateEmailId,                                
case when ul.LastName is null then ul.FirstName else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName)) end as CandidateName,                                
ca.ClientHRAttemptContact as HREmail, ca.ProfileSource,IRF.id as IRFId,                                 
irf.IRFId as IRFname, MAX(im.interviewDate) interviewDate,                              
Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end as PrimarySkill,      
ul.MobileNumber as Phone,IRF.CalendlyLink,ISNULL(cci.IsColdCandidate,0) as IsColdCandidate
from InterviewMeetings im join IRF on IRF.IRFId=im.IRFId          
join UserList ul on ul.EmailId=im.candidateEmail           
join CandidatesMaster cm on cm.UserId=ul.id          
join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id          
left join ColdCandidateInfos cci on ca.id=cci.FKCandidateId 
inner join TransClientUser tcu on IRF.id=tcu.IRFId                                
left join UserList ulc on tcu.ClientUserId = ulc.id           
where ulc.EmailId = @EmailId and irf.IsDeleted=0 and ISNULL(im.IsDeleted,0)=0  and irf.Clientid=@Clientid and ca.ProfileSource=@EmailId                           
and im.interviewDate>=@startDate and im.interviewDate<=@endDate
group by ca.id, ca.CandidateMasterId, ul.EmailId,                                
    case when ul.LastName is null then ul.FirstName else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName)) end,                                
    ca.ClientHRAttemptContact, ca.ProfileSource,IRF.id,  irf.IRFId,
 Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end,ul.MobileNumber,      
 IRF.id,IRF.IRFId,IRF.CalendlyLink,cci.IsColdCandidate),
 CTE2 as (Select ca.id as CandidateId,IRF.id as imid,IRF.id as IRFId,ul.Emailid,IRF.IRFId as IRFname,(ISNULL(ul.FirstName,'')+' '+ISNULL(ul.LastName,'')) as Name,Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end as JDTiltle,ul.MobileNumber,
ca.ClientHRAttemptContact as ClientHrContact,cs.Date as dtCreatedOn,IRF.CalendlyLink,ca.CandidateMasterId,ca.ProfileSource,SUBSTRING( IRF.CalendlyLink , LEN(IRF.CalendlyLink) -  CHARINDEX('=',REVERSE(IRF.CalendlyLink)) + 2  , LEN(IRF.CalendlyLink)) as SchedulerUniqueid,ISNULL(cci.IsColdCandidate,0) as IsColdCandidate    
From IRF join 
Candidate ca on ca.IRFId=IRF.id
join CandidatesMaster cm on cm.id=ca.CandidateMasterId
join UserList ul on ul.id=cm.UserId
left join ColdCandidateInfos cci on ca.id=cci.FKCandidateId    
left join InterviewMeetings im on im.candidateEmail=ul.EmailId and im.IRFId=IRF.IRFId
left join CandidateStatus cs on cs.CandidateId=cm.id and cs.IRFId=ca.IRFId and cs.Status=12                              
inner join TransClientUser tcu on IRF.id=tcu.IRFId                                
left join UserList ulc on tcu.ClientUserId = ulc.id   
where ulc.EmailId = @EmailId and ca.ProfileSource=@EmailId and cs.Date>=@startDate and cs.Date<@endDate and IRF.ClientId=@Clientid and im.id is null)
Select CTE.CandidateId,CTE.CandidateMasterId,CTE.IRFId,CTE.CandidateEmailId,CTE.CandidateName,CTE.IRFname,CTE.PrimarySkill,
CTE.Phone,CTE.HREmail,CTE.interviewDate,CTE.CalendlyLink,CTE.ProfileSource,im.RedoInterview,dbo.fn_HasEmailDroppedOrBounced(CTE.CandidateEmailId) as HasEmailDroppedOrBounced,
ic.InterviewStatus as CurrentInterviewStatus,im.id as InterviewId,CTE.IsColdCandidate,
ic.MeetingStatus as CurrentMeetingStatus, im.confirmationStatus,SUBSTRING(CTE.CalendlyLink , LEN(CTE.CalendlyLink) -  CHARINDEX('=',REVERSE(CTE.CalendlyLink)) + 2  , LEN(CTE.CalendlyLink)) as SchedulerUniqueid,Case when CTE.IsColdCandidate=1 then LastCandidateStatus.StatusValue else '' end as StatusValue
From CTE 
 join InterviewMeetings im on im.interviewDate=CTE.interviewDate and im.candidateEmail=CTE.CandidateEmailId and im.IRFId=CTE.IRFname
 join InterviewComments ic on ic.Interviewid=im.id  
 outer apply(select top 1 csom.StatusValue,cs.Comments as LatestComment From CandidateStatus cs join CandidateStatusOptionsMst csom on cs.[Status]=csom.id  where cs.IRFId=CTE.IRFId and CandidateId = CTE.CandidateMasterId and CTE.IsColdCandidate=1 order by cs.Date desc) as LastCandidateStatus    
union
Select CTE2.CandidateId,CTE2.CandidateMasterId,CTE2.IRFId,CTE2.EmailId as CandidateEmailId,CTE2.Name as CandidateName,CTE2.IRFname,CTE2.JDTiltle as PrimarySkill,
CTE2.MobileNumber as Phone,CTE2.ClientHrContact as HREmail,CTE2.dtCreatedOn as interviewDate,CTE2.CalendlyLink,
CTE2.ProfileSource,0 as RedoInterview,dbo.fn_HasEmailDroppedOrBounced(CTE2.EmailId) as HasEmailDroppedOrBounced,
null as CurrentInterviewStatus,0 as InterviewId,CTE2.IsColdCandidate, null as CurrentMeetingStatus,0 as confirmationStatus,CTE2.SchedulerUniqueid,ct.StatusValue
From CTE2 
outer Apply(Select top 1 cs.Date,csm.StatusValue From  CandidateStatus cs join CandidateStatusOptionsMst csm on csm.id=cs.Status
where CTE2.IRFId=cs.IRFId and CTE2.CandidateMasterId=cs.CandidateId order by cs.Date desc) ct 
end
else if(@roleId=9)
begin
;with CTE as(Select Distinct ca.id as CandidateId, ca.CandidateMasterId, ul.EmailId as CandidateEmailId,                                
case when ul.LastName is null then ul.FirstName else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName)) end as CandidateName,                                
ca.ClientHRAttemptContact as HREmail, ca.ProfileSource,IRF.id as IRFId,                                 
irf.IRFId as IRFname, MAX(im.interviewDate) interviewDate,                              
Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end as PrimarySkill,      
ul.MobileNumber as Phone,IRF.CalendlyLink,ISNULL(cci.IsColdCandidate,0) as IsColdCandidate
from InterviewMeetings im join IRF on IRF.IRFId=im.IRFId          
join UserList ul on ul.EmailId=im.candidateEmail           
join CandidatesMaster cm on cm.UserId=ul.id          
join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id          
left join ColdCandidateInfos cci on ca.id=cci.FKCandidateId    
where irf.IsDeleted=0 and ISNULL(im.IsDeleted,0)=0  and irf.Clientid=@Clientid                            
and im.interviewDate>=@startDate and im.interviewDate<=@endDate
group by ca.id, ca.CandidateMasterId, ul.EmailId,                                
    case when ul.LastName is null then ul.FirstName else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName)) end,                                
    ca.ClientHRAttemptContact, ca.ProfileSource,IRF.id,  irf.IRFId,
 Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end,ul.MobileNumber,      
 IRF.id,IRF.IRFId,IRF.CalendlyLink,cci.IsColdCandidate),
 CTE2 as (Select ca.id as CandidateId,IRF.id as imid,IRF.id as IRFId,ul.Emailid,IRF.IRFId as IRFname,(ISNULL(ul.FirstName,'')+' '+ISNULL(ul.LastName,'')) as Name,Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end as JDTiltle,ul.MobileNumber,
ca.ClientHRAttemptContact as ClientHrContact,cs.Date as dtCreatedOn,IRF.CalendlyLink,ca.CandidateMasterId,ca.ProfileSource,SUBSTRING( IRF.CalendlyLink , LEN(IRF.CalendlyLink) -  CHARINDEX('=',REVERSE(IRF.CalendlyLink)) + 2  , LEN(IRF.CalendlyLink)) as SchedulerUniqueid,ISNULL(cci.IsColdCandidate,0) as IsColdCandidate    
From IRF join 
Candidate ca on ca.IRFId=IRF.id
join CandidatesMaster cm on cm.id=ca.CandidateMasterId
join UserList ul on ul.id=cm.UserId
left join ColdCandidateInfos cci on ca.id=cci.FKCandidateId    
left join InterviewMeetings im on im.candidateEmail=ul.EmailId and im.IRFId=IRF.IRFId
left join CandidateStatus cs on cs.CandidateId=cm.id and cs.IRFId=ca.IRFId and cs.Status=12                              
where cs.Date>=@startDate and cs.Date<@endDate and IRF.ClientId=@Clientid and im.id is null)
Select CTE.CandidateId,CTE.CandidateMasterId,CTE.IRFId,CTE.CandidateEmailId,CTE.CandidateName,CTE.IRFname,CTE.PrimarySkill,
CTE.Phone,CTE.HREmail,CTE.interviewDate,CTE.CalendlyLink,CTE.ProfileSource,im.RedoInterview,dbo.fn_HasEmailDroppedOrBounced(CTE.CandidateEmailId) as HasEmailDroppedOrBounced,
ic.InterviewStatus as CurrentInterviewStatus,im.id as InterviewId,CTE.IsColdCandidate,
ic.MeetingStatus as CurrentMeetingStatus, im.confirmationStatus,SUBSTRING(CTE.CalendlyLink , LEN(CTE.CalendlyLink) -  CHARINDEX('=',REVERSE(CTE.CalendlyLink)) + 2  , LEN(CTE.CalendlyLink)) as SchedulerUniqueid,Case when CTE.IsColdCandidate=1 then LastCandidateStatus.StatusValue else '' end as StatusValue
From CTE 
 join InterviewMeetings im on im.interviewDate=CTE.interviewDate and im.candidateEmail=CTE.CandidateEmailId and im.IRFId=CTE.IRFname
 join InterviewComments ic on ic.Interviewid=im.id  
 outer apply(select top 1 csom.StatusValue,cs.Comments as LatestComment From CandidateStatus cs join CandidateStatusOptionsMst csom on cs.[Status]=csom.id  where cs.IRFId=CTE.IRFId and CandidateId = CTE.CandidateMasterId and CTE.IsColdCandidate=1 order by cs.Date desc) as LastCandidateStatus    
union
Select CTE2.CandidateId,CTE2.CandidateMasterId,CTE2.IRFId,CTE2.EmailId as CandidateEmailId,CTE2.Name as CandidateName,CTE2.IRFname,CTE2.JDTiltle as PrimarySkill,
CTE2.MobileNumber as Phone,CTE2.ClientHrContact as HREmail,CTE2.dtCreatedOn as interviewDate,CTE2.CalendlyLink,
CTE2.ProfileSource,0 as RedoInterview,dbo.fn_HasEmailDroppedOrBounced(CTE2.EmailId) as HasEmailDroppedOrBounced,
null as CurrentInterviewStatus,0 as InterviewId,CTE2.IsColdCandidate, null as CurrentMeetingStatus,0 as confirmationStatus,CTE2.SchedulerUniqueid,ct.StatusValue
From CTE2 
outer Apply(Select top 1 cs.Date,csm.StatusValue From  CandidateStatus cs join CandidateStatusOptionsMst csm on csm.id=cs.Status
where CTE2.IRFId=cs.IRFId and CTE2.CandidateMasterId=cs.CandidateId order by cs.Date desc) ct    
end
else
begin
;with CTE as(Select Distinct ca.id as CandidateId, ca.CandidateMasterId, ul.EmailId as CandidateEmailId,                                
case when ul.LastName is null then ul.FirstName else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName)) end as CandidateName,                                
ca.ClientHRAttemptContact as HREmail, ca.ProfileSource,IRF.id as IRFId,                                 
irf.IRFId as IRFname, MAX(im.interviewDate) interviewDate,                              
Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end as PrimarySkill,      
ul.MobileNumber as Phone,IRF.CalendlyLink,ISNULL(cci.IsColdCandidate,0) as IsColdCandidate
from InterviewMeetings im join IRF on IRF.IRFId=im.IRFId          
join UserList ul on ul.EmailId=im.candidateEmail           
join CandidatesMaster cm on cm.UserId=ul.id          
join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id          
left join ColdCandidateInfos cci on ca.id=cci.FKCandidateId    
inner join TransClientUser tcu on IRF.id=tcu.IRFId                                
left join UserList ulc on tcu.ClientUserId = ulc.id   
where ulc.EmailId = @EmailId AND irf.IsDeleted=0 and ISNULL(im.IsDeleted,0)=0  and irf.Clientid=@Clientid                            
and im.interviewDate>=@startDate and im.interviewDate<=@endDate
group by ca.id, ca.CandidateMasterId, ul.EmailId,                                
    case when ul.LastName is null then ul.FirstName else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName)) end,                                
    ca.ClientHRAttemptContact, ca.ProfileSource,IRF.id,  irf.IRFId,
 Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end,ul.MobileNumber,      
 IRF.id,IRF.IRFId,IRF.CalendlyLink,cci.IsColdCandidate),
 CTE2 as (Select ca.id as CandidateId,IRF.id as imid,IRF.id as IRFId,ul.Emailid,IRF.IRFId as IRFname,(ISNULL(ul.FirstName,'')+' '+ISNULL(ul.LastName,'')) as Name,Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end as JDTiltle,ul.MobileNumber,
ca.ClientHRAttemptContact as ClientHrContact,cs.Date as dtCreatedOn,IRF.CalendlyLink,ca.CandidateMasterId,ca.ProfileSource,SUBSTRING( IRF.CalendlyLink , LEN(IRF.CalendlyLink) -  CHARINDEX('=',REVERSE(IRF.CalendlyLink)) + 2  , LEN(IRF.CalendlyLink)) as SchedulerUniqueid,ISNULL(cci.IsColdCandidate,0) as IsColdCandidate    
From IRF join 
Candidate ca on ca.IRFId=IRF.id
join CandidatesMaster cm on cm.id=ca.CandidateMasterId
join UserList ul on ul.id=cm.UserId
left join ColdCandidateInfos cci on ca.id=cci.FKCandidateId    
left join InterviewMeetings im on im.candidateEmail=ul.EmailId and im.IRFId=IRF.IRFId
left join CandidateStatus cs on cs.CandidateId=cm.id and cs.IRFId=ca.IRFId and cs.Status=12                              
inner join TransClientUser tcu on IRF.id=tcu.IRFId                                
left join UserList ulc on tcu.ClientUserId = ulc.id   
where ulc.EmailId = @EmailId AND cs.Date>=@startDate and cs.Date<@endDate and IRF.ClientId=@Clientid and im.id is null)
Select CTE.CandidateId,CTE.CandidateMasterId,CTE.IRFId,CTE.CandidateEmailId,CTE.CandidateName,CTE.IRFname,CTE.PrimarySkill,
CTE.Phone,CTE.HREmail,CTE.interviewDate,CTE.CalendlyLink,CTE.ProfileSource,im.RedoInterview,dbo.fn_HasEmailDroppedOrBounced(CTE.CandidateEmailId) as HasEmailDroppedOrBounced,
ic.InterviewStatus as CurrentInterviewStatus,im.id as InterviewId,CTE.IsColdCandidate,
ic.MeetingStatus as CurrentMeetingStatus, im.confirmationStatus,SUBSTRING(CTE.CalendlyLink , LEN(CTE.CalendlyLink) -  CHARINDEX('=',REVERSE(CTE.CalendlyLink)) + 2  , LEN(CTE.CalendlyLink)) as SchedulerUniqueid,Case when CTE.IsColdCandidate=1 then LastCandidateStatus.StatusValue else '' end as StatusValue
From CTE 
 join InterviewMeetings im on im.interviewDate=CTE.interviewDate and im.candidateEmail=CTE.CandidateEmailId and im.IRFId=CTE.IRFname
 join InterviewComments ic on ic.Interviewid=im.id  
 outer apply(select top 1 csom.StatusValue,cs.Comments as LatestComment From CandidateStatus cs join CandidateStatusOptionsMst csom on cs.[Status]=csom.id  where cs.IRFId=CTE.IRFId and CandidateId = CTE.CandidateMasterId and CTE.IsColdCandidate=1 order by cs.Date desc) as LastCandidateStatus    
union
Select CTE2.CandidateId,CTE2.CandidateMasterId,CTE2.IRFId,CTE2.EmailId as CandidateEmailId,CTE2.Name as CandidateName,CTE2.IRFname,CTE2.JDTiltle as PrimarySkill,
CTE2.MobileNumber as Phone,CTE2.ClientHrContact as HREmail,CTE2.dtCreatedOn as interviewDate,CTE2.CalendlyLink,
CTE2.ProfileSource,0 as RedoInterview,dbo.fn_HasEmailDroppedOrBounced(CTE2.EmailId) as HasEmailDroppedOrBounced,
null as CurrentInterviewStatus,0 as InterviewId,CTE2.IsColdCandidate, null as CurrentMeetingStatus,0 as confirmationStatus,CTE2.SchedulerUniqueid,ct.StatusValue
From CTE2 
outer Apply(Select top 1 cs.Date,csm.StatusValue From  CandidateStatus cs join CandidateStatusOptionsMst csm on csm.id=cs.Status
where CTE2.IRFId=cs.IRFId and CTE2.CandidateMasterId=cs.CandidateId order by cs.Date desc) ct 
end
end
--Not Processed Resume
else if(@datatype=1)
begin
if(@roleId=1 or @roleId=4)
begin
select irf.IRFId as IRFname, cr.ResumeLink, cr.IsResumeParsed, cr.IsResumeParsingFailed, cr.IsEmailMissing, cr.AddedOn, cr.AddedBy, cr.IRFId
from CandidateResume cr   
join IRF on irf.id = cr.IRFId  
left join UserList ul on ul.id=cr.AddedBy
where irf.ClientId =@clientId and cr.IsProcessed = 0  and ul.EmailId=@EmailId
order by irf.IRFId  
end
else
begin
 select irf.IRFId as IRFname, cr.ResumeLink, cr.IsResumeParsed, cr.IsResumeParsingFailed, cr.IsEmailMissing, cr.AddedOn, cr.AddedBy, cr.IRFId from CandidateResume cr   
 join IRF on irf.id = cr.IRFId  
 where irf.ClientId =@clientId and cr.IsProcessed = 0   
 order by irf.IRFId  
end
end
END
GO