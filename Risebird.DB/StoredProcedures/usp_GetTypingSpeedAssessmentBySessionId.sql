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
        tsa.id,
        tsa.SessionId,
        tsa.QuestionId,
        tsa.SampleText,
        tsa.TypedText,
        tsa.TimeStarted,
        tsa.TimeCompleted,
        tsa.TimeTakenInSeconds,
        tsa.TotalCharacters,
        tsa.CorrectCharacters,
        tsa.AccuracyPercentage,
        tsa.WordsPerMinute,
        tsa.PassingWPM,
        tsa.PassingAccuracyPercentage,
        tsa.IsPassed,
        tsa.OpenAIAnalysisJson,
        tsa.OpenAIThreadId,
        tsa.OpenAIRunId,
        tsa.CreatedAt,
        tsa.CreatedBy,
        -- Get video link from BotInterviewQuestionVideos
        bv.VideoLink,
        bv.question AS QuestionText
    FROM TypingSpeedAssessment tsa
    LEFT JOIN BotInterviewQuestionVideos bv 
        ON tsa.SessionId = bv.SessionId 
        AND tsa.QuestionId = bv.DeliveryHeadQuestionId
    WHERE tsa.SessionId = @SessionId
    ORDER BY tsa.CreatedAt DESC;
END
GO
