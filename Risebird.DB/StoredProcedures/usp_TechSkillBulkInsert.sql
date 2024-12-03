/****** Object:  StoredProcedure [dbo].[usp_TechSkillBulkInsert]    Script Date: 24-06-2020 1.24.35 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_TechSkillBulkInsert'))
BEGIN
    DROP PROCEDURE dbo.usp_TechSkillBulkInsert
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

  
    
CREATE PROCEDURE [dbo].[usp_TechSkillBulkInsert]    
(    
      @TechSkillBulkData AS [dbo].[TransTechSkill_TableTypeV4] READONLY    --TransTechSkill2_TableType
)    
AS    
BEGIN    
    
SET NOCOUNT ON;    
  DELETE from TransTechSkill where IRFID in (Select IRFId From @TechSkillBulkData)  
--This row performs bulk copy of object DataTable to Table in SQL    
--INSERT INTO [dbo].[TransTechSkill](    
-- [IRFId] ,    
-- [LevelId],    
-- [SkillId],    
-- [Weightage],[Concepts],[Natureofquestion],[Timelimit],[isEliminationskill],[SkillSet])    
    
--SELECT * FROM @TechSkillBulkData    
    
 INSERT INTO [dbo].[TransTechSkill](    
					[IRFId] ,    
					[LevelId],    
					[SkillId],    
					[Weightage],[Concepts],[Natureofquestion],[Timelimit],[isEliminationskill],[SkillSet], [SkillName])    
    
SELECT tsbd.IRFId, tsbd.LevelId, tsbd.SkillId, tsbd.Weightage, tsbd.Concepts, tsbd.Natureofquestion, 
tsbd.Timelimit,tsbd.isEliminationskill,tsbd.SkillSet, tsbd.SkillName FROM @TechSkillBulkData tsbd
					   
END 

GO


