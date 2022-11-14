/****** Object:  UserDefinedFunction [dbo].[fn_GetConcurrentInterviewsForZoomAccount]     AM ******/
IF EXISTS (SELECT * FROM sys.objects WHERE  object_id = OBJECT_ID(N'[dbo].[fn_GetConcurrentInterviewsForZoomAccount]')
                      AND type IN ( N'FN' ))
BEGIN
    DROP FUNCTION dbo.fn_GetConcurrentInterviewsForZoomAccount
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


Create Function [dbo].[fn_GetConcurrentInterviewsForZoomAccount](@userName varchar(50), @startTime datetime, @endTime datetime )
Returns int
as
Begin

declare @concurrentMeetings int = 0

select @concurrentMeetings = count(id) from InterviewMeetings where risebirdHR = @userName
and interviewDate between @startTime and @endTime
and charindex('zoom', zoomLink) > 0

return @concurrentMeetings


End
GO


