IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetUserIdByEmailId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetUserIdByEmailId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetUserIdByEmailId]
    @Emailid VARCHAR(200)
AS
BEGIN
    SELECT Id FROM UserList WHERE Emailid = @Emailid
END
GO
