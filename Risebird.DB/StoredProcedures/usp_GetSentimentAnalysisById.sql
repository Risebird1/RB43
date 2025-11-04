IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetSentimentAnalysisById'))
BEGIN
    DROP PROCEDURE dbo.usp_GetSentimentAnalysisById
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_GetSentimentAnalysisById
@biqvId int
AS
BEGIN

select vsa.*, biqv.VideoLink as VideoUrl from VideoSentimentAnalysis vsa
join BotInterviewQuestionVideos biqv  on biqv.id = vsa.biqvId
where biqvId =@biqvId

END
GO