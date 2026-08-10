IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_ClearSessionForOthersUsers'))
BEGIN
    DROP PROCEDURE dbo.usp_ClearSessionForOthersUsers
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_ClearSessionForOthersUsers] (@emailId VARCHAR(255))
AS
BEGIN
    UPDATE uss
    SET IsActive = 0, Logout = GETUTCDATE()
    FROM [dbo].[UserSessions] uss
    JOIN UserList us ON uss.UserId = us.id
    WHERE us.EmailId = @emailId
END
GO
