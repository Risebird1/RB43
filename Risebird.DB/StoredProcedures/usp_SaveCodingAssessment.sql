IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_SaveCodingAssessment'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveCodingAssessment
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_SaveCodingAssessment
	@SessionId VARCHAR(50),
	@QuestionId INT,
	@QuestionText NVARCHAR(MAX) = NULL,
	@CodeLanguage VARCHAR(50) = NULL,
	@SubmittedCode NVARCHAR(MAX) = NULL,
	@TimeLimitInMinutes INT = NULL,
	@TimeStarted DATETIME = NULL,
	@TimeCompleted DATETIME = NULL,
	@TimeTakenInSeconds DECIMAL(10,2) = NULL,
	@IsTimeExpired BIT = 0,
	@CreatedBy VARCHAR(150) = NULL
AS
BEGIN
    SET NOCOUNT ON;

	-- Check if a record already exists for this SessionId + QuestionId combination
	DECLARE @ExistingId INT;
	SELECT @ExistingId = id 
	FROM CodingAssessment 
	WHERE SessionId = @SessionId AND QuestionId = @QuestionId;

	IF @ExistingId IS NOT NULL
	BEGIN
		-- Update existing record (prevents duplicates from resubmissions)
		UPDATE CodingAssessment
		SET 
			QuestionText = @QuestionText,
			CodeLanguage = @CodeLanguage,
			SubmittedCode = @SubmittedCode,
			TimeLimitInMinutes = @TimeLimitInMinutes,
			TimeStarted = @TimeStarted,
			TimeCompleted = @TimeCompleted,
			TimeTakenInSeconds = @TimeTakenInSeconds,
			IsTimeExpired = @IsTimeExpired,
			CreatedBy = @CreatedBy
		WHERE id = @ExistingId;

		SELECT @ExistingId AS NewId;
	END
	ELSE
	BEGIN
		INSERT INTO CodingAssessment (
			SessionId,
			QuestionId,
			QuestionText,
			CodeLanguage,
			SubmittedCode,
			TimeLimitInMinutes,
			TimeStarted,
			TimeCompleted,
			TimeTakenInSeconds,
			IsTimeExpired,
			CreatedAt,
			CreatedBy
		)
		VALUES (
			@SessionId,
			@QuestionId,
			@QuestionText,
			@CodeLanguage,
			@SubmittedCode,
			@TimeLimitInMinutes,
			@TimeStarted,
			@TimeCompleted,
			@TimeTakenInSeconds,
			@IsTimeExpired,
			GETDATE(),
			@CreatedBy
		);

		SELECT SCOPE_IDENTITY() AS NewId;
	END
END;
GO
