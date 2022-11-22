
IF EXISTS (SELECT 1 FROM sys.triggers 
           WHERE Name = 'tr_UserList_ForUpdate')
BEGIN
    Drop trigger tr_UserList_ForUpdate
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TRIGGER tr_UserList_ForUpdate
ON UserList
FOR UPDATE
AS
BEGIN
 Declare @id int
 Select @id = Id from inserted
 
 insert into UserListHistory 
 (id,	idProfile,	FirstName,	LastName,	EmailId,	LastLogin,	LastLogout, IsActive,
 ImageURL,	MobileNumber, Isonline, Usertheme, UserPassword, WhatsappNumber, PasswordCreation, uniqueid, AadhaarVerified, ActionType, 
 PerformedAt)

 (select id, idProfile,	FirstName,	LastName,	EmailId,	LastLogin,	LastLogout, IsActive,
 ImageURL,	MobileNumber, Isonline, Usertheme, UserPassword, WhatsappNumber, PasswordCreation, uniqueid, AadhaarVerified,
 'Updated', DATEADD(MINUTE, 330, getdate())
 from UserList where id = @id)
END