IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetCandidateTimelineByDateStatus'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCandidateTimelineByDateStatus
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_GetCandidateTimelineByDateStatus] 
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
;with CTE as (Select ca.id as CandidateId,IRF.id as imid,IRF.id as IRFId,ul.Emailid,IRF.IRFId as IRFname,(ISNULL(ul.FirstName,'')+' '+ISNULL(ul.LastName,'')) as Name,Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end as JDTiltle,ul.MobileNumber,ca.resumeLink,
ca.ClientHRAttemptContact as ClientHrContact,cs.Date as dtCreatedOn,IRF.CalendlyLink,IRF.JobCountry,ca.CandidateMasterId,
ca.ProfileSource,SUBSTRING( IRF.CalendlyLink , LEN(IRF.CalendlyLink) -  CHARINDEX('=',REVERSE(IRF.CalendlyLink)) + 2  , LEN(IRF.CalendlyLink)) as SchedulerUniqueid,
IRF.EndDate,DATEDIFF(Day,cs.Date,Getdate()) as CandidateAging, irf.IsAIInterview, im.SessionId   
From IRF join 
Candidate ca on ca.IRFId=IRF.id
join CandidatesMaster cm on cm.id=ca.CandidateMasterId
join UserList ul on ul.id=cm.UserId
join TransClientUser tcu on tcu.IRFId=IRF.id                            
 left join Userlist tcul on tcul.id=tcu.ClientUserId                   
left join ColdCandidateInfos cci on ca.id=cci.FKCandidateId    
left join InterviewMeetings im on im.candidateEmail=ul.EmailId and im.IRFId=IRF.IRFId
left join CandidateStatus cs on cs.CandidateId=cm.id and cs.IRFId=ca.IRFId and cs.Status=12                              
where tcul.EmailId=@Emailid and ca.ProfileSource=@Emailid and cs.Date>=@startDate and cs.Date<@endDate and ISNULL(cci.IsColdCandidate,0) !=1 and im.id is null)
Select CTE.CandidateId,CTE.CandidateMasterId,CTE.IRFId,CTE.EmailId as CandidateEmailId,CTE.Name as CandidateName,CTE.IRFname,CTE.JDTiltle as PrimarySkill,
CTE.MobileNumber as Phone,CTE.ResumeLink,CTE.ClientHrContact as HREmail,CTE.dtCreatedOn as interviewDate,CTE.CalendlyLink,CTE.JobCountry,
CTE.ProfileSource,CTE.SchedulerUniqueid,CTE.EndDate,ct.StatusValue,ct.Date,CTE.CandidateAging, cte.IsAIInterview, cte.SessionId
From CTE 
outer Apply(Select top 1 cs.Date,csm.StatusValue From  CandidateStatus cs join CandidateStatusOptionsMst csm on csm.id=cs.Status
where CTE.IRFId=cs.IRFId and CTE.CandidateMasterId=cs.CandidateId order by cs.Date desc) ct  
        
end          
  else if(@clientRole = 1)              
