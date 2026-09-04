IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.Usp_UpdateRBPayableStatus'))
BEGIN
    DROP PROCEDURE dbo.Usp_UpdateRBPayableStatus
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create Procedure Usp_UpdateRBPayableStatus
@Emailid varchar(200)
AS
BEGIN
DECLARE @UserId int
Select @UserId=id from UserList where EmailId=@Emailid
Update InterviewPoints set Status='Submitted' where userid=@UserId and (Status='Pending' or Status='Submitted')
END