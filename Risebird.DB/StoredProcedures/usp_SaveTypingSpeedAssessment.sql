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
	@PassingWPM DECIMAL(10,2) = 30.00,
	@PassingAccuracyPercentage DECIMAL(5,2) = 97.00,
	@IsPassed BIT = NULL,
	@CreatedBy VARCHAR(150) = NULL
AS
BEGIN
    SET NOCOUNT ON;

	-- Calculate pass/fail if not provided
	IF @IsPassed IS NULL
	BEGIN
		SET @IsPassed = CASE 
			WHEN @WordsPerMinute >= @PassingWPM AND @AccuracyPercentage >= @PassingAccuracyPercentage 
			THEN 1 
			ELSE 0 
		END
	END

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
		PassingWPM,
		PassingAccuracyPercentage,
		IsPassed,
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
		@PassingWPM,
		@PassingAccuracyPercentage,
		@IsPassed,
		GETDATE(),
		@CreatedBy
	);

	SELECT SCOPE_IDENTITY() AS NewId;
END;
GO
