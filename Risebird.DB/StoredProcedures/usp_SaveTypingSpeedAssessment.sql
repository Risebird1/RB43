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
	@OpenAIAnalysisJson NVARCHAR(MAX) = NULL,
	@OpenAIThreadId VARCHAR(100) = NULL,
	@OpenAIRunId VARCHAR(100) = NULL,
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

	-- Check if a record already exists for this SessionId + QuestionId combination
	DECLARE @ExistingId INT;
	SELECT @ExistingId = id 
	FROM TypingSpeedAssessment 
	WHERE SessionId = @SessionId AND QuestionId = @QuestionId;

	IF @ExistingId IS NOT NULL
	BEGIN
		-- Update existing record (prevents duplicates from resubmissions)
		UPDATE TypingSpeedAssessment
		SET 
			SampleText = @SampleText,
			TypedText = @TypedText,
			TimeStarted = @TimeStarted,
			TimeCompleted = @TimeCompleted,
			TimeTakenInSeconds = @TimeTakenInSeconds,
			TotalCharacters = @TotalCharacters,
			CorrectCharacters = @CorrectCharacters,
			AccuracyPercentage = @AccuracyPercentage,
			WordsPerMinute = @WordsPerMinute,
			PassingWPM = @PassingWPM,
			PassingAccuracyPercentage = @PassingAccuracyPercentage,
			IsPassed = @IsPassed,
			OpenAIAnalysisJson = @OpenAIAnalysisJson,
			OpenAIThreadId = @OpenAIThreadId,
			OpenAIRunId = @OpenAIRunId,
			CreatedBy = @CreatedBy
		WHERE id = @ExistingId;

		SELECT @ExistingId AS NewId;
	END
	ELSE
	BEGIN
		-- Insert new record
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
			OpenAIAnalysisJson,
			OpenAIThreadId,
			OpenAIRunId,
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
			@OpenAIAnalysisJson,
			@OpenAIThreadId,
			@OpenAIRunId,
			GETDATE(),
			@CreatedBy
		);

		SELECT SCOPE_IDENTITY() AS NewId;
	END
END;
GO
