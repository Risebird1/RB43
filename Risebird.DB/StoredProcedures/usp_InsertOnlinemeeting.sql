IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_InsertOnlinemeeting'))
BEGIN
    DROP PROCEDURE [dbo].[usp_InsertOnlinemeeting]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO



Create Procedure usp_InsertOnlinemeeting
@startdate datetime,
@enddate datetime,
@Whocreate varchar(100),
@Meetingid varchar(100),
@LinkUrl varchar(max),
@subject varchar(max),
@whom varchar(max)
AS
BEGIN
DECLARE @id int
Select @id=id from MeetingAccount where OnlineEmail= @Meetingid
Insert into OnlinemeetingDetails values(@startdate,@enddate,@Whocreate,'Active',@LinkUrl,@subject,@whom,Getdate(),@id)
END