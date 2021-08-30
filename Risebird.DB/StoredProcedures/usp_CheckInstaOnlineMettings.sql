IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_CheckInstaOnlineMettings'))
BEGIN
    DROP PROCEDURE dbo.usp_CheckInstaOnlineMettings
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create procedure usp_CheckInstaOnlineMettings   
@startdate datetime,  
@enddate datetime  
AS  
BEGIN  
 Select AccountEmailid from (
 select AccountEmailid,ROW_NUMBER() over (order by MeetingCount) as rn
 from(Select * From InstaZoomMeetingAccounts izm left join (Select AccountId,count(*) as Meetingcount From InstaZoomMeetingDetails where ((MeetingStart between @startdate and @enddate) or (MeetingEnd between @startdate and @enddate)) group by AccountId) as abc
on abc.AccountId=izm.id)t
) as xyz where rn=1
 
END  
GO