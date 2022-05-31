IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_MCQCandidateQuestionsBulkInsert'))
BEGIN
    DROP PROCEDURE dbo.usp_MCQCandidateQuestionsBulkInsert
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
        
CREATE PROCEDURE [dbo].[usp_MCQCandidateQuestionsBulkInsert]        
(        
      @MCQCandidateQuestionsBulkData AS [dbo].[MCQCandidateQuestions_TableType] READONLY        
)        
AS        
BEGIN        
        
SET NOCOUNT ON;        
    
INSERT INTO [dbo].[MCQCandidateQuestionAnswer](        
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
QuestionNumber,  
CandidateID,  
MultiAnswerOption1,  
MultiAnswerOption2,  
MultiAnswerOption3,  
MultiAnswerOption4,  
QuestionId,  
Rating,  
QuestionAnswer,  
SingleAnswer)        
SELECT * FROM @MCQCandidateQuestionsBulkData        
END 
Go