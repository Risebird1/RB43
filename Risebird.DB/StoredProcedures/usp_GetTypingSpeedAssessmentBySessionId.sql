-- =============================================
-- Stored Procedure: usp_GetTypingSpeedAssessmentBySessionId
-- Purpose: Fetches typing speed assessment data for Report display
-- Used by: AIInterviewController.Report -> GetTypingSpeedAssessmentBySessionId
-- =============================================

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetTypingSpeedAssessmentBySessionId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetTypingSpeedAssessmentBySessionId
END
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE dbo.usp_GetTypingSpeedAssessmentBySessionId
    @SessionId VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        id,
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
    FROM TypingSpeedAssessment
    WHERE SessionId = @SessionId
    ORDER BY CreatedAt DESC;
END
GO
