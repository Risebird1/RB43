




IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetUserThemeByEmail'))
BEGIN
    DROP PROCEDURE dbo.usp_GetUserThemeByEmail
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetUserThemeByEmail]
    @id VARCHAR(255)
AS
BEGIN
    SELECT Usertheme FROM UserList WHERE EmailId = @id
END
GO
