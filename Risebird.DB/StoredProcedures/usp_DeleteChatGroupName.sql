IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_DeleteChatGroupName'))
BEGIN
    DROP PROCEDURE dbo.usp_DeleteChatGroupName
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_DeleteChatGroupName]
    @Id VARCHAR(50)
AS
BEGIN
    DELETE FROM ChatGroupName WHERE Id = @Id
END
GO
