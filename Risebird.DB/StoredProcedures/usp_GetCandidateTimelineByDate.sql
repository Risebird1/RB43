/****** Object:  StoredProcedure [dbo].[usp_GetCandidateTimeline]    Script Date: 11-12-2020 05.54.02 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetCandidateTimelineByDate'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCandidateTimelineByDate
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

  
  
  
Create PROCEDURE [dbo].[usp_GetCandidateTimelineByDate]  
@EmailId varchar(100),  
@startDate datetime,  
@endDate datetime  
AS    
Begin    
    
    
declare  @addedStatusId int    
    
 set @addedStatusId = (select id from CandidateStatusOptionsMst where StatusValue='Added')    
    
declare @CandidateStatusGrouped table (IRFId int, CandidateId int)    
    insert into @CandidateStatusGrouped     
    select IRFId, CandidateId from CandidateStatus where [Status]=@addedStatusId and cast([Date] as date) <= cast (@endDate as date) and cast([Date] as date) >= cast (@startDate as date)    
    
    
declare  @meetingStatusId int, @interviewStatusId int, @qcPendingId int;    
    
 set @meetingStatusId = (select id from CandidateStatusOptionsMst where StatusValue='Meeting Status')    
 set @interviewStatusId = (select id from CandidateStatusOptionsMst where StatusValue='Interview Status')    
 set @qcPendingId = (select id from CandidateStatusOptionsMst where StatusValue='QC Pending')--19   
   
 declare @allStatustable table (CandidateMasterId int, IRFId int, [Date] datetime, StatusValue varchar(50), Comments varchar(max))    
    insert into @allStatustable     
    select cs.CandidateId, cs.IRFId, cs.[Date], csom.StatusValue, cs.Comments from CandidateStatus cs   
 join CandidateStatusOptionsMst csom  on cs.[Status] = csom.id   
 join @CandidateStatusGrouped csg on (csg.CandidateId=cs.CandidateId and csg.IRFId=cs.IRFId)  
  
    
    
 declare @meetingStatusTable table (CandidateMasterId int, IRFId int, [Date] datetime, Comments varchar(max))    
    insert into @meetingStatusTable     
    select cs.CandidateId, cs.IRFId, [Date], Comments from CandidateStatus cs join @CandidateStatusGrouped csg on (csg.CandidateId=cs.CandidateId and csg.IRFId=cs.IRFId) where [Status]=@meetingStatusId     
    
 declare @interviewStatusTable table (CandidateMasterId int, IRFId int, [Date] datetime, Comments varchar(max))    
    insert into @interviewStatusTable     
    select cs.CandidateId, cs.IRFId, [Date], Comments from CandidateStatus cs join @CandidateStatusGrouped csg on (csg.CandidateId=cs.CandidateId and csg.IRFId=cs.IRFId)   
 where ([Status]=@interviewStatusId  or [Status]= @qcPendingId)  
    
     
if @EmailId is not null    
 Begin    
  declare @profileId int, @roleId int, @userId int=0;    
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
  (CASE WHEN CHARINDEX('|', JDTiltle) > 0 THEN SUBSTRING(JDTiltle, 1, CHARINDEX('|', JDTiltle) -1) ELSE JDTiltle END) as PrimarySkill    
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
    (CASE WHEN CHARINDEX('|', JDTiltle) > 0 THEN SUBSTRING(JDTiltle, 1, CHARINDEX('|', JDTiltle) -1) ELSE JDTiltle END) as PrimarySkill    
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
    and irf.IsDeleted=0    
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
    (CASE WHEN CHARINDEX('|', JDTiltle) > 0 THEN SUBSTRING(JDTiltle, 1, CHARINDEX('|', JDTiltle) -1) ELSE JDTiltle END) as PrimarySkill    
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
    where ulc.EmailId LIKE @EmailId AND ulc.IsActive=1 and ca.ProfileSource=@EmailId    
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
    (CASE WHEN CHARINDEX('|', JDTiltle) > 0 THEN SUBSTRING(JDTiltle, 1, CHARINDEX('|', JDTiltle) -1) ELSE JDTiltle END) as PrimarySkill    
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
  (CASE WHEN CHARINDEX('|', JDTiltle) > 0 THEN SUBSTRING(JDTiltle, 1, CHARINDEX('|', JDTiltle) -1) ELSE JDTiltle END) as PrimarySkill    
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
GO


