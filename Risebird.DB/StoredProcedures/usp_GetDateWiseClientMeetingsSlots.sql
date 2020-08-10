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
		

		select irf.irfid, (irf.irfID + '('+ CASE WHEN CHARINDEX('|', JDTiltle) != 0 THEN (SUBSTRING(JDTiltle,0, (CHARINDEX('|', JDTiltle)))) ELSE JDTiltle END +')' ) as IRFnSkill,
		(select count(interviewSlot) from InterviewMeetings where IRFId=im.IRFId and interviewSlot=1 and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and ClientHrContact=@emailId) as Morning,
		(select count(interviewSlot) from InterviewMeetings where IRFId=im.IRFId and interviewSlot=2 and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and ClientHrContact=@emailId) as Evening,
		(select count(interviewSlot) from InterviewMeetings where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and ClientHrContact=@emailId) as total,
		(select count(inm.id) from interviewmeetings inm left join InterviewComments inc on inc.InterviewId = inm.id where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and ClientHrContact=@emailId and InterviewStatus=1) as Proxy,
		(select count(inm.id) from interviewmeetings inm left join InterviewComments inc on inc.InterviewId = inm.id where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and ClientHrContact=@emailId and InterviewStatus=2) as Selected,
		(select count(inm.id) from interviewmeetings inm left join InterviewComments inc on inc.InterviewId = inm.id where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and ClientHrContact=@emailId and InterviewStatus=3) as Rejected,
		(select count(inm.id) from interviewmeetings inm left join InterviewComments inc on inc.InterviewId = inm.id where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and ClientHrContact=@emailId and InterviewStatus=4) as BorderLine,
		(select count(inm.id) from interviewmeetings inm left join InterviewComments inc on inc.InterviewId = inm.id where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and ClientHrContact=@emailId and InterviewStatus is null and MeetingStatus=1) as NoShowCount,
		(select count(inm.id) from interviewmeetings inm left join InterviewComments inc on inc.InterviewId = inm.id where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and ClientHrContact=@emailId and InterviewStatus is null and (MeetingStatus=2 or MeetingStatus is null)) as ScheduledCount,
		(select count(inm.id) from interviewmeetings inm left join InterviewComments inc on inc.InterviewId = inm.id where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and ClientHrContact=@emailId and InterviewStatus is null and MeetingStatus=3) as DroppedCount,
		(select count(inm.id) from interviewmeetings inm left join InterviewComments inc on inc.InterviewId = inm.id where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and ClientHrContact=@emailId and InterviewStatus is null and MeetingStatus=4) as Pending
		from InterviewMeetings im join IRF on irf.IRFId = im.IRFId
		where cast(interviewDate as date) = cast (@dateValue as date) and im.confirmationStatus=1 and im.ClientHrContact=@emailId
		group by IRF.IRFId, im.ClientHrContact, im.confirmationStatus, im.IRFId,irf.JDTiltle order by total desc

		
	End