begin 
;with CTE as (Select ca.id as CandidateId,IRF.id as imid,IRF.id as IRFId,ul.Emailid,IRF.IRFId as IRFname,(ISNULL(ul.FirstName,'')+' '+ISNULL(ul.LastName,'')) as Name,Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end as JDTiltle,ul.MobileNumber,ca.resumeLink,
ca.ClientHRAttemptContact as ClientHrContact,cs.Date as dtCreatedOn,IRF.CalendlyLink,IRF.JobCountry,ca.CandidateMasterId,
ca.ProfileSource,SUBSTRING( IRF.CalendlyLink , LEN(IRF.CalendlyLink) -  CHARINDEX('=',REVERSE(IRF.CalendlyLink)) + 2  , LEN(IRF.CalendlyLink)) as SchedulerUniqueid,
IRF.EndDate,DATEDIFF(Day,cs.Date,Getdate()) as CandidateAging, irf.IsAIInterview, im.SessionId        
From IRF join 
Candidate ca on ca.IRFId=IRF.id
join CandidatesMaster cm on cm.id=ca.CandidateMasterId
join UserList ul on ul.id=cm.UserId
join TransClientUser tcu on tcu.IRFId=IRF.id                            
left join Userlist tcul on tcul.id=tcu.ClientUserId  
left join ColdCandidateInfos cci on ca.id=cci.FKCandidateId    
left join InterviewMeetings im on im.candidateEmail=ul.EmailId and im.IRFId=IRF.IRFId
left join CandidateStatus cs on cs.CandidateId=cm.id and cs.IRFId=ca.IRFId and cs.Status=12                              
where tcul.EmailId=@Emailid and ca.ClientHRAttemptContact=@Emailid and cs.Date>=@startDate and cs.Date<@endDate and ISNULL(cci.IsColdCandidate,0) !=1 and im.id is null)
Select CTE.CandidateId,CTE.CandidateMasterId,CTE.IRFId,CTE.EmailId as CandidateEmailId,CTE.Name as CandidateName,CTE.IRFname,CTE.JDTiltle as PrimarySkill,
CTE.MobileNumber as Phone,CTE.ResumeLink,CTE.ClientHrContact as HREmail,CTE.dtCreatedOn as interviewDate,CTE.CalendlyLink,CTE.JobCountry,
CTE.ProfileSource,CTE.SchedulerUniqueid,CTE.EndDate,ct.StatusValue,ct.Date,CTE.CandidateAging, cte.IsAIInterview, cte.SessionId From CTE 
outer Apply(Select top 1 cs.Date,csm.StatusValue From  CandidateStatus cs join CandidateStatusOptionsMst csm on csm.id=cs.Status
where CTE.IRFId=cs.IRFId and CTE.CandidateMasterId=cs.CandidateId order by cs.Date desc) ct
end       
  else if(@clientRole = 9)              
begin     
;with CTE as (Select ca.id as CandidateId,IRF.id as imid,IRF.id as IRFId,ul.Emailid,IRF.IRFId as IRFname,(ISNULL(ul.FirstName,'')+' '+ISNULL(ul.LastName,'')) as Name,Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end as JDTiltle,ul.MobileNumber,ca.resumeLink,
ca.ClientHRAttemptContact as ClientHrContact,cs.Date as dtCreatedOn,IRF.CalendlyLink,IRF.JobCountry,ca.CandidateMasterId,ca.ProfileSource,
SUBSTRING( IRF.CalendlyLink , LEN(IRF.CalendlyLink) -  CHARINDEX('=',REVERSE(IRF.CalendlyLink)) + 2  , LEN(IRF.CalendlyLink)) as SchedulerUniqueid,
IRF.EndDate,DATEDIFF(Day,cs.Date,Getdate()) as CandidateAging, irf.IsAIInterview, im.SessionId        
From IRF join 
Candidate ca on ca.IRFId=IRF.id
join CandidatesMaster cm on cm.id=ca.CandidateMasterId
join UserList ul on ul.id=cm.UserId
left join ColdCandidateInfos cci on ca.id=cci.FKCandidateId    
left join InterviewMeetings im on im.candidateEmail=ul.EmailId and im.IRFId=IRF.IRFId
left join CandidateStatus cs on cs.CandidateId=cm.id and cs.IRFId=ca.IRFId and cs.Status=12                              
where cs.Date>=@startDate and cs.Date<@endDate and ISNULL(cci.IsColdCandidate,0) !=1 and IRF.ClientId=@Clientid and im.id is null)
Select CTE.CandidateId,CTE.CandidateMasterId,CTE.IRFId,CTE.EmailId as CandidateEmailId,CTE.Name as CandidateName,CTE.IRFname,CTE.JDTiltle as PrimarySkill,
CTE.MobileNumber as Phone,CTE.ResumeLink,CTE.ClientHrContact as HREmail,CTE.dtCreatedOn as interviewDate,CTE.CalendlyLink,CTE.JobCountry,
CTE.ProfileSource,CTE.SchedulerUniqueid,CTE.EndDate,ct.StatusValue,ct.Date,CTE.CandidateAging, cte.IsAIInterview, cte.SessionId From CTE 
outer Apply(Select top 1 cs.Date,csm.StatusValue From  CandidateStatus cs join CandidateStatusOptionsMst csm on csm.id=cs.Status
where CTE.IRFId=cs.IRFId and CTE.CandidateMasterId=cs.CandidateId order by cs.Date desc) ct    
        
