IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateUserTheme'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateUserTheme
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_UpdateUserTheme]
    @id VARCHAR(200),
    @Usertheme VARCHAR(200)
AS
BEGIN
    UPDATE UserList SET Usertheme = @Usertheme WHERE EmailId = @id
END
GO
