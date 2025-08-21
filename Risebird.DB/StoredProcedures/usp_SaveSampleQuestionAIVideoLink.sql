IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveSampleQuestionAIVideoLink'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveSampleQuestionAIVideoLink
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[usp_SaveSampleQuestionAIVideoLink]
@id int,
@aiVideoLink varchar(max)
AS
Begin

	update SkillChannelSampleQuestions set AIQuestionVideoLink = @aiVideoLink where id = @id

END

GO