end       
else          
begin      
;with CTE as (Select ca.id as CandidateId,IRF.id as imid,IRF.id as IRFId,ul.Emailid,IRF.IRFId as IRFname,(ISNULL(ul.FirstName,'')+' '+ISNULL(ul.LastName,'')) as Name,Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end as JDTiltle,ul.MobileNumber,ca.resumeLink,
ca.ClientHRAttemptContact as ClientHrContact,cs.Date as dtCreatedOn,IRF.CalendlyLink,IRF.JobCountry,ca.CandidateMasterId,ca.ProfileSource,
SUBSTRING( IRF.CalendlyLink , LEN(IRF.CalendlyLink) -  CHARINDEX('=',REVERSE(IRF.CalendlyLink)) + 2  , LEN(IRF.CalendlyLink)) as SchedulerUniqueid,
IRF.EndDate,DATEDIFF(Day,cs.Date,Getdate()) as CandidateAging, irf.IsAIInterview, im.SessionId        
From IRF join 
Candidate ca on ca.IRFId=IRF.id
join CandidatesMaster cm on cm.id=ca.CandidateMasterId
join UserList ul on ul.id=cm.UserId
join TransClientUser tcu on tcu.IRFId=IRF.id                            
 left join Userlist tcul on tcul.id=tcu.ClientUserId                   
left join ColdCandidateInfos cci on ca.id=cci.FKCandidateId    
left join InterviewMeetings im on im.candidateEmail=ul.EmailId and im.IRFId=IRF.IRFId
left join CandidateStatus cs on cs.CandidateId=cm.id and cs.IRFId=ca.IRFId and cs.Status=12                              
where tcul.EmailId=@Emailid and cs.Date>=@startDate and cs.Date<@endDate and ISNULL(cci.IsColdCandidate,0) !=1 and im.id is null)
Select CTE.CandidateId,CTE.CandidateMasterId,CTE.IRFId,CTE.EmailId as CandidateEmailId,CTE.Name as CandidateName,CTE.IRFname,CTE.JDTiltle as PrimarySkill,
CTE.MobileNumber as Phone,CTE.ResumeLink,CTE.ClientHrContact as HREmail,CTE.dtCreatedOn as interviewDate,CTE.CalendlyLink,CTE.JobCountry,
CTE.ProfileSource,CTE.SchedulerUniqueid,CTE.EndDate,ct.StatusValue,ct.Date,CTE.CandidateAging, cte.SessionId, cte.IsAIInterview From CTE 
outer Apply(Select top 1 cs.Date,csm.StatusValue From  CandidateStatus cs join CandidateStatusOptionsMst csm on csm.id=cs.Status
where CTE.IRFId=cs.IRFId and CTE.CandidateMasterId=cs.CandidateId order by cs.Date desc) ct     
end          
End                        
Else                        
BEGIN                        
if @EmailId is not null                                  
 Begin                                  
  set @Clientid= (Select cu.ClientId from UserList ul join ClientUser cu on cu.UserId=ul.id where ul.EmailId=@Emailid)                                      
  set @profileId = (select id from [Profile] where [Name]='Client')                                  
  set @userId  = (select id from UserList where EmailId=@EmailId and idProfile=@profileId)                                  
  set @roleId  = (select RoleId from ClientUser where UserId=@userId)                                  
                                
  if(Isnull(@userId,0)=0)                              
  Begin                              
  set @profileId = (select id from [Profile] where [Name]='SeniorHR')                                 
  set @userId  = (select id from UserList where EmailId=@EmailId and idProfile=@profileId)                               
                                
  if(Isnull(@userId,0)>0)                              
  begin   
  ;with CTE as(Select Distinct ca.id as CandidateId, ca.CandidateMasterId, ul.EmailId as CandidateEmailId,                                
    case when ul.LastName is null then ul.FirstName else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName)) end as CandidateName,                                
    ca.ClientHRAttemptContact as HREmail, ca.ProfileSource,IRF.id as IRFId,                                 
    irf.IRFId as IRFname, MAX(im.interviewDate) interviewDate,                              
    Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end as PrimarySkill,      
 ul.MobileNumber as Phone,IRF.id as imid,IRF.CalendlyLink,Count(im.id) as reportcount,cci.IsColdCandidate, irf.IsAIInterview
