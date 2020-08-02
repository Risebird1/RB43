
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_checkOnlinemettings'))
BEGIN
    DROP PROCEDURE [dbo].[usp_checkOnlinemettings]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO



Create procedure usp_checkOnlinemettings 
@startdate datetime,
@enddate datetime
AS
BEGIN
Select OnlineEmail from MeetingAccount b where Not Exists(Select AccountId from  OnlinemeetingDetails a where ((MeetingStart between @startdate and @enddate) or (MeetingEnd between @startdate and @enddate)) and b.id = a.AccountId)
END