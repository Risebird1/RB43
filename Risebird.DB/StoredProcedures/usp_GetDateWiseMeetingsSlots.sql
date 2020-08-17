/****** Object:  StoredProcedure [dbo].[usp_GetDateWiseMeetingsSlots]    Script Date: 24-07-2020 9.15.24 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetDateWiseMeetingsSlots'))
BEGIN
    DROP PROCEDURE dbo.usp_GetDateWiseMeetingsSlots
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetDateWiseMeetingsSlots]    
@dateValue datetime  
AS    
Begin 

	select irf.IRFId, (irf.irfID + '('+ CASE WHEN CHARINDEX('|', JDTiltle) != 0 THEN (SUBSTRING(JDTiltle,0, (CHARINDEX('|', JDTiltle)))) ELSE JDTiltle END +')' ) as IRFnSkill,
	(select count(interviewSlot) from InterviewMeetings where IRFId=im.IRFId and interviewSlot=1 and cast(interviewDate as date) = cast (@dateValue as date) and im.confirmationStatus=1 and confirmationStatus=1) as Morning,
	(select count(interviewSlot) from InterviewMeetings where IRFId=im.IRFId and interviewSlot=2 and cast(interviewDate as date) = cast (@dateValue as date) and im.confirmationStatus=1 and confirmationStatus=1) as Evening,
	(select count(interviewSlot) from InterviewMeetings where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and im.confirmationStatus=1 and confirmationStatus=1) as total,
	(select count(inm.id) from interviewmeetings inm left join InterviewComments inc on inc.InterviewId = inm.id where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and InterviewStatus=1  and confirmationStatus=1) as Proxy,
	(select count(inm.id) from interviewmeetings inm left join InterviewComments inc on inc.InterviewId = inm.id where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and InterviewStatus=2  and confirmationStatus=1) as Selected,
	(select count(inm.id) from interviewmeetings inm left join InterviewComments inc on inc.InterviewId = inm.id where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and InterviewStatus=3  and confirmationStatus=1) as Rejected,
	(select count(inm.id) from interviewmeetings inm left join InterviewComments inc on inc.InterviewId = inm.id where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and InterviewStatus=4  and confirmationStatus=1) as BorderLine,
	(select count(inm.id) from interviewmeetings inm left join InterviewComments inc on inc.InterviewId = inm.id where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and InterviewStatus is null and MeetingStatus=1  and confirmationStatus=1) as NoShowCount,
	(select count(inm.id) from interviewmeetings inm left join InterviewComments inc on inc.InterviewId = inm.id where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and InterviewStatus is null and (MeetingStatus=2 or MeetingStatus is null)  and confirmationStatus=1) as ScheduledCount,
	(select count(inm.id) from interviewmeetings inm left join InterviewComments inc on inc.InterviewId = inm.id where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and InterviewStatus is null and MeetingStatus=3  and confirmationStatus=1) as DroppedCount,
	(select count(inm.id) from interviewmeetings inm left join InterviewComments inc on inc.InterviewId = inm.id where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and InterviewStatus is null and MeetingStatus=4  and confirmationStatus=1) as Pending
	from InterviewMeetings im join irf on irf.IRFId = im.IRFId
	where cast(interviewDate as date) = cast (@dateValue as date) and im.confirmationStatus=1 
	group by IRF.IRFId, irf.JDTiltle, im.IRFId, im.confirmationStatus order by total desc

End

  

GO