from InterviewMeetings im join IRF on IRF.IRFId=im.IRFId          
join UserList ul on ul.EmailId=im.candidateEmail           
join CandidatesMaster cm on cm.UserId=ul.id          
join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id    
join SeniorHr sh on sh.Clientid=IRF.ClientId                              
left join Userlist ulsh on ulsh.id=sh.Userid          
left join ColdCandidateInfos cci on ca.id=cci.FKCandidateId  
where ulsh.EmailId=@EmailId and irf.IsDeleted=0 and ISNULL(im.IsDeleted,0)=0                           
and im.interviewDate>=@startDate and im.interviewDate<=@endDate and ISNULL(cci.IsColdCandidate,0) !=1       
group by ca.id, ca.CandidateMasterId, ul.EmailId,                                
    case when ul.LastName is null then ul.FirstName else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName)) end,                                
    ca.ClientHRAttemptContact, ca.ProfileSource,IRF.id,  irf.IRFId,
 Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end,ul.MobileNumber,      
 IRF.id,IRF.IRFId,IRF.CalendlyLink,cci.IsColdCandidate, irf.IsAIInterview)
 Select CTE.*,im.Interviewer,im.RedoInterview,ic.InterviewerComment,ic.VideoLink,im.zoomLink
,dbo.fn_HasEmailDroppedOrBounced(CTE.CandidateEmailId) as HasEmailDroppedOrBounced,
ic.InterviewStatus as CurrentInterviewStatus,im.id as InterviewId,tqr.IsReportBlocked,im.RedoInterview,ISNull(psd.isQcDone,0) as IsQCDone,qcfr.QCStatus,
ic.MeetingStatus as CurrentMeetingStatus, im.confirmationStatus,
SUBSTRING(CTE.CalendlyLink , LEN(CTE.CalendlyLink) -  CHARINDEX('=',REVERSE(CTE.CalendlyLink)) + 2  , LEN(CTE.CalendlyLink)) as SchedulerUniqueid,
DATEDIFF(Day,CandidateAddStatus.Date,Getdate()) as CandidateAging, im.SessionId
From CTE 
 join InterviewMeetings im on im.interviewDate=CTE.interviewDate and im.candidateEmail=CTE.CandidateEmailId and im.IRFId=CTE.IRFname
 join InterviewComments ic on ic.Interviewid=im.id  
 left join Trans_QCFormReport tqr on tqr.interviewid=im.id 
 left join PaymentSLADetails psd on psd.Interviewid=im.id
 left join QCFeedbackReport qcfr on qcfr.InterviewId = im.id
 outer apply(select top 1 cs.Date From CandidateStatus cs where cs.IRFId=CTE.IRFId and CandidateId = CTE.CandidateMasterId and cs.Status=12 order by cs.Date desc) as CandidateAddStatus    
 order by im.interviewDate desc
  End                              
 End                                  
                               
  else if(@roleId=1)                                  
   Begin      
   ;with CTE as(Select Distinct ca.id as CandidateId, ca.CandidateMasterId, ul.EmailId as CandidateEmailId,                                
    case when ul.LastName is null then ul.FirstName else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName)) end as CandidateName,                                
    ca.ClientHRAttemptContact as HREmail, ca.ProfileSource,IRF.id as IRFId,                                 
    irf.IRFId as IRFname, MAX(im.interviewDate) interviewDate,                              
    Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end as PrimarySkill,      
 ul.MobileNumber as Phone,IRF.id as imid,IRF.CalendlyLink,Count(im.id) as reportcount,cci.IsColdCandidate, irf.IsAIInterview
