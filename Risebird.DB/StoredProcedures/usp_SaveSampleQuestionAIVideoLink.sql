IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveSampleQuestionAIVideoLink'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveSampleQuestionAIVideoLink
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- @aiVideoJobId carries the in-flight Synthesia render so a retry can resume it rather than start
-- a second one. Passing a link clears it: the render that job represented has now been collected.
Create PROCEDURE [dbo].[usp_SaveSampleQuestionAIVideoLink]
@id int,
@aiVideoLink varchar(max) = null,
@aiVideoJobId varchar(100) = null
AS
Begin

	update SkillChannelSampleQuestions
	set AIQuestionVideoLink = ISNULL(@aiVideoLink, AIQuestionVideoLink),
	    AIQuestionVideoJobId = CASE WHEN @aiVideoLink IS NOT NULL THEN NULL ELSE @aiVideoJobId END
	where id = @id

END

GO


