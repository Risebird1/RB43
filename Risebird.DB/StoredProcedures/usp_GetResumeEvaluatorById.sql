IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetResumeEvaluatorById'))
BEGIN
    DROP PROCEDURE dbo.usp_GetResumeEvaluatorById
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetResumeEvaluatorById] (@id INT)
AS
BEGIN
    SELECT re.*, ul.FirstName, ul.LastName, ul.EmailId, ul.MobileNumber
    FROM ResumeEvaluator re
    JOIN UserList ul ON re.UserId = ul.id
    WHERE re.id = @id
END
GO
