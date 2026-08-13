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
   @IRFSampleQuestionsBulkData AS [dbo].[AddIRFSampleQuestions_TableTypeV6] READONLY,  --AddIRFSampleQuestions_TableTypeV5, AddIRFSampleQuestions_TableTypeV4, AddIRFSampleQuestions_TableTypeV3,  AddIRFSampleQuestions_TableType, AddIRFSampleQuestions_TableTypeV2
   @IRFId int
)    
AS    
BEGIN    
 begin try  
  begin tran 
	
   INSERT INTO SkillChannelSampleQuestions([IRFId] ,[Skill],[Comment], CreatedBy, SkillName, ExpectedAnswer, IsCodingQuestion, CodingTimeLimitInMinutes, QuestionGist)    
   SELECT tscbd.IRFId, tscbd.Skill, tscbd.Comment, tscbd.CreatedBy, tscbd.SkillName, tscbd.ExpectedAnswer,
          ISNULL(tscbd.IsCodingQuestion, 0),
          CASE WHEN ISNULL(tscbd.IsCodingQuestion, 0) = 1 THEN tscbd.CodingTimeLimitInMinutes ELSE NULL END,
          tscbd.QuestionGist
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