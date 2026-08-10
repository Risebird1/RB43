IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchChatGroupUsers'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchChatGroupUsers
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_FetchChatGroupUsers]
    @Id VARCHAR(50)
AS
BEGIN
    SELECT cg.id, ul.Emailid
    FROM ChatGroupUsers cg
    JOIN UserList ul ON cg.Userid = ul.id
    WHERE GroupId = @Id
END
GO