else if(@roleId=4)
	Begin

		select irf.irfid, (irf.irfID + '('+ CASE WHEN CHARINDEX('|', JDTiltle) != 0 THEN (SUBSTRING(JDTiltle,0, (CHARINDEX('|', JDTiltle)))) ELSE JDTiltle END +')' ) as IRFnSkill,
		(select count(interviewSlot) from InterviewMeetings where IRFId=im.IRFId and interviewSlot=1 and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and staffingAgency=@emailId) as Morning,
		(select count(interviewSlot) from InterviewMeetings where IRFId=im.IRFId and interviewSlot=2 and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and staffingAgency=@emailId) as Evening,
		(select count(interviewSlot) from InterviewMeetings where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and staffingAgency=@emailId) as total,
		(select count(inm.id) from interviewmeetings inm left join InterviewComments inc on inc.InterviewId = inm.id where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and staffingAgency=@emailId and InterviewStatus=1) as Proxy,
		(select count(inm.id) from interviewmeetings inm left join InterviewComments inc on inc.InterviewId = inm.id where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and staffingAgency=@emailId and InterviewStatus=2) as Selected,
		(select count(inm.id) from interviewmeetings inm left join InterviewComments inc on inc.InterviewId = inm.id where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and staffingAgency=@emailId and InterviewStatus=3) as Rejected,
		(select count(inm.id) from interviewmeetings inm left join InterviewComments inc on inc.InterviewId = inm.id where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and staffingAgency=@emailId and InterviewStatus=4) as BorderLine,
		(select count(inm.id) from interviewmeetings inm left join InterviewComments inc on inc.InterviewId = inm.id where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and staffingAgency=@emailId and InterviewStatus is null and MeetingStatus=1) as NoShowCount,
		(select count(inm.id) from interviewmeetings inm left join InterviewComments inc on inc.InterviewId = inm.id where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and staffingAgency=@emailId and InterviewStatus is null and (MeetingStatus=2 or MeetingStatus is null)) as ScheduledCount,
		(select count(inm.id) from interviewmeetings inm left join InterviewComments inc on inc.InterviewId = inm.id where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and staffingAgency=@emailId and InterviewStatus is null and MeetingStatus=3) as DroppedCount,
		(select count(inm.id) from interviewmeetings inm left join InterviewComments inc on inc.InterviewId = inm.id where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and staffingAgency=@emailId and InterviewStatus is null and MeetingStatus=4) as Pending
		from InterviewMeetings im join IRF on irf.IRFId = im.IRFId
		where cast(interviewDate as date) = cast (@dateValue as date) and im.confirmationStatus=1 and im.staffingAgency=@emailId
		group by IRF.IRFId, im.staffingAgency, im.confirmationStatus, im.IRFId,irf.JDTiltle order by total desc
	End

else
	Begin

		select irf.irfid, (irf.irfID + '('+ CASE WHEN CHARINDEX('|', JDTiltle) != 0 THEN (SUBSTRING(JDTiltle,0, (CHARINDEX('|', JDTiltle)))) ELSE JDTiltle END +')' ) as IRFnSkill,
		(select count(interviewSlot) from InterviewMeetings where IRFId=im.IRFId and interviewSlot=1 and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1) as Morning,
		(select count(interviewSlot) from InterviewMeetings where IRFId=im.IRFId and interviewSlot=2 and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1) as Evening,
		(select count(interviewSlot) from InterviewMeetings where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1) as total,
		(select count(inm.id) from interviewmeetings inm left join InterviewComments inc on inc.InterviewId = inm.id where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and InterviewStatus=1) as Proxy,
		(select count(inm.id) from interviewmeetings inm left join InterviewComments inc on inc.InterviewId = inm.id where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and InterviewStatus=2) as Selected,
		(select count(inm.id) from interviewmeetings inm left join InterviewComments inc on inc.InterviewId = inm.id where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and InterviewStatus=3) as Rejected,
		(select count(inm.id) from interviewmeetings inm left join InterviewComments inc on inc.InterviewId = inm.id where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and InterviewStatus=4) as BorderLine,
		(select count(inm.id) from interviewmeetings inm left join InterviewComments inc on inc.InterviewId = inm.id where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and InterviewStatus is null and MeetingStatus=1) as NoShowCount,
		(select count(inm.id) from interviewmeetings inm left join InterviewComments inc on inc.InterviewId = inm.id where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and InterviewStatus is null and (MeetingStatus=2 or MeetingStatus is null)) as ScheduledCount,
		(select count(inm.id) from interviewmeetings inm left join InterviewComments inc on inc.InterviewId = inm.id where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and InterviewStatus is null and MeetingStatus=3) as DroppedCount,
		(select count(inm.id) from interviewmeetings inm left join InterviewComments inc on inc.InterviewId = inm.id where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and InterviewStatus is null and MeetingStatus=4) as Pending
		from InterviewMeetings im join IRF on irf.IRFId = im.IRFId 
		join TransClientUser tcu on tcu.IRFId = irf.id
		join UserList ul on tcu.ClientUserId = ul.id
		where cast(interviewDate as date) = cast (@dateValue as date) and im.confirmationStatus=1 and tcu.ClientUserId =@userId 
		group by IRF.IRFId, im.confirmationStatus, im.IRFId,irf.JDTiltle order by total desc

	End

End

GO


