IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_AssessmentQuestionInsert'))
BEGIN
    DROP PROCEDURE dbo.usp_AssessmentQuestionInsert
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
    
    
Create PROCEDURE [dbo].[usp_AssessmentQuestionInsert]    
(    
      @QuestionsDiscussedBulkData AS [dbo].[AddQuestionDetails_TableType] READONLY    
)    
AS    
    
BEGIN    
    
SET NOCOUNT ON;    
    
--This row performs bulk copy of object DataTable to Table in SQL    
INSERT INTO [dbo].[AssessmentQuestions](    
 [Assessmentid] ,    
 [QuestionCovered],    
 [Skill],    
 [QuestionLevel],    
 [Time],
 [VideoLink])    
     
    
SELECT id as Assessmentid,Question as QuestionCovered,ThumbnailLink as Skill ,Priority as QuestionLevel,VideoTitle as Time,VideoLink FROM @QuestionsDiscussedBulkData    
    
    
END    
  
  
  
  