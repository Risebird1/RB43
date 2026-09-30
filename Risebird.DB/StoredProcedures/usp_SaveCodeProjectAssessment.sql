IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_SaveCodeProjectAssessment'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveCodeProjectAssessment
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_SaveCodeProjectAssessment
    @SessionId VARCHAR(50),
    @QuestionId INT,
    @QuestionText NVARCHAR(MAX) = NULL,
    @CodeLanguage VARCHAR(50) = NULL,
    @SubmittedCode NVARCHAR(MAX) = NULL,     -- the candidate's diff against the baseline commit
    @TimeLimitInMinutes INT = NULL,
    @TimeStarted DATETIME = NULL,
    @TimeCompleted DATETIME = NULL,
    @TimeTakenInSeconds DECIMAL(10,2) = NULL,
    @IsTimeExpired BIT = 0,
    @VMPropertyId INT = NULL,
    @RequirementsPassed INT = NULL,
    @RequirementsTotal INT = NULL,
    @ResultsJson NVARCHAR(MAX) = NULL,
    @CreatedBy VARCHAR(150) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ExistingId INT;
    SELECT @ExistingId = id FROM CodingAssessment
    WHERE SessionId = @SessionId AND QuestionId = @QuestionId;

    IF @ExistingId IS NOT NULL
    BEGIN
        UPDATE CodingAssessment
        SET QuestionText = ISNULL(@QuestionText, QuestionText),
            CodeLanguage = ISNULL(@CodeLanguage, CodeLanguage),
            SubmittedCode = ISNULL(@SubmittedCode, SubmittedCode),
            TimeLimitInMinutes = ISNULL(@TimeLimitInMinutes, TimeLimitInMinutes),
            TimeStarted = ISNULL(@TimeStarted, TimeStarted),
            TimeCompleted = ISNULL(@TimeCompleted, TimeCompleted),
            TimeTakenInSeconds = ISNULL(@TimeTakenInSeconds, TimeTakenInSeconds),
            IsTimeExpired = ISNULL(@IsTimeExpired, IsTimeExpired),
            VMPropertyId = ISNULL(@VMPropertyId, VMPropertyId),
            RequirementsPassed = ISNULL(@RequirementsPassed, RequirementsPassed),
            RequirementsTotal = ISNULL(@RequirementsTotal, RequirementsTotal),
            ResultsJson = ISNULL(@ResultsJson, ResultsJson)
        WHERE id = @ExistingId;
        SELECT @ExistingId AS NewId;
    END
    ELSE
    BEGIN
        INSERT INTO CodingAssessment (
            SessionId, QuestionId, QuestionText, CodeLanguage, SubmittedCode,
            TimeLimitInMinutes, TimeStarted, TimeCompleted, TimeTakenInSeconds, IsTimeExpired,
            VMPropertyId, RequirementsPassed, RequirementsTotal, ResultsJson, CreatedAt, CreatedBy)
        VALUES (
            @SessionId, @QuestionId, @QuestionText, @CodeLanguage, @SubmittedCode,
            @TimeLimitInMinutes, @TimeStarted, @TimeCompleted, @TimeTakenInSeconds, @IsTimeExpired,
            @VMPropertyId, @RequirementsPassed, @RequirementsTotal, @ResultsJson, GETDATE(), @CreatedBy);
        SELECT CAST(SCOPE_IDENTITY() AS int) AS NewId;
    END
END
GO
