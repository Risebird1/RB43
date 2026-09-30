IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_PeekNextCodeProjectQuestionForSession'))
BEGIN
    DROP PROCEDURE dbo.usp_PeekNextCodeProjectQuestionForSession
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_PeekNextCodeProjectQuestionForSession
    @interviewId int,
    @irfId int,
    @currentQuestionId int
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @SetId int = (SELECT SetId FROM InterviewAIQuestionSet WHERE InterviewId = @interviewId);

    -- Mirror whichever picker serves this interview (the bound set, else the channel pool),
    -- with the same ordering and filter, then take the question right after the current one.
    ;WITH OrderedQuestions AS (
        SELECT q.id, ISNULL(q.IsCodeProjectQuestion, 0) AS IsCodeProjectQuestion, cpd.StackImageCode,
               ROW_NUMBER() OVER (ORDER BY i.id) AS RowNum
        FROM AIQuestionSetItems i
        JOIN SkillChannelSampleQuestions q ON q.id = i.QuestionId
        LEFT JOIN CodeProjectDetails cpd ON cpd.QuestionId = q.id
        WHERE @SetId IS NOT NULL AND i.SetId = @SetId
          AND (ISNULL(q.IsCodeProjectQuestion, 0) = 0 OR cpd.ValidationStatus = 'passed')
        UNION ALL
        SELECT q.id, ISNULL(q.IsCodeProjectQuestion, 0), cpd.StackImageCode,
               ROW_NUMBER() OVER (ORDER BY q.Id)
        FROM SkillChannelSampleQuestions q
        LEFT JOIN CodeProjectDetails cpd ON cpd.QuestionId = q.id
        WHERE @SetId IS NULL AND q.IRFId = @irfId
          AND (ISNULL(q.IsCodeProjectQuestion, 0) = 0 OR cpd.ValidationStatus = 'passed')
    )
    SELECT nxt.id AS QuestionId, nxt.StackImageCode
    FROM OrderedQuestions cur
    JOIN OrderedQuestions nxt ON nxt.RowNum = cur.RowNum + 1
    WHERE cur.id = @currentQuestionId AND nxt.IsCodeProjectQuestion = 1;
END
GO
