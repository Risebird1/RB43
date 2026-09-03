IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_SetSampleQuestionMcqSectionMode'))
BEGIN
    DROP PROCEDURE dbo.usp_SetSampleQuestionMcqSectionMode
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Turns the MCQ assessment section on or off for one existing question, from the Delivery Manager
-- questions panel. Mirrors usp_SetSampleQuestionCodingMode.
--
-- Switching it on clears the coding settings on that row: the MCQ section is served from
-- screen.risebird.io and is never asked by the avatar, so it cannot also be a coding question.
-- Only one row per channel may carry it, which the caller enforces before getting here.
CREATE PROCEDURE [dbo].[usp_SetSampleQuestionMcqSectionMode]
    @id int,
    @isMcqAssessmentSection bit
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE SkillChannelSampleQuestions
       SET IsMcqAssessmentSection = ISNULL(@isMcqAssessmentSection, 0),
           IsCodingQuestion = CASE WHEN ISNULL(@isMcqAssessmentSection, 0) = 1 THEN 0 ELSE IsCodingQuestion END,
           CodingTimeLimitInMinutes = CASE WHEN ISNULL(@isMcqAssessmentSection, 0) = 1 THEN NULL ELSE CodingTimeLimitInMinutes END,
           IsTypingSpeedAssessment = CASE WHEN ISNULL(@isMcqAssessmentSection, 0) = 1 THEN 0 ELSE IsTypingSpeedAssessment END,
           IsListeningComprehension = CASE WHEN ISNULL(@isMcqAssessmentSection, 0) = 1 THEN 0 ELSE IsListeningComprehension END
     WHERE id = @id;

    SELECT @@ROWCOUNT AS RowsUpdated;
END
GO
