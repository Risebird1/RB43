IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_SaveTypingSpeedAssessment'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveTypingSpeedAssessment
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_SaveTypingSpeedAssessment
	@SessionId VARCHAR(50),
	@QuestionId INT,
	@SampleText NVARCHAR(MAX),
	@TypedText NVARCHAR(MAX),
	@TimeStarted DATETIME,
	@TimeCompleted DATETIME,
	@TimeTakenInSeconds DECIMAL(10,2),
	@TotalCharacters INT,
	@CorrectCharacters INT,
	@AccuracyPercentage DECIMAL(5,2),
	@WordsPerMinute DECIMAL(10,2),
	@CreatedBy VARCHAR(150) = NULL
AS
BEGIN
    SET NOCOUNT ON;

	INSERT INTO TypingSpeedAssessment (
		SessionId,
		QuestionId,
		SampleText,
		TypedText,
		TimeStarted,
		TimeCompleted,
		TimeTakenInSeconds,
		TotalCharacters,
		CorrectCharacters,
		AccuracyPercentage,
		WordsPerMinute,
		CreatedAt,
		CreatedBy
	)
	VALUES (
		@SessionId,
		@QuestionId,
		@SampleText,
		@TypedText,
		@TimeStarted,
		@TimeCompleted,
		@TimeTakenInSeconds,
		@TotalCharacters,
		@CorrectCharacters,
		@AccuracyPercentage,
		@WordsPerMinute,
		GETDATE(),
		@CreatedBy
	)

	SELECT SCOPE_IDENTITY() AS NewId;
END;
GO