from InterviewMeetings im join IRF on IRF.IRFId=im.IRFId          
join UserList ul on ul.EmailId=im.candidateEmail           
join CandidatesMaster cm on cm.UserId=ul.id          
join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id          
inner join TransClientUser tcu on IRF.id=tcu.IRFId                                
left join UserList ulc on tcu.ClientUserId = ulc.id           
left join ColdCandidateInfos cci on ca.id=cci.FKCandidateId    
where ulc.EmailId = @EmailId AND ulc.IsActive=1 and irf.IsDeleted=0 and ISNULL(im.IsDeleted,0)=0 and ca.ClientHRAttemptContact=@EmailId                               
and im.interviewDate>=@startDate and im.interviewDate<=@endDate and ISNULL(cci.IsColdCandidate,0) !=1       
group by ca.id, ca.CandidateMasterId, ul.EmailId,                                
    case when ul.LastName is null then ul.FirstName else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName)) end,                                
    ca.ClientHRAttemptContact, ca.ProfileSource,IRF.id,  irf.IRFId,
 Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end,ul.MobileNumber,      
 IRF.id,IRF.IRFId,IRF.CalendlyLink,cci.IsColdCandidate, irf.IsAIInterview)
 Select CTE.*,im.Interviewer,im.RedoInterview,ic.InterviewerComment,ic.VideoLink,im.zoomLink
,dbo.fn_HasEmailDroppedOrBounced(CTE.CandidateEmailId) as HasEmailDroppedOrBounced,
ic.InterviewStatus as CurrentInterviewStatus,im.id as InterviewId,tqr.IsReportBlocked,im.RedoInterview,ISNull(psd.isQcDone,0) as IsQCDone,qcfr.QCStatus,
ic.MeetingStatus as CurrentMeetingStatus, im.confirmationStatus,
SUBSTRING(CTE.CalendlyLink , LEN(CTE.CalendlyLink) -  CHARINDEX('=',REVERSE(CTE.CalendlyLink)) + 2  , LEN(CTE.CalendlyLink)) as SchedulerUniqueid,
DATEDIFF(Day,CandidateAddStatus.Date,Getdate()) as CandidateAging, im.SessionId
From CTE 
 join InterviewMeetings im on im.interviewDate=CTE.interviewDate and im.candidateEmail=CTE.CandidateEmailId and im.IRFId=CTE.IRFname
 join InterviewComments ic on ic.Interviewid=im.id  
 left join Trans_QCFormReport tqr on tqr.interviewid=im.id 
 left join PaymentSLADetails psd on psd.Interviewid=im.id
 left join QCFeedbackReport qcfr on qcfr.InterviewId = im.id
 outer apply(select top 1 cs.Date  From CandidateStatus cs where cs.IRFId=CTE.IRFId and CandidateId = CTE.CandidateMasterId and cs.Status=12 order by cs.Date desc) as CandidateAddStatus    
 order by im.interviewDate desc
        
   End                                 
  else if(@roleId=4)                                
  Begin        
  ;with CTE as(Select Distinct ca.id as CandidateId, ca.CandidateMasterId, ul.EmailId as CandidateEmailId,                                
    case when ul.LastName is null then ul.FirstName else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName)) end as CandidateName,                                
    ca.ClientHRAttemptContact as HREmail, ca.ProfileSource,IRF.id as IRFId,                                 
    irf.IRFId as IRFname, MAX(im.interviewDate) interviewDate,                              
    Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end as PrimarySkill,      
 ul.MobileNumber as Phone,IRF.id as imid,IRF.CalendlyLink,Count(im.id) as reportcount,cci.IsColdCandidate, irf.IsAIInterview
from InterviewMeetings im join IRF on IRF.IRFId=im.IRFId          
join UserList ul on ul.EmailId=im.candidateEmail           
join CandidatesMaster cm on cm.UserId=ul.id          
join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id   
inner join TransClientUser tcu on IRF.id=tcu.IRFId                                
left join UserList ulc on tcu.ClientUserId = ulc.id           
left join ColdCandidateInfos cci on ca.id=cci.FKCandidateId    
where ulc.EmailId = @EmailId AND ulc.IsActive=1 and irf.IsDeleted=0 and ISNULL(im.IsDeleted,0)=0   and (ca.ProfileSource=@EmailId or im.staffingAgency= @EmailId) 
and im.interviewDate>=@startDate and im.interviewDate<=@endDate and ISNULL(cci.IsColdCandidate,0) !=1       
group by ca.id, ca.CandidateMasterId, ul.EmailId,                                
    case when ul.LastName is null then ul.FirstName else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName)) end,                                
    ca.ClientHRAttemptContact, ca.ProfileSource,IRF.id,  irf.IRFId,
 Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end,ul.MobileNumber,      
 IRF.id,IRF.IRFId,IRF.CalendlyLink,cci.IsColdCandidate, irf.IsAIInterview)
 Select CTE.*,im.Interviewer,im.RedoInterview,ic.InterviewerComment,ic.VideoLink,im.zoomLink
