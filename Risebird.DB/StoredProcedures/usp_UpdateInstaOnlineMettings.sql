IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateInstaOnlineMettings'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateInstaOnlineMettings
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_UpdateInstaOnlineMettings
@startdate datetime,  
@enddate datetime,
@Accountemail varchar(150)
AS
BEGIN
Declare @Accountid int
Select @Accountid=id From InstaZoomMeetingAccounts where AccountEmailid=@Accountemail
Insert into InstaZoomMeetingDetails values(@startdate,@enddate,@Accountid)
END
GO