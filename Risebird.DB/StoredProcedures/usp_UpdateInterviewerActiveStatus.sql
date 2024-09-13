IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateInterviewerActiveStatus'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateInterviewerActiveStatus
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_UpdateInterviewerActiveStatus
@userId int,
@status int
AS
BEGIN

if(@status = 1)
Begin
	update UserList set IsActive = 1 where id = @userId

	select EmailId, UserPassword from UserList where id = @userId
end

END
Go