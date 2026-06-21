IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_DeleteChatGroupUser'))
BEGIN
    DROP PROCEDURE dbo.usp_DeleteChatGroupUser
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_DeleteChatGroupUser]
    @Id VARCHAR(50)
AS
BEGIN
    DELETE FROM ChatGroupUsers WHERE Id = @Id
END
GO
