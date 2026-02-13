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

    -- Use CTE with ROW_NUMBER to get only the most recent record per QuestionId
    WITH RankedAssessments AS (
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
            ROW_NUMBER() OVER (PARTITION BY tsa.QuestionId ORDER BY tsa.CreatedAt DESC) AS RowNum
        FROM TypingSpeedAssessment tsa
        WHERE tsa.SessionId = @SessionId
    ),
    RankedVideos AS (
        SELECT 
            bv.SessionId,
            bv.DeliveryHeadQuestionId,
            bv.VideoLink,
            bv.question,
            ROW_NUMBER() OVER (PARTITION BY bv.DeliveryHeadQuestionId ORDER BY bv.CreatedAt DESC) AS RowNum
        FROM BotInterviewQuestionVideos bv
        WHERE bv.SessionId = @SessionId
    )
    SELECT 
        ra.id,
        ra.SessionId,
        ra.QuestionId,
        ra.SampleText,
        ra.TypedText,
        ra.TimeStarted,
        ra.TimeCompleted,
        ra.TimeTakenInSeconds,
        ra.TotalCharacters,
        ra.CorrectCharacters,
        ra.AccuracyPercentage,
        ra.WordsPerMinute,
        ra.PassingWPM,
        ra.PassingAccuracyPercentage,
        ra.IsPassed,
        ra.OpenAIAnalysisJson,
        ra.OpenAIThreadId,
        ra.OpenAIRunId,
        ra.CreatedAt,
        ra.CreatedBy,
        -- Get video link from BotInterviewQuestionVideos (most recent only)
        rv.VideoLink,
        rv.question AS QuestionText
    FROM RankedAssessments ra
    LEFT JOIN RankedVideos rv 
        ON ra.SessionId = rv.SessionId 
        AND ra.QuestionId = rv.DeliveryHeadQuestionId
        AND rv.RowNum = 1  -- Only join the most recent video record
    WHERE ra.RowNum = 1  -- Only return the most recent assessment per QuestionId
    ORDER BY ra.CreatedAt DESC;
END
GO
