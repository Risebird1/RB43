IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddIRFSampleQuestions'))
BEGIN
    DROP PROCEDURE dbo.usp_AddIRFSampleQuestions
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO  
Create PROCEDURE [dbo].[usp_AddIRFSampleQuestions]    
(    
   @IRFSampleQuestionsBulkData AS [dbo].[AddIRFSampleQuestions_TableTypeV7] READONLY,  --AddIRFSampleQuestions_TableTypeV6, AddIRFSampleQuestions_TableTypeV5, AddIRFSampleQuestions_TableTypeV4, AddIRFSampleQuestions_TableTypeV3,  AddIRFSampleQuestions_TableType, AddIRFSampleQuestions_TableTypeV2
   @IRFId int
)
AS
BEGIN
 begin try
  begin tran

   INSERT INTO SkillChannelSampleQuestions([IRFId] ,[Skill],[Comment], CreatedBy, SkillName, ExpectedAnswer, IsCodingQuestion, CodingTimeLimitInMinutes, QuestionGist, IsMcqAssessmentSection)
   SELECT tscbd.IRFId, tscbd.Skill, tscbd.Comment, tscbd.CreatedBy, tscbd.SkillName, tscbd.ExpectedAnswer,
          -- The MCQ section is not asked by the avatar, so it can never also be a coding question.
          -- Forcing the pair apart here keeps a mis-ticked row from becoming an unanswerable question.
          CASE WHEN ISNULL(tscbd.IsMcqAssessmentSection, 0) = 1 THEN 0 ELSE ISNULL(tscbd.IsCodingQuestion, 0) END,
          CASE WHEN ISNULL(tscbd.IsMcqAssessmentSection, 0) = 0 AND ISNULL(tscbd.IsCodingQuestion, 0) = 1
               THEN tscbd.CodingTimeLimitInMinutes ELSE NULL END,
          tscbd.QuestionGist,
          ISNULL(tscbd.IsMcqAssessmentSection, 0)
   FROM @IRFSampleQuestionsBulkData tscbd
  
commit tran  
 end try  
 begin catch  
  rollback transaction  
  DECLARE @Message varchar(MAX) = ERROR_MESSAGE(),  
   @Severity int = ERROR_SEVERITY(),  
   @State smallint = ERROR_STATE()  
  RAISERROR (@Message, @Severity, @State)  
 end catch  
END 
GO