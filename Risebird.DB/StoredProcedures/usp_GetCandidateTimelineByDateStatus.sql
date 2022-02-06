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
declare  @addedStatusId int            
declare @CandidateStatusGrouped table (IRFId int, CandidateId int,Interviewid int)             
declare  @meetingStatusId int, @interviewStatusId int, @qcPendingId int;           
declare @allStatustable table (CandidateMasterId int, IRFId int, [Date] datetime, StatusValue varchar(50), Comments varchar(max))                    
declare @meetingStatusTable table (CandidateMasterId int, IRFId int, [Date] datetime, Comments varchar(max))          
declare @interviewStatusTable table (CandidateMasterId int, IRFId int, [Date] datetime, Comments varchar(max))                    
declare @profileId int, @roleId int, @userId int=0;            
if(@datatype=1)          
Begin          
 set @addedStatusId = (select id from CandidateStatusOptionsMst where StatusValue='Added')                    
                    
    insert into @CandidateStatusGrouped                     
    select IRFId, CandidateId,0 as Interviewid from CandidateStatus where [Status]=@addedStatusId and cast([Date] as date) <= cast (@endDate as date) and cast([Date] as date) >= cast (@startDate as date)                    
                    
 set @meetingStatusId = (select id from CandidateStatusOptionsMst where StatusValue='Meeting Status')                    
 set @interviewStatusId = (select id from CandidateStatusOptionsMst where StatusValue='Interview Status')                    
 set @qcPendingId = (select id from CandidateStatusOptionsMst where StatusValue='QC Pending')--19                   
                   
    insert into @allStatustable                     
    select cs.CandidateId, cs.IRFId, cs.[Date], csom.StatusValue, cs.Comments from CandidateStatus cs                   
 join CandidateStatusOptionsMst csom  on cs.[Status] = csom.id                   
 join @CandidateStatusGrouped csg on (csg.CandidateId=cs.CandidateId and csg.IRFId=cs.IRFId)                  
                  
                    
    insert into @meetingStatusTable                     
    select cs.CandidateId, cs.IRFId, [Date], Comments from CandidateStatus cs join @CandidateStatusGrouped csg on (csg.CandidateId=cs.CandidateId and csg.IRFId=cs.IRFId) where [Status]=@meetingStatusId                     
                    
    insert into @interviewStatusTable                     
    select cs.CandidateId, cs.IRFId, [Date], Comments from CandidateStatus cs join @CandidateStatusGrouped csg on (csg.CandidateId=cs.CandidateId and csg.IRFId=cs.IRFId)                   
 where ([Status]=@interviewStatusId  or [Status]= @qcPendingId)                  
                    
                     
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
  select ca.id as CandidateId, ca.CandidateMasterId, ul.EmailId,                    
  case                     
   when ul.LastName is null then ul.FirstName                    
   else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName))                    
  end as [Name],                    
  ca.ClientHRAttemptContact as HREmail, ca.IRFId, ca.ProfileSource, im.id as InterviewId,                    
  ast.[Date] as ScheduleStatusDate, ast.Comments as ScheduleStatusComments, ast.[StatusValue] as ScheduleStatusValue,       
  mst.Comments as MeetingStatus, mst.[Date] as MeetingStatusDate, ist.Comments as InterviewStatus, ist.[Date] as InterviewStatusDate,                    
  ic.InterviewStatus as CurrentInterviewStatus, qcfr.QCStatus,                    
  ic.MeetingStatus as CurrentMeetingStatus, im.confirmationStatus, irf.IRFId as IRFName, ca.Phone,  im.interviewDate,                  
  (CASE WHEN CHARINDEX('|', JDTiltle) > 0 THEN SUBSTRING(JDTiltle, 1, CHARINDEX('|', JDTiltle) -1) ELSE JDTiltle END) as PrimarySkill,ul.MobileNumber,im.interviewDate,IRF.id as imid,IRF.IRFId as IRFname,IRF.CalendlyLink                    
  from Candidate ca                     
  join CandidatesMaster cm on cm.id =ca.CandidateMasterId                    
  join UserList ul on ul.id =cm.UserId                    
  join IRF on IRF.id=ca.IRFId                    
  join SeniorHr sh on sh.Clientid=IRF.ClientId                
  left join Userlist ulsh on ulsh.id=sh.Userid                
  left join InterviewMeetings im on im.candidateEmail=ul.EmailId and im.IRFId =IRF.IRFId                    
  left join InterviewComments ic on ic.InterviewId = im.id                    
  left join QCFeedbackReport qcfr on qcfr.InterviewId = im.id    
  join @allStatustable ast on ca.CandidateMasterId = ast.CandidateMasterId and ca.IRFId=ast.IRFId                     
  left join @meetingStatusTable mst on ca.CandidateMasterId= mst.CandidateMasterId and ca.IRFId= mst.IRFId                    
  left join @interviewStatusTable ist on ca.CandidateMasterId= ist.CandidateMasterId and ca.IRFId= ist.IRFId                   
  where ulsh.EmailId=@EmailId                
  End                
 End                    
                 
  else if(@roleId=1)                    
   Begin                    
    select ca.id as CandidateId, ca.CandidateMasterId, ul.EmailId,                   
 case                     
     when ul.LastName is null then ul.FirstName                    
     else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName))                    
    end as [Name],                   
 ca.ClientHRAttemptContact as HREmail, ca.IRFId, ca.ProfileSource, im.id as InterviewId,                    
    ast.[Date] as ScheduleStatusDate, ast.Comments as ScheduleStatusComments, ast.[StatusValue] as ScheduleStatusValue,                    
    mst.Comments as MeetingStatus, mst.[Date] as MeetingStatusDate, ist.Comments as InterviewStatus, ist.[Date] as InterviewStatusDate,                    
    ic.InterviewStatus as CurrentInterviewStatus, qcfr.QCStatus,                    
    ic.MeetingStatus as CurrentMeetingStatus, im.confirmationStatus, irf.IRFId as IRFName, ca.Phone, im.interviewDate,                   
    (CASE WHEN CHARINDEX('|', JDTiltle) > 0 THEN SUBSTRING(JDTiltle, 1, CHARINDEX('|', JDTiltle) -1) ELSE JDTiltle END) as PrimarySkill,ul.MobileNumber,im.interviewDate,IRF.id as imid,IRF.IRFId as IRFname,IRF.CalendlyLink                    
    from Candidate ca                     
    join CandidatesMaster cm on cm.id =ca.CandidateMasterId                    
    join UserList ul on ul.id =cm.UserId                    
    join IRF on IRF.id=ca.IRFId                    
    inner join TransClientUser tcu on IRF.id=tcu.IRFId                    
    left join UserList ulc on tcu.ClientUserId = ulc.id                    
    left join InterviewMeetings im on im.candidateEmail=ul.EmailId and im.IRFId =IRF.IRFId                    
    left join InterviewComments ic on ic.InterviewId = im.id                    
    left join QCFeedbackReport qcfr on qcfr.InterviewId = im.id  
    join @allStatustable ast on ca.CandidateMasterId = ast.CandidateMasterId and ca.IRFId=ast.IRFId                     
    left join @meetingStatusTable mst on ca.CandidateMasterId= mst.CandidateMasterId and ca.IRFId= mst.IRFId                    
    left join @interviewStatusTable ist on ca.CandidateMasterId= ist.CandidateMasterId and ca.IRFId= ist.IRFId                    
    where ulc.EmailId LIKE @EmailId AND ulc.IsActive=1 and ca.ClientHRAttemptContact=@EmailId                      and irf.IsDeleted=0                    
   End                   
  else if(@roleId=4)                  
  Begin                  
      select ca.id as CandidateId, ca.CandidateMasterId, ul.EmailId,                   
 case                     
   when ul.LastName is null then ul.FirstName                    
     else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName))                    
    end as [Name],                   
 ca.ClientHRAttemptContact as HREmail, ca.IRFId, ca.ProfileSource, im.id as InterviewId,                    
    ast.[Date] as ScheduleStatusDate, ast.Comments as ScheduleStatusComments, ast.[StatusValue] as ScheduleStatusValue,                    
    mst.Comments as MeetingStatus, mst.[Date] as MeetingStatusDate, ist.Comments as InterviewStatus, ist.[Date] as InterviewStatusDate,                    
    ic.InterviewStatus as CurrentInterviewStatus, qcfr.QCStatus,                    
    ic.MeetingStatus as CurrentMeetingStatus, im.confirmationStatus, irf.IRFId as IRFName, ca.Phone, im.interviewDate,                   
    (CASE WHEN CHARINDEX('|', JDTiltle) > 0 THEN SUBSTRING(JDTiltle, 1, CHARINDEX('|', JDTiltle) -1) ELSE JDTiltle END) as PrimarySkill,ul.MobileNumber,im.interviewDate,IRF.id as imid,IRF.IRFId as IRFname,IRF.CalendlyLink
    from Candidate ca                     
    join CandidatesMaster cm on cm.id =ca.CandidateMasterId                    
    join UserList ul on ul.id =cm.UserId                    
    join IRF on IRF.id=ca.IRFId                    
    inner join TransClientUser tcu on IRF.id=tcu.IRFId                    
    left join UserList ulc on tcu.ClientUserId = ulc.id                    
    left join InterviewMeetings im on im.candidateEmail=ul.EmailId and im.IRFId =IRF.IRFId                    
    left join InterviewComments ic on ic.InterviewId = im.id                    
    left join QCFeedbackReport qcfr on qcfr.InterviewId = im.id   
    join @allStatustable ast on ca.CandidateMasterId = ast.CandidateMasterId and ca.IRFId=ast.IRFId                     
    left join @meetingStatusTable mst on ca.CandidateMasterId= mst.CandidateMasterId and ca.IRFId= mst.IRFId                    
    left join @interviewStatusTable ist on ca.CandidateMasterId= ist.CandidateMasterId and ca.IRFId= ist.IRFId                    
    where ulc.EmailId LIKE @EmailId AND ulc.IsActive=1 and (ca.ProfileSource=@EmailId  or im.staffingAgency=@EmailId)                  
    and irf.IsDeleted=0                    
                  
  End                  
  else                    
   Begin                    
    select ca.id as CandidateId, ca.CandidateMasterId, ul.EmailId,                    
    case                     
     when ul.LastName is null then ul.FirstName                    
     else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName))                    
    end as [Name],                    
    ca.ClientHRAttemptContact as HREmail, ca.IRFId, ca.ProfileSource, im.id as InterviewId,                    
    ast.[Date] as ScheduleStatusDate, ast.Comments as ScheduleStatusComments, ast.[StatusValue] as ScheduleStatusValue,                    
    mst.Comments as MeetingStatus, mst.[Date] as MeetingStatusDate, ist.Comments as InterviewStatus, ist.[Date] as InterviewStatusDate,                    
    ic.InterviewStatus as CurrentInterviewStatus, qcfr.QCStatus,                    
    ic.MeetingStatus as CurrentMeetingStatus, im.confirmationStatus, irf.IRFId as IRFName, ca.Phone,  im.interviewDate,                  
    (CASE WHEN CHARINDEX('|', JDTiltle) > 0 THEN SUBSTRING(JDTiltle, 1, CHARINDEX('|', JDTiltle) -1) ELSE JDTiltle END) as PrimarySkill,ul.MobileNumber,im.interviewDate,IRF.id as imid,IRF.IRFId as IRFname,IRF.CalendlyLink
    from Candidate ca                     
    join CandidatesMaster cm on cm.id =ca.CandidateMasterId                    
    join UserList ul on ul.id =cm.UserId                    
    join IRF on IRF.id=ca.IRFId                    
    inner join TransClientUser tcu on IRF.id=tcu.IRFId                      left join UserList ulc on tcu.ClientUserId = ulc.id                    
    left join InterviewMeetings im on im.candidateEmail=ul.EmailId and im.IRFId =IRF.IRFId                    
    left join InterviewComments ic on ic.InterviewId = im.id                    
    left join QCFeedbackReport qcfr on qcfr.InterviewId = im.id   
    join @allStatustable ast on ca.CandidateMasterId = ast.CandidateMasterId and ca.IRFId=ast.IRFId                     
    left join @meetingStatusTable mst on ca.CandidateMasterId= mst.CandidateMasterId and ca.IRFId= mst.IRFId                    
    left join @interviewStatusTable ist on ca.CandidateMasterId= ist.CandidateMasterId and ca.IRFId= ist.IRFId                    
    where ulc.EmailId LIKE @EmailId AND ulc.IsActive=1                    
    and irf.IsDeleted=0                    
   End                    
 End                    