,dbo.fn_HasEmailDroppedOrBounced(CTE.CandidateEmailId) as HasEmailDroppedOrBounced,
ic.InterviewStatus as CurrentInterviewStatus,im.id as InterviewId,tqr.IsReportBlocked,im.RedoInterview,ISNull(psd.isQcDone,0) as IsQCDone,qcfr.QCStatus,
ic.MeetingStatus as CurrentMeetingStatus, im.confirmationStatus,
SUBSTRING(CTE.CalendlyLink , LEN(CTE.CalendlyLink) -  CHARINDEX('=',REVERSE(CTE.CalendlyLink)) + 2  , LEN(CTE.CalendlyLink)) as SchedulerUniqueid,
DATEDIFF(Day,CandidateAddStatus.Date,Getdate()) as CandidateAging, im.SessionId
From CTE 
 join InterviewMeetings im on im.interviewDate=CTE.interviewDate and im.candidateEmail=CTE.CandidateEmailId and im.IRFId=CTE.IRFname
 join InterviewComments ic on ic.Interviewid=im.id  
 left join Trans_QCFormReport tqr on tqr.interviewid=im.id 
 left join PaymentSLADetails psd on psd.Interviewid=im.id
 left join QCFeedbackReport qcfr on qcfr.InterviewId = im.id
 outer apply(select top 1 cs.Date From CandidateStatus cs where cs.IRFId=CTE.IRFId and CandidateId = CTE.CandidateMasterId and cs.Status=12 order by cs.Date desc) as CandidateAddStatus    
 order by im.interviewDate desc
                      
  End          
   else if(@roleId=9)                                
  Begin 
  ;with CTE as(Select Distinct ca.id as CandidateId, ca.CandidateMasterId, ul.EmailId as CandidateEmailId,                                
    case when ul.LastName is null then ul.FirstName else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName)) end as CandidateName,                                
    ca.ClientHRAttemptContact as HREmail, ca.ProfileSource,IRF.id as IRFId,                                 
    irf.IRFId as IRFname, MAX(im.interviewDate) interviewDate,                              
    Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end as PrimarySkill,      
 ul.MobileNumber as Phone,IRF.id as imid,IRF.CalendlyLink,Count(im.id) as reportcount,cci.IsColdCandidate, irf.IsAIInterview
from InterviewMeetings im join IRF on IRF.IRFId=im.IRFId          
join UserList ul on ul.EmailId=im.candidateEmail           
join CandidatesMaster cm on cm.UserId=ul.id          
join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id          
left join ColdCandidateInfos cci on ca.id=cci.FKCandidateId    
where irf.IsDeleted=0 and ISNULL(im.IsDeleted,0)=0  and irf.Clientid=@Clientid                            
and im.interviewDate>=@startDate and im.interviewDate<=@endDate and ISNULL(cci.IsColdCandidate,0) !=1       
group by ca.id, ca.CandidateMasterId, ul.EmailId,                                
    case when ul.LastName is null then ul.FirstName else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName)) end,                                
    ca.ClientHRAttemptContact, ca.ProfileSource,IRF.id,  irf.IRFId,
 Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end,ul.MobileNumber,      
 IRF.id,IRF.IRFId,IRF.CalendlyLink,cci.IsColdCandidate, irf.IsAIInterview)
 Select CTE.*,im.Interviewer,im.RedoInterview,ic.InterviewerComment,ic.VideoLink,im.zoomLink
