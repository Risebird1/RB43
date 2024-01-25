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
@timeNow datetime,
@irfId int = null
AS  
Begin
declare @nearestThirtyMinute datetime, @threeHoursFromNow datetime

set @nearestThirtyMinute = (SELECT DATEADD(MINUTE, ROUND(DATEDIFF(MINUTE, 0,  @timeNow) / 30.0, 0) * 30, 0))

set @threeHoursFromNow = (SELECT DATEADD(MINUTE, 180, @nearestThirtyMinute))

if(@irfId is not null and @irfId>0)
Begin

select im.interviewDate, irf.JDTiltle, irf.id as irfIntId, im.id as imid, im.IRFId from InterviewMeetings im 
join IRF on IRF.IRFId  = im.IRFId
where (im.interviewDate = @threeHoursFromNow  or im.interviewDate = DATEADD(MINUTE, 30, @threeHoursFromNow)
or im.interviewDate = DATEADD(MINUTE, 60, @threeHoursFromNow) or im.interviewDate = DATEADD(MINUTE, 90, @threeHoursFromNow)
or im.interviewDate = DATEADD(MINUTE, 120, @threeHoursFromNow) or im.interviewDate = DATEADD(MINUTE, 150, @threeHoursFromNow)
or im.interviewDate = DATEADD(MINUTE, 180, @threeHoursFromNow) or im.interviewDate = DATEADD(MINUTE, 210, @threeHoursFromNow)
or im.interviewDate = DATEADD(MINUTE, -30, @threeHoursFromNow) or im.interviewDate = DATEADD(MINUTE, -60, @threeHoursFromNow))
and im.interviewer is null and irf.id = @irfId

End
Else
Begin

select im.interviewDate, irf.JDTiltle, irf.id as irfIntId, im.id as imid, im.IRFId from InterviewMeetings im 
join IRF on IRF.IRFId  = im.IRFId
where im.interviewDate = DATEADD(MINUTE, 60, @threeHoursFromNow)-- 4 hours left for interview
or im.interviewDate = DATEADD(MINUTE, -60, @threeHoursFromNow) -- 2 hours left for interview
and im.interviewer is null

End

End
GO

