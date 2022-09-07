    
 IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_InterviewerTechSkillReportBulkInsert'))
BEGIN
    DROP PROCEDURE dbo.usp_InterviewerTechSkillReportBulkInsert
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
   
     
CREATE PROCEDURE [dbo].[usp_InterviewerTechSkillReportBulkInsert]      
(      
      @TechSkillReportBulkData AS [dbo].[TechSkillReport_TableType] READONLY      
)      
AS      
BEGIN      
      
SET NOCOUNT ON;      
      
--This row performs bulk copy of object DataTable to Table in SQL      
INSERT INTO [dbo].[InterviewerTechSkillReport](      
 [InterviewId] ,      
 [RequiredLevelId],      
 [SkillId],      
 [ActualLevelId],      
 [WeightageRequired],      
 [WeightageActual])      
      
SELECT InterviewId,RequiredLevelId,SkillId,ActualLevelId,WeightageRequired,WeightageActual FROM @TechSkillReportBulkData      
      
      
END 
GO