IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_ResetAIQCAttempted'))
BEGIN
    DROP PROCEDURE dbo.usp_ResetAIQCAttempted
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_ResetAIQCAttempted]
    @interviewId int
AS
BEGIN
    UPDATE InterviewComments
    SET IsAIQCAtempted = 0
    WHERE InterviewId = @interviewId
      AND OpenAIQCResponse IS NULL
END
GO