Else                    
 Begin                    
  select ca.id as CandidateId, ca.CandidateMasterId, ul.EmailId,                    
  case                     
   when ul.LastName is null then ul.FirstName                    
 else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName))                    
  end as [Name],                    
  ca.ClientHRAttemptContact as HREmail, ca.IRFId, ca.ProfileSource, im.id as InterviewId,                    
  ast.[Date] as ScheduleStatusDate, ast.Comments as ScheduleStatusComments, ast.[StatusValue] as ScheduleStatusValue,                    
  mst.Comments as MeetingStatus, mst.[Date] as MeetingStatusDate, ist.Comments as InterviewStatus, ist.[Date] as InterviewStatusDate,                    
  ic.InterviewStatus as CurrentInterviewStatus, qcfr.QCStatus,                    
  ic.MeetingStatus as CurrentMeetingStatus, im.confirmationStatus, irf.IRFId as IRFName, ca.Phone,  im.interviewDate,                  
  (CASE WHEN CHARINDEX('|', JDTiltle) > 0 THEN SUBSTRING(JDTiltle, 1, CHARINDEX('|', JDTiltle) -1) ELSE JDTiltle END) as PrimarySkill,ul.MobileNumber,im.interviewDate,IRF.id as imid,IRF.IRFId as IRFname,IRF.CalendlyLink
  from Candidate ca                     
  join CandidatesMaster cm on cm.id =ca.CandidateMasterId                    
  join UserList ul on ul.id =cm.UserId                    
  join IRF on IRF.id=ca.IRFId        
  left join InterviewMeetings im on im.candidateEmail=ul.EmailId and im.IRFId =IRF.IRFId                    
  left join InterviewComments ic on ic.InterviewId = im.id                    
  left join QCFeedbackReport qcfr on qcfr.InterviewId = im.id  
  join @allStatustable ast on ca.CandidateMasterId = ast.CandidateMasterId and ca.IRFId=ast.IRFId                     
