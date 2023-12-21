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
   @IRFSampleQuestionsBulkData AS [dbo].[AddIRFSampleQuestions_TableType] READONLY,  
   @IRFId int
)    
AS    
BEGIN    
 begin try  
  begin tran 
	
   INSERT INTO SkillChannelSampleQuestions([IRFId] ,[Skill],[Comment], CreatedBy)    
   SELECT tscbd.IRFId, tscbd.Skill, tscbd.Comment, tscbd.CreatedBy FROM @IRFSampleQuestionsBulkData tscbd
  
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