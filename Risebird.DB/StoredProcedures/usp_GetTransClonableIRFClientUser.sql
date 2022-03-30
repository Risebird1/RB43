IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetTransClonableIRFClientUser'))
BEGIN
    DROP PROCEDURE dbo.usp_GetTransClonableIRFClientUser
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetTransClonableIRFClientUser]

@id int

AS
Begin

select a.*, ClientRole.[Name] as [Role] from (select TransClonableIRFClientUser.*, UserList.EmailId from TransClonableIRFClientUser 
inner join UserList on TransClonableIRFClientUser.ClientUserId = UserList.id) as a 
inner join ClientRole on ClientRole.id = a.ClientRoleId where ClonableIRFId = @id


End

GO


