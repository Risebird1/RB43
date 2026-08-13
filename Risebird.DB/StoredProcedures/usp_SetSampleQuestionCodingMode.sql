IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SetSampleQuestionCodingMode'))
BEGIN
    DROP PROCEDURE dbo.usp_SetSampleQuestionCodingMode
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Toggles coding mode on an existing question without touching its text, so questions
-- already written (including AI generated ones) can be converted in place.
CREATE PROCEDURE [dbo].[usp_SetSampleQuestionCodingMode]
    @id int,
    @isCodingQuestion bit = 0,
    @codingTimeLimitInMinutes int = NULL,
    @questionGist nvarchar(max) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE SkillChannelSampleQuestions
    SET IsCodingQuestion = ISNULL(@isCodingQuestion, 0),
        CodingTimeLimitInMinutes = CASE WHEN ISNULL(@isCodingQuestion, 0) = 1 THEN @codingTimeLimitInMinutes ELSE NULL END,
        -- The gist survives un-ticking coding mode: it is prose the user typed, and
        -- re-ticking should not force them to write it again.
        QuestionGist = ISNULL(@questionGist, QuestionGist)
    WHERE id = @id;
END
GO
