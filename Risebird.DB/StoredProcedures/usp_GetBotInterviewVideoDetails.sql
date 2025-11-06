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
where SessionId = @SessionId
order by CreatedAt 

END
GO