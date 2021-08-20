IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_InstaTechSkillReportBulkInsert'))
BEGIN
    DROP PROCEDURE dbo.usp_InstaTechSkillReportBulkInsert
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_InstaTechSkillReportBulkInsert]    
(    
      @TechSkillReportBulkData AS [dbo].[TransInstaTechSkill_TableType] READONLY    
)    
AS    
BEGIN    
SET NOCOUNT ON;    
--This row performs bulk copy of object DataTable to Table in SQL    
  DELETE from TransInstaSkills where IRFID in (Select IRFId From @TechSkillReportBulkData)    
--This row performs bulk copy of object DataTable to Table in SQL      
INSERT INTO [dbo].[TransInstaSkills](      
 [IRFId] ,      
 [LevelId],      
 [SkillId],  
 [ActualLevelId])      
      
SELECT IRFId,LevelId,SkillId,ActualLevelId FROM @TechSkillReportBulkData    
    
    
END    
  
GO
