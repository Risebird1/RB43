IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_InterviewRequestTechSkillBulkInsert'))
BEGIN
    DROP PROCEDURE [dbo].[usp_InterviewRequestTechSkillBulkInsert]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_InterviewRequestTechSkillBulkInsert]        
(        
      @TechSkillBulkData AS [dbo].[TransInstaTechSkill_TableType] READONLY        
)        
AS        
BEGIN        
SET NOCOUNT ON;        
  DELETE from TransInterviewRequestSkills where IRFID in (Select IRFId From @TechSkillBulkData)      
      
INSERT INTO [dbo].[TransInterviewRequestSkills](        
 [IRFId] ,        
 [LevelId],        
 [SkillId])        
        
SELECT IRFId,LevelId,SkillId FROM @TechSkillBulkData        
END   
GO