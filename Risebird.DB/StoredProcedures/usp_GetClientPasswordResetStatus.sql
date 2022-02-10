  
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetClientPasswordResetStatus'))
BEGIN
    DROP PROCEDURE [dbo].[usp_GetClientPasswordResetStatus]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

  
Create PROCEDURE [dbo].[usp_GetClientPasswordResetStatus]  
  
@emailId varchar(100)  
  
AS  
Begin  
	declare @IsPasswordReset bit, @userId int
	set @IsPasswordReset = 0
	set @userId = (select id from UserList where EmailId = @emailId)
	if((select idProfile from UserList where id=@userId) = 4) --client
	Begin
		set @IsPasswordReset = (select IsPasswordReset from Client where id = (select UserId from ClientUser where UserId = (select id from UserList where EmailId = @emailId)))
	End

	select @IsPasswordReset
 
End  
  