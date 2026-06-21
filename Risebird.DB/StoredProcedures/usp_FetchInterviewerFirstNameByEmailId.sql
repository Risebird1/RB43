IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchInterviewerFirstNameByEmailId'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchInterviewerFirstNameByEmailId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_FetchInterviewerFirstNameByEmailId]
    @id VARCHAR(200)
AS
BEGIN
    SELECT FirstName FROM UserList WHERE Emailid = @id
END
GO
