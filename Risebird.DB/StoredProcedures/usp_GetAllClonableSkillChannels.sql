
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAllClonableSkillChannels'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAllClonableSkillChannels
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetAllClonableSkillChannels]  

AS  
Begin

	select csc.* from ClonableSkillChannel csc join IRFRecommendations ir on ir.CloneIRFId = csc.id
	where ir.[Status] = 1

End
GO


