/****** Object:  StoredProcedure [dbo].[usp_GetUnassignedOpenInterviews]    ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetUnassignedOpenInterviews'))
BEGIN
    DROP PROCEDURE dbo.usp_GetUnassignedOpenInterviews
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO




CREATE PROCEDURE [dbo].[usp_GetUnassignedOpenInterviews]  
@timeNow datetime
AS  
Begin
declare @nearestThirtyMinute datetime, @threeHoursFromNow datetime

set @nearestThirtyMinute = (SELECT DATEADD(MINUTE, ROUND(DATEDIFF(MINUTE, 0,  @timeNow) / 30.0, 0) * 30, 0))

set @threeHoursFromNow = (SELECT DATEADD(MINUTE, 180, @nearestThirtyMinute))


select im.interviewDate, irf.JDTiltle, irf.id as irfIntId, im.id as imid from InterviewMeetings im join IRF on IRF.IRFId  = im.IRFId
where (im.interviewDate = @threeHoursFromNow  or im.interviewDate = DATEADD(MINUTE, 30, @threeHoursFromNow)
or im.interviewDate = DATEADD(MINUTE, 60, @threeHoursFromNow) or im.interviewDate = DATEADD(MINUTE, 90, @threeHoursFromNow)
or im.interviewDate = DATEADD(MINUTE, 120, @threeHoursFromNow) or im.interviewDate = DATEADD(MINUTE, 150, @threeHoursFromNow)
or im.interviewDate = DATEADD(MINUTE, 180, @threeHoursFromNow) or im.interviewDate = DATEADD(MINUTE, 210, @threeHoursFromNow)
or im.interviewDate = DATEADD(MINUTE, -30, @threeHoursFromNow) or im.interviewDate = DATEADD(MINUTE, -60, @threeHoursFromNow))
and im.interviewer is null
End
GO

