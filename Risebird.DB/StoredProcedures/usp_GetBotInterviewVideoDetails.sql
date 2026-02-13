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

select biqv.*, sq.IsFlaggedForSentimentAnalysis from BotInterviewQuestionVideos biqv 
join SkillChannelSampleQuestions sq on sq.id = biqv.DeliveryHeadQuestionId
where SessionId = @SessionId and (sq.IsTypingSpeedAssessment is null or sq.IsTypingSpeedAssessment = 0)
and (sq.IsListeningComprehension is null or sq.IsListeningComprehension = 0)
order by CreatedAt 

END
GO