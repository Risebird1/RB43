IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetInterviewQuestionsDiscussed'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewQuestionsDiscussed
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetInterviewQuestionsDiscussed]
    @InterviewId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        qd.id,
        qd.InterviewId,
        qd.Comment,
        qd.SkillName,
        qd.RatingId,
        qd.Timing,
        vc.VideoLink,
        vc.Priority,
        qd.SkillSet
    FROM dbo.QuestionsDiscussed qd
    LEFT JOIN dbo.VideoQuestionDetails vc
        ON vc.Question = qd.Comment
       AND vc.InterviewId = @InterviewId
    WHERE qd.InterviewId = @InterviewId;
END
GO
