IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateUserLastLogin'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateUserLastLogin
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_UpdateUserLastLogin] (@LastLogin DATETIME, @EmailId VARCHAR(255))
AS BEGIN UPDATE UserList SET LastLogin = @LastLogin WHERE EmailId = @EmailId END
GO
