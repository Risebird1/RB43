
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_CheckOnlineMettings'))
BEGIN
    DROP PROCEDURE [dbo].[usp_CheckOnlineMettings]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO



Create procedure usp_CheckOnlineMettings 
@startdate datetime,
@enddate datetime
AS
BEGIN
Select AccountEmail from MeetingAccount b where Not Exists(Select AccountId from  MeetingDetails a where ((MeetingStart between @startdate and @enddate) or (MeetingEnd between @startdate and @enddate)) and b.id = a.AccountId)
END
GO