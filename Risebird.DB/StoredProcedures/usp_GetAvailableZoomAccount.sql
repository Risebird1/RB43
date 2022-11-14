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
and interviewDate between @startTime and @endTime
and charindex('zoom', zoomLink) > 0

select @concurrencyAllowed = ConcurrencyAllowed from ZoomAccounts where UserName = @currentZoomAccount
end


if(@concurrentMeetings < @concurrencyAllowed)
begin
	select @currentZoomAccount
End
else
Begin


--Select top 1 UserName from ZoomAccounts za
--outer apply 
--(select COUNT(id) as totalcount from InterviewMeetings where risebirdHR=za.UserName 
--and interviewDate >= @startTime and interviewDate <= @endTime
--and charindex('zoom', zoomLink) > 0 ) abc
--where za.IsActive= 1 and (za.ConcurrencyAllowed-abc.totalcount)>0
--order by (za.ConcurrencyAllowed-abc.totalcount) desc

;with cte
as
(
	select dbo.fn_GetConcurrentInterviewsForZoomAccount(UserName, @startTime, @endTime) as totalConcurrentMeetings, UserName, ConcurrencyAllowed from ZoomAccounts 
	where IsActive = 1
	group by UserName, ConcurrencyAllowed
)
select top 1 UserName from cte
where (ConcurrencyAllowed - totalConcurrentMeetings) > 0
order by (ConcurrencyAllowed - totalConcurrentMeetings) desc

End



End
GO

