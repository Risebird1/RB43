/****** Object:  StoredProcedure [dbo].[usp_GetClientMeetingsResults]    Script Date: 10-08-2020 6.18.31 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetClientMeetingsResults'))
BEGIN
    DROP PROCEDURE dbo.usp_GetClientMeetingsResults
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetClientMeetingsResults]  
@emailId varchar(50)
AS  
Begin  
 declare @profileId int, @roleId int, @userId int;  
 set @profileId = (select id from [Profile] where [Name]='Client')  
 set @userId  = (select id from UserList where EmailId=@emailId and idProfile=@profileId)  
 set @roleId  = (select RoleId from ClientUser where UserId=@userId)  
if(@roleId=1)  
 Begin  
    
  declare @commonClientHRTable table (imid int, icid int, InterviewStatus int, MeetingStatus int, QCStatus int)  
    insert into @commonClientHRTable   
    select inm.id, inc.id, inc.InterviewStatus, inc.MeetingStatus, qcf.QCStatus from InterviewMeetings inm   
    join IRF on inm.IRFId=irf.IRFId 
	join UserList ul on ul.EmailId=inm.candidateEmail 
	join CandidatesMaster cm on cm.UserId = ul.id 
	join Candidate ca on (ca.CandidateMasterId= cm.id and ca.IRFId=irf.id)  
    left join InterviewComments inc on inc.InterviewId= inm.id  
    left join QCFeedbackReport qcf on qcf.InterviewId=inm.id  
    where inm.confirmationStatus=1 and (inm.ClientHrContact=@emailId or ca.ClientHRAttemptContact=@emailId)
  
   select (select Count(imid) from @commonClientHRTable where InterviewStatus=1) as ProxyCount,  
   (select Count(imid) from @commonClientHRTable where MeetingStatus = 4 and InterviewStatus=2 ) as SelectedCount,  
   (select Count(imid) from @commonClientHRTable where MeetingStatus = 4 and InterviewStatus=3) as RejectedCount,  
   (select Count(imid) from @commonClientHRTable where(((InterviewStatus=2 or InterviewStatus=4)  ) or (InterviewStatus is null and MeetingStatus=4))) as Pending,  
   (select Count(imid) from @commonClientHRTable where  MeetingStatus = 4 and InterviewStatus=4) as BorderLineCount,  
   (select Count(imid) from @commonClientHRTable where InterviewStatus is null and MeetingStatus=1) as NoShowCount,  
   (select Count(imid) from @commonClientHRTable where InterviewStatus is null and (MeetingStatus=2 or MeetingStatus is null)) as ScheduledCount,  
   (select Count(imid) from @commonClientHRTable where  InterviewStatus is null and MeetingStatus=3) as DroppedCount,  
   (select Count(imid) from @commonClientHRTable) as TotalMeetingsCount  
 End  
  
else if(@roleId=4)  
 Begin  
    
  declare @commonStaffiingAgencyTable table (imid int, icid int, InterviewStatus int, MeetingStatus int, QCStatus int)  
    insert into @commonStaffiingAgencyTable   
    select inm.id, inc.id, inc.InterviewStatus, inc.MeetingStatus, qcf.QCStatus from InterviewMeetings inm   
    join IRF on inm.IRFId=irf.IRFId join UserList ul on ul.EmailId=inm.candidateEmail join CandidatesMaster cm on cm.UserId = ul.id join Candidate ca on (ca.CandidateMasterId= cm.id and ca.IRFId=irf.id)  
    left join InterviewComments inc on inc.InterviewId= inm.id  
    left join QCFeedbackReport qcf on qcf.InterviewId=inm.id  
    where inm.confirmationStatus=1 and inm.staffingAgency=@emailId  
  
   select (select Count(imid) from @commonStaffiingAgencyTable where InterviewStatus=1) as ProxyCount,  
   (select Count(imid) from @commonStaffiingAgencyTable where MeetingStatus = 4 and InterviewStatus=2 ) as SelectedCount,  
   (select Count(imid) from @commonStaffiingAgencyTable where  MeetingStatus = 4 and InterviewStatus=3) as RejectedCount,  
   (select Count(imid) from @commonStaffiingAgencyTable where  (((InterviewStatus=2 or InterviewStatus=4)  ) or (InterviewStatus is null and MeetingStatus=4))) as Pending,  
   (select Count(imid) from @commonStaffiingAgencyTable where  MeetingStatus = 4 and InterviewStatus=4) as BorderLineCount,  
   (select Count(imid) from @commonStaffiingAgencyTable where InterviewStatus is null and MeetingStatus=1) as NoShowCount,  
   (select Count(imid) from @commonStaffiingAgencyTable where InterviewStatus is null and (MeetingStatus=2 or MeetingStatus is null)) as ScheduledCount,  
   (select Count(imid) from @commonStaffiingAgencyTable where  InterviewStatus is null and MeetingStatus=3) as DroppedCount,  
   (select Count(imid) from @commonStaffiingAgencyTable) as TotalMeetingsCount  
  
 End  
  
else  
  Begin  
  
   declare @commonTable table (imid int, icid int, InterviewStatus int, MeetingStatus int, QCStatus int)  
    insert into @commonTable   
    select inm.id, inc.id, inc.InterviewStatus, inc.MeetingStatus, qcf.QCStatus from InterviewMeetings inm join IRF on irf.IRFId=inm.IRFId join TransClientUser tcu on tcu.IRFId = irf.id  
    join UserList ul on ul.EmailId=inm.candidateEmail join CandidatesMaster cm on cm.UserId = ul.id join Candidate ca on (ca.CandidateMasterId= cm.id and ca.IRFId=irf.id)  
    left join InterviewComments inc on inc.InterviewId= inm.id  
    left join QCFeedbackReport qcf on qcf.InterviewId=inm.id  
    where tcu.ClientUserId = @userId and inm.confirmationStatus=1  
  
   select (select Count(imid) from @commonTable where InterviewStatus=1) as ProxyCount,  
   (select Count(imid) from @commonTable where   MeetingStatus = 4 and InterviewStatus=2 ) as SelectedCount,  
   (select Count(imid) from @commonTable where  MeetingStatus = 4 and InterviewStatus=3) as RejectedCount,  
   (select Count(imid) from @commonTable where  (((InterviewStatus=2 or InterviewStatus=4)  and QCStatus is null) or (InterviewStatus is null and MeetingStatus=4))) as Pending,  
   (select Count(imid) from @commonTable where  MeetingStatus = 4 and InterviewStatus=4) as BorderLineCount,  
   (select Count(imid) from @commonTable where InterviewStatus is null and MeetingStatus=1) as NoShowCount,  
   (select Count(imid) from @commonTable where  InterviewStatus is null and (MeetingStatus=2 or MeetingStatus is null)) as ScheduledCount,  
   (select Count(imid) from @commonTable where InterviewStatus is null and MeetingStatus=3) as DroppedCount,  
   (select Count(imid) from @commonTable) as TotalMeetingsCount  
  
  
  
  End  
End 


GO


