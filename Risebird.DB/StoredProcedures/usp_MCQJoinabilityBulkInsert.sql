
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_MCQJoinabilityBulkInsert'))
BEGIN
    DROP PROCEDURE dbo.usp_MCQJoinabilityBulkInsert
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_MCQJoinabilityBulkInsert]      
(      
      @JoinabilityBulkData AS [dbo].[TransMCQSelectability_TableType] READONLY      
)      
AS      
BEGIN      
      
SET NOCOUNT ON;      
  DELETE from MCQ_TransJoinability where CMCQId in (Select CMCQId From @JoinabilityBulkData)    
--This row performs bulk copy of object DataTable to Table in SQL      
INSERT INTO [dbo].[MCQ_TransJoinability](      
 [MCQId],  
[CMCQId],  
[CandidateEmail],  
[ApplyingCompany],  
[Type],  
[Value],  
[LevelId],  
[RequiredLevelId])      
SELECT * FROM @JoinabilityBulkData      
END 

GO


