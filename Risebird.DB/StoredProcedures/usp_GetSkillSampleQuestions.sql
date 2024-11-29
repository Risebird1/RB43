IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetSkillSampleQuestions'))
BEGIN
    DROP PROCEDURE dbo.usp_GetSkillSampleQuestions
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_GetSkillSampleQuestions]
@IRFId int
AS
Begin

	Select tsc.* from SkillChannelSampleQuestions tsc where IRFId = @IRFId

END
GO