left join @meetingStatusTable mst on ca.CandidateMasterId= mst.CandidateMasterId and ca.IRFId= mst.IRFId                    
  left join @interviewStatusTable ist on ca.CandidateMasterId= ist.CandidateMasterId and ca.IRFId= ist.IRFId                    
 End                    
End          
Else          
BEGIN          
          
                    
 set @addedStatusId = (select id from CandidateStatusOptionsMst where StatusValue='Added')                    
                 
    insert into @CandidateStatusGrouped                     
    Select IRF.id as IRFId,cm.id as CandidateId,im.id as Interviewid from Interviewmeetings im join IRF on im.IRFID=IRF.IRFId join UserList ul on ul.EmailId=im.candidateEmail          
join CandidatesMaster cm on cm.UserId=ul.id where im.confirmationStatus=1 and cast(im.interviewDate as date) <= cast (@endDate as date) and cast(im.interviewDate as date) >= cast (@startDate as date)           
                    
 set @meetingStatusId = (select id from CandidateStatusOptionsMst where StatusValue='Meeting Status')                    
 set @interviewStatusId = (select id from CandidateStatusOptionsMst where StatusValue='Interview Status')                    
 set @qcPendingId = (select id from CandidateStatusOptionsMst where StatusValue='QC Pending')--19                   
           
    insert into @allStatustable                     
    select cs.CandidateId, cs.IRFId, cs.[Date], csom.StatusValue, cs.Comments from CandidateStatus cs                   
 join CandidateStatusOptionsMst csom  on cs.[Status] = csom.id                   
 join @CandidateStatusGrouped csg on (csg.CandidateId=cs.CandidateId and csg.IRFId=cs.IRFId)                  
                    
           
    insert into @meetingStatusTable                     
    select cs.CandidateId, cs.IRFId, [Date], Comments from CandidateStatus cs join @CandidateStatusGrouped csg on (csg.CandidateId=cs.CandidateId and csg.IRFId=cs.IRFId) where [Status]=@meetingStatusId                     
                    
           
    insert into @interviewStatusTable                     
    select cs.CandidateId, cs.IRFId, [Date], Comments from CandidateStatus cs join @CandidateStatusGrouped csg on (csg.CandidateId=cs.CandidateId and csg.IRFId=cs.IRFId)                   
 where ([Status]=@interviewStatusId  or [Status]= @qcPendingId)                  
                    
                     
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
  select ca.id as CandidateId, ca.CandidateMasterId, ul.EmailId,                    
  case                     
   when ul.LastName is null then ul.FirstName                    
   else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName))                    
  end as [Name],                    
  ca.ClientHRAttemptContact as HREmail, ca.IRFId, ca.ProfileSource, im.id as InterviewId,                    
  ast.[Date] as ScheduleStatusDate, ast.Comments as ScheduleStatusComments, ast.[StatusValue] as ScheduleStatusValue,                    
  mst.Comments as MeetingStatus, mst.[Date] as MeetingStatusDate, ist.Comments as InterviewStatus, ist.[Date] as InterviewStatusDate,                    
  ic.InterviewStatus as CurrentInterviewStatus, qcfr.QCStatus,                    
  ic.MeetingStatus as CurrentMeetingStatus, im.confirmationStatus, irf.IRFId as IRFName, ca.Phone,  im.interviewDate,                  
  (CASE WHEN CHARINDEX('|', JDTiltle) > 0 THEN SUBSTRING(JDTiltle, 1, CHARINDEX('|', JDTiltle) -1) ELSE JDTiltle END) as PrimarySkill,ul.MobileNumber,im.interviewDate,IRF.id as imid,IRF.IRFId as IRFname,IRF.CalendlyLink
  from Candidate ca                     
  join CandidatesMaster cm on cm.id =ca.CandidateMasterId                    
  join UserList ul on ul.id =cm.UserId                    
  join IRF on IRF.id=ca.IRFId                    
  join SeniorHr sh on sh.Clientid=IRF.ClientId                
  left join Userlist ulsh on ulsh.id=sh.Userid                
  left join InterviewMeetings im on im.candidateEmail=ul.EmailId and im.IRFId =IRF.IRFId                    
  left join InterviewComments ic on ic.InterviewId = im.id                    
  left join QCFeedbackReport qcfr on qcfr.InterviewId = im.id   
  join @allStatustable ast on ca.CandidateMasterId = ast.CandidateMasterId and ca.IRFId=ast.IRFId                     
  left join @meetingStatusTable mst on ca.CandidateMasterId= mst.CandidateMasterId and ca.IRFId= mst.IRFId                    
  left join @interviewStatusTable ist on ca.CandidateMasterId= ist.CandidateMasterId and ca.IRFId= ist.IRFId                   
  where ulsh.EmailId=@EmailId  and im.id in(Select interviewid from @CandidateStatusGrouped)         
  End                
 End                    
                 
  else if(@roleId=1)                    
   Begin                    
    select ca.id as CandidateId, ca.CandidateMasterId, ul.EmailId,                   
 case                     
     when ul.LastName is null then ul.FirstName                    
     else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName))                    
    end as [Name],                   
 ca.ClientHRAttemptContact as HREmail, ca.IRFId, ca.ProfileSource, im.id as InterviewId,                    
    ast.[Date] as ScheduleStatusDate, ast.Comments as ScheduleStatusComments, ast.[StatusValue] as ScheduleStatusValue,                    
    mst.Comments as MeetingStatus, mst.[Date] as MeetingStatusDate, ist.Comments as InterviewStatus, ist.[Date] as InterviewStatusDate,                    
    ic.InterviewStatus as CurrentInterviewStatus, qcfr.QCStatus,                    
    ic.MeetingStatus as CurrentMeetingStatus, im.confirmationStatus, irf.IRFId as IRFName, ca.Phone, im.interviewDate,                   
    (CASE WHEN CHARINDEX('|', JDTiltle) > 0 THEN SUBSTRING(JDTiltle, 1, CHARINDEX('|', JDTiltle) -1) ELSE JDTiltle END) as PrimarySkill,ul.MobileNumber,im.interviewDate,IRF.id as imid,IRF.IRFId as IRFname,IRF.CalendlyLink
    from Candidate ca                     
    join CandidatesMaster cm on cm.id =ca.CandidateMasterId                    
    join UserList ul on ul.id =cm.UserId                    
    join IRF on IRF.id=ca.IRFId                    
    inner join TransClientUser tcu on IRF.id=tcu.IRFId                    
    left join UserList ulc on tcu.ClientUserId = ulc.id                    
    left join InterviewMeetings im on im.candidateEmail=ul.EmailId and im.IRFId =IRF.IRFId                    
    left join InterviewComments ic on ic.InterviewId = im.id                    
    left join QCFeedbackReport qcfr on qcfr.InterviewId = im.id    
    join @allStatustable ast on ca.CandidateMasterId = ast.CandidateMasterId and ca.IRFId=ast.IRFId                     
    left join @meetingStatusTable mst on ca.CandidateMasterId= mst.CandidateMasterId and ca.IRFId= mst.IRFId                    
    left join @interviewStatusTable ist on ca.CandidateMasterId= ist.CandidateMasterId and ca.IRFId= ist.IRFId                    
    where ulc.EmailId LIKE @EmailId AND ulc.IsActive=1 and ca.ClientHRAttemptContact=@EmailId                    
    and irf.IsDeleted=0 and im.id in(Select interviewid from @CandidateStatusGrouped)          
   End                   
  else if(@roleId=4)                  
  Begin                  
      select ca.id as CandidateId, ca.CandidateMasterId, ul.EmailId,                   
 case                     
     when ul.LastName is null then ul.FirstName                    
     else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName))                    
    end as [Name],                   
 ca.ClientHRAttemptContact as HREmail, ca.IRFId, ca.ProfileSource, im.id as InterviewId,                    
    ast.[Date] as ScheduleStatusDate, ast.Comments as ScheduleStatusComments, ast.[StatusValue] as ScheduleStatusValue,                    
    mst.Comments as MeetingStatus, mst.[Date] as MeetingStatusDate, ist.Comments as InterviewStatus, ist.[Date] as InterviewStatusDate,                    
    ic.InterviewStatus as CurrentInterviewStatus, qcfr.QCStatus,                    
    ic.MeetingStatus as CurrentMeetingStatus, im.confirmationStatus, irf.IRFId as IRFName, ca.Phone, im.interviewDate,                   
    (CASE WHEN CHARINDEX('|', JDTiltle) > 0 THEN SUBSTRING(JDTiltle, 1, CHARINDEX('|', JDTiltle) -1) ELSE JDTiltle END) as PrimarySkill,ul.MobileNumber,im.interviewDate,IRF.id as imid,IRF.IRFId as IRFname,IRF.CalendlyLink
    from Candidate ca                     
    join CandidatesMaster cm on cm.id =ca.CandidateMasterId                    
    join UserList ul on ul.id =cm.UserId                    
    join IRF on IRF.id=ca.IRFId                    
    inner join TransClientUser tcu on IRF.id=tcu.IRFId                    
    left join UserList ulc on tcu.ClientUserId = ulc.id                    
    left join InterviewMeetings im on im.candidateEmail=ul.EmailId and im.IRFId =IRF.IRFId                    
    left join InterviewComments ic on ic.InterviewId = im.id                    
    left join QCFeedbackReport qcfr on qcfr.InterviewId = im.id   
    join @allStatustable ast on ca.CandidateMasterId = ast.CandidateMasterId and ca.IRFId=ast.IRFId                     
    left join @meetingStatusTable mst on ca.CandidateMasterId= mst.CandidateMasterId and ca.IRFId= mst.IRFId                    
    left join @interviewStatusTable ist on ca.CandidateMasterId= ist.CandidateMasterId and ca.IRFId= ist.IRFId                    
    where ulc.EmailId LIKE @EmailId AND ulc.IsActive=1 and (ca.ProfileSource=@EmailId or im.staffingAgency= @EmailId)                   
    and irf.IsDeleted=0  and im.id in(Select interviewid from @CandidateStatusGrouped)         
                  
  End                  
  else                    
   Begin                    
    select ca.id as CandidateId, ca.CandidateMasterId, ul.EmailId,                    
    case                     
     when ul.LastName is null then ul.FirstName                    
     else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName))                    
    end as [Name],                    
    ca.ClientHRAttemptContact as HREmail, ca.IRFId, ca.ProfileSource, im.id as InterviewId,                    
    ast.[Date] as ScheduleStatusDate, ast.Comments as ScheduleStatusComments, ast.[StatusValue] as ScheduleStatusValue,                    
    mst.Comments as MeetingStatus, mst.[Date] as MeetingStatusDate, ist.Comments as InterviewStatus, ist.[Date] as InterviewStatusDate,                    
    ic.InterviewStatus as CurrentInterviewStatus, qcfr.QCStatus,                    
    ic.MeetingStatus as CurrentMeetingStatus, im.confirmationStatus, irf.IRFId as IRFName, ca.Phone,  im.interviewDate,                  
    (CASE WHEN CHARINDEX('|', JDTiltle) > 0 THEN SUBSTRING(JDTiltle, 1, CHARINDEX('|', JDTiltle) -1) ELSE JDTiltle END) as PrimarySkill,ul.MobileNumber,im.interviewDate,IRF.id as imid,IRF.IRFId as IRFname,IRF.CalendlyLink
    from Candidate ca                     
    join CandidatesMaster cm on cm.id =ca.CandidateMasterId                    
    join UserList ul on ul.id =cm.UserId                    
    join IRF on IRF.id=ca.IRFId                    
 inner join TransClientUser tcu on IRF.id=tcu.IRFId                    
    left join UserList ulc on tcu.ClientUserId = ulc.id                    
    left join InterviewMeetings im on im.candidateEmail=ul.EmailId and im.IRFId =IRF.IRFId                    
    left join InterviewComments ic on ic.InterviewId = im.id                    
    left join QCFeedbackReport qcfr on qcfr.InterviewId = im.id  
    join @allStatustable ast on ca.CandidateMasterId = ast.CandidateMasterId and ca.IRFId=ast.IRFId                     
    left join @meetingStatusTable mst on ca.CandidateMasterId= mst.CandidateMasterId and ca.IRFId= mst.IRFId                    
    left join @interviewStatusTable ist on ca.CandidateMasterId= ist.CandidateMasterId and ca.IRFId= ist.IRFId                    
    where ulc.EmailId LIKE @EmailId AND ulc.IsActive=1                    
    and irf.IsDeleted=0 and im.id in(Select interviewid from @CandidateStatusGrouped)         
   End                    
 End                    
Else                    
 Begin                    
  select ca.id as CandidateId, ca.CandidateMasterId, ul.EmailId,                    
  case                     
   when ul.LastName is null then ul.FirstName                    
   else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName))                    
  end as [Name],                    
  ca.ClientHRAttemptContact as HREmail, ca.IRFId, ca.ProfileSource, im.id as InterviewId,                    
  ast.[Date] as ScheduleStatusDate, ast.Comments as ScheduleStatusComments, ast.[StatusValue] as ScheduleStatusValue,                    
  mst.Comments as MeetingStatus, mst.[Date] as MeetingStatusDate, ist.Comments as InterviewStatus, ist.[Date] as InterviewStatusDate,                    
  ic.InterviewStatus as CurrentInterviewStatus, qcfr.QCStatus,                    
  ic.MeetingStatus as CurrentMeetingStatus, im.confirmationStatus, irf.IRFId as IRFName, ca.Phone,  im.interviewDate,                  
  (CASE WHEN CHARINDEX('|', JDTiltle) > 0 THEN SUBSTRING(JDTiltle, 1, CHARINDEX('|', JDTiltle) -1) ELSE JDTiltle END) as PrimarySkill,ul.MobileNumber,im.interviewDate,IRF.id as imid,IRF.IRFId as IRFname,IRF.CalendlyLink
  from Candidate ca                     
  join CandidatesMaster cm on cm.id =ca.CandidateMasterId                    
  join UserList ul on ul.id =cm.UserId                    
  join IRF on IRF.id=ca.IRFId                    
  left join InterviewMeetings im on im.candidateEmail=ul.EmailId and im.IRFId =IRF.IRFId                    
  left join InterviewComments ic on ic.InterviewId = im.id                    
  left join QCFeedbackReport qcfr on qcfr.InterviewId = im.id    
  join @allStatustable ast on ca.CandidateMasterId = ast.CandidateMasterId and ca.IRFId=ast.IRFId                     
left join @meetingStatusTable mst on ca.CandidateMasterId= mst.CandidateMasterId and ca.IRFId= mst.IRFId                    
  left join @interviewStatusTable ist on ca.CandidateMasterId= ist.CandidateMasterId and ca.IRFId= ist.IRFId          
  where im.id in(Select interviewid from @CandidateStatusGrouped)        
 End           
END          
                     
End    
Go