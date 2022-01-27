/****** Object:  StoredProcedure [dbo].[usp_GetClientPasswordResetEnableStatus]   ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetClientPasswordResetEnableStatus'))
BEGIN
    DROP PROCEDURE dbo.usp_GetClientPasswordResetEnableStatus
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_GetClientPasswordResetEnableStatus]
@clientId int

AS
Begin

 select IsPasswordReset from Client where id = @clientId

END

GO


