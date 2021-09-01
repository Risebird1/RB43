/****** Object:  StoredProcedure [dbo].[usp_GetPendingVideoInstantMeetings]    ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetPendingVideoInstantMeetings'))
BEGIN
    DROP PROCEDURE dbo.usp_GetPendingVideoInstantMeetings
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO




CREATE PROCEDURE [dbo].[usp_GetPendingVideoInstantMeetings]  
@timeStart datetime,
@timeEnd datetime
AS  
Begin 

select im.id as imid, im.RequestedDate, im.zoomLink from InstaInterviewMeetings im 
where im.RequestedDate<@timeEnd and im.RequestedDate>@timeStart and (im.VideoLink is null or im.VideoLink='') and (im.ZoomLink is not null or im.ZoomLink!='')

End
GO


