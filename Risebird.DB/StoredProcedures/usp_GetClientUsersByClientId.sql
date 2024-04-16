/****** Object:  StoredProcedure [dbo].[usp_GetClientUsersByClientId]     ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetClientUsersByClientId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetClientUsersByClientId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetClientUsersByClientId]

@clientId int

AS
Begin


select ul.EmailId from ClientUser cu join UserList ul on ul.id = cu.UserId where cu.ClientId=@clientId and ul.IsActive = 1 and cu.IsEmailNotification =1


End
GO


