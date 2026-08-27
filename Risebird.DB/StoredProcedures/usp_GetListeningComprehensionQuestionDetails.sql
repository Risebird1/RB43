IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetListeningComprehensionQuestionDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_GetListeningComprehensionQuestionDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Fetches the audio transcription and expected answer for a listening comprehension
-- question at scoring time. Kept as a server-side lookup on purpose: the expected answer
-- must never be round-tripped through the candidate's browser, where it would be
-- readable in the page source.
CREATE PROCEDURE usp_GetListeningComprehensionQuestionDetails
	@QuestionId INT
AS
BEGIN
    SET NOCOUNT ON;

	SELECT
		id,
		Comment AS [Message],
		ISNULL(IsListeningComprehension, 0) AS IsListeningComprehension,
		ListeningAudioUrl,
		ListeningAudioTranscription,
		ExpectedAnswer,
		QuestionGist
	FROM SkillChannelSampleQuestions
	WHERE id = @QuestionId;
END;
GO
