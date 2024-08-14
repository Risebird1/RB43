IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_DeleteQuestionsDiscussedFromReport'))
BEGIN
    DROP PROCEDURE dbo.usp_DeleteQuestionsDiscussedFromReport
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_DeleteQuestionsDiscussedFromReport]     
@interviewId int
As
Begin
	begin try  
		begin tran 
			delete from QuestionsDiscussed where InterviewId = @interviewId
		commit tran  
	end try  
	begin catch  
 
		rollback transaction  
 
		DECLARE @Message varchar(MAX) = ERROR_MESSAGE(),  
		 @Severity int = ERROR_SEVERITY(),  
		 @State smallint = ERROR_STATE()  
  
	RAISERROR (@Message, @Severity, @State)  
  
	end catch  



End
GO