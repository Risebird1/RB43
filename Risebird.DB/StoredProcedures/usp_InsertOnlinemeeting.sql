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
@LinkUrl varchar(max)
AS
BEGIN
DECLARE @id int
Select @id=id from UserList where Emailid= @Whocreate
Insert into OnlinemeetingDetails values(@startdate,@enddate,@id,@Meetingid,'Active',@LinkUrl)
END