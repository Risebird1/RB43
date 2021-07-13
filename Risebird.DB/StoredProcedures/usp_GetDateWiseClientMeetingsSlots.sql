/****** Object:  StoredProcedure [dbo].[usp_GetDateWiseClientMeetingsSlots]    Script Date: 10-08-2020 6.17.33 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetDateWiseClientMeetingsSlots'))
BEGIN
    DROP PROCEDURE dbo.usp_GetDateWiseClientMeetingsSlots
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetDateWiseClientMeetingsSlots]  
@dateValue datetime,
@emailId varchar(100)
AS  
Begin   
  
declare @profileId int, @roleId int, @userId int;  
 set @profileId = (select id from [Profile] where [Name]='Client')  
 set @userId  = (select id from UserList where EmailId=@emailId and idProfile=@profileId)  
 set @roleId  = (select RoleId from ClientUser where UserId=@userId)  
  
  
if(@roleId=1)  
  
 Begin  
    
  declare @commonHRTable table (imid int, icid int, MeetingStatus int, InterviewStatus int, irfId varchar(100));  
  insert into @commonHRTable select distinct im.id, ic.id,ic.MeetingStatus, ic.InterviewStatus, im.IRFId from InterviewMeetings im   
  left join InterviewComments ic on ic.InterviewId=im.id 
  join irf on irf.IRFId = im.IRFId
  join UserList ulc on im.candidateEmail= ulc.EmailId      
  join CandidatesMaster cm on ulc.id= cm.UserId      
  join Candidate ca on cm.id = ca.CandidateMasterId and irf.id= ca.IRFId
  where confirmationStatus=1 and cast(interviewDate as date) = cast (@dateValue as date)  
  and (ClientHrContact=@emailId or ca.ClientHRAttemptContact =@emailId) 
  
  select irf.irfid, (irf.irfID + '('+ CASE WHEN CHARINDEX('|', JDTiltle) != 0 THEN (SUBSTRING(JDTiltle,0, (CHARINDEX('|', JDTiltle)))) ELSE JDTiltle END +')' ) as IRFnSkill,  
  (select count(interviewSlot) from InterviewMeetings where IRFId=im.IRFId and interviewSlot=1 and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and ClientHrContact=@emailId) as Morning,  
  (select count(interviewSlot) from InterviewMeetings where IRFId=im.IRFId and interviewSlot=2 and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and ClientHrContact=@emailId) as Evening,  
  (select count(interviewSlot) from InterviewMeetings where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and ClientHrContact=@emailId) as total,  
  (select count(imid) from @commonHRTable where irfId=im.IRFId and InterviewStatus=1) as Proxy,  
  (select count(imid) from @commonHRTable where irfId=im.IRFId and InterviewStatus=2) as Selected,  
  (select count(imid) from @commonHRTable where irfId=im.IRFId and InterviewStatus=3) as Rejected,  
  (select count(imid) from @commonHRTable where irfId=im.IRFId and InterviewStatus=4) as BorderLine,  
  (select count(imid) from @commonHRTable where irfId=im.IRFId and InterviewStatus is null and MeetingStatus=1) as NoShowCount,  
  (select count(imid) from @commonHRTable where irfId=im.IRFId and InterviewStatus is null and (MeetingStatus=2 or MeetingStatus is null)) as ScheduledCount,  
  (select count(imid) from @commonHRTable where irfId=im.IRFId and InterviewStatus is null and MeetingStatus=3) as DroppedCount,  
  (select count(imid) from @commonHRTable where irfId=im.IRFId and InterviewStatus is null and MeetingStatus=4) as Pending  
  from InterviewMeetings im join IRF on irf.IRFId = im.IRFId
  join UserList ulc on im.candidateEmail= ulc.EmailId      
  join CandidatesMaster cm on ulc.id= cm.UserId      
  join Candidate ca on cm.id = ca.CandidateMasterId and irf.id= ca.IRFId
  where cast(interviewDate as date) = cast (@dateValue as date) and im.confirmationStatus=1 and (im.ClientHrContact=@emailId or ca.ClientHRAttemptContact =@emailId) 
  group by IRF.IRFId, im.ClientHrContact, im.confirmationStatus, im.IRFId,irf.JDTiltle order by total desc  
  
 End  
  
else if(@roleId=4)  
 Begin  
  
  declare @commonStaffingTable table (imid int, icid int, MeetingStatus int, InterviewStatus int, irfId varchar(100));  
  insert into @commonStaffingTable select im.id, ic.id,ic.MeetingStatus, ic.InterviewStatus, im.IRFId from InterviewMeetings im   
  left join InterviewComments ic on ic.InterviewId=im.id where confirmationStatus=1 and cast(interviewDate as date) = cast (@dateValue as date)  
  and staffingAgency=@emailId  
    
  select irf.irfid, (irf.irfID + '('+ CASE WHEN CHARINDEX('|', JDTiltle) != 0 THEN (SUBSTRING(JDTiltle,0, (CHARINDEX('|', JDTiltle)))) ELSE JDTiltle END +')' ) as IRFnSkill,  
  (select count(interviewSlot) from InterviewMeetings where IRFId=im.IRFId and interviewSlot=1 and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and staffingAgency=@emailId) as Morning,  
  (select count(interviewSlot) from InterviewMeetings where IRFId=im.IRFId and interviewSlot=2 and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and staffingAgency=@emailId) as Evening,  
  (select count(interviewSlot) from InterviewMeetings where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and staffingAgency=@emailId) as total,  
  (select count(imid) from @commonStaffingTable where irfId=im.IRFId and InterviewStatus=1) as Proxy,  
  (select count(imid) from @commonStaffingTable where irfId=im.IRFId and InterviewStatus=2) as Selected,  
  (select count(imid) from @commonStaffingTable where irfId=im.IRFId and InterviewStatus=3) as Rejected,  
  (select count(imid) from @commonStaffingTable where irfId=im.IRFId and InterviewStatus=4) as BorderLine,  
  (select count(imid) from @commonStaffingTable where irfId=im.IRFId and InterviewStatus is null and MeetingStatus=1) as NoShowCount,  
  (select count(imid) from @commonStaffingTable where irfId=im.IRFId and InterviewStatus is null and (MeetingStatus=2 or MeetingStatus is null)) as ScheduledCount,  
  (select count(imid) from @commonStaffingTable where irfId=im.IRFId and InterviewStatus is null and MeetingStatus=3) as DroppedCount,  
  (select count(imid) from @commonStaffingTable where irfId=im.IRFId and InterviewStatus is null and MeetingStatus=4) as Pending  
  from InterviewMeetings im join IRF on irf.IRFId = im.IRFId  
  where cast(interviewDate as date) = cast (@dateValue as date) and im.confirmationStatus=1 and im.staffingAgency=@emailId  
  group by IRF.IRFId, im.staffingAgency, im.confirmationStatus, im.IRFId,irf.JDTiltle order by total desc  
  
 End  
  
else  
 Begin  
    
  declare @commonTable table (imid int, icid int, MeetingStatus int, InterviewStatus int, irfId varchar(100));  
  insert into @commonTable select im.id, ic.id,ic.MeetingStatus, ic.InterviewStatus, im.IRFId from InterviewMeetings im   
  left join InterviewComments ic on ic.InterviewId=im.id where confirmationStatus=1 and cast(interviewDate as date) = cast (@dateValue as date)  
  
  
  select irf.irfid, (irf.irfID + '('+ CASE WHEN CHARINDEX('|', JDTiltle) != 0 THEN (SUBSTRING(JDTiltle,0, (CHARINDEX('|', JDTiltle)))) ELSE JDTiltle END +')' ) as IRFnSkill,  
  (select count(interviewSlot) from InterviewMeetings where IRFId=im.IRFId and interviewSlot=1 and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1) as Morning,  
  (select count(interviewSlot) from InterviewMeetings where IRFId=im.IRFId and interviewSlot=2 and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1) as Evening,  
  (select count(interviewSlot) from InterviewMeetings where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1) as total,  
  (select count(imid) from @commonTable where irfId=im.IRFId and InterviewStatus=1) as Proxy,  
  (select count(imid) from @commonTable where irfId=im.IRFId and InterviewStatus=2) as Selected,  
  (select count(imid) from @commonTable where irfId=im.IRFId and InterviewStatus=3) as Rejected,  
  (select count(imid) from @commonTable where irfId=im.IRFId and InterviewStatus=4) as BorderLine,  
  (select count(imid) from @commonTable where irfId=im.IRFId and InterviewStatus is null and MeetingStatus=1) as NoShowCount,  
  (select count(imid) from @commonTable where irfId=im.IRFId and InterviewStatus is null and (MeetingStatus=2 or MeetingStatus is null)) as ScheduledCount,  
  (select count(imid) from @commonTable where irfId=im.IRFId and InterviewStatus is null and MeetingStatus=3) as DroppedCount,  
  (select count(imid) from @commonTable where irfId=im.IRFId and InterviewStatus is null and MeetingStatus=4) as Pending  
  from InterviewMeetings im join IRF on irf.IRFId = im.IRFId   
  join TransClientUser tcu on tcu.IRFId = irf.id  
  join UserList ul on tcu.ClientUserId = ul.id  
  where cast(interviewDate as date) = cast (@dateValue as date) and im.confirmationStatus=1 and tcu.ClientUserId =@userId   
  group by IRF.IRFId, im.confirmationStatus, im.IRFId,irf.JDTiltle order by total desc  
    
 End  
  
End 

GO


