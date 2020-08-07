/****** Object:  StoredProcedure [dbo].[usp_GetDateWiseClientMeetingsSlots]    Script Date: 07-08-2020 3.41.48 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetDateWiseClientMeetingsSlotsWithoutRoles'))
BEGIN
    DROP PROCEDURE dbo.usp_GetDateWiseClientMeetingsSlotsWithoutRoles
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetDateWiseClientMeetingsSlotsWithoutRoles]  
@dateValue datetime,
@emailId varchar(100)
AS  
Begin 

declare @profileId int, @userId int;
	set @profileId = (select id from [Profile] where [Name]='Client')
	set @userId  = (select id from UserList where EmailId=@emailId and idProfile=@profileId)

	Begin

		select irf.irfid, (irf.irfID + '('+ CASE WHEN CHARINDEX('|', JDTiltle) != 0 THEN (SUBSTRING(JDTiltle,0, (CHARINDEX('|', JDTiltle)))) ELSE JDTiltle END +')' ) as IRFnSkill,
		(select count(interviewSlot) from InterviewMeetings where IRFId=im.IRFId and interviewSlot=1 and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1) as Morning,
		(select count(interviewSlot) from InterviewMeetings where IRFId=im.IRFId and interviewSlot=2 and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1) as Evening,
		(select count(interviewSlot) from InterviewMeetings where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1) as total,
		(select count(inm.id) from interviewmeetings inm left join InterviewComments inc on inc.InterviewId = inm.id where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and InterviewStatus=1) as Proxy,
		(select count(inm.id) from interviewmeetings inm left join InterviewComments inc on inc.InterviewId = inm.id where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and InterviewStatus=2) as Selected,
		(select count(inm.id) from interviewmeetings inm left join InterviewComments inc on inc.InterviewId = inm.id where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and InterviewStatus=3) as Rejected,
		(select count(inm.id) from interviewmeetings inm left join InterviewComments inc on inc.InterviewId = inm.id where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and InterviewStatus=4) as BorderLine,
		(select count(inm.id) from interviewmeetings inm left join InterviewComments inc on inc.InterviewId = inm.id where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and InterviewStatus is null) as Pending
		from InterviewMeetings im join IRF on irf.IRFId = im.IRFId 
		join TransClientUser tcu on tcu.IRFId = irf.id
		join UserList ul on tcu.ClientUserId = ul.id
		where cast(interviewDate as date) = cast (@dateValue as date) and im.confirmationStatus=1 and tcu.ClientUserId =@userId 
		group by IRF.IRFId, im.confirmationStatus, im.IRFId,irf.JDTiltle order by total desc

	End

End

GO


