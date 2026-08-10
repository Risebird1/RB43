IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateResumeEvaluatorUserList'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateResumeEvaluatorUserList
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_UpdateResumeEvaluatorUserList]
    (@id INT, @FirstName VARCHAR(255), @LastName VARCHAR(255), @EmailId VARCHAR(255), @MobileNumber VARCHAR(50))
AS
BEGIN
    UPDATE UserList
    SET FirstName = @FirstName, LastName = @LastName, EmailId = @EmailId, MobileNumber = @MobileNumber
    WHERE id = @id
END
GO
