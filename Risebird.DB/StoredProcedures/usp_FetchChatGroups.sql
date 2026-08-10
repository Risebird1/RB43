IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchChatGroups'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchChatGroups
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_FetchChatGroups]
AS
BEGIN
    SELECT id, GroupName FROM ChatGroupName
END
GO
