/****** Object:  StoredProcedure [dbo].[usp_GetMeetingsResults]    Script Date: 30-07-2020 8.07.03 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetMeetingsResults'))
BEGIN
    DROP PROCEDURE dbo.usp_GetMeetingsResults
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetMeetingsResults]  
AS  
Begin 

select (select Count(im.id) from InterviewMeetings im  left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1 and ic.InterviewStatus=1) as ProxyCount,
(select Count(im.id) from InterviewMeetings im  left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1 and ic.InterviewStatus=2) as SelectedCount,
(select Count(im.id) from InterviewMeetings im  left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1 and ic.InterviewStatus=3) as RejectedCount,
(select Count(im.id) from InterviewMeetings im  left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1 and ic.InterviewStatus=4) as BorderLineCount,
(select Count(im.id) from InterviewMeetings im  left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1 and ic.InterviewStatus is null and ic.MeetingStatus=1) as NoShowCount,
(select Count(im.id) from InterviewMeetings im  left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1 and ic.InterviewStatus is null and (ic.MeetingStatus=2 or ic.MeetingStatus is null)) as ScheduledCount,
(select Count(im.id) from InterviewMeetings im  left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1 and ic.InterviewStatus is null and ic.MeetingStatus=3) as DroppedCount,
(select Count(im.id) from InterviewMeetings im  left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1) as TotalMeetingsCount


End

GO