,dbo.fn_HasEmailDroppedOrBounced(CTE.CandidateEmailId) as HasEmailDroppedOrBounced,
ic.InterviewStatus as CurrentInterviewStatus,im.id as InterviewId,tqr.IsReportBlocked,im.RedoInterview,ISNull(psd.isQcDone,0) as IsQCDone,qcfr.QCStatus,
ic.MeetingStatus as CurrentMeetingStatus, im.confirmationStatus,
SUBSTRING(CTE.CalendlyLink , LEN(CTE.CalendlyLink) -  CHARINDEX('=',REVERSE(CTE.CalendlyLink)) + 2  , LEN(CTE.CalendlyLink)) as SchedulerUniqueid,
DATEDIFF(Day,CandidateAddStatus.Date,Getdate()) as CandidateAging, im.SessionId
From CTE 
 join InterviewMeetings im on im.interviewDate=CTE.interviewDate and im.candidateEmail=CTE.CandidateEmailId and im.IRFId=CTE.IRFname
 join InterviewComments ic on ic.Interviewid=im.id  
 left join Trans_QCFormReport tqr on tqr.interviewid=im.id 
 left join PaymentSLADetails psd on psd.Interviewid=im.id
 left join QCFeedbackReport qcfr on qcfr.InterviewId = im.id
 outer apply(select top 1 cs.Date  From CandidateStatus cs where cs.IRFId=CTE.IRFId and CandidateId = CTE.CandidateMasterId and cs.Status=12 order by cs.Date desc) as CandidateAddStatus    
 order by im.interviewDate desc
                           
  End              
  else                                  
   Begin                                  
   ;with CTE as(Select Distinct ca.id as CandidateId, ca.CandidateMasterId, ul.EmailId as CandidateEmailId,                                
    case when ul.LastName is null then ul.FirstName else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName)) end as CandidateName,                                
    ca.ClientHRAttemptContact as HREmail, ca.ProfileSource,IRF.id as IRFId,                                 
    irf.IRFId as IRFname, MAX(im.interviewDate) interviewDate,                              
    Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end as PrimarySkill,      
 ul.MobileNumber as Phone,IRF.id as imid,IRF.CalendlyLink,Count(im.id) as reportcount,cci.IsColdCandidate, irf.IsAIInterview
from InterviewMeetings im join IRF on IRF.IRFId=im.IRFId          
join UserList ul on ul.EmailId=im.candidateEmail           
join CandidatesMaster cm on cm.UserId=ul.id          
join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id 
inner join TransClientUser tcu on IRF.id=tcu.IRFId                                
left join UserList ulc on tcu.ClientUserId = ulc.id   
left join ColdCandidateInfos cci on ca.id=cci.FKCandidateId    
where ulc.EmailId = @EmailId AND ulc.IsActive=1 and irf.IsDeleted=0 and ISNULL(im.IsDeleted,0)=0
and im.interviewDate>=@startDate and im.interviewDate<=@endDate and ISNULL(cci.IsColdCandidate,0) !=1       
group by ca.id, ca.CandidateMasterId, ul.EmailId,                                
    case when ul.LastName is null then ul.FirstName else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName)) end,                                
    ca.ClientHRAttemptContact, ca.ProfileSource,IRF.id,  irf.IRFId,
 Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end,ul.MobileNumber,      
 IRF.id,IRF.IRFId,IRF.CalendlyLink,cci.IsColdCandidate, irf.IsAIInterview)
 Select CTE.*,im.Interviewer,im.RedoInterview,ic.InterviewerComment,ic.VideoLink,im.zoomLink
