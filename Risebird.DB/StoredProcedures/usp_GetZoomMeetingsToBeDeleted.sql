/****** Object:  StoredProcedure [dbo].[usp_GetAccounts]    Script Date: 24-03-2020 8.20.45 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetZoomMeetingsToBeDeleted'))
BEGIN
    DROP PROCEDURE dbo.usp_GetZoomMeetingsToBeDeleted
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO




CREATE PROCEDURE [dbo].[usp_GetZoomMeetingsToBeDeleted]  
@cutOffDate datetime
AS  
Begin 

select distinct im.id as imid, im.ZoomMeetingId, im.zoomLink
from InterviewMeetings im 
join IRF on IRF.IRFId = im.IRFId
join InterviewComments ic on ic.InterviewId = im.id
where cast(im.interviewDate as date)<=cast(@cutOffDate as date) and cast(im.interviewDate as date)>=cast(dateadd(DAY, -30, getdate()) as date) and im.ZoomMeetingId is not null and ZoomMeetingId != '0' and (zoomLink is not null and zoomLink != '') and (irf.MeetingPlatform is null or irf.MeetingPlatform = 'Zoom')
and ic.VideoLink is not null and ic.VideoLink != ''

End
GO


