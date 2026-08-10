IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_IsUserActiveByEmail'))
BEGIN
    DROP PROCEDURE dbo.usp_IsUserActiveByEmail
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_IsUserActiveByEmail]
    @EmailId VARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT IsActive FROM dbo.UserList WHERE EmailId = @EmailId;
END
GO
