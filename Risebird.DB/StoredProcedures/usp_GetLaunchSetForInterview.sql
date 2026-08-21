IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetLaunchSetForInterview'))
BEGIN
    DROP PROCEDURE dbo.usp_GetLaunchSetForInterview
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
-- Read-only: returns the set already bound to an interview, plus its questions in ask order.
-- Binding is done by usp_AssignAIQuestionSetToInterview; this procedure never assigns.
--
-- Both result sets come back empty when the interview has no bound set. The caller must fail
-- cleanly in that case and must never widen the query to all of the channel's questions.
Create PROCEDURE [dbo].[usp_GetLaunchSetForInterview]
	@InterviewId int
AS
BEGIN
    SET NOCOUNT ON;

    SELECT ias.InterviewId,
           s.id AS SetId,
           s.IRFId,
           s.SetName,
           ISNULL(s.TotalMinutes, 0) AS TotalMinutes,
           ISNULL(s.TotalPoints, 0) AS TotalPoints,
           s.PromptId,
           s.PromptVersion,
           ias.AssignedOn
    FROM InterviewAIQuestionSet ias
    JOIN AIQuestionSets s ON s.id = ias.SetId
    WHERE ias.InterviewId = @InterviewId;

    -- AIQuestionSetItems identity order IS ask order. Do not re-sort this payload.
    SELECT i.id AS ItemId,
           i.QuestionId,
           i.SkillName,
           q.Comment AS QuestionText
    FROM InterviewAIQuestionSet ias
    JOIN AIQuestionSetItems i ON i.SetId = ias.SetId
    LEFT JOIN SkillChannelSampleQuestions q ON q.id = i.QuestionId
    WHERE ias.InterviewId = @InterviewId
    ORDER BY i.id;
END
GO
