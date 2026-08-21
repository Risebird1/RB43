IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetAIQuestionSetsByIRF'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAIQuestionSetsByIRF
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
-- Backs the Delivery Manager's read of a channel's papers. Returns nothing for a channel that is
-- not a preset AI interview channel, regardless of who is calling -- the third of the three
-- confidentiality guards, and the one that does not depend on a view or a script behaving.
Create PROCEDURE [dbo].[usp_GetAIQuestionSetsByIRF]
	@IRFId int
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM IRF
                   WHERE id = @IRFId
                     AND ISNULL(IsAIInterview, 0) = 1
                     AND ISNULL(AIInterviewType, 0) = 2)
        RETURN;

    SELECT s.id AS SetId,
           s.IRFId,
           s.SetName,
           ISNULL(s.TotalMinutes, 0) AS TotalMinutes,
           ISNULL(s.TotalPoints, 0) AS TotalPoints,
           s.PromptId,
           s.PromptVersion,
           s.CreatedBy,
           s.CreatedOn,
           (SELECT COUNT(*) FROM InterviewAIQuestionSet x WHERE x.SetId = s.id) AS AssignedInterviews
    FROM AIQuestionSets s
    WHERE s.IRFId = @IRFId
    ORDER BY s.SetName;

    -- Identity order IS ask order.
    SELECT i.id AS ItemId,
           i.SetId,
           s.SetName,
           i.QuestionId,
           i.SkillName,
           q.Comment AS QuestionText
    FROM AIQuestionSetItems i
    JOIN AIQuestionSets s ON s.id = i.SetId
    LEFT JOIN SkillChannelSampleQuestions q ON q.id = i.QuestionId
    WHERE s.IRFId = @IRFId
    ORDER BY s.SetName, i.id;
END
GO
