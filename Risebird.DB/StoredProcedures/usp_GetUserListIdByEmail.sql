



IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetUserListIdByEmail'))
BEGIN
    DROP PROCEDURE dbo.usp_GetUserListIdByEmail
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetUserListIdByEmail]
    @Emailid VARCHAR(255)
AS
BEGIN
    SELECT * FROM UserList WHERE Emailid = @Emailid
END
GO
