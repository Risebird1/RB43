IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_CheckMultiSession'))
BEGIN
    DROP PROCEDURE dbo.usp_CheckMultiSession
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_CheckMultiSession] (@EmailId VARCHAR(255))
AS
BEGIN
    SELECT TOP 1 uss.IsActive
    FROM [dbo].[UserSessions] uss
    JOIN UserList us ON uss.UserId = us.id
    WHERE us.EmailId = @EmailId
    ORDER BY uss.Id DESC
END
GO