,dbo.fn_HasEmailDroppedOrBounced(CTE.CandidateEmailId) as HasEmailDroppedOrBounced,
ic.InterviewStatus as CurrentInterviewStatus,im.id as InterviewId,tqr.IsReportBlocked,im.RedoInterview,ISNull(psd.isQcDone,0) as IsQCDone,
ic.MeetingStatus as CurrentMeetingStatus, im.confirmationStatus,
SUBSTRING(CTE.CalendlyLink , LEN(CTE.CalendlyLink) -  CHARINDEX('=',REVERSE(CTE.CalendlyLink)) + 2  , LEN(CTE.CalendlyLink)) as SchedulerUniqueid,
DATEDIFF(Day,CandidateAddStatus.Date,Getdate()) as CandidateAging, im.SessionId
From CTE 
 join InterviewMeetings im on im.interviewDate=CTE.interviewDate and im.candidateEmail=CTE.CandidateEmailId and im.IRFId=CTE.IRFname
 join InterviewComments ic on ic.Interviewid=im.id  
 left join Trans_QCFormReport tqr on tqr.interviewid=im.id
 left join PaymentSLADetails psd on psd.Interviewid=im.id
 outer apply(select top 1 cs.Date  From CandidateStatus cs where cs.IRFId=CTE.IRFId and CandidateId = CTE.CandidateMasterId and cs.Status=12 order by cs.Date desc) as CandidateAddStatus    
 order by im.interviewDate desc
   End                                  
 End                                  
Else                                  
 Begin  
 ;with CTE as(Select Distinct ca.id as CandidateId, ca.CandidateMasterId, ul.EmailId as CandidateEmailId,                                
    case when ul.LastName is null then ul.FirstName else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName)) end as CandidateName,                                
    ca.ClientHRAttemptContact as HREmail, ca.ProfileSource,IRF.id as IRFId,                                 
    irf.IRFId as IRFname, MAX(im.interviewDate) interviewDate,                              
    Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end as PrimarySkill,      
 ul.MobileNumber as Phone,IRF.id as imid,IRF.CalendlyLink,Count(im.id) as reportcount,cci.IsColdCandidate, irf.IsAIInterview
from InterviewMeetings im join IRF on IRF.IRFId=im.IRFId          
join UserList ul on ul.EmailId=im.candidateEmail           
join CandidatesMaster cm on cm.UserId=ul.id          
join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id          
left join ColdCandidateInfos cci on ca.id=cci.FKCandidateId    
where irf.IsDeleted=0 and ISNULL(im.IsDeleted,0)=0                          
and im.interviewDate>=@startDate and im.interviewDate<=@endDate and ISNULL(cci.IsColdCandidate,0) !=1       
group by ca.id, ca.CandidateMasterId, ul.EmailId,                                
    case when ul.LastName is null then ul.FirstName else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName)) end,                                
    ca.ClientHRAttemptContact, ca.ProfileSource,IRF.id,  irf.IRFId,
 Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end,ul.MobileNumber,      
 IRF.id,IRF.IRFId,IRF.CalendlyLink,cci.IsColdCandidate, irf.IsAIInterview)
 Select CTE.*,im.Interviewer,im.RedoInterview,ic.InterviewerComment,ic.VideoLink,im.zoomLink
,dbo.fn_HasEmailDroppedOrBounced(CTE.CandidateEmailId) as HasEmailDroppedOrBounced,
ic.InterviewStatus as CurrentInterviewStatus,im.id as InterviewId,tqr.IsReportBlocked,im.RedoInterview,ISNull(psd.isQcDone,0) as IsQCDone,qcfr.QCStatus,
ic.MeetingStatus as CurrentMeetingStatus, im.confirmationStatus,
SUBSTRING(CTE.CalendlyLink , LEN(CTE.CalendlyLink) -  CHARINDEX('=',REVERSE(CTE.CalendlyLink)) + 2  , LEN(CTE.CalendlyLink)) as SchedulerUniqueid,
DATEDIFF(Day,CandidateAddStatus.Date,Getdate()) as CandidateAging, im.SessionId
From CTE 
 join InterviewMeetings im on im.interviewDate=CTE.interviewDate and im.candidateEmail=CTE.CandidateEmailId and im.IRFId=CTE.IRFname
 join InterviewComments ic on ic.Interviewid=im.id  
 left join Trans_QCFormReport tqr on tqr.interviewid=im.id 
 left join PaymentSLADetails psd on psd.Interviewid=im.id
 left join QCFeedbackReport qcfr on qcfr.InterviewId = im.id
 outer apply(select top 1 cs.Date  From CandidateStatus cs where cs.IRFId=CTE.IRFId and CandidateId = CTE.CandidateMasterId and cs.Status=12 order by cs.Date desc) as CandidateAddStatus    
 order by im.interviewDate desc
 
 End                         
END                        
End 
Go