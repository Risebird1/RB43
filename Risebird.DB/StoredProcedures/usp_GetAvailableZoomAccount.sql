/****** Object:  StoredProcedure [dbo].[usp_GetAvailableZoomAccount]    Script Date: 28-09-2020 12.02.13 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAvailableZoomAccount'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAvailableZoomAccount
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_GetAvailableZoomAccount]
@startTime datetime,
@endTime datetime,
@currentZoomAccount varchar(50) = null
AS  
Begin 
declare @concurrentMeetings int = 0, @concurrencyAllowed int = 0

if(@currentZoomAccount is not null and @currentZoomAccount != '')
Begin
select @concurrentMeetings = count(id) from InterviewMeetings where risebirdHR = @currentZoomAccount
and interviewDate between @startTime and @startTime

select @concurrencyAllowed = ConcurrencyAllowed from ZoomAccounts where UserName = @currentZoomAccount
end


if(@concurrentMeetings < @concurrencyAllowed)
begin
	select @currentZoomAccount
End
else
Begin

;with cte
as
(
	select count(im.id) as totalConcurrentMeetings, za.UserName, za.ConcurrencyAllowed from InterviewMeetings im 
	join ZoomAccounts za on im.risebirdHR = za.UserName
	where (@currentZoomAccount is null or (za.UserName != @currentZoomAccount and @currentZoomAccount != ''))
	and interviewDate between @startTime and @startTime
	and charindex('zoom', im.zoomLink) > 0
	group by za.UserName, za.ConcurrencyAllowed
)
select top 1 UserName  from cte
where (ConcurrencyAllowed - totalConcurrentMeetings) > 0
order by (ConcurrencyAllowed - totalConcurrentMeetings) desc

End



End
GO

