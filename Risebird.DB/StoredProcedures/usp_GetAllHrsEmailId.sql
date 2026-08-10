IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAllHrsEmailId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAllHrsEmailId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetAllHrsEmailId]
AS
BEGIN
    SELECT Emailid FROM [dbo].[UserList] WHERE idprofile = 2
END
GO
