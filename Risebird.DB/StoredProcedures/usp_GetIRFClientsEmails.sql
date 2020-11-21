/****** Object:  StoredProcedure [dbo].[usp_GetIRFMembersEmails]    Script Date: 10-11-2020 1.16.55 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('sp_GetIRFClientsEmails'))
BEGIN
    DROP PROCEDURE dbo.sp_GetIRFClientsEmails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_GetIRFClientsEmails]
@IRFId int 

AS
Begin

		
	Begin

	select EmailId from TransClientUser tcu join UserList ul on ul.id = tcu.ClientUserId  where IRFId = @IRFId and (ClientRoleId=1 or ClientRoleId=2)

	End
		


End

GO


