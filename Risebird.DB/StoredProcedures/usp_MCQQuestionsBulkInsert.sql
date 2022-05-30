IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_MCQQuestionsBulkInsert'))
BEGIN
    DROP PROCEDURE dbo.usp_MCQQuestionsBulkInsert
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
      
CREATE PROCEDURE [dbo].[usp_MCQQuestionsBulkInsert]      
(      
      @MCQQuestionsBulkData AS [dbo].[MCQQuestions_TableType] READONLY      
)      
AS      
BEGIN      
      
SET NOCOUNT ON;      
  DELETE from MCQquestions where MCQId in (Select MCQId From @MCQQuestionsBulkData)    
  
INSERT INTO [dbo].[MCQquestions](      
PartitionKey,  
QuestionType,  
Question,  
Option1,  
Option2,  
Option3,  
Option4,  
Answer,  
Explanation,  
MCQId,  
QuestionNumber)      
SELECT * FROM @MCQQuestionsBulkData      
END 
Fo