IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_DeleteChatGroupUsersByGroupId'))
BEGIN
    DROP PROCEDURE dbo.usp_DeleteChatGroupUsersByGroupId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_DeleteChatGroupUsersByGroupId]
    @Id VARCHAR(50)
AS
BEGIN
    DELETE FROM ChatGroupUsers WHERE Groupid = @Id
END
GO
