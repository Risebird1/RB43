IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_InstaTechSkillBulkInsert'))
BEGIN
    DROP PROCEDURE dbo.usp_InstaTechSkillBulkInsert
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_InstaTechSkillBulkInsert]      
(      
      @TechSkillBulkData AS [dbo].[TransTechSkill_TableType] READONLY      
)      
AS      
BEGIN      
SET NOCOUNT ON;      
  DELETE from TransInstaSkills where IRFID in (Select IRFId From @TechSkillBulkData)    
--This row performs bulk copy of object DataTable to Table in SQL      
INSERT INTO [dbo].[TransInstaSkills](      
 [IRFId] ,      
 [LevelId],      
 [SkillId])      
      
SELECT IRFId,LevelId,SkillId FROM @TechSkillBulkData      
END 