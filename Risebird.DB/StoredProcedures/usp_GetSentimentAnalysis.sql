/****** Object:  StoredProcedure [dbo].[usp_GetSkillById]    Script Date: 28-08-2020 7.26.36 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetSentimentAnalysis'))
BEGIN
    DROP PROCEDURE dbo.usp_GetSentimentAnalysis
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_GetSentimentAnalysis]

@id int

AS
Begin

select EmotionType, [Value] from VideoSentiment where InterviewId = @id

End



GO


