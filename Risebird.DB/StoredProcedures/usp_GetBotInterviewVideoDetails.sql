IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetBotInterviewVideoDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_GetBotInterviewVideoDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_GetBotInterviewVideoDetails

@SessionId varchar(50)
AS
BEGIN

-- LEFT JOIN instead of INNER JOIN so that Adaptive/OpenAI rows (DeliveryHeadQuestionId IS NULL)
-- are not silently dropped. SkillChannelSampleQuestions only applies to the Preset flow (AIInterviewType = 2).
-- For Adaptive sessions (AIInterviewType = 1) DeliveryHeadQuestionId is always NULL, so sq columns
-- will be NULL after the left join -- that is expected and correct.
select biqv.*, sq.IsFlaggedForSentimentAnalysis from BotInterviewQuestionVideos biqv 
left join SkillChannelSampleQuestions sq on sq.id = biqv.DeliveryHeadQuestionId
where biqv.SessionId = @SessionId
and (
    biqv.DeliveryHeadQuestionId IS NULL  -- Adaptive flow: no preset question, include all rows
    OR (
        (sq.IsTypingSpeedAssessment is null or sq.IsTypingSpeedAssessment = 0)
        and (sq.IsListeningComprehension is null or sq.IsListeningComprehension = 0)
    )
)
order by biqv.CreatedAt 

END
GO