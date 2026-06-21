IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddChatGroupName'))
BEGIN
    DROP PROCEDURE dbo.usp_AddChatGroupName
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_AddChatGroupName]
    @GroupName VARCHAR(200)
AS
BEGIN
    INSERT INTO ChatGroupName VALUES (@GroupName)
END
GO
