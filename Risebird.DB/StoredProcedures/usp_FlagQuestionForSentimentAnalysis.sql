IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FlagQuestionForSentimentAnalysis'))
BEGIN
    DROP PROCEDURE dbo.usp_FlagQuestionForSentimentAnalysis
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[usp_FlagQuestionForSentimentAnalysis]
@id int
AS
Begin

   --check whether there exists another question for the same irf that is flagged for sentiment analysis

   declare @irfId int, @existingFlaggedQuestionId int = 0

   set @irfId = (select irfid  from SkillChannelSampleQuestions where id = @id)

   set @existingFlaggedQuestionId = (select id  from SkillChannelSampleQuestions where IRFId = @irfId and IsFlaggedForSentimentAnalysis = 1)

   if(@existingFlaggedQuestionId is null or @existingFlaggedQuestionId != @id)
   Begin
	update SkillChannelSampleQuestions set IsFlaggedForSentimentAnalysis = 1 where id = @id

	if(@existingFlaggedQuestionId is not null and @existingFlaggedQuestionId != 0)
	Begin
		update SkillChannelSampleQuestions set IsFlaggedForSentimentAnalysis = 0 where id = @existingFlaggedQuestionId

	End
   End



END

GO


