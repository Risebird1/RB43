/****** Object:  StoredProcedure [dbo].[usp_GetAccounts]    Script Date: 24-03-2020 8.20.45 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateTechSkillQuestionsForRemovedTechSkill'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateTechSkillQuestionsForRemovedTechSkill
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO




CREATE PROCEDURE [dbo].[usp_UpdateTechSkillQuestionsForRemovedTechSkill]  
@IRFId int,
@SkillId int

AS  
Begin 


select 1
--delete from TechSkillComments where IRFId =@IRFId and Skill=@SkillId

End
GO


