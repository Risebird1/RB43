IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AssignInterviewRequest'))
BEGIN
    DROP PROCEDURE dbo.usp_AssignInterviewRequest
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_AssignInterviewRequest]
    @id INT,
    @Assignto INT
AS
BEGIN
    UPDATE InterviewRequests SET Assignto = @Assignto WHERE Id = @id
END
GO
