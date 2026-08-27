IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_SaveListeningComprehensionAssessment'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveListeningComprehensionAssessment
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_SaveListeningComprehensionAssessment
	@SessionId VARCHAR(50),
	@QuestionId INT,
	@AudioUrl VARCHAR(MAX),
	@AudioTranscription NVARCHAR(MAX),
	@CandidateSummary NVARCHAR(MAX),
	@TimeStarted DATETIME,
	@TimeCompleted DATETIME,
	@TimeTakenInSeconds DECIMAL(10,2),
	@CreatedBy VARCHAR(150) = NULL,
	@ExpectedAnswer NVARCHAR(MAX) = NULL,
	@AudioPlayCount INT = NULL,
	@IsTimeExpired BIT = 0,
	@Score DECIMAL(5,2) = NULL,
	@MaxScore DECIMAL(5,2) = NULL,
	@IsPassed BIT = NULL,
	@OpenAIAnalysisJson NVARCHAR(MAX) = NULL,
	@OpenAIResponseId VARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

	INSERT INTO ListeningComprehensionAssessment (
		SessionId,
		QuestionId,
		AudioUrl,
		AudioTranscription,
		CandidateSummary,
		ExpectedAnswer,
		TimeStarted,
		TimeCompleted,
		TimeTakenInSeconds,
		AudioPlayCount,
		IsTimeExpired,
		Score,
		MaxScore,
		IsPassed,
		OpenAIAnalysisJson,
		OpenAIResponseId,
		CreatedAt,
		CreatedBy
	)
	VALUES (
		@SessionId,
		@QuestionId,
		@AudioUrl,
		@AudioTranscription,
		@CandidateSummary,
		@ExpectedAnswer,
		@TimeStarted,
		@TimeCompleted,
		@TimeTakenInSeconds,
		@AudioPlayCount,
		@IsTimeExpired,
		@Score,
		@MaxScore,
		@IsPassed,
		@OpenAIAnalysisJson,
		@OpenAIResponseId,
		GETDATE(),
		@CreatedBy
	)

	SELECT SCOPE_IDENTITY() AS NewId;
END;
GO
