IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_MCQSelectabilityBulkInsert'))
BEGIN
    DROP PROCEDURE dbo.usp_MCQSelectabilityBulkInsert
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

  
CREATE PROCEDURE [dbo].[usp_MCQSelectabilityBulkInsert]    
(    
      @SelectabilityBulkData AS [dbo].[TransMCQSelectability_TableType] READONLY    
)    
AS    
BEGIN    
    
SET NOCOUNT ON;    
  DELETE from MCQ_TransSelectability where CMCQId in (Select CMCQId From @SelectabilityBulkData)  

INSERT INTO [dbo].[MCQ_TransSelectability](    
 [MCQId],
[CMCQId],
[CandidateEmail],
[ApplyingCompany],
[Type],
[Value],
[LevelId],
[RequiredLevelId])    
SELECT * FROM @SelectabilityBulkData    
END    

GO


