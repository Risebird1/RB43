IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchMeetingPendingVideo'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchMeetingPendingVideo
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_FetchMeetingPendingVideo]  
@timeStart datetime,
@timeEnd datetime
AS  
Begin 
Select id as imid,MeetingDate as interviewDate,MeetingLink as zoomLink,VideoLink,'CalibrationMeetings' as meetingtype from CalibrationMeetings 
where (VideoLink is null or VideoLink='') and MeetingDate<=@timeEnd and MeetingDate>=@timeStart
End
GO