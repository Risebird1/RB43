IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddChatGroupUser'))
BEGIN
    DROP PROCEDURE dbo.usp_AddChatGroupUser
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_AddChatGroupUser]
    @GroupId VARCHAR(50),
    @Id INT
AS
BEGIN
    INSERT INTO ChatGroupUsers VALUES (@GroupId, @Id)
END
GO
