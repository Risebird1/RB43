IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_InsertMeeting'))
BEGIN
    DROP PROCEDURE [dbo].[usp_InsertMeeting]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create Procedure usp_InsertMeeting
@startdate datetime,
@enddate datetime,
@CreatedBy varchar(100),
@AccountEmail varchar(100),
@LinkUrl varchar(max),
@AttendesMails varchar(500),
@Subject varchar(300),
@TimeZone varchar(120)
AS
BEGIN
DECLARE @id int
select @id=id from MeetingAccount where AccountEmail= @AccountEmail
Insert into MeetingDetails values(@startdate,@enddate,@CreatedBy,@LinkUrl,@Subject,@AttendesMails,@TimeZone,@id,1,getutcdate())
select Password from MeetingAccount where AccountEmail= @AccountEmail
END