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
      @TechSkillReportBulkData AS [dbo].[TechSkillReport_TableType] READONLY  
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
    
SELECT InterviewId,RequiredLevelId,SkillId,ActualLevelId FROM @TechSkillReportBulkData  
  
  
END  

