

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetFirstNameByEmail'))
BEGIN
    DROP PROCEDURE dbo.usp_GetFirstNameByEmail
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetFirstNameByEmail]
@Emailid VARCHAR(255)
AS
BEGIN
   SELECT FirstName FROM UserList WHERE Emailid = @Emailid
END
GO
