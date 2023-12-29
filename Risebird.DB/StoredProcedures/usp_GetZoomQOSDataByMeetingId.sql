IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetZoomQOSDataByMeetingId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetZoomQOSDataByMeetingId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetZoomQOSDataByMeetingId]    
@interviewId int
AS    
Begin  


declare @ZoomMeetingId varchar(200)
set @ZoomMeetingId = (select ZoomMeetingId from InterviewMeetings where id = @interviewId)


select * from PoorQOSZoom where ZoomMeetingId = @ZoomMeetingId

End
GO

