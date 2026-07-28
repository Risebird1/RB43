IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddAIInterviewStatusWebHookData'))
BEGIN
    DROP PROCEDURE dbo.usp_AddAIInterviewStatusWebHookData
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
    
Create PROCEDURE [dbo].[usp_AddAIInterviewStatusWebHookData]    
(    
    
   @candidateEmailId varchar(200),  
   @IRFName varchar(25),
   @status varchar(50) = null
)    
AS    
BEGIN    
  
 begin try  
  begin tran  
  -- only queue status updates for skill channels that are AI interviews
  declare @isAIInterview bit = (select ISNULL(IsAIInterview, 0) from IRF where irfid = @IRFName)
  if(@isAIInterview = 1)
  Begin
	  if not exists (select top 1* from AIInterviewStatusWebHookData where IRFId = @IRFName and CandidateEmailId = @candidateEmailId)
	  Begin
		insert into AIInterviewStatusWebHookData values (@IRFName, @candidateEmailId, @status, getdate())
	  End
	  Else
	  Begin
		update AIInterviewStatusWebHookData set Status = @status, PerformedAt = getdate() where IRFId = @IRFName and CandidateEmailId = @candidateEmailId
	  End
  End
  

    
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
