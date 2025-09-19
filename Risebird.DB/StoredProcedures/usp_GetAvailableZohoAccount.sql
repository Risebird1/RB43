IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAvailableZohoAccount'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAvailableZohoAccount
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_GetAvailableZohoAccount]
@startTime datetime,
@endTime datetime,
@currentZohoAccount varchar(50) = null
AS  
Begin 
declare @concurrentMeetings int = 0, @concurrencyAllowed int = 0

if(@currentZohoAccount is not null and @currentZohoAccount != '')
Begin
select @concurrentMeetings = count(id) from InterviewMeetings where risebirdHR = @currentZohoAccount
and interviewDate between @startTime and @endTime and MeetingPlatForm='Zoho'

select @concurrencyAllowed = ConcurrencyAllowed from ZoomAccounts where UserName = @currentZohoAccount
end

if(@concurrentMeetings < @concurrencyAllowed)
begin
	select @currentZohoAccount
End
else
Begin
;with cte
as
(
	select zat.totalConcurrentMeetings,UserId, UserEmail, ConcurrencyAllowed from ZohoAccounts za
	Outer Apply (select count(im.id) totalConcurrentMeetings from InterviewMeetings im where risebirdHR = za.UserId  
				and interviewDate between @startTime and @endTime and MeetingPlatForm='Zoho') zat
	where IsActive = 1
)
select top 1 UserId from cte
where (ConcurrencyAllowed - totalConcurrentMeetings) > 0
order by (ConcurrencyAllowed - totalConcurrentMeetings) desc
End
End
Go