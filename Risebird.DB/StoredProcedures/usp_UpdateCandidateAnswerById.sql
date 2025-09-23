IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateCandidateAnswerById'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateCandidateAnswerById
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_UpdateCandidateAnswerById
@questionId int,
@updatedAnswer varchar(max) = null,
@timeNow datetime = null
AS
BEGIN

update BotInterviewQuestionVideos set CandidateAnswer = @updatedAnswer, AnswerModifiedAt = @timeNow where id =@questionId

END
GO



