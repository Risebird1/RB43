IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetChatGroupUserCount'))
BEGIN
    DROP PROCEDURE dbo.usp_GetChatGroupUserCount
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetChatGroupUserCount]
    @Id INT,
    @GroupId VARCHAR(50)
AS
BEGIN
    SELECT COUNT(*) FROM ChatGroupUsers WHERE Userid = @Id AND Groupid = @GroupId
END
GO
