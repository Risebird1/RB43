IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SetInterviewRequestIRFCreated'))
BEGIN
    DROP PROCEDURE dbo.usp_SetInterviewRequestIRFCreated
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_SetInterviewRequestIRFCreated]
    @id INT,
    @IRFId VARCHAR(200)
AS
BEGIN
    UPDATE InterviewRequests SET IsIRFCreated = 1, IRFId = @IRFId WHERE Id = @id
END
GO